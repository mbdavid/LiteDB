// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Drawing;

namespace ICSharpCode.TextEditor.Undo
{
	/// <summary>
	/// This class implements an undo stack
	/// </summary>
	public class UndoStack
	{
		Stack<IUndoableOperation> undostack = new Stack<IUndoableOperation>();
		Stack<IUndoableOperation> redostack = new Stack<IUndoableOperation>();
		
		public TextEditorControlBase TextEditorControl = null;
		
		/// <summary>
		/// </summary>
		public event EventHandler ActionUndone;
		/// <summary>
		/// </summary>
		public event EventHandler ActionRedone;
		
		public event OperationEventHandler OperationPushed;
		
		/// <summary>
		/// Gets/Sets if changes to the document are protocolled by the undo stack.
		/// Used internally to disable the undo stack temporarily while undoing an action.
		/// </summary>
		internal bool AcceptChanges = true;
		
		/// <summary>
		/// Gets if there are actions on the undo stack.
		/// </summary>
		public bool CanUndo {
			get {
				return undostack.Count > 0;
			}
		}
		
		/// <summary>
		/// Gets if there are actions on the redo stack.
		/// </summary>
		public bool CanRedo {
			get {
				return redostack.Count > 0;
			}
		}
		
		/// <summary>
		/// Gets the number of actions on the undo stack.
		/// </summary>
		public int UndoItemCount {
			get {
				return undostack.Count;
			}
		}
		
		/// <summary>
		/// Gets the number of actions on the redo stack.
		/// </summary>
		public int RedoItemCount {
			get {
				return redostack.Count;
			}
		}
		
		int undoGroupDepth;
		int actionCountInUndoGroup;
		
		public void StartUndoGroup()
		{
			if (undoGroupDepth == 0) {
				actionCountInUndoGroup = 0;
			}
			undoGroupDepth++;
			//Util.LoggingService.Debug("Open undo group (new depth=" + undoGroupDepth + ")");
		}
		
		public void EndUndoGroup()
		{
			if (undoGroupDepth == 0)
				throw new InvalidOperationException("There are no open undo groups");
			undoGroupDepth--;
			//Util.LoggingService.Debug("Close undo group (new depth=" + undoGroupDepth + ")");
			if (undoGroupDepth == 0 && actionCountInUndoGroup > 1) {
				UndoQueue op = new UndoQueue(undostack, actionCountInUndoGroup);
				undostack.Push(op);
				if (OperationPushed != null) {
					OperationPushed(this, new OperationEventArgs(op));
				}
			}
		}
		
		public void AssertNoUndoGroupOpen()
		{
			if (undoGroupDepth != 0) {
				undoGroupDepth = 0;
				throw new InvalidOperationException("No undo group should be open at this point");
			}
		}
		
		/// <summary>
		/// Call this method to undo the last operation on the stack
		/// </summary>
		public void Undo()
		{
			AssertNoUndoGroupOpen();
			if (undostack.Count > 0) {
				IUndoableOperation uedit = (IUndoableOperation)undostack.Pop();
				redostack.Push(uedit);
				uedit.Undo();
				OnActionUndone();
			}
		}
		
		/// <summary>
		/// Call this method to redo the last undone operation
		/// </summary>
		public void Redo()
		{
			AssertNoUndoGroupOpen();
			if (redostack.Count > 0) {
				IUndoableOperation uedit = (IUndoableOperation)redostack.Pop();
				undostack.Push(uedit);
				uedit.Redo();
				OnActionRedone();
			}
		}
		
		/// <summary>
		/// Call this method to push an UndoableOperation on the undostack, the redostack
		/// will be cleared, if you use this method.
		/// </summary>
		public void Push(IUndoableOperation operation)
		{
			if (operation == null) {
				throw new ArgumentNullException("operation");
			}
			
			if (AcceptChanges) {
				StartUndoGroup();
				undostack.Push(operation);
				actionCountInUndoGroup++;
				if (TextEditorControl != null) {
					undostack.Push(new UndoableSetCaretPosition(this, TextEditorControl.ActiveTextAreaControl.Caret.Position));
					actionCountInUndoGroup++;
				}
				EndUndoGroup();
				ClearRedoStack();
			}
		}
		
		/// <summary>
		/// Call this method, if you want to clear the redo stack
		/// </summary>
		public void ClearRedoStack()
		{
			redostack.Clear();
		}
		
		/// <summary>
		/// Clears both the undo and redo stack.
		/// </summary>
		public void ClearAll()
		{
			AssertNoUndoGroupOpen();
			undostack.Clear();
			redostack.Clear();
			actionCountInUndoGroup = 0;
		}
		
		/// <summary>
		/// </summary>
		protected void OnActionUndone()
		{
			if (ActionUndone != null) {
				ActionUndone(null, null);
			}
		}
		
		/// <summary>
		/// </summary>
		protected void OnActionRedone()
		{
			if (ActionRedone != null) {
				ActionRedone(null, null);
			}
		}
		
		class UndoableSetCaretPosition : IUndoableOperation
		{
			UndoStack stack;
			TextLocation pos;
			TextLocation redoPos;
			
			public UndoableSetCaretPosition(UndoStack stack, TextLocation pos)
			{
				this.stack = stack;
				this.pos = pos;
			}
			
			public void Undo()
			{
				redoPos = stack.TextEditorControl.ActiveTextAreaControl.Caret.Position;
				stack.TextEditorControl.ActiveTextAreaControl.Caret.Position = pos;
				stack.TextEditorControl.ActiveTextAreaControl.SelectionManager.ClearSelection();
			}
			
			public void Redo()
			{
				stack.TextEditorControl.ActiveTextAreaControl.Caret.Position = redoPos;
				stack.TextEditorControl.ActiveTextAreaControl.SelectionManager.ClearSelection();
			}
		}
	}
		
	public class OperationEventArgs : EventArgs
	{
		public OperationEventArgs(IUndoableOperation op)
		{
			this.op = op;
		}
		
		IUndoableOperation op;
		
		public IUndoableOperation Operation {
			get {
				return op;
			}
		}
	}
	
	public delegate void OperationEventHandler(object sender, OperationEventArgs e);
}
