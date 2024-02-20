using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public enum ForwardedMessage { MouseWheel = 0x20A }

    public class MessageForwarder : NativeWindow, IMessageFilter, IDisposable
    {
        private Control _Control;
        private Control _PreviousParent;
        private HashSet<ForwardedMessage> _Messages;
        private bool _IsMouseOverControl;

        // ローカルにストップする実装
        public bool Stop = false;

        // グローバルにストップする実装
        //private static bool stop = false;
        //public bool Stop { get { return stop; } set { stop = value; } }
        //public static bool GlobalStop { get { return stop; } set { stop = value; } }

        public MessageForwarder(Control control, ForwardedMessage message)
            : this(control, new ForwardedMessage[] { message })
        {
        }
        public MessageForwarder(Control control, IEnumerable<ForwardedMessage> messages)
        {
            _Control = control;
            AssignHandle(control.Handle);
            _Messages = new HashSet<ForwardedMessage>(messages);
            _PreviousParent = control.Parent;
            _IsMouseOverControl = false;

            control.ParentChanged += control_ParentChanged;
            control.MouseEnter += control_MouseEnter;
            control.MouseLeave += control_MouseLeave;
            control.Leave += control_Leave;

            // 追記
            control.LostFocus += control_Leave; // Form がアクティブでなくなるときの問題はこれで解消される
            control.MouseMove += control_MouseMove; // MouseLeave では ClientRectangle 内で別のコントロールの上に来た場合の処理が不十分

            control.Disposed += control_Disposed;

            if (control.Parent != null)
            {
                Application.AddMessageFilter(this);
            }
        }

        
        private void control_MouseMove(object sender, MouseEventArgs e)
        {
            // 上に別のコントロールがあっても無視されるので不適切
            //_IsMouseOverControl = _Control.ClientRectangle.Contains(e.Location);

            _IsMouseOverControl = ActivateManager.InVisibleRegion(_Control, e.Location);
        }
        

        void control_ParentChanged(object sender, EventArgs e)
        {
            if (_Control.Parent == null)
            {
                Application.RemoveMessageFilter(this);
            }
            else
            {
                if (_PreviousParent == null)
                {
                    Application.AddMessageFilter(this);
                }
            }
            _PreviousParent = _Control.Parent;
        }

        void control_MouseEnter(object sender, EventArgs e)
        {
            _IsMouseOverControl = true;
        }

        void control_MouseLeave(object sender, EventArgs e)
        {
            _IsMouseOverControl = false;
        }

        void control_Leave(object sender, EventArgs e)
        {
            _IsMouseOverControl = false;
        }

        public bool PreFilterMessage(ref System.Windows.Forms.Message m)
        {
            if (_Messages.Contains((ForwardedMessage)m.Msg))
            {
                if (
                  _Control.CanFocus &&
                  _IsMouseOverControl)
                {
                    if (!_Control.Focused)
                    {
                        if (!Stop)
                        {
                            m.HWnd = _Control.Handle;
                            WndProc(ref m);
                        }
                        return true;
                    }
                }

                if (Stop && _Control.Handle == m.HWnd)
                {
                    if (
                      _Control.CanFocus &&
                      _IsMouseOverControl)
                    {
                    }
                    else
                    {
                        //m.HWnd = _Control.Handle;
                        //WndProc(ref m);
                        return true;
                    }
                }
            }

            return false;
        }
        
        public void Dispose()
        {
            if (_Control != null)
            {
                _Control.ParentChanged -= control_ParentChanged;
                _Control.MouseEnter -= control_MouseEnter;
                _Control.MouseLeave -= control_MouseLeave;
                _Control.Leave -= control_Leave;
                _Control.Disposed -= control_Disposed;
                if (_PreviousParent != null) Application.RemoveMessageFilter(this);
                _Control = null;
            }
        }

        private void control_Disposed(object sender, EventArgs e)
        {
            Dispose();
        }
    }
}
