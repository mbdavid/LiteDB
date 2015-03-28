using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Pretty : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Scan(@"pretty\s*").Length > 0;
        }

        public override void Execute(LiteShell shell, StringScanner s, Display display, InputCommand input)
        {
            display.Pretty = !(s.Scan(@"off\s*").Length > 0);
        }
    }
}
