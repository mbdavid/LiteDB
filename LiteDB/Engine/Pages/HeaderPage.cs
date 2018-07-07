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
        private const byte FILE_VERSION = 8;

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
        /// DateTime when database was changed (commited) [8 bytes]
        /// </summary>
        public DateTime LastCommit { get; set; }

        /// <summary>
        /// DateTime when database run checkpoint [8 bytes]
        /// </summary>
        public DateTime LastCheckpoint { get; set; }

        /// <summary>
        /// Transaction commit counter - this counter reset after last vaccum/shrink [4 bytes]
        /// </summary>
        public uint CommitCounter { get; set; }

        /// <summary>
        /// UserVersion int - for user get/set database version changes
        /// </summary>
        public int UserVersion { get; set; }

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
            this.LastCommit = DateTime.MinValue;
            this.LastCheckpoint = DateTime.MinValue;
            this.CommitCounter = 0;
            this.UserVersion = 0;
            this.Collections = new ConcurrentDictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Update header page with confirm data
        /// </summary>
        public void Update(Guid transactionID, uint freeEmptyPageID, TransactionPages transPages)
        {
            this.TransactionID = transactionID;
            this.FreeEmptyPageID = freeEmptyPageID;
            this.CommitCounter++;
            this.LastCommit = DateTime.Now;

            // remove/add collections based on transPages
            if (transPages != null)
            {
                foreach (var name in transPages.DeletedCollections)
                {
                    if (this.Collections.TryRemove(name, out var x) == false)
                    {
                        throw LiteException.CollectionNotFound(name);
                    }
                }

                foreach (var p in transPages.NewCollections)
                {
                    if (this.Collections.TryAdd(p.Key, p.Value) == false)
                    {
                        throw LiteException.CollectionAlreadyExist(p.Key);
                    }
                }

                this.ItemCount = this.ItemCount - transPages.DeletedCollections.Count + transPages.NewCollections.Count;
            }
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
            var start = reader.BaseStream.Position;

            var info = reader.ReadFixedString(HEADER_INFO.Length);
            var ver = reader.ReadByte();

            if (info != HEADER_INFO) throw LiteException.InvalidDatabase();
            if (ver != FILE_VERSION) throw LiteException.InvalidDatabaseVersion(ver);

            this.FreeEmptyPageID = reader.ReadUInt32();
            this.LastPageID = reader.ReadUInt32();
            this.CreationTime = reader.ReadDateTime(utcDate);
            this.LastCommit = reader.ReadDateTime(utcDate);
            this.LastCheckpoint = reader.ReadDateTime(utcDate);
            this.CommitCounter = reader.ReadUInt32();
            this.UserVersion = reader.ReadInt32();

            // read resered bytes
            var used = reader.BaseStream.Position - start;
            var reserved = (int)(HEADER_PAGE_FIXED_DATA_SIZE - used);

            reader.ReadBytes(reserved);

            DEBUG(reserved != 188, "For current version, reserved space must return 188 bytes");

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
            writer.Write(this.LastCommit);
            writer.Write(this.LastCheckpoint);
            writer.Write(this.CommitCounter);
            writer.Write(this.UserVersion);

            // write resered bytes
            var used = writer.BaseStream.Position - start;
            var reserved = HEADER_PAGE_FIXED_DATA_SIZE - used;

            DEBUG(reserved != 188, "For current version, reserved space must return 188 bytes");

            writer.Write(new byte[reserved]);

            foreach (var col in this.Collections)
            {
                writer.Write(col.Key);
                writer.Write(col.Value);
            }
        }

        public override BasePage Clone()
        {
            return new HeaderPage
            {
                // base page
                PageID = this.PageID,
                PrevPageID = this.PrevPageID,
                NextPageID = this.NextPageID,
                ItemCount = this.ItemCount,
                FreeBytes = this.FreeBytes,
                TransactionID = this.TransactionID,
                ColID = this.ColID,
                // header page
                FreeEmptyPageID = this.FreeEmptyPageID,
                LastPageID = this.LastPageID,
                CreationTime = this.CreationTime,
                LastCommit = this.LastCommit,
                LastCheckpoint = this.LastCheckpoint,
                CommitCounter = this.CommitCounter,
                UserVersion = this.UserVersion,
                Collections = new ConcurrentDictionary<string, uint>(this.Collections)
            };
        }

        #endregion
    }
}