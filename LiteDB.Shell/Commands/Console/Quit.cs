using System;

namespace LiteDB.Shell.Commands
{
    internal class Quit : ICommand
    {
        public DataAccess Access { get { return DataAccess.None; } }

        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"(quit|exit)$");
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            if(engine != null) engine.Dispose();
            input.Running = false;
        }
    }
}