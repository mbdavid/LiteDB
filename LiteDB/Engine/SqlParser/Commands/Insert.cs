using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteDB.Engine
{
    internal partial class SqlParser
    {
        /// <summary>
        /// INSERT INTO [colname] { ... } WITH ID=[TYPE]
        /// </summary>
        private BsonDataReader ParseInsert()
        {
            _tokenizer.ReadToken().Expect("INTO");

            var collection = _tokenizer.ReadToken().Expect(TokenType.Word).Value;

            _tokenizer.ReadToken().Expect("VALUES");

            // get list of documents (must read all now)
            var docs = this.ParseListOfDocuments()
                .ToList(); //TODO: will review if ID=INT can be changed to support IEnumerable in INSERT SQL command

            var autoId = this.ParseWithAutoId();

            _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

            var result = _engine.Insert(collection, docs, autoId);

            return new BsonDataReader(result);
        }

        /// <summary>
        /// Parse WITH ID=[type] for AutoId
        /// </summary>
        private BsonAutoId ParseWithAutoId()
        {
            var with = _tokenizer.LookAhead();

            if (with.Is("WITH"))
            {
                _tokenizer.ReadToken();

                var id = _tokenizer.ReadToken().Expect("ID");

                _tokenizer.ReadToken().Expect(TokenType.Equals);

                var type = _tokenizer.ReadToken().Expect(TokenType.Word);

                switch(type.Value.ToUpper())
                {
                    case "DATE": return BsonAutoId.DateTime;
                    case "GUID": return BsonAutoId.Guid;
                    case "INT": return BsonAutoId.Int32;
                    case "LONG": return BsonAutoId.Int64;
                    case "OBJECTID": return BsonAutoId.ObjectId;
                    default: throw LiteException.UnexpectedToken(type, "DATE, GUID, INT, LONG, OBJECTID");
                }
            }

            return BsonAutoId.ObjectId;
        }
    }
}