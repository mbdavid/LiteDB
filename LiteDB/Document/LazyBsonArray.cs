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
    /// Lazy array supports on demand IEnumerable of BsonValues. It's usefull to interete only without store all objects
    /// </summary>
    public class LazyBsonArray : BsonArray
    {

    }
}
