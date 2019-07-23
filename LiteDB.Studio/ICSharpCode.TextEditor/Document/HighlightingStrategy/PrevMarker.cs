// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Xml;

namespace ICSharpCode.TextEditor.Document
{
	/// <summary>
	/// Used for mark previous token
	/// </summary>
	public class PrevMarker
	{
		string      what;
		HighlightColor color;
		bool        markMarker = false;
		
		/// <value>
		/// String value to indicate to mark previous token
		/// </value>
		public string What {
			get {
				return what;
			}
		}
		
		/// <value>
		/// Color for marking previous token
		/// </value>
		public HighlightColor Color {
			get {
				return color;
			}
		}
		
		/// <value>
		/// If true the indication text will be marked with the same color
		/// too
		/// </value>
		public bool MarkMarker {
			get {
				return markMarker;
			}
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="PrevMarker"/>
		/// </summary>
		public PrevMarker(XmlElement mark)
		{
			color = new HighlightColor(mark);
			what  = mark.InnerText;
			if (mark.Attributes["markmarker"] != null) {
				markMarker = Boolean.Parse(mark.Attributes["markmarker"].InnerText);
			}
		}
	}

}
