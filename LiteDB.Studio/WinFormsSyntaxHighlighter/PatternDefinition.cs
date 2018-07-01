using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WinFormsSyntaxHighlighter
{
    public class PatternDefinition
    {
        private readonly Regex _regex;
        private ExpressionType _expressionType = ExpressionType.Identifier;
        private readonly bool _isCaseSensitive = true;

        public PatternDefinition(Regex regularExpression)
        {
            if (regularExpression == null)
                throw new ArgumentNullException("regularExpression");
            _regex = regularExpression;
        }

        public PatternDefinition(string regexPattern)
        {
            if (String.IsNullOrEmpty(regexPattern))
                throw new ArgumentException("regex pattern must not be null or empty", "regexPattern");

            _regex = new Regex(regexPattern, RegexOptions.Compiled);
        }

        public PatternDefinition(params string[] tokens)
            : this(false, tokens)
        {
        }

        public PatternDefinition(IEnumerable<string> tokens)
            : this(true, tokens)
        {
        }

        internal PatternDefinition(bool caseSensitive, IEnumerable<string> tokens)
        {
            if (tokens == null)
                throw new ArgumentNullException("tokens");

            _isCaseSensitive = caseSensitive;

            var regexTokens = new List<string>();

            foreach (var token in tokens)
            {
                var escaptedToken = Regex.Escape(token.Trim());

                if (escaptedToken.Length > 0)
                {
                    if (Char.IsLetterOrDigit(escaptedToken[0]))
                        regexTokens.Add(String.Format(@"\b{0}\b", escaptedToken));
                    else
                        regexTokens.Add(escaptedToken);
                }
            }

            string pattern = String.Join("|", regexTokens);
            var regexOptions = RegexOptions.Compiled;
            if (!caseSensitive)
                regexOptions = regexOptions | RegexOptions.IgnoreCase;
            _regex = new Regex(pattern, regexOptions);
        }

        internal ExpressionType ExpressionType 
        {
            get { return _expressionType; }
            set { _expressionType = value; }
        }

        internal bool IsCaseSensitive 
        {
            get { return _isCaseSensitive; }
        }

        internal Regex Regex
        {
            get { return _regex; }
        }
    }
}
