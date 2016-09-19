using System;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Debug : IConsoleCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"debug\s*").Length > 0;
        }

        public void Execute(ref LiteEngine engine, StringScanner s, Display d, InputCommand input)
        {
            var sb = new StringBuilder();
            var enabled = !(s.Scan(@"off\s*").Length > 0);

            if(engine == null) throw ShellExpcetion.NoDatabase();

            engine.Log.Level = enabled ? Logger.FULL : Logger.NONE;
            engine.Log.Logging += (msg) =>
            {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.WriteLine(msg);
            };
        }
    }
}