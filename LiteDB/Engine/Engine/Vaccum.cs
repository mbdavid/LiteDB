using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Read database searching for empty pages but non-linked in FreeListPage. Must run Checkpoint before and do lock reserved
        /// </summary>
        public int Vaccum()
        {
            // do not accept any command after shutdown database
            if (_shutdown) throw LiteException.DatabaseShutdown();

            _locker.EnterReserved(false);

            try
            {
                // do checkpoint with no lock (already locked)
                _wal.Checkpoint(true, _header, false);

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
            }
        }
    }
}