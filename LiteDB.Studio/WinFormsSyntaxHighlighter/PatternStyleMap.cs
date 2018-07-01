using System;

namespace WinFormsSyntaxHighlighter
{
    internal class PatternStyleMap
    {
        public string Name { get; set; }
        public PatternDefinition PatternDefinition { get; set; }
        public SyntaxStyle SyntaxStyle { get; set; }

        public PatternStyleMap(string name, PatternDefinition patternDefinition, SyntaxStyle syntaxStyle)
        {
            if (patternDefinition == null)
                throw new ArgumentNullException("patternDefinition");
            if (syntaxStyle == null)
                throw new ArgumentNullException("syntaxStyle");
            if (String.IsNullOrEmpty(name))
                throw new ArgumentException("name must not be null or empty", "name");

            Name = name;
            PatternDefinition = patternDefinition;
            SyntaxStyle = syntaxStyle;
        }
    }
}