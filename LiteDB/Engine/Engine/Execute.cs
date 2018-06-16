using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Execute single SQL-Like command and return data reader (can contains single or multiple resultsets)
        /// </summary>
        public BsonDataReader Execute(string command, BsonDocument parameters = null)
        {
            var tokenizer = new Tokenizer(command);
            var sql = new SqlParser(this, tokenizer, parameters);
            var reader = sql.Execute();

            // when request .NextResult() run another SqlParser
            reader.NextResultFunc = () =>
            {
                // checks if has more tokens
                if (tokenizer.Current.Type == TokenType.EOF) return null;

                if (tokenizer.Current.Type == TokenType.SemiColon)
                {
                    var ahead = tokenizer.LookAhead();

                    if (ahead.Type == TokenType.EOF) return null;
                }

                var next = new SqlParser(this, tokenizer, parameters);

                return next.Execute();
            };

            return reader;
        }

        /// <summary>
        /// Execute single SQL-Like command and return data reader (can contains single or multiple values)
        /// </summary>
        public BsonDataReader Execute(string command, params BsonValue[] args)
        {
            var parameters = new BsonDocument();

            for(var i = 0; i < args.Length; i++)
            {
                parameters[i.ToString()] = args[i];
            }

            return this.Execute(command, parameters);
        }
    }
}