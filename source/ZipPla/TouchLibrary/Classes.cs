#if !AUTOBUILD
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TouchLibrary.Core;
using TouchLibrary.TouchWindow;

namespace TouchLibrary
{
    public struct PointD
    {
        public double X, Y;
        public PointD(double x, double y) { X = x; Y = y; }

        public static MotionVector operator -(PointD a, PointD b)
        {
            return new MotionVector(a.X - b.X, a.Y - b.Y);
        }
    }

    public struct MotionVector
    {
        public double X, Y;
        public MotionVector(double x, double y) { X = x; Y = y; }

        public static MotionVector operator -(MotionVector a, MotionVector b)
        {
            return new MotionVector(a.X - b.X, a.Y - b.Y);
        }
    }

    public struct SizeD
    {
        double Width, Height;
        public SizeD(double width, double height) { Width = width; Height = height; }
    }

    public static class CastHelper
    {
        public static int Round(double value)
        {
            return (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, Math.Round(value)));
        }
    }
}
#endif
