using System;
using System.IO;
using System.Text;

namespace LiteDB
{
    #region TokenType definition

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
        /// <summary> ; </summary>
        Colon,
        /// <summary> @ </summary>
        At,
        /// <summary> . </summary>
        Period,
        /// <summary> $ </summary>
        Dollar,
        /// <summary> `=` `&gt;` `&lt;` `!=` `&gt;=` `&lt;=` `+` `-` `*` `/` `\` `%` `BETWEEN` </summary>
        Operator,
        String,
        Number,
        Word,
        EOF
    }

    #endregion

    #region Token definition

    /// <summary>
    /// Represent a single token
    /// </summary>
    internal class Token
    {
        public Token(TokenType tokenType, string value, long position)
        {
            this.Position = position;
            this.Value = value;
            this.TokenType = tokenType;
        }

        public TokenType TokenType { get; private set; }
        public string Value { get; private set; }
        public long Position { get; private set; }

        public Token Expect(TokenType type)
        {
            if (this.TokenType != type)
            {
                throw LiteException.UnexpectedToken(this);
            }

            return this;
        }

        public Token Expect(TokenType type1, TokenType type2)
        {
            if (this.TokenType != type1 && this.TokenType != type2)
            {
                throw LiteException.UnexpectedToken(this);
            }

            return this;
        }
    }

    #endregion

    /// <summary>
    /// Class to tokenize TextReader input used in JsonRead/BsonExpressions
    /// </summary>
    internal class Tokenizer
    {
        private char _current = '\0';
        private TextReader _reader;

        public bool EOF { get; private set; }
        public long Position { get; private set; }

        public Tokenizer(TextReader reader)
        {
            _reader = reader;
            this.Position = 0;
            this.Read();
        }

        /// <summary>
        /// Read next char in stream and set in _current
        /// </summary>
        private char Read()
        {
            if (this.EOF) return '\0';

            var c = _reader.Read();

            this.Position++;

            if (c == -1)
            {
                _current = '\0';
                this.EOF = true;
            }

            _current = (char)c;

            return _current;
        }

        /// <summary>
        /// Read next token (do not read operators tokens)
        /// </summary>
        public Token ReadToken(bool eatWhitespace = true)
        {
            // remove whitespace before get next token
            if (eatWhitespace)
            {
                this.EatWhitespace();
            }

            if (this.EOF)
            {
                return new Token(TokenType.EOF, null, this.Position);
            }

            Token token = null;

            switch (_current)
            {
                case '[':
                    token = new Token(TokenType.OpenBracket, "[", this.Position);
                    this.Read();
                    break;

                case ']':
                    token = new Token(TokenType.CloseBracket, "]", this.Position);
                    this.Read();
                    break;

                case '{':
                    token = new Token(TokenType.OpenBrace, "{", this.Position);
                    this.Read();
                    break;

                case '}':
                    token = new Token(TokenType.CloseBrace, "}", this.Position);
                    this.Read();
                    break;

                case '(':
                    token = new Token(TokenType.OpenParenthesis, "(", this.Position);
                    this.Read();
                    break;

                case ')':
                    token = new Token(TokenType.CloseParenthesis, ")", this.Position);
                    this.Read();
                    break;

                case ',':
                    token = new Token(TokenType.Comma, ",", this.Position);
                    this.Read();
                    break;

                case ':':
                    token = new Token(TokenType.Colon, ":", this.Position);
                    this.Read();
                    break;

                case '.':
                    token = new Token(TokenType.Period, ".", this.Position);
                    this.Read();
                    break;

                case '@':
                    token = new Token(TokenType.At, "@", this.Position);
                    this.Read();
                    break;

                case '$':
                    token = new Token(TokenType.Dollar, "$", this.Position);
                    this.Read();
                    break;

                case '\"':
                case '\'':
                    token = new Token(TokenType.String, this.ReadString(_current), this.Position);
                    break;

                case '-':
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
                    token = new Token(TokenType.Number, this.ReadNumber(), this.Position);
                    break;

                default:
                    token = new Token(TokenType.Word, this.ReadWord(), this.Position);
                    break;
            }

            return token;
        }

        /// <summary>
        /// Read next operator (do not read any other token type)
        /// </summary>
        public Token ReadOperator()
        {
            // remove whitespace before get next token
            this.EatWhitespace();

            if (this.EOF)
            {
                return new Token(TokenType.EOF, null, this.Position);
            }

            Token token = null;

            switch (_current)
            {
                case '+':
                case '-':
                case '*':
                case '\\':
                case '/':
                case '%':
                    token = new Token(TokenType.Operator, _current.ToString(), this.Position);
                    this.Read();
                    break;

                case '>':
                case '<':
                    var op = _current.ToString();
                    this.Read();
                    if (_current == '=')
                    {
                        op += "=";
                        this.Read();
                    }
                    token = new Token(TokenType.Operator, op, this.Position);
                    break;

                default:
                    var word = this.ReadWord();

                    if (word.Equals("BETWEEN", StringComparison.OrdinalIgnoreCase))
                    {
                        token = new Token(TokenType.Operator, "BETWEEN", this.Position);
                    }
                    else
                    {
                        throw LiteException.UnexpectedToken(this.Position, "Unexpected token: invalid operator");
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
            while (char.IsWhiteSpace(_current) && !this.EOF)
            {
                this.Read();
            }
        }

        /// <summary>
        /// Read a word without "
        /// </summary>
        private string ReadWord()
        {
            var sb = new StringBuilder();
            sb.Append(_current);

            this.Read();

            while (!this.EOF &&
                (char.IsLetterOrDigit(_current) || _current == '_' || _current == '$'))
            {
                sb.Append(_current);
                this.Read();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Read a number - it's accepts all number char, but not validate. When run Convert, .NET will check if number is correct
        /// </summary>
        private string ReadNumber()
        {
            var sb = new StringBuilder();
            sb.Append(_current);

            this.Read();

            while (!this.EOF &&
                (char.IsDigit(_current) || _current == '+' || _current == '-' || _current == '.' || _current == 'e' || _current == 'E'))
            {
                sb.Append(_current);
                this.Read();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Read a string removing open and close " or '
        /// </summary>
        private string ReadString(char quote)
        {
            var sb = new StringBuilder();
            this.Read(); // remove first " or '

            while (_current != quote && !this.EOF)
            {
                if (_current == '\\')
                {
                    this.Read();

                    switch (_current)
                    {
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            var codePoint = ParseUnicode(this.Read(), this.Read(), this.Read(), this.Read());
                            sb.Append((char)codePoint);
                            break;
                    }
                }
                else
                {
                    sb.Append(_current);
                }

                this.Read();
            }

            this.Read(); // read last " or '

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
    }
}