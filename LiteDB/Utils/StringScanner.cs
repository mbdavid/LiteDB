using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    public class StringScanner
    {
        public string Source { get; private set; }
        public int Index { get; private set; }

        public StringScanner(string source)
        {
            this.Source = source;
            this.Index = 0;
        }

        public override string ToString()
        {
            return this.HasTerminated ? "<EOF>" : this.Source.Substring(this.Index);
        }

        public void Reset()
        {
            this.Index = 0;
        }

        public bool HasTerminated
        {
            get { return this.Index >= this.Source.Length; }
        }

        public string Scan(string pattern)
        {
            return this.Scan(new Regex((pattern.StartsWith("^") ? "" : "^") + pattern, RegexOptions.IgnorePatternWhitespace));
        }

        public string Scan(Regex regex)
        {
            var match = regex.Match(this.Source, this.Index, this.Source.Length - this.Index);

            if (match.Success)
            {
                this.Index += match.Length;
                return match.Value;
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Scan pattern and returns group string index 1 based
        /// </summary>
        public string Scan(string pattern, int group)
        {
            return this.Scan(new Regex((pattern.StartsWith("^") ? "" : "^") + pattern, RegexOptions.IgnorePatternWhitespace), group);
        }

        public string Scan(Regex regex, int group)
        {
            var match = regex.Match(this.Source, this.Index, this.Source.Length - this.Index);

            if (match.Success)
            {
                this.Index += match.Length;
                return group >= match.Groups.Count ? "" : match.Groups[group].Value;
            }
            else
            {
                return string.Empty;
            }
        }

        public string ScanUntil(string pattern)
        {
            return this.ScanUntil(new Regex(pattern, RegexOptions.IgnorePatternWhitespace));
        }

        public string ScanUntil(Regex regex)
        {
            var match = regex.Match(this.Source, this.Index, this.Source.Length - this.Index);

            if (match.Success)
            {
                this.Index += match.Value.Length;
                return match.Value;
            }
            else
            {
                this.Index = this.Source.Length; // go to the end of string
                return string.Empty;
            }
        }

        public bool Match(string pattern)
        {
            return this.Match(new Regex((pattern.StartsWith("^") ? "" : "^") + pattern, RegexOptions.IgnorePatternWhitespace));
        }

        public bool Match(Regex regex)
        {
            var match = regex.Match(this.Source, this.Index, this.Source.Length - this.Index);
            return match.Success;
        }
    }
}
