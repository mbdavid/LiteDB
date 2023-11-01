namespace LiteDB;

/// <summary>
/// Compile and execute simple expressions using BsonDocuments. Used in indexes and updates operations. See https://github.com/mbdavid/LiteDB/wiki/Expressions
/// </summary>
internal class BsonExpressionParser
{
    #region Operators quick access

    /// <summary>
    /// Operation definition by methods with defined expression type (operators are in precedence order)
    /// </summary>
    private static readonly Dictionary<string, Func<BsonExpression, BsonExpression, BsonExpression>> _operators = new (StringComparer.OrdinalIgnoreCase)
    {
        // map function
        ["=>"] = BsonExpression.Map,

        // arithmetic
        ["%"] = BsonExpression.Modulo,
        ["/"] = BsonExpression.Divide,
        ["*"] = BsonExpression.Multiply,
        ["+"] = BsonExpression.Add,
        ["-"] = BsonExpression.Subtract,

        // predicate
        ["LIKE"] = BsonExpression.Like,
        ["BETWEEN"] = BsonExpression.Between,
        ["IN"] = BsonExpression.In,
        ["CONTAINS"] = BsonExpression.Contains,

        [">"] = BsonExpression.GreaterThan,
        [">="] = BsonExpression.GreaterThanOrEqual,
        ["<"] = BsonExpression.LessThan,
        ["<="] = BsonExpression.LessThanOrEqual,

        ["!="] = BsonExpression.NotEqual,
        ["="] = BsonExpression.Equal,

        // logic
        ["AND"] = BsonExpression.And,
        ["OR"] = BsonExpression.Or,
    };

    #endregion

    /// <summary>
    /// Start parse full string expression into BsonExpression. Full expression are composed with conected expressions Read BsonExpression syntax. 
    /// Root indicate that fields will be from root value, otherwise, use current value (only when fields are no prefix with $. or @.). 
    /// </summary>
    public static BsonExpression ParseFullExpression(Tokenizer tokenizer, bool root)
    {
        var first = ParseSingleExpression(tokenizer, root);
        var values = new List<BsonExpression> { first };
        var ops = new List<string>();

        // read all blocks and operation first
        while (!tokenizer.EOF)
        {
            // read operator between expressions
            var op = ReadOperant(tokenizer);

            if (op is null) break;

            // for map operation, get next expression force read as Current
            var expr = ParseSingleExpression(tokenizer, (op == "=>" ? false : root));

            // special BETWEEN "AND" read
            if (op.EndsWith("BETWEEN", StringComparison.OrdinalIgnoreCase))
            {
                tokenizer.ReadToken(true).Expect("AND");

                var end = ParseSingleExpression(tokenizer, root);

                // convert expr and expr2 into an array with 2 values
                expr = BsonExpression.MakeArray(new[] { expr, end });
            }

            values.Add(expr);
            ops.Add(op.ToUpper());
        }

        var order = 0;

        // now, process operator in correct order
        while (values.Count >= 2 && order < _operators.Count)
        {
            var op = _operators.ElementAt(order);

            var n = ops.IndexOf(op.Key);

            if (n == -1)
            {
                order++;
            }
            else
            {
                // get left/right values to execute operator
                var left = values.ElementAt(n);
                var right = values.ElementAt(n + 1);

                var result = op.Value(left, right);

                // remove left+right and insert result
                values.Insert(n, result);
                values.RemoveRange(n + 1, 2);

                // remove operation
                ops.RemoveAt(n);
            }
        }

        if (values.Count == 1)
        {
            return values.Single();
        }
        else
        {
            return ResolveConditional(0, ops, values, out _);
        }
    }

    /// <summary>
    /// Resolve expressions with ? and : conditional blocks. Support full expression in any parameter (ifTest, trueExpr, falseExpr)
    /// </summary>
    private static BsonExpression ResolveConditional(int questionIndex, List<string> ops, List<BsonExpression> values, out int colonIndex)
    {
        var ifTest = values[questionIndex];

        var op = ops[questionIndex + 1]; // : or ?

        if (op == ":")
        {
            colonIndex = questionIndex + 1;

            var trueExpr = values[questionIndex + 1];

            var next = questionIndex + 2 == ops.Count ? ":" : ops[questionIndex + 2]; // read next operator

            if (next == ":")
            {
                var falseExpr = values[colonIndex + 1];

                return BsonExpression.Conditional(ifTest, trueExpr, falseExpr);
            }
            else
            {
                var falseExpr = ResolveConditional(colonIndex + 1, ops, values, out colonIndex);

                return BsonExpression.Conditional(ifTest, trueExpr, falseExpr);
            }
        }
        else
        {
            var trueExpr = ResolveConditional(questionIndex + 1, ops, values, out colonIndex);

            var next = ops[colonIndex + 1];

            if (next == ":")
            {
                var falseExpr = values[colonIndex + 2];

                return BsonExpression.Conditional(ifTest, trueExpr, falseExpr);
            }
            else
            {
                var falseExpr = ResolveConditional(colonIndex + 1, ops, values, out colonIndex);

                return BsonExpression.Conditional(ifTest, trueExpr, falseExpr);
            }
        }
    }

    /// <summary>
    /// Read a single expression (with no operator). Usefull when parse inside a string
    /// </summary>
    public static BsonExpression ParseSingleExpression(Tokenizer tokenizer, bool root)
    {
        // read next token and test with all expression parts
        var token = tokenizer.ReadToken();

        return
            TryParseDouble(tokenizer) ??
            TryParseInt(tokenizer) ??
            TryParseBool(tokenizer) ??
            TryParseNull(tokenizer) ??
            TryParseString(tokenizer) ??
            TryParseDocument(tokenizer, root) ??
            TryParseArray(tokenizer, root) ??
            TryParseParameter(tokenizer) ??
            TryParseInnerExpression(tokenizer, root) ??
            TryParseMethodCall(tokenizer, root) ??
            TryParsePath(tokenizer, root) ??
            throw ERR_UNEXPECTED_TOKEN(token);
    }

    #region Constants

    /// <summary>
    /// Try parse double number - return null if not double token
    /// </summary>
    private static BsonExpression? TryParseDouble(Tokenizer tokenizer)
    {
        string? value = null;

        if (tokenizer.Current.Type == TokenType.Double)
        {
            value = tokenizer.Current.Value;
        }
        else if (tokenizer.Current.Type == TokenType.Minus)
        {
            var ahead = tokenizer.LookAhead(false);

            if (ahead.Type == TokenType.Double)
            {
                value = "-" + tokenizer.ReadToken().Value;
            }
        }

        if (value is not null)
        {
            var number = Convert.ToDouble(value, CultureInfo.InvariantCulture.NumberFormat);

            return BsonExpression.Constant(number);
        }

        return null;
    }

    /// <summary>
    /// Try parse int number - return null if not int token
    /// </summary>
    private static BsonExpression? TryParseInt(Tokenizer tokenizer)
    {
        string? value = null;

        if (tokenizer.Current.Type == TokenType.Int)
        {
            value = tokenizer.Current.Value;
        }
        else if (tokenizer.Current.Type == TokenType.Minus)
        {
            var ahead = tokenizer.LookAhead(false);

            if (ahead.Type == TokenType.Int)
            {
                value = "-" + tokenizer.ReadToken().Value;
            }
        }

        if (value != null)
        {
            var isInt32 = Int32.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out var i32);

            if (isInt32)
            {
                return BsonExpression.Constant(i32);
            }

            var i64 = Int64.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat);

            return BsonExpression.Constant(i64);
        }

        return null;
    }

    /// <summary>
    /// Try parse bool - return null if not bool token
    /// </summary>
    private static BsonExpression? TryParseBool(Tokenizer tokenizer)
    {
        if (tokenizer.Current.Type == TokenType.Word && (tokenizer.Current.Match("true") || tokenizer.Current.Match("false")))
        {
            var boolean = Convert.ToBoolean(tokenizer.Current.Value);

            return BsonExpression.Constant(boolean);
        }

        return null;
    }

    /// <summary>
    /// Try parse null constant - return null if not null token
    /// </summary>
    private static BsonExpression? TryParseNull(Tokenizer tokenizer)
    {
        if (tokenizer.Current.Type == TokenType.Word && tokenizer.Current.Match("null"))
        {
            return BsonExpression.Constant(BsonValue.Null);
        }

        return null;
    }

    /// <summary>
    /// Try parse string with both single/double quote - return null if not string
    /// </summary>
    private static BsonExpression? TryParseString(Tokenizer tokenizer)
    {
        if (tokenizer.Current.Type == TokenType.String)
        {
            return BsonExpression.Constant(tokenizer.Current.Value);
        }

        return null;
    }

    #endregion

    /// <summary>
    /// Try parse json document - return null if not document token
    /// </summary>
    private static BsonExpression? TryParseDocument(Tokenizer tokenizer, bool root)
    {
        if (tokenizer.Current.Type != TokenType.OpenBrace) return null;

        // read key value
        var values = new Dictionary<string, BsonExpression>();

        // test for empty array
        if (tokenizer.LookAhead().Type == TokenType.CloseBrace)
        {
            tokenizer.ReadToken(); // read }
        }
        else
        {
            while (!tokenizer.CheckEOF())
            {
                // read simple or complex document key name
                var key = ReadKey(tokenizer);

                tokenizer.ReadToken(); // update s.Current 

                BsonExpression value;

                // test normal notation { a: 1 }
                if (tokenizer.Current.Type == TokenType.Colon)
                {
                    value = ParseFullExpression(tokenizer, root);

                    // read next token here (, or }) because simplified version already did
                    tokenizer.ReadToken();
                }
                else
                {
                    value = BsonExpression.Path(root ? BsonExpression.Root : BsonExpression.Current, key);
                }

                values.Add(key, value);

                // test next token for , (continue) or } (break)
                tokenizer.Current.Expect(TokenType.Comma, TokenType.CloseBrace);

                if (tokenizer.Current.Type != TokenType.Comma) break;
            }
        }

        return BsonExpression.MakeDocument(values);
    }

    /// <summary>
    /// Try parse array - return null if not array token
    /// </summary>
    private static BsonExpression? TryParseArray(Tokenizer tokenizer, bool root)
    {
        if (tokenizer.Current.Type != TokenType.OpenBracket) return null;

        var values = new List<BsonExpression>();

        // test for empty array
        if (tokenizer.LookAhead().Type == TokenType.CloseBracket)
        {
            tokenizer.ReadToken(); // read ]
        }
        else
        {
            while (!tokenizer.CheckEOF())
            {
                // read value expression
                var value = ParseFullExpression(tokenizer, root);

                // include value source in current source
                values.Add(value);

                // expect , or ] in next token
                var token = tokenizer.ReadToken()
                    .Expect(TokenType.Comma, TokenType.CloseBracket);

                if (token.Type != TokenType.Comma) break;
            }
        }

        return BsonExpression.MakeArray(values);
    }

    /// <summary>
    /// Try parse parameter - return null if not parameter token
    /// </summary>
    private static BsonExpression? TryParseParameter(Tokenizer tokenizer)
    {
        if (tokenizer.Current.Type != TokenType.At) return null;

        var ahead = tokenizer.LookAhead(false);

        if (ahead.Type == TokenType.Word || ahead.Type == TokenType.Int)
        {
            var parameterName = tokenizer.ReadToken(false).Value;

            return BsonExpression.Parameter(parameterName);
        }

        return null;
    }

    /// <summary>
    /// Try parse inner expression - return null if not bracket token
    /// </summary>
    private static BsonExpression? TryParseInnerExpression(Tokenizer tokenizer, bool root)
    {
        if (tokenizer.Current.Type != TokenType.OpenParenthesis) return null;

        // read a inner expression inside ( and )
        var inner = ParseFullExpression(tokenizer, root);

        // read close )
        tokenizer.ReadToken().Expect(TokenType.CloseParenthesis);

        return BsonExpression.Inner(inner);
    }

    /// <summary>
    /// Try parse method call - return null if not method call
    /// </summary>
    private static BsonExpression? TryParseMethodCall(Tokenizer tokenizer, bool root)
    {
        var token = tokenizer.Current;

        if (tokenizer.Current.Type != TokenType.Word) return null;
        if (tokenizer.LookAhead().Type != TokenType.OpenParenthesis) return null;

        // read (
        tokenizer.ReadToken();

        // get static method from this class
        var parameters = new List<BsonExpression>();
        var src = new StringBuilder();

        // method call with no parameters
        if (tokenizer.LookAhead().Type == TokenType.CloseParenthesis)
        {
            src.Append(tokenizer.ReadToken().Value); // read )
        }
        else
        {
            while (!tokenizer.CheckEOF())
            {
                var parameter = ParseFullExpression(tokenizer, root);

                parameters.Add(parameter);

                // read , or )
                var next = tokenizer.ReadToken()
                    .Expect(TokenType.Comma, TokenType.CloseParenthesis);

                src.Append(next.Value);

                if (next.Type != TokenType.Comma) break;
            }
        }

        var method = BsonExpression.GetMethod(token.Value, parameters.Count);

        return method is null
            ? throw ERR_UNEXPECTED_TOKEN(token, $"Method '{token.Value.ToUpper()}' does not exist or contains invalid parameters")
            : BsonExpression.Call(method, parameters.ToArray());
    }

    /// <summary>
    /// Parse JSON-Path - return null if not method call
    /// </summary>
    private static BsonExpression? TryParsePath(Tokenizer tokenizer, bool root)
    {
        // test $ or @ or WORD
        if (tokenizer.Current.Type != TokenType.At && tokenizer.Current.Type != TokenType.Dollar && tokenizer.Current.Type != TokenType.Word) return null;

        var defaultScope = (root ? TokenType.Dollar : TokenType.At);

        if (tokenizer.Current.Type == TokenType.At || tokenizer.Current.Type == TokenType.Dollar)
        {
            defaultScope = tokenizer.Current.Type;

            var ahead = tokenizer.LookAhead(false);

            if (ahead.Type == TokenType.Period)
            {
                tokenizer.ReadToken(); // read .
                tokenizer.ReadToken(); // read word or [
            }
            else
            {
                // test if is end of expression
                if (ahead.Type == TokenType.EOF || ahead.Type == TokenType.SemiColon)
                    tokenizer.ReadToken();

                return BsonExpression.Root;
            }
        }

        // get root/current expression
        var scope = defaultScope == TokenType.Dollar ? BsonExpression.Root : BsonExpression.Current;

        // read field name
        var field = ReadField(tokenizer);

        var expr = BsonExpression.Path(scope, field);

        // keep reading full path/array navigation
        while (true)
        {
            // checks if next token is a period (new path) or open bracket (array filter)
            var next = tokenizer.LookAhead();

            // if contains more path navigation, 
            if (next.Type == TokenType.Period || next.Type == TokenType.OpenBracket)
            {
                expr = ParsePathArrayNavigation(expr, tokenizer);
            }
            else if (next.Type == TokenType.EOF)
            {
                tokenizer.ReadToken(); // read eof
                break;
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    /// <summary>
    /// Read path/array navigation after first field.
    /// </summary>
    private static BsonExpression ParsePathArrayNavigation(BsonExpression source, Tokenizer tokenizer)
    {
        var token = tokenizer.ReadToken(); // read . or [

        // sub document path
        if (token.Type == TokenType.Period)
        {
            tokenizer.ReadToken(); // read word or [

            var field = ReadField(tokenizer);

            return BsonExpression.Path(source, field);
        }
        // array navigation
        else
        {
            // keeping support for $.items[*] (return same $.items)
            if (tokenizer.LookAhead().Type == TokenType.Asterisk)
            {
                tokenizer.ReadToken(); // read *
                tokenizer.ReadToken().Expect(TokenType.CloseBracket); // read ]

                return source;
            }

            // get filter selector or fixed array index
            var selector = ParseFullExpression(tokenizer, false);

            tokenizer.ReadToken().Expect(TokenType.CloseBracket); // read close ]

            //TODO: verificar se AND/OR não deveriam entrar aqui tambem
            if (selector.Type.IsPredicate())
            {
                return BsonExpression.Filter(source, selector);
            }
            else
            {
                return BsonExpression.ArrayIndex(source, selector);
            }
        }
    }

    /// <summary>
    /// Get field from simple \w regex or ['comp-lex'] - also, add into source. Can read empty field (root)
    /// </summary>
    private static string ReadField(Tokenizer tokenizer)
    {
        var field = "";

        // if field are complex
        if (tokenizer.Current.Type == TokenType.OpenBracket)
        {
            field = tokenizer.ReadToken().Expect(TokenType.String).Value;
            tokenizer.ReadToken().Expect(TokenType.CloseBracket);
        }
        else if (tokenizer.Current.Type == TokenType.Word)
        {
            field = tokenizer.Current.Value;
        }

        return field;
    }

    /// <summary>
    /// Read key in document definition with single word or "comp-lex"
    /// </summary>
    public static string ReadKey(Tokenizer tokenizer)
    {
        var token = tokenizer.ReadToken();

        if (token.Type == TokenType.String)
        {
            return token.Value;
        }
        else
        {
            return token.Expect(TokenType.Word, TokenType.Int).Value;
        }
    }

    /// <summary>
    /// Read next token as an operant - returns null if next token are not an operant
    /// </summary>
    private static string? ReadOperant(Tokenizer tokenizer)
    {
        var token = tokenizer.LookAhead(true);

        var isCondition = token.Type == TokenType.Question || token.Type == TokenType.Colon;

        if (isCondition || _operators.Keys.Contains(token.Value))
        {
            tokenizer.ReadToken(); // consume operant

            return token.Value;
        }

        return null;
    }
}