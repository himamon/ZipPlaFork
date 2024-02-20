using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public class BetterFormRestoreBounds
    {
        private Form form;

        public BetterFormRestoreBounds(Form form)
        {
            this.form = form;
            form.LocationChanged += Form_LocationChanged;
            form.SizeChanged += Form_SizeChanged;
            Form_SizeChanged(form, null);
            Form_LocationChanged_PreviousState = form.WindowState;
        }

        Rectangle Form_SizeChanged_BoundsLastNoMinimized;
        private void Form_SizeChanged(object sender, EventArgs e)
        {
            if (form.WindowState != FormWindowState.Minimized)
            {
                Form_SizeChanged_BoundsLastNoMinimized = form.Bounds;
            }
        }

        public Rectangle BoundsLastNoMinimized => Form_SizeChanged_BoundsLastNoMinimized;

        private FormWindowState Form_LocationChanged_PreviousState;
        private Rectangle? Form_LocationChanged_FixedRestoreBounds;
        private void Form_LocationChanged(object sender, EventArgs e)
        {
            var newWindowState = form.WindowState;
            if (Form_LocationChanged_PreviousState == FormWindowState.Maximized && newWindowState == FormWindowState.Normal)
            {
                var firstBounds = form.Bounds;
                var brokenRestoreBounds = form.RestoreBounds != firstBounds;
                if (brokenRestoreBounds)
                {
                    Form_LocationChanged_FixedRestoreBounds = firstBounds;
                }
                else
                {
                    Form_LocationChanged_FixedRestoreBounds = null;
                }
            }
            else if (newWindowState == FormWindowState.Normal && Form_LocationChanged_PreviousState == FormWindowState.Normal &&
                Form_LocationChanged_FixedRestoreBounds is Rectangle fixedRestoreBounds && fixedRestoreBounds != form.RestoreBounds)
            {
                Form_LocationChanged_FixedRestoreBounds = null;
                Task.Run(() =>
                {
                    try
                    {
                        form.Invoke(((MethodInvoker)(() =>
                        {
                            // 特殊な方法でウィンドウを移動すると fixedRestoreBounds が画面外になることがあるため Pack しておく
                            if (form.WindowState == FormWindowState.Normal)
                            {
                                var nearestScreen = Screen.FromRectangle(fixedRestoreBounds).WorkingArea;
                                form.Bounds = Pack(fixedRestoreBounds, nearestScreen);
                            }
                        })));
                    }
                    catch (ObjectDisposedException) { }
                });
            }
            else
            {
                Form_LocationChanged_FixedRestoreBounds = null;
            }
            Form_LocationChanged_PreviousState = newWindowState;
        }

        //public bool StopMemoryNormalStateBound = false;

        public Rectangle BetterRestoreBounds => GetMovedWindowBounds(form, form.RestoreBounds, Form_SizeChanged_BoundsLastNoMinimized); // GetNormalBounds(form)

        public static Rectangle GetMovedWindowBounds(Rectangle newWorkingArea, Rectangle oldWindowBounds, Rectangle oldWorkingArea)
        {
            //int roundDiv(long x, long y) => y == 0 ? default(int) : (y ^ x) >= 0 ? (int)((x + (y >> 1)) / y) : (int)((x - (y >> 1)) / y);
            int roundRescale(int newSize, int oldPosition, int oldSize)
            {
                if (oldSize == 0)
                {
                    if (oldPosition == 0)
                    {
                        oldSize = 2;
                        oldPosition = 1;
                    }
                    else
                    {
                        oldSize = 1;
                        if (oldPosition < 0)
                        {
                            oldPosition = 0;
                        }
                        else
                        {
                            oldPosition = 1;
                        }
                    }
                }
                var n = (long)newSize * oldPosition;
                var d = (long)oldSize;
                return (int)((n ^ d) >= 0 ? (n + (d >> 1)) / d : (n - (d >> 1)) / d);
            }

            return Pack(new Rectangle(
                newWorkingArea.X + roundRescale(oldWindowBounds.X - oldWorkingArea.X, newWorkingArea.Width - oldWindowBounds.Width, oldWorkingArea.Width - oldWindowBounds.Width),
                newWorkingArea.Y + roundRescale(oldWindowBounds.Y - oldWorkingArea.Y, newWorkingArea.Height - oldWindowBounds.Height, oldWorkingArea.Height - oldWindowBounds.Height),
                oldWindowBounds.Width,
                oldWindowBounds.Height),
                newWorkingArea);
        }

        /*
        public static Rectangle GetMovedWindowBounds(Rectangle newWorkingArea, Rectangle oldWindowBounds, Rectangle oldWorkingArea) =>
            ViewerForm.Pack(new Rectangle(
                newWorkingArea.X + (oldWindowBounds.X - oldWorkingArea.X),
                newWorkingArea.Y + (oldWindowBounds.Y - oldWorkingArea.Y),
                oldWindowBounds.Width,
                oldWindowBounds.Height),
                newWorkingArea);
                */

        public static Rectangle GetMovedWindowBounds(Rectangle newWorkingArea, Rectangle oldWindowBounds) =>
            GetMovedWindowBounds(newWorkingArea, oldWindowBounds, Screen.FromRectangle(oldWindowBounds).WorkingArea);

        public static Rectangle GetMovedWindowBounds(Form form, Point oldWIndowLocation, Size oldWindowSize, Rectangle formBoundsForMinimized) =>
            GetMovedWindowBounds(form, new Rectangle(oldWIndowLocation, oldWindowSize), formBoundsForMinimized);
        public static Rectangle GetMovedWindowBounds(Form form, Rectangle oldWindowBounds, Rectangle formBoundsForMinimized)
        {
            var windowState = form.WindowState;
            if (windowState == FormWindowState.Normal) return oldWindowBounds;
            var formScreenWorkingArea = windowState != FormWindowState.Minimized ? Screen.FromControl(form).WorkingArea : Screen.FromRectangle(formBoundsForMinimized).WorkingArea;
            if (formScreenWorkingArea.Contains(oldWindowBounds)) return oldWindowBounds;
            return GetMovedWindowBounds(formScreenWorkingArea, oldWindowBounds);
        }
        
        public static Rectangle Pack(Rectangle target, Rectangle bounds)
        {
            var hori = Pack(Interval.FromHorizontal(target), Interval.FromHorizontal(bounds));
            var vert = Pack(Interval.FromVertical(target), Interval.FromVertical(bounds));
            return new Rectangle(hori.Start, vert.Start, hori.Length, vert.Length);
        }

        static Interval Pack(Interval target, Interval bounds)
        {
            if (target.Length >= bounds.Length) return bounds;
            if (target.Start < bounds.Start) target.Start = bounds.Start;
            else if (target.Stop > bounds.Stop) target.Start = bounds.Stop - target.Length;
            return target;
        }

        /*
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public Point ptMinPosition;
            public Point ptMaxPosition;
            public Rectangle rcNormalPosition;
        }

        //このメソッドがあるフォームの通常状態時の位置とサイズを返す
        private static Rectangle GetNormalBounds(Form form)
        {
            WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
            wp.length = Marshal.SizeOf(wp);
            GetWindowPlacement(form.Handle, ref wp);
            //rcNormalPositionのWidthがRight、HeightがBottomとなっているので、修正
            return Rectangle.FromLTRB(
                wp.rcNormalPosition.X,
                wp.rcNormalPosition.Y,
                wp.rcNormalPosition.Width,
                wp.rcNormalPosition.Height);
        }
        */
    }

    public struct Interval
    {
        public int Start, Length;
        public static Interval FromHorizontal(Rectangle rect)
        {
            return new Interval { Start = rect.X, Length = rect.Width };
        }
        public static Interval FromVertical(Rectangle rect)
        {
            return new Interval { Start = rect.Y, Length = rect.Height };
        }
        public int Stop => Start + Length;
    }

    /*
    public class BetterFormRestoreBounds
    {
        private Form form;
        private Rectangle? betterRestoreBounds = null;

        public BetterFormRestoreBounds(Form form)
        {
            this.form = form;
            form.LocationChanged += Form_LocationChanged;
        }
        
        public Rectangle BetterRestoreBounds
        {
            get
            {
                var windowState = form.WindowState;
                return windowState != FormWindowState.Normal && betterRestoreBounds != null ? (Rectangle)betterRestoreBounds : form.RestoreBounds;
            }
        }

        private void Form_LocationChanged(object sender, EventArgs e)
        {
            var windowState = form.WindowState;
            if (windowState == FormWindowState.Maximized)
            {
                var restoreBounds = form.RestoreBounds;
                var workingArea = form.Bounds;
                if (workingArea.Contains(restoreBounds))
                {
                    //betterRestoreBounds = null;
                    betterRestoreBounds = restoreBounds; // フルスクリーンから復帰時にこのイベントハンドラのあと RestoreBounds が意味のないものに書き換わるので
                }
                else
                {
                    var restoreBoundsWorkingArea = Screen.FromRectangle(restoreBounds).Bounds;
                    restoreBounds.X += workingArea.X - restoreBoundsWorkingArea.X;
                    restoreBounds.Y += workingArea.Y - restoreBoundsWorkingArea.Y;
                    betterRestoreBounds =  ViewerForm.Pack(restoreBounds, workingArea);
                }
            }
            else if (betterRestoreBounds is Rectangle restoreBounds)
            {
                if (windowState == FormWindowState.Normal)
                {
                    var bounds = form.Bounds;
                    if (bounds == form.RestoreBounds)
                    {
                        betterRestoreBounds = null;
                        if (bounds != restoreBounds)
                        {
                            Task.Run(() =>
                            {
                                try
                                {
                                    form.Invoke(((MethodInvoker)(() =>
                                    {
                                        form.Bounds = restoreBounds;
                                    })));
                                }
                                catch (ObjectDisposedException) { }
                            });
                        }
                    }
                    else
                    {
                        betterRestoreBounds = bounds;
                    }
                }
                else
                {
                    betterRestoreBounds = null;
                }
            }
            else
            {
                betterRestoreBounds = null;
            }
        }
    }
    */
}
