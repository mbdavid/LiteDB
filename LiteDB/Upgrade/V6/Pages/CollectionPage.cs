using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LiteDB_V6
{
    /// <summary>
    /// Represents the collection page AND a collection item, because CollectionPage represent a Collection (1 page = 1 collection). All collections pages are linked with Prev/Next links
    /// </summary>
    internal class CollectionPage : BasePage
    {
        /// <summary>
        /// Page type = Collection
        /// </summary>
        public override PageType PageType { get { return PageType.Collection; } }

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
        public long DocumentCount { get; set; }

        /// <summary>
        /// Get all indexes from this collection - includes non-used indexes
        /// </summary>
        public CollectionIndex[] Indexes { get; set; }

        public CollectionPage(uint pageID)
            : base(pageID)
        {
            this.FreeDataPageID = uint.MaxValue;
            this.DocumentCount = 0;
            this.ItemCount = 1; // fixed for CollectionPage
            this.Indexes = new CollectionIndex[CollectionIndex.INDEX_PER_COLLECTION];

            for (var i = 0; i < Indexes.Length; i++)
            {
                this.Indexes[i] = new CollectionIndex() { Page = this, Slot = i };
            }
        }

        protected override void ReadContent(LiteDB.ByteReader reader)
        {
            this.CollectionName = reader.ReadString();
            this.FreeDataPageID = reader.ReadUInt32();
            var uintCount = reader.ReadUInt32(); // read as uint (4 bytes)

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

            // be compatible with v2_beta
            var longCount = reader.ReadInt64();
            this.DocumentCount = Math.Max(uintCount, longCount);

        }

        /// <summary>
        /// Get primary key index (_id index)
        /// </summary>
        public CollectionIndex PK { get { return this.Indexes[0]; } }
    }
}