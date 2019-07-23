// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace ICSharpCode.TextEditor.Document
{
	public class DefaultHighlightingStrategy : IHighlightingStrategyUsingRuleSets
	{
		string    name;
		List<HighlightRuleSet> rules = new List<HighlightRuleSet>();
		
		Dictionary<string, HighlightColor> environmentColors = new Dictionary<string, HighlightColor>();
		Dictionary<string, string> properties       = new Dictionary<string, string>();
		string[]  extensions;
		
		HighlightColor   digitColor;
		HighlightRuleSet defaultRuleSet = null;
		
		public HighlightColor DigitColor {
			get {
				return digitColor;
			}
			set {
				digitColor = value;
			}
		}
		
		public IEnumerable<KeyValuePair<string, HighlightColor>> EnvironmentColors {
			get {
				return environmentColors;
			}
		}
		
		protected void ImportSettingsFrom(DefaultHighlightingStrategy source)
		{
			if (source == null)
				throw new ArgumentNullException("source");
			properties = source.properties;
			extensions = source.extensions;
			digitColor = source.digitColor;
			defaultRuleSet = source.defaultRuleSet;
			name = source.name;
			rules = source.rules;
			environmentColors = source.environmentColors;
			defaultTextColor = source.defaultTextColor;
		}
		
		public DefaultHighlightingStrategy() : this("Default")
		{
		}
		
		public DefaultHighlightingStrategy(string name)
		{
			this.name = name;
			
			digitColor       = new HighlightColor(SystemColors.WindowText, false, false);
			defaultTextColor = new HighlightColor(SystemColors.WindowText, false, false);
			
			// set small 'default color environment'
			environmentColors["Default"]          = new HighlightBackground("WindowText", "Window", false, false);
			environmentColors["Selection"]        = new HighlightColor("HighlightText", "Highlight", false, false);
			environmentColors["VRuler"]           = new HighlightColor("ControlLight", "Window", false, false);
			environmentColors["InvalidLines"]     = new HighlightColor(Color.Red, false, false);
			environmentColors["CaretMarker"]      = new HighlightColor(Color.Yellow, false, false);
			environmentColors["CaretLine"] = new HighlightBackground("ControlLight", "Window", false, false);
			environmentColors["LineNumbers"] = new HighlightBackground("ControlDark", "Window", false, false);
			
			environmentColors["FoldLine"]         = new HighlightColor("ControlDark", false, false);
			environmentColors["FoldMarker"]       = new HighlightColor("WindowText", "Window", false, false);
			environmentColors["SelectedFoldLine"] = new HighlightColor("WindowText", false, false);
			environmentColors["EOLMarkers"]       = new HighlightColor("ControlLight", "Window", false, false);
			environmentColors["SpaceMarkers"]     = new HighlightColor("ControlLight", "Window", false, false);
			environmentColors["TabMarkers"]       = new HighlightColor("ControlLight", "Window", false, false);
			
		}
		
		public Dictionary<string, string> Properties {
			get {
				return properties;
			}
		}
		
		public string Name
		{
			get {
				return name;
			}
		}
		
		public string[] Extensions
		{
			set {
				extensions = value;
			}
			get {
				return extensions;
			}
		}
		
		public List<HighlightRuleSet> Rules {
			get {
				return rules;
			}
		}
		
		public HighlightRuleSet FindHighlightRuleSet(string name)
		{
			foreach(HighlightRuleSet ruleSet in rules) {
				if (ruleSet.Name == name) {
					return ruleSet;
				}
			}
			return null;
		}
		
		public void AddRuleSet(HighlightRuleSet aRuleSet)
		{
			HighlightRuleSet existing = FindHighlightRuleSet(aRuleSet.Name);
			if (existing != null) {
				existing.MergeFrom(aRuleSet);
			} else {
				rules.Add(aRuleSet);
			}
		}
		
		public void ResolveReferences()
		{
			// Resolve references from Span definitions to RuleSets
			ResolveRuleSetReferences();
			// Resolve references from RuleSet defintitions to Highlighters defined in an external mode file
			ResolveExternalReferences();
		}
		
		void ResolveRuleSetReferences()
		{
			foreach (HighlightRuleSet ruleSet in Rules) {
				if (ruleSet.Name == null) {
					defaultRuleSet = ruleSet;
				}
				
				foreach (Span aSpan in ruleSet.Spans) {
					if (aSpan.Rule != null) {
						bool found = false;
						foreach (HighlightRuleSet refSet in Rules) {
							if (refSet.Name == aSpan.Rule) {
								found = true;
								aSpan.RuleSet = refSet;
								break;
							}
						}
						if (!found) {
							aSpan.RuleSet = null;
							throw new HighlightingDefinitionInvalidException("The RuleSet " + aSpan.Rule + " could not be found in mode definition " + this.Name);
						}
					} else {
						aSpan.RuleSet = null;
					}
				}
			}
			
			if (defaultRuleSet == null) {
				throw new HighlightingDefinitionInvalidException("No default RuleSet is defined for mode definition " + this.Name);
			}
		}
		
		void ResolveExternalReferences()
		{
			foreach (HighlightRuleSet ruleSet in Rules) {
				ruleSet.Highlighter = this;
				if (ruleSet.Reference != null) {
					IHighlightingStrategy highlighter = HighlightingManager.Manager.FindHighlighter (ruleSet.Reference);
					
					if (highlighter == null)
						throw new HighlightingDefinitionInvalidException("The mode defintion " + ruleSet.Reference + " which is refered from the " + this.Name + " mode definition could not be found");
					if (highlighter is IHighlightingStrategyUsingRuleSets)
						ruleSet.Highlighter = (IHighlightingStrategyUsingRuleSets)highlighter;
					else
						throw new HighlightingDefinitionInvalidException("The mode defintion " + ruleSet.Reference + " which is refered from the " + this.Name + " mode definition does not implement IHighlightingStrategyUsingRuleSets");
				}
			}
		}
		
//		internal void SetDefaultColor(HighlightBackground color)
//		{
//			return (HighlightColor)environmentColors[name];
//			defaultColor = color;
//		}
		
		HighlightColor defaultTextColor;
		
		public HighlightColor DefaultTextColor {
			get {
				return defaultTextColor;
			}
		}
		
		public void SetColorFor(string name, HighlightColor color)
		{
			if (name == "Default")
				defaultTextColor = new HighlightColor(color.Color, color.Bold, color.Italic);
			environmentColors[name] = color;
		}

		public HighlightColor GetColorFor(string name)
		{
			HighlightColor color;
			if (environmentColors.TryGetValue(name, out color))
				return color;
			else
				return defaultTextColor;
		}
		
		public HighlightColor GetColor(IDocument document, LineSegment currentSegment, int currentOffset, int currentLength)
		{
			return GetColor(defaultRuleSet, document, currentSegment, currentOffset, currentLength);
		}

		protected virtual HighlightColor GetColor(HighlightRuleSet ruleSet, IDocument document, LineSegment currentSegment, int currentOffset, int currentLength)
		{
			if (ruleSet != null) {
				if (ruleSet.Reference != null) {
					return ruleSet.Highlighter.GetColor(document, currentSegment, currentOffset, currentLength);
				} else {
					return (HighlightColor)ruleSet.KeyWords[document,  currentSegment, currentOffset, currentLength];
				}
			}
			return null;
		}
		
		public HighlightRuleSet GetRuleSet(Span aSpan)
		{
			if (aSpan == null) {
				return this.defaultRuleSet;
			} else {
				if (aSpan.RuleSet != null)
				{
					if (aSpan.RuleSet.Reference != null) {
						return aSpan.RuleSet.Highlighter.GetRuleSet(null);
					} else {
						return aSpan.RuleSet;
					}
				} else {
					return null;
				}
			}
		}

		// Line state variable
		protected LineSegment currentLine;
		protected int currentLineNumber;
		
		// Span stack state variable
		protected SpanStack currentSpanStack;

		public virtual void MarkTokens(IDocument document)
		{
			if (Rules.Count == 0) {
				return;
			}
			
			int lineNumber = 0;
			
			while (lineNumber < document.TotalNumberOfLines) {
				LineSegment previousLine = (lineNumber > 0 ? document.GetLineSegment(lineNumber - 1) : null);
				if (lineNumber >= document.LineSegmentCollection.Count) { // may be, if the last line ends with a delimiter
					break;                                                // then the last line is not in the collection :)
				}
				
				currentSpanStack = ((previousLine != null && previousLine.HighlightSpanStack != null) ? previousLine.HighlightSpanStack.Clone() : null);
				
				if (currentSpanStack != null) {
					while (!currentSpanStack.IsEmpty && currentSpanStack.Peek().StopEOL)
					{
						currentSpanStack.Pop();
					}
					if (currentSpanStack.IsEmpty) currentSpanStack = null;
				}
				
				currentLine = (LineSegment)document.LineSegmentCollection[lineNumber];
				
				if (currentLine.Length == -1) { // happens when buffer is empty !
					return;
				}
				
				currentLineNumber = lineNumber;
				List<TextWord> words = ParseLine(document);
				// Alex: clear old words
				if (currentLine.Words != null) {
					currentLine.Words.Clear();
				}
				currentLine.Words = words;
				currentLine.HighlightSpanStack = (currentSpanStack==null || currentSpanStack.IsEmpty) ? null : currentSpanStack;
				
				++lineNumber;
			}
			document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
			document.CommitUpdate();
			currentLine = null;
		}
		
		bool MarkTokensInLine(IDocument document, int lineNumber, ref bool spanChanged)
		{
			currentLineNumber = lineNumber;
			bool processNextLine = false;
			LineSegment previousLine = (lineNumber > 0 ? document.GetLineSegment(lineNumber - 1) : null);
			
			currentSpanStack = ((previousLine != null && previousLine.HighlightSpanStack != null) ? previousLine.HighlightSpanStack.Clone() : null);
			if (currentSpanStack != null) {
				while (!currentSpanStack.IsEmpty && currentSpanStack.Peek().StopEOL) {
					currentSpanStack.Pop();
				}
				if (currentSpanStack.IsEmpty) {
					currentSpanStack = null;
				}
			}
			
			currentLine = (LineSegment)document.LineSegmentCollection[lineNumber];
			
			if (currentLine.Length == -1) { // happens when buffer is empty !
				return false;
			}
			
			List<TextWord> words = ParseLine(document);
			
			if (currentSpanStack != null && currentSpanStack.IsEmpty) {
				currentSpanStack = null;
			}
			
			// Check if the span state has changed, if so we must re-render the next line
			// This check may seem utterly complicated but I didn't want to introduce any function calls
			// or allocations here for perf reasons.
			if(currentLine.HighlightSpanStack != currentSpanStack) {
				if (currentLine.HighlightSpanStack == null) {
					processNextLine = false;
					foreach (Span sp in currentSpanStack) {
						if (!sp.StopEOL) {
							spanChanged = true;
							processNextLine = true;
							break;
						}
					}
				} else if (currentSpanStack == null) {
					processNextLine = false;
					foreach (Span sp in currentLine.HighlightSpanStack) {
						if (!sp.StopEOL) {
							spanChanged = true;
							processNextLine = true;
							break;
						}
					}
				} else {
					SpanStack.Enumerator e1 = currentSpanStack.GetEnumerator();
					SpanStack.Enumerator e2 = currentLine.HighlightSpanStack.GetEnumerator();
					bool done = false;
					while (!done) {
						bool blockSpanIn1 = false;
						while (e1.MoveNext()) {
							if (!((Span)e1.Current).StopEOL) {
								blockSpanIn1 = true;
								break;
							}
						}
						bool blockSpanIn2 = false;
						while (e2.MoveNext()) {
							if (!((Span)e2.Current).StopEOL) {
								blockSpanIn2 = true;
								break;
							}
						}
						if (blockSpanIn1 || blockSpanIn2) {
							if (blockSpanIn1 && blockSpanIn2) {
								if (e1.Current != e2.Current) {
									done = true;
									processNextLine = true;
									spanChanged = true;
								}
							} else {
								spanChanged = true;
								done = true;
								processNextLine = true;
							}
						} else {
							done = true;
							processNextLine = false;
						}
					}
				}
			} else {
				processNextLine = false;
			}
			
			//// Alex: remove old words
			if (currentLine.Words!=null) currentLine.Words.Clear();
			currentLine.Words = words;
			currentLine.HighlightSpanStack = (currentSpanStack != null && !currentSpanStack.IsEmpty) ? currentSpanStack : null;
			
			return processNextLine;
		}
		
		public virtual void MarkTokens(IDocument document, List<LineSegment> inputLines)
		{
			if (Rules.Count == 0) {
				return;
			}
			
			Dictionary<LineSegment, bool> processedLines = new Dictionary<LineSegment, bool>();
			
			bool spanChanged = false;
			int documentLineSegmentCount = document.LineSegmentCollection.Count;
			
			foreach (LineSegment lineToProcess in inputLines) {
				if (!processedLines.ContainsKey(lineToProcess)) {
					int lineNumber = lineToProcess.LineNumber;
					bool processNextLine = true;
					
					if (lineNumber != -1) {
						while (processNextLine && lineNumber < documentLineSegmentCount) {
							processNextLine = MarkTokensInLine(document, lineNumber, ref spanChanged);
							processedLines[currentLine] = true;
							++lineNumber;
						}
					}
				}
			}
			
			if (spanChanged || inputLines.Count > 20) {
				// if the span was changed (more than inputLines lines had to be reevaluated)
				// or if there are many lines in inputLines, it's faster to update the whole
				// text area instead of many small segments
				document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
			} else {
//				document.Caret.ValidateCaretPos();
//				document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, document.GetLineNumberForOffset(document.Caret.Offset)));
//
				foreach (LineSegment lineToProcess in inputLines) {
					document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, lineToProcess.LineNumber));
				}
				
			}
			document.CommitUpdate();
			currentLine = null;
		}
		
		// Span state variables
		protected bool inSpan;
		protected Span activeSpan;
		protected HighlightRuleSet activeRuleSet;
		
		// Line scanning state variables
		protected int currentOffset;
		protected int currentLength;
		
		void UpdateSpanStateVariables()
		{
			inSpan = (currentSpanStack != null && !currentSpanStack.IsEmpty);
			activeSpan = inSpan ? currentSpanStack.Peek() : null;
			activeRuleSet = GetRuleSet(activeSpan);
		}

		List<TextWord> ParseLine(IDocument document)
		{
			List<TextWord> words = new List<TextWord>();
			HighlightColor markNext = null;
			
			currentOffset = 0;
			currentLength = 0;
			UpdateSpanStateVariables();
			
			int currentLineLength = currentLine.Length;
			int currentLineOffset = currentLine.Offset;
			
			for (int i = 0; i < currentLineLength; ++i)
			{
			    char ch = document.GetCharAt(currentLineOffset + i);
			    switch (ch)
			    {
			        case '\n':
			        case '\r':
			            PushCurWord(document, ref markNext, words);
			            ++currentOffset;
			            continue;
			        case ' ':
			            PushCurWord(document, ref markNext, words);
			            if (activeSpan != null && activeSpan.Color.HasBackground)
			            {
			                words.Add(new TextWord.SpaceTextWord(activeSpan.Color));
			            }
			            else
			            {
			                words.Add(TextWord.Space);
			            }
			            ++currentOffset;
			            continue;
			        case '\t':
			            PushCurWord(document, ref markNext, words);
			            if (activeSpan != null && activeSpan.Color.HasBackground)
			            {
			                words.Add(new TextWord.TabTextWord(activeSpan.Color));
			            }
			            else
			            {
			                words.Add(TextWord.Tab);
			            }
			            ++currentOffset;
			            continue;
			    }
    		    // handle escape characters
				char escapeCharacter = '\0';
				if (activeSpan != null && activeSpan.EscapeCharacter != '\0') {
					escapeCharacter = activeSpan.EscapeCharacter;
				} else if (activeRuleSet != null) {
					escapeCharacter = activeRuleSet.EscapeCharacter;
				}
				if (escapeCharacter != '\0' && escapeCharacter == ch) {
					// we found the escape character
					if (activeSpan != null && activeSpan.End != null && activeSpan.End.Length == 1
						&& escapeCharacter == activeSpan.End[0])
					{
						// the escape character is a end-doubling escape character
						// it may count as escape only when the next character is the escape, too
						if (i + 1 < currentLineLength) {
							if (document.GetCharAt(currentLineOffset + i + 1) == escapeCharacter) {
								currentLength += 2;
								PushCurWord(document, ref markNext, words);
								++i;
								continue;
							}
						}
					} else {
						// this is a normal \-style escape
						++currentLength;
						if (i + 1 < currentLineLength) {
							++currentLength;
						}
						PushCurWord(document, ref markNext, words);
						++i;
						continue;
					}
				}
							
				// highlight digits
				if (!inSpan && (Char.IsDigit(ch) || (ch == '.' && i + 1 < currentLineLength && Char.IsDigit(document.GetCharAt(currentLineOffset + i + 1)))) && currentLength == 0) {
					bool ishex = false;
					bool isfloatingpoint = false;
								
					if (ch == '0' && i + 1 < currentLineLength && Char.ToUpper(document.GetCharAt(currentLineOffset + i + 1)) == 'X') { // hex digits
						const string hex = "0123456789ABCDEF";
						++currentLength;
						++i; // skip 'x'
						++currentLength;
						ishex = true;
						while (i + 1 < currentLineLength && hex.IndexOf(Char.ToUpper(document.GetCharAt(currentLineOffset + i + 1))) != -1) {
							++i;
							++currentLength;
						}
					} else {
						++currentLength;
						while (i + 1 < currentLineLength && Char.IsDigit(document.GetCharAt(currentLineOffset + i + 1))) {
							++i;
							++currentLength;
						}
					}
					if (!ishex && i + 1 < currentLineLength && document.GetCharAt(currentLineOffset + i + 1) == '.') {
						isfloatingpoint = true;
						++i;
						++currentLength;
						while (i + 1 < currentLineLength && Char.IsDigit(document.GetCharAt(currentLineOffset + i + 1))) {
							++i;
							++currentLength;
						}
					}
								
					if (i + 1 < currentLineLength && Char.ToUpper(document.GetCharAt(currentLineOffset + i + 1)) == 'E') {
						isfloatingpoint = true;
						++i;
						++currentLength;
						if (i + 1 < currentLineLength && (document.GetCharAt(currentLineOffset + i + 1) == '+' || document.GetCharAt(currentLine.Offset + i + 1) == '-')) {
							++i;
							++currentLength;
						}
						while (i + 1 < currentLine.Length && Char.IsDigit(document.GetCharAt(currentLineOffset + i + 1))) {
							++i;
							++currentLength;
						}
					}
								
					if (i + 1 < currentLine.Length) {
						char nextch = Char.ToUpper(document.GetCharAt(currentLineOffset + i + 1));
						if (nextch == 'F' || nextch == 'M' || nextch == 'D') {
							isfloatingpoint = true;
							++i;
							++currentLength;
						}
					}
								
					if (!isfloatingpoint) {
						bool isunsigned = false;
						if (i + 1 < currentLineLength && Char.ToUpper(document.GetCharAt(currentLineOffset + i + 1)) == 'U') {
							++i;
							++currentLength;
							isunsigned = true;
						}
						if (i + 1 < currentLineLength && Char.ToUpper(document.GetCharAt(currentLineOffset + i + 1)) == 'L') {
							++i;
							++currentLength;
							if (!isunsigned && i + 1 < currentLineLength && Char.ToUpper(document.GetCharAt(currentLineOffset + i + 1)) == 'U') {
								++i;
								++currentLength;
							}
						}
					}
								
					words.Add(new TextWord(document, currentLine, currentOffset, currentLength, DigitColor, false));
					currentOffset += currentLength;
					currentLength = 0;
					continue;
				}

				// Check for SPAN ENDs
				if (inSpan) {
					if (activeSpan.End != null && activeSpan.End.Length > 0) {
						if (MatchExpr(currentLine, activeSpan.End, i, document, activeSpan.IgnoreCase)) {
							PushCurWord(document, ref markNext, words);
							string regex = GetRegString(currentLine, activeSpan.End, i, document);
							currentLength += regex.Length;
							words.Add(new TextWord(document, currentLine, currentOffset, currentLength, activeSpan.EndColor, false));
							currentOffset += currentLength;
							currentLength = 0;
							i += regex.Length - 1;
							currentSpanStack.Pop();
							UpdateSpanStateVariables();
							continue;
						}
					}
				}

                // check for SPAN BEGIN
                if (activeRuleSet != null) {
                    Span mySpan = null;
                    foreach (Span span in activeRuleSet.Spans)
                    {
                        if (span.IsBeginSingleWord && currentLength != 0)
                            continue;
                        if (span.IsBeginStartOfLine.HasValue &&
                            span.IsBeginStartOfLine.Value !=
                            (currentLength == 0 && words.TrueForAll(
                                delegate(TextWord textWord) { return textWord.Type != TextWordType.Word; })))
                            continue;
                        if (!MatchExpr(currentLine, span.Begin, i, document, activeRuleSet.IgnoreCase))
                            continue;
                        mySpan = span;
                        break;
                    }
                    if (mySpan != null)
                    {
                        PushCurWord(document, ref markNext, words);
                        string regex = GetRegString(currentLine, mySpan.Begin, i, document);

                        if (!OverrideSpan(regex, document, words, mySpan, ref i))
                        {
                            currentLength += regex.Length;
                            words.Add(new TextWord(document, currentLine, currentOffset, currentLength, mySpan.BeginColor, false));
                            currentOffset += currentLength;
                            currentLength = 0;

                            i += regex.Length - 1;
                            if (currentSpanStack == null)
                                currentSpanStack = new SpanStack();
                            currentSpanStack.Push(mySpan);
                            mySpan.IgnoreCase = activeRuleSet.IgnoreCase;

                            UpdateSpanStateVariables();
                        }
                        continue;
                    }
                }
							
				// check if the char is a delimiter
				if (activeRuleSet != null && (int)ch < 256 && activeRuleSet.Delimiters[(int)ch]) {
					PushCurWord(document, ref markNext, words);
					if (currentOffset + currentLength +1 < currentLine.Length) {
						++currentLength;
                        PushCurWord(document, ref markNext, words);
                        continue;
					}
				}

                ++currentLength;
			}
			
			PushCurWord(document, ref markNext, words);
			
			OnParsedLine(document, currentLine, words);
			
			return words;
		}
		
		protected virtual void OnParsedLine(IDocument document, LineSegment currentLine, List<TextWord> words)
		{
		}
		
		protected virtual bool OverrideSpan(string spanBegin, IDocument document, List<TextWord> words, Span span, ref int lineOffset)
		{
			return false;
		}
		
		/// <summary>
		/// pushes the curWord string on the word list, with the
		/// correct color.
		/// </summary>
		void PushCurWord(IDocument document, ref HighlightColor markNext, List<TextWord> words)
		{
			// Svante Lidman : Need to look through the next prev logic.
			if (currentLength > 0) {
				if (words.Count > 0 && activeRuleSet != null) {
					TextWord prevWord = null;
					int pInd = words.Count - 1;
					while (pInd >= 0) {
						if (!((TextWord)words[pInd]).IsWhiteSpace) {
							prevWord = (TextWord)words[pInd];
							if (prevWord.HasDefaultColor) {
								PrevMarker marker = (PrevMarker)activeRuleSet.PrevMarkers[document, currentLine, currentOffset, currentLength];
								if (marker != null) {
									prevWord.SyntaxColor = marker.Color;
//									document.Caret.ValidateCaretPos();
//									document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, document.GetLineNumberForOffset(document.Caret.Offset)));
								}
							}
							break;
						}
						pInd--;
					}
				}
				
				if (inSpan) {
					HighlightColor c = null;
					bool hasDefaultColor = true;
					if (activeSpan.Rule == null) {
						c = activeSpan.Color;
					} else {
						c = GetColor(activeRuleSet, document, currentLine, currentOffset, currentLength);
						hasDefaultColor = false;
					}
					
					if (c == null) {
						c = activeSpan.Color;
						if (c.Color == Color.Transparent) {
							c = this.DefaultTextColor;
						}
						hasDefaultColor = true;
					}
					words.Add(new TextWord(document, currentLine, currentOffset, currentLength, markNext != null ? markNext : c, hasDefaultColor));
				} else {
					HighlightColor c = markNext != null ? markNext : GetColor(activeRuleSet, document, currentLine, currentOffset, currentLength);
					if (c == null) {
						words.Add(new TextWord(document, currentLine, currentOffset, currentLength, this.DefaultTextColor, true));
					} else {
						words.Add(new TextWord(document, currentLine, currentOffset, currentLength, c, false));
					}
				}
				
				if (activeRuleSet != null) {
					NextMarker nextMarker = (NextMarker)activeRuleSet.NextMarkers[document, currentLine, currentOffset, currentLength];
					if (nextMarker != null) {
						if (nextMarker.MarkMarker && words.Count > 0) {
							TextWord prevword = ((TextWord)words[words.Count - 1]);
							prevword.SyntaxColor = nextMarker.Color;
						}
						markNext = nextMarker.Color;
					} else {
						markNext = null;
					}
				}
				currentOffset += currentLength;
				currentLength = 0;
			}
		}
		
		#region Matching
		/// <summary>
		/// get the string, which matches the regular expression expr,
		/// in string s2 at index
		/// </summary>
		static string GetRegString(LineSegment lineSegment, char[] expr, int index, IDocument document)
		{
			int j = 0;
			StringBuilder regexpr = new StringBuilder();
			
			for (int i = 0; i < expr.Length; ++i, ++j) {
				if (index + j >= lineSegment.Length)
					break;
				
				switch (expr[i]) {
					case '@': // "special" meaning
						++i;
						if (i == expr.Length)
							throw new HighlightingDefinitionInvalidException("Unexpected end of @ sequence, use @@ to look for a single @.");
						switch (expr[i]) {
							case '!': // don't match the following expression
								StringBuilder whatmatch = new StringBuilder();
								++i;
								while (i < expr.Length && expr[i] != '@') {
									whatmatch.Append(expr[i++]);
								}
								break;
							case '@': // matches @
								regexpr.Append(document.GetCharAt(lineSegment.Offset + index + j));
								break;
						}
						break;
					default:
						if (expr[i] != document.GetCharAt(lineSegment.Offset + index + j)) {
							return regexpr.ToString();
						}
						regexpr.Append(document.GetCharAt(lineSegment.Offset + index + j));
						break;
				}
			}
			return regexpr.ToString();
		}
		
		/// <summary>
		/// returns true, if the get the string s2 at index matches the expression expr
		/// </summary>
		static bool MatchExpr(LineSegment lineSegment, char[] expr, int index, IDocument document, bool ignoreCase)
		{
			for (int i = 0, j = 0; i < expr.Length; ++i, ++j) {
				switch (expr[i]) {
					case '@': // "special" meaning
						++i;
						if (i == expr.Length)
							throw new HighlightingDefinitionInvalidException("Unexpected end of @ sequence, use @@ to look for a single @.");
						switch (expr[i]) {
							case 'C': // match whitespace or punctuation
								if (index + j == lineSegment.Offset || index + j >= lineSegment.Offset + lineSegment.Length) {
									// nothing (EOL or SOL)
								} else {
									char ch = document.GetCharAt(lineSegment.Offset + index + j);
									if (!Char.IsWhiteSpace(ch) && !Char.IsPunctuation(ch)) {
										return false;
									}
								}
								break;
							case '!': // don't match the following expression
								{
									StringBuilder whatmatch = new StringBuilder();
									++i;
									while (i < expr.Length && expr[i] != '@') {
										whatmatch.Append(expr[i++]);
									}
									if (lineSegment.Offset + index + j + whatmatch.Length < document.TextLength) {
										int k = 0;
										for (; k < whatmatch.Length; ++k) {
											char docChar = ignoreCase ? Char.ToUpperInvariant(document.GetCharAt(lineSegment.Offset + index + j + k)) : document.GetCharAt(lineSegment.Offset + index + j + k);
											char spanChar = ignoreCase ? Char.ToUpperInvariant(whatmatch[k]) : whatmatch[k];
											if (docChar != spanChar) {
												break;
											}
										}
										if (k >= whatmatch.Length) {
											return false;
										}
									}
//									--j;
									break;
								}
							case '-': // don't match the  expression before
								{
									StringBuilder whatmatch = new StringBuilder();
									++i;
									while (i < expr.Length && expr[i] != '@') {
										whatmatch.Append(expr[i++]);
									}
									if (index - whatmatch.Length >= 0) {
										int k = 0;
										for (; k < whatmatch.Length; ++k) {
											char docChar = ignoreCase ? Char.ToUpperInvariant(document.GetCharAt(lineSegment.Offset + index - whatmatch.Length + k)) : document.GetCharAt(lineSegment.Offset + index - whatmatch.Length + k);
											char spanChar = ignoreCase ? Char.ToUpperInvariant(whatmatch[k]) : whatmatch[k];
											if (docChar != spanChar)
												break;
										}
										if (k >= whatmatch.Length) {
											return false;
										}
									}
//									--j;
									break;
								}
							case '@': // matches @
								if (index + j >= lineSegment.Length || '@' != document.GetCharAt(lineSegment.Offset + index + j)) {
									return false;
								}
								break;
						}
						break;
					default:
				        {
				            if (index + j >= lineSegment.Length)
				            {
				                return false;
				            }
				            int offset = lineSegment.Offset;
				            char docChar = document.GetCharAt(offset + index + j);
				            char spanChar = expr[i];
				            if (ignoreCase)
                            {
                                docChar = Char.ToUpperInvariant(docChar);
                                spanChar = Char.ToUpperInvariant(spanChar);
				            }
				            if (docChar != spanChar) {
								return false;
							}
							break;
						}
				}
			}
			return true;
		}
		#endregion
	}
}
