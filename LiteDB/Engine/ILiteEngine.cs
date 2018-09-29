using System;
using System.Collections.Generic;

namespace LiteDB.Engine
{
    public interface ILiteEngine : IDisposable
    {
        int Analyze(string[] collections);
        int Checkpoint(bool delete);
        long Shrink();
        int Vaccum();

        bool BeginTrans();
        bool Commit();
        bool Rollback();

        IBsonDataReader Query(string collection, QueryDefinition query);

        int Insert(string collection, IEnumerable<BsonDocument> docs, BsonAutoId autoId);
        int Update(string collection, IEnumerable<BsonDocument> docs);
        int UpdateMany(string collection, BsonExpression extend, BsonExpression predicate);
        int Upsert(string collection, IEnumerable<BsonDocument> docs, BsonAutoId autoId);
        int Delete(string collection, IEnumerable<BsonValue> ids);
        int DeleteMany(string collection, BsonExpression predicate);

        bool EnsureIndex(string collection, string name, BsonExpression expression, bool unique);
        bool DropCollection(string collection);
        bool DropIndex(string collection, string name);

        bool RenameCollection(string collection, string newName);

        int GetUserVersion();
        void SetUserVersion(int value);
    }
}