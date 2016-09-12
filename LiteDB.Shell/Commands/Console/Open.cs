using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB.Shell.Commands
{
    internal class Open : IConsoleCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"open\s+").Length > 0;
        }

        public void Execute(ref LiteEngine engine, StringScanner s, Display display, InputCommand input)
        {
            var filename = s.Scan(@".+");

            if (engine != null)
            {
                engine.Dispose();
            }

            engine = new LiteEngine(filename);
        }
    }
}