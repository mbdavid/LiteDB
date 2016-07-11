// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace LiteDB.Tests.Mac
{
	[Register ("MainWindowController")]
	partial class MainWindowController
	{
		[Outlet]
		AppKit.NSOutlineView TestOutlineView { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (TestOutlineView != null) {
				TestOutlineView.Dispose ();
				TestOutlineView = null;
			}
		}
	}
}
