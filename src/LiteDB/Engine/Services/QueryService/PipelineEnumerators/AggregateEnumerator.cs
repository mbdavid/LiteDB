namespace LiteDB.Engine;

internal class AggregateEnumerator : IPipeEnumerator
{
    // depenrency injection
    private readonly Collation _collation;
    private readonly BsonExpression _keyExpr;

    // fields
    private readonly IPipeEnumerator _enumerator;
    private readonly List<(string key, IAggregateFunc func)> _fields = new();

    private BsonValue _currentKey = BsonValue.MinValue;

    private bool _init = false;
    private bool _eof = false;

    /// <summary>
    /// Aggregate values according aggregate functions (reduce functions). Require "ProvideDocument" from IPipeEnumerator
    /// </summary>
    public AggregateEnumerator(BsonExpression keyExpr, SelectFields fields, IPipeEnumerator enumerator, Collation collation)
    {
        _keyExpr = keyExpr;
        _enumerator = enumerator;
        _collation = collation;

        // aggregate requires key/value fields to compute (does not support single root)
        if (fields.IsSingleExpression) throw new ArgumentException($"AggregateEnumerator has no support for single expression");

        // getting aggregate expressions call
        foreach (var field in fields.Fields)
        {
            if (!field.IsAggregate) continue;

            var func = field.CreateAggregateFunc();

            _fields.Add((field.Name, func));
        }

        if (_enumerator.Emit.Value == false) throw ERR($"Aggregate pipe enumerator requires document from last pipe");
    }

    public PipeEmit Emit => new(indexNodeID: false, dataBlockID: false, value: true);

    public PipeValue MoveNext(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        while (!_eof)
        {
            var item = _enumerator.MoveNext(context);

            if (item.IsEmpty)
            {
                _init = _eof = true;
            }
            else
            {
                var key = _keyExpr.IsEmpty ? BsonValue.Null :
                    _keyExpr.Execute(item.Value, context.QueryParameters, _collation);

                // initialize current key with first key
                if (_init == false)
                {
                    _init = true;
                    _currentKey = key;
                }

                // keep running with same value
                if (_currentKey == key)
                {
                    foreach (var field in _fields)
                    {
                        item.Value.AsDocument[GROUP_BY_KEY_NAME] = key;

                        field.func.Iterate(_currentKey, item.Value.AsDocument, _collation);
                    }
                }
                // if key changes, return results in a new document
                else
                {
                    var results = this.GetResults();

                    _currentKey = key;

                    foreach (var field in _fields)
                    {
                        item.Value.AsDocument[GROUP_BY_KEY_NAME] = key;

                        field.func.Iterate(_currentKey, item.Value.AsDocument, _collation);
                    }

                    return results;
                }
            }
        }

        return this.GetResults();
    }

    /// <summary>
    /// Get all results from all aggregate functions and transform into a document
    /// </summary>
    private PipeValue GetResults()
    {
        var keyName = _keyExpr is PathBsonExpression path ?
            path.Field : GROUP_BY_KEY_NAME;

        var doc = new BsonDocument
        {
            [keyName] = _currentKey,
        };

        foreach (var field in _fields)
        {
            doc[field.key] = field.func.GetResult();
            field.func.Reset();
        }

        return new PipeValue(doc);
    }

    public void GetPlan(ExplainPlainBuilder builder, int deep)
    {
        builder.Add($"AGGREGATE {_keyExpr}", deep);

        foreach (var field in _fields)
        {
            builder.Add($"{field.key} = {field.func}", deep - 1);
        }

        _enumerator.GetPlan(builder, ++deep);
    }

    public void Dispose()
    {
    }
}
