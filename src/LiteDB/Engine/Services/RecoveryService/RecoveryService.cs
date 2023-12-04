namespace LiteDB.Engine;

[AutoInterface(typeof(IDisposable))]
internal class RecoveryService : IRecoveryService
{
    // dependency injections
    private readonly IMemoryFactory _memoryFactory;
    private readonly IDiskService _diskService;

    private readonly List<LogPageHeader> _logPages = new();
    private readonly List<LogPageHeader> _tempPages = new();
    private readonly HashSet<int> _confirmedTransactions = new();

    private uint _startTempPositionID;
    private uint _lastPageID;
    private uint _lastPositionID;

    public RecoveryService(
        IMemoryFactory memoryFactory, 
        IDiskService diskService)
    {
        _memoryFactory = memoryFactory;
        _diskService = diskService;
    }

    public void DoRecovery()
    {
        // read all data information on disk on a first-pass (log/temp pages/crc)
        this.ReadDatafile();

        // if there is log pages to copy to data
        if (_logPages.Count + _tempPages.Count > 0)
        {
            // do checkpoint operation direct on disk
            this.RecoveryCheckpoint();
        }

        // re-create all allocation map pages based on page info on a second-pass on datafile
        this.RebuildAllocationMap();

    }

    /// <summary>
    /// Read all pages from database to find log pages and temp pages
    /// </summary>
    private unsafe void ReadDatafile()
    {
        var page = _memoryFactory.AllocateNewPage();

        // get last position from disk
        _lastPositionID = _diskService.GetLastFilePositionID();

        // init temp file at end of file (will check if need be before)
        _startTempPositionID = _lastPositionID + 1;

        var positionID = 0u;

        while(positionID < _lastPositionID)
        {
            // allocation pages will be full rebuilded
            var isAllocationMap = (positionID % AM_MAP_PAGES_COUNT) == 0;

            if (isAllocationMap)
            {
                positionID++;
                continue;
            }

            var read = _diskService.ReadPage(page, positionID);

            // skip empty pages
            if (MarshalEx.IsFullZero(page))
            {
                positionID++;
                continue;                 //TODO: report
            }

            // calculate real crc32 over data from disk
            var crc32 = Crc32.ComputeChecksum(page);

            if (crc32 != page->Crc32)
            {
                // skip crc32 error pages
                positionID++;
                continue;                 //TODO: report
            }

            // check if this pages confirms a transaction (valid only for log/temp pages)
            if (page->IsConfirmed)
            {
                ENSURE(page->IsPageInLogFile || page->IsPageInTempFile);

                _confirmedTransactions.Add(page->TransactionID);
            }

            // read data page and check crc
            if (page->IsPageInDataFile)
            {
                // get last PageID (consider here only data pages - log pages will be consider later)
                _lastPageID = Math.Max(_lastPageID, page->PageID);
            }
            // read page from logfile
            else if (page->IsPageInLogFile)
            {
                var header = new LogPageHeader(page);

                _logPages.Add(header);
            }
            // read temp page
            else if (page->IsPageInTempFile)
            {
                // set first temp positionID
                if (positionID < _startTempPositionID)
                {
                    _startTempPositionID = page->PositionID;
                }

                var header = new LogPageHeader(page);

                _tempPages.Add(header);
            }
            else
            {
                //TODO: report
            }

            positionID++;
        }

        var maxTempPageID = _tempPages.Count > 0 ?
            _tempPages.Max(x => x.PositionID) : 0;

        var maxLogPageID = _logPages.Count > 0 ?
            _logPages.Max(x => x.PositionID) : 0;

        // update lastPageID for last page on data/log or temp page
        _lastPageID =
            Math.Max(Math.Max(_lastPageID, maxLogPageID), maxTempPageID);

        _memoryFactory.DeallocatePage(page);
    }

    /// <summary>
    /// Do a in-disk checkpoint, with no cache and single page allocation
    /// </summary>
    private unsafe void RecoveryCheckpoint()
    {
        // get all checkpoint actions based on log/temp pages
        var actions = new CheckpointActions()
            .GetActions(
                _logPages,
                _confirmedTransactions,
                _lastPageID,
                _startTempPositionID,
                _tempPages).ToArray();

        var page = _memoryFactory.AllocateNewPage();
        var counter = 0;

        foreach (var action in actions)
        {
            if (action.Action == CheckpointActionType.ClearPage)
            {
                // clear page position
                _diskService.WriteEmptyPage(action.PositionID);
                continue;
            }

            // get page from file position ID (log or data)
            _diskService.ReadPage(page, action.PositionID);

            if (action.Action == CheckpointActionType.CopyToDataFile)
            {
                // transform this page into a data file page
                page->PositionID = page->PageID = action.TargetPositionID;
                page->TransactionID = 0;
                page->IsConfirmed = false;
                page->IsDirty = true;

                _diskService.WritePage(page);

                // increment checkpoint counter page
                counter++;
            }
            else if (action.Action == CheckpointActionType.CopyToTempFile)
            {
                // transform this page into a log temp file (keeps Header.PositionID in original value)
                page->PositionID = action.TargetPositionID;
                page->IsConfirmed = true; // mark all pages to true in temp disk (to recovery)
                page->IsDirty = true;

                _diskService.WritePage(page);
            }

            // after copy page, checks if page need to be clean on disk
            if (action.MustClear)
            {
                _diskService.WriteEmptyPage(action.PositionID);
            }
        }

        _memoryFactory.DeallocatePage(page);

        // crop file after last pageID
        _diskService.SetLength(_lastPageID);
    }

    /// <summary>
    /// Create all new AllocationMap pages based on a all datafile pages read. Writes direct on disk
    /// </summary>
    private unsafe void RebuildAllocationMap()
    {
        var readPage = _memoryFactory.AllocateNewPage();
        var amPage = _memoryFactory.AllocateNewPage();

        var amPageID = AM_FIRST_PAGE_ID;
        var positionID = 0u;
        var eof = false;

        while (!eof)
        {
            // initialize page as AllocationMap page
            amPage->PageID = amPageID;
            amPage->PositionID = positionID;
            amPage->PageType = PageType.AllocationMap;

            // skip created allocation map page position
            positionID++;

            for(var extendIndex = 0; extendIndex < AM_EXTEND_COUNT && !eof; extendIndex++)
            {
                // each extend contains 8 pages for only 1 collection
                byte colID = 0;

                for(var pageIndex = 0; pageIndex < AM_EXTEND_SIZE && !eof; pageIndex++)
                {
                    var read = _diskService.ReadPage(readPage, positionID);

                    if (!read)
                    {
                        eof = true;
                        break;
                    }

                    // when read first page with colID > 0, update page buffer with colID value
                    if (colID == 0 && readPage->ColID > 0)
                    {
                        colID = readPage->ColID;

                        //** get position, on page, where this colID must be setted in current extend
                        //**var colIDLocation = PAGE_HEADER_SIZE +
                        //**    (extendIndex * AM_EXTEND_SIZE);
                        //**
                        //**readPage->ColID = colID;
                        //**readPage.AsSpan(colIDLocation, 1)[0] = colID;
                    }
                    else
                    {
                        ENSURE(readPage->ColID > 0, readPage->ColID == colID, "All pages in an extend must be from same collection", new { colID });
                    }

                    // get allocation value for each page
                    var value = PageMemory.GetExtendPageValue(readPage->PageType, readPage->FreeBytes);

                    // update page allocation free space
                    PageMemory.UpdateExtendPageValue(amPage, extendIndex, pageIndex, value);

                    // move no next position
                    positionID++;

                    if (positionID > _lastPositionID)
                    {
                        eof = true;
                        break;
                    }
                }
            }

            // write allocation map on disk
            _diskService.WritePage(amPage);

            // increment allocation pageID
            amPageID++;
        }

        _memoryFactory.DeallocatePage(readPage);
        _memoryFactory.DeallocatePage(amPage);
    }

    public void Dispose()
    {
        _logPages.Clear();
        _tempPages.Clear();
        _confirmedTransactions.Clear();

        _startTempPositionID = _lastPageID = _lastPositionID = 0;
    }
}
