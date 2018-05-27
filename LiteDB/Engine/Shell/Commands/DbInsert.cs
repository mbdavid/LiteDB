using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class ShellParser
    {
        /// <summary>
        /// db.[colname].insert [expr] [with id="int|long|date|objectid"]
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

            var count = _engine.Insert(name, docs, autoId);

            this.WriteSingle(count);
        }
    }
}