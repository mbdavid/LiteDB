using System.Collections.Generic;

namespace LiteDB
{
    public partial interface ILiteCollection<T>
    {
        /// <summary>
        /// Insert a new entity to this collection. Document Id must be a new value in collection - Returns document Id
        /// </summary>
        BsonValue Insert(T document);

        /// <summary>
        /// Insert a new document to this collection using passed id value.
        /// </summary>
        void Insert(BsonValue id, T document);

        /// <summary>
        /// Insert an array of new documents to this collection. Document Id must be a new value in collection. Can be set buffer size to commit at each N documents
        /// </summary>
        int Insert(IEnumerable<T> docs);        

        /// <summary>
        /// Implements bulk insert documents in a collection. Usefull when need lots of documents.
        /// </summary>
        int InsertBulk(IEnumerable<T> docs, int batchSize = 5000);
    }
}