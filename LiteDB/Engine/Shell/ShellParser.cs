/*using System;
using System.Collections.Generic;

namespace LiteDB
{
    /// <summary>
    /// Internal class to parse and execute shell commands
    /// </summary>
    internal class ShellParser
    {
        private readonly Tokenizer _tokenizer;
        private readonly LiteEngine _engine;
        private readonly BsonDocument _parameters;

        private string _collection;

        public ShellParser(Tokenizer tokenizer, LiteEngine engine, BsonDocument parameters)
        {
            _tokenizer = tokenizer;
            _engine = engine;
            _parameters = parameters;
        }

        public IEnumerable<BsonDocument> RunQuery(BsonDocument parameters)
        {
            var first = _tokenizer.ReadToken();

            switch(first.Value)
            {
                case "db":
                    break;
                case "fs":
                    break;
            }

            throw LiteException.UnexpectedToken(first);
        }

        /// <summary>
        /// Read "." and next word
        /// </summary>
        private string ReadDotWord()
        {
            _tokenizer.ReadToken(false).Expect(TokenType.Period);
            return _tokenizer.ReadToken(false).Expect(TokenType.Word).Value;
        }

        private void ReadCollectionCommand()
        {
            var name = this.ReadDotWord();
            var command = this.ReadDotWord();


            switch(command)
            {
                case "insert":
                    DoInsert(name);
                    break;
            }
        }

        private void DoInsert(string name)
        {

        }

        private void DoDrop(string name)
        {
            _tokenizer.ReadToken().Expect(TokenType.EOF);


        }

        private void DoParam(string name)
        {
            var paramName = _tokenizer.ReadToken().Expect(TokenType.Word);
            var equals = _tokenizer.ReadToken().Expect(TokenType.Equals, TokenType.EOF);

            if (equals.Type == TokenType.Equals)
            {
                var valueExpr = BsonExpression.Create(_tokenizer);

                _engine.SetParameter()
            }



        }
    }
}*/