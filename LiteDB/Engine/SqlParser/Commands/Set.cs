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
                this.ParseSetValue(token);
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

            // read `=`
            _tokenizer.ReadToken().Expect(TokenType.Equals);

            BsonValue value;

            // execute
            using (var result = this.Execute())
            {
                if (result.IsRecordset)
                {
                    var array = new BsonArray();

                    while(result.Read())
                    {
                        array.Add(result.Current);
                    }

                    value = array;
                }
                else
                {
                    value = result.Current ?? BsonValue.Null;
                }
            }

            _parameters[name] = value;
        }

        /// <summary>
        /// Read key=value to update database settings
        /// </summary>
        private void ParseSetValue(Token key)
        {
            var reader = new JsonReader(_tokenizer);

            var value = reader.Deserialize();

            // read eof/;
            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

            switch (key.Value.ToLower())
            {
                case "userversion":
                    _engine.UserVersion = value.AsInt32;
                    break;

                default:
                    throw LiteException.UnexpectedToken("Unkown key value", key);
            }
        }
    }
}