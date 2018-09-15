// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Drawing;

namespace ICSharpCode.TextEditor.Document
{
	/// <summary>
	/// This class is used to generate bold, italic and bold/italic fonts out
	/// of a base font.
	/// </summary>
	public class FontContainer
	{
		Font defaultFont;
		Font regularfont, boldfont, italicfont, bolditalicfont;
		
		/// <value>
		/// The scaled, regular version of the base font
		/// </value>
		public Font RegularFont {
			get {
				return regularfont;
			}
		}
		
		/// <value>
		/// The scaled, bold version of the base font
		/// </value>
		public Font BoldFont {
			get {
				return boldfont;
			}
		}
		
		/// <value>
		/// The scaled, italic version of the base font
		/// </value>
		public Font ItalicFont {
			get {
				return italicfont;
			}
		}
		
		/// <value>
		/// The scaled, bold/italic version of the base font
		/// </value>
		public Font BoldItalicFont {
			get {
				return bolditalicfont;
			}
		}
		
		static float twipsPerPixelY;
		
		public static float TwipsPerPixelY {
			get {
				if (twipsPerPixelY == 0) {
					using (Bitmap bmp = new Bitmap(1,1)) {
						using (Graphics g = Graphics.FromImage(bmp)) {
							twipsPerPixelY = 1440 / g.DpiY;
						}
					}
				}
				return twipsPerPixelY;
			}
		}
		
		/// <value>
		/// The base font
		/// </value>
		public Font DefaultFont {
			get {
				return defaultFont;
			}
			set {
				// 1440 twips is one inch
				float pixelSize = (float)Math.Round(value.SizeInPoints * 20 / TwipsPerPixelY);
				
				defaultFont    = value;
				regularfont    = new Font(value.FontFamily, pixelSize * TwipsPerPixelY / 20f, FontStyle.Regular);
				boldfont       = new Font(regularfont, FontStyle.Bold);
				italicfont     = new Font(regularfont, FontStyle.Italic);
				bolditalicfont = new Font(regularfont, FontStyle.Bold | FontStyle.Italic);
			}
		}
		
		public static Font ParseFont(string font)
		{
			string[] descr = font.Split(new char[]{',', '='});
			return new Font(descr[1], Single.Parse(descr[3]));
		}
		
		public FontContainer(Font defaultFont)
		{
			this.DefaultFont = defaultFont;
		}
	}
}
