using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    internal class HeaderPage : BasePage
    {
        /// <summary>
        /// Header info the validate that datafile is a LiteDB file
        /// </summary>
        private const string HEADER_INFO = "** This is a LiteDB file **";

        /// <summary>
        /// Datafile specification version
        /// </summary>
        private const byte FILE_VERSION = 5;

        /// <summary>
        /// Get/Set the changeID of data. When a client read pages, all pages are in the same version. But when OpenTransaction, we need validade that current changeID is the sabe that we have in cache
        /// </summary>
        public ushort ChangeID { get; set; }

        /// <summary>
        /// Get/Set the pageID that start sequenece with a complete empty pages (can be used as a new page)
        /// </summary>
        public uint FreeEmptyPageID;

        /// <summary>
        /// Last created page - Used when there is no free page inside file
        /// </summary>
        public uint LastPageID { get; set; }

        /// <summary>
        /// Get/Set a user version of database file
        /// </summary>
        public int UserVersion { get; set; }

        /// <summary>
        /// Get/Set the first collection pageID (used as Field to be passed as reference)
        /// </summary>
        public uint FirstCollectionPageID;

        public HeaderPage()
            : base()
        {
            this.PageID = 0;
            this.PageType = PageType.Header;
            this.FreeEmptyPageID = uint.MaxValue;
            this.FirstCollectionPageID = uint.MaxValue;
            this.ChangeID = 0;
            this.LastPageID = 0;
            this.ItemCount = 1; // fixed for header
            this.FreeBytes = 0; // no free bytes on header
        }

        #region Read/Write pages

        public override void ReadHeader(BinaryReader reader)
        {
            this.ChangeID = reader.ReadUInt16();
            var info = reader.ReadString(HEADER_INFO.Length);
            var ver = reader.ReadByte();
            this.FreeEmptyPageID = reader.ReadUInt32();
            this.FirstCollectionPageID = reader.ReadUInt32();
            this.LastPageID = reader.ReadUInt32();
            this.UserVersion = reader.ReadInt32();

            if (info != HEADER_INFO) throw LiteException.InvalidDatabase(reader.BaseStream);
            if (ver != FILE_VERSION) throw LiteException.InvalidDatabaseVersion(reader.BaseStream, ver);
        }

        public override void WriteHeader(BinaryWriter writer)
        {
            writer.Write(this.ChangeID);
            writer.Write(HEADER_INFO, HEADER_INFO.Length);
            writer.Write(FILE_VERSION);
            writer.Write(this.FreeEmptyPageID);
            writer.Write(this.FirstCollectionPageID);
            writer.Write(this.LastPageID);
            writer.Write(this.UserVersion);
        }

        #endregion
    }
}
