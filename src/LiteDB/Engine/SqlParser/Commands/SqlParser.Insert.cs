using System.Data;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace LiteDB.Engine;

/// <summary>
/// Internal class to parse and execute sql-like commands
/// </summary>
internal partial class SqlParser
{
    /// <summary>
    /// insert_statement:: 
    ///    "INSERT" _ "INTO" _ document_store. [":" auto_id]
    ///  _ "VALUES" _ (json_document | json_array | sub_query | expr_parameter)
    /// </summary>
    private IEngineStatement ParseInsert()
    {
        _tokenizer.ReadToken().Expect("INSERT");
        _tokenizer.ReadToken().Expect("INTO");

        if (!this.TryParseDocumentStore(out var store)) throw ERR_UNEXPECTED_TOKEN(_tokenizer.Current, "{document_store}");

        TryParseWithAutoId(out var autoId);

        _tokenizer.ReadToken().Expect("VALUES");

        var ahead = _tokenizer.LookAhead();
        InsertStatement statement;

        if (ahead.Type == TokenType.At) // @0 - is expression parameter
        {
            var docExpr = BsonExpression.Create(_tokenizer, false);

            statement = new InsertStatement(store, docExpr, autoId);
        }
        else if (ahead.Type == TokenType.OpenBrace) // { new json document
        {
            var doc = JsonReaderStatic.ReadDocument(_tokenizer); // read full json_document

            statement = new InsertStatement(store, doc, autoId);
        }
        else if (ahead.Type == TokenType.OpenBracket) // [ new json array
        {
            var array = JsonReaderStatic.ReadArray(_tokenizer); // read full json_array

            statement = new InsertStatement(store, array, autoId);
        }
        else if (ahead.Type == TokenType.OpenParenthesis) // ( new sub_query
        {
            throw new NotImplementedException("sub_query");
        }
        else
        {
            throw ERR_UNEXPECTED_TOKEN(ahead, "Expression parameter, JsonDocument or JsonArray");
        }

        // expect end of statement
        _tokenizer.ReadToken().Expect(TokenType.EOF, TokenType.SemiColon);

        return statement;
    }
}
