using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Insert or Update a document in this collection.
        /// </summary>
        public bool Upsert(T document)
        {
            if (document == null) throw new ArgumentNullException("document");

            // use locker because needs by SetAutoId be isolated
            using (_engine.Value.Locker.Reserved())
            {
                // set autoId if there is no Id
                _mapper.SetAutoId(document, _engine.Value, _name);

                // get BsonDocument from object
                var doc = _mapper.ToDocument(document);

                return _engine.Value.Upsert(_name, doc);
            }
        }

        /// <summary>
        /// Insert or Update a document in this collection.
        /// </summary>
        public bool Upsert(BsonValue id, T document)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (id == null || id.IsNull) throw new ArgumentNullException("id");

            // get BsonDocument from object
            var doc = _mapper.ToDocument(document);

            // set document _id using id parameter
            doc["_id"] = id;

            return _engine.Value.Upsert(_name, doc);
        }

        /// <summary>
        /// Insert or Update all documents
        /// </summary>
        public int Upsert(IEnumerable<T> documents)
        {
            if (documents == null) throw new ArgumentNullException("document");

            using (_engine.Value.Locker.Reserved())
            {
                return _engine.Value.Upsert(_name, this.GetBsonDocs(documents));
            }
        }
    }
}