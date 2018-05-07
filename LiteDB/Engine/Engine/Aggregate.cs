using System;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Returns first value from an index (first is min value)
        /// </summary>
        public BsonValue Min(string collection, string field)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (field.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(field));

            using (_locker.Read())
            {
                var col = this.GetCollectionPage(collection, false);

                if (col == null) return BsonValue.MinValue;

                // get index (no index, no min)
                var index = col.GetIndex(field);

                if (index == null) return BsonValue.MinValue;

                var head = _indexer.GetNode(index.HeadNode);
                var next = _indexer.GetNode(head.Next[0]);

                if (next.IsHeadTail(index)) return BsonValue.MinValue;

                return next.Key;
            }
        }

        /// <summary>
        /// Returns last value from an index (last is max value)
        /// </summary>
        public BsonValue Max(string collection, string field)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (field.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(field));

            using (_locker.Read())
            {
                var col = this.GetCollectionPage(collection, false);

                if (col == null) return BsonValue.MaxValue;

                // get index (no index, no max)
                var index = col.GetIndex(field);

                if (index == null) return BsonValue.MaxValue;

                var tail = _indexer.GetNode(index.TailNode);
                var prev = _indexer.GetNode(tail.Prev[0]);

                if (prev.IsHeadTail(index)) return BsonValue.MaxValue;

                return prev.Key;
            }
        }

        /// <summary>
        /// Count all nodes from a query execution - do not deserialize documents to count. If query is null, use Collection counter variable
        /// </summary>
        public long Count(string collection, Query query = null)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));

            using (_locker.Read())
            {
                var col = this.GetCollectionPage(collection, false);

                if (col == null) return 0;

                if (query == null) return col.DocumentCount;

                // run query in this collection
                var nodes = query.Run(col, _indexer);

                if (query.UseFilter)
                {
                    // count distinct documents
                    return nodes
                        .Select(x => _bsonReader.Deserialize(_data.Read(x.DataBlock)).AsDocument)
                        .Where(x => query.FilterDocument(x))
                        .Distinct()
                        .LongCount();
                }
                else
                {
                    // count distinct nodes based on DataBlock
                    return nodes
                        .Select(x => x.DataBlock)
                        .Distinct()
                        .LongCount();
                }
            }
        }

        /// <summary>
        /// Check if has at least one node in a query execution - do not deserialize documents to check
        /// </summary>
        public bool Exists(string collection, Query query)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(collection));
            if (query == null) throw new ArgumentNullException(nameof(query));

            using (_locker.Read())
            {
                var col = this.GetCollectionPage(collection, false);

                if (col == null) return false;

                // run query in this collection
                var nodes = query.Run(col, _indexer);

                if (query.UseFilter)
                {
                    // check if has at least first document
                    return nodes
                        .Select(x => _bsonReader.Deserialize(_data.Read(x.DataBlock)).AsDocument)
                        .Where(x => query.FilterDocument(x))
                        .Any();
                }
                else
                {
                    var first = nodes.FirstOrDefault();

                    // check if has at least first node
                    return first != null;
                }
            }
        }
    }
}
