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

    private FileHeader? _fileHeader;
    private Pragmas? _pragmas;

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

            // keep local instances
            _fileHeader = new FileHeader(buffer.AsSpan(FILE_HEADER_SIZE));
            _pragmas = new Pragmas(buffer.AsSpan(FILE_HEADER_SIZE, PRAGMA_SIZE));

            return (_fileHeader, _pragmas);
        }
    }

    /// <summary>
    /// Create a empty database using user-settings as default values
    /// Create FileHeader, first AllocationMap page and first $master data page
    /// </summary>
    private async ValueTask<(FileHeader, Pragmas)> CreateNewDatabaseAsync()
    {
        using var buffer = SharedArray<byte>.Rent(PAGE_OFFSET);

        // initialize FileHeader/Pragmas with user settings (keep in instance)
        _fileHeader = new FileHeader(_settings);
        _pragmas = new Pragmas();

        var bsonWriter = new BsonWriter();

        // update buffer with header/pragmas values 
        _fileHeader.Write(buffer.AsSpan(0, FILE_HEADER_SIZE));
        _pragmas.Write(buffer.AsSpan(FILE_HEADER_SIZE, PRAGMA_SIZE));

        // write on disk initial header/pragmas
        _disk.CreateNew();

        await _disk.WriteBufferAsync(buffer.AsMemory(), 0);

        // get 2 new empty pages in memory
        var mapPage = _memoryFactory.AllocateNewPage();
        var masterPage = _memoryFactory.AllocateNewPage();

        // initialize map/master pages
        mapPage.PageID = mapPage.PositionID = AM_FIRST_PAGE_ID;
        masterPage.PositionID = masterPage.PositionID = MASTER_PAGE_ID;
        mapPage.IsDirty = masterPage.IsDirty = true;

        // initialize first allocation map
        mapPage.PageType = PageType.AllocationMap;

        unsafe
        {
            // mark first extend to $master and first page as data
            mapPage.Page->Buffer[0] = MASTER_COL_ID;
            mapPage.Page->Buffer[1] = 0b0010_0000; // set first 3 bits as "001" - data page
        }

        // initialize page buffer as data page
        PageMemory.InitializeAsDataPage(masterPage.Ptr, MASTER_PAGE_ID, MASTER_COL_ID);

        // create new/empty $master document
        var master = new MasterDocument();
        var masterDoc = _masterMapper.MapToDocument(master);
        using var masterBuffer = SharedArray<byte>.Rent(masterDoc.GetBytesCount());

        // serialize $master document 
        bsonWriter.WriteDocument(masterBuffer.AsSpan(), masterDoc, out _);

        // insert $master document into master page
        PageMemory.InsertDataBlock(masterPage, masterBuffer.AsSpan(), false, out _, out _);

        await this.WritePageAsync(mapPage);
        await this.WritePageAsync(masterPage);

        // deallocate buffers
        _memoryFactory.DeallocatePage(mapPage);
        _memoryFactory.DeallocatePage(masterPage);

        return (_fileHeader, _pragmas);
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
    /// Read single page from disk using disk position. Load header instance too.
    /// </summary>
    public async ValueTask<bool> ReadPageAsync(PageMemoryResult page, uint positionID)
    {
        using var _pc = PERF_COUNTER(60, nameof(ReadPageAsync), nameof(DiskService));

        ENSURE(positionID != uint.MaxValue, "PositionID should not be empty");

        // get real position on stream
        var position = PAGE_OFFSET + (positionID * PAGE_SIZE);

        // read from disk
        var read = await _disk.ReadBufferAsync(page.Buffer, position);

        page.ShareCounter = NO_CACHE;
        page.IsDirty = false;

        return read;
    }

    public async ValueTask WritePageAsync(PageMemoryResult page)
    {
        using var _pc = PERF_COUNTER(70, nameof(WritePageAsync), nameof(DiskService));

        ENSURE(page.IsDirty);
        ENSURE(page.ShareCounter == NO_CACHE);
        ENSURE(page.PositionID != int.MaxValue);

        // test pragma IsDirty
        if (_pragmas!.IsDirty == false)
        {
            _pragmas.IsDirty = true;

            await this.WritePragmasAsync(_pragmas);
        }

        // update crc32 page
        // page->Crc32 = 0; // pagePtr->ComputeCrc8();

        // get real position on stream
        var position = PAGE_OFFSET + (page.PositionID * PAGE_SIZE);

        await _disk.WriteBufferAsync(page.Buffer, position);

        page.ShareCounter = NO_CACHE;
        page.IsDirty = false;
    }

    /// <summary>
    /// Write an empty (full \0) PAGE_SIZE using positionID
    /// </summary>
    public async ValueTask WriteEmptyPageAsync(uint positionID)
    {
        // test pragma IsDirty
        if (_pragmas!.IsDirty == false)
        {
            _pragmas.IsDirty = true;

            await this.WritePragmasAsync(_pragmas);
        }

        // get real position on stream
        var position = PAGE_OFFSET + (positionID * PAGE_SIZE);

        await _disk.WriteBufferAsync(PAGE_EMPTY, position);
    }

    /// <summary>
    /// Write an empty (full \0) PAGE_SIZE using from/to (inclusive)
    /// </summary>
    public async ValueTask WriteEmptyPagesAsync(uint fromPositionID, uint toPositionID, CancellationToken token = default)
    {
        for (var i = fromPositionID; i <= toPositionID && token.IsCancellationRequested; i++)
        {
            await this.WriteEmptyPageAsync(i);
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
