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
    private IDocumentStore ParseDocumentStore()
    {
        var ahead = _tokenizer.LookAhead();

        if (ahead.Type == TokenType.Word) // user_collection
        {
            var token = _tokenizer.ReadToken(); // read "collection_name";

            var store = new UserCollectionStore(token.Value);

            return store;
        }
        else if (ahead.Type != TokenType.Dollar)
        {
            _tokenizer.ReadToken(); // read "$";

            var token = _tokenizer.ReadToken().Expect(TokenType.Word); // read "collection-name"

            if (TryParseParameters(out var parameters))
            {
                //TODO: verificar metodos
            }


            throw new NotImplementedException();

            //return true;
        }

        throw ERR_UNEXPECTED_TOKEN(_tokenizer.Current, "{document_store}");
    }
}
