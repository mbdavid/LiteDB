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
            using(var reader = _engine.Query("$indexes", new QueryDefinition()))
            {
                while(reader.Read())
                {
                    yield return new IndexInfo
                    {
                        Collection = reader.Current["collection"].AsString,
                        Name = reader.Current["name"].AsString,
                        Expression = reader.Current["expression"].AsString,
                        Unique = reader.Current["unique"].AsBoolean,
                        HeadPageID = 0 // not used
                    };
                }
            }
        }

        /// <summary>
        /// Read all document based on collection name
        /// </summary>
        public IEnumerable<BsonDocument> GetDocuments(IndexInfo index)
        {
            using (var reader = _engine.Query(index.Collection, new QueryDefinition()))
            {
                while(reader.Read())
                {
                    yield return reader.Current.AsDocument;
                }
            }
        }
    }
}