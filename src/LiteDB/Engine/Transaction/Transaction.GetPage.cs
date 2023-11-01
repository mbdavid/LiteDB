namespace LiteDB.Engine;

/// <summary>
/// </summary>
internal partial class Transaction : ITransaction
{
    /// <summary>
    /// Get a existing page on database based on ReadVersion. Try get first from localPages,
    /// cache and in last case read from disk (and add to localPages)
    /// </summary>
    public unsafe PageMemory* GetPage(uint pageID)
    {
        using var _pc = PERF_COUNTER(90, nameof(GetPage), nameof(Transaction));

        ENSURE(pageID != uint.MaxValue, "PageID must have a value");

        if (_localPages.TryGetValue(pageID, out var ptr))
        {
            var page = (PageMemory*)ptr;

            // if writable, page should not be in cache
            ENSURE(Array.IndexOf(_writeCollections, page->ColID) > -1, page->ShareCounter == NO_CACHE, "Page should not be in cache", new { _writeCollections });

            return page;
        }

        var newPage = this.ReadPage(pageID, this.ReadVersion);

        _localPages.Add(pageID, (nint)newPage);

        return newPage;
    }

    /// <summary>
    /// Read a data/index page from disk (data or log). Can return page from global cache
    /// </summary>
    private unsafe PageMemory* ReadPage(uint pageID, int readVersion)
    {
        using var _pc = PERF_COUNTER(100, nameof(ReadPage), nameof(Transaction));

        _reader ??= _diskService.RentDiskReader();

        var writable = false;
        var found = false;

        // test if page are in local wal pages
        if (_walDirtyPages.TryGetValue(pageID, out var positionID))
        {
            // if page are in local wal, try get from cache
            var cachePage = _memoryCache.GetPageReadWrite(positionID, _writeCollections, out writable, out found);

            if (found == false)
            {
                // if not found, allocate new page
                var walPage = _memoryFactory.AllocateNewPage();

                _reader.ReadPage(walPage, positionID);

                ENSURE(walPage->PageType == PageType.Data || walPage->PageType == PageType.Index, $"Only data/index page on transaction read page: {walPage->PageID}");

                return walPage;
            }
            else
            {
                return cachePage;
            }
        }

        // get disk position from global wal (data/log)
        positionID = _walIndexService.GetPagePositionID(pageID, readVersion, out _);

        // get a page from cache (if writable, this page are not linked to cache anymore)
        var page = _memoryCache.GetPageReadWrite(positionID, _writeCollections, out writable, out found);

        // if page not found, allocate new page and read from disk
        if (found == false)
        {
            page = _memoryFactory.AllocateNewPage();

            _reader.ReadPage(page, positionID);

            ENSURE(page->PageType == PageType.Data || page->PageType == PageType.Index, $"Only data/index page on transaction read page: {page->PageID}");
        }

        return page;
    }

    /// <summary>
    /// Get a Data Page with, at least, 30% free space
    /// </summary>
    public unsafe PageMemory* GetFreeDataPage(byte colID)
    {
        using var _pc = PERF_COUNTER(110, nameof(GetFreeDataPage), nameof(Transaction));

        var colIndex = Array.IndexOf(_writeCollections, colID);
        var currentExtend = _currentDataExtend[colIndex];

        // request for allocation map service a new PageID for this collection
        var (pageID, isNew, nextExtend) = _allocationMapService.GetFreeExtend(currentExtend, colID, PageType.Data);

        // update current collection extend location
        _currentDataExtend[colIndex] = nextExtend;

        if (isNew)
        {
            var page = _memoryFactory.AllocateNewPage();

            // initialize empty page as data page
            PageMemory.InitializeAsDataPage(page, pageID, colID);

            // add in local cache
            _localPages.Add(pageID, (nint)page);

            return page;
        }
        else
        {
            // if page already exists, just get page
            var page = this.GetPage(pageID);

            return page;
        }
    }

    /// <summary>
    /// Get a Index Page with space enougth for index node
    /// </summary>
    public unsafe PageMemory* GetFreeIndexPage(byte colID, int indexNodeLength)
    {
        using var _pc = PERF_COUNTER(120, nameof(GetFreeIndexPage), nameof(Transaction));

        var colIndex = Array.IndexOf(_writeCollections, colID);
        var currentExtend = _currentIndexExtend[colIndex];

        // request for allocation map service a new PageID for this collection
        var (pageID, isNew, nextExtend) = _allocationMapService.GetFreeExtend(currentExtend, colID, PageType.Index);

        // update current collection extend location
        _currentIndexExtend[colIndex] = nextExtend;

        if (isNew)
        {
            var page = _memoryFactory.AllocateNewPage();

            // initialize empty page as index page
            PageMemory.InitializeAsIndexPage(page, pageID, colID);

            // add in local cache
            _localPages.Add(pageID, (nint)page);

            return page;
        }
        else
        {
            var page = this.GetPage(pageID);

            // get how many avaiable bytes (excluding new added record) this page contains
            var pageAvailableSpace =
                page->FreeBytes -
                indexNodeLength -
                8; // extra align

            // if current page has no avaiable space (super rare cases), get another page
            if (pageAvailableSpace < indexNodeLength)
            {
                // set this page as full before get next page
                this.UpdatePageMap(page->PageID, ExtendPageValue.Full);

                // call recursive to get another page
                return this.GetFreeIndexPage(colID, indexNodeLength);
            }

            return page;
        }
    }

    /// <summary>
    /// Update allocation page map according with header page type and used bytes but keeps a copy
    /// of original extend value (if need rollback)
    /// </summary>
    public void UpdatePageMap(uint pageID, ExtendPageValue value)
    {
        var allocationMapID = (int)(pageID / AM_PAGE_STEP);
        var extendIndex = (pageID - 1 - allocationMapID * AM_PAGE_STEP) / AM_EXTEND_SIZE;

        var extendLocation = new ExtendLocation(allocationMapID, (int)extendIndex);
        var extendID = extendLocation.ExtendID;

        if (!_initialExtendValues.ContainsKey(extendID))
        {
            var extendValue = _allocationMapService.GetExtendValue(extendLocation);

            _initialExtendValues.Add(extendID, extendValue);
        }

        _allocationMapService.UpdatePageMap(pageID, value);
    }
}