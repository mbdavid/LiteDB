using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    public interface ILiteEngine : IAsyncDisposable/*: IDisposable or IAsyncDisposable?*/
    {
        Task OpenAsync();

        //Task<int> CheckpointAsync();
        //Task<long> RebuildAsync(RebuildOptions options);

        //Task<QueryResult> QueryAsync(string collection, Query query);
        //Task<QueryResult> FetchAsync(string cursorId);
        //=> QueryResult = { result: [], cursorId: 'vKs4Tk2A' }

        //Task<int> InsertAsync(string collection, ICollection<BsonDocument> docs, BsonAutoId autoId);
        //Task<int> UpdateAsync(string collection, ICollection<BsonDocument> docs);
        //Task<int> UpdateManyAsync(string collection, BsonExpression transform, BsonExpression predicate);
        //Task<int> UpsertAsync(string collection, ICollection<BsonDocument> docs, BsonAutoId autoId);
        //Task<int> DeleteAsync(string collection, ICollection<BsonValue> ids);
        //Task<int> DeleteManyAsync(string collection, BsonExpression predicate);

        //bool DropCollectionAsync(string name);
        //bool RenameCollectionAsync(string name, string newName);

        //bool EnsureIndexAsync(string collection, string name, BsonExpression expression, bool unique);
        //bool DropIndexAsync(string collection, string name);

        //BsonValue Pragma(string name);
        //bool PragmaAsync(string name, BsonValue value);
    }
}