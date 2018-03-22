using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB
{
    internal class HeaderPage : BasePage
    {
        /// <summary>
        /// Represent maximum bytes that all parameters must store in header page
        /// </summary>
        public const ushort MAX_PARAMETERS_SIZE = 1000;

        /// <summary>
        /// Represent maximum bytes that all collections names can be used in collection list page (must fit inside a single header page)
        /// </summary>
        public const ushort MAX_COLLECTIONS_NAME_SIZE = PAGE_SIZE -
            PAGE_HEADER_SIZE -
            128 - // used in header page
            172 - // reserved
            MAX_PARAMETERS_SIZE;

        /// <summary>
        /// Page type = Header
        /// </summary>
        public override PageType PageType { get { return PageType.Header; } }

        /// <summary>
        /// Header info the validate that datafile is a LiteDB file [27 bytes]
        /// </summary>
        private const string HEADER_INFO = "** This is a LiteDB file **";

        /// <summary>
        /// Datafile specification version [1 byte]
        /// </summary>
        private const byte FILE_VERSION = 8;

        /// <summary>
        /// Hash Password in PBKDF2 [20 bytes]
        /// </summary>
        public byte[] Password { get; set; }

        /// <summary>
        /// When using encryption, store salt for password [16 bytes]
        /// </summary>
        public byte[] Salt { get; set; }

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
        /// DateTime when database run analyze [8 bytes]
        /// </summary>
        public DateTime LastAnalyze { get; set; }

        /// <summary>
        /// DateTime when database run vaccum [8 bytes]
        /// </summary>
        public DateTime LastVaccum { get; set; }

        /// <summary>
        /// DateTime when database run shrink [8 bytes]
        /// </summary>
        public DateTime LastShrink { get; set; }

        /// <summary>
        /// Transaction commit counter - this counter reset after last vaccum/shrink [4 bytes]
        /// </summary>
        public uint CommitCount { get; set; }

        /// <summary>
        /// Checkpoint counter - this counter reset after last vaccum/shrink [4 bytes]
        /// </summary>
        public uint CheckpointCounter { get; set; }

        /// <summary>
        /// Contains all collection in database using PageID to direct access
        /// </summary>
        public ConcurrentDictionary<string, uint> Collections { get; set; }

        /// <summary>
        /// Contains all database persisted parameters, like "userVersion", "autoIndex", "checkPoint", "maxMemoryCache"
        /// </summary>
        public ConcurrentDictionary<string, BsonValue> Parameters { get; set; }

        private HeaderPage()
        {
        }

        public HeaderPage(uint pageID)
            : base(pageID)
        {
            this.ItemCount = 0; // used to store collection names
            this.FreeBytes = 0; // no free bytes on header
            this.Password = new byte[20];
            this.Salt = new byte[16];
            this.FreeEmptyPageID = uint.MaxValue;
            this.LastPageID = 2; // LockPage
            this.CreationTime = DateTime.Now;
            this.LastCommit = DateTime.MinValue;
            this.LastCheckpoint = DateTime.MinValue;
            this.LastAnalyze = DateTime.MinValue;
            this.LastVaccum = DateTime.MinValue;
            this.LastShrink = DateTime.MinValue;
            this.CommitCount = 0;
            this.CheckpointCounter = 0;
            this.Collections = new ConcurrentDictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
            this.Parameters = new ConcurrentDictionary<string, BsonValue>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Update header page with confirm data
        /// </summary>
        public void Update(Guid transactionID, uint freeEmptyPageID, TransactionPages transPages)
        {
            this.TransactionID = transactionID;
            this.FreeEmptyPageID = freeEmptyPageID;
            this.CommitCount++;
            this.LastCommit = DateTime.Now;

            // remove/add collections based on transPages
            if (transPages != null)
            {
                foreach (var p in transPages.DeletedCollections)
                {
                    if (this.Collections.TryRemove(p.Key, out var x) == false)
                    {
                        throw LiteException.CollectionNotFound(p.Key);
                    }
                }

                foreach (var p in transPages.NewCollections)
                {
                    if (this.Collections.ContainsKey(p.Key))
                    {
                        throw LiteException.CollectionAlreadyExist(p.Key);
                    }
                    
                    this.Collections.TryAdd(p.Key, p.Value.PageID);
                }

                this.ItemCount = this.ItemCount - transPages.DeletedPages + transPages.NewCollections.Count;
            }
        }

        /// <summary>
        /// Check if all new collection names fit on header page with all existing collection
        /// </summary>
        public void CheckCollectionsSize(IEnumerable<string> names)
        {
            var sum =
                this.Collections.Sum(x => x.Key.Length + 8) +
                names.Sum(x => x.Length + 8);

            if (sum >= MAX_COLLECTIONS_NAME_SIZE)
            {
                throw LiteException.CollectionLimitExceeded(HeaderPage.MAX_COLLECTIONS_NAME_SIZE);
            }
        }

        #region Read/Write pages

        protected override void ReadContent(BinaryReader reader, bool utcDate)
        {
            var info = reader.ReadString();
            var ver = reader.ReadByte();

            if (info != HEADER_INFO) throw LiteException.InvalidDatabase();
            if (ver != FILE_VERSION) throw LiteException.InvalidDatabaseVersion(ver);

            this.Password = reader.ReadBytes(this.Password.Length);
            this.Salt = reader.ReadBytes(this.Salt.Length);
            this.FreeEmptyPageID = reader.ReadUInt32();
            this.LastPageID = reader.ReadUInt32();
            this.CreationTime = reader.ReadDateTime(utcDate);
            this.LastCommit = reader.ReadDateTime(utcDate);
            this.LastCheckpoint = reader.ReadDateTime(utcDate);
            this.LastAnalyze = reader.ReadDateTime(utcDate);
            this.LastVaccum = reader.ReadDateTime(utcDate);
            this.LastShrink = reader.ReadDateTime(utcDate);
            this.CommitCount = reader.ReadUInt32();
            this.CheckpointCounter = reader.ReadUInt32();

            var parameters = reader.ReadDocument(utcDate);

            this.Parameters = new ConcurrentDictionary<string, BsonValue>(parameters.RawValue as Dictionary<string, BsonValue>);

            for (var i = 0; i < this.ItemCount; i++)
            {
                this.Collections.TryAdd(reader.ReadString(), reader.ReadUInt32());
            }
        }

        protected override void WriteContent(BinaryWriter writer)
        {
            writer.Write(HEADER_INFO);
            writer.Write(FILE_VERSION);
            writer.Write(this.Password);
            writer.Write(this.Salt);
            writer.Write(this.FreeEmptyPageID);
            writer.Write(this.LastPageID);
            writer.Write(this.CreationTime);
            writer.Write(this.LastCommit);
            writer.Write(this.LastCheckpoint);
            writer.Write(this.LastAnalyze);
            writer.Write(this.LastVaccum);
            writer.Write(this.LastShrink);
            writer.Write(this.CommitCount);
            writer.Write(this.CheckpointCounter);

            var parameters = new BsonDocument(this.Parameters);

            new BsonWriter().WriteDocument(writer, parameters);

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
                // header page
                Password = this.Password,
                Salt = this.Salt,
                FreeEmptyPageID = this.FreeEmptyPageID,
                LastPageID = this.LastPageID,
                CreationTime = this.CreationTime,
                LastCommit = this.LastCommit,
                LastCheckpoint = this.LastCheckpoint,
                LastAnalyze = this.LastAnalyze,
                LastVaccum = this.LastVaccum,
                LastShrink = this.LastShrink,
                CommitCount = this.CommitCount,
                CheckpointCounter = this.CheckpointCounter,
                Parameters = new ConcurrentDictionary<string, BsonValue>(this.Parameters),
                Collections = new ConcurrentDictionary<string, uint>(this.Collections)
            };
        }

        #endregion
    }
}