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
	class TipSplitter: TipSection
	{
		bool         isHorizontal;
		float     [] offsets;
		TipSection[] tipSections;
		
		public TipSplitter(Graphics graphics, bool horizontal, params TipSection[] sections): base(graphics)
		{
			Debug.Assert(sections != null);
			
			isHorizontal = horizontal;
			offsets = new float[sections.Length];
			tipSections = (TipSection[])sections.Clone();	
		}
		
		public override void Draw(PointF location)
		{
			if (isHorizontal) {
				for (int i = 0; i < tipSections.Length; i ++) {
					tipSections[i].Draw
						(new PointF(location.X + offsets[i], location.Y));
				}
			} else {
				for (int i = 0; i < tipSections.Length; i ++) {
					tipSections[i].Draw
						(new PointF(location.X, location.Y + offsets[i]));
				}
			}
		}
		
		protected override void OnMaximumSizeChanged()
		{
			base.OnMaximumSizeChanged();
			
			float currentDim = 0;
			float otherDim = 0;
			SizeF availableArea = MaximumSize;
			
			for (int i = 0; i < tipSections.Length; i ++) {
				TipSection section = (TipSection)tipSections[i];
			
				section.SetMaximumSize(availableArea);
				
				SizeF requiredArea = section.GetRequiredSize();
				offsets[i] = currentDim;

				// It's best to start on pixel borders, so this will
				// round up to the nearest pixel. Otherwise there are
				// weird cutoff artifacts.
				float pixelsUsed;
				
				if (isHorizontal) {
					pixelsUsed  = (float)Math.Ceiling(requiredArea.Width);
					currentDim += pixelsUsed;
					
					availableArea.Width = Math.Max
						(0, availableArea.Width - pixelsUsed);
					
					otherDim = Math.Max(otherDim, requiredArea.Height);
				} else {
					pixelsUsed  = (float)Math.Ceiling(requiredArea.Height);
					currentDim += pixelsUsed;
					
					availableArea.Height = Math.Max
						(0, availableArea.Height - pixelsUsed);
					
					otherDim = Math.Max(otherDim, requiredArea.Width);
				}
			}
			
			foreach (TipSection section in tipSections) {
				if (isHorizontal) {
					section.SetAllocatedSize(new SizeF(section.GetRequiredSize().Width, otherDim));
				} else {
					section.SetAllocatedSize(new SizeF(otherDim, section.GetRequiredSize().Height));
				}
			}

			if (isHorizontal) {
				SetRequiredSize(new SizeF(currentDim, otherDim));
			} else {
				SetRequiredSize(new SizeF(otherDim, currentDim));
			}
		}
	}
}
