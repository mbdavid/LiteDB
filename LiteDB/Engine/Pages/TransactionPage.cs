using System;

namespace LiteDB
{
    /// <summary>
    /// Represent a confirmed transaction page (used only in WAL-file). There is no PageID and no extra information
    /// </summary>
    internal class TransactionPage : BasePage
    {
        /// <summary>
        /// Page type = Transaction
        /// </summary>
        public override PageType PageType { get { return PageType.Transaction; } }

        public TransactionPage()
            : base(uint.MaxValue)
        {
        }

        public TransactionPage(Guid transactionID)
            : base(uint.MaxValue)
        {
            this.TransactionID = transactionID;
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