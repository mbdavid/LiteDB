using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class Param : IShellCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"db\.param\s*").Length > 0;
        }

        public void Execute(LiteEngine db, StringScanner s, Display display)
        {
            var name = s.Scan(@"[\w-]+\s*").Trim();
            var value = new JsonReader().ReadValue(s);

            db.SetParameter(name, value.RawValue);

            display.WriteBson(db.GetDatabaseInfo());
        }
    }
}
