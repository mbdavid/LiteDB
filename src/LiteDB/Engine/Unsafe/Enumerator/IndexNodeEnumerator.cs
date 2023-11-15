namespace LiteDB.Engine;

internal class IndexNodeEnumerator : IAsyncEnumerator<IndexNodeResult>
{
    private readonly IIndexService _indexService;
    private readonly IndexDocument _indexDocument;

    private IndexNodeResult _current;
    private bool _init = false;
    private bool _eof = false;
    private RowID _nextID = RowID.Empty;

    public IndexNodeEnumerator(IIndexService indexService, IndexDocument indexDocument)
    {
        _indexService = indexService;
        _indexDocument = indexDocument;

        _current = IndexNodeResult.Empty;
    }

    public IndexNodeResult Current => _current;

    public async ValueTask<bool> MoveNextAsync()
    {
        if (_eof) return false;

        if (_init == false)
        {
            _init = true;

            var head = await _indexService.GetNodeAsync(_indexDocument.HeadIndexNodeID);

            var nextID = head.GetNextID(0);

            if (nextID == _indexDocument.TailIndexNodeID) return false;

            _current = await _indexService.GetNodeAsync(nextID);

            // buffer next in level 0
            unsafe
            {
                _nextID = _current[0]->NextID;
            }

            return true;
        }

        if (_nextID == _indexDocument.TailIndexNodeID)
        {
            _eof = true;
            return false;
        }

        _current = await _indexService.GetNodeAsync(_nextID);

        // buffer next in level 0
        _nextID = _current.GetNextID(0);

        return true;
    }

    public void Reset()
    {
        _current = IndexNodeResult.Empty;
        _init = false;
        _eof = false;
    }

    public ValueTask DisposeAsync()
    {
        this.Reset();

        return new ValueTask();
    }
}
