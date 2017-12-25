using System;

namespace LiteDB
{
    /// <summary>
    /// Represent a empty page (reused)
    /// </summary>
    internal class EmptyPage : BasePage
    {
        /// <summary>
        /// Page type = Empty
        /// </summary>
        public override PageType PageType { get { return PageType.Empty; } }

        public EmptyPage(uint pageID)
            : base(pageID)
        {
            this.ItemCount = 0;
            this.FreeBytes = PAGE_AVAILABLE_BYTES;
        }

        public EmptyPage(BasePage page)
            : base(page.PageID)
        {
        }

        #region Read/Write pages

        protected override void ReadContent(ByteReader reader)
        {
        }

        protected override void WriteContent(ByteWriter writer)
        {
        }

        #endregion
    }
}