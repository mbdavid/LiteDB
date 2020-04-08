using System;
using System.Collections.Generic;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    internal interface ILogReader
    {
        IEnumerable<PageBuffer> ReadLog();
    }
}