using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Comment : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Match(@"--");
        }

        public override void Execute(LiteShell shell, StringScanner s, Display display, InputCommand input)
        {
        }
    }
}
