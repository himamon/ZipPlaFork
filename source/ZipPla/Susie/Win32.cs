using System;
using System.ComponentModel;			// Win32Exception
using System.Drawing;					// Size, Point, ...
using System.Runtime.InteropServices;	// DllImport, Marshal

namespace garu.Util
{
	public static class Win32
	{
		[Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;

			public RECT(int Left, int Top, int Right, int Bottom)
			{
				this.Left = Left;
				this.Top = Top;
				this.Right = Right;
				this.Bottom = Bottom;
			}

			public static implicit operator Rectangle(RECT winr)
			{
				return new Rectangle(winr.Left, winr.Top,
				winr.Right - winr.Left, winr.Bottom - winr.Top);
			}

			public static implicit operator RECT(Rectangle netr)
			{
				return new RECT(netr.X, netr.Y,
				netr.Width - netr.X, netr.Height - netr.Y);
			}
		}

		//-------------------------------------
		// user32.dll
		//-------------------------------------

		//
		// FindWindow
		//
		[DllImport("user32.dll")]
		public static extern IntPtr FindWindow(
			string lpClassName, string lpWindowName);

		//
		// FindWindowEx
		//
		[DllImport("user32.dll")]
		public static extern IntPtr FindWindowEx(
			IntPtr hWndParent, IntPtr hWndChildAfter,
			string lpszClass, string lpszWindow);

		//
		// SendMessage
		//
		[DllImport("user32.dll")]
		public static extern int SendMessage(
			IntPtr hWnd, WM Msg, int wParam, IntPtr lParam);

		public enum WM
		{
			NULL = 0x0000,
			CREATE = 0x0001,
			DESTROY = 0x0002,
			MOVE = 0x0003,
			SIZE = 0x0005,
			ACTIVATE = 0x0006,
			SETFOCUS = 0x0007,
			KILLFOCUS = 0x0008,
			ENABLE = 0x000A,
			SETREDRAW = 0x000B,
			SETTEXT = 0x000C,
			GETTEXT = 0x000D,
			GETTEXTLENGTH = 0x000E,
			PAINT = 0x000F,
			CLOSE = 0x0010,
			QUERYENDSESSION = 0x0011,
			QUERYOPEN = 0x0013,
			ENDSESSION = 0x0016,
			QUIT = 0x0012,
			ERASEBKGND = 0x0014,
			SYSCOLORCHANGE = 0x0015,
			SHOWWINDOW = 0x0018,
			WININICHANGE = 0x001A,
			SETTINGCHANGE = WININICHANGE,
			DEVMODECHANGE = 0x001B,
			ACTIVATEAPP = 0x001C,
			FONTCHANGE = 0x001D,
			TIMECHANGE = 0x001E,
			CANCELMODE = 0x001F,
			SETCURSOR = 0x0020,
			MOUSEACTIVATE = 0x0021,
			CHILDACTIVATE = 0x0022,
			QUEUESYNC = 0x0023,
			GETMINMAXINFO = 0x0024,
			PAINTICON = 0x0026,
			ICONERASEBKGND = 0x0027,
			NEXTDLGCTL = 0x0028,
			SPOOLERSTATUS = 0x002A,
			DRAWITEM = 0x002B,
			MEASUREITEM = 0x002C,
			DELETEITEM = 0x002D,
			VKEYTOITEM = 0x002E,
			CHARTOITEM = 0x002F,
			SETFONT = 0x0030,
			GETFONT = 0x0031,
			SETHOTKEY = 0x0032,
			GETHOTKEY = 0x0033,
			QUERYDRAGICON = 0x0037,
			COMPAREITEM = 0x0039,
			GETOBJECT = 0x003D,
			COMPACTING = 0x0041,
			COMMNOTIFY = 0x0044,
			WINDOWPOSCHANGING = 0x0046,
			WINDOWPOSCHANGED = 0x0047,
			POWER = 0x0048,
			COPYDATA = 0x004A,
			CANCELJOURNAL = 0x004B,
			NOTIFY = 0x004E,
			INPUTLANGCHANGEREQUEST = 0x0050,
			INPUTLANGCHANGE = 0x0051,
			TCARD = 0x0052,
			HELP = 0x0053,
			USERCHANGED = 0x0054,
			NOTIFYFORMAT = 0x0055,
			CONTEXTMENU = 0x007B,
			STYLECHANGING = 0x007C,
			STYLECHANGED = 0x007D,
			DISPLAYCHANGE = 0x007E,
			GETICON = 0x007F,
			SETICON = 0x0080,
			NCCREATE = 0x0081,
			NCDESTROY = 0x0082,
			NCCALCSIZE = 0x0083,
			NCHITTEST = 0x0084,
			NCPAINT = 0x0085,
			NCACTIVATE = 0x0086,
			GETDLGCODE = 0x0087,
			SYNCPAINT = 0x0088,
			NCMOUSEMOVE = 0x00A0,
			NCLBUTTONDOWN = 0x00A1,
			NCLBUTTONUP = 0x00A2,
			NCLBUTTONDBLCLK = 0x00A3,
			NCRBUTTONDOWN = 0x00A4,
			NCRBUTTONUP = 0x00A5,
			NCRBUTTONDBLCLK = 0x00A6,
			NCMBUTTONDOWN = 0x00A7,
			NCMBUTTONUP = 0x00A8,
			NCMBUTTONDBLCLK = 0x00A9,
			NCXBUTTONDOWN = 0x00AB,
			NCXBUTTONUP = 0x00AC,
			NCXBUTTONDBLCLK = 0x00AD,
			INPUT_DEVICE_CHANGE = 0x00FE,
			INPUT = 0x00FF,
			KEYFIRST = 0x0100,
			KEYDOWN = 0x0100,
			KEYUP = 0x0101,
			CHAR = 0x0102,
			DEADCHAR = 0x0103,
			SYSKEYDOWN = 0x0104,
			SYSKEYUP = 0x0105,
			SYSCHAR = 0x0106,
			SYSDEADCHAR = 0x0107,
			UNICHAR = 0x0109,
			KEYLAST = 0x0109,
			//KEYLAST = 0x0108,
			IME_STARTCOMPOSITION = 0x010D,
			IME_ENDCOMPOSITION = 0x010E,
			IME_COMPOSITION = 0x010F,
			IME_KEYLAST = 0x010F,
			INITDIALOG = 0x0110,
			COMMAND = 0x0111,
			SYSCOMMAND = 0x0112,
			TIMER = 0x0113,
			HSCROLL = 0x0114,
			VSCROLL = 0x0115,
			INITMENU = 0x0116,
			INITMENUPOPUP = 0x0117,
			MENUSELECT = 0x011F,
			MENUCHAR = 0x0120,
			ENTERIDLE = 0x0121,
			MENURBUTTONUP = 0x0122,
			MENUDRAG = 0x0123,
			MENUGETOBJECT = 0x0124,
			UNINITMENUPOPUP = 0x0125,
			MENUCOMMAND = 0x0126,
			CHANGEUISTATE = 0x0127,
			UPDATEUISTATE = 0x0128,
			QUERYUISTATE = 0x0129,
			CTLCOLORMSGBOX = 0x0132,
			CTLCOLOREDIT = 0x0133,
			CTLCOLORLISTBOX = 0x0134,
			CTLCOLORBTN = 0x0135,
			CTLCOLORDLG = 0x0136,
			CTLCOLORSCROLLBAR = 0x0137,
			CTLCOLORSTATIC = 0x0138,
			MOUSEFIRST = 0x0200,
			MOUSEMOVE = 0x0200,
			LBUTTONDOWN = 0x0201,
			LBUTTONUP = 0x0202,
			LBUTTONDBLCLK = 0x0203,
			RBUTTONDOWN = 0x0204,
			RBUTTONUP = 0x0205,
			RBUTTONDBLCLK = 0x0206,
			MBUTTONDOWN = 0x0207,
			MBUTTONUP = 0x0208,
			MBUTTONDBLCLK = 0x0209,
			MOUSEWHEEL = 0x020A,
			XBUTTONDOWN = 0x020B,
			XBUTTONUP = 0x020C,
			XBUTTONDBLCLK = 0x020D,
			MOUSEHWHEEL = 0x020E,
			MOUSELAST = 0x020E,
			PARENTNOTIFY = 0x0210,
			ENTERMENULOOP = 0x0211,
			EXITMENULOOP = 0x0212,
			NEXTMENU = 0x0213,
			SIZING = 0x0214,
			CAPTURECHANGED = 0x0215,
			MOVING = 0x0216,
			POWERBROADCAST = 0x0218,
			DEVICECHANGE = 0x0219,
			MDICREATE = 0x0220,
			MDIDESTROY = 0x0221,
			MDIACTIVATE = 0x0222,
			MDIRESTORE = 0x0223,
			MDINEXT = 0x0224,
			MDIMAXIMIZE = 0x0225,
			MDITILE = 0x0226,
			MDICASCADE = 0x0227,
			MDIICONARRANGE = 0x0228,
			MDIGETACTIVE = 0x0229,
			MDISETMENU = 0x0230,
			ENTERSIZEMOVE = 0x0231,
			EXITSIZEMOVE = 0x0232,
			DROPFILES = 0x0233,
			MDIREFRESHMENU = 0x0234,
			IME_SETCONTEXT = 0x0281,
			IME_NOTIFY = 0x0282,
			IME_CONTROL = 0x0283,
			IME_COMPOSITIONFULL = 0x0284,
			IME_SELECT = 0x0285,
			IME_CHAR = 0x0286,
			IME_REQUEST = 0x0288,
			IME_KEYDOWN = 0x0290,
			IME_KEYUP = 0x0291,
			MOUSEHOVER = 0x02A1,
			MOUSELEAVE = 0x02A3,
			NCMOUSEHOVER = 0x02A0,
			NCMOUSELEAVE = 0x02A2,
			WTSSESSION_CHANGE = 0x02B1,
			TABLET_FIRST = 0x02c0,
			TABLET_LAST = 0x02df,
			CUT = 0x0300,
			COPY = 0x0301,
			PASTE = 0x0302,
			CLEAR = 0x0303,
			UNDO = 0x0304,
			RENDERFORMAT = 0x0305,
			RENDERALLFORMATS = 0x0306,
			DESTROYCLIPBOARD = 0x0307,
			DRAWCLIPBOARD = 0x0308,
			PAINTCLIPBOARD = 0x0309,
			VSCROLLCLIPBOARD = 0x030A,
			SIZECLIPBOARD = 0x030B,
			ASKCBFORMATNAME = 0x030C,
			CHANGECBCHAIN = 0x030D,
			HSCROLLCLIPBOARD = 0x030E,
			QUERYNEWPALETTE = 0x030F,
			PALETTEISCHANGING = 0x0310,
			PALETTECHANGED = 0x0311,
			HOTKEY = 0x0312,
			PRINT = 0x0317,
			PRINTCLIENT = 0x0318,
			APPCOMMAND = 0x0319,
			THEMECHANGED = 0x031A,
			CLIPBOARDUPDATE = 0x031D,
			DWMCOMPOSITIONCHANGED = 0x031E,
			DWMNCRENDERINGCHANGED = 0x031F,
			DWMCOLORIZATIONCOLORCHANGED = 0x0320,
			DWMWINDOWMAXIMIZEDCHANGE = 0x0321,
			GETTITLEBARINFOEX = 0x033F,
			HANDHELDFIRST = 0x0358,
			HANDHELDLAST = 0x035F,
			AFXFIRST = 0x0360,
			AFXLAST = 0x037F,
			PENWINFIRST = 0x0380,
			PENWINLAST = 0x038F,
			APP = 0x8000,
			USER = 0x0400,
		}

		//
		// WindowFromPoint
		//
		[DllImport("user32.dll")]
		public static extern IntPtr WindowFromPoint(Point Point);

		//
		// ShowWindow
		//
		[DllImport("user32.dll")]
		public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
		public const int SW_NORMAL = 1;

		//
		// SetWindowPos
		//
		[DllImport("user32.dll")]
		public static extern bool SetWindowPos(
			IntPtr hWnd, IntPtr hWndInsertAfter,
			int X, int Y, int cx, int cy, SWPF uFlags);

		[Flags]
		public enum SWPF
		{
			NOSIZE = 0x0001,
			NOMOVE = 0x0002,
			NOZORDER = 0x0004,
			NOREDRAW = 0x0008,
			NOACTIVATE = 0x0010,
			FRAMECHANGED = 0x0020,
			SHOWWINDOW = 0x0040,
			HIDEWINDOW = 0x0080,
			NOCOPYBITS = 0x0100,
			NOOWNERZORDER = 0x0200,
			NOSENDCHANGING = 0x0400,
			DRAWFRAME = FRAMECHANGED,
			NOREPOSITION = NOOWNERZORDER,
			DEFERERASE = 0x2000,
			ASYNCWINDOWPOS = 0x4000,
		}

		public static readonly IntPtr HWND_TOP = new IntPtr(0);
		public static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
		public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
		public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

		//
		// GetWindowRect
		//
		[DllImport("user32.dll")]
		public static extern bool GetWindowRect(
			IntPtr hwnd, out RECT lpRect);

		//
		// ScreenToClient
		//
		[DllImport("user32.dll")]
		public static extern bool ScreenToClient(
			IntPtr hWnd, ref Point lpPoint);

		//
		// ClientToScreen
		//
		[DllImport("user32.dll")]
		public static extern bool ClientToScreen(
			IntPtr hWnd, ref Point lpPoint);

		//
		// GetDC
		//
		[DllImport("user32.dll")]
		public static extern IntPtr GetDC(IntPtr hWnd);

		//
		// ReleaseDC
		//
		[DllImport("user32.dll")]
		public static extern Int32 ReleaseDC(IntPtr hWnd, IntPtr hDC);

		//
		// SendInput
		//
		[DllImport("user32.dll", SetLastError = true)]
		public static extern uint SendInput(
			uint nInputs, INPUT[] pInputs, int cbSize);

		//
		// (Local) DoMouseButtonEvent
		//
		public static void DoMouseButtonEvent(MOUSEEVENTF buttonf)
		{
			INPUT[] input = new INPUT[1];
			input[0].type = INPUT_MOUSE;
			input[0].mi.dx = 0;
			input[0].mi.dy = 0;
			input[0].mi.mouseData = 0;
			input[0].mi.dwFlags = buttonf;
			input[0].mi.time = 0;
			input[0].mi.dwExtraInfo = IntPtr.Zero;
			int cbSize = Marshal.SizeOf(input[0]);
			SendInput(1, input, cbSize);
		}

		[Flags]
		public enum KEYEVENTF
		{
			EXTENDEDKEY = 0x0001,
			KEYUP = 0x0002,
			UNICODE = 0x0004,
			SCANCODE = 0x0008,
		}

		[Flags]
		public enum MOUSEEVENTF
		{
			MOVE = 0x0001,
			LEFTDOWN = 0x0002,
			LEFTUP = 0x0004,
			RIGHTDOWN = 0x0008,
			RIGHTUP = 0x0010,
			MIDDLEDOWN = 0x0020,
			MIDDLEUP = 0x0040,
			XDOWN = 0x0080,
			XUP = 0x0100,
			WHEEL = 0x0800,
			HWHEEL = 0x01000,
			MOVE_NOCOALESCE = 0x2000,
			VIRTUALDESK = 0x4000,
			ABSOLUTE = 0x8000,
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct MOUSEINPUT
		{
			public int dx;
			public int dy;
			public uint mouseData;
			public MOUSEEVENTF dwFlags;
			public uint time;
			public IntPtr dwExtraInfo;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct KEYBDINPUT
		{
			public ushort wVk;
			public ushort wScan;
			public KEYEVENTF dwFlags;
			public uint time;
			public IntPtr dwExtraInfo;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct HARDWAREINPUT
		{
			public uint uMsg;
			public ushort wParamL;
			public ushort wParamH;
		}

		public const uint INPUT_MOUSE = 0;
		public const uint INPUT_KEYBOARD = 1;
		public const uint INPUT_HARDWARE = 2;

		[StructLayout(LayoutKind.Explicit, Size = 28)]
		public struct INPUT
		{
			[FieldOffset(0)]
			public uint type;
			[FieldOffset(4)]
			public MOUSEINPUT mi;
			[FieldOffset(4)]
			public KEYBDINPUT ki;
			[FieldOffset(4)]
			public HARDWAREINPUT hi;
		}

		//
		// GetAsyncKeyState
		//
		[DllImport("user32.dll")]
		public static extern short GetAsyncKeyState(int vKey);

		//
		// SetWindowsHookEx
		//
		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(
			WH hookType, HookHandler hookDelegate, IntPtr module, uint threadId);
		public static IntPtr SetWindowsHookEx(
			WH hookType, HookHandler hookDelegate)
		{
			IntPtr hMod = Marshal.GetHINSTANCE(
			System.Reflection.Assembly.
			GetExecutingAssembly().GetModules()[0]);
			IntPtr hook = SetWindowsHookEx(hookType, hookDelegate, hMod, 0);
			if (hook == IntPtr.Zero) {
				int errorCode = Marshal.GetLastWin32Error();
				throw new Win32Exception(errorCode);
			}
			return hook;
		}

		public enum WH
		{
			MIN = (-1),
			MSGFILTER = (-1),
			JOURNALRECORD = 0,
			JOURNALPLAYBACK = 1,
			KEYBOARD = 2,
			GETMESSAGE = 3,
			CALLWNDPROC = 4,
			CBT = 5,
			SYSMSGFILTER = 6,
			MOUSE = 7,
			HARDWARE = 8,
			DEBUG = 9,
			SHELL = 10,
			FOREGROUNDIDLE = 11,
			CALLWNDPROCRET = 12,
			KEYBOARD_LL = 13,
			MOUSE_LL = 14,
			MAX = 14,
			MINHOOK = MIN,
			MAXHOOK = MAX,
		}

		//
		// UnhookWindowsHookEx
		//
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool UnhookWindowsHookEx(IntPtr hook);

		//
		// CallNextHookEx
		//
		[DllImport("user32.dll")]
		public static extern int CallNextHookEx(
			IntPtr hook, int code, WM message, IntPtr state);

		public const int VK_SHIFT = 0x10;
		public const int VK_CONTROL = 0x11;
		public const int VK_MENU = 0x12;

		//
		// STRUCT for Hook
		//
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct KeyboardHookStruct
		{
			public int vkCode;
			public int scanCode;
			public int flags;
			public int time;
			public int dwExtraInfo;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct MouseHookStruct
		{
			public Point pt;
			public int mouseData;
			public int flags;
			public int time;
			public int dwExtraInfo;
		}

		//
		// DELEGATE for Hook
		//
		public delegate int HookHandler(
			int code, WM message, IntPtr state);

		//-------------------------------------
		// kernel32.dll
		//-------------------------------------

		//
		// LoadLibrary
		//
		[DllImport("kernel32", SetLastError = true)]
		public extern static IntPtr LoadLibrary(string lpFileName);

		//
		// FreeLibrary
		//
		[DllImport("kernel32", SetLastError = true)]
		public extern static bool FreeLibrary(IntPtr hModule);

		//
		// GetProcAddress
		//
		[DllImport("kernel32", SetLastError = true)]
		public extern static IntPtr GetProcAddress(
			IntPtr hModule, string lpProcName);

		//
		// LocalLock
		//
		[DllImport("kernel32")]
		public extern static IntPtr LocalLock(IntPtr hMem);

		//
		// LocalUnLock
		//
		[DllImport("kernel32")]
		public extern static bool LocalUnlock(IntPtr hMem);

		//
		// LocalFree
		//
		[DllImport("kernel32")]
		public extern static IntPtr LocalFree(IntPtr hMem);

		//
		// CopyMemory
		//
		[DllImport("kernel32")]
		public extern static void CopyMemory(IntPtr dst, IntPtr src, int len);

		//-------------------------------------
		// gdi32.dll
		//-------------------------------------

		//
		// STRUCT for DIBs
		//
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct BITMAPFILEHEADER
		{
			public ushort bfType;
			public uint bfSize;
			public ushort bfReserved1;
			public ushort bfReserved2;
			public uint bfOffBits;
		}
		public const ushort BM = 0x4d42;	// 'BM' ... set to BITMAPFILEHEADER.bfType

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct BITMAPINFOHEADER
		{
			public uint biSize;
			public int biWidth;
			public int biHeight;
			public ushort biPlanes;
			public ushort biBitCount;
			public uint biCompression;
			public uint biSizeImage;
			public int biXPelsPerMeter;
			public int biYPelsPerMeter;
			public uint biClrUsed;
			public uint biClrImportant;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct BITMAPINFO
		{
			public BITMAPINFOHEADER bmiHeader;
			public RGBQUAD biColors;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct RGBQUAD
		{
			public byte rgbBlue;
			public byte rgbGreen;
			public byte rgbRed;
			public byte rgbReserved;
		}

		//
		// CreateDC
		//
		[DllImport("gdi32.dll")]
		public static extern IntPtr CreateDC(string strDriver,
			string strDevice, string strOutput, IntPtr pData);

		//
		// CreateCompatibleDC
		//
		[DllImport("gdi32.dll")]
		public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

		//
		// CreateDIBSection
		//
		[DllImport("gdi32.dll")]
		public static extern IntPtr CreateDIBSection(
			IntPtr hdc, ref BITMAPINFO pbmi, uint iUsage,
			out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

		//
		// CreateDIBitmap
		//
		[DllImport("gdi32.dll")]
		public static extern IntPtr CreateDIBitmap(
			IntPtr hdc, ref BITMAPINFOHEADER lpbmih, uint fdwInit,
			out IntPtr lpbInit, ref BITMAPINFO ipbmi, uint fuUsage);

		//
		// DeleteDC
		//
		[DllImport("gdi32.dll")]
		public static extern bool DeleteDC(IntPtr hdc);

		//
		// CreateCompatibleBitmap
		//
		[DllImport("gdi32.dll")]
		public static extern IntPtr CreateCompatibleBitmap(
			IntPtr hdc, int nWidth, int nHeight);

		//
		// SelectObject
		//
		[DllImport("gdi32.dll")]
		public static extern IntPtr SelectObject(
			IntPtr hdc, IntPtr hgdiobj);

		//
		// DeleteObject
		//
		[DllImport("gdi32.dll")]
		public static extern bool DeleteObject(IntPtr hObject);

		//
		// GetPixel
		//
		[DllImport("gdi32.dll")]
		public static extern uint GetPixel(
			IntPtr hdc, int nXPos, int nYPos);

		//
		// (Local) GetPixel by hWnd,Point
		//
		public static uint GetPixel(IntPtr hWnd, Point pt)
		{
			IntPtr hDC = Win32.GetDC(hWnd);
			uint cl = GetPixel(hDC, pt.X, pt.Y);
			Win32.ReleaseDC(hWnd, hDC);
			return cl;
		}

		//
		// BitBlt
		//
		[DllImport("gdi32.dll")]
		public static extern bool BitBlt(
			IntPtr hdc, int x, int y, int cx, int cy,
			IntPtr hdcSrc, int x1, int y1, BITBLTOP rop);

		public enum BITBLTOP
		{
			SRCCOPY = 0x00CC0020,		// dest = source
			SRCPAINT = 0x00EE0086,		// dest = source OR dest
			SRCAND = 0x008800C6,		// dest = source AND dest
			SRCINVERT = 0x00660046,		// dest = source XOR dest
			SRCERASE = 0x00440328,		// dest = source AND (NOT dest)
			NOTSRCCOPY = 0x00330008,	// dest = (NOT source)
			NOTSRCERASE = 0x001100A6,	// dest = (NOT src) AND (NOT dest)
			MERGECOPY = 0x00C000CA,		// dest = (source AND pattern)
			MERGEPAINT = 0x00BB0226,	// dest = (NOT source) OR dest
			PATCOPY = 0x00F00021,		// dest = pattern
			PATPAINT = 0x00FB0A09,		// dest = DPSnoo
			PATINVERT = 0x005A0049,		// dest = pattern XOR dest
			DSTINVERT = 0x00550009,		// dest = (NOT dest)
			BLACKNESS = 0x00000042,		// dest = BLACK
			WHITENESS = 0x00FF0062,		// dest = WHITE
		}

		//
		// GetTextExtentPoint32
		//
		[DllImport("gdi32.dll")]
		public static extern bool GetTextExtentPoint32(
			IntPtr hdc, string lpString,
			int cbString, out Size lpSize);

		//
		// CreatePen
		//
		[DllImport("gdi32.dll")]
		public static extern IntPtr CreatePen(
			PS penStyle, int Width, int color);

		public enum PS
		{
			SOLID = 0,
			DASH = 1,		// -------
			DOT = 2,		// .......
			DASHDOT = 3,	// _._._._
			DASHDOTDOT = 4,	// _.._.._
			NULL = 5,
			INSIDEFRAME = 6,
			USERSTYLE = 7,
			ALTERNATE = 8,
			STYLE_MASK = 0x0000000F,
			ENDCAP_ROUND = 0x00000000,
			ENDCAP_SQUARE = 0x00000100,
			ENDCAP_FLAT = 0x00000200,
			ENDCAP_MASK = 0x00000F00,
			JOIN_ROUND = 0x00000000,
			JOIN_BEVEL = 0x00001000,
			JOIN_MITER = 0x00002000,
			JOIN_MASK = 0x0000F000,
			COSMETIC = 0x00000000,
			GEOMETRIC = 0x00010000,
			TYPE_MASK = 0x000F0000,
		}

		//
		// CreateSolidBrush
		//
		[DllImport("gdi32.dll")]
		public static extern IntPtr CreateSolidBrush(int color);

		//
		// MoveToEx
		//
		[DllImport("gdi32.dll")]
		public static extern bool MoveToEx(
			IntPtr hdc, int x, int y, ref Point lppt);

		//
		// LineTo
		//
		[DllImport("gdi32.dll")]
		public static extern bool LineTo(IntPtr hdc, int x, int y);

		//
		// Rectangle
		//
		[DllImport("gdi32.dll")]
		public static extern bool Rectangle(
			IntPtr hdc, int left, int top, int right, int bottom);

		//
		// Ellipse
		//
		[DllImport("gdi32.dll")]
		public static extern bool Ellipse(
			IntPtr hdc, int left, int top, int right, int bottom);
	}
}
