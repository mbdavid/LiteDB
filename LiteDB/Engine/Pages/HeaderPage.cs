using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    internal class HeaderPage : BasePage
    {
        /// <summary>
        /// Header info the validate that datafile is a LiteDB file (27 bytes)
        /// </summary>
        private const string HEADER_INFO = "** This is a LiteDB file **";

        /// <summary>
        /// Datafile specification version
        /// This data still at same position as v4 (FILE_VERSION=7)
        /// </summary>
        public const byte FILE_VERSION = 8;

        #region Buffer Field Positions

        private const int P_HEADER_INFO = 32;  // 32-58
        private const int P_FILE_VERSION = 59;
        private const int P_FREE_EMPTY_PAGE_ID = 60; // 60-63
        private const int P_LAST_PAGE_ID = 64; // 64-67
        private const int P_CREATION_TIME = 68; // 68-75
        private const int P_USER_VERSION = 76; // 76-79
        // reserved 80-95
        private const int P_COLLECTIONS = 96; // 96-8128
        private const int P_COLLECTIONS_COUNT = PAGE_SIZE - P_COLLECTIONS - PAGE_BLOCK_SIZE; // 8064
        // reserved 32 bytes at end of header page (for encryption SALT)

        #endregion

        /// <summary>
        /// Get/Set the pageID that start sequence with a complete empty pages (can be used as a new page) [4 bytes]
        /// </summary>
        public uint FreeEmptyPageID;

        /// <summary>
        /// Last created page - Used when there is no free page inside file [4 bytes]
        /// </summary>
        public uint LastPageID;

        /// <summary>
        /// DateTime when database was created [8 bytes]
        /// </summary>
        public DateTime CreationTime { get; private set; }

        /// <summary>
        /// UserVersion int - for user get/set database version changes
        /// </summary>
        public int UserVersion { get; set; }

        /// <summary>
        /// All collections names/link ponter are stored inside this document
        /// </summary>
        private readonly BsonDocument _collections;

        /// <summary>
        /// Check if collections was changed
        /// </summary>
        private bool _isCollectionsChanged = false;

        public HeaderPage(PageBuffer buffer, uint pageID)
            : base(buffer, pageID, PageType.Header)
        {
            // initialize page version
            this.CreationTime = DateTime.UtcNow;
            this.FreeEmptyPageID = uint.MaxValue;
            this.LastPageID = 0;
            this.UserVersion = 0;

            // writing direct into buffer in Ctor() because there is no change later (write once)
            _buffer.Write(HEADER_INFO, P_HEADER_INFO, HEADER_INFO.Length);
            _buffer[P_FILE_VERSION] = FILE_VERSION;

            _buffer.Write(this.CreationTime, P_CREATION_TIME);

            // initialize collections
            _collections = new BsonDocument();
        }

        public HeaderPage(PageBuffer buffer)
            : base(buffer)
        {
            DEBUG(this.PageType != PageType.Header, $"page {this.PageID} should be 'Header' but is {this.PageType}.");

            var info = _buffer.ReadString(P_HEADER_INFO, HEADER_INFO.Length);
            var ver = _buffer[P_FILE_VERSION];

            if (string.CompareOrdinal(info, HEADER_INFO) != 0) throw LiteException.InvalidDatabase();
            if (ver != FILE_VERSION) throw LiteException.InvalidDatabaseVersion(ver);

            this.FreeEmptyPageID = _buffer.ReadUInt32(P_FREE_EMPTY_PAGE_ID);
            this.LastPageID = _buffer.ReadUInt32(P_LAST_PAGE_ID);
            this.CreationTime = _buffer.ReadDateTime(P_CREATION_TIME);
            this.UserVersion = _buffer.ReadInt32(P_USER_VERSION);

            // clear SALT area in buffer to work CRC
            _buffer.Array.Fill((byte)0, _buffer.Offset + P_ENCRYPTION_SALT, ENCRYPTION_SALT_SIZE);

            // create new buffer area to store BsonDocument collections
            var area = _buffer.Slice(P_COLLECTIONS, P_COLLECTIONS_COUNT);

            using (var r = new BufferReader(new[] { area }, false))
            {
                _collections = r.ReadDocument();
            }
        }

        public override PageBuffer UpdateBuffer()
        {
            _buffer.ReadUInt32(P_FREE_EMPTY_PAGE_ID);
            _buffer.ReadUInt32(P_LAST_PAGE_ID);
            _buffer.ReadInt32(P_USER_VERSION);

            // CreationTime - never change - no need to override buffer

            // update collection only if needed
            if (_isCollectionsChanged)
            {
                var area = _buffer.Slice(P_COLLECTIONS, P_COLLECTIONS_COUNT);

                using (var w = new BufferWriter(new[] { area }))
                {
                    w.WriteDocument(_collections);
                }
            }

            return base.UpdateBuffer();
        }

        /// <summary>
        /// Update header page with new/drop collections. If any collection change, track this to update on UpdateBuffer()
        /// </summary>
        public void UpdateCollections(TransactionPages transPages)
        {
            // remove/add collections based on transPages
            if (transPages.DeletedCollection != null)
            {
                _collections.Remove(transPages.DeletedCollection);

                _isCollectionsChanged = true;
            }

            // add all new collections
            foreach (var p in transPages.NewCollections)
            {
                _collections[p.Key] = (int)p.Value;

                _isCollectionsChanged = true;
            }
        }

        /// <summary>
        /// Get collection PageID - return uint.MaxValue if not exists
        /// </summary>
        public uint GetCollectionPageID(string collection)
        {
            if (_collections.TryGetValue(collection, out var pageID))
            {
                return (uint)pageID.AsInt32;
            }

            return uint.MaxValue;
        }

        /// <summary>
        /// Get all collections with pageID
        /// </summary>
        public IEnumerable<KeyValuePair<string, uint>> GetCollections()
        {
            foreach(var key in _collections.Keys)
            {
                var item = _collections[key];

                yield return new KeyValuePair<string, uint>(key, (uint)item.AsInt32);
            }
        }

        /// <summary>
        /// Get how many bytes are avaiable in collection to store new collections
        /// </summary>
        public int GetAvaiableCollectionSpace()
        {
            return P_COLLECTIONS_COUNT -
                _collections.GetBytesCount(true) -
                1 + // for int32 type (0x10)
                1 + // for new CString ('\0')
                4 + // for PageID (int32)
                8; // reserved
        }
    }
}