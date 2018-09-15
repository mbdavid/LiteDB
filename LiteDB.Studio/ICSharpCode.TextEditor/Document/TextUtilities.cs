// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Diagnostics;
using System.Text;

namespace ICSharpCode.TextEditor.Document
{
	
	public sealed class TextUtilities
	{
		/// <remarks>
		/// This function takes a string and converts the whitespace in front of
		/// it to tabs. If the length of the whitespace at the start of the string
		/// was not a whole number of tabs then there will still be some spaces just
		/// before the text starts.
		/// the output string will be of the form:
		/// 1. zero or more tabs
		/// 2. zero or more spaces (less than tabIndent)
		/// 3. the rest of the line
		/// </remarks>
		public static string LeadingWhiteSpaceToTabs(string line, int tabIndent) {
			StringBuilder sb = new StringBuilder(line.Length);
			int consecutiveSpaces = 0;
			int i = 0;
			for(i = 0; i < line.Length; i++) {
				if(line[i] == ' ') {
					consecutiveSpaces++;
					if(consecutiveSpaces == tabIndent) {
						sb.Append('\t');
						consecutiveSpaces = 0;
					}
				}
				else if(line[i] == '\t') {
					sb.Append('\t');
					// if we had say 3 spaces then a tab and tabIndent was 4 then
					// we would want to simply replace all of that with 1 tab
					consecutiveSpaces = 0;
				}
				else {
					break;
				}
			}
			
			if(i < line.Length) {
				sb.Append(line.Substring(i-consecutiveSpaces));
			}
			return sb.ToString();
		}
		
		public static bool IsLetterDigitOrUnderscore(char c)
		{
			if(!Char.IsLetterOrDigit(c)) {
				return c == '_';
			}
			return true;
		}
		
		public enum CharacterType {
			LetterDigitOrUnderscore,
			WhiteSpace,
			Other
		}
		
		/// <remarks>
		/// This method returns the expression before a specified offset.
		/// That method is used in code completion to determine the expression given
		/// to the parser for type resolve.
		/// </remarks>
		public static string GetExpressionBeforeOffset(TextArea textArea, int initialOffset)
		{
			IDocument document = textArea.Document;
			int offset = initialOffset;
			while (offset - 1 > 0) {
				switch (document.GetCharAt(offset - 1)) {
					case '\n':
					case '\r':
					case '}':
						goto done;
//						offset = SearchBracketBackward(document, offset - 2, '{','}');
//						break;
					case ']':
						offset = SearchBracketBackward(document, offset - 2, '[',']');
						break;
					case ')':
						offset = SearchBracketBackward(document, offset - 2, '(',')');
						break;
					case '.':
						--offset;
						break;
					case '"':
						if (offset < initialOffset - 1) {
							return null;
						}
						return "\"\"";
					case '\'':
						if (offset < initialOffset - 1) {
							return null;
						}
						return "'a'";
					case '>':
						if (document.GetCharAt(offset - 2) == '-') {
							offset -= 2;
							break;
						}
						goto done;
					default:
						if (Char.IsWhiteSpace(document.GetCharAt(offset - 1))) {
							--offset;
							break;
						}
						int start = offset - 1;
						if (!IsLetterDigitOrUnderscore(document.GetCharAt(start))) {
							goto done;
						}
						
						while (start > 0 && IsLetterDigitOrUnderscore(document.GetCharAt(start - 1))) {
							--start;
						}
						string word = document.GetText(start, offset - start).Trim();
						switch (word) {
							case "ref":
							case "out":
							case "in":
							case "return":
							case "throw":
							case "case":
								goto done;
						}
						
						if (word.Length > 0 && !IsLetterDigitOrUnderscore(word[0])) {
							goto done;
						}
						offset = start;
						break;
				}
			}
		done:
			//// simple exit fails when : is inside comment line or any other character
			//// we have to check if we got several ids in resulting line, which usually happens when
			//// id. is typed on next line after comment one
			//// Would be better if lexer would parse properly such expressions. However this will cause
			//// modifications in this area too - to get full comment line and remove it afterwards
			if (offset < 0)
				return string.Empty;
			
			string resText=document.GetText(offset, textArea.Caret.Offset - offset ).Trim();
			int pos=resText.LastIndexOf('\n');
			if (pos>=0) {
				offset+=pos+1;
				//// whitespaces and tabs, which might be inside, will be skipped by trim below
			}
			string expression = document.GetText(offset, textArea.Caret.Offset - offset ).Trim();
			return expression;
		}
		
		
		public static CharacterType GetCharacterType(char c)
		{
			if(IsLetterDigitOrUnderscore(c))
				return CharacterType.LetterDigitOrUnderscore;
			if(Char.IsWhiteSpace(c))
				return CharacterType.WhiteSpace;
			return CharacterType.Other;
		}
		
		public static int GetFirstNonWSChar(IDocument document, int offset)
		{
			while (offset < document.TextLength && Char.IsWhiteSpace(document.GetCharAt(offset))) {
				++offset;
			}
			return offset;
		}
		
		public static int FindWordEnd(IDocument document, int offset)
		{
			LineSegment line   = document.GetLineSegmentForOffset(offset);
			int     endPos = line.Offset + line.Length;
			while (offset < endPos && IsLetterDigitOrUnderscore(document.GetCharAt(offset))) {
				++offset;
			}
			
			return offset;
		}
		
		public static int FindWordStart(IDocument document, int offset)
		{
			LineSegment line = document.GetLineSegmentForOffset(offset);
			int lineOffset = line.Offset;
			while (offset > lineOffset && IsLetterDigitOrUnderscore(document.GetCharAt(offset - 1))) {
				--offset;
			}
			
			return offset;
		}
		
		// go forward to the start of the next word
		// if the cursor is at the start or in the middle of a word we move to the end of the word
		// and then past any whitespace that follows it
		// if the cursor is at the start or in the middle of some whitespace we move to the start of the
		// next word
		public static int FindNextWordStart(IDocument document, int offset)
		{
			int originalOffset = offset;
			LineSegment line   = document.GetLineSegmentForOffset(offset);
			int     endPos = line.Offset + line.Length;
			// lets go to the end of the word, whitespace or operator
			CharacterType t = GetCharacterType(document.GetCharAt(offset));
			while (offset < endPos && GetCharacterType(document.GetCharAt(offset)) == t) {
				++offset;
			}
			
			// now we're at the end of the word, lets find the start of the next one by skipping whitespace
			while (offset < endPos && GetCharacterType(document.GetCharAt(offset)) == CharacterType.WhiteSpace) {
				++offset;
			}

			return offset;
		}
		
		// go back to the start of the word we are on
		// if we are already at the start of a word or if we are in whitespace, then go back
		// to the start of the previous word
		public static int FindPrevWordStart(IDocument document, int offset)
		{
			int originalOffset = offset;
			if (offset > 0) {
				LineSegment line = document.GetLineSegmentForOffset(offset);
				CharacterType t = GetCharacterType(document.GetCharAt(offset - 1));
				while (offset > line.Offset && GetCharacterType(document.GetCharAt(offset - 1)) == t) {
					--offset;
				}
				
				// if we were in whitespace, and now we're at the end of a word or operator, go back to the beginning of it
				if(t == CharacterType.WhiteSpace && offset > line.Offset) {
					t = GetCharacterType(document.GetCharAt(offset - 1));
					while (offset > line.Offset && GetCharacterType(document.GetCharAt(offset - 1)) == t) {
						--offset;
					}
				}
			}
			
			return offset;
		}
		
		public static string GetLineAsString(IDocument document, int lineNumber)
		{
			LineSegment line = document.GetLineSegment(lineNumber);
			return document.GetText(line.Offset, line.Length);
		}
		
		public static int SearchBracketBackward(IDocument document, int offset, char openBracket, char closingBracket)
		{
			return document.FormattingStrategy.SearchBracketBackward(document, offset, openBracket, closingBracket);
		}
		
		public static int SearchBracketForward(IDocument document, int offset, char openBracket, char closingBracket)
		{
			return document.FormattingStrategy.SearchBracketForward(document, offset, openBracket, closingBracket);
		}
		
		/// <remarks>
		/// Returns true, if the line lineNumber is empty or filled with whitespaces.
		/// </remarks>
		public static bool IsEmptyLine(IDocument document, int lineNumber)
		{
			return IsEmptyLine(document, document.GetLineSegment(lineNumber));
		}

		/// <remarks>
		/// Returns true, if the line lineNumber is empty or filled with whitespaces.
		/// </remarks>
		public static bool IsEmptyLine(IDocument document, LineSegment line)
		{
			for (int i = line.Offset; i < line.Offset + line.Length; ++i) {
				char ch = document.GetCharAt(i);
				if (!Char.IsWhiteSpace(ch)) {
					return false;
				}
			}
			return true;
		}
		
		static bool IsWordPart(char ch)
		{
			return IsLetterDigitOrUnderscore(ch) || ch == '.';
		}
		
		public static string GetWordAt(IDocument document, int offset)
		{
			if (offset < 0 || offset >= document.TextLength - 1 || !IsWordPart(document.GetCharAt(offset))) {
				return String.Empty;
			}
			int startOffset = offset;
			int endOffset   = offset;
			while (startOffset > 0 && IsWordPart(document.GetCharAt(startOffset - 1))) {
				--startOffset;
			}
			
			while (endOffset < document.TextLength - 1 && IsWordPart(document.GetCharAt(endOffset + 1))) {
				++endOffset;
			}
			
			Debug.Assert(endOffset >= startOffset);
			return document.GetText(startOffset, endOffset - startOffset + 1);
		}
	}
}
