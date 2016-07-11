using System;

namespace LiteDB.Shell.Commands
{
    internal class Quit : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Match(@"(quit|exit)$");
        }

        public override void Execute(ref IShellEngine engine, StringScanner s, Display display, InputCommand input)
        {
            if(engine != null) engine.Dispose();
            input.Running = false;
        }
    }
}