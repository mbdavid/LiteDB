// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Windows.Forms;

namespace ICSharpCode.TextEditor.Gui.CompletionWindow
{
	public interface ICompletionDataProvider
	{
		ImageList ImageList {
			get;
		}
		string PreSelection {
			get;
		}
		/// <summary>
		/// Gets the index of the element in the list that is chosen by default.
		/// </summary>
		int DefaultIndex {
			get;
		}
		
		/// <summary>
		/// Processes a keypress. Returns the action to be run with the key.
		/// </summary>
		CompletionDataProviderKeyResult ProcessKey(char key);
		
		/// <summary>
		/// Executes the insertion. The provider should set the caret position and then
		/// call data.InsertAction.
		/// </summary>
		bool InsertAction(ICompletionData data, TextArea textArea, int insertionOffset, char key);
		
		/// <summary>
		/// Generates the completion data. This method is called by the text editor control.
		/// </summary>
		ICompletionData[] GenerateCompletionData(string fileName, TextArea textArea, char charTyped);
	}
	
	public enum CompletionDataProviderKeyResult
	{
		/// <summary>
		/// Normal key, used to choose an entry from the completion list
		/// </summary>
		NormalKey,
		/// <summary>
		/// This key triggers insertion of the completed expression
		/// </summary>
		InsertionKey,
		/// <summary>
		/// Increment both start and end offset of completion region when inserting this
		/// key. Can be used to insert whitespace (or other characters) in front of the expression
		/// while the completion window is open.
		/// </summary>
		BeforeStartKey
	}
}
