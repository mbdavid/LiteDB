namespace LiteDB.Engine;

/// <summary>
/// * Singleton (thread safe)
/// </summary>
[AutoInterface(typeof(IDisposable))]
internal class AllocationMapService : IAllocationMapService
{
    private readonly IDiskService _diskService;
    private readonly IMemoryFactory _memoryFactory;

    /// <summary>
    /// List of all allocation map pages, in pageID order
    /// </summary>
    private readonly List<PageMemoryResult> _pages = new();

    public AllocationMapService(
        IDiskService diskService, 
        IMemoryFactory memoryFactory)
    {
        _diskService = diskService;
        _memoryFactory = memoryFactory;
    }

    /// <summary>
    /// Initialize allocation map service loading all AM pages into memory and getting
    /// </summary>
    public async ValueTask InitializeAsync()
    {
        // read all allocation maps pages on disk
        var positionID = AM_FIRST_PAGE_ID;

        var lastPositionID = _diskService.GetLastFilePositionID();

        while (positionID <= lastPositionID)
        {
            var page = _memoryFactory.AllocateNewPage();

            await _diskService.ReadPageAsync(page, positionID);

            _pages.Add(page);

            positionID += AM_PAGE_STEP;
        }
    }

    /// <summary>
    /// Get a free PageID based on colID/type. Create extend or new am page if needed. Return isNew if page are empty (must be initialized)
    /// </summary>
    public (uint pageID, bool isNew, ExtendLocation next) GetFreeExtend(ExtendLocation current, byte colID, PageType type)
    {
        var page = _pages[current.AllocationMapID];

        var (extendIndex, pageIndex, isNew) = PageMemory.GetFreeExtend(page.Ptr, current.ExtendIndex, colID, type);

        if (extendIndex >= 0)
        {
            var extend = new ExtendLocation(current.AllocationMapID, extendIndex);

            var pageID = (uint)(current.AllocationMapID * AM_PAGE_STEP + extendIndex * AM_EXTEND_SIZE + 1 + pageIndex);

            return (pageID, isNew, extend);
        }
        else if (extendIndex == -1 && current.AllocationMapID < _pages.Count - 1)
        {
            var next = new ExtendLocation(current.AllocationMapID + 1, 0);

            return this.GetFreeExtend(next, colID, type);
        }
        else
        {
            // create new extend map page
            var extend = new ExtendLocation(current.AllocationMapID + 1, 0);

            // if there is no more free extend in any AM page, let's create a new allocation map page
            var newPage = _memoryFactory.AllocateNewPage();

            ENSURE(_pages.Count > 0);

            var lastPage = _pages.Last();

            // get a new PageID based on last AM page
            var nextPageID = lastPage.PageID + AM_PAGE_STEP;

            // get allocation map position
            newPage.PositionID = newPage.RecoveryPositionID = nextPageID;
            newPage.PageID = nextPageID;
            newPage.PageType = PageType.AllocationMap;
            newPage.IsDirty = true;

            _pages.Add(newPage);

            // call again this method with this new page
            return this.GetFreeExtend(extend, colID, type);
        }
    }

    /// <summary>
    /// Get an extend value from a extendID (global). This extendID should be already exists
    /// </summary>
    public unsafe uint GetExtendValue(ExtendLocation extend)
    {
        var page = _pages[extend.AllocationMapID].Page;

        return page->Extends[extend.ExtendIndex];
    }

    /// <summary>
    /// Get PageMemory instance for a specific allocationMapID
    /// </summary>
    public PageMemoryResult GetPageMemory(int allocationMapID)
    {
        return _pages[allocationMapID];
    }

    /// <summary>
    /// Get all used pages from a single collection
    /// </summary>
    public IReadOnlyList<uint> GetAllPages(byte colID)
    {
        var result = new List<uint>();

        for(var i = 0; i < _pages.Count; i++)
        {
            PageMemory.LoadPageIDFromExtends(_pages[i].Ptr, i, colID, result);
        }

        return result;
    }

    /// <summary>
    /// Clear all extends used in this collection
    /// </summary>
    public unsafe void ClearExtends(byte colID)
    {
        // loop over all allocation map pages
        for (var i = 0; i < _pages.Count; i++)
        {
            var page = _pages[i].Page;

            // and loop over all extend value inside a allocation map page
            for (var j = 0; j < AM_EXTEND_COUNT; j++)
            {
                var extendValue = page->Extends[j];

                // checks if first byte are for this colID
                if (extendValue >> 24 == colID)
                {
                    page->Extends[j] = 0;
                }
            }
        }
    }

    /// <summary>
    /// Update allocation page map according with header page type and used bytes
    /// </summary>
    public void UpdatePageMap(uint pageID, ExtendPageValue pageValue)
    {
        var allocationMapID = (int)(pageID / AM_PAGE_STEP);
        var extendIndex = (int)((pageID - 1 - allocationMapID * AM_PAGE_STEP) / AM_EXTEND_SIZE);
        var pageIndex = (int)pageID - 1 - allocationMapID * AM_PAGE_STEP - extendIndex * AM_EXTEND_SIZE;

        var page = _pages[allocationMapID];

        PageMemory.UpdateExtendPageValue(page.Ptr, extendIndex, pageIndex, pageValue);
    }

    /// <summary>
    /// In a rollback error, should return all initial values to used extends
    /// </summary>
    public unsafe void RestoreExtendValues(IDictionary<int, uint> extendValues)
    {
        foreach(var extendValue in extendValues)
        {
            var extendLocation = new ExtendLocation(extendValue.Key);

            var page = _pages[extendLocation.AllocationMapID].Page;

            page->Extends[extendLocation.ExtendIndex] = extendValue.Value;
        }
    }

    /// <summary>
    /// Write all dirty pages direct into disk (there is no log file to amp)
    /// </summary>
    public async ValueTask WriteAllChangesAsync()
    {
        foreach(var page in _pages)
        {
            if (page.IsDirty)
            {
                await _diskService.WritePageAsync(page);
            }
        }
    }

    public override string ToString()
    {
        return Dump.Object(new { _pages = _pages.Count });
    }

    public unsafe void Dispose()
    {
        // let's deallocate all amp
        foreach (var ptr in _pages)
        {
            _memoryFactory.DeallocatePage(ptr);
        }

        // clear list to be ready to use
        _pages.Clear();
    }
}