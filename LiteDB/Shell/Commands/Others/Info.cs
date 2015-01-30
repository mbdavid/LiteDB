using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class Info : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"db\.info$");
        }

        public void Execute(LiteEngine db, StringScanner s, Display display)
        {
            if (db == null) throw new LiteException("No database");

            display.WriteBson(db.GetDatabaseInfo());
        }
    }
}
