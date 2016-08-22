using System;

namespace LiteDB
{
    /// <summary>
    /// A class that represent all index options used on a index creation
    /// </summary>
    public class IndexOptions : IEquatable<IndexOptions>
    {
        /// <summary>
        /// Unique keys?
        /// </summary>
        public bool Unique { get; set; }

        /// <summary>
        /// Ignore case? (convert all strings to lowercase)
        /// </summary>
        public bool IgnoreCase { get; set; }

        /// <summary>
        /// Remove all whitespace on start/end string?
        /// </summary>
        public bool TrimWhitespace { get; set; }

        /// <summary>
        /// Convert all empty string to null?
        /// </summary>
        public bool EmptyStringToNull { get; set; }

        /// <summary>
        /// Removing accents on string?
        /// </summary>
        public bool RemoveAccents { get; set; }

        public IndexOptions()
        {
            this.Unique = false;
            this.IgnoreCase = true;
            this.TrimWhitespace = true;
            this.EmptyStringToNull = true;
            this.RemoveAccents = true;
        }

        public bool Equals(IndexOptions other)
        {
            return this.Unique == other.Unique &&
                this.IgnoreCase == other.IgnoreCase &&
                this.TrimWhitespace == other.TrimWhitespace &&
                this.EmptyStringToNull == other.EmptyStringToNull &&
                this.RemoveAccents == other.RemoveAccents;
        }

        public IndexOptions Clone()
        {
            return new IndexOptions
            {
                Unique = this.Unique,
                IgnoreCase = this.IgnoreCase,
                TrimWhitespace = this.TrimWhitespace,
                EmptyStringToNull = this.EmptyStringToNull,
                RemoveAccents = this.RemoveAccents
            };
        }
    }
}