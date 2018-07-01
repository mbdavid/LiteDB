using System;
using System.Drawing;

namespace WinFormsSyntaxHighlighter
{
    public class ColorUtils
    {
        public static string ColorToRtfTableEntry(Color color)
        {
            return String.Format(@"\red{0}\green{1}\blue{2}", color.R, color.G, color.B);
        }
    }
}
