namespace LiteDB.Engine;

[AutoInterface]
internal class NewDatafile : INewDatafile
{
    private readonly IMemoryFactory _memoryFactory;
    private readonly IMasterMapper _masterMapper;
    private readonly IBsonWriter _bsonWriter;
    private readonly IEngineSettings _settings;

    public NewDatafile(
        IMemoryFactory memoryFactory,
        IMasterMapper masterMapper,
        IBsonWriter bsonWriter,
        IEngineSettings settings)
    {
        _memoryFactory = memoryFactory;
        _masterMapper = masterMapper;
        _bsonWriter = bsonWriter;
        _settings = settings;
    }

    /// <summary>
    /// Create a empty database using user-settings as default values
    /// Create FileHeader, first AllocationMap page and first $master data page
    /// </summary>
    public async ValueTask<FileHeader> CreateNewAsync(IDiskStream writer)
    {
        // initialize FileHeader with user settings
        var fileHeader = new FileHeader(_settings);

        // create new file and write header
        writer.CreateNewFile(fileHeader);

        unsafe
        {
            // create map page
            var mapPage = _memoryFactory.AllocateNewPage();

            mapPage->PageID = AM_FIRST_PAGE_ID;
            mapPage->PageType = PageType.AllocationMap;

            // mark first extend to $master and first page as data
            mapPage->Buffer[0] = MASTER_COL_ID;
            mapPage->Buffer[1] = 0b0010_0000; // set first 3 bits as "001" - data page

            mapPage->IsDirty = true;

            // create $master page buffer
            var masterPage = _memoryFactory.AllocateNewPage();

            // initialize page buffer as data page
            PageMemory.InitializeAsDataPage(masterPage, MASTER_PAGE_ID, MASTER_COL_ID);

            // create new/empty $master document
            var master = new MasterDocument();
            var masterDoc = _masterMapper.MapToDocument(master);
            using var masterBuffer = SharedArray<byte>.Rent(masterDoc.GetBytesCount());

            // serialize $master document 
            _bsonWriter.WriteDocument(masterBuffer.AsSpan(), masterDoc, out _);

            // insert $master document into master page
            PageMemory.InsertDataBlock(masterPage, masterBuffer.AsSpan(), false, out _, out _);

            // initialize fixed position id 
            mapPage->PositionID = 0;
            masterPage->PositionID = 1;

            // write both pages in disk and flush to OS
            writer.WritePage(mapPage);
            writer.WritePage(masterPage);

            // deallocate buffers
            _memoryFactory.DeallocatePage(mapPage);
            _memoryFactory.DeallocatePage(masterPage);
        }

        await writer.FlushAsync();

        return fileHeader;
    }
}
