using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    internal class SharedEngine : ILiteEngine
    {
        private readonly EngineSettings _settings;
        private LiteEngine _engine = null;

        public SharedEngine(EngineSettings settings)
        {
            _settings = settings;
        }

        private void OpenShared()
        {
            _engine = new LiteEngine(_settings);
        }

        internal void CloseShared()
        {
            _engine.Dispose();
            _engine = null;
        }

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

        public bool BeginTrans()
        {
            this.OpenShared();

            try
            {
                return _engine.BeginTrans();
            }
            catch
            {
                this.CloseShared();
                throw;
            }
        }

        public bool Commit()
        {
            if (_engine == null) return false;

            var result = _engine.Commit();

            this.CloseShared();

            return result;
        }

        public bool Rollback()
        {
            if (_engine == null) return false;

            throw new NotImplementedException();
        }

        public IBsonDataReader Query(string collection, QueryDefinition query)
        {
            this.OpenShared();

            var reader = _engine.Query(collection, query);

            return new SharedBsonDataReader(reader, this);
        }

        public int Checkpoint()
        {
            throw new NotImplementedException();
        }

        public int Delete(string collection, IEnumerable<BsonValue> ids)
        {
            throw new NotImplementedException();
        }

        public int DeleteMany(string collection, BsonExpression predicate)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            this.CloseShared();
        }

        public bool DropCollection(string collection)
        {
            throw new NotImplementedException();
        }

        public bool DropIndex(string collection, string name)
        {
            throw new NotImplementedException();
        }

        public bool EnsureIndex(string collection, string name, BsonExpression expression, bool unique)
        {
            throw new NotImplementedException();
        }

        public BsonValue DbParam(string parameterName)
        {
            throw new NotImplementedException();
        }

        public bool DbParam(string parameterName, BsonValue value)
        {
            throw new NotImplementedException();
        }

        public int Insert(string collection, IEnumerable<BsonDocument> docs, BsonAutoId autoId)
        {
            throw new NotImplementedException();
        }

        public bool RenameCollection(string collection, string newName)
        {
            throw new NotImplementedException();
        }

        public long Shrink()
        {
            throw new NotImplementedException();
        }

        public int Update(string collection, IEnumerable<BsonDocument> docs)
        {
            throw new NotImplementedException();
        }

        public int UpdateMany(string collection, BsonExpression extend, BsonExpression predicate)
        {
            throw new NotImplementedException();
        }

        public int Upsert(string collection, IEnumerable<BsonDocument> docs, BsonAutoId autoId)
        {
            throw new NotImplementedException();
        }

        public int Vaccum()
        {
            throw new NotImplementedException();
        }
    }
}