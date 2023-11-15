namespace LiteDB.Engine;

/// <summary>
/// Thread Safe disk access using RandomAccess (a new class in .NET 6)
/// </summary>
internal class FileDisk : IDisk
{
    private readonly string _filename;
    private readonly string? _password;
    private readonly bool _isTemp;

    private SafeFileHandle? _handle;

    public FileDisk(string filename, string? password, bool isTemp)
    {
        _filename = filename;
        _password = password;
        _isTemp = isTemp;
    }

    public bool IsOpen => _handle is not null;

    public string Name => Path.GetFileName(_filename);

    /// <summary>
    /// Open datafile
    /// </summary>
    public void Open()
    {
        var options = FileOptions.RandomAccess;

        // create a new file handle
        _handle = File.OpenHandle(
            path: _filename,
            mode: FileMode.Open,
            access: FileAccess.ReadWrite,
            share: FileShare.ReadWrite,
            options);
    }

    /// <summary>
    /// Create new data disk
    /// </summary>
    public void CreateNew()
    {
        var options = FileOptions.RandomAccess;

        // create a new file handle
        _handle = File.OpenHandle(
            path: _filename,
            mode: FileMode.CreateNew,
            access: FileAccess.ReadWrite,
            share: FileShare.ReadWrite,
            options: _isTemp ? (FileOptions.DeleteOnClose | options) : options);

        //if (_password is not null)
        {
            // initialize aes
        }
    }

    public async ValueTask<bool> ReadBufferAsync(Memory<byte> buffer, long position)
    {
        var read = await RandomAccess.ReadAsync(_handle!, buffer, position);

        return read == buffer.Length;
    }

    public ValueTask WriteBufferAsync(ReadOnlyMemory<byte> buffer, long position)
    {
        ENSURE(
            buffer.Length == PAGE_SIZE || // full page
            (buffer.Length == PRAGMA_SIZE && position == FILE_HEADER_SIZE) || // pragma update
            (buffer.Length == PAGE_OFFSET && position == 0)); // file init

        //RandomAccess.Write(_handle!, buffer, position);

        return RandomAccess.WriteAsync(_handle!, buffer, position);
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

    /// <summary>
    /// Close stream (disconect from disk)
    /// </summary>
    public void Dispose()
    {
        _handle?.Dispose();
        _handle = null;
    }
}
