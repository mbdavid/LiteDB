using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LiteDB
{
    public class SharedEngine : ILiteEngine
    {
        private readonly EngineSettings _settings;
        private readonly CrossProcessReaderWriterLock _locker;

        private LiteEngine _engine;
        private int _stack = 0;
        private bool _disposed = false;

        public SharedEngine(EngineSettings settings)
        {
            _settings = settings;

            var name = settings.Filename.ToLower().Sha1();

            _locker = new CrossProcessReaderWriterLock(name);
        }

        /// <summary>
        /// Open engine as readonly and stack operation
        /// </summary>
        private void OpenRead()
        {
            lock (_locker)
            {
                _stack++;

                if (_stack == 1)
                {
                    _locker.AcquireReaderLock();

                    _settings.ReadOnly = true;

                    _engine = new LiteEngine(_settings);
                }
            }
        }

        /// <summary>
        /// Open engine as writable and stack operation
        /// </summary>
        private void OpenWrite()
        {
            lock(_locker)
            {
                _stack++;

                if (_stack == 1)
                {
                    _locker.AcquireWriterLock();

                    _settings.ReadOnly = false;

                    _engine = new LiteEngine(_settings);
                }
                else if (_settings.ReadOnly)
                {
                    // if database already open in readonly mode, change to writeable mode
                    _settings.ReadOnly = false;
                    _engine.Dispose();
                    _engine = new LiteEngine(_settings);
                }
            }
        }

        /// <summary>
        /// Dequeue stack and dispose database on empty stack
        /// </summary>
        private void Close()
        {
            lock(_locker)
            {
                _stack--;

                if (_stack == 0)
                {
                    _engine.Dispose();
                    _engine = null;

                    // release locker
                    if (_settings.ReadOnly)
                    {
                        _locker.ReleaseReaderLock();
                    }
                    else
                    {
                        _locker.ReleaseWriterLock();
                    }
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            if (_engine != null)
            {
                if (_settings.ReadOnly)
                {
                    _locker.ReleaseReaderLock();
                }
                else
                {
                    _locker.ReleaseWriterLock();
                }

                _engine.Dispose();
            }
        }

        #region Transaction Operations

        public bool BeginTrans()
        {
            this.OpenWrite();

            try
            {
                return _engine.BeginTrans();
            }
            catch
            {
                this.Close();
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
                this.Close();
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
                this.Close();
            }
        }

        #endregion

        #region Read Operation

        public IBsonDataReader Query(string collection, Query query)
        {
            this.OpenRead();

            var reader = _engine.Query(collection, query);

            return new SharedDataReader(reader, () => this.Close());
        }

        public int UserVersion
        {
            get
            {
                this.OpenRead();

                var value = _engine.UserVersion;

                this.Close();

                return value;
            }
            set
            {
                this.OpenWrite();

                try
                {
                    _engine.UserVersion = value;
                }
                finally
                {
                    this.Close();
                }
            }
        }

        #endregion

        #region Write Operations

        public int Analyze(string[] collections)
        {
            this.OpenWrite();

            try
            {
                return _engine.Analyze(collections);
            }
            finally
            {
                this.Close();
            }
        }

        public void Checkpoint()
        {
            this.OpenWrite();

            try
            {
                _engine.Checkpoint();
            }
            finally
            {
                this.Close();
            }
        }

        public long Shrink()
        {
            this.OpenWrite();

            try
            {
                return _engine.Shrink();
            }
            finally
            {
                this.Close();
            }
        }

        public int Insert(string collection, IEnumerable<BsonDocument> docs, BsonAutoId autoId)
        {
            this.OpenWrite();

            try
            {
                return _engine.Insert(collection, docs, autoId);
            }
            finally
            {
                this.Close();
            }
        }

        public int Update(string collection, IEnumerable<BsonDocument> docs)
        {
            this.OpenWrite();

            try
            {
                return _engine.Update(collection, docs);
            }
            finally
            {
                this.Close();
            }
        }

        public int UpdateMany(string collection, BsonExpression extend, BsonExpression predicate)
        {
            this.OpenWrite();

            try
            {
                return _engine.UpdateMany(collection, extend, predicate);
            }
            finally
            {
                this.Close();
            }
        }

        public int Upsert(string collection, IEnumerable<BsonDocument> docs, BsonAutoId autoId)
        {
            this.OpenWrite();

            try
            {
                return _engine.Upsert(collection, docs, autoId);
            }
            finally
            {
                this.Close();
            }
        }

        public int Delete(string collection, IEnumerable<BsonValue> ids)
        {
            this.OpenWrite();

            try
            {
                return _engine.Delete(collection, ids);
            }
            finally
            {
                this.Close();
            }
        }

        public int DeleteMany(string collection, BsonExpression predicate)
        {
            this.OpenWrite();

            try
            {
                return _engine.DeleteMany(collection, predicate);
            }
            finally
            {
                this.Close();
            }
        }

        public bool DropCollection(string name)
        {
            this.OpenWrite();

            try
            {
                return _engine.DropCollection(name);
            }
            finally
            {
                this.Close();
            }
        }

        public bool RenameCollection(string name, string newName)
        {
            this.OpenWrite();

            try
            {
                return _engine.RenameCollection(name, newName);
            }
            finally
            {
                this.Close();
            }
        }

        public bool DropIndex(string collection, string name)
        {
            this.OpenWrite();

            try
            {
                return _engine.DropIndex(collection, name);
            }
            finally
            {
                this.Close();
            }
        }

        public bool EnsureIndex(string collection, string name, BsonExpression expression, bool unique)
        {
            this.OpenWrite();

            try
            {
                return _engine.EnsureIndex(collection, name, expression, unique);
            }
            finally
            {
                this.Close();
            }
        }

        #endregion
    }
}