using System;

namespace LiteDB.Shell.Commands
{
    internal class FileDelete : BaseStorage, ICommand
    {
        public DataAccess Access { get { return DataAccess.Read; } }

        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "delete");
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            var fs = new LiteStorage(engine);
            var id = this.ReadId(s);

            display.WriteResult(fs.Delete(id));
        }
    }
}