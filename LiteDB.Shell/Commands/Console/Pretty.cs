using System;

namespace LiteDB.Shell.Commands
{
    internal class Pretty : ICommand
    {
        public DataAccess Access { get { return DataAccess.None; } }

        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"pretty\s*").Length > 0;
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            display.Pretty = !(s.Scan(@"off\s*").Length > 0);
        }
    }
}