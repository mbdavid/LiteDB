using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class SqlParser
    {
        /// <summary>
        /// SET {key} = {value}
        /// SET {@parameter} = {command}
        /// </summary>
        private BsonDataReader ParseSet()
        {
            var token = _tokenizer.ReadToken().Expect(TokenType.Word, TokenType.At);

            if (token.Type == TokenType.Word)
            {
                return new BsonDataReader(this.ParseSetValue(token));
            }
            else
            {
                this.ParseSetParameter();
            }

            return new BsonDataReader();
        }

        /// <summary>
        /// Read new command and set return of this value into output parameter
        /// </summary>
        /// <returns></returns>
        private void ParseSetParameter()
        {
            var name = _tokenizer.ReadToken(false).Expect(TokenType.Word).Value;
            var token = _tokenizer.LookAhead(false);

            // read `=`
            _tokenizer.ReadToken().Expect(TokenType.Equals);

            BsonValue value;

            // execute
            using (var result = this.Execute())
            {
                value = result.Current ?? BsonValue.Null;
            }

            _parameters[name] = value;
        }

        /// <summary>
        /// Read key=value to update database settings
        /// </summary>
        private bool ParseSetValue(Token key)
        {
            // read `=`
            _tokenizer.ReadToken().Expect(TokenType.Equals);

            var reader = new JsonReader(_tokenizer);
            var value = reader.Deserialize();

            // read eof or ;
            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

            return _engine.DbParam(key.Value, value);
        }
    }
}