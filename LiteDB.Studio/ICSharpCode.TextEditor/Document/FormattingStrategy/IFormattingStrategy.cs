// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;

namespace ICSharpCode.TextEditor.Document
{
	/// <summary>
	/// This interface handles the auto and smart indenting and formating
	/// in the document while  you type. Language bindings could overwrite this 
	/// interface and define their own indentation/formating.
	/// </summary>
	public interface IFormattingStrategy
	{
		/// <summary>
		/// This function formats a specific line after <code>ch</code> is pressed.
		/// </summary>
		void FormatLine(TextArea textArea, int line, int caretOffset, char charTyped);
		
		/// <summary>
		/// This function sets the indentation level in a specific line
		/// </summary>
		/// <returns>
		/// The target caret position (length of new indentation).
		/// </returns>
		int IndentLine(TextArea textArea, int line);
		
		/// <summary>
		/// This function sets the indentlevel in a range of lines.
		/// </summary>
		void IndentLines(TextArea textArea, int begin, int end);
		
		/// <summary>
		/// Finds the offset of the opening bracket in the block defined by offset skipping
		/// brackets in strings and comments.
		/// </summary>
		/// <param name="document">The document to search in.</param>
		/// <param name="offset">The offset of an position in the block or the offset of the closing bracket.</param>
		/// <param name="openBracket">The character for the opening bracket.</param>
		/// <param name="closingBracket">The character for the closing bracket.</param>
		/// <returns>Returns the offset of the opening bracket or -1 if no matching bracket was found.</returns>
		int SearchBracketBackward(IDocument document, int offset, char openBracket, char closingBracket);
		
		/// <summary>
		/// Finds the offset of the closing bracket in the block defined by offset skipping
		/// brackets in strings and comments.
		/// </summary>
		/// <param name="document">The document to search in.</param>
		/// <param name="offset">The offset of an position in the block or the offset of the opening bracket.</param>
		/// <param name="openBracket">The character for the opening bracket.</param>
		/// <param name="closingBracket">The character for the closing bracket.</param>
		/// <returns>Returns the offset of the closing bracket or -1 if no matching bracket was found.</returns>
		int SearchBracketForward(IDocument document, int offset, char openBracket, char closingBracket);
	}
}
