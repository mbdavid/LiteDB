using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LiteDB
{
    /// <summary>
    /// QueryLinq implement a query resolver to Linq expressions. If it's possible convert to Engine Query object (using index or full scan) will
    /// use internal _query object. Otherwise, convert BsonDocument into class T do works with final class
    /// </summary>
    internal class QueryLinq<T> : Query
    {
        private Expression _expr;
        private BsonMapper _mapper;
        private Func<T, bool> _where = null;

        public QueryLinq(Expression expr, ParameterExpression p, BsonMapper mapper)
            : base(null)
        {
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(expr, p);

            _where = lambda.Compile();

            _mapper = mapper;
            _expr = expr;
        }

        internal override IEnumerable<IndexNode> Run(CollectionPage col, IndexService indexer)
        {
            this.UseIndex = false;
            this.UseFilter = true;

            return Query.All().Run(col, indexer);
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            throw new NotSupportedException();
        }

        internal override bool FilterDocument(BsonDocument doc)
        {
            // must deserialize documento into class T to apply _where func
            var obj = _mapper.ToObject<T>(doc);

            return _where(obj);
        }

        public override string ToString()
        {
            return string.Format("Linq({0})", _expr);
        }
    }
}