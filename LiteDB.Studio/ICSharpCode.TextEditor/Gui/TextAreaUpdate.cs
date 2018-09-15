// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Drawing;

namespace ICSharpCode.TextEditor
{
	/// <summary>
	/// This enum describes all implemented request types
	/// </summary>
	public enum TextAreaUpdateType {
		WholeTextArea,
		SingleLine,
		SinglePosition,
		PositionToLineEnd,
		PositionToEnd,
		LinesBetween
	}
	
	/// <summary>
	/// This class is used to request an update of the textarea
	/// </summary>
	public class TextAreaUpdate
	{
		TextLocation position;
		TextAreaUpdateType type;
		
		public TextAreaUpdateType TextAreaUpdateType {
			get {
				return type;
			}
		}
		
		public TextLocation Position {
			get {
				return position;
			}
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="TextAreaUpdate"/>
		/// </summary>
		public TextAreaUpdate(TextAreaUpdateType type)
		{
			this.type = type;
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="TextAreaUpdate"/>
		/// </summary>
		public TextAreaUpdate(TextAreaUpdateType type, TextLocation position)
		{
			this.type     = type;
			this.position = position;
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="TextAreaUpdate"/>
		/// </summary>
		public TextAreaUpdate(TextAreaUpdateType type, int startLine, int endLine)
		{
			this.type     = type;
			this.position = new TextLocation(startLine, endLine);
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="TextAreaUpdate"/>
		/// </summary>
		public TextAreaUpdate(TextAreaUpdateType type, int singleLine)
		{
			this.type     = type;
			this.position = new TextLocation(0, singleLine);
		}
		
		public override string ToString()
		{
			return String.Format("[TextAreaUpdate: Type={0}, Position={1}]", type, position);
		}
	}
}
