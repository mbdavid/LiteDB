using System;
using System.Collections.Generic;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Insert a new document to this collection. Document Id must be a new value in collection - Returns document Id
        /// </summary>
        public void Insert(T document)
        {
            if (document == null) throw new ArgumentNullException("document");

            _engine.Value.Insert(_name, this.GetBsonDocs(new [] { document }), _autoId);
        }

        /// <summary>
        /// Insert a new document to this collection using passed id value.
        /// </summary>
        public void Insert(BsonValue id, T document)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (id == null || id.IsNull) throw new ArgumentNullException("id");

            var doc = _mapper.ToDocument(document);

            doc["_id"] = id;

            _engine.Value.Insert(_name, doc);
        }

        /// <summary>
        /// Insert an array of new documents to this collection. Document Id must be a new value in collection. Can be set buffer size to commit at each N documents
        /// </summary>
        public int Insert(IEnumerable<T> docs)
        {
            if (docs == null) throw new ArgumentNullException("docs");

            return _engine.Value.Insert(_name, this.GetBsonDocs(docs), _autoId);
        }

        /// <summary>
        /// Implements bulk insert documents in a collection. Usefull when need lots of documents.
        /// </summary>
        public int InsertBulk(IEnumerable<T> docs, int batchSize = 5000)
        {
            if (docs == null) throw new ArgumentNullException("docs");

            return _engine.Value.InsertBulk(_name, this.GetBsonDocs(docs), batchSize, _autoId);
        }

        /// <summary>
        /// Convert each T document in a BsonDocument, setting autoId for each one
        /// </summary>
        private IEnumerable<BsonDocument> GetBsonDocs(IEnumerable<T> documents)
        {
            foreach (var document in documents)
            {
                var doc = _mapper.ToDocument(document);

                if (_autoId != BsonType.Null)
                {
                    doc.Remove("_id");
                }

                yield return doc;

                if (_autoId != BsonType.Null)
                {
                    _id.Setter(document, doc["_id"].RawValue);
                }
            }
        }
    }
}