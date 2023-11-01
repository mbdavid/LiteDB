namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
unsafe internal class MemoryCache : IMemoryCache
{
    // dependency injection
    private readonly IMemoryFactory _memoryFactory;

    /// <summary>
    /// A dictionary to cache use/re-use same data buffer indexed by PositionID
    /// </summary>
    private readonly ConcurrentDictionary<uint, nint> _cache = new();

    public int ItemsCount => _cache.Count;

    public MemoryCache(IMemoryFactory memoryFactory)
    {
        _memoryFactory = memoryFactory;
    }

    public PageMemory* GetPageReadWrite(uint positionID, byte[] writeCollections, out bool writable, out bool found)
    {
        using var _pc = PERF_COUNTER(101, nameof(GetPageReadWrite), nameof(MemoryCache));

        found = _cache.TryGetValue(positionID, out var ptr);

        if (!found)
        {
            writable = false;
            return null;
        }

        using var _ph = PERF_COUNTER(102, nameof(GetPageReadWrite) + " (hit)", nameof(MemoryCache));

        var page = (PageMemory*)ptr;

        ENSURE(page->ShareCounter != NO_CACHE);

        // test if this page are getted from a writable collection in transaction
        writable = Array.IndexOf(writeCollections, page->ColID) > -1;

        if (writable == false) // read only - copy same copy as in memory (+1 sharecounter)
        {
            // get page for read-only
            ENSURE(page->ShareCounter != NO_CACHE);

            // increment ShareCounter to be used by another transaction
            Interlocked.Increment(ref page->ShareCounter);

            return page;
        }
        else
        {
            // if no one are using, remove from cache (double check)
            if (page->ShareCounter == 0)
            {
                var removed = _cache.TryRemove(positionID, out _);

                ENSURE(removed, new { removed, self = this });

                // clean share counter after remove from cache
                page->ShareCounter = NO_CACHE;
                page->IsDirty = false;

                return page;
            }
            else
            {
                // if page is in use, create a new page
                var newPage = _memoryFactory.AllocateNewPage();

                // keep uniqueID
                var uniqueID = page->UniqueID;

                // get span from each page pointer
                var sourceSpan = new Span<byte>(page, PAGE_SIZE);
                var targetSpan = new Span<byte>(newPage, PAGE_SIZE);

                sourceSpan.CopyTo(targetSpan);

                // clean page after copy all content
                newPage->UniqueID = uniqueID;
                newPage->ShareCounter = NO_CACHE;
                newPage->IsDirty = false;

                // and return as a new page instance
                return newPage;
            }
        }

        throw new NotSupportedException();
    }

    /// <summary>
    /// Remove page from cache. Must not be in use
    /// </summary>
    public bool TryRemove(uint positionID, [MaybeNullWhen(false)] out PageMemory* page)
    {
        // first try to remove from cache
        if (_cache.TryRemove(positionID, out var ptr))
        {
            page = (PageMemory*)ptr;

            page->ShareCounter = NO_CACHE;

            return true;
        }

        page = default;

        return false;
    }

    /// <summary>
    /// Add a new page to cache. Returns true if page was added. If returns false,
    /// page are not in cache and must be released in bufferFactory
    /// </summary>
    public bool AddPageInCache(PageMemory* page)
    {
        ENSURE(!page->IsDirty, "PageMemory must be clean before add into cache");
        ENSURE(page->PositionID != uint.MaxValue, "PageMemory must have a position before add in cache");
        ENSURE(page->ShareCounter == NO_CACHE);
        ENSURE(page->PageType == PageType.Data || page->PageType == PageType.Index);

        // before add, checks cache limit and cleanup if full
        if (_cache.Count >= CACHE_LIMIT)
        {
            var clean = this.CleanUp();

            // all pages are in use, do not add this page in cache (cache full used)
            if (clean == 0)
            {
                return false;
            }
        }

        // try add into cache before change page
        var added = _cache.TryAdd(page->PositionID, (nint)page);

        if (!added) return false;

        // initialize shared counter
        page->ShareCounter = 0;

        return true;
    }

    public void ReturnPageToCache(PageMemory* page)
    {
        ENSURE(!page->IsDirty); // vai sair essa regra na disk queue
        ENSURE(page->ShareCounter > 0);
        ENSURE(page->ShareCounter != NO_CACHE);

        Interlocked.Decrement(ref page->ShareCounter);
    }

    /// <summary>
    /// Try remove pages with ShareCounter = 0 (not in use) and release this
    /// pages from cache. Returns how many pages was removed
    /// </summary>
    public int CleanUp()
    {
        var limit = (int)(CACHE_LIMIT * .3); // 30% of CACHE_LIMIT

        var positions = _cache.Values
            .Where(x => ((PageMemory*)x)->ShareCounter == 0)
//            .OrderByDescending(x => x.Timestamp)
            .Select(x => ((PageMemory*)x)->PositionID)
            .Take(limit)
            .ToArray();

        var total = 0;

        foreach(var positionID in positions)
        {
            var removed = _cache.TryRemove(positionID, out var ptr);

            if (!removed) continue;

            var page = (PageMemory*)ptr;

            // double check
            if (page->ShareCounter == 0)
            {
                // set page out of cache
                page->ShareCounter = NO_CACHE;

                // deallocate page
                _memoryFactory.DeallocatePage(page);

                total++;
            }
            else
            {
                // page should be re-added to cache
                var added = _cache.TryAdd(positionID, ptr);

                if (!added)
                {
                    throw new NotImplementedException("problema de concorrencia. não posso descartar paginas.. como fazer? manter em lista paralela?");
                }
            }
        }

        return total;
    }

    /// <summary>
    /// Remove from cache all logfile pages. Keeps only page that are from datafile. Used after checkpoint operation
    /// </summary>
    public void ClearLogPages()
    {
        var logPositions = _cache.Values
            .Where(x => ((PageMemory*)x)->PositionID != ((PageMemory*)x)->PageID)
            .Select(x => ((PageMemory*)x)->PositionID)
            .ToArray();

        foreach(var logPosition in logPositions)
        {
            _cache.Remove(logPosition, out var ptr);

            var page = (PageMemory*)ptr;

            page->ShareCounter = NO_CACHE;

            _memoryFactory.DeallocatePage(page);
        }
    }

    public override string ToString()
    {
        return Dump.Object(new { _cache });
    }

    public void Dispose()
    {
        ENSURE(_cache.Count(x => ((PageMemory*)x.Value) -> ShareCounter != 0) == 0, "Cache must be clean before dipose");

#if DEBUG
        // in DEBUG mode, let's clear all pages one-by-one
        foreach(var ptr in _cache.Values)
        {
            var page = (PageMemory*)ptr;
            page->ShareCounter = NO_CACHE;
            _memoryFactory.DeallocatePage(page);
        }
#endif

        // deattach PageBuffers from _cache object
        _cache.Clear();
    }
}