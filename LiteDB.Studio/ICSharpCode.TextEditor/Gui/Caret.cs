// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;

using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor
{
	/// <summary>
	/// In this enumeration are all caret modes listed.
	/// </summary>
	public enum CaretMode {
		/// <summary>
		/// If the caret is in insert mode typed characters will be
		/// inserted at the caret position
		/// </summary>
		InsertMode,
		
		/// <summary>
		/// If the caret is in overwirte mode typed characters will
		/// overwrite the character at the caret position
		/// </summary>
		OverwriteMode
	}
	
	
	public class Caret : System.IDisposable
	{
		int       line          = 0;
		int       column        = 0;
		int       desiredXPos   = 0;
		CaretMode caretMode;
		
		static bool     caretCreated = false;
		bool     hidden       = true;
		TextArea textArea;
		Point    currentPos   = new Point(-1, -1);
		Ime      ime          = null;
		CaretImplementation caretImplementation;
		
		/// <value>
		/// The 'prefered' xPos in which the caret moves, when it is moved
		/// up/down. Measured in pixels, not in characters!
		/// </value>
		public int DesiredColumn {
			get {
				return desiredXPos;
			}
			set {
				desiredXPos = value;
			}
		}
		
		/// <value>
		/// The current caret mode.
		/// </value>
		public CaretMode CaretMode {
			get {
				return caretMode;
			}
			set {
				caretMode = value;
				OnCaretModeChanged(EventArgs.Empty);
			}
		}
		
		public int Line {
			get {
				return line;
			}
			set {
				line = value;
				ValidateCaretPos();
				UpdateCaretPosition();
				OnPositionChanged(EventArgs.Empty);
			}
		}
		
		public int Column {
			get {
				return column;
			}
			set {
				column = value;
				ValidateCaretPos();
				UpdateCaretPosition();
				OnPositionChanged(EventArgs.Empty);
			}
		}
		
		public TextLocation Position {
			get {
				return new TextLocation(column, line);
			}
			set {
				line   = value.Y;
				column = value.X;
				ValidateCaretPos();
				UpdateCaretPosition();
				OnPositionChanged(EventArgs.Empty);
			}
		}
		
		public int Offset {
			get {
				return textArea.Document.PositionToOffset(Position);
			}
		}
		
		public Caret(TextArea textArea)
		{
			this.textArea = textArea;
			textArea.GotFocus  += new EventHandler(GotFocus);
			textArea.LostFocus += new EventHandler(LostFocus);
			if (Environment.OSVersion.Platform == PlatformID.Unix)
				caretImplementation = new ManagedCaret(this);
			else
				caretImplementation = new Win32Caret(this);
		}
		
		public void Dispose()
		{
			textArea.GotFocus  -= new EventHandler(GotFocus);
			textArea.LostFocus -= new EventHandler(LostFocus);
			textArea = null;
			caretImplementation.Dispose();
		}
		
		public TextLocation ValidatePosition(TextLocation pos)
		{
			int line   = Math.Max(0, Math.Min(textArea.Document.TotalNumberOfLines - 1, pos.Y));
			int column = Math.Max(0, pos.X);
			
			if (column == int.MaxValue || !textArea.TextEditorProperties.AllowCaretBeyondEOL) {
				LineSegment lineSegment = textArea.Document.GetLineSegment(line);
				column = Math.Min(column, lineSegment.Length);
			}
			return new TextLocation(column, line);
		}
		
		/// <remarks>
		/// If the caret position is outside the document text bounds
		/// it is set to the correct position by calling ValidateCaretPos.
		/// </remarks>
		public void ValidateCaretPos()
		{
			line = Math.Max(0, Math.Min(textArea.Document.TotalNumberOfLines - 1, line));
			column = Math.Max(0, column);
			
			if (column == int.MaxValue || !textArea.TextEditorProperties.AllowCaretBeyondEOL) {
				LineSegment lineSegment = textArea.Document.GetLineSegment(line);
				column = Math.Min(column, lineSegment.Length);
			}
		}
		
		void CreateCaret()
		{
			while (!caretCreated) {
				switch (caretMode) {
					case CaretMode.InsertMode:
						caretCreated = caretImplementation.Create(2, textArea.TextView.FontHeight);
						break;
					case CaretMode.OverwriteMode:
						caretCreated = caretImplementation.Create((int)textArea.TextView.SpaceWidth, textArea.TextView.FontHeight);
						break;
				}
			}
			if (currentPos.X  < 0) {
				ValidateCaretPos();
				currentPos = ScreenPosition;
			}
			caretImplementation.SetPosition(currentPos.X, currentPos.Y);
			caretImplementation.Show();
		}
		
		public void RecreateCaret()
		{
			Log("RecreateCaret");
			DisposeCaret();
			if (!hidden) {
				CreateCaret();
			}
		}
		
		void DisposeCaret()
		{
			if (caretCreated) {
				caretCreated = false;
				caretImplementation.Hide();
				caretImplementation.Destroy();
			}
		}
		
		void GotFocus(object sender, EventArgs e)
		{
			Log("GotFocus, IsInUpdate=" + textArea.MotherTextEditorControl.IsInUpdate);
			hidden = false;
			if (!textArea.MotherTextEditorControl.IsInUpdate) {
				CreateCaret();
				UpdateCaretPosition();
			}
		}
		
		void LostFocus(object sender, EventArgs e)
		{
			Log("LostFocus");
			hidden = true;
			DisposeCaret();
		}
		
		public Point ScreenPosition {
			get {
				int xpos = textArea.TextView.GetDrawingXPos(this.line, this.column);
				return new Point(textArea.TextView.DrawingPosition.X + xpos,
				                 textArea.TextView.DrawingPosition.Y
				                 + (textArea.Document.GetVisibleLine(this.line)) * textArea.TextView.FontHeight
				                 - textArea.TextView.TextArea.VirtualTop.Y);
			}
		}
		int oldLine = -1;
		bool outstandingUpdate;
		
		internal void OnEndUpdate()
		{
			if (outstandingUpdate)
				UpdateCaretPosition();
		}

		void PaintCaretLine(Graphics g)
		{
			if (!textArea.Document.TextEditorProperties.CaretLine)
				return;

			HighlightColor caretLineColor = textArea.Document.HighlightingStrategy.GetColorFor("CaretLine");

			g.DrawLine(BrushRegistry.GetDotPen(caretLineColor.Color),
			           currentPos.X,
			           0,
			           currentPos.X,
			           textArea.DisplayRectangle.Height);
		}

		public void UpdateCaretPosition()
		{
			Log("UpdateCaretPosition");
			
			if (textArea.TextEditorProperties.CaretLine) {
				textArea.Invalidate();
			} else {
				if (caretImplementation.RequireRedrawOnPositionChange) {
					textArea.UpdateLine(oldLine);
					if (line != oldLine)
						textArea.UpdateLine(line);
				} else {
					if (textArea.MotherTextAreaControl.TextEditorProperties.LineViewerStyle == LineViewerStyle.FullRow && oldLine != line) {
						textArea.UpdateLine(oldLine);
						textArea.UpdateLine(line);
					}
				}
			}
			oldLine = line;
			
			
			if (hidden || textArea.MotherTextEditorControl.IsInUpdate) {
				outstandingUpdate = true;
				return;
			} else {
				outstandingUpdate = false;
			}
			ValidateCaretPos();
			int lineNr = this.line;
			int xpos = textArea.TextView.GetDrawingXPos(lineNr, this.column);
			//LineSegment lineSegment = textArea.Document.GetLineSegment(lineNr);
			Point pos = ScreenPosition;
			if (xpos >= 0) {
				CreateCaret();
				bool success = caretImplementation.SetPosition(pos.X, pos.Y);
				if (!success) {
					caretImplementation.Destroy();
					caretCreated = false;
					UpdateCaretPosition();
				}
			} else {
				caretImplementation.Destroy();
			}
			
			// set the input method editor location
			if (ime == null) {
				ime = new Ime(textArea.Handle, textArea.Document.TextEditorProperties.Font);
			} else {
				ime.HWnd = textArea.Handle;
				ime.Font = textArea.Document.TextEditorProperties.Font;
			}
			ime.SetIMEWindowLocation(pos.X, pos.Y);
			
			currentPos = pos;
		}

		[Conditional("DEBUG")]
		static void Log(string text)
		{
			//Console.WriteLine(text);
		}
		
		#region Caret implementation
		internal void PaintCaret(Graphics g)
		{
			caretImplementation.PaintCaret(g);
			PaintCaretLine(g);
		}
		
		abstract class CaretImplementation : IDisposable
		{
			public bool RequireRedrawOnPositionChange;
			
			public abstract bool Create(int width, int height);
			public abstract void Hide();
			public abstract void Show();
			public abstract bool SetPosition(int x, int y);
			public abstract void PaintCaret(Graphics g);
			public abstract void Destroy();
			
			public virtual void Dispose()
			{
				Destroy();
			}
		}
		
		class ManagedCaret : CaretImplementation
		{
			System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer { Interval = 300 };
			bool visible;
			bool blink = true;
			int x, y, width, height;
			TextArea textArea;
			Caret parentCaret;
			
			public ManagedCaret(Caret caret)
			{
				base.RequireRedrawOnPositionChange = true;
				this.textArea = caret.textArea;
				this.parentCaret = caret;
				timer.Tick += CaretTimerTick;
			}
			
			void CaretTimerTick(object sender, EventArgs e)
			{
				blink = !blink;
				if (visible)
					textArea.UpdateLine(parentCaret.Line);
			}
			
			public override bool Create(int width, int height)
			{
				this.visible = true;
				this.width = width - 2;
				this.height = height;
				timer.Enabled = true;
				return true;
			}
			public override void Hide()
			{
				visible = false;
			}
			public override void Show()
			{
				visible = true;
			}
			public override bool SetPosition(int x, int y)
			{
				this.x = x - 1;
				this.y = y;
				return true;
			}
			public override void PaintCaret(Graphics g)
			{
				if (visible && blink)
					g.DrawRectangle(Pens.Gray, x, y, width, height);
			}
			public override void Destroy()
			{
				visible = false;
				timer.Enabled = false;
			}
			public override void Dispose()
			{
				base.Dispose();
				timer.Dispose();
			}
		}
		
		class Win32Caret : CaretImplementation
		{
			[DllImport("User32.dll")]
			static extern bool CreateCaret(IntPtr hWnd, int hBitmap, int nWidth, int nHeight);
			
			[DllImport("User32.dll")]
			static extern bool SetCaretPos(int x, int y);
			
			[DllImport("User32.dll")]
			static extern bool DestroyCaret();
			
			[DllImport("User32.dll")]
			static extern bool ShowCaret(IntPtr hWnd);
			
			[DllImport("User32.dll")]
			static extern bool HideCaret(IntPtr hWnd);
			
			TextArea textArea;
			
			public Win32Caret(Caret caret)
			{
				this.textArea = caret.textArea;
			}
			
			public override bool Create(int width, int height)
			{
				return CreateCaret(textArea.Handle, 0, width, height);
			}
			public override void Hide()
			{
				HideCaret(textArea.Handle);
			}
			public override void Show()
			{
				ShowCaret(textArea.Handle);
			}
			public override bool SetPosition(int x, int y)
			{
				return SetCaretPos(x, y);
			}
			public override void PaintCaret(Graphics g)
			{
			}
			public override void Destroy()
			{
				DestroyCaret();
			}
		}
		#endregion
		
		bool firePositionChangedAfterUpdateEnd;
		
		void FirePositionChangedAfterUpdateEnd(object sender, EventArgs e)
		{
			OnPositionChanged(EventArgs.Empty);
		}
		
		protected virtual void OnPositionChanged(EventArgs e)
		{
			if (this.textArea.MotherTextEditorControl.IsInUpdate) {
				if (firePositionChangedAfterUpdateEnd == false) {
					firePositionChangedAfterUpdateEnd = true;
					this.textArea.Document.UpdateCommited += FirePositionChangedAfterUpdateEnd;
				}
				return;
			} else if (firePositionChangedAfterUpdateEnd) {
				this.textArea.Document.UpdateCommited -= FirePositionChangedAfterUpdateEnd;
				firePositionChangedAfterUpdateEnd = false;
			}
			
			List<FoldMarker> foldings = textArea.Document.FoldingManager.GetFoldingsFromPosition(line, column);
			bool  shouldUpdate = false;
			foreach (FoldMarker foldMarker in foldings) {
				shouldUpdate |= foldMarker.IsFolded;
				foldMarker.IsFolded = false;
			}
			
			if (shouldUpdate) {
				textArea.Document.FoldingManager.NotifyFoldingsChanged(EventArgs.Empty);
			}
			
			if (PositionChanged != null) {
				PositionChanged(this, e);
			}
			textArea.ScrollToCaret();
		}
		
		protected virtual void OnCaretModeChanged(EventArgs e)
		{
			if (CaretModeChanged != null) {
				CaretModeChanged(this, e);
			}
			caretImplementation.Hide();
			caretImplementation.Destroy();
			caretCreated = false;
			CreateCaret();
			caretImplementation.Show();
		}
		
		/// <remarks>
		/// Is called each time the caret is moved.
		/// </remarks>
		public event EventHandler PositionChanged;
		
		/// <remarks>
		/// Is called each time the CaretMode has changed.
		/// </remarks>
		public event EventHandler CaretModeChanged;
	}
}
