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
        private byte[] _data = new byte[0];

        public ExtendPage(uint pageID)
            : base(pageID)
        {
        }

        /// <summary>
        /// Set slice of byte array source  into this page area
        /// </summary>
        public void SetData(byte[] data, int offset, int length)
        {
            this.ItemCount = length;
            this.FreeBytes = PAGE_AVAILABLE_BYTES - length; // not used on ExtendPage

            _data = new byte[length];

            Buffer.BlockCopy(data, offset, _data, 0, length);
        }

        /// <summary>
        /// Get internal page byte array data
        /// </summary>
        public byte[] GetData()
        {
            return _data;
        }

        #region Read/Write pages

        protected override void ReadContent(ByteReader reader)
        {
            _data = reader.ReadBytes(this.ItemCount);
        }

        protected override void WriteContent(ByteWriter writer)
        {
            writer.Write(_data);
        }

        #endregion
    }
}