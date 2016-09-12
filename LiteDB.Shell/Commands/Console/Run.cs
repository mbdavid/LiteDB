using System;
using System.IO;

namespace LiteDB.Shell.Commands
{
    internal class Run : IConsoleCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"run\s+").Length > 0;
        }

        public void Execute(ref LiteEngine engine, StringScanner s, Display display, InputCommand input)
        {
            if (engine == null) throw ShellExpcetion.NoDatabase();

            var filename = s.Scan(@".+").Trim();

            foreach (var line in File.ReadAllLines(filename))
            {
                input.Queue.Enqueue(line);
            }
        }
    }
}