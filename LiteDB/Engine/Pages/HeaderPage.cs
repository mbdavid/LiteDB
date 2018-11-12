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
        /// Header info the validate that datafile is a LiteDB file (6 bytes)
        /// </summary>
        private const string HEADER_INFO = "LiteDB";

        /// <summary>
        /// Datafile specification version
        /// </summary>
        public const byte FILE_VERSION = 8;

        #region Buffer Field Positions

        private const int P_HEADER_INFO = 32;  // 32-37
        private const int P_FILE_VERSION = 38;
        private const int P_FREE_EMPTY_PAGE_ID = 39; // 39-42
        private const int P_LAST_PAGE_ID = 43; // 43-46
        private const int P_CREATION_TIME = 47; // 47-54
        private const int P_USER_VERSION = 55; // 55-58

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
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// UserVersion int - for user get/set database version changes
        /// </summary>
        public int UserVersion { get; set; }

        /// <summary>
        /// Contains all collection in database using PageID to direct access
        /// </summary>
        public ConcurrentDictionary<string, uint> Collections { get; set; } = new ConcurrentDictionary<string, uint>(StringComparer.OrdinalIgnoreCase);

        public HeaderPage(PageBuffer buffer, uint pageID)
            : base(buffer, pageID, PageType.Header)
        {
            // initialize page version
            this.CreationTime = DateTime.Now;
            this.FreeEmptyPageID = uint.MaxValue;
            this.LastPageID = 0;
            this.UserVersion = 0;

            // writing direct into buffer in Ctor() because there is no change later (write once)
            Encoding.UTF8.GetBytes(HEADER_INFO, 0, 6, _buffer.Array, _buffer.Offset + P_HEADER_INFO);
            _buffer[P_FILE_VERSION] = FILE_VERSION;
            this.CreationTime.ToUniversalTime().Ticks.ToBytes(_buffer.Array, _buffer.Offset + P_CREATION_TIME);
        }

        public HeaderPage(PageBuffer buffer)
            : base(buffer)
        {
            DEBUG(this.PageType != PageType.Header, $"page {this.PageID} should be 'Header' but is {this.PageType}.");

            // header page use "header area" after 31 (2 and 3 blocks)
            var info = Encoding.UTF8.GetString(_buffer.Array, _buffer.Offset + P_HEADER_INFO, HEADER_INFO.Length);
            var ver = _buffer[P_FILE_VERSION];

            if (info != HEADER_INFO) throw LiteException.InvalidDatabase();
            if (ver != FILE_VERSION) throw LiteException.InvalidDatabaseVersion(ver);

            this.FreeEmptyPageID = BitConverter.ToUInt32(_buffer.Array, _buffer.Offset + P_FREE_EMPTY_PAGE_ID);
            this.LastPageID = BitConverter.ToUInt32(_buffer.Array, _buffer.Offset + P_LAST_PAGE_ID);
            this.CreationTime = new DateTime(BitConverter.ToInt64(_buffer.Array, _buffer.Offset + P_CREATION_TIME), DateTimeKind.Utc).ToLocalTime();
            this.UserVersion = BitConverter.ToInt32(_buffer.Array, _buffer.Offset + P_USER_VERSION);
        }

        public override void UpdateBuffer()
        {
            this.FreeEmptyPageID.ToBytes(_buffer.Array, _buffer.Offset + P_FREE_EMPTY_PAGE_ID);
            this.LastPageID.ToBytes(_buffer.Array, _buffer.Offset + P_LAST_PAGE_ID);
            this.UserVersion.ToBytes(_buffer.Array, _buffer.Offset + P_USER_VERSION);

            base.UpdateBuffer();
        }

        /// <summary>
        /// Update header page with new/drop collections. If any collection change, track this to update on UpdateBuffer()
        /// </summary>
        public void UpdateCollections(TransactionPages transPages)
        {
            // remove/add collections based on transPages
            if (transPages.DeletedCollection != null)
            {
                if (this.Collections.TryRemove(transPages.DeletedCollection, out var x) == false)
                {
                    throw LiteException.CollectionNotFound(transPages.DeletedCollection);
                }

                _isDirtyCollections = true;
            }

            // add all new collections
            foreach (var p in transPages.NewCollections)
            {
                if (this.Collections.TryAdd(p.Key, p.Value) == false)
                {
                    throw LiteException.CollectionAlreadyExist(p.Key);
                }

                _isDirtyCollections = true;
            }

            // update header collection count
            this.ItemsCount = (byte)(this.ItemsCount
                + transPages.NewCollections.Count
                - (transPages.DeletedCollection == null ? 0 : 1));
        }

        public HeaderPage Clone(PageBuffer clone)
        {
            this.UpdateBuffer();

            System.Buffer.BlockCopy(
                _buffer.Array,
                _buffer.Offset,
                clone.Array,
                clone.Offset,
                PAGE_SIZE);

            return new HeaderPage(clone);
        }
    }
}