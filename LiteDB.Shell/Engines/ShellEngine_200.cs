extern alias v200;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using v200::LiteDB;

namespace LiteDB.Shell
{
    class ShellEngine_200 : IShellEngine
    {
        private LiteDatabase _db;

        public Version Version { get { return typeof(LiteDatabase).Assembly.GetName().Version; } }

        public bool Detect(string filename)
        {
            return Helper.Try(() => new LiteDatabase(filename).CollectionExists("dummy"));
        }

        public void Open(string connectionString)
        {
            _db = new LiteDatabase(connectionString);
            _db.Log.Logging += ShellProgram.LogMessage;
        }

        public void Dispose()
        {
            _db.Dispose();
        }

        public void Debug(bool enabled)
        {
            _db.Log.Level = enabled ? Logger.FULL : Logger.NONE;
        }

        public void Run(string command, Display display)
        {
            var result = _db.Run(command);

            this.WriteResult(result, display);
        }

        public void Export(Stream stream)
        {
            using (var dump = new DumpWriter(stream))
            {
                var collections =  _db.GetCollectionNames();

                foreach (var name in collections)
                {
                    _db.Log.Write(Logger.COMMAND, "exporting \"{0}\"...", name);

                    var col = _db.GetCollection(name);
                    var indexes = col.GetIndexes();

                    dump.WriteCollection(name);

                    foreach (var index in indexes)
                    {
                        dump.WriteIndex(index["field"].AsString, "");
                    }

                    foreach (var doc in col.Find(Query.All()))
                    {
                        dump.WriteDocument(JsonSerializer.Serialize(doc, false, true));
                    }
                }
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
