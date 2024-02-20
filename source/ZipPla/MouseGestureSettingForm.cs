using Alteridem.WinTouch;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public partial class MouseGestureSettingForm : Form
    {
        private class DisplayableGesture : IComparable<DisplayableGesture>
        {
            public DisplayableGesture(MouseGestureDirection[] gesture)
            {
                Gesture = gesture;
                gestureString = ToString(gesture);
            }

            public readonly MouseGestureDirection[] Gesture;
            private readonly string gestureString;
            
            public override string ToString()
            {
                return gestureString;
            }

            private static string ToString(MouseGestureDirection[] gesture)
            {
                return string.Join("", (from d in gesture
                                        select
                                        d == MouseGestureDirection.Right ? "→" :
                                        d == MouseGestureDirection.Up ? "↑" :
                                        d == MouseGestureDirection.Left ? "←" :
                                        "↓").ToArray());
            }

            public int CompareTo(DisplayableGesture other)
            {
                var length = Gesture.Length;
                var oGesture = other.Gesture;
                var oLength = oGesture.Length;
                if (length > oLength) return 1;
                else if (length < oLength) return -1;
                else
                {
                    for(var i = 0; i < length; i++)
                    {
                        var d = Gesture[i];
                        var od = oGesture[i];
                        if (d == od) continue;
                        return (int)d - (int)od;
                    }
                    return 0;
                }
            }
        }

        /*
        private struct CommandInfo
        {
            public readonly int Key;
            public readonly int TemplateIndex;

            public CommandInfo(int key, int templateIndex)
            {
                Key = key;
                TemplateIndex = templateIndex;
            }
        }
        */

        //public string Gesture1IsAlreadyRegistered = "Gusture \"$1\" is already registered.";
        public string EnabledText { get { return cbEnabled.Text; } set{ cbEnabled.Text = value; } }
        public string GestureToAddCommandText { get { return gbMouseGestureRegion.Text; } set { gbMouseGestureRegion.Text = value; } }
        public string AppearanceText { get { return gbAppearance.Text; } set { gbAppearance.Text = value; } }
        public string WidthText { get { return btnWidth.Text; } set { btnWidth.Text = value; } }
        public string ColorText { get { return btnColor.Text; } set { btnColor.Text = value; } }
        public string GestureText { get { return tbcGesture.HeaderText; }
            set
            {
                tbcGesture.HeaderText = value;
                tbcGesture.MinimumWidth = TextRenderer.MeasureText(value + "   ", dataGridView.ColumnHeadersDefaultCellStyle.Font).Width;
                //Shown += (sender, e) => tbcGesture.MinimumWidth = TextRenderer.MeasureText(value, dataGridView.ColumnHeadersDefaultCellStyle.Font).Width + tbcGesture.Width - tbcGesture.HeaderCell.GetContentBounds(-1).Width;
            }
        }
        public string CommandText { get { return cbcCommand.HeaderText; } set { cbcCommand.HeaderText = value; } }
        public string DeleteText { get { return btnDelete.Text; } set { btnDelete.Text = deleteToolStripMenuItem.Text = value; } }
        public string OKText { get { return btnOK.Text; } set { btnOK.Text = value; } }
        public string CancelText { get { return btnCancel.Text; } set { btnCancel.Text = value; } }
        public string DoYouDiscardChangedSettingsText = "Are you sure that you want to discard changed settings?";
        public string QuestionText = "Question";

        private MouseGesture privateGesture, targetGesture;
        
        public bool Edited { get { return initialCommands == null; } }

        public double LineWidth
        {
            get { return privateGesture.GetWidth(); }
            set
            {
                if (privateGesture.Width != value)
                {
                    privateGesture.Width = value;
                    pnlAppearance.Invalidate();
                }
            }
        }
        public Color LineColor
        {
            get { return privateGesture.GetColor(); }
            set
            {
                if (privateGesture.Color != value)
                {
                    privateGesture.Color = value;
                    pnlAppearance.Invalidate();
                }
            }
        }
        public void SetLine(double lineWidth, Color lineColor)
        {
            var changed = false;
            if(privateGesture.Width != lineWidth)
            {
                privateGesture.Width = lineWidth;
                changed = true;
            }
            if(privateGesture.Color != lineColor)
            {
                privateGesture.Color = lineColor;
                changed = true;
            }
            if (changed) pnlAppearance.Invalidate();
        }

        private MouseGestureSettingTemplate[] templates;
        private Tuple<MouseGestureDirection[], int>[] initialCommands;
        private double initialWidth;
        private Color initialColor;
        private bool initialEnabled;


        private GestureListener gestureListener;
        private bool allowVStartTouchGesture;
        public MouseGestureSettingForm(MouseGesture targetGesture, MouseGestureSettingTemplate[] templates, bool allowTouchGesture, bool allowVStartTouchGesture)
        {
            if (targetGesture == null) throw new ArgumentNullException("targetGesture");
            if (templates == null) throw new ArgumentNullException("templates");
            if (templates.Length == 0) throw new ArgumentException("templates must have at least one element.");
            InitializeComponent();

            new MessageForwarder(dataGridView, ForwardedMessage.MouseWheel);

            ToolStripOverwriter.SquarizeToolStripInClass(this);

            Program.SetDoubleBuffered(dataGridView);

            tbcGesture.HeaderCell.Style.WrapMode = DataGridViewTriState.False;
            cbcCommand.HeaderCell.Style.WrapMode = DataGridViewTriState.False;

            this.targetGesture = targetGesture;
            privateGesture = new MouseGesture(pnlMouseGestureRegion);

            privateGesture.MouseGestureCompleted += privateGesture_MouseGestureCompleted;

            SetLine(targetGesture.GetWidth(), targetGesture.GetColor());
            foreach (ToolStripMenuItem item in cmsWidth.Items)
            {
                var w = double.Parse(item.Text);
                item.Text = null;
                item.Click += (sender, e) => LineWidth = w;
                item.Paint += (sender, e) =>
                {
                    if (!item.Selected) e.Graphics.FillRectangle(Brushes.White, e.ClipRectangle);
                    drawAppearanceLine(e, w, privateGesture.GetColor());
                };
            }

            this.templates = templates;
            cbcCommand.Items.AddRange((from t in templates select t.DisplayedText).ToArray());

            var keyToIndex = new int[(from t in templates select t.Key).Max() + 1];
            for (var i = 0; i < templates.Length; i++) keyToIndex[templates[i].Key] = i;

            var actions = targetGesture.Actions;
            if(actions != null && actions.Length > 0)
            {
                var rows = new List<DataGridViewRow>();
                foreach(var action in actions)
                {
                    var displayableGesture = new DisplayableGesture(action.Gesture);
                    int index;
                    if(getInsertIndex(out index, displayableGesture, rows))
                    {
                        rows.Insert(index, getRow(keyToIndex[action.Key], displayableGesture));
                    }
                }
                dataGridView.Rows.AddRange(rows.ToArray());

                // マウスジェスチャは表示順が完全に固定されているので他の表とは扱いを変えても不自然ではない
                // キーボードを中心に操作することがあまり想定されない事も踏まえると非選択状態での開始が妥当
                Shown += (sender, e) => { foreach (DataGridViewRow row in dataGridView.Rows) row.Selected = false; };
            }

            initialCommands = getCurrentCommands();
            initialColor = targetGesture.GetColor();
            initialWidth = targetGesture.GetWidth();
            initialEnabled = targetGesture.Enabled;

            cbEnabled.Checked = targetGesture.Enabled;

            if (allowTouchGesture)
            {
                this.allowVStartTouchGesture = allowVStartTouchGesture;
                try
                {
                    gestureListener = new GestureListener(this, new GestureConfig[] {
                //new GestureConfig(0, 1, 0),
                //new GestureConfig(3, 1, 0),
                    new GestureConfig(4, 2 | 4 | 16 , 8 ), // パン、向き拘束なし

            });
                    new DataGridViewScrollBarTouchFixer(dataGridView, gestureListener);
                    gestureListener.Pan += TouchListener_Pan;
                }
                catch { }
            }
            else
            {
                var listener = DataGridViewScrollBarTouchFixer.GetGestureListener(this);
                new DataGridViewScrollBarTouchFixer(dataGridView, listener);
            }

            Program.SetInitialHeightAndPackToOwnerFormScreen(this, dataGridView);
        }

        private bool TouchListener_Pan_GesturingOrInertia = false;
        private void TouchListener_Pan(object sender, PanEventArgs e)
        {
            if (e.Handled) return;
            var points = e.Info.location;
            var point = pnlMouseGestureRegion.PointToClient(new Point(points.x, points.y));
            if (e.Begin)
            {
                if (pnlMouseGestureRegion.ClientRectangle.Contains(point))
                {
                    privateGesture.GestureBegin(point);
                    TouchListener_Pan_GesturingOrInertia = true;
                    e.Handled = true;
                }
            }
            else if (TouchListener_Pan_GesturingOrInertia)
            {
                if (!e.End)
                {
                    if (!e.Inertia)
                    {
                        privateGesture.GestureContinue(point);
                    }
                    else
                    {
                        privateGesture.GestureEnd(point, inNativeThread: false); // ブロックの分離は privateGesture_MouseGestureCompleted 側で行っている
                    }
                }
                else
                {
                    privateGesture.GestureEnd(point, inNativeThread: false);
                    TouchListener_Pan_GesturingOrInertia = false;
                }
                e.Handled = true;
            }
        }

        private bool TouchListener_Pan_Warning = false;
        private void privateGesture_MouseGestureCompleted(MouseGesture sender, MouseGestureCompletedEventArgs e)
        {
            if (e.UserDirections.Length > 0)
            {
                var first = e.UserDirections[0];
                if (allowVStartTouchGesture || e.Sender == MouseGestureSender.Mouse ||
                    first == MouseGestureDirection.Left || first == MouseGestureDirection.Right)
                {
                    insertGesture(e.UserDirections);
                }
                else
                {
                    if (!TouchListener_Pan_Warning)
                    {
                        TouchListener_Pan_Warning = true;
                        Task.Run(() =>
                        {
                            try
                            {
                                Invoke((MethodInvoker)(() =>
                                {
                                    MessageBox.Show(this, Message.InThumbnailWindowTouchGestureMustStartWithHorizontalDirection,
                                        Message.Information, MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    TouchListener_Pan_Warning = false;
                                }));
                            }
                            catch (ObjectDisposedException) { }
                        });
                    }
                }
            }
        }

        private void insertGesture(MouseGestureDirection[] gesture)
        {
            var count = templates.Length;
            var names = (from DataGridViewRow row in dataGridView.Rows select row.Cells[cbcCommand.Index].Value.ToString()).ToArray();
            for (var i = 0; i < count; i++)
            {
                if(!names.Contains(templates[i].DisplayedText))
                {
                    insertGesture(gesture, i);
                    return;
                }
            }
            insertGesture(gesture, 0);
        }

        private void insertGesture(MouseGestureDirection[] gesture, int templateIndex)
        {
            if (gesture.Length > 4) return;
            var displayableGesture = new DisplayableGesture(gesture);
            int index;
            if(getInsertIndex(out index, displayableGesture, (from DataGridViewRow row in dataGridView.Rows select row).ToArray()))
            {
                var row = getRow(templateIndex, displayableGesture);
                dataGridView.Rows.Insert(index, row);
                foreach (DataGridViewRow row2 in dataGridView.Rows) row2.Selected = row2 == row;
            }
            else
            {
                var row = dataGridView.Rows[index];
                foreach (DataGridViewRow row2 in dataGridView.Rows) row2.Selected = row2 == row;
            }
        }
        
        private DataGridViewRow getRow(int templateIndex, DisplayableGesture dGesture)
        {
            var row = new DataGridViewRow();
            row.CreateCells(dataGridView);
            var cells = row.Cells;
            cells[tbcGesture.Index].Value = dGesture;
            cells[cbcCommand.Index].Value = templates[templateIndex].DisplayedText;
            return row;
        }

        private bool getInsertIndex(out int index, DisplayableGesture displayableGesture, IReadOnlyList<DataGridViewRow> rows)
        {
            var a = 0;
            var b = rows.Count;
            while (a < b)
            {
                var c = (a + b) / 2;
                var testRow = rows[c];
                var comp = displayableGesture.CompareTo(testRow.Cells[tbcGesture.Index].Value as DisplayableGesture);
                if (comp > 0)
                {
                    a = c + 1;
                }
                else if (comp < 0)
                {
                    b = c;
                }
                else
                {
                    index = c;
                    return false;

                }
            }
            index = a;
            return true;
        }

        private Tuple<MouseGestureDirection[], int>[] getCurrentCommands()
        {
            var templateStrings = (from t in templates select t.DisplayedText).ToArray();

            var templateIndexToKey = (from t in templates select t.Key).ToArray();
            
            return (from DataGridViewRow row in dataGridView.Rows
                    select Tuple.Create(
                        (row.Cells[tbcGesture.Index].Value as DisplayableGesture).Gesture,
                        // templates[Array.IndexOf(templateStrings, row.Cells[cbcCommand.Index].Value.ToString())].Key)).ToArray();
                        getTemplateIndex(row.Cells[cbcCommand.Index].Value.ToString())
                        )).ToArray();
        }
        private int getTemplateIndex(string displayedText)
        {
            for(var i = 0; i < templates.Length; i++)
            {
                if (templates[i].DisplayedText == displayedText) return i;
            }
            return -1;
        }

        private bool equalsCommands(Tuple<MouseGestureDirection[], int>[] a, Tuple<MouseGestureDirection[], int>[] b)
        {
            var count = a.Length;
            if (b.Length != count) return false;
            for(var i =0; i < count; i++)
            {
                var ai = a[i];
                var bi = b[i];
                if (ai.Item2 != bi.Item2 || !ai.Item1.SequenceEqual(bi.Item1)) return false;
            }
            return true;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            var currentState = getCurrentCommandsIfChangedSettings();
            if (currentState != null)
            {
                targetGesture.Enabled = cbEnabled.Checked;
                targetGesture.Width = privateGesture.Width;
                targetGesture.Color = privateGesture.Color;
                targetGesture.Actions = (from s in currentState select new MouseGestureAction(templates[s.Item2].Key, s.Item1, templates[s.Item2].Action)).ToArray();
                initialCommands = null;
            }
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnWidth_Click(object sender, EventArgs e)
        {
            cmsWidth.Show(Cursor.Position);
        }

        private void btnColor_Click(object sender, EventArgs e)
        {
            using (var cd = new ColorDialog())
            {
                cd.Color = privateGesture.GetColor();
                cd.AllowFullOpen = true;
                cd.SolidColorOnly = false;
                if (cd.ShowDialog(this) == DialogResult.OK)
                {
                    LineColor = cd.Color;
                }
            }
        }

        private void pnlAppearance_Paint(object sender, PaintEventArgs e)
        {
            drawAppearanceLine(e, privateGesture.GetWidth(), privateGesture.GetColor());
        }

        private void dataGridView_MouseDown(object sender, MouseEventArgs e)
        {
            var location = e.Location;
            var hit = dataGridView.HitTest(location.X, location.Y);
            if (hit.RowIndex < 0 || hit.ColumnIndex < 0)
            {
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    row.Selected = false;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
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
             btnDelete.Enabled = cbEnabled.Checked && dataGridView.SelectedRows.Count > 0;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView.SelectedRows)
            {
                dataGridView.Rows.Remove(row);
            }
        }

        private void cbEnabled_CheckedChanged(object sender, EventArgs e)
        {
            var ckd = cbEnabled.Checked;
            gbMouseGestureRegion.Enabled = ckd;
            gbAppearance.Enabled = ckd;
            dataGridView.Enabled = ckd;
            dataGridView_SelectionChanged(sender, e);
            pnlMouseGestureRegion.Visible = ckd;
            pnlAppearance.Visible = ckd;

            if(ckd)
            {
                tbcGesture.DefaultCellStyle.BackColor = Color.White;
            }
            else
            {
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    row.Selected = false;
                }
                tbcGesture.DefaultCellStyle.BackColor = SystemColors.Control;
            }
        }

        private void dataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            var c = e.ColumnIndex;
            var r = e.RowIndex;
            if (c >= 1 && r >= 0 && !dataGridView[c, r].IsInEditMode)
            {
                SendKeys.SendWait("{F4}");
            }
        }

        private void MouseGestureSettingForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(isChangedSettings())
            {
                if (MessageBox.Show(this, DoYouDiscardChangedSettingsText, QuestionText, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    e.Cancel = true;
                }
            }
        }

        private Tuple<MouseGestureDirection[], int>[] getCurrentCommandsIfChangedSettings()
        {
            Tuple<MouseGestureDirection[], int>[] currentCommands;
            if(isChangedSettings(out currentCommands))
            {
                if (currentCommands == null) currentCommands = getCurrentCommands();
                return currentCommands;
            }
            else
            {
                return null;
            }
        }

        private bool isChangedSettings() => isChangedSettings(out var dummy);
        
        private bool isChangedSettings(out Tuple<MouseGestureDirection[], int>[] currentCommands)
        {
            currentCommands = null;
            return initialCommands != null && (initialEnabled != cbEnabled.Checked || initialWidth != privateGesture.GetWidth() || initialColor != privateGesture.GetColor() ||
                !equalsCommands(initialCommands, currentCommands = getCurrentCommands()));
        }

        private void MouseGestureSettingForm_Shown(object sender, EventArgs e)
        {
            new BetterFormRestoreBounds(this);
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

        private void drawAppearanceLine(PaintEventArgs e, double width, Color color)
        {
            var rect = e.ClipRectangle;
            var w = rect.Width;
            var h = rect.Height;
            Point pt1, pt2;
            if (w > h)
            {
                var d = h / 2;
                var y = rect.Top + d;
                pt1 = new Point(rect.Left + d, y);
                pt2 = new Point(rect.Right - d, y);
            }
            else
            {
                var d = w / 2;
                var x = rect.Left + d;
                pt1 = new Point(x, rect.Top + d);
                pt2 = new Point(x, rect.Bottom - d);
            }
            var tempWidth = privateGesture.Width;
            var tempColor = privateGesture.Color;
            privateGesture.Width = width;
            privateGesture.Color = color;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var pen = privateGesture.GetPen())
            {
                e.Graphics.DrawLine(pen, pt1, pt2);
            }
            privateGesture.Width = tempWidth;
            privateGesture.Color = tempColor;
        }
    }

    public class MouseGestureSettingTemplate
    {
        public readonly int Key;
        public readonly string DisplayedText;
        public readonly Action Action;
        public MouseGestureSettingTemplate(int key, string displayedText, Action action)
        {
            Key = key;
            DisplayedText = displayedText;
            Action = action;
        }
        public override string ToString()
        {
            if (DisplayedText != null)
            {
                return DisplayedText;
            }
            else
            {
                return base.ToString();
            }
        }
    }
}
