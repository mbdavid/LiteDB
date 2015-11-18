using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        public const int DOCUMENT_BUFFER_SIZE = 1024;

        /// <summary>
        /// Insert a new document to this collection. Document Id must be a new value in collection - Returns document Id
        /// </summary>
        public BsonValue Insert(T document)
        {
            if (document == null) throw new ArgumentNullException("document");

            _mapper.SetAutoId(document, new LiteCollection<BsonDocument>(_name, _engine, _mapper, _log));

            var doc = _mapper.ToDocument(document);

            _engine.InsertDocuments(_name, new BsonDocument[] { doc }, 1);

            return doc["_id"];
        }

        /// <summary>
        /// Insert an array of new documents to this collection. Document Id must be a new value in collection. Can be set buffer size to commit at each N documents
        /// </summary>
        public int Insert(IEnumerable<T> docs, int buffer = DOCUMENT_BUFFER_SIZE)
        {
            if (docs == null) throw new ArgumentNullException("docs");

            return _engine.InsertDocuments(_name, this.GetBsonDocs(docs), buffer);
        }

        /// <summary>
        /// Convert each T document in a BsonDocument, setting autoId for each one
        /// </summary>
        private IEnumerable<BsonDocument> GetBsonDocs(IEnumerable<T> docs)
        {
            foreach (var document in docs)
            {
                _mapper.SetAutoId(document, new LiteCollection<BsonDocument>(_name, _engine, _mapper, _log));

                var doc = _mapper.ToDocument(document);

                yield return doc;
            }
        }
    }
}
