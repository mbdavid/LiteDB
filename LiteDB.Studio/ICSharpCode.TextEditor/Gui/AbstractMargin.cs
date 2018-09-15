// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Drawing;
using System.Windows.Forms;

using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor
{
	public delegate void MarginMouseEventHandler(AbstractMargin sender, Point mousepos, MouseButtons mouseButtons);
	public delegate void MarginPaintEventHandler(AbstractMargin sender, Graphics g, Rectangle rect);
	
	/// <summary>
	/// This class views the line numbers and folding markers.
	/// </summary>
	public abstract class AbstractMargin
	{
		Cursor cursor = Cursors.Default;
		
		protected Rectangle drawingPosition = new Rectangle(0, 0, 0, 0);
		protected TextArea textArea;
		
		public Rectangle DrawingPosition {
			get {
				return drawingPosition;
			}
			set {
				drawingPosition = value;
			}
		}
		
		public TextArea TextArea {
			get {
				return textArea;
			}
		}
		
		public IDocument Document {
			get {
				return textArea.Document;
			}
		}
		
		public ITextEditorProperties TextEditorProperties {
			get {
				return textArea.Document.TextEditorProperties;
			}
		}
		
		public virtual Cursor Cursor {
			get {
				return cursor;
			}
			set {
				cursor = value;
			}
		}
		
		public virtual Size Size {
			get {
				return new Size(-1, -1);
			}
		}
		
		public virtual bool IsVisible {
			get {
				return true;
			}
		}
		
		protected AbstractMargin(TextArea textArea)
		{
			this.textArea = textArea;
		}
		
		public virtual void HandleMouseDown(Point mousepos, MouseButtons mouseButtons)
		{
			if (MouseDown != null) {
				MouseDown(this, mousepos, mouseButtons);
			}
		}
		public virtual void HandleMouseMove(Point mousepos, MouseButtons mouseButtons)
		{
			if (MouseMove != null) {
				MouseMove(this, mousepos, mouseButtons);
			}
		}
		public virtual void HandleMouseLeave(EventArgs e)
		{
			if (MouseLeave != null) {
				MouseLeave(this, e);
			}
		}
		
		public virtual void Paint(Graphics g, Rectangle rect)
		{
			if (Painted != null) {
				Painted(this, g, rect);
			}
		}
		
		public event MarginPaintEventHandler Painted;
		public event MarginMouseEventHandler MouseDown;
		public event MarginMouseEventHandler MouseMove;
		public event EventHandler            MouseLeave;
	}
}
