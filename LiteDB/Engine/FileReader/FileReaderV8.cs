using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Internal class to read all datafile documents - use current engine version
    /// </summary>
    internal class FileReaderV8 : IFileReader
    {
        private LiteEngine _engine;

        public int UserVersion { get; set; }

        public FileReaderV8(string filename, string password)
        {
            _engine = new LiteEngine(new EngineSettings
            {
                Filename = filename,
                Password = password,
                ReadOnly = true,
                LogStream = new MemoryStream() // never will be used... it's a readonly database
            });

            this.UserVersion = _engine.UserVersion;
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
            using(var reader = _engine.Query("$indexes", new Query()))
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
            using (var reader = _engine.Query(index.Collection, new Query()))
            {
                while(reader.Read())
                {
                    yield return reader.Current.AsDocument;
                }
            }
        }

        public void Dispose()
        {
            _engine.Dispose();
        }
    }
}