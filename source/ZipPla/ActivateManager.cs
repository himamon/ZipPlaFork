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
    /// <summary>
    /// 対象の WndProc を WndProcOverrider に置き換え、コントロールが全て読み込まれた後で new を実行する
    /// </summary>
    public class ActivateManager
    {

        private bool activatorReady = false;
        MouseEventArgs mouseEventCanceler = null;
        public bool GetActivatorReady()
        {
            return activatorReady;
        }

        public void CancelAcrivatorReady(MouseEventArgs e)
        {
            activatorReady = false;
            mouseEventCanceler = e;
        }

        private Form owner;
        private static readonly System.Reflection.MethodInfo SetStyleMethodInfo = typeof(Control).GetMethod(
            "SetStyle",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null,
            new Type[2] {
                typeof(ControlStyles),
                typeof(bool)
            }, null);
        private static readonly System.Reflection.MethodInfo GetStyleMethodInfo = typeof(Control).GetMethod(
            "GetStyle",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null,
            new Type[1] {
                typeof(ControlStyles)
            }, null);

        public ActivateManager(Form owner, params Control[] controlsActivatedByMouseUp)
        {
            this.owner = owner;
            var controls = owner.Controls;
            if (controls != null)
            {
                Apply(controls, controlsActivatedByMouseUp);
            }
        }

        private void Apply(Control.ControlCollection controls, params Control[] controlsActivatedByMouseUp)
        {
            if (controls == null) return;
            foreach (Control control in controls)
            {
                if (control != null)
                {
                    if (controlsActivatedByMouseUp.Contains(control))
                    {
                        SetSelectable(control, false);
                        control.MouseDown += Control_MouseDown;
                        control.MouseUp += Control_MouseUpActivator;
                    }
                    else
                    {
                        if (GetStyleMethodInfo.Invoke(control, new object[1] { ControlStyles.Selectable }) as bool? == false)
                        {
                            control.MouseDown += Control_MouseDownActivator;
                        }
                        Apply(control.Controls, controlsActivatedByMouseUp);
                    }
                }
            }
        }

        public static void SetSelectable(Control control, bool value)
        {
            SetStyleMethodInfo.Invoke(control, new object[2] { ControlStyles.Selectable, value });
        }

        private void Control_MouseDown(object sender, MouseEventArgs e)
        {
            if (Form.ActiveForm != owner)
            {
                if (e != mouseEventCanceler)
                {
                    activatorReady = true;
                }
            }
            else if (sender is Control control && !control.Focused)
            {
                control.Focus();
            }
            mouseEventCanceler = null;
        }

        private void Control_MouseUpActivator(object sender, MouseEventArgs e)
        {
            if (activatorReady)
            {
                owner.Activate();
                if (sender is Control control && !control.Focused)
                {
                    control.Focus();
                }
            }
            activatorReady = false;
            mouseEventCanceler = null;
        }

        private void Control_MouseDownActivator(object sender, MouseEventArgs e)
        {
            owner.Activate();
        }

        public delegate void WndProcDelegate(ref System.Windows.Forms.Message m);
        public static void WndProcOverrider(WndProcDelegate baseWndProc, ref System.Windows.Forms.Message m)
        {
            const int WM_MOUSEACTIVATE = 0x0021;
            const int WA_NOACTIVATE = 3;
            switch (m.Msg)
            {
                case WM_MOUSEACTIVATE:
                    m.Result = (IntPtr)WA_NOACTIVATE;
                    return;
            }
            baseWndProc(ref m);
        }
        
        public static Control FindPointedControl(IEnumerable<Control> controls, Point screenLocation)
        {
            var pointedControlHandle = IntPtr.Zero;
            var handleZero = true;
            foreach (var control in controls)
            {
                if (handleZero)
                {
                    if (!control.Visible || !control.ClientRectangle.Contains(control.PointToClient(screenLocation))) continue;
                    pointedControlHandle = WindowFromPoint(screenLocation);
                }
                if (control.Visible && pointedControlHandle == control.Handle) return control;

            }
            return null;
        }

        public static bool InVisibleRegion(Control control, Point? clientPoint = null, Point? screenPoint = null, int checkParentsCount = 0)
        {
            if (!control.ClientRectangle.Contains(clientPoint ?? control.PointToClient((Point)screenPoint))) return false;
            try
            {
                var controlHandle = control.Handle;
                var handle = WindowFromPoint(screenPoint ?? control.PointToScreen((Point)clientPoint));
                for (var i = 0; i <= checkParentsCount; i++)
                {
                    if (controlHandle == handle) return true;
                    var child = handle;
                    handle = GetParent(handle);
                    if (handle == IntPtr.Zero || handle == child) return false;
                }
                return false;
            }
            catch
            {
                return true;
            }
        }
        
        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT Point);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;

            public static implicit operator POINT(Point point)
            {
                return new POINT { x = point.X, y = point.Y };
            }
        }
    }
}
