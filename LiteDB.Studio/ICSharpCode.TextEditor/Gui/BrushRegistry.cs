// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ICSharpCode.TextEditor
{
	/// <summary>
	/// Contains brushes/pens for the text editor to speed up drawing. Re-Creation of brushes and pens
	/// seems too costly.
	/// </summary>
	public class BrushRegistry
	{
		static Dictionary<Color, Brush> brushes = new Dictionary<Color, Brush>();
		static Dictionary<Color, Pen> pens = new Dictionary<Color, Pen>();
		static Dictionary<Color, Pen> dotPens = new Dictionary<Color, Pen>();
		
		public static Brush GetBrush(Color color)
		{
			lock (brushes) {
				Brush brush;
				if (!brushes.TryGetValue(color, out brush)) {
					brush = new SolidBrush(color);
					brushes.Add(color, brush);
				}
				return brush;
			}
		}
		
		public static Pen GetPen(Color color)
		{
			lock (pens) {
				Pen pen;
				if (!pens.TryGetValue(color, out pen)) {
					pen = new Pen(color);
					pens.Add(color, pen);
				}
				return pen;
			}
		}
		
		static readonly float[] dotPattern = { 1, 1, 1, 1 };
		
		public static Pen GetDotPen(Color color)
		{
			lock (dotPens) {
				Pen pen;
				if (!dotPens.TryGetValue(color, out pen)) {
					pen = new Pen(color);
					pen.DashPattern = dotPattern;
					dotPens.Add(color, pen);
				}
				return pen;
			}
		}
	}
}
