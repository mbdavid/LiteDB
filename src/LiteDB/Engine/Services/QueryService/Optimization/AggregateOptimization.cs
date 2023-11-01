//namespace LiteDB.Engine;

//internal class AggregateOptimization : QueryOptimization
//{
//    private OrderBy _aggregateOrderBy = OrderBy.Empty;

//    // Define exclusive lookups for aggregates
//    protected IDocumentLookup? _aggregateLookup;

//    public AggregateOptimization(
//        IServicesFactory factory,
//        CollectionDocument collection)
//        : base(factory, collection)
//    {
//    }

//    public override IPipeEnumerator ProcessQuery(Query query, BsonDocument queryParameters)
//    {
//        var aggregateQuery = (AggregateQuery)query;

//        // split where expressions into TERMs (splited by AND operator)
//        this.SplitWhereInTerms(aggregateQuery.Where);

//        // get lower cost index or pk index
//        this.DefineIndexAggregate(aggregateQuery.Key, aggregateQuery.OrderBy);

//        // define _orderBy field (or use index order)
//        // this.DefineOrderBy(aggregateQuery.OrderBy);

//        // define lookup for index/order by
//        this.DefineLookups(aggregateQuery);

//        // create pipe enumerator based on query optimization
//        return this.CreatePipeEnumerator(aggregateQuery, queryParameters);
//    }

//    private IPipeEnumerator CreatePipeEnumerator(AggregateQuery query, BsonDocument queryParameters)
//    {
//        var pipe = _factory.CreatePipelineBuilder(_collection.Name, queryParameters);

//        pipe.AddIndex(_indexExpression!, _indexOrder);

//        pipe.AddLookup(_documentLookup!);

//        if (_filter.IsEmpty == false)
//            pipe.AddFilter(_filter);

//        // if has not direct index to use in query, create an order by to get keys in order
//        if (_aggregateOrderBy.IsEmpty == false)
//            pipe.AddOrderBy(_aggregateOrderBy);

//        if (_aggregateLookup is not null)
//            pipe.AddLookup(_aggregateLookup);

//        // at this point, key are order to run aggregate function and returns a document with all results per "group"
//        pipe.AddAggregate(query.Key, query.Functions);

//        if (query.Having.IsEmpty == false)
//            pipe.AddFilter(query.Having);

//        //foreach (var include in _includesBefore)
//        //    pipe.AddInclude(include);

//        //if (_orderBy.IsEmpty == false)
//        //    pipe.AddOrderBy(_orderBy);

//        if (query.Offset > 0)
//            pipe.AddOffset(query.Offset);

//        if (query.Limit != int.MaxValue)
//            pipe.AddLimit(query.Limit);

//        if (_orderByLookup is not null)
//            pipe.AddLookup(_orderByLookup);

//        //foreach (var include in _includesAfter)
//        //    pipe.AddInclude(include);

//        if (query.Select.IsEmpty == false && query.Select.Type != BsonExpressionType.Root)
//            pipe.AddTransform(query.Select);

//        return pipe.GetPipeEnumerator();
//    }

//    private void DefineIndexAggregate(BsonExpression key, OrderBy orderBy)
//    {
//        // try find a index for this key
//        var indexByKey = _collection.Indexes.FirstOrDefault(x => x.Expression == key);

//        // if found, test if have any term using (define if will be full index scan or another index for this key)
//        if (indexByKey is not null)
//        {
//            var term =
//                _terms.FirstOrDefault(x => x.Left == key) ??
//                _terms.FirstOrDefault(x => x.Right == key);

//            if (term is null)
//            {
//                // full index scan
//                _indexExpression = indexByKey.Expression;
//            }
//            else
//            {
//                _indexExpression = term;

//                _terms.Remove(term);
//            }
//        }
//        else
//        {
//            // get PK for index full scan
//            _indexExpression = _collection.PK.Expression;

//            // and than order by according key
//            _aggregateOrderBy = new OrderBy(key, 1);
//        }

//        // after define index, create filter with terms
//        if (_terms.Count > 0)
//        {
//            _filter = BsonExpression.And(_terms);
//        }
//    }

//    #region Lookup

//    /// <summary>
//    /// Define both lookups, for index and order by pipe enumerator
//    /// </summary>
//    private void DefineLookups(AggregateQuery query)
//    {
//        // without OrderBy
//        if (_orderBy.IsEmpty)
//        {
//            // if this query requires no sort keys (will use existing index)
//            if (_aggregateOrderBy.IsEmpty)
//            {
//                // get all root fiels using in this query (empty means need load full document)
//                var fields = this.GetFields(query, where: true, select: true, aggregate: true);

//                // if contains a single field and are index expression
//                if (fields.Length == 1 && fields[0] == _indexExpression.ToString()![2..])
//                {
//                    // use index based document lookup
//                    _documentLookup = new IndexLookup(fields[0]);
//                }
//                else
//                {
//                    _documentLookup = new DataLookup(fields);
//                }
//            }
//            // in this case, we need run an orderBy before run aggregate
//            else
//            {
//                // get all root fiels using in this query (empty means need load full document)
//                var fields = this.GetFields(query, where: true, aggregate: true);

//                // if contains a single field and are index expression
//                if (fields.Length == 1 && fields[0] == _indexExpression.ToString()![2..])
//                {
//                    // use index based document lookup
//                    _documentLookup = new IndexLookup(fields[0]);
//                }
//                else
//                {
//                    _documentLookup = new DataLookup(fields);
//                }

//                // now defile aggregateLookup
//                var aggrFields = this.GetFields(query, select: true);

//                // if contains a single field and are index expression
//                if (aggrFields.Length == 1 && aggrFields[0] == _indexExpression.ToString()![2..])
//                {
//                    // use index based document lookup
//                    _aggregateLookup = new IndexLookup(fields[0]);
//                }
//                else
//                {
//                    _aggregateLookup = new DataLookup(fields);
//                }
//           }
//        }

//        // with OrderBy
//        else
//        {
//            throw new NotImplementedException();

//            // get all fields used before order by
//            var docFields = this.GetFields(query, where: true, orderBy: true);

//            // if contains a single field and are index expression
//            if (docFields.Length == 1 && docFields[0] == _indexExpression.ToString()![2..])
//            {
//                // use index based document lookup
//                _documentLookup = new IndexLookup(docFields[0]);
//            }
//            else
//            {
//                _documentLookup = new DataLookup(docFields);
//            }

//            // get all fields used after order by
//            var orderFields = this.GetFields(query, select: true);

//            // if contains a single field and are index expression
//            if (orderFields.Length == 1 && orderFields[0] == _indexExpression.ToString()![2..])
//            {
//                _orderByLookup = new IndexLookup(orderFields[0]);
//            }
//            else
//            {
//                _orderByLookup = new DataLookup(orderFields);
//            }
//        }

//    }

//    /// <summary>
//    /// Get all fields used in many expressions (used bool to avoid new array)
//    /// </summary>
//    private string[] GetFields(
//        AggregateQuery query,
//        bool aggregate = false,
//        bool where = false,
//        bool select = false,
//        bool orderBy = false
//        //bool before = false,
//        //bool after = false
//        )
//    {
//        var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

//        if (aggregate)
//        {
//            if (add(true, query.Key, fields)) return Array.Empty<string>();

//            foreach (var fun in query.Functions)
//            {
//                if (add(true, fun.Expression, fields)) return Array.Empty<string>();
//            }
//        }

//        if (add(where, query.Where, fields)) return Array.Empty<string>();
//        if (add(select, query.Select, fields)) return Array.Empty<string>();
//        if (add(orderBy, query.OrderBy.Expression, fields)) return Array.Empty<string>();

//        //if (before)
//        //{
//        //    foreach (var expr in _includesBefore)
//        //    {
//        //        if (add(true, expr, fields)) return Array.Empty<string>();
//        //    }
//        //}

//        //if (after)
//        //{
//        //    foreach (var expr in _includesAfter)
//        //    {
//        //        if (add(true, expr, fields)) return Array.Empty<string>();
//        //    }
//        //}

//        return fields.ToArray();

//        static bool add(bool conditional, BsonExpression expr, HashSet<string> fields)
//        {
//            if (!conditional) return false;

//            var info = expr.GetInfo();

//            if (info.FullRoot) return true;

//            fields.AddRange(info.RootFields);

//            return false;
//        }
//    }

//    #endregion

//}