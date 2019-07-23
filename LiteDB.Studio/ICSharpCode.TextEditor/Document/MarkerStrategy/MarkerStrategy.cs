// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Drawing;

namespace ICSharpCode.TextEditor.Document
{
	/// <summary>
	/// Manages the list of markers and provides ways to retrieve markers for specific positions.
	/// </summary>
	public sealed class MarkerStrategy
	{
		List<TextMarker> textMarker = new List<TextMarker>();
		IDocument document;
		
		public IDocument Document {
			get {
				return document;
			}
		}
		
		public IEnumerable<TextMarker> TextMarker {
			get {
				return textMarker.AsReadOnly();
			}
		}
		
		public void AddMarker(TextMarker item)
		{
			markersTable.Clear();
			textMarker.Add(item);
		}
		
		public void InsertMarker(int index, TextMarker item)
		{
			markersTable.Clear();
			textMarker.Insert(index, item);
		}
		
		public void RemoveMarker(TextMarker item)
		{
			markersTable.Clear();
			textMarker.Remove(item);
		}
		
		public void RemoveAll(Predicate<TextMarker> match)
		{
			markersTable.Clear();
			textMarker.RemoveAll(match);
		}
		
		public MarkerStrategy(IDocument document)
		{
			this.document = document;
			document.DocumentChanged += new DocumentEventHandler(DocumentChanged);
		}
		
		Dictionary<int, List<TextMarker>> markersTable = new Dictionary<int, List<TextMarker>>();
		
		public List<TextMarker> GetMarkers(int offset)
		{
			if (!markersTable.ContainsKey(offset)) {
				List<TextMarker> markers = new List<TextMarker>();
				for (int i = 0; i < textMarker.Count; ++i) {
					TextMarker marker = (TextMarker)textMarker[i];
					if (marker.Offset <= offset && offset <= marker.EndOffset) {
						markers.Add(marker);
					}
				}
				markersTable[offset] = markers;
			}
			return markersTable[offset];
		}
		
		public List<TextMarker> GetMarkers(int offset, int length)
		{
			int endOffset = offset + length - 1;
			List<TextMarker> markers = new List<TextMarker>();
			for (int i = 0; i < textMarker.Count; ++i) {
                TextMarker marker = (TextMarker)textMarker[i];
                int markerOffset = marker.Offset;
                int markerEndOffset = marker.EndOffset;
				if (// start in marker region
                    markerOffset <= offset && offset <= markerEndOffset ||
				    // end in marker region
                    markerOffset <= endOffset && endOffset <= markerEndOffset ||
				    // marker start in region
                    offset <= markerOffset && markerOffset <= endOffset ||
				    // marker end in region
                    offset <= markerEndOffset && markerEndOffset <= endOffset
				   )
				{
					markers.Add(marker);
				}
			}
			return markers;
		}
		
		public List<TextMarker> GetMarkers(TextLocation position)
		{
			if (position.Y >= document.TotalNumberOfLines || position.Y < 0) {
				return new List<TextMarker>();
			}
			LineSegment segment = document.GetLineSegment(position.Y);
			return GetMarkers(segment.Offset + position.X);
		}
		
		void DocumentChanged(object sender, DocumentEventArgs e)
		{
			// reset markers table
			markersTable.Clear();
			document.UpdateSegmentListOnDocumentChange(textMarker, e);
		}
	}
}
