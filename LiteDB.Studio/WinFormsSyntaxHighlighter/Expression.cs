using System;

namespace WinFormsSyntaxHighlighter
{
    public class Expression
    {
        public ExpressionType Type { get; private set; }
        public string Content { get; private set; }
        public string Group { get; private set; }

        public Expression(string content, ExpressionType type, string group)
        {
            if (content == null)
                throw new ArgumentNullException("content");
            if (group == null)
                throw new ArgumentNullException("group");

            Type = type;
            Content = content;
            Group = group;
        }

        public Expression(string content, ExpressionType type)
            : this(content, type, String.Empty)
        {
        }

        public override string ToString()
        {
            if (Type == ExpressionType.Newline)
                return String.Format("({0})", Type);

            return String.Format("({0} --> {1}{2})", Content, Type, Group.Length > 0 ? " --> " + Group : String.Empty);
        }
    }
}
