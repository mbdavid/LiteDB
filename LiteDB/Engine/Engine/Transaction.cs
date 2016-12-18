using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LiteDB
{
    public partial class LiteEngine : IDisposable
    {
        private Stack<LockControl> _transactions = new Stack<LockControl>();

        /// <summary>
        /// Starts a new transaction keeping all changed from now in memory only until Commit() be executed.
        /// Lock thread in write mode to not accept other transaction
        /// </summary>
        public void BeginTrans()
        {
            // lock as reserved mode
            var locker = _locker.Reserved(_trans.AvoidDirtyRead);

            _transactions.Push(locker);
        }

        /// <summary>
        /// Persist in disk all changed from last BeginTrans()
        /// Returns true if real commit was done (false to nested commit only)
        /// </summary>
        public bool Commit()
        {
            lock(_locker)
            {
                var commit = false;

                // only do "real commit" if is last transaction in stack or if autocommit = false
                if (_transactions.Count == 1)
                {
                    _trans.Commit();
                    commit = true;
                }

                // if contains transactions on stack, remove top and dispose (only last transaction will release lock)
                if (_transactions.Count > 0)
                {
                    _transactions.Pop().Dispose();
                }

                return commit;
            }
        }

        /// <summary>
        /// Discard all changes from last BeginTrans()
        /// </summary>
        public void Rollback()
        {
            lock(_locker)
            {
                _trans.Rollback();

                while (_transactions.Count > 0)
                {
                    _transactions.Pop().Dispose();
                }
            }
        }

        /// <summary>
        /// Encapsulate all write transaction operation
        /// </summary>
        private T Transaction<T>(string colName, bool addIfNotExists, Func<CollectionPage, T> action)
        {
            using (_locker.Write())
            {
                this.BeginTrans();

                try
                {
                    var col = this.GetCollectionPage(colName, addIfNotExists);

                    var result = action(col);

                    this.Commit();

                    return result;
                }
                catch (Exception ex)
                {
                    _log.Write(Logger.ERROR, ex.Message);

                    // if an error occurs during an operation, rollback must be called to avoid datafile inconsistent
                    this.Rollback();

                    throw;
                }
            }
        }
    }
}