using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Open : ConsoleCommand
    {
        public override bool IsCommand(StringScanner s)
        {
            return s.Scan(@"open\s+").Length > 0;
        }

        public override void Execute(LiteShell shell, StringScanner s, Display display, InputCommand input)
        {
            var filename = s.Scan(@".+");

            if (shell.Database != null)
            {
                shell.Database.Dispose();
            }

            shell.Database = new LiteDatabase(filename);
        }
    }
}
