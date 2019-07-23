// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Xml;

namespace ICSharpCode.TextEditor.Document
{
	/// <summary>
	/// A color used for highlighting
	/// </summary>
	public class HighlightColor
	{
		Color  color;
		Color  backgroundcolor = System.Drawing.Color.WhiteSmoke;
		
		bool   bold   = false;
		bool   italic = false;
		bool   hasForeground = false;
		bool   hasBackground = false;
		
		public bool HasForeground {
			get {
				return hasForeground;
			}
		}
		
		public bool HasBackground {
			get {
				return hasBackground;
			}
		}
		
		
		/// <value>
		/// If true the font will be displayed bold style
		/// </value>
		public bool Bold {
			get {
				return bold;
			}
		}
		
		/// <value>
		/// If true the font will be displayed italic style
		/// </value>
		public bool Italic {
			get {
				return italic;
			}
		}
		
		/// <value>
		/// The background color used
		/// </value>
		public Color BackgroundColor {
			get {
				return backgroundcolor;
			}
		}
		
		/// <value>
		/// The foreground color used
		/// </value>
		public Color Color {
			get {
				return color;
			}
		}
		
		/// <value>
		/// The font used
		/// </value>
		public Font GetFont(FontContainer fontContainer)
		{
			if (Bold) {
				return Italic ? fontContainer.BoldItalicFont : fontContainer.BoldFont;
			}
			return Italic ? fontContainer.ItalicFont : fontContainer.RegularFont;
		}
		
		Color ParseColorString(string colorName)
		{
			string[] cNames = colorName.Split('*');
			PropertyInfo myPropInfo = typeof(System.Drawing.SystemColors).GetProperty(cNames[0], BindingFlags.Public |
			                                                                          BindingFlags.Instance |
			                                                                          BindingFlags.Static);
			Color c = (Color)myPropInfo.GetValue(null, null);
			
			if (cNames.Length == 2) {
				// hack : can't figure out how to parse doubles with '.' (culture info might set the '.' to ',')
				double factor = Double.Parse(cNames[1]) / 100;
				c = Color.FromArgb((int)((double)c.R * factor), (int)((double)c.G * factor), (int)((double)c.B * factor));
			}
			
			return c;
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="HighlightColor"/>
		/// </summary>
		public HighlightColor(XmlElement el)
		{
			Debug.Assert(el != null, "ICSharpCode.TextEditor.Document.SyntaxColor(XmlElement el) : el == null");
			if (el.Attributes["bold"] != null) {
				bold = Boolean.Parse(el.Attributes["bold"].InnerText);
			}
			
			if (el.Attributes["italic"] != null) {
				italic = Boolean.Parse(el.Attributes["italic"].InnerText);
			}
			
			if (el.Attributes["color"] != null) {
				string c = el.Attributes["color"].InnerText;
				if (c[0] == '#') {
					color = ParseColor(c);
				} else if (c.StartsWith("SystemColors.")) {
					color = ParseColorString(c.Substring("SystemColors.".Length));
				} else {
					color = (Color)(Color.GetType()).InvokeMember(c, BindingFlags.GetProperty, null, Color, new object[0]);
				}
				hasForeground = true;
			} else {
				color = Color.Transparent; // to set it to the default value.
			}
			
			if (el.Attributes["bgcolor"] != null) {
				string c = el.Attributes["bgcolor"].InnerText;
				if (c[0] == '#') {
					backgroundcolor = ParseColor(c);
				} else if (c.StartsWith("SystemColors.")) {
					backgroundcolor = ParseColorString(c.Substring("SystemColors.".Length));
				} else {
					backgroundcolor = (Color)(Color.GetType()).InvokeMember(c, BindingFlags.GetProperty, null, Color, new object[0]);
				}
				hasBackground = true;
			}
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="HighlightColor"/>
		/// </summary>
		public HighlightColor(XmlElement el, HighlightColor defaultColor)
		{
			Debug.Assert(el != null, "ICSharpCode.TextEditor.Document.SyntaxColor(XmlElement el) : el == null");
			if (el.Attributes["bold"] != null) {
				bold = Boolean.Parse(el.Attributes["bold"].InnerText);
			} else {
				bold = defaultColor.Bold;
			}
			
			if (el.Attributes["italic"] != null) {
				italic = Boolean.Parse(el.Attributes["italic"].InnerText);
			} else {
				italic = defaultColor.Italic;
			}
			
			if (el.Attributes["color"] != null) {
				string c = el.Attributes["color"].InnerText;
				if (c[0] == '#') {
					color = ParseColor(c);
				} else if (c.StartsWith("SystemColors.")) {
					color = ParseColorString(c.Substring("SystemColors.".Length));
				} else {
					color = (Color)(Color.GetType()).InvokeMember(c, BindingFlags.GetProperty, null, Color, new object[0]);
				}
				hasForeground = true;
			} else {
				color = defaultColor.color;
			}
			
			if (el.Attributes["bgcolor"] != null) {
				string c = el.Attributes["bgcolor"].InnerText;
				if (c[0] == '#') {
					backgroundcolor = ParseColor(c);
				} else if (c.StartsWith("SystemColors.")) {
					backgroundcolor = ParseColorString(c.Substring("SystemColors.".Length));
				} else {
					backgroundcolor = (Color)(Color.GetType()).InvokeMember(c, BindingFlags.GetProperty, null, Color, new object[0]);
				}
				hasBackground = true;
			} else {
				backgroundcolor = defaultColor.BackgroundColor;
			}
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="HighlightColor"/>
		/// </summary>
		public HighlightColor(Color color, bool bold, bool italic)
		{
			hasForeground = true;
			this.color  = color;
			this.bold   = bold;
			this.italic = italic;
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="HighlightColor"/>
		/// </summary>
		public HighlightColor(Color color, Color backgroundcolor, bool bold, bool italic)
		{
			hasForeground = true;
			hasBackground  = true;
			this.color            = color;
			this.backgroundcolor  = backgroundcolor;
			this.bold             = bold;
			this.italic           = italic;
		}
		
		
		/// <summary>
		/// Creates a new instance of <see cref="HighlightColor"/>
		/// </summary>
		public HighlightColor(string systemColor, string systemBackgroundColor, bool bold, bool italic)
		{
			hasForeground = true;
			hasBackground  = true;
			
			this.color = ParseColorString(systemColor);
			this.backgroundcolor = ParseColorString(systemBackgroundColor);
			
			this.bold         = bold;
			this.italic       = italic;
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="HighlightColor"/>
		/// </summary>
		public HighlightColor(string systemColor, bool bold, bool italic)
		{
			hasForeground = true;
			
			this.color = ParseColorString(systemColor);
			
			this.bold         = bold;
			this.italic       = italic;
		}
		
		static Color ParseColor(string c)
		{
			int a = 255;
			int offset = 0;
			if (c.Length > 7) {
				offset = 2;
				a = Int32.Parse(c.Substring(1,2), NumberStyles.HexNumber);
			}
			
			int r = Int32.Parse(c.Substring(1 + offset,2), NumberStyles.HexNumber);
			int g = Int32.Parse(c.Substring(3 + offset,2), NumberStyles.HexNumber);
			int b = Int32.Parse(c.Substring(5 + offset,2), NumberStyles.HexNumber);
			return Color.FromArgb(a, r, g, b);
		}
		
		/// <summary>
		/// Converts a <see cref="HighlightColor"/> instance to string (for debug purposes)
		/// </summary>
		public override string ToString()
		{
			return "[HighlightColor: Bold = " + Bold +
				", Italic = " + Italic +
				", Color = " + Color +
				", BackgroundColor = " + BackgroundColor + "]";
		}
	}
}
