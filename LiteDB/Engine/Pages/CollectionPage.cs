using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace LiteDB
{
    /// <summary>
    /// Represents the collection page AND a collection item, because CollectionPage represent a Collection (1 page = 1 collection). All collections pages are linked with Prev/Next links
    /// </summary>
    internal class CollectionPage : BasePage
    {
        public static Regex CollectionNamePattern = new Regex(@"^[\w]{1,60}$", RegexOptions.Compiled);

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
        /// Storage number sequence to be used in auto _id values
        /// </summary>
        public long Sequence { get; set; }

        /// <summary>
        /// DateTime when collection was created
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Get all indexes from this collection - includes non-used indexes
        /// </summary>
        private CollectionIndex[] _indexes;

        public CollectionPage(uint pageID)
            : base(pageID)
        {
            this.FreeDataPageID = uint.MaxValue;
            this.DocumentCount = 0;
            this.ItemCount = 1; // fixed for CollectionPage
            this.FreeBytes = 0; // no free bytes on collection-page - only one collection per page
            this.Sequence = 0;
            this.CreationTime = DateTime.Now;

            _indexes = new CollectionIndex[CollectionIndex.INDEX_PER_COLLECTION];

            for (var i = 0; i < _indexes.Length; i++)
            {
                _indexes[i] = new CollectionIndex() { Page = this, Slot = i };
            }
        }

        #region Read/Write pages

        protected override void ReadContent(ByteReader reader)
        {
            this.CollectionName = reader.ReadString();
            this.DocumentCount = reader.ReadInt64();
            this.FreeDataPageID = reader.ReadUInt32();
            this.Sequence = reader.ReadInt64();
            this.CreationTime = reader.ReadDateTime();

            foreach (var index in _indexes)
            {
                index.Name = reader.ReadString();
                index.Expression = reader.ReadString();

                index.Unique = reader.ReadBoolean();
                index.HeadNode = reader.ReadPageAddress();
                index.TailNode = reader.ReadPageAddress();
                index.FreeIndexPageID = reader.ReadUInt32();
                index.MaxLevel = reader.ReadByte();
            }
        }

        protected override void WriteContent(ByteWriter writer)
        {
            writer.Write(this.CollectionName);
            writer.Write(this.DocumentCount);
            writer.Write(this.FreeDataPageID);
            writer.Write(this.Sequence);
            writer.Write(this.CreationTime);

            foreach (var index in _indexes)
            {
                writer.Write(index.Name);
                writer.Write(index.Expression);
                writer.Write(index.Unique);
                writer.Write(index.HeadNode);
                writer.Write(index.TailNode);
                writer.Write(index.FreeIndexPageID);
                writer.Write(index.MaxLevel);
            }
        }

        #endregion

        #region Methods to work with index array

        /// <summary>
        /// Returns first free index slot to be used
        /// </summary>
        public CollectionIndex GetFreeIndex()
        {
            for (byte i = 0; i < _indexes.Length; i++)
            {
                if (_indexes[i].IsEmpty) return this._indexes[i];
            }

            throw LiteException.IndexLimitExceeded(this.CollectionName);
        }

        /// <summary>
        /// Get index from index name (index name is case sensitive) - returns null if not found
        /// </summary>
        public CollectionIndex GetIndex(string name)
        {
            return _indexes.FirstOrDefault(x => x.Name == name);
        }

        /// <summary>
        /// Get index from index slot
        /// </summary>
        public CollectionIndex GetIndex(int slot)
        {
            return _indexes[slot];
        }

        /// <summary>
        /// Get primary key index (_id index)
        /// </summary>
        public CollectionIndex PK { get { return this._indexes[0]; } }

        /// <summary>
        /// Returns all used indexes
        /// </summary>
        public IEnumerable<CollectionIndex> GetIndexes(bool includePK)
        {
            return _indexes.Where(x => x.IsEmpty == false && x.Slot >= (includePK ? 0 : 1));
        }

        #endregion
    }
}