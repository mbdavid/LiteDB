// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Xml;

namespace ICSharpCode.TextEditor.Document
{
	public sealed class Span
	{
		bool        stopEOL;
		HighlightColor color;
		HighlightColor beginColor;
		HighlightColor endColor;
		char[]      begin;
		char[]      end;
		string      name;
		string      rule;
		HighlightRuleSet ruleSet;
		char escapeCharacter;
		bool ignoreCase;
		bool isBeginSingleWord;
		bool? isBeginStartOfLine;
		bool isEndSingleWord;
		
		internal HighlightRuleSet RuleSet {
			get {
				return ruleSet;
			}
			set {
				ruleSet = value;
			}
		}

		public bool IgnoreCase	{
			get	{
				return ignoreCase;
			}
			set	{
				ignoreCase = value;
			}
		}

		public bool StopEOL {
			get {
				return stopEOL;
			}
		}
		
		public bool? IsBeginStartOfLine {
			get {
				return isBeginStartOfLine;
			}
		}
		
		public bool IsBeginSingleWord {
			get {
				return isBeginSingleWord;
			}
		}
		
		public bool IsEndSingleWord {
			get {
				return isEndSingleWord;
			}
		}
		
		public HighlightColor Color {
			get {
				return color;
			}
		}
		
		public HighlightColor BeginColor {
			get {
				if(beginColor != null) {
					return beginColor;
				} else {
					return color;
				}
			}
		}
		
		public HighlightColor EndColor {
			get {
				return endColor!=null ? endColor : color;
			}
		}
		
		public char[] Begin {
			get { return begin; }
		}
		
		public char[] End {
			get { return end; }
		}
		
		public string Name {
			get { return name; }
		}
		
		public string Rule {
			get { return rule; }
		}
		
		/// <summary>
		/// Gets the escape character of the span. The escape character is a character that can be used in front
		/// of the span end to make it not end the span. The escape character followed by another escape character
		/// means the escape character was escaped like in @"a "" b" literals in C#.
		/// The default value '\0' means no escape character is allowed.
		/// </summary>
		public char EscapeCharacter {
			get { return escapeCharacter; }
		}
		
		public Span(XmlElement span)
		{
			color   = new HighlightColor(span);
			
			if (span.HasAttribute("rule")) {
				rule = span.GetAttribute("rule");
			}
			
			if (span.HasAttribute("escapecharacter")) {
				escapeCharacter = span.GetAttribute("escapecharacter")[0];
			}
			
			name = span.GetAttribute("name");
			if (span.HasAttribute("stopateol")) {
				stopEOL = Boolean.Parse(span.GetAttribute("stopateol"));
			}
			
			begin   = span["Begin"].InnerText.ToCharArray();
			beginColor = new HighlightColor(span["Begin"], color);
			
			if (span["Begin"].HasAttribute("singleword")) {
				this.isBeginSingleWord = Boolean.Parse(span["Begin"].GetAttribute("singleword"));
			}
			if (span["Begin"].HasAttribute("startofline")) {
				this.isBeginStartOfLine = Boolean.Parse(span["Begin"].GetAttribute("startofline"));
			}
			
			if (span["End"] != null) {
				end  = span["End"].InnerText.ToCharArray();
				endColor = new HighlightColor(span["End"], color);
				if (span["End"].HasAttribute("singleword")) {
					this.isEndSingleWord = Boolean.Parse(span["End"].GetAttribute("singleword"));
				}

			}
		}
	}
}
