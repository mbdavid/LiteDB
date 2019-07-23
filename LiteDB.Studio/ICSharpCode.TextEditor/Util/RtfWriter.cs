// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Util
{
	public class RtfWriter
	{
		static Dictionary<string, int> colors;
		static int           colorNum;
		static StringBuilder colorString;
		
		public static string GenerateRtf(TextArea textArea)
		{
			colors = new Dictionary<string, int>();
			colorNum = 0;
			colorString = new StringBuilder();
			
			
			StringBuilder rtf = new StringBuilder();
			
			rtf.Append(@"{\rtf1\ansi\ansicpg1252\deff0\deflang1031");
			BuildFontTable(textArea.Document, rtf);
			rtf.Append('\n');
			
			string fileContent = BuildFileContent(textArea);
			BuildColorTable(textArea.Document, rtf);
			rtf.Append('\n');
			rtf.Append(@"\viewkind4\uc1\pard");
			rtf.Append(fileContent);
			rtf.Append("}");
			return rtf.ToString();
		}
		
		static void BuildColorTable(IDocument doc, StringBuilder rtf)
		{
			rtf.Append(@"{\colortbl ;");
			rtf.Append(colorString.ToString());
			rtf.Append("}");
		}
		
		static void BuildFontTable(IDocument doc, StringBuilder rtf)
		{
			rtf.Append(@"{\fonttbl");
			rtf.Append(@"{\f0\fmodern\fprq1\fcharset0 " + doc.TextEditorProperties.Font.Name + ";}");
			rtf.Append("}");
		}
		
		static string BuildFileContent(TextArea textArea)
		{
			StringBuilder rtf = new StringBuilder();
			bool firstLine = true;
			Color curColor = Color.Black;
			bool  oldItalic = false;
			bool  oldBold   = false;
			bool  escapeSequence = false;
			
			foreach (ISelection selection in textArea.SelectionManager.SelectionCollection) {
				int selectionOffset    = textArea.Document.PositionToOffset(selection.StartPosition);
				int selectionEndOffset = textArea.Document.PositionToOffset(selection.EndPosition);
				for (int i = selection.StartPosition.Y; i <= selection.EndPosition.Y; ++i) {
					LineSegment line = textArea.Document.GetLineSegment(i);
					int offset = line.Offset;
					if (line.Words == null) {
						continue;
					}
					
					foreach (TextWord word in line.Words) {
						switch (word.Type) {
							case TextWordType.Space:
								if (selection.ContainsOffset(offset)) {
									rtf.Append(' ');
								}
								++offset;
								break;
							
							case TextWordType.Tab:
								if (selection.ContainsOffset(offset)) {
									rtf.Append(@"\tab");
								}
								++offset;
								escapeSequence = true;
								break;
							
							case TextWordType.Word:
								Color c = word.Color;
								
								if (offset + word.Word.Length > selectionOffset && offset < selectionEndOffset) {
									string colorstr = c.R + ", " + c.G + ", " + c.B;
									
									if (!colors.ContainsKey(colorstr)) {
										colors[colorstr] = ++colorNum;
										colorString.Append(@"\red" + c.R + @"\green" + c.G + @"\blue" + c.B + ";");
									}
									if (c != curColor || firstLine) {
										rtf.Append(@"\cf" + colors[colorstr].ToString());
										curColor = c;
										escapeSequence = true;
									}
									
									if (oldItalic != word.Italic) {
										if (word.Italic) {
											rtf.Append(@"\i");
										} else {
											rtf.Append(@"\i0");
										}
										oldItalic = word.Italic;
										escapeSequence = true;
									}
									
									if (oldBold != word.Bold) {
										if (word.Bold) {
											rtf.Append(@"\b");
										} else {
											rtf.Append(@"\b0");
										}
										oldBold = word.Bold;
										escapeSequence = true;
									}
									
									if (firstLine) {
										rtf.Append(@"\f0\fs" + (textArea.TextEditorProperties.Font.Size * 2));
										firstLine = false;
									}
									if (escapeSequence) {
										rtf.Append(' ');
										escapeSequence = false;
									}
									string printWord;
									if (offset < selectionOffset) {
										printWord = word.Word.Substring(selectionOffset - offset);
									} else if (offset + word.Word.Length > selectionEndOffset) {
										printWord = word.Word.Substring(0, (offset + word.Word.Length) - selectionEndOffset);
									} else {
										printWord = word.Word;
									}
									
									AppendText(rtf, printWord);
								}
								offset += word.Length;
								break;
						}
					}
					if (offset < selectionEndOffset) {
						rtf.Append(@"\par");
					}
					rtf.Append('\n');
				}
			}
			
			return rtf.ToString();
		}
		
		static void AppendText(StringBuilder rtfOutput, string text)
		{
			//rtf.Append(printWord.Replace(@"\", @"\\").Replace("{", "\\{").Replace("}", "\\}"));
			foreach (char c in text) {
				switch (c) {
					case '\\':
						rtfOutput.Append(@"\\");
						break;
					case '{':
						rtfOutput.Append("\\{");
						break;
					case '}':
						rtfOutput.Append("\\}");
						break;
					default:
						if (c < 256) {
							rtfOutput.Append(c);
						} else {
							// yes, RTF really expects signed 16-bit integers!
							rtfOutput.Append("\\u" + unchecked((short)c).ToString() + "?");
						}
						break;
				}
			}
		}
	}
}
