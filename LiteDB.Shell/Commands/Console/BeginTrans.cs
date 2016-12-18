using System;

namespace LiteDB.Shell.Commands
{
    internal class BeginTrans : ICommand
    {
        public DataAccess Access { get { return DataAccess.Write; } }

        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"begin\s+trans(action)?").Length > 0;
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            engine.BeginTrans();
        }
    }
}