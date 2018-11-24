using System.Collections.Generic;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Implement basic document loader based on data service/bson reader
    /// </summary>
    internal class DocumentLoader : IDocumentLoader
    {
        protected readonly DataService _data;
        protected readonly BufferReader _bsonReader;
        protected readonly HashSet<string> _fields;

        public DocumentLoader(DataService data, bool utcDate, HashSet<string> fields)
        {
            _data = data;
            _bsonReader = new BsonReader(utcDate);
            _fields = fields;
        }

        public virtual BsonDocument Load(PageAddress rawId)
        {
            // first, get datablock
            var block = _data.GetBlock(rawId);

            // otherwise, load byte array and deserialize
            var buffer = _data.Read(block);

            var doc = _bsonReader.Deserialize(buffer, _fields);
            doc.RawId = rawId;

            // inc cursor document fetch
            _cursor.DocumentLoad++;

            return doc;
        }
    }
}