// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Diagnostics;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Undo
{
	/// <summary>
	/// This class is for the undo of Document insert operations
	/// </summary>
	public class UndoableReplace : IUndoableOperation
	{
		IDocument document;
//		int       oldCaretPos;
		int       offset;
		string    text;
		string    origText;
		
		/// <summary>
		/// Creates a new instance of <see cref="UndoableReplace"/>
		/// </summary>	
		public UndoableReplace(IDocument document, int offset, string origText, string text)
		{
			if (document == null) {
				throw new ArgumentNullException("document");
			}
			if (offset < 0 || offset > document.TextLength) {
				throw new ArgumentOutOfRangeException("offset");
			}
			
			Debug.Assert(text != null, "text can't be null");
//			oldCaretPos   = document.Caret.Offset;
			this.document = document;
			this.offset   = offset;
			this.text     = text;
			this.origText = origText;
		}
		
		/// <remarks>
		/// Undo last operation
		/// </remarks>
		public void Undo()
		{
			// we clear all selection direct, because the redraw
			// is done per refresh at the end of the action
//			document.SelectionCollection.Clear();

			document.UndoStack.AcceptChanges = false;
			document.Replace(offset, text.Length, origText);
//			document.Caret.Offset = Math.Min(document.TextLength, Math.Max(0, oldCaretPos));
			document.UndoStack.AcceptChanges = true;
		}
		
		/// <remarks>
		/// Redo last undone operation
		/// </remarks>
		public void Redo()
		{
			// we clear all selection direct, because the redraw
			// is done per refresh at the end of the action
//			document.SelectionCollection.Clear();

			document.UndoStack.AcceptChanges = false;
			document.Replace(offset, origText.Length, text);
//			document.Caret.Offset = Math.Min(document.TextLength, Math.Max(0, document.Caret.Offset));
			document.UndoStack.AcceptChanges = true;
		}
	}
}
