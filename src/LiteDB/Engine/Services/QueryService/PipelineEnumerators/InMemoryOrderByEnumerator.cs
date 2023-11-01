namespace LiteDB.Engine;

internal class InMemoryOrderByEnumerator : IPipeEnumerator
{
    // dependency injections
    private readonly Collation _collation;

    private readonly OrderBy _orderBy;
    private readonly IPipeEnumerator _enumerator;

    private Queue<SortItemDocument>? _sortedItems;

    public InMemoryOrderByEnumerator(
        OrderBy orderBy,
        IPipeEnumerator enumerator,
        Collation collation)
    {
        _orderBy = orderBy;
        _enumerator = enumerator;
        _collation = collation;

        if (_enumerator.Emit.Value == false) throw ERR($"InMemoryOrderBy pipe enumerator requires document from last pipe");
    }

    public PipeEmit Emit => new(indexNodeID: false, dataBlockID: true, value: true);

    public PipeValue MoveNext(PipeContext context)
    {
        if (_sortedItems is null)
        {
            var list = new List<SortItemDocument>();

            while (true)
            {
                var item = _enumerator.MoveNext(context);

                if (item.IsEmpty) break;

                // get sort key 
                var key = _orderBy.Expression.Execute(item.Value, context.QueryParameters, _collation);

                //list.Add(new (item.DataBlockID, key, item.Document!));
                throw new NotImplementedException();
            }

            // sort list in a new enumerable
            var query = _orderBy.Order == Query.Ascending ?
                list.OrderBy(x => x.Key, _collation) : list.OrderByDescending(x => x.Key, _collation);

            _sortedItems = new Queue<SortItemDocument>(query);
        }

        if (_sortedItems.Count > 0)
        {
            var item = _sortedItems.Dequeue();

            return new PipeValue(RowID.Empty, item.DataBlockID, item.Document);
        }

        return PipeValue.Empty;
    }

    public void GetPlan(ExplainPlainBuilder builder, int deep)
    {
        builder.Add($"IN-MEMORY ORDER BY {_orderBy}", deep);

        _enumerator.GetPlan(builder, ++deep);
    }

    public void Dispose()
    {
    }
}
