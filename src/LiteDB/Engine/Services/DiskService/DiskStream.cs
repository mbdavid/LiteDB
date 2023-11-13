namespace LiteDB.Engine;

/// <summary>
/// Thread Safe disk access
/// </summary>
[AutoInterface(typeof(IDisposable))]
unsafe internal class DiskStream : IDiskStream
{
    private readonly string _filename;
    private readonly string? _password;

    private SafeFileHandle? _handle;

    public DiskStream(string filename, string? password)
    {
        _filename = filename;
        _password = password;
    }

    public string Name => Path.GetFileName(_filename);

    /// <summary>
    /// Open datafile and read header/pragma content
    /// </summary>
    public void OpenFile(Span<byte> buffer)
    {
        // create a new file handle
        _handle = File.OpenHandle(
            path: _filename,
            mode: FileMode.Open,
            access: FileAccess.ReadWrite,
            share: FileShare.ReadWrite);

        var read = RandomAccess.Read(_handle, buffer, 0);

        ENSURE(read == PAGE_OFFSET, new { read });
    }

    /// <summary>
    /// Initialize disk creating a new datafile and writing file header
    /// </summary>
    public void CreateNewFile(Span<byte> buffer)
    {
        ENSURE(buffer.Length == PAGE_OFFSET);

        // create a new file handle
        _handle = File.OpenHandle(
            path: _filename,
            mode: FileMode.CreateNew,
            access: FileAccess.ReadWrite,
            share: FileShare.ReadWrite);

        // write on disk
        RandomAccess.Write(_handle, buffer, 0);

        //if (fileHeader.IsEncrypted)
        {
            // initialize aes
        }
    }

    public bool ReadBuffer(Span<byte> buffer, long position)
    {
        var read = RandomAccess.Read(_handle!, buffer, position);

        return read == PAGE_SIZE;
    }

    public void WriteBuffer(Span<byte> buffer, long position)
    {
        ENSURE(buffer.Length == PAGE_SIZE || (buffer.Length == PRAGMA_SIZE && position == FILE_HEADER_SIZE));

        RandomAccess.Write(_handle!, buffer, position);
    }

    public bool Exists()
    {
        return _handle is not null || File.Exists(_filename);
    }

    public long GetLength()
    {
        var fileLength = _handle is null ?
            new FileInfo(_filename).Length :
            RandomAccess.GetLength(_handle);

        return fileLength;
    }

    public void SetLength(long position)
    {
        //_stream!.SetLength(fileLength);
    }

    public void Delete()
    {
        ENSURE(_handle is null);

        //TODO: implementar testes
        File.Delete(_filename);
    }

    /// <summary>
    /// Close stream (disconect from disk)
    /// </summary>
    public void Dispose()
    {
        _handle?.Dispose();
        _handle = null;
    }
}
