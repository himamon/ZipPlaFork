using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public class HandlableMouseEventArgs : MouseEventArgs
    {
        public HandlableMouseEventArgs(MouseEventArgs e) : base(e.Button, e.Clicks, e.X, e.Y, e.Delta) { }

        public bool Handled = false;
    }

    public class DataGridViewFileList : DataGridView, IGraphicalList
    {
        /*
        DataGridView dataGridView;

        public DataGridViewFileList(DataGridView dataGridView)
        {
            this.dataGridView = dataGridView;
            dataGridView.MouseDown += dataGridView_MouseDown;
        }

        private void dataGridView_MouseDown(object sender, MouseEventArgs e)
        {
            throw new NotImplementedException();
        }

        */

        //public Func<int?> GetCurrentIndex;

        public SimplifiedKeyBoard SimplifiedKeyBoard;

        public bool DragOutSelection = false;
        public bool DragOutSingle = false;

        public event MouseEventHandler DragReady;
        public event MouseEventHandler DragAttempted;
        private Rectangle OnMouseDown_DragOutStartRectangle = Rectangle.Empty;
        private int OnMouseDown_DragOutStartIndex = -1;
        protected override void OnMouseDown(MouseEventArgs e)
        {
            OnMouseDown_DragOutStartRectangle = Rectangle.Empty;
            OnMouseDown_DragOutStartIndex = -1;

            var e2 = new HandlableMouseEventArgs(e);

            if (e.Button != MouseButtons.Left && e.Button != MouseButtons.Right)
            {
                MyOnMouseDown(e2);
                return;
            }

            var hit = HitTest(e.X, e.Y);
            if (hit.ColumnIndex < 0 || hit.RowIndex < 0)
            {
                MyOnMouseDown(e2);
                return;
            }

            if (!DragOutSelection && !DragOutSingle)
            {
                MyOnMouseDown(e2);
                return;
            }
            
            var clickedCell = this[hit.ColumnIndex, hit.RowIndex];
            var clickedCellSelected = clickedCell.Selected;
            if (clickedCellSelected)
            {
                InvokeMouseClick(this, e2, hit);
                MouseDown_Private?.Invoke(this, e2);
                //InvokeMouseDownWithoutBaseFunctions(e2, hit); // マウスダウンでカレントを変更すると Shift+MouseUp で一つしか変化しなくなる
                //foreach (var action in mouseDown) action(this, e2);
                if (DragOutSelection)
                {
                    if (!e2.Handled)
                    {
                        //OnMouseDown_DragOutStartRectangle = GetRectangleWithCenter(e.Location, SystemInformation.DragSize);
                        OnMouseDown_DragOutStartRectangle = GetRowDisplayRectangle(hit.RowIndex, cutOverflow: true);
                    }
                    return;
                }
            }
            else
            {
                MyOnMouseDown(e2);
                if (DragOutSingle)
                {
                    if (!e2.Handled)
                    {
                        if (e.Button == MouseButtons.Left)
                        {
                            OnMouseDown_DragOutStartIndex = clickedCell.RowIndex;
                        }
                        else
                        {
                            OnMouseDown_DragOutStartRectangle = GetRowDisplayRectangle(hit.RowIndex, cutOverflow: true);
                        }
                    }
                    return;
                }
            }
        }

        private static readonly System.Reflection.MethodInfo cellOnMouseDownInfo = typeof(DataGridViewCell).GetMethod("OnMouseDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        private static void InvokeMouseDownWithoutBaseFunctions(DataGridView dataGridView, MouseEventHandler mouseDown, Func<int,int,bool,bool,bool,bool> setCurrentCellAddressCore, MouseEventArgs e, HitTestInfo hit)
        {
            InvokeMouseClick(dataGridView,e, hit);
            mouseDown?.Invoke(dataGridView, e);

            // クリックした場所に必ずフォーカスを移す場合
            //if (hit.RowIndex >= 0 && hit.ColumnIndex >= 0)
            //{
            //    setCurrentCellAddressCore(hit.ColumnIndex, hit.RowIndex, /*setAnchorCellAddress:*/ false, /*validateCurrentCell:*/ false, /*throughMouseClick:*/ false);
            //}
        }

        private static void InvokeMouseClick(DataGridView dataGridView, MouseEventArgs e, HitTestInfo hit)
        {
            // セルの MouseDown は実行する必要あり
            if (hit.ColumnIndex >= 0 && hit.RowIndex >= 0)
            {
                var cell = dataGridView[hit.ColumnIndex, hit.RowIndex];
                cellOnMouseDownInfo.Invoke(cell, new object[] { new DataGridViewCellMouseEventArgs(hit.ColumnIndex, hit.RowIndex, e.X, e.Y, e) });
            }
        }

        private void MyOnMouseDown(MouseEventArgs e)
        {
            MyOnMouseDown(this, MouseDown_Private, base.OnMouseDown, SetCurrentCellAddressCore, SimplifiedKeyBoard, e);
        }

        public static void MyOnMouseDown(DataGridView dataGridView, MouseEventHandler mouseDown, Action<MouseEventArgs> baseOnMouseDown, Func<int, int, bool, bool, bool, bool> setCurrentCellAddressCore, SimplifiedKeyBoard simplifiedKeyBoard, MouseEventArgs e)
        {
            if (simplifiedKeyBoard == null)
            {
                baseOnMouseDown(e);
                return;
            }

            var hit = dataGridView.HitTest(e.X, e.Y);
            var index = hit.RowIndex;
            if (hit.ColumnIndex < 0 || index < 0)
            {
                baseOnMouseDown(e);
                return;
            }

            var modifier = simplifiedKeyBoard.GetModifierKeysWithoutUpHeldKeys();

            switch (modifier)
            {
                case Keys.Control:
                    if (e.Button == MouseButtons.Left)
                    {
                        if (index >= 0) dataGridView.Rows[index].Selected = !dataGridView.Rows[index].Selected;
                        InvokeMouseDownWithoutBaseFunctions(dataGridView, mouseDown, setCurrentCellAddressCore, e, hit);
                    }
                    else
                    {
                        baseOnMouseDown(e);
                    }
                    break;
                case Keys.Shift:
                    if (index >= 0)
                    {
                        //var f = GetCurrentIndex == null ? CurrentCell?.RowIndex : GetCurrentIndex();
                        var f = dataGridView.CurrentCell?.RowIndex;
                        if (f != null)
                        {
                            var f2 = (int)f;
                            var min = Math.Min(index, f2);
                            var max = Math.Max(index, f2);
                            for (var i = 0; i < min; i++) dataGridView.Rows[i].Selected = false;
                            for (var i = min; i <= max; i++) dataGridView.Rows[i].Selected = true;
                            var count = dataGridView.Rows.Count;
                            for (var i = max + 1; i < count; i++) dataGridView.Rows[i].Selected = false;
                        }
                    }
                    InvokeMouseDownWithoutBaseFunctions(dataGridView, mouseDown, setCurrentCellAddressCore, e, hit);
                    break;
                case Keys.Control | Keys.Shift:
                    if (index >= 0)
                    {
                        var newSelect = !dataGridView[hit.ColumnIndex, hit.RowIndex].Selected;

                        //var f = GetCurrentIndex == null ? CurrentCell?.RowIndex : GetCurrentIndex();
                        var f = dataGridView.CurrentCell?.RowIndex;
                        if (f != null)
                        {
                            var f2 = (int)f;
                            var min = Math.Min(index, f2);
                            var max = Math.Max(index, f2);
                            for (var i = min; i <= max; i++) dataGridView.Rows[i].Selected = newSelect;
                        }
                    }
                    InvokeMouseDownWithoutBaseFunctions(dataGridView, mouseDown, setCurrentCellAddressCore, e, hit);
                    break;
                default:
                    baseOnMouseDown(e);
                    break;
            }

        }

        //private readonly List<MouseEventHandler> mouseDown = new List<MouseEventHandler>();
        private event MouseEventHandler MouseDown_Private;

        public event GraphicalListDragEventHandler GraphicalListDragEnter;
        public event GraphicalListDragEventHandler GraphicalListDragOver;
        public event EventHandler GraphicalListDragLeave;
        public event GraphicalListDragEventHandler GraphicalListDragDrop;

        protected override void OnDragEnter(DragEventArgs drgevent)
        {
            if (GraphicalListDragEnter != null)
            {
                var e = new GraphicalListDragEventArgs(drgevent);
                GraphicalListDragEnter(this, e);
                e.SetTo(drgevent);
            }
            base.OnDragEnter(drgevent);
        }

        protected override void OnDragOver(DragEventArgs drgevent)
        {
            if (GraphicalListDragOver != null)
            {
                var p = PointToClient(new Point(drgevent.X, drgevent.Y));
                var hit = HitTest(p.X, p.Y);
                var e = new GraphicalListDragEventArgs(drgevent, hit.RowIndex);
                GraphicalListDragOver(this, e);
                e.SetTo(drgevent);
            }
            base.OnDragOver(drgevent);
        }

        protected override void OnDragLeave(EventArgs e)
        {
            GraphicalListDragLeave?.Invoke(this, e);
            base.OnDragLeave(e);
        }

        protected override void OnDragDrop(DragEventArgs drgevent)
        {
            if (GraphicalListDragDrop != null)
            {
                var p = PointToClient(new Point(drgevent.X, drgevent.Y));
                var hit = HitTest(p.X, p.Y);
                var e = new GraphicalListDragEventArgs(drgevent, hit.RowIndex);
                GraphicalListDragDrop(this, e);
                e.SetTo(drgevent);
            }
            base.OnDragDrop(drgevent);
        }

        public new event MouseEventHandler MouseDown
        {
            add
            {
                //mouseDown.Add(value);
                MouseDown_Private += value;
                base.MouseDown += value;
            }
            remove
            {
                //mouseDown.Remove(value);
                MouseDown_Private -= value;
                base.MouseDown -= value;
            }
        }

        private static Rectangle GetRectangleWithCenter(Point center, Size size)
        {
            var w = size.Width;
            var h = size.Height;
            return new Rectangle(center.X - w / 2, center.Y - h / 2, w, h);
        }

        protected override void OnQueryContinueDrag(QueryContinueDragEventArgs e)
        {
            if (((Keys)e.KeyState & (Keys.LButton | Keys.RButton)) == (Keys.LButton | Keys.RButton))
            {
                e.Action = DragAction.Cancel;
            }
            base.OnQueryContinueDrag(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (!OnMouseDown_DragOutStartRectangle.IsEmpty)
            {
                OnMouseDown_DragOutStartRectangle = Rectangle.Empty;

                //foreach (var action in mouseDown) base.MouseDown -= action;
                //OnMouseClick(e);
                //foreach (var action in mouseDown) base.MouseDown += action;


                var hit = HitTest(e.X, e.Y);
                var index = hit.RowIndex;
                var modifier = SimplifiedKeyBoard == null ? ModifierKeys : SimplifiedKeyBoard.GetModifierKeys(upHeldKeys: false);
                if (e.Button == MouseButtons.Left)
                {
                    switch (modifier)
                    {
                        case Keys.Control:
                            if (index >= 0) Rows[index].Selected = !Rows[index].Selected;
                            break;
                        case Keys.Shift:
                            if (index >= 0)
                            {
                                //var f = GetCurrentIndex == null ? CurrentCell?.RowIndex : GetCurrentIndex();
                                var f = CurrentCell?.RowIndex;
                                if (f != null)
                                {
                                    var f2 = (int)f;
                                    var min = Math.Min(index, f2);
                                    var max = Math.Max(index, f2);
                                    for (var i = 0; i < min; i++) Rows[i].Selected = false;
                                    for (var i = min; i <= max; i++) Rows[i].Selected = true;
                                    var count = Rows.Count;
                                    for (var i = max + 1; i < count; i++) Rows[i].Selected = false;
                                }
                            }
                            break;
                        case Keys.Shift | Keys.Control:
                            if (index >= 0)
                            {
                                //var f = GetCurrentIndex == null ? CurrentCell?.RowIndex : GetCurrentIndex();
                                var f = CurrentCell?.RowIndex;
                                if (f != null)
                                {
                                    var f2 = (int)f;
                                    var min = Math.Min(index, f2);
                                    var max = Math.Max(index, f2);
                                    //for (var i = min; i <= max; i++) Rows[i].Selected = true; // 追加
                                    //for (var i = min; i <= max; i++) Rows[i].Selected = !Rows[i].Selected; // 反転
                                    for (var i = min; i <= max; i++) Rows[i].Selected = false; // 取り除き
                                }
                            }
                            break;
                        default:
                            {
                                var count = Rows.Count;
                                for (var i = 0; i < count; i++) Rows[i].Selected = i == index;
                            }
                            break;
                    }
                }

                DragAttempted?.Invoke(this, e);
            }
            OnMouseDown_DragOutStartIndex = -1;
            //if (index >= 0 && hit.ColumnIndex >= 0)
            //{
                //SetCurrentCellAddressCore(hit.ColumnIndex, index, setAnchorCellAddress: false, validateCurrentCell: false, throughMouseClick: false);
            //}
            
            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (DragOutSelection && !OnMouseDown_DragOutStartRectangle.IsEmpty)
            {
                if (!OnMouseDown_DragOutStartRectangle.Contains(e.Location))
                {
                    OnMouseDown_DragOutStartRectangle = Rectangle.Empty;
                    DragReady?.Invoke(this, e);
                }
            }
            else if (DragOutSingle && OnMouseDown_DragOutStartIndex >= 0)
            {
                //if (!ClientRectangle.Contains(e.Location))
                if (!ActivateManager.InVisibleRegion(this, e.Location))
                    {
                    var count = Rows.Count;
                    if (count > OnMouseDown_DragOutStartIndex)
                    {
                        for (var i = 0; i < count; i++) Rows[i].Selected = i == OnMouseDown_DragOutStartIndex;
                        DragReady?.Invoke(this, e);
                    }
                    OnMouseDown_DragOutStartIndex = -1;
                }
            }

            /*
            ★ のところで MoveLocation を呼び出し、
            移動後マウスポインタの下に行が存在し、
            移動後の列数が移動前の列番号以下である場合に
            ArgumentOutOfRangeException が投げられる
            */
            try
            {
                base.OnMouseMove(e);
            }
            catch (ArgumentOutOfRangeException) { }
        }

        private int highlightIndex = -1;

        private static readonly Color HighlightBackColor = SystemColors.Highlight;
        public static readonly Color HighlightForeColoor = SystemColors.HighlightText;

        public void DrawHighlight(int index)
        {
            if (index < 0 || index >= RowCount) return;
            highlightIndex = index;
            InvalidateRow(index);
        }
        protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
        {
            if (highlightIndex < 0 || e.RowIndex != highlightIndex)
            {
                base.OnCellPainting(e);
            }
            else
            {
                e.CellStyle.SelectionBackColor = e.CellStyle.BackColor = HighlightBackColor;
                e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor = HighlightForeColoor;
                base.OnCellPainting(e);
            }
        }

        public void Invalidate(int index)
        {
            if (index < 0 || index >= RowCount) return;
            highlightIndex = -1;
            InvalidateRow(index);
        }

        protected override void OnCellValidating(DataGridViewCellValidatingEventArgs e)
        {
            if (Program.CancelMovingeEditingCellByLeftOrRightKey(this, e)) return;

            base.OnCellValidating(e);
        }
    }
}
