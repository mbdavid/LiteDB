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
        protected readonly bool _utcDate;
        protected readonly HashSet<string> _fields;

        public DocumentLoader(DataService data, bool utcDate, HashSet<string> fields)
        {
            _data = data;
            _utcDate = utcDate;
            _fields = fields;
        }

        public virtual BsonDocument Load(IndexNode node)
        {
            ENSURE(node.DataBlock != PageAddress.Empty, "data block must be a valid block address");

            using (var reader = new BufferReader(_data.Read(node.DataBlock), _utcDate))
            {
                var doc = reader.ReadDocument(_fields);

                doc.RawId = node.DataBlock;

                return doc;
            }
        }
    }
}