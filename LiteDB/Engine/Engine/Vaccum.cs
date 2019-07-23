using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Read database searching for empty pages but non-linked in FreeListPage. Must run Checkpoint before and do lock reserved
        /// Runs only in with no current transaction
        /// Returns how many pages was mark as deleted page
        /// </summary>
        public int Vaccum()
        {
            throw new NotImplementedException(); /*
            // shrink can only run with no transaction
            if (_locker.IsInTransaction) throw LiteException.InvalidTransactionState("Vaccum", TransactionState.Active);

            _locker.EnterReserved(false);

            try
            {
                // do checkpoint with no lock (already locked)
                _wal.Checkpoint(_header, false);

                _log.Info("vaccum datafile");

                return this.AutoTransaction(transaction =>
                {
                    var snapshot = transaction.CreateSnapshot(LockMode.Write, "_vaccum", false);
                    var count = 0;

                    foreach (var pageID in _dataFile.ReadZeroPages())
                    {
                        snapshot.DeletePage(pageID);

                        count++;
                    }

                    return count;
                });
            }
            finally
            {
                _locker.ExitReserved(false);
            }*/
        }
    }
}