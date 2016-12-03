using System;
using System.Collections.Generic;

namespace LiteDB_V6
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
        private const byte FILE_VERSION = 6;

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
        /// Database user version [2 bytes]
        /// </summary>
        public ushort DbVersion = 0;

        /// <summary>
        /// Password hash in SHA1 [20 bytes]
        /// </summary>
        public byte[] Password = new byte[20];

        /// <summary>
        /// Get a dictionary with all collection pages with pageID link
        /// </summary>
        public Dictionary<string, uint> CollectionPages { get; set; }

        public HeaderPage()
            : base(0)
        {
            this.FreeEmptyPageID = uint.MaxValue;
            this.ChangeID = 0;
            this.LastPageID = 0;
            this.ItemCount = 1; // fixed for header
            this.DbVersion = 0;
            this.Password = new byte[20];
            this.CollectionPages = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        }

        protected override void ReadContent(LiteDB.ByteReader reader)
        {
            var info = reader.ReadString(HEADER_INFO.Length);
            var ver = reader.ReadByte();

            this.ChangeID = reader.ReadUInt16();
            this.FreeEmptyPageID = reader.ReadUInt32();
            this.LastPageID = reader.ReadUInt32();
            this.DbVersion = reader.ReadUInt16();
            this.Password = reader.ReadBytes(this.Password.Length);

            // read page collections references (position on end of page)
            var cols = reader.ReadByte();
            for (var i = 0; i < cols; i++)
            {
                this.CollectionPages.Add(reader.ReadString(), reader.ReadUInt32());
            }
        }
    }
}