#if !AUTOBUILD
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Text.RegularExpressions;
using TouchLibrary.Core;
using TouchLibrary;
using System.Reflection;

/*
namespace TouchWindowLibrary
{
    public class TouchWindow : IDisposable
    {
        private TouchWindowCore touchWindowCore;
        
        public TouchWindow(Control target, TWF ulFlags = TWF.None)
        {
            touchWindowCore = new TouchWindowCore(target, ulFlags);
            touchWindowCore.Touch += touchWindowCore_Touch;
        }

        public bool IsTouchWindowRegistered { get { return touchWindowCore.IsTouchWindowRegistered; } }
        public bool? CanRegisterTouchWindow { get { return touchWindowCore.CanRegisterTouchWindow; } }

        public event TouchEventHandler Touch;
        
        private void touchWindowCore_Touch(TouchWindowCore sender, TouchCoreEventArgs e)
        {
            if (Touch == null) return;

            var inputs = e.Inputs.Select(input => (TouchInput)input).ToArray();

            if (Touch != null)
            {
                var touchEventArgs = new TouchEventArgs(inputs);
                Touch(this, touchEventArgs);
                if (touchEventArgs.Handled)
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        public void Dispose()
        {
            touchWindowCore.Touch -= touchWindowCore_Touch;
            touchWindowCore.Dispose();
        }
    }

    public class TouchEventArgs : EventArgs
    {
        public bool Handled = false;
        public readonly TouchInput[] Inputs;
        public TouchEventArgs(TouchInput[] inputs) { Inputs = inputs; }
    }
    public delegate void TouchEventHandler(TouchWindow sender, TouchEventArgs e);
}
*/

namespace ZipPla
{
#if LAB
    

    public static class ZipPlaLab
    {

        public static void Start()
        {
            Show(3 / 2, (-3) / 2, 3 / (-2), (-3) / (-2));
            return;
            var dropDown = new ToolStripDropDown();
            const int n = 100;
            for (var i = 0; i < n; i++) dropDown.Items.Add(new ToolStripMenuItem());

            var tm = new TimeMeasure();
            //dropDown.SuspendLayout();
            for (var i = 0; i < n; i++) dropDown.Items[i].Text = new string('a', n + 1);
            //dropDown.ResumeLayout(false);
            //dropDown.PerformLayout();
            tm.Dispose();

            //MessageForm.Show(null, "abc\ndef\nghf", "cap", "OK", MessageBoxIcon.Information);
        }

        static void CharCodeTest()
        {
            var abc = Encoding.ASCII.GetBytes("abc");
            var irohaUTF8 = Encoding.UTF8.GetBytes("いろは");
            var irohaSJIS = Encoding.GetEncoding("shift_jis").GetBytes("いろは");
            Show($"{ImageLoader.GetCode(abc)}\n{ImageLoader.GetCode(irohaUTF8)}\n{ImageLoader.GetCode(irohaSJIS)}");
        }

        static void TouchTest()
        {
            using (var form = new Form())
            using (var touchListener = new ControlTouchListener(form))
            using (var touchAnalyzer = new TouchAnalyzer(touchListener))
            {
                touchAnalyzer.Pan += (sender, e) =>
                {
                    //if (e.Condition != TouchGestureCondition.Begin )Show($"{e.Condition}");
                    if (e.Condition == TouchGestureCondition.Complete) Show("Complete");
                    e.Handled = true;
                };
                //touchListener.Touch += (sender, e) => Show("touch");
                form.ShowDialog();
            }
        }

        private static bool returnFalse()
        {
            return (null as object) is object; // これは false になる。 nullable なら is object と as object == null は全く同じと考えてよい。
        }

        static void GestureTest()
        {
            using (var form = new Form())
            {
                var gestureListener = new Alteridem.WinTouch.GestureListener(form);
                gestureListener.Pan += (sender, e) => Show("test");
                form.ShowDialog();
            }
        }

        static void ChildDisposedTest()
        {
            var form = new Form();
            var button = new Button();
            Show(form.IsDisposed, button.IsDisposed);
            form.Controls.Add(button);
            form.Dispose();
            Show(form.IsDisposed, button.IsDisposed); // 期待通り true, true になる
        }

        static void MessageFormTest()
        {
            //MessageForm.Show("testaaaaaaaaaaaaaaaaaaaaaaaa", "cap", "OK", "キャンセル", MessageBoxIcon.Information);
            MessageBox.Show("testaaaaaaaaaaaaaaaaaaaaaaaa", "cap", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
        }

    #region MesureStringTest
        // g.MeasureString に比べて TextRenderer.MeasureText が 10 倍ほど遅かったので
        // TextRenderer.MeasureText(g, ... なら高速かと考えて実験
        // 結果は否で、更に 4 倍ほど遅くなった
        // ただし 10 万で 5秒程度、100 だと 0.2 フレーム程度
        private static void MesureStringTest()
        {
            const string str = "TestString";
            const int count = 100000;
            var font = Control.DefaultFont;

            using (var tm = new TimeMeasure())
            {
                tm.Block("Graphics");
                using (var form = new Form())
                using (var g = form.CreateGraphics())
                {
                    for (var i = 0; i < count; i++)
                    {
                        g.MeasureString(str, font);
                    }
                }

                tm.Block("TextRenderer");
                for (var i = 0; i < count; i++)
                {
                    TextRenderer.MeasureText(str, font);
                }

                tm.Block("TextRenderer with Graphics");
                using (var form = new Form())
                using (var g = form.CreateGraphics())
                {
                    for (var i = 0; i < count; i++)
                    {
                        TextRenderer.MeasureText(g, str, font);
                    }
                }
            }
        }
    #endregion

    #region Show
        static void Show(Image image)
        {
            using (var form = new Form())
            using (var pictureBox = new PictureBox())
            {
                form.Text = "ZipPlaLab";
                form.ClientSize = image.Size;
                form.FormBorderStyle = FormBorderStyle.FixedSingle;
                form.MaximizeBox = false;
                form.Controls.Add(pictureBox);
                pictureBox.Dock = DockStyle.Fill;
                pictureBox.Image = image;
                pictureBox.SizeMode = PictureBoxSizeMode.Normal;
                form.ShowDialog();
            }
        }

        static void Show(params object[] messages)
        {
            MessageBox.Show(messages == null ? "null" :
                string.Join(", ", messages.Select(s => s ?? "null")), "ZipPlaLab");
        }
    #endregion
    }
#endif

    public class TimeMeasure : IDisposable
    {
        private Stopwatch stopwatch = null;
        private List<Tuple<string, TimeSpan>> log = null;
        private int currentBlock = -1;
        private bool showBeforeDispose;

        public TimeMeasure(bool showBeforeDispose = true)
        {
            stopwatch = new Stopwatch();
            this.showBeforeDispose = showBeforeDispose;
            stopwatch.Start();
        }

        public void Block(string name)
        {
            if (disposed) throw new ObjectDisposedException(nameof(TimeMeasure));
            stopwatch.Stop();

            if (log == null)
            {
                log = new List<Tuple<string, TimeSpan>>();
            }
            else
            {
                var current = log[currentBlock];
                log[currentBlock] = Tuple.Create(current.Item1, current.Item2 + stopwatch.Elapsed);
            }

            for (currentBlock = 0; currentBlock < log.Count; currentBlock++)
            {
                if (log[currentBlock].Item1 == name) break;
            }
            if (currentBlock == log.Count)
            {
                log.Add(Tuple.Create(name, TimeSpan.Zero));
            }
            stopwatch.Restart();
        }

        public void Show()
        {
            if (disposed) throw new ObjectDisposedException(nameof(TimeMeasure));
            stopwatch.Stop();

            if (log == null)
            {
                MessageBox.Show(stopwatch.Elapsed.ToString(), nameof(TimeMeasure));
            }
            else
            {
                var index = log.Count - 1;
                var current = log[currentBlock];
                log[currentBlock] = Tuple.Create(current.Item1, current.Item2 + stopwatch.Elapsed);

                var tt = log.Select(l => l.Item2.Ticks).Sum();
                var total = new TimeSpan(tt);

                MessageBox.Show(
                    $"Total: {total}\n" +
                    $"\n" +
                    string.Join("\n", log.Select(l => $"{l.Item1}:\n\t{l.Item2} ({100.0 * l.Item2.Ticks / tt:F2} %)")
                    ), nameof(TimeSpan));

                log = null;
                currentBlock = -1;
            }

            stopwatch.Restart();
        }

        private bool disposed = false;
        public void Dispose()
        {
            if (!disposed)
            {
                if (showBeforeDispose) Show();
                stopwatch.Stop();
                stopwatch = null;
                if (log != null)
                {
                    log.Clear();
                    log = null;
                }
            }
        }
    }
}
#endif