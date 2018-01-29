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
        /// DateTime when database was created
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Get/Set the pageID that start sequence with a complete empty pages (can be used as a new page)
        /// </summary>
        public uint FreeEmptyPageID;

        /// <summary>
        /// Last created page - Used when there is no free page inside file
        /// </summary>
        public uint LastPageID;

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
            this.CreationTime = DateTime.Now;
            this.FreeEmptyPageID = uint.MaxValue;
            this.LastPageID = 1; // CollectionListPage
            this.TransactionCollections = new string[0];
        }

        /// <summary>
        /// Create new copy of header page and keep non-changed values
        /// </summary>
        public HeaderPage Copy(Guid transactionID, uint freeEmptyPageID, string[] transactionCollections)
        {
            return new HeaderPage
            {
                Password = this.Password,
                Salt = this.Salt,
                CreationTime = this.CreationTime,
                LastPageID = this.LastPageID,
                TransactionID = transactionID,
                FreeEmptyPageID = freeEmptyPageID,
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
            this.CreationTime = reader.ReadDateTime();
            this.FreeEmptyPageID = reader.ReadUInt32();
            this.LastPageID = reader.ReadUInt32();
            this.TransactionCollections = reader.ReadString().Split(',');
        }

        protected override void WriteContent(ByteWriter writer)
        {
            writer.Write(HEADER_INFO, HEADER_INFO.Length);
            writer.Write(FILE_VERSION);
            writer.Write(this.Salt);
            writer.Write(this.CreationTime);
            writer.Write(this.FreeEmptyPageID);
            writer.Write(this.LastPageID);
            writer.Write(string.Join(",", this.TransactionCollections));
        }

        #endregion

    }
}