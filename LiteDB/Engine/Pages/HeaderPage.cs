using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// Header page represent first page on datafile. Engine contains a single instance of HeaderPage and all changes
    /// must be syncornized (using lock).
    /// </summary>
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
        private const int P_LAST_CHECKPOINT = 76; // 76-83
        private const int P_USER_VERSION = 84; // 84-87
        // reserved 87-95 (9 bytes)
        private const int P_COLLECTIONS = 96; // 96-8128
        private const int P_COLLECTIONS_COUNT = PAGE_SIZE - P_COLLECTIONS - 1; // 8095 [-1 for CRC]

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
        public DateTime CreationTime { get; }

        /// <summary>
        /// DateTime last checkpoint command was executed [8 bytes]
        /// </summary>
        public DateTime LastCheckpoint { get; set; }

        /// <summary>
        /// UserVersion int - for user get/set database version changes
        /// </summary>
        public int UserVersion { get; set; }

        /// <summary>
        /// Global last used transaction ID (should be int to use in Interlocked.Increment)
        /// </summary>
        public int LastTransactionID = 0;

        /// <summary>
        /// All collections names/link ponter are stored inside this document
        /// </summary>
        private BsonDocument _collections;

        /// <summary>
        /// Check if collections was changed
        /// </summary>
        private bool _isCollectionsChanged = false;

        /// <summary>
        /// Internal (no-shared) buffer to store page content in save point
        /// </summary>
        private PageBuffer _savepoint = new PageBuffer(new byte[PAGE_SIZE], 0, 0);

        /// <summary>
        /// Create new Header Page
        /// </summary>
        public HeaderPage(PageBuffer buffer, uint pageID)
            : base(buffer, pageID, PageType.Header)
        {
            // initialize page version
            this.CreationTime = DateTime.UtcNow;
            this.LastCheckpoint = DateTime.MinValue;
            this.FreeEmptyPageID = uint.MaxValue;
            this.LastPageID = 0;
            this.UserVersion = 0;

            // writing direct into buffer in Ctor() because there is no change later (write once)
            _buffer.Write(HEADER_INFO, P_HEADER_INFO);
            _buffer[P_FILE_VERSION] = FILE_VERSION;

            _buffer.Write(this.CreationTime, P_CREATION_TIME);

            // initialize collections
            _collections = new BsonDocument();
        }

        /// <summary>
        /// Load HeaderPage from buffer page
        /// </summary>
        public HeaderPage(PageBuffer buffer)
            : base(buffer)
        {
            if (this.PageType != PageType.Header) throw new LiteException(0, $"Invalid HeaderPage buffer on {PageID}");

            this.CreationTime = buffer.ReadDateTime(P_CREATION_TIME);

            this.LoadPage(buffer);
        }

        /// <summary>
        /// Load page content based on page buffer
        /// </summary>
        private void LoadPage(PageBuffer buffer)
        {
            var info = buffer.ReadString(P_HEADER_INFO, HEADER_INFO.Length);
            var ver = buffer[P_FILE_VERSION];

            if (string.CompareOrdinal(info, HEADER_INFO) != 0) throw LiteException.InvalidDatabase();
            if (ver != FILE_VERSION) throw LiteException.InvalidDatabaseVersion(ver);

            this.FreeEmptyPageID = buffer.ReadUInt32(P_FREE_EMPTY_PAGE_ID);
            this.LastPageID = buffer.ReadUInt32(P_LAST_PAGE_ID);
            this.LastCheckpoint = buffer.ReadDateTime(P_LAST_CHECKPOINT);
            this.UserVersion = buffer.ReadInt32(P_USER_VERSION);

            // create new buffer area to store BsonDocument collections
            var area = buffer.Slice(P_COLLECTIONS, P_COLLECTIONS_COUNT);

            using (var r = new BufferReader(new[] { area }, false))
            {
                _collections = r.ReadDocument();
            }
        }

        public override PageBuffer GetBuffer(bool update)
        {
            if (update == false) return _buffer;

            _buffer.Write(this.FreeEmptyPageID, P_FREE_EMPTY_PAGE_ID);
            _buffer.Write(this.LastPageID, P_LAST_PAGE_ID);
            // CreationTime - never change - no need to override buffer
            _buffer.Write(this.LastCheckpoint, P_LAST_CHECKPOINT);
            _buffer.Write(this.UserVersion, P_USER_VERSION);

            // update collection only if needed
            if (_isCollectionsChanged)
            {
                var area = _buffer.Slice(P_COLLECTIONS, P_COLLECTIONS_COUNT);

                using (var w = new BufferWriter(area))
                {
                    w.WriteDocument(_collections);
                }

                _isCollectionsChanged = false;
            }

            return base.GetBuffer(update);
        }

        /// <summary>
        /// Create a save point before do any change on header page
        /// </summary>
        public void Savepoint()
        {
            Buffer.BlockCopy(_buffer.Array, _buffer.Offset, _savepoint.Array, _savepoint.Offset, PAGE_SIZE);
        }

        /// <summary>
        /// Restore savepoint content and override on page. Must run in lock(_header)
        /// </summary>
        public void RestoreSavepoint()
        {
            Buffer.BlockCopy(_savepoint.Array, _savepoint.Offset, _buffer.Array, _buffer.Offset, PAGE_SIZE);

            this.LoadPage(_buffer);
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
            foreach(var el in _collections.GetElements())
            {
                yield return new KeyValuePair<string, uint>(el.Key, (uint)el.Value.AsInt32);
            }
        }

        /// <summary>
        /// Insert new collection in header
        /// </summary>
        public void InsertCollection(string name, uint pageID)
        {
            _collections[name] = (int)pageID;

            _isCollectionsChanged = true;
        }

        /// <summary>
        /// Remove existing collection reference in header
        /// </summary>
        public void DeleteCollection(string name)
        {
            _collections.Remove(name);

            _isCollectionsChanged = true;
        }

        /// <summary>
        /// Get how many bytes are avaiable in collection to store new collections
        /// </summary>
        public int GetAvaiableCollectionSpace()
        {
            return P_COLLECTIONS_COUNT -
                _collections.GetBytesCount() -
                1 + // for int32 type (0x10)
                1 + // for new CString ('\0')
                4 + // for PageID (int32)
                8; // reserved
        }
    }
}