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
                case "SELECT": return this.ParseSelect();
                case "INSERT": return this.ParseInsert();
                case "DELETE": return this.ParseDelete();
                case "UPDATE": return this.ParseUpadate();
                case "DROP": return this.ParseDrop();
                case "CREATE": return this.ParseCreate();

                case "CHECKPOINT": return this.ParseCheckpoint();

                case "BEGIN": return this.ParseBegin();
                case "ROLLBACK": return this.ParseRollback();
                case "COMMIT": return this.ParseCommit();

                default:  throw LiteException.UnexpectedToken(first);
            }

        }
    }
}