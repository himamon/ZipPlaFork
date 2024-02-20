#if !AUTOBUILD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TouchLibrary.TouchWindow
{
    public abstract class TouchWindowManager : NativeWindow, IDisposable
    {
        protected abstract bool IsHandleCreated();
        protected abstract IntPtr GetHandle();
        protected abstract void AddHandleCreated(EventHandler handleCreated);
        protected abstract void RemoveHandleCreated(EventHandler handleCreated);
        protected abstract void AddHandleDestroyed(EventHandler handleDestroyed);
        protected abstract void RemoveHandleDestroyed(EventHandler handleDestroyed);

        public TouchWindowManager(TWF ulFlags)
        {
            this.ulFlags = ulFlags;
        }

        private bool initialized = false;
        private void Initialize()
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(TouchWindowManager));
            if (initialized) return;
            if (IsHandleCreated()) RegisterTouchWindow();
            AddHandleCreated(OnHandleCreated);
            AddHandleDestroyed(OnHandleDestroyed);
            initialized = true;
        }

        public bool IsTouchWindowRegistered { get { Initialize(); return isTouchWindowRegistered; } }
        public bool? CanRegisterTouchWindow { get { Initialize(); return canRegisterTouchWindow; } }

        private event TouchWindowEventHandler _Touch;
        public event TouchWindowEventHandler Touch { add { Initialize(); _Touch += value; } remove { _Touch -= value; } }

#region Private Members
        private TWF ulFlags;
        private bool isTouchWindowRegistered = false;
        private bool? canRegisterTouchWindow = null;

        private void OnHandleCreated(object sender, EventArgs e)
        {
            RegisterTouchWindow();
        }

        private void OnHandleDestroyed(object sender, EventArgs e)
        {
            UnregisterTouchWindow();
        }

        private void RegisterTouchWindow()
        {
            if (!isTouchWindowRegistered && canRegisterTouchWindow != false)
            {
                var handle = GetHandle();
                AssignHandle(handle);
                isTouchWindowRegistered = RegisterTouchWindow(handle, ulFlags);
                canRegisterTouchWindow = isTouchWindowRegistered;
                if (!isTouchWindowRegistered) ReleaseHandle();
            }
        }

        private void UnregisterTouchWindow()
        {
            if (isTouchWindowRegistered)
            {
                var handle = GetHandle();
                var result = UnregisterTouchWindow(handle);
                ReleaseHandle();
                isTouchWindowRegistered = false;
            }
        }

        private bool WndProc_RequestMouseMoveCancel = false;
        private bool WndProc_RequestLButtonDownCancel = false;
        private bool WndProc_RequestLButtonUpCancel = false;
        protected override void WndProc(ref Message m)
        {
            const int WM_TOUCH = 0x0240;
            const int WM_MOUSEMOVE = 0x0200;
            const int WM_LBUTTONDOWN = 0x201;
            const int WM_LBUTTONUP = 0x0202;
            var handled = false;
            var msg = m.Msg;
            if (msg == WM_TOUCH)
            {
                if (_Touch != null)
                {
                    var inputCount = m.WParam.ToInt32() & 0xFFFF;
                    TOUCHINPUT[] inputs = new TOUCHINPUT[inputCount];
                    if (GetTouchInputInfo(m.LParam, inputCount, inputs, Marshal.SizeOf(inputs[0])))
                    {
                        var e = new TouchWindowEventArgs(inputs);
                        _Touch(this, e);
                        WndProc_RequestMouseMoveCancel = false;
                        if (e.Handled)
                        {
                            CloseTouchInputHandle(m.LParam);
                            if (inputCount == 1)
                            {
                                var flags = inputs[0].dwFlags;
                                if ((flags & TOUCHEVENTF.UP) == TOUCHEVENTF.UP)
                                {
                                    WndProc_RequestLButtonUpCancel = true;
                                }
                                else
                                {
                                    WndProc_RequestMouseMoveCancel = true;
                                    if ((flags & TOUCHEVENTF.DOWN) == TOUCHEVENTF.DOWN)
                                    {
                                        WndProc_RequestLButtonDownCancel = true;
                                    }
                                }
                            }
                            handled = true;
                        }
                    }
                }
            }
            else if (msg == WM_MOUSEMOVE)
            {
                if (WndProc_RequestMouseMoveCancel)
                {
                    handled = true;
                }
            }
            else if (msg == WM_LBUTTONDOWN)
            {
                if (WndProc_RequestLButtonDownCancel)
                {
                    WndProc_RequestLButtonDownCancel = false;
                    handled = true;
                }
            }
            else if (msg == WM_LBUTTONUP)
            {
                if (WndProc_RequestLButtonUpCancel)
                {
                    WndProc_RequestLButtonUpCancel = false;
                    handled = true;
                }
            }
            if (handled)
            {
                DefWindowProc(m.HWnd, m.Msg, m.WParam, m.LParam);
                return;
            }
            base.WndProc(ref m);
        }
        #endregion

        #region DllImport
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterTouchWindow(IntPtr hWnd, TWF ulFlags);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnregisterTouchWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetTouchInputInfo(IntPtr hTouchInput, int cInputs, [In, Out] TOUCHINPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseTouchInputHandle(IntPtr hTouchInput);

        [DllImport("user32.dll")]
        static extern IntPtr DefWindowProc(IntPtr hWnd, int uMsg, IntPtr wParam, IntPtr lParam);
        #endregion

        #region IDisposable Support
        private bool disposedValue = false;

        private void Dispose(bool disposing)
        {
            if (!disposedValue && initialized)
            {
                if (disposing)
                {
                    RemoveHandleCreated(OnHandleCreated);
                    RemoveHandleDestroyed(OnHandleDestroyed);
                }

                UnregisterTouchWindow();
                ulFlags = 0;
                isTouchWindowRegistered = false;
                canRegisterTouchWindow = null;

                disposedValue = true;
            }
        }

        ~TouchWindowManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
#endregion
    }

    public class TouchWindowEventArgs : EventArgs
    {
        public bool Handled = false;
        public readonly TOUCHINPUT[] Inputs;
        public TouchWindowEventArgs(TOUCHINPUT[] inputs) { Inputs = inputs; }
    }
    public delegate void TouchWindowEventHandler(TouchWindowManager sender, TouchWindowEventArgs e);

    public struct TOUCHINPUT
    {
        public int x;
        public int y;
        public IntPtr hSource;
        public uint dwID;
        public TOUCHEVENTF dwFlags;
        public TOUCHINPUTMASKF dwMask;
        public uint dwTime;
        public UIntPtr dwExtraInfo;
        public uint cxContact;
        public uint cyContact;
    }

    [Flags]
    public enum TWF : UInt32
    {
        None = 0,
        FINETOUCH = 1 << 0,
        WANTPALM = 1 << 1
    }

    [Flags]
    public enum TOUCHEVENTF : UInt32
    {
        None = 0,
        MOVE = 1 << 0,
        DOWN = 1 << 1,
        UP = 1 << 2,
        INRANGE = 1 << 3,
        PRIMARY = 1 << 4,
        NOCOALESCE = 1 << 5,
        PALM = 1 << 7
    }

    [Flags]
    public enum TOUCHINPUTMASKF : UInt32
    {
        None = 0,
        TIMEFROMSYSTEM = 1 << 0,
        EXTRAINFO = 1 << 1,
        CONTACTAREA = 1 << 2
    }
}
#endif