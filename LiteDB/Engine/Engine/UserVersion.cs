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
        /// Get database user version. If set new value, requires no current transaction
        /// </summary>
        public int GetUserVersion()
        {
            return _header.UserVersion;
        }

        /// <summary>
        /// Set new database user version. Requires no transaction at this time
        /// </summary>
        public void SetUserVersion(int value)
        {
            if (value == _header.UserVersion || _shutdown) return;

            if (_locker.IsInTransaction) throw LiteException.InvalidTransactionState("UserVersion", TransactionState.Active);

            lock (_header)
            {
                // clone header to use in writer
                var header = _header.Clone();

                header.UserVersion = value;
                header.TransactionID = ObjectId.NewObjectId();
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