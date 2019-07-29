using System;
using System.Collections.Generic;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Insert a new entity to this collection. Document Id must be a new value in collection - Returns document Id
        /// </summary>
        public BsonValue Insert(T document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            var doc = _mapper.ToDocument(document);
            var removed = this.RemoveDocId(doc);

            _engine.Value.Insert(_collection, new[] { doc }, _autoId);

            var id = doc["_id"];

            // checks if must update _id value in entity
            if (removed)
            {
                _id.Setter(document, id.RawValue);
            }

            return id;
        }

        /// <summary>
        /// Insert a new document to this collection using passed id value.
        /// </summary>
        public void Insert(BsonValue id, T document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (id == null || id.IsNull) throw new ArgumentNullException(nameof(id));

            var doc = _mapper.ToDocument(document);

            doc["_id"] = id;

            _engine.Value.Insert(_collection, new [] { doc }, _autoId);
        }

        /// <summary>
        /// Insert an array of new documents to this collection. Document Id must be a new value in collection. Can be set buffer size to commit at each N documents
        /// </summary>
        public int Insert(IEnumerable<T> docs)
        {
            if (docs == null) throw new ArgumentNullException(nameof(docs));

            return _engine.Value.Insert(_collection, this.GetBsonDocs(docs), _autoId);
        }

        /// <summary>
        /// Implements bulk insert documents in a collection. Usefull when need lots of documents.
        /// </summary>
        [Obsolete("Use normal Insert()")]
        public int InsertBulk(IEnumerable<T> docs, int batchSize = 5000)
        {
            if (docs == null) throw new ArgumentNullException(nameof(docs));

            return _engine.Value.Insert(_collection, this.GetBsonDocs(docs), _autoId);
        }

        /// <summary>
        /// Convert each T document in a BsonDocument, setting autoId for each one
        /// </summary>
        private IEnumerable<BsonDocument> GetBsonDocs(IEnumerable<T> documents)
        {
            foreach (var document in documents)
            {
                var doc = _mapper.ToDocument(document);
                var removed = this.RemoveDocId(doc);

                yield return doc;

                if (removed && _id != null)
                {
                    _id.Setter(document, doc["_id"].RawValue);
                }
            }
        }

        /// <summary>
        /// Remove document _id if contains a "empty" value (checks for autoId bson type)
        /// </summary>
        private bool RemoveDocId(BsonDocument doc)
        {
            if (doc.TryGetValue("_id", out var id)) 
            {
                // check if exists _autoId and current id is "empty"
                if ((_autoId == BsonAutoId.Int32 && (id.IsInt32 && id.AsInt32 == 0)) ||
                    (_autoId == BsonAutoId.ObjectId && (id.IsNull || (id.IsObjectId && id.AsObjectId == ObjectId.Empty))) ||
                    (_autoId == BsonAutoId.Guid && id.IsGuid && id.AsGuid == Guid.Empty) ||
                    (_autoId == BsonAutoId.Int64 && id.IsInt64 && id.AsInt64 == 0))
                {
                    // in this cases, remove _id and set new value after
                    doc.Remove("_id");
                    return true;
                }
            }

            return false;   
        }
    }
}