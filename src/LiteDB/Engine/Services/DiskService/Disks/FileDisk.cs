namespace LiteDB.Engine;

/// <summary>
/// Thread Safe disk access using RandomAccess (a new class in .NET 6)
/// </summary>
internal class FileDisk : IDisk
{
    private readonly string _filename;
    private readonly string? _password;
    private readonly bool _isTemp;

    private FileStream? _stream;

    public FileDisk(string filename, string? password, bool isTemp)
    {
        _filename = filename;
        _password = password;
        _isTemp = isTemp;
    }

    public bool IsOpen => _stream is not null;

    public string Name => Path.GetFileName(_filename);

    /// <summary>
    /// Open datafile
    /// </summary>
    public void Open()
    {
        var options = FileOptions.RandomAccess;

        // create a new file handle
        _stream = new FileStream(
            path: _filename,
            mode: FileMode.Open,
            access: FileAccess.ReadWrite,
            share: FileShare.ReadWrite,
            bufferSize: PAGE_SIZE,
            options);
    }

    /// <summary>
    /// Create new data disk
    /// </summary>
    public void CreateNew()
    {
        var options = FileOptions.RandomAccess;

        // create a new file handle
        _stream = new FileStream(
            path: _filename,
            mode: FileMode.CreateNew,
            access: FileAccess.ReadWrite,
            share: FileShare.ReadWrite,
            bufferSize: PAGE_SIZE,
            options: _isTemp ? (FileOptions.DeleteOnClose | options) : options);

        //if (_password is not null)
        {
            // initialize aes
        }
    }

    public async ValueTask<bool> ReadBufferAsync(Memory<byte> buffer, long position)
    {
        var handle = _stream!.SafeFileHandle;

        var read = await RandomAccess.ReadAsync(handle, buffer, position);
        
        return read == buffer.Length;

        //RandomAccess.Read(handle, buffer.Span, position);
        //
        //return new ValueTask<bool>(true);
    }

    public ValueTask WriteBufferAsync(ReadOnlyMemory<byte> buffer, long position)
    {
        ENSURE(
            buffer.Length == PAGE_SIZE || // full page
            (buffer.Length == PRAGMA_SIZE && position == FILE_HEADER_SIZE) || // pragma update
            (buffer.Length == PAGE_OFFSET && position == 0)); // file init

        var handle = _stream!.SafeFileHandle;

        //RandomAccess.Write(handle, buffer.Span, position);
        
        //return ValueTask.CompletedTask;

        return RandomAccess.WriteAsync(handle, buffer, position);
    }

    public bool Exists()
    {
        return _stream is not null || File.Exists(_filename);
    }

    public long GetLength()
    {
        var fileLength = _stream is null ?
            new FileInfo(_filename).Length :
            RandomAccess.GetLength(_stream.SafeFileHandle);

        return fileLength;
    }

    public void SetLength(long position)
    {
        _stream!.SetLength(position);
    }

    /// <summary>
    /// Close stream (disconect from disk)
    /// </summary>
    public void Dispose()
    {
        _stream?.Dispose();
        _stream = null;
    }

    public override string ToString()
    {
        var len = this.Exists() ? this.GetLength() : 0;

        return Dump.Object(new { _filename, IsOpen, Length = len });
    }
}
