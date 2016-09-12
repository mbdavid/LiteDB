using System;

namespace LiteDB.Shell.Commands
{
    internal class Close : IConsoleCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"close$").Length > 0;
        }

        public void Execute(ref LiteEngine engine, StringScanner s, Display display, InputCommand input)
        {
            if (engine == null) throw ShellExpcetion.NoDatabase();

            engine.Dispose();

            engine = null;
        }
    }
}