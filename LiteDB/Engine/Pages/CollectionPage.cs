using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    internal class CollectionPage : BasePage
    {
        /// <summary>
        /// Get how many slots collection pages will have for free list page (data/index)
        /// </summary>
        public const int PAGE_FREE_LIST_SLOTS = 5;

        #region Buffer Field Positions

        private const int P_INDEXES = 96; // 96-8192
        private const int P_INDEXES_COUNT = PAGE_SIZE - P_INDEXES; // 8096

        #endregion

        /// <summary>
        /// Free data page linked-list (N lists for different range of FreeBlocks)
        /// </summary>
        public uint[] FreeDataPageID = new uint[PAGE_FREE_LIST_SLOTS];

        /// <summary>
        /// Free index page linked-list (N lists for different range of FreeBlocks)
        /// </summary>
        public uint[] FreeIndexPageID = new uint[PAGE_FREE_LIST_SLOTS];

        /// <summary>
        /// DateTime when collection was created
        /// </summary>
        public DateTime CreationTime { get; private set; }

        /// <summary>
        /// DateTime from last index counter
        /// </summary>
        public DateTime LastAnalyzed { get; set; }

        /// <summary>
        /// All indexes references for this collection
        /// </summary>
        private readonly Dictionary<string, CollectionIndex> _indexes = new Dictionary<string, CollectionIndex>();

        /// <summary>
        /// Check if indexes was changed
        /// </summary>
        private bool _isIndexesChanged = false;

        public CollectionPage(PageBuffer buffer, uint pageID)
            : base(buffer, pageID, PageType.Collection)
        {
            // initialize page version
            this.CreationTime = DateTime.Now;
            this.LastAnalyzed = DateTime.MinValue;

            for(var i = 0; i < PAGE_FREE_LIST_SLOTS; i++)
            {
                this.FreeDataPageID[i] = uint.MaxValue;
                this.FreeIndexPageID[i] = uint.MaxValue;
            }
        }

        public CollectionPage(PageBuffer buffer)
            : base(buffer)
        {
            if (this.PageType != PageType.Collection) throw new LiteException(0, $"Invalid CollectionPage buffer on {PageID}");

            // create new buffer area to store BsonDocument indexes
            var area = _buffer.Slice(PAGE_HEADER_SIZE, PAGE_SIZE - PAGE_HEADER_SIZE);

            using (var r = new BufferReader(new[] { area }, false))
            {
                // read position for FreeDataPage and FreeIndexPage
                for(var i = 0; i < PAGE_FREE_LIST_SLOTS; i++)
                {
                    this.FreeDataPageID[i] = r.ReadUInt32();
                    this.FreeIndexPageID[i] = r.ReadUInt32();
                }

                // read create/last analyzed (16 bytes)
                this.CreationTime = r.ReadDateTime();
                this.LastAnalyzed = r.ReadDateTime();

                // skip reserved area
                r.Skip(P_INDEXES - r.Position);

                // read indexes count (max 256 indexes per collection)
                var count = r.ReadByte(); // 1 byte

                for(var i = 0; i < count; i++)
                {
                    var index = new CollectionIndex(
                        slot: r.ReadByte(),
                        name: r.ReadCString(),
                        expr: r.ReadCString(),
                        unique: r.ReadBoolean())
                    { 
                        Head = r.ReadPageAddress(), // 5
                        Tail = r.ReadPageAddress(), // 5
                        MaxLevel = r.ReadByte(), // 1
                        KeyCount = r.ReadUInt32(), // 4
                        UniqueKeyCount = r.ReadUInt32() // 4
                    };

                    _indexes[index.Name] = index;
                }
            }
        }

        public override PageBuffer UpdateBuffer()
        {
            // if page was deleted, do not write in content area (must keep with 0 only)
            if (this.PageType == PageType.Empty) return base.UpdateBuffer();

            var area = _buffer.Slice(PAGE_HEADER_SIZE, PAGE_SIZE - PAGE_HEADER_SIZE);

            using (var w = new BufferWriter(area))
            {
                // read position for FreeDataPage and FreeIndexPage
                for (var i = 0; i < PAGE_FREE_LIST_SLOTS; i++)
                {
                    w.Write(this.FreeDataPageID[i]);
                    w.Write(this.FreeIndexPageID[i]);
                }

                // write creation/last analyzed (16 bytes)
                w.Write(this.CreationTime);
                w.Write(this.LastAnalyzed);

                // update collection only if needed
                if (_isIndexesChanged)
                {
                    // skip reserved area (indexes starts at position 96)
                    w.Skip(P_INDEXES - w.Position);

                    w.Write((byte)_indexes.Count); // 1 byte

                    foreach (var index in _indexes.Values)
                    {
                        w.Write(index.Slot);
                        w.WriteCString(index.Name);
                        w.WriteCString(index.Expression);
                        w.Write(index.Unique);
                        w.Write(index.Head);
                        w.Write(index.Tail);
                        w.Write(index.MaxLevel);
                        w.Write(index.KeyCount);
                        w.Write(index.UniqueKeyCount);
                    }

                    _isIndexesChanged = false;
                }
            }

            return base.UpdateBuffer();
        }

        /// <summary>
        /// Get PK index
        /// </summary>
        public CollectionIndex PK { get { return _indexes["_id"]; } }

        /// <summary>
        /// Get index from index name (index name is case sensitive) - returns null if not found
        /// </summary>
        public CollectionIndex GetCollectionIndex(string name)
        {
            if (_indexes.TryGetValue(name, out var index))
            {
                return index;
            }

            return null;
        }

        /// <summary>
        /// Get all indexes in this collection page
        /// </summary>
        public IEnumerable<CollectionIndex> GetCollectionIndexes()
        {
            return _indexes.Values;
        }

        /// <summary>
        /// Insert new index inside this collection page
        /// </summary>
        public CollectionIndex InsertCollectionIndex(string name, string expr, bool unique)
        {
            var totalLength = 1 +
                _indexes.Sum(x => CollectionIndex.GetLength(x.Value)) +
                CollectionIndex.GetLength(name, expr);

            // check if has space avaiable
            if (_indexes.Count == 255 || totalLength >= P_INDEXES_COUNT) throw new LiteException(0, $"This collection has no more space for new indexes");

            var slot = (byte)(_indexes.Count == 0 ? 0 : (_indexes.Max(x => x.Value.Slot) + 1));

            var index = new CollectionIndex(slot, name, expr, unique);
            
            _indexes[name] = index;

            _isIndexesChanged = true;

            this.IsDirty = true;

            return index;
        }

        /// <summary>
        /// Return index instance and mark as updatable
        /// </summary>
        public CollectionIndex UpdateCollectionIndex(string name)
        {
            _isIndexesChanged = true;

            this.IsDirty = true;

            return _indexes[name];
        }

        /// <summary>
        /// Remove index reference in this page
        /// </summary>
        public void DeleteCollectionIndex(string name)
        {
            _indexes.Remove(name);

            this.IsDirty = true;

            _isIndexesChanged = true;
        }

    }
}