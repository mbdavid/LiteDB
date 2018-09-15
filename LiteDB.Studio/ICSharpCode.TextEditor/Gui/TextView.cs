// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor
{
	/// <summary>
	/// This class paints the textarea.
	/// </summary>
	public class TextView : AbstractMargin, IDisposable
	{
		int          fontHeight;
		//Hashtable    charWitdh           = new Hashtable();
		//StringFormat measureStringFormat = (StringFormat)StringFormat.GenericTypographic.Clone();
		Highlight    highlight;
		int          physicalColumn = 0; // used for calculating physical column during paint
		
		public void Dispose()
		{
			measureCache.Clear();
			//measureStringFormat.Dispose();
		}
		
		public Highlight Highlight {
			get {
				return highlight;
			}
			set {
				highlight = value;
			}
		}
		
		public int FirstPhysicalLine {
			get {
				return textArea.VirtualTop.Y / fontHeight;
			}
		}
		public int LineHeightRemainder {
			get {
				return textArea.VirtualTop.Y % fontHeight;
			}
		}
		/// <summary>Gets the first visible <b>logical</b> line.</summary>
		public int FirstVisibleLine {
			get {
				return textArea.Document.GetFirstLogicalLine(textArea.VirtualTop.Y / fontHeight);
			}
			set {
				if (FirstVisibleLine != value) {
					textArea.VirtualTop = new Point(textArea.VirtualTop.X, textArea.Document.GetVisibleLine(value) * fontHeight);
					
				}
			}
		}
		
		public int VisibleLineDrawingRemainder {
			get {
				return textArea.VirtualTop.Y % fontHeight;
			}
		}
		
		public int FontHeight {
			get {
				return fontHeight;
			}
		}
		
		public int VisibleLineCount {
			get {
				return 1 + DrawingPosition.Height / fontHeight;
			}
		}
		
		public int VisibleColumnCount {
			get {
				return (int)(DrawingPosition.Width / WideSpaceWidth) - 1;
			}
		}
		
		public TextView(TextArea textArea) : base(textArea)
		{
			base.Cursor = Cursors.IBeam;
			OptionsChanged();
		}
		
		static int GetFontHeight(Font font)
		{
			int height1 = TextRenderer.MeasureText("_", font).Height;
			int height2 = (int)Math.Ceiling(font.GetHeight());
			return Math.Max(height1, height2) + 1;
		}
		
		int spaceWidth;
		
		/// <summary>
		/// Gets the width of a space character.
		/// This value can be quite small in some fonts - consider using WideSpaceWidth instead.
		/// </summary>
		public int SpaceWidth {
			get {
				return spaceWidth;
			}
		}
		
		int wideSpaceWidth;
		
		/// <summary>
		/// Gets the width of a 'wide space' (=one quarter of a tab, if tab is set to 4 spaces).
		/// On monospaced fonts, this is the same value as spaceWidth.
		/// </summary>
		public int WideSpaceWidth {
			get {
				return wideSpaceWidth;
			}
		}
		
		Font lastFont;
		
		public void OptionsChanged()
		{
			this.lastFont = TextEditorProperties.FontContainer.RegularFont;
			this.fontHeight = GetFontHeight(lastFont);
			// use minimum width - in some fonts, space has no width but kerning is used instead
			// -> DivideByZeroException
			this.spaceWidth = Math.Max(GetWidth(' ', lastFont), 1);
			// tab should have the width of 4*'x'
			this.wideSpaceWidth = Math.Max(spaceWidth, GetWidth('x', lastFont));
		}
		
		#region Paint functions
		public override void Paint(Graphics g, Rectangle rect)
		{
			if (rect.Width <= 0 || rect.Height <= 0) {
				return;
			}
			
			// Just to ensure that fontHeight and char widths are always correct...
			if (lastFont != TextEditorProperties.FontContainer.RegularFont) {
				OptionsChanged();
				textArea.Invalidate();
			}
			
			int horizontalDelta = textArea.VirtualTop.X;
			if (horizontalDelta > 0) {
				g.SetClip(this.DrawingPosition);
			}
			
			for (int y = 0; y < (DrawingPosition.Height + VisibleLineDrawingRemainder) / fontHeight + 1; ++y) {
				Rectangle lineRectangle = new Rectangle(DrawingPosition.X - horizontalDelta,
				                                        DrawingPosition.Top + y * fontHeight - VisibleLineDrawingRemainder,
				                                        DrawingPosition.Width + horizontalDelta,
				                                        fontHeight);
				
				if (rect.IntersectsWith(lineRectangle)) {
					int fvl = textArea.Document.GetVisibleLine(FirstVisibleLine);
					int currentLine = textArea.Document.GetFirstLogicalLine(textArea.Document.GetVisibleLine(FirstVisibleLine) + y);
					PaintDocumentLine(g, currentLine, lineRectangle);
				}
			}
			
			DrawMarkerDraw(g);
			
			if (horizontalDelta > 0) {
				g.ResetClip();
			}
			textArea.Caret.PaintCaret(g);
		}
		
		void PaintDocumentLine(Graphics g, int lineNumber, Rectangle lineRectangle)
		{
			Debug.Assert(lineNumber >= 0);
			Brush bgColorBrush    = GetBgColorBrush(lineNumber);
			Brush backgroundBrush = textArea.Enabled ? bgColorBrush : SystemBrushes.InactiveBorder;
			
			if (lineNumber >= textArea.Document.TotalNumberOfLines) {
				g.FillRectangle(backgroundBrush, lineRectangle);
				if (TextEditorProperties.ShowInvalidLines) {
					DrawInvalidLineMarker(g, lineRectangle.Left, lineRectangle.Top);
				}
				if (TextEditorProperties.ShowVerticalRuler) {
					DrawVerticalRuler(g, lineRectangle);
				}
//				bgColorBrush.Dispose();
				return;
			}
			
			int physicalXPos = lineRectangle.X;
			// there can't be a folding wich starts in an above line and ends here, because the line is a new one,
			// there must be a return before this line.
			int column = 0;
			physicalColumn = 0;
			if (TextEditorProperties.EnableFolding) {
				while (true) {
					List<FoldMarker> starts = textArea.Document.FoldingManager.GetFoldedFoldingsWithStartAfterColumn(lineNumber, column - 1);
					if (starts == null || starts.Count <= 0) {
						if (lineNumber < textArea.Document.TotalNumberOfLines) {
							physicalXPos = PaintLinePart(g, lineNumber, column, textArea.Document.GetLineSegment(lineNumber).Length, lineRectangle, physicalXPos);
						}
						break;
					}
					// search the first starting folding
					FoldMarker firstFolding = (FoldMarker)starts[0];
					foreach (FoldMarker fm in starts) {
						if (fm.StartColumn < firstFolding.StartColumn) {
							firstFolding = fm;
						}
					}
					starts.Clear();
					
					physicalXPos = PaintLinePart(g, lineNumber, column, firstFolding.StartColumn, lineRectangle, physicalXPos);
					column     = firstFolding.EndColumn;
					lineNumber = firstFolding.EndLine;
					if (lineNumber >= textArea.Document.TotalNumberOfLines) {
						Debug.Assert(false, "Folding ends after document end");
						break;
					}
					
					ColumnRange    selectionRange2 = textArea.SelectionManager.GetSelectionAtLine(lineNumber);
					bool drawSelected = ColumnRange.WholeColumn.Equals(selectionRange2) || firstFolding.StartColumn >= selectionRange2.StartColumn && firstFolding.EndColumn <= selectionRange2.EndColumn;
					
					physicalXPos = PaintFoldingText(g, lineNumber, physicalXPos, lineRectangle, firstFolding.FoldText, drawSelected);
				}
			} else {
				physicalXPos = PaintLinePart(g, lineNumber, 0, textArea.Document.GetLineSegment(lineNumber).Length, lineRectangle, physicalXPos);
			}
			
			if (lineNumber < textArea.Document.TotalNumberOfLines) {
				// Paint things after end of line
				ColumnRange    selectionRange = textArea.SelectionManager.GetSelectionAtLine(lineNumber);
				LineSegment    currentLine    = textArea.Document.GetLineSegment(lineNumber);
				HighlightColor selectionColor = textArea.Document.HighlightingStrategy.GetColorFor("Selection");
				
				bool  selectionBeyondEOL = selectionRange.EndColumn > currentLine.Length || ColumnRange.WholeColumn.Equals(selectionRange);
				
				if (TextEditorProperties.ShowEOLMarker) {
					HighlightColor eolMarkerColor = textArea.Document.HighlightingStrategy.GetColorFor("EOLMarkers");
					physicalXPos += DrawEOLMarker(g, eolMarkerColor.Color, selectionBeyondEOL ? bgColorBrush : backgroundBrush, physicalXPos, lineRectangle.Y);
				} else {
					if (selectionBeyondEOL) {
						g.FillRectangle(BrushRegistry.GetBrush(selectionColor.BackgroundColor), new RectangleF(physicalXPos, lineRectangle.Y, WideSpaceWidth, lineRectangle.Height));
						physicalXPos += WideSpaceWidth;
					}
				}
				
				Brush fillBrush = selectionBeyondEOL && TextEditorProperties.AllowCaretBeyondEOL ? bgColorBrush : backgroundBrush;
				g.FillRectangle(fillBrush,
				                new RectangleF(physicalXPos, lineRectangle.Y, lineRectangle.Width - physicalXPos + lineRectangle.X, lineRectangle.Height));
			}
			if (TextEditorProperties.ShowVerticalRuler) {
				DrawVerticalRuler(g, lineRectangle);
			}
//			bgColorBrush.Dispose();
		}
		
		bool DrawLineMarkerAtLine(int lineNumber)
		{
			return lineNumber == base.textArea.Caret.Line && textArea.MotherTextAreaControl.TextEditorProperties.LineViewerStyle == LineViewerStyle.FullRow;
		}
		
		Brush GetBgColorBrush(int lineNumber)
		{
			if (DrawLineMarkerAtLine(lineNumber)) {
				HighlightColor caretLine = textArea.Document.HighlightingStrategy.GetColorFor("CaretMarker");
				return BrushRegistry.GetBrush(caretLine.Color);
			}
			HighlightColor background = textArea.Document.HighlightingStrategy.GetColorFor("Default");
			Color bgColor = background.BackgroundColor;
			return BrushRegistry.GetBrush(bgColor);
		}
		
		const int additionalFoldTextSize = 1;
		
		int PaintFoldingText(Graphics g, int lineNumber, int physicalXPos, Rectangle lineRectangle, string text, bool drawSelected)
		{
			// TODO: get font and color from the highlighting file
			HighlightColor      selectionColor  = textArea.Document.HighlightingStrategy.GetColorFor("Selection");
			Brush               bgColorBrush    = drawSelected ? BrushRegistry.GetBrush(selectionColor.BackgroundColor) : GetBgColorBrush(lineNumber);
			Brush               backgroundBrush = textArea.Enabled ? bgColorBrush : SystemBrushes.InactiveBorder;
			
			Font font = textArea.TextEditorProperties.FontContainer.RegularFont;
			
			int wordWidth = MeasureStringWidth(g, text, font) + additionalFoldTextSize;
			Rectangle rect = new Rectangle(physicalXPos, lineRectangle.Y, wordWidth, lineRectangle.Height - 1);
			
			g.FillRectangle(backgroundBrush, rect);
			
			physicalColumn += text.Length;
			DrawString(g,
			           text,
			           font,
			           drawSelected ? selectionColor.Color : Color.Gray,
			           rect.X + 1, rect.Y);
			g.DrawRectangle(BrushRegistry.GetPen(drawSelected ? Color.DarkGray : Color.Gray), rect.X, rect.Y, rect.Width, rect.Height);
			
			return physicalXPos + wordWidth + 1;
		}
		
		struct MarkerToDraw {
			internal TextMarker marker;
			internal RectangleF drawingRect;
			
			public MarkerToDraw(TextMarker marker, RectangleF drawingRect)
			{
				this.marker = marker;
				this.drawingRect = drawingRect;
			}
		}
		
		List<MarkerToDraw> markersToDraw = new List<MarkerToDraw>();
		
		void DrawMarker(Graphics g, TextMarker marker, RectangleF drawingRect)
		{
			// draw markers later so they can overdraw the following text
			markersToDraw.Add(new MarkerToDraw(marker, drawingRect));
		}
		
		void DrawMarkerDraw(Graphics g)
		{
			foreach (MarkerToDraw m in markersToDraw) {
				TextMarker marker = m.marker;
				RectangleF drawingRect = m.drawingRect;
				float drawYPos = drawingRect.Bottom - 1;
				switch (marker.TextMarkerType) {
					case TextMarkerType.Underlined:
						g.DrawLine(BrushRegistry.GetPen(marker.Color), drawingRect.X, drawYPos, drawingRect.Right, drawYPos);
						break;
					case TextMarkerType.WaveLine:
						int reminder = ((int)drawingRect.X) % 6;
						for (float i = (int)drawingRect.X - reminder; i < drawingRect.Right; i += 6) {
							g.DrawLine(BrushRegistry.GetPen(marker.Color), i,     drawYPos + 3 - 4, i + 3, drawYPos + 1 - 4);
							if (i + 3 < drawingRect.Right) {
								g.DrawLine(BrushRegistry.GetPen(marker.Color), i + 3, drawYPos + 1 - 4, i + 6, drawYPos + 3 - 4);
							}
						}
						break;
					case TextMarkerType.SolidBlock:
						g.FillRectangle(BrushRegistry.GetBrush(marker.Color), drawingRect);
						break;
				}
			}
			markersToDraw.Clear();
		}
		
		/// <summary>
		/// Get the marker brush (for solid block markers) at a given position.
		/// </summary>
		/// <param name="offset">The offset.</param>
		/// <param name="length">The length.</param>
		/// <param name="markers">All markers that have been found.</param>
		/// <returns>The Brush or null when no marker was found.</returns>
		Brush GetMarkerBrush(IList<TextMarker> markers, ref Color foreColor)
		{
			foreach (TextMarker marker in markers) {
				if (marker.TextMarkerType == TextMarkerType.SolidBlock) {
					if (marker.OverrideForeColor) {
						foreColor = marker.ForeColor;
					}
					return BrushRegistry.GetBrush(marker.Color);
				}
			}
			return null;
		}
		
		int PaintLinePart(Graphics g, int lineNumber, int startColumn, int endColumn, Rectangle lineRectangle, int physicalXPos)
		{
			bool  drawLineMarker  = DrawLineMarkerAtLine(lineNumber);
			Brush backgroundBrush = textArea.Enabled ? GetBgColorBrush(lineNumber) : SystemBrushes.InactiveBorder;
			
			HighlightColor selectionColor = textArea.Document.HighlightingStrategy.GetColorFor("Selection");
			ColumnRange    selectionRange = textArea.SelectionManager.GetSelectionAtLine(lineNumber);
			HighlightColor tabMarkerColor   = textArea.Document.HighlightingStrategy.GetColorFor("TabMarkers");
			HighlightColor spaceMarkerColor = textArea.Document.HighlightingStrategy.GetColorFor("SpaceMarkers");
			
			LineSegment currentLine    = textArea.Document.GetLineSegment(lineNumber);
			
			Brush selectionBackgroundBrush  = BrushRegistry.GetBrush(selectionColor.BackgroundColor);
			
			if (currentLine.Words == null) {
				return physicalXPos;
			}
			
			int currentWordOffset = 0; // we cannot use currentWord.Offset because it is not set on space words
			
			TextWord currentWord;
			TextWord nextCurrentWord = null;
			FontContainer fontContainer = TextEditorProperties.FontContainer;
			for (int wordIdx = 0; wordIdx < currentLine.Words.Count; wordIdx++) {
				currentWord = currentLine.Words[wordIdx];
				if (currentWordOffset < startColumn) {
					// TODO: maybe we need to split at startColumn when we support fold markers
					// inside words
					currentWordOffset += currentWord.Length;
					continue;
				}
			repeatDrawCurrentWord:
				//physicalXPos += 10; // leave room between drawn words - useful for debugging the drawing code
				if (currentWordOffset >= endColumn || physicalXPos >= lineRectangle.Right) {
					break;
				}
				int currentWordEndOffset = currentWordOffset + currentWord.Length - 1;
				TextWordType currentWordType = currentWord.Type;
				
				Color wordForeColor;
				if (currentWordType == TextWordType.Space)
					wordForeColor = spaceMarkerColor.Color;
				else if (currentWordType == TextWordType.Tab)
					wordForeColor = tabMarkerColor.Color;
				else
					wordForeColor = currentWord.Color;

                IList<TextMarker> markers = Document.MarkerStrategy.GetMarkers(currentLine.Offset + currentWordOffset, currentWord.Length);
				Brush wordBackBrush = GetMarkerBrush(markers, ref wordForeColor);
				
				// It is possible that we have to split the current word because a marker/the selection begins/ends inside it
				if (currentWord.Length > 1) {
					int splitPos = int.MaxValue;
					if (highlight != null) {
						// split both before and after highlight
						if (highlight.OpenBrace.Y == lineNumber) {
							if (highlight.OpenBrace.X >= currentWordOffset && highlight.OpenBrace.X <= currentWordEndOffset) {
								splitPos = Math.Min(splitPos, highlight.OpenBrace.X - currentWordOffset);
							}
						}
						if (highlight.CloseBrace.Y == lineNumber) {
							if (highlight.CloseBrace.X >= currentWordOffset && highlight.CloseBrace.X <= currentWordEndOffset) {
								splitPos = Math.Min(splitPos, highlight.CloseBrace.X - currentWordOffset);
							}
						}
						if (splitPos == 0) {
							splitPos = 1; // split after highlight
						}
					}
					if (endColumn < currentWordEndOffset) { // split when endColumn is reached
						splitPos = Math.Min(splitPos, endColumn - currentWordOffset);
					}
					if (selectionRange.StartColumn > currentWordOffset && selectionRange.StartColumn <= currentWordEndOffset) {
						splitPos = Math.Min(splitPos, selectionRange.StartColumn - currentWordOffset);
					} else if (selectionRange.EndColumn > currentWordOffset && selectionRange.EndColumn <= currentWordEndOffset) {
						splitPos = Math.Min(splitPos, selectionRange.EndColumn - currentWordOffset);
					}
					foreach (TextMarker marker in markers) {
						int markerColumn = marker.Offset - currentLine.Offset;
						int markerEndColumn = marker.EndOffset - currentLine.Offset + 1; // make end offset exclusive
						if (markerColumn > currentWordOffset && markerColumn <= currentWordEndOffset) {
							splitPos = Math.Min(splitPos, markerColumn - currentWordOffset);
						} else if (markerEndColumn > currentWordOffset && markerEndColumn <= currentWordEndOffset) {
							splitPos = Math.Min(splitPos, markerEndColumn - currentWordOffset);
						}
					}
					if (splitPos != int.MaxValue) {
						if (nextCurrentWord != null)
							throw new ApplicationException("split part invalid: first part cannot be splitted further");
						nextCurrentWord = TextWord.Split(ref currentWord, splitPos);
						goto repeatDrawCurrentWord; // get markers for first word part
					}
				}
				
				// get colors from selection status:
				if (ColumnRange.WholeColumn.Equals(selectionRange) || (selectionRange.StartColumn <= currentWordOffset
				                                                       && selectionRange.EndColumn > currentWordEndOffset))
				{
					// word is completely selected
					wordBackBrush = selectionBackgroundBrush;
					if (selectionColor.HasForeground) {
						wordForeColor = selectionColor.Color;
					}
				} else if (drawLineMarker) {
					wordBackBrush = backgroundBrush;
				}
				
				if (wordBackBrush == null) { // use default background if no other background is set
					if (currentWord.SyntaxColor != null && currentWord.SyntaxColor.HasBackground)
						wordBackBrush = BrushRegistry.GetBrush(currentWord.SyntaxColor.BackgroundColor);
					else
						wordBackBrush = backgroundBrush;
				}
				
				RectangleF wordRectangle;
				
				if (currentWord.Type == TextWordType.Space) {
					++physicalColumn;
					
					wordRectangle = new RectangleF(physicalXPos, lineRectangle.Y, SpaceWidth, lineRectangle.Height);
					g.FillRectangle(wordBackBrush, wordRectangle);
					
					if (TextEditorProperties.ShowSpaces) {
						DrawSpaceMarker(g, wordForeColor, physicalXPos, lineRectangle.Y);
					}
					physicalXPos += SpaceWidth;
				} else if (currentWord.Type == TextWordType.Tab) {
					
					physicalColumn += TextEditorProperties.TabIndent;
					physicalColumn = (physicalColumn / TextEditorProperties.TabIndent) * TextEditorProperties.TabIndent;
					// go to next tabstop
					int physicalTabEnd = ((physicalXPos + MinTabWidth - lineRectangle.X)
					                      / WideSpaceWidth / TextEditorProperties.TabIndent)
						* WideSpaceWidth * TextEditorProperties.TabIndent + lineRectangle.X;
					physicalTabEnd += WideSpaceWidth * TextEditorProperties.TabIndent;
					
					wordRectangle = new RectangleF(physicalXPos, lineRectangle.Y, physicalTabEnd - physicalXPos, lineRectangle.Height);
					g.FillRectangle(wordBackBrush, wordRectangle);
					
					if (TextEditorProperties.ShowTabs) {
						DrawTabMarker(g, wordForeColor, physicalXPos, lineRectangle.Y);
					}
					physicalXPos = physicalTabEnd;
				} else {
					int wordWidth = DrawDocumentWord(g,
					                                 currentWord.Word,
					                                 new Point(physicalXPos, lineRectangle.Y),
					                                 currentWord.GetFont(fontContainer),
					                                 wordForeColor,
					                                 wordBackBrush);
					wordRectangle = new RectangleF(physicalXPos, lineRectangle.Y, wordWidth, lineRectangle.Height);
					physicalXPos += wordWidth;
				}
				foreach (TextMarker marker in markers) {
					if (marker.TextMarkerType != TextMarkerType.SolidBlock) {
						DrawMarker(g, marker, wordRectangle);
					}
				}
				
				// draw bracket highlight
				if (highlight != null) {
					if (highlight.OpenBrace.Y == lineNumber && highlight.OpenBrace.X == currentWordOffset ||
					    highlight.CloseBrace.Y == lineNumber && highlight.CloseBrace.X == currentWordOffset) {
						DrawBracketHighlight(g, new Rectangle((int)wordRectangle.X, lineRectangle.Y, (int)wordRectangle.Width - 1, lineRectangle.Height - 1));
					}
				}
				
				currentWordOffset += currentWord.Length;
				if (nextCurrentWord != null) {
					currentWord = nextCurrentWord;
					nextCurrentWord = null;
					goto repeatDrawCurrentWord;
				}
			}
			if (physicalXPos < lineRectangle.Right && endColumn >= currentLine.Length) {
				// draw markers at line end
				IList<TextMarker> markers = Document.MarkerStrategy.GetMarkers(currentLine.Offset + currentLine.Length);
				foreach (TextMarker marker in markers) {
					if (marker.TextMarkerType != TextMarkerType.SolidBlock) {
						DrawMarker(g, marker, new RectangleF(physicalXPos, lineRectangle.Y, WideSpaceWidth, lineRectangle.Height));
					}
				}
			}
			return physicalXPos;
		}
		
		int DrawDocumentWord(Graphics g, string word, Point position, Font font, Color foreColor, Brush backBrush)
		{
			if (word == null || word.Length == 0) {
				return 0;
			}
			
			if (word.Length > MaximumWordLength) {
				int width = 0;
				for (int i = 0; i < word.Length; i += MaximumWordLength) {
					Point pos = position;
					pos.X += width;
					if (i + MaximumWordLength < word.Length)
						width += DrawDocumentWord(g, word.Substring(i, MaximumWordLength), pos, font, foreColor, backBrush);
					else
						width += DrawDocumentWord(g, word.Substring(i, word.Length - i), pos, font, foreColor, backBrush);
				}
				return width;
			}
			
			int wordWidth = MeasureStringWidth(g, word, font);
			
			//num = ++num % 3;
			g.FillRectangle(backBrush, //num == 0 ? Brushes.LightBlue : num == 1 ? Brushes.LightGreen : Brushes.Yellow,
			                new RectangleF(position.X, position.Y, wordWidth + 1, FontHeight));
			
			DrawString(g,
			           word,
			           font,
			           foreColor,
			           position.X,
			           position.Y);
			return wordWidth;
		}
		
		struct WordFontPair {
			string word;
			Font font;
			public WordFontPair(string word, Font font) {
				this.word = word;
				this.font = font;
			}
			public override bool Equals(object obj) {
				WordFontPair myWordFontPair = (WordFontPair)obj;
				if (!word.Equals(myWordFontPair.word)) return false;
				return font.Equals(myWordFontPair.font);
			}
			
			public override int GetHashCode() {
				return word.GetHashCode() ^ font.GetHashCode();
			}
		}
		
		Dictionary<WordFontPair, int> measureCache = new Dictionary<WordFontPair, int>();
		
		// split words after 1000 characters. Fixes GDI+ crash on very longs words, for example
		// a 100 KB Base64-file without any line breaks.
		const int MaximumWordLength = 1000;
		const int MaximumCacheSize = 2000;
		
		int MeasureStringWidth(Graphics g, string word, Font font)
		{
			int width;
			
			if (word == null || word.Length == 0)
				return 0;
			if (word.Length > MaximumWordLength) {
				width = 0;
				for (int i = 0; i < word.Length; i += MaximumWordLength) {
					if (i + MaximumWordLength < word.Length)
						width += MeasureStringWidth(g, word.Substring(i, MaximumWordLength), font);
					else
						width += MeasureStringWidth(g, word.Substring(i, word.Length - i), font);
				}
				return width;
			}
			if (measureCache.TryGetValue(new WordFontPair(word, font), out width)) {
				return width;
			}
			if (measureCache.Count > MaximumCacheSize) {
				measureCache.Clear();
			}
			
			// This code here provides better results than MeasureString!
			// Example line that is measured wrong:
			// txt.GetPositionFromCharIndex(txt.SelectionStart)
			// (Verdana 10, highlighting makes GetP... bold) -> note the space between 'x' and '('
			// this also fixes "jumping" characters when selecting in non-monospace fonts
			// [...]
			// Replaced GDI+ measurement with GDI measurement: faster and even more exact
			width = TextRenderer.MeasureText(g, word, font, new Size(short.MaxValue, short.MaxValue), textFormatFlags).Width;
			measureCache.Add(new WordFontPair(word, font), width);
			return width;
		}
		
		// Important: Some flags combinations work on WinXP, but not on Win2000.
		// Make sure to test changes here on all operating systems.
		const TextFormatFlags textFormatFlags =
			TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.PreserveGraphicsClipping;
		#endregion
		
		#region Conversion Functions
		Dictionary<Font, Dictionary<char, int>> fontBoundCharWidth = new Dictionary<Font, Dictionary<char, int>>();
		
		public int GetWidth(char ch, Font font)
		{
			if (!fontBoundCharWidth.ContainsKey(font)) {
				fontBoundCharWidth.Add(font, new Dictionary<char, int>());
			}
			if (!fontBoundCharWidth[font].ContainsKey(ch)) {
				using (Graphics g = textArea.CreateGraphics()) {
					return GetWidth(g, ch, font);
				}
			}
			return fontBoundCharWidth[font][ch];
		}
		
		public int GetWidth(Graphics g, char ch, Font font)
		{
			if (!fontBoundCharWidth.ContainsKey(font)) {
				fontBoundCharWidth.Add(font, new Dictionary<char, int>());
			}
			if (!fontBoundCharWidth[font].ContainsKey(ch)) {
				//Console.WriteLine("Calculate character width: " + ch);
				fontBoundCharWidth[font].Add(ch, MeasureStringWidth(g, ch.ToString(), font));
			}
			return fontBoundCharWidth[font][ch];
		}
		
		public int GetVisualColumn(int logicalLine, int logicalColumn)
		{
			int column = 0;
			using (Graphics g = textArea.CreateGraphics()) {
				CountColumns(ref column, 0, logicalColumn, logicalLine, g);
			}
			return column;
		}
		
		public int GetVisualColumnFast(LineSegment line, int logicalColumn)
		{
			int lineOffset = line.Offset;
			int tabIndent = Document.TextEditorProperties.TabIndent;
			int guessedColumn = 0;
			for (int i = 0; i < logicalColumn; ++i) {
				char ch;
				if (i >= line.Length) {
					ch = ' ';
				} else {
					ch = Document.GetCharAt(lineOffset + i);
				}
				switch (ch) {
					case '\t':
						guessedColumn += tabIndent;
						guessedColumn = (guessedColumn / tabIndent) * tabIndent;
						break;
					default:
						++guessedColumn;
						break;
				}
			}
			return guessedColumn;
		}
		
		/// <summary>
		/// returns line/column for a visual point position
		/// </summary>
		public TextLocation GetLogicalPosition(Point mousePosition)
		{
			FoldMarker dummy;
			return GetLogicalColumn(GetLogicalLine(mousePosition.Y), mousePosition.X, out dummy);
		}
		
		/// <summary>
		/// returns line/column for a visual point position
		/// </summary>
		public TextLocation GetLogicalPosition(int visualPosX, int visualPosY)
		{
			FoldMarker dummy;
			return GetLogicalColumn(GetLogicalLine(visualPosY), visualPosX, out dummy);
		}
		
		/// <summary>
		/// returns line/column for a visual point position
		/// </summary>
		public FoldMarker GetFoldMarkerFromPosition(int visualPosX, int visualPosY)
		{
			FoldMarker foldMarker;
			GetLogicalColumn(GetLogicalLine(visualPosY), visualPosX, out foldMarker);
			return foldMarker;
		}
		
		/// <summary>
		/// returns logical line number for a visual point
		/// </summary>
		public int GetLogicalLine(int visualPosY)
		{
			int clickedVisualLine = Math.Max(0, (visualPosY + this.textArea.VirtualTop.Y) / fontHeight);
			return Document.GetFirstLogicalLine(clickedVisualLine);
		}
		
		internal TextLocation GetLogicalColumn(int lineNumber, int visualPosX, out FoldMarker inFoldMarker)
		{
			visualPosX += textArea.VirtualTop.X;
			
			inFoldMarker = null;
			if (lineNumber >= Document.TotalNumberOfLines) {
				return new TextLocation((int)(visualPosX / WideSpaceWidth), lineNumber);
			}
			if (visualPosX <= 0) {
				return new TextLocation(0, lineNumber);
			}
			
			int start = 0; // column
			int posX = 0; // visual position
			
			int result;
			using (Graphics g = textArea.CreateGraphics()) {
				// call GetLogicalColumnInternal to skip over text,
				// then skip over fold markers
				// and repeat as necessary.
				// The loop terminates once the correct logical column is reached in
				// GetLogicalColumnInternal or inside a fold marker.
				while (true) {
					
					LineSegment line = Document.GetLineSegment(lineNumber);
					FoldMarker nextFolding = FindNextFoldedFoldingOnLineAfterColumn(lineNumber, start-1);
					int end = nextFolding != null ? nextFolding.StartColumn : int.MaxValue;
					result = GetLogicalColumnInternal(g, line, start, end, ref posX, visualPosX);
					
					// break when GetLogicalColumnInternal found the result column
					if (result < end)
						break;
					
					// reached fold marker
					lineNumber = nextFolding.EndLine;
					start = nextFolding.EndColumn;
					int newPosX = posX + 1 + MeasureStringWidth(g, nextFolding.FoldText, TextEditorProperties.FontContainer.RegularFont);
					if (newPosX >= visualPosX) {
						inFoldMarker = nextFolding;
						if (IsNearerToAThanB(visualPosX, posX, newPosX))
							return new TextLocation(nextFolding.StartColumn, nextFolding.StartLine);
						else
							return new TextLocation(nextFolding.EndColumn, nextFolding.EndLine);
					}
					posX = newPosX;
				}
			}
			return new TextLocation(result, lineNumber);
		}
		
		int GetLogicalColumnInternal(Graphics g, LineSegment line, int start, int end, ref int drawingPos, int targetVisualPosX)
		{
			if (start == end)
				return end;
			Debug.Assert(start < end);
			Debug.Assert(drawingPos < targetVisualPosX);
			
			int tabIndent = Document.TextEditorProperties.TabIndent;
			
			/*float spaceWidth = SpaceWidth;
			float drawingPos = 0;
			LineSegment currentLine = Document.GetLineSegment(logicalLine);
			List<TextWord> words = currentLine.Words;
			if (words == null) return 0;
			int wordCount = words.Count;
			int wordOffset = 0;
			FontContainer fontContainer = TextEditorProperties.FontContainer;
			 */
			FontContainer fontContainer = TextEditorProperties.FontContainer;
			
			List<TextWord> words = line.Words;
			if (words == null) return 0;
			int wordOffset = 0;
			for (int i = 0; i < words.Count; i++) {
				TextWord word = words[i];
				if (wordOffset >= end) {
					return wordOffset;
				}
				if (wordOffset + word.Length >= start) {
					int newDrawingPos;
					switch (word.Type) {
						case TextWordType.Space:
							newDrawingPos = drawingPos + spaceWidth;
							if (newDrawingPos >= targetVisualPosX)
								return IsNearerToAThanB(targetVisualPosX, drawingPos, newDrawingPos) ? wordOffset : wordOffset+1;
							break;
						case TextWordType.Tab:
							// go to next tab position
							drawingPos = (int)((drawingPos + MinTabWidth) / tabIndent / WideSpaceWidth) * tabIndent * WideSpaceWidth;
							newDrawingPos = drawingPos + tabIndent * WideSpaceWidth;
							if (newDrawingPos >= targetVisualPosX)
								return IsNearerToAThanB(targetVisualPosX, drawingPos, newDrawingPos) ? wordOffset : wordOffset+1;
							break;
						case TextWordType.Word:
							int wordStart = Math.Max(wordOffset, start);
							int wordLength = Math.Min(wordOffset + word.Length, end) - wordStart;
							string text = Document.GetText(line.Offset + wordStart, wordLength);
							Font font = word.GetFont(fontContainer) ?? fontContainer.RegularFont;
							newDrawingPos = drawingPos + MeasureStringWidth(g, text, font);
							if (newDrawingPos >= targetVisualPosX) {
								for (int j = 0; j < text.Length; j++) {
									newDrawingPos = drawingPos + MeasureStringWidth(g, text[j].ToString(), font);
									if (newDrawingPos >= targetVisualPosX) {
										if (IsNearerToAThanB(targetVisualPosX, drawingPos, newDrawingPos))
											return wordStart + j;
										else
											return wordStart + j + 1;
									}
									drawingPos = newDrawingPos;
								}
								return wordStart + text.Length;
							}
							break;
						default:
							throw new NotSupportedException();
					}
					drawingPos = newDrawingPos;
				}
				wordOffset += word.Length;
			}
			return wordOffset;
		}
		
		static bool IsNearerToAThanB(int num, int a, int b)
		{
			return Math.Abs(a - num) < Math.Abs(b - num);
		}
		
		FoldMarker FindNextFoldedFoldingOnLineAfterColumn(int lineNumber, int column)
		{
			List<FoldMarker> list = Document.FoldingManager.GetFoldedFoldingsWithStartAfterColumn(lineNumber, column);
			if (list.Count != 0)
				return list[0];
			else
				return null;
		}
		
		const int MinTabWidth = 4;
		
		float CountColumns(ref int column, int start, int end, int logicalLine, Graphics g)
		{
			if (start > end) throw new ArgumentException("start > end");
			if (start == end) return 0;
			float spaceWidth = SpaceWidth;
			float drawingPos = 0;
			int tabIndent  = Document.TextEditorProperties.TabIndent;
			LineSegment currentLine = Document.GetLineSegment(logicalLine);
			List<TextWord> words = currentLine.Words;
			if (words == null) return 0;
			int wordCount = words.Count;
			int wordOffset = 0;
			FontContainer fontContainer = TextEditorProperties.FontContainer;
			for (int i = 0; i < wordCount; i++) {
				TextWord word = words[i];
				if (wordOffset >= end)
					break;
				if (wordOffset + word.Length >= start) {
					switch (word.Type) {
						case TextWordType.Space:
							drawingPos += spaceWidth;
							break;
						case TextWordType.Tab:
							// go to next tab position
							drawingPos = (int)((drawingPos + MinTabWidth) / tabIndent / WideSpaceWidth) * tabIndent * WideSpaceWidth;
							drawingPos += tabIndent * WideSpaceWidth;
							break;
						case TextWordType.Word:
							int wordStart = Math.Max(wordOffset, start);
							int wordLength = Math.Min(wordOffset + word.Length, end) - wordStart;
							string text = Document.GetText(currentLine.Offset + wordStart, wordLength);
							drawingPos += MeasureStringWidth(g, text, word.GetFont(fontContainer) ?? fontContainer.RegularFont);
							break;
					}
				}
				wordOffset += word.Length;
			}
			for (int j = currentLine.Length; j < end; j++) {
				drawingPos += WideSpaceWidth;
			}
			// add one pixel in column calculation to account for floating point calculation errors
			column += (int)((drawingPos + 1) / WideSpaceWidth);
			
			/* OLD Code (does not work for fonts like Verdana)
			for (int j = start; j < end; ++j) {
				char ch;
				if (j >= line.Length) {
					ch = ' ';
				} else {
					ch = Document.GetCharAt(line.Offset + j);
				}
				
				switch (ch) {
					case '\t':
						int oldColumn = column;
						column += tabIndent;
						column = (column / tabIndent) * tabIndent;
						drawingPos += (column - oldColumn) * spaceWidth;
						break;
					default:
						++column;
						TextWord word = line.GetWord(j);
						if (word == null || word.Font == null) {
							drawingPos += GetWidth(ch, TextEditorProperties.Font);
						} else {
							drawingPos += GetWidth(ch, word.Font);
						}
						break;
				}
			}
			//*/
			return drawingPos;
		}
		
		public int GetDrawingXPos(int logicalLine, int logicalColumn)
		{
			List<FoldMarker> foldings = Document.FoldingManager.GetTopLevelFoldedFoldings();
			int i;
			FoldMarker f = null;
			// search the last folding that's interresting
			for (i = foldings.Count - 1; i >= 0; --i) {
				f = foldings[i];
				if (f.StartLine < logicalLine || f.StartLine == logicalLine && f.StartColumn < logicalColumn) {
					break;
				}
				FoldMarker f2 = foldings[i / 2];
				if (f2.StartLine > logicalLine || f2.StartLine == logicalLine && f2.StartColumn >= logicalColumn) {
					i /= 2;
				}
			}
			int lastFolding  = 0;
			int firstFolding = 0;
			int column       = 0;
			int tabIndent    = Document.TextEditorProperties.TabIndent;
			float drawingPos;
			Graphics g = textArea.CreateGraphics();
			// if no folding is interresting
			if (f == null || !(f.StartLine < logicalLine || f.StartLine == logicalLine && f.StartColumn < logicalColumn)) {
				drawingPos = CountColumns(ref column, 0, logicalColumn, logicalLine, g);
				return (int)(drawingPos - textArea.VirtualTop.X);
			}
			
			// if logicalLine/logicalColumn is in folding
			if (f.EndLine > logicalLine || f.EndLine == logicalLine && f.EndColumn > logicalColumn) {
				logicalColumn = f.StartColumn;
				logicalLine = f.StartLine;
				--i;
			}
			lastFolding = i;
			
			// search backwards until a new visible line is reched
			for (; i >= 0; --i) {
				f = (FoldMarker)foldings[i];
				if (f.EndLine < logicalLine) { // reached the begin of a new visible line
					break;
				}
			}
			firstFolding = i + 1;
			
			if (lastFolding < firstFolding) {
				drawingPos = CountColumns(ref column, 0, logicalColumn, logicalLine, g);
				return (int)(drawingPos - textArea.VirtualTop.X);
			}
			
			int foldEnd      = 0;
			drawingPos = 0;
			for (i = firstFolding; i <= lastFolding; ++i) {
				f = foldings[i];
				drawingPos += CountColumns(ref column, foldEnd, f.StartColumn, f.StartLine, g);
				foldEnd = f.EndColumn;
				column += f.FoldText.Length;
				drawingPos += additionalFoldTextSize;
				drawingPos += MeasureStringWidth(g, f.FoldText, TextEditorProperties.FontContainer.RegularFont);
			}
			drawingPos += CountColumns(ref column, foldEnd, logicalColumn, logicalLine, g);
			g.Dispose();
			return (int)(drawingPos - textArea.VirtualTop.X);
		}
		#endregion
		
		#region DrawHelper functions
		void DrawBracketHighlight(Graphics g, Rectangle rect)
		{
			g.FillRectangle(BrushRegistry.GetBrush(Color.FromArgb(50, 0, 0, 255)), rect);
			g.DrawRectangle(Pens.Blue, rect);
		}
		
		void DrawString(Graphics g, string text, Font font, Color color, int x, int y)
		{
			TextRenderer.DrawText(g, text, font, new Point(x, y), color, textFormatFlags);
		}
		
		void DrawInvalidLineMarker(Graphics g, int x, int y)
		{
			HighlightColor invalidLinesColor = textArea.Document.HighlightingStrategy.GetColorFor("InvalidLines");
			DrawString(g, "~", invalidLinesColor.GetFont(TextEditorProperties.FontContainer), invalidLinesColor.Color, x, y);
		}
		
		void DrawSpaceMarker(Graphics g, Color color, int x, int y)
		{
			HighlightColor spaceMarkerColor = textArea.Document.HighlightingStrategy.GetColorFor("SpaceMarkers");
			DrawString(g, "\u00B7", spaceMarkerColor.GetFont(TextEditorProperties.FontContainer), color, x, y);
		}
		
		void DrawTabMarker(Graphics g, Color color, int x, int y)
		{
			HighlightColor tabMarkerColor   = textArea.Document.HighlightingStrategy.GetColorFor("TabMarkers");
			DrawString(g, "\u00BB", tabMarkerColor.GetFont(TextEditorProperties.FontContainer), color, x, y);
		}
		
		int DrawEOLMarker(Graphics g, Color color, Brush backBrush, int x, int y)
		{
			HighlightColor eolMarkerColor = textArea.Document.HighlightingStrategy.GetColorFor("EOLMarkers");
			
			int width = GetWidth('\u00B6', eolMarkerColor.GetFont(TextEditorProperties.FontContainer));
			g.FillRectangle(backBrush,
			                new RectangleF(x, y, width, fontHeight));
			
			DrawString(g, "\u00B6", eolMarkerColor.GetFont(TextEditorProperties.FontContainer), color, x, y);
			return width;
		}
		
		void DrawVerticalRuler(Graphics g, Rectangle lineRectangle)
		{
			int xpos = WideSpaceWidth * TextEditorProperties.VerticalRulerRow - textArea.VirtualTop.X;
			if (xpos <= 0) {
				return;
			}
			HighlightColor vRulerColor = textArea.Document.HighlightingStrategy.GetColorFor("VRuler");
			
			g.DrawLine(BrushRegistry.GetPen(vRulerColor.Color),
			           drawingPosition.Left + xpos,
			           lineRectangle.Top,
			           drawingPosition.Left + xpos,
			           lineRectangle.Bottom);
		}
		#endregion
	}
}
