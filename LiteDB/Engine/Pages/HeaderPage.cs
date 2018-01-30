using System;
using System.Collections.Generic;

namespace LiteDB
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
        /// Hash Password in PBKDF2 [20 bytes]
        /// </summary>
        public byte[] Password { get; set; }

        /// <summary>
        /// When using encryption, store salt for password [16 bytes]
        /// </summary>
        public byte[] Salt { get; set; }

        /// <summary>
        /// Get/Set the pageID that start sequence with a complete empty pages (can be used as a new page)
        /// </summary>
        public uint FreeEmptyPageID;

        /// <summary>
        /// Last created page - Used when there is no free page inside file
        /// </summary>
        public uint LastPageID;

        /// <summary>
        /// DateTime when database was created
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// DateTime when database was changed (commited)
        /// </summary>
        public DateTime LastCommit { get; set; }

        /// <summary>
        /// DateTime when database run checkpoint
        /// </summary>
        public DateTime LastCheckpoint { get; set; }

        /// <summary>
        /// DateTime when database run analyze
        /// </summary>
        public DateTime LastAnalyze { get; set; }

        /// <summary>
        /// DateTime when database run vaccum
        /// </summary>
        public DateTime LastVaccum { get; set; }

        /// <summary>
        /// DateTime when database run shrink
        /// </summary>
        public DateTime LastShrink { get; set; }

        /// <summary>
        /// Checkpoint counter - this counter reset after last vaccum/shrink
        /// </summary>
        public uint CheckpointCounter { get; set; }

        /// <summary>
        /// Transaction commit counter - this counter reset after last vaccum/shrink
        /// </summary>
        public uint CommitCount { get; set; }

        /// <summary>
        /// Store all collections used in current transaction. Useful only for checkpoint confim page
        /// </summary>
        public string[] TransactionCollections { get; set; }

        public HeaderPage()
            : base(0)
        {
            this.ItemCount = 1; // fixed for header
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
            this.CheckpointCounter = 0;
            this.CommitCount = 0;

            this.TransactionCollections = new string[0];
        }

        /// <summary>
        /// Create new copy of header page and keep non-changed values
        /// </summary>
        public HeaderPage Copy(Guid transactionID, uint freeEmptyPageID, string[] transactionCollections)
        {
            return new HeaderPage
            {
                TransactionID = transactionID,
                Password = this.Password,
                Salt = this.Salt,
                FreeEmptyPageID = freeEmptyPageID,
                LastPageID = this.LastPageID,
                CreationTime = this.CreationTime,
                LastCommit = DateTime.Now, // update last commit datetime
                LastCheckpoint = this.LastCheckpoint,
                LastAnalyze = this.LastAnalyze,
                LastVaccum = this.LastVaccum,
                LastShrink = this.LastShrink,
                CommitCount = this.CommitCount + 1, // increment counter
                CheckpointCounter = this.CheckpointCounter,
                TransactionCollections = transactionCollections
            };
        }

        #region Read/Write pages

        protected override void ReadContent(ByteReader reader)
        {
            var info = reader.ReadString(HEADER_INFO.Length);
            var ver = reader.ReadByte();

            if (info != HEADER_INFO) throw LiteException.InvalidDatabase();
            if (ver != FILE_VERSION) throw LiteException.InvalidDatabaseVersion(ver);

            this.Salt = reader.ReadBytes(this.Salt.Length);
            this.FreeEmptyPageID = reader.ReadUInt32();
            this.LastPageID = reader.ReadUInt32();
            this.CreationTime = reader.ReadDateTime();
            this.LastCommit = reader.ReadDateTime();
            this.LastCheckpoint = reader.ReadDateTime();
            this.LastAnalyze = reader.ReadDateTime();
            this.LastVaccum = reader.ReadDateTime();
            this.LastShrink = reader.ReadDateTime();
            this.CommitCount = reader.ReadUInt32();
            this.CheckpointCounter = reader.ReadUInt32();

            this.TransactionCollections = reader.ReadString().Split(',');
        }

        protected override void WriteContent(ByteWriter writer)
        {
            writer.Write(HEADER_INFO, HEADER_INFO.Length);
            writer.Write(FILE_VERSION);
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

            writer.Write(string.Join(",", this.TransactionCollections));
        }

        #endregion

    }
}