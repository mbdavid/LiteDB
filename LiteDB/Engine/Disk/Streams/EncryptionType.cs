using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    public enum EncryptionType : byte
    {
        None = 0,
        AesEcb = 1,
        AesXts = 2
    }
}
