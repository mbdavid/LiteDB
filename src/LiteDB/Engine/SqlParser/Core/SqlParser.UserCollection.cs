
namespace LiteDB.Engine;

internal partial class SqlParser
{
    /// <summary>
    /// user_collection::
    ///   char word
    /// </summary>
    private string ParseUserCollection()
    {
        var ahead = _tokenizer.LookAhead();

        if (ahead.Type == TokenType.Word) // user_collection
        {
            var token = _tokenizer.ReadToken(); // read "collection_name";

            return token.Value;
        }

        throw ERR_UNEXPECTED_TOKEN(_tokenizer.Current, "Invalid user collection name");
    }
}
