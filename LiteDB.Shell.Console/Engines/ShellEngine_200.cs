extern alias v200;
using System;
using System.IO;
using System.Linq;
using v200::LiteDB;
using v200::LiteDB.Shell;
using v200::LiteDB.Interfaces;

namespace LiteDB.Shell
{
    class ShellEngine_200 : IShellEngine
    {
        private ILiteDatabase _db;
        private LiteShell _liteShell;

        public Version Version { get { return typeof(LiteDatabase).Assembly.GetName().Version; } }

        public bool Detect(string filename)
        {
            return true; //TODO: implement a better version detect (using byte position)
        }

        public void Open(string connectionString)
        {
            _db = LiteDatabaseFactory.Create(connectionString);
            _db.Log.Logging += ShellProgram.LogMessage;
            _liteShell = new LiteShell(_db);        }

        public void Dispose()
        {

         if (_db != null)
            _db.Dispose();
        }

        public void Debug(bool enabled)
        {
            _db.Log.Level = enabled ? Logger.FULL : Logger.NONE;
        }

        public void Run(string command, Display display)
        {
            var result = _liteShell.Run(command);

            this.WriteResult(result, display);
        }

        public void Dump(TextWriter writer)
        {
            foreach (var name in _db.GetCollectionNames())
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
                    writer.WriteLine("db.{0}.insert {1}", name, JsonSerializer.Serialize(doc, false, true));
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
