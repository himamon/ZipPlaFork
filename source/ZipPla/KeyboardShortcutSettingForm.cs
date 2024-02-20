using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormUtilities;

namespace ZipPla
{
    public partial class KeyboardShortcutSettingForm : Form
    {
        private class DisplayableShortcut : IEquatable<DisplayableShortcut>
        {
            public DisplayableShortcut(HashSet<Keys>[] shortcut, string shortcutName)
            {
                Shortcut = shortcut;
                shortcutString = shortcutName;
            }

            public readonly HashSet<Keys>[] Shortcut;
            private readonly string shortcutString;

            public override string ToString()
            {
                return shortcutString;
            }

            public bool Equals(DisplayableShortcut other)
            {
                throw new NotImplementedException();
            }
        }

        public bool UseLButton = false;
        public bool UseMButton = false;
        public bool UseRButton = false;
        public bool UseX1Button = false;
        public bool UseX2Button = false;

        public string UseLButtonWarningMessage;
        public string UseMButtonWarningMessage;
        public string UseRButtonWarningMessage;

        //private int LButtonCount = 0;

        public string ShortcutText
        {
            get { return tbcShortcut.HeaderText; }
            set
            {
                tbcShortcut.HeaderText = value;
                tbcShortcut.MinimumWidth = TextRenderer.MeasureText(value + "   ", dataGridView.ColumnHeadersDefaultCellStyle.Font).Width;
            }
        }
        public string CommandText { get { return cbcCommand.HeaderText; } set { cbcCommand.HeaderText = value; } }
        public string DefaultText { get { return btnDefault.Text; } set { btnDefault.Text = value; } }
        public string DeleteText { get { return btnDelete.Text; } set { btnDelete.Text = deleteToolStripMenuItem.Text = value; } }
        public string OKText { get { return btnOK.Text; } set { btnOK.Text = value; } }
        public string CancelText { get { return btnCancel.Text; } set { btnCancel.Text = value; } }
        public string DoYouDiscardChangedSettingsText = "Are you sure that you want to discard changed settings?";
        public string DoYouRestoreDefaultSettingOfKeyboardShortcutText = "Are you sure that you want to restore default setting of keyboard shortcut?";
        public string QuestionText = "Question";
        public string AddText = "Add";
        public string AbortText = "Abort";
        public string InformationText = "Information";
        public string UnassignedCommandsIsComplementedText = "Unassigned commands is complemented.";
        public string InputKeyboardShortcutText = "Input keyboard shortcut...";

        private KeyboardShortcut targetShortcut;

        public bool Edited { get { return initialCommands == null; } }

        private DataGridViewSorter dgvSorter;
        
        private MouseGestureSettingTemplate[] templates;
        private Tuple<HashSet<Keys>[], int>[] initialCommands, defaultCommands;
        private int[] continuousExecutionKeys;
        public KeyboardShortcutSettingForm(KeyboardShortcut targetShortcut, MouseGestureSettingTemplate[] templates, int[] continuousExecutionKeys, Tuple<HashSet<Keys>[], int>[] defaultCommandsShortcutKeyPair)
        {
            if (targetShortcut == null) throw new ArgumentNullException("targetShortcut");
            if (templates == null) throw new ArgumentNullException("templates");
            if (templates.Length == 0) throw new ArgumentException("templates must have at least one element.");
            InitializeComponent();

            new MessageForwarder(dataGridView, ForwardedMessage.MouseWheel);

            Shown += (sender, e ) => btnAdd.Text = AddText;

            ToolStripOverwriter.SquarizeToolStripInClass(this);

            Program.SetDoubleBuffered(dataGridView);

            tbcShortcut.HeaderCell.Style.WrapMode = DataGridViewTriState.False;
            cbcCommand.HeaderCell.Style.WrapMode = DataGridViewTriState.False;

            this.targetShortcut = targetShortcut;
            
            this.templates = templates;
            cbcCommand.Items.AddRange((from t in templates select t.DisplayedText).ToArray());

            this.continuousExecutionKeys = continuousExecutionKeys == null ? new int[0] : continuousExecutionKeys;


            var keyToIndex = new int[(from t in templates select t.Key).Max() + 1];
            for (var i = 0; i < templates.Length; i++) keyToIndex[templates[i].Key] = i;

            defaultCommands = defaultCommandsShortcutKeyPair == null ? new Tuple<HashSet<Keys>[], int>[0] :
                defaultCommandsShortcutKeyPair.Select(p => Tuple.Create(p.Item1, keyToIndex[p.Item2])).ToArray();

            var actions = targetShortcut.Actions;
            if (actions != null && actions.Length > 0)
            {
                var rows = new List<DataGridViewRow>();
                foreach (var action in actions)
                {
                    var displayableShortcut = new DisplayableShortcut(action.Shortcut, action.Name);
                    rows.Add(getRow(keyToIndex[action.Key], displayableShortcut));

                    /*
                    if (action.Shortcut.Any(s => s.Contains(Keys.LButton)))
                    {
                        LButtonCount++;
                    }
                    */
                }
                dataGridView.Rows.AddRange(rows.ToArray());
                Shown += (sender, e) => { foreach (DataGridViewRow row in dataGridView.Rows) row.Selected = false; };
            }

            initialCommands = getCurrentCommands();

            dgvSorter = new DataGridViewSorter(dataGridView);
            dgvSorter.RowsReleased += dataGridView_SelectionChanged;

            SetMessages();

            var listener = DataGridViewScrollBarTouchFixer.GetGestureListener(this);
            new DataGridViewScrollBarTouchFixer(dataGridView, listener);

            Program.SetInitialHeightAndPackToOwnerFormScreen(this, dataGridView);

            MinimumSize = new Size(Math.Max(Math.Max(Width, btnDefault.Right - Left + Right -btnAdd.Left +
                btnSpecials.Left - btnAdd.Right), MinimumSize.Width), MinimumSize.Height);
        }
        
        private void SetMessages()
        {
            btnSpecials.Text = Message.Specials;

            lButtonDoubleToolStripMenuItem.Text = Message.DoubleLeftClick;
            lButtonOnLeftSideToolStripMenuItem.Text = Message.LeftClickOnLeftSide;
            lButtonOnRightSideToolStripMenuItem.Text = Message.LeftClickOnRightSide;
            lButtonOnTopSideToolStripMenuItem.Text = Message.LeftClickOnTopSide;
            lButtonOnBottomSideToolStripMenuItem.Text = Message.LeftClickOnBottomSide;
            lButtonOnCenterToolStripMenuItem.Text = Message.LeftClickOnCenter;
            lButtonOnTopLeftToolStripMenuItem.Text = Message.LeftClickOnTopLeft;
            lButtonOnTopRightToolStripMenuItem.Text = Message.LeftClickOnTopRight;
            lButtonOnBottomLeftToolStripMenuItem.Text = Message.LeftClickOnBottomLeft;
            lButtonOnBottomRightToolStripMenuItem.Text = Message.LeftClickOnBottomRight;

            nextPageFailedToolStripMenuItem.Text = Message.NextPageAtLastPage;
            previousPageFailedToolStripMenuItem.Text = Message.PreviousPageAtFirstPage;
        }

        private readonly List<Keys> addedKeys = new List<Keys>();
        private bool addMode = false;

        // エンターでは反応しないように
        private void btnAdd_MouseClick(object sender, MouseEventArgs e)
        {
            if (addMode)
            {
                endAddMode();
            }
            else
            {
                Control_MouseUp_LButton_Accept = false;
                startAddMode();
            }
        }
        private void btnAdd_KeyDown(object sender, KeyEventArgs e)
        {
            if (!addMode && e.KeyCode == Keys.Enter)
            {
                startAddMode();
            }
        }

        private void startAddMode()
        {
            setEnabled(false);
            btnAdd.Text = AbortText;
            lbHint.Text = InputKeyboardShortcutText;
            addMode = true;
            //KeyDown += KeyboardShortcutSettingForm_KeyDown;
            // 矢印キーに反応するように
            // KeyPoreview は一箇所でしか発生しない
            // 仮に多重に発生しても連続入力は無視するので問題にはならない
            PreviewKeyDown += Control_PreviewKeyDown;
            MouseDown += Control_MouseDown;
            MouseUp += Control_MouseUp;
            MouseWheel += Control_MouseWheel;
            foreach (Control control in Controls)
            {
                control.PreviewKeyDown += Control_PreviewKeyDown;
                control.MouseDown += Control_MouseDown;
                control.MouseUp += Control_MouseUp;
                control.MouseWheel += Control_MouseWheel;
            }
            KeyUp += KeyboardShortcutSettingForm_KeyUp;
        }

        private Keys MouseButtonsToKeys(MouseButtons button)
        {
            switch (button)
            {
                case MouseButtons.Left: return UseLButton ? Keys.LButton : Keys.None;
                case MouseButtons.Middle: return UseMButton ? Keys.MButton : Keys.None;
                case MouseButtons.Right: return UseRButton ? Keys.RButton : Keys.None;
                case MouseButtons.XButton1: return UseX1Button ? Keys.XButton1 : Keys.None;
                case MouseButtons.XButton2: return UseX2Button ? Keys.XButton2 : Keys.None;
                default: return Keys.None;
            }
        }

        private bool Control_MouseUp_LButton_Accept = false;
        private void Control_MouseUp(object sender, MouseEventArgs e)
        {
            // KeyUp は基本的に Keys.None に対しても動作させるが、
            // Left の MouseUp にまで反応すると追加ボタンの押し上げに反応するため正常に動作しなくなる。
            var key = MouseButtonsToKeys(e.Button);
            if (Control_MouseUp_LButton_Accept || key != (UseLButton ? Keys.LButton : Keys.None))
            {
                Control_KeyUpTemplate();
                //KeyboardShortcutSettingForm_KeyUp(sender, new KeyEventArgs(key));
            }
            Control_MouseUp_LButton_Accept = true;
        }

        private void Control_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender == btnAdd) return;
            Control_KeyDownTemplate(MouseButtonsToKeys(e.Button));
        }

        private void Control_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                Control_KeyDownTemplate(ExtendedKeys.WheelUp);
                Control_KeyUpTemplate();
            }
            else if (e.Delta < 0)
            {
                Control_KeyDownTemplate(ExtendedKeys.WheelDown);
                Control_KeyUpTemplate();
            }
        }

        /*
        private void KeyboardShortcutSettingForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (addedKeys.Count == 0 || addedKeys.Last() != e.KeyCode)
            {
                addedKeys.Add(e.KeyCode);
                lbHint.Text = KeyboardShortcutAction.GetNameOfShortcut(addedKeys);
            }
            e.Handled = true;
        }
        */

        private void Control_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            Control_KeyDownTemplate(e.KeyCode);
        }

        private void Control_KeyDownTemplate(Keys keyCode)
        {
            if (keyCode == Keys.None || addedKeys.Contains(keyCode)) return;
            if (addedKeys.Count == 0 || addedKeys.Last() != keyCode)
            {
                addedKeys.Add(keyCode);
                lbHint.Text = KeyboardShortcutAction.GetNameOfShortcut(addedKeys);
            }
            if (addedKeys.Count >= 4)
            {
                KeyboardShortcutSettingForm_KeyUp(null, null);
                return;
            }
        }

        private void KeyboardShortcutSettingForm_KeyUp(object sender, KeyEventArgs e)
        {
            Control_KeyUpTemplate(e);
        }

        private void Control_KeyUpTemplate(KeyEventArgs e = null)
        {
            //var preLButtonCount = LButtonCount;
            insertShortcutWithAlert(addedKeys);

            if (e != null) e.Handled = true;
            endAddMode();

            /*
            if (preLButtonCount == 0 && LButtonCount > 0 && UseLButtonWarningMessage != null)
            {
                MessageBox.Show(this, UseLButtonWarningMessage, InformationText, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            */
        }
        
        private void endAddMode()
        {
            addedKeys.Clear();
            //KeyDown -= KeyboardShortcutSettingForm_KeyDown;
            PreviewKeyDown -= Control_PreviewKeyDown;
            MouseDown -= Control_MouseDown;
            MouseUp -= Control_MouseUp;
            MouseWheel -= Control_MouseWheel;
            foreach (Control control in Controls)
            {
                control.PreviewKeyDown -= Control_PreviewKeyDown;
                control.MouseDown -= Control_MouseDown;
                control.MouseUp -= Control_MouseUp;
                control.MouseWheel -= Control_MouseWheel;
            }
            KeyUp -= KeyboardShortcutSettingForm_KeyUp;
            addMode = false;
            setEnabled(true);
            lbHint.Text = null;
            btnAdd.Text = AddText;
        }

        private void insertShortcutWithAlert(params Keys[] keys)
        {
            insertShortcutWithAlert(keys as IReadOnlyList<Keys>);
        }

        private void insertShortcutWithAlert(IReadOnlyList<Keys> keys)
        {
            int keysCount;
            if (keys != null && (keysCount = keys.Count) > 0)
            {
                var count = templates.Length;
                var names = (from DataGridViewRow row in dataGridView.Rows select row.Cells[cbcCommand.Index].Value.ToString()).ToArray();
                Keys simpleKey;
                var needAlert = false;
                if (keysCount == 1 && UseLButtonWarningMessage != null && ExtendedKeys.IsLButton(simpleKey = keys[0]) && simpleKey != ExtendedKeys.LButtonDouble)
                {
                    var lButtons = (from c in getCurrentCommands() let a = c.Item1 where a?.Length == 1 let h = a[0]
                                    where h?.Count == 1 let k = h.First() where ExtendedKeys.IsLButton(k) && k != ExtendedKeys.Double select k ).ToArray();
                    if (simpleKey == Keys.LButton)
                    {
                        needAlert = !(!lButtons.Any() || lButtons.Contains(Keys.LButton));
                    }
                    else
                    {
                        needAlert = !(!lButtons.Contains(Keys.LButton) || lButtons.Any(k => k != Keys.LButton));
                    }
                }
                var added = false;
                for (var i = 0; i < count; i++)
                {
                    if (!names.Contains(templates[i].DisplayedText))
                    {
                        insertShortcut(keys, i);
                        added = true;
                        break;
                    }
                }
                if (!added) insertShortcut(keys, 0);
                if (needAlert) MessageBox.Show(MessageBoxOwner, UseLButtonWarningMessage, InformationText, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void insertShortcut(IReadOnlyList<Keys> keys, int templateIndex)
        {
            if (keys == null || keys.Count == 0) return;
            //var key = templates[templateIndex].Key;
            var displayableShortcut = new DisplayableShortcut(KeyboardShortcutAction.SimpleShortcutToFullShortcut(keys), KeyboardShortcutAction.GetNameOfShortcut(keys));
            int index;
            DataGridViewRow row;
            if (getInsertIndex(out index, displayableShortcut))
            {
                row = getRow(templateIndex, displayableShortcut);
                dataGridView.Rows.Insert(index, row);
                /*
                if (displayableShortcut.Shortcut.Any(s => s.Contains(Keys.LButton)))
                {
                    LButtonCount++;
                }
                */
            }
            else
            {
                row = dataGridView.Rows[index];
            }
            foreach (DataGridViewRow row2 in dataGridView.Rows) row2.Selected = row2 == row;
            dataGridView.CurrentCell = row.Cells[0];
        }

        private void setEnabled(bool enabled)
        {
            dataGridView.Enabled = enabled;
            btnDefault.Enabled = enabled;
            btnSpecials.Enabled = enabled;
            btnDelete.Enabled = enabled && dataGridView.SelectedRows.Count > 0;
            btnOK.Enabled = enabled;
            btnCancel.Enabled = enabled;
        }

        private DataGridViewRow getRow(Tuple<HashSet<Keys>[], int> t)
        {
            return getRow(t.Item2, new DisplayableShortcut(t.Item1, KeyboardShortcutAction.GetNameOfShortcut(t.Item1)));
        }
        private DataGridViewRow getRow(int templateIndex, DisplayableShortcut displayableShortcut)
        {
            var row = new DataGridViewRow();
            row.CreateCells(dataGridView);
            var cells = row.Cells;
            cells[tbcShortcut.Index].Value = displayableShortcut;
            cells[cbcCommand.Index].Value = templates[templateIndex].DisplayedText;
            return row;
        }

        private bool getInsertIndex(out int index, DisplayableShortcut displayableShortcut)
        {
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                if ((row.Cells[tbcShortcut.Index].Value as DisplayableShortcut).ToString() == displayableShortcut.ToString())
                {
                    index = row.Index;
                    return false;
                }
            }
            var currentRow = dataGridView.CurrentRow;
            index = currentRow == null ? 0 : currentRow.Index + 1;
            return true;
        }
        
        private Tuple<HashSet<Keys>[], int>[] getCurrentCommands()
        {
            //var templateStrings = (from t in templates select t.DisplayedText).ToArray();

            //var templateIndexToKey = (from t in templates select t.Key).ToArray();
            
            return (from DataGridViewRow row in dataGridView.Rows
                    select Tuple.Create(
                        (row.Cells[tbcShortcut.Index].Value as DisplayableShortcut).Shortcut,
                        getTemplateIndex(row.Cells[cbcCommand.Index].Value.ToString())
                        )).ToArray();
        }
        private int getTemplateIndex(string displayedText)
        {
            for (var i = 0; i < templates.Length; i++)
            {
                if (templates[i].DisplayedText == displayedText) return i;
            }
            return -1;
        }

        private static bool equalsCommands(Tuple<HashSet<Keys>[], int>[] a, Tuple<HashSet<Keys>[], int>[] b)
        {
            var count = a.Length;
            if (b.Length != count) return false;
            for (var i = 0; i < count; i++)
            {
                if (!equalsCommand(a[i], b[i]))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool equalsCommand(Tuple<HashSet<Keys>[], int> a, Tuple<HashSet<Keys>[], int> b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.Item2 != b.Item2) return false;
            return equalsShortcut(a.Item1, b.Item1);
        }

        private static bool equalsShortcut(HashSet<Keys>[] a, HashSet<Keys>[] b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            var length = a.Length;
            if (length != b.Length) return false;
            for (var j = 0; j < length; j++)
            {
                var aState = a[j];
                var bState = b[j];
                if (aState == null && bState == null) continue;
                if (aState == null || bState == null) return false;
                if (!aState.SetEquals(bState)) return false;
            }
            return true;
        }
        private static bool equalsShortcut(HashSet<Keys>[] a, Keys b)
        {
            if (a == null || a.Length != 1) return false;
            var a0 = a[0];
            return a0 != null && a0.Count == 1 && a0.First() == b;
        }
        
        public static bool competesShortcut(HashSet<Keys>[] a, HashSet<Keys>[] b)
        {
            if (equalsShortcut(a, b)) return true;

            if (equalsShortcut(a, Keys.LButton))
            {
                a = b;
            }
            else if (!equalsShortcut(b, Keys.LButton)) return false;
            if (a == null || a.Length != 1) return false;
            var a0 = a[0];
            return a0 != null && a0.Count == 1 && ExtendedKeys.IsLButton((System.Windows.Forms.Keys)a0.First());
        }

        /*
        public static bool intersectsShortcut(HashSet<Keys>[] a, HashSet<Keys>[] b)
        {
            return a != null && a.Any(ai => ai != null && ai.Any(ak => b != null && b.Any(bi => bi != null && bi.Any(bk => ak == bk || ExtendedKeys.IsLButton(ak) && ExtendedKeys.IsLButton(bk)))));
        }
        */
        public static bool WeaklyCompetesShortcut(HashSet<Keys>[] a, HashSet<Keys>[] b)
        {
            if (a == null || b == null) return false;
            var length = Math.Min(a.Length, b.Length);
            if (length == 0) return false;
            if (length == 1 && a[0].Count == 1 && b[0].Count == 1)
            {
                var ak = a[0].First();
                var bk = b[0].First();
                if (ExtendedKeys.IsLButton(ak) && ExtendedKeys.IsLButton(bk) &&
                    !(
                    ak == ExtendedKeys.LButtonOnLeft && bk == ExtendedKeys.LButtonOnRight ||
                    ak == ExtendedKeys.LButtonOnRight && bk == ExtendedKeys.LButtonOnLeft ||
                    ak == ExtendedKeys.LButtonOnTop && bk == ExtendedKeys.LButtonOnBottom ||
                    ak == ExtendedKeys.LButtonOnBottom && bk == ExtendedKeys.LButtonOnTop
                        ))
                {
                    return true;
                }
            }
            if (length < a.Length)
            {
                var t = new HashSet<Keys>[length];
                for (var i = 0; i < length; i++) t[i] = a[i];
                a = t;
            }
            else if (length < b.Length)
            {
                var t = new HashSet<Keys>[length];
                for (var i = 0; i < length; i++) t[i] = b[i];
                b = t;
            }
            return equalsShortcut(a, b);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            var currentState = getCurrentCommandsIfChangedSettings();
            if (currentState != null)
            {
                bool changed;
                var supCurrentState = getSupplementCommands(currentState, out changed);
                // var preActionsContainsLButton = targetShortcut.ActionsContainsLButton;
                //var preActionsContainsMButton = targetShortcut.ActionsContainsMButton;
                //var preActionsContainsRButton = targetShortcut.ActionsContainsRButton;
                targetShortcut.Actions = (from s in supCurrentState select new KeyboardShortcutAction(templates[s.Item2].Key, templates[s.Item2].Action, continuousExecutionKeys.Contains(templates[s.Item2].Key), s.Item1)).ToArray();
                if(changed
                    //|| UseLButtonWarningMessage != null && !preActionsContainsLButton && targetShortcut.ActionsContainsLButton
                    //|| UseMButtonWarningMessage != null && !preActionsContainsMButton && targetShortcut.ActionsContainsMButton 
                    //|| UseRButtonWarningMessage != null && !preActionsContainsRButton && targetShortcut.ActionsContainsRButton
                    )
                {
                    var message = "";

                    if (changed) message = UnassignedCommandsIsComplementedText;

                    // 入力直後に Warning を出すように変更

                    /*
                    // もとから LButton 等が使われていれば積極的にワーニングを出すことはしないが
                    // ワーニングを出すなら表示するので preActionsContainsLButton は参照しない

                    if (UseLButtonWarningMessage != null && targetShortcut.ActionsContainsLButton)
                    {
                        if (message != "") message += "\n\n";
                        message += UseLButtonWarningMessage;
                    }

                    if (UseMButtonWarningMessage != null && targetShortcut.ActionsContainsMButton)
                    {
                        if (message != "") message += "\n\n";
                        message += UseMButtonWarningMessage;
                    }

                    if (UseRButtonWarningMessage != null && targetShortcut.ActionsContainsRButton)
                    {
                        if (message != "") message += "\n\n";
                        message += UseRButtonWarningMessage;
                    }
                    */

                    MessageBox.Show(MessageBoxOwner, message, InformationText, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                initialCommands = null;
            }
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
        
        private void dataGridView_MouseDown(object sender, MouseEventArgs e)
        {
            // sorter を使う場合なにもないところのクリックで非選択にする処理は不要

            if (e.Button == MouseButtons.Right && !dgvSorter.InRowsHolded)
            {
                var location = e.Location;
                var hit = dataGridView.HitTest(location.X, location.Y);
                if (!dataGridView.Rows[hit.RowIndex].Cells[hit.ColumnIndex].Selected)
                {
                    foreach (DataGridViewRow row in dataGridView.Rows)
                    {
                        row.Selected = row.Index == hit.RowIndex;
                    }
                }
                cmsDelete.Show(Cursor.Position);
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnDelete.PerformClick();
        }

        private void dataGridView_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvSorter == null || !dgvSorter.InRowsHolded) btnDelete.Enabled = !addMode && dataGridView.SelectedRows.Count > 0;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView.SelectedRows)
            {
                /*
                var sc = row.Cells[tbcShortcut.Index].Value as DisplayableShortcut;
                if (sc != null && sc.Shortcut.Any(s => s.Contains(Keys.LButton)))
                {
                    LButtonCount--;
                }
                */
                dataGridView.Rows.Remove(row);
            }
        }

        private void dataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            var c = e.ColumnIndex;
            var r = e.RowIndex;
            if (c >= 1 && r >= 0 && !dataGridView[c, r].IsInEditMode)
            {
                //dataGridView.EndEdit();
                SendKeys.SendWait("{F4}");
            }
        }
        
        private void dataGridView_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dataGridView.EditingControl is DataGridViewComboBoxEditingControl comboBoxEditingControl)
            {
                comboBoxEditingControl.DropDownClosed -= ComboBoxEditingControl_DropDownClosed;
                comboBoxEditingControl.DropDownClosed += ComboBoxEditingControl_DropDownClosed;
            }
        }
        
        private void ComboBoxEditingControl_DropDownClosed(object sender, EventArgs e)
        {
            var currentCell = dataGridView.CurrentCell;
            Task.Run(() =>
            {
                try
                {
                    Invoke((MethodInvoker)(() =>
                    {
                        if (currentCell.IsInEditMode) dataGridView.EndEdit();
                    }));
                }
                catch { }
            });
        }

        private void KeyboardShortcutSettingForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            endAddMode();
            if (isChangedSettings())
            {
                if (MessageBox.Show(MessageBoxOwner, DoYouDiscardChangedSettingsText, QuestionText, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    e.Cancel = true;
                }
            }
            if (!e.Cancel)
            {
                // MessageBoxOwner の別解。他のプログラムのウィンドウがアクティブになっても対応可能
                Owner?.Activate();
            }
        }

        private Tuple<HashSet<Keys>[], int>[] getCurrentCommandsIfChangedSettings()
        {
            Tuple<HashSet<Keys>[], int>[] currentCommands;
            if (isChangedSettings(out currentCommands))
            {
                if (currentCommands == null) currentCommands = getCurrentCommands();
                return currentCommands;
            }
            else
            {
                return null;
            }
        }

        private bool isChangedSettings()
        {
            Tuple<HashSet<Keys>[], int>[] dummy;
            return isChangedSettings(out dummy);
        }

        private bool isChangedSettings(out Tuple<HashSet<Keys>[], int>[] currentCommands)
        {
            currentCommands = null;
            return initialCommands != null && !equalsCommands(initialCommands, currentCommands = getCurrentCommands());
        }

        public Tuple<HashSet<Keys>[], int>[] getSupplementCommands(Tuple<HashSet<Keys>[], int>[] baseSetting, out bool changed)
        {
            if (baseSetting == null) baseSetting = new Tuple<HashSet<Keys>[], int>[0];
            var sup = defaultCommands.Where(d => baseSetting.All(c => !(c.Item2 == d.Item2 && ViewerFormConfig.UseMouse(c.Item1) == ViewerFormConfig.UseMouse(d.Item1)) &&
           // !competesShortcut(c.Item1, d.Item1)));
           // !intersectsShortcut(c.Item1, d.Item1)));
            !WeaklyCompetesShortcut(c.Item1, d.Item1)));
            changed = sup.Any();
            if (changed)
            {
                return baseSetting.Concat(sup).ToArray();
            }
            else
            {
                return baseSetting;
            }
        }

        private void btnSpecials_Click(object sender, EventArgs e)
        {
            cmsAddSpecials.Show(Cursor.Position);
        }
        
        private void lButtonDoubleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            insertShortcutWithAlert(ExtendedKeys.LButtonDouble);
        }

        private void lButtonOnLeftSideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            insertShortcutWithAlert(ExtendedKeys.LButtonOnLeft);
        }

        private void lButtonOnRightSideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            insertShortcutWithAlert(ExtendedKeys.LButtonOnRight);
        }

        private void lButtonOnTopSideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            insertShortcutWithAlert(ExtendedKeys.LButtonOnTop);
        }

        private void lButtonOnBottomSideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            insertShortcutWithAlert(ExtendedKeys.LButtonOnBottom);
        }

        private void lButtonOnCenterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            insertShortcutWithAlert(ExtendedKeys.LButtonOnCenter);
        }

        private void lButtonOnTopLeftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            insertShortcutWithAlert(ExtendedKeys.LButtonOnTopLeft);
        }

        private void lButtonOnTopRightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            insertShortcutWithAlert(ExtendedKeys.LButtonOnTopRight);
        }

        private void lButtonOnButtomLeftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            insertShortcutWithAlert(ExtendedKeys.LButtonOnBottomLeft);
        }

        private void lButtonOnButtonRightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            insertShortcutWithAlert(ExtendedKeys.LButtonOnBottomRight);
        }

        private void nextPageFailedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            insertShortcutWithAlert(ExtendedKeys.NextPageAtLastPage);
        }

        private void previousPageFailedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            insertShortcutWithAlert(ExtendedKeys.PreviousPageAtFirstPage);
        }

        private void KeyboardShortcutSettingForm_Shown(object sender, EventArgs e)
        {
            new BetterFormRestoreBounds(this);
        }

        private void btnDefault_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(MessageBoxOwner, DoYouRestoreDefaultSettingOfKeyboardShortcutText, Message.Question, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            //if (MessageForm.Show(MessageBoxOwner, DoYouRestoreDefaultSettingOfKeyboardShortcutText, Message.Question, Message.OK, Message.Cancel, MessageBoxIcon.Question) == 0)
            {
                var rows = defaultCommands.Select(d => getRow(d)).ToArray();
                dataGridView.Rows.Clear();
                dataGridView.Rows.AddRange(rows);
            }
        }

        private void dataGridView_Scroll(object sender, ScrollEventArgs e)
        {
            // ドロップダウンが開いたままスクロールするのが DataGridViewComboBoxColumn の仕様だが
            // 誤った行の設定を変更してしまうリスクを考えると良い仕様とは言い難いので動作を変更する
            if (dataGridView.EditingControl is DataGridViewComboBoxEditingControl comboBoxEditingControl)
            {
                comboBoxEditingControl.DroppedDown = false;
            }
        }

        // this ではこれを呼び出した後このウィンドウを閉じると Owner が非アクティブになる
        //private Form MessageBoxOwner => Owner ?? this; // FormClosing で対応
        private Form MessageBoxOwner => this;
    }
}
