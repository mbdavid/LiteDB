using System;

namespace LiteDB.Shell.Commands
{
    internal class Pretty : IConsoleCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"pretty\s*").Length > 0;
        }

        public void Execute(ref LiteEngine engine, StringScanner s, Display display, InputCommand input)
        {
            display.Pretty = !(s.Scan(@"off\s*").Length > 0);
        }
    }
}