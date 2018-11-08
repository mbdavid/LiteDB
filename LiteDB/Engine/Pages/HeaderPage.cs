//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using static LiteDB.Constants;

//namespace LiteDB.Engine
//{
//    internal class HeaderPage : BasePage
//    {
//        /// <summary>
//        /// Header info the validate that datafile is a LiteDB file (6 bytes)
//        /// </summary>
//        private const string HEADER_INFO = "LiteDB";

//        /// <summary>
//        /// Datafile specification version
//        /// </summary>
//        public const byte FILE_VERSION = 8;

//        /// <summary>
//        /// Get/Set the pageID that start sequence with a complete empty pages (can be used as a new page) [4 bytes]
//        /// </summary>
//        public uint FreeEmptyPageID;

//        /// <summary>
//        /// Last created page - Used when there is no free page inside file [4 bytes]
//        /// </summary>
//        public uint LastPageID;

//        /// <summary>
//        /// DateTime when database was created [8 bytes]
//        /// </summary>
//        public DateTime CreationTime { get; set; }

//        /// <summary>
//        /// UserVersion int - for user get/set database version changes
//        /// </summary>
//        public int UserVersion { get; set; }

//        /// <summary>
//        /// Contains all collection in database using PageID to direct access
//        /// </summary>
//        public ConcurrentDictionary<string, uint> Collections { get; set; } = new ConcurrentDictionary<string, uint>(StringComparer.OrdinalIgnoreCase);

//        /// <summary>
//        /// Track if collections was changes (insert/delete/renamed)
//        /// </summary>
//        private bool _isDirtyCollections = false;

//        public HeaderPage(PageBuffer buffer, uint pageID)
//            : base (buffer, pageID, PageType.Header)
//        {
//            // initialize page version
//            this.CreationTime = DateTime.Now;
//            this.FreeEmptyPageID = uint.MaxValue;
//            this.LastPageID = 0;
//            this.UserVersion = 0;

//            // writing direct into buffer in Ctor() because there is no change later (write once)
//            Encoding.UTF8.GetBytes(HEADER_INFO, 0, 6, _buffer.Array, _buffer.Offset + 32); // 32-37
//            _buffer[38] = FILE_VERSION;
//            this.CreationTime.ToUniversalTime().Ticks.ToBytes(_buffer.Array, _buffer.Offset + 39); // 39-46
//        }

//        public HeaderPage(PageBuffer buffer)
//            : base(buffer)
//        {
//            DEBUG(this.PageType != PageType.Header, $"page {this.PageID} should be 'Header' but is {this.PageType}.");

//            // header page use "header area" after 31 (second block)
//            var info = Encoding.UTF8.GetString(_buffer.Array, _buffer.Offset + 32, 6); // 32-37
//            var ver = _buffer[38]; // 38

//            if (info != HEADER_INFO) throw LiteException.InvalidDatabase();
//            if (ver != FILE_VERSION) throw LiteException.InvalidDatabaseVersion(ver);

//            this.CreationTime = new DateTime(BitConverter.ToInt64(_buffer.Array, _buffer.Offset + 39), DateTimeKind.Utc).ToLocalTime(); // 39-46
//            this.FreeEmptyPageID = BitConverter.ToUInt32(_buffer.Array, _buffer.Offset + 37); // 46-49
//            this.LastPageID = BitConverter.ToUInt32(_buffer.Array, _buffer.Offset + 50); // 50-53
//            this.UserVersion = BitConverter.ToInt32(_buffer.Array, _buffer.Offset + 54); // 54-57

//            // for header page, I will implement my own page read/write operations because contains
//            // I will track if this collection changed and will override all items
//            // let's start collection pages in
//            var position = 200;

//            for (var i = 0; i < this.ItemsCount; i++)
//            {
//                // read collection name
//                var name = buffer.Array.ReadCString(_buffer.Offset + position, out var length);

//                position += length;

//                // read collection pageID
//                var pageID = BitConverter.ToUInt32(_buffer.Array, _buffer.Offset + position);

//                position += 4;

//                // add into my local collection
//                this.Collections.TryAdd(name, pageID);
//            }
//        }

//        public override void UpdateBuffer()
//        {
//            base.UpdateBuffer();

//            this.FreeEmptyPageID.ToBytes(_buffer.Array, _buffer.Offset + 46); // 46-49
//            this.LastPageID.ToBytes(_buffer.Array, _buffer.Offset + 50); // 50-53
//            this.UserVersion.ToBytes(_buffer.Array, _buffer.Offset + 54); // 54-57

//            if (_isDirtyCollections)
//            {
//                var position = 200;

//                foreach(var col in this.Collections)
//                {
//                    var length = Encoding.UTF8.GetByteCount(col.Key);

//                    // write collection name
//                    Encoding.UTF8.GetBytes(col.Key, 0, col.Key.Length, _buffer.Array, _buffer.Offset + position);

//                    _buffer[length + position + 1] = 0x00;

//                    position += length + 1;

//                    // write collection root
//                    col.Value.ToBytes(_buffer.Array, _buffer.Offset + position);

//                    position += 4;

//                }

//                _isDirtyCollections = false;
//            }
//        }

//        /// <summary>
//        /// Update header page with new/drop collections. If any collection change, track this to update on UpdateBuffer()
//        /// </summary>
//        public void UpdateCollections(TransactionPages transPages)
//        {
//            // remove/add collections based on transPages
//            if (transPages.DeletedCollection != null)
//            {
//                if (this.Collections.TryRemove(transPages.DeletedCollection, out var x) == false)
//                {
//                    throw LiteException.CollectionNotFound(transPages.DeletedCollection);
//                }

//                _isDirtyCollections = true;
//            }

//            // add all new collections
//            foreach (var p in transPages.NewCollections)
//            {
//                if (this.Collections.TryAdd(p.Key, p.Value) == false)
//                {
//                    throw LiteException.CollectionAlreadyExist(p.Key);
//                }

//                _isDirtyCollections = true;
//            }

//            // update header collection count
//            this.ItemsCount = (byte)(this.ItemsCount 
//                + transPages.NewCollections.Count
//                - (transPages.DeletedCollection == null ? 0 : 1));
//        }

//        /// <summary>
//        /// Check if all new collection names fit on header page with all existing collection
//        /// </summary>
//        public void CheckCollectionsSize(string newCollection)
//        {
//            var sum =
//                this.Collections.Sum(x => x.Key.Length + 8) +
//                (newCollection.Length + 8);

//            if (sum >= MAX_COLLECTIONS_NAME_SIZE)
//            {
//                throw LiteException.CollectionLimitExceeded(MAX_COLLECTIONS_NAME_SIZE);
//            }
//        }

//        public HeaderPage Clone(PageBuffer clone)
//        {
//            this.UpdateBuffer();

//            System.Buffer.BlockCopy(
//                _buffer.Array,
//                _buffer.Offset, 
//                clone.Array, 
//                clone.Offset, 
//                PAGE_SIZE);

//            return new HeaderPage(clone);
//        }
//    }
//}