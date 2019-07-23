// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <author name="Daniel Grunwald"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Threading;

namespace ICSharpCode.TextEditor.Util
{
	/// <summary>
	/// A IList{T} that checks that it is only accessed on the thread that created it, and that
	/// it is not modified while an enumerator is running.
	/// </summary>
	sealed class CheckedList<T> : IList<T>
	{
		readonly int threadID;
		readonly IList<T> baseList;
		int enumeratorCount;
		
		public CheckedList() : this(new List<T>()) {}
		
		public CheckedList(IList<T> baseList)
		{
			if (baseList == null)
				throw new ArgumentNullException("baseList");
			this.baseList = baseList;
			this.threadID = Thread.CurrentThread.ManagedThreadId;
		}
		
		void CheckRead()
		{
			if (Thread.CurrentThread.ManagedThreadId != threadID)
				throw new InvalidOperationException("CheckList cannot be accessed from this thread!");
		}
		
		void CheckWrite()
		{
			if (Thread.CurrentThread.ManagedThreadId != threadID)
				throw new InvalidOperationException("CheckList cannot be accessed from this thread!");
			if (enumeratorCount != 0)
				throw new InvalidOperationException("CheckList cannot be written to while enumerators are active!");
		}
		
		public T this[int index] {
			get {
				CheckRead();
				return baseList[index];
			}
			set {
				CheckWrite();
				baseList[index] = value;
			}
		}
		
		public int Count {
			get {
				CheckRead();
				return baseList.Count;
			}
		}
		
		public bool IsReadOnly {
			get {
				CheckRead();
				return baseList.IsReadOnly;
			}
		}
		
		public int IndexOf(T item)
		{
			CheckRead();
			return baseList.IndexOf(item);
		}
		
		public void Insert(int index, T item)
		{
			CheckWrite();
			baseList.Insert(index, item);
		}
		
		public void RemoveAt(int index)
		{
			CheckWrite();
			baseList.RemoveAt(index);
		}
		
		public void Add(T item)
		{
			CheckWrite();
			baseList.Add(item);
		}
		
		public void Clear()
		{
			CheckWrite();
			baseList.Clear();
		}
		
		public bool Contains(T item)
		{
			CheckRead();
			return baseList.Contains(item);
		}
		
		public void CopyTo(T[] array, int arrayIndex)
		{
			CheckRead();
			baseList.CopyTo(array, arrayIndex);
		}
		
		public bool Remove(T item)
		{
			CheckWrite();
			return baseList.Remove(item);
		}
		
		public IEnumerator<T> GetEnumerator()
		{
			CheckRead();
			return Enumerate();
		}
		
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			CheckRead();
			return Enumerate();
		}
		
		IEnumerator<T> Enumerate()
		{
			CheckRead();
			try {
				enumeratorCount++;
				foreach (T val in baseList) {
					yield return val;
					CheckRead();
				}
			} finally {
				enumeratorCount--;
				CheckRead();
			}
		}
	}
}
