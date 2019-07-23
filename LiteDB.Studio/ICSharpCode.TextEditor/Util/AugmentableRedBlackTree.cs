// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;

namespace ICSharpCode.TextEditor.Util
{
	internal sealed class RedBlackTreeNode<T>
	{
		internal RedBlackTreeNode<T> left, right, parent;
		internal T val;
		internal bool color;
		
		internal RedBlackTreeNode(T val)
		{
			this.val = val;
		}
		
		internal RedBlackTreeNode<T> LeftMost {
			get {
				RedBlackTreeNode<T> node = this;
				while (node.left != null)
					node = node.left;
				return node;
			}
		}
		
		internal RedBlackTreeNode<T> RightMost {
			get {
				RedBlackTreeNode<T> node = this;
				while (node.right != null)
					node = node.right;
				return node;
			}
		}
	}
	
	internal interface IRedBlackTreeHost<T> : IComparer<T>
	{
		bool Equals(T a, T b);
		
		void UpdateAfterChildrenChange(RedBlackTreeNode<T> node);
		void UpdateAfterRotateLeft(RedBlackTreeNode<T> node);
		void UpdateAfterRotateRight(RedBlackTreeNode<T> node);
	}
	
	/// <summary>
	/// Description of RedBlackTree.
	/// </summary>
	internal sealed class AugmentableRedBlackTree<T, Host> : ICollection<T> where Host : IRedBlackTreeHost<T>
	{
		readonly Host host;
		int count;
		internal RedBlackTreeNode<T> root;
		
		public AugmentableRedBlackTree(Host host)
		{
			if (host == null) throw new ArgumentNullException("host");
			this.host = host;
		}
		
		public int Count {
			get { return count; }
		}
		
		public void Clear()
		{
			root = null;
			count = 0;
		}
		
		#region Debugging code
		#if DEBUG
		/// <summary>
		/// Check tree for consistency and being balanced.
		/// </summary>
		[Conditional("DATACONSISTENCYTEST")]
		void CheckProperties()
		{
			int blackCount = -1;
			CheckNodeProperties(root, null, RED, 0, ref blackCount);
			
			int nodeCount = 0;
			foreach (T val in this) {
				nodeCount++;
			}
			Debug.Assert(count == nodeCount);
		}
		
		/*
		1. A node is either red or black.
		2. The root is black.
		3. All leaves are black. (The leaves are the NIL children.)
		4. Both children of every red node are black. (So every red node must have a black parent.)
		5. Every simple path from a node to a descendant leaf contains the same number of black nodes. (Not counting the leaf node.)
		 */
		void CheckNodeProperties(RedBlackTreeNode<T> node, RedBlackTreeNode<T> parentNode, bool parentColor, int blackCount, ref int expectedBlackCount)
		{
			if (node == null) return;
			
			Debug.Assert(node.parent == parentNode);
			
			if (parentColor == RED) {
				Debug.Assert(node.color == BLACK);
			}
			if (node.color == BLACK) {
				blackCount++;
			}
			if (node.left == null && node.right == null) {
				// node is a leaf node:
				if (expectedBlackCount == -1)
					expectedBlackCount = blackCount;
				else
					Debug.Assert(expectedBlackCount == blackCount);
			}
			CheckNodeProperties(node.left, node, node.color, blackCount, ref expectedBlackCount);
			CheckNodeProperties(node.right, node, node.color, blackCount, ref expectedBlackCount);
		}
		
		public string GetTreeAsString()
		{
			StringBuilder b = new StringBuilder();
			AppendTreeToString(root, b, 0);
			return b.ToString();
		}
		
		static void AppendTreeToString(RedBlackTreeNode<T> node, StringBuilder b, int indent)
		{
			if (node.color == RED)
				b.Append("RED   ");
			else
				b.Append("BLACK ");
			b.AppendLine(node.val.ToString());
			indent += 2;
			if (node.left != null) {
				b.Append(' ', indent);
				b.Append("L: ");
				AppendTreeToString(node.left, b, indent);
			}
			if (node.right != null) {
				b.Append(' ', indent);
				b.Append("R: ");
				AppendTreeToString(node.right, b, indent);
			}
		}
		#endif
		#endregion
		
		#region Add
		public void Add(T item)
		{
			AddInternal(new RedBlackTreeNode<T>(item));
			#if DEBUG
			CheckProperties();
			#endif
		}
		
		void AddInternal(RedBlackTreeNode<T> newNode)
		{
			Debug.Assert(newNode.color == BLACK);
			if (root == null) {
				count = 1;
				root = newNode;
				return;
			}
			// Insert into the tree
			RedBlackTreeNode<T> parentNode = root;
			while (true) {
				if (host.Compare(newNode.val, parentNode.val) <= 0) {
					if (parentNode.left == null) {
						InsertAsLeft(parentNode, newNode);
						return;
					}
					parentNode = parentNode.left;
				} else {
					if (parentNode.right == null) {
						InsertAsRight(parentNode, newNode);
						return;
					}
					parentNode = parentNode.right;
				}
			}
		}
		
		internal void InsertAsLeft(RedBlackTreeNode<T> parentNode, RedBlackTreeNode<T> newNode)
		{
			Debug.Assert(parentNode.left == null);
			parentNode.left = newNode;
			newNode.parent = parentNode;
			newNode.color = RED;
			host.UpdateAfterChildrenChange(parentNode);
			FixTreeOnInsert(newNode);
			count++;
		}
		
		internal void InsertAsRight(RedBlackTreeNode<T> parentNode, RedBlackTreeNode<T> newNode)
		{
			Debug.Assert(parentNode.right == null);
			parentNode.right = newNode;
			newNode.parent = parentNode;
			newNode.color = RED;
			host.UpdateAfterChildrenChange(parentNode);
			FixTreeOnInsert(newNode);
			count++;
		}
		
		void FixTreeOnInsert(RedBlackTreeNode<T> node)
		{
			Debug.Assert(node != null);
			Debug.Assert(node.color == RED);
			Debug.Assert(node.left == null || node.left.color == BLACK);
			Debug.Assert(node.right == null || node.right.color == BLACK);
			
			RedBlackTreeNode<T> parentNode = node.parent;
			if (parentNode == null) {
				// we inserted in the root -> the node must be black
				// since this is a root node, making the node black increments the number of black nodes
				// on all paths by one, so it is still the same for all paths.
				node.color = BLACK;
				return;
			}
			if (parentNode.color == BLACK) {
				// if the parent node where we inserted was black, our red node is placed correctly.
				// since we inserted a red node, the number of black nodes on each path is unchanged
				// -> the tree is still balanced
				return;
			}
			// parentNode is red, so there is a conflict here!
			
			// because the root is black, parentNode is not the root -> there is a grandparent node
			RedBlackTreeNode<T> grandparentNode = parentNode.parent;
			RedBlackTreeNode<T> uncleNode = Sibling(parentNode);
			if (uncleNode != null && uncleNode.color == RED) {
				parentNode.color = BLACK;
				uncleNode.color = BLACK;
				grandparentNode.color = RED;
				FixTreeOnInsert(grandparentNode);
				return;
			}
			// now we know: parent is red but uncle is black
			// First rotation:
			if (node == parentNode.right && parentNode == grandparentNode.left) {
				RotateLeft(parentNode);
				node = node.left;
			} else if (node == parentNode.left && parentNode == grandparentNode.right) {
				RotateRight(parentNode);
				node = node.right;
			}
			// because node might have changed, reassign variables:
			parentNode = node.parent;
			grandparentNode = parentNode.parent;
			
			// Now recolor a bit:
			parentNode.color = BLACK;
			grandparentNode.color = RED;
			// Second rotation:
			if (node == parentNode.left && parentNode == grandparentNode.left) {
				RotateRight(grandparentNode);
			} else {
				// because of the first rotation, this is guaranteed:
				Debug.Assert(node == parentNode.right && parentNode == grandparentNode.right);
				RotateLeft(grandparentNode);
			}
		}
		
		void ReplaceNode(RedBlackTreeNode<T> replacedNode, RedBlackTreeNode<T> newNode)
		{
			if (replacedNode.parent == null) {
				Debug.Assert(replacedNode == root);
				root = newNode;
			} else {
				if (replacedNode.parent.left == replacedNode)
					replacedNode.parent.left = newNode;
				else
					replacedNode.parent.right = newNode;
			}
			if (newNode != null) {
				newNode.parent = replacedNode.parent;
			}
			replacedNode.parent = null;
		}
		
		void RotateLeft(RedBlackTreeNode<T> p)
		{
			// let q be p's right child
			RedBlackTreeNode<T> q = p.right;
			Debug.Assert(q != null);
			Debug.Assert(q.parent == p);
			// set q to be the new root
			ReplaceNode(p, q);
			
			// set p's right child to be q's left child
			p.right = q.left;
			if (p.right != null) p.right.parent = p;
			// set q's left child to be p
			q.left = p;
			p.parent = q;
			host.UpdateAfterRotateLeft(p);
		}
		
		void RotateRight(RedBlackTreeNode<T> p)
		{
			// let q be p's left child
			RedBlackTreeNode<T> q = p.left;
			Debug.Assert(q != null);
			Debug.Assert(q.parent == p);
			// set q to be the new root
			ReplaceNode(p, q);
			
			// set p's left child to be q's right child
			p.left = q.right;
			if (p.left != null) p.left.parent = p;
			// set q's right child to be p
			q.right = p;
			p.parent = q;
			host.UpdateAfterRotateRight(p);
		}
		
		RedBlackTreeNode<T> Sibling(RedBlackTreeNode<T> node)
		{
			if (node == node.parent.left)
				return node.parent.right;
			else
				return node.parent.left;
		}
		#endregion
		
		#region Remove
		public void RemoveAt(RedBlackTreeIterator<T> iterator)
		{
			RedBlackTreeNode<T> node = iterator.node;
			if (node == null)
				throw new ArgumentException("Invalid iterator");
			while (node.parent != null)
				node = node.parent;
			if (node != root)
				throw new ArgumentException("Iterator does not belong to this tree");
			RemoveNode(iterator.node);
			#if DEBUG
			CheckProperties();
			#endif
		}
		
		internal void RemoveNode(RedBlackTreeNode<T> removedNode)
		{
			if (removedNode.left != null && removedNode.right != null) {
				// replace removedNode with it's in-order successor
				
				RedBlackTreeNode<T> leftMost = removedNode.right.LeftMost;
				RemoveNode(leftMost); // remove leftMost from its current location
				
				// and overwrite the removedNode with it
				ReplaceNode(removedNode, leftMost);
				leftMost.left = removedNode.left;
				if (leftMost.left != null) leftMost.left.parent = leftMost;
				leftMost.right = removedNode.right;
				if (leftMost.right != null) leftMost.right.parent = leftMost;
				leftMost.color = removedNode.color;
				
				host.UpdateAfterChildrenChange(leftMost);
				if (leftMost.parent != null) host.UpdateAfterChildrenChange(leftMost.parent);
				return;
			}
			
			count--;
			
			// now either removedNode.left or removedNode.right is null
			// get the remaining child
			RedBlackTreeNode<T> parentNode = removedNode.parent;
			RedBlackTreeNode<T> childNode = removedNode.left ?? removedNode.right;
			ReplaceNode(removedNode, childNode);
			if (parentNode != null) host.UpdateAfterChildrenChange(parentNode);
			if (removedNode.color == BLACK) {
				if (childNode != null && childNode.color == RED) {
					childNode.color = BLACK;
				} else {
					FixTreeOnDelete(childNode, parentNode);
				}
			}
		}
		
		static RedBlackTreeNode<T> Sibling(RedBlackTreeNode<T> node, RedBlackTreeNode<T> parentNode)
		{
			Debug.Assert(node == null || node.parent == parentNode);
			if (node == parentNode.left)
				return parentNode.right;
			else
				return parentNode.left;
		}
		
		const bool RED = true;
		const bool BLACK = false;
		
		static bool GetColor(RedBlackTreeNode<T> node)
		{
			return node != null ? node.color : BLACK;
		}
		
		void FixTreeOnDelete(RedBlackTreeNode<T> node, RedBlackTreeNode<T> parentNode)
		{
			Debug.Assert(node == null || node.parent == parentNode);
			if (parentNode == null)
				return;
			
			// warning: node may be null
			RedBlackTreeNode<T> sibling = Sibling(node, parentNode);
			if (sibling.color == RED) {
				parentNode.color = RED;
				sibling.color = BLACK;
				if (node == parentNode.left) {
					RotateLeft(parentNode);
				} else {
					RotateRight(parentNode);
				}
				
				sibling = Sibling(node, parentNode); // update value of sibling after rotation
			}
			
			if (parentNode.color == BLACK
			    && sibling.color == BLACK
			    && GetColor(sibling.left) == BLACK
			    && GetColor(sibling.right) == BLACK)
			{
				sibling.color = RED;
				FixTreeOnDelete(parentNode, parentNode.parent);
				return;
			}
			
			if (parentNode.color == RED
			    && sibling.color == BLACK
			    && GetColor(sibling.left) == BLACK
			    && GetColor(sibling.right) == BLACK)
			{
				sibling.color = RED;
				parentNode.color = BLACK;
				return;
			}
			
			if (node == parentNode.left &&
			    sibling.color == BLACK &&
			    GetColor(sibling.left) == RED &&
			    GetColor(sibling.right) == BLACK)
			{
				sibling.color = RED;
				sibling.left.color = BLACK;
				RotateRight(sibling);
			}
			else if (node == parentNode.right &&
			         sibling.color == BLACK &&
			         GetColor(sibling.right) == RED &&
			         GetColor(sibling.left) == BLACK)
			{
				sibling.color = RED;
				sibling.right.color = BLACK;
				RotateLeft(sibling);
			}
			sibling = Sibling(node, parentNode); // update value of sibling after rotation
			
			sibling.color = parentNode.color;
			parentNode.color = BLACK;
			if (node == parentNode.left) {
				if (sibling.right != null) {
					Debug.Assert(sibling.right.color == RED);
					sibling.right.color = BLACK;
				}
				RotateLeft(parentNode);
			} else {
				if (sibling.left != null) {
					Debug.Assert(sibling.left.color == RED);
					sibling.left.color = BLACK;
				}
				RotateRight(parentNode);
			}
		}
		#endregion
		
		#region Find/LowerBound/UpperBound/GetEnumerator
		/// <summary>
		/// Returns the iterator pointing to the specified item, or an iterator in End state if the item is not found.
		/// </summary>
		public RedBlackTreeIterator<T> Find(T item)
		{
			RedBlackTreeIterator<T> it = LowerBound(item);
			while (it.IsValid && host.Compare(it.Current, item) == 0) {
				if (host.Equals(it.Current, item))
					return it;
				it.MoveNext();
			}
			return default(RedBlackTreeIterator<T>);
		}
		
		/// <summary>
		/// Returns the iterator pointing to the first item greater or equal to <paramref name="item"/>.
		/// </summary>
		public RedBlackTreeIterator<T> LowerBound(T item)
		{
			RedBlackTreeNode<T> node = root;
			RedBlackTreeNode<T> resultNode = null;
			while (node != null) {
				if (host.Compare(node.val, item) < 0) {
					node = node.right;
				} else {
					resultNode = node;
					node = node.left;
				}
			}
			return new RedBlackTreeIterator<T>(resultNode);
		}
		
		/// <summary>
		/// Returns the iterator pointing to the first item greater than <paramref name="item"/>.
		/// </summary>
		public RedBlackTreeIterator<T> UpperBound(T item)
		{
			RedBlackTreeIterator<T> it = LowerBound(item);
			while (it.IsValid && host.Compare(it.Current, item) == 0) {
				it.MoveNext();
			}
			return it;
		}
		
		/// <summary>
		/// Gets a tree iterator that starts on the first node.
		/// </summary>
		public RedBlackTreeIterator<T> Begin()
		{
			if (root == null) return default(RedBlackTreeIterator<T>);
			return new RedBlackTreeIterator<T>(root.LeftMost);
		}
		
		/// <summary>
		/// Gets a tree iterator that starts one node before the first node.
		/// </summary>
		public RedBlackTreeIterator<T> GetEnumerator()
		{
			if (root == null) return default(RedBlackTreeIterator<T>);
			RedBlackTreeNode<T> dummyNode = new RedBlackTreeNode<T>(default(T));
			dummyNode.right = root;
			return new RedBlackTreeIterator<T>(dummyNode);
		}
		#endregion
		
		#region ICollection members
		public bool Contains(T item)
		{
			return Find(item).IsValid;
		}
		
		public bool Remove(T item)
		{
			RedBlackTreeIterator<T> it = Find(item);
			if (!it.IsValid) {
				return false;
			} else {
				RemoveAt(it);
				return true;
			}
		}
		
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		
		bool ICollection<T>.IsReadOnly {
			get { return false; }
		}
		
		public void CopyTo(T[] array, int arrayIndex)
		{
			if (array == null) throw new ArgumentNullException("array");
			foreach (T val in this) {
				array[arrayIndex++] = val;
			}
		}
		#endregion
	}
}
