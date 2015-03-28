using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    /// <summary>
    /// Represents the collection page AND a collection item, because CollectionPage represent a Collection (1 page = 1 collection). All collections pages are linked with Prev/Next links
    /// </summary>
    internal class CollectionPage : BasePage
    {
        public const int MAX_COLLECTIONS = 256;

        public static Regex NamePattern = new Regex(@"^[\w-]{1,30}$");

        /// <summary>
        /// Name of collection
        /// </summary>
        public string CollectionName { get; set; }

        /// <summary>
        /// Get a reference for the free list data page - its private list per collection - each DataPage contains only data for 1 collection (no mixing)
        /// Must to be a Field to be used as parameter reference
        /// </summary>
        public uint FreeDataPageID;

        /// <summary>
        /// Get the number of documents inside this collection
        /// </summary>
        public uint DocumentCount { get; set; }

        /// <summary>
        /// Get all indexes from this collection - includes non-used indexes
        /// </summary>
        public CollectionIndex[] Indexes { get; set; }

        public CollectionPage()
            : base()
        {
            this.PageType = PageType.Collection;
            this.FreeDataPageID = uint.MaxValue;
            this.DocumentCount = 0;
            this.ItemCount = 1; // fixed for CollectionPage
            this.FreeBytes = 0; // no free bytes on collection-page - only one collection per page
            this.Indexes = new CollectionIndex[CollectionIndex.INDEX_PER_COLLECTION];

            for (var i = 0; i < Indexes.Length; i++)
            {
                this.Indexes[i] = new CollectionIndex() { Page = this, Slot = i };
            }
        }

        public override void ReadContent(BinaryReader reader)
        {
            this.CollectionName = reader.ReadString();
            this.FreeDataPageID = reader.ReadUInt32();
            this.DocumentCount = reader.ReadUInt32();

            foreach (var index in this.Indexes)
            {
                index.Field = reader.ReadString();
                index.HeadNode = reader.ReadPageAddress();
                index.TailNode = reader.ReadPageAddress();
                index.FreeIndexPageID = reader.ReadUInt32();
                index.Options.Unique = reader.ReadBoolean();
                index.Options.IgnoreCase = reader.ReadBoolean();
                index.Options.TrimWhitespace = reader.ReadBoolean();
                index.Options.EmptyStringToNull = reader.ReadBoolean();
                index.Options.RemoveAccents = reader.ReadBoolean();
            }
        }

        public override void WriteContent(BinaryWriter writer)
        {
            writer.Write(this.CollectionName);
            writer.Write(this.FreeDataPageID);
            writer.Write(this.DocumentCount);

            foreach (var index in this.Indexes)
            {
                writer.Write(index.Field);
                writer.Write(index.HeadNode);
                writer.Write(index.TailNode);
                writer.Write(index.FreeIndexPageID);
                writer.Write(index.Options.Unique);
                writer.Write(index.Options.IgnoreCase);
                writer.Write(index.Options.TrimWhitespace);
                writer.Write(index.Options.EmptyStringToNull);
                writer.Write(index.Options.RemoveAccents);
            }
        }

        #region Methods to work with index array

        /// <summary>
        /// Returns first free index slot to be used 
        /// </summary>
        public CollectionIndex GetFreeIndex()
        {
            for (byte i = 0; i < this.Indexes.Length; i++)
            {
                if (this.Indexes[i].IsEmpty) return this.Indexes[i];
            }

            throw LiteException.IndexLimitExceeded(this.CollectionName);
        }

        /// <summary>
        /// Get index from field name (index field name is case sensitive) - returns null if not found
        /// </summary>
        public CollectionIndex GetIndex(string field)
        {
            return this.Indexes.FirstOrDefault(x => x.Field == field);
        }

        /// <summary>
        /// Get primary key index (_id index)
        /// </summary>
        public CollectionIndex PK { get { return this.Indexes[0]; } }

        /// <summary>
        /// Returns all used indexes
        /// </summary>
        public IEnumerable<CollectionIndex> GetIndexes(bool includePK)
        {
            return this.Indexes.Where(x => x.IsEmpty == false && x.Slot >= (includePK ? 0 : 1));
        }

        #endregion
    }
}
