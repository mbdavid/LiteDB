// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Util
{
	internal struct RedBlackTreeIterator<T> : IEnumerator<T>
	{
		internal RedBlackTreeNode<T> node;
		
		internal RedBlackTreeIterator(RedBlackTreeNode<T> node)
		{
			this.node = node;
		}
		
		public bool IsValid {
			get { return node != null; }
		}
		
		public T Current {
			get {
				if (node != null)
					return node.val;
				else
					throw new InvalidOperationException();
			}
		}
		
		object System.Collections.IEnumerator.Current {
			get {
				return this.Current;
			}
		}
		
		void IDisposable.Dispose()
		{
		}
		
		void System.Collections.IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}
		
		public bool MoveNext()
		{
			if (node == null)
				return false;
			if (node.right != null) {
				node = node.right.LeftMost;
			} else {
				RedBlackTreeNode<T> oldNode;
				do {
					oldNode = node;
					node = node.parent;
					// we are on the way up from the right part, don't output node again
				} while (node != null && node.right == oldNode);
			}
			return node != null;
		}
		
		public bool MoveBack()
		{
			if (node == null)
				return false;
			if (node.left != null) {
				node = node.left.RightMost;
			} else {
				RedBlackTreeNode<T> oldNode;
				do {
					oldNode = node;
					node = node.parent;
					// we are on the way up from the left part, don't output node again
				} while (node != null && node.left == oldNode);
			}
			return node != null;
		}
	}
}
