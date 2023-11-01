namespace LiteDB;

/// <summary>
/// Class to tokenize TextReader input used in JsonRead/BsonExpressions
/// This class are not thread safe
/// </summary>
internal class Tokenizer
{
    private readonly ReadOnlyMemory<char> _source;

    private int _position = 0;
    private char _char = '\0';

    private Token _current = Token.Empty;
    private Token _ahead = Token.Empty;

    private bool _eof = false;

    public bool EOF => _eof && _ahead.IsEmpty;
    public int Position => _position;
    public Token Current => _current;

    public Tokenizer(string source)
    {
        _source = source.AsMemory();
        _position = 0;

        this.ReadChar();
    }

    /// <summary>
    /// If EOF throw an invalid token exception (used in while()) otherwise return "false" (not EOF)
    /// </summary>
    public bool CheckEOF()
    {
        if (_eof) throw ERR_UNEXPECTED_TOKEN(this.Current);

        return false;
    }

    /// <summary>
    /// Checks if char is an valid part of a word [a-Z_]+[a-Z0-9_$]*
    /// </summary>
    public static bool IsWordChar(char c, bool first)
    {
        if (first)
        {
            return char.IsLetter(c) || c == '_' || c == '$';
        }

        return char.IsLetterOrDigit(c) || c == '_' || c == '$';
    }

    /// <summary>
    /// Read next char in stream and set in _current
    /// </summary>
    private char ReadChar()
    {
        if (_eof) return '\0';

        if (_position >= _source.Length)
        {
            _eof = true;
            _position++;
            return _char = '\0';
        }

        _char = _source.Span[_position];

        _position++;

        return _char;
    }

    /// <summary>
    /// Look for next token but keeps in buffer when run "ReadToken()" again.
    /// </summary>
    public Token LookAhead(bool eatWhitespace = true)
    {
        if (_ahead.IsEmpty == false)
        {
            if (eatWhitespace && _ahead.Type == TokenType.Whitespace)
            {
                _ahead = this.ReadNext(eatWhitespace);
            }

            return _ahead;
        }

        return _ahead = this.ReadNext(eatWhitespace);
    }

    /// <summary>
    /// Read next token (or from ahead buffer).
    /// </summary>
    public Token ReadToken(bool eatWhitespace = true)
    {
        if (_ahead.IsEmpty)
        {
            return _current = this.ReadNext(eatWhitespace);
        }

        if (eatWhitespace && _ahead.Type == TokenType.Whitespace)
        {
            _ahead = this.ReadNext(eatWhitespace);
        }

        _current = _ahead;
        _ahead = Token.Empty;

        return _current;
    }

    /// <summary>
    /// Read next token from reader
    /// </summary>
    private Token ReadNext(bool eatWhitespace)
    {
        // remove whitespace before get next token
        if (eatWhitespace) this.EatWhitespace();

        if (_eof)
        {
            return new Token(TokenType.EOF, _position);
        }

        Token token = default;
        var start = _position; // get start position 

        switch (_char)
        {
            case '{':
                token = new Token(TokenType.OpenBrace, start);
                this.ReadChar();
                break;

            case '}':
                token = new Token(TokenType.CloseBrace, start);
                this.ReadChar();
                break;

            case '[':
                token = new Token(TokenType.OpenBracket, start);
                this.ReadChar();
                break;

            case ']':
                token = new Token(TokenType.CloseBracket, start);
                this.ReadChar();
                break;

            case '(':
                token = new Token(TokenType.OpenParenthesis, start);
                this.ReadChar();
                break;

            case ')':
                token = new Token(TokenType.CloseParenthesis, start);
                this.ReadChar();
                break;

            case ',':
                token = new Token(TokenType.Comma, start);
                this.ReadChar();
                break;

            case ':':
                token = new Token(TokenType.Colon, start);
                this.ReadChar();
                break;

            case ';':
                token = new Token(TokenType.SemiColon, start);
                this.ReadChar();
                break;

            case '@':
                token = new Token(TokenType.At, start);
                this.ReadChar();
                break;

            case '#':
                token = new Token(TokenType.Hashtag, start);
                this.ReadChar();
                break;

            case '~':
                token = new Token(TokenType.Til, start);
                this.ReadChar();
                break;

            case '.':
                token = new Token(TokenType.Period, start);
                this.ReadChar();
                break;

            case '&':
                token = new Token(TokenType.Ampersand, start);
                this.ReadChar();
                break;

            case '?':
                token = new Token(TokenType.Question, start);
                this.ReadChar();
                break;

            case '$':
                this.ReadChar();
                if (IsWordChar(_char, true))
                {
                    token = new Token(TokenType.Word, this.ReadWord(-2), start);
                }
                else
                {
                    token = new Token(TokenType.Dollar, start);
                }
                break;

            case '!':
                this.ReadChar();
                if (_char == '=')
                {
                    token = new Token(TokenType.NotEquals, start);
                    this.ReadChar();
                }
                else
                {
                    token = new Token(TokenType.Exclamation, start);
                }
                break;

            case '=':
                this.ReadChar();
                if (_char == '>')
                {
                    token = new Token(TokenType.Arrow, start);
                    this.ReadChar();
                }
                else
                {
                    token = new Token(TokenType.Equals, start);
                }
                break;

            case '>':
                this.ReadChar();
                if (_char == '=')
                {
                    token = new Token(TokenType.GreaterOrEquals, start);
                    this.ReadChar();
                }
                else
                {
                    token = new Token(TokenType.Greater, start);
                }
                break;

            case '<':
                this.ReadChar();
                if (_char == '=')
                {
                    token = new Token(TokenType.LessOrEquals, start);
                    this.ReadChar();
                }
                else
                {
                    token = new Token(TokenType.Less, start);
                }
                break;

            case '-':
                this.ReadChar();
                if (_char == '-')
                {
                    this.ReadLine(); // comment (discard token)
                    token = this.ReadNext(eatWhitespace);
                }
                else
                {
                    token = new Token(TokenType.Minus, start);
                }
                break;

            case '+':
                token = new Token(TokenType.Plus, start);
                this.ReadChar();
                break;

            case '*':
                token = new Token(TokenType.Asterisk, start);
                this.ReadChar();
                break;

            case '/':
                token = new Token(TokenType.Slash, start);
                this.ReadChar();
                break;
            case '\\':
                token = new Token(TokenType.Backslash, start);
                this.ReadChar();
                break;

            case '%':
                token = new Token(TokenType.Percent, start);
                this.ReadChar();
                break;

            case '\"':
            case '\'':
                token = new Token(TokenType.String, this.ReadString(_char), start);
                break;

            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                var dbl = false;
                var number = this.ReadNumber(ref dbl);
                token = new Token(dbl ? TokenType.Double : TokenType.Int, number, start);
                break;

            case ' ':
            case '\n':
            case '\r':
            case '\t':
                while(char.IsWhiteSpace(_char) && !_eof)
                {
                    this.ReadChar();
                }
                token = new Token(TokenType.Whitespace, start, _position - start);
                break;

            default:
                // test if first char is an word 
                if (IsWordChar(_char, true))
                {
                    token = new Token(TokenType.Word, this.ReadWord(-1), start);
                }
                else
                {
                    this.ReadChar();
                }
                break;
        }

        return token;
    }

    /// <summary>
    /// Eat all whitespace - used before a valid token
    /// </summary>
    private void EatWhitespace()
    {
        while (char.IsWhiteSpace(_char) && !_eof)
        {
            this.ReadChar();
        }
    }

    /// <summary>
    /// Read a word (word = [\w$]+)
    /// </summary>
    private ReadOnlyMemory<char> ReadWord(int offset)
    {
        var start = _position + offset;

        this.ReadChar();

        while (!_eof && IsWordChar(_char, false))
        {
            this.ReadChar();
        }

        return _source[start..(_position - 1)];
    }

    /// <summary>
    /// Read a number - it's accepts all number char, but not validate. When run Convert, .NET will check if number is correct
    /// </summary>
    private ReadOnlyMemory<char> ReadNumber(ref bool dbl)
    {
        var canDot = true;
        var canE = true;
        var canSign = false;
        var start = _position - 1; // already read first position

        this.ReadChar();

        while (!_eof &&
            (char.IsDigit(_char) || _char == '+' || _char == '-' || _char == '.' || _char == 'e' || _char == 'E'))
        {
            if (_char == '.')
            {
                if (canDot == false) break;
                dbl = true;
                canDot = false;
            }
            else if (_char == 'e' || _char == 'E')
            {
                if (canE == false) break;
                canE = false;
                canSign = true;
                dbl = true;
            }
            else if (_char == '-' || _char == '+')
            {
                if (canSign == false) break;
                canSign = false;
            }

            this.ReadChar();
        }

        return _source[start..(_position - 1)];
    }
        
    /// <summary>
    /// Read a string removing open and close " or '
    /// </summary>
    private ReadOnlyMemory<char> ReadString(char quote)
    {
        var start = _position; // already read " or '

        // test if can re-use string from source (no escapes)
        var span = _source.Span[start..];

        var index = span.IndexOf(quote);

        if (index <= -1) throw ERR_UNEXPECTED_TOKEN(_current, quote.ToString());

        // test if contains any escape, use direct slice
        var escape = span[0..index].IndexOf('\\') >= 0;

        if (escape == false)
        {
            _position += index + 1;

            this.ReadChar();

            return _source.Slice(start, index);
        }

        // when has escape, must use stringbuilder to convert escape chars
        else
        {
            var sb = StringBuilderCache.Acquire();

            this.ReadChar(); // remove first " or '

            while (_char != quote && !_eof)
            {
                if (_char == '\\')
                {
                    this.ReadChar();

                    if (_char == quote) sb.Append(quote);

                    switch (_char)
                    {
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            var codePoint = ParseUnicode(this.ReadChar(), this.ReadChar(), this.ReadChar(), this.ReadChar());
                            sb.Append((char)codePoint);
                            break;
                    }
                }
                else
                {
                    sb.Append(_char);
                }

                this.ReadChar();
            }

            this.ReadChar(); // read last " or '

            return sb.ToString().AsMemory();
        }
    }

    /// <summary>
    /// Read all chars to end of LINE
    /// </summary>
    private void ReadLine()
    {
        // remove all char until new line
        while (_char != '\n' && !_eof)
        {
            this.ReadChar();
        }
        if (_char == '\n') this.ReadChar();
    }

    public static uint ParseUnicode(char c1, char c2, char c3, char c4)
    {
        uint p1 = ParseSingleChar(c1, 0x1000);
        uint p2 = ParseSingleChar(c2, 0x100);
        uint p3 = ParseSingleChar(c3, 0x10);
        uint p4 = ParseSingleChar(c4, 1);

        return p1 + p2 + p3 + p4;
    }

    public static uint ParseSingleChar(char c1, uint multiplier)
    {
        uint p1 = 0;
        if (c1 >= '0' && c1 <= '9')
            p1 = (uint)(c1 - '0') * multiplier;
        else if (c1 >= 'A' && c1 <= 'F')
            p1 = (uint)((c1 - 'A') + 10) * multiplier;
        else if (c1 >= 'a' && c1 <= 'f')
            p1 = (uint)((c1 - 'a') + 10) * multiplier;
        return p1;
    }

    public override string ToString()
    {
        return Dump.Object(new { Current = _current, Ahead = _ahead, Position = _position, EOF, @Char = $"`{_char}`", EndContent = $"`{_source[_position..]}`" });
    }

}
