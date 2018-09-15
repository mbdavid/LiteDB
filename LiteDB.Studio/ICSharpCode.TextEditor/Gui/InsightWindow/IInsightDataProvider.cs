// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;

namespace ICSharpCode.TextEditor.Gui.InsightWindow
{
	public interface IInsightDataProvider
	{
		/// <summary>
		/// Tells the insight provider to prepare its data.
		/// </summary>
		/// <param name="fileName">The name of the edited file</param>
		/// <param name="textArea">The text area in which the file is being edited</param>
		void SetupDataProvider(string fileName, TextArea textArea);
		
		/// <summary>
		/// Notifies the insight provider that the caret offset has changed.
		/// </summary>
		/// <returns>Return true to close the insight window (e.g. when the
		/// caret was moved outside the region where insight is displayed for).
		/// Return false to keep the window open.</returns>
		bool CaretOffsetChanged();
		
		/// <summary>
		/// Gets the text to display in the insight window.
		/// </summary>
		/// <param name="number">The number of the active insight entry.
		/// Multiple insight entries might be multiple overloads of the same method.</param>
		/// <returns>The text to display, e.g. a multi-line string where
		/// the first line is the method definition, followed by a description.</returns>
		string GetInsightData(int number);
		
		/// <summary>
		/// Gets the number of available insight entries, e.g. the number of available
		/// overloads to call.
		/// </summary>
		int InsightDataCount {
			get;
		}
		
		/// <summary>
		/// Gets the index of the entry to initially select.
		/// </summary>
		int DefaultIndex {
			get;
		}
	}
}
