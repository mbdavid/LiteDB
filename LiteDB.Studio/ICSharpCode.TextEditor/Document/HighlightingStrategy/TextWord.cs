// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Diagnostics;
using System.Drawing;

namespace ICSharpCode.TextEditor.Document
{
	public enum TextWordType {
		Word,
		Space,
		Tab
	}
	
	/// <summary>
	/// This class represents single words with color information, two special versions of a word are
	/// spaces and tabs.
	/// </summary>
	public class TextWord
	{
		HighlightColor  color;
		LineSegment     line;
		IDocument       document;
		
		int          offset;
		int          length;
		
		public sealed class SpaceTextWord : TextWord
		{
			public SpaceTextWord()
			{
				length = 1;
			}
			
			public SpaceTextWord(HighlightColor color)
			{
				length = 1;
				base.SyntaxColor = color;
			}
			
			public override Font GetFont(FontContainer fontContainer)
			{
				return null;
			}
			
			public override TextWordType Type {
				get {
					return TextWordType.Space;
				}
			}
			public override bool IsWhiteSpace {
				get {
					return true;
				}
			}
		}
		
		public sealed class TabTextWord : TextWord
		{
			public TabTextWord()
			{
				length = 1;
			}
			public TabTextWord(HighlightColor color)
			{
				length = 1;
				base.SyntaxColor = color;
			}
			
			public override Font GetFont(FontContainer fontContainer)
			{
				return null;
			}
			
			public override TextWordType Type {
				get {
					return TextWordType.Tab;
				}
			}
			public override bool IsWhiteSpace {
				get {
					return true;
				}
			}
		}
		
		static TextWord spaceWord = new SpaceTextWord();
		static TextWord tabWord   = new TabTextWord();
		
		bool hasDefaultColor;
		
		public static TextWord Space {
			get {
				return spaceWord;
			}
		}
		
		public static TextWord Tab {
			get {
				return tabWord;
			}
		}
		
		public int Offset {
			get {
				return offset;
			}
		}
		
		public int Length {
			get {
				return length;
			}
		}
		
		/// <summary>
		/// Splits the <paramref name="word"/> into two parts: the part before <paramref name="pos"/> is assigned to
		/// the reference parameter <paramref name="word"/>, the part after <paramref name="pos"/> is returned.
		/// </summary>
		public static TextWord Split(ref TextWord word, int pos)
		{
			#if DEBUG
			if (word.Type != TextWordType.Word)
				throw new ArgumentException("word.Type must be Word");
			if (pos <= 0)
				throw new ArgumentOutOfRangeException("pos", pos, "pos must be > 0");
			if (pos >= word.Length)
				throw new ArgumentOutOfRangeException("pos", pos, "pos must be < word.Length");
			#endif
			TextWord after = new TextWord(word.document, word.line, word.offset + pos, word.length - pos, word.color, word.hasDefaultColor);
			word = new TextWord(word.document, word.line, word.offset, pos, word.color, word.hasDefaultColor);
			return after;
		}
		
		public bool HasDefaultColor {
			get {
				return hasDefaultColor;
			}
		}
		
		public virtual TextWordType Type {
			get {
				return TextWordType.Word;
			}
		}
		
		public string Word {
			get {
				if (document == null) {
					return String.Empty;
				}
				return document.GetText(line.Offset + offset, length);
			}
		}
		
		public virtual Font GetFont(FontContainer fontContainer)
		{
			return color.GetFont(fontContainer);
		}
		
		public Color Color {
			get {
				if (color == null)
					return Color.Black;
				else
					return color.Color;
			}
		}
		
		public bool Bold {
			get {
				if (color == null)
					return false;
				else
					return color.Bold;
			}
		}
		
		public bool Italic {
			get {
				if (color == null)
					return false;
				else
					return color.Italic;
			}
		}
		
		public HighlightColor SyntaxColor {
			get {
				return color;
			}
			set {
				Debug.Assert(value != null);
				color = value;
			}
		}
		
		public virtual bool IsWhiteSpace {
			get {
				return false;
			}
		}
		
		protected TextWord()
		{
		}
		
		// TAB
		public TextWord(IDocument document, LineSegment line, int offset, int length, HighlightColor color, bool hasDefaultColor)
		{
			Debug.Assert(document != null);
			Debug.Assert(line != null);
			Debug.Assert(color != null);
			
			this.document = document;
			this.line  = line;
			this.offset = offset;
			this.length = length;
			this.color = color;
			this.hasDefaultColor = hasDefaultColor;
		}
		
		/// <summary>
		/// Converts a <see cref="TextWord"/> instance to string (for debug purposes)
		/// </summary>
		public override string ToString()
		{
			return "[TextWord: Word = " + Word + ", Color = " + Color + "]";
		}
	}
}
