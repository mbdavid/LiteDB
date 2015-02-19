
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
    /// Set a name to this property in BsonDocument
    /// </summary>
    public class BsonPropertyAttribute : Attribute
    {
        public string Name { get; set; }

        public BsonPropertyAttribute(string name)
        {
            this.Name = name;
        }
    }
}