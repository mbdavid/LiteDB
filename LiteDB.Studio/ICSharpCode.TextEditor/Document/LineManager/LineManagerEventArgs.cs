// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;

namespace ICSharpCode.TextEditor.Document
{
	public class LineCountChangeEventArgs : EventArgs
	{
		IDocument document;
		int       start;
		int       moved;
		
		/// <returns>
		/// always a valid Document which is related to the Event.
		/// </returns>
		public IDocument Document {
			get {
				return document;
			}
		}
		
		/// <returns>
		/// -1 if no offset was specified for this event
		/// </returns>
		public int LineStart {
			get {
				return start;
			}
		}
		
		/// <returns>
		/// -1 if no length was specified for this event
		/// </returns>
		public int LinesMoved {
			get {
				return moved;
			}
		}
		
		public LineCountChangeEventArgs(IDocument document, int lineStart, int linesMoved)
		{
			this.document = document;
			this.start    = lineStart;
			this.moved    = linesMoved;
		}
	}
	
	public class LineEventArgs : EventArgs
	{
		IDocument document;
		LineSegment lineSegment;
		
		public IDocument Document {
			get { return document; }
		}
		
		public LineSegment LineSegment {
			get { return lineSegment; }
		}
		
		public LineEventArgs(IDocument document, LineSegment lineSegment)
		{
			this.document = document;
			this.lineSegment = lineSegment;
		}
		
		public override string ToString()
		{
			return string.Format("[LineEventArgs Document={0} LineSegment={1}]", this.document, this.lineSegment);
		}
	}
	
	public class LineLengthChangeEventArgs : LineEventArgs
	{
		int lengthDelta;
		
		public int LengthDelta {
			get { return lengthDelta; }
		}
		
		public LineLengthChangeEventArgs(IDocument document, LineSegment lineSegment, int moved)
			: base(document, lineSegment)
		{
			this.lengthDelta = moved;
		}
		
		public override string ToString()
		{
			return string.Format("[LineLengthEventArgs Document={0} LineSegment={1} LengthDelta={2}]", this.Document, this.LineSegment, this.lengthDelta);
		}
	}
}
