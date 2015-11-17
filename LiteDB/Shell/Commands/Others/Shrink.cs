using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB.Shell.Commands
{
    internal class Shrink : ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"shrink\s*").Length > 0;
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            return (Int32)db.Shrink();
        }
    }
}
