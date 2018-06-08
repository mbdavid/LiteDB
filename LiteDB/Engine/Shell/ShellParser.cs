using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    /// <summary>
    /// Internal class to parse and execute shell commands
    /// </summary>
    internal partial class ShellParser
    {
        private readonly LiteEngine _engine;
        private readonly Tokenizer _tokenizer;
        private readonly BsonDocument _parameters;
        private readonly IShellOutput _output;

        private int _resultset = 0;

        public ShellParser(LiteEngine engine, Tokenizer tokenizer, BsonDocument parameters, IShellOutput output)
        {
            _engine = engine;
            _tokenizer = tokenizer;
            _parameters = parameters ?? new BsonDocument();
            _output = output;
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
                _output.Write(ex);
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
                        case "ensureindex":
                            this.DbEnsureIndex(name);
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
                        case "checkpoint":
                            this.DbCheckpoint();
                            break;

                        default:
                            throw LiteException.UnexpectedToken(_tokenizer.Current);
                    }

                }


            }
            else if (first.Is("fs"))
            {

            }
            else if (first.Is("begin"))
            {
                _engine.BeginTrans();
            }
            else if (first.Is("commit"))
            {
                _engine.GetTransaction(out var isNew).Commit();
            }
            else if (first.Is("rollback"))
            {
                _engine.GetTransaction(out var isNew).Rollback(true);
            }
            else
            {
                throw LiteException.UnexpectedToken(first);
            }

        }

        /// <summary>
        /// Write single result into shell output class
        /// </summary>
        private void WriteSingle(BsonValue value)
        {
            _output.Write(value, -1, _resultset);
        }

        /// <summary>
        /// Write all output recorset into shell output class (use Limit write output)
        /// </summary>
        private void WriteResult(IEnumerable<BsonDocument> docs)
        {
            var index = 0;

            foreach(var doc in docs.Take(_output.Limit))
            {
                _output.Write(doc, index++, _resultset);
            }
        }
    }
}