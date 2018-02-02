namespace LiteDB
{
    /// <summary>
    /// Implement basic document loader based on data service/bson reader
    /// </summary>
    internal class DocumentLoader : IDocumentLoader
    {
        private readonly DataService _data;
        private readonly BsonReader _bsonReader;

        public DocumentLoader(DataService data, BsonReader bsonReader)
        {
            _data = data;
            _bsonReader = bsonReader;
        }

        public BsonDocument Load(PageAddress rawId)
        {
            // read bytes from disk and deserialize with BSON reader
            var buffer = _data.Read(rawId);
            var doc = _bsonReader.Deserialize(buffer);
            doc.RawId = rawId;

            return doc;
        }
    }
}