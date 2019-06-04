using System.Collections.Generic;

namespace LiteDB
{
    /// <summary>
    /// All is an Index Scan operation
    /// </summary>
    internal class QueryAll : Query
    {
        private int _order;

        public QueryAll(string field, int order)
            : base(field)
        {
            _order = order;
        }

        internal override IEnumerable<IndexNode> ExecuteIndex(IndexService indexer, CollectionIndex index)
        {
            return indexer.FindAll(index, _order);
        }

        internal override bool FilterDocument(BsonDocument doc)
        {
            return true;
        }

        public override BsonValue ToMongoQuery()
        {
            BsonDocument all = new BsonDocument();
            all.Add("$query", new BsonDocument());
            //BsonDocument order = new BsonDocument();
            //order.Add(this.Field, _order);
            //all.Add("$orderby", order);
            return all;
        }

        public override string ToString()
        {
            return string.Format("{0}({1})",
                this.UseFilter ? "Filter" : this.UseIndex ? "Scan" : "",
                this.Expression?.ToString() ?? this.Field);
        }
    }
}