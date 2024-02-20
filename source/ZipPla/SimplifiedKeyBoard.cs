using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public class SimplifiedKeyBoard
    {
        StatusStrip owner;
        Form form;
        int offset;
        public readonly List<SimplifiedKey> Keys = new List<SimplifiedKey>();
        readonly List<ToolStripStatusLabel> separators = new List<ToolStripStatusLabel>();
        
        public SimplifiedKeyBoard(StatusStrip owner, int offset)
        {
            this.owner = owner;
            if (offset < 0)
            {
                var separator = GetSeparator(ToolStripStatusLabelBorderSides.Right);
                owner.Items.Insert(owner.Items.Count + offset + 1, separator);
                separators.Add(separator);
            }
            this.offset = offset;
            form = owner.FindForm();
        }

        public Keys GetModifierKeys()
        {
            return GetModifierKeys(upHeldKeys: true);
        }

        public Keys GetModifierKeysWithoutUpHeldKeys()
        {
            return GetModifierKeys(upHeldKeys: false);
        }

        public void UpHeldKey()
        {
            foreach (var key in Keys) (key as SimplifiedKeyToHold)?.UpHeldKey();
        }

        public Keys GetModifierKeys(bool upHeldKeys)
        {
            var result = System.Windows.Forms.Keys.None;
            foreach (var key in Keys)
            {
                if (key is SimplifiedKeyToHold keyToHold && keyToHold.GetPushed(upHeldKeys))
                {
                    switch (keyToHold.KeyCode)
                    {
                        case System.Windows.Forms.Keys.ControlKey: result |= System.Windows.Forms.Keys.Control; break;
                        case System.Windows.Forms.Keys.ShiftKey: result |= System.Windows.Forms.Keys.Shift; break;
                        case System.Windows.Forms.Keys.Menu: result |= System.Windows.Forms.Keys.Alt; break;
                    }
                }
            }
            return result;
        }

        public void SetVisible(bool visible)
        {
            foreach (var key in Keys)
            {
                key.Visible = visible;
            }
            foreach (var sep in separators) sep.Visible = visible;
        }

        private static ToolStripStatusLabel GetSeparator(ToolStripStatusLabelBorderSides side) => new ToolStripStatusLabel
        {
            BorderSides = side,
            BorderStyle = Border3DStyle.Etched
        };

        public void Add(SimplifiedKey key)
        {
            var items = owner.Items;
            var index = Keys.Any() ? items.IndexOf(Keys.Last()) + 1: offset >= 0 ? offset : items.Count + offset + 1;
            Keys.Add(key);
            items.Insert(index, key);
            form.KeyDown += key.KeyDown;
        }

        public void Add(string text, Keys keyCode)
        {
            var key = SimplifiedKey.GetSuitableSimplifiedKey(keyCode, form);
            key.Text = text;
            Add(key);
        }

        public void Add(string text, Action action) => Add(new SimplifiedKeyToPush(action, form) { Text = text });
    }

    public class SimplifiedKey : ToolStripButton
    {
        public readonly Keys KeyCode;
        private readonly int keyCode;
        private readonly Timer keyWatcher;

        private bool pushed;
        public virtual bool Pushed
        {
            get
            {
                if (pushed)
                {
                    return true;
                }
                else if (keyCode != 0 && GetKeyState(keyCode) < 0)
                {
                    Pushed = true;
                    keyWatcher.Start();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            protected set
            {
                if (pushed != value)
                {
                    pushed = value;
                    if (pushed)
                    {
                        BackColor = pushedBackColor;
                        ForeColor = pushedForeColor;
                    }
                    else
                    {
                        BackColor = releasedBackColor;
                        ForeColor = releasedForeColor;
                    }
                }
            }
        }

        private readonly Color releasedBackColor;
        private readonly Color releasedForeColor;
        private static readonly Color pushedBackColor = SystemColors.MenuHighlight;
        private static readonly Color pushedForeColor = SystemColors.HighlightText;
        
        public SimplifiedKey(Keys key)
        {
            if (key != Keys.None)
            {
                KeyCode = key;
                keyCode = (int)key;

                keyWatcher = new Timer { Interval = 10 };
                keyWatcher.Tick += KeyWatcher_Tick;
            }

            releasedBackColor = BackColor;
            releasedForeColor = ForeColor;
        }
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern short GetKeyState(int nVirtKey);

        public bool IsPhysicalPushed()
        {
            return GetKeyState(keyCode) < 0;
        }

        private void KeyWatcher_Tick(object sender, EventArgs e)
        {
            if (!IsPhysicalPushed())
            {
                keyWatcher.Stop();
                Pushed = false;
            }
        }

        public static bool IsKeyToHold(Keys key)
        {
            return key == Keys.ShiftKey || key == Keys.Menu || key == Keys.ControlKey;
        }

        public static SimplifiedKey GetSuitableSimplifiedKey(Keys key, Form form)
        {
            SimplifiedKey result;
            if (IsKeyToHold(key))
            {
                result = new SimplifiedKeyToHold(key);
            }
            else
            {
                result = new SimplifiedKeyToPush(key, form);
            }
            return result;
        }
        
        public void KeyDown(object sender, KeyEventArgs e)
        {
            if (keyWatcher != null)
            {
                if (e.KeyCode == KeyCode)
                {
                    Pushed = true;
                    keyWatcher.Start();
                }
            }
        }
    }

    public class SimplifiedKeyToPush : SimplifiedKey
    {
        private static readonly MethodInfo OnKeyDownMethodInfo = typeof(Control).GetMethod("OnKeyDown",
            BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[1] { typeof(KeyEventArgs) }, null);
        private static readonly MethodInfo OnKeyUpMethodInfo = typeof(Control).GetMethod("OnKeyUp",
            BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[1] { typeof(KeyEventArgs) }, null);

        readonly Form form;
        readonly Timer mouseWatcher;
        readonly Action action;

        public SimplifiedKeyToPush(Action action, Form form) : this(Keys.None, form)
        {
            this.action = action;
        }

        public SimplifiedKeyToPush(Keys key, Form form) : base(key)
        {
            this.form = form;
            mouseWatcher = new Timer { Interval = 10 };
            mouseWatcher.Tick += MouseWatcher_Tick;
        }
        
        private void MouseWatcher_Tick(object sender, EventArgs e)
        {
            //if ((Control.MouseButtons & MouseButtons.Left) != MouseButtons.Left) // この方法では MessageBox 表示中などに取得できない
            if (GetKeyState((int)Keys.LButton) >= 0)
            {
                mouseWatcher.Stop();
                Pushed = false;
                InvokeKeyUp();
            }
        }

        private void InvokeKeyDown()
        {
            if (action != null)
            {
                action();
            }
            else
            {
                OnKeyDownMethodInfo.Invoke(form.ActiveControl, new object[1] { new KeyEventArgs(KeyCode) });
            }
        }

        private void InvokeKeyUp()
        {
            if (action == null)
            {
                OnKeyUpMethodInfo.Invoke(form.ActiveControl, new object[1] { new KeyEventArgs(KeyCode) });
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            if (!Pushed)
            {
                Pushed = true;
                mouseWatcher.Start();
                InvokeKeyDown();
            }
        }

        private static Keys GetKeyCode(MouseButtons button)
        {
            switch(button)
            {
                case MouseButtons.Left: return Keys.LButton;
                default: return Keys.None;
            }
        }
    }

    public class SimplifiedKeyToHold : SimplifiedKey
    {
        private bool held;
        private bool Held
        {
            get
            {
                return held;
            }
            set
            {
                if (held != value)
                {
                    held = value;
                    if (held)
                    {
                        base.Pushed = true;
                        Font = new Font(Font, FontStyle.Underline);
                    }
                    else
                    {
                        Font = new Font(Font, FontStyle.Regular);
                    }
                }
            }
        }

        public override bool Pushed
        {
            get
            {
                return GetPushed(upHeldKey: true);
            }

            protected set
            {
                base.Pushed = value;
                if (!value) Held = false;
            }
        }

        public void UpHeldKey()
        {
            if (!Held && base.Pushed && !IsPhysicalPushed())
            {
                Pushed = false;
            }
        }

        public bool GetPushed(bool upHeldKey)
        {
            var result = base.Pushed;
            if (upHeldKey && !Held && result && !IsPhysicalPushed())
            {
                Pushed = false;
            }
            return result;
        }

        protected override void OnVisibleChanged(EventArgs e)
        {
            if (!Visible)
            {
                if (IsPhysicalPushed())
                {
                    Held = false;
                }
                else
                {
                    Pushed = false;
                }
            }
            base.OnVisibleChanged(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (Held)
            {
                Pushed = false;
            }
            else if (base.Pushed)
            {
                Held = true;
            }
            else
            {
                Pushed = true;
            }
            base.OnMouseDown(e);
        }

        public SimplifiedKeyToHold(Keys key) : base(key)
        {

        }
    }
}
