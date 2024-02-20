using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public static class ControlPainter
    {
        public static void FillCross(ToolStripItem toolStripItem, PaintEventArgs e)
        {
            FullScaledFillPolygon(toolStripItem, e, Cross);
        }

        private static void FullScaledFillPolygon(ToolStripItem toolStripItem, PaintEventArgs e, IEnumerable<PointF> scaledPoints)
        {
            if (toolStripItem == null) return;
            //var fixedRectangle = toolStripItem.ContentRectangle;
            var fixedRectangle = new Rectangle(0, 0, toolStripItem.Width, toolStripItem.Height);
            //FullScaledFillPolygon(GetBrush(toolStripItem.Enabled), toolStripItem.ContentRectangle, e, scaledPoints, -0.5f, -0.5f);
            FullScaledFillPolygon(GetBrush(toolStripItem.Enabled), fixedRectangle, e, scaledPoints, -0.5f, -0.5f);
        }

        private static Pen GetPen(ToolStripItem toolStripItem)
        {
            return toolStripItem.Enabled ? SystemPens.ControlText : Pens.LightGray; //SystemBrushes.GrayText;
        }

        public static void FillLeftArrowControlHandler(object sender, PaintEventArgs e)
        {
            FullScaledFillPolygon(sender as Control, e, LeftArrow);
        }
        public static void FillLeftArrow(Control control, PaintEventArgs e)
        {
            FullScaledFillPolygon(control, e, LeftArrow);
        }
        public static void FillRightArrowControlHandler(object sender, PaintEventArgs e)
        {
            FullScaledFillPolygon(sender as Control, e, RightArrow);
        }
        public static void FillRightArrow(Control control, PaintEventArgs e)
        {
            FullScaledFillPolygon(control, e, RightArrow);
        }
        
        public static void FillLeftTriangle(Control control, PaintEventArgs e)
        {
            FullScaledFillPolygon(control, e, LeftTriangle);
        }
        public static void FillRightTriangle(Control control, PaintEventArgs e)
        {
            FullScaledFillPolygon(control, e, RightTriangle);
        }

        private static void FullScaledFillPolygon(Control control, PaintEventArgs e, IEnumerable<PointF> scaledPoints)
        {
            if (control == null) return;
            FullScaledFillPolygon(GetBrush(control.Enabled), control.ClientRectangle, e, scaledPoints, -1, -1);
        }

        private static Brush GetBrush(bool enabled)
        {
            return enabled ? SystemBrushes.ControlText : Brushes.LightGray; //SystemBrushes.GrayText;
        }

        private static void FullScaledFillPolygon(Brush b, Rectangle r, PaintEventArgs e, IEnumerable<PointF> scaledPoints, float offsetX, float offsetY)
        {
            if (e == null || scaledPoints == null) return;
            var g = e.Graphics;
            int x = r.X, y = r.Y, w = r.Width, h = r.Height;
            var points = (from p in scaledPoints select new PointF(x + p.X * w + offsetX, y + p.Y * h + offsetY)).ToArray();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.FillPolygon(b, points);
        }

        private static readonly PointF[] RightArrow, LeftArrow, RightTriangle, LeftTriangle, Cross;
        static ControlPainter()
        {
            RightArrow = new PointF[]
            {
                new PointF(0.85f, 0.50f),
                new PointF(0.50f, 0.85f),
                new PointF(0.40f, 0.75f),
                new PointF(0.60f, 0.55f),
                new PointF(0.15f, 0.55f),
                new PointF(0.15f, 0.45f),
                new PointF(0.60f, 0.45f),
                new PointF(0.40f, 0.25f),
                new PointF(0.50f, 0.15f),
            };
            LeftArrow = (from p in RightArrow select new PointF(1 - p.X, p.Y)).ToArray();

            RightTriangle = new PointF[]
            {
                new PointF(0.70f, 0.50f),
                new PointF(0.35f, 0.30f),
                new PointF(0.35f, 0.70f),
            };
            LeftTriangle = (from p in RightTriangle select new PointF(1 - p.X, p.Y)).ToArray();

            const float crossHorizontalWidth = 0.10f;
            const float crossVerticalMargin = 0.30f;
            const float crossHorizontalMargin = 0.25f;

            //const float crossVerticalWidth = crossHorizontalWidth / (1 - crossHorizontalWidth / (1 - 2 * crossHorizontalMargin));
            const float crossVerticalWidth = (0.5f - crossVerticalMargin) / ((0.5f - crossHorizontalMargin) / crossHorizontalWidth - 0.5f);
            Cross = new PointF[]
            {
                new PointF(crossHorizontalMargin, crossVerticalMargin),
                new PointF(crossHorizontalMargin + crossHorizontalWidth, crossVerticalMargin),
                new PointF(0.5f, 0.5f - crossVerticalWidth / 2),
                new PointF(1 - crossHorizontalMargin - crossHorizontalWidth, crossVerticalMargin),
                new PointF(1 - crossHorizontalMargin, crossVerticalMargin),
                new PointF(0.5f + crossHorizontalWidth / 2, 0.5f),
                new PointF(1 - crossHorizontalMargin, 1 - crossVerticalMargin),
                new PointF(1 - crossHorizontalMargin - crossHorizontalWidth, 1 - crossVerticalMargin),
                new PointF(0.5f, 0.5f + crossVerticalWidth / 2),
                new PointF(crossHorizontalMargin + crossHorizontalWidth, 1 - crossVerticalMargin),
                new PointF(crossHorizontalMargin, 1 - crossVerticalMargin),
                new PointF(0.5f - crossHorizontalWidth / 2, 0.5f),
            };
        }

        private enum ButtonState { Disabled, Normal, Entered, Pushed}
        private static ButtonState GetButtonState(Button button)
        {
            if (!button.Enabled) return ButtonState.Disabled;
            if (!button.Bounds.Contains(button.PointToClient(Cursor.Position))) return ButtonState.Normal;
            if (Control.MouseButtons != MouseButtons.Left) return ButtonState.Entered;
            return ButtonState.Pushed;
        }
    }
}
