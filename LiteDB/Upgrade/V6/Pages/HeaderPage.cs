using System;
using System.Collections.Generic;

namespace LiteDB_V6
{
    internal class HeaderPage : BasePage
    {
        private const string HEADER_INFO = "** This is a LiteDB file **";
        private const byte FILE_VERSION = 6;

        public override PageType PageType { get { return PageType.Header; } }
        public ushort ChangeID { get; set; }
        public uint FreeEmptyPageID;
        public uint LastPageID { get; set; }
        public ushort DbVersion = 0;
        public byte[] Password = new byte[20];
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