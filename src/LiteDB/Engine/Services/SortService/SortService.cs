namespace LiteDB.Engine;

[AutoInterface(typeof(IDisposable))]
internal class SortService : ISortService
{
    private readonly IServicesFactory _factory;

    private IDiskStream? _stream = null;
    private readonly ConcurrentQueue<int> _availableContainersID = new();
    private int _nextContainerID = -1;

    public SortService(
        IServicesFactory factory)
    {
        _factory = factory;
    }

    public ISortOperation CreateSort(OrderBy orderBy)
    {
        var sorter = _factory.CreateSortOperation(orderBy);

        return sorter;
    }

    public IDiskStream GetSortStream()
    {
        return _stream ?? _factory.CreateSortDiskStream();
    }

    public int GetAvailableContainerID()
    {
        if (_availableContainersID.TryDequeue(out var containerID))
        {
            return containerID;
        }

        return Interlocked.Increment(ref _nextContainerID);
    }

    public void Dispose()
    {
        _stream?.Dispose();
        _stream?.Delete();

        // clear service states
        _availableContainersID.Clear();
        _nextContainerID = -1;
    }
}
