// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Runtime.Serialization;

namespace ICSharpCode.TextEditor.Document
{
	/// <summary>
	/// Indicates that the highlighting definition that was tried to load was invalid.
	/// You get this exception only once per highlighting definition, after that the definition
	/// is replaced with the default highlighter.
	/// </summary>
	[Serializable()]
	public class HighlightingDefinitionInvalidException : Exception
	{
		public HighlightingDefinitionInvalidException() : base()
		{
		}
		
		public HighlightingDefinitionInvalidException(string message) : base(message)
		{
		}
		
		public HighlightingDefinitionInvalidException(string message, Exception innerException) : base(message, innerException)
		{
		}
		
		protected HighlightingDefinitionInvalidException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
