
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
    /// Indicate that property will be persistant in Bson serialization ()
    /// </summary>
    public class BsonPropertyAttribute : Attribute
    {
    }
}