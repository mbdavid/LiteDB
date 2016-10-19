using System;

namespace LiteDB
{
    /// <summary>
    /// Add an index in this entity property.
    /// </summary>
    public class BsonIndexAttribute : Attribute
    {
        public bool Unique { get; private set; }

        public BsonIndexAttribute()
            : this(false)
        {
        }

        public BsonIndexAttribute(bool unique)
        {
            this.Unique = unique;
        }
    }
}