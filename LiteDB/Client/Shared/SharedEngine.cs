using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using static LiteDB.Constants;

namespace LiteDB
{
    public class SharedEngine : ILiteEngine
    {
        private readonly EngineSettings _settings;
        private readonly ReadWriteLockFile _locker;
        private LiteEngine _engine;
        private int _stack = 0;

        public SharedEngine(EngineSettings settings)
        {
            _settings = settings;

            var name = Path.GetFullPath(settings.Filename).ToLower().Sha1();

            var lockfile = FileHelper.GetSufixFile(settings.Filename, "-lock", false);

            _locker = new ReadWriteLockFile(lockfile, TimeSpan.FromSeconds(60));

            // create empty database if not exists
            if (File.Exists(settings.Filename) == false)
            {
                try
                {
                    _locker.AcquireLock(LockMode.Write, () =>
                    {
                        using (var e = new LiteEngine(settings)) { }
                    });
                }
                finally
                {
                    _locker.ReleaseLock();
                }
            }
        }

        /// <summary>
        /// Open database in safe mode
        /// </summary>
        private void OpenDatabase(bool @readonly)
        {
            lock (_locker)
            {
                _stack++;

                if (_stack == 1)
                {
                    open();
                }
                // change from read-only to read-write
                else if (_settings.ReadOnly == true && @readonly == false && _engine != null)
                {
                    _engine.Dispose();
                    open();
                }
            }

            void open()
            {
                try
                {
                    _locker.AcquireLock(@readonly ? LockMode.Read : LockMode.Write, () =>
                    {
                        _settings.ReadOnly = @readonly;

                        _engine = new LiteEngine(_settings);
                    });
                }
                catch
                {
                    if (_locker.IsLocked)
                    {
                        _locker.ReleaseLock();
                    }

                    _stack = 0;
                    throw;
                }
            }
        }

        /// <summary>
        /// Dequeue stack and dispose database on empty stack
        /// </summary>
        private void CloseDatabase()
        {
            lock(_locker)
            {
                _stack--;

                if (_stack == 0)
                {
                    _engine.Dispose();
                    _engine = null;

                    _locker.ReleaseLock();
                }
            }
        }

        #region Transaction Operations

        public bool BeginTrans()
        {
            this.OpenDatabase(false);

            try
            {
                var result = _engine.BeginTrans();

                if (result == false)
                {
                    _stack--;
                }

                return result;
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
                this.CloseDatabase();
            }
        }

        #endregion

        #region Read Operation

        public IBsonDataReader Query(string collection, Query query)
        {
            this.OpenDatabase(true);

            var reader = _engine.Query(collection, query);

            return new SharedDataReader(reader, () => this.CloseDatabase());
        }

        public BsonValue Pragma(string name)
        {
            this.OpenDatabase(true);

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
            this.OpenDatabase(false);

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
            this.OpenDatabase(false);

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
            this.OpenDatabase(false);

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
            this.OpenDatabase(false);

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
            this.OpenDatabase(false);

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
            this.OpenDatabase(false);

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
            this.OpenDatabase(false);

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
            this.OpenDatabase(false);

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
            this.OpenDatabase(false);

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
            this.OpenDatabase(false);

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
            this.OpenDatabase(false);

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
            this.OpenDatabase(false);

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
            this.OpenDatabase(false);

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
                }

                _locker.Dispose();
            }
        }
    }
}