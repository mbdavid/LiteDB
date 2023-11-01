namespace LiteDB.Engine;

internal partial class SqlParser
{
    /// <summary>
    /// collection_parameters::
    ///   "(" [ . json_value [ . "," . json_value ] ] . ")"
    /// </summary>
    private bool TryParseParameters(out IReadOnlyList<BsonValue> parameters)
    {
        var ahead = _tokenizer.LookAhead();

        if (ahead.Type != TokenType.OpenParenthesis)
        {
            parameters = Array.Empty<BsonValue>();
            return false;
        }

        var token = _tokenizer.ReadToken(); // read "("

        var result = new List<BsonValue>();

        while (token.Type != TokenType.CloseParenthesis)
        {
            var value = JsonReaderStatic.ReadValue(_tokenizer);

            result.Add(value);

            token = _tokenizer.ReadToken(); // read <next>, "," or ")"

            if (token.Type == TokenType.Comma)
            {
                token = _tokenizer.ReadToken();
            }
        }

        parameters = result;
        return true;
    }
}
