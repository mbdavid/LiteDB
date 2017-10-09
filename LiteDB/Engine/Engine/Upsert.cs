using System;
using System.Collections.Generic;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Implement upsert command to documents in a collection. Calls update on all documents,
        /// then any documents not updated are then attempted to insert.
        /// This will have the side effect of throwing if duplicate items are attempted to be inserted. Returns true if document is inserted
        /// </summary>
        public bool Upsert(string collection, BsonDocument doc, BsonType autoId = BsonType.ObjectId)
        {
            if (doc == null) throw new ArgumentNullException("doc");

            return this.Upsert(collection, new BsonDocument[] { doc }, autoId) == 1;
        }

        /// <summary>
        /// Implement upsert command to documents in a collection. Calls update on all documents,
        /// then any documents not updated are then attempted to insert.
        /// This will have the side effect of throwing if duplicate items are attempted to be inserted.
        /// </summary>
        public int Upsert(string collection, IEnumerable<BsonDocument> docs, BsonType autoId = BsonType.ObjectId)
        {
            if (collection.IsNullOrWhiteSpace()) throw new ArgumentNullException("collection");
            if (docs == null) throw new ArgumentNullException("docs");

            return this.Transaction<int>(collection, true, (col) =>
            {
                var count = 0;

                foreach (var doc in docs)
                {
                    if (doc["_id"] == BsonValue.Null)
                    {
                        // Try insert document (if doc has no _id)
                        this.InsertDocument(col, doc, autoId);
                        count++;
                    }
                    else {
                        if (this.UpdateDocument(col, doc))
                        {
                            // Try update document (if doc has _id)
                            count++;
                        }
                        else
                        {
                            // Try insert document (if doc has _id, but couldn't update)
                            this.InsertDocument(col, doc, autoId);
                            count++;
                        }
                    }

                    _trans.CheckPoint();
                }

                // returns how many document was inserted
                return count;
            });
        }
    }
}