using System;

namespace LiteDB.Shell.Commands
{
    internal class Timer : ICommand
    {
        public DataAccess Access { get { return DataAccess.None; } }

        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"timer$");
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            input.Timer.Start();
        }
    }
}