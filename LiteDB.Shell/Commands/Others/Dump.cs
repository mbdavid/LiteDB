using System;
using System.IO;

namespace LiteDB.Shell.Commands
{
    internal class Dump : IConsoleCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"dump\s+").Length > 0;
        }

        public void Execute(ref LiteEngine engine, StringScanner s, Display display, InputCommand input)
        {
            if (engine == null) throw ShellExpcetion.NoDatabase();

            var dir = s.Scan(@"[<>]\s*").Trim();
            var filename = s.Scan(@".+").Trim();



        }
    }
}