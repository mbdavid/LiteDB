using System.Collections.Generic;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Implement basic document loader based on data service/bson reader
    /// </summary>
    internal class DocumentLoader : IDocumentLoader
    {
        private readonly DataService _data;
        private readonly BsonReader _bsonReader;
        private readonly HashSet<string> _fields;
        private readonly CursorInfo _cursor;

        private Cache<PageAddress, BsonDocument> _cache = new Cache<PageAddress, BsonDocument>(MAX_CACHE_SIZE);

        public DocumentLoader(DataService data, bool utcDate, HashSet<string> fields, CursorInfo cursor)
        {
            _data = data;
            _bsonReader = new BsonReader(utcDate);
            _fields = fields;
            _cursor = cursor;
        }

        public BsonDocument Load(PageAddress rawId)
        {
            return _cache.GetOrAdd(rawId, id =>
            {
                // first, get datablock
                var block = _data.GetBlock(id);

                // otherwise, load byte array and deserialize
                var buffer = _data.Read(block);

                var doc = _bsonReader.Deserialize(buffer, _fields);
                doc.RawId = id;

                // inc cursor document fetch
                _cursor.DocumentFetch++;

                return doc;
            });
        }
    }
}