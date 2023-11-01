namespace LiteDB;

public class Program
{
    public static void Main(string[] args)
    {

    }
}



[AutoInterface(typeof(IDisposable))]
public unsafe class DiskService : IDiskService
{
    public DiskService()
    {
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public Stream RendStreamReader()
    {
        throw new NotImplementedException();
    }

    public void ReturnReader(Stream stream)
    {
    }
}

[AutoInterface]
public class StreamPool : IStreamPool
{
    public StreamPool(int limit)
    {
    }

    public Stream RendStreamReader()
    {
        return new MemoryStream();
    }

    public void ReturnReader(Stream stream)
    {
    }

}