using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace LiteDB
{
    public class SharedEngine : ILiteEngine
    {
        private readonly EngineSettings _settings;
        private readonly Mutex _mutex;
        private LiteEngine _engine;
        private int _stack = 0;
        private bool _disposed = false;

        public SharedEngine(EngineSettings settings)
        {
            _settings = settings;

            var name = settings.Filename.ToLower().Sha1();

            _mutex = new Mutex(false, name + ".Mutex");
        }

        /// <summary>
        /// Open database in safe mode
        /// </summary>
        private void OpenDatabase()
        {
            lock (_mutex)
            {
                _stack++;

                if (_stack == 1)
                {
                    _mutex.WaitOne();

                    try
                    {
                        _engine = new LiteEngine(_settings);
                    }
                    catch
                    {
                        _mutex.ReleaseMutex();
                        _stack = 0;
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Dequeue stack and dispose database on empty stack
        /// </summary>
        private void CloseDatabase()
        {
            lock(_mutex)
            {
                _stack--;

                if (_stack == 0)
                {
                    _engine.Dispose();
                    _engine = null;

                    _mutex.ReleaseMutex();
                }
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            if (_engine != null)
            {
                _engine.Dispose();

                _mutex.ReleaseMutex();
            }
        }

        #region Transaction Operations

        public bool BeginTrans()
        {
            this.OpenDatabase();

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
            this.OpenDatabase();

            var reader = _engine.Query(collection, query);

            return new SharedDataReader(reader, () => this.CloseDatabase());
        }

        public int UserVersion
        {
            get
            {
                this.OpenDatabase();

                var value = _engine.UserVersion;

                this.CloseDatabase();

                return value;
            }
            set
            {
                this.OpenDatabase();

                try
                {
                    _engine.UserVersion = value;
                }
                finally
                {
                    this.CloseDatabase();
                }
            }
        }

        #endregion

        #region Write Operations

        public int Analyze(string[] collections)
        {
            this.OpenDatabase();

            try
            {
                return _engine.Analyze(collections);
            }
            finally
            {
                this.CloseDatabase();
            }
        }

        public void Checkpoint()
        {
            this.OpenDatabase();

            try
            {
                _engine.Checkpoint();
            }
            finally
            {
                this.CloseDatabase();
            }
        }

        public long Shrink()
        {
            this.OpenDatabase();

            try
            {
                return _engine.Shrink();
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
                this.OpenDatabase();
            }
        }

        #endregion
    }
}