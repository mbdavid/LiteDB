using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Insert a new entity to this collection. Document Id must be a new value in collection - Returns document Id
        /// </summary>
        public BsonValue Insert(T document)
        {
            if (document == null) throw new ArgumentNullException("document");

            var docs = this.GetBsonDocs(new[] { document });

            _engine.Value.Insert(_name, docs, _autoId);

            return docs.Single()["_id"];
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
                var setId = false;
                var id = doc["_id"];

                // check if exists _autoId and current id is "empty"
                if ((_autoId == BsonType.ObjectId && (id.IsNull || id.AsObjectId == ObjectId.Empty)) ||
                    (_autoId == BsonType.Guid && id.AsGuid == Guid.Empty) ||
                    (_autoId == BsonType.DateTime && id.AsDateTime == DateTime.MinValue) ||
                    (_autoId == BsonType.Int32 && id.AsInt32 == 0) ||
                    (_autoId == BsonType.Int64 && id.AsInt64 == 0))
                {
                    // in this cases, remove _id and set new value after
                    doc.Remove("_id");
                    setId = true;
                }

                yield return doc;

                if (setId && _id != null)
                {
                    _id.Setter(document, doc["_id"].RawValue);
                }
            }
        }
    }
}