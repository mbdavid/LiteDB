using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    /// <summary>
    /// Implement virtual index for system collections
    /// </summary>
    internal class IndexVirtual : Index, IDocumentLoader
    {
        private IEnumerable<BsonDocument> _source;
        private Dictionary<uint, BsonDocument> _cache = new Dictionary<uint, BsonDocument>();
        private uint _counter = 0;

        public IndexVirtual(IEnumerable<BsonDocument> source)
            : base("_id", Query.Ascending)
        {
            _source = source;
        }

        internal override uint GetCost(CollectionIndex index)
        {
            // there is no way to determine how many document are inside _source without run Count() this
            return uint.MaxValue;
        }

        internal override IEnumerable<IndexNode> Execute(IndexService indexer, CollectionIndex index)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<IndexNode> Run(CollectionPage col, IndexService indexer)
        {
            foreach(var doc in _source)
            {
                _counter++;

                // create an fake page address for this document
                doc.RawId = new PageAddress(_counter, 1);

                // and add this document into cache to be used on Load method
                _cache[_counter] = doc;

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
            return _cache[dataBlock.PageID];
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