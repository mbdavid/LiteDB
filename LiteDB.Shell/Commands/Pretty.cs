using System;

namespace LiteDB.Shell.Commands
{
    internal class Pretty : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"pretty\s*").Length > 0;
        }

        public void Execute(StringScanner s, Env env)
        {
            env.Display.Pretty = !(s.Scan(@"off\s*").Length > 0);
        }
    }
}