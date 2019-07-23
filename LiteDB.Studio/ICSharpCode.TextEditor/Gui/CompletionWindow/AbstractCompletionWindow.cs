// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Drawing;
using System.Windows.Forms;

namespace ICSharpCode.TextEditor.Gui.CompletionWindow
{
	/// <summary>
	/// Description of AbstractCompletionWindow.
	/// </summary>
	public abstract class AbstractCompletionWindow : System.Windows.Forms.Form
	{
		protected TextEditorControl control;
		protected Size              drawingSize;
		Rectangle workingScreen;
		Form parentForm;
		
		protected AbstractCompletionWindow(Form parentForm, TextEditorControl control)
		{
			workingScreen = Screen.GetWorkingArea(parentForm);
//			SetStyle(ControlStyles.Selectable, false);
			this.parentForm = parentForm;
			this.control  = control;
			
			SetLocation();
			StartPosition   = FormStartPosition.Manual;
			FormBorderStyle = FormBorderStyle.None;
			ShowInTaskbar   = false;
			MinimumSize     = new Size(1, 1);
			Size            = new Size(1, 1);
		}
		
		protected virtual void SetLocation()
		{
			TextArea textArea = control.ActiveTextAreaControl.TextArea;
			TextLocation caretPos  = textArea.Caret.Position;
			
			int xpos = textArea.TextView.GetDrawingXPos(caretPos.Y, caretPos.X);
			int rulerHeight = textArea.TextEditorProperties.ShowHorizontalRuler ? textArea.TextView.FontHeight : 0;
			Point pos = new Point(textArea.TextView.DrawingPosition.X + xpos,
			                      textArea.TextView.DrawingPosition.Y + (textArea.Document.GetVisibleLine(caretPos.Y)) * textArea.TextView.FontHeight 
			                      - textArea.TextView.TextArea.VirtualTop.Y + textArea.TextView.FontHeight + rulerHeight);
			
			Point location = control.ActiveTextAreaControl.PointToScreen(pos);
			
			// set bounds
			Rectangle bounds = new Rectangle(location, drawingSize);
			
			if (!workingScreen.Contains(bounds)) {
				if (bounds.Right > workingScreen.Right) {
					bounds.X = workingScreen.Right - bounds.Width;
				}
				if (bounds.Left < workingScreen.Left) {
					bounds.X = workingScreen.Left;
				}
				if (bounds.Top < workingScreen.Top) {
					bounds.Y = workingScreen.Top;
				}
				if (bounds.Bottom > workingScreen.Bottom) {
					bounds.Y = bounds.Y - bounds.Height - control.ActiveTextAreaControl.TextArea.TextView.FontHeight;
					if (bounds.Bottom > workingScreen.Bottom) {
						bounds.Y = workingScreen.Bottom - bounds.Height;
					}
				}
			}
			Bounds = bounds;
		}
		
		protected override CreateParams CreateParams {
			get {
				CreateParams p = base.CreateParams;
				AddShadowToWindow(p);
				return p;
			}
		}
		
		static int shadowStatus;
		
		/// <summary>
		/// Adds a shadow to the create params if it is supported by the operating system.
		/// </summary>
		public static void AddShadowToWindow(CreateParams createParams)
		{
			if (shadowStatus == 0) {
				// Test OS version
				shadowStatus = -1; // shadow not supported
				if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
					Version ver = Environment.OSVersion.Version;
					if (ver.Major > 5 || ver.Major == 5 && ver.Minor >= 1) {
						shadowStatus = 1;
					}
				}
			}
			if (shadowStatus == 1) {
				createParams.ClassStyle |= 0x00020000; // set CS_DROPSHADOW
			}
		}
		
		protected override bool ShowWithoutActivation {
			get {
				return true;
			}
		}
		
		protected void ShowCompletionWindow()
		{
			Owner = parentForm;
			Enabled = true;
			this.Show();
			
			control.Focus();
			
			if (parentForm != null) {
				parentForm.LocationChanged += new EventHandler(this.ParentFormLocationChanged);
			}
			
			control.ActiveTextAreaControl.VScrollBar.ValueChanged     += new EventHandler(ParentFormLocationChanged);
			control.ActiveTextAreaControl.HScrollBar.ValueChanged     += new EventHandler(ParentFormLocationChanged);
			control.ActiveTextAreaControl.TextArea.DoProcessDialogKey += new DialogKeyProcessor(ProcessTextAreaKey);
			control.ActiveTextAreaControl.Caret.PositionChanged       += new EventHandler(CaretOffsetChanged);
			control.ActiveTextAreaControl.TextArea.LostFocus          += new EventHandler(this.TextEditorLostFocus);
			control.Resize += new EventHandler(ParentFormLocationChanged);
			
			foreach (Control c in Controls) {
				c.MouseMove += ControlMouseMove;
			}
		}
		
		void ParentFormLocationChanged(object sender, EventArgs e)
		{
			SetLocation();
		}
		
		public virtual bool ProcessKeyEvent(char ch)
		{
			return false;
		}
		
		protected virtual bool ProcessTextAreaKey(Keys keyData)
		{
			if (!Visible) {
				return false;
			}
			switch (keyData) {
				case Keys.Escape:
					Close();
					return true;
			}
			return false;
		}
		
		protected virtual void CaretOffsetChanged(object sender, EventArgs e)
		{
		}
		
		protected void TextEditorLostFocus(object sender, EventArgs e)
		{
			if (!control.ActiveTextAreaControl.TextArea.Focused && !this.ContainsFocus) {
				Close();
			}
		}
		
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			
			// take out the inserted methods
			parentForm.LocationChanged -= new EventHandler(ParentFormLocationChanged);
			
			foreach (Control c in Controls) {
				c.MouseMove -= ControlMouseMove;
			}
			
			if (control.ActiveTextAreaControl.VScrollBar != null) {
				control.ActiveTextAreaControl.VScrollBar.ValueChanged -= new EventHandler(ParentFormLocationChanged);
			}
			if (control.ActiveTextAreaControl.HScrollBar != null) {
				control.ActiveTextAreaControl.HScrollBar.ValueChanged -= new EventHandler(ParentFormLocationChanged);
			}
			
			control.ActiveTextAreaControl.TextArea.LostFocus          -= new EventHandler(this.TextEditorLostFocus);
			control.ActiveTextAreaControl.Caret.PositionChanged       -= new EventHandler(CaretOffsetChanged);
			control.ActiveTextAreaControl.TextArea.DoProcessDialogKey -= new DialogKeyProcessor(ProcessTextAreaKey);
			control.Resize -= new EventHandler(ParentFormLocationChanged);
			Dispose();
		}
		
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			ControlMouseMove(this, e);
		}
		
		/// <summary>
		/// Invoked when the mouse moves over this form or any child control.
		/// Shows the mouse cursor on the text area if it has been hidden.
		/// </summary>
		/// <remarks>
		/// Derived classes should attach this handler to the MouseMove event
		/// of all created controls which are not added to the Controls
		/// collection.
		/// </remarks>
		protected void ControlMouseMove(object sender, MouseEventArgs e)
		{
			control.ActiveTextAreaControl.TextArea.ShowHiddenCursor(false);
		}
	}
}
