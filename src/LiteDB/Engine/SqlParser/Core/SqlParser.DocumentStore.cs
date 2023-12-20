namespace LiteDB.Engine;

internal partial class SqlParser
{
    /// <summary>
    /// document_store::
    ///   | user_collection
    ///   | virtual_collection
    ///
    /// user_collection::
    ///  char word
    ///
    /// virtual_collection::
    ///  "$" word [ collection_parameters ] 
    /// </summary>
    internal static IDocumentSource ParseDocumentStore(Tokenizer tokenizer)
    {
        var ahead = tokenizer.LookAhead();

        if (ahead.Type == TokenType.Word) // user_collection
        {
            var token = tokenizer.ReadToken(); // read "collection_name";

            var store = new UserCollection(token.Value);

            return store;
        }
        else if (ahead.Type != TokenType.Dollar)
        {
            tokenizer.ReadToken(); // read "$";

            var token = tokenizer.ReadToken().Expect(TokenType.Word); // read "collection-name"

            if (TryParseParameters(tokenizer, out var parameters))
            {
                //TODO: verificar metodos
            }


            throw new NotImplementedException();

            //return true;
        }

        throw ERR_UNEXPECTED_TOKEN(tokenizer.Current, "{document_store}");
    }
}