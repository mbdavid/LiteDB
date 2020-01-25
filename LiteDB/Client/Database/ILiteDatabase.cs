using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.IO;

namespace LiteDB
{
    public interface ILiteDatabase : IDisposable
    {
        LiteStorage<string> FileStorage { get; }
        BsonMapper Mapper { get; }
        int UserVersion { get; set; }
        bool BeginTrans();
        void Checkpoint();
        bool CollectionExists(string name);
        bool Commit();
        bool DropCollection(string name);
        IBsonDataReader Execute(string command, BsonDocument parameters = null);
        IBsonDataReader Execute(string command, params BsonValue[] args);
        IBsonDataReader Execute(TextReader commandReader, BsonDocument parameters = null);
        ILiteCollection<BsonDocument> GetCollection(string name, BsonAutoId autoId = BsonAutoId.ObjectId);
        ILiteCollection<T> GetCollection<T>();
        ILiteCollection<T> GetCollection<T>(BsonAutoId autoId);
        ILiteCollection<T> GetCollection<T>(string name);
        IEnumerable<string> GetCollectionNames();
        LiteStorage<TFileId> GetStorage<TFileId>(string filesCollection = "_files", string chunksCollection = "_chunks");
        BsonValue Pragma(string name);
        BsonValue Pragma(string name, BsonValue value);
        long Rebuild(RebuildOptions options = null);
        bool RenameCollection(string oldName, string newName);
        bool Rollback();
    }
}