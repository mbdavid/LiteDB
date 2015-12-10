using System;

namespace LiteDB.Shell.Commands
{
    internal class Quit : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Match(@"(quit|exit)$");
        }

        public override void Execute(ref LiteDatabase db, StringScanner s, Display display, InputCommand input)
        {
            Environment.Exit(0);
        }
    }
}