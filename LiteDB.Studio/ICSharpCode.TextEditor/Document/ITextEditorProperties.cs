// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Drawing;
using System.Text;

namespace ICSharpCode.TextEditor.Document
{
	public interface ITextEditorProperties
	{
		bool CaretLine
		{
			get;
			set;
		}

		bool AutoInsertCurlyBracket { // is wrapped in text editor control
			get;
			set;
		}
		
		bool HideMouseCursor { // is wrapped in text editor control
			get;
			set;
		}
		
		bool IsIconBarVisible { // is wrapped in text editor control
			get;
			set;
		}
		
		bool AllowCaretBeyondEOL {
			get;
			set;
		}
		
		bool ShowMatchingBracket { // is wrapped in text editor control
			get;
			set;
		}
		
		bool CutCopyWholeLine {
			get;
			set;
		}

		System.Drawing.Text.TextRenderingHint TextRenderingHint { // is wrapped in text editor control
			get;
			set;
		}
		
		bool MouseWheelScrollDown {
			get;
			set;
		}
		
		bool MouseWheelTextZoom {
			get;
			set;
		}
		
		string LineTerminator {
			get;
			set;
		}
		
		LineViewerStyle LineViewerStyle { // is wrapped in text editor control
			get;
			set;
		}
		
		bool ShowInvalidLines { // is wrapped in text editor control
			get;
			set;
		}
		
		int VerticalRulerRow { // is wrapped in text editor control
			get;
			set;
		}
		
		bool ShowSpaces { // is wrapped in text editor control
			get;
			set;
		}
		
		bool ShowTabs { // is wrapped in text editor control
			get;
			set;
		}
		
		bool ShowEOLMarker { // is wrapped in text editor control
			get;
			set;
		}
		
		bool ConvertTabsToSpaces { // is wrapped in text editor control
			get;
			set;
		}
		
		bool ShowHorizontalRuler { // is wrapped in text editor control
			get;
			set;
		}
		
		bool ShowVerticalRuler { // is wrapped in text editor control
			get;
			set;
		}
		
		Encoding Encoding {
			get;
			set;
		}
		
		bool EnableFolding { // is wrapped in text editor control
			get;
			set;
		}
		
		bool ShowLineNumbers { // is wrapped in text editor control
			get;
			set;
		}
		
		/// <summary>
		/// The width of a tab.
		/// </summary>
		int TabIndent { // is wrapped in text editor control
			get;
			set;
		}
		
		/// <summary>
		/// The amount of spaces a tab is converted to if ConvertTabsToSpaces is true.
		/// </summary>
		int IndentationSize {
			get;
			set;
		}
		
		IndentStyle IndentStyle { // is wrapped in text editor control
			get;
			set;
		}
		
		DocumentSelectionMode DocumentSelectionMode {
			get;
			set;
		}
		
		Font Font { // is wrapped in text editor control
			get;
			set;
		}
		
		FontContainer FontContainer {
			get;
		}
		
		BracketMatchingStyle  BracketMatchingStyle { // is wrapped in text editor control
			get;
			set;
		}
		
		bool SupportReadOnlySegments {
			get;
			set;
		}
	}
}
