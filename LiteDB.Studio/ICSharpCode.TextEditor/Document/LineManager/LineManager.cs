// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ICSharpCode.TextEditor.Document
{
	internal sealed class LineManager
	{
		LineSegmentTree lineCollection = new LineSegmentTree();
		
		IDocument document;
		IHighlightingStrategy highlightingStrategy;
		
		public IList<LineSegment> LineSegmentCollection {
			get {
				return lineCollection;
			}
		}
		
		public int TotalNumberOfLines {
			get {
				return lineCollection.Count;
			}
		}
		
		public IHighlightingStrategy HighlightingStrategy {
			get {
				return highlightingStrategy;
			}
			set {
				if (highlightingStrategy != value) {
					highlightingStrategy = value;
					if (highlightingStrategy != null) {
						highlightingStrategy.MarkTokens(document);
					}
				}
			}
		}
		
		public LineManager(IDocument document, IHighlightingStrategy highlightingStrategy)
		{
			this.document = document;
			this.highlightingStrategy = highlightingStrategy;
		}
		
		public int GetLineNumberForOffset(int offset)
		{
			return GetLineSegmentForOffset(offset).LineNumber;
		}
		
		public LineSegment GetLineSegmentForOffset(int offset)
		{
			return lineCollection.GetByOffset(offset);
		}
		
		public LineSegment GetLineSegment(int lineNr)
		{
			return lineCollection[lineNr];
		}
		
		public void Insert(int offset, string text)
		{
			Replace(offset, 0, text);
		}
		
		public void Remove(int offset, int length)
		{
			Replace(offset, length, String.Empty);
		}
		
		public void Replace(int offset, int length, string text)
		{
			//Debug.WriteLine("Replace offset="+offset+" length="+length+" text.Length="+text.Length);
			int lineStart = GetLineNumberForOffset(offset);
			int oldNumberOfLines = this.TotalNumberOfLines;
			DeferredEventList deferredEventList = new DeferredEventList();
			RemoveInternal(ref deferredEventList, offset, length);
			int numberOfLinesAfterRemoving = this.TotalNumberOfLines;
			if (!string.IsNullOrEmpty(text)) {
				InsertInternal(offset, text);
			}
//			#if DEBUG
//			Console.WriteLine("New line collection:");
//			Console.WriteLine(lineCollection.GetTreeAsString());
//			Console.WriteLine("New text:");
//			Console.WriteLine("'" + document.TextContent + "'");
//			#endif
			// Only fire events after RemoveInternal+InsertInternal finished completely:
			// Otherwise we would expose inconsistent state to the event handlers.
			RunHighlighter(lineStart, 1 + Math.Max(0, this.TotalNumberOfLines - numberOfLinesAfterRemoving));
			
			if (deferredEventList.removedLines != null) {
				foreach (LineSegment ls in deferredEventList.removedLines)
					OnLineDeleted(new LineEventArgs(document, ls));
			}
			deferredEventList.RaiseEvents();
			if (this.TotalNumberOfLines != oldNumberOfLines) {
				OnLineCountChanged(new LineCountChangeEventArgs(document, lineStart, this.TotalNumberOfLines - oldNumberOfLines));
			}
		}
		
		void RemoveInternal(ref DeferredEventList deferredEventList, int offset, int length)
		{
			Debug.Assert(length >= 0);
			if (length == 0) return;
			LineSegmentTree.Enumerator it = lineCollection.GetEnumeratorForOffset(offset);
			LineSegment startSegment = it.Current;
			int startSegmentOffset = startSegment.Offset;
			if (offset + length < startSegmentOffset + startSegment.TotalLength) {
				// just removing a part of this line segment
				startSegment.RemovedLinePart(ref deferredEventList, offset - startSegmentOffset, length);
				SetSegmentLength(startSegment, startSegment.TotalLength - length);
				return;
			}
			// merge startSegment with another line segment because startSegment's delimiter was deleted
			// possibly remove lines in between if multiple delimiters were deleted
			int charactersRemovedInStartLine = startSegmentOffset + startSegment.TotalLength - offset;
			Debug.Assert(charactersRemovedInStartLine > 0);
			startSegment.RemovedLinePart(ref deferredEventList, offset - startSegmentOffset, charactersRemovedInStartLine);
			
			
			LineSegment endSegment = lineCollection.GetByOffset(offset + length);
			if (endSegment == startSegment) {
				// special case: we are removing a part of the last line up to the
				// end of the document
				SetSegmentLength(startSegment, startSegment.TotalLength - length);
				return;
			}
			int endSegmentOffset = endSegment.Offset;
			int charactersLeftInEndLine = endSegmentOffset + endSegment.TotalLength - (offset + length);
			endSegment.RemovedLinePart(ref deferredEventList, 0, endSegment.TotalLength - charactersLeftInEndLine);
			startSegment.MergedWith(endSegment, offset - startSegmentOffset);
			SetSegmentLength(startSegment, startSegment.TotalLength - charactersRemovedInStartLine + charactersLeftInEndLine);
			startSegment.DelimiterLength = endSegment.DelimiterLength;
			// remove all segments between startSegment (excl.) and endSegment (incl.)
			it.MoveNext();
			LineSegment segmentToRemove;
			do {
				segmentToRemove = it.Current;
				it.MoveNext();
				lineCollection.RemoveSegment(segmentToRemove);
				segmentToRemove.Deleted(ref deferredEventList);
			} while (segmentToRemove != endSegment);
		}
		
		void InsertInternal(int offset, string text)
		{
			LineSegment segment = lineCollection.GetByOffset(offset);
			DelimiterSegment ds = NextDelimiter(text, 0);
			if (ds == null) {
				// no newline is being inserted, all text is inserted in a single line
				segment.InsertedLinePart(offset - segment.Offset, text.Length);
				SetSegmentLength(segment, segment.TotalLength + text.Length);
				return;
			}
			LineSegment firstLine = segment;
			firstLine.InsertedLinePart(offset - firstLine.Offset, ds.Offset);
			int lastDelimiterEnd = 0;
			while (ds != null) {
				// split line segment at line delimiter
				int lineBreakOffset = offset + ds.Offset + ds.Length;
				int segmentOffset = segment.Offset;
				int lengthAfterInsertionPos = segmentOffset + segment.TotalLength - (offset + lastDelimiterEnd);
				lineCollection.SetSegmentLength(segment, lineBreakOffset - segmentOffset);
				LineSegment newSegment = lineCollection.InsertSegmentAfter(segment, lengthAfterInsertionPos);
				segment.DelimiterLength = ds.Length;
				
				segment = newSegment;
				lastDelimiterEnd = ds.Offset + ds.Length;
				
				ds = NextDelimiter(text, lastDelimiterEnd);
			}
			firstLine.SplitTo(segment);
			// insert rest after last delimiter
			if (lastDelimiterEnd != text.Length) {
				segment.InsertedLinePart(0, text.Length - lastDelimiterEnd);
				SetSegmentLength(segment, segment.TotalLength + text.Length - lastDelimiterEnd);
			}
		}
		
		void SetSegmentLength(LineSegment segment, int newTotalLength)
		{
			int delta = newTotalLength - segment.TotalLength;
			if (delta != 0) {
				lineCollection.SetSegmentLength(segment, newTotalLength);
				OnLineLengthChanged(new LineLengthChangeEventArgs(document, segment, delta));
			}
		}
		
		void RunHighlighter(int firstLine, int lineCount)
		{
			if (highlightingStrategy != null) {
				List<LineSegment> markLines = new List<LineSegment>();
				LineSegmentTree.Enumerator it = lineCollection.GetEnumeratorForIndex(firstLine);
				for (int i = 0; i < lineCount && it.IsValid; i++) {
					markLines.Add(it.Current);
					it.MoveNext();
				}
				highlightingStrategy.MarkTokens(document, markLines);
			}
		}
		
		public void SetContent(string text)
		{
			lineCollection.Clear();
			if (text != null) {
				Replace(0, 0, text);
			}
		}
		
		public int GetVisibleLine(int logicalLineNumber)
		{
			if (!document.TextEditorProperties.EnableFolding) {
				return logicalLineNumber;
			}
			
			int visibleLine = 0;
			int foldEnd = 0;
			List<FoldMarker> foldings = document.FoldingManager.GetTopLevelFoldedFoldings();
			foreach (FoldMarker fm in foldings) {
				if (fm.StartLine >= logicalLineNumber) {
					break;
				}
				if (fm.StartLine >= foldEnd) {
					visibleLine += fm.StartLine - foldEnd;
					if (fm.EndLine > logicalLineNumber) {
						return visibleLine;
					}
					foldEnd = fm.EndLine;
				}
			}
//			Debug.Assert(logicalLineNumber >= foldEnd);
			visibleLine += logicalLineNumber - foldEnd;
			return visibleLine;
		}
		
		public int GetFirstLogicalLine(int visibleLineNumber)
		{
			if (!document.TextEditorProperties.EnableFolding) {
				return visibleLineNumber;
			}
			int v = 0;
			int foldEnd = 0;
			List<FoldMarker> foldings = document.FoldingManager.GetTopLevelFoldedFoldings();
			foreach (FoldMarker fm in foldings) {
				if (fm.StartLine >= foldEnd) {
					if (v + fm.StartLine - foldEnd >= visibleLineNumber) {
						break;
					}
					v += fm.StartLine - foldEnd;
					foldEnd = fm.EndLine;
				}
			}
			// help GC
			foldings.Clear();
			foldings = null;
			return foldEnd + visibleLineNumber - v;
		}
		
		public int GetLastLogicalLine(int visibleLineNumber)
		{
			if (!document.TextEditorProperties.EnableFolding) {
				return visibleLineNumber;
			}
			return GetFirstLogicalLine(visibleLineNumber + 1) - 1;
		}
		
		// TODO : speedup the next/prev visible line search
		// HOW? : save the foldings in a sorted list and lookup the
		//        line numbers in this list
		public int GetNextVisibleLineAbove(int lineNumber, int lineCount)
		{
			int curLineNumber = lineNumber;
			if (document.TextEditorProperties.EnableFolding) {
				for (int i = 0; i < lineCount && curLineNumber < TotalNumberOfLines; ++i) {
					++curLineNumber;
					while (curLineNumber < TotalNumberOfLines && (curLineNumber >= lineCollection.Count || !document.FoldingManager.IsLineVisible(curLineNumber))) {
						++curLineNumber;
					}
				}
			} else {
				curLineNumber += lineCount;
			}
			return Math.Min(TotalNumberOfLines - 1, curLineNumber);
		}
		
		public int GetNextVisibleLineBelow(int lineNumber, int lineCount)
		{
			int curLineNumber = lineNumber;
			if (document.TextEditorProperties.EnableFolding) {
				for (int i = 0; i < lineCount; ++i) {
					--curLineNumber;
					while (curLineNumber >= 0 && !document.FoldingManager.IsLineVisible(curLineNumber)) {
						--curLineNumber;
					}
				}
			} else {
				curLineNumber -= lineCount;
			}
			return Math.Max(0, curLineNumber);
		}
		
		// use always the same DelimiterSegment object for the NextDelimiter
		DelimiterSegment delimiterSegment = new DelimiterSegment();
		
		DelimiterSegment NextDelimiter(string text, int offset)
		{
			for (int i = offset; i < text.Length; i++) {
				switch (text[i]) {
					case '\r':
						if (i + 1 < text.Length) {
							if (text[i + 1] == '\n') {
								delimiterSegment.Offset = i;
								delimiterSegment.Length = 2;
								return delimiterSegment;
							}
						}
						#if DATACONSISTENCYTEST
						Debug.Assert(false, "Found lone \\r, data consistency problems?");
						#endif
						goto case '\n';
					case '\n':
						delimiterSegment.Offset = i;
						delimiterSegment.Length = 1;
						return delimiterSegment;
				}
			}
			return null;
		}
		
		void OnLineCountChanged(LineCountChangeEventArgs e)
		{
			if (LineCountChanged != null) {
				LineCountChanged(this, e);
			}
		}
		
		void OnLineLengthChanged(LineLengthChangeEventArgs e)
		{
			if (LineLengthChanged != null) {
				LineLengthChanged(this, e);
			}
		}
		
		void OnLineDeleted(LineEventArgs e)
		{
			if (LineDeleted != null) {
				LineDeleted(this, e);
			}
		}
		
		public event EventHandler<LineLengthChangeEventArgs> LineLengthChanged;
		public event EventHandler<LineCountChangeEventArgs> LineCountChanged;
		public event EventHandler<LineEventArgs> LineDeleted;
		
		sealed class DelimiterSegment
		{
			internal int Offset;
			internal int Length;
		}
	}
}
