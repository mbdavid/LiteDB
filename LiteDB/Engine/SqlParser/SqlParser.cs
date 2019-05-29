using System;
using System.Collections.Generic;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
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
            var first = _tokenizer.ReadToken().Expect(TokenType.Word);

            LOG($"executing `{first.Value.ToUpper()}`", "SQL");

            switch (first.Value.ToUpper())
            {
                case "SELECT": return this.ParseSelect(false);
                case "EXPLAIN":
                    _tokenizer.ReadToken().Expect("SELECT");
                    return this.ParseSelect(true);
                case "INSERT": return this.ParseInsert();
                case "DELETE": return this.ParseDelete();
                case "UPDATE": return this.ParseUpadate();
                case "DROP": return this.ParseDrop();
                case "RENAME": return this.ParseRename();
                case "CREATE": return this.ParseCreate();

                case "ANALYZE": return this.ParseAnalyze();
                case "CHECKPOINT": return this.ParseCheckpoint();
                case "VACCUM": return this.ParseVaccum();
                case "CHECK": return this.ParseCheck();

                case "BEGIN": return this.ParseBegin();
                case "ROLLBACK": return this.ParseRollback();
                case "COMMIT": return this.ParseCommit();

                case "SET": return this.ParseSet();

                default:  throw LiteException.UnexpectedToken(first);
            }
        }
    }
}