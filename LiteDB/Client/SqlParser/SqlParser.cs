using static LiteDB.Constants;

namespace LiteDB;

using System;
using LiteDB.Engine;

/// <summary>
///     Internal class to parse and execute sql-like commands
/// </summary>
internal partial class SqlParser
{
    private readonly ILiteEngine _engine;
    private readonly Tokenizer _tokenizer;
    private readonly BsonDocument _parameters;
    private readonly Lazy<Collation> _collation;

    public SqlParser(ILiteEngine engine, Tokenizer tokenizer, BsonDocument parameters)
    {
        _engine = engine;
        _tokenizer = tokenizer;
        _parameters = parameters ?? new BsonDocument();
        _collation = new Lazy<Collation>(() => new Collation(_engine.Pragma(Pragmas.COLLATION)));
    }

    public IBsonDataReader Execute()
    {
        var ahead = _tokenizer.LookAhead().Expect(TokenType.Word);

        LOG($"executing `{ahead.Value.ToUpper()}`", "SQL");

        switch (ahead.Value.ToUpper())
        {
            case "SELECT":
            case "EXPLAIN":
                return ParseSelect();
            case "INSERT":
                return ParseInsert();
            case "DELETE":
                return ParseDelete();
            case "UPDATE":
                return ParseUpdate();
            case "DROP":
                return ParseDrop();
            case "RENAME":
                return ParseRename();
            case "CREATE":
                return ParseCreate();

            case "CHECKPOINT":
                return ParseCheckpoint();
            case "REBUILD":
                return ParseRebuild();

            case "BEGIN":
                return ParseBegin();
            case "ROLLBACK":
                return ParseRollback();
            case "COMMIT":
                return ParseCommit();

            case "PRAGMA":
                return ParsePragma();

            default:
                throw LiteException.UnexpectedToken(ahead);
        }
    }
}