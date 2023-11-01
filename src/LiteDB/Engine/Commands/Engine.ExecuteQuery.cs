namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public ValueTask<IDataReader> ExecuteReaderAsync(string sql)
        => this.ExecuteReaderAsync(sql, BsonDocument.Empty);

    public ValueTask<IDataReader> ExecuteReaderAsync(string sql, BsonValue args0)
        => this.ExecuteReaderAsync(sql, new BsonDocument { ["0"] = args0 });

    public ValueTask<IDataReader> ExecuteReaderAsync(string sql, BsonValue args0, BsonValue args1)
        => this.ExecuteReaderAsync(sql, new BsonDocument { ["0"] = args0, ["1"] = args1 });

    /// <summary>
    /// Parse and execute an ah-hoc sql statement
    /// </summary>
    public ValueTask<IDataReader> ExecuteReaderAsync(string sql, BsonDocument parameters)
    {
        var collation = _factory.FileHeader.Collation;
        var tokenizer = new Tokenizer(sql);
        var parser = new SqlParser(tokenizer, collation);

        var statement = parser.ParseStatement();

        var reader = statement.ExecuteReaderAsync(_factory, parameters);

        return reader;
    }
}
