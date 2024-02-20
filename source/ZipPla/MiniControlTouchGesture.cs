using Alteridem.WinTouch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace ZipPla
{
    public class MiniControlTouchGestureCompletedEventArgs : MouseGestureCompletedEventArgs
    {
        public readonly Control StartingControl;
        public MiniControlTouchGestureCompletedEventArgs(MouseGestureCompletedEventArgs e, Point[] clientOrbit, Control startingControl)
            : base(e.Button, e.Clicks, e.X, e.Y, e.Delta, e.Sender, clientOrbit, e.UserDirections, e.PerformedActionIndex)
        {
            StartingControl = startingControl;
        }
    }
    public delegate void MiniControlTouchGestureCompletedEventHandler(MiniControlTouchGesture sender, MiniControlTouchGestureCompletedEventArgs e);
    public class MiniControlTouchGestureStartingEventArgs : PanEventArgs
    {
        public bool Cancel = false;
        public readonly Control Control;
        public MiniControlTouchGestureStartingEventArgs(PanEventArgs e, Control target) : base(e.Info, e.Location)
        {
            Control = target;
        }
    }
    public delegate void MiniControlTouchGestureStartingEventHandler(MiniControlTouchGesture sender, MiniControlTouchGestureStartingEventArgs e);
    public class MiniControlTouchGesture
    {
        private GestureListener gestureListener;
        private MouseGesture mouseGesture;
        private Control container;
        private bool onlyHorizontalStart;
        public readonly HashSet<Control> Targets = new HashSet<Control>();

        public bool Enabled { get { return mouseGesture.Enabled; } set { mouseGesture.Enabled = value; } }

        public event MiniControlTouchGestureCompletedEventHandler TouchGestureCompleted;
        public event MiniControlTouchGestureStartingEventHandler TouchGestureStarting;

        public static GestureListener GetGestureListener(Control container)
        {
            return new GestureListener(container, new GestureConfig[] {
                    //new GestureConfig(3, 1, 0), // ズーム
                    new GestureConfig(4, 2 | 4 , 8 | 16 ), // パン、向き拘束と慣性なし
                });
        }

        public MiniControlTouchGesture(Control container) : this(container, GetGestureListener(container), onlyHorizontalStart: false) { }
        
        public MiniControlTouchGesture(Control container, GestureListener gestureListener, bool onlyHorizontalStart)
        {
            mouseGesture = new MouseGesture(container, manualMode: true);
            mouseGesture.Width = 0;
            mouseGesture.MouseGestureCompleted += mouseGesture_MouseGestureCompleted;
            this.gestureListener = gestureListener;
            this.container = container;
            gestureListener.Pan += gestureListener_Pan;
            this.onlyHorizontalStart = onlyHorizontalStart;
        }

        private void mouseGesture_MouseGestureCompleted(MouseGesture sender, MouseGestureCompletedEventArgs e)
        {
            var containerOrbit = e.MouseOrbit;
            if (containerOrbit.Length <= 0) return;
            var clientOrbit = (from p in containerOrbit select gestureListener_Pan_Control.PointToClient(container.PointToScreen(p))).ToArray();
            var e2 = new MiniControlTouchGestureCompletedEventArgs(e, clientOrbit, gestureListener_Pan_Control);
            Task.Run(() =>
            {
                try
                {
                    container.Invoke((MethodInvoker)(() =>
                    {
                        TouchGestureCompleted?.Invoke(this, e2);
                    }));
                }
                catch (ObjectDisposedException) { }
            });
        }

        public static ToolStripItem GetItem(ToolStrip toolStrip, Point clientPoint)
        {
            var items = toolStrip.Items;
            var count = items.Count;
            foreach (ToolStripItem item in items)
            {
                if (item.Bounds.Contains(clientPoint))
                {
                    return item;
                }
            }
            return null;
        }

        private Control gestureListener_Pan_Control = null;
        private void gestureListener_Pan(object sender, PanEventArgs e)
        {
            if (e.Begin)
            {
                gestureListener_Pan_Control = ActivateManager.FindPointedControl(Targets, e.Location); // 重なっていた場合に手前が取得されるように
                if (gestureListener_Pan_Control == null) // ConboBox など一部のコントロールは上の方法では取得できない
                {
                    gestureListener_Pan_Control = Targets.FirstOrDefault(t => t.Visible && t.ClientRectangle.Contains(t.PointToClient(e.Location)));
                }
                if (gestureListener_Pan_Control != null)
                {
                    if (TouchGestureStarting != null)
                    {
                        var e2 = new MiniControlTouchGestureStartingEventArgs(e, gestureListener_Pan_Control);
                        TouchGestureStarting(this, e2);
                        if (e2.Cancel)
                        {
                            gestureListener_Pan_Control = null;
                            return;
                        }
                    }

                    mouseGesture.GestureBegin(container.PointToClient(e.Location));
                    e.Handled = true;
                }
            }
            else if (gestureListener_Pan_Control != null)
            {
                var clientLocation = container.PointToClient(e.Location);
                if (!e.End && !e.Inertia)
                {
                    if (!onlyHorizontalStart || mouseGesture.GesturingOrbitCount > 1 || mouseGesture.InGesturing && Math.Abs(e.PanOffset.X) > Math.Abs(e.PanOffset.Y))
                    {
                        mouseGesture.GestureContinue(clientLocation);
                        e.Handled = true;
                    }
                    else
                    {
                        mouseGesture.Clear();
                        gestureListener_Pan_Control = null;
                    }
                }
                else
                {
                    if (mouseGesture.InGesturing)
                    {
                        mouseGesture.GestureEnd(clientLocation, inNativeThread: false); // true では慣性がないと gestureListener_Pan_Control = null が先に行われるのでイベントの分離は自前で。
                    }
                    if (e.End)
                    {
                        gestureListener_Pan_Control = null;
                    }
                    e.Handled = true;
                }
            }
        }
    }
}
