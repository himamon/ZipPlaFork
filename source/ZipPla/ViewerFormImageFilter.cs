using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public struct DihedralFraction
    {
        private int numerator;
        private int denominator;

        public int SignedDenominator { get { return denominator; } }

        public DihedralFraction(int numerator, int denominator)
        {
            this.numerator = numerator;
            this.denominator = denominator;
            reduction();
        }

        public DihedralFraction(int numerator, int denominator, bool reflection)
            : this(numerator, reflection ? -denominator : denominator) { }

        public static readonly DihedralFraction Identity = new DihedralFraction { numerator = 0, denominator = 1 };

        public static DihedralFraction operator *(DihedralFraction a, DihedralFraction b)
        {
            a.fix();
            b.fix();
            var result = new DihedralFraction
            {
                numerator = a.numerator * Math.Abs(b.denominator) + b.numerator * a.denominator,
                denominator = a.denominator * b.denominator
            };
            result.reduction();
            return result;
        }

        public static DihedralFraction operator /(DihedralFraction a, DihedralFraction b)
        {
            a.fix();
            b.fix();
            var result = new DihedralFraction
            {
                numerator = Math.Sign(b.denominator) * (a.numerator * b.denominator - b.numerator * a.denominator),
                denominator = a.denominator * b.denominator
            };
            result.reduction();
            return result;
        }

        public static bool operator ==(DihedralFraction a, DihedralFraction b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(DihedralFraction a, DihedralFraction b)
        {
            return !a.Equals(b);
        }

        public override bool Equals(object obj)
        {
            if (obj is DihedralFraction)
            {
                return Equals((DihedralFraction)obj);
            }
            else
            {
                return false;
            }
        }
        
        public bool Equals(DihedralFraction other)
        {
            fix();
            other.fix();
            return numerator == other.numerator && denominator == other.denominator;
        }

        public override int GetHashCode()
        {
            return numerator ^ denominator;
        }

        private void reduction()
        {
            var g = Math.Abs(gcd(numerator, denominator));
            numerator /= g;
            denominator /= g;
            numerator %= denominator;
            if (numerator < 0) numerator += denominator;
        }

        public override string ToString()
        {
            fix();
            return denominator > 0 ? $"R{numerator} / D{denominator}" : $"S{numerator} / D{-denominator}";
        }

        private void fix()
        {
            if (denominator == 0) denominator = 1;
        }

        private static int gcd(int a, int b)
        {
            while (a != 0)
            {
                var temp = a;
                a = b % a;
                b = temp;
            }
            return b;
        }
    }

    public class ViewerFormImageFilter
    {
        public double RotationAngle = 0;
        public bool ToneCurveEnabled = false;
        //public bool MoireRemoverEnabled = false;
        //public PixelValueConversion PixelValueConversion = PixelValueConversion.None;

        //private static string PseudoColoringToneCurvesFile = Path.Combine(Application.StartupPath, "PseudoColoring.cur");
        private static string PseudoColoringToneCurvesFile = Path.Combine(Application.StartupPath, "ToneCurve.cur");

        public bool PreFilterExists() // プロパティにすると Config に保存される可能性
        {
            return RotationAngle != 0;
            //return !PseudoColoringEnabled && RotationAngle == 0;// && PixelValueConversion == PixelValueConversion.None;
        }
        public bool PostFilterExists() // プロパティにすると Config に保存される可能性
        {
            return ToneCurveEnabled;// || MoireRemoverEnabled;
            //return !PseudoColoringEnabled && RotationAngle == 0;// && PixelValueConversion == PixelValueConversion.None;
        }

        /*
        private bool PreFilterExistsPlain(bool? isColorImage) // プロパティにすると Config に保存される可能性
        {
            return (!PseudoColoringEnabled || isColorImage == true) && RotationAngle == 0;// && PixelValueConversion == PixelValueConversion.None;
        }
        */

        public bool NeedToCheckWhetherColorOrNot()
        {
            return ToneCurveEnabled;// || MoireRemoverEnabled;
        }

        public void Set(ViewerFormImageFilter other)
        {
            if (other == null)
            {
                RotationAngle = 0;
                ToneCurveEnabled = false;
                //MoireRemoverEnabled = false;
               // PixelValueConversion = PixelValueConversion.None;
            }
            else
            {
                RotationAngle = other.RotationAngle;
                ToneCurveEnabled = other.ToneCurveEnabled;
                //MoireRemoverEnabled = other.MoireRemoverEnabled;
               // PixelValueConversion = other.PixelValueConversion;
            }
        }

        public static Bitmap Rotate(Bitmap bmp, DihedralFraction rotate)
        {
            if (rotate == new DihedralFraction())
            {
                return bmp;
            }
            var data = bmp.LockBits(
                new Rectangle(0, 0, bmp.Width, bmp.Height),
                rotate == new DihedralFraction(1, 4) || rotate == new DihedralFraction(3, 4) ? ImageLockMode.ReadOnly : ImageLockMode.ReadWrite,
                bmp.PixelFormat);
            try
            {
                Rotate(ref bmp, ref data, rotate);
            }
            finally
            {
                bmp.UnlockBits(data);
            }
            return bmp;
        }

        private static void Rotate(ref Bitmap bmp, ref BitmapData data, DihedralFraction rotate)
        {
            if (rotate == new DihedralFraction())
            {
                return;
            }
            bool is90;
            if ((is90 = rotate == new DihedralFraction(3, 4)) || rotate == new DihedralFraction(1, 4))
            {
                Bitmap outBitmap;
                BitmapData outData;
                Rotate(data, is90 ? 90 : 270, out outBitmap, out outData);
                if (data != outData)
                {
                    bmp.UnlockBits(data);
                    if (outBitmap.PixelFormat == PixelFormat.Format8bppIndexed)
                    {
                        outBitmap.Palette = bmp.Palette;
                    }
                    bmp.Dispose();
                    data = outData;
                    bmp = outBitmap;
                }
                return;
            }
            if (rotate == new DihedralFraction(0, -4))
            {
                VerticalReflection(data);
                return;
            }
            if (rotate == new DihedralFraction(2, 4))
            {
                Rotate180(data);
                return;
            }
            if (rotate == new DihedralFraction(1, -4))
            {
                VerticalReflection(data);
                Rotate(ref bmp, ref data, new DihedralFraction(3, 4));
                return;
            }
            if (rotate == new DihedralFraction(2, -4))
            {
                Rotate180(data);
                VerticalReflection(data);
                return;
            }
            if (rotate == new DihedralFraction(3, -4))
            {
                VerticalReflection(data);
                Rotate(ref bmp, ref data, new DihedralFraction(1, 4));
                return;
            }
        }
        
        public static DihedralFraction GetOrientation(int orientation)
        {
            switch (orientation)
            {
                case 2: return new DihedralFraction(0, -4); // S0
                case 3: return new DihedralFraction(2, 4); // R2
                case 4: return new DihedralFraction(2, -4); // S2
                case 5: return new DihedralFraction(3, -4); // S0 * R1 = S3
                case 6: return new DihedralFraction(3, 4); // R3
                case 7: return new DihedralFraction(1, -4); // S0 * R3 = S1
                case 8: return new DihedralFraction(1, 4); // R1
                default: return new DihedralFraction();
            }
        }


        private static DihedralFraction GetOrientation(double angle)
        {
            return
                angle == 90 ? new DihedralFraction(3, 4) :
                angle == 180 ? new DihedralFraction(2, 4) :
                angle == 270 ? new DihedralFraction(1, 4) :
                new DihedralFraction();
        }

        public Bitmap PreFilter(Bitmap src)//, bool? isColorImage)
        {
            var rotate = GetOrientation(RotationAngle);
            return Rotate(src, rotate);
            //if (PreFilterExists())
            //if (rotate != new DihedralFraction())
            //{
                /*
                Bitmap outBitmap;
                BitmapData outData;
                var pixelFormat = src.PixelFormat; ;
                if (pixelFormat != PixelFormat.Format8bppIndexed) pixelFormat = PixelFormat.Format24bppRgb;
                var data = src.LockBits(new Rectangle(Point.Empty, src.Size), ImageLockMode.ReadWrite, pixelFormat);
                try
                {
                    if (RotationAngle == 180)
                    {
                        Rotate180(data);
                    }
                    else if (RotationAngle == 90 || RotationAngle == 270)
                    {
                        Rotate(data, RotationAngle, out outBitmap, out outData);
                        if (data != outData)
                        {
                            src.UnlockBits(data);
                            if (outBitmap.PixelFormat == PixelFormat.Format8bppIndexed)
                            {
                                outBitmap.Palette = src.Palette;
                            }
                            src.Dispose();
                            data = outData;
                            src = outBitmap;
                        }
                    }
*/
                /*
                if (PseudoColoringEnabled && isColorImage != true)
                {
                    if (isColorImage == false || IsGrayScaleProperly(data))
                    {
                        var toneCurve = TryGetToneCorveFile();
                        if (toneCurve == null)toneCurve = getDefaultPseudoColoringToneCurves();
                        ApplyToneCurveForPseudoColoring(toneCurve, data);
                    }
                }
                */
                /*
                switch(PixelValueConversion)
                {
                    case PixelValueConversion.Binarization:
                        Binarization(data);
                        break;
                    case PixelValueConversion.AutoContrastControl:
                        AutoContrastControl(data);
                        break;
                }
                *//*
        }
        finally
        {
            src.UnlockBits(data);
        }
            }*/
            //}
            //return src;
        }

        public void PostFilter(Bitmap src, ref int? maxColorDiff, BackgroundWorker bw = null)
        {
            PostFilter(src, new Rectangle(0, 0, src.Width, src.Height), ref maxColorDiff, bw);
        }
        public void PostFilter(Bitmap src, Rectangle rect, ref int? maxColorDiff, BackgroundWorker bw = null)
        {
            if ((ToneCurveEnabled /*|| MoireRemoverEnabled*/) && (maxColorDiff == null || maxColorDiff <= GrayScaleThreshold))
            {
                if (maxColorDiff != null && src.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    ApplayColorTransformTo8bpp(src);
                }
                else
                {
                    var changePalette = false;
                    var data = src.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                    try
                    {
                        // 他の PostFilter が追加されなければ if は不要
                        //if (PseudoColoringEnabled && (maxColorDiff == null || maxColorDiff <= GrayScaleThreshold))

                        if (maxColorDiff == null)
                        {
                            if (bw == null)
                            {
                                maxColorDiff = GetMaxColorDiffProperly(data);
                            }
                            else
                            {
                                var result = GetMaxColorDiffProperly(data, bw);
                                if (result == null) return; // キャンセルがかかった場合
                                maxColorDiff = result;
                            }
                        }

                        if (maxColorDiff <= GrayScaleThreshold)
                        {
                            /*
                            if (MoireRemoverEnabled)
                            {
                                MoireRemover(data, bw);
                            }
                            */

                            if (ToneCurveEnabled)
                            {
                                if (src.PixelFormat == PixelFormat.Format8bppIndexed)
                                {
                                    changePalette = true;
                                }
                                else
                                {
                                    var toneCurve = TryGetPseudoColoringToneCurves();
                                    using (var painter = toneCurve == null ? new PseudoColoringPainter() : new PseudoColoringPainter(toneCurve))
                                    {
                                        var transform = /*MoireRemoverEnabled ||*/ maxColorDiff == 0 ? painter.ColorTransformForGrayScale : painter.ColorTransform;
                                        if (bw == null)
                                        {
                                            ApplyColorTransform(transform, data);
                                        }
                                        else
                                        {
                                            ApplyColorTransform(transform, data, bw);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        src.UnlockBits(data);
                    }
                    if(changePalette)
                    {
                        ApplayColorTransformTo8bpp(src);
                    }
                }
            }
        }

        private static void ApplayColorTransformTo8bpp(Bitmap bmp)
        {
            var palette = bmp.Palette;
            var entries = palette.Entries;
            var toneCurve = TryGetPseudoColoringToneCurves();
            using (var painter = toneCurve == null ? new PseudoColoringPainter() : new PseudoColoringPainter(toneCurve))
            {
                var transform = painter.ColorTransformForGrayScale;
                var canvas = new byte[3];
                unsafe
                {
                    fixed (byte* p = canvas)
                    {
                        for (var i = 0; i < entries.Length; i++)
                        {
                            p[0] = (byte)i;
                            transform(p);
                            entries[i] = Color.FromArgb(p[2], p[1], p[0]);
                        }
                    }
                }
            }
            bmp.Palette = palette;
        }

        public static int? TryGetMaxColorDiff(Bitmap bmp)
        {
            var pixelFormat = bmp.PixelFormat;
            if (pixelFormat == PixelFormat.Format16bppGrayScale) return 0;

            if ((pixelFormat & PixelFormat.Indexed) != PixelFormat.Indexed) return null;

            const int Flag_IsGrayScale = 0x00000002;
            var palette = bmp.Palette;
            if ((palette.Flags & Flag_IsGrayScale) == Flag_IsGrayScale) return 0;
            
            var bpp = Image.GetPixelFormatSize(pixelFormat);
            var indexSize = 1 << bpp;
            var bmpPalette = palette.Entries;
            var stop = Math.Min(indexSize, bmpPalette.Length);
            var dMax = 0;
            for (var i = 0; i < stop; i++)
            {
                var c = bmpPalette[i];
                var d = getMaxDiff(c.R, c.G, c.B);
                if (d > GrayScaleThreshold)
                {
                    return d;
                }
                if (d > dMax)
                {
                    dMax = d;
                }
            }
            return dMax;
        }

        private static ToneCurves TryGetPseudoColoringToneCurves()
        {
            try
            {
                if (File.Exists(PseudoColoringToneCurvesFile))
                {
                    return ToneCurves.FromFile(PseudoColoringToneCurvesFile);
                }
            }
            catch { }
            return null;
        }

        public static bool TestGetToneCorveFile()
        {
            try
            {
                if (File.Exists(PseudoColoringToneCurvesFile))
                {
                    return ToneCurves.FromFileTest(PseudoColoringToneCurvesFile);
                }
            }
            catch { }
            return false;
        }
        
        private static void Rotate(BitmapData data, double angle, out Bitmap outBitmap, out BitmapData outData)
        {
            if (data.PixelFormat == PixelFormat.Format24bppRgb)
            {
                outBitmap = new Bitmap(data.Height, data.Width, PixelFormat.Format24bppRgb);
                try
                {
                    outData = outBitmap.LockBits(new Rectangle(Point.Empty, outBitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                    try
                    {
                        unsafe
                        {
                            var inScan0 = (byte*)data.Scan0;
                            var inStride = data.Stride;
                            var outScan0 = (byte*)outData.Scan0;
                            var outStride = outData.Stride;
                            var w = outData.Width;
                            if (angle == 90)
                            {
                                var w1 = w - 1;
                                Parallel.For(0, outData.Height, y =>
                                {
                                    var outScan = outScan0 + y * outStride;
                                    var inScan = inScan0 + y * 3;
                                    for (var x = 0; x < w; x++)
                                    {
                                        var outP = outScan + x * 3;
                                        var inP = inScan + (w1 - x) * inStride;
                                        outP[0] = inP[0];
                                        outP[1] = inP[1];
                                        outP[2] = inP[2];
                                    }
                                });
                            }
                            else
                            {
                                var h1 = outData.Height - 1;
                                Parallel.For(0, outData.Height, y =>
                                {
                                    var outScan = outScan0 + y * outStride;
                                    var inScan = inScan0 + (h1 - y) * 3;
                                    for (var x = 0; x < w; x++)
                                    {
                                        var outP = outScan + x * 3;
                                        var inP = inScan + x * inStride;
                                        outP[0] = inP[0];
                                        outP[1] = inP[1];
                                        outP[2] = inP[2];
                                    }
                                });
                            }
                        }
                    }
                    catch
                    {
                        outBitmap.UnlockBits(outData);
                        throw;
                    }
                }
                catch
                {
                    outBitmap.Dispose();
                    throw;
                }
            }
            else if (data.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                outBitmap = new Bitmap(data.Height, data.Width, PixelFormat.Format8bppIndexed);
                try
                {
                    outData = outBitmap.LockBits(new Rectangle(Point.Empty, outBitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
                    try
                    {
                        unsafe
                        {
                            var inScan0 = (byte*)data.Scan0;
                            var inStride = data.Stride;
                            var outScan0 = (byte*)outData.Scan0;
                            var outStride = outData.Stride;
                            var w = outData.Width;
                            if (angle == 90)
                            {
                                var w1 = w - 1;
                                Parallel.For(0, outData.Height, y =>
                                {
                                    var outScan = outScan0 + y * outStride;
                                    var inScan = inScan0 + y;
                                    for (var x = 0; x < w; x++)
                                    {
                                        var outP = outScan + x;
                                        var inP = inScan + (w1 - x) * inStride;
                                        outP[0] = inP[0];
                                    }
                                });
                            }
                            else
                            {
                                var h1 = outData.Height - 1;
                                Parallel.For(0, outData.Height, y =>
                                {
                                    var outScan = outScan0 + y * outStride;
                                    var inScan = inScan0 + (h1 - y);
                                    for (var x = 0; x < w; x++)
                                    {
                                        var outP = outScan + x;
                                        var inP = inScan + x * inStride;
                                        outP[0] = inP[0];
                                    }
                                });
                            }
                        }
                    }
                    catch
                    {
                        outBitmap.UnlockBits(outData);
                        throw;
                    }
                }
                catch
                {
                    outBitmap.Dispose();
                    throw;
                }
            }
            else throw new ArgumentException();
        }


        private static void VerticalReflection(BitmapData data)
        {
            var scan0 = data.Scan0;
            var stride = data.Stride;
            var height = data.Height;
            var height1 = height - 1;
            Parallel.For(0, height1 / 2, () => Marshal.AllocCoTaskMem(stride), (y, loop, pointer) =>
            {
                var a = scan0 + y * stride;
                var b = scan0 + (height1 - y) * stride;
                ViewerForm.CopyMemory(pointer, a, stride);
                ViewerForm.CopyMemory(a, b, stride);
                ViewerForm.CopyMemory(b, pointer, stride);
                return pointer;
            }, pointer => Marshal.FreeCoTaskMem(pointer));

        }

        private static void Rotate180(BitmapData data)
        {
            unsafe
            {
                var scan0 = (byte*)data.Scan0;
                var stride = data.Stride;
                if (data.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    var w13 = (data.Width - 1) * 3;
                    var h1Stride = (data.Height - 1) * stride;
                    var odd = (data.Width & 1) == 1;
                    var wHalf3 = (data.Width >> 1) * 3; // 切り下げ
                    var hHalf = (data.Height + 1) >> 1; // 切り上げ
                    Parallel.For(0, hHalf, y =>
                    {
                        var yStride = y * stride;
                        var scanU = scan0 + yStride;
                        var scanL = scan0 + h1Stride - yStride;
                        for (var x3 = 0; x3 < wHalf3; x3 += 3)
                        {
                            var xo = w13 - x3;
                            byte* pA, pB;
                            byte temp;

                            pA = scanU + x3;
                            pB = scanL + xo;
                            temp = pA[0]; pA[0] = pB[0]; pB[0] = temp;
                            temp = pA[1]; pA[1] = pB[1]; pB[1] = temp;
                            temp = pA[2]; pA[2] = pB[2]; pB[2] = temp;

                            pA = scanU + xo;
                            pB = scanL + x3;
                            temp = pA[0]; pA[0] = pB[0]; pB[0] = temp;
                            temp = pA[1]; pA[1] = pB[1]; pB[1] = temp;
                            temp = pA[2]; pA[2] = pB[2]; pB[2] = temp;
                        }
                        if (odd)
                        {
                            var pA = scanU + wHalf3;
                            var pB = scanL + wHalf3;
                            byte temp;
                            temp = pA[0]; pA[0] = pB[0]; pB[0] = temp;
                            temp = pA[1]; pA[1] = pB[1]; pB[1] = temp;
                            temp = pA[2]; pA[2] = pB[2]; pB[2] = temp;
                        }
                    });
                }
                else if (data.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    var w1 = data.Width - 1;
                    var h1Stride = (data.Height - 1) * stride;
                    var odd = (data.Width & 1) == 1;
                    var wHalf = data.Width >> 1; // 切り下げ
                    var hHalf = (data.Height + 1) >> 1; // 切り上げ
                    Parallel.For(0, hHalf, y =>
                    {
                        var yStride = y * stride;
                        var scanU = scan0 + yStride;
                        var scanL = scan0 + h1Stride - yStride;
                        for (var x = 0; x < wHalf; x++)
                        {
                            var xo = w1 - x;
                            byte* pA, pB;
                            byte temp;

                            pA = scanU + x;
                            pB = scanL + xo;
                            temp = pA[0]; pA[0] = pB[0]; pB[0] = temp;

                            pA = scanU + xo;
                            pB = scanL + x;
                            temp = pA[0]; pA[0] = pB[0]; pB[0] = temp;
                        }
                        if (odd)
                        {
                            var pA = scanU + wHalf;
                            var pB = scanL + wHalf;
                            byte temp;
                            temp = pA[0]; pA[0] = pB[0]; pB[0] = temp;
                        }
                    });
                }
                else
                {
                    throw new ArgumentException();
                }
            }

        }
        
        private static void ApplyColorTransform(ColorTransform transform, BitmapData data)
        {
            unsafe
            {
                var scan0 = (byte*)data.Scan0;
                var stride = data.Stride;
                var w3 = data.Width * 3;
                Parallel.For(0, data.Height, y =>
                //for (var y = 0; y < data.Height; y++)
                {
                    var left = scan0 + y * stride;
                    for (var x3 = 0; x3 < w3; x3 += 3)
                    {
                        transform(left + x3);
                    }
                }
                );
            }
        }

        private static void ApplyColorTransform(ColorTransform transform, BitmapData data, BackgroundWorker bw)
        {
            unsafe
            {
                var scan0 = (byte*)data.Scan0;
                var stride = data.Stride;
                var w3 = data.Width * 3;
                Parallel.For(0, data.Height, (y, state) =>
                //for (var y = 0; y < data.Height; y++)
                {
                    var left = scan0 + y * stride;
                    for (var x3 = 0; x3 < w3; x3 += 3)
                    {
                        transform(left + x3);
                    }
                    if (bw.CancellationPending)
                    {
                        state.Break();
                    }
                }
                );
            }
        }
        
        /*
        private static void MoireRemover(BitmapData data, BackgroundWorker bw)
        {
            unsafe
            {
                var scan0 = (byte*)data.Scan0;
                var stride = data.Stride;
                var w13 = (data.Width - 1) * 3;

                if (bw == null)
                {
                    Parallel.For(1, data.Height - 1, y => MoireRemover1(scan0, stride, y, w13));
                    Parallel.For(1, data.Height - 1, y => MoireRemover2(scan0, stride, y, w13));
                }
                else
                {
                    Parallel.For(0, data.Height, (y, state) =>
                    {
                        var left = scan0 + y * stride;
                        for (var x3 = 1; x3 < w13; x3 += 3)
                        {
                            var v = left + x3;
                            v[0] = 0;
                            v[1] = 0;
                            v[2] = 255;
                        }
                        if (bw.CancellationPending)
                        {
                            state.Break();
                        }
                    }
                    );
                }
            }
        }

        private unsafe static void MoireRemover1(byte* scan0, int stride, int y, int w13)
        {
            var left0 = scan0 + y * stride;
            var leftN = left0 - stride;
            var leftS = left0 + stride;
            for (var x3 = 3; x3 < w13; x3 += 3)
            {
                var xW3 = x3 - 3;
                var xE3 = x3 + 3;
                int nw = leftN[xW3];
                int n0 = leftN[x3];
                int ne = leftN[xE3];
                int zw = left0[xW3];
                int z0 = left0[x3];
                int ze = left0[xE3];
                int sw = leftS[xW3];
                int s0 = leftS[x3];
                int se = leftS[xE3];

                var t1 = TIG(nw, n0, zw, z0);
                var t2 = TIG(ne, n0, ze, z0);
                var t3 = TIG(z0, ze, s0, se);
                var t4 = TIG(z0, zw, s0, sw);

                var t = (t1 + t2 + t3 + t4 + 4) / 8;
                var v = z0 + t; ;
                left0[x3 + 1] = v < 0 ? (byte)0 : v > 255 ? (byte)255 : (byte)v;
            }
        }
        */

        private unsafe static void MoireRemover2(byte* scan0, int stride, int y, int w13)
        {
            var left0 = scan0 + y * stride;
            for (var x3 = 3; x3 < w13; x3 += 3)
            {
                var a = left0 + x3;
                var v = a[1];
                a[0] = v;
                a[2] = v;
            }
        }

        const int GrayScaleThreshold = 2;

#if DEBUG
        // これは真を返す
        public static bool ParallelBreakTestCode()
        {
            var success = false;
            Parallel.For(0, 1, () => false,
            (y, state, subResult) =>
            {
                state.Break();
                return true;
            },
            subResult =>
            {
                success = subResult;
            });
            return success;
        }
        // よって GetMaxColorDiffProperly でこの様な実装は不要
        public static bool ParallelBreakTestCode2()
        {
            var success = false;
            var successForOnlySuccess = false;
            Parallel.For(0, 1, () => false,
            (y, state, subResult) =>
            {
                successForOnlySuccess = true;
                state.Break();
                return true;
            },
            subResult =>
            {
                success = subResult;
            });
            return success || successForOnlySuccess;
        }
#endif

        // 正確に見る実装
        // 入力がカラーならすぐに終了する
        private static int GetMaxColorDiffProperly(BitmapData data)
        {
            var dMax = GetMaxColorDiff(data);
            if (dMax > GrayScaleThreshold) return dMax;

            var w = data.Width;
            var h = data.Height;

            unsafe
            {
                var scan0 = (byte*)data.Scan0;
                var stride = data.Stride;
                var w3 = data.Width * 3;
                Parallel.For(0, h, () => dMax,
                (y, state, localMax) =>
                {
                    var left = scan0 + y * stride;
                    for (var x3 = 0; x3 < w3; x3 += 3)
                    {
                        var v = left + x3;
                        var d = getMaxDiff(v[0], v[1], v[2]);
                        if (d > GrayScaleThreshold)
                        {
                            state.Break();
                            return d;
                        }
                        if (d > localMax)
                        {
                            localMax = d;
                        }
                    }
                    return localMax;
                },
                localMax =>
                {
                    if (localMax > dMax) dMax = localMax;
                });
            }

            return dMax;
        }

        private static int? GetMaxColorDiffProperly(BitmapData data, BackgroundWorker bw)
        {
            var dMax = GetMaxColorDiff(data);
            if (dMax > GrayScaleThreshold) return dMax;
            
            var w = data.Width;
            var h = data.Height;

            var canceled = false;

            unsafe
            {
                var scan0 = (byte*)data.Scan0;
                var stride = data.Stride;
                var w3 = data.Width * 3;
                Parallel.For(0, h, () => dMax,
                (y, state, localMax) =>
                {
                    if (bw.CancellationPending)
                    {
                        canceled = true;
                        state.Break();
                        return localMax;
                    }

                    var left = scan0 + y * stride;
                    for (var x3 = 0; x3 < w3; x3 += 3)
                    {
                        var v = left + x3;
                        var d = getMaxDiff(v[0], v[1], v[2]);
                        if (d > GrayScaleThreshold)
                        {
                            state.Break();
                            return d;
                        }
                        if (d > localMax)
                        {
                            localMax = d;
                        }
                    }
                    return localMax;
                },
                localMax =>
                {
                    if (localMax > dMax) dMax = localMax;
                });
            }

            if (canceled) return null;

            return dMax;
        }
        
        // 荒く見る実装
        private static int GetMaxColorDiff(BitmapData data)
        {
            const int xCount = 10;
            const int yCount = 10;
            var w = data.Width;
            var h = data.Height;
            var xStep3 = Math.Max(1, w / xCount) * 3;
            var yStep = Math.Max(1, h / yCount);

            var xStart3 = xStep3 / 3 / 2 * 3;
            var yStart = yStep / 2;

            var dMax = 0;
            unsafe
            {
                var scan0 = (byte*)data.Scan0;
                var stride = data.Stride;
                var w3 = data.Width * 3;
                for (var y = yStart; y < h; y++)
                {
                    var left = scan0 + y * stride;
                    for (var x3 = xStart3; x3 < w3; x3 += xStep3)
                    {
                        var v = left + x3;
                        var d = getMaxDiff(v[0], v[1], v[2]);
                        if (d > GrayScaleThreshold)
                        {
                            return d;
                        }
                        if (d > dMax) dMax = d;
                    }
                }
            }

            return dMax;
        }

        private static int getMaxDiff(int r, int g, int b)
        {
            if (b > g)
            {
                if (r > b)
                {
                    return r - g;
                }
                else if (r > g)
                {
                    return b - g;
                }
                else
                {
                    return b - r;
                }
            }
            else
            {
                if (r > g)
                {
                    return r - b;
                }
                else if (r > b)
                {
                    return g - b;
                }
                else
                {
                    return g - r;
                }
            }
        }
        /*

        private static void lockBit(Bitmap bmp, ImageLockMode flags, Action<BitmapData> body)
        {
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), flags, bmp.PixelFormat);
            try
            {
                body(data);
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        private static void lockBit(Bitmap src, Bitmap dst, Action<BitmapData, BitmapData> body)
        {
            lockBit(src, ImageLockMode.ReadOnly, srcData => lockBit(dst, ImageLockMode.WriteOnly, dstData => body(srcData, dstData)));
        }

        private static T following<T>(T disposable, Action<T> body) where T : IDisposable
        {
            try
            {
                body(disposable);
                return disposable;
            }
            catch
            {
                disposable.Dispose();
                throw;
            }
        }

        */

        private static void Binarization(BitmapData data)
        {
            unsafe
            {
                var scan0 = (byte*)data.Scan0;
                var stride = data.Stride;
                var hist = new ulong[256];
                var w3 = data.Width * 3;
                var h = data.Height;
                Parallel.For(0, h, () =>
                {
                    // 一番内側のループで配列の境界チェックを行わないようにする
                    var handle = GCHandle.Alloc(new ulong[256], GCHandleType.Pinned);
                    var pointer = handle.AddrOfPinnedObject().ToPointer();
                    return Tuple.Create((IntPtr)pointer, handle);
                }
                , (y, state, resource) =>
                {
                    var localHist = (byte*)resource.Item1;
                    var scan = scan0 + y * stride;
                    for (var x3 = 0; x3 < w3; x3 += 3)
                    {
                        var p = scan + x3;
                        var g = ToGrayScale(p);
                        p[0] = g;
                        localHist[g]++;
                    }
                    return resource;
                },
                resource =>
                {
                    var localHist = (byte*)resource.Item1;
                    for (var i = 0; i < 256; i++) hist[i] += localHist[i];
                    resource.Item2.Free();
                });

                var t = DiscriminantAnalysisMethod(hist);

                Parallel.For(0, h, y =>
                {
                    var scan = scan0 + y * stride;
                    for (var x3 = 0; x3 < w3; x3 += 3)
                    {
                        var p = scan + x3;
                        var g = p[0];
                        var b = g < t ? (byte)0 : (byte)255;
                        p[0] = p[1] = p[2] = b;
                    }
                });
            }
        }

        private static unsafe byte ToGrayScale(byte* p)
        {
            return (byte)((114 * p[0] + 587 * p[1] + 299 * p[2] + 500) / 1000);
        }

        private static int DiscriminantAnalysisMethod(ulong[] hist)
        {

            double s0 = 0;
            ulong w0 = 0;
            double s0s1 = 0;
            ulong w0w1 = 0;
            for (int i = 0; i < hist.Length; i++)
            {
                var c = hist[i];
                s0s1 += ToLinear[i] * c;
                w0w1 += c;
            }
            double hMax = 0;
            int tMax = 0;
            for (var t = 1; t < 255; t++) // t == 0 と t == 255 の評価値は同じなので tMax == 255 にはならない
            {
                var t1 = t - 1;
                var c = hist[t1];
                w0 += c; if (w0 == 0) continue;
                var w1 = w0w1 - w0; if (w1 == 0) break;
                s0 += ToLinear[t1] * c;
                var s1 = s0s1 - s0;
                var m0m1 = s0 / w0 - s1 / w1;
                var h = (double)w0 * w1 * m0m1 * m0m1;
                if (h > hMax)
                {
                    hMax = h;
                    tMax = t;
                }
            }
            return tMax;
        }
        /*
        private static void UpConvertX2_Median3(BitmapData data, out Bitmap outBitmap, out BitmapData outData)
        {
            var w = data.Width;
            var h = data.Height;
            var w2 = w * 2;
            var h2 = h * 2;
            outBitmap = new Bitmap(w2, h2, PixelFormat.Format24bppRgb);
            try
            {
                outData = outBitmap.LockBits(new Rectangle(0, 0, w2, h2), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                try
                {
                    unsafe
                    {
                        fixed (byte* adr = new byte[w * h])
                        {
                            var gray = adr;

                            var inScan0 = (byte*)data.Scan0;
                            var inStride = data.Stride;

                            Parallel.For(0, h, y =>
                            {
                                var inScan = inScan0 + y * inStride;
                                var grayScan = gray + y * w;
                                for (var x = 0; x < w; x++)
                                {
                                    grayScan[x] = ToGrayScale(inScan + 3 * x);
                                }
                            });

                            var outScan0 = (byte*)outData.Scan0;
                            var outStride = outData.Stride;
                            var outStride2 = outStride * 2;
                            var w1 = w - 1;
                            Parallel.For(0, h - 1, y =>
                            {
                                var inScan = inScan0 + y * inStride;
                                var grayScan = gray + y * w;
                                var outScan = outScan0 + y * outStride2 + outStride + 3;
                                for (var x = 0; x < w1; x++)
                                {
                                    var g00 = grayScan[x];
                                    var g01 = grayScan[x + 1];
                                    var xw = x + w;
                                    var g10 = grayScan[xw];
                                    var g11 = grayScan[xw + 1];

                                    var i00 = inScan + 3 * x;
                                    var i01 = i00 + 3;
                                    var i10 = i00 + inStride;
                                    var i11 = i10 + 3;

                                    var o00 = outScan + 6 * x;
                                    var o01 = o00 + 3;
                                    var o10 = o00 + outStride;
                                    var o11 = o10 + 3;

                                    setMedian(i01, i00, i10, g01, g00, g10, o00);
                                    setMedian(i00, i01, i11, g00, g01, g11, o01);
                                    setMedian(i00, i10, i11, g00, g10, g11, o10);
                                    setMedian(i01, i11, i10, g01, g11, g10, o11);
                                }
                            });
                        }
                    }
                }
                catch
                {
                    outBitmap.UnlockBits(outData);
                    throw;
                }
            }
            catch
            {
                outBitmap.Dispose();
                throw;
            }
        }

        private static unsafe void setMedian(byte* src0, byte* src1, byte* src2, byte value0, byte value1, byte value2, byte* dst)
        {
            if (value0 <= value1)
            {
                if (value1 <= value2)
                {
                    dst[0] = src1[0];
                    dst[1] = src1[1];
                    dst[2] = src1[2];
                }
                else if (value0 > value2)
                {
                    dst[0] = src0[0];
                    dst[1] = src0[1];
                    dst[2] = src0[2];
                }
                else
                {
                    dst[0] = src2[0];
                    dst[1] = src2[1];
                    dst[2] = src2[2];
                }
            }
            else
            {
                if (value1 >= value2)
                {
                    dst[0] = src1[0];
                    dst[1] = src1[1];
                    dst[2] = src1[2];
                }
                else if (value0 < value2)
                {
                    dst[0] = src0[0];
                    dst[1] = src0[1];
                    dst[2] = src0[2];
                }
                else
                {
                    dst[0] = src2[0];
                    dst[1] = src2[1];
                    dst[2] = src2[2];
                }
            }
        }
        */

        const double standardGamma = 2.2;

        private static readonly double[] ToLinear = getToLinear();
        private static double[] getToLinear()
        {
            var result = new double[256];
            for (var i = 1; i < 255; i++)
            {
                result[i] = Math.Pow(i / 255d, standardGamma);
            }
            result[255] = 1;
            return result;
        }

        /*
        private static int TIG(int x00, int x01, int x10, int x11)
        {
            if (x00 < x11)
            {
                if (x01 < x10)
                {
                    if (x00 < x01)
                    {
                        if (x10 < x11)
                        {
                            return 0;
                        }
                        else
                        {
                            // x00 < ? < ? < x10
                            var t1 = x01 - x00;
                            var t2 = x10 - x11;
                            return t1 < t2 ? t1 : t2;
                        }
                    }
                    else
                    {
                        if (x11 < x10)
                        {
                            return 0;
                        }
                        else
                        {
                            // x01 < ? < ? < x11
                            var t1 = x01 - x00;
                            var t2 = x10 - x11;
                            return t1 < t2 ? t2 : t1;
                        }
                    }
                }
                else
                {
                    if (x00 < x10)
                    {
                        if (x01 < x11)
                        {
                            return 0;
                        }
                        else
                        {
                            // x00 < ? < ? < x01
                            var t1 = x10 - x00;
                            var t2 = x01 - x11;
                            return t1 < t2 ? t1 : t2;
                        }
                    }
                    else
                    {
                        if (x11 < x01)
                        {
                            return 0;
                        }
                        else
                        {
                            // x10 < ? < ? < x11
                            var t1 = x10 - x00;
                            var t2 = x01 - x11;
                            return t1 < t2 ? t2 : t1;
                        }
                    }
                }
            }
            else
            {
                if (x01 < x10)
                {
                    if (x11 < x01)
                    {
                        if (x10 < x00)
                        {
                            return 0;
                        }
                        else
                        {
                            // x11 < .. < .. < x10
                            var t1 = x01 - x11;
                            var t2 = x10 - x00;
                            return t1 < t2 ? t1 : t2;
                        }
                    }
                    else
                    {
                        if (x00 < x10)
                        {
                            return 0;
                        }
                        else
                        {
                            // x01 < ? < ? < x00
                            var t1 = x01 - x11;
                            var t2 = x10 - x00;
                            return t1 < t2 ? t2 : t1;
                        }
                    }
                }
                else
                {
                    if (x11 < x10)
                    {
                        if (x01 < x00)
                        {
                            return 0;
                        }
                        else
                        {
                            // x11 < ? < ? < x01
                            var t1 = x10 - x11;
                            var t2 = x01 - x00;
                            return t1 < t2 ? t1 : t2;
                        }
                    }
                    else
                    {
                        if (x00 < x01)
                        {
                            return 0;
                        }
                        else
                        {
                            // x10 < ? < ? < x00
                            var t1 = x10 - x11;
                            var t2 = x01 - x00;
                            return t1 < t2 ? t2 : t1;
                        }
                    }
                }
            }
        }
        */

        /*
        private static void AutoContrastControl(BitmapData data)
        {
            unsafe
            {
                var scan0 = (byte*)data.Scan0;
                var stride = data.Stride;
                var hist = new ulong[256];
                var w3 = data.Width * 3;
                var h = data.Height;
                Parallel.For(0, h, () =>
                {
                    // 一番内側のループで配列の境界チェックを行わないようにする
                    var handle = GCHandle.Alloc(new ulong[256], GCHandleType.Pinned);
                    var pointer = handle.AddrOfPinnedObject().ToPointer();
                    return Tuple.Create((IntPtr)pointer, handle);
                }
                , (y, state, resource) =>
                {
                    var localHist = (byte*)resource.Item1;
                    var scan = scan0 + y * stride;
                    for (var x3 = 0; x3 < w3; x3 += 3)
                    {
                        localHist[ToGrayScale(scan + x3)]++;
                    }
                    return resource;
                },
                resource =>
                {
                    var localHist = (byte*)resource.Item1;
                    for (var i = 0; i < 256; i++) hist[i] += localHist[i];
                    resource.Item2.Free();
                });

                var t = getPixelTransformer(hist);

                Parallel.For(0, h, y =>
                {
                    var scan = scan0 + y * stride;
                    for (var x3 = 0; x3 < w3; x3 += 3)
                    {
                        var p = scan + x3;
                        p[0] = t[p[0]];
                        p[1] = t[p[1]];
                        p[2] = t[p[2]];
                    }
                });
            }
        }

        private static byte[] getPixelTransformer(ulong[] hist)
        {
            var result = new byte[256];
            return result;
        }


        private static readonly double[] Coef = getCoef();
        private static double[] getCoef()
        {
            var result = new double[256];
            result[0] = double.PositiveInfinity;
            for (var i = 1; i < 256; i++)
            {
                result[i] = (1 / standardGamma) * Math.Pow(i / 255d, 1 - standardGamma);
            }
            return result;


        }
        private static byte FromLinear(double x)
        {
#region FromLinear
            if (x <= 0.217633959144357)
            {
                if (x <= 0.0469552523417007)
                {
                    if (x <= 0.010040941629323)
                    {
                        if (x <= 0.00210787070900656)
                        {
                            if (x <= 0.000425224650390458)
                            {
                                if (x <= 7.81107798354122E-05)
                                {
                                    if (x <= 1.08828989900974E-05)
                                    {
                                        if (x <= 0)
                                        {
                                            return 0;
                                        }
                                        else
                                        {
                                            return 1;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 3.64399672905538E-05)
                                        {
                                            return 2;
                                        }
                                        else
                                        {
                                            return 3;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.000214015379324961)
                                    {
                                        if (x <= 0.000137007580926228)
                                        {
                                            return 4;
                                        }
                                        else
                                        {
                                            return 5;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.000309874488202223)
                                        {
                                            return 6;
                                        }
                                        else
                                        {
                                            return 7;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (x <= 0.0010920510163259)
                                {
                                    if (x <= 0.000716602573392738)
                                    {
                                        if (x <= 0.000560631310834358)
                                        {
                                            return 8;
                                        }
                                        else
                                        {
                                            return 9;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.000893600816064848)
                                        {
                                            return 10;
                                        }
                                        else
                                        {
                                            return 11;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.00155485584230114)
                                    {
                                        if (x <= 0.00131234693483934)
                                        {
                                            return 12;
                                        }
                                        else
                                        {
                                            return 13;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.00181992221911018)
                                        {
                                            return 14;
                                        }
                                        else
                                        {
                                            return 15;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (x <= 0.00526919885218325)
                            {
                                if (x <= 0.00349440758459532)
                                {
                                    if (x <= 0.002753627385531)
                                    {
                                        if (x <= 0.00241900851714782)
                                        {
                                            return 16;
                                        }
                                        else
                                        {
                                            return 17;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.00311200524088104)
                                        {
                                            return 18;
                                        }
                                        else
                                        {
                                            return 19;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.00433229255280445)
                                    {
                                        if (x <= 0.0039010886766449)
                                        {
                                            return 20;
                                        }
                                        else
                                        {
                                            return 21;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.00478825390553377)
                                        {
                                            return 22;
                                        }
                                        else
                                        {
                                            return 23;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (x <= 0.00744707243510637)
                                {
                                    if (x <= 0.00630690508749572)
                                    {
                                        if (x <= 0.00577534560923044)
                                        {
                                            return 24;
                                        }
                                        else
                                        {
                                            return 25;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.00686408142040203)
                                        {
                                            return 26;
                                        }
                                        else
                                        {
                                            return 27;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.00869126077729575)
                                    {
                                        if (x <= 0.00805607007457637)
                                        {
                                            return 28;
                                        }
                                        else
                                        {
                                            return 29;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.00935282582017179)
                                        {
                                            return 30;
                                        }
                                        else
                                        {
                                            return 31;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (x <= 0.0247904612833799)
                        {
                            if (x <= 0.0165214690535943)
                            {
                                if (x <= 0.0130622864278939)
                                {
                                    if (x <= 0.011497508667883)
                                    {
                                        if (x <= 0.0107557800626981)
                                        {
                                            return 32;
                                        }
                                        else
                                        {
                                            return 33;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.012266290917962)
                                        {
                                            return 34;
                                        }
                                        else
                                        {
                                            return 35;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.0147365375750818)
                                    {
                                        if (x <= 0.0138856511535259)
                                        {
                                            return 36;
                                        }
                                        else
                                        {
                                            return 37;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.0156150948667253)
                                        {
                                            return 38;
                                        }
                                        else
                                        {
                                            return 39;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (x <= 0.0204279529221187)
                                {
                                    if (x <= 0.0184182373325779)
                                    {
                                        if (x <= 0.01745580315753)
                                        {
                                            return 40;
                                        }
                                        else
                                        {
                                            return 41;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.0194089089912112)
                                        {
                                            return 42;
                                        }
                                        else
                                        {
                                            return 43;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.0225516842901574)
                                    {
                                        if (x <= 0.0214755014003032)
                                        {
                                            return 44;
                                        }
                                        else
                                        {
                                            return 45;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.0236566291421106)
                                        {
                                            return 46;
                                        }
                                        else
                                        {
                                            return 47;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (x <= 0.034915429698057)
                            {
                                if (x <= 0.0296170961155155)
                                {
                                    if (x <= 0.0271452781336779)
                                    {
                                        if (x <= 0.0259533039033021)
                                        {
                                            return 48;
                                        }
                                        else
                                        {
                                            return 49;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.0283665031245139)
                                        {
                                            return 50;
                                        }
                                        else
                                        {
                                            return 51;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.0322068459070396)
                                    {
                                        if (x <= 0.0308971725036458)
                                        {
                                            return 52;
                                        }
                                        else
                                        {
                                            return 53;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.0335462282255346)
                                        {
                                            return 54;
                                        }
                                        else
                                        {
                                            return 55;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (x <= 0.0406925767498176)
                                {
                                    if (x <= 0.0377437230803591)
                                    {
                                        if (x <= 0.0363145589570808)
                                        {
                                            return 56;
                                        }
                                        else
                                        {
                                            return 57;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.0392030276401087)
                                        {
                                            return 58;
                                        }
                                        else
                                        {
                                            return 59;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.0437628180448207)
                                    {
                                        if (x <= 0.0422124731088253)
                                        {
                                            return 60;
                                        }
                                        else
                                        {
                                            return 61;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.0453437115543849)
                                        {
                                            return 62;
                                        }
                                        else
                                        {
                                            return 63;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (x <= 0.115240709971913)
                    {
                        if (x <= 0.0769839851755384)
                        {
                            if (x <= 0.0609623265168548)
                            {
                                if (x <= 0.0537098191447705)
                                {
                                    if (x <= 0.0502706643246205)
                                    {
                                        if (x <= 0.0485975378555383)
                                        {
                                            return 64;
                                        }
                                        else
                                        {
                                            return 65;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.0519747267914618)
                                        {
                                            return 66;
                                        }
                                        else
                                        {
                                            return 67;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.0572734634815802)
                                    {
                                        if (x <= 0.0554760341504932)
                                        {
                                            return 68;
                                        }
                                        else
                                        {
                                            return 69;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.0591021977465409)
                                        {
                                            return 70;
                                        }
                                        else
                                        {
                                            return 71;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (x <= 0.0687185432313283)
                                {
                                    if (x <= 0.0647771208312606)
                                    {
                                        if (x <= 0.0628539383533009)
                                        {
                                            return 72;
                                        }
                                        else
                                        {
                                            return 73;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.0667319605650499)
                                        {
                                            return 74;
                                        }
                                        else
                                        {
                                            return 75;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.0727872755140856)
                                    {
                                        if (x <= 0.0707369535916337)
                                        {
                                            return 76;
                                        }
                                        else
                                        {
                                            return 77;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.0748695919942975)
                                        {
                                            return 78;
                                        }
                                        else
                                        {
                                            return 79;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (x <= 0.0950634825767966)
                            {
                                if (x <= 0.0857639390142413)
                                {
                                    if (x <= 0.081309326068453)
                                    {
                                        if (x <= 0.0791305363681785)
                                        {
                                            return 80;
                                        }
                                        else
                                        {
                                            return 81;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.0835204339765771)
                                        {
                                            return 82;
                                        }
                                        else
                                        {
                                            return 83;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.0903484523731909)
                                    {
                                        if (x <= 0.0880399193415158)
                                        {
                                            return 84;
                                        }
                                        else
                                        {
                                            return 85;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.0926896147945781)
                                        {
                                            return 86;
                                        }
                                        else
                                        {
                                            return 87;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (x <= 0.104887502555048)
                                {
                                    if (x <= 0.0999096346255337)
                                    {
                                        if (x <= 0.0974701309915671)
                                        {
                                            return 88;
                                        }
                                        else
                                        {
                                            return 89;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.102382067394136)
                                        {
                                            return 90;
                                        }
                                        else
                                        {
                                            return 91;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.109997669873424)
                                    {
                                        if (x <= 0.107426012721204)
                                        {
                                            return 92;
                                        }
                                        else
                                        {
                                            return 93;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.112602545372669)
                                        {
                                            return 94;
                                        }
                                        else
                                        {
                                            return 95;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (x <= 0.162034205742345)
                        {
                            if (x <= 0.137552732921598)
                            {
                                if (x <= 0.126127653785547)
                                {
                                    if (x <= 0.120617186511258)
                                    {
                                        if (x <= 0.117912233827685)
                                        {
                                            return 96;
                                        }
                                        else
                                        {
                                            return 97;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.123355637019522)
                                        {
                                            return 98;
                                        }
                                        else
                                        {
                                            return 99;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.131772657065336)
                                    {
                                        if (x <= 0.128933304688844)
                                        {
                                            return 100;
                                        }
                                        else
                                        {
                                            return 101;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.134645777717061)
                                        {
                                            return 102;
                                        }
                                        else
                                        {
                                            return 103;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (x <= 0.149520206970367)
                                {
                                    if (x <= 0.143468409531938)
                                    {
                                        if (x <= 0.140493588441243)
                                        {
                                            return 104;
                                        }
                                        else
                                        {
                                            return 105;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.146477260951957)
                                        {
                                            return 106;
                                        }
                                        else
                                        {
                                            return 107;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.155708637481797)
                                    {
                                        if (x <= 0.152597311375264)
                                        {
                                            return 108;
                                        }
                                        else
                                        {
                                            return 109;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.158854248139988)
                                        {
                                            return 110;
                                        }
                                        else
                                        {
                                            return 111;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (x <= 0.188717698388145)
                            {
                                if (x <= 0.175098737841439)
                                {
                                    if (x <= 0.168497409106411)
                                    {
                                        if (x <= 0.165248572231293)
                                        {
                                            return 112;
                                        }
                                        else
                                        {
                                            return 113;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.171780777431494)
                                        {
                                            return 114;
                                        }
                                        else
                                        {
                                            return 115;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.181838675351172)
                                    {
                                        if (x <= 0.178451350548966)
                                        {
                                            return 116;
                                        }
                                        else
                                        {
                                            return 117;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.185260771635931)
                                        {
                                            return 118;
                                        }
                                        else
                                        {
                                            return 119;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (x <= 0.202894876003151)
                                {
                                    if (x <= 0.195736277256131)
                                    {
                                        if (x <= 0.192209514195841)
                                        {
                                            return 120;
                                        }
                                        else
                                        {
                                            return 121;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.199298045381032)
                                        {
                                            return 122;
                                        }
                                        else
                                        {
                                            return 123;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.210193952605647)
                                    {
                                        if (x <= 0.206526826181244)
                                        {
                                            return 124;
                                        }
                                        else
                                        {
                                            return 125;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.213896311603583)
                                        {
                                            return 126;
                                        }
                                        else
                                        {
                                            return 127;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (x <= 0.532573629675202)
                {
                    if (x <= 0.356188932317535)
                    {
                        if (x <= 0.282279919922953)
                        {
                            if (x <= 0.248812129136535)
                            {
                                if (x <= 0.232938541875955)
                                {
                                    if (x <= 0.225215341972388)
                                    {
                                        if (x <= 0.22140695084443)
                                        {
                                            return 128;
                                        }
                                        else
                                        {
                                            return 129;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.229059187453796)
                                        {
                                            return 130;
                                        }
                                        else
                                        {
                                            return 131;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.240803994228205)
                                    {
                                        if (x <= 0.23685345949255)
                                        {
                                            return 132;
                                        }
                                        else
                                        {
                                            return 133;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.244790199682941)
                                        {
                                            return 134;
                                        }
                                        else
                                        {
                                            return 135;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (x <= 0.265258141563487)
                                {
                                    if (x <= 0.25696337158376)
                                    {
                                        if (x <= 0.252869835552799)
                                        {
                                            return 136;
                                        }
                                        else
                                        {
                                            return 137;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.261092789573764)
                                        {
                                            return 138;
                                        }
                                        else
                                        {
                                            return 139;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.273696854209998)
                                    {
                                        if (x <= 0.269459479293875)
                                        {
                                            return 140;
                                        }
                                        else
                                        {
                                            return 141;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.277970317464829)
                                        {
                                            return 142;
                                        }
                                        else
                                        {
                                            return 143;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (x <= 0.318063762300924)
                            {
                                if (x <= 0.299880729187857)
                                {
                                    if (x <= 0.291007744487189)
                                    {
                                        if (x <= 0.286625712164196)
                                        {
                                            return 144;
                                        }
                                        else
                                        {
                                            return 145;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.295426066912859)
                                        {
                                            return 146;
                                        }
                                        else
                                        {
                                            return 147;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.308899270921164)
                                    {
                                        if (x <= 0.30437178078792)
                                        {
                                            return 148;
                                        }
                                        else
                                        {
                                            return 149;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.313463248531324)
                                        {
                                            return 150;
                                        }
                                        else
                                        {
                                            return 151;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (x <= 0.336832143654819)
                                {
                                    if (x <= 0.32737459176114)
                                    {
                                        if (x <= 0.322700860654396)
                                        {
                                            return 152;
                                        }
                                        else
                                        {
                                            return 153;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.33208500353852)
                                        {
                                            return 154;
                                        }
                                        else
                                        {
                                            return 155;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.346436798349194)
                                    {
                                        if (x <= 0.341616059532129)
                                        {
                                            return 156;
                                        }
                                        else
                                        {
                                            return 157;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.351294407044204)
                                        {
                                            return 158;
                                        }
                                        else
                                        {
                                            return 159;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (x <= 0.439559217105967)
                        {
                            if (x <= 0.396679658981368)
                            {
                                if (x <= 0.376137125027673)
                                {
                                    if (x <= 0.366088918227714)
                                    {
                                        if (x <= 0.361120420634443)
                                        {
                                            return 160;
                                        }
                                        else
                                        {
                                            return 161;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.371094471100258)
                                        {
                                            return 162;
                                        }
                                        else
                                        {
                                            return 163;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.386333918027948)
                                    {
                                        if (x <= 0.381216925560751)
                                        {
                                            return 164;
                                        }
                                        else
                                        {
                                            return 165;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.391488147537812)
                                        {
                                            return 166;
                                        }
                                        else
                                        {
                                            return 167;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (x <= 0.417819414430034)
                                {
                                    if (x <= 0.407174706160086)
                                    {
                                        if (x <= 0.401908497034466)
                                        {
                                            return 168;
                                        }
                                        else
                                        {
                                            return 169;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.412478330610606)
                                        {
                                            return 170;
                                        }
                                        else
                                        {
                                            return 171;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.428614135322934)
                                    {
                                        if (x <= 0.423198001456204)
                                        {
                                            return 172;
                                        }
                                        else
                                        {
                                            return 173;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.434067859462147)
                                        {
                                            return 174;
                                        }
                                        else
                                        {
                                            return 175;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (x <= 0.484850007941112)
                            {
                                if (x <= 0.461901840490916)
                                {
                                    if (x <= 0.45065500484921)
                                    {
                                        if (x <= 0.44508825128877)
                                        {
                                            return 176;
                                        }
                                        else
                                        {
                                            return 177;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.456259520432208)
                                        {
                                            return 178;
                                        }
                                        else
                                        {
                                            return 179;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.47330006290076)
                                    {
                                        if (x <= 0.467582007288644)
                                        {
                                            return 180;
                                        }
                                        else
                                        {
                                            return 181;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.479056049216561)
                                        {
                                            return 182;
                                        }
                                        else
                                        {
                                            return 183;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (x <= 0.50840639468082)
                                {
                                    if (x <= 0.496552008526423)
                                    {
                                        if (x <= 0.49068198059706)
                                        {
                                            return 184;
                                        }
                                        else
                                        {
                                            return 185;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.502460132892342)
                                        {
                                            return 186;
                                        }
                                        else
                                        {
                                            return 187;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.520413493593955)
                                    {
                                        if (x <= 0.51439083470242)
                                        {
                                            return 188;
                                        }
                                        else
                                        {
                                            return 189;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.526474411820134)
                                        {
                                            return 190;
                                        }
                                        else
                                        {
                                            return 191;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (x <= 0.7482035639591)
                    {
                        if (x <= 0.6354021082998)
                        {
                            if (x <= 0.582750939462239)
                            {
                                if (x <= 0.55735429739221)
                                {
                                    if (x <= 0.544887124606259)
                                    {
                                        if (x <= 0.538711187284542)
                                        {
                                            return 192;
                                        }
                                        else
                                        {
                                            return 193;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.551101481432744)
                                        {
                                            return 194;
                                        }
                                        else
                                        {
                                            return 195;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.569975464411126)
                                    {
                                        if (x <= 0.563645611950209)
                                        {
                                            return 196;
                                        }
                                        else
                                        {
                                            return 197;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.576343893919652)
                                        {
                                            return 198;
                                        }
                                        else
                                        {
                                            return 199;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (x <= 0.608766056243436)
                                {
                                    if (x <= 0.595681033812771)
                                    {
                                        if (x <= 0.58919663986853)
                                        {
                                            return 200;
                                        }
                                        else
                                        {
                                            return 201;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.602204159815203)
                                        {
                                            return 202;
                                        }
                                        else
                                        {
                                            return 203;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.622006313092697)
                                    {
                                        if (x <= 0.615366761313804)
                                        {
                                            return 204;
                                        }
                                        else
                                        {
                                            return 205;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.628684749497882)
                                        {
                                            return 206;
                                        }
                                        else
                                        {
                                            return 207;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (x <= 0.6905466698325)
                            {
                                if (x <= 0.662661517798529)
                                {
                                    if (x <= 0.648953743446644)
                                    {
                                        if (x <= 0.642158427122848)
                                        {
                                            return 208;
                                        }
                                        else
                                        {
                                            return 209;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.655788094607276)
                                        {
                                            return 210;
                                        }
                                        else
                                        {
                                            return 211;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.676525728343806)
                                    {
                                        if (x <= 0.669574050073102)
                                        {
                                            return 212;
                                        }
                                        else
                                        {
                                            return 213;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.68351658938475)
                                        {
                                            return 214;
                                        }
                                        else
                                        {
                                            return 215;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (x <= 0.719059913673218)
                                {
                                    if (x <= 0.704724634813905)
                                    {
                                        if (x <= 0.697616006187241)
                                        {
                                            return 216;
                                        }
                                        else
                                        {
                                            return 217;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.711872591943301)
                                        {
                                            return 218;
                                        }
                                        else
                                        {
                                            return 219;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.733552794667229)
                                    {
                                        if (x <= 0.726286635969521)
                                        {
                                            return 220;
                                        }
                                        else
                                        {
                                            return 221;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.740858425471583)
                                        {
                                            return 222;
                                        }
                                        else
                                        {
                                            return 223;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (x <= 0.871127371794158)
                        {
                            if (x <= 0.80839117596689)
                            {
                                if (x <= 0.77797990182355)
                                {
                                    if (x <= 0.763012505652278)
                                    {
                                        if (x <= 0.755588245578609)
                                        {
                                            return 224;
                                        }
                                        else
                                        {
                                            return 225;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.770476379376633)
                                        {
                                            return 226;
                                        }
                                        else
                                        {
                                            return 227;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.793106032555286)
                                    {
                                        if (x <= 0.785523107941254)
                                        {
                                            return 228;
                                        }
                                        else
                                        {
                                            return 229;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.800728710369476)
                                        {
                                            return 230;
                                        }
                                        else
                                        {
                                            return 231;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (x <= 0.839439603675865)
                                {
                                    if (x <= 0.823835608245486)
                                    {
                                        if (x <= 0.816093463810775)
                                        {
                                            return 232;
                                        }
                                        else
                                        {
                                            return 233;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.831617643497409)
                                        {
                                            return 234;
                                        }
                                        else
                                        {
                                            return 235;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.855203434669715)
                                    {
                                        if (x <= 0.847301522774009)
                                        {
                                            return 236;
                                        }
                                        else
                                        {
                                            return 237;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.863145373126454)
                                        {
                                            return 238;
                                        }
                                        else
                                        {
                                            return 239;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (x <= 0.936429529923044)
                            {
                                if (x <= 0.903456637647036)
                                {
                                    if (x <= 0.887211683799617)
                                    {
                                        if (x <= 0.879149464210075)
                                        {
                                            return 240;
                                        }
                                        else
                                        {
                                            return 241;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.895314063877193)
                                        {
                                            return 242;
                                        }
                                        else
                                        {
                                            return 243;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.919862498534466)
                                    {
                                        if (x <= 0.91163943820402)
                                        {
                                            return 244;
                                        }
                                        else
                                        {
                                            return 245;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.928125851516941)
                                        {
                                            return 246;
                                        }
                                        else
                                        {
                                            return 247;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (x <= 0.970048149515388)
                                {
                                    if (x <= 0.953157993562384)
                                    {
                                        if (x <= 0.944773566418192)
                                        {
                                            return 248;
                                        }
                                        else
                                        {
                                            return 249;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.961582843810966)
                                        {
                                            return 250;
                                        }
                                        else
                                        {
                                            return 251;
                                        }
                                    }
                                }
                                else
                                {
                                    if (x <= 0.987100256182511)
                                    {
                                        if (x <= 0.978553942923943)
                                        {
                                            return 252;
                                        }
                                        else
                                        {
                                            return 253;
                                        }
                                    }
                                    else
                                    {
                                        if (x <= 0.995687121335282)
                                        {
                                            return 254;
                                        }
                                        else
                                        {
                                            return 255;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
#endregion
        }

        */
#if DEBUG
        public static void GetFromLinear()
        {
            System.Windows.Forms.Clipboard.SetText(GetFromLinear("", 0, 256));
            System.Windows.Forms.MessageBox.Show("success!");
        }
        private static string GetFromLinear(string indent, int fromInclusive, int toExclusive)
        {
            if (fromInclusive == toExclusive - 1)
            {
                return $"{indent}return {fromInclusive};\n";
            }
            else
            {
                var center = (fromInclusive + toExclusive) / 2;
                var newIndent = indent + "    ";
                var lCenter = Math.Sqrt(ToLinear[center - 1] * ToLinear[center]);
                return
                    $"{indent}if (x <= {lCenter})\n" + // もともと幾何平均なので切り上げを優遇する根拠はない
                    $"{indent}{'{'}\n" +
                    GetFromLinear(newIndent, fromInclusive, center) +
                    $"{indent}{'}'}\n" +
                    $"{indent}else\n" +
                    $"{indent}{'{'}\n" +
                    GetFromLinear(newIndent, center, toExclusive) +
                    $"{indent}{'}'}\n";
            }
        }
#endif

    }

    //enum TrimmingMode { None = 0, Left = 1 << 0, Right = 1 << 1, Top = 1 << 2, Bottom = 1 << 3,
    //    LeftRight = Left | Right, TopBottom = Top | Bottom }

    //enum PixelValueConversion { None, Binarization, AutoContrastControl }


    public unsafe delegate void ColorTransform(byte* bgr);
    public class PseudoColoringPainter : IDisposable
    {
        bool disposed = true;
        IntPtr pointer;
        unsafe byte* lookup = null;
        //GCHandle gch;

        public PseudoColoringPainter(ToneCurves toneCurve)
        {
            alloc(initialize: true);
            try
            {
                unsafe
                {
                    for (var i = 0; i < 256; i++) toneCurve.ApplyToValueForBrightness(lookup + i * 3);
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public PseudoColoringPainter()
        {
            unsafe
            {
                lookup = DefaultLookup;
            }
        }

        private void alloc(bool initialize)
        {
            Dispose();
            //gch = GCHandle.Alloc(256 * 3, GCHandleType.Pinned);
            pointer = Marshal.AllocHGlobal(256 * 3);
            disposed = false;
            try
            {
                unsafe
                {
                    lookup = (byte*)pointer;
                    if (initialize)
                    {
                        for (var i = 0; i < 256; i++)
                        {
                            var a = lookup + i * 3;
                            a[0] = a[1] = a[2] = (byte)i;
                        }
                    }
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private unsafe readonly static byte* DefaultLookup = getDefaultTable();
        private unsafe static byte* getDefaultTable()
        {
            var table = new byte[256 * 3] {
0, 0, 0, 1, 1, 1, 2, 2, 2, 3, 3, 3, 4, 3, 4, 5, 4, 5, 6, 5, 7, 7, 6, 8,
8, 7, 9, 9, 8, 10, 10, 9, 11, 11, 10, 12, 12, 11, 13, 13, 12, 14, 14, 13, 16, 15, 13, 17,
16, 14, 18, 17, 15, 19, 18, 16, 20, 19, 17, 21, 20, 18, 22, 21, 19, 24, 22, 20, 25, 23, 21, 26,
24, 22, 27, 25, 22, 28, 26, 23, 30, 27, 24, 31, 28, 25, 32, 29, 26, 33, 30, 27, 34, 31, 28, 35,
32, 29, 37, 33, 30, 38, 34, 31, 39, 35, 32, 40, 36, 33, 41, 37, 33, 42, 38, 34, 43, 39, 35, 44,
40, 36, 46, 41, 37, 47, 42, 38, 48, 43, 39, 49, 44, 40, 51, 45, 41, 52, 46, 42, 53, 47, 43, 54,
48, 43, 55, 48, 44, 56, 49, 45, 58, 50, 46, 59, 51, 47, 60, 52, 48, 61, 53, 49, 63, 54, 50, 64,
55, 51, 65, 56, 52, 66, 57, 52, 68, 57, 53, 69, 58, 54, 70, 59, 55, 71, 60, 56, 73, 61, 57, 74,
62, 58, 75, 62, 59, 77, 63, 59, 78, 64, 60, 80, 65, 61, 81, 65, 62, 82, 66, 63, 84, 67, 64, 86,
68, 64, 87, 68, 65, 88, 69, 66, 90, 70, 67, 92, 70, 67, 94, 70, 68, 96, 72, 69, 97, 73, 70, 98,
74, 71, 99, 75, 72, 100, 76, 73, 101, 77, 74, 102, 78, 75, 104, 79, 76, 105, 80, 77, 106, 81, 78, 108,
81, 79, 109, 82, 80, 110, 83, 80, 111, 84, 81, 113, 85, 82, 114, 85, 83, 115, 86, 84, 117, 87, 85, 118,
88, 86, 119, 89, 87, 120, 90, 88, 121, 90, 89, 123, 91, 90, 124, 92, 91, 125, 93, 92, 126, 94, 93, 128,
95, 94, 129, 95, 95, 130, 96, 96, 131, 97, 96, 132, 98, 98, 133, 99, 98, 135, 100, 99, 136, 100, 101, 137,
102, 102, 138, 102, 103, 139, 103, 103, 140, 104, 104, 142, 105, 105, 143, 106, 106, 144, 107, 107, 145, 107, 108, 146,
108, 109, 148, 109, 110, 148, 110, 111, 150, 110, 112, 151, 111, 113, 153, 112, 114, 154, 113, 115, 155, 114, 116, 156,
114, 117, 157, 116, 118, 158, 116, 119, 159, 117, 120, 161, 118, 121, 162, 119, 122, 163, 120, 123, 164, 121, 124, 165,
121, 125, 166, 122, 126, 167, 123, 127, 169, 123, 128, 170, 124, 129, 171, 125, 129, 172, 126, 130, 174, 127, 131, 175,
128, 132, 176, 129, 133, 177, 129, 134, 178, 131, 136, 179, 132, 137, 180, 132, 138, 181, 133, 138, 183, 134, 139, 184,
134, 140, 185, 136, 141, 186, 136, 142, 188, 137, 143, 189, 137, 144, 190, 138, 145, 191, 138, 146, 192, 139, 147, 194,
140, 148, 195, 140, 149, 196, 141, 150, 197, 142, 151, 198, 143, 152, 200, 144, 153, 201, 144, 154, 202, 145, 155, 203,
146, 156, 205, 146, 157, 206, 146, 158, 207, 147, 159, 208, 148, 160, 209, 149, 161, 210, 150, 162, 211, 150, 163, 212,
151, 164, 214, 151, 165, 215, 152, 166, 216, 153, 167, 217, 154, 168, 218, 154, 169, 219, 155, 171, 220, 156, 172, 221,
157, 173, 222, 158, 174, 224, 158, 175, 224, 160, 176, 225, 160, 177, 226, 161, 178, 227, 162, 179, 228, 163, 180, 229,
163, 181, 230, 165, 182, 231, 165, 183, 231, 166, 184, 232, 167, 186, 233, 168, 187, 234, 169, 188, 234, 170, 189, 235,
171, 190, 235, 172, 191, 236, 173, 192, 237, 174, 194, 237, 175, 195, 238, 176, 196, 239, 177, 197, 239, 178, 198, 240,
179, 199, 240, 181, 201, 241, 182, 202, 241, 183, 203, 241, 184, 204, 242, 185, 206, 242, 186, 207, 243, 188, 208, 243,
189, 209, 244, 190, 211, 244, 192, 212, 244, 193, 213, 245, 195, 214, 245, 196, 216, 245, 198, 217, 245, 200, 218, 246,
201, 219, 246, 203, 221, 246, 204, 222, 247, 206, 223, 247, 207, 224, 247, 209, 226, 247, 211, 227, 247, 213, 228, 247,
214, 230, 248, 216, 231, 248, 218, 232, 248, 220, 233, 248, 222, 234, 248, 224, 236, 248, 226, 237, 248, 228, 238, 248,
231, 239, 248, 233, 240, 249, 235, 241, 249, 237, 243, 249, 239, 244, 249, 240, 245, 250, 242, 246, 250, 244, 247, 250,
245, 248, 251, 247, 249, 251, 249, 250, 252, 250, 251, 252, 252, 252, 253, 253, 253, 253, 254, 254, 254, 255, 255, 255,
            };

            var parray = Marshal.AllocHGlobal(256 * 3);
            Marshal.Copy(table, 0, parray, 256 * 3);
            table = null;
            return (byte*)parray;
        }

        public ColorTransform ColorTransform
        {
            get
            {
                unsafe
                {
                    return transform;
                }
            }
        }

        public ColorTransform ColorTransformForGrayScale
        {
            get
            {
                unsafe
                {
                    return transformForGrayScale;
                }
            }
        }

        private unsafe void transform(byte* bgr)
        {
            var a = lookup + 3 * median(bgr[0], bgr[1], bgr[2]);
            bgr[0] = a[0];
            bgr[1] = a[1];
            bgr[2] = a[2];
        }

        private unsafe void transformForGrayScale(byte* bgr)
        {
            var a = lookup + 3 * *bgr;
            bgr[0] = a[0];
            bgr[1] = a[1];
            bgr[2] = a[2];
        }

        private static byte median(byte r, byte g, byte b)
        {
            if (b > g)
            {
                if (r > b)
                {
                    return b;
                }
                else if (r > g)
                {
                    return r;
                }
                else
                {
                    return g;
                }
            }
            else
            {
                if (r > g)
                {
                    return g;
                }
                else if (r > b)
                {
                    return r;
                }
                else
                {
                    return b;
                }
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                //gch.Free();
                Marshal.FreeHGlobal(pointer);
                disposed = true;
            }
        }
    }
}
