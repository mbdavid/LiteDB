namespace LiteDB;

/// <summary>
/// Class to tokenize TextReader input used in JsonRead
/// This class are not thread safe
/// </summary>
internal class JsonTokenizer
{
    private TextReader _reader;
    private char _char = '\0';
    private JsonToken? _current = null;
    private bool _eof = false;
    private long _position = 0;

    public bool EOF => _eof;
    public long Position => _position;
    public JsonToken Current => _current.GetValueOrDefault();

    public JsonTokenizer(TextReader reader)
    {
        _reader = reader;
        _position = 0;
        this.ReadChar();
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

        var c = _reader.Read();

        _position++;

        if (c == -1)
        {
            _char = '\0';
            _eof = true;
        }
        else
        {
            _char = (char)c;
        }

        return _char;
    }

    /// <summary>
    /// Read next token from reader
    /// </summary>
    public JsonToken ReadToken(bool eatWhitespace = true)
    {
        // remove whitespace before get next token
        if (eatWhitespace)
        {
            while (char.IsWhiteSpace(_char) && !_eof)
            {
                this.ReadChar();
            }
        }

        if (_eof)
        {
            return new JsonToken(JsonTokenType.EOF, "", _position);
        }

        JsonToken? token = null;

        switch (_char)
        {
            case '{':
                token = new JsonToken(JsonTokenType.OpenBrace, "{", _position);
                this.ReadChar();
                break;

            case '}':
                token = new JsonToken(JsonTokenType.CloseBrace, "}", _position);
                this.ReadChar();
                break;

            case '[':
                token = new JsonToken(JsonTokenType.OpenBracket, "[", _position);
                this.ReadChar();
                break;

            case ']':
                token = new JsonToken(JsonTokenType.CloseBracket, "]", _position);
                this.ReadChar();
                break;

            case ',':
                token = new JsonToken(JsonTokenType.Comma, ",", _position);
                this.ReadChar();
                break;

            case ':':
                token = new JsonToken(JsonTokenType.Colon, ":", _position);
                this.ReadChar();
                break;

            case '.':
                token = new JsonToken(JsonTokenType.Period, ".", _position);
                this.ReadChar();
                break;

            case '-':
                this.ReadChar();
                if (_char == '-')
                {
                    // remove all char until new line
                    while (_char != '\n' && !_eof)
                    {
                        this.ReadChar();
                    }
                    if (_char == '\n') this.ReadChar();
                    token = this.ReadToken(eatWhitespace);
                }
                else
                {
                    token = new JsonToken(JsonTokenType.Minus, "-", _position);
                }
                break;

            case '+':
                token = new JsonToken(JsonTokenType.Plus, "+", _position);
                this.ReadChar();
                break;

            case '*':
                token = new JsonToken(JsonTokenType.Asterisk, "*", _position);
                this.ReadChar();
                break;

            case '/':
                token = new JsonToken(JsonTokenType.Slash, "/", _position);
                this.ReadChar();
                break;
            case '\\':
                token = new JsonToken(JsonTokenType.Backslash, @"\", _position);
                this.ReadChar();
                break;

            case '\"':
            case '\'':
                token = new JsonToken(JsonTokenType.String, this.ReadString(_char), _position);
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
                token = new JsonToken(dbl ? JsonTokenType.Double : JsonTokenType.Int, number, _position);
                break;

            case ' ':
            case '\n':
            case '\r':
            case '\t':
                var sb = new StringBuilder();
                while (char.IsWhiteSpace(_char) && !_eof)
                {
                    sb.Append(_char);
                    this.ReadChar();
                }
                token = new JsonToken(JsonTokenType.Whitespace, sb.ToString(), _position);
                break;

            default:
                // test if first char is an word 
                if (IsWordChar(_char, true))
                {
                    token = new JsonToken(JsonTokenType.Word, this.ReadWord(), _position);
                }
                else
                {
                    this.ReadChar();
                }
                break;
        }
        _current = token ?? new JsonToken(JsonTokenType.Unknown, _char.ToString(), _position);

        return _current.GetValueOrDefault();
    }

    /// <summary>
    /// Read a word (word = [\w$]+)
    /// </summary>
    private string ReadWord()
    {
        var sb = new StringBuilder();
        sb.Append(_char);

        this.ReadChar();

        while (!_eof && IsWordChar(_char, false))
        {
            sb.Append(_char);
            this.ReadChar();
        }

        return sb.ToString();
    }


    /// <summary>
    /// Read a number - it's accepts all number char, but not validate. When run Convert, .NET will check if number is correct
    /// </summary>
    private string ReadNumber(ref bool dbl)
    {
        var sb = new StringBuilder();
        sb.Append(_char);

        var canDot = false;
        var canE = true;
        var canSign = false;

        this.ReadChar();

        while (!_eof &&
            (char.IsDigit(_char) || _char == '+' || _char == '-' || _char == '.' || _char == 'e' || _char == 'E'))
        {
            if (_char == '.')
            {
                if (canDot == true) break;
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

            sb.Append(_char);
            this.ReadChar();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Read a string removing open and close " or '
    /// </summary>
    private string ReadString(char quote)
    {
        var sb = new StringBuilder();
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

        return sb.ToString();
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
        return $"{{ Current = {_current}, Position = {_position} }}";
    }
}
