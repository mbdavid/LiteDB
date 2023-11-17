namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
unsafe internal class MemoryFactory : IMemoryFactory
{
    private readonly ConcurrentDictionary<int, PageMemoryResult> _inUsePages = new();
    private readonly ConcurrentQueue<PageMemoryResult> _freePages = new();

    private int _nextUniqueID = BUFFER_UNIQUE_ID - 1;
    private int _pagesAllocated = 0;

    public MemoryFactory()
    {
    }

    /// <summary>
    /// Allocate new byte[PAGE_SIZE] (or get from cache) and return pointer to (PageMemory*)
    /// </summary>
    public PageMemoryResult AllocateNewPage()
    {
        if (_freePages.TryDequeue(out var page))
        {
            _inUsePages.TryAdd(page.UniqueID, page);

            return page;
        }

        var uniqueID = Interlocked.Increment(ref _nextUniqueID);

        Interlocked.Increment(ref _pagesAllocated);

        page = new PageMemoryResult(uniqueID);

        _inUsePages.TryAdd(uniqueID, page);

        return page;
    }

    public void DeallocatePage(PageMemoryResult page)
    {
        ENSURE(page.ShareCounter != NO_CACHE, page.ShareCounter != 0, "ShareCounter must be 0 before return page to memory");

        // remove from inUse pages 
        var removed = _inUsePages.TryRemove(page.UniqueID, out _);

        ENSURE(removed, new { _pagesAllocated, _freePages, _inUsePages });

        // clear page
        page.Initialize();

        // add used page as new free page
        _freePages.Enqueue(page);

        // i can decide here if _freePages are too high it's possible to deallocate some byte[]
    }

    public override string ToString()
    {
        return Dump.Object(this);
    }

    public void Dispose()
    {
        ENSURE(_pagesAllocated == (_inUsePages.Count + _freePages.Count));

        // release memory
        foreach (var page in _freePages)
        {
            page.Dispose();
        }

        foreach (var page in _inUsePages.Values)
        {
            page.Dispose();
        }

        _inUsePages.Clear();
        _freePages.Clear();

        _nextUniqueID = BUFFER_UNIQUE_ID - 1;
        _pagesAllocated = 0;
    }
}
