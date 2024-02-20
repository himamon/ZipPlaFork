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
    public partial class FilterEditForm : Form
    {
        private List<string> filteringStrings;
        private Dictionary<string, string> aliasesToFilteringStrings;

        private string[] initialFilteringStrings;
        private string[] initialAliasStrings;
        private string[] initialFilteringStringsByAlias;
        private bool initialinvariant;
        public bool invariantOut;

        DataGridViewSorter dgvSorter;

        public bool Edited = false;

        public FilterEditForm(List<string> filterinStringsInOut, Dictionary<string, string> aliasesToFilteringStringsInOut, bool invariantIn, Color profileColor)
        {
            InitializeComponent();
            
            new MessageForwarder(dataGridView, ForwardedMessage.MouseWheel);

            ToolStripOverwriter.SquarizeToolStripInClass(this);

            filteringStrings = filterinStringsInOut;
            aliasesToFilteringStrings = aliasesToFilteringStringsInOut;
            cbInvariantList.Checked = invariantIn;

            Program.SetDoubleBuffered(dataGridView);

            filteringStringColumn.HeaderCell.Style.WrapMode = DataGridViewTriState.False;
            filterAliasColumn.HeaderCell.Style.WrapMode = DataGridViewTriState.False;

            SetMessages();
            Width -= btnAdd.Left - (cbInvariantList.Right + (btnCancel.Left - btnOK.Right));
            MinimumSize = Size;

            dataToGridView();
            gridViewToData();
            initialFilteringStrings = filteringStrings.ToArray();
            initialAliasStrings = aliasesToFilteringStrings.Keys.ToArray();
            initialFilteringStringsByAlias = aliasesToFilteringStrings.Values.ToArray();
            initialinvariant = invariantIn;

            dgvSorter = new DataGridViewSorter(dataGridView);
            dgvSorter.RowsReleased += dataGridView_SelectionChanged;


            pictureBox.Width = pictureBox.Height = btnOK.Height - 2;
            pictureBox.Left = dataGridView.Left;
            pictureBox.Top = btnOK.Bottom - pictureBox.Height - 1;
            pictureBox.BackColor = profileColor;

            Program.SetInitialHeightAndPackToOwnerFormScreen(this, dataGridView);

            var listener = DataGridViewScrollBarTouchFixer.GetGestureListener(this);
            new DataGridViewScrollBarTouchFixer(dataGridView, listener);

            btnOK.Select();
        }

        private void SetMessages()
        {
            Text = Message.EditFilters;
            filteringStringColumn.HeaderText = Message.FilteringString;
            filterAliasColumn.HeaderText = Message.Alias;
            cbInvariantList.Text = Message.InvariantDropdownList;
            btnDelete.Text = Message.Delete;
            btnAdd.Text = Message.Add;
            btnAddPreset.Text = Message.Presets;
            btnOK.Text = Message.OK;
            btnCancel.Text = Message.Cancel;
            deleteToolStripMenuItem.Text = Message.Delete;
        }

        private bool setToDataAndCheckChanged()
        {
            if (initialFilteringStrings == null) return false;
            gridViewToData();
            if (cbInvariantList.Checked != initialinvariant) return true;
            if (!initialFilteringStrings.SequenceEqual(filteringStrings)) return true;
            if (!initialAliasStrings.SequenceEqual(aliasesToFilteringStrings.Keys)) return true;
            if (!initialFilteringStringsByAlias.SequenceEqual(aliasesToFilteringStrings.Values)) return true;
            return false;
        }

        private void rollbackDictionary()
        {
            aliasesToFilteringStrings.Clear();
            for(var i = 0; i < initialAliasStrings.Length; i++)
            {
                aliasesToFilteringStrings.Add(initialAliasStrings[i], initialFilteringStringsByAlias[i]);
            }
        }

        private void dataToGridView()
        {
            var rows = dataGridView.Rows;
            var newRows = new List<DataGridViewRow>();
            var dupCheck = new HashSet<string>();
            foreach(var fs in filteringStrings)
            {
                if (string.IsNullOrEmpty(fs)) continue;

                if (dupCheck.Contains(fs)) continue;
                else dupCheck.Add(fs);

                var row = new DataGridViewRow();
                row.CreateCells(dataGridView);
                var cells = row.Cells;
                if(aliasesToFilteringStrings != null && aliasesToFilteringStrings.ContainsKey(fs))
                {
                    if (!canBeAlias(fs)) continue;
                    cells[filteringStringColumn.Index].Value = aliasesToFilteringStrings[fs]; 
                    cells[filterAliasColumn.Index].Value = fs;
                }
                else
                {
                    cells[filteringStringColumn.Index].Value = fs;
                    cells[filterAliasColumn.Index].Value = "";
                }
                newRows.Add(row);
            }
            rows.Clear();
            rows.AddRange(newRows.ToArray());
        }

        private void gridViewToData()
        {
            filteringStrings.Clear();
            aliasesToFilteringStrings.Clear();
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                var cells = row.Cells;
                var s = $"{cells[filteringStringColumn.Index].Value}";
                var a = $"{cells[filterAliasColumn.Index].Value}";
                if(a == "")
                {
                    filteringStrings.Add(s);
                }
                else
                {
                    filteringStrings.Add(a);
                    aliasesToFilteringStrings[a] = s;
                }
            }
        }

        private static readonly Regex canNotBeAliasRegex = new Regex(@"[ \\<=>+\-*?""]", RegexOptions.Compiled);
        private bool canBeAlias(string str)
        {
            return !canNotBeAliasRegex.IsMatch(str);
        }

        private bool UniqueDisplayName(string str, int currentRowIndex)
        {
            var index = 0;
            foreach(DataGridViewRow row in dataGridView.Rows)
            {
                if (index++ == currentRowIndex) continue;
                var cells = row.Cells;
                var s = cells[filteringStringColumn.Index].Value?.ToString();
                var a = cells[filterAliasColumn.Index].Value?.ToString();
                var d = string.IsNullOrEmpty(a) ? s : a;
                if (d == str) return false;
            }
            return true;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void dataGridView_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvSorter == null || !dgvSorter.InRowsHolded) btnDelete.Enabled =  dataGridView.SelectedRows.Count > 0;
        }

        private bool dataGridView_CellValidating_canceled = false;
        private void dataGridView_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (Program.CancelMovingeEditingCellByLeftOrRightKey(sender, e)) return;

            DataGridView dgv = (DataGridView)sender;

            //新しい行のセルでなく、セルの内容が変更されている時だけ検証する
            if (e.RowIndex == dgv.NewRowIndex || !dgv.IsCurrentCellDirty)
            {
                return;
            }
            
            var newStr = e.FormattedValue?.ToString();
            if (newStr == null) newStr = "";
            if (e.ColumnIndex == filteringStringColumn.Index)
            {
                if (string.IsNullOrEmpty(newStr) && !string.IsNullOrEmpty($"{dataGridView.Rows[e.RowIndex].Cells[filterAliasColumn.Index].Value}"))
                {
                    dgvSorter.DoMouseUp();
                    MessageBox.Show(this, Message.FilteringStringMustNotBeEmpty, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    dataGridView_CellValidating_canceled = true;
                    dgv.CancelEdit();
                }
                else if (!UniqueDisplayName(newStr, e.RowIndex))
                {
                    dgvSorter.DoMouseUp();
                    MessageBox.Show(this, Message.DisplayNameCanNotShareSameName, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    dataGridView_CellValidating_canceled = true;
                    dgv.CancelEdit();
                }
                else
                {
                    try
                    {
                        new SearchManager(newStr);
                    }
                    catch (Exception ex)
                    {
                        dgvSorter.DoMouseUp();
                        MessageBox.Show(this, ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        dataGridView_CellValidating_canceled = true;
                        dgv.CancelEdit();
                    }
                }

            }
            else if(e.ColumnIndex == filterAliasColumn.Index)
            {
                var trimedNewName = newStr.Trim();
                if (string.IsNullOrEmpty(trimedNewName))
                {
                    if (!UniqueDisplayName($"{dataGridView.Rows[e.RowIndex].Cells[filteringStringColumn.Index].Value}", e.RowIndex))
                    {
                        dgvSorter.DoMouseUp();
                        MessageBox.Show(this, Message.DisplayNameCanNotShareSameName, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        dataGridView_CellValidating_canceled = true;
                        dgv.CancelEdit();
                    }
                }
                else
                {
                    if (!canBeAlias(trimedNewName))
                    {
                        dgvSorter.DoMouseUp();
                        MessageBox.Show(this, Message.AliasMustBeNoEmptyStringWhichDoesNotIncludeSpaceAndFollowingCharacters +
                            "\n\n\\ < = > + - * ? \"", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        dataGridView_CellValidating_canceled = true;
                        dgv.CancelEdit();
                    }
                    else if (!UniqueDisplayName(trimedNewName, e.RowIndex))
                    {
                        dgvSorter.DoMouseUp();
                        MessageBox.Show(this, Message.DisplayNameCanNotShareSameName, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        dataGridView_CellValidating_canceled = true;
                        dgv.CancelEdit();
                    }
                }
            }

            
        }

        private void dataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridView dgv = (DataGridView)sender;
            var rows = dgv.Rows;
            var row = rows[e.RowIndex];
            var cell = row.Cells[e.ColumnIndex];
            var newName = cell.Value as string;
            if (e.ColumnIndex == filteringStringColumn.Index)
            {
                if (string.IsNullOrEmpty(newName))
                {
                    rows.Remove(row);

                    // これをしないと削除ボタンを押したときに意図しない二重削除が起こってしまう
                    foreach (DataGridViewRow row2 in rows) row2.Selected = false;
                }
                else
                {
                    dataGridView.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = SortOrder.None;
                }
            }

            else if (e.ColumnIndex == filterAliasColumn.Index)
            {
                var trimedNewName = $"{newName}".Trim();
                if (trimedNewName != newName)
                {
                    cell.Value = trimedNewName;
                }
                dataGridView.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = SortOrder.None;
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            addLine(getNewFilteringString(), "");
        }

        private void addLine(string str, string alias)
        {
            var rows = dataGridView.Rows;
            var currentRow = dataGridView.CurrentRow;
            var currentIndex = currentRow != null ? currentRow.Index : rows.Count - 1;
            var row = new DataGridViewRow();
            row.CreateCells(dataGridView);
            var cells = row.Cells;

            foreach (DataGridViewColumn column in dataGridView.Columns)
            {
                column.HeaderCell.SortGlyphDirection = SortOrder.None;
            }

            cells[filteringStringColumn.Index].Value = str;
            cells[filterAliasColumn.Index].Value = alias;
            dataGridView.Rows.Insert(++currentIndex, row);
            dataGridView.CurrentCell = dataGridView.Rows[currentIndex].Cells[0];
            dataGridView.BeginEdit(true);
        }

        private string getNewFilteringString()
        {
            var newstring = Message.NewFilteringString1;
            var pos = newstring.IndexOf("$1");
            string prefix, suffix;
            if (pos >= 0)
            {
                prefix = newstring.Substring(0, pos);
                suffix = newstring.Substring(pos + 2);
            }
            else
            {
                prefix = newstring + " ";
                suffix = "";
            }

            var maxIndex = 0;
            var filteringStringCulumnIndex = filteringStringColumn.Index;
            var aliasCurumnIndex = filterAliasColumn.Index;
            var prefixLength = prefix.Length;
            var baseLength = prefixLength + suffix.Length;
            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                var a = $"{row.Cells[aliasCurumnIndex].Value}";
                var d = a != "" ? a : $"{row.Cells[filteringStringCulumnIndex].Value}";
                var digitLength = d.Length - baseLength;
                int index;
                if (digitLength > 0 && d.StartsWith(prefix) && d.EndsWith(suffix) &&
                    int.TryParse(d.Substring(prefixLength, digitLength), out index) && index > maxIndex)
                {
                    maxIndex = index;
                }
            }
            return prefix + (maxIndex + 1) + suffix;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView.SelectedRows)
            {
                dataGridView.Rows.Remove(row);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (setToDataAndCheckChanged())
            {
                invariantOut = cbInvariantList.Checked;
                Edited = true;
                initialFilteringStrings = null;
                Close();
            }
            else
            {
                initialFilteringStrings = null;
                Close();
            }
        }

        private void FilterEditForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            dataGridView_CellValidating_canceled = false;
            Validate();
            if (dataGridView_CellValidating_canceled)
            {
                e.Cancel = true;
                return;
            }
            if (setToDataAndCheckChanged())
            {
                dgvSorter.DoMouseUp();
                var r = MessageBox.Show(this, Message.DoYouDiscardChangedSettings, Message.Question, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (r != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            if(!Edited) rollbackDictionary();
        }

        private static readonly LogicalStringComparer NaturalSort = new LogicalStringComparer();
        private void dataGridView_SortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            e.SortResult = NaturalSort.Compare(e.CellValue1.ToString(), e.CellValue2.ToString());
            e.Handled = true;
        }

        private string FilterSampleDescription(int i)
        {
            switch(i)
            {
                case 1: return Message.FilterSampleDescription1;
                case 2: return Message.FilterSampleDescription2;
                case 3: return Message.FilterSampleDescription3;
                case 4: return Message.FilterSampleDescription4;
                case 5: return Message.FilterSampleDescription5;
                case 6: return Message.FilterSampleDescription6;
                case 7: return Message.FilterSampleDescription7;
                case 8: return Message.FilterSampleDescription8;
                case 9: return Message.FilterSampleDescription9;
                default: return "";
            }
        }

        private string FilterSampleString(int i)
        {
            switch (i)
            {
                case 1: return Message.FilterSampleString1;
                case 2: return Message.FilterSampleString2;
                case 3: return Message.FilterSampleString3;
                case 4: return Message.FilterSampleString4;
                case 5: return Message.FilterSampleString5;
                case 6: return Message.FilterSampleString6;
                case 7: return Message.FilterSampleString7;
                case 8: return Message.FilterSampleString8;
                case 9: return Message.FilterSampleString9;
                default: return "";
            }
        }

        private string FilterSampleAlias(int i)
        {
            switch (i)
            {
                case 1: return Message.FilterSampleAlias1;
                case 2: return Message.FilterSampleAlias2;
                case 3: return Message.FilterSampleAlias3;
                case 4: return Message.FilterSampleAlias4;
                case 5: return Message.FilterSampleAlias5;
                case 6: return Message.FilterSampleAlias6;
                case 7: return Message.FilterSampleAlias7;
                case 8: return Message.FilterSampleAlias8;
                case 9: return Message.FilterSampleAlias9;
                default: return "";
            }
        }

        private void btnAddPreset_Click(object sender, EventArgs e)
        {
            var filteringStringCulumnIndex = filteringStringColumn.Index;
            var aliasCurumnIndex = filterAliasColumn.Index;
            var rows = dataGridView.Rows;
            var count = rows.Count;
            var displayNames = new string[count];
            for(var i = 0; i < count; i++)
            {
                var row = rows[i];
                var a = $"{row.Cells[aliasCurumnIndex].Value}";
                displayNames[i] = a != "" ? a : $"{row.Cells[filteringStringCulumnIndex].Value}";
            }
            var newItems = new List<ToolStripMenuItem>();
            for (var i = 1; i <= 9; i++)
            {
                var desc = FilterSampleDescription(i); if (string.IsNullOrEmpty(desc)) continue;
                var str = FilterSampleString(i); if (string.IsNullOrEmpty(desc)) continue;
                var alias = FilterSampleAlias(i);if (alias == null) alias = "";
                alias = alias.Trim();
                if (alias != "" && !canBeAlias(alias)) alias = "";
                var display = alias == "" ? str : alias;
                var item = new ToolStripMenuItem(desc);
                if (UniqueDisplayName(display, -1))
                {
                    item.Click += Item_Click;
                }
                else
                {
                    item.Enabled = false;
                }
                newItems.Add(item);
            }
            cmsPreset.Items.Clear();
            cmsPreset.Items.AddRange(newItems.ToArray());
            cmsPreset.Show(Cursor.Position);
        }

        private void Item_Click(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItem;
            var index = cmsPreset.Items.IndexOf(item) + 1;
            var alias = FilterSampleAlias(index);
            if (alias == null) alias = "";
            alias = alias.Trim();
            addLine(FilterSampleString(index), alias);
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

        private void FilterEditForm_Shown(object sender, EventArgs e)
        {
            new BetterFormRestoreBounds(this);
        }
    }
}
