using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class Rollback : ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Scan(@"rollback(\s+trans)?$").Length > 0;
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            db.Rollback();

            return BsonValue.Null;
        }
    }
}
