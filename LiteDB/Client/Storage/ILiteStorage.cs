using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;

namespace LiteDB
{
    public interface ILiteStorage<T>
    {
        /// <summary>
        /// Find a file inside datafile and returns LiteFileInfo instance. Returns null if not found
        /// </summary>
        LiteFileInfo<T> FindById(T id);

        /// <summary>
        /// Find all files that match with predicate expression.
        /// </summary>
        IEnumerable<LiteFileInfo<T>> Find(BsonExpression predicate);

        /// <summary>
        /// Find all files that match with predicate expression.
        /// </summary>
        IEnumerable<LiteFileInfo<T>> Find(string predicate, BsonDocument parameters);

        /// <summary>
        /// Find all files that match with predicate expression.
        /// </summary>
        IEnumerable<LiteFileInfo<T>> Find(string predicate, params BsonValue[] args);

        /// <summary>
        /// Find all files that match with predicate expression.
        /// </summary>
        IEnumerable<LiteFileInfo<T>> Find(Expression<Func<LiteFileInfo<T>, bool>> predicate);

        /// <summary>
        /// Find all files inside file collections
        /// </summary>
        IEnumerable<LiteFileInfo<T>> FindAll();

        /// <summary>
        /// Returns if a file exisits in database
        /// </summary>
        bool Exists(T id);

        /// <summary>
        /// Open/Create new file storage and returns linked Stream to write operations.
        /// </summary>
        LiteFileStream<T> OpenWrite(T id, string filename, BsonDocument metadata = null);

        /// <summary>
        /// Upload a file based on stream data
        /// </summary>
        LiteFileInfo<T> Upload(T id, string filename, Stream stream, BsonDocument metadata = null);

        /// <summary>
        /// Upload a file based on file system data
        /// </summary>
        LiteFileInfo<T> Upload(T id, string filename);

        /// <summary>
        /// Update metadata on a file. File must exist.
        /// </summary>
        bool SetMetadata(T id, BsonDocument metadata);

        /// <summary>
        /// Load data inside storage and returns as Stream
        /// </summary>
        LiteFileStream<T> OpenRead(T id);

        /// <summary>
        /// Copy all file content to a steam
        /// </summary>
        LiteFileInfo<T> Download(T id, Stream stream);

        /// <summary>
        /// Copy all file content to a file
        /// </summary>
        LiteFileInfo<T> Download(T id, string filename, bool overwritten);

        /// <summary>
        /// Delete a file inside datafile and all metadata related
        /// </summary>
        bool Delete(T id);
    }
}