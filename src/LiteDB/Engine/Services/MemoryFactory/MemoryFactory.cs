namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
unsafe internal class MemoryFactory : IMemoryFactory
{
    private readonly ConcurrentDictionary<int, nint> _inUsePages = new();
    private readonly ConcurrentQueue<nint> _freePages = new();

    private int _nextUniqueID = BUFFER_UNIQUE_ID - 1;
    private int _pagesAllocated = 0;

    public MemoryFactory()
    {
    }

    public PageMemory* AllocateNewPage()
    {
        if (_freePages.TryDequeue(out var ptr))
        {
            var page = (PageMemory*)ptr;

            _inUsePages.TryAdd(page->UniqueID, ptr);

            return page;
        }

        // get memory pointer from unmanaged memory
        var newPage = (PageMemory*)Marshal.AllocHGlobal(sizeof(PageMemory));

        var uniqueID = Interlocked.Increment(ref _nextUniqueID);
        Interlocked.Increment(ref _pagesAllocated);

        // clear page and initialize with uniqueID
        PageMemory.Initialize(newPage, uniqueID);

        _inUsePages.TryAdd(newPage->UniqueID, (nint)newPage);

        return newPage;
    }

    public void DeallocatePage(PageMemory* page)
    {
        ENSURE(page->ShareCounter != NO_CACHE, page->ShareCounter != 0, "ShareCounter must be 0 before return page to memory");

        // remove from inUse pages 
        var removed = _inUsePages.TryRemove(page->UniqueID, out _);

        ENSURE(removed, new { _pagesAllocated, _freePages, _inUsePages });

        // clear page
        PageMemory.Initialize(page, page->UniqueID);

        // add used page as new free page
        _freePages.Enqueue((nint)page);
    }


    public override string ToString()
    {
        return Dump.Object(this);
    }

    public void Dispose()
    {
        ENSURE(_pagesAllocated == (_inUsePages.Count + _freePages.Count));

        // release unmanaged memory
        foreach (var ptr in _inUsePages.Values)
        {
            Marshal.FreeHGlobal(ptr);
        }
        foreach (var ptr in _freePages)
        {
            Marshal.FreeHGlobal(ptr);
        }

        _inUsePages.Clear();
        _freePages.Clear();

        _nextUniqueID = BUFFER_UNIQUE_ID - 1;
        _pagesAllocated = 0;
    }
}
