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
    public enum MouseGestureDirection { Right, Up, Left, Down }
    public class MouseGestureAction
    {
        public readonly int Key;
        public readonly MouseGestureDirection[] Gesture;
        public readonly Action Action;

        public MouseGestureAction(int key, MouseGestureDirection[] gesture, Action action)
        {
            if (gesture == null) throw new ArgumentNullException("gesture");
            if (action == null) throw new ArgumentNullException("action");
            Key = key;
            Gesture = gesture;
            Action = action;
        }
    }
    public enum MouseGestureSender { Mouse, Program }
    public class MouseGestureCompletedEventArgs : MouseEventArgs
    {
        public readonly Point[] MouseOrbit;
        public readonly MouseGestureDirection[] UserDirections;
        public readonly int PerformedActionIndex;
        public readonly MouseGestureSender Sender;
        public MouseGestureCompletedEventArgs(MouseButtons button, int clicks, int x, int y, int delta,
            MouseGestureSender sender, IEnumerable<Point> mouseOrbit, IEnumerable<MouseGestureDirection> userDirections, int performedActionIndex)
            : base(button, clicks, x, y, delta)
        {
            Sender = sender;
            MouseOrbit = mouseOrbit.ToArray();
            UserDirections = userDirections.ToArray();
            PerformedActionIndex = performedActionIndex;
        }
    }
    public delegate void MouseGestureCompletedEventHandler(MouseGesture sender, MouseGestureCompletedEventArgs e);

    public class MouseGestureStartingEventArgs : MouseEventArgs
    {
        public bool Cancel = false;
        public readonly MouseGestureSender Sender;
        public MouseGestureStartingEventArgs(MouseButtons button, int clicks, int x, int y, int delta, MouseGestureSender sender)
            : base(button, clicks, x, y, delta)
        {
            Sender = sender;
        }
    }
    public delegate void MouseGestureStartingEventHandler(MouseGesture sender, MouseGestureStartingEventArgs e);

    public class MouseGesture : IDisposable
    {
        private Control owner;
        public Control Owner { get { return owner; } }
        private readonly List<Point> MouseOrbit = new List<Point>();
        private Graphics Graphics = null;
        private MouseGestureSender CurrentSender;

        private Pen Pen = null;
        public double? Width;
        public Color? Color;
        private const double DefaultWidth = 5;
        private static readonly Color DefaultColor = System.Drawing.Color.Red;
        public double GetWidth() { return Width != null ? (double)Width : DefaultWidth; }
        public Color GetColor() { return Color != null ? (Color)Color : DefaultColor; }

        public MouseGestureAction[] Actions;

        public event MouseGestureCompletedEventHandler MouseGestureCompleted;
        public event MouseGestureStartingEventHandler MouseGestureStarting;

        public MouseButtons MouseButtons = MouseButtons.Right;
        private bool enabled = true;
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                if (value != enabled)
                {
                    if (value)
                    {
                        enabled = true;
                    }
                    else
                    {
                        enabled = false;
                        Clear();
                    }
                    EnabledChanged?.Invoke(owner, new EventArgs());
                }
            }
        }

        public event EventHandler EnabledChanged;

        /// <summary>
        /// ジェスチャ中は真を返す
        /// </summary>
        public bool InGesturing => MouseOrbit.Any();

        /// <summary>
        /// ジェスチャが始まりある程度移動していれば真を返す
        /// これが偽のままジェスチャが終わればコマンドは実行されないことが保証される
        /// その逆は保証されない
        /// </summary>
        public bool InGesturing2
        {
            get
            {
                if (MouseOrbit.Count <= 1) return false;
                var length = 0;
                var o0 = MouseOrbit[0];
                var x0 = o0.X;
                var y0 = o0.Y;
                for (var i = 1; i < MouseOrbit.Count; i++)
                {
                    var o = MouseOrbit[i];
                    var x = o.X;
                    var y = o.Y;
                    length += Math.Max(Math.Abs(x - x0), Math.Abs(y - y0));
                    // Math.Abs(Math.Abs(x - x0) - Math.Abs(y - y0)) でも「偽ならコマンドは実行されない」は保証されるが
                    // 45°の方向に大きく移動しても偽を返すのはユーザーの直感に反する
                    if (length > DragDelta) return true;
                    x0 = x;
                    y0 = y;
                }
                return false;
            }
        }

        public int GesturingOrbitCount
        {
            get
            {
                return MouseOrbit.Count;
            }
        }

        public MouseGesture(Control owner, bool manualMode = false)
        {
            this.owner = owner;
            if (!manualMode)
            {
                this.owner.MouseDown += Owner_MouseDown;
                this.owner.MouseMove += Owner_MouseMove;
                this.owner.MouseUp += Owner_MouseUp;
            }
        }

        public Pen GetPen()
        {
            float w;
            if (Graphics != null)
            {
                w = (float)(GetWidth() * (Graphics.DpiX + Graphics.DpiY) / (2 * 96));
            }
            else
            {
                using (var g = owner.CreateGraphics())
                {
                    w = (float)(GetWidth() * (g.DpiX + g.DpiY) / (2 * 96));
                }
            }
            var c = w > 0 ? GetColor() : System.Drawing.Color.Transparent;
            var result = new Pen(c, w);
            result.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            result.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            return result;
        }

        private void Owner_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons) == e.Button)
            {
                GestureBegin(e, MouseGestureSender.Mouse);
            }
        }

        public void GestureBegin(Point clientPoint)
        {
            GestureBegin(clientPoint, MouseGestureSender.Program);
        }

        private void GestureBegin(Point clientPoint, MouseGestureSender sender)
        {
            GestureBegin(new MouseEventArgs(MouseButtons.None, 0, clientPoint.X, clientPoint.Y, 0), sender);
        }

        private void GestureBegin(MouseEventArgs e, MouseGestureSender sender)
        {
            if (enabled)
            {
                if (MouseGestureStarting != null)
                {
                    var e2 = new MouseGestureStartingEventArgs(e.Button, e.Clicks, e.X, e.Y, e.Delta, sender);
                    MouseGestureStarting(this, e2);
                    if (e2.Cancel) return;
                }

                Clear();
                CurrentSender = sender;
                Graphics = owner.CreateGraphics();
                Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                Pen = GetPen();
                MouseOrbit.Add(e.Location);
            }
        }

        private void Owner_MouseMove(object sender, MouseEventArgs e)
        {
            GestureContinue(e);
        }

        public void GestureContinue(Point clientPoint)
        {
            GestureContinue(new MouseEventArgs(MouseButtons.None, 0, clientPoint.X, clientPoint.Y, 0));
        }

        private void GestureContinue(MouseEventArgs e)
        {
            if (Graphics != null)
            {
                MouseOrbit.Add(e.Location);
                var count = MouseOrbit.Count;
                if (count >= 2)
                {
                    Graphics.DrawLine(Pen != null ? Pen : Pen, MouseOrbit[count - 2], e.Location);
                }
            }
        }

        private static readonly int DragDelta = SystemInformation.DragSize.Width + SystemInformation.DragSize.Height;
        private void Owner_MouseUp(object sender, MouseEventArgs e)
        {
            GestureEnd(e, inNativeThread: false);
        }

        public void GestureEnd(Point clientPoint, bool inNativeThread)
        {
            var e = new MouseEventArgs(MouseButtons.None, 0, clientPoint.X, clientPoint.Y, 0);
            GestureEnd(e, inNativeThread);
        }

        private void GestureEnd(MouseEventArgs e, bool inNativeThread)
        {
            if (MouseOrbit.Count > 0)
            {
                var count = MouseOrbit.Count;

                var strictDirectionList = new List<MouseGestureDirection>();
                var strictLengthList = new List<int>();
                for (var i = 1; i < count; i++)
                {
                    var p1 = MouseOrbit[i - 1];
                    var p2 = MouseOrbit[i];
                    var dx = p2.X - p1.X;
                    var dy = p2.Y - p1.Y;
                    var adx = dx > 0 ? dx : -dx;
                    var ady = dy > 0 ? dy : -dy;
                    MouseGestureDirection dir;
                    int length;
                    if (adx > ady)
                    {
                        dir = dx > 0 ? MouseGestureDirection.Right : MouseGestureDirection.Left;
                        length = adx - ady;
                    }
                    else if (ady > adx)
                    {
                        dir = dy > 0 ? MouseGestureDirection.Down : MouseGestureDirection.Up;
                        length = ady - adx;
                    }
                    else continue;
                    if (strictDirectionList.Count == 0 || strictDirectionList[strictDirectionList.Count - 1] != dir)
                    {
                        strictDirectionList.Add(dir);
                        strictLengthList.Add(length);
                    }
                    else
                    {
                        strictLengthList[strictLengthList.Count - 1] += length;
                    }
                }
                var maxLength = 0;

                for (var i = 0; i < strictLengthList.Count; i++)
                {
                    if (maxLength < strictLengthList[i]) maxLength = strictLengthList[i];
                }
                var userDirectionList = new List<MouseGestureDirection>();

                try
                {
                    for (var i = 0; i < strictLengthList.Count; i++)
                    {
                        var length = strictLengthList[i];
                        //if (length > DragDelta && length * 19 > maxLength * 7 &&
                        if (length > DragDelta && length * 10 > maxLength &&
                            (userDirectionList.Count == 0 || userDirectionList.Last() != strictDirectionList[i]))
                        {
                            userDirectionList.Add(strictDirectionList[i]);
                        }
                    }
                }
                catch (OverflowException)
                // 百万回程度ポインタを大きく動かすと長さがオーバーフローするので
                // ただし極論を言えば十億フレーム分ポインタを動かせばインデックスがオーバーフローする
                {
                    owner.Invalidate();
                    Clear();
                    return;
                }

                owner.Invalidate();

                var performedActionIndex = -1;
                if (Actions != null)
                {
                    for (var i = 0; i < Actions.Length; i++)
                    {
                        var action = Actions[i];
                        if (userDirectionList.SequenceEqual(action.Gesture))
                        {
                            performedActionIndex = i;
                            break;
                        }
                    }
                }

                var mgce = new MouseGestureCompletedEventArgs(e.Button, e.Clicks, e.X, e.Y, e.Delta,
                    CurrentSender, MouseOrbit, userDirectionList, performedActionIndex);
                Clear();


                if (inNativeThread)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            owner.Invoke((MethodInvoker)(() =>
                            {
                                if (performedActionIndex >= 0) Actions[performedActionIndex].Action();
                                MouseGestureCompleted?.Invoke(this, mgce);
                            }));
                        }
                        catch (ObjectDisposedException) { }
                    });
                }
                else
                {
                    if (performedActionIndex >= 0) Actions[performedActionIndex].Action();
                    MouseGestureCompleted?.Invoke(this, mgce);
                }

            }

            // Actions == null だった場合に上の if 文を実行しない実装だった場合に付け加えたもの
            // MouseOrbit.Count > 0 でなければ（実際には MouseOrbit.Count > 1 でなければ）
            // なにも描画されていないし、Graphics も Pen も null のまま
            /*
            else
            {
                Owner.Invalidate();
                Clear();
            }*/
        }

        public void Clear()
        {
            if (Graphics != null)
            {
                Graphics.Dispose();
                Graphics = null;
                Pen.Dispose();
                Pen = null;
            }
            MouseOrbit.Clear();
        }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
                    Clear();
                    owner.MouseDown -= Owner_MouseDown;
                    owner.MouseUp -= Owner_MouseUp;

                    //Canvas.Dispose();
                }

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~MouseGesture() {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
