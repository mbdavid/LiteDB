using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class Dump : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"dump\s*").Length > 0;
        }

        public void Execute(LiteEngine db, StringScanner s, Display display)
        {
            if (db == null) throw new LiteException("No database");

            if (s.HasTerminated || s.Match("mem$"))
            {
                display.WriteResult(DumpDatabase.Pages(db, s.Match("mem$")));
            }
            else
            {
                display.WriteResult(DumpDatabase.Index(db, s.Scan(@"\w+"), s.Scan(@"\s+\w+").Trim()));
            }
        }
    }
}
