
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
    /// Indicate that property will be used as BsonDocument Id
    /// </summary>
    public class BsonIdAttribute : Attribute
    {
        public bool AutoId { get; private set; }

        public BsonIdAttribute()
        {
            this.AutoId = true;
        }

        public BsonIdAttribute(bool autoId)
        {
            this.AutoId = autoId;
        }
    }
}