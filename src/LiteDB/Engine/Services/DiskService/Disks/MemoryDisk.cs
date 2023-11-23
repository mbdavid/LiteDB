namespace LiteDB.Engine;

/// <summary>
/// Implement a simplw wrapper over generic Stream to read as a IDisk
/// </summary>
unsafe internal class MemoryDisk : IDisk
{
    private readonly Stream _stream;
    private bool _disposed;

    public MemoryDisk()
    {
        _stream = new MemoryStream();
        _disposed = false;
    }

    public MemoryDisk(Stream stream)
    {
        _stream = stream;
        _disposed = true;
    }

    public bool IsOpen => true;

    public string Name => "stream";

    public void Open()
    {
    }

    public void CreateNew()
    {
    }

    public bool ReadBuffer(Span<byte> buffer, long position)
    {
        lock(_stream)
        {
            _stream.Position = position;

            var read = _stream.Read(buffer);

            return read == buffer.Length;
        }
    }

    public void WriteBuffer(Span<byte> buffer, long position)
    {
        ENSURE(
            buffer.Length == PAGE_SIZE || // full page
            (buffer.Length == PRAGMA_SIZE && position == FILE_HEADER_SIZE) || // pragma update
            (buffer.Length == PAGE_OFFSET && position == 0)); // file init

        lock (_stream)
        {
            _stream.Position = position;

            _stream.Write(buffer);
        }
    }

    public bool Exists() => _stream.Length == 0;

    public long GetLength() => _stream.Length;

    public void SetLength(long position) => _stream.SetLength(position);

    public void Dispose()
    {
        if (_disposed)
        {
            _stream.Dispose();
            _disposed = true;
        }
    }
}
