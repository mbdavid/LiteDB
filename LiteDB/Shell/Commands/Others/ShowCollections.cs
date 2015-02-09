using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    internal class ShowCollections : ILiteCommand
    {
        public bool IsCommand(StringScanner s)
        {
            return s.Match(@"show\scollections");
        }

        public BsonValue Execute(LiteDatabase db, StringScanner s)
        {
            var cols = db.GetCollectionNames().OrderBy(x => x).ToArray();

            return string.Join(Environment.NewLine, cols);
        }
    }
}
