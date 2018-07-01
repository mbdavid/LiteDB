using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WinFormsSyntaxHighlighter
{
    public class SyntaxHighlighter
    {
        /// <summary>
        /// Reference to the RichTextBox instance, for which 
        /// the syntax highlighting is going to occur.
        /// </summary>
        private readonly RichTextBox _richTextBox;

        private readonly int _maxLength;

        private readonly int _fontSizeFactor;

        private readonly string _fontName;

        /// <summary>
        /// Determines whether the program is busy creating rtf for the previous
        /// modification of the text-box. It is necessary to avoid blinks when the 
        /// user is typing fast.
        /// </summary>
        private bool _isDuringHighlight;

        private List<StyleGroupPair> _styleGroupPairs;

        private readonly List<PatternStyleMap> _patternStyles = new List<PatternStyleMap>(); 

        public SyntaxHighlighter(RichTextBox richTextBox, int maxLength)
        {
            if (richTextBox == null)
                throw new ArgumentNullException("richTextBox");

            _richTextBox = richTextBox;
            _maxLength = maxLength;

            _fontSizeFactor = Convert.ToInt32(_richTextBox.Font.Size * 2);
            _fontName = _richTextBox.Font.Name;

            DisableHighlighting = false;

            _richTextBox.TextChanged += RichTextBox_TextChanged;
        }


        /// <summary>
        /// Gets or sets a value indicating whether highlighting should be disabled or not.
        /// If true, the user input will remain intact. If false, the rich content will be
        /// modified to match the syntax of the currently selected language.
        /// </summary>
        public bool DisableHighlighting { get; set; }

        public void AddPattern(PatternDefinition patternDefinition, SyntaxStyle syntaxStyle)
        {
            AddPattern((_patternStyles.Count + 1).ToString(CultureInfo.InvariantCulture), patternDefinition, syntaxStyle);
        }

        public void AddPattern(string name, PatternDefinition patternDefinition, SyntaxStyle syntaxStyle)
        {
            if (patternDefinition == null)
                throw new ArgumentNullException("patternDefinition");
            if (syntaxStyle == null)
                throw new ArgumentNullException("syntaxStyle");
            if (String.IsNullOrEmpty(name))
                throw new ArgumentException("name must not be null or empty", "name");

            var existingPatternStyle = FindPatternStyle(name);

            if (existingPatternStyle != null)
                throw new ArgumentException("A pattern style pair with the same name already exists");

            _patternStyles.Add(new PatternStyleMap(name, patternDefinition, syntaxStyle));
        }

        protected SyntaxStyle GetDefaultStyle()
        {
            return new SyntaxStyle(_richTextBox.ForeColor, _richTextBox.Font.Bold, _richTextBox.Font.Italic);
        }

        private PatternStyleMap FindPatternStyle(string name)
        {
            var patternStyle = _patternStyles.FirstOrDefault(p => String.Equals(p.Name, name, StringComparison.Ordinal));
            return patternStyle;
        }

        /// <summary>
        /// Rehighlights the text-box content.
        /// </summary>
        public void ReHighlight()
        {
            if (!DisableHighlighting)
            {
                if (_isDuringHighlight) 
                    return;

                if (_richTextBox.TextLength < _maxLength)
                {
                    _richTextBox.DisableThenDoThenEnable(HighlighTextBase);
                }
            }
        }

        private void RichTextBox_TextChanged(object sender, EventArgs e)
        {
            ReHighlight();
        }

        // TODO: make abstact
        internal IEnumerable<Expression> Parse(string text)
        {
            text = text.NormalizeLineBreaks("\n");
            var parsedExpressions = new List<Expression> { new Expression(text, ExpressionType.None, String.Empty) };

            foreach (var patternStyleMap in _patternStyles)
            {
                parsedExpressions = ParsePattern(patternStyleMap, parsedExpressions);
            }

            parsedExpressions = ProcessLineBreaks(parsedExpressions);
            return parsedExpressions;
        }

        // TODO: move to child
        private Regex _lineBreakRegex;

        // TODO: move to child
        private Regex GetLineBreakRegex()
        {
            if (_lineBreakRegex == null)
                _lineBreakRegex = new Regex(Regex.Escape("\n"), RegexOptions.Compiled);

            return _lineBreakRegex;
        }

        private List<Expression> ProcessLineBreaks(List<Expression> expressions)
        {
            var parsedExpressions = new List<Expression>();

            var regex = GetLineBreakRegex();

            foreach (var inputExpression in expressions)
            {
                int lastProcessedIndex = -1;

                foreach (var match in regex.Matches(inputExpression.Content).Cast<Match>().OrderBy(m => m.Index))
                {
                    if (match.Success)
                    {
                        if (match.Index > lastProcessedIndex + 1)
                        {
                            string nonMatchedContent = inputExpression.Content.Substring(lastProcessedIndex + 1,
                                match.Index - lastProcessedIndex - 1);
                            var nonMatchedExpression = new Expression(nonMatchedContent, inputExpression.Type,
                                inputExpression.Group);
                            parsedExpressions.Add(nonMatchedExpression);
                            //lastProcessedIndex = match.Index + match.Length - 1;
                        }

                        string matchedContent = inputExpression.Content.Substring(match.Index, match.Length);
                        var matchedExpression = new Expression(matchedContent,
                            ExpressionType.Newline, "line-break");
                        parsedExpressions.Add(matchedExpression);
                        lastProcessedIndex = match.Index + match.Length - 1;
                    }
                }

                if (lastProcessedIndex < inputExpression.Content.Length - 1)
                {
                    string nonMatchedContent = inputExpression.Content.Substring(lastProcessedIndex + 1,
                        inputExpression.Content.Length - lastProcessedIndex - 1);
                    var nonMatchedExpression = new Expression(nonMatchedContent, inputExpression.Type, inputExpression.Group);
                    parsedExpressions.Add(nonMatchedExpression);
                }
            }

            return parsedExpressions;
        }

        // TODO: move to relevant child class
        private List<Expression> ParsePattern(PatternStyleMap patternStyleMap, List<Expression> expressions)
        {
            var parsedExpressions = new List<Expression>();

            foreach (var inputExpression in expressions)
            {
                if (inputExpression.Type != ExpressionType.None)
                {
                    parsedExpressions.Add(inputExpression);
                }
                else
                {
                    var regex = patternStyleMap.PatternDefinition.Regex;

                    int lastProcessedIndex = -1;

                    foreach (var match in regex.Matches(inputExpression.Content).Cast<Match>().OrderBy(m => m.Index))
                    {
                        if (match.Success)
                        {
                            if (match.Index > lastProcessedIndex + 1)
                            {
                                string nonMatchedContent = inputExpression.Content.Substring(lastProcessedIndex + 1, match.Index - lastProcessedIndex - 1);
                                var nonMatchedExpression = new Expression(nonMatchedContent, ExpressionType.None, String.Empty);
                                parsedExpressions.Add(nonMatchedExpression);
                                //lastProcessedIndex = match.Index + match.Length - 1;
                            }

                            string matchedContent = inputExpression.Content.Substring(match.Index, match.Length);
                            var matchedExpression = new Expression(matchedContent, patternStyleMap.PatternDefinition.ExpressionType, patternStyleMap.Name);
                            parsedExpressions.Add(matchedExpression);
                            lastProcessedIndex = match.Index + match.Length - 1;
                        }
                    }

                    if (lastProcessedIndex < inputExpression.Content.Length - 1)
                    {
                        string nonMatchedContent = inputExpression.Content.Substring(lastProcessedIndex + 1, inputExpression.Content.Length - lastProcessedIndex - 1);
                        var nonMatchedExpression = new Expression(nonMatchedContent, ExpressionType.None, String.Empty);
                        parsedExpressions.Add(nonMatchedExpression);
                    }
                }
            }

            return parsedExpressions;
        }

        // TODO: make abstract
        internal IEnumerable<StyleGroupPair> GetStyles()
        {
            yield return new StyleGroupPair(GetDefaultStyle(), String.Empty);

            foreach (var patternStyle in _patternStyles)
            {
                var style = patternStyle.SyntaxStyle;
                yield return new StyleGroupPair(new SyntaxStyle(style.Color, style.Bold, style.Italic), patternStyle.Name);
            }
        }

        // TODO: make virtual
        internal virtual string GetGroupName(Expression expression)
        {
            return expression.Group;
        }

        private List<StyleGroupPair> GetStyleGroupPairs()
        {
            if (_styleGroupPairs == null)
            {
                _styleGroupPairs = GetStyles().ToList();

                for (int i = 0; i < _styleGroupPairs.Count; i++)
                {
                    _styleGroupPairs[i].Index = i + 1;
                }
            }

            return _styleGroupPairs;
        }

        #region RTF Stuff
        /// <summary>
        /// The base method that highlights the text-box content.
        /// </summary>
        private void HighlighTextBase()
        {
            _isDuringHighlight = true;

            try
            {
                var sb = new StringBuilder();

                sb.AppendLine(RTFHeader());
                sb.AppendLine(RTFColorTable());
                sb.Append(@"\viewkind4\uc1\pard\f0\fs").Append(_fontSizeFactor).Append(" ");

                foreach (var exp in Parse(_richTextBox.Text))
                {
                    if (exp.Type == ExpressionType.Whitespace)
                    {
                        string wsContent = exp.Content;
                        sb.Append(wsContent);
                    }
                    else if (exp.Type == ExpressionType.Newline)
                    {
                        sb.AppendLine(@"\par");
                    }
                    else
                    {
                        string content = exp.Content.Replace("\\", "\\\\").Replace("{", @"\{").Replace("}", @"\}");

                        var styleGroups = GetStyleGroupPairs();

                        string groupName = GetGroupName(exp);

                        var styleToApply = styleGroups.FirstOrDefault(s => String.Equals(s.GroupName, groupName, StringComparison.Ordinal));

                        if (styleToApply != null)
                        {
                            string opening = String.Empty, cloing = String.Empty;

                            if (styleToApply.SyntaxStyle.Bold)
                            {
                                opening += @"\b";
                                cloing += @"\b0";
                            }

                            if (styleToApply.SyntaxStyle.Italic)
                            {
                                opening += @"\i";
                                cloing += @"\i0";
                            }

                            sb.AppendFormat(@"\cf{0}{2} {1}\cf0{3} ", styleToApply.Index,
                                content, opening, cloing);
                        }
                        else
                        {
                            sb.AppendFormat(@"\cf{0} {1}\cf0 ", 1, content);
                        }
                    }
                }

                sb.Append(@"\par }");

                _richTextBox.Rtf = sb.ToString();
            }
            finally
            {
                _isDuringHighlight = false;
            }
        }

        private string RTFColorTable()
        {
            var styleGroupPairs = GetStyleGroupPairs();

            if (styleGroupPairs.Count <= 0)
                styleGroupPairs.Add(new StyleGroupPair(GetDefaultStyle(), String.Empty));

            var sbRtfColorTable = new StringBuilder();
            sbRtfColorTable.Append(@"{\colortbl ;");

            foreach (var styleGroup in styleGroupPairs)
            {
                sbRtfColorTable.AppendFormat("{0};", ColorUtils.ColorToRtfTableEntry(styleGroup.SyntaxStyle.Color));
            }

            sbRtfColorTable.Append("}");

            return sbRtfColorTable.ToString();
        }

        private string RTFHeader()
        {
            return String.Concat(@"{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 ", _fontName, @";}}");
        }

        #endregion

    }
}
