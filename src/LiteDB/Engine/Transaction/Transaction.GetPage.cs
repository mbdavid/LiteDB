namespace LiteDB.Engine;

/// <summary>
/// </summary>
internal partial class Transaction : ITransaction
{
    /// <summary>
    /// Get a existing page on database based on ReadVersion. Try get first from localPages,
    /// cache and in last case read from disk (and add to localPages)
    /// </summary>
    public async ValueTask<nint> GetPageAsync(uint pageID)
    {
        using var _pc = PERF_COUNTER(90, nameof(GetPageAsync), nameof(Transaction));

        ENSURE(pageID != uint.MaxValue, "PageID must have a value");

        if (_localPages.TryGetValue(pageID, out var ptr))
        {
            unsafe
            {
                var page = (PageMemory*)ptr;

                // if writable, page should not be in cache
                ENSURE(Array.IndexOf(_writeCollections, page->ColID) > -1, page->ShareCounter == NO_CACHE, "Page should not be in cache", new { _writeCollections });
            }

            return ptr;
        }

        var newPagePtr = await this.ReadPageAsync(pageID, this.ReadVersion);

        _localPages.Add(pageID, newPagePtr);

        return newPagePtr;
    }

    /// <summary>
    /// Read a data/index page from disk (data or log). Can return page from global cache
    /// </summary>
    private async ValueTask<nint> ReadPageAsync(uint pageID, int readVersion)
    {
        using var _pc = PERF_COUNTER(100, nameof(ReadPageAsync), nameof(Transaction));

        var writable = false;
        var found = false;

        // test if page are in local wal pages
        if (_walDirtyPages.TryGetValue(pageID, out var positionID))
        {
            // if page are in local wal, try get from cache
            var cachePagePtr = _memoryCache.GetPageReadWritePtr(positionID, _writeCollections, out writable, out found);

            if (found == false)
            {
                // if not found, allocate new page
                var walPagePtr = _memoryFactory.AllocateNewPagePtr();

                await _diskService.ReadPageAsync(walPagePtr, positionID);

                //ENSURE(walPage->PageType == PageType.Data || walPage->PageType == PageType.Index, $"Only data/index page on transaction read page: {walPage->PageID}");

                return walPagePtr;
            }
            else
            {
                return cachePagePtr;
            }
        }

        // get disk position from global wal (data/log)
        positionID = _walIndexService.GetPagePositionID(pageID, readVersion, out _);

        // get a page from cache (if writable, this page are not linked to cache anymore)
        var ptr = _memoryCache.GetPageReadWritePtr(positionID, _writeCollections, out writable, out found);

        // if page not found, allocate new page and read from disk
        if (found == false)
        {
            ptr = _memoryFactory.AllocateNewPagePtr();

            await _diskService.ReadPageAsync(ptr, positionID);

            //ENSURE(page->PageType == PageType.Data || page->PageType == PageType.Index, $"Only data/index page on transaction read page: {page->PageID}");
        }

        return ptr;
    }

    /// <summary>
    /// Get a Data Page with, at least, 30% free space
    /// </summary>
    public async ValueTask<nint> GetFreeDataPageAsync(byte colID)
    {
        using var _pc = PERF_COUNTER(110, nameof(GetFreeDataPageAsync), nameof(Transaction));

        var colIndex = Array.IndexOf(_writeCollections, colID);
        var currentExtend = _currentDataExtend[colIndex];

        // request for allocation map service a new PageID for this collection
        var (pageID, isNew, nextExtend) = _allocationMapService.GetFreeExtend(currentExtend, colID, PageType.Data);

        // update current collection extend location
        _currentDataExtend[colIndex] = nextExtend;

        if (isNew)
        {
            var ptr = _memoryFactory.AllocateNewPagePtr();

            // initialize empty page as data page
            PageMemory.InitializeAsDataPage(ptr, pageID, colID);

            // add in local cache
            _localPages.Add(pageID, ptr);

            return ptr;
        }
        else
        {
            // if page already exists, just get page
            var ptr = await this.GetPageAsync(pageID);

            return ptr;
        }
    }

    /// <summary>
    /// Get a Index Page with space enougth for index node
    /// </summary>
    public async ValueTask<nint> GetFreeIndexPageAsync(byte colID, int indexNodeLength)
    {
        using var _pc = PERF_COUNTER(120, nameof(GetFreeIndexPageAsync), nameof(Transaction));

        var colIndex = Array.IndexOf(_writeCollections, colID);
        var currentExtend = _currentIndexExtend[colIndex];

        // request for allocation map service a new PageID for this collection
        var (pageID, isNew, nextExtend) = _allocationMapService.GetFreeExtend(currentExtend, colID, PageType.Index);

        // update current collection extend location
        _currentIndexExtend[colIndex] = nextExtend;

        if (isNew)
        {
            var ptr = _memoryFactory.AllocateNewPagePtr();

            // initialize empty page as index page
            PageMemory.InitializeAsIndexPage(ptr, pageID, colID);

            // add in local cache
            _localPages.Add(pageID, ptr);

            return ptr;
        }
        else
        {
            var ptr = await this.GetPageAsync(pageID);

            // get free bytes from readed page
            int freeBytes;

            unsafe
            {
                var page = (PageMemory*)ptr;
                freeBytes = page->FreeBytes;
            }

            // get how many avaiable bytes (excluding new added record) this page contains
            var pageAvailableSpace =
                freeBytes -
                indexNodeLength -
                8; // extra align

            // if current page has no avaiable space (super rare cases), get another page
            if (pageAvailableSpace < indexNodeLength)
            {
                // set this page as full before get next page
                //**this.UpdatePageMap(page->PageID, ExtendPageValue.Full);
                this.UpdatePageMap(pageID, ExtendPageValue.Full);

                // call recursive to get another page
                return await this.GetFreeIndexPageAsync(colID, indexNodeLength);
            }

            return ptr;
        }
    }
}