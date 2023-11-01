namespace LiteDB.Engine;

[AutoInterface(typeof(IDisposable))]
internal class SortService : ISortService
{
    private readonly IServicesFactory _factory;
    private readonly IStreamFactory _sortStreamFactory;

    private readonly ConcurrentQueue<int> _availableContainersID = new();
    private readonly ConcurrentQueue<Stream> _streamPool = new();
    private int _nextContainerID = -1;

    public SortService(
        IStreamFactory sortStreamFactory,
        IServicesFactory factory)
    {
        _sortStreamFactory = sortStreamFactory;
        _factory = factory;
    }

    public ISortOperation CreateSort(OrderBy orderBy)
    {
        var sorter = _factory.CreateSortOperation(orderBy);

        return sorter;
    }

    public int GetAvailableContainerID()
    {
        if (_availableContainersID.TryDequeue(out var containerID))
        {
            return containerID;
        }

        return Interlocked.Increment(ref _nextContainerID);
    }

    public Stream RentSortStream()
    {
        if (!_streamPool.TryDequeue(out var stream))
        {
            stream = _sortStreamFactory.GetStream(true, FileOptions.None); // parameters are not used for sort stream factory
        }

        return stream;
    }

    public void ReleaseSortStream(Stream stream)
    {
        _streamPool.Enqueue(stream);
    }

    public void Dispose()
    {
        foreach(var stream  in _streamPool)
        {
            stream.Dispose();
        }

        if (_streamPool.Count > 0)
        {
            _sortStreamFactory.Delete();
        }

        // clear service states
        _availableContainersID.Clear();
        _streamPool.Clear();
        _nextContainerID = -1;
    }
}
