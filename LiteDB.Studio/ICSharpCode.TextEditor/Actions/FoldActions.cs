// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using ICSharpCode.TextEditor.Document;

namespace ICSharpCode.TextEditor.Actions
{
	public class ToggleFolding : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			List<FoldMarker> foldMarkers = textArea.Document.FoldingManager.GetFoldingsWithStart(textArea.Caret.Line);
			if (foldMarkers.Count != 0) {
				foreach (FoldMarker fm in foldMarkers)
					fm.IsFolded = !fm.IsFolded;
			} else {
				foldMarkers = textArea.Document.FoldingManager.GetFoldingsContainsLineNumber(textArea.Caret.Line);
				if (foldMarkers.Count != 0) {
					FoldMarker innerMost = foldMarkers[0];
					for (int i = 1; i < foldMarkers.Count; i++) {
						if (new TextLocation(foldMarkers[i].StartColumn, foldMarkers[i].StartLine) >
						    new TextLocation(innerMost.StartColumn, innerMost.StartLine))
						{
							innerMost = foldMarkers[i];
						}
					}
					innerMost.IsFolded = !innerMost.IsFolded;
				}
			}
			textArea.Document.FoldingManager.NotifyFoldingsChanged(EventArgs.Empty);
		}
	}
	
	public class ToggleAllFoldings : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			bool doFold = true;
			foreach (FoldMarker fm in  textArea.Document.FoldingManager.FoldMarker) {
				if (fm.IsFolded) {
					doFold = false;
					break;
				}
			}
			foreach (FoldMarker fm in  textArea.Document.FoldingManager.FoldMarker) {
				fm.IsFolded = doFold;
			}
			textArea.Document.FoldingManager.NotifyFoldingsChanged(EventArgs.Empty);
		}
	}
	
	public class ShowDefinitionsOnly : AbstractEditAction
	{
		public override void Execute(TextArea textArea)
		{
			foreach (FoldMarker fm in  textArea.Document.FoldingManager.FoldMarker) {
				fm.IsFolded = fm.FoldType == FoldType.MemberBody || fm.FoldType == FoldType.Region;
			}
			textArea.Document.FoldingManager.NotifyFoldingsChanged(EventArgs.Empty);
		}
	}
}
