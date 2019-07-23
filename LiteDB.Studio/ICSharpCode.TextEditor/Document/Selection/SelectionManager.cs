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

namespace ICSharpCode.TextEditor.Document
{
	/// <summary>
	/// This class manages the selections in a document.
	/// </summary>
	public class SelectionManager : IDisposable
	{
		TextLocation selectionStart;
		
		internal TextLocation SelectionStart {
			get { return selectionStart; }
			set {
				DefaultDocument.ValidatePosition(document, value);
				selectionStart = value;
			}
		}
		IDocument document;
		TextArea textArea;
		internal SelectFrom selectFrom = new SelectFrom();

		internal List<ISelection> selectionCollection = new List<ISelection>();
		
		/// <value>
		/// A collection containing all selections.
		/// </value>
		public List<ISelection> SelectionCollection {
			get {
				return selectionCollection;
			}
		}
		
		/// <value>
		/// true if the <see cref="SelectionCollection"/> is not empty, false otherwise.
		/// </value>
		public bool HasSomethingSelected {
			get {
				return selectionCollection.Count > 0;
			}
		}
		
		public bool SelectionIsReadonly {
			get {
				if (document.ReadOnly)
					return true;
				foreach (ISelection sel in selectionCollection) {
					if (SelectionIsReadOnly(document, sel))
						return true;
				}
				return false;
			}
		}
		
		internal static bool SelectionIsReadOnly(IDocument document, ISelection sel)
		{
			if (document.TextEditorProperties.SupportReadOnlySegments)
				return document.MarkerStrategy.GetMarkers(sel.Offset, sel.Length).Exists(m=>m.IsReadOnly);
			else
				return false;
		}
		
		/// <value>
		/// The text that is currently selected.
		/// </value>
		public string SelectedText {
			get {
				StringBuilder builder = new StringBuilder();
				
//				PriorityQueue queue = new PriorityQueue();
				
				foreach (ISelection s in selectionCollection) {
					builder.Append(s.SelectedText);
//					queue.Insert(-s.Offset, s);
				}
				
//				while (queue.Count > 0) {
//					ISelection s = ((ISelection)queue.Remove());
//					builder.Append(s.SelectedText);
//				}
				
				return builder.ToString();
			}
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="SelectionManager"/>
		/// </summary>
		public SelectionManager(IDocument document)
		{
			this.document = document;
			document.DocumentChanged += new DocumentEventHandler(DocumentChanged);
		}

		/// <summary>
		/// Creates a new instance of <see cref="SelectionManager"/>
		/// </summary>
		public SelectionManager(IDocument document, TextArea textArea)
		{
			this.document = document;
			this.textArea = textArea;
			document.DocumentChanged += new DocumentEventHandler(DocumentChanged);
		}

		public void Dispose()
		{
			if (this.document != null) {
				document.DocumentChanged -= new DocumentEventHandler(DocumentChanged);
				this.document = null;
			}
		}
		
		void DocumentChanged(object sender, DocumentEventArgs e)
		{
			if (e.Text == null) {
				Remove(e.Offset, e.Length);
			} else {
				if (e.Length < 0) {
					Insert(e.Offset, e.Text);
				} else {
					Replace(e.Offset, e.Length, e.Text);
				}
			}
		}
		
		/// <remarks>
		/// Clears the selection and sets a new selection
		/// using the given <see cref="ISelection"/> object.
		/// </remarks>
		public void SetSelection(ISelection selection)
		{
//			autoClearSelection = false;
			if (selection != null) {
				if (SelectionCollection.Count == 1 &&
				    selection.StartPosition == SelectionCollection[0].StartPosition &&
				    selection.EndPosition == SelectionCollection[0].EndPosition ) {
					return;
				}
				ClearWithoutUpdate();
				selectionCollection.Add(selection);
				document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, selection.StartPosition.Y, selection.EndPosition.Y));
				document.CommitUpdate();
				OnSelectionChanged(EventArgs.Empty);
			} else {
				ClearSelection();
			}
		}
		
		public void SetSelection(TextLocation startPosition, TextLocation endPosition)
		{
			SetSelection(new DefaultSelection(document, startPosition, endPosition));
		}
		
		public bool GreaterEqPos(TextLocation p1, TextLocation p2)
		{
			return p1.Y > p2.Y || p1.Y == p2.Y && p1.X >= p2.X;
		}
		
		public void ExtendSelection(TextLocation oldPosition, TextLocation newPosition)
		{
			// where oldposition is where the cursor was,
			// and newposition is where it has ended up from a click (both zero based)

			if (oldPosition == newPosition)
			{
				return;
			}

			TextLocation min;
			TextLocation max;
			int oldnewX = newPosition.X;
			bool  oldIsGreater = GreaterEqPos(oldPosition, newPosition);
			if (oldIsGreater) {
				min = newPosition;
				max = oldPosition;
			} else {
				min = oldPosition;
				max = newPosition;
			}

			if (min == max) {
				return;
			}

			if (!HasSomethingSelected)
			{
				SetSelection(new DefaultSelection(document, min, max));
				// initialise selectFrom for a cursor selection
				if (selectFrom.where == WhereFrom.None)
					SelectionStart = oldPosition; //textArea.Caret.Position;
				return;
			}

			ISelection selection = this.selectionCollection[0];

			if (min == max) {
				//selection.StartPosition = newPosition;
				return;
			} else {
				// changed selection via gutter
				if (selectFrom.where == WhereFrom.Gutter)
				{
					// selection new position is always at the left edge for gutter selections
					newPosition.X = 0;
				}

				if (GreaterEqPos(newPosition, SelectionStart)) // selecting forward
				{
					selection.StartPosition = SelectionStart;
					// this handles last line selection
					if (selectFrom.where == WhereFrom.Gutter ) //&& newPosition.Y != oldPosition.Y)
						selection.EndPosition = new TextLocation(textArea.Caret.Column, textArea.Caret.Line);
					else {
						newPosition.X = oldnewX;
						selection.EndPosition = newPosition;
					}
				} else { // selecting back
					if (selectFrom.where == WhereFrom.Gutter && selectFrom.first == WhereFrom.Gutter)
					{ // gutter selection
						selection.EndPosition = NextValidPosition(SelectionStart.Y);
					} else { // internal text selection
						selection.EndPosition = SelectionStart; //selection.StartPosition;
					}
					selection.StartPosition = newPosition;
				}
			}

			document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, min.Y, max.Y));
			document.CommitUpdate();
			OnSelectionChanged(EventArgs.Empty);
		}

		// retrieve the next available line
		// - checks that there are more lines available after the current one
		// - if there are then the next line is returned
		// - if there are NOT then the last position on the given line is returned
		public TextLocation NextValidPosition(int line)
		{
			if (line < document.TotalNumberOfLines - 1)
				return new TextLocation(0, line + 1);
			else
				return new TextLocation(document.GetLineSegment(document.TotalNumberOfLines - 1).Length + 1, line);
		}

		void ClearWithoutUpdate()
		{
			while (selectionCollection.Count > 0) {
				ISelection selection = selectionCollection[selectionCollection.Count - 1];
				selectionCollection.RemoveAt(selectionCollection.Count - 1);
				document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.LinesBetween, selection.StartPosition.Y, selection.EndPosition.Y));
				OnSelectionChanged(EventArgs.Empty);
			}
		}
		/// <remarks>
		/// Clears the selection.
		/// </remarks>
		public void ClearSelection()
		{
			Point mousepos;
			mousepos = textArea.mousepos;
			// this is the most logical place to reset selection starting
			// positions because it is always called before a new selection
			selectFrom.first = selectFrom.where;
			TextLocation newSelectionStart = textArea.TextView.GetLogicalPosition(mousepos.X - textArea.TextView.DrawingPosition.X, mousepos.Y - textArea.TextView.DrawingPosition.Y);
			if (selectFrom.where == WhereFrom.Gutter) {
				newSelectionStart.X = 0;
//				selectionStart.Y = -1;
			}
			if (newSelectionStart.Line >= document.TotalNumberOfLines) {
				newSelectionStart.Line = document.TotalNumberOfLines-1;
				newSelectionStart.Column = document.GetLineSegment(document.TotalNumberOfLines-1).Length;
			}
			this.SelectionStart = newSelectionStart;

			ClearWithoutUpdate();
			document.CommitUpdate();
		}
		
		/// <remarks>
		/// Removes the selected text from the buffer and clears
		/// the selection.
		/// </remarks>
		public void RemoveSelectedText()
		{
			if (SelectionIsReadonly) {
				ClearSelection();
				return;
			}
			List<int> lines = new List<int>();
			int offset = -1;
			bool oneLine = true;
//			PriorityQueue queue = new PriorityQueue();
			foreach (ISelection s in selectionCollection) {
//				ISelection s = ((ISelection)queue.Remove());
				if (oneLine) {
					int lineBegin = s.StartPosition.Y;
					if (lineBegin != s.EndPosition.Y) {
						oneLine = false;
					} else {
						lines.Add(lineBegin);
					}
				}
				offset = s.Offset;
				document.Remove(s.Offset, s.Length);

//				queue.Insert(-s.Offset, s);
			}
			ClearSelection();
			if (offset >= 0) {
				//             TODO:
//				document.Caret.Offset = offset;
			}
			if (offset != -1) {
				if (oneLine) {
					foreach (int i in lines) {
						document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, i));
					}
				} else {
					document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
				}
				document.CommitUpdate();
			}
		}
		
		
		bool SelectionsOverlap(ISelection s1, ISelection s2)
		{
			return (s1.Offset <= s2.Offset && s2.Offset <= s1.Offset + s1.Length)                         ||
				(s1.Offset <= s2.Offset + s2.Length && s2.Offset + s2.Length <= s1.Offset + s1.Length) ||
				(s1.Offset >= s2.Offset && s1.Offset + s1.Length <= s2.Offset + s2.Length);
		}
		
		/// <remarks>
		/// Returns true if the given offset points to a section which is
		/// selected.
		/// </remarks>
		public bool IsSelected(int offset)
		{
			return GetSelectionAt(offset) != null;
		}

		/// <remarks>
		/// Returns a <see cref="ISelection"/> object giving the selection in which
		/// the offset points to.
		/// </remarks>
		/// <returns>
		/// <code>null</code> if the offset doesn't point to a selection
		/// </returns>
		public ISelection GetSelectionAt(int offset)
		{
			foreach (ISelection s in selectionCollection) {
				if (s.ContainsOffset(offset)) {
					return s;
				}
			}
			return null;
		}
		
		/// <remarks>
		/// Used internally, do not call.
		/// </remarks>
		internal void Insert(int offset, string text)
		{
//			foreach (ISelection selection in SelectionCollection) {
//				if (selection.Offset > offset) {
//					selection.Offset += text.Length;
//				} else if (selection.Offset + selection.Length > offset) {
//					selection.Length += text.Length;
//				}
//			}
		}
		
		/// <remarks>
		/// Used internally, do not call.
		/// </remarks>
		internal void Remove(int offset, int length)
		{
//			foreach (ISelection selection in selectionCollection) {
//				if (selection.Offset > offset) {
//					selection.Offset -= length;
//				} else if (selection.Offset + selection.Length > offset) {
//					selection.Length -= length;
//				}
//			}
		}
		
		/// <remarks>
		/// Used internally, do not call.
		/// </remarks>
		internal void Replace(int offset, int length, string text)
		{
//			foreach (ISelection selection in selectionCollection) {
//				if (selection.Offset > offset) {
//					selection.Offset = selection.Offset - length + text.Length;
//				} else if (selection.Offset + selection.Length > offset) {
//					selection.Length = selection.Length - length + text.Length;
//				}
//			}
		}
		
		public ColumnRange GetSelectionAtLine(int lineNumber)
		{
			foreach (ISelection selection in selectionCollection) {
				int startLine = selection.StartPosition.Y;
				int endLine   = selection.EndPosition.Y;
				if (startLine < lineNumber && lineNumber < endLine) {
					return ColumnRange.WholeColumn;
				}
				
				if (startLine == lineNumber) {
					LineSegment line = document.GetLineSegment(startLine);
					int startColumn = selection.StartPosition.X;
					int endColumn   = endLine == lineNumber ? selection.EndPosition.X : line.Length + 1;
					return new ColumnRange(startColumn, endColumn);
				}
				
				if (endLine == lineNumber) {
					int endColumn   = selection.EndPosition.X;
					return new ColumnRange(0, endColumn);
				}
			}
			
			return ColumnRange.NoColumn;
		}
		
		public void FireSelectionChanged()
		{
			OnSelectionChanged(EventArgs.Empty);
		}
		protected virtual void OnSelectionChanged(EventArgs e)
		{
			if (SelectionChanged != null) {
				SelectionChanged(this, e);
			}
		}
		
		public event EventHandler SelectionChanged;
	}

	// selection initiated from...
	internal class SelectFrom {
		public int where = WhereFrom.None; // last selection initiator
		public int first = WhereFrom.None; // first selection initiator

		public SelectFrom()
		{
		}
	}

	// selection initiated from type...
	internal class WhereFrom {
		public const int None = 0;
		public const int Gutter = 1;
		public const int TArea = 2;
	}
}
