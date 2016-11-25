using System;

namespace LiteDB
{
    /// <summary>
    /// Represent a extra data page that contains the object when is not possible store in DataPage (bigger then  PAGE_SIZE or on update has no more space on page)
    /// Can be used in sequence of pages to store big objects
    /// </summary>
    internal class ExtendPage : BasePage
    {
        /// <summary>
        /// Page type = Extend
        /// </summary>
        public override PageType PageType { get { return PageType.Extend; } }

        /// <summary>
        /// Represent the part or full of the object - if this page has NextPageID the object is bigger than this page
        /// </summary>
        public byte[] Data { get; set; }

        public ExtendPage(uint pageID)
            : base(pageID)
        {
            this.Data = new byte[0];
        }

        /// <summary>
        /// Update freebytes + items count
        /// </summary>
        public override void UpdateItemCount()
        {
            this.ItemCount = (ushort)Data.Length;
            this.FreeBytes = PAGE_AVAILABLE_BYTES - this.Data.Length; // not used on ExtendPage
        }

        #region Read/Write pages

        protected override void ReadContent(ByteReader reader)
        {
            this.Data = reader.ReadBytes(this.ItemCount);
        }

        protected override void WriteContent(ByteWriter writer)
        {
            writer.Write(this.Data);
        }

        #endregion
    }
}