namespace LiteDB.Engine;

/// <summary>
/// Singleton (thread safe)
/// </summary>
[AutoInterface]
internal class DiskService : IDiskService
{
    // dependency injection
    private readonly IEngineSettings _settings;
    private readonly IMasterMapper _masterMapper;
    private readonly IMemoryFactory _memoryFactory;
    private readonly IDisk _disk;

    public DiskService(
        IEngineSettings settings,
        IMasterMapper masterMapper,
        IMemoryFactory memoryFactory,
        IDisk disk)
    {
        _settings = settings;
        _masterMapper = masterMapper;
        _memoryFactory = memoryFactory;
        _disk = disk;
    }

    /// <summary>
    /// Open (or create) datafile.
    /// </summary>
    public async ValueTask<(FileHeader, Pragmas)> InitializeAsync()
    {
        // if file not exists, create empty database
        if (_disk.Exists() == false)
        {
            return await this.CreateNewDatabaseAsync();
        }
        else
        {
            using var buffer = SharedArray<byte>.Rent(PAGE_OFFSET);

            // open and read header content
            _disk.Open();

            await _disk.ReadBufferAsync(buffer.AsMemory(), 0);

            var header = new FileHeader(buffer.AsSpan(FILE_HEADER_SIZE));
            var pragmas = new Pragmas(buffer.AsSpan(FILE_HEADER_SIZE, PRAGMA_SIZE));

            return (header, pragmas);
        }
    }

    /// <summary>
    /// Create a empty database using user-settings as default values
    /// Create FileHeader, first AllocationMap page and first $master data page
    /// </summary>
    private async ValueTask<(FileHeader, Pragmas)> CreateNewDatabaseAsync()
    {
        using var buffer = SharedArray<byte>.Rent(PAGE_OFFSET);

        // initialize FileHeader with user settings
        var fileHeader = new FileHeader(_settings);
        var pragmas = new Pragmas();
        var bsonWriter = new BsonWriter();

        // update buffer with header/pragmas values 
        fileHeader.Write(buffer.AsSpan(0, FILE_HEADER_SIZE));
        pragmas.Write(buffer.AsSpan(FILE_HEADER_SIZE, PRAGMA_SIZE));

        // write on disk initial header/pragmas
        _disk.CreateNew();

        await _disk.WriteBufferAsync(buffer.AsMemory(), 0);

        // keep map and master page pointer
        var mapPagePtr = _memoryFactory.AllocateNewPage();
        var masterPagePtr = _memoryFactory.AllocateNewPage();

        unsafe
        {
            // create map page
            var mapPage = (PageMemory*)_memoryFactory.AllocateNewPage();

            mapPage->PageID = AM_FIRST_PAGE_ID;
            mapPage->PageType = PageType.AllocationMap;

            // mark first extend to $master and first page as data
            mapPage->Buffer[0] = MASTER_COL_ID;
            mapPage->Buffer[1] = 0b0010_0000; // set first 3 bits as "001" - data page

            mapPage->IsDirty = true;

            // initialize page buffer as data page
            PageMemory.InitializeAsDataPage(masterPagePtr, MASTER_PAGE_ID, MASTER_COL_ID);

            // create new/empty $master document
            var master = new MasterDocument();
            var masterDoc = _masterMapper.MapToDocument(master);
            using var masterBuffer = SharedArray<byte>.Rent(masterDoc.GetBytesCount());

            // serialize $master document 
            bsonWriter.WriteDocument(masterBuffer.AsSpan(), masterDoc, out _);

            // insert $master document into master page
            PageMemory.InsertDataBlock(masterPagePtr, masterBuffer.AsSpan(), false, out _, out _);

            // initialize fixed position id 
            mapPage->PositionID = 0;
            ((PageMemory*)masterPagePtr)->PositionID = 1;
        }

        await this.WritePageAsync(mapPagePtr);
        await this.WritePageAsync(masterPagePtr);

        // deallocate buffers
        _memoryFactory.DeallocatePage(mapPagePtr);
        _memoryFactory.DeallocatePage(masterPagePtr);

        return (fileHeader, pragmas);
    }

    /// <summary>
    /// Calculate, using disk file length, last PositionID. Should considering FILE_HEADER_SIZE and celling pages.
    /// </summary>
    public uint GetLastFilePositionID()
    {
        var fileLength = _disk.GetLength();

        // fileLength must be, at least, FILE_HEADER
        if (fileLength <= PAGE_OFFSET) throw ERR($"Invalid datafile. Data file is too small (length = {fileLength}).");

        var content = fileLength - PAGE_OFFSET;
        var celling = content % PAGE_SIZE > 0 ? 1 : 0;
        var result = content / PAGE_SIZE;

        // if last page was not completed written, add missing bytes to complete

        return (uint)(result + celling - 1);
    }

    /// <summary>
    /// Set file length according with last pageID
    /// </summary>
    public void SetLength(uint lastPageID)
    {
        var fileLength = PAGE_OFFSET +
            ((lastPageID + 1) * PAGE_SIZE);

        _disk.SetLength(fileLength);
    }

    /// <summary>
    /// Read single page from disk using disk position. Load header instance too. This position has FILE_HEADER_SIZE offset
    /// </summary>
    public async ValueTask<bool> ReadPageAsync(nint ptr, uint positionID)
    {
        using var _pc = PERF_COUNTER(40, nameof(ReadPageAsync), nameof(DiskService));

        ENSURE(positionID != uint.MaxValue, "PositionID should not be empty");

        // get real position on stream
        var position = PAGE_OFFSET + (positionID * PAGE_SIZE);

        int uniqueID;

        unsafe
        {
            var page = (PageMemory*)ptr;

            // read uniqueID to restore after read from disk
            uniqueID = page->UniqueID;
        }

        // get page array from memory factory
        var buffer = _memoryFactory.GetPageArray(ptr);

        // read from disk
        var read = await _disk.ReadBufferAsync(buffer, position);

        unsafe
        {
            var page = (PageMemory*)ptr;

            ENSURE(page->UniqueID == 0);

            // update init value on page (memory)
            page->UniqueID = uniqueID;
            page->ShareCounter = NO_CACHE;
            page->IsDirty = false;
        }

        return read;
    }

    public async ValueTask WritePageAsync(nint ptr)
    {
        using var _pc = PERF_COUNTER(50, nameof(WritePageAsync), nameof(DiskService));

        long position;
        byte[] buffer;
        int uniqueID;

        unsafe
        {
            var page = (PageMemory*)ptr;

            ENSURE(page->IsDirty);
            ENSURE(page->ShareCounter == NO_CACHE);
            ENSURE(page->PositionID != int.MaxValue);

            // update crc32 page
            page->Crc32 = 0; // pagePtr->ComputeCrc8();

            // cache and clear before write on disk
            uniqueID = page->UniqueID;
            page->UniqueID = 0;

            // get real position on stream
            position = PAGE_OFFSET + (page->PositionID * PAGE_SIZE);

            buffer = _memoryFactory.GetPageArray(ptr);
        }

        await _disk.WriteBufferAsync(buffer, position);

        unsafe
        {
            var page = (PageMemory*)ptr;

            // clear isDirty flag before write on disk
            page->UniqueID = uniqueID;
            page->ShareCounter = NO_CACHE;
            page->IsDirty = false;
        }
    }

    /// <summary>
    /// Write an empty (full \0) PAGE_SIZE using positionID
    /// </summary>
    public ValueTask WriteEmptyPageAsync(uint positionID)
    {
        // get real position on stream
        var position = PAGE_OFFSET + (positionID * PAGE_SIZE);

        return _disk.WriteBufferAsync(PAGE_EMPTY, position);
    }

    /// <summary>
    /// Write an empty (full \0) PAGE_SIZE using from/to (inclusive)
    /// </summary>
    public async ValueTask WriteEmptyPagesAsync(uint fromPositionID, uint toPositionID, CancellationToken token = default)
    {
        for (var i = fromPositionID; i <= toPositionID && token.IsCancellationRequested; i++)
        {
            // set real position on stream
            var position = PAGE_OFFSET + (i * PAGE_SIZE);

            await _disk.WriteBufferAsync(PAGE_EMPTY, position);
        }
    }

    /// <summary>
    /// Write pragma values into disk stream
    /// </summary>
    public ValueTask WritePragmasAsync(Pragmas pragmas)
    {
        using var buffer = SharedArray<byte>.Rent(PRAGMA_SIZE);

        pragmas.Write(buffer.AsSpan());

        var position = FILE_HEADER_SIZE; // just after file header

        return _disk.WriteBufferAsync(buffer.AsMemory(), position);
    }

    public override string ToString()
    {
        return Dump.Object(new { disk = _disk.ToString() });
    }
}
