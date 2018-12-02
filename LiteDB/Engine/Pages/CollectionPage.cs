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
        #region Buffer Field Positions

        private const int P_INDEXES = 96; // 96-8192
        private const int P_INDEXES_COUNT = PAGE_SIZE - P_INDEXES; // 8096

        #endregion

        /// <summary>
        /// Free data page linked-list (5 lists for different range of FreeBlocks)
        /// </summary>
        public uint[] FreeDataPageID = new uint[5];

        /// <summary>
        /// Free index page linked-list (5 lists for different range of FreeBlocks)
        /// </summary>
        public uint[] FreeIndexPageID = new uint[5];

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

            for(var i = 0; i < 5; i++)
            {
                this.FreeDataPageID[i] = uint.MaxValue;
                this.FreeIndexPageID[i] = uint.MaxValue;
            }
        }

        public CollectionPage(PageBuffer buffer)
            : base(buffer)
        {
            ENSURE(this.PageType == PageType.Collection, "invalid collection page buffer");

            // create new buffer area to store BsonDocument indexes
            var area = _buffer.Slice(PAGE_HEADER_SIZE, PAGE_SIZE - PAGE_HEADER_SIZE);

            using (var r = new BufferReader(new[] { area }, false))
            {
                // read 5 position of FreeDataPage (20 bytes)
                this.FreeDataPageID[0] = r.ReadUInt32();
                this.FreeDataPageID[1] = r.ReadUInt32();
                this.FreeDataPageID[2] = r.ReadUInt32();
                this.FreeDataPageID[3] = r.ReadUInt32();
                this.FreeDataPageID[4] = r.ReadUInt32();

                // read 5 position of FreeIndexPage (20 bytes)
                this.FreeIndexPageID[0] = r.ReadUInt32();
                this.FreeIndexPageID[1] = r.ReadUInt32();
                this.FreeIndexPageID[2] = r.ReadUInt32();
                this.FreeIndexPageID[3] = r.ReadUInt32();
                this.FreeIndexPageID[4] = r.ReadUInt32();

                // read create/last analyzed (16 bytes)
                this.CreationTime = r.ReadDateTime();
                this.LastAnalyzed = r.ReadDateTime();

                // read indexes count (max 256 indexes per collection)
                var count = r.ReadByte(); // 1 byte

                for(var i = 0; i < count; i++)
                {
                    var index = new CollectionIndex(
                        name: r.ReadCString(),
                        expr: r.ReadCString(),
                        unique: r.ReadBoolean(),
                        head: r.ReadPageAddress(),
                        tail: r.ReadPageAddress())
                    {
                        MaxLevel = r.ReadByte(), // 1
                        KeyCount = r.ReadUInt32(), // 4
                        UniqueKeyCount = r.ReadUInt32() // 4
                    };

                    _indexes[index.Name] = index;
                }
            }
        }

        public override PageBuffer GetBuffer(bool update)
        {
            if (update == false) return _buffer;

            var area = _buffer.Slice(PAGE_HEADER_SIZE, PAGE_SIZE - PAGE_HEADER_SIZE);

            using (var w = new BufferWriter(area))
            {
                // write 5 position of FreeDataPage (20 bytes)
                w.Write(this.FreeDataPageID[0]);
                w.Write(this.FreeDataPageID[1]);
                w.Write(this.FreeDataPageID[2]);
                w.Write(this.FreeDataPageID[3]);
                w.Write(this.FreeDataPageID[4]);

                // write 5 position of FreeIndexPage (20 bytes)
                w.Write(this.FreeIndexPageID[0]);
                w.Write(this.FreeIndexPageID[1]);
                w.Write(this.FreeIndexPageID[2]);
                w.Write(this.FreeIndexPageID[3]);
                w.Write(this.FreeIndexPageID[4]);

                // write creation/last analyzed (16 bytes)
                w.Write(this.CreationTime);
                w.Write(this.LastAnalyzed);

                // update collection only if needed
                if (_isIndexesChanged)
                {
                    w.Write((byte)_indexes.Count); // 1 byte

                    foreach (var index in _indexes.Values)
                    {
                        w.WriteCString(index.Name);
                        w.WriteCString(index.Expression);
                        w.Write(index.Unique);
                        w.Write(index.Head);
                        w.Write(index.Tail);
                        w.Write(index.MaxLevel);
                        w.Write(index.KeyCount);
                        w.Write(index.UniqueKeyCount);
                    }
                }
            }

            return base.GetBuffer(update);
        }

        /// <summary>
        /// Get PK index
        /// </summary>
        public CollectionIndex PK { get { return _indexes["_id"]; } }

        /// <summary>
        /// Get index from index name (index name is case sensitive) - returns null if not found
        /// </summary>
        public CollectionIndex GetIndex(string name)
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
        public IEnumerable<CollectionIndex> GetAllIndexes()
        {
            return _indexes.Values;
        }

        /// <summary>
        /// Insert new index inside this collection page
        /// </summary>
        public CollectionIndex InsertIndex(string name, string expr, bool unique, PageAddress head, PageAddress tail)
        {
            //TODO: test page space for this new index

            var index = new CollectionIndex(name, expr, unique, head, tail);
            
            _indexes[name] = index;

            _isIndexesChanged = true;

            this.IsDirty = true;

            return index;
        }

        /// <summary>
        /// Return index instance and mark as updatable
        /// </summary>
        public CollectionIndex UpdateIndex(string name)
        {
            _isIndexesChanged = true;

            this.IsDirty = true;

            return _indexes[name];
        }

        /// <summary>
        /// Remove index reference in this page
        /// </summary>
        public void DeleteIndex(string name)
        {
            _indexes.Remove(name);

            this.IsDirty = true;

            _isIndexesChanged = true;
        }

        /// <summary>
        /// Get how many bytes are avaiable in page to store new indexes
        /// </summary>
        public int GetAvaiableIndexSpace()
        {
            throw new NotImplementedException();
        }
    }
}