using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace LiteDB
{
    public enum BsonType
    { 
        Null,

        Array,
        Object, 

        Byte,
        ByteArray,
        Char,
        Boolean,
        String,

        Short,
        Int,
        Long,
        UShort,
        UInt,
        ULong,

        Float,
        Double,
        Decimal,

        DateTime,
        Guid
    }
}
