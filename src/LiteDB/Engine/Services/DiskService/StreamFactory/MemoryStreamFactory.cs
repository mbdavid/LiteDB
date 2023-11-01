namespace LiteDB.Engine;

internal class MemoryStreamFactory : IStreamFactory
{
    private readonly Stream _stream;

    public string Name => ":memory:";

    public MemoryStreamFactory(Stream stream)
    {
        _stream = stream;
    }

    public void Delete()
    {
    }

    public bool Exists()
    {
        return true;
    }

    public long GetLength()
    {
        return _stream.Length;
    }

    public Stream GetStream(bool canWrite, FileOptions options)
    {
        return _stream;
    }

    public bool DisposeOnClose => true;
}
