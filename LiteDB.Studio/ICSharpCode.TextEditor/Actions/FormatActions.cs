// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Text;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions
{
	public abstract class AbstractLineFormatAction : AbstractEditAction
	{
		protected TextArea textArea;
		abstract protected void Convert(IDocument document, int startLine, int endLine);
		
		public override void Execute(TextArea textArea)
		{
			if (textArea.SelectionManager.SelectionIsReadonly) {
				return;
			}
			this.textArea = textArea;
			textArea.BeginUpdate();
			textArea.Document.UndoStack.StartUndoGroup();
			if (textArea.SelectionManager.HasSomethingSelected) {
				foreach (ISelection selection in textArea.SelectionManager.SelectionCollection) {
					Convert(textArea.Document, selection.StartPosition.Y, selection.EndPosition.Y);
				}
			} else {
				Convert(textArea.Document, 0, textArea.Document.TotalNumberOfLines - 1);
			}
			textArea.Document.UndoStack.EndUndoGroup();
			textArea.Caret.ValidateCaretPos();
			textArea.EndUpdate();
			textArea.Refresh();
		}
	}
	
	public abstract class AbstractSelectionFormatAction : AbstractEditAction
	{
		protected TextArea textArea;
		abstract protected void Convert(IDocument document, int offset, int length);
		
		public override void Execute(TextArea textArea)
		{
			if (textArea.SelectionManager.SelectionIsReadonly) {
				return;
			}
			this.textArea = textArea;
			textArea.BeginUpdate();
			if (textArea.SelectionManager.HasSomethingSelected) {
				foreach (ISelection selection in textArea.SelectionManager.SelectionCollection) {
					Convert(textArea.Document, selection.Offset, selection.Length);
				}
			} else {
				Convert(textArea.Document, 0, textArea.Document.TextLength);
			}
			textArea.Caret.ValidateCaretPos();
			textArea.EndUpdate();
			textArea.Refresh();
		}
	}
	
	public class RemoveLeadingWS : AbstractLineFormatAction
	{
		protected override void Convert(IDocument document, int y1, int y2)
		{
			for (int i = y1; i < y2; ++i) {
				LineSegment line = document.GetLineSegment(i);
				int removeNumber = 0;
				for (int x = line.Offset; x < line.Offset + line.Length && Char.IsWhiteSpace(document.GetCharAt(x)); ++x) {
					++removeNumber;
				}
				if (removeNumber > 0) {
					document.Remove(line.Offset, removeNumber);
				}
			}
		}
	}
	
	public class RemoveTrailingWS : AbstractLineFormatAction
	{
		protected override void Convert(IDocument document, int y1, int y2)
		{
			for (int i = y2 - 1; i >= y1; --i) {
				LineSegment line = document.GetLineSegment(i);
				int removeNumber = 0;
				for (int x = line.Offset + line.Length - 1; x >= line.Offset && Char.IsWhiteSpace(document.GetCharAt(x)); --x) {
					++removeNumber;
				}
				if (removeNumber > 0) {
					document.Remove(line.Offset + line.Length - removeNumber, removeNumber);
				}
			}
		}
	}
	
	
	public class ToUpperCase : AbstractSelectionFormatAction
	{
		protected override void Convert(IDocument document, int startOffset, int length)
		{
			string what = document.GetText(startOffset, length).ToUpper();
			document.Replace(startOffset, length, what);
		}
	}
	
	public class ToLowerCase : AbstractSelectionFormatAction
	{
		protected override void Convert(IDocument document, int startOffset, int length)
		{
			string what = document.GetText(startOffset, length).ToLower();
			document.Replace(startOffset, length, what);
		}
	}
	
	public class InvertCaseAction : AbstractSelectionFormatAction
	{
		protected override void Convert(IDocument document, int startOffset, int length)
		{
			StringBuilder what = new StringBuilder(document.GetText(startOffset, length));
			
			for (int i = 0; i < what.Length; ++i) {
				what[i] = Char.IsUpper(what[i]) ? Char.ToLower(what[i]) : Char.ToUpper(what[i]);
			}
			
			document.Replace(startOffset, length, what.ToString());
		}
	}
	
	public class CapitalizeAction : AbstractSelectionFormatAction
	{
		protected override void Convert(IDocument document, int startOffset, int length)
		{
			StringBuilder what = new StringBuilder(document.GetText(startOffset, length));
			
			for (int i = 0; i < what.Length; ++i) {
				if (!Char.IsLetter(what[i]) && i < what.Length - 1) {
					what[i + 1] = Char.ToUpper(what[i + 1]);
				}
			}
			document.Replace(startOffset, length, what.ToString());
		}
		
	}
	
	public class ConvertTabsToSpaces : AbstractSelectionFormatAction
	{
		protected override void Convert(IDocument document, int startOffset, int length)
		{
			string what = document.GetText(startOffset, length);
			string spaces = new string(' ', document.TextEditorProperties.TabIndent);
			document.Replace(startOffset, length, what.Replace("\t", spaces));
		}
	}
	
	public class ConvertSpacesToTabs : AbstractSelectionFormatAction
	{
		protected override void Convert(IDocument document, int startOffset, int length)
		{
			string what = document.GetText(startOffset, length);
			string spaces = new string(' ', document.TextEditorProperties.TabIndent);
			document.Replace(startOffset, length, what.Replace(spaces, "\t"));
		}
	}
	
	public class ConvertLeadingTabsToSpaces : AbstractLineFormatAction
	{
		protected override void Convert(IDocument document, int y1, int y2)
		{
			for (int i = y2; i >= y1; --i) {
				LineSegment line = document.GetLineSegment(i);
				
				if(line.Length > 0) {
					// count how many whitespace characters there are at the start
					int whiteSpace = 0;
					for(whiteSpace = 0; whiteSpace < line.Length && Char.IsWhiteSpace(document.GetCharAt(line.Offset + whiteSpace)); whiteSpace++) {
						// deliberately empty
					}
					if(whiteSpace > 0) {
						string newLine = document.GetText(line.Offset,whiteSpace);
						string newPrefix = newLine.Replace("\t",new string(' ', document.TextEditorProperties.TabIndent));
						document.Replace(line.Offset,whiteSpace,newPrefix);
					}
				}
			}
		}
	}
	
	public class ConvertLeadingSpacesToTabs : AbstractLineFormatAction
	{
		protected override void Convert(IDocument document, int y1, int y2)
		{
			for (int i = y2; i >= y1; --i) {
				LineSegment line = document.GetLineSegment(i);
				if(line.Length > 0) {
					// note: some users may prefer a more radical ConvertLeadingSpacesToTabs that
					// means there can be no spaces before the first character even if the spaces
					// didn't add up to a whole number of tabs
					string newLine = TextUtilities.LeadingWhiteSpaceToTabs(document.GetText(line.Offset,line.Length), document.TextEditorProperties.TabIndent);
					document.Replace(line.Offset,line.Length,newLine);
				}
			}
		}
	}

	/// <summary>
	/// This is a sample editaction plugin, it indents the selected area.
	/// </summary>
	public class IndentSelection : AbstractLineFormatAction
	{
		protected override void Convert(IDocument document, int startLine, int endLine)
		{
			document.FormattingStrategy.IndentLines(textArea, startLine, endLine);
		}
	}
}
