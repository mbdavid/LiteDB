using System;

namespace LiteDB.Shell.Commands
{
    internal class Quit : IConsoleCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"(quit|exit)$");
        }

        public void Execute(ref LiteEngine engine, StringScanner s, Display display, InputCommand input)
        {
            if(engine != null) engine.Dispose();
            input.Running = false;
        }
    }
}