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

    private int _startTempPositionID;
    private int _lastPageID;
    private int _lastPositionID;

    public RecoveryService(
        IMemoryFactory memoryFactory, 
        IDiskService diskService)
    {
        _memoryFactory = memoryFactory;
        _diskService = diskService;
    }

    public async ValueTask DoRecoveryAsync()
    {
        //// read all data information on disk on a first-pass (log/temp pages/crc)
        //await this.ReadDatafileAsync();

        //// if there is log pages to copy to data
        //if (_logPages.Count + _tempPages.Count > 0)
        //{
        //    // do checkpoint operation direct on disk
        //    await this.CheckpointAsync();
        //}

        //// re-create all allocation map pages based on page info on a second-pass on datafile
        //await this.RebuildAllocationMap();

    }

    ///// <summary>
    ///// Read all pages from database to find log pages and temp pages
    ///// </summary>
    //private async ValueTask ReadDatafileAsync()
    //{
    //    var page = _bufferFactory.AllocateNewPage();
    //    var writer = _diskService.GetDiskWriter();

    //    // get last position from disk
    //    _lastPositionID = writer.GetLastFilePositionID();

    //    // init temp file at end of file (will check if need be before)
    //    _startTempPositionID = _lastPositionID + 1;

    //    var positionID = 0;

    //    while(positionID < _lastPositionID)
    //    {
    //        var read = await writer.ReadPageAsync(positionID, page);

    //        // skip empty pages
    //        if (page.IsHeaderEmpty())
    //        {
    //            positionID++;
    //            continue;
    //        }

    //        // calculate real crc8 over data from disk
    //        var crc = page.ComputeCrc8();

    //        // check if this pages confirms a transaction (valid only for log/temp pages)
    //        if (page.Header.IsConfirmed)
    //        {
    //            _confirmedTransactions.Add(page.Header.TransactionID);
    //        }

    //        // read data page and check crc
    //        if (page.IsDataFile)
    //        {
    //            // get last PageID (consider here only data pages - log pages will be consider later)
    //            _lastPageID = Math.Max(_lastPageID, page.Header.PageID);
    //        }
    //        // read page from logfile
    //        else if (page.IsLogFile)
    //        {
    //            _logPages.Add(page.Header);
    //        }
    //        // read temp page
    //        else if (page.IsTempFile)
    //        {
    //            // set first temp positionID
    //            if (positionID < _startTempPositionID)
    //            {
    //                _startTempPositionID = page.PositionID;
    //            }

    //            _tempPages.Add(page.Header);
    //        }
    //        else
    //        {
    //            throw new NotSupportedException();
    //        }

    //        positionID++;
    //    }

    //    // update lastPageID for last page on data/log or temp page
    //    _lastPageID = Math.Max(
    //        Math.Max(_lastPageID, _logPages.Max(x => x.PositionID)),
    //        _tempPages.Max(x => x.PositionID));

    //    _bufferFactory.DeallocatePage(page);
    //}

    ///// <summary>
    ///// Do a in-disk checkpoint, with no cache and single page allocation
    ///// </summary>
    //private async ValueTask CheckpointAsync()
    //{
    //    // get all checkpoint actions based on log/temp pages
    //    var actions = new __CheckpointActions()
    //        .GetActions(
    //            _logPages,
    //            _confirmedTransactions,
    //            _lastPageID,
    //            _startTempPositionID,
    //            _tempPages).ToArray();

    //    var page = _bufferFactory.AllocateNewPage();
    //    var writer = _diskService.GetDiskWriter();
    //    var counter = 0;

    //    foreach (var action in actions)
    //    {
    //        if (action.Action == CheckpointActionType.ClearPage)
    //        {
    //            // clear page position
    //            await writer.WriteEmptyAsync(action.PositionID);
    //            continue;
    //        }

    //        // get page from file position ID (log or data)
    //        await writer.ReadPageAsync(action.PositionID, page);

    //        if (action.Action == CheckpointActionType.CopyToDataFile)
    //        {
    //            // transform this page into a data file page
    //            page.PositionID = page.Header.PositionID = page.Header.PageID = action.TargetPositionID;
    //            page.Header.TransactionID = 0;
    //            page.Header.IsConfirmed = false;
    //            page.IsDirty = true;

    //            await writer.WritePageAsync(page);

    //            // increment checkpoint counter page
    //            counter++;
    //        }
    //        else if (action.Action == CheckpointActionType.CopyToTempFile)
    //        {
    //            // transform this page into a log temp file (keeps Header.PositionID in original value)
    //            page.PositionID = action.TargetPositionID;
    //            page.Header.IsConfirmed = true; // mark all pages to true in temp disk (to recovery)
    //            page.IsDirty = true;

    //            await writer.WritePageAsync(page);
    //        }

    //        // after copy page, checks if page need to be clean on disk
    //        if (action.MustClear)
    //        {
    //            await writer.WriteEmptyAsync(action.PositionID);
    //        }
    //    }

    //    _bufferFactory.DeallocatePage(page);

    //    // crop file after last pageID
    //    writer.SetSize(_lastPageID);
    //}

    ///// <summary>
    ///// Create all new AllocationMap pages based on a all datafile pages read. Writes direct on disk
    ///// </summary>
    //private async ValueTask RebuildAllocationMap()
    //{
    //    var readPage = _bufferFactory.AllocateNewPage();
    //    var amPage = _bufferFactory.AllocateNewPage();

    //    var amPageID = __AM_FIRST_PAGE_ID;
    //    var positionID = 0;
    //    var eof = false;

    //    var stream = _diskService.GetDiskWriter();

    //    while (!eof)
    //    {
    //        var pageMap = new __AllocationMapPage(amPageID, amPage);

    //        // update buffer with current positionID
    //        amPage.PositionID = positionID;

    //        // skip created allocation map page position
    //        positionID++;

    //        for(var extendIndex = 0; extendIndex < AM_EXTEND_COUNT && !eof; extendIndex++)
    //        {
    //            // each extend contains 8 pages for only 1 collection
    //            byte colID = 0;

    //            for(var pageIndex = 0; pageIndex < AM_EXTEND_SIZE && !eof; pageIndex++)
    //            {
    //                var read = await stream.ReadPageAsync(positionID, readPage);

    //                if (!read)
    //                {
    //                    eof = true;
    //                    break;
    //                }

    //                // when read first page with colID > 0, update page buffer with colID value
    //                if (colID == 0 && readPage.Header.ColID > 0)
    //                {
    //                    colID = readPage.Header.ColID;

    //                    // get position, on page, where this colID must be setted in current extend
    //                    var colIDLocation = PAGE_HEADER_SIZE +
    //                        (extendIndex * AM_EXTEND_SIZE);

    //                    readPage.AsSpan(colIDLocation, 1)[0] = colID;
    //                }
    //                else
    //                {
    //                    ENSURE(readPage.Header.ColID > 0, readPage.Header.ColID == colID, "All pages in an extend must be from same collection", new { readPage, colID });
    //                }

    //                // get allocation value for each page
    //                var value = __AllocationMapPage.GetExtendPageValue(readPage.Header.PageType, readPage.Header.FreeBytes);

    //                // update page allocation free space
    //                pageMap.UpdateExtendPageValue(extendIndex, pageIndex, value);

    //                // move no next position
    //                positionID++;
    //            }
    //        }

    //        // write allocation map on disk
    //        await stream.WritePageAsync(amPage);

    //        // increment allocation pageID
    //        amPageID++;
    //    }

    //    _bufferFactory.DeallocatePage(readPage);
    //    _bufferFactory.DeallocatePage(amPage);
    //}

    public void Dispose()
    {
        _logPages.Clear();
        _tempPages.Clear();
        _confirmedTransactions.Clear();

        _startTempPositionID = _lastPageID = _lastPositionID = 0;
    }
}
