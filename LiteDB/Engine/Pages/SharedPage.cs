using System;

namespace LiteDB
{
    /// <summary>
    /// Represent a shared page structure stored only in position 1. Is also used to confirm a commit a transaction in WAL file.
    /// Shared is a special page that all concurrent transaction use same instance and changed are always saved on all write-transactions
    /// </summary>
    internal class SharedPage : BasePage
    {
        /// <summary>
        /// Page type = Transaction
        /// </summary>
        public override PageType PageType { get { return PageType.Shared; } }

        /// <summary>
        /// Get/Set the pageID that start sequence with a complete empty pages (can be used as a new page)
        /// </summary>
        public uint FreeEmptyPageID;

        /// <summary>
        /// Last created page - Used when there is no free page inside file
        /// </summary>
        public uint LastPageID;

        public SharedPage()
            : base(1)
        {
            this.ItemCount = 1; // fixed for shared
            this.FreeBytes = 0; // no free bytes on shared page
            this.FreeEmptyPageID = uint.MaxValue;
            this.LastPageID = 1;
        }

        public SharedPage(Guid transactionID)
            : this()
        {
            this.TransactionID = transactionID;
        }

        #region Read/Write pages

        protected override void ReadContent(ByteReader reader)
        {
            this.FreeEmptyPageID = reader.ReadUInt32();
            this.LastPageID = reader.ReadUInt32();
        }

        protected override void WriteContent(ByteWriter writer)
        {
            writer.Write(this.FreeEmptyPageID);
            writer.Write(this.LastPageID);
        }

        #endregion
    }
}