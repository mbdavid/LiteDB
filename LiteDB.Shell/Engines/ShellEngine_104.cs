extern alias v104;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using v104::LiteDB;

namespace LiteDB.Shell
{
    class ShellEngine_104 : IShellEngine
    {
        private LiteDatabase _db;

        public Version Version { get { return typeof(LiteDatabase).Assembly.GetName().Version; } }

        public bool Detect(string filename)
        {
            return Helper.Try(() => new LiteDatabase(filename));
        }

        public void Open(string connectionString)
        {
            _db = new LiteDatabase(connectionString);
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
            var result = _db.RunCommand(command);

            this.WriteResult(result, display);
        }

        public void Dump(TextWriter writer)
        {
            // do not include this collections now
            var specials = new string[] { "_files" };

            foreach (var name in _db.GetCollectionNames().Where(x => !specials.Contains(x)))
            {
                var col = _db.GetCollection(name);
                var indexes = col.GetIndexes().Where(x => x["field"] != "_id");

                writer.WriteLine("-- Collection '{0}'", name);

                foreach (var index in indexes)
                {
                    writer.WriteLine("db.{0}.ensureIndex {1} {2}",
                        name,
                        index["field"].AsString,
                        JsonSerializer.Serialize(index["options"].AsDocument));
                }

                foreach (var doc in col.Find(Query.All()))
                {
                    writer.WriteLine("db.{0}.insert {1}", name, JsonSerializer.Serialize(doc));
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
                file["chunks"] = chunks.Count(Query.StartsWith("_id", file["_id"] + @"\"));

                writer.WriteLine("db._files.insert {0}", JsonSerializer.Serialize(file));
            }
        }

        #region Display Bson Result

        private void WriteResult(BsonValue result, Display display)
        {
            var index = 0;

            if (result.IsNull) return;

            if (result.IsDocument)
            {
                display.WriteLine(ConsoleColor.DarkCyan, JsonSerializer.Serialize(result, display.Pretty, false));
            }
            else if (result.IsArray)
            {
                foreach (var doc in result.AsArray)
                {
                    display.Write(ConsoleColor.Cyan, string.Format("[{0}]:{1}", ++index, display.Pretty ? Environment.NewLine : " "));
                    display.WriteLine(ConsoleColor.DarkCyan, JsonSerializer.Serialize(doc, display.Pretty, false));
                }

                if (index == 0)
                {
                    display.WriteLine(ConsoleColor.DarkCyan, "no documents");
                }
            }
            else if (result.IsString)
            {
                display.WriteLine(ConsoleColor.DarkCyan, result.AsString);
            }
            else
            {
                display.WriteLine(ConsoleColor.DarkCyan, JsonSerializer.Serialize(result, display.Pretty, false));
            }
        }

        #endregion

    }
}
