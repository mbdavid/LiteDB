using System;
using System.Linq;

namespace LiteDB.Shell.Commands
{
    internal class FileFind : BaseStorage, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "find");
        }

        public void Execute(StringScanner s, Env env)
        {
            var fs = new LiteStorage(env.Engine);

            if (s.HasTerminated)
            {
                var files = fs.FindAll().Select(x => x.AsDocument);

                env.Display.WriteResult(new BsonArray(files));
            }
            else
            {
                var id = this.ReadId(s);

                var files = fs.Find(id).Select(x => x.AsDocument);

                env.Display.WriteResult(new BsonArray(files));
            }
        }
    }
}