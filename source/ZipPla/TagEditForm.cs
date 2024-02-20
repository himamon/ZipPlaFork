using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormUtilities;

namespace ZipPla
{
    public partial class TagEditForm : Form
    {
        public bool Edited = false;

        ZipTag[] initialZipTagArray;
        DataGridViewSorter dgvSorter;

        Color EditingProfileColor;

        public TagEditForm(IReadOnlyList<string> newTags)
        {
            InitializeComponent();

            ToolStripOverwriter.SquarizeToolStripInClass(this);

            new MessageForwarder(dgvTagList, ForwardedMessage.MouseWheel);

            Program.SetDoubleBuffered(dgvTagList);

            MinimumSize = new Size(Width - btnAdd.Left + dgvTagList.Left, Height);

            dgvTagList_SelectionChanged(null, null);
            SetMessages();
            
            loadTags();

            
            pictureBox.Width = pictureBox.Height = btnOK.Height - 2;
            pictureBox.Left = dgvTagList.Left;
            pictureBox.Top = btnOK.Bottom - pictureBox.Height - 1;
            
            pictureBox.BackColor = EditingProfileColor;
            //SetProfileColorPixtureBox(this, pictureBox);

            var loadedCount = dgvTagList.Rows.Count;

            if (newTags != null && newTags.Count > 0)
            {
                var rows = new DataGridViewRow[newTags.Count];
                var i = 0;
                var currentTagsInLowerCase = (from DataGridViewRow row in dgvTagList.Rows
                                              select (row.Cells[TagNameColumn.Index].Value as string)?.ToLower()).ToList();
                foreach (var newTag in newTags)
                {
                    string newTagLowerCase;
                    if (ZipPlaInfo.CanBeTag(newTag) && !currentTagsInLowerCase.Contains(newTagLowerCase = newTag.ToLower()))
                    {
                        currentTagsInLowerCase.Add(newTagLowerCase);
                        var row = new DataGridViewRow();
                        row.CreateCells(dgvTagList);
                        var tag = new ZipTag(newTag);
                        var cell = row.Cells[TagNameColumn.Index];
                        cell.Value = tag.Name;
                        cell.Style.BackColor = tag.BackColor;
                        cell.Style.ForeColor = tag.ForeColor;
                        rows[i++] = row;
                    }
                }
                dgvTagList.Rows.AddRange(rows);
            }

            var fullCount = dgvTagList.Rows.Count;
            Shown += (sender, e) =>
            {
                for (var i = 0; i < fullCount; i++)
                {
                    dgvTagList.Rows[i].Selected = i >= loadedCount;
                }
            };

            var listener = DataGridViewScrollBarTouchFixer.GetGestureListener(this);
            new DataGridViewScrollBarTouchFixer(dgvTagList, listener);

            Program.SetInitialHeightAndPackToOwnerFormScreen(this, dgvTagList);

            dgvSorter = new DataGridViewSorter(dgvTagList);
            dgvSorter.RowsReleased += dgvTagList_SelectionChanged;

            btnOK.Select();
        }

        /*
        public static void SetProfileColorPixtureBox(Form form, PictureBox pictureBox)
        {
            var w = Program.DpiScalingY(6);
            pictureBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pictureBox.BorderStyle = BorderStyle.None;
            pictureBox.Bounds = new Rectangle(0, 0, form.Width, w);
        }
        */

        private void SetMessages()
        {
            Text = Message.EditTags;
            TagNameColumn.HeaderText = Message.TagName;
            btnDelete.Text = Message.Delete;
            btnAdd.Text = Message.Add;
            btnColor.Text = Message.Color;
            btnOK.Text = Message.OK;
            btnCancel.Text = Message.Cancel;
            colorToolStripMenuItem.Text = Message.Color;
            deleteToolStripMenuItem.Text = Message.Delete;

            toolTip.SetToolTip(pictureBox, Message.ProfileColorUnderEditing);
        }

        private void loadTags()
        {
            var zipTagConfig = new ZipTagConfig();
            var tags = zipTagConfig.Tags;
            EditingProfileColor = zipTagConfig.ProfileColor;
            if (tags != null && tags.Length > 0)
            {
                var rows = new DataGridViewRow[tags.Length];
                for (var i = 0; i < tags.Length; i++)
                {
                    var row = new DataGridViewRow();
                    row.CreateCells(dgvTagList);
                    var tag = tags[i];
                    var cell = row.Cells[TagNameColumn.Index];
                    cell.Value = tag.Name;
                    cell.Style.BackColor = tag.BackColor;
                    cell.Style.ForeColor = tag.ForeColor;
                    rows[i] = row;
                }
                dgvTagList.Rows.AddRange(rows);
            }
            initialZipTagArray = tags != null ? tags : new ZipTag[0];
        }

        private Exception saveTags()
        {
            var zipTagConfig = new ZipTagConfig();
            zipTagConfig.ProfileColor = EditingProfileColor;
            var tags = (from DataGridViewRow row in dgvTagList.Rows select new ZipTag(row.Cells[TagNameColumn.Index])).ToArray();
            zipTagConfig.Tags = tags;
            var exception =  zipTagConfig.Save();
            var bookmarkConfig = new ColoredBookmarkConfig();
            var colors = bookmarkConfig.ProfileColors;
            var profiles = bookmarkConfig.Profiles;
            if(colors != null && profiles != null && colors.Length == profiles.Length)
            {
                for(var i = 0; i < colors.Length; i++)
                {
                    if (colors[i] == EditingProfileColor) profiles[i].Tags = tags;
                }
            }
            var exception2 = bookmarkConfig.Save();
            return exception != null ? exception : exception2;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewColumn column in dgvTagList.Columns)
            {
                column.HeaderCell.SortGlyphDirection = SortOrder.None;
            }

            var rows = dgvTagList.Rows;
            var currentRow = dgvTagList.CurrentRow;
            var currentIndex = currentRow != null ? currentRow.Index : rows.Count - 1;
            var row = new DataGridViewRow();
            row.CreateCells(dgvTagList);
            var cell = row.Cells[TagNameColumn.Index];
            int numberZeroOrigin;
            var zipTag = new ZipTag(getNewTagName(out numberZeroOrigin));
            zipTag.BackColor = defaultColors[numberZeroOrigin % defaultColors.Length];
            cell.Value = zipTag.Name;
            cell.Style.BackColor = zipTag.BackColor;
            cell.Style.ForeColor = zipTag.ForeColor;
            rows.Insert(++currentIndex, row);
            dgvTagList.CurrentCell = dgvTagList.Rows[currentIndex].Cells[0];
            dgvTagList.BeginEdit(true);
        }
        
        bool dgvTagList_CellValidating_canceled;
        private void dgvTagList_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            DataGridView dgv = (DataGridView)sender;

            //新しい行のセルでなく、セルの内容が変更されている時だけ検証する
            if (e.RowIndex == dgv.NewRowIndex || !dgv.IsCurrentCellDirty)
            {
                return;
            }

            var newName = $"{e.FormattedValue}";
            var trimedNewName = newName.Trim();

            if(!string.IsNullOrEmpty(trimedNewName) && !checkTagFormat(trimedNewName, e.RowIndex))
            {
                dgvTagList_CellValidating_canceled = true;
                dgv.CancelEdit();
                //e.Cancel = true;
            }

        }

        private bool checkTagFormat(string tag, int rowIndex)
        {
            if (string.IsNullOrEmpty(tag) || !ZipPlaInfo.CanBeTag(tag))
            {
                dgvSorter.DoMouseUp();
                MessageBox.Show(this, Message.TagMustBeNoEmptyStringWhichDoesNotIncludeFollowingCharacters +
                    "\n\n\\ / : * ? \" < > | { } ; ,", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            else if ((from DataGridViewRow row in dgvTagList.Rows where row.Index != rowIndex
                      select (row.Cells[TagNameColumn.Index].Value as string)?.ToLower()).Contains(tag?.ToLower()))
            //else if ((dgvTagList.Rows as IEnumerable<DataGridViewRow>).Any(row => row.Cells[TagNameColumn.Index].Value as string == tag))
            {
                dgvSorter.DoMouseUp();
                MessageBox.Show(this, Message.TagsCanNotShareSameName, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            else
            {
                return true;
            }
        }

        private void dgvTagList_MouseDown(object sender, MouseEventArgs e)
        {
            var location = e.Location;
            var hit = dgvTagList.HitTest(location.X, location.Y);
            if (hit.RowIndex < 0 || hit.ColumnIndex < 0)
            {
                Validate();
                foreach (DataGridViewRow row in dgvTagList.Rows)
                {
                    row.Selected = false;
                }
            }
            else if(e.Button == MouseButtons.Right && !dgvSorter.InRowsHolded && !dgvTagList.IsCurrentCellInEditMode)
            {
                dgvTagList_CellValidating_canceled = false;
                Validate();
                if (!dgvTagList_CellValidating_canceled)
                {
                    if (!dgvTagList.Rows[hit.RowIndex].Cells[hit.ColumnIndex].Selected)
                    {
                        foreach (DataGridViewRow row in dgvTagList.Rows)
                        {
                            row.Selected = row.Index == hit.RowIndex;
                        }
                    }
                    contextMenuStrip.Show(Cursor.Position);
                }
            }
        }

        private string getNewTagName(out int numberZeroOrigin)
        {
            var newtag = Message.NewTag1;
            var pos = newtag.IndexOf("$1");
            string prefix, suffix;
            if (pos >= 0)
            {
                prefix = newtag.Substring(0, pos);
                suffix = newtag.Substring(pos + 2);
            }
            else
            {
                prefix = newtag + " ";
                suffix = "";
            }

            var maxIndex = 0;
            var tagNameCulumnIndex = TagNameColumn.Index;
            var prefixLength = prefix.Length;
            var baseLength = prefixLength + suffix.Length;
            foreach (DataGridViewRow row in dgvTagList.Rows)
            {
                var tag = row.Cells[tagNameCulumnIndex].Value.ToString();
                var digitLength = tag.Length - baseLength;
                int index;
                if (digitLength > 0 && tag.StartsWith(prefix) && tag.EndsWith(suffix) &&
                    int.TryParse(tag.Substring(prefixLength, digitLength), out index) && index > maxIndex)
                {
                    maxIndex = index;
                }
            }
            numberZeroOrigin = maxIndex;
            return prefix + (maxIndex + 1) + suffix;
        }

        private void dgvTagList_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvSorter == null || !dgvSorter.InRowsHolded) btnDelete.Enabled = btnColor.Enabled = dgvTagList.SelectedRows.Count > 0;
        }

        private static Color[] defaultColors = new Color[18] {
            Color.FromArgb(255,127,127), Color.FromArgb(127,255,127), Color.FromArgb(127,127,255),
            Color.FromArgb(255,255,127), Color.FromArgb(255,127,255), Color.FromArgb(127,255,255),
            Color.FromArgb(127,000,000), Color.FromArgb(000,127,000), Color.FromArgb(000,000,127),
            Color.FromArgb(127,127,000), Color.FromArgb(127,000,127), Color.FromArgb(000,127,127),
            Color.FromArgb(000,000,000), Color.FromArgb(050,050,050), Color.FromArgb(100,100,100),
            Color.FromArgb(161,161,161), Color.FromArgb(208,208,208), Color.FromArgb(255,255,255),
        };

        private void btnColor_Click(object sender, EventArgs e)
        {
            var selectedRows = dgvTagList.SelectedRows;
            if (selectedRows.Count <= 0) return;
            var firstRow = selectedRows[0];

            //ColorDialogクラスのインスタンスを作成
            using (var cd = new ColorDialog())
            {

                //はじめに選択されている色を設定
                cd.Color = firstRow.Cells[0].Style.BackColor;
                //色の作成部分を表示可能にする
                //デフォルトがTrueのため必要はない
                cd.AllowFullOpen = true;
                //純色だけに制限しない
                //デフォルトがFalseのため必要はない
                cd.SolidColorOnly = false;
                /*
                //[作成した色]に指定した色（RGB値）を表示する
                cd.CustomColors = new int[] {
        0x33, 0x66, 0x99, 0xCC, 0x3300, 0x3333,
        0x3366, 0x3399, 0x33CC, 0x6600, 0x6633,
        0x6666, 0x6699, 0x66CC, 0x9900, 0x9933};
        */
                //ダイアログを表示する
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    //選択された色の取得
                    var backColor = cd.Color;
                    var foreColor = ZipTag.GetForeColor(backColor);

                    foreach (DataGridViewRow row in selectedRows)
                    {
                        row.Cells[TagNameColumn.Index].Style.BackColor = backColor;
                        row.Cells[TagNameColumn.Index].Style.ForeColor = ZipTag.GetForeColor(backColor);
                    }
                }
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            foreach(DataGridViewRow row in dgvTagList.SelectedRows)
            {
                dgvTagList.Rows.Remove(row);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (isChanged())
            {
                var exception = saveTags();
                if (exception == null)
                {
                    initialZipTagArray = null;
                    Edited = true;
                    Close();
                }
                else
                {
                    dgvSorter.DoMouseUp();
                    MessageBox.Show(this, exception.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                initialZipTagArray = null;
                Close();
            }
        }

        private void TagEditForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            dgvTagList_CellValidating_canceled = false;
            Validate();
            if (dgvTagList_CellValidating_canceled)
            {
                e.Cancel = true;
                return;
            }
            if (isChanged())
            {
                dgvSorter.DoMouseUp();
                var r = MessageBox.Show(this, Message.DoYouDiscardChangedSettings, Message.Question, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes)
                {
                    e.Cancel = true;
                }
            }
        }

        private bool isChanged()
        {
            if (initialZipTagArray == null) return false;
            var rows = dgvTagList.Rows;
            bool diff = rows.Count != initialZipTagArray.Length;
            if (diff)
            {
                return true;
            }
            else
            {
                for (var i = 0; i < initialZipTagArray.Length; i++)
                {
                    var cell = rows[i].Cells[TagNameColumn.Index];
                    var tag = initialZipTagArray[i];
                    if (cell.Value as string != tag.Name || cell.Style.BackColor != tag.BackColor)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void dgvTagList_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridView dgv = (DataGridView)sender;
            var rows = dgv.Rows;
            var row = rows[e.RowIndex];
            var cell = row.Cells[e.ColumnIndex];
            var newName = cell.Value as string; if (newName == null) newName = "";
             var trimedNewName = newName.Trim();
            if (string.IsNullOrEmpty(trimedNewName))
            {
                rows.Remove(row);

                // これをしないと削除ボタンを押したときに意図しない二重削除が起こってしまう
                foreach (DataGridViewRow row2 in rows) row2.Selected = false;
            }
            else
            {
                if (trimedNewName != newName)
                {
                    cell.Value = trimedNewName;
                }
                dgvTagList.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = SortOrder.None;
            }
        }

        private static readonly LogicalStringComparer NaturalSort = new LogicalStringComparer();
        private void dgvTagList_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            e.SortResult = NaturalSort.Compare(e.CellValue1.ToString(), e.CellValue2.ToString());
            e.Handled = true;
        }

        private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            colorToolStripMenuItem.Enabled = btnColor.Enabled;
            deleteToolStripMenuItem.Enabled = btnDelete.Enabled;
        }

        private void TagEditForm_Shown(object sender, EventArgs e)
        {
            new BetterFormRestoreBounds(this);
        }
    }
}
