using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Internal class to parse and execute shell commands
    /// </summary>
    internal class ShellParser
    {
        private readonly LiteEngine _engine;
        private readonly Tokenizer _tokenizer;
        private readonly BsonDocument _parameters;
        private readonly IShellResult _result;

        private int _resultset = 0;

        public ShellParser(LiteEngine engine, Tokenizer tokenizer, BsonDocument parameters, IShellResult result)
        {
            _engine = engine;
            _tokenizer = tokenizer;
            _parameters = parameters ?? new BsonDocument();
            _result = result;
        }

        public void Execute()
        {
            try
            {
                while(!_tokenizer.EOF)
                {
                    this.ParseSingleCommand();

                    _resultset++;
                }
            }
            catch(Exception ex)
            {
                _result.Write(ex);
            }
        }

        private void ParseSingleCommand()
        {
            var first = _tokenizer.ReadToken();

            // db.??? comands
            if (first.Is("db"))
            {
                _tokenizer.ReadToken(false).Expect(TokenType.Period);
                var name = _tokenizer.ReadToken(false).Expect(TokenType.Word).Value;

                // db.<col>.<command>
                if (_tokenizer.LookAhead(false).Type == TokenType.Period)
                {
                    _tokenizer.ReadToken(); // read .
                    var cmd = _tokenizer.ReadToken().Expect(TokenType.Word).Value.ToLower(); // read command name

                    switch (cmd)
                    {
                        case "insert":
                            this.DbInsert(name);
                            break;
                        case "drop":
                            this.DbDrop(name);
                            break;
                        case "query":
                            this.DbQuery(name, cmd);
                            break;

                        default:
                            throw LiteException.UnexpectedToken(_tokenizer.Current);
                    }
                }
                // db.<command>
                else
                {
                    switch (name.ToLower())
                    {
                        case "param":
                            this.DbParam();
                            break;

                        default:
                            throw LiteException.UnexpectedToken(_tokenizer.Current);
                    }

                }


            }
            else if (first.Is("fs"))
            {

            }
            else
            {
                throw LiteException.UnexpectedToken(first);
            }

        }

        /// <summary>
        /// db.[colname].insert [expr] id=int
        /// </summary>
        private void DbInsert(string name)
        {
            var expr = BsonExpression.Create(_tokenizer, _parameters);
            var with = _tokenizer.ReadToken().Expect(TokenType.Word, TokenType.EOF, TokenType.SemiColon);

            var autoId = BsonAutoId.ObjectId;

            if (with.Is("WITH"))
            {
                _tokenizer.ReadToken().Expect("ID");
                _tokenizer.ReadToken().Expect(TokenType.Equals);
                var dataType = _tokenizer.ReadToken().Expect(TokenType.Word).Value;

                autoId = (BsonAutoId)Enum.Parse(typeof(BsonAutoId), dataType, true);
            }

            _tokenizer.ReadToken().Expect(TokenType.EOF);

            var docs = expr.Execute()
                .Where(x => x.IsDocument)
                .Select(x => x.AsDocument);

            var result = _engine.Insert(name, docs, autoId);

            _result.Write(_resultset, true, result);
        }

        /// <summary>
        /// db.[colname].drop
        /// </summary>
        private void DbDrop(string name)
        {
            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

            var result = _engine.DropCollection(name);

            _result.Write(_resultset, true, result);
        }

        /// <summary>
        /// db.param userVersion
        /// db.param userVersion = [value]
        /// </summary>
        private void DbParam()
        {
            var paramName = _tokenizer.ReadToken().Expect(TokenType.Word).Value;
            var equals = _tokenizer.ReadToken().Expect(TokenType.Equals, TokenType.EOF, TokenType.SemiColon);

            if (equals.Type == TokenType.Equals)
            {
                var expr = BsonExpression.Create(_tokenizer, _parameters);

                // after read expression must EOF or ;
                _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

                var value = expr.Execute().FirstOrDefault();

                _engine.SetParameter(paramName, value);

                _result.Write(_resultset, true, value);
            }
            else
            {
                var value = _engine.GetParameter(paramName, BsonValue.Null);

                _result.Write(_resultset, true, value);
            }
        }

        /// <summary>
        /// db.[col].[query|count|aggregate|exists] ...
        /// </summary>
        private void DbQuery(string name, string command)
        {
            var query = _engine.Query(name);


            foreach(var doc in query.ToEnumerable())
            {
                _result.Write(_resultset, false, doc);

            }
        }
    }
}