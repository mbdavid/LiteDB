using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Info : ICommand, IWebCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"db\.info$");
        }

        public void Execute(ref LiteEngine db, StringScanner s, Display display)
        {
            display.WriteBson(db.GetDatabaseInfo());
        }
    }
}
