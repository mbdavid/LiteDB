// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Drawing;

namespace ICSharpCode.TextEditor
{
	public delegate void ToolTipRequestEventHandler(object sender, ToolTipRequestEventArgs e);
	
	public class ToolTipRequestEventArgs
	{
		Point mousePosition;
		TextLocation logicalPosition;
		bool inDocument;
		
		public Point MousePosition {
			get {
				return mousePosition;
			}
		}
		
		public TextLocation LogicalPosition {
			get {
				return logicalPosition;
			}
		}
		
		public bool InDocument {
			get {
				return inDocument;
			}
		}
		
		/// <summary>
		/// Gets if some client handling the event has already shown a tool tip.
		/// </summary>
		public bool ToolTipShown {
			get {
				return toolTipText != null;
			}
		}
		
		internal string toolTipText;
		
		public void ShowToolTip(string text)
		{
			toolTipText = text;
		}
		
		public ToolTipRequestEventArgs(Point mousePosition, TextLocation logicalPosition, bool inDocument)
		{
			this.mousePosition = mousePosition;
			this.logicalPosition = logicalPosition;
			this.inDocument = inDocument;
		}
	}
}
