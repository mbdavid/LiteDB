namespace LiteDB.Engine;

internal interface IEngineStatement
{
    EngineStatementType StatementType { get; }
    ValueTask<int> ExecuteAsync(IServicesFactory factory, BsonDocument parameters);
    ValueTask<IDataReader> ExecuteReaderAsync(IServicesFactory factory, BsonDocument parameters);
}
