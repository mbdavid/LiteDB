// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald"/>
//     <version>$Revision$</version>
// </file>
using System;
using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Document
{
	/// <summary>
	/// A list of events that are fired after the line manager has finished working.
	/// </summary>
	struct DeferredEventList
	{
		internal List<LineSegment> removedLines;
		internal List<TextAnchor> textAnchor;
		
		public void AddRemovedLine(LineSegment line)
		{
			if (removedLines == null)
				removedLines = new List<LineSegment>();
			removedLines.Add(line);
		}
		
		public void AddDeletedAnchor(TextAnchor anchor)
		{
			if (textAnchor == null)
				textAnchor = new List<TextAnchor>();
			textAnchor.Add(anchor);
		}
		
		public void RaiseEvents()
		{
			// removedLines is raised by the LineManager
			if (textAnchor != null) {
				foreach (TextAnchor a in textAnchor) {
					a.RaiseDeleted();
				}
			}
		}
	}
}
