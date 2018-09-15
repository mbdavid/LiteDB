// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;

namespace ICSharpCode.TextEditor.Document
{
	public class ColumnRange 
	{
		public static readonly ColumnRange NoColumn    = new ColumnRange(-2, -2);
		public static readonly ColumnRange WholeColumn = new ColumnRange(-1, -1);
		
		int startColumn;
		int endColumn;
		
		public int StartColumn {
			get {
				return startColumn;
			}
			set {
				startColumn = value;
			}
		}
		
		public int EndColumn {
			get {
				return endColumn;
			}
			set {
				endColumn = value;
			}
		}
		
		public ColumnRange(int startColumn, int endColumn)
		{
			this.startColumn = startColumn;
			this.endColumn = endColumn;
			
		}
		
		public override int GetHashCode()
		{
			return startColumn + (endColumn << 16);
		}
		
		public override bool Equals(object obj)
		{
			if (obj is ColumnRange) {
				return ((ColumnRange)obj).startColumn == startColumn &&
				       ((ColumnRange)obj).endColumn == endColumn;
				
			}
			return false;
		}
		
		public override string ToString()
		{
			return String.Format("[ColumnRange: StartColumn={0}, EndColumn={1}]", startColumn, endColumn);
		}
		
	}
}
