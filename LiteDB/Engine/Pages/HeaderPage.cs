using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    internal class HeaderPage : BasePage
    {
        /// <summary>
        /// Page type = Header
        /// </summary>
        public override PageType PageType { get { return PageType.Header; } }

        /// <summary>
        /// Header info the validate that datafile is a LiteDB file (27 bytes)
        /// </summary>
        private const string HEADER_INFO = "** This is a LiteDB file **";

        /// <summary>
        /// Datafile specification version
        /// </summary>
        public const byte FILE_VERSION = 8;

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
        /// DateTime when database run checkpoint [8 bytes]
        /// </summary>
        public DateTime LastCheckpoint { get; set; }

        /// <summary>
        /// UserVersion int - for user get/set database version changes
        /// </summary>
        public int UserVersion { get; set; }

        /// <summary>
        /// Get data strcuture file version. If not equals to FILE_VERSION const (8) must be upgrade 
        /// </summary>
        public byte FileVersion { get; set; }

        /// <summary>
        /// Contains all collection in database using PageID to direct access
        /// </summary>
        public ConcurrentDictionary<string, uint> Collections { get; set; }

        private HeaderPage()
        {
        }

        public HeaderPage(uint pageID)
            : base(pageID)
        {
            this.ItemCount = 0; // used to store collection names
            this.FreeBytes = 0; // no free bytes on header
            this.FreeEmptyPageID = uint.MaxValue;
            this.LastPageID = 0;
            this.CreationTime = DateTime.Now;
            this.LastCheckpoint = DateTime.MinValue;
            this.UserVersion = 0;
            this.FileVersion = FILE_VERSION;
            this.Collections = new ConcurrentDictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Update header page with new/drop collections
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
            }

            // add all new collections
            foreach (var p in transPages.NewCollections)
            {
                if (this.Collections.TryAdd(p.Key, p.Value) == false)
                {
                    throw LiteException.CollectionAlreadyExist(p.Key);
                }
            }

            // update header collection count
            this.ItemCount = this.ItemCount 
                + transPages.NewCollections.Count
                - (transPages.DeletedCollection == null ? 0 : 1);
        }

        /// <summary>
        /// Check if all new collection names fit on header page with all existing collection
        /// </summary>
        public void CheckCollectionsSize(string newCollection)
        {
            var sum =
                this.Collections.Sum(x => x.Key.Length + 8) +
                (newCollection.Length + 8);

            if (sum >= MAX_COLLECTIONS_NAME_SIZE)
            {
                throw LiteException.CollectionLimitExceeded(MAX_COLLECTIONS_NAME_SIZE);
            }
        }

        #region Read/Write pages

        protected override void ReadContent(BinaryReader reader, bool utcDate)
        {
            // this will check for v7 datafile structure
            if (this.TransactionID == V7_TRANSID)
            {
                // must stop read now because this page structure is not compatible with old v7
                this.FileVersion = 7;
                return;
            }

            var start = reader.BaseStream.Position;

            var info = reader.ReadFixedString(HEADER_INFO.Length); // 27
            var ver = reader.ReadByte(); // 1

            if (info != HEADER_INFO) throw LiteException.InvalidDatabase();
            if (ver != FILE_VERSION) throw LiteException.InvalidDatabaseVersion(ver);

            this.FreeEmptyPageID = reader.ReadUInt32(); // 4
            this.LastPageID = reader.ReadUInt32(); // 4
            this.CreationTime = reader.ReadDateTime(utcDate); // 8
            this.LastCheckpoint = reader.ReadDateTime(utcDate); // 8
            this.UserVersion = reader.ReadInt32(); // 4

            // read resered bytes (256 - 56 = 200 bytes)
            var used = reader.BaseStream.Position - start;
            var reserved = (int)(HEADER_PAGE_FIXED_DATA_SIZE - used);

            reader.ReadBytes(reserved);

            DEBUG(reserved != 200, "For current version, reserved space must return 200 bytes");

            for (var i = 0; i < this.ItemCount; i++)
            {
                this.Collections.TryAdd(reader.ReadString(), reader.ReadUInt32());
            }
        }

        protected override void WriteContent(BinaryWriter writer)
        {
            var start = writer.BaseStream.Position;

            writer.WriteFixedString(HEADER_INFO);
            writer.Write(FILE_VERSION);
            writer.Write(this.FreeEmptyPageID);
            writer.Write(this.LastPageID);
            writer.Write(this.CreationTime);
            writer.Write(this.LastCheckpoint);
            writer.Write(this.UserVersion);

            // write resered bytes
            var used = writer.BaseStream.Position - start;
            var reserved = HEADER_PAGE_FIXED_DATA_SIZE - used;

            DEBUG(reserved != 200, "For current version, reserved space must return 200 bytes");

            writer.Write(new byte[reserved]);

            foreach (var col in this.Collections)
            {
                writer.Write(col.Key);
                writer.Write(col.Value);
            }
        }

        public HeaderPage Clone()
        {
            return new HeaderPage
            {
                // base page
                PageID = this.PageID,
                PrevPageID = this.PrevPageID,
                NextPageID = this.NextPageID,
                ItemCount = this.ItemCount,
                FreeBytes = this.FreeBytes,
                ColID = this.ColID,
                TransactionID = this.TransactionID,
                IsConfirmed = this.IsConfirmed,
                // header page
                FreeEmptyPageID = this.FreeEmptyPageID,
                LastPageID = this.LastPageID,
                CreationTime = this.CreationTime,
                LastCheckpoint = this.LastCheckpoint,
                UserVersion = this.UserVersion,
                FileVersion = this.FileVersion,
                Collections = new ConcurrentDictionary<string, uint>(this.Collections)
            };
        }

        #endregion
    }
}