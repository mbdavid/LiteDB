using System;
using System.Linq;

namespace LiteDB.Shell.Commands
{
    internal class FileFind : BaseStorage, ICommand
    {
        public DataAccess Access { get { return DataAccess.Read; } }

        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "find");
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            var fs = new LiteStorage(engine);

            if (s.HasTerminated)
            {
                var files = fs.FindAll().Select(x => x.AsDocument);

                display.WriteResult(new BsonArray(files));
            }
            else
            {
                var id = this.ReadId(s);

                var files = fs.Find(id).Select(x => x.AsDocument);

                display.WriteResult(new BsonArray(files));
            }
        }
    }
}