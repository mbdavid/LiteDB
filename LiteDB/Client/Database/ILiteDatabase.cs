using System.Collections.Generic;
using System.IO;

namespace LiteDB
{
    public interface ILiteDatabase
    {
        BsonMapper Mapper { get; }
        LiteStorage<string> FileStorage { get; }

        int Analyze(params string[] collections);
        bool BeginTrans();
        void Checkpoint();
        bool CollectionExists(string name);
        bool Commit();
        void Dispose();
        bool DropCollection(string name);
        IBsonDataReader Execute(TextReader commandReader, BsonDocument parameters = null);
        IBsonDataReader Execute(string command, BsonDocument parameters = null);
        IBsonDataReader Execute(string command, params BsonValue[] args);
        ILiteCollection<T> GetCollection<T>(string name);
        ILiteCollection<T> GetCollection<T>();
        ILiteCollection<T> GetCollection<T>(BsonAutoId autoId);
        ILiteCollection<BsonDocument> GetCollection(string name, BsonAutoId autoId = BsonAutoId.ObjectId);
        IEnumerable<string> GetCollectionNames();
        LiteStorage<TFileId> GetStorage<TFileId>(string filesCollection = "_files", string chunksCollection = "_chunks");
        bool RenameCollection(string oldName, string newName);
        bool Rollback();
        long Shrink();
    }
}
