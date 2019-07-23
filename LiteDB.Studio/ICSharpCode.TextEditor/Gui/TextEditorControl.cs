// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor
{
	/// <summary>
	/// This class is used for a basic text area control
	/// </summary>
	[ToolboxBitmap("LiteDB.Studio.ICSharpCode.TextEditor.Resources.TextEditorControl.bmp")]
	[ToolboxItem(true)]
	public class TextEditorControl : TextEditorControlBase
	{
		protected Panel textAreaPanel     = new Panel();
		TextAreaControl primaryTextArea;
		Splitter        textAreaSplitter  = null;
		TextAreaControl secondaryTextArea = null;
		
		PrintDocument   printDocument = null;
        string highlighting;
		
		[Browsable(false)]
		public PrintDocument PrintDocument {
			get {
				if (printDocument == null) {
					printDocument = new PrintDocument();
					printDocument.BeginPrint += new PrintEventHandler(this.BeginPrint);
					printDocument.PrintPage  += new PrintPageEventHandler(this.PrintPage);
				}
				return printDocument;
			}
		}
		
		TextAreaControl activeTextAreaControl;
		
		public override TextAreaControl ActiveTextAreaControl {
			get {
				return activeTextAreaControl;
			}
		}
		
		protected void SetActiveTextAreaControl(TextAreaControl value)
		{
			if (activeTextAreaControl != value) {
				activeTextAreaControl = value;
				
				if (ActiveTextAreaControlChanged != null) {
					ActiveTextAreaControlChanged(this, EventArgs.Empty);
				}
			}
		}
		
		public event EventHandler ActiveTextAreaControlChanged;

        [EditorBrowsable(EditorBrowsableState.Always), Browsable(true)]
        [Category("Behavior")]
        [DefaultValue(true)]
        public bool AutoHideScrollbars {
            get {
                return activeTextAreaControl.AutoHideScrollbars;
            }
            set {
                activeTextAreaControl.AutoHideScrollbars = value;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Always), Browsable(true)]
        [Category("Appearance")]
        [Description("The Syntax Highlighting to use.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [TypeConverter(typeof(HighlightStringConverter))]
        [DefaultValue("C#")]
        public string Highlighting {
            get {
                return highlighting;
            }
            set {
                highlighting = value;
                SetHighlighting(highlighting ?? "Default");
            }
        }

        public TextEditorControl()
		{
			SetStyle(ControlStyles.ContainerControl, true);
			
			textAreaPanel.Dock = DockStyle.Fill;
			
			Document = (new DocumentFactory()).CreateDocument();
			Document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategy();
			
			primaryTextArea  = new TextAreaControl(this);
			activeTextAreaControl = primaryTextArea;
			primaryTextArea.TextArea.GotFocus += delegate {
				SetActiveTextAreaControl(primaryTextArea);
			};
			primaryTextArea.Dock = DockStyle.Fill;
			textAreaPanel.Controls.Add(primaryTextArea);
			InitializeTextAreaControl(primaryTextArea);
			Controls.Add(textAreaPanel);
			ResizeRedraw = true;
			Document.UpdateCommited += new EventHandler(CommitUpdateRequested);
			OptionsChanged();
		}

        public TextEditorControl Append(string s, bool refresh = true) {
            // http://community.icsharpcode.net/forums/t/9931.aspx
            // Is this really the best way to do this?
            Document.Insert(Document.TextLength, s);
            if (refresh) {
                ActiveTextAreaControl.JumpTo(Document.TotalNumberOfLines,
                    Document.LineSegmentCollection[Document.TotalNumberOfLines - 1].Length, true);
            }
            return this;
        }

        public TextEditorControl AppendLine(string s, bool refresh = true) {
            return Append($"{(Document.TextLength > 0 ? Environment.NewLine : string.Empty)}{s}", refresh);
        }

        public TextEditorControl Clear() {
            Document.Remove(0, Document.TextLength);
            Refresh();
            return this;
        }
		
		protected virtual void InitializeTextAreaControl(TextAreaControl newControl)
		{
		}
		
		public override void OptionsChanged()
		{
			primaryTextArea.OptionsChanged();
			if (secondaryTextArea != null) {
				secondaryTextArea.OptionsChanged();
			}
		}
		
		public void Split()
		{
			if (secondaryTextArea == null) {
				secondaryTextArea = new TextAreaControl(this);
				secondaryTextArea.Dock = DockStyle.Bottom;
				secondaryTextArea.Height = Height / 2;
				
				secondaryTextArea.TextArea.GotFocus += delegate {
					SetActiveTextAreaControl(secondaryTextArea);
				};
				
				textAreaSplitter =  new Splitter();
				textAreaSplitter.BorderStyle = BorderStyle.FixedSingle ;
				textAreaSplitter.Height = 8;
				textAreaSplitter.Dock = DockStyle.Bottom;
				textAreaPanel.Controls.Add(textAreaSplitter);
				textAreaPanel.Controls.Add(secondaryTextArea);
				InitializeTextAreaControl(secondaryTextArea);
				secondaryTextArea.OptionsChanged();
			} else {
				SetActiveTextAreaControl(primaryTextArea);
				
				textAreaPanel.Controls.Remove(secondaryTextArea);
				textAreaPanel.Controls.Remove(textAreaSplitter);
				
				secondaryTextArea.Dispose();
				textAreaSplitter.Dispose();
				secondaryTextArea = null;
				textAreaSplitter  = null;
			}
		}
		
		[Browsable(false)]
		public bool EnableUndo {
			get {
				return Document.UndoStack.CanUndo;
			}
		}
		
		[Browsable(false)]
		public bool EnableRedo {
			get {
				return Document.UndoStack.CanRedo;
			}
		}

		public void Undo()
		{
			if (Document.ReadOnly) {
				return;
			}
			if (Document.UndoStack.CanUndo) {
				BeginUpdate();
				Document.UndoStack.Undo();
				
				Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
				this.primaryTextArea.TextArea.UpdateMatchingBracket();
				if (secondaryTextArea != null) {
					this.secondaryTextArea.TextArea.UpdateMatchingBracket();
				}
				EndUpdate();
			}
		}
		
		public void Redo()
		{
			if (Document.ReadOnly) {
				return;
			}
			if (Document.UndoStack.CanRedo) {
				BeginUpdate();
				Document.UndoStack.Redo();
				
				Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
				this.primaryTextArea.TextArea.UpdateMatchingBracket();
				if (secondaryTextArea != null) {
					this.secondaryTextArea.TextArea.UpdateMatchingBracket();
				}
				EndUpdate();
			}
		}
		
		public virtual void SetHighlighting(string name)
		{
			Document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategy(name);
		}
		
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (printDocument != null) {
					printDocument.BeginPrint -= new PrintEventHandler(this.BeginPrint);
					printDocument.PrintPage  -= new PrintPageEventHandler(this.PrintPage);
					printDocument = null;
				}
				Document.UndoStack.ClearAll();
				Document.UpdateCommited -= new EventHandler(CommitUpdateRequested);
				if (textAreaPanel != null) {
					if (secondaryTextArea != null) {
						secondaryTextArea.Dispose();
						textAreaSplitter.Dispose();
						secondaryTextArea = null;
						textAreaSplitter  = null;
					}
					if (primaryTextArea != null) {
						primaryTextArea.Dispose();
					}
					textAreaPanel.Dispose();
					textAreaPanel = null;
				}
			}
			base.Dispose(disposing);
		}
		
		#region Update Methods
		public override void EndUpdate()
		{
			base.EndUpdate();
			Document.CommitUpdate();
			if (!IsInUpdate) {
				ActiveTextAreaControl.Caret.OnEndUpdate();
			}
		}
		
		void CommitUpdateRequested(object sender, EventArgs e)
		{
			if (IsInUpdate) {
				return;
			}
			foreach (TextAreaUpdate update in Document.UpdateQueue) {
				switch (update.TextAreaUpdateType) {
					case TextAreaUpdateType.PositionToEnd:
						this.primaryTextArea.TextArea.UpdateToEnd(update.Position.Y);
						if (this.secondaryTextArea != null) {
							this.secondaryTextArea.TextArea.UpdateToEnd(update.Position.Y);
						}
						break;
					case TextAreaUpdateType.PositionToLineEnd:
					case TextAreaUpdateType.SingleLine:
						this.primaryTextArea.TextArea.UpdateLine(update.Position.Y);
						if (this.secondaryTextArea != null) {
							this.secondaryTextArea.TextArea.UpdateLine(update.Position.Y);
						}
						break;
					case TextAreaUpdateType.SinglePosition:
						this.primaryTextArea.TextArea.UpdateLine(update.Position.Y, update.Position.X, update.Position.X);
						if (this.secondaryTextArea != null) {
							this.secondaryTextArea.TextArea.UpdateLine(update.Position.Y, update.Position.X, update.Position.X);
						}
						break;
					case TextAreaUpdateType.LinesBetween:
						this.primaryTextArea.TextArea.UpdateLines(update.Position.X, update.Position.Y);
						if (this.secondaryTextArea != null) {
							this.secondaryTextArea.TextArea.UpdateLines(update.Position.X, update.Position.Y);
						}
						break;
					case TextAreaUpdateType.WholeTextArea:
						this.primaryTextArea.TextArea.Invalidate();
						if (this.secondaryTextArea != null) {
							this.secondaryTextArea.TextArea.Invalidate();
						}
						break;
				}
			}
			Document.UpdateQueue.Clear();
//			this.primaryTextArea.TextArea.Update();
//			if (this.secondaryTextArea != null) {
//				this.secondaryTextArea.TextArea.Update();
//			}
		}
		#endregion
		
		#region Printing routines
		int          curLineNr = 0;
		float        curTabIndent = 0;
		StringFormat printingStringFormat;
		
		void BeginPrint(object sender, PrintEventArgs ev)
		{
			curLineNr = 0;
			printingStringFormat = (StringFormat)System.Drawing.StringFormat.GenericTypographic.Clone();
			
			// 100 should be enough for everyone ...err ?
			float[] tabStops = new float[100];
			for (int i = 0; i < tabStops.Length; ++i) {
				tabStops[i] = TabIndent * primaryTextArea.TextArea.TextView.WideSpaceWidth;
			}
			
			printingStringFormat.SetTabStops(0, tabStops);
		}
		
		void Advance(ref float x, ref float y, float maxWidth, float size, float fontHeight)
		{
			if (x + size < maxWidth) {
				x += size;
			} else {
				x  = curTabIndent;
				y += fontHeight;
			}
		}
		
		// btw. I hate source code duplication ... but this time I don't care !!!!
		float MeasurePrintingHeight(Graphics g, LineSegment line, float maxWidth)
		{
			float xPos = 0;
			float yPos = 0;
			float fontHeight = Font.GetHeight(g);
//			bool  gotNonWhitespace = false;
			curTabIndent = 0;
			FontContainer fontContainer = TextEditorProperties.FontContainer;
			foreach (TextWord word in line.Words) {
				switch (word.Type) {
					case TextWordType.Space:
						Advance(ref xPos, ref yPos, maxWidth, primaryTextArea.TextArea.TextView.SpaceWidth, fontHeight);
//						if (!gotNonWhitespace) {
//							curTabIndent = xPos;
//						}
						break;
					case TextWordType.Tab:
						Advance(ref xPos, ref yPos, maxWidth, TabIndent * primaryTextArea.TextArea.TextView.WideSpaceWidth, fontHeight);
//						if (!gotNonWhitespace) {
//							curTabIndent = xPos;
//						}
						break;
					case TextWordType.Word:
//						if (!gotNonWhitespace) {
//							gotNonWhitespace = true;
//							curTabIndent    += TabIndent * primaryTextArea.TextArea.TextView.GetWidth(' ');
//						}
						SizeF drawingSize = g.MeasureString(word.Word, word.GetFont(fontContainer), new SizeF(maxWidth, fontHeight * 100), printingStringFormat);
						Advance(ref xPos, ref yPos, maxWidth, drawingSize.Width, fontHeight);
						break;
				}
			}
			return yPos + fontHeight;
		}
		
		void DrawLine(Graphics g, LineSegment line, float yPos, RectangleF margin)
		{
			float xPos = 0;
			float fontHeight = Font.GetHeight(g);
//			bool  gotNonWhitespace = false;
			curTabIndent = 0 ;
			
			FontContainer fontContainer = TextEditorProperties.FontContainer;
			foreach (TextWord word in line.Words) {
				switch (word.Type) {
					case TextWordType.Space:
						Advance(ref xPos, ref yPos, margin.Width, primaryTextArea.TextArea.TextView.SpaceWidth, fontHeight);
//						if (!gotNonWhitespace) {
//							curTabIndent = xPos;
//						}
						break;
					case TextWordType.Tab:
						Advance(ref xPos, ref yPos, margin.Width, TabIndent * primaryTextArea.TextArea.TextView.WideSpaceWidth, fontHeight);
//						if (!gotNonWhitespace) {
//							curTabIndent = xPos;
//						}
						break;
					case TextWordType.Word:
//						if (!gotNonWhitespace) {
//							gotNonWhitespace = true;
//							curTabIndent    += TabIndent * primaryTextArea.TextArea.TextView.GetWidth(' ');
//						}
						g.DrawString(word.Word, word.GetFont(fontContainer), BrushRegistry.GetBrush(word.Color), xPos + margin.X, yPos);
						SizeF drawingSize = g.MeasureString(word.Word, word.GetFont(fontContainer), new SizeF(margin.Width, fontHeight * 100), printingStringFormat);
						Advance(ref xPos, ref yPos, margin.Width, drawingSize.Width, fontHeight);
						break;
				}
			}
		}
		
		void PrintPage(object sender, PrintPageEventArgs ev)
		{
			Graphics g = ev.Graphics;
			float yPos = ev.MarginBounds.Top;
			
			while (curLineNr < Document.TotalNumberOfLines) {
				LineSegment curLine  = Document.GetLineSegment(curLineNr);
				if (curLine.Words != null) {
					float drawingHeight = MeasurePrintingHeight(g, curLine, ev.MarginBounds.Width);
					if (drawingHeight + yPos > ev.MarginBounds.Bottom) {
						break;
					}
					
					DrawLine(g, curLine, yPos, ev.MarginBounds);
					yPos += drawingHeight;
				}
				++curLineNr;
			}
			
			// If more lines exist, print another page.
			ev.HasMorePages = curLineNr < Document.TotalNumberOfLines;
		}
		#endregion
	}
}
