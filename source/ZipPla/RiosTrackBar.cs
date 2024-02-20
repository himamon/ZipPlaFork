using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public enum KnobDirection { Top, Bottom, Left, Right }

    public class myTrackBar : Control, ISupportInitialize
    {
        public event EventHandler ValueChanged;
        private int value;
        public int Value
        {
            get
            {
                return value;
            }
            set
            {
                if (this.value != value)
                {
                    using (var g = CreateGraphics())
                    {
                        redrawKnob(g, value);
                        this.value = value;
                    }
                    ValueChanged?.Invoke(this, new EventArgs());
                }
            }
        }
        private int minimum;
        public int Minimum
        {
            get
            {
                return minimum;
            }
            set
            {
                if(minimum != value)
                {
                    minimum = value;
                    Invalidate();
                }
            }
        }
        private int maximum;
        public int Maximum
        {
            get
            {
                return maximum;
            }
            set
            {
                if (maximum != value)
                {
                    maximum = value;
                    Invalidate();
                }
            }
        }

        public int ControlThickness
        {
            get
            {
                if (KnobDirection == KnobDirection.Top || KnobDirection == KnobDirection.Bottom) return Height;
                else return Width;
            }
        }
        public int ControlLength
        {
            get
            {
                if (KnobDirection == KnobDirection.Top || KnobDirection == KnobDirection.Bottom) return Width;
                else return Height;
            }
        }
        public KnobDirection KnobDirection { get; set; } = KnobDirection.Top;
        

        #region デザイン
        private bool inversedSlider;
        public bool InversedSlider
        {
            get
            {
                return inversedSlider;
            }
            set
            {
                if (value != inversedSlider)
                {
                    using (var g = CreateGraphics())
                    {
                        if (maximum == minimum)
                        {
                            eraseKnob(g);
                            drawScaleMarks(g, erase: true);
                            inversedSlider = value;
                            drawScaleMarks(g);
                            drawKnob(g);
                        }
                        else
                        {
                            redrawKnob(g, maximum - this.value + minimum);
                            inversedSlider = value;
                        }
                    }
                }
            }
        }
        public float CenterLineFrontMarginPerControlThickness { get; set; } = 0.45f;
        public float CenterLineBackMarginPerControlThickness { get; set; } = 0.45f;
        public float CenterLineSideMarginPerControlThickness { get; set; } = 0.3f;
        private float ScaleMarkFrontMarginPerControlThickness { get; set; } = 0.1f;
        private float ScaleMarkLengthPerControlThickness { get; set; } = 0.05f;
        public PointF[] KnobShape { get; set; } = new PointF[]
        {
            new PointF(0, 0.25f),
            new PointF(0.15f, 0.4f),
            new PointF(0.15f, 0.8f),
            new PointF(-0.15f, 0.8f),
            new PointF(-0.15f, 0.4f)
        };
        private RectangleF KnobBound = new RectangleF(-0.15f, 0.25f, 0.3f, 0.55f);
        //private Region KnobRegion = new Region(new RectangleF(-0.15f, 0.25f, 0.3f, 0.55f));
        private Color centerLineColor = Color.Black;
        private Pen centerLinePen = SystemPens.ActiveBorder;
        private Pen scaleMarkPen = SystemPens.ControlDarkDark;
        private Brush scaleMarkBrush = SystemBrushes.ControlDark;
        private Brush fixedKnobBrush = SystemBrushes.Highlight;
        private Brush normalKnobBrush = SystemBrushes.Highlight;
        private Brush enteredKnobBrush = Brushes.Black;
        private Brush holdedKnobBrush = SystemBrushes.ControlDark;
        private Brush currentKnobBrush
        {
            get
            {
                if (minimum != maximum)
                {
                    switch (CurrentKnobState)
                    {
                        case KnobState.Normal: return normalKnobBrush;
                        case KnobState.Enter: return enteredKnobBrush;
                        case KnobState.Hold: return holdedKnobBrush;
                        default: throw new NotImplementedException();
                    }
                }
                else return fixedKnobBrush;
            }
        }
        #endregion

        public myTrackBar() : base()
        {
            // ダブルバッファリングによる描画の高速化
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            setBackBrush();
        }

        public int PointToValue(Point clientPoint)
        {
            switch(KnobDirection)
            {
                case KnobDirection.Top:
                    return FloatToValue(clientPoint.X);
                default:
                    throw new NotImplementedException();
            }
        }

        public bool ValueCanChangedByMouseButton = true;

        enum KnobState { Normal, Enter, Hold }
        KnobState CurrentKnobState = KnobState.Normal;
        protected override void OnMouseDown(MouseEventArgs e)
        {
            if(ValueCanChangedByMouseButton && e.Button == MouseButtons.Left)
            {
                CurrentKnobState = KnobState.Hold;
                var value = PointToValue(e.Location);
                if(value != Value)
                {
                    Value = value;
                }
                else
                {
                    using (var g = CreateGraphics()) drawKnob(g, overWrite: true);
                }
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Value = PointToValue(e.Location);
            }
            else
            {
                if(ValueToRectangleF().Contains(e.Location))
                {
                    if(CurrentKnobState != KnobState.Enter)
                    {
                        CurrentKnobState = KnobState.Enter;
                        using (var g = CreateGraphics()) drawKnob(g, overWrite: true);
                    }
                }
                else
                {
                    if (CurrentKnobState != KnobState.Normal)
                    {
                        CurrentKnobState = KnobState.Normal;
                        using (var g = CreateGraphics()) drawKnob(g, overWrite: true);
                    }
                }
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (ValueToRectangleF().Contains(e.Location))
            {
                if (CurrentKnobState != KnobState.Enter)
                {
                    CurrentKnobState = KnobState.Enter;
                    using (var g = CreateGraphics()) drawKnob(g, overWrite: true);
                }
            }
            else
            {
                if (CurrentKnobState != KnobState.Normal)
                {
                    CurrentKnobState = KnobState.Normal;
                    using (var g = CreateGraphics()) drawKnob(g, overWrite: true);
                }
            }
            base.OnMouseUp(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (CurrentKnobState == KnobState.Enter)
            {
                CurrentKnobState = KnobState.Normal;
                using (var g = CreateGraphics()) drawKnob(g, overWrite: true);
            }
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clip = new Region(e.ClipRectangle);
            drawScaleMarks(g);
            drawCenterLine(g);
            drawKnob(g);

            base.OnPaint(e);
        }

        private void drawCenterLine(Graphics g, Point offset = default(Point))
        {
            float leftPerInversedSlider, rightPerInversedSlider, topPerInversedSlider, bottomPerInversedSlider;
            switch (KnobDirection)
            {
                case KnobDirection.Top:
                    leftPerInversedSlider = rightPerInversedSlider = CenterLineSideMarginPerControlThickness;
                    topPerInversedSlider = CenterLineFrontMarginPerControlThickness;
                    bottomPerInversedSlider = CenterLineBackMarginPerControlThickness;
                    break;
                default:
                    throw new NotImplementedException();
            }

            var controlThickness = ControlThickness;
            g.DrawRectangle(centerLinePen,
                leftPerInversedSlider * controlThickness - offset.X, topPerInversedSlider * controlThickness - offset.Y,
                Width - (leftPerInversedSlider + rightPerInversedSlider) * controlThickness,
                Height - (topPerInversedSlider + bottomPerInversedSlider) * controlThickness);
        }

        private void drawScaleMarks(Graphics g, bool erase = false)
        {
            var controlThickness = ControlThickness;
            var controlLength = ControlLength;
            var side = CenterLineSideMarginPerControlThickness * controlThickness;
            var centerLineLength = controlLength - 2 * side;

            var uniqueValue = maximum == minimum;
            var denom = uniqueValue ? 1 :  maximum - minimum;
            switch (KnobDirection)
            {
                case KnobDirection.Top:
                    {
                        var y1f = controlThickness * ScaleMarkFrontMarginPerControlThickness;
                        var y1 =(int)y1f;
                        var y2 = (int)(y1f + controlThickness * ScaleMarkLengthPerControlThickness);

                        if (centerLineLength >= 2 * denom)
                        {
                            var bound = g.ClipBounds;
                            var start = Math.Max(0, (int)Math.Ceiling((bound.Left - side) * denom / centerLineLength));
                            var stop = Math.Min(denom, (int)Math.Floor((bound.Right - side) * denom / centerLineLength));

                            if(uniqueValue)
                            {
                                if (inversedSlider) start = denom;
                                else stop = 0;
                            }

                            if (erase)
                            {
                                for (var i = start; i <= stop; i++)
                                {
                                    var p = (int)(side + centerLineLength * i / denom);
                                    g.DrawLine(BackPen, p, y1, p, y2);
                                }
                            }
                            else
                            {
                                for (var i = start; i <= stop; i++)
                                {
                                    var p = (int)(side + centerLineLength * i / denom);
                                    g.DrawLine(scaleMarkPen, p, y1, p, y2);
                                }
                            }
                        }
                        else
                        {
                            var rect = g.ClipBounds;
                            //var rect = new Rectangle((int)(rectF.X - 1), (int)(rectF.Y - 1), (int)(rectF.Width + 2), (int)(rectF.Height + 2));
                            //var x1 = (int)side;
                            //var x2 = (int)(side + centerLineLength);
                            rect.Intersect(new RectangleF(side, y1, centerLineLength, y2 - y1 + 1));

                            if (erase)
                            {
                                g.FillRectangle(BackBrush, rect);
                            }
                            else
                            {
                                g.FillRectangle(scaleMarkBrush, rect);
                            }
                            
                        }
                        break;
                    }
                default:
                    throw new NotImplementedException();
            }
        }


        private void drawKnob(Graphics g, bool overWrite = false)
        {
            if (overWrite && g.SmoothingMode != System.Drawing.Drawing2D.SmoothingMode.None) eraseKnob(g);
            drawKnob(g, ValueToPointF());
        }
        private void drawKnob(Graphics g, PointF knobCenter)
        {
            if (KnobShape == null) return;
            PointF[] locatedKnobShape;
            var x0 = knobCenter.X;
            var y0 = knobCenter.Y;
            var controlThickness = ControlThickness;
            switch (KnobDirection)
            {
                case KnobDirection.Top:
                    locatedKnobShape = (from r in KnobShape
                                        select new PointF(x0 + r.X * controlThickness,
                                        y0 + (r.Y - 0.5f)* controlThickness)).ToArray();
                    break;
                default:
                    throw new NotImplementedException();
            }

            //var temp = g.SmoothingMode;
            //g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.FillPolygon(currentKnobBrush, locatedKnobShape);
            //g.SmoothingMode = temp;
        }
        
        private void redrawKnob(Graphics g, int newValue)
        {
            if (value != newValue)
            {
                var oldRect = ValueToRectangleF(1f);
                var newRect = ValueToRectangleF(newValue, 1f);
                if (oldRect.IntersectsWith(newRect))
                {
                    var unionLeft = (int)Math.Floor(Math.Min(oldRect.Left, newRect.Left));
                    var unionRight = (int)Math.Ceiling(Math.Max(oldRect.Right, newRect.Right));
                    var unionTop = (int)Math.Floor(Math.Min(oldRect.Top, newRect.Top));
                    var unionBottom = (int)Math.Ceiling(Math.Max(oldRect.Bottom, newRect.Bottom));
                    var unionWidth = unionRight - unionLeft;
                    var unionHeight = unionBottom - unionTop;

                    // 途中に DoEvent が発生することがあるため canvas の使い回しは不可
                    using (var canvas = new Bitmap(unionWidth, unionHeight))
                    {
                        var newPoint = ValueToPointF(newValue);
                        newPoint.X -= unionLeft;
                        newPoint.Y -= unionTop;

                        var offset = new Point(unionLeft, unionTop);

                        using (var g2 = Graphics.FromImage(canvas))
                        {
                            eraseKnob(g2, oldRect, offset);
                            drawKnob(g2, newPoint);
                        }

                        g.DrawImageUnscaledAndClipped(canvas, new Rectangle(unionLeft, unionTop, unionWidth, unionHeight));
                    }
                }
                else
                {
                    eraseKnob(g, oldRect);
                    drawKnob(g, ValueToPointF(newValue));
                }
            }

        }

        private Brush BackBrush;
        private Pen BackPen;
        protected override void OnBackColorChanged(EventArgs e)
        {
            setBackBrush();
            base.OnBackColorChanged(e);
        }

        private void setBackBrush()
        {
            BackBrush?.Dispose();
            BackBrush = new SolidBrush(BackColor);
            BackPen?.Dispose();
            BackPen = new Pen(BackColor);
        }

        private void eraseKnob(Graphics g, RectangleF knobBound = default(RectangleF), Point offset = default(Point))
        {
            var originalClip = g.Clip;
            if(knobBound == default(RectangleF)) knobBound = ValueToRectangleF(1f);
            if (!offset.IsEmpty)
            {
                knobBound.X -= offset.X;
                knobBound.Y -= offset.Y;
            }
            knobBound.Intersect(g.ClipBounds);
            g.FillRectangle(BackBrush, knobBound);
            g.Clip = new Region(knobBound);
            drawCenterLine(g, offset);
            g.Clip = originalClip;
        }
        

        private RectangleF ValueToRectangleF(float margin = 0)
        {
            return ValueToRectangleF(value, margin);
        }
        private RectangleF ValueToRectangleF(int value, float margin = 0)
        {
            var controlThickness = ControlThickness;
            var margin2 = margin + 2;
            switch (KnobDirection)
            {
                case KnobDirection.Top:
                    {
                        var knobBound = KnobBound;
                        return new RectangleF(knobBound.X * controlThickness+ ValueToFloat(value) - margin,
                            knobBound.Y * controlThickness - margin,
                            knobBound.Width * controlThickness + margin2,
                            knobBound.Height * controlThickness + margin2);
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        private PointF ValueToPointF()
        {
            return ValueToPointF(value);
        }
        private PointF ValueToPointF(int value)
        {
            switch (KnobDirection)
            {
                case KnobDirection.Top:
                case KnobDirection.Bottom:
                    return new PointF(ValueToFloat(value), ControlThickness / 2);
                default:
                    return new PointF(ControlThickness / 2, ValueToFloat(value));
            }
        }

        private float ValueToFloat(int value)
        {
            var controlThickness = ControlThickness;
            var controlLength = ControlLength;
            var side = CenterLineSideMarginPerControlThickness * controlThickness;
            var length = controlLength - 2 * side;
            float p;
            if (Maximum != Minimum)
            {
                p = side + length * (value - Minimum) / (Maximum - Minimum);
            }
            else
            {
                p = side;
            }
            if (inversedSlider)
            {
                p = controlLength - p;
            }
            return p;
        }

        private int FloatToValue(float p)
        {
            if (maximum == minimum) return maximum;
            
            var controlThickness = ControlThickness;
            var controlLength = ControlLength;
            var side = CenterLineSideMarginPerControlThickness * controlThickness;
            var length = controlLength - 2 * side;

            if (inversedSlider)
            {
                p = controlLength - p;
            }

            var value = ((p - side) * (maximum - minimum)) / length + minimum;

            return Math.Min(maximum, Math.Max(minimum, (int)Math.Round(value)));
        }

        private int OnSizeChanged_LastWidth = int.MinValue;
        protected override void OnSizeChanged(EventArgs e)
        {
            var width = Width;
            if(OnSizeChanged_LastWidth != width)
            {
                OnSizeChanged_LastWidth = width;
                Invalidate();
            }
            base.OnSizeChanged(e);
        }

        #region 未使用
        /// <summary>
        /// 未使用
        /// </summary>
        public int LargeChange { get; set; }

        /// <summary>
        /// 未使用
        /// </summary>
        public TickStyle TickStyle { get; set; }
#endregion

#region ISupportInitialize
        //private bool initializing = false;
        public void BeginInit()
        {
            //initializing = true;
        }

        public void EndInit()
        {
            //initializing = false;
        }
#endregion
    }
}
