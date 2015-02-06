using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Close : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Scan(@"close$").Length > 0;
        }

        public override void Execute(LiteShell shell, StringScanner s, Display display, InputCommand input)
        {
            if (shell.Database == null) throw new LiteException("No database");

            shell.Database.Dispose();

            shell.Database = null;
        }
    }
}
