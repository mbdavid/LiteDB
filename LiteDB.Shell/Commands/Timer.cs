using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Timer : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Match(@"timer$");
        }

        public override void Execute(LiteShell shell, StringScanner s, Display display, InputCommand input)
        {
            input.Timer.Start();
        }
    }
}
