namespace LiteDB.Engine;

[AutoInterface(typeof(IDisposable))]
unsafe internal class DiskStream : IDiskStream
{
    private readonly IEngineSettings _settings;
    private readonly IStreamFactory _streamFactory;

    private Stream? _stream;
    private Stream? _contentStream;

    public string Name => Path.GetFileName(_settings.Filename);

    public DiskStream(
        IEngineSettings settings, 
        IStreamFactory streamFactory)
    {
        _settings = settings;
        _streamFactory = streamFactory;
    }

    /// <summary>
    /// Initialize disk opening already exist datafile and return file header structure.
    /// Can open file as read or write
    /// </summary>
    public FileHeader OpenFile(bool canWrite)
    {
        // get a new FileStream connected to file
        _stream = _streamFactory.GetStream(canWrite,
            FileOptions.RandomAccess);

        // reading file header
        using var buffer = SharedArray<byte>.Rent(FILE_HEADER_SIZE);

        _stream.Position = 0;

        var read = _stream.Read(buffer.AsSpan());

        ENSURE(read != PAGE_HEADER_SIZE, new { read });

        var header = new FileHeader(buffer.AsSpan());

        // for content stream, use AesStream (for encrypted file) or same _stream
        _contentStream = header.Encrypted ?
            new AesStream(_stream, _settings.Password ?? "", header.EncryptionSalt) :
            _stream;

        return header;
    }

    /// <summary>
    /// Open stream with no FileHeader read (need FileHeader instance)
    /// </summary>
    public void OpenFile(FileHeader header)
    {
        // get a new FileStream connected to file
        _stream = _streamFactory.GetStream(false, FileOptions.RandomAccess);

        // for content stream, use AesStream (for encrypted file) or same _stream
        _contentStream = header.Encrypted ?
            new AesStream(_stream, _settings.Password ?? "", header.EncryptionSalt) :
            _stream;
    }

    /// <summary>
    /// Initialize disk creating a new datafile and writing file header
    /// </summary>
    public void CreateNewFile(FileHeader fileHeader)
    {
        // create new data file
        _stream = _streamFactory.GetStream(true, FileOptions.SequentialScan);

        // writing file header
        _stream.Position = 0;

        _stream.Write(fileHeader.ToArray());

        // for content stream, use AesStream (for encrypted file) or same _stream
        _contentStream = fileHeader.Encrypted ?
            new AesStream(_stream, _settings.Password ?? "", fileHeader.EncryptionSalt) :
            _stream;
    }

    public Task FlushAsync()
    {
        return _contentStream?.FlushAsync() ?? Task.CompletedTask;
    }

    /// <summary>
    /// Calculate, using disk file length, last PositionID. Should considering FILE_HEADER_SIZE and celling pages.
    /// </summary>
    public uint GetLastFilePositionID()
    {
        var fileLength = _streamFactory.GetLength();

        // fileLength must be, at least, FILE_HEADER
        if (fileLength <= FILE_HEADER_SIZE) throw ERR($"Invalid datafile. Data file is too small (length = {fileLength}).");

        var content = fileLength - FILE_HEADER_SIZE;
        var celling = content % PAGE_SIZE > 0 ? 1 : 0;
        var result = content / PAGE_SIZE;

        // if last page was not completed written, add missing bytes to complete

        return (uint)(result + celling - 1);
    }

    /// <summary>
    /// Read single page from disk using disk position. Load header instance too. This position has FILE_HEADER_SIZE offset
    /// </summary>
    public bool ReadPage(PageMemory* page, uint positionID)
    {
        using var _pc = PERF_COUNTER(40, nameof(ReadPage), nameof(DiskStream));

        ENSURE(positionID != uint.MaxValue, "PositionID should not be empty");

        // set real position on stream
        _contentStream!.Position = FILE_HEADER_SIZE + (positionID * PAGE_SIZE);

        var span = new Span<byte>(page, PAGE_SIZE);

        // read uniqueID to restore after read from disk
        var uniqueID = page->UniqueID;

        var read = _contentStream.Read(span);

        ENSURE(page->UniqueID == 0);

        // update init value on page (memory)
        page->UniqueID = uniqueID;
        page->ShareCounter = NO_CACHE;
        page->IsDirty = false;

        ENSURE(page->PositionID == positionID);

        return read == PAGE_SIZE;
    }

    public void WritePage(PageMemory* page)
    {
        using var _pc = PERF_COUNTER(50, nameof(WritePage), nameof(DiskStream));

        ENSURE(page->IsDirty);
        ENSURE(page->ShareCounter == NO_CACHE);
        ENSURE(page->PositionID != int.MaxValue);

        // update crc32 page
        page->Crc32 = 0; // pagePtr->ComputeCrc8();

        // cache and clear before write on disk
        var uniqueID = page->UniqueID;
        page->UniqueID = 0; 

        // set real position on stream
        _contentStream!.Position = FILE_HEADER_SIZE + (page->PositionID * PAGE_SIZE);

        var span = new Span<byte>(page, PAGE_SIZE);

        _contentStream.Write(span);

        // clear isDirty flag before write on disk
        page->UniqueID = uniqueID;
        page->ShareCounter = NO_CACHE;
        page->IsDirty = false;

    }

    /// <summary>
    /// Write an empty (full \0) PAGE_SIZE using positionID
    /// </summary>
    public void WriteEmptyPage(uint positionID)
    {
        // set real position on stream
        _contentStream!.Position = FILE_HEADER_SIZE + (positionID * PAGE_SIZE);

        _contentStream.Write(PAGE_EMPTY);
    }

    /// <summary>
    /// Write an empty (full \0) PAGE_SIZE using from/to (inclusive)
    /// </summary>
    public void WriteEmptyPages(uint fromPositionID, uint toPositionID, CancellationToken token = default)
    {
        for (var i = fromPositionID; i <= toPositionID && token.IsCancellationRequested; i++)
        {
            // set real position on stream
            _contentStream!.Position = FILE_HEADER_SIZE + (i * PAGE_SIZE);

            _contentStream.Write(PAGE_EMPTY);
        }
    }

    /// <summary>
    /// Set new file length using lastPageID as end of file.
    /// 0 = 8k, 1 = 16k, ...
    /// </summary>
    public void SetSize(uint lastPageID)
    {
        var fileLength = FILE_HEADER_SIZE +
            ((lastPageID + 1) * PAGE_SIZE);

        _stream!.SetLength(fileLength);
    }

    /// <summary>
    /// Write a specific byte in datafile with a flag/byte value - used to restore. Use sync write
    /// </summary>
    public void WriteFlag(int headerPosition, byte flag)
    {
        _stream!.Position = FileHeader.P_IS_DIRTY;
        _stream.WriteByte(flag);

        _stream.Flush();
    }

    /// <summary>
    /// Close stream (disconect from disk)
    /// </summary>
    public void Dispose()
    {
        _stream?.Dispose();
        _contentStream?.Dispose();
    }
}
