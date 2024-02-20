using Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormUtilities;

namespace ZipPla
{
    public partial class SmartFolderSettingForm : Form
    {
        SmartFolder initialSmartFolder;
        string targetSmartFolderPath;

        private bool showCanceled = false;

        DataGridViewSorter dgvSorter;
        public SmartFolderSettingForm(string smartFolderPath)
        {
            SmartFolder smartFolder = null;
            if (smartFolderPath != null)
            {
                targetSmartFolderPath = smartFolderPath;
                try
                {
                    smartFolder = new SmartFolder(smartFolderPath);
                }
                catch (Exception e)
                {
                    MessageBox.Show(this, e.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //Shown += new EventHandler((sender , e) => { Close(); }); // コンストラクタ中で Close を呼んではいけないため
                    showCanceled = true; // 代替案
                    return;
                }
            }
            InitializeComponent();

            new MessageForwarder(dataGridView, ForwardedMessage.MouseWheel);

            ToolStripOverwriter.SquarizeToolStripInClass(this);

            Program.SetDoubleBuffered(dataGridView);

            // 内部で Path を呼び出しているので念のため
            try
            {
                SetMessages();
            }
            catch (Exception e)
            {
                MessageBox.Show(this, e.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                //Shown += new EventHandler((sender , e) => { Close(); }); // コンストラクタ中で Close を呼んではいけないため
                showCanceled = true; // 代替案
                return;
            }

            cbcEssential.HeaderCell.Style.WrapMode = DataGridViewTriState.False;
            tbcFolderPath.HeaderCell.Style.WrapMode = DataGridViewTriState.False;
            tbcFileFilter.HeaderCell.Style.WrapMode = DataGridViewTriState.False;

            dgvSorter = new DataGridViewSorter(dataGridView);
            dgvSorter.RowsReleased += dataGridView_SelectionChanged;

            if (smartFolder != null)
            {
                // smartFolder を読み取れるが設定ファイルの破損やバージョンアップ等で内容が不正ということは想定される
                try
                {
                    setCurrentSmartFolder(smartFolder);
                }
                catch (Exception e)
                {
                    MessageBox.Show(this, e.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //Shown += new EventHandler((sender , e) => { Close(); }); // コンストラクタ中で Close を呼んではいけないため
                    showCanceled = true; // 代替案
                    return;
                }
            }

            initialSmartFolder = getCurrentSmartFolder(); // 拡張耐性のため smartFolder != null でも一度表示したものを取り出す

            var listener = DataGridViewScrollBarTouchFixer.GetGestureListener(this);
            new DataGridViewScrollBarTouchFixer(dataGridView, listener);

            Program.SetInitialHeightAndPackToOwnerFormScreen(this, dataGridView);
        }

        public new void Show()
        {
            if (showCanceled)
            {
                return;
            }
            else
            {
                base.Show();
            }
        }

        public new void ShowDialog(IWin32Window owner)
        {
            if (showCanceled)
            {
                return;
            }
            else
            {
                base.ShowDialog(owner);
            }
        }

        public bool Edited { get; private set; } = false;
        public string TargetSmartFolderPath { get { return targetSmartFolderPath; } }

        private void SetMessages()
        {
            Text = Message.EditSmartFolder;
            lblHint.Text = targetSmartFolderPath == null ? Message.HintRightClickNameOfKdkFileOnAddressBarToEditItAgain :
                Message.Editing1.Replace("$1", Path.GetFileName(targetSmartFolderPath));
            //MinimumSize = new Size(Math.Max(Width -(btnAdd.Left - dataGridView.Left), lblHint.Right + lblHint.Left + Width - ClientSize.Width), MinimumSize.Height);
            MinimumSize = new Size( Math.Max
            (
                Width - btnAdd.Left + btnNotEssential.Right + btnNotEssential.Left - btnEssential.Right,
                lblHint.Right + lblHint.Left - btnOK.Left + Width
            ),
            MinimumSize.Height);

            cbcEssential.HeaderText = Message.Essential;
            tbcFolderPath.HeaderText = Message.FolderPath;
            tbcFileFilter.HeaderText = Message.Filter;

            btnEssential.Text = Message.CheckAll;
            btnNotEssential.Text = Message.UncheckAll;

            btnAdd.Text = Message.Add;
            btnDelete.Text = Message.Delete;
            btnOK.Text = targetSmartFolderPath == null ? Message.Save : Message.Overwrite;
            btnOK.Text = Message.OK;
            btnCancel.Text = Message.Cancel;

            saveFileDialog.Filter = $"{Message.SmartFolder}|*.{SmartFolder.ExtensionWithoutPeriodInLower}";

            deleteToolStripMenuItem.Text = Message.Delete;

            dataGridView.Columns[0].ToolTipText = Message.IfEssentialFolderDoesNotExistErrorWillBeDisplayed;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if(!SaveSettings()) return;
            initialSmartFolder = null;
            Close();
        }

        private bool SaveSettings()
        {
            var currentSmartFolder = getCurrentSmartFolder();

            if(currentSmartFolder.Items.Count <= 0)
            {
                // btnOK.Enabled == false のためここへの到達は想定されない
                return false;
            }

            if(initialSmartFolder == null || initialSmartFolder.Equals(currentSmartFolder))
            {
                return true;
            }

            var temp = targetSmartFolderPath;
            if (targetSmartFolderPath == null)
            {
                if(saveFileDialog.ShowDialog(this) != DialogResult.OK)
                {
                    return false;
                }
                targetSmartFolderPath = saveFileDialog.FileName;
            }

            try
            {
                currentSmartFolder.SaveToFile(targetSmartFolderPath);
                Edited = true;
                return true;
            }
            catch(Exception e)
            {
                targetSmartFolderPath = temp;
                MessageBox.Show(this, e.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private static readonly LogicalStringComparer NaturalSort = new LogicalStringComparer();
        private void dataGridView_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            var s1 = e.CellValue1 as string;
            if (s1 != null)
            {
                var s2 = e.CellValue2 as string;
                if (s2 != null)
                {
                    e.SortResult = NaturalSort.Compare(s1, s2);
                    e.Handled = true;
                }
            }
        }

        const int DragEventKeyState_LeftButton = 1;
        private void dataGridView_DragEnter(object sender, DragEventArgs e)
        {
            try
            {
                if (e.KeyState == DragEventKeyState_LeftButton && e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] Files = ShortcutResolver.Exec((string[])e.Data.GetData(DataFormats.FileDrop, false)).ToArray();
                    if (Files.Any(file => Directory.Exists(file)))
                    {
                        e.Effect = DragDropEffects.Copy;
                    }
                    else
                    {
                        e.Effect = DragDropEffects.None;
                    }
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }
            catch (Exception error)
            {
                Program.AlertError(error);
            }
        }

        private void dataGridView_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    addFolders(
                        from path in ShortcutResolver.Exec((string[])e.Data.GetData(DataFormats.FileDrop, false))
                        where Directory.Exists(path)
                        select CatalogForm.GetDirectoryOrRootFullPath(path),
                        AddRowsPostAction.SelectAddition,
                        replaceFilterToCurrent: true);
                }
            }
            catch (Exception error)
            {
                Program.AlertError(error);
            }
        }

        private void addFolders(string folderPath, AddRowsPostAction postAction, bool replaceFilterToCurrent)
        {
            addFolders(new string[1] { folderPath }, postAction, replaceFilterToCurrent);
        }

        private void addFolders(IEnumerable<string> folderPathCollection, AddRowsPostAction postAction, bool replaceFilterToCurrent)
        {
            addRows(
                from path in folderPathCollection
                select makeRow(essential: true, folderPath: path, filter: SettingForm.AnyPattern),
                postAction, replaceFilterToCurrent);
        }

        private DataGridViewRow makeRow(bool essential, string folderPath, string filter)
        {
            var row = new DataGridViewRow();
            row.CreateCells(dataGridView);
            var cells = row.Cells;
            var essentialCell = cells[cbcEssential.Index];
            essentialCell.Value = essential;
            essentialCell.ToolTipText = Message.IfEssentialFolderDoesNotExistErrorWillBeDisplayed;
            cells[tbcFolderPath.Index].Value = folderPath;
            cells[tbcFileFilter.Index].Value = filter;
            return row;
        }

        enum AddRowsPostAction { None, BeginEdit, SelectAddition }
        private void addRows(IEnumerable<DataGridViewRow> newRows, AddRowsPostAction postAction, bool replaceFilterToCurrent)
        {
            if (newRows == null || !newRows.Any()) return;

            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                column.HeaderCell.SortGlyphDirection = SortOrder.None;
            }

            var rows = dataGridView.Rows;
            var existingPathList = (from DataGridViewRow row in rows select row.Cells[tbcFolderPath.Index].Value as string).ToList();
            var currentRow = dataGridView.CurrentRow;
            var filterReplacer = replaceFilterToCurrent && currentRow != null ? currentRow.Cells[tbcFileFilter.Index].Value : null;
            var currentIndex = currentRow != null ? currentRow.Index : rows.Count - 1;
            var newRows2= new List<DataGridViewRow>();
            foreach (var row in newRows)
            {
                var currentPath = row.Cells[tbcFolderPath.Index].Value as string;
                var foundIndex = existingPathList.IndexOf(currentPath);
                if (foundIndex >= 0)
                {
                    newRows2.Add(rows[foundIndex]);
                }
                else
                {
                    existingPathList.Add(currentPath);
                    if (filterReplacer != null)
                    {
                        row.Cells[tbcFileFilter.Index].Value = filterReplacer;
                    }
                    rows.Insert(++currentIndex, row);
                    newRows2.Add(row);
                }
            }

            var indexArray = (from row in newRows2 select row.Index).ToArray();
            var topRowIndex = indexArray.Min();
            currentIndex = indexArray.Max();

            if (postAction == AddRowsPostAction.BeginEdit)
            {
                dataGridView.CurrentCell = rows[topRowIndex].Cells[tbcFileFilter.Index];
                dataGridView.BeginEdit(true);
            }
            else
            {
                if (postAction == AddRowsPostAction.SelectAddition)
                {
                    foreach (DataGridViewRow row in rows) row.Selected = newRows2.Contains(row);
                }
                else
                {
                    foreach (DataGridViewRow row in rows) row.Selected = false;
                }
                dataGridView.CurrentCell = rows[currentIndex].Cells[tbcFileFilter.Index];
            }

        }

        private void dataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex == tbcFileFilter.Index)
            {
                DataGridView dgv = (DataGridView)sender;
                var rows = dgv.Rows;
                var row = rows[e.RowIndex];
                var cell = row.Cells[e.ColumnIndex];
                var newName = $"{cell.Value}";
                var trimedNewName = newName.Trim();
                if (trimedNewName == "") trimedNewName = SettingForm.AnyPattern;
                if (trimedNewName != newName)
                {
                    cell.Value = trimedNewName;
                }
            }
            else if (e.ColumnIndex == cbcEssential.Index)
            {
                setEssentialButtonEnabled();
            }
        }

        private void dataGridView_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvSorter == null || !dgvSorter.InRowsHolded) btnDelete.Enabled = dataGridView.SelectedRows.Count > 0;
        }

        bool dataGridView_CellValidating_canceled;
        private void dataGridView_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (Program.CancelMovingeEditingCellByLeftOrRightKey(sender, e)) return;

            DataGridView dgv = dataGridView;

            //新しい行のセルでなく、セルの内容が変更されている時だけ検証する
            if (e.RowIndex == dgv.NewRowIndex || !dgv.IsCurrentCellDirty)
            {
                return;
            }

            if (e.ColumnIndex != tbcFileFilter.Index) return;
            
            var newName = $"{e.FormattedValue}";
            var trimedNewName = newName.Trim();
            if (trimedNewName == "") trimedNewName = SettingForm.AnyPattern;

            if (!ApplicationInfo.Parsable(trimedNewName))
            {
                dgvSorter.DoMouseUp();
                MessageBox.Show(this, Message.ApplicationFilterDescription.Replace(@"\n", "\n"), null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                dataGridView_CellValidating_canceled = true;
                dgv.CancelEdit();
            }
        }

        private void dataGridView_MouseDown(object sender, MouseEventArgs e)
        {
            var location = e.Location;
            var hit = dataGridView.HitTest(location.X, location.Y);
            if (hit.RowIndex < 0 || hit.ColumnIndex < 0)
            {
                Validate();
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    row.Selected = false;
                }
            }
            else if (e.Button == MouseButtons.Right && !dgvSorter.InRowsHolded && !dataGridView.IsCurrentCellInEditMode)
            {
                dataGridView_CellValidating_canceled = false;
                Validate();
                if (!dataGridView_CellValidating_canceled)
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
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if(folderBrowserDialog.ShowDialog(this) == DialogResult.OK)
            {
                addFolders(folderBrowserDialog.SelectedPath, AddRowsPostAction.BeginEdit, replaceFilterToCurrent: true);
            }
        }

        private SmartFolder getCurrentSmartFolder()
        {
            var result = new SmartFolder();
            result.DefaultCondition = null;
            result.Items.AddRange(
                from DataGridViewRow row in dataGridView.Rows
                select new SmartFolderItem()
                {
                    ResumeOnError = row.Cells[cbcEssential.Index].Value as bool? == false,
                    Path = row.Cells[tbcFolderPath.Index].Value as string,
                    Condition = new FileSystemCondition()
                    {
                        PathFilter = new RegexPathFilter(row.Cells[tbcFileFilter.Index].Value as string)
                    }
                });
            return result;
        }

        private void setCurrentSmartFolder(SmartFolder smartFolder)
        {
            addRows(from item in smartFolder.Items select makeRow(!item.ResumeOnError, item.Path, item.Condition.PathFilter.ToString()),
                AddRowsPostAction.None, replaceFilterToCurrent: false);
        }

        private void SmartFolderSettingForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            dataGridView_CellValidating_canceled = false;
            Validate();
            if (dataGridView_CellValidating_canceled)
            {
                e.Cancel = true;
                return;
            }
            if (initialSmartFolder != null && !initialSmartFolder.Equals(getCurrentSmartFolder()))
            {
                if (MessageBox.Show(this, Message.DoYouDiscardChangedSettings, Message.Question, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    e.Cancel = true;
                }
            }

        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView.SelectedRows)
            {
                dataGridView.Rows.Remove(row);
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnDelete.PerformClick();
        }

        private void dataGridView_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            btnOK.Enabled = true;
            setEssentialButtonEnabled();
        }

        private void dataGridView_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            if (dataGridView.Rows.Count <= 0) btnOK.Enabled = false;
            setEssentialButtonEnabled();
        }

        private void dataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dataGridView_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            if(e.RowIndex >= 0 && e.ColumnIndex == cbcEssential.Index && (e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                dataGridView.EndEdit();
            }
        }

        private void setEssentialButtonEnabled()
        {
            var checkedExists = false;
            var unCheckedExists = false;
            var cbcEssentialIndex = cbcEssential.Index;
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                if (row.Cells[cbcEssentialIndex].Value as bool? == true)
                {
                    checkedExists = true;
                    if (unCheckedExists) break;
                }
                else
                {
                    unCheckedExists = true;
                    if (checkedExists) break;

                }
            }
            btnEssential.Enabled = unCheckedExists;
            btnNotEssential.Enabled = checkedExists;
        }

        private void btnEssential_Click(object sender, EventArgs e)
        {
            setValue(cbcEssential, true);
        }

        private void btnNotEssential_Click(object sender, EventArgs e)
        {
            setValue(cbcEssential, false);
        }

        private void setValue(DataGridViewColumn column, object value)
        {
            var index = column.Index;
            foreach (DataGridViewRow row in dataGridView.Rows) row.Cells[index].Value = value;
        }

        private void SmartFolderSettingForm_Shown(object sender, EventArgs e)
        {
            new BetterFormRestoreBounds(this);
        }
    }
}
