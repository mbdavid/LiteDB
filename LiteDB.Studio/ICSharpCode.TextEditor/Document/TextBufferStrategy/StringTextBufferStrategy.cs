// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.IO;
using System.Text;

namespace ICSharpCode.TextEditor.Document
{
	/// <summary>
	/// Simple implementation of the ITextBuffer interface implemented using a
	/// string.
	/// Only for fall-back purposes.
	/// </summary>
	public class StringTextBufferStrategy : ITextBufferStrategy
	{
		string storedText = "";
		
		public int Length {
			get {
				return storedText.Length;
			}
		}
		
		public void Insert(int offset, string text)
		{
			if (text != null) {
				storedText = storedText.Insert(offset, text);
			}
		}
		
		public void Remove(int offset, int length)
		{
			storedText = storedText.Remove(offset, length);
		}
		
		public void Replace(int offset, int length, string text)
		{
			Remove(offset, length);
			Insert(offset, text);
		}
		
		public string GetText(int offset, int length)
		{
			if (length == 0) {
				return "";
			}
			if (offset == 0 && length >= storedText.Length) {
				return storedText;
			}
			return storedText.Substring(offset, Math.Min(length, storedText.Length - offset));
		}
		
		public char GetCharAt(int offset)
		{
			if (offset == Length) {
				return '\0';
			}
			return storedText[offset];
		}
		
		public void SetContent(string text)
		{
			storedText = text;
		}
		
		public StringTextBufferStrategy()
		{
		}
		
		public static ITextBufferStrategy CreateTextBufferFromFile(string fileName)
		{
			if (!File.Exists(fileName)) {
				throw new System.IO.FileNotFoundException(fileName);
			}
			StringTextBufferStrategy s = new StringTextBufferStrategy();
			s.SetContent(Util.FileReader.ReadFileContent(fileName, Encoding.Default));
			return s;
		}
	}
}
