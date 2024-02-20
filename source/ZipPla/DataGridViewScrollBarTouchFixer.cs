using Alteridem.WinTouch;
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
    public class DataGridViewScrollBarTouchFixer
    {
        private DataGridView dataGridView;
        private ScrollBar verticalScrollBar;
        private ScrollBar horizontalScrollBar;
        private GestureListener gestureListener;

        public DataGridViewScrollBarTouchFixer(DataGridView dataGridView, GestureListener gestureListener)
        {
            this.dataGridView = dataGridView;
            this.gestureListener = gestureListener;
            verticalScrollBar = GetVerticalScrollBar(dataGridView);
            horizontalScrollBar = GetHorizontalScrollBar(dataGridView);
            
            gestureListener.Pan += gestureListener_Pan;
            dataGridView.Disposed += dataGridView_Disposed;
        }

        public static GestureListener GetGestureListener(Form ownerForm)
        {
            return new GestureListener(ownerForm, new GestureConfig[] {
                    //new GestureConfig(3, 1, 0), // ズーム
                    new GestureConfig(4, 2 | 4 | 16 , 8 ), // パン、向き拘束なし
                    
                    //new GestureConfig(5, 1, 0), // 回転
                    //new GestureConfig(6, 1, 0), // ツーフィンガータップ
                    //new GestureConfig(7, 1, 0), // プレスアンドタップ

                });
        }

        private ScrollBar gestureListener_Pan_HoldedScrollBar = null;
        private int gestureListener_Pan_InitialCoordinate;
        private int gestureListener_Pan_InitialValue;

        private void gestureListener_Pan(object sender, PanEventArgs e)
        {
            if (e.Handled) return;
            if (e.Begin)
            {
                var location = e.Location;
                ScrollBar scrollBar;
                var clientPoint = default(Point);
                if ((scrollBar = verticalScrollBar).Visible && scrollBar.ClientRectangle.Contains(clientPoint = scrollBar.PointToClient(location)))
                {
                    e.Handled = true;
                    gestureListener_Pan_HoldedScrollBar = scrollBar;
                    gestureListener_Pan_InitialCoordinate = clientPoint.Y;
                    gestureListener_Pan_InitialValue = dataGridView.FirstDisplayedScrollingRowIndex;
                }
                else if ((scrollBar = horizontalScrollBar).Visible && scrollBar.ClientRectangle.Contains(clientPoint = scrollBar.PointToClient(location)))
                {
                    e.Handled = true;
                    gestureListener_Pan_HoldedScrollBar = scrollBar;
                    gestureListener_Pan_InitialCoordinate = clientPoint.X;
                    gestureListener_Pan_InitialValue = dataGridView.HorizontalScrollingOffset;
                }
                else
                {
                    e.Handled = false;
                    gestureListener_Pan_HoldedScrollBar = null;
                }
            }
            else
            {
                if (e.Handled = gestureListener_Pan_HoldedScrollBar != null)
                {
                    if (!e.End)
                    {
                        if (!e.Inertia)
                        {
                            var scrollBar = gestureListener_Pan_HoldedScrollBar;
                            var clientPoint = scrollBar.PointToClient(e.Location);
                            if (scrollBar == verticalScrollBar)
                            {
                                if (gestureListener_Pan_InitialValue >= 0)
                                {
                                    var rowCount = dataGridView.RowCount;
                                    var barHeight = scrollBar.Height;
                                    if (rowCount > 0 && barHeight > 0)
                                    {
                                        var currentCoordinate = clientPoint.Y;
                                        var deltaCoordinate = currentCoordinate - gestureListener_Pan_InitialCoordinate;
                                        dataGridView.FirstDisplayedScrollingRowIndex = Math.Max(0, Math.Min(rowCount - 1,
                                            gestureListener_Pan_InitialValue + roundDiv(deltaCoordinate * rowCount, barHeight)));
                                    }
                                }
                            }
                            else
                            {
                                var width = (from DataGridViewColumn col in dataGridView.Columns where col.Visible select col.Width).Sum();
                                var barWidth = scrollBar.Width;
                                var currentCoordinate = clientPoint.X;
                                var deltaCoordinate = currentCoordinate - gestureListener_Pan_InitialCoordinate;
                                dataGridView.HorizontalScrollingOffset = gestureListener_Pan_InitialValue + roundDiv(deltaCoordinate * width, barWidth);
                            }
                        }
                    }
                    else
                    {
                        gestureListener_Pan_HoldedScrollBar = null;
                    }
                }
            }
        }

        private int roundDiv(int a, int b)
        {
            return (int)Math.Round(Math.Max(int.MinValue, Math.Min(int.MaxValue, (double)a / b)));
        }

        private void dataGridView_Disposed(object sender, EventArgs e)
        {
            gestureListener.Pan -= gestureListener_Pan;
        }
        
        private static readonly PropertyInfo VerticalScrollBarInfo = typeof(DataGridView).GetProperty("VerticalScrollBar", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly PropertyInfo HorizontalScrollBarInfo = typeof(DataGridView).GetProperty("HorizontalScrollBar", BindingFlags.NonPublic | BindingFlags.Instance);
        private static ScrollBar GetVerticalScrollBar(DataGridView dataGridView) { return (ScrollBar)VerticalScrollBarInfo.GetValue(dataGridView); }
        private static ScrollBar GetHorizontalScrollBar(DataGridView dataGridView) { return (ScrollBar)HorizontalScrollBarInfo.GetValue(dataGridView); }

        public static Rectangle GetClientRectangleWithoutScrollBars(DataGridView dataGridView)
        {
            var verticalScrollBar = GetVerticalScrollBar(dataGridView);
            var horizontalScrollBar = GetHorizontalScrollBar(dataGridView);
            var clientRectangle = dataGridView.ClientRectangle;
            if (verticalScrollBar.Visible) clientRectangle.Width -= verticalScrollBar.Width;
            if (horizontalScrollBar.Visible) clientRectangle.Height -= horizontalScrollBar.Height;
            return clientRectangle;
        }

    }
}
