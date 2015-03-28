
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
    /// Add an index in this entity property.
    /// </summary>
    public class BsonIndexAttribute : Attribute
    {
        public IndexOptions Options { get; private set; }

        public BsonIndexAttribute()
            : this (new IndexOptions())
        {
        }

        public BsonIndexAttribute(bool unique)
            : this(new IndexOptions { Unique = unique })
        {
        }

        public BsonIndexAttribute(IndexOptions options)
        {
            if (options == null) throw new ArgumentNullException("options");

            this.Options = options;
        }
    }
}