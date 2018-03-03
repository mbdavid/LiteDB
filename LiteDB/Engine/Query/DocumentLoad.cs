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
            // first, get datablock
            var block = _data.GetBlock(rawId);

            // if document already in dataBlock cache, just return
            if (block.CacheDocument != null)
            {
                return block.CacheDocument;
            }

            // otherwise, load byte array and deserialize
            var buffer = _data.Read(block);
            var doc = _bsonReader.Deserialize(buffer);
            doc.RawId = rawId;

            // add document to cache
            block.CacheDocument = doc;

            return doc;
        }
    }
}