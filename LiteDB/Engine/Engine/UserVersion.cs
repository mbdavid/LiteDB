using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Get/Set database user version. If set new value, requires no current transaction
        /// </summary>
        public int UserVersion
        {
            get
            {
                return _header.UserVersion;
            }
            set
            {
                if (value == _header.UserVersion || _shutdown) return;

                if (_locker.IsInTransaction) throw LiteException.InvalidTransactionState("UserVersion", TransactionState.Active);

                lock (_header)
                {
                    // clone header to use in writer
                    var header = _header.Clone();

                    header.UserVersion = value;
                    header.TransactionID = Guid.NewGuid();
                    header.IsConfirmed = true;
                    header.IsDirty = true;

                    _wal.WalFile.WritePages(new[] { header }, null);

                    // create fake transaction with no pages to update (only confirm page)
                    _wal.ConfirmTransaction(header.TransactionID, new PagePosition[0]);

                    // update header instance
                    _header.UserVersion = value;
                }
            }
        }
    }
}