using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB.Shell.Commands
{
    internal class Dump : ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"dump\s*").Length > 0;
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            if (s.HasTerminated || s.Match(@"\d+"))
            {
                var start = s.Scan(@"\d*").Trim();
                var end = s.Scan(@"\s*\d*").Trim();

                return db.DumpPages(
                    start.Length == 0 ? 0 : Convert.ToUInt32(start),
                    end.Length == 0 ? uint.MaxValue : Convert.ToUInt32(end));
            }
            else
            {
                var col = s.Scan(@"[\w-]+");
                var field = s.Scan(@"\s+\w+").Trim();

                return db.DumpIndex(col, field);
            }
        }
    }
}
