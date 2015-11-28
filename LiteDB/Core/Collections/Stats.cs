using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Get collection stats 
        /// </summary>
        public BsonDocument Stats()
        {
            return _engine.Stats(_name).AsDocument;
        }
    }
}
