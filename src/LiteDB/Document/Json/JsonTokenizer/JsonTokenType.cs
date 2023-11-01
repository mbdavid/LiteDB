namespace LiteDB;

internal enum JsonTokenType
{
    /// <summary> { </summary>
    OpenBrace,
    /// <summary> } </summary>
    CloseBrace,
    /// <summary> [ </summary>
    OpenBracket,
    /// <summary> ] </summary>
    CloseBracket,
    /// <summary> , </summary>
    Comma,
    /// <summary> : </summary>
    Colon,
    /// <summary> ; </summary>
    SemiColon,
    /// <summary> . </summary>
    Period,
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
    Unknown
}