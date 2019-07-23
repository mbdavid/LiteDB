// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

namespace ICSharpCode.TextEditor.Document
{
	/// <summary>
	/// Interface to describe a sequence of characters that can be edited. 	
	/// </summary>
	public interface ITextBufferStrategy
	{
		/// <value>
		/// The current length of the sequence of characters that can be edited.
		/// </value>
		int Length {
			get;
		}
		
		/// <summary>
		/// Inserts a string of characters into the sequence.
		/// </summary>
		/// <param name="offset">
		/// offset where to insert the string.
		/// </param>
		/// <param name="text">
		/// text to be inserted.
		/// </param>
		void Insert(int offset, string text);
		
		/// <summary>
		/// Removes some portion of the sequence.
		/// </summary>
		/// <param name="offset">
		/// offset of the remove.
		/// </param>
		/// <param name="length">
		/// number of characters to remove.
		/// </param>
		void Remove(int offset, int length);
		
		/// <summary>
		/// Replace some portion of the sequence.
		/// </summary>
		/// <param name="offset">
		/// offset.
		/// </param>
		/// <param name="length">
		/// number of characters to replace.
		/// </param>
		/// <param name="text">
		/// text to be replaced with.
		/// </param>
		void Replace(int offset, int length, string text);
		
		/// <summary>
		/// Fetches a string of characters contained in the sequence.
		/// </summary>
		/// <param name="offset">
		/// Offset into the sequence to fetch
		/// </param>
		/// <param name="length">
		/// number of characters to copy.
		/// </param>
		string GetText(int offset, int length);
		
		/// <summary>
		/// Returns a specific char of the sequence.
		/// </summary>
		/// <param name="offset">
		/// Offset of the char to get.
		/// </param>
		char GetCharAt(int offset);
		
		/// <summary>
		/// This method sets the stored content.
		/// </summary>
		/// <param name="text">
		/// The string that represents the character sequence.
		/// </param>
		void SetContent(string text);
	}
}
