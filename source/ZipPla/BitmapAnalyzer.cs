using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ZipPla
{
    public static class BitmapAnalyzer
    {
        private const int maxLength = 128;

        /*
        public static Point GetFocus(Image img, Fraction pixelRatio)
        {
            return GetFocus(img, new Rectangle(0, 0, img.Width, img.Height), pixelRatio);
        }
        public static Point GetFocus(Image img, RectangleF rectangle, Fraction pixelRatio)
        {
            var bmp = img as Bitmap;
            var rect = new Rectangle((int)Math.Round(rectangle.X), (int)Math.Round(rectangle.Y), (int)Math.Round(rectangle.Width), (int)Math.Round(rectangle.Height));
            if (bmp != null)
            {
                return GetFocus(bmp, rect, pixelRatio);
            }
            else
            {
                using (var bmp2 = new Bitmap(img))
                {
                    return GetFocus(bmp2, rect, pixelRatio);
                }
            }
        }
        */
        public static Point GetFocus(Bitmap bmp, RectangleF rectangle, Fraction pixelRatio)
        {
            var rect = new Rectangle((int)Math.Round(rectangle.X), (int)Math.Round(rectangle.Y), (int)Math.Round(rectangle.Width), (int)Math.Round(rectangle.Height));
            return GetFocus(bmp, rect, pixelRatio);
        }
        public static Point GetFocus(Bitmap bmp, Fraction pixelRatio)
        {
            return GetFocus(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height), pixelRatio);
        }
        public static Point GetFocus(Bitmap bmp, Rectangle rectangle, Fraction pixelRatio)
        {
            var orgW = rectangle.Width;
            var orgH = rectangle.Height;
            var workingSize = GetWorkignSize(rectangle.Size, pixelRatio);
            var workingWidth = workingSize.Width;
            var workingHeight = workingSize.Height;
            /*
            var m = orgW > orgH ? orgW : orgH;
            var workingWidth = orgW * maxLength / m;
            var workingHeight = orgH * maxLength / m;
            */

            var pointsLinearArray = GetLinearArrayForFocus(bmp, rectangle, workingWidth, workingHeight);

            var result = 0;
            int max = 0;
            var wh = workingWidth * workingHeight;
            for (var i = 0; i < wh; i++)
            {
                var v = pointsLinearArray[i];
                if(v > max)
                {
                    max = v;
                    result = i;
                }
            }
            
            var resultX = result % workingWidth;
            var resultY = result / workingWidth;
            return new Point(((resultX * 2 + 1) * orgW / (2 * workingWidth)) + rectangle.X, ((resultY * 2 + 1) * orgH / (2 * workingHeight)) + rectangle.Y);
        }

        public static Bitmap GetIntensiveLines(Bitmap bmp, Fraction pixelRatio)
        {
            const int mag = 4;
            var rectangle = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var wokingSize0 = GetWorkignSize(bmp.Size, pixelRatio);
            var workingWidth = mag * wokingSize0.Width;
            var workingHeight = mag * wokingSize0.Height;

            return LinearToBitmap(GetLinearArrayForFocus(bmp, rectangle, workingWidth, workingHeight), workingWidth);
        }

        private static Size GetWorkignSize(Size targetSize, Fraction pixelRatio)
        {
            return GetWorkignSize((Fraction)targetSize * pixelRatio);
        }
        private static Size GetWorkignSize(Fraction targetAspect)
        {
            var orgW = targetAspect.Numerator;
            var orgH = targetAspect.Denominator;
            var m = orgW > orgH ? orgW : orgH;
            var workingWidth = Math.Max(1, orgW * maxLength / m);
            var workingHeight = Math.Max(1, orgH * maxLength / m);
            return new Size(workingWidth, workingHeight);
        }

        private static int[] GetLinearArrayForFocus(Bitmap bmp, Rectangle rectangle, int workingWidth, int workingHeight)
        {
            var orgW = rectangle.Width;
            var orgH = rectangle.Height;
            int[] grayLinearArray;
            int wh;
            var workingBmp = new Bitmap(workingWidth, workingHeight);
            try
            {
                using (var g = Graphics.FromImage(workingBmp))
                {
                    g.DrawImage(bmp, new Rectangle(0, 0, workingWidth, workingHeight), rectangle, GraphicsUnit.Pixel);
                }

                wh = workingWidth * workingHeight;
                grayLinearArray = new int[wh];
                var format = workingBmp.PixelFormat;
                var bytePerPixel = Image.GetPixelFormatSize(format) / 8;
                var img = workingBmp.LockBits(new Rectangle(new Point(), workingBmp.Size), System.Drawing.Imaging.ImageLockMode.ReadOnly, format);
                try
                {
                    var stride = img.Stride;
                    unsafe
                    {
                        var adr = (byte*)img.Scan0;
                        for (var y = 0; y < workingHeight; y++)
                        {
                            var adrYStride = adr + y * stride;
                            var yW = y * workingWidth;
                            for (var x = 0; x < workingWidth; x++)
                            {
                                var bgr = adrYStride + bytePerPixel * x;
                                int b = bgr[0];
                                int g = bgr[1];
                                int r = bgr[2];
                                grayLinearArray[yW + x] = b + 6 * b + 3 * r;
                            }
                        }
                    }
                }
                finally
                {
                    workingBmp.UnlockBits(img);
                }
            }
            catch
            {
                workingBmp.Dispose();
                throw;
            }
            var pointsLinearArray = new int[wh];
            var h1 = workingHeight - 1;
            var w1 = workingWidth - 1;
            for (var y = 0; y < h1; y++)
            {
                var yW = y * workingWidth;
                var y1W = (y + 1) * workingWidth;
                for (var x = 0; x < w1; x++)
                {
                    var x1 = x + 1;
                    var dp = grayLinearArray[y1W + x1] - grayLinearArray[yW + x];
                    var dn = grayLinearArray[y1W + x] - grayLinearArray[yW + x1];

                    var dy = dp + dn;
                    var dx = dp - dn;

                    var absDx = dx > 0 ? dx : -dx;
                    var absDy = dy > 0 ? dy : -dy;

                    var d2 = absDx + absDy; // 斜めが強まり帳尻が合う
                    if (d2 == 0) continue;


                    var nonInc = !(dx > 0 && dy > 0 || dx < 0 && dy < 0);
                    var nonDec = !(dx > 0 && dy < 0 || dx < 0 && dy > 0);

                    // 異符号
                    /*
                    if (absDx > absDy)
                    {
                        if (dx > 0)
                        {
                            for (var x2 = 0; x2 < x; x2++)
                            {
                                var y2 = Round(dy * (2 * (x2 - x) - 1) + dx, 2 * dx) + y;
                                if (y2 < 0) { if (nonInc) break; }
                                else if (y2 >= h) { if (nonDec) break; }
                                else pointsLinearArray[x2 + y2 * w] -= d2;
                            }
                            for (var x2 = x + 1; x2 < w; x2++)
                            {
                                var y2 = Round(dy * (2 * (x2 - x) - 1) + dx, 2 * dx) + y;
                                if (y2 < 0) { if (nonInc) break; }
                                else if (y2 >= h) { if (nonDec) break; }
                                else pointsLinearArray[x2 + y2 * w] += d2;
                            }
                        }
                        else
                        {
                            for (var x2 = 0; x2 < x; x2++)
                            {
                                var y2 = Round(dy * (2 * (x2 - x) - 1) + dx, 2 * dx) + y;
                                if (y2 < 0) { if (nonInc) break; }
                                else if (y2 >= h) { if (nonDec) break; }
                                else pointsLinearArray[x2 + y2 * w] += d2;
                            }
                            for (var x2 = x + 1; x2 < w; x2++)
                            {
                                var y2 = Round(dy * (2 * (x2 - x) - 1) + dx, 2 * dx) + y;
                                if (y2 < 0) { if (nonInc) break; }
                                else if (y2 >= h) { if (nonDec) break; }
                                else pointsLinearArray[x2 + y2 * w] -= d2;
                            }
                        }
                    }
                    else
                    {
                        if (dy > 0)
                        {
                            for (var y2 = 0; y2 < y; y2++)
                            {
                                var y2w = y2 * w;
                                var x2 = Round(dx * (2 * (y2 - y) - 1) + dy, 2 * dy) + x;
                                if (x2 < 0) { if (nonInc) break; }
                                else if (x2 >= w) { if (nonDec) break; }
                                else pointsLinearArray[x2 + y2w] -= d2;
                            }
                            for (var y2 = y + 1; y2 < h; y2++)
                            {
                                var y2w = y2 * w;
                                var x2 = Round(dx * (2 * (y2 - y) - 1) + dy, 2 * dy) + x;
                                if (x2 < 0) { if (nonInc) break; }
                                else if (x2 >= w) { if (nonDec) break; }
                                else pointsLinearArray[x2 + y2w] += d2;
                            }
                        }
                        else
                        {
                            for (var y2 = 0; y2 < y; y2++)
                            {
                                var y2w = y2 * w;
                                var x2 = Round(dx * (2 * (y2 - y) - 1) + dy, 2 * dy) + x;
                                if (x2 < 0) { if (nonInc) break; }
                                else if (x2 >= w) { if (nonDec) break; }
                                else pointsLinearArray[x2 + y2w] += d2;
                            }
                            for (var y2 = y + 1; y2 < h; y2++)
                            {
                                var y2w = y2 * w;
                                var x2 = Round(dx * (2 * (y2 - y) - 1) + dy, 2 * dy) + x;
                                if (x2 < 0) { if (nonInc) break; }
                                else if (x2 >= w) { if (nonDec) break; }
                                else pointsLinearArray[x2 + y2w] -= d2;
                            }
                        }
                    }
                    */
                    // 等符号
                    if (absDx > absDy)
                    {
                        for (var x2 = 0; x2 < workingWidth; x2++)
                        {
                            var y2 = Round(dy * (2 * (x2 - x) - 1) + dx, 2 * dx) + y;
                            if (y2 < 0)
                            {
                                if (nonInc) break;
                            }
                            else if (y2 >= workingHeight)
                            {
                                if (nonDec) break;
                            }
                            else
                            {
                                pointsLinearArray[x2 + y2 * workingWidth] += d2;
                            }
                        }
                    }
                    else
                    {
                        for (var y2 = 0; y2 < workingHeight; y2++)
                        {
                            var y2w = y2 * workingWidth;
                            var x2 = Round(dx * (2 * (y2 - y) - 1) + dy, 2 * dy) + x;
                            if (x2 < 0)
                            {
                                if (nonInc) break;
                            }
                            else if (x2 >= workingWidth)
                            {
                                if (nonDec) break;
                            }
                            else
                            {
                                pointsLinearArray[x2 + y2w] += d2;
                            }
                        }
                    }
                }
            }

            return pointsLinearArray;
        }

        private static Bitmap LinearToBitmap(int[] linear, int width)
        {
            var widthHeight = linear.Length;
            var max = (ulong)linear.Max();
            var height = widthHeight / width;
            var output = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            try
            {
                var image = output.LockBits(new Rectangle(new Point(), output.Size), System.Drawing.Imaging.ImageLockMode.WriteOnly, output.PixelFormat);
                try
                {
                    var stride = image.Stride;
                    unsafe
                    {
                        var adr = (byte*)image.Scan0;
                        for (var y = 0; y < height; y++)
                        {
                            var adrYStride = adr + y * stride;
                            var yWidth = y * width;
                            for(var x = 0; x < width; x++)
                            {
                                var x3 = x * 3;
                                var v = (byte)(((ulong)linear[yWidth + x] * byte.MaxValue) / max);
                                adrYStride[x3] = v;
                                adrYStride[x3 + 1] = v;
                                adrYStride[x3 + 2] = v;
                            }
                        }
                    }
                }
                finally
                {
                    output.UnlockBits(image);
                }
                return output;
            }
            catch
            {
                output.Dispose();
                throw;
            }
        }

        private static int Round(int p, int q)
        {
            if(p > 0)
            {
                if(q > 0)
                {
                    return (2 * p + q) / (2 * q);
                }
                else
                {
                    return (2 * p - q) / (2 * q);
                }
            }
            else
            {
                if (q < 0)
                {
                    return (2 * p + q) / (2 * q);
                }
                else
                {
                    return (2 * p - q) / (2 * q);
                }
            }
        }
    }
}
