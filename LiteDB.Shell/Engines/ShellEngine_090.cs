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

        public void Export(Stream stream)
        {
            var dump = new DumpWriter(stream);
            var collections = _db.GetCollections();

            foreach(var name in collections)
            {
                var col = _db.GetCollection(name);
                var indexes = col.GetIndexes();

                dump.WriteCollection(name);

                foreach(var index in indexes)
                {
                    dump.WriteIndex(index["field"], "");
                }

                foreach(var doc in col.Find(Query.All()))
                {
                    dump.WriteDocument(JsonEx.Serialize(doc, false, true));
                }
            }
        }
    }
}
