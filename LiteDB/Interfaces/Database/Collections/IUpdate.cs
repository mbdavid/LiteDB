using System.Collections.Generic;

namespace LiteDB
{
    public partial interface ILiteCollection<T>
    {
        /// <summary>
        /// Update a document in this collection. Returns false if not found document in collection
        /// </summary>
        bool Update(T document);

        /// <summary>
        /// Update a document in this collection. Returns false if not found document in collection
        /// </summary>
        bool Update(BsonValue id, T document);

        /// <summary>
        /// Update all documents
        /// </summary>
        int Update(IEnumerable<T> documents);        
    }
}