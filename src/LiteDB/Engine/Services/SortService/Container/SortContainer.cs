namespace LiteDB.Engine;

[AutoInterface(typeof(IDisposable))]
unsafe internal class SortContainer : ISortContainer
{
    // dependency injections
    private readonly IMemoryFactory _memoryFactory;
    private readonly Collation _collation;

    private readonly int _containerID;
    private readonly int _order;
    private readonly Stream _stream;

    private byte[] _buffer; // 8k page buffe

    private SortItem _current; // current sorted item
    private int _pageIndex = -1; // current read page
    private int _position = 0; // current read position

    private int _containerRemaining; // remaining items on this container 
    private int _pageRemaining; // remaining items on current page

    public SortContainer(
        Collation collation,
        int containerID,
        int order,
        Stream stream)
    {
        _collation = collation;
        _containerID = containerID;
        _order = order;
        _stream = stream;

        // rent a full 8k buffer in managed memory
        _buffer = ArrayPool<byte>.Shared.Rent(PAGE_SIZE);
    }

    /// <summary>
    /// Get container ID on disk
    /// </summary>
    public int ContainerID => _containerID;

    /// <summary>
    /// Get current readed item
    /// </summary>
    public SortItem Current => _current;

    /// <summary>
    /// Get how many items are not readed from container (if 0 all container already readed)
    /// </summary>
    public int Remaining => _containerRemaining;

    /// <summary>
    /// Sort all unsorted items based on order. Write all bytes into buffer only. 
    /// Organized all items in 8k pages, with first 2 bytes to contains how many items this page contains
    /// Remaining items are not inserted in this container e must be returned to be added into a new container
    /// </summary>
    public int Sort(IEnumerable<SortItem> unsortedItems, byte[] containerBuffer, List<SortItem> remaining)
    {
        // order items
        var query = _order == Query.Ascending ?
            unsortedItems.OrderBy(x => x.Key, _collation) : unsortedItems.OrderByDescending(x => x.Key, _collation);

        var pagePosition = 2; // first 2 bytes per page was used to store how many item will contain this page
        short pageItems = 0;
        var pageCount = 0;
        var remainingBytes = 0;

        var span = containerBuffer.AsSpan(0, PAGE_SIZE);

        foreach (var orderedItem in query)
        {
            var itemSize = orderedItem.GetBytesCount();

            // test if this new 
            if (pagePosition + itemSize > PAGE_SIZE)
            {
                // use first 2 bytes to store how many sort items this page has
                span[0..].WriteInt16(pageItems);

                pageItems = 0;
                pagePosition = 2;
                pageCount++;

                // define span as new page
                span = containerBuffer.AsSpan(pageCount * PAGE_SIZE, PAGE_SIZE);
            }

            // if need more pages than _containerSizeInPages, add to "remaining" list to be added in another container
            if (pageCount >= CONTAINER_SORT_SIZE_IN_PAGES)
            {
                remainingBytes += itemSize;

                remaining.Add(orderedItem);
            }
            else
            {
                // write DataBlockID, Key on buffer
                span[pagePosition..].WriteRowID(orderedItem.DataBlockID);

                pagePosition += sizeof(RowID);

                span[pagePosition..].WriteBsonValue(orderedItem.Key, out var keyLength);

                pagePosition += keyLength;

                // increment total container items
                pageItems++;
                _containerRemaining++;
            }
        }

        return remainingBytes;
    }

    /// <summary>
    /// Move "Current" to next item on this container. Returns false if eof
    /// </summary>
    public bool MoveNext()
    {
        if (_containerRemaining == 0) return false;

        if (_pageRemaining == 0)
        {
            // set stream position to page position (increment pageIndex before)
            _stream.Position = (_containerID * (CONTAINER_SORT_SIZE_IN_PAGES * PAGE_SIZE)) + (++_pageIndex * PAGE_SIZE);

            _stream.Read(_buffer, 0, PAGE_SIZE);

            // set position and read remaining page items
            _position = 2; // for int16
            _pageRemaining = _buffer.AsSpan(0, 2).ReadInt16();
        }

        var itemSize = this.ReadCurrent();

        _position += itemSize;
        _pageRemaining--;
        _containerRemaining--;

        return true;
    }

    /// <summary>
    /// Read current item on buffer and return item length
    /// </summary>
    private int ReadCurrent()
    {
        var span = _buffer.AsSpan(_position);

        var dataBlockID = span[0..].ReadRowID();
        var key = span[sizeof(RowID)..].ReadBsonValue(out var keyLength);

        // set current item
        _current = new SortItem(dataBlockID, key);

        return sizeof(RowID) + keyLength;
    }

    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(_buffer);
    }
}
