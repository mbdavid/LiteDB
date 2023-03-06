using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
#if NETFRAMEWORK
using System.Security.AccessControl;
using System.Security.Principal;
#endif

namespace LiteDB
{
    public class SharedEngine : ILiteEngine
    {
        private readonly EngineSettings _settings;
        private readonly Mutex _mutex;
        private LiteEngine _engine;
        private bool _transactionRunning = false;

        public SharedEngine(EngineSettings settings)
        {
            _settings = settings;

            var name = Path.GetFullPath(settings.Filename).ToLower().Sha1();

            try
            {
#if NETFRAMEWORK
                var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                           MutexRights.FullControl, AccessControlType.Allow);

                var securitySettings = new MutexSecurity();
                securitySettings.AddAccessRule(allowEveryoneRule);

                _mutex = new Mutex(false, "Global\\" + name + ".Mutex", out _, securitySettings);
#else
                _mutex = new Mutex(false, "Global\\" + name + ".Mutex");
#endif
            }
            catch (NotSupportedException ex)
            {
                throw new PlatformNotSupportedException("Shared mode is not supported in platforms that do not implement named mutex.", ex);
            }
        }

        /// <summary>
        /// Open database in safe mode
        /// </summary>
        private void OpenDatabase()
        {
            try
            {
                // Acquire mutex for every call to open DB.
                _mutex.WaitOne();
            }
            catch (AbandonedMutexException) { }

            // Don't create a new engine while a transaction is running.
            if (!_transactionRunning && _engine == null)
            {
                try
                {
                    _engine = new LiteEngine(_settings);
                }
                catch
                {
                    _mutex.ReleaseMutex();
                    throw;
                }
            }
        }

        /// <summary>
        /// Dequeue stack and dispose database on empty stack
        /// </summary>
        private void CloseDatabase()
        {
            // Don't dispose the engine while a transaction is running.
            if (!this._transactionRunning && _engine != null)
            {
                // If no transaction pending, dispose the engine.
                _engine.Dispose();
                _engine = null;
            }

            // Release Mutex on every call to close DB.
            _mutex.ReleaseMutex();
        }

        #region Transaction Operations

        public bool BeginTrans()
        {
            this.OpenDatabase();

            try
            {
                this._transactionRunning = _engine.BeginTrans();

                return this._transactionRunning;
            }
            catch
            {
                this.CloseDatabase();
                throw;
            }
        }

        public bool Commit()
        {
            if (_engine == null) return false;

            try
            {
                return _engine.Commit();
            }
            finally
            {
                this._transactionRunning = false;
                this.CloseDatabase();
            }
        }

        public bool Rollback()
        {
            if (_engine == null) return false;

            try
            {
                return _engine.Rollback();
            }
            finally
            {
                this._transactionRunning = false;
                this.CloseDatabase();
            }
        }

        #endregion

        #region Read Operation

        public IBsonDataReader Query(string collection, Query query)
        {
            this.OpenDatabase();

            var reader = _engine.Query(collection, query);

            return new SharedDataReader(reader, () => this.CloseDatabase());
        }

        public BsonValue Pragma(string name)
        {
            this.OpenDatabase();

            try
            {
                return _engine.Pragma(name);
            }
            finally
            {
                this.CloseDatabase();
            }
        }

        public bool Pragma(string name, BsonValue value)
        {
            this.OpenDatabase();

            try
            {
                return _engine.Pragma(name, value);
            }
            finally
            {
                this.CloseDatabase();
            }
        }

        #endregion

        #region Write Operations

        public int Checkpoint()
        {
            this.OpenDatabase();

            try
            {
                return _engine.Checkpoint();
            }
            finally
            {
                this.CloseDatabase();
            }
        }

        public long Rebuild(RebuildOptions options)
        {
            this.OpenDatabase();

            try
            {
                return _engine.Rebuild(options);
            }
            finally
            {
                this.CloseDatabase();
            }
        }

        public int Insert(string collection, IEnumerable<BsonDocument> docs, BsonAutoId autoId)
        {
            this.OpenDatabase();

            try
            {
                return _engine.Insert(collection, docs, autoId);
            }
            finally
            {
                this.CloseDatabase();
            }
        }

        public int Update(string collection, IEnumerable<BsonDocument> docs)
        {
            this.OpenDatabase();

            try
            {
                return _engine.Update(collection, docs);
            }
            finally
            {
                this.CloseDatabase();
            }
        }

        public int UpdateMany(string collection, BsonExpression extend, BsonExpression predicate)
        {
            this.OpenDatabase();

            try
            {
                return _engine.UpdateMany(collection, extend, predicate);
            }
            finally
            {
                this.CloseDatabase();
            }
        }

        public int Upsert(string collection, IEnumerable<BsonDocument> docs, BsonAutoId autoId)
        {
            this.OpenDatabase();

            try
            {
                return _engine.Upsert(collection, docs, autoId);
            }
            finally
            {
                this.CloseDatabase();
            }
        }

        public int Delete(string collection, IEnumerable<BsonValue> ids)
        {
            this.OpenDatabase();

            try
            {
                return _engine.Delete(collection, ids);
            }
            finally
            {
                this.CloseDatabase();
            }
        }

        public int DeleteMany(string collection, BsonExpression predicate)
        {
            this.OpenDatabase();

            try
            {
                return _engine.DeleteMany(collection, predicate);
            }
            finally
            {
                this.CloseDatabase();
            }
        }

        public bool DropCollection(string name)
        {
            this.OpenDatabase();

            try
            {
                return _engine.DropCollection(name);
            }
            finally
            {
                this.CloseDatabase();
            }
        }

        public bool RenameCollection(string name, string newName)
        {
            this.OpenDatabase();

            try
            {
                return _engine.RenameCollection(name, newName);
            }
            finally
            {
                this.CloseDatabase();
            }
        }

        public bool DropIndex(string collection, string name)
        {
            this.OpenDatabase();

            try
            {
                return _engine.DropIndex(collection, name);
            }
            finally
            {
                this.CloseDatabase();
            }
        }

        public bool EnsureIndex(string collection, string name, BsonExpression expression, bool unique)
        {
            this.OpenDatabase();

            try
            {
                return _engine.EnsureIndex(collection, name, expression, unique);
            }
            finally
            {
                this.CloseDatabase();
            }
        }

        #endregion

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SharedEngine()
        {
            this.Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_engine != null)
                {
                    _engine.Dispose();
                    _engine = null;
                    _mutex.ReleaseMutex();
                }
            }
        }
    }
}