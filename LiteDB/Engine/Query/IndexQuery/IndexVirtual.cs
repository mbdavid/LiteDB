using System;
using System.Collections.Generic;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Implement virtual index for system collections
    /// </summary>
    internal class IndexVirtual : Index, IDocumentLoader
    {
        private readonly IEnumerable<BsonDocument> _source;
        private readonly Dictionary<uint, BsonDocument> _cache = new Dictionary<uint, BsonDocument>();
        private BsonDocument _current = null;
        private uint _counter = 0;

        public IndexVirtual(IEnumerable<BsonDocument> source)
            : base("_id", Query.Ascending)
        {
            _source = source;
        }

        public override uint GetCost(CollectionIndex index)
        {
            // there is no way to determine how many document are inside _source without run Count() this
            return uint.MaxValue;
        }

        public override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IndexNode> Run(CollectionPage col, IndexService indexer)
        {
            foreach(var doc in _source)
            {
                _counter++;

                // create an fake page address for this document
                doc.RawId = new PageAddress(_counter, 1);

                // if cache reach max count, clear cache and use only single value (with no support from random access - group by)
                if (_counter > MAX_CACHE_DOCUMENT_LOADER_SIZE)
                {
                    _current = doc;
                }
                else
                {
                    // and add this document into cache to be used on Load method
                    _cache[_counter] = doc;
                }

                // return an fake indexNode
                yield return new IndexNode(0)
                {
                    Key = (int)_counter,
                    DataBlock = doc.RawId
                };
            }
        }

        public BsonDocument Load(PageAddress dataBlock)
        {
            if (_current != null)
            {
                if (_current.RawId.PageID != dataBlock.PageID) throw new LiteException(0, $"When system collection reach {MAX_CACHE_DOCUMENT_LOADER_SIZE} documents there is no more support for random access (like in group by operations)");

                return _current;
            }
            else
            {
                return _cache[dataBlock.PageID];
            }
        }

        public override string ToString()
        {
            return string.Format("FULL INDEX SCAN(VIRTUAL)");
        }

        /// <summary>
        /// Create fake collection page for virtual collections
        /// </summary>
        public static CollectionPage CreateCollectionPage(string name)
        {
            var col = new CollectionPage(0) { CollectionName = name };

            var pk = col.GetFreeIndex();

            pk.Name = "_id";
            pk.Expression = "$._id";

            return col;
        }
    }
}