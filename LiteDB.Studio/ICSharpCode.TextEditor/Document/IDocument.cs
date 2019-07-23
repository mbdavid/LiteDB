// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Drawing;

using ICSharpCode.TextEditor.Undo;

namespace ICSharpCode.TextEditor.Document
{
	/// <summary>
	/// This interface represents a container which holds a text sequence and
	/// all necessary information about it. It is used as the base for a text editor.
	/// </summary>
	public interface IDocument
	{
		ITextEditorProperties TextEditorProperties {
			get;
			set;
		}
		
		UndoStack UndoStack {
			get;
		}
		/// <value>
		/// If true the document can't be altered
		/// </value>
		bool ReadOnly {
			get;
			set;
		}
		
		/// <summary>
		/// The <see cref="IFormattingStrategy"/> attached to the <see cref="IDocument"/> instance
		/// </summary>
		IFormattingStrategy FormattingStrategy {
			get;
			set;
		}
		
		/// <summary>
		/// The <see cref="ITextBufferStrategy"/> attached to the <see cref="IDocument"/> instance
		/// </summary>
		ITextBufferStrategy TextBufferStrategy {
			get;
		}
		
		/// <summary>
		/// The <see cref="FoldingManager"/> attached to the <see cref="IDocument"/> instance
		/// </summary>
		FoldingManager FoldingManager {
			get;
		}
		
		/// <summary>
		/// The <see cref="IHighlightingStrategy"/> attached to the <see cref="IDocument"/> instance
		/// </summary>
		IHighlightingStrategy HighlightingStrategy {
			get;
			set;
		}
		
		/// <summary>
		/// The <see cref="IBookMarkManager"/> attached to the <see cref="IDocument"/> instance
		/// </summary>
		BookmarkManager BookmarkManager {
			get;
		}
		
		MarkerStrategy MarkerStrategy {
			get;
		}
		
//		/// <summary>
//		/// The <see cref="SelectionManager"/> attached to the <see cref="IDocument"/> instance
//		/// </summary>
//		SelectionManager SelectionManager {
//			get;
//		}
		
		#region ILineManager interface
		/// <value>
		/// A collection of all line segments
		/// </value>
		/// <remarks>
		/// The collection should only be used if you're aware
		/// of the 'last line ends with a delimiter problem'. Otherwise
		/// the <see cref="GetLineSegment"/> method should be used.
		/// </remarks>
		IList<LineSegment> LineSegmentCollection {
			get;
		}
		
		/// <value>
		/// The total number of lines in the document.
		/// </value>
		int TotalNumberOfLines {
			get;
		}
		
		/// <remarks>
		/// Returns a valid line number for the given offset.
		/// </remarks>
		/// <param name="offset">
		/// A offset which points to a character in the line which
		/// line number is returned.
		/// </param>
		/// <returns>
		/// An int which value is the line number.
		/// </returns>
		/// <exception cref="System.ArgumentException">If offset points not to a valid position</exception>
		int GetLineNumberForOffset(int offset);
		
		/// <remarks>
		/// Returns a <see cref="LineSegment"/> for the given offset.
		/// </remarks>
		/// <param name="offset">
		/// A offset which points to a character in the line which
		/// is returned.
		/// </param>
		/// <returns>
		/// A <see cref="LineSegment"/> object.
		/// </returns>
		/// <exception cref="System.ArgumentException">If offset points not to a valid position</exception>
		LineSegment GetLineSegmentForOffset(int offset);
		
		/// <remarks>
		/// Returns a <see cref="LineSegment"/> for the given line number.
		/// This function should be used to get a line instead of getting the
		/// line using the <see cref="ArrayList"/>.
		/// </remarks>
		/// <param name="lineNumber">
		/// The line number which is requested.
		/// </param>
		/// <returns>
		/// A <see cref="LineSegment"/> object.
		/// </returns>
		/// <exception cref="System.ArgumentException">If offset points not to a valid position</exception>
		LineSegment GetLineSegment(int lineNumber);
		
		/// <remarks>
		/// Get the first logical line for a given visible line.
		/// example : lineNumber == 100 foldings are in the linetracker
		/// between 0..1 (2 folded, invisible lines) this method returns 102
		/// the 'logical' line number
		/// </remarks>
		int GetFirstLogicalLine(int lineNumber);
		
		/// <remarks>
		/// Get the last logical line for a given visible line.
		/// example : lineNumber == 100 foldings are in the linetracker
		/// between 0..1 (2 folded, invisible lines) this method returns 102
		/// the 'logical' line number
		/// </remarks>
		int GetLastLogicalLine(int lineNumber);
		
		/// <remarks>
		/// Get the visible line for a given logical line.
		/// example : lineNumber == 100 foldings are in the linetracker
		/// between 0..1 (2 folded, invisible lines) this method returns 98
		/// the 'visible' line number
		/// </remarks>
		int GetVisibleLine(int lineNumber);
		
//		/// <remarks>
//		/// Get the visible column for a given logical line and logical column.
//		/// </remarks>
//		int GetVisibleColumn(int logicalLine, int logicalColumn);
		
		/// <remarks>
		/// Get the next visible line after lineNumber
		/// </remarks>
		int GetNextVisibleLineAbove(int lineNumber, int lineCount);
		
		/// <remarks>
		/// Get the next visible line below lineNumber
		/// </remarks>
		int GetNextVisibleLineBelow(int lineNumber, int lineCount);
		
		event EventHandler<LineLengthChangeEventArgs> LineLengthChanged;
		event EventHandler<LineCountChangeEventArgs> LineCountChanged;
		event EventHandler<LineEventArgs> LineDeleted;
		#endregion

		#region ITextBufferStrategy interface
		/// <value>
		/// Get the whole text as string.
		/// When setting the text using the TextContent property, the undo stack is cleared.
		/// Set TextContent only for actions such as loading a file; if you want to change the current document
		/// use the Replace method instead.
		/// </value>
		string TextContent {
			get;
			set;
		}
		
		/// <value>
		/// The current length of the sequence of characters that can be edited.
		/// </value>
		int TextLength {
			get;
		}
		
		/// <summary>
		/// Inserts a string of characters into the sequence.
		/// </summary>
		/// <param name="offset">
		/// offset where to insert the string.
		/// </param>
		/// <param name="text">
		/// text to be inserted.
		/// </param>
		void Insert(int offset, string text);
		
		/// <summary>
		/// Removes some portion of the sequence.
		/// </summary>
		/// <param name="offset">
		/// offset of the remove.
		/// </param>
		/// <param name="length">
		/// number of characters to remove.
		/// </param>
		void Remove(int offset, int length);
		
		/// <summary>
		/// Replace some portion of the sequence.
		/// </summary>
		/// <param name="offset">
		/// offset.
		/// </param>
		/// <param name="length">
		/// number of characters to replace.
		/// </param>
		/// <param name="text">
		/// text to be replaced with.
		/// </param>
		void Replace(int offset, int length, string text);
		
		/// <summary>
		/// Returns a specific char of the sequence.
		/// </summary>
		/// <param name="offset">
		/// Offset of the char to get.
		/// </param>
		char GetCharAt(int offset);
		
		/// <summary>
		/// Fetches a string of characters contained in the sequence.
		/// </summary>
		/// <param name="offset">
		/// Offset into the sequence to fetch
		/// </param>
		/// <param name="length">
		/// number of characters to copy.
		/// </param>
		string GetText(int offset, int length);
		#endregion
		string GetText(ISegment segment);
		
		#region ITextModel interface
		/// <summary>
		/// returns the logical line/column position from an offset
		/// </summary>
		TextLocation OffsetToPosition(int offset);
		
		/// <summary>
		/// returns the offset from a logical line/column position
		/// </summary>
		int PositionToOffset(TextLocation p);
		#endregion
		/// <value>
		/// A container where all TextAreaUpdate objects get stored
		/// </value>
		List<TextAreaUpdate> UpdateQueue {
			get;
		}
		
		/// <remarks>
		/// Requests an update of the textarea
		/// </remarks>
		void RequestUpdate(TextAreaUpdate update);
		
		/// <remarks>
		/// Commits all updates in the queue to the textarea (the
		/// textarea will be painted)
		/// </remarks>
		void CommitUpdate();
		
		/// <summary>
		/// Moves, Resizes, Removes a list of segments on insert/remove/replace events.
		/// </summary>
		void UpdateSegmentListOnDocumentChange<T>(List<T> list, DocumentEventArgs e) where T : ISegment;
		
		/// <summary>
		/// Is fired when CommitUpdate is called
		/// </summary>
		event EventHandler UpdateCommited;
		
		/// <summary>
		/// </summary>
		event DocumentEventHandler DocumentAboutToBeChanged;
		
		/// <summary>
		/// </summary>
		event DocumentEventHandler DocumentChanged;
		
		event EventHandler TextContentChanged;
	}
}
