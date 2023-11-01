namespace LiteDB;

internal class JsonReaderStatic
{
    private readonly static IFormatProvider _numberFormat = CultureInfo.InvariantCulture.NumberFormat;

    /// <summary>
    /// Read a new bson document based on tokenizer position
    /// </summary>
    public static BsonDocument ReadDocument(Tokenizer tokenizer)
        => ReadDocumentInternal(tokenizer).AsDocument;

    /// <summary>
    /// Internal implementation of ReadDocument but returns BsonValue (non-root documents can be just a extend value $
    /// </summary>
    private static BsonValue ReadDocumentInternal(Tokenizer tokenizer)
    {
        tokenizer.ReadToken().Expect(TokenType.OpenBrace); // read "{"

        if (tokenizer.LookAhead().IsCloseBrace)
        {
            tokenizer.ReadToken(); // read "}"

            return BsonDocument.Empty;
        }

        var doc = new BsonDocument();

        var token = tokenizer.ReadToken(); // read "<key>" or "}"

        while (token.Type != TokenType.CloseBrace)
        {
            token.Expect(TokenType.String, TokenType.Word);

            var key = token.Value;

            tokenizer.ReadToken().Expect(TokenType.Colon); // read ":"

            // check if not a special data type - only if is first attribute
            if (key[0] == '$' && doc.Count == 0)
            {
                token = tokenizer.ReadToken(); // read "<value>"

                var extendValue = ReadExtendedDataType(key, token.Value);

                tokenizer.ReadToken().Expect(TokenType.CloseBrace);

                return extendValue;
            }

            var value = ReadValue(tokenizer); // read "<value>"

            // add (or override) key-value to document
            doc[key] = value;

            token = tokenizer.ReadToken(); // read "," or "}"

            if (token.Type == TokenType.Comma)
            {
                token = tokenizer.ReadToken(); // read "<key>"
            }
        }

        return doc;
    }

    /// <summary>
    /// Read a new bson array based on tokenizer position
    /// </summary>
    public static BsonArray ReadArray(Tokenizer tokenizer)
    {
        tokenizer.ReadToken().Expect(TokenType.OpenBracket); // read "["

        if (tokenizer.LookAhead().IsCloseBracket)
        {
            tokenizer.ReadToken(); // read "]"

            return BsonArray.Empty;
        }

        var arr = new BsonArray();
        var token = Token.Empty;

        while (token.Type != TokenType.CloseBracket)
        {
            var value = ReadValue(tokenizer);

            arr.Add(value);

            token = tokenizer.ReadToken()  // read "," or "]"
                .Expect(TokenType.Comma, TokenType.CloseBracket);
        }

        return arr;
    }

    /// <summary>
    /// Read any BsonValue from tokenizer position
    /// </summary>
    public static BsonValue ReadValue(Tokenizer tokenizer)
    {
        // first checks for document/array subtype
        var ahead = tokenizer.LookAhead();

        switch(ahead.Type)
        {
            case TokenType.OpenBrace: return ReadDocumentInternal(tokenizer);
            case TokenType.OpenBracket: return ReadArray(tokenizer);
        }

        // read token
        var token = tokenizer.ReadToken();
        var value = token.Value;
        var isPositive = true;

        switch (token.Type)
        {
            case TokenType.String: 
                return new BsonString(value);


            case TokenType.Minus:
                // read next token (must be a number)
                var number = tokenizer.ReadToken(false).Expect(TokenType.Int, TokenType.Double);
                isPositive = false;

                if (number.Type == TokenType.Int) goto case TokenType.Int;
                else if (number.Type == TokenType.Double) goto case TokenType.Double;

                break;

            case TokenType.Int:
                if (Int32.TryParse(value, NumberStyles.Any, _numberFormat, out int result))
                {
                    return isPositive ? 
                        new BsonInt32(result) : 
                        new BsonInt32(result * -1);
                }
                else
                {
                    return isPositive ?
                        new BsonInt64(Int64.Parse(value, NumberStyles.Any, _numberFormat)) :
                        new BsonInt64(Int64.Parse(value, NumberStyles.Any, _numberFormat) * -1);
                }

            case TokenType.Double:
                return isPositive ?
                    new BsonDouble(Convert.ToDouble(value, _numberFormat)) : 
                    new BsonDouble(Convert.ToDouble(value, _numberFormat) * -1);

            case TokenType.Word:
                if (value.Eq("null")) return BsonValue.Null;
                else if (value.Eq("true")) return BsonBoolean.True;
                else if (value.Eq("false")) return BsonBoolean.False;
                else throw ERR_UNEXPECTED_TOKEN(token, "Supports only null, true or false keywords");
        }

        throw ERR_UNEXPECTED_TOKEN(token);
    }

    /// <summary>
    /// Read extend data formated as { $key: "value_encoded" }
    /// </summary>
    private static BsonValue ReadExtendedDataType(string key, string value)
    {
        return key switch
        {
            "$binary" => new BsonBinary(Convert.FromBase64String(value)),
            "$oid" => new BsonObjectId(new ObjectId(value)),
            "$guid" => new BsonGuid(new Guid(value)),
            "$date" => new BsonDateTime(DateTime.Parse(value).ToLocalTime()),
            "$numberLong" => new BsonInt64(Convert.ToInt64(value, _numberFormat)),
            "$numberDecimal" => new BsonDecimal(Convert.ToDecimal(value, _numberFormat)),
            "$minValue" => BsonValue.MinValue,
            "$maxValue" => BsonValue.MaxValue,
            _ => BsonValue.Null
        };
    }
}
