using System;
using System.Collections.Generic;
using System.Linq;
using LiteDB.Engine;
using static LiteDB.Constants;

namespace LiteDB
{
    /// <summary>
    /// Internal class to parse and execute sql-like commands
    /// </summary>
    internal partial class SqlParser
    {
        private readonly ILiteEngine _engine;
        private readonly Tokenizer _tokenizer;
        private readonly BsonDocument _parameters;

        public SqlParser(ILiteEngine engine, Tokenizer tokenizer, BsonDocument parameters)
        {
            _engine = engine;
            _tokenizer = tokenizer;
            _parameters = parameters ?? new BsonDocument();
        }

        public IBsonDataReader Execute()
        {
            var ahead = _tokenizer.LookAhead().Expect(TokenType.Word);

            LOG($"executing `{ahead.Value.ToUpper()}`", "SQL");

            switch (ahead.Value.ToUpper())
            {
                case "SELECT": 
                case "EXPLAIN":
                    return this.ParseSelect();
                case "INSERT": return this.ParseInsert();
                case "DELETE": return this.ParseDelete();
                case "UPDATE": return this.ParseUpadate();
                case "DROP": return this.ParseDrop();
                case "RENAME": return this.ParseRename();
                case "CREATE": return this.ParseCreate();

                case "ANALYZE": return this.ParseAnalyze();
                case "CHECKPOINT": return this.ParseCheckpoint();
                case "SHRINK": return this.ParseShrink();

                case "BEGIN": return this.ParseBegin();
                case "ROLLBACK": return this.ParseRollback();
                case "COMMIT": return this.ParseCommit();

                default:  throw LiteException.UnexpectedToken(ahead);
            }
        }
    }
}