// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using ICSharpCode.TextEditor.Gui.CompletionWindow;
using ICSharpCode.TextEditor.Util;

namespace ICSharpCode.TextEditor.Gui.InsightWindow
{
	public class InsightWindow : AbstractCompletionWindow
	{
		public InsightWindow(Form parentForm, TextEditorControl control) : base(parentForm, control)
		{
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
		}
		
		public void ShowInsightWindow()
		{
			if (!Visible) {
				if (insightDataProviderStack.Count > 0) {
					ShowCompletionWindow();
				}
			} else {
				Refresh();
			}
		}
		
		#region Event handling routines
		protected override bool ProcessTextAreaKey(Keys keyData)
		{
			if (!Visible) {
				return false;
			}
			switch (keyData) {
				case Keys.Down:
					if (DataProvider != null && DataProvider.InsightDataCount > 0) {
						CurrentData = (CurrentData + 1) % DataProvider.InsightDataCount;
						Refresh();
					}
					return true;
				case Keys.Up:
					if (DataProvider != null && DataProvider.InsightDataCount > 0) {
						CurrentData = (CurrentData + DataProvider.InsightDataCount - 1) % DataProvider.InsightDataCount;
						Refresh();
					}
					return true;
			}
			return base.ProcessTextAreaKey(keyData);
		}
		
		protected override void CaretOffsetChanged(object sender, EventArgs e)
		{
			// move the window under the caret (don't change the x position)
			TextLocation caretPos  = control.ActiveTextAreaControl.Caret.Position;
			int y = (int)((1 + caretPos.Y) * control.ActiveTextAreaControl.TextArea.TextView.FontHeight)
				- control.ActiveTextAreaControl.TextArea.VirtualTop.Y - 1
				+ control.ActiveTextAreaControl.TextArea.TextView.DrawingPosition.Y;
			
			int xpos = control.ActiveTextAreaControl.TextArea.TextView.GetDrawingXPos(caretPos.Y, caretPos.X);
			int ypos = (control.ActiveTextAreaControl.Document.GetVisibleLine(caretPos.Y) + 1) * control.ActiveTextAreaControl.TextArea.TextView.FontHeight
				- control.ActiveTextAreaControl.TextArea.VirtualTop.Y;
			int rulerHeight = control.TextEditorProperties.ShowHorizontalRuler ? control.ActiveTextAreaControl.TextArea.TextView.FontHeight : 0;
			
			Point p = control.ActiveTextAreaControl.PointToScreen(new Point(xpos, ypos + rulerHeight));
			if (p.Y != Location.Y) {
				Location = p;
			}
			
			while (DataProvider != null && DataProvider.CaretOffsetChanged()) {
				CloseCurrentDataProvider();
			}
		}
		
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			control.ActiveTextAreaControl.TextArea.Focus();
			if (TipPainterTools.DrawingRectangle1.Contains(e.X, e.Y)) {
				CurrentData = (CurrentData + DataProvider.InsightDataCount - 1) % DataProvider.InsightDataCount;
				Refresh();
			}
			if (TipPainterTools.DrawingRectangle2.Contains(e.X, e.Y)) {
				CurrentData = (CurrentData + 1) % DataProvider.InsightDataCount;
				Refresh();
			}
		}
		
		#endregion
		
		MouseWheelHandler mouseWheelHandler = new MouseWheelHandler();
		
		public void HandleMouseWheel(MouseEventArgs e)
		{
			if (DataProvider != null && DataProvider.InsightDataCount > 0) {
				int distance = mouseWheelHandler.GetScrollAmount(e);
				if (control.TextEditorProperties.MouseWheelScrollDown)
					distance = -distance;
				if (distance > 0) {
					CurrentData = (CurrentData + 1) % DataProvider.InsightDataCount;
				} else if (distance < 0) {
					CurrentData = (CurrentData + DataProvider.InsightDataCount - 1) % DataProvider.InsightDataCount;
				}
				Refresh();
			}
		}
		
		#region Insight Window Drawing routines
		protected override void OnPaint(PaintEventArgs pe)
		{
			string methodCountMessage = null, description;
			if (DataProvider == null || DataProvider.InsightDataCount < 1) {
				description = "Unknown Method";
			} else {
				if (DataProvider.InsightDataCount > 1) {
					methodCountMessage = control.GetRangeDescription(CurrentData + 1, DataProvider.InsightDataCount);
				}
				description = DataProvider.GetInsightData(CurrentData);
			}
			
			drawingSize = TipPainterTools.GetDrawingSizeHelpTipFromCombinedDescription(this,
			                                                                           pe.Graphics,
			                                                                           Font,
			                                                                           methodCountMessage,
			                                                                           description);
			if (drawingSize != Size) {
				SetLocation();
			} else {
				TipPainterTools.DrawHelpTipFromCombinedDescription(this, pe.Graphics, Font, methodCountMessage, description);
			}
		}
		
		protected override void OnPaintBackground(PaintEventArgs pe)
		{
			pe.Graphics.FillRectangle(SystemBrushes.Info, pe.ClipRectangle);
		}
		#endregion
		
		#region InsightDataProvider handling
		Stack<InsightDataProviderStackElement> insightDataProviderStack = new Stack<InsightDataProviderStackElement>();
		
		int CurrentData {
			get {
				return insightDataProviderStack.Peek().currentData;
			}
			set {
				insightDataProviderStack.Peek().currentData = value;
			}
		}
		
		IInsightDataProvider DataProvider {
			get {
				if (insightDataProviderStack.Count == 0) {
					return null;
				}
				return insightDataProviderStack.Peek().dataProvider;
			}
		}
		
		public void AddInsightDataProvider(IInsightDataProvider provider, string fileName)
		{
			provider.SetupDataProvider(fileName, control.ActiveTextAreaControl.TextArea);
			if (provider.InsightDataCount > 0) {
				insightDataProviderStack.Push(new InsightDataProviderStackElement(provider));
			}
		}
		
		void CloseCurrentDataProvider()
		{
			insightDataProviderStack.Pop();
			if (insightDataProviderStack.Count == 0) {
				Close();
			} else {
				Refresh();
			}
		}
		
		class InsightDataProviderStackElement
		{
			public int                  currentData;
			public IInsightDataProvider dataProvider;
			
			public InsightDataProviderStackElement(IInsightDataProvider dataProvider)
			{
				this.currentData  = Math.Max(dataProvider.DefaultIndex, 0);
				this.dataProvider = dataProvider;
			}
		}
		#endregion
	}
}
