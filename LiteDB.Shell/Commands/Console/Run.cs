using System;
using System.IO;

namespace LiteDB.Shell.Commands
{
    internal class Run : ICommand
    {
        public DataAccess Access { get { return DataAccess.None; } }

        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"run\s+").Length > 0;
        }

        public void Execute(LiteEngine engine, StringScanner s, Display display, InputCommand input, Env env)
        {
            if (engine == null) throw ShellException.NoDatabase();

            var filename = s.Scan(@".+").Trim();

            foreach (var line in File.ReadAllLines(filename))
            {
                input.Queue.Enqueue(line);
            }
        }
    }
}