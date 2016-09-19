using System;

namespace LiteDB.Shell.Commands
{
    internal class Comment : IConsoleCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"--");
        }

        public void Execute(ref LiteEngine engine, StringScanner s, Display display, InputCommand input)
        {
        }
    }
}