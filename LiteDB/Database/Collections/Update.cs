using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Update a document in this collection. Returns false if not found document in collection
        /// </summary>
        public bool Update(T document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            // get BsonDocument from object
            var doc = _mapper.ToDocument(document);

            return _engine.Value.Update(_name, new BsonDocument[] { doc }) > 0;
        }

        /// <summary>
        /// Update a document in this collection. Returns false if not found document in collection
        /// </summary>
        public bool Update(BsonValue id, T document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (id == null || id.IsNull) throw new ArgumentNullException(nameof(id));

            // get BsonDocument from object
            var doc = _mapper.ToDocument(document);

            // set document _id using id parameter
            doc["_id"] = id;

            return _engine.Value.Update(_name, new BsonDocument[] { doc }) > 0;
        }

        /// <summary>
        /// Update all documents
        /// </summary>
        public int Update(IEnumerable<T> documents)
        {
            if (documents == null) throw new ArgumentNullException(nameof(documents));

            return _engine.Value.Update(_name, documents.Select(x => _mapper.ToDocument(x)));
        }
    }
}