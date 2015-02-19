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

        Binary,
        Boolean,
        String,
        Int32,
        Int64,
        Double,
        DateTime,
        Guid
    }
}
