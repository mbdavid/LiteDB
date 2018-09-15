// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Windows.Forms;

namespace ICSharpCode.TextEditor.Actions
{
	/// <summary>
	/// To define a new key for the textarea, you must write a class which
	/// implements this interface.
	/// </summary>
	public interface IEditAction
	{
		/// <value>
		/// An array of keys on which this edit action occurs.
		/// </value>
		Keys[] Keys {
			get;
			set;
		}
		
		/// <remarks>
		/// When the key which is defined per XML is pressed, this method will be launched.
		/// </remarks>
		void Execute(TextArea textArea);
	}
	
	/// <summary>
	/// To define a new key for the textarea, you must write a class which
	/// implements this interface.
	/// </summary>
	public abstract class AbstractEditAction : IEditAction
	{
		Keys[] keys = null;
		
		/// <value>
		/// An array of keys on which this edit action occurs.
		/// </value>
		public Keys[] Keys {
			get {
				return keys;
			}
			set {
				keys = value;
			}
		}
		
		/// <remarks>
		/// When the key which is defined per XML is pressed, this method will be launched.
		/// </remarks>
		public abstract void Execute(TextArea textArea);
	}		
}
