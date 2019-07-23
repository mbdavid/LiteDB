// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;

namespace ICSharpCode.TextEditor.Document
{
	public class HighlightInfo
	{
		public bool BlockSpanOn = false;
		public bool Span        = false;
		public Span CurSpan     = null;
		
		public HighlightInfo(Span curSpan, bool span, bool blockSpanOn)
		{
			this.CurSpan     = curSpan;
			this.Span        = span;
			this.BlockSpanOn = blockSpanOn;
		}
	}
}
