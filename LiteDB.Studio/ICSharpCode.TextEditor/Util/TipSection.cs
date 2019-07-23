// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Diagnostics;
using System.Drawing;

namespace ICSharpCode.TextEditor.Util
{
	abstract class TipSection
	{
		SizeF    tipAllocatedSize;
		Graphics tipGraphics;
		SizeF    tipMaxSize;
		SizeF    tipRequiredSize;
		
		protected TipSection(Graphics graphics)
		{
			tipGraphics = graphics;
		}
		
		public abstract void Draw(PointF location);
		
		public SizeF GetRequiredSize()
		{
			return tipRequiredSize;
		}
		
		public void SetAllocatedSize(SizeF allocatedSize)
		{
			Debug.Assert(allocatedSize.Width >= tipRequiredSize.Width &&
			             allocatedSize.Height >= tipRequiredSize.Height);
			
			tipAllocatedSize = allocatedSize; OnAllocatedSizeChanged();
		}
		
		public void SetMaximumSize(SizeF maximumSize)
		{
			tipMaxSize = maximumSize; OnMaximumSizeChanged();
		}
		
		protected virtual void OnAllocatedSizeChanged()
		{
			
		}
		
		protected virtual void OnMaximumSizeChanged()
		{
			
		}
		
		protected void SetRequiredSize(SizeF requiredSize)
		{
			requiredSize.Width  = Math.Max(0, requiredSize.Width);
			requiredSize.Height = Math.Max(0, requiredSize.Height);
			requiredSize.Width  = Math.Min(tipMaxSize.Width, requiredSize.Width);
			requiredSize.Height = Math.Min(tipMaxSize.Height, requiredSize.Height);
			
			tipRequiredSize = requiredSize;
		}
		
		protected Graphics Graphics	{
			get {
				return tipGraphics;
			}
		}
		
		protected SizeF AllocatedSize {
			get {
				return tipAllocatedSize;
			}
		}
		
		protected SizeF MaximumSize {
			get {
				return tipMaxSize;
			}
		}
	}
}
