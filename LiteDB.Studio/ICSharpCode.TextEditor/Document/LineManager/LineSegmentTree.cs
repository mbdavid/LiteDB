// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ICSharpCode.TextEditor.Util;

namespace ICSharpCode.TextEditor.Document
{
	/// <summary>
	/// Data structure for efficient management of the line segments (most operations are O(lg n)).
	/// This implements an augmented red-black tree where each node has fields for the number of
	/// nodes in its subtree (like an order statistics tree) for access by index(=line number).
	/// Additionally, each node knows the total length of all segments in its subtree.
	/// This means we can find nodes by offset in O(lg n) time. Since the offset itself is not stored in
	/// the line segment but computed from the lengths stored in the tree, we adjusting the offsets when
	/// text is inserted in one line means we just have to increment the totalLength of the affected line and
	/// its parent nodes - an O(lg n) operation.
	/// However this means getting the line number or offset from a LineSegment is not a constant time
	/// operation, but takes O(lg n).
	/// 
	/// NOTE: The tree is never empty, Clear() causes it to contain an empty segment.
	/// </summary>
	sealed class LineSegmentTree : IList<LineSegment>
	{
		internal struct RBNode
		{
			internal LineSegment lineSegment;
			internal int count;
			internal int totalLength;
			
			public RBNode(LineSegment lineSegment)
			{
				this.lineSegment = lineSegment;
				this.count = 1;
				this.totalLength = lineSegment.TotalLength;
			}
			
			public override string ToString()
			{
				return "[RBNode count=" + count + " totalLength="+totalLength
					+ " lineSegment.LineNumber=" + lineSegment.LineNumber
					+ " lineSegment.Offset=" + lineSegment.Offset
					+ " lineSegment.TotalLength=" + lineSegment.TotalLength
					+ " lineSegment.DelimiterLength=" + lineSegment.DelimiterLength + "]";
			}
		}
		
		struct MyHost : IRedBlackTreeHost<RBNode>
		{
			public int Compare(RBNode x, RBNode y)
			{
				throw new NotImplementedException();
			}
			
			public bool Equals(RBNode a, RBNode b)
			{
				throw new NotImplementedException();
			}
			
			public void UpdateAfterChildrenChange(RedBlackTreeNode<RBNode> node)
			{
				int count = 1;
				int totalLength = node.val.lineSegment.TotalLength;
				if (node.left != null) {
					count += node.left.val.count;
					totalLength += node.left.val.totalLength;
				}
				if (node.right != null) {
					count += node.right.val.count;
					totalLength += node.right.val.totalLength;
				}
				if (count != node.val.count || totalLength != node.val.totalLength) {
					node.val.count = count;
					node.val.totalLength = totalLength;
					if (node.parent != null) UpdateAfterChildrenChange(node.parent);
				}
			}
			
			public void UpdateAfterRotateLeft(RedBlackTreeNode<RBNode> node)
			{
				UpdateAfterChildrenChange(node);
				UpdateAfterChildrenChange(node.parent);
			}
			
			public void UpdateAfterRotateRight(RedBlackTreeNode<RBNode> node)
			{
				UpdateAfterChildrenChange(node);
				UpdateAfterChildrenChange(node.parent);
			}
		}
		
		readonly AugmentableRedBlackTree<RBNode, MyHost> tree = new AugmentableRedBlackTree<RBNode, MyHost>(new MyHost());
		
		RedBlackTreeNode<RBNode> GetNode(int index)
		{
			if (index < 0 || index >= tree.Count)
				throw new ArgumentOutOfRangeException("index", index, "index should be between 0 and " + (tree.Count-1));
			RedBlackTreeNode<RBNode> node = tree.root;
			while (true) {
				if (node.left != null && index < node.left.val.count) {
					node = node.left;
				} else {
					if (node.left != null) {
						index -= node.left.val.count;
					}
					if (index == 0)
						return node;
					index--;
					node = node.right;
				}
			}
		}
		
		static int GetIndexFromNode(RedBlackTreeNode<RBNode> node)
		{
			int index = (node.left != null) ? node.left.val.count : 0;
			while (node.parent != null) {
				if (node == node.parent.right) {
					if (node.parent.left != null)
						index += node.parent.left.val.count;
					index++;
				}
				node = node.parent;
			}
			return index;
		}
		
		RedBlackTreeNode<RBNode> GetNodeByOffset(int offset)
		{
			if (offset < 0 || offset > this.TotalLength)
				throw new ArgumentOutOfRangeException("offset", offset, "offset should be between 0 and " + this.TotalLength);
			if (offset == this.TotalLength) {
				if (tree.root == null)
					throw new InvalidOperationException("Cannot call GetNodeByOffset while tree is empty.");
				return tree.root.RightMost;
			}
			RedBlackTreeNode<RBNode> node = tree.root;
			while (true) {
				if (node.left != null && offset < node.left.val.totalLength) {
					node = node.left;
				} else {
					if (node.left != null) {
						offset -= node.left.val.totalLength;
					}
					offset -= node.val.lineSegment.TotalLength;
					if (offset < 0)
						return node;
					node = node.right;
				}
			}
		}
		
		static int GetOffsetFromNode(RedBlackTreeNode<RBNode> node)
		{
			int offset = (node.left != null) ? node.left.val.totalLength : 0;
			while (node.parent != null) {
				if (node == node.parent.right) {
					if (node.parent.left != null)
						offset += node.parent.left.val.totalLength;
					offset += node.parent.val.lineSegment.TotalLength;
				}
				node = node.parent;
			}
			return offset;
		}
		
		public LineSegment GetByOffset(int offset)
		{
			return GetNodeByOffset(offset).val.lineSegment;
		}
		
		/// <summary>
		/// Gets the total length of all line segments. Runs in O(1).
		/// </summary>
		public int TotalLength {
			get {
				if (tree.root == null)
					return 0;
				else
					return tree.root.val.totalLength;
			}
		}
		
		/// <summary>
		/// Updates the length of a line segment. Runs in O(lg n).
		/// </summary>
		public void SetSegmentLength(LineSegment segment, int newTotalLength)
		{
			if (segment == null)
				throw new ArgumentNullException("segment");
			RedBlackTreeNode<RBNode> node = segment.treeEntry.it.node;
			segment.TotalLength = newTotalLength;
			default(MyHost).UpdateAfterChildrenChange(node);
			#if DEBUG
			CheckProperties();
			#endif
		}
		
		public void RemoveSegment(LineSegment segment)
		{
			tree.RemoveAt(segment.treeEntry.it);
			#if DEBUG
			CheckProperties();
			#endif
		}
		
		public LineSegment InsertSegmentAfter(LineSegment segment, int length)
		{
			LineSegment newSegment = new LineSegment();
			newSegment.TotalLength = length;
			newSegment.DelimiterLength = segment.DelimiterLength;
			
			newSegment.treeEntry = InsertAfter(segment.treeEntry.it.node, newSegment);
			return newSegment;
		}
		
		Enumerator InsertAfter(RedBlackTreeNode<RBNode> node, LineSegment newSegment)
		{
			RedBlackTreeNode<RBNode> newNode = new RedBlackTreeNode<RBNode>(new RBNode(newSegment));
			if (node.right == null) {
				tree.InsertAsRight(node, newNode);
			} else {
				tree.InsertAsLeft(node.right.LeftMost, newNode);
			}
			#if DEBUG
			CheckProperties();
			#endif
			return new Enumerator(new RedBlackTreeIterator<RBNode>(newNode));
		}
		
		/// <summary>
		/// Gets the number of items in the collections. Runs in O(1).
		/// </summary>
		public int Count {
			get { return tree.Count; }
		}
		
		/// <summary>
		/// Gets or sets an item by index. Runs in O(lg n).
		/// </summary>
		public LineSegment this[int index] {
			get {
				return GetNode(index).val.lineSegment;
			}
			set {
				throw new NotSupportedException();
			}
		}
		
		bool ICollection<LineSegment>.IsReadOnly {
			get { return true; }
		}
		
		/// <summary>
		/// Gets the index of an item. Runs in O(lg n).
		/// </summary>
		public int IndexOf(LineSegment item)
		{
			int index = item.LineNumber;
			if (index < 0 || index >= this.Count)
				return -1;
			if (item != this[index])
				return -1;
			return index;
		}
		
		void IList<LineSegment>.RemoveAt(int index)
		{
			throw new NotSupportedException();
		}
		
		#if DEBUG
		[Conditional("DATACONSISTENCYTEST")]
		void CheckProperties()
		{
			if (tree.root == null) {
				Debug.Assert(this.Count == 0);
			} else {
				Debug.Assert(tree.root.val.count == this.Count);
				CheckProperties(tree.root);
			}
		}
		
		void CheckProperties(RedBlackTreeNode<RBNode> node)
		{
			int count = 1;
			int totalLength = node.val.lineSegment.TotalLength;
			if (node.left != null) {
				CheckProperties(node.left);
				count += node.left.val.count;
				totalLength += node.left.val.totalLength;
			}
			if (node.right != null) {
				CheckProperties(node.right);
				count += node.right.val.count;
				totalLength += node.right.val.totalLength;
			}
			Debug.Assert(node.val.count == count);
			Debug.Assert(node.val.totalLength == totalLength);
		}
		
		public string GetTreeAsString()
		{
			return tree.GetTreeAsString();
		}
		#endif
		
		public LineSegmentTree()
		{
			Clear();
		}
		
		/// <summary>
		/// Clears the list. Runs in O(1).
		/// </summary>
		public void Clear()
		{
			tree.Clear();
			LineSegment emptySegment = new LineSegment();
			emptySegment.TotalLength = 0;
			emptySegment.DelimiterLength = 0;
			tree.Add(new RBNode(emptySegment));
			emptySegment.treeEntry = GetEnumeratorForIndex(0);
			#if DEBUG
			CheckProperties();
			#endif
		}
		
		/// <summary>
		/// Tests whether an item is in the list. Runs in O(n).
		/// </summary>
		public bool Contains(LineSegment item)
		{
			return IndexOf(item) >= 0;
		}
		
		/// <summary>
		/// Copies all elements from the list to the array.
		/// </summary>
		public void CopyTo(LineSegment[] array, int arrayIndex)
		{
			if (array == null) throw new ArgumentNullException("array");
			foreach (LineSegment val in this)
				array[arrayIndex++] = val;
		}
		
		IEnumerator<LineSegment> IEnumerable<LineSegment>.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		
		public Enumerator GetEnumerator()
		{
			return new Enumerator(tree.GetEnumerator());
		}
		
		public Enumerator GetEnumeratorForIndex(int index)
		{
			return new Enumerator(new RedBlackTreeIterator<RBNode>(GetNode(index)));
		}
		
		public Enumerator GetEnumeratorForOffset(int offset)
		{
			return new Enumerator(new RedBlackTreeIterator<RBNode>(GetNodeByOffset(offset)));
		}
		
		public struct Enumerator : IEnumerator<LineSegment>
		{
			/// <summary>
			/// An invalid enumerator value. Calling MoveNext on the invalid enumerator
			/// will always return false, accessing Current will throw an exception.
			/// </summary>
			public static readonly Enumerator Invalid = default(Enumerator);
			
			internal RedBlackTreeIterator<RBNode> it;
			
			internal Enumerator(RedBlackTreeIterator<RBNode> it)
			{
				this.it = it;
			}
			
			/// <summary>
			/// Gets the current value. Runs in O(1).
			/// </summary>
			public LineSegment Current {
				get {
					return it.Current.lineSegment;
				}
			}
			
			public bool IsValid {
				get {
					return it.IsValid;
				}
			}
			
			/// <summary>
			/// Gets the index of the current value. Runs in O(lg n).
			/// </summary>
			public int CurrentIndex {
				get {
					if (it.node == null)
						throw new InvalidOperationException();
					return GetIndexFromNode(it.node);
				}
			}
			
			/// <summary>
			/// Gets the offset of the current value. Runs in O(lg n).
			/// </summary>
			public int CurrentOffset {
				get {
					if (it.node == null)
						throw new InvalidOperationException();
					return GetOffsetFromNode(it.node);
				}
			}
			
			object System.Collections.IEnumerator.Current {
				get {
					return it.Current.lineSegment;
				}
			}
			
			public void Dispose()
			{
			}
			
			/// <summary>
			/// Moves to the next index. Runs in O(lg n), but for k calls, the combined time is only O(k+lg n).
			/// </summary>
			public bool MoveNext()
			{
				return it.MoveNext();
			}
			
			/// <summary>
			/// Moves to the previous index. Runs in O(lg n), but for k calls, the combined time is only O(k+lg n).
			/// </summary>
			public bool MoveBack()
			{
				return it.MoveBack();
			}
			
			void System.Collections.IEnumerator.Reset()
			{
				throw new NotSupportedException();
			}
		}
		
		void IList<LineSegment>.Insert(int index, LineSegment item)
		{
			throw new NotSupportedException();
		}
		
		void ICollection<LineSegment>.Add(LineSegment item)
		{
			throw new NotSupportedException();
		}
		
		bool ICollection<LineSegment>.Remove(LineSegment item)
		{
			throw new NotSupportedException();
		}
	}
}
