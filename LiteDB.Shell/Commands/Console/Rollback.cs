using System;

namespace LiteDB.Shell.Commands
{
    internal class Rollback : ICommand
    {
        public DataAccess Access { get { return DataAccess.Write; } }

        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"rollback$").Length > 0;
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            engine.Rollback();
        }
    }
}