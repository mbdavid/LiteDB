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
	/// <summary>
	/// Default implementation of the <see cref="ICSharpCode.TextEditor.Document.ISelection"/> interface.
	/// </summary>
	public class DefaultSelection : ISelection
	{
		IDocument document;
		bool      isRectangularSelection;
		TextLocation     startPosition;
		TextLocation     endPosition;
		
		public TextLocation StartPosition {
			get {
				return startPosition;
			}
			set {
				DefaultDocument.ValidatePosition(document, value);
				startPosition = value;
			}
		}
		
		public TextLocation EndPosition {
			get {
				return endPosition;
			}
			set {
				DefaultDocument.ValidatePosition(document, value);
				endPosition = value;
			}
		}
		
		public int Offset {
			get {
				return document.PositionToOffset(startPosition);
			}
		}
		
		public int EndOffset {
			get {
				return document.PositionToOffset(endPosition);
			}
		}
		
		public int Length {
			get {
				return EndOffset - Offset;
			}
		}
		
		/// <value>
		/// Returns true, if the selection is empty
		/// </value>
		public bool IsEmpty {
			get {
				return startPosition == endPosition;
			}
		}
		
		/// <value>
		/// Returns true, if the selection is rectangular
		/// </value>
		// TODO : make this unused property used.
		public bool IsRectangularSelection {
			get {
				return isRectangularSelection;
			}
			set {
				isRectangularSelection = value;
			}
		}
		
		/// <value>
		/// The text which is selected by this selection.
		/// </value>
		public string SelectedText {
			get {
				if (document != null) {
					if (Length < 0) {
						return null;
					}
					return document.GetText(Offset, Length);
				}
				return null;
			}
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="DefaultSelection"/>
		/// </summary>
		public DefaultSelection(IDocument document, TextLocation startPosition, TextLocation endPosition)
		{
			DefaultDocument.ValidatePosition(document, startPosition);
			DefaultDocument.ValidatePosition(document, endPosition);
			Debug.Assert(startPosition <= endPosition);
			this.document      = document;
			this.startPosition = startPosition;
			this.endPosition   = endPosition;
		}
		
		/// <summary>
		/// Converts a <see cref="DefaultSelection"/> instance to string (for debug purposes)
		/// </summary>
		public override string ToString()
		{
			return String.Format("[DefaultSelection : StartPosition={0}, EndPosition={1}]", startPosition, endPosition);
		}
		public bool ContainsPosition(TextLocation position)
		{
			if (this.IsEmpty)
				return false;
			return startPosition.Y < position.Y && position.Y  < endPosition.Y ||
				startPosition.Y == position.Y && startPosition.X <= position.X && (startPosition.Y != endPosition.Y || position.X <= endPosition.X) ||
				endPosition.Y == position.Y && startPosition.Y != endPosition.Y && position.X <= endPosition.X;
		}
		
		public bool ContainsOffset(int offset)
		{
			return Offset <= offset && offset <= EndOffset;
		}
	}
}
