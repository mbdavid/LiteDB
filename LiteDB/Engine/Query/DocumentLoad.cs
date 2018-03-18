using System.Collections.Generic;

namespace LiteDB
{
    /// <summary>
    /// Implement basic document loader based on data service/bson reader
    /// </summary>
    internal class DocumentLoader : IDocumentLoader
    {
        private readonly DataService _data;
        private readonly BsonReader _bsonReader;
        private readonly HashSet<string> _fields;

        public DocumentLoader(DataService data, bool utcDate, HashSet<string> fields)
        {
            _data = data;
            _bsonReader = new BsonReader(utcDate);
            _fields = fields;
        }

        public BsonDocument Load(PageAddress rawId)
        {
            // first, get datablock
            var block = _data.GetBlock(rawId);

            // if document already in dataBlock cache, just return
            if (block.CacheDocument != null)
            {
                return block.CacheDocument;
            }

            // otherwise, load byte array and deserialize
            var buffer = _data.Read(block);
            var doc = _bsonReader.Deserialize(buffer, _fields);
            doc.RawId = rawId;

            // add document to cache
            block.CacheDocument = doc;

            return doc;
        }
    }
}