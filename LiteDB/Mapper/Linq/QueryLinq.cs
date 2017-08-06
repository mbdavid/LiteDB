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
        private Expression<Func<T, bool>> _predicate;
        private QueryVisitor<T> _visitor;
        private BsonMapper _mapper;
        private Query _query = null;
        private Func<T, bool> _where = null;

        public QueryLinq(Expression<Func<T, bool>> predicate, QueryVisitor<T> visitor, BsonMapper mapper)
            : base(null)
        {
            _predicate = predicate;
            _mapper = mapper;
            _visitor = visitor;

            try
            {
                _query = _visitor.Visit(_predicate);
            }
            catch(NotSupportedException)
            {
                _where = predicate.Compile();
            }
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            if (_query != null) return _query.ExecuteIndex(indexer, index);

            throw new NotSupportedException();
        }

        internal override bool FilterDocument(BsonDocument doc)
        {
            if (_query != null) return _query.FilterDocument(doc);

            // must deserialize documento into class T to apply _where func
            var obj = _mapper.ToObject<T>(doc);

            return _where(obj);
        }

        internal override IEnumerable<IndexNode> Run(CollectionPage col, IndexService indexer)
        {
            if (_query != null)
            {
                //this.RunMode = _query.RunMode;

                return _query.Run(col, indexer);
            }
            else
            {
                //this.RunMode = QueryMode.Fullscan;

                return Query.All().Run(col, indexer);
            }
        }

        public override string ToString()
        {
            if (_query != null) return _query.ToString();

            return "LINQ expr: " + _predicate.ToString();
        }
    }
}