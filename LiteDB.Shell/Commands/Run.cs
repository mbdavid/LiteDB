using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Run : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Scan(@"run\s+").Length > 0;
        }

        public override void Execute(LiteShell shell, StringScanner s, Display display, InputCommand input)
        {
            var filename = s.Scan(@".+").Trim();

            foreach (var line in File.ReadAllLines(filename))
            {
                input.Queue.Enqueue(line);
            }
        }
    }
}
