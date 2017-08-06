using System;

namespace LiteDB.Shell.Commands
{
    internal class FileDelete : BaseStorage, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsFileCommand(s, "delete");
        }

        public void Execute(StringScanner s, Env env)
        {
            var fs = new LiteStorage(env.Engine);
            var id = this.ReadId(s);

            env.Display.WriteResult(fs.Delete(id));
        }
    }
}