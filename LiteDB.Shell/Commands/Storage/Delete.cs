using System;

namespace LiteDB.Shell.Commands
{
    internal class FileDelete : BaseStorage, IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "delete");
        }

        public BsonValue Execute(LiteEngine engine, StringScanner s)
        {
            var fs = new LiteStorage(engine);
            var id = this.ReadId(s);

            return fs.Delete(id);
        }
    }
}