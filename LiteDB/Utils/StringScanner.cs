using System;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    /// <summary>
    /// A StringScanner is state machine used in text parsers based on regular expressions
    /// </summary>
    public class StringScanner
    {
        public string Source { get; private set; }
        public int Index { get; set; }

        /// <summary>
        /// Initialize scanner with a string to be parsed
        /// </summary>
        public StringScanner(string source)
        {
            this.Source = source;
            this.Index = 0;
        }

        public override string ToString()
        {
            return this.HasTerminated ? "<EOF>" : this.Source.Substring(this.Index);
        }

        /// <summary>
        /// Reset cursor position
        /// </summary>
        public void Reset()
        {
            this.Index = 0;
        }

        /// <summary>
        /// Skip cursor position in string source
        /// </summary>
        public void Seek(int length)
        {
            this.Index += length;
        }

        /// <summary>
        /// Read current char an move to next
        /// </summary>
        public char Current()
        {
            var c = this.Source.Substring(this.Index, 1);

            this.Index++;

            return c.ToCharArray()[0];
        }

        /// <summary>
        /// Indicate that cursor is EOF
        /// </summary>
        public bool HasTerminated
        {
            get { return this.Index >= this.Source.Length; }
        }

        #region Scan Method

        /// <summary>
        /// Scan in current cursor position for this patterns. If found, returns string and run with cursor
        /// </summary>
        public string Scan(string pattern)
        {
            return this.Scan(Create(pattern), 0);
        }

        /// <summary>
        /// Scan in current cursor position for this patterns. If found, returns string and run with cursor
        /// </summary>
        public string Scan(Regex regex)
        {
            return this.Scan(regex, 0);
        }

        /// <summary>
        /// Scan pattern and returns group string index 1 based
        /// </summary>
        public string Scan(string pattern, int group)
        {
            return this.Scan(Create(pattern), group);
        }

        /// <summary>
        /// Scan in current cursor position for this patterns. Returns true if found and update output value parameter
        /// </summary>
        public bool Scan(string pattern, out string value)
        {
            value = this.Scan(Create(pattern), 0);

            return value.Length > 0;
        }

        /// <summary>
        /// Scan in current cursor position for this patterns. Returns true if found and update output value parameter with group based
        /// </summary>
        public bool Scan(string pattern, int group, out string value)
        {
            value = this.Scan(Create(pattern), group);

            return value.Length > 0;
        }

        /// <summary>
        /// Scan pattern and returns group string index 1 based. If group = 0, return all match
        /// </summary>
        public string Scan(Regex regex, int group)
        {
            var match = regex.Match(this.Source, this.Index, this.Source.Length - this.Index);

            if (match.Success)
            {
                this.Index += match.Length;

                return 
                    group == 0 ? match.Value :
                    group >= match.Groups.Count ? "" : match.Groups[group].Value;
            }
            else
            {
                return "";
            }
        }

        #endregion

        #region Match

        /// <summary>
        /// Match if pattern is true in current cursor position. Do not change cursor position
        /// </summary>
        public bool Match(string pattern)
        {
            return this.Match(new Regex((pattern.StartsWith("^") ? "" : "^") + pattern, RegexOptions.IgnorePatternWhitespace));
        }

        /// <summary>
        /// Match if pattern is true in current cursor position. Do not change cursor position
        /// </summary>
        public bool Match(Regex regex)
        {
            var match = regex.Match(this.Source, this.Index, this.Source.Length - this.Index);
            return match.Success;
        }

        #endregion

        /// <summary>
        /// Read string until finish - must be consume first quote (single or double)
        /// </summary>
        public string ReadString(char quote)
        {
            var current = this.Current();
            var sb = new StringBuilder();

            while (current != quote && !this.HasTerminated)
            {
                if (current == '\\')
                {
                    current = this.Current();

                    if (current == quote)
                    {
                        sb.Append(quote);
                    }
                    else
                    {
                        switch (current)
                        {
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                var codePoint = Tokenizer.ParseUnicode(this.Current(), this.Current(), this.Current(), this.Current());
                                sb.Append((char)codePoint);
                                break;
                        }
                    }
                }
                else
                {
                    sb.Append(current);
                }

                current = this.Current();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Create (or get from cache) regular expression from an string
        /// </summary>
        public static Regex Create(string pattern)
        {
            return new Regex((pattern.StartsWith("^") ? "" : "^") + pattern, RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase);
        }
    }
}