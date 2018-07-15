using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Represents the collection page AND a collection item, because CollectionPage represent a Collection (1 page = 1 collection). All collections pages are linked with Prev/Next links
    /// </summary>
    internal class CollectionPage : BasePage
    {
        /// <summary>
        /// Define reserved bytes for data structure
        /// </summary>
        private const int INDEX_PAGE_FIXED_HEADER = 200;

        /// <summary>
        /// Max length of all indexes names (including string expressions)
        /// </summary>
        private const int MAX_INDEX_NAME_SIZE = PAGE_AVAILABLE_BYTES - INDEX_PAGE_FIXED_HEADER;

        /// <summary>
        /// Each index fixed size
        /// </summary>
        private const int FIXED_INDEX_SIZE = 4 + // Name (length)
                                             4 + // Expression (length)
                                             1 + // Unique
                                             6 + // HeadNode
                                             6 + // TailNode
                                             4 + // FreeIndexPageID
                                             1 + // MaxLevel
                                             4 + // KeyCount
                                             4;  // UniqueKeyCount

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
        /// DateTime when collection was created
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Get all indexes from this collection - includes non-used indexes
        /// </summary>
        private CollectionIndex[] _indexes;

        private CollectionPage()
        {
        }

        public CollectionPage(uint pageID)
            : base(pageID)
        {
            this.FreeDataPageID = uint.MaxValue;
            this.ItemCount = 1; // fixed for CollectionPage
            this.FreeBytes = 0; // no free bytes on collection-page - only one collection per page
            this.CreationTime = DateTime.Now;

            _indexes = new CollectionIndex[INDEX_PER_COLLECTION];

            for (var i = 0; i < _indexes.Length; i++)
            {
                _indexes[i] = new CollectionIndex { Page = this, Slot = i };
            }
        }

        #region Read/Write pages

        protected override void ReadContent(BinaryReader reader, bool utcDate)
        {
            var start = reader.BaseStream.Position;

            this.CollectionName = reader.ReadString();
            this.FreeDataPageID = reader.ReadUInt32();
            this.CreationTime = reader.ReadDateTime(utcDate);

            // keep 200 bytes in page before starts write indexes
            var skip = INDEX_PAGE_FIXED_HEADER - (reader.BaseStream.Position - start);

            reader.BaseStream.Seek(skip, SeekOrigin.Current);

            foreach (var index in _indexes)
            {
                index.Name = reader.ReadString();
                index.Expression = reader.ReadString();

                index.Unique = reader.ReadBoolean();
                index.HeadNode = reader.ReadPageAddress();
                index.TailNode = reader.ReadPageAddress();
                index.FreeIndexPageID = reader.ReadUInt32();
                index.MaxLevel = reader.ReadByte();
                index.KeyCount = reader.ReadUInt32();
                index.UniqueKeyCount = reader.ReadUInt32();
            }
        }

        protected override void WriteContent(BinaryWriter writer)
        {
            var start = writer.BaseStream.Position;

            writer.Write(this.CollectionName);
            writer.Write(this.FreeDataPageID);
            writer.Write(this.CreationTime);

            var skip = INDEX_PAGE_FIXED_HEADER - (writer.BaseStream.Position - start);

            DEBUG(skip < 0 || skip > INDEX_PAGE_FIXED_HEADER, "reserved area must be between 0 and 200");

            writer.BaseStream.Seek(skip, SeekOrigin.Current);

            foreach (var index in _indexes)
            {
                writer.Write(index.Name);
                writer.Write(index.Expression);
                writer.Write(index.Unique);
                writer.Write(index.HeadNode);
                writer.Write(index.TailNode);
                writer.Write(index.FreeIndexPageID);
                writer.Write(index.MaxLevel);
                writer.Write(index.KeyCount);
                writer.Write(index.UniqueKeyCount);
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

        /// <summary>
        /// Calculate if all indexes names and expressions fit on this page
        /// </summary>
        public void CalculateNameSize()
        {
            var sum = _indexes.Sum(x => x.Name.Length + x.Expression.Length + FIXED_INDEX_SIZE);

            if (sum > MAX_INDEX_NAME_SIZE) throw LiteException.IndexNameLimitExceeded(MAX_INDEX_NAME_SIZE);
        }

        #endregion
    }
}