using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public static class ExtendedKeys
    {
        // 0xFF000000 と 0x0000FF00 の部分が空いているので利用する
        // 自前の Keys 列挙型を通しているので変更しても Config に影響はない
        public const Keys WheelUp = (Keys)0x0100;
        public const Keys WheelDown = (Keys)0x0200;

        public const Keys OnRight = (Keys)0x01000000;
        public const Keys OnTopRight = (Keys)0x02000000;
        public const Keys OnTop = (Keys)0x03000000;
        public const Keys OnTopLeft = (Keys)0x04000000;
        public const Keys OnLeft = (Keys)0x05000000;
        public const Keys OnBottomLeft = (Keys)0x06000000;
        public const Keys OnBottom = (Keys)0x07000000;
        public const Keys OnBottomRight = (Keys)0x08000000;
        public const Keys OnCenter = (Keys)0x09000000;

        public const Keys Double = (Keys)0x11000000;
        
        public const Keys NextPageAtLastPage = unchecked((Keys)0x81000000);
        public const Keys PreviousPageAtFirstPage = unchecked((Keys)0x82000000);

        public const Keys LButtonOnRight = Keys.LButton | OnRight;
        public const Keys LButtonOnTopRight = Keys.LButton | OnTopRight;
        public const Keys LButtonOnTop = Keys.LButton | OnTop;
        public const Keys LButtonOnTopLeft = Keys.LButton | OnTopLeft;
        public const Keys LButtonOnLeft = Keys.LButton | OnLeft;
        public const Keys LButtonOnBottomLeft = Keys.LButton | OnBottomLeft;
        public const Keys LButtonOnBottom = Keys.LButton | OnBottom;
        public const Keys LButtonOnBottomRight = Keys.LButton | OnBottomRight;
        public const Keys LButtonOnCenter = Keys.LButton | OnCenter;

        public const Keys LButtonDouble = Keys.LButton | Double;

        public static bool IsLButton(Keys key)
        {
            return (key & (Keys)0x00ffffff) == Keys.LButton;
        }

        public static bool IsLButtonOnSpecifiedPosition(Keys key)
        {
            return key != Keys.LButton && key != LButtonDouble && IsLButton(key);
        }
    }

    public class DoubleClickChecker
    {
        public DoubleClickChecker()
        {
        }

        int lastClickedTime;
        MouseButtons lastClickedButton = MouseButtons.None;
        System.Drawing.Point lastClickedPoint;

        public bool Check(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.None)
            {
                lastClickedButton = MouseButtons.None;
                return false;
            }

            var now = Environment.TickCount;

            if (lastClickedButton == e.Button && lastClickedPoint is Point p)
            {
                var size = SystemInformation.DoubleClickSize;
                var w = size.Width;
                var h = size.Height;

                if (new Rectangle(p.X - w / 2, p.Y - h / 2, w | 1, h | 1).Contains(e.Location))
                {
                    var delta = now - lastClickedTime;
                    if (0 <= delta && delta <= SystemInformation.DoubleClickTime)
                    {
                        lastClickedButton = MouseButtons.None;
                        return true;
                    }
                }
            }
            lastClickedTime = now;
            lastClickedButton = e.Button;
            lastClickedPoint = e.Location;
            return false;
        }

        public bool Check(System.Drawing.Point p, Keys k)
        {
            switch (k)
            {
                case Keys.LButton: return Check(new MouseEventArgs(MouseButtons.Left, 1, p.X, p.Y, 0));
                case Keys.MButton: return Check(new MouseEventArgs(MouseButtons.Middle, 1, p.X, p.Y, 0));
                case Keys.RButton: return Check(new MouseEventArgs(MouseButtons.Right, 1, p.X, p.Y, 0));
                case Keys.XButton1: return Check(new MouseEventArgs(MouseButtons.XButton1, 1, p.X, p.Y, 0));
                case Keys.XButton2: return Check(new MouseEventArgs(MouseButtons.XButton2, 1, p.X, p.Y, 0));
            }
            lastClickedButton = MouseButtons.None;
            return false;
        }

    }

    public class KeyboardShortcutAction
    {
        public string Name { get; private set; }
        public readonly int Key;
        public readonly HashSet<Keys>[] Shortcut;
        public readonly Action Action;
        public readonly bool ContinuousExecution;

        public KeyboardShortcutAction(int actionKey, Action action, bool continuousExecution, IEnumerable<IEnumerable<Keys>> shortcut)
            : this(actionKey, action, continuousExecution, null, null,
                  (from s in shortcut select new HashSet<Keys>(s)).ToArray()) { }
        public KeyboardShortcutAction( int actionKey, Action action, bool continuousExecution, string shortcutName, HashSet<Keys>[] shortcut) : this( actionKey, action, continuousExecution, shortcutName, null, shortcut) { }
        public KeyboardShortcutAction(int actionKey, Action action, bool continuousExecution, params Keys[] shortcut) : this(actionKey, action, continuousExecution, null, shortcut, null) { }
        private KeyboardShortcutAction(int actionKey, Action action, bool continuousExecution, string shortcutName, IEnumerable<Keys> simpleShortcut, HashSet<Keys>[] fullShortcut)
        {
            if (simpleShortcut == null && fullShortcut == null) throw new ArgumentNullException("shortcut");
            if (action == null) throw new ArgumentNullException("action");
            if (fullShortcut == null)
            {
                fullShortcut = SimpleShortcutToFullShortcut(simpleShortcut);
            }

            if (fullShortcut.Length == 0 || fullShortcut.Any(s => s == null || s.Count == 0)) throw new ArgumentException("shortcut");
            Name = shortcutName != null ? shortcutName : GetNameOfShortcut(fullShortcut);
            Key = actionKey;
            Shortcut = fullShortcut;
            Action = action;
            ContinuousExecution = continuousExecution;
        }

        public static HashSet<Keys>[] SimpleShortcutToFullShortcut(IEnumerable<Keys> simpleShortcut)
        {
            var fullShortcutList = new List<HashSet<Keys>>();
            foreach (var k in simpleShortcut)
            {
                var keyState = fullShortcutList.Count == 0 ? new HashSet<Keys>() : new HashSet<Keys>(fullShortcutList.Last());
                if (keyState.Contains(k))
                {
                    keyState.Remove(k);
                    fullShortcutList.Add(new HashSet<Keys>(keyState));
                }
                keyState.Add(k);
                //fullShortcutList.Add(new HashSet<Keys>(keyState));
                fullShortcutList.Add(keyState);
            }
            return fullShortcutList.ToArray();
        }

        public static Dictionary<Keys, string> KeyNames;
        private static string KeyToString(Keys key)
        {
            if (KeyNames == null) return key.ToString();
            string name;
            if (KeyNames.TryGetValue(key, out name)) return name;
            else return key.ToString();
        }

        public static string GetNameOfShortcut(HashSet<Keys>[] shortcut)
        {
            string result = null;
            var prevState = new HashSet<Keys>();
            foreach (var state in shortcut)
            {
                var newKeys = string.Join("+", from k in state where !prevState.Contains(k) select KeyToString(k));
                result = result == null ? newKeys : result + "+" + newKeys;
                prevState = state;
            }
            return result;
        }

        public static string GetNameOfShortcut(IEnumerable<Keys> shortcut)
        {
            string result = null;
            foreach (var key in shortcut)
            {
                var newKeys = KeyToString(key);
                result = result == null ? newKeys : result + "+" + newKeys;
            }
            return result;
        }

        public void SetNameFromShortcut()
        {
            Name = GetNameOfShortcut(Shortcut);
        }

        public bool? IsMatch(IEnumerable<HashSet<Keys>> inputSequence)
        {
            if (inputSequence == null) throw new ArgumentNullException("inputSequence");
            
            var index = 0;
            var length = Shortcut.Length;
            foreach(var keyState in inputSequence)
            {
                if (index == Shortcut.Length)
                {
                    // キーの押上で確定するのは単一キーの場合のみ
                    //return keyState.Count == 0;

                    // 最初のキーの押上で確定
                    if (index > 0)
                    {
                        var prev = Shortcut[index - 1];
                        if (prev.Count > keyState.Count)
                        {
                            return keyState.All(key => prev.Contains(key));
                        }
                    }
                    return false;

                    // すべてのキーの押上で確定、も考えられるが
                    // Ctrl, Ctrl+A, Ctrl, Ctrl+B のようなコマンドを割り当てない限り不要
                }


                if (!keyState.SetEquals(Shortcut[index++])) return false;
            }
            return index < Shortcut.Length ? null : true as bool?;
        }
    }

    public class MouseAcceptedCancelEventArgs : EventArgs
    {
        public Point ScreenPoint;
        public bool Cancel;
        public MouseAcceptedCancelEventArgs(Point screenPoint)
        {
            ScreenPoint = screenPoint;
        }
    }
    public delegate void MouseAcceptedCancelEventHandler(KeyboardShortcut sender, MouseAcceptedCancelEventArgs e);

    public class KeyboardShortcutStartingEventArgs : EventArgs
    {
        public readonly Keys StartingKey;
        public readonly bool ByProgram;
        public Keys[] InitialState;
        public bool Cancel = false;
        public KeyboardShortcutStartingEventArgs(Keys startingKey, bool byProgram)
        {
            StartingKey = startingKey;
            ByProgram = byProgram;
        }
    }
    public delegate void KeyboardShortcutStartingEventHandler(KeyboardShortcut sender, KeyboardShortcutStartingEventArgs e);

    public class KeyboardShortcut : IDisposable
    {
        public bool UseLButton = false;
        public bool UseMButton = false;
        public bool UseRButton = false;
        public bool UseX1Button = false;
        public bool UseX2Button = false;

        public Control[] MouseAcceptControls;
        public Control[] LButtonAcceptControls;
        public Control[] RButtonAcceptControls;
        public Control[] WheelAcceptControls;

        public event MouseAcceptedCancelEventHandler WheelAccepted;
        public event KeyboardShortcutStartingEventHandler KeyboardShortcutStarting;

        public readonly Form Owner;
#if DEBUG
        public readonly List<HashSet<Keys>> KeySequence = new List<HashSet<Keys>>();
#else
        private readonly List<HashSet<Keys>> KeySequence = new List<HashSet<Keys>>();
#endif
        public bool InInput
        {
            get
            {
                checkKeyState();
                return KeySequence.Any();
            }
        }
       

        public bool Rollback = true;
        
        private readonly DoubleClickChecker doubleClickChecker = new DoubleClickChecker();

        //public bool ActionsContainsLButton { get; private set; }
        //public bool ActionsContainsMButton { get; private set; }
        //public bool ActionsContainsRButton { get; private set; }
        private KeyboardShortcutAction[] actions;
        public KeyboardShortcutAction[] Actions
        {
            get
            {
                return actions;
            }
            set
            {
                actions = value;
                //ActionsContainsLButton = actions.Any(action => action.Shortcut.Any(cond => cond.Contains(Keys.LButton)));
                //ActionsContainsMButton = actions.Any(action => action.Shortcut.Any(cond => cond.Contains(Keys.MButton)));
                //ActionsContainsRButton = actions.Any(action => action.Shortcut.Any(cond => cond.Contains(Keys.RButton)));
            }
        }

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
                        checkKeyState();
                    }
                    else
                    {
                        enabled = false;
                        //KeySequence.Clear();
                    }
                    EnabledChanged?.Invoke(Owner, new EventArgs());
                }
            }
        }

        public event EventHandler EnabledChanged;

        private bool originalKeyPreview;
        private Control[] initialChildren;

        public KeyboardShortcut(Form owner, params Control[] additionalControls)
        {
            Owner = owner;
            originalKeyPreview = owner.KeyPreview;
            owner.KeyPreview = true;

            // 矢印キーなどを見落とすのでこの方法は使わない
            //Owner.KeyDown += Owner_KeyDown;

            // PreviewKeyDown や MouseDown は一箇所でしか発生しない
            Owner.PreviewKeyDown += Control_PreviewKeyDown;
            Owner.MouseDown += Control_MouseDown;
            Owner.MouseWheel += Control_MouseWheel;
            Owner.MouseUp += Control_MouseUp;
            var controls = expand(null, Owner.Controls);
            initialChildren = new Control[controls.Count + additionalControls.Length];
            var i = 0; foreach (Control control in controls) initialChildren[i++] = control; foreach (var control in additionalControls) initialChildren[i++] = control;
            foreach (Control control in initialChildren)
            {
                control.PreviewKeyDown += Control_PreviewKeyDown;
                control.MouseDown += Control_MouseDown;
                control.MouseWheel += Control_MouseWheel;
                control.MouseUp += Control_MouseUp;
            }
            
            // 矢印キーなどでも KeyUp は見落とさない
            Owner.KeyUp += Owner_KeyUp;
            //Owner.Deactivate += Owner_Deactivate;
            
        }

        public static List<Control> expand(List<Control> pre, Control.ControlCollection cc)
        {
            if (pre == null) pre = new List<Control>();
            foreach (Control c in cc)
            {
                pre.Add(c);
                expand(pre, c.Controls);
            }
            return pre;
        }


        public System.Windows.Forms.Keys MouseButtonsToKeys(MouseButtons button)
        {
            switch(button)
            {
                case MouseButtons.Left: return UseLButton ? System.Windows.Forms.Keys.LButton : System.Windows.Forms.Keys.None;
                case MouseButtons.Middle: return UseMButton ? System.Windows.Forms.Keys.MButton : System.Windows.Forms.Keys.None;
                case MouseButtons.Right: return UseRButton ? System.Windows.Forms.Keys.RButton : System.Windows.Forms.Keys.None;
                case MouseButtons.XButton1: return UseX1Button ? System.Windows.Forms.Keys.XButton1 : System.Windows.Forms.Keys.None;
                case MouseButtons.XButton2: return UseX2Button ? System.Windows.Forms.Keys.XButton2 : System.Windows.Forms.Keys.None;
                default: return System.Windows.Forms.Keys.None;
            }
        }


        public MouseEventArgs IgnoreMouseEventOnce = null;
        public MouseButtons IgnoreAnyMouseEventOnce = MouseButtons.None;

        private void Control_MouseDown(object sender, MouseEventArgs e)
        {
            var ig = IgnoreMouseEventOnce;
            var ia = IgnoreAnyMouseEventOnce;
            IgnoreMouseEventOnce = null;
            IgnoreAnyMouseEventOnce = MouseButtons.None;
            if (ig == e || ia == e.Button)
            {
                return;
            }

            if (acceptForMouseButtons(sender, e))
            {
                Owner_KeyDownOrUp(new KeyEventArgs(MouseButtonsToKeys(e.Button)), e, isUp: false);
            }
            else
            {
                Clear();
            }
        }

        private void Control_MouseUp(object sender, MouseEventArgs e)
        {
            if (acceptForMouseButtons(sender, e))
            {
                Owner_KeyDownOrUp(new KeyEventArgs(MouseButtonsToKeys(e.Button)), e, isUp: true);
            }
            else
            {
                Clear();
            }
        }

        private void Control_MouseWheel(object sender, MouseEventArgs e)
        {
            if (acceptForWheel(sender, e))
            {
                if (e.Delta > 0)
                {
                    Owner_KeyDownOrUp(null, e, isUp: false, keyCode: ExtendedKeys.WheelUp);
                    Owner_KeyDownOrUp(null, null, isUp: true, keyCode: ExtendedKeys.WheelUp);
                }
                else if (e.Delta < 0)
                {
                    Owner_KeyDownOrUp(null, e, isUp: false, keyCode: ExtendedKeys.WheelDown);
                    Owner_KeyDownOrUp(null, null, isUp: true, keyCode: ExtendedKeys.WheelDown);
                }
            }
        }

        public void InvokeCommand(params Keys[] keys)
        {
            InvokeCommand(keys as IEnumerable<Keys>);
        }

        private bool InvokeCommand_Excuting = false; // 再帰呼び出し防止
        public void InvokeCommand(IEnumerable<Keys> keys)
        {
            if (enabled && !InvokeCommand_Excuting)
            {
                InvokeCommand_Excuting = true;
                var s = KeyboardShortcutAction.SimpleShortcutToFullShortcut(keys);
                Actions?.FirstOrDefault(a => a?.IsMatch(s) == true)?.Action?.Invoke();
                InvokeCommand_Excuting = false;
            }
        }

        /// <summary>
        /// LButtonAcceptControls などと無関係にクリックさせる。マウスジェスチャの中止時に呼び出すような使い方を想定。
        /// </summary>
        /// <param name="button"></param>
        public void InvokeMouseClick(MouseButtons button)
        {
            KeyEventArgs e;
            e = new KeyEventArgs(MouseButtonsToKeys(button));
            Owner_KeyDownOrUp(e, e, isUp: false, byInvoke: true);
            e = new KeyEventArgs(MouseButtonsToKeys(button));
            Owner_KeyDownOrUp(e, e, isUp: true, byInvoke: true);
        }

        private bool acceptForMouseButtons(object sender, MouseEventArgs e)
        {
            if (!(MouseAcceptControls == null || MouseAcceptControls.Contains(sender))) return false;
            switch (e.Button)
            {
                case MouseButtons.Left: return LButtonAcceptControls == null || LButtonAcceptControls.Contains(sender);
                case MouseButtons.Right: return RButtonAcceptControls == null || RButtonAcceptControls.Contains(sender);
                default: return true;
            }
        }

        private bool acceptForWheel(object sender, MouseEventArgs e)
        {
            if (!((WheelAcceptControls == null || WheelAcceptControls.Contains(sender)) &&
                MouseAcceptControls == null || MouseAcceptControls.Contains(sender))) return false;
            if (WheelAccepted != null)
            {
                var control = sender as Control;
                if (control != null)
                {
                    var e2 = new MouseAcceptedCancelEventArgs(control.PointToScreen(e.Location));
                    WheelAccepted(this, e2);
                    return !e2.Cancel;
                }
            }
            return true;
        }

        private static bool sendingEscapeKey = false;
        public static void SendEscapeKey()
        {
            sendingEscapeKey = true;
            SendKeys.Send("{ESC}");
            sendingEscapeKey = false;
        }

        private void Control_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            Owner_KeyDownOrUp(new KeyEventArgs(e.KeyData), e, isUp: false);
        }
        //public void PerformKeyDown(Keys keyData) { Owner_KeyDownOrUp(new KeyEventArgs(keyData), isUp: false); }
        //private void Owner_KeyDown(object sender, KeyEventArgs e) { Owner_KeyDownOrUp(e, isUp: false); }
        private void Owner_KeyUp(object sender, KeyEventArgs e)
        {
            Owner_KeyDownOrUp(e, e, isUp: true);
        }

        private EventArgs Owner_KeyDownOrUp_LastBaseEventArgs = null;
        private void Owner_KeyDownOrUp(KeyEventArgs e, EventArgs lastBaseEventArgs, bool isUp, Keys keyCode = Keys.None, bool byInvoke = false)
        {
            // PreviewKeyDown は親子のコントロールで発生することがあるため。
            // そうでないこともあるので親だけにイベントハンドラを登録すればいいわけではない。
            if (Owner_KeyDownOrUp_LastBaseEventArgs == lastBaseEventArgs)
            {
                return;
            }
            Owner_KeyDownOrUp_LastBaseEventArgs = lastBaseEventArgs;

            if (keyCode == Keys.None && e != null) keyCode = e.KeyCode;
            if (sendingEscapeKey && keyCode == Keys.Escape) return;

            if (enabled)
            {
                if (keyCode == Keys.None) return; // 半角／全角キーが KeyDown だけこれで発生するので無視
                
                if (!isUp)
                {
                    checkKeyState(keyCode);
                }
                
                var KeyStateCount = KeySequence.Count;

                if (!isUp && KeyStateCount == 0 && KeyboardShortcutStarting != null)
                {
                    var se = new KeyboardShortcutStartingEventArgs(keyCode, byInvoke);
                    KeyboardShortcutStarting(this, se);
                    if (se.Cancel) return;
                    if (se.InitialState is Keys[] initialState)
                    {
                        KeySequence.AddRange(KeyboardShortcutAction.SimpleShortcutToFullShortcut(initialState));
                        KeyStateCount = KeySequence.Count;
                    }
                }

                var keyState = KeyStateCount == 0 ? new HashSet<Keys>() : new HashSet<Keys>(KeySequence.Last());
                
                var contains = keyState.Contains(keyCode);
                
                if (isUp && contains)
                {
                    keyState.Remove(keyCode);

                    KeySequence.Add(keyState);
                    checkState(isUp, programKeyUp: false); // programKeyUp は isUp == true の場合は動作に影響しない

                    if (
                        //!doneAction && // 押し上げを含むショートカットキーを使わない場合不要。使う場合このイベント中の doAction だけをチェックする仕組みが必要
                        //（temp = doAction; doAction = false; checkState(isUp); doAction0 = doAction; doAction = temp; ... のように）
                        KeySequence.Count == KeyStateCount + 1)
                    {
                        if (Rollback && KeyStateCount >= 2 && KeySequence[KeyStateCount - 2].SetEquals(keyState))
                        {
                            KeySequence.RemoveAt(KeyStateCount);
                            KeySequence.RemoveAt(KeyStateCount - 1);


                            // 以下旧実装。現在は doneAction = false にする代わりに programKeyUp を使っている。
                            // 新しい方法では A Down → B Down → B Up → A Up で A が実行されることがない。
                            // 従来通り A Down → B Down → B Up → B Down なら A+B が２回実行される
                            // doneAction = false;
                            //【誤】上の行を有効にすると全てのコマンドが KeyDown, KeyUp とも continuousExecution のように実行される
                            //【正】上の行によってキーの押し上げ→押し下げで再度コマンドが実行できるようになる
                            //　　　continuousExecution は押し上げを経なくとも再度コマンドが実行可能なのでこれとは別
                        }
                    }

                    /*
                    else
                    {
                        KeySequence.Add(keyState);
                    }
                    */
                }
                else if (!isUp)
                {
                    // キーの押しつづけによる連続入力
                    if (contains)
                    {
                        if (isMouseKey(keyCode)) return;
                    }
                    else
                    {
                        keyState.Add(keyCode);
                        KeySequence.Add(keyState);
                    }
                    checkState(isUp, programKeyUp: contains); // KeySequence.Add(keyState) しない ＝ ユーザーはキーアップしていない
                }
            }
        }

        private static bool isMouseKey(Keys key)
        {
            return ExtendedKeys.IsLButton(key) || key == Keys.MButton || key == Keys.RButton || key == Keys.XButton1 || key == Keys.XButton2;
        }

        private KeyboardShortcutAction performingAction = null;
        private bool? pureKeyboardShortcutExcuting = null;

        public bool PureKeyboardShortcutExcuting()
        {
            if (pureKeyboardShortcutExcuting != null)
            {
                return (bool)pureKeyboardShortcutExcuting;
            }
            else
            {
                var result = performingAction?.Shortcut.All(s => s.All(k => !(k == Keys.LButton || k == Keys.MButton || k == Keys.RButton || k == Keys.XButton1 || k == Keys.XButton2))) == true;
                pureKeyboardShortcutExcuting = result;
                return result;
            }
        }

        bool keyboardShortcutExcutingWithKeyOrMouseDown = false;
        public bool KeyboardShortcutExcutingWithKeyOrMouseDown()
        {
            return keyboardShortcutExcutingWithKeyOrMouseDown;
        }


        private bool doneAction = false;
        private void checkState(bool isUp, bool programKeyUp)
        {
            if (Actions != null)
            {
                KeyboardShortcutAction action = null;
                var continuousExecution = false;
                var candsFound = false;
                var resolved = GetLButtonResolvedKeySequence();
                foreach (var a in Actions)
                {
                    switch (a.IsMatch(resolved))
                    {
                        case true:
                            if (candsFound) return;
                            candsFound = true;
                            action = a;
                            continuousExecution = a.ContinuousExecution;
                            break;
                        case null:
                            if (candsFound) return;
                            candsFound = true;
                            break;
                    }
                }
                if (action != null && (!doneAction || (continuousExecution && !isUp) || !isUp && !programKeyUp))
                {
                    performingAction = action;
                    pureKeyboardShortcutExcuting = null;
                    keyboardShortcutExcutingWithKeyOrMouseDown = !isUp;
                    action.Action();
                    keyboardShortcutExcutingWithKeyOrMouseDown = false;
                    performingAction = null;
                    pureKeyboardShortcutExcuting = null;
                    doneAction = true;
                }
            }

            if (KeySequence.Count > 0 && KeySequence.Last().Count == 0)
            {
                KeySequence.Clear();
            }

            if (KeySequence.Count == 0)
            {
                doneAction = false;
            }
        }

        private List<HashSet<Keys>> GetLButtonResolvedKeySequence()
        {
            if (LButtonAcceptControls == null || !LButtonAcceptControls.Any()) return KeySequence;
            if (Actions == null || Actions.Length == 0) return KeySequence;
            if (KeySequence == null || KeySequence.Count != 1) return KeySequence;
            var keys = KeySequence.First();
            if (keys == null || keys.Count != 1) return KeySequence;
            var key = keys.First();
            if (key != Keys.LButton) return KeySequence;
            
            var registeredLButtons = (from a in Actions let h = a?.Shortcut where h?.Length == 1 let c = h.First()
                                      where c?.Count == 1 let k = c.First() where ExtendedKeys.IsLButton(k) select k).ToArray();

            if (registeredLButtons.Length == 0) return KeySequence;
            var pos = Cursor.Position;
            if (registeredLButtons.Contains(ExtendedKeys.LButtonDouble) && doubleClickChecker.Check(pos, Keys.LButton))
            {
                return new List<HashSet<Keys>> { new HashSet<Keys> { ExtendedKeys.LButtonDouble } };
            }

            var control = LButtonAcceptControls.FirstOrDefault(c => c.RectangleToScreen(c.ClientRectangle).Contains(pos));
            if (control == null) return KeySequence;
            pos = control.PointToClient(pos);
            var x = pos.X / (double)control.Width;
            var y = pos.Y / (double)control.Height;
            Keys resultKey;

            if (x < 0.2 && y < 0.2 && registeredLButtons.Contains(resultKey = ExtendedKeys.LButtonOnTopLeft))
            {
                return new List<HashSet<Keys>> { new HashSet<Keys> { resultKey } };
            }

            if (x < 0.2 && y > 0.8 && registeredLButtons.Contains(resultKey = ExtendedKeys.LButtonOnBottomLeft))
            {
                return new List<HashSet<Keys>> { new HashSet<Keys> { resultKey } };
            }

            if (x > 0.8 && y < 0.2 && registeredLButtons.Contains(resultKey = ExtendedKeys.LButtonOnTopRight))
            {
                return new List<HashSet<Keys>> { new HashSet<Keys> { resultKey } };
            }

            if (x > 0.8 && y > 0.8 && registeredLButtons.Contains(resultKey = ExtendedKeys.LButtonOnBottomRight))
            {
                return new List<HashSet<Keys>> { new HashSet<Keys> { resultKey } };
            }

            var nearestSide = Keys.None;
            var distance = double.PositiveInfinity;
            bool leftExists, rightExists, topExists, bottomExists;

            if (leftExists = registeredLButtons.Contains(resultKey = ExtendedKeys.LButtonOnLeft))
            {
                var d = x;
                if (d < distance)
                {
                    distance = d;
                    nearestSide = resultKey;
                }
            }

            if (rightExists = registeredLButtons.Contains(resultKey = ExtendedKeys.LButtonOnRight))
            {
                var d = 1 - x;
                if (d < distance)
                {
                    distance = d;
                    nearestSide = resultKey;
                }
            }

            if (topExists = registeredLButtons.Contains(resultKey = ExtendedKeys.LButtonOnTop))
            {
                var d = y;
                if (d < distance)
                {
                    distance = d;
                    nearestSide = resultKey;
                }
            }

            if (bottomExists = registeredLButtons.Contains(resultKey = ExtendedKeys.LButtonOnBottom))
            {
                var d = 1 - y;
                if (d < distance)
                {
                    distance = d;
                    nearestSide = resultKey;
                }
            }

            var centerExists = registeredLButtons.Contains(ExtendedKeys.LButtonOnCenter);

            if (nearestSide != Keys.None && (distance < 0.4 || distance < 0.5 && !centerExists))
            {
                return new List<HashSet<Keys>> { new HashSet<Keys> { nearestSide } };
            }

            if (centerExists)
            {
                var hBorder = (topExists || bottomExists) && !leftExists && !rightExists ? 0 : 0.4;
                var vBorder = (leftExists || rightExists) && !topExists && !bottomExists ? 0 : 0.4;
                if (hBorder < x && x < 1 - hBorder && vBorder < y && y < 1 - vBorder)
                {
                    return new List<HashSet<Keys>> { new HashSet<Keys> { ExtendedKeys.LButtonOnCenter } };
                }
            }

            return KeySequence;
        }
        

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern short GetKeyState(int nVirtKey);

        public static bool GetKeyState(Keys key)
        {
            return GetKeyState((int)key) < 0;
        }

        private void checkKeyState(Keys newKey)
        {
            if (KeySequence.Any())
            {
                var last = KeySequence.Last();
                if (last.Contains(newKey) || last.Any(key => !GetKeyState(key)))
                {
                    Clear();
                }
            }
        }

        private void checkKeyState()
        {
            if (KeySequence.Any() && KeySequence.Last().Any(key => !GetKeyState(key)))
            {
                Clear();
            }
        }

        private void Clear()
        {
            KeySequence.Clear();
            doneAction = false;
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
                    Owner.KeyPreview = originalKeyPreview;
                    //Owner.KeyDown -= Owner_KeyDown;
                    Owner.MouseDown -= Control_MouseDown;
                    Owner.MouseWheel -= Control_MouseWheel;
                    Owner.MouseUp -= Control_MouseUp;
                    foreach (Control control in initialChildren)
                    {
                        control.PreviewKeyDown -= Control_PreviewKeyDown;
                        control.MouseDown -= Control_MouseDown;
                        control.MouseWheel -= Control_MouseWheel;
                        control.MouseUp -= Control_MouseUp;
                    }
                    Owner.KeyUp -= Owner_KeyUp;
                }

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~KeyboardShortcut() {
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
        

#if DEBUG
        public static string GetKeysString()
        {
            var result = new StringBuilder();
            var t = typeof(System.Windows.Forms.Keys);
            foreach (var name in Enum.GetNames(t)) // 別名を取得するため GetValues は使わない
            {
                result.AppendLine($"{name} = {(int)Enum.Parse(t, name)},");
            }
            return result.ToString();
        }
#endif

    }


}
