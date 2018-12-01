using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    internal enum PageMode : byte
    {
        None = 0,
        Data = 1,
        Log = 2
    }
}
