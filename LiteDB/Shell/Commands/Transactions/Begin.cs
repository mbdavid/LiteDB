using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Begin : ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"begin(\s+trans)?$").Length > 0;
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            db.BeginTrans();

            return BsonValue.Null;
        }
    }
}
