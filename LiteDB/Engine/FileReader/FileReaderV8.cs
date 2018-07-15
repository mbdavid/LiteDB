using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    /// <summary>
    /// Internal class to read all datafile documents - use simplest way using current engine
    /// </summary>
    internal class FileReaderV8 : IFileReader
    {
        private LiteEngine _engine;

        public DateTime CreationTime { get; set; } = DateTime.Now;
        public uint CommitCounter { get; set; } = 0;
        public DateTime LastCommit { get; set; } = DateTime.MinValue;
        public int UserVersion { get; set; }

        public FileReaderV8(LiteEngine engine, HeaderPage header)
        {
            _engine = engine;

            this.CreationTime = header.CreationTime;
            this.UserVersion = header.UserVersion;
        }

        /// <summary>
        /// Read all collection based on header page
        /// </summary>
        public IEnumerable<string> GetCollections()
        {
            return _engine.GetCollectionNames();
        }

        /// <summary>
        /// Read all indexes from all collection pages
        /// </summary>
        public IEnumerable<IndexInfo> GetIndexes()
        {
            foreach(var index in _engine.Query("$indexes").ToEnumerable())
            {
                yield return new IndexInfo
                {
                    Collection = index["collection"].AsString,
                    Name = index["name"].AsString,
                    Expression = index["expression"].AsString,
                    Unique = index["unique"].AsBoolean,
                    HeadPageID = 0 // not used
                };
            }
        }

        /// <summary>
        /// Read all document based on collection name
        /// </summary>
        public IEnumerable<BsonDocument> GetDocuments(IndexInfo index)
        {
            return _engine.Query(index.Collection).ToEnumerable();
        }
    }
}