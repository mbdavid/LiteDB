namespace LiteDB.Engine;

internal class TransformEnumerator : IPipeEnumerator
{
    private readonly SelectFields _fields;
    private readonly Collation _collation;
    private readonly IPipeEnumerator _enumerator;

    private bool _eof = false;

    public TransformEnumerator(SelectFields fields, Collation collation, IPipeEnumerator enumerator)
    {
        _fields = fields;
        _enumerator = enumerator;
        _collation = collation;

        if (_enumerator.Emit.Value == false) throw ERR($"Transform pipe enumerator requires document from last pipe");
    }

    public PipeEmit Emit => new(indexNodeID: _enumerator.Emit.IndexNodeID, dataBlockID: _enumerator.Emit.DataBlockID, value: true);

    public PipeValue MoveNext(PipeContext context)
    {
        if (_eof) return PipeValue.Empty;

        var item = _enumerator.MoveNext(context);

        if (item.IsEmpty)
        {
            _eof = true;
            return PipeValue.Empty;
        }

        //TODO: otimizar essa criação de um novo documento, pois pode chegar o item.Document já pronto
        // ou seja, pode ser que não seja necessario fazer nada aqui

        if (_fields.IsSingleExpression)
        {
            var value = _fields.SingleExpression.Execute(item.Value, context.QueryParameters, _collation);

            return new PipeValue(item.IndexNodeID, item.DataBlockID, value.AsDocument);
        }
        else
        {
            var output = new BsonDocument();
            var hidden = new List<string>();

            foreach (var field in _fields.Fields)
            {
                var root = item.Value.AsDocument;

                // add previous values in expression root
                foreach (var key in output.Keys)
                {
                    root[key] = output[key];
                }

                var value = field.IsAggregate ? 
                    item.Value[field.Name] :
                    field.Expression.Execute(root, context.QueryParameters, _collation);

                // and add to final document
                output.Add(field.Name, value);

                if (field.Hidden) hidden.Add(field.Name);
            }

            // remove hidden fields from output
            foreach(var key in hidden)
            {
                output.Remove(key);
            }

            return new PipeValue(item.IndexNodeID, item.DataBlockID, output);
        }
    }

    public void GetPlan(ExplainPlainBuilder builder, int deep)
    {
        builder.Add($"TRANSFORM {_fields}", deep);

        _enumerator.GetPlan(builder, ++deep);
    }

    public void Dispose()
    {
    }
}
