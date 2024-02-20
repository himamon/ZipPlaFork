using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormUtilities
{
    public enum DataGridViewSorterRowsReleasedOperation { Changed, BackToOriginal, NotMoved }
    public class DataGridViewSorterRowsReleasedEventArgs : DataGridViewCellEventArgs
    {
        public readonly DataGridViewSorterRowsReleasedOperation Operation;
        public DataGridViewSorterRowsReleasedEventArgs(int columnIndex, int rowIndex,
            DataGridViewSorterRowsReleasedOperation oparation)
            : base(columnIndex, rowIndex)
        {
            Operation = oparation;
        }
    }
    public delegate void DataGridViewSorterRowsReleasedEventHandler(DataGridView sender, DataGridViewSorterRowsReleasedEventArgs e);

    public class DataGridViewSorter : IDisposable
    {
        public event DataGridViewCellEventHandler CellClick;

        private readonly DataGridView dataGridView;
        private Point mouseDownPoint = Point.Empty;
        private bool holdedRowsWareMoved = false;
        private DataGridViewRow holdedRow = null;
        private DataGridViewRow[] holdedRows = null;
        private DataGridViewRow[] beforeSortedRows = null;
        private DataGridViewRow[] mouseDownSelectedRows = null;

        public TimeSpan AutoScrollSpan = TimeSpan.FromSeconds(0.05);

        private IEnumerable<DataGridViewRow> selectedRowsManualFix = null;

        public event EventHandler UserSorted;
        public event EventHandler RowsHolded;
        public event DataGridViewSorterRowsReleasedEventHandler RowsReleased;

        public bool InRowsHolded
        {
            get
            {
                return holdedRows != null;
            }
        }

        public void DoMouseUp()
        {
            var pos = Cursor.Position;
            DataGridView_MouseUp(dataGridView, new MouseEventArgs(MouseButtons.Left, 1, pos.X, pos.Y, 0));
        }

        private IEnumerable<DataGridViewRow> selectedRows
        {
            get
            {
                return from DataGridViewRow row in dataGridView.SelectedRows orderby row.Index select row;
            }
            set
            {
                if (value == null) dataGridView.ClearSelection();
                else
                {
                    foreach (DataGridViewRow row in dataGridView.Rows)
                    {
                        row.Selected = value.Contains(row);
                    }
                }
            }
        }

        private IEnumerable<DataGridViewRow> rows
        {
            get
            {
                return from DataGridViewRow row in dataGridView.Rows select row;
            }
            set
            {
                var currentRows = dataGridView.Rows;
                var x = dataGridView.FirstDisplayedScrollingRowIndex;
                currentRows.Clear();
                if (value != null)
                {
                    currentRows.AddRange((from v in value where v != null select v).ToArray());
                }
                dataGridView.FirstDisplayedScrollingRowIndex = x;
            }
        }

        private void adsorbRows(int centerIndex, DataGridViewRow center, DataGridViewRow[] adsorbedRows)
        {
            var localCenterIndex = Array.IndexOf(adsorbedRows, center);
            var currentRows = rows.ToArray();
            var rowsCount = currentRows.Length;
            var adsorbedRowsCount = adsorbedRows.Length;
            if (centerIndex < localCenterIndex)
            {
                centerIndex = localCenterIndex;
            }
            else
            {
                var centerIndexUBound = rowsCount - adsorbedRowsCount + localCenterIndex;
                if (centerIndex > centerIndexUBound)
                {
                    centerIndex = centerIndexUBound;
                }
            }
            var newRows = new DataGridViewRow[rowsCount];
            var newIdx = 0;
            int currentIdx;
            for (currentIdx = 0; currentIdx < rowsCount && newIdx < centerIndex - localCenterIndex; currentIdx++)
            {
                var row = currentRows[currentIdx];
                if (!adsorbedRows.Contains(row)) newRows[newIdx++] = row;
            }
            for (var i = 0; i < localCenterIndex; i++) newRows[newIdx++] = adsorbedRows[i];
            newRows[newIdx++] = center;
            for (var i = localCenterIndex + 1; i < adsorbedRowsCount; i++) newRows[newIdx++] = adsorbedRows[i];
            for (; currentIdx < rowsCount; currentIdx++)
            {
                var row = currentRows[currentIdx];
                if (!adsorbedRows.Contains(row)) newRows[newIdx++] = row;
            }
            rows = newRows;
            selectedRows = adsorbedRows;
#if DEBUG
            /*
            var sr = selectedRows.OrderBy(r => r.Index).ToArray();
            for (var i = 1; i < sr.Length; i++)
            {
                if (sr[i].Index - sr[i - 1].Index > 1)
                {

                    break;
                }
            }
            */
#endif
        }

        public DataGridViewSorter(DataGridView dgv)
        {
            dataGridView = dgv;

            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView.MouseDown += DataGridView_MouseDown;
            dataGridView.MouseMove += DataGridView_MouseMove;
            dataGridView.MouseUp += DataGridView_MouseUp;
            dataGridView.SelectionChanged += DataGridView_SelectionChanged;

            RowsReleased += DefaultRowsReleased;
        }

        private void DefaultRowsReleased(DataGridView sender, DataGridViewSorterRowsReleasedEventArgs e)
        {
            if(e.Operation == DataGridViewSorterRowsReleasedOperation.Changed)
            {
                foreach(DataGridViewColumn column in dataGridView.Columns)
                {
                    column.HeaderCell.SortGlyphDirection = SortOrder.None;
                }
            }
        }

        private void DataGridView_SelectionChanged(object sender, EventArgs e)
        {
            if (selectedRowsManualFix != null)
            {
                selectedRows = selectedRowsManualFix;
                selectedRowsManualFix = null;
            }
            else if (InRowsHolded && holdedRows != null)
            {
                selectedRows = holdedRows;
            }
        }

        private void DataGridView_MouseDown(object sender, MouseEventArgs e)
        {
            var hit = dataGridView.HitTest(e.X, e.Y);
            if (e.Button == MouseButtons.Left)
            {
                if (hit.RowIndex < 0)
                {
                    if (hit.Type == DataGridViewHitTestType.None)
                    {
                        dataGridView.ClearSelection();
                    }
                    return;
                }

                var clickedRow = dataGridView.Rows[hit.RowIndex];

                if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                {
                    var list = new List<DataGridViewRow>(selectedRows);
                    if (list.Contains(clickedRow))
                    {
                        list.Remove(clickedRow);
                    }
                    else
                    {
                        list.Add(clickedRow);
                    }
                    selectedRowsManualFix = list.ToArray();
                    return;
                }
                else
                {
                    if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                    {
                        var list = new List<DataGridViewRow>(selectedRows);
                        var currentRows = rows.ToArray();
                        var focused = dataGridView.CurrentRow.Index;
                        var start = Math.Min(hit.RowIndex, focused);
                        var stop = Math.Max(hit.RowIndex, focused);
                        for (var i = start; i <= stop; i++)
                        {
                            var row = currentRows[i];
                            if (!list.Contains(row)) list.Add(row);
                        }
                        selectedRowsManualFix = list.ToArray();
                        return;
                    }
                }


                if (!clickedRow.Selected)
                {
                    dataGridView.ClearSelection();
                    clickedRow.Selected = true;
                }

                mouseDownPoint = e.Location;
                mouseDownSelectedRows = selectedRows.ToArray();
                var selectedRowsArray = mouseDownSelectedRows;
                if (selectedRowsArray.Length > 1)
                {
                    selectedRowsManualFix = selectedRowsArray;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                CancelDrag(hit);
            }
        }

        public void CancelDrag()
        {
            CancelDrag(null);
        }

        private void CancelDrag(DataGridView.HitTestInfo hit)
        {
            if (mouseDownPoint != Point.Empty)
            {
                if (holdedRow != null)
                {
                    rows = beforeSortedRows;
                    selectedRows = mouseDownSelectedRows;
                    holdedRow = null;
                    Cursor.Current = Cursors.Default;
                    holdedRows = null;

                    if (hit != null && RowsReleased != null)
                    {
                        var e2 = new DataGridViewSorterRowsReleasedEventArgs(hit.ColumnIndex, hit.RowIndex,
                            holdedRowsWareMoved ? DataGridViewSorterRowsReleasedOperation.BackToOriginal :
                            DataGridViewSorterRowsReleasedOperation.NotMoved);
                        holdedRowsWareMoved = false;
                        RowsReleased(dataGridView, e2);
                    }
                    else
                    {
                        holdedRowsWareMoved = false;
                    }
                    beforeSortedRows = null;
                }
            }
            mouseDownPoint = Point.Empty;
            mouseDownSelectedRows = null;
        }

        private static readonly int DragSizeOffesetX = SystemInformation.DragSize.Width >> 1;
        private static readonly int DragSizeOffesetY = SystemInformation.DragSize.Height >> 1;
        private static readonly int DragSizeWidth = (SystemInformation.DragSize.Width >> 1 << 1) + 1;
        private static readonly int DragSizeHeight = (SystemInformation.DragSize.Height >> 1 << 1) + 1;

        private DateTime DataGridView_MouseMove_LastAutoScrollTime = DateTime.Now;
        private void DataGridView_MouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDownPoint != Point.Empty)
            {
                if (holdedRow == null)
                {
                    var moveRect = new Rectangle(
                        mouseDownPoint.X - DragSizeOffesetX,
                        mouseDownPoint.Y - DragSizeOffesetY,
                        DragSizeWidth,
                        DragSizeHeight);
                    holdedRowsWareMoved = false;
                    if (!moveRect.Contains(e.Location))
                    {
                        if (mouseDownSelectedRows.Length > 0)
                        {
                            var hit = dataGridView.HitTest(mouseDownPoint.X, mouseDownPoint.Y);
                            var clickedRow = dataGridView.Rows[hit.RowIndex];
                            if (mouseDownSelectedRows.Contains(clickedRow))
                            {
                                holdedRow = clickedRow;
                                Cursor.Current = Cursors.NoMoveVert;
                                holdedRows = mouseDownSelectedRows;
                                RowsHolded?.Invoke(sender, e);
                                beforeSortedRows = rows.ToArray();

                                var rowCount = beforeSortedRows.Length;
                                var centerPosition = Array.IndexOf(beforeSortedRows, holdedRow);
                                adsorbRows(centerPosition, holdedRow, holdedRows);

                            }
                        }
                    }
                }
                else
                {
                    var hit = dataGridView.HitTest(e.X, e.Y);
                    var hitRowIndex = hit.RowIndex;
                    if (hitRowIndex < 0 && dataGridView.ClientRectangle.Contains(e.Location))
                    {
                        hitRowIndex = beforeSortedRows.Length;
                    }
                    
                    if (hitRowIndex < 0)
                    {
                        rows = beforeSortedRows;
                        selectedRows = holdedRows;
                        var now = DateTime.Now;
                        if(now >= DataGridView_MouseMove_LastAutoScrollTime + AutoScrollSpan)
                        {
                            var rect = dataGridView.DisplayRectangle;
                            var i = dataGridView.FirstDisplayedScrollingRowIndex;
                            if (e.Y < rect.Top)
                            {
                                if (i > 0)
                                {
                                    dataGridView.FirstDisplayedScrollingRowIndex = i - 1;
                                    DataGridView_MouseMove_LastAutoScrollTime = now;
                                }
                            }
                            else if (e.Y >= rect.Bottom)
                            {
                                dataGridView.FirstDisplayedScrollingRowIndex++;
                                DataGridView_MouseMove_LastAutoScrollTime = now;
                            }
                        }
                    }
                    else
                    {
                        adsorbRows(hitRowIndex, holdedRow, holdedRows);
                        holdedRowsWareMoved = holdedRowsWareMoved ||
                            (hitRowIndex < beforeSortedRows.Length && beforeSortedRows[hitRowIndex] != holdedRow);
                    }

                    dataGridView.Refresh();
                }
            }
        }

        private void DataGridView_MouseUp(object sender, MouseEventArgs e)
        {
            if (holdedRow != null)
            {
                holdedRow = null;
                Cursor.Current = Cursors.Default;
                holdedRows = null;
                var hit = dataGridView.HitTest(e.X, e.Y);
                var e2 = new DataGridViewSorterRowsReleasedEventArgs(hit.ColumnIndex, hit.RowIndex,
                    holdedRowsWareMoved ?
                        beforeSortedRows.SequenceEqual(rows) ?
                            DataGridViewSorterRowsReleasedOperation.BackToOriginal :
                            DataGridViewSorterRowsReleasedOperation.Changed:
                        DataGridViewSorterRowsReleasedOperation.NotMoved);
                holdedRowsWareMoved = false;
                RowsReleased?.Invoke(dataGridView, e2);
                beforeSortedRows = null;
                UserSorted?.Invoke(sender, e);
            }
            else if (mouseDownPoint != Point.Empty && (Control.ModifierKeys & (Keys.Shift | Keys.Control)) == 0)
            {
                var hit = dataGridView.HitTest(mouseDownPoint.X, mouseDownPoint.Y);
                var clickedRow = dataGridView.Rows[hit.RowIndex];
                selectedRows = new DataGridViewRow[1] { clickedRow };

                CellClick?.Invoke(dataGridView, new DataGridViewCellEventArgs(hit.ColumnIndex, hit.RowIndex));
            }
            mouseDownPoint = Point.Empty;
            mouseDownSelectedRows = null;
        }

        public void Dispose()
        {
            dataGridView.MouseDown -= DataGridView_MouseDown;
            dataGridView.MouseMove -= DataGridView_MouseMove;
            dataGridView.MouseUp -= DataGridView_MouseUp;
            dataGridView.SelectionChanged -= DataGridView_SelectionChanged;
        }

        ~DataGridViewSorter()
        {
            Dispose();
        }
    }
}
