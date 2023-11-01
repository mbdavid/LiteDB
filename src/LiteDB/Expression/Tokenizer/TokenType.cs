namespace LiteDB;

/// <summary>
/// ASCII char names: https://www.ascii.cl/htmlcodes.htm
/// </summary>
internal enum TokenType
{
    /// <summary> { </summary>
    OpenBrace,
    /// <summary> } </summary>
    CloseBrace,
    /// <summary> [ </summary>
    OpenBracket,
    /// <summary> ] </summary>
    CloseBracket,
    /// <summary> ( </summary>
    OpenParenthesis,
    /// <summary> ) </summary>
    CloseParenthesis,
    /// <summary> , </summary>
    Comma,
    /// <summary> : </summary>
    Colon,
    /// <summary> ; </summary>
    SemiColon,
    /// <summary> =&gt; </summary>
    Arrow,
    /// <summary> @ </summary>
    At,
    /// <summary> # </summary>
    Hashtag,
    /// <summary> ~ </summary>
    Til,
    /// <summary> . </summary>
    Period,
    /// <summary> &amp; </summary>
    Ampersand,
    /// <summary> ? </summary>
    Question,
    /// <summary> $ </summary>
    Dollar,
    /// <summary> ! </summary>
    Exclamation,
    /// <summary> != </summary>
    NotEquals,
    /// <summary> = </summary>
    Equals,
    /// <summary> &gt; </summary>
    Greater,
    /// <summary> &gt;= </summary>
    GreaterOrEquals,
    /// <summary> &lt; </summary>
    Less,
    /// <summary> &lt;= </summary>
    LessOrEquals,
    /// <summary> - </summary>
    Minus,
    /// <summary> + </summary>
    Plus,
    /// <summary> * </summary>
    Asterisk,
    /// <summary> / </summary>
    Slash,
    /// <summary> \ </summary>
    Backslash,
    /// <summary> % </summary>
    Percent,
    /// <summary> "..." or '...' </summary>
    String,
    /// <summary> [0-9]+ </summary>
    Int,
    /// <summary> [0-9]+.[0-9] </summary>
    Double,
    /// <summary> \n\r\t \u0032 </summary>
    Whitespace,
    /// <summary> [a-Z_$]+[a-Z0-9_$] </summary>
    Word,
    EOF,
    Unknown,

    /// <summary> Uninitalized token </summary>
    Empty
}
