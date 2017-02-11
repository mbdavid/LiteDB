using System;

namespace LiteDB_V6
{
    /// <summary>
    /// Represent a extra data page that contains the object when is not possible store in DataPage (bigger then  PAGE_SIZE or on update has no more space on page)
    /// Can be used in sequence of pages to store big objects
    /// </summary>
    internal class ExtendPage : BasePage
    {
        public override PageType PageType { get { return PageType.Extend; } }
        public Byte[] Data { get; set; }

        public ExtendPage(uint pageID)
            : base(pageID)
        {
            this.Data = new byte[0];
        }

        protected override void ReadContent(LiteDB.ByteReader reader)
        {
            this.Data = reader.ReadBytes(this.ItemCount);
        }
    }
}