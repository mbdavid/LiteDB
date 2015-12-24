extern alias v090;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using v090::LiteDB;

namespace LiteDB.Shell
{
    class ShellEngine_090 : IShellEngine
    {
        private LiteEngine _db;

        public Version Version { get { return typeof(LiteEngine).Assembly.GetName().Version; } }

        public bool Detect(string filename)
        {
            return Helper.Try(() => new LiteEngine(filename));
        }

        public void Open(string connectionString)
        {
            _db = new LiteEngine(connectionString);
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        public void Debug(bool enabled)
        {
            throw new NotImplementedException("Debug does not work in this version");
        }

        public void Run(string command, Display display)
        {
            throw new NotImplementedException("This command does not work in this version");
        }

        /// <summary>
        /// Dump database converting to most recent version syntax
        /// </summary>
        public void Dump(TextWriter writer)
        {
            // do not include this collections now
            var specials = new string[] { "_master", "_files", "_chunks" };

            foreach(var name in _db.GetCollections().Where(x => !specials.Contains(x)))
            {
                var col = _db.GetCollection(name);
                var indexes = col.GetIndexes().Where(x => x["field"] != "_id");

                writer.WriteLine("-- Collection '{0}'", name);

                foreach(var index in indexes)
                {
                    writer.WriteLine("db.{0}.ensureIndex {1} {2}",
                        name,
                        index["field"].AsString,
                        JsonEx.Serialize(index));
                }

                foreach (var doc in col.Find(Query.All()))
                {
                    writer.WriteLine("db.{0}.insert {1}", name,  JsonEx.Serialize(doc));
                }
            }

            // convert FileStorage to new format
            var files = _db.GetCollection("_files");
            var chunks = _db.GetCollection("_chunks");

            if (files.Count() == 0) return;

            writer.WriteLine("-- FileStorage: _files");

            foreach (var file in files.Find(Query.All()))
            {
                // adding missing values
                file["chunks"] = chunks.Count(Query.StartsWith("_id", file.Id + @"\"));

                writer.WriteLine("db._files.insert {0}", JsonEx.Serialize(file));
            }

            writer.WriteLine("-- FileStorage: _chunks");

            foreach (var chunk in chunks.Find(Query.All()))
            {
                // adding _id format 00000
                chunk.Id = string.Format("{0}\\{1:00000}", 
                    chunk.Id.ToString().Substring(0, chunk.Id.ToString().IndexOf('\\')),
                    Convert.ToInt32(chunk.Id.ToString().Substring(chunk.Id.ToString().IndexOf('\\') + 1)));

                writer.WriteLine("db._chunks.insert {0}", JsonEx.Serialize(chunk));
            }
        }
    }
}
