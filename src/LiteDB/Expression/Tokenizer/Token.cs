namespace LiteDB;

/// <summary>
/// Represent a single string token
/// </summary>
internal struct Token : IIsEmpty
{
    public static Token Empty = new (TokenType.Empty, Array.Empty<char>(), 0);

    private readonly TokenType _type;
    private readonly ReadOnlyMemory<char> _value;
    private readonly int _start;
    private readonly int _length;

    public bool IsEmpty => _type == TokenType.Empty;

    public Token(TokenType tokenType, int start, int length = 0)
        : this (tokenType, Array.Empty<char>(), start, length)
    {
    }

    public Token(TokenType tokenType, ReadOnlyMemory<char> value, int start, int length = 0)
    {
        _type = tokenType;
        _value = value;
        _start = start;

        _length = tokenType switch
        {
            TokenType.Word => value.Length,
            TokenType.String => value.Length,
            TokenType.Int => value.Length,
            TokenType.Double => value.Length,
            TokenType.Whitespace => length,

            TokenType.NotEquals => 2, // !=
            TokenType.GreaterOrEquals => 2, // >=
            TokenType.LessOrEquals => 2, // <=
            TokenType.EOF => 0, 
            TokenType.Empty => 0,
            _ => 1 // all other contains only 1 char
        };
    }

    public TokenType Type => _type;

    public int Position => _start;


    //public ReadOnlySpan<char> Value => _value.Span;
    public string Value =>
        this.Type switch
        {
            // fixed token value
            TokenType.CloseBrace => "}",
            TokenType.OpenBracket => "[",
            TokenType.CloseBracket => "]",
            TokenType.OpenParenthesis => "(",
            TokenType.CloseParenthesis => ")",
            TokenType.Comma => ",",
            TokenType.Colon => ":",
            TokenType.SemiColon => ";",
            TokenType.Arrow => "=",
            TokenType.At => "@",
            TokenType.Hashtag => "#",
            TokenType.Til => "~",
            TokenType.Period => ".",
            TokenType.Ampersand => "&",
            TokenType.Dollar => "$",
            TokenType.Exclamation => "!",
            TokenType.NotEquals => "!=",
            TokenType.Equals => "=",
            TokenType.Greater => ">",
            TokenType.GreaterOrEquals => ">=",
            TokenType.Less => "<",
            TokenType.LessOrEquals => "<=",
            TokenType.Minus => "-",
            TokenType.Plus => "+",
            TokenType.Asterisk => "*",
            TokenType.Slash => "/",
            TokenType.Backslash => "\\",
            TokenType.Percent => "%",
            TokenType.Question => "?",

            // convert to space
            TokenType.Whitespace => "".PadLeft(_length, ' '),

            // instanced value based 
            TokenType.Word or
            TokenType.String or
            TokenType.Int or
            TokenType.Double => _value.Span.ToString(),

            TokenType.EOF => "<EOF>",
            TokenType.Empty => "<EMPTY>",
            TokenType.Unknown => "<UNKNOWN>",

            _ => ""
        };

    #region Expects

    /// <summary>
    /// Expect if token is type (if not, throw UnexpectedToken)
    /// </summary>
    public Token Expect(TokenType type)
    {
        if (this.Type != type)
        {
            throw ERR_UNEXPECTED_TOKEN(this);
        }

        return this;
    }

    /// <summary>
    /// Expect for type1 OR type2 (if not, throw UnexpectedToken)
    /// </summary>
    public Token Expect(TokenType type1, TokenType type2)
    {
        if (this.Type != type1 && this.Type != type2)
        {
            throw ERR_UNEXPECTED_TOKEN(this);
        }

        return this;
    }

    /// <summary>
    /// Expect for type1 OR type2 OR type3 (if not, throw UnexpectedToken)
    /// </summary>
    public Token Expect(TokenType type1, TokenType type2, TokenType type3)
    {
        if (this.Type != type1 && this.Type != type2 && this.Type != type3)
        {
            throw ERR_UNEXPECTED_TOKEN(this);
        }

        return this;
    }

    public Token Expect(string value, bool ignoreCase = true)
    {
        if (!this.Match(value, ignoreCase))
        {
            throw ERR_UNEXPECTED_TOKEN(this, value);
        }

        return this;
    }

    public bool Match(ReadOnlySpan<char> value, bool ignoreCase = true)
    {
        return
            this.Type == TokenType.Word &&
            value.Equals(this.Value, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    #endregion

    #region IsType Check Properties

    /// <summary> { </summary>
    public bool IsOpenBrace => this.Type == TokenType.OpenBrace;
    /// <summary> } </summary>
    public bool IsCloseBrace => this.Type == TokenType.CloseBrace;
    /// <summary> [ </summary>
    public bool IsOpenBracket => this.Type == TokenType.OpenBracket;
    /// <summary> ] </summary>
    public bool IsCloseBracket => this.Type == TokenType.CloseBracket;
    /// <summary> ( </summary>
    public bool IsOpenParenthesis => this.Type == TokenType.OpenParenthesis;
    /// <summary> ) </summary>
    public bool IsCloseParenthesis => this.Type == TokenType.CloseParenthesis;
    /// <summary> , </summary>
    public bool IsComma => this.Type == TokenType.Comma;
    /// <summary> : </summary>
    public bool IsColon => this.Type == TokenType.Colon;
    /// <summary> ; </summary>
    public bool IsSemiColon => this.Type == TokenType.SemiColon;
    /// <summary> =&gt; </summary>
    public bool IsArrow => this.Type == TokenType.Arrow;
    /// <summary> @ </summary>
    public bool IsAt => this.Type == TokenType.At;
    /// <summary> # </summary>
    public bool IsHashtag => this.Type == TokenType.Hashtag;
    /// <summary> ~ </summary>
    public bool IsTil => this.Type == TokenType.Til;
    /// <summary> . </summary>
    public bool IsPeriod => this.Type == TokenType.Period;
    /// <summary> &amp; </summary>
    public bool IsAmpersand => this.Type == TokenType.Ampersand;
    /// <summary> $ </summary>
    public bool IsDollar => this.Type == TokenType.Dollar;
    /// <summary> ! </summary>
    public bool IsExclamation => this.Type == TokenType.Exclamation;
    /// <summary> != </summary>
    public bool IsNotEquals => this.Type == TokenType.NotEquals;
    /// <summary> = </summary>
    public bool IsEquals => this.Type == TokenType.Equals;
    /// <summary> &gt; </summary>
    public bool IsGreater => this.Type == TokenType.Greater;
    /// <summary> &gt;= </summary>
    public bool IsGreaterOrEquals => this.Type == TokenType.GreaterOrEquals;
    /// <summary> &lt; </summary>
    public bool IsLess => this.Type == TokenType.Less;
    /// <summary> &lt;= </summary>
    public bool IsLessOrEquals => this.Type == TokenType.LessOrEquals;
    /// <summary> - </summary>
    public bool IsMinus => this.Type == TokenType.Minus;
    /// <summary> + </summary>
    public bool IsPlus => this.Type == TokenType.Plus;
    /// <summary> * </summary>
    public bool IsAsterisk => this.Type == TokenType.Asterisk;
    /// <summary> / </summary>
    public bool IsSlash => this.Type == TokenType.Slash;
    /// <summary> \ </summary>
    public bool IsBackslash => this.Type == TokenType.Backslash;
    /// <summary> % </summary>
    public bool IsPercent => this.Type == TokenType.Percent;
    /// <summary> "..." or '...' </summary>
    public bool IsString => this.Type == TokenType.String;
    /// <summary> [0-9]+ </summary>
    public bool IsInt => this.Type == TokenType.Int;
    /// <summary> [0-9]+.[0-9] </summary>
    public bool IsDouble => this.Type == TokenType.Double;
    /// <summary> \n\r\t \u0032 </summary>
    public bool IsWhitespace => this.Type == TokenType.Whitespace;
    /// <summary> [a-Z_$]+[a-Z0-9_$] </summary>
    public bool IsWord => this.Type == TokenType.Word;
    public bool IsEOF => this.Type == TokenType.EOF;
    public bool IsUnknown => this.Type == TokenType.Unknown;

    #endregion

    public override string ToString()
    {
        return Dump.Object(new { Value, Type = _type, Start = _start, Length = _length });
    }
}
