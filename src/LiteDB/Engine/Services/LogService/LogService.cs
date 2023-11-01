namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class LogService : ILogService
{
    // dependency injection
    private readonly IDiskService _diskService;
    private readonly IMemoryCache _memoryCache;
    private readonly IMemoryFactory _memoryFactory;
    private readonly IWalIndexService _walIndexService;
    private readonly IServicesFactory _factory;

    private uint _lastPageID;
    private uint _logPositionID;

    private readonly List<LogPageHeader> _logPages = new();
    private readonly HashSet<int> _confirmedTransactions = new();

    public LogService(
        IDiskService diskService,
        IMemoryCache memoryCache,
        IMemoryFactory memoryFactory,
        IWalIndexService walIndexService,
        IServicesFactory factory)
    {
        _diskService = diskService;
        _memoryCache = memoryCache;
        _memoryFactory = memoryFactory;
        _walIndexService = walIndexService;
        _factory = factory;
    }

    public void Initialize()
    {
        var writer = _diskService.GetDiskWriter();

        _lastPageID = writer.GetLastFilePositionID();

        _logPositionID = this.CalcInitLogPositionID(_lastPageID);
    }

    /// <summary>
    /// Get initial file log position based on next extent
    /// </summary>
    private uint CalcInitLogPositionID(uint lastPageID)
    {
        // add 2 extend space between lastPageID and new logPositionID
        var allocationMapID = (int)(lastPageID / AM_PAGE_STEP);
        var extendIndex = (uint)((lastPageID - 1 - allocationMapID * AM_PAGE_STEP) / AM_EXTEND_SIZE);

        var nextExtendIndex = (extendIndex + 2) % AM_EXTEND_COUNT;
        var nextAllocationMapID = allocationMapID + (nextExtendIndex < extendIndex ? 1 : 0);
        var nextPositionID = (uint)(nextAllocationMapID * AM_PAGE_STEP + nextExtendIndex * AM_EXTEND_SIZE + 1);

        return nextPositionID - 1; // first run get next()
    }

    /// <summary>
    /// </summary>
    public unsafe void WriteLogPages(nint[] pages)
    {
        var writer = _diskService.GetDiskWriter();

        // set IsDirty flag in header file to true at first use
        if (_factory.FileHeader.IsDirty == false)
        {
            _factory.FileHeader.IsDirty = true;

            writer.WriteFlag(FileHeader.P_IS_DIRTY, 1);
        }

        var lastPositionID = 0u;

        // set all lastPosition before write on disk
        for (var i = 0; i < pages.Length; i++)
        {
            var page = (PageMemory*)pages[i];

            // get next page position on log (update header PositionID too)
            lastPositionID = page->PositionID = page->RecoveryPositionID = this.GetNextLogPositionID();
        }

        // pre-allocate file
        writer.SetSize(lastPositionID);

        for (var i = 0; i < pages.Length; i++)
        {
            var page = (PageMemory*)pages[i];

            // write page to writer stream
            writer.WritePage(page);

            // create a log header structure with needed information about this page on checkpoint
            var header = new LogPageHeader { PositionID = page->PositionID, PageID = page->PageID, TransactionID = page->TransactionID, IsConfirmed = page->IsConfirmed };

            // add page header only into log memory list
            this.AddLogPage(header);
        }
    }

    /// <summary>
    /// Get next positionID in log
    /// </summary>
    private uint GetNextLogPositionID()
    {
        var next = Interlocked.Increment(ref _logPositionID);

        // test if next log position is not an AMP
        if (next % AM_PAGE_STEP == 0) next = Interlocked.Increment(ref _logPositionID);

        return next;
    }

    /// <summary>
    /// Add a page header in log list, to be used in checkpoint operation.
    /// This page should be added here after write on disk
    /// </summary>
    private void AddLogPage(LogPageHeader header)
    {
        // if page is confirmed, set transaction as confirmed and ok to override on data file
        if (header.IsConfirmed)
        {
            _confirmedTransactions.Add(header.TransactionID);
        }

        // update _lastPageID
        if (header.PageID > _lastPageID)
        {
            _lastPageID = header.PageID;
        }

        _logPages.Add(header);
    }

    public ValueTask<int> CheckpointAsync(bool crop, bool addToCache)
    {
        var logLength = _logPages.Count;

        if (logLength == 0 && !crop) return new ValueTask<int>(0);

        ENSURE(logLength > 0, _logPositionID == _logPages.LastOrDefault().PositionID, $"Last log page must be {_logPositionID}", new { logLength, _logPositionID });

        // temp file start after lastPageID or last log used page
        var startTempPositionID = Math.Max(_lastPageID, _logPositionID) + 1;
        var tempPages = Array.Empty<LogPageHeader>();

        var result = this.CheckpointInternal(startTempPositionID, tempPages, crop, addToCache);

        return new ValueTask<int>(result);
    }

    private unsafe int CheckpointInternal(uint startTempPositionID, IList<LogPageHeader> tempPages, bool crop, bool addToCache)
    {
        // get all actions that checkpoint must do with all pages
        var actions = new CheckpointActions().GetActions(
            _logPages, 
            _confirmedTransactions,
            _lastPageID,
            startTempPositionID, 
            tempPages).ToArray();

        // get writer stream from disk service
        var writer = _diskService.GetDiskWriter();
        var counter = 0;

        foreach (var action in actions)
        {
            if (action.Action == CheckpointActionType.ClearPage)
            {
                // if this page are in cache, remove and deallocate
                if (_memoryCache.TryRemove(action.PositionID, out var pagePtr))
                {
                    _memoryFactory.DeallocatePage(pagePtr);
                }

                // write an empty page at position
                writer.WriteEmptyPage(action.PositionID);
            }
            else
            {
                // get page from file position ID (log or data)
                var page = this.GetLogPage(writer, action.PositionID);

                if (action.Action == CheckpointActionType.CopyToDataFile)
                {
                    // transform this page into a data file page
                    page->PositionID = page->RecoveryPositionID = page->PageID = action.TargetPositionID;
                    page->TransactionID = 0;
                    page->IsConfirmed = false;
                    page->IsDirty = true;

                    writer.WritePage(page);

                    // increment checkpoint counter page
                    counter++;
                }
                else if (action.Action == CheckpointActionType.CopyToTempFile)
                {
                    // transform this page into a log temp file (keeps Header.PositionID in original value)
                    page->PositionID = action.TargetPositionID;
                    page->IsConfirmed = true; // mark all pages to true in temp disk (to recovery)
                    page->IsDirty = true;

                    writer.WritePage(page);
                }

                // after copy page, checks if page need to be clean on disk
                if (action.MustClear)
                {
                    writer.WriteEmptyPage(action.PositionID);
                }

                // if cache contains this position (old data version) must be removed from cache and deallocate
                if (_memoryCache.TryRemove(page->PositionID, out var removedPage))
                {
                    ENSURE(false); // quando para aqui?
                    _memoryFactory.DeallocatePage(removedPage);
                }

                // add this page to cache (or try it)
                var added = false;

                if (addToCache)
                {
                    added = _memoryCache.AddPageInCache(page);
                }

                // if cache is full, deallocate page
                if (!added)
                {
                    _memoryFactory.DeallocatePage(page);
                }
            }
        }

        // crop file or fill with \0 after _lastPageID
        if (crop)
        {
            // crop file after _lastPageID
            writer.SetSize(_lastPageID);
        }
        else
        {
            // get last page (from log or from temp file)
            var lastFilePositionID = tempPages.Count > 0 ?
                startTempPositionID * tempPages.Count :
                _logPositionID;

            writer.WriteEmptyPages(_lastPageID + 1, (uint)lastFilePositionID);
        }

        // reset initial log position
        _logPositionID = this.CalcInitLogPositionID(_lastPageID);

        // empty all wal index pointer (there is no wal index after checkpoint)
        _walIndexService.Clear();

        // clear all log pages/confirm transactions
        _logPages.Clear();
        _confirmedTransactions.Clear();

        // clear all logfile pages (keeps in cache only non-changed datafile pages)
        _memoryCache.ClearLogPages();

        // ao terminar o checkpoint, nenhuma pagina na cache deve ser de log
        return counter;
    }

    /// <summary>
    /// Get page from cache (remove if found) or create a new from page factory
    /// </summary>
    private unsafe PageMemory* GetLogPage(IDiskStream stream, uint positionID)
    {
        // try get page from cache
        if (_memoryCache.TryRemove(positionID, out var page))
        {
            return page;
        }

        // otherwise, allocate new buffer page and read from disk
        page = _memoryFactory.AllocateNewPage();

        stream.ReadPage(page, positionID);

        return page;
    }

    public override string ToString()
    {
        return Dump.Object(new { _lastPageID, _logPositionID, _confirmedTransactions, _logPages });
    }

    public void Dispose()
    {
        _logPages.Clear();
        _confirmedTransactions.Clear();
    }
}
