using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static LiteDB.Constants;

namespace LiteDB
{
    internal class SharedEngine : ILiteEngine
    {
        private readonly EngineSettings _settings;
        private LiteEngine _engine = null;
        private int _counter = 0;

        public SharedEngine(EngineSettings settings)
        {
            _settings = settings;
        }

        #region Open/Close Engine

        internal void OpenShared()
        {
            if (Interlocked.Increment(ref _counter) == 1)
            {
                ENSURE(_engine == null, "engine here must be null");

                _engine = new LiteEngine(_settings);
            }
        }

        internal void CloseShared()
        {
            if (Interlocked.Decrement(ref _counter) == 0)
            {
                _engine.Dispose();
                _engine = null;
            }
        }

        #endregion

        #region Analyze/Checkpoint/Shrink/Vaccum

        public int Analyze(string[] collections)
        {
            this.OpenShared();

            try
            {
                return _engine.Analyze(collections);
            }
            finally
            {
                this.CloseShared();
            }
        }

        public void Checkpoint()
        {
            this.OpenShared();

            try
            {
                _engine.Checkpoint();
            }
            finally
            {
                this.CloseShared();
            }
        }

        public long Shrink()
        {
            this.OpenShared();

            try
            {
                return _engine.Shrink();
            }
            finally
            {
                this.CloseShared();
            }
        }

        public int Vaccum()
        {
            this.OpenShared();

            try
            {
                return _engine.Vaccum();
            }
            finally
            {
                this.CloseShared();
            }
        }

        #endregion

        #region Begin/Commit/Rollback

        public bool BeginTrans()
        {
            throw new NotSupportedException();
        }

        public bool Commit()
        {
            throw new NotSupportedException();
        }

        public bool Rollback()
        {
            throw new NotSupportedException();
        }

        #endregion

        #region Query

        public IBsonDataReader Query(string collection, QueryDefinition query)
        {
            this.OpenShared();

            var reader = _engine.Query(collection, query);

            return new SharedBsonDataReader(reader, this);
        }

        #endregion

        #region Insert/Update/Delete

        public int Insert(string collection, IEnumerable<BsonDocument> docs, BsonAutoId autoId)
        {
            this.OpenShared();

            try
            {
                return _engine.Insert(collection, docs, autoId);
            }
            finally
            {
                this.CloseShared();
            }
        }

        public int Update(string collection, IEnumerable<BsonDocument> docs)
        {
            this.OpenShared();

            try
            {
                return _engine.Update(collection, docs);
            }
            finally
            {
                this.CloseShared();
            }
        }

        public int UpdateMany(string collection, BsonExpression extend, BsonExpression predicate)
        {
            this.OpenShared();

            try
            {
                return _engine.UpdateMany(collection, extend, predicate);
            }
            finally
            {
                this.CloseShared();
            }
        }

        public int Upsert(string collection, IEnumerable<BsonDocument> docs, BsonAutoId autoId)
        {
            this.OpenShared();

            try
            {
                return _engine.Upsert(collection, docs, autoId);
            }
            finally
            {
                this.CloseShared();
            }
        }

        public int Delete(string collection, IEnumerable<BsonValue> ids)
        {
            this.OpenShared();

            try
            {
                return _engine.Delete(collection, ids);
            }
            finally
            {
                this.CloseShared();
            }
        }

        public int DeleteMany(string collection, BsonExpression predicate)
        {
            this.OpenShared();

            try
            {
                return _engine.DeleteMany(collection, predicate);
            }
            finally
            {
                this.CloseShared();
            }
        }

        #endregion

        #region EnsureIndex/DropIndex/Drop/Rename

        public bool DropCollection(string collection)
        {
            this.OpenShared();

            try
            {
                return _engine.DropCollection(collection);
            }
            finally
            {
                this.CloseShared();
            }
        }

        public bool RenameCollection(string collection, string newName)
        {
            this.OpenShared();

            try
            {
                return _engine.RenameCollection(collection, newName);
            }
            finally
            {
                this.CloseShared();
            }
        }

        public bool EnsureIndex(string collection, string name, BsonExpression expression, bool unique)
        {
            this.OpenShared();

            try
            {
                return _engine.EnsureIndex(collection, name, expression, unique);
            }
            finally
            {
                this.CloseShared();
            }
        }

        public bool DropIndex(string collection, string name)
        {
            this.OpenShared();

            try
            {
                return _engine.DropIndex(collection, name);
            }
            finally
            {
                this.CloseShared();
            }
        }

        #endregion

        #region DbParam

        public BsonValue DbParam(string parameterName)
        {
            this.OpenShared();

            try
            {
                return _engine.DbParam(parameterName);
            }
            finally
            {
                this.CloseShared();
            }
        }

        public bool DbParam(string parameterName, BsonValue value)
        {
            this.OpenShared();

            try
            {
                return _engine.DbParam(parameterName, value);
            }
            finally
            {
                this.CloseShared();
            }
        }

        #endregion

        public void Dispose()
        {
            _engine?.Dispose();
            _engine = null;
        }

        public DatabaseReport CheckIntegrity()
        {
            throw new NotImplementedException();
        }
    }
}