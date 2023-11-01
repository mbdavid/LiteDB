namespace LiteDB.Engine;

public partial class LiteEngine : ILiteEngine
{
    public ValueTask<int> ExecuteAsync(string sql)
        => this.ExecuteAsync(sql, BsonDocument.Empty);

    public ValueTask<int> ExecuteAsync(string sql, BsonValue args0)
        => this.ExecuteAsync(sql, new BsonDocument { ["0"] = args0 });

    public ValueTask<int> ExecuteAsync(string sql, BsonValue args0, BsonValue args1)
        => this.ExecuteAsync(sql, new BsonDocument { ["0"] = args0, ["1"] = args1 });

    /// <summary>
    /// Parse and execute an ah-hoc sql statement
    /// </summary>
    public ValueTask<int> ExecuteAsync(string sql, BsonDocument parameters)
    {
        var collation = _factory.FileHeader.Collation;
        var tokenizer = new Tokenizer(sql);
        var parser = new SqlParser(tokenizer, collation);

        var statement = parser.ParseStatement();

        var result = statement.ExecuteAsync(_factory, parameters);

        return result;
    }
}
