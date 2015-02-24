using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// All supported BsonTypes in sort order
    /// </summary>
    public enum BsonType
    { 
        Null = 1,

        Int32 = 2,
        Int64 = 3,
        Double = 4,

        String = 5,

        Document = 6,
        Array = 7,

        Binary = 8,
        Guid = 9,

        Boolean = 10,
        DateTime = 11
    }
}
