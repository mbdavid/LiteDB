// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Xml;

namespace ICSharpCode.TextEditor.Document
{
	/// <summary>
	/// This class is used for storing the state of a bookmark manager 
	/// </summary>
	public class BookmarkManagerMemento
	{
		List<int> bookmarks = new List<int>();
		
		/// <value>
		/// Contains all bookmarks as int values
		/// </value>
		public List<int> Bookmarks {
			get {
				return bookmarks;
			}
			set {
				bookmarks = value;
			}
		}
		
		/// <summary>
		/// Validates all bookmarks if they're in range of the document.
		/// (removing all bookmarks &lt; 0 and bookmarks &gt; max. line number
		/// </summary>
		public void CheckMemento(IDocument document)
		{
			for (int i = 0; i < bookmarks.Count; ++i) {
				int mark = (int)bookmarks[i];
				if (mark < 0 || mark >= document.TotalNumberOfLines) {
					bookmarks.RemoveAt(i);
					--i;
				}
			}
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="BookmarkManagerMemento"/>
		/// </summary>
		public BookmarkManagerMemento()
		{
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="BookmarkManagerMemento"/>
		/// </summary>
		public BookmarkManagerMemento(XmlElement element)
		{
			foreach (XmlElement el in element.ChildNodes) {
				bookmarks.Add(Int32.Parse(el.Attributes["line"].InnerText));
			}
		}
		
		/// <summary>
		/// Creates a new instance of <see cref="BookmarkManagerMemento"/>
		/// </summary>
		public BookmarkManagerMemento(List<int> bookmarks)
		{
			this.bookmarks = bookmarks;
		}
		
		/// <summary>
		/// Converts a xml element to a <see cref="BookmarkManagerMemento"/> object
		/// </summary>
		public object FromXmlElement(XmlElement element)
		{
			return new BookmarkManagerMemento(element);
		}
		
		/// <summary>
		/// Converts this <see cref="BookmarkManagerMemento"/> to a xml element
		/// </summary>
		public XmlElement ToXmlElement(XmlDocument doc)
		{
			XmlElement bookmarknode  = doc.CreateElement("Bookmarks");
			
			foreach (int line in bookmarks) {
				XmlElement markNode = doc.CreateElement("Mark");
				
				XmlAttribute lineAttr = doc.CreateAttribute("line");
				lineAttr.InnerText = line.ToString();
				markNode.Attributes.Append(lineAttr);
						
				bookmarknode.AppendChild(markNode);
			}
			
			return bookmarknode;
		}
	}
}
