using System;
using System.Collections.Generic;

namespace LiteDB.Engine
{
    /// <summary>
    /// Interface to read current or old datafile structure - Used to shirnk/upgrade datafile from old LiteDB versions
    /// </summary>
    interface IFileReader
    {
        /// <summary>
        /// Get all collections name from database
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetCollections();

        /// <summary>
        /// Get all indexes from collection (except _id index)
        /// </summary>
        IEnumerable<IndexInfo> GetIndexes(string name);

        /// <summary>
        /// Get all documents from a collection
        /// </summary>
        IEnumerable<BsonDocument> GetDocuments(string collection);
    }
}