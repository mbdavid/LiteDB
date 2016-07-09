using System;

namespace LiteDB
{
    /// <summary>
    /// Add an index in this entity property.
    /// </summary>
    public class BsonIndexAttribute : Attribute
    {
        public IndexOptions Options { get; private set; }

        public BsonIndexAttribute()
            : this(new IndexOptions())
        {
        }

        public BsonIndexAttribute(bool unique)
            : this(new IndexOptions { Unique = unique })
        {
        }

        public BsonIndexAttribute(
            bool unique = false,
            bool ignoreCase = true,
            bool trimWhiteSpace = true,
            bool emptyStringToNull = true,
            bool removeAccents = true)
            : this(new IndexOptions
            {
                Unique = unique,
                IgnoreCase = ignoreCase,
                TrimWhitespace = trimWhiteSpace,
                EmptyStringToNull = emptyStringToNull,
                RemoveAccents = removeAccents
            })
        {
        }

        private BsonIndexAttribute(IndexOptions options)
        {
            this.Options = options;
        }
    }
}