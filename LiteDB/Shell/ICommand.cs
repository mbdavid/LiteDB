using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Shell
{
    internal interface ICommand
    {
        bool IsCommand(StringScanner s);

        IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine);
    }
}