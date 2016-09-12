using System;

namespace LiteDB.Shell.Commands
{
    internal class Timer : IConsoleCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"timer$");
        }

        public void Execute(ref LiteEngine engine, StringScanner s, Display display, InputCommand input)
        {
            input.Timer.Start();
        }
    }
}