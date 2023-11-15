namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
unsafe internal class MemoryFactory : IMemoryFactory
{
    private readonly ConcurrentDictionary<nint, (GCHandle handle, byte[] buffer)> _pages = new();

    private readonly ConcurrentDictionary<int, nint> _inUsePages = new();
    private readonly ConcurrentQueue<nint> _freePages = new();

    private int _nextUniqueID = BUFFER_UNIQUE_ID - 1;
    private int _pagesAllocated = 0;

    public MemoryFactory()
    {
    }

    /// <summary>
    /// Allocate new byte[PAGE_SIZE] (or get from cache) and return pointer to (PageMemory*)
    /// </summary>
    /// <returns></returns>
    public nint AllocateNewPage()
    {
        if (_freePages.TryDequeue(out var ptr))
        {
            var page = (PageMemory*)ptr;

            _inUsePages.TryAdd(page->UniqueID, ptr);

            return ptr;
        }

        // create new byte[] in memory and get pinned point to use with pointer types
        var buffer = new byte[PAGE_SIZE];
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        ptr = handle.AddrOfPinnedObject();

        // add this page into local map
        var added = _pages.TryAdd(ptr, (handle, buffer));

        ENSURE(added);

        var newPage = (PageMemory*)ptr;

        var uniqueID = Interlocked.Increment(ref _nextUniqueID);
        Interlocked.Increment(ref _pagesAllocated);

        // clear page and initialize with uniqueID
        PageMemory.Initialize(newPage, uniqueID);

        _inUsePages.TryAdd(newPage->UniqueID, (nint)newPage);

        return ptr;
    }

    public void DeallocatePage(nint ptr)
    {
        // cast pointer type as PageMemory pointer
        var page = (PageMemory*)ptr;

        ENSURE(page->ShareCounter != NO_CACHE, page->ShareCounter != 0, "ShareCounter must be 0 before return page to memory");

        // remove from inUse pages 
        var removed = _inUsePages.TryRemove(page->UniqueID, out _);

        ENSURE(removed, new { _pagesAllocated, _freePages, _inUsePages });

        // clear page
        PageMemory.Initialize(page, page->UniqueID);

        // add used page as new free page
        _freePages.Enqueue((nint)page);
    }

    /// <summary>
    /// Get buffer array (in managed memory) from a page pointer
    /// </summary>
    public byte[] GetPageArray(nint ptr)
    {
        var found = _pages.TryGetValue(ptr, out var result);

        ENSURE(found);

        return result.buffer;
    }

    public override string ToString()
    {
        return Dump.Object(this);
    }

    public void Dispose()
    {
        ENSURE(_pagesAllocated == (_inUsePages.Count + _freePages.Count));

        // release memory
        foreach(var (handle, _) in _pages.Values)
        {
            handle.Free();
        }

        _pages.Clear();
        _inUsePages.Clear();
        _freePages.Clear();

        _nextUniqueID = BUFFER_UNIQUE_ID - 1;
        _pagesAllocated = 0;
    }
}
