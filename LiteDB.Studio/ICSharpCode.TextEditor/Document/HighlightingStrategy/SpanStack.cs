// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Document
{
	/// <summary>
	/// A stack of Span instances. Works like Stack&lt;Span&gt;, but can be cloned quickly
	/// because it is implemented as linked list.
	/// </summary>
	public sealed class SpanStack : ICloneable, IEnumerable<Span>
	{
		internal sealed class StackNode
		{
			public readonly StackNode Previous;
			public readonly Span Data;
			
			public StackNode(StackNode previous, Span data)
			{
				this.Previous = previous;
				this.Data = data;
			}
		}
		
		StackNode top = null;
		
		public Span Pop()
		{
			Span s = top.Data;
			top = top.Previous;
			return s;
		}
		
		public Span Peek()
		{
			return top.Data;
		}
		
		public void Push(Span s)
		{
			top = new StackNode(top, s);
		}
		
		public bool IsEmpty {
			get {
				return top == null;
			}
		}
		
		public SpanStack Clone()
		{
			SpanStack n = new SpanStack();
			n.top = this.top;
			return n;
		}
		object ICloneable.Clone()
		{
			return this.Clone();
		}
		
		public Enumerator GetEnumerator()
		{
			return new Enumerator(new StackNode(top, null));
		}
		IEnumerator<Span> IEnumerable<Span>.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		
		public struct Enumerator : IEnumerator<Span>
		{
			StackNode c;
			
			internal Enumerator(StackNode node)
			{
				c = node;
			}
			
			public Span Current {
				get {
					return c.Data;
				}
			}
			
			object System.Collections.IEnumerator.Current {
				get {
					return c.Data;
				}
			}
			
			public void Dispose()
			{
				c = null;
			}
			
			public bool MoveNext()
			{
				c = c.Previous;
				return c != null;
			}
			
			public void Reset()
			{
				throw new NotSupportedException();
			}
		}
	}
}
