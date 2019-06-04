using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Not is an Index Scan operation
    /// </summary>
    internal class QueryNotEquals : Query
    {
        private BsonValue _value;

        public QueryNotEquals(string field, BsonValue value)
            : base(field)
        {
            _value = value;
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            return indexer
                .FindAll(index, Query.Ascending)
                .Where(x => x.Key.CompareTo(_value) != 0);
        }

        internal override bool FilterDocument(BsonDocument doc)
        {
            return this.Expression.Execute(doc, true)
                .Any(x => x.CompareTo(_value) != 0);
        }

        public override BsonValue ToMongoQuery()
        {
            BsonDocument opt = new BsonDocument();
            opt.Add("$ne", _value);
            BsonDocument notequal = new BsonDocument();
            notequal.Add(this.Field, opt);
            return notequal;
        }

        public override string ToString()
        {
            return string.Format("{0}({1} != {2})",
                this.UseFilter ? "Filter" : this.UseIndex ? "Scan" : "",
                this.Expression?.ToString() ?? this.Field,
                _value);
        }
    }
}