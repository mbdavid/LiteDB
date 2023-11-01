namespace LiteDB.Engine;

internal partial class SqlParser
{
    /// <summary>
    /// list_of_expressions::
    ///   expr_single [ . "," . expr_single ]*
    /// </summary>
    private IEnumerable<BsonExpression> ParseListOfExpressions()
    {
        while (true)
        {
            var expr = BsonExpression.Create(_tokenizer, false);

            yield return expr;

            var next = _tokenizer.LookAhead();

            if (next.Type == TokenType.Comma)
            {
                _tokenizer.ReadToken();
            }
            else
            {
                yield break;
            }
        }
    }
}
