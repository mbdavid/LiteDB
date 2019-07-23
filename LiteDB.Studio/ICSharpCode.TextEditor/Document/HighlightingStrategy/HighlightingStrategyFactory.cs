// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;

namespace ICSharpCode.TextEditor.Document
{
	public class HighlightingStrategyFactory
	{
		public static IHighlightingStrategy CreateHighlightingStrategy()
		{
			return (IHighlightingStrategy)HighlightingManager.Manager.HighlightingDefinitions["Default"];
		}
		
		public static IHighlightingStrategy CreateHighlightingStrategy(string name)
		{
			IHighlightingStrategy highlightingStrategy  = HighlightingManager.Manager.FindHighlighter(name);
			
			if (highlightingStrategy == null) {
				return CreateHighlightingStrategy();
			}
			return highlightingStrategy;
		}
		
		public static IHighlightingStrategy CreateHighlightingStrategyForFile(string fileName)
		{
			IHighlightingStrategy highlightingStrategy  = HighlightingManager.Manager.FindHighlighterForFile(fileName);
			if (highlightingStrategy == null) {
				return CreateHighlightingStrategy();
			}
			return highlightingStrategy;
		}
	}
}
