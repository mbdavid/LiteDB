// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Shinsaku Nakagawa" email="shinsaku@users.sourceforge.jp"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ICSharpCode.TextEditor
{
	/// <summary>
	/// Used internally, not for own use.
	/// </summary>
	internal class Ime
	{
		public Ime(IntPtr hWnd, Font font)
		{
			// For unknown reasons, the IME support is causing crashes when used in a WOW64 process
			// or when used in .NET 4.0. We'll disable IME support in those cases.
			string PROCESSOR_ARCHITEW6432 = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432");
			if (PROCESSOR_ARCHITEW6432 == "IA64" || PROCESSOR_ARCHITEW6432 == "AMD64" || Environment.OSVersion.Platform == PlatformID.Unix || Environment.Version >= new Version(4,0)) {
				disableIME = true;
			} else {
				this.hIMEWnd = ImmGetDefaultIMEWnd(hWnd);
			}
			this.hWnd = hWnd;
			this.font = font;
			SetIMEWindowFont(font);
		}

		private Font font = null;
		public Font Font
		{
			get {
				return font;
			}
			set {
				if (!value.Equals(font)) {
					font = value;
					lf = null;
					SetIMEWindowFont(value);
				}
			}
		}

		public IntPtr HWnd
		{
			set {
				if (this.hWnd != value) {
					this.hWnd = value;
					if (!disableIME)
						this.hIMEWnd = ImmGetDefaultIMEWnd(value);
					SetIMEWindowFont(font);
				}
			}
		}

		[ DllImport("imm32.dll") ]
		private static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);

		[ DllImport("user32.dll") ]
		private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, COMPOSITIONFORM lParam);
		[ DllImport("user32.dll") ]
		private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, [In, MarshalAs(UnmanagedType.LPStruct)] LOGFONT lParam);

		[ StructLayout(LayoutKind.Sequential) ]
		private class COMPOSITIONFORM
		{
			public int dwStyle = 0;
			public POINT ptCurrentPos = null;
			public RECT rcArea = null;
		}

		[ StructLayout(LayoutKind.Sequential) ]
		private class POINT
		{
			public int x = 0;
			public int y = 0;
		}

		[ StructLayout(LayoutKind.Sequential) ]
		private class RECT
		{
			public int left = 0;
			public int top = 0;
			public int right = 0;
			public int bottom = 0;
		}

		private const int WM_IME_CONTROL = 0x0283;

		private const int IMC_SETCOMPOSITIONWINDOW = 0x000c;
		private IntPtr hIMEWnd;
		private IntPtr hWnd;
		private const int CFS_POINT = 0x0002;

		[ StructLayout(LayoutKind.Sequential) ]
		private class LOGFONT
		{
			public int lfHeight = 0;
			public int lfWidth = 0;
			public int lfEscapement = 0;
			public int lfOrientation = 0;
			public int lfWeight = 0;
			public byte lfItalic = 0;
			public byte lfUnderline = 0;
			public byte lfStrikeOut = 0;
			public byte lfCharSet = 0;
			public byte lfOutPrecision = 0;
			public byte lfClipPrecision = 0;
			public byte lfQuality = 0;
			public byte lfPitchAndFamily = 0;
			[ MarshalAs(UnmanagedType.ByValTStr, SizeConst=32) ] public string lfFaceName = null;
		}
		private const int IMC_SETCOMPOSITIONFONT = 0x000a;
		LOGFONT lf = null;
		static bool disableIME;
		
		private void SetIMEWindowFont(Font f)
		{
			if (disableIME || hIMEWnd == IntPtr.Zero) return;
			
			if (lf == null) {
				lf = new LOGFONT();
				f.ToLogFont(lf);
				lf.lfFaceName = f.Name;  // This is very important! "Font.ToLogFont" Method sets invalid value to LOGFONT.lfFaceName
			}

			try {
				SendMessage(
					hIMEWnd,
					WM_IME_CONTROL,
					new IntPtr(IMC_SETCOMPOSITIONFONT),
					lf
				);
			} catch (AccessViolationException ex) {
				Handle(ex);
			}
		}

		public void SetIMEWindowLocation(int x, int y)
		{
			if (disableIME || hIMEWnd == IntPtr.Zero) return;

			POINT p = new POINT();
			p.x = x;
			p.y = y;

			COMPOSITIONFORM lParam = new COMPOSITIONFORM();
			lParam.dwStyle = CFS_POINT;
			lParam.ptCurrentPos = p;
			lParam.rcArea = new RECT();

			try {
				SendMessage(
					hIMEWnd,
					WM_IME_CONTROL,
					new IntPtr(IMC_SETCOMPOSITIONWINDOW),
					lParam
				);
			} catch (AccessViolationException ex) {
				Handle(ex);
			}
		}
		
		void Handle(Exception ex)
		{
			Console.WriteLine(ex);
			if (!disableIME) {
				disableIME = true;
				MessageBox.Show("Error calling IME: " + ex.Message + "\nIME is disabled.", "IME error");
			}
		}
	}
}
