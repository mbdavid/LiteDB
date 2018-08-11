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
            var token = _tokenizer.LookAhead(false);
            var isArray = false;

            // checks if result must be read as array `SET @var[] = ...`
            if (token.Type == TokenType.OpenBracket)
            {
                _tokenizer.ReadToken();
                _tokenizer.ReadToken(false).Expect(TokenType.CloseBracket);
                isArray = true;
            }

            // read `=`
            _tokenizer.ReadToken().Expect(TokenType.Equals);

            BsonValue value;

            // execute
            using (var result = this.Execute())
            {
                if (isArray)
                {
                    var array = new BsonArray();

                    while (result.Read())
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
            // set value factory
            Action<BsonValue> setFactory;

            switch (key.Value.ToLower())
            {
                case "userversion":
                    setFactory = v => _engine.SetUserVersion(v.AsInt32);
                    break;

                default:
                    throw LiteException.UnexpectedToken("Unkown key or missing @ prefix", key);
            }

            // read `=`
            _tokenizer.ReadToken().Expect(TokenType.Equals);

            var reader = new JsonReader(_tokenizer);
            var value = reader.Deserialize();

            // read eof or ;
            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

            setFactory(value);
        }
    }
}