namespace LiteDB.Engine;

internal partial class SqlParser
{
    /// <summary>
    /// collection_parameters::
    ///   "(" [ . json_value [ . "," . json_value ] ] . ")"
    /// </summary>
    internal static bool TryParseParameters(Tokenizer tokenizer, out IReadOnlyList<BsonValue> parameters)
    {
        var ahead = tokenizer.LookAhead();

        if (ahead.Type != TokenType.OpenParenthesis)
        {
            parameters = Array.Empty<BsonValue>();
            return false;
        }

        var token = tokenizer.ReadToken(); // read "("

        var result = new List<BsonValue>();

        while (token.Type != TokenType.CloseParenthesis)
        {
            var value = JsonReaderStatic.ReadValue(tokenizer);

            result.Add(value);

            token = tokenizer.ReadToken(); // read <next>, "," or ")"

            if (token.Type == TokenType.Comma)
            {
                token = tokenizer.ReadToken();
            }
        }

        parameters = result;
        return true;
    }
}
