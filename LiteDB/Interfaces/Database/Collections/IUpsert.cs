using System.Collections.Generic;

namespace LiteDB
{
    public partial interface ILiteCollection<T>
    {
        /// <summary>
        /// Insert or Update a document in this collection.
        /// </summary>
        bool Upsert(T document);

        /// <summary>
        /// Insert or Update all documents
        /// </summary>
        int Upsert(IEnumerable<T> documents);

        /// <summary>
        /// Insert or Update a document in this collection.
        /// </summary>
        bool Upsert(BsonValue id, T document);
    }
}