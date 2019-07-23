// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor
{
	/// <summary>
	/// This class paints the textarea.
	/// </summary>
	[ToolboxItem(false)]
	public class TextAreaControl : Panel
	{
		TextEditorControl motherTextEditorControl;
		
		HRuler     hRuler     = null;
		
		VScrollBar vScrollBar = new VScrollBar();
		HScrollBar hScrollBar = new HScrollBar();
		TextArea   textArea;
		bool       doHandleMousewheel = true;
		bool       disposed;
        bool autoHideScrollbars = true;
		
		public TextArea TextArea {
			get {
				return textArea;
			}
		}
		
		public SelectionManager SelectionManager {
			get {
				return textArea.SelectionManager;
			}
		}
		
		public Caret Caret {
			get {
				return textArea.Caret;
			}
		}
		
		[Browsable(false)]
		public IDocument Document {
			get {
				if (motherTextEditorControl != null)
					return motherTextEditorControl.Document;
				return null;
			}
		}
		
		public ITextEditorProperties TextEditorProperties {
			get {
				if (motherTextEditorControl != null)
					return motherTextEditorControl.TextEditorProperties;
				return null;
			}
		}
		
		public VScrollBar VScrollBar {
			get {
				return vScrollBar;
			}
		}
		
		public HScrollBar HScrollBar {
			get {
				return hScrollBar;
			}
		}

        public bool AutoHideScrollbars {
            get {
                return autoHideScrollbars;
            }
            set {
                autoHideScrollbars = value;
                AdjustScrollBars();
            }
        }
		
		public bool DoHandleMousewheel {
			get {
				return doHandleMousewheel;
			}
			set {
				doHandleMousewheel = value;
			}
		}
		
		public TextAreaControl(TextEditorControl motherTextEditorControl)
		{
			this.motherTextEditorControl = motherTextEditorControl;
			
			this.textArea                = new TextArea(motherTextEditorControl, this);
			Controls.Add(textArea);
			
			vScrollBar.ValueChanged += new EventHandler(VScrollBarValueChanged);
			Controls.Add(this.vScrollBar);
			
			hScrollBar.ValueChanged += new EventHandler(HScrollBarValueChanged);
			Controls.Add(this.hScrollBar);
			ResizeRedraw = true;
            AutoHideScrollbars = true;

            Document.TextContentChanged += DocumentTextContentChanged;
			Document.DocumentChanged += AdjustScrollBarsOnDocumentChange;
			Document.UpdateCommited  += DocumentUpdateCommitted;

            ContextMenuStrip = new ContextMenu(this);
		}

        public void Undo() {
            motherTextEditorControl.Undo();
        }
		
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (!disposed) {
					disposed = true;
					Document.TextContentChanged -= DocumentTextContentChanged;
					Document.DocumentChanged -= AdjustScrollBarsOnDocumentChange;
					Document.UpdateCommited  -= DocumentUpdateCommitted;
					motherTextEditorControl = null;
					if (vScrollBar != null) {
						vScrollBar.Dispose();
						vScrollBar = null;
					}
					if (hScrollBar != null) {
						hScrollBar.Dispose();
						hScrollBar = null;
					}
					if (hRuler != null) {
						hRuler.Dispose();
						hRuler = null;
					}
				}
			}
			base.Dispose(disposing);
		}
		
		void DocumentTextContentChanged(object sender, EventArgs e)
		{
			// after the text content is changed abruptly, we need to validate the
			// caret position - otherwise the caret position is invalid for a short amount
			// of time, which can break client code that expects that the caret position is always valid
			Caret.ValidateCaretPos();
		}
		
		protected override void OnResize(System.EventArgs e)
		{
			base.OnResize(e);
			ResizeTextArea();
		}
		
		public void ResizeTextArea()
		{
			int y = 0;
			int h = 0;
			if (hRuler != null) {
				hRuler.Bounds = new Rectangle(0,
				                              0,
				                              Width - SystemInformation.HorizontalScrollBarArrowWidth,
				                              textArea.TextView.FontHeight);
				
				y = hRuler.Bounds.Bottom;
				h = hRuler.Bounds.Height;
			}

            var vScrollBarWidth = vScrollBar.Visible ? SystemInformation.HorizontalScrollBarArrowWidth : 0;
            var hScrollBarHeight = hScrollBar.Visible ? SystemInformation.VerticalScrollBarArrowHeight : 0;
			textArea.Bounds = new Rectangle(0, y,
			                                Width - vScrollBarWidth,
			                                Height - hScrollBarHeight - h);
			SetScrollBarBounds();
		}
		
		public void SetScrollBarBounds()
		{
            var vHeight = Height - (hScrollBar.Visible ? SystemInformation.VerticalScrollBarArrowHeight : 0);
			vScrollBar.Bounds = new Rectangle(textArea.Bounds.Right, 0, SystemInformation.HorizontalScrollBarArrowWidth, vHeight);
			hScrollBar.Bounds = new Rectangle(0,
			                                  textArea.Bounds.Bottom,
			                                  Width - SystemInformation.HorizontalScrollBarArrowWidth,
			                                  SystemInformation.VerticalScrollBarArrowHeight);
		}
		
		bool adjustScrollBarsOnNextUpdate;
		Point scrollToPosOnNextUpdate;
		
		void AdjustScrollBarsOnDocumentChange(object sender, DocumentEventArgs e)
		{
			if (motherTextEditorControl.IsInUpdate == false) {
				AdjustScrollBarsClearCache();
				AdjustScrollBars();
			} else {
				adjustScrollBarsOnNextUpdate = true;
			}
		}
		
		void DocumentUpdateCommitted(object sender, EventArgs e)
		{
			if (motherTextEditorControl.IsInUpdate == false) {
				Caret.ValidateCaretPos();
				
				// AdjustScrollBarsOnCommittedUpdate
				if (!scrollToPosOnNextUpdate.IsEmpty) {
					ScrollTo(scrollToPosOnNextUpdate.Y, scrollToPosOnNextUpdate.X);
				}
				if (adjustScrollBarsOnNextUpdate) {
					AdjustScrollBarsClearCache();
					AdjustScrollBars();
				}
			}
		}
		
		int[] lineLengthCache;
		const int LineLengthCacheAdditionalSize = 100;
		
		void AdjustScrollBarsClearCache()
		{
			if (lineLengthCache != null) {
				if (lineLengthCache.Length < this.Document.TotalNumberOfLines + 2 * LineLengthCacheAdditionalSize) {
					lineLengthCache = null;
				} else {
					Array.Clear(lineLengthCache, 0, lineLengthCache.Length);
				}
			}
		}
		
		public void AdjustScrollBars()
		{
			if (this.textArea == null)
				return;

			adjustScrollBarsOnNextUpdate = false;
			vScrollBar.Minimum = 0;
			// number of visible lines in document (folding!)
			vScrollBar.Maximum = textArea.MaxVScrollValue;
			int max = 0;
			
			int firstLine = textArea.TextView.FirstVisibleLine;
			int lastLine = this.Document.GetFirstLogicalLine(textArea.TextView.FirstPhysicalLine + textArea.TextView.VisibleLineCount);
			if (lastLine >= this.Document.TotalNumberOfLines)
				lastLine = this.Document.TotalNumberOfLines - 1;
			
			if (lineLengthCache == null || lineLengthCache.Length <= lastLine) {
				lineLengthCache = new int[lastLine + LineLengthCacheAdditionalSize];
			}
			
			for (int lineNumber = firstLine; lineNumber <= lastLine; lineNumber++) {
				LineSegment lineSegment = this.Document.GetLineSegment(lineNumber);
				if (Document.FoldingManager.IsLineVisible(lineNumber)) {
					if (lineLengthCache[lineNumber] > 0) {
						max = Math.Max(max, lineLengthCache[lineNumber]);
					} else {
						int visualLength = textArea.TextView.GetVisualColumnFast(lineSegment, lineSegment.Length);
						lineLengthCache[lineNumber] = Math.Max(1, visualLength);
						max = Math.Max(max, visualLength);
					}
				}
			}
			hScrollBar.Minimum = 0;
			hScrollBar.Maximum = (Math.Max(max + 20, textArea.TextView.VisibleColumnCount - 1));
			
			vScrollBar.LargeChange = Math.Max(0, textArea.TextView.DrawingPosition.Height);
			vScrollBar.SmallChange = Math.Max(0, textArea.TextView.FontHeight);
			
			hScrollBar.LargeChange = Math.Max(0, textArea.TextView.VisibleColumnCount - 1);
			hScrollBar.SmallChange = Math.Max(0, (int)textArea.TextView.SpaceWidth);

            if (AutoHideScrollbars) {
                var vVisible = Document.TotalNumberOfLines > TextArea.TextView.VisibleLineCount - 1;
                var hVisible = max > TextArea.TextView.VisibleColumnCount;
                var changed = vScrollBar.Visible != vVisible || hScrollBar.Visible != hVisible;

                vScrollBar.Visible = vVisible;
                hScrollBar.Visible = hVisible;
                if (changed)
                    ResizeTextArea();
            } else  {
                vScrollBar.Visible = true;
                hScrollBar.Visible = true;
            }

        }

        public void OptionsChanged()
		{
			textArea.OptionsChanged();
			
			if (textArea.TextEditorProperties.ShowHorizontalRuler) {
				if (hRuler == null) {
					hRuler = new HRuler(textArea);
					Controls.Add(hRuler);
					ResizeTextArea();
				} else {
					hRuler.Invalidate();
				}
			} else {
				if (hRuler != null) {
					Controls.Remove(hRuler);
					hRuler.Dispose();
					hRuler = null;
					ResizeTextArea();
				}
			}
			
			AdjustScrollBars();
		}
		
		void VScrollBarValueChanged(object sender, EventArgs e)
		{
			textArea.VirtualTop = new Point(textArea.VirtualTop.X, vScrollBar.Value);
			textArea.Invalidate();
			AdjustScrollBars();
		}
		
		void HScrollBarValueChanged(object sender, EventArgs e)
		{
			textArea.VirtualTop = new Point(hScrollBar.Value * textArea.TextView.WideSpaceWidth, textArea.VirtualTop.Y);
			textArea.Invalidate();
		}
		
		Util.MouseWheelHandler mouseWheelHandler = new Util.MouseWheelHandler();
		
		public void HandleMouseWheel(MouseEventArgs e)
		{
			int scrollDistance = mouseWheelHandler.GetScrollAmount(e);
			if (scrollDistance == 0)
				return;
			if ((Control.ModifierKeys & Keys.Control) != 0 && TextEditorProperties.MouseWheelTextZoom) {
				if (scrollDistance > 0) {
					motherTextEditorControl.Font = new Font(motherTextEditorControl.Font.Name,
					                                        motherTextEditorControl.Font.Size + 1);
				} else {
					motherTextEditorControl.Font = new Font(motherTextEditorControl.Font.Name,
					                                        Math.Max(6, motherTextEditorControl.Font.Size - 1));
				}
			} else {
				if (TextEditorProperties.MouseWheelScrollDown)
					scrollDistance = -scrollDistance;
				int newValue = vScrollBar.Value + vScrollBar.SmallChange * scrollDistance;
				vScrollBar.Value = Math.Max(vScrollBar.Minimum, Math.Min(vScrollBar.Maximum - vScrollBar.LargeChange + 1, newValue));
			}
		}
		
		protected override void OnMouseWheel(MouseEventArgs e)
		{
			base.OnMouseWheel(e);
			if (DoHandleMousewheel) {
				HandleMouseWheel(e);
			}
		}
		
		public void ScrollToCaret()
		{
			ScrollTo(textArea.Caret.Line, textArea.Caret.Column);
		}
		
		public void ScrollTo(int line, int column)
		{
			if (motherTextEditorControl.IsInUpdate) {
				scrollToPosOnNextUpdate = new Point(column, line);
				return;
			} else {
				scrollToPosOnNextUpdate = Point.Empty;
			}
			
			ScrollTo(line);
			
			int curCharMin  = (int)(this.hScrollBar.Value - this.hScrollBar.Minimum);
			int curCharMax  = curCharMin + textArea.TextView.VisibleColumnCount;
			
			int pos = textArea.TextView.GetVisualColumn(line, column);
			
			if (textArea.TextView.VisibleColumnCount < 0) {
				hScrollBar.Value = 0;
			} else {
				if (pos < curCharMin) {
					hScrollBar.Value = (int)(Math.Max(0, pos - scrollMarginHeight));
				} else {
					if (pos > curCharMax) {
						hScrollBar.Value = (int)Math.Max(0, Math.Min(hScrollBar.Maximum, (pos - textArea.TextView.VisibleColumnCount + scrollMarginHeight)));
					}
				}
			}
		}
		
		int scrollMarginHeight  = 3;
		
		/// <summary>
		/// Ensure that <paramref name="line"/> is visible.
		/// </summary>
		public void ScrollTo(int line)
		{
			line = Math.Max(0, Math.Min(Document.TotalNumberOfLines - 1, line));
			line = Document.GetVisibleLine(line);
			int curLineMin = textArea.TextView.FirstPhysicalLine;
			if (textArea.TextView.LineHeightRemainder > 0) {
				curLineMin ++;
			}
			
			if (line - scrollMarginHeight + 3 < curLineMin) {
				this.vScrollBar.Value =  Math.Max(0, Math.Min(this.vScrollBar.Maximum, (line - scrollMarginHeight + 3) * textArea.TextView.FontHeight)) ;
				VScrollBarValueChanged(this, EventArgs.Empty);
			} else {
				int curLineMax = curLineMin + this.textArea.TextView.VisibleLineCount;
				if (line + scrollMarginHeight - 1 > curLineMax) {
					if (this.textArea.TextView.VisibleLineCount == 1) {
						this.vScrollBar.Value =  Math.Max(0, Math.Min(this.vScrollBar.Maximum, (line - scrollMarginHeight - 1) * textArea.TextView.FontHeight)) ;
					} else {
						this.vScrollBar.Value = Math.Min(this.vScrollBar.Maximum,
						                                 (line - this.textArea.TextView.VisibleLineCount + scrollMarginHeight - 1)* textArea.TextView.FontHeight) ;
					}
					VScrollBarValueChanged(this, EventArgs.Empty);
				}
			}
		}
		
		/// <summary>
		/// Scroll so that the specified line is centered.
		/// </summary>
		/// <param name="line">Line to center view on</param>
		/// <param name="treshold">If this action would cause scrolling by less than or equal to
		/// <paramref name="treshold"/> lines in any direction, don't scroll.
		/// Use -1 to always center the view.</param>
		public void CenterViewOn(int line, int treshold)
		{
			line = Math.Max(0, Math.Min(Document.TotalNumberOfLines - 1, line));
			// convert line to visible line:
			line = Document.GetVisibleLine(line);
			// subtract half the visible line count
			line -= textArea.TextView.VisibleLineCount / 2;
			
			int curLineMin = textArea.TextView.FirstPhysicalLine;
			if (textArea.TextView.LineHeightRemainder > 0) {
				curLineMin ++;
			}
			if (Math.Abs(curLineMin - line) > treshold) {
				// scroll:
				this.vScrollBar.Value =  Math.Max(0, Math.Min(this.vScrollBar.Maximum, (line - scrollMarginHeight + 3) * textArea.TextView.FontHeight)) ;
				VScrollBarValueChanged(this, EventArgs.Empty);
			}
		}
		
		public void JumpTo(int line)
		{
			line = Math.Max(0, Math.Min(line, Document.TotalNumberOfLines - 1));
			string text = Document.GetText(Document.GetLineSegment(line));
			JumpTo(line, text.Length - text.TrimStart().Length);
		}
		
		public void JumpTo(int line, int column, bool dontFocus = false)
		{
            if(!dontFocus)
    			textArea.Focus();
			textArea.SelectionManager.ClearSelection();
			textArea.Caret.Position = new TextLocation(column, line);
			textArea.SetDesiredColumn();
			ScrollToCaret();
		}
		
		public event MouseEventHandler ShowContextMenu;
		
		protected override void WndProc(ref Message m)
		{
			if (m.Msg == 0x007B) { // handle WM_CONTEXTMENU
				if (ShowContextMenu != null) {
					long lParam = m.LParam.ToInt64();
					int x = unchecked((short)(lParam & 0xffff));
					int y = unchecked((short)((lParam & 0xffff0000) >> 16));
					if (x == -1 && y == -1) {
						Point pos = Caret.ScreenPosition;
						ShowContextMenu(this, new MouseEventArgs(MouseButtons.None, 0, pos.X, pos.Y + textArea.TextView.FontHeight, 0));
					} else {
						Point pos = PointToClient(new Point(x, y));
						ShowContextMenu(this, new MouseEventArgs(MouseButtons.Right, 1, pos.X, pos.Y, 0));
					}
				}
			}
			base.WndProc(ref m);
		}
		
		protected override void OnEnter(EventArgs e)
		{
			// SD2-1072 - Make sure the caret line is valid if anyone
			// has handlers for the Enter event.
			Caret.ValidateCaretPos();
			base.OnEnter(e);
		}
	}
}
