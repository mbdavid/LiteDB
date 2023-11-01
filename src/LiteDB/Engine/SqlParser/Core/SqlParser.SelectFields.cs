namespace LiteDB.Engine;

internal partial class SqlParser
{
    /// <summary>
    /// select-fields::
    ///   | "*" 
    ///   | root
    ///   | expr_document 
    ///   | select-named-fields
    /// </summary>
    private SelectFields ParseSelectFields()
    {
        var ahead = _tokenizer.LookAhead();

        if (ahead.Type == TokenType.Dollar || ahead.Type == TokenType.Asterisk) // look for * or $
        {
            _tokenizer.ReadToken();

            return new SelectFields(BsonExpression.Root); // full document read
        }
        
        if (ahead.Type == TokenType.OpenBrace) // look for { 
        {
            var expr = BsonExpression.Create(_tokenizer, false);

            return new SelectFields(expr);
        }

        return ParseSelectNamedFields();
    }

    /// <summary>
    /// select-named-fields::
    ///   select-named-field [ . "," . select-named-field ]*
    /// </summary>
    private SelectFields ParseSelectNamedFields()
    {
        var fields = new List<SelectField>();

        var field = this.ParseSelectNamedField(); // read first expression/name

        fields.Add(field);

        var ahead = _tokenizer.LookAhead();

        while(ahead.Type == TokenType.Comma)
        {
            _tokenizer.ReadToken(); // read ","

            var next = this.ParseSelectNamedField(); // read next expression/name

            fields.Add(next);

            ahead = _tokenizer.LookAhead();
        }

        return new SelectFields(fields);
    }

    /// <summary>
    /// select-named-field::
    ///   expr_single [ [ _ "AS"] _ (word | json_string) ]
    /// </summary>
    private SelectField ParseSelectNamedField()
    {
        var expr = BsonExpression.Create(_tokenizer, true);

        var ahead = _tokenizer.LookAhead();
        string name;
        var hidden = false;

        if (ahead.Value.Eq("AS"))
        {
            _tokenizer.ReadToken(); // read AS

            var token = _tokenizer.ReadToken();

            if (token.Type == TokenType.Hashtag) // check for # hidden column
            {
                token = _tokenizer.ReadToken(); 
                hidden = true;
            }

            name = token
                .Expect(TokenType.Word, TokenType.String)
                .Value;
        }
        else
        {
            if (expr is PathBsonExpression path)
            {
                name = path.Field;
            }
            else
            {
                name = "expr" + (++_nameIndex);
            }
        }

        return new SelectField(name, hidden, expr);
    }
}
