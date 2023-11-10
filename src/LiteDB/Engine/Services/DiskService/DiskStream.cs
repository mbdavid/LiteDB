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
    public (FileHeader, Pragmas) OpenFile(bool canWrite)
    {
        // get a new FileStream connected to file
        _stream = _streamFactory.GetStream(canWrite,
            FileOptions.RandomAccess);

        // reading file header
        using var buffer = SharedArray<byte>.Rent(PAGE_OFFSET);

        _stream.Position = 0;

        var read = _stream.Read(buffer.AsSpan());

        ENSURE(read == PAGE_OFFSET, new { read });

        var header = new FileHeader(buffer.AsSpan(FILE_HEADER_SIZE));
        var pragmas = new Pragmas(buffer.AsSpan(FILE_HEADER_SIZE, PRAGMA_SIZE));

        // for content stream, use AesStream (for encrypted file) or same _stream
        _contentStream = header.Encrypted ?
            new AesStream(_stream, _settings.Password ?? "", header.EncryptionSalt) :
            _stream;

        return (header, pragmas);
    }

    /// <summary>
    /// Initialize disk creating a new datafile and writing file header
    /// </summary>
    public void CreateNewFile(FileHeader fileHeader, Pragmas pragmas)
    {
        using var buffer = SharedArray<byte>.Rent(PAGE_OFFSET);

        // create new data file
        _stream = _streamFactory.GetStream(true, FileOptions.SequentialScan);

        // writing file header
        _stream.Position = 0;

        // update buffer with header/pragmas values 
        fileHeader.Write(buffer.AsSpan(FILE_HEADER_SIZE));
        pragmas.Write(buffer.AsSpan(FILE_HEADER_SIZE, PRAGMA_SIZE));

        // write on disk
        _stream.Write(buffer.AsSpan());

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
        if (fileLength <= PAGE_OFFSET) throw ERR($"Invalid datafile. Data file is too small (length = {fileLength}).");

        var content = fileLength - PAGE_OFFSET;
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
        _contentStream!.Position = PAGE_OFFSET + (positionID * PAGE_SIZE);

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
        _contentStream!.Position = PAGE_OFFSET + (page->PositionID * PAGE_SIZE);

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
        _contentStream!.Position = PAGE_OFFSET + (positionID * PAGE_SIZE);

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
            _contentStream!.Position = PAGE_OFFSET + (i * PAGE_SIZE);

            _contentStream.Write(PAGE_EMPTY);
        }
    }

    /// <summary>
    /// Set new file length using lastPageID as end of file.
    /// 0 = 8k, 1 = 16k, ...
    /// </summary>
    public void SetSize(uint lastPageID)
    {
        var fileLength = PAGE_OFFSET +
            ((lastPageID + 1) * PAGE_SIZE);

        _stream!.SetLength(fileLength);
    }

    /// <summary>
    /// Write pragma values into disk stream
    /// </summary>
    public void WritePragmas(Pragmas pragmas)
    {
        _stream!.Position = PAGE_OFFSET; // just after file header

        using var buffer = SharedArray<byte>.Rent(PRAGMA_SIZE);

        pragmas.Write(buffer.AsSpan());

        _stream.Write(buffer.AsSpan());

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
