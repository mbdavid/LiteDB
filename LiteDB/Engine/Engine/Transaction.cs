using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    public partial class LiteEngine : IDisposable
    {
        private Stack<LockControl> _transactions = new Stack<LockControl>();

        /// <summary>
        /// Starts a new transaction keeping all changed from now in memory only until Commit() be executed.
        /// Returns true if new lock request was made
        /// </summary>
        public bool BeginTrans()
        {
            // lock as reserved mode
            var locker = _locker.Reserved();

            _transactions.Push(locker);

            return locker.IsNewLock;
        }

        /// <summary>
        /// Persist in disk all changed from last BeginTrans()
        /// Returns true if real commit was done (false to nested commit only)
        /// </summary>
        public bool Commit()
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

        /// <summary>
        /// Discard all changes from last BeginTrans()
        /// </summary>
        public void Rollback()
        {
            _trans.Rollback();

            while(_transactions.Count > 0)
            {
                _transactions.Pop().Dispose();
            }
        }

        /// <summary>
        /// Encapsulate all write transaction operation
        /// </summary>
        private T Transaction<T>(string colName, bool addIfNotExists, Func<CollectionPage, T> action)
        {
            if(this.BeginTrans())
            {
                _trans.AvoidDirtyRead();
            }

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