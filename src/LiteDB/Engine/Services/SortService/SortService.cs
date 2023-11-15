namespace LiteDB.Engine;

[AutoInterface]
internal class SortService : ISortService
{
    private readonly IServicesFactory _factory;

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
        // clear service states
        _availableContainersID.Clear();
        _nextContainerID = -1;
    }
}
