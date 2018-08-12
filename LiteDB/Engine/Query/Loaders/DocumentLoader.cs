using System.Collections.Generic;

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

        public DocumentLoader(DataService data, bool utcDate, HashSet<string> fields, CursorInfo cursor)
        {
            _data = data;
            _bsonReader = new BsonReader(utcDate);
            _fields = fields;
            _cursor = cursor;
        }

        public BsonDocument Load(PageAddress rawId)
        {
            // first, get datablock
            var block = _data.GetBlock(rawId);

            // otherwise, load byte array and deserialize
            var buffer = _data.Read(block);
            var doc = _bsonReader.Deserialize(buffer, _fields);
            doc.RawId = rawId;

            // inc cursor document fetch
            _cursor.DocumentFetch++;

            return doc;
        }
    }
}