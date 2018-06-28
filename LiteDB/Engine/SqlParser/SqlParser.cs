using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    /// <summary>
    /// Internal class to parse and execute sql-like commands
    /// </summary>
    internal partial class SqlParser
    {
        private readonly LiteEngine _engine;
        private readonly Tokenizer _tokenizer;
        private readonly BsonDocument _parameters;

        public SqlParser(LiteEngine engine, Tokenizer tokenizer, BsonDocument parameters)
        {
            _engine = engine;
            _tokenizer = tokenizer;
            _parameters = parameters ?? new BsonDocument();
        }

        public BsonDataReader Execute()
        {
            var first = _tokenizer.ReadToken().Expect(TokenType.Word);

            switch(first.Value.ToUpper())
            {
                case "SELECT": return this.ParseSelect(false);
                case "EXPLAIN":
                    _tokenizer.ReadToken().Expect("SELECT");
                    return this.ParseSelect(true);
                case "INSERT": return this.ParseInsert();
                case "DELETE": return this.ParseDelete();
                case "UPDATE": return this.ParseUpadateReplace(UpdateMode.Merge);
                case "REPLACE": return this.ParseUpadateReplace(UpdateMode.Replace);
                case "DROP": return this.ParseDrop();
                case "CREATE": return this.ParseCreate();

                case "ANALYZE": return this.ParseAnalyze();
                case "CHECKPOINT": return this.ParseCheckpoint();
                case "SHRINK": return this.ParseShrink();
                case "VACCUM": return this.ParseVaccum();

                case "BEGIN": return this.ParseBegin();
                case "ROLLBACK": return this.ParseRollback();
                case "COMMIT": return this.ParseCommit();

                case "SET": return this.ParseSet();

                default:  throw LiteException.UnexpectedToken(first);
            }
        }
    }
}