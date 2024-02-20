using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ZipPla
{
    public static class BitmapResizer
    {
        //private static BitmapScalingMode lastBitmapScalingMode;
        //public static BitmapScalingMode LastBitmapScalingMode { get { return lastBitmapScalingMode; } }

        public static Bitmap Load(string fileName)
        {
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                return Load(fs);
            }
        }

        public static Bitmap Load(Stream stream)
        {
            return GetBitmap(LoadAsBitmapImage(stream));
        }

        private static BitmapImage LoadAsBitmapImage(Stream stream)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            return image;
        }


        public static Bitmap CreateNew(Bitmap bmp, Size size)
        {
            return CreateNew(bmp, size.Width, size.Height);
        }

        public static Bitmap CreateNew(Bitmap bmp, int width, int height)
        {
            var result = CreateNew(bmp, (double)width / bmp.Width, (double)height / bmp.Height);
#if !AUTOBUILD
            if (result.Width != width || result.Height != height)
            {
                System.Windows.Forms.MessageBox.Show($"{result.Width}x{result.Height} != {width}x{height}");
            }
#endif
            return result;
        }

        public static Bitmap CreateNew(Bitmap bmp, double scaleX, double scaleY)
        {
            var transformedBitmap = new TransformedBitmap(GetBitmapSource(bmp), new ScaleTransform(scaleX, scaleY));
            //lastBitmapScalingMode = RenderOptions.GetBitmapScalingMode(transformedBitmap);
            return GetBitmap(transformedBitmap);
        }
        
        const System.Drawing.Imaging.PixelFormat PixelFormat_Format32bppCMYK = (System.Drawing.Imaging.PixelFormat)8207;
        
        public static PixelFormat PixelFormatConverter(System.Drawing.Imaging.PixelFormat pixelFormat)
        {
            switch(pixelFormat)
            {
                case System.Drawing.Imaging.PixelFormat.Format24bppRgb: return PixelFormats.Bgr24;
                case System.Drawing.Imaging.PixelFormat.Format32bppRgb: return PixelFormats.Bgr32;
                case System.Drawing.Imaging.PixelFormat.Format32bppArgb: return PixelFormats.Bgra32;
                case System.Drawing.Imaging.PixelFormat.Format32bppPArgb: return PixelFormats.Pbgra32;
                case System.Drawing.Imaging.PixelFormat.Format16bppArgb1555: return PixelFormats.Default;
                case System.Drawing.Imaging.PixelFormat.Format16bppGrayScale: return PixelFormats.Gray16;
                case System.Drawing.Imaging.PixelFormat.Format16bppRgb555: return PixelFormats.Bgr555;
                case System.Drawing.Imaging.PixelFormat.Format16bppRgb565: return PixelFormats.Bgr565;
                case System.Drawing.Imaging.PixelFormat.Format1bppIndexed: return PixelFormats.Indexed1;
                case System.Drawing.Imaging.PixelFormat.Format48bppRgb: return PixelFormats.Rgb48;
                case System.Drawing.Imaging.PixelFormat.Format4bppIndexed: return PixelFormats.Indexed4;
                case System.Drawing.Imaging.PixelFormat.Format64bppArgb: return PixelFormats.Rgba64;
                case System.Drawing.Imaging.PixelFormat.Format64bppPArgb: return PixelFormats.Prgba64;
                case System.Drawing.Imaging.PixelFormat.Format8bppIndexed: return PixelFormats.Indexed8;
                case PixelFormat_Format32bppCMYK: return PixelFormats.Cmyk32;
                default: return PixelFormats.Default;
            }
        }

        public static System.Drawing.Imaging.PixelFormat PixelFormatConverter(PixelFormat pixelFormat)
        {
            if (pixelFormat == PixelFormats.Bgr24) return System.Drawing.Imaging.PixelFormat.Format24bppRgb;
            if (pixelFormat == PixelFormats.Bgr32) return System.Drawing.Imaging.PixelFormat.Format32bppRgb;
            if (pixelFormat == PixelFormats.Bgra32) return System.Drawing.Imaging.PixelFormat.Format32bppArgb;
            if (pixelFormat == PixelFormats.Pbgra32) return System.Drawing.Imaging.PixelFormat.Format32bppPArgb;
            if (pixelFormat == PixelFormats.Bgr555) return System.Drawing.Imaging.PixelFormat.Format16bppRgb555;
            if (pixelFormat == PixelFormats.Bgr565) return System.Drawing.Imaging.PixelFormat.Format16bppRgb565;
            if (pixelFormat == PixelFormats.Gray16) return System.Drawing.Imaging.PixelFormat.Format16bppGrayScale;
            if (pixelFormat == PixelFormats.Indexed1) return System.Drawing.Imaging.PixelFormat.Format1bppIndexed;
            if (pixelFormat == PixelFormats.Indexed4) return System.Drawing.Imaging.PixelFormat.Format4bppIndexed;
            if (pixelFormat == PixelFormats.Indexed8) return System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
            if (pixelFormat == PixelFormats.Prgba64) return System.Drawing.Imaging.PixelFormat.Format64bppPArgb;
            if (pixelFormat == PixelFormats.Rgba64) return System.Drawing.Imaging.PixelFormat.Format64bppArgb;
            if (pixelFormat == PixelFormats.Cmyk32) return PixelFormat_Format32bppCMYK;
            return System.Drawing.Imaging.PixelFormat.Undefined;
        }

        private static bool IsIndexed(PixelFormat pixelFormat)
        {
            return pixelFormat == PixelFormats.Indexed1 || pixelFormat == PixelFormats.Indexed4 ||
                pixelFormat == PixelFormats.Indexed8 || pixelFormat == PixelFormats.Indexed2;
        }

        public static BitmapSource GetBitmapSource(Bitmap bitmap)
        {
            return GetBitmapSource(bitmap, new Rectangle(Point.Empty, bitmap.Size));
        }

        public static BitmapSource GetBitmapSource(Bitmap bitmap, Rectangle rect)
        {
            var pixelFormat1 = bitmap.PixelFormat;
            var pixelFormat2 = PixelFormatConverter(pixelFormat1);
            if (pixelFormat2 == PixelFormats.Default || IsIndexed(pixelFormat2))
            {
                pixelFormat1 = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
                pixelFormat2 = PixelFormats.Bgra32;
            }
            return GetBitmapSource(bitmap, rect, pixelFormat1, pixelFormat2);
        }

        private static BitmapSource GetBitmapSource(Bitmap bitmap, Rectangle rect,
            System.Drawing.Imaging.PixelFormat pixelFormat1, PixelFormat pixelFormat2)
        {
            var data = bitmap.LockBits(
                    rect,
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    pixelFormat1);
            try
            {
                return BitmapSource.Create(
                    data.Width, data.Height,
                    (int)bitmap.HorizontalResolution, (int)bitmap.VerticalResolution,
                    pixelFormat2,
                    BitmapPalettes.BlackAndWhite,
                    data.Scan0,
                    data.Height * data.Stride,
                    data.Stride);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }

        public static Bitmap GetBitmap(BitmapSource source)
        {
            return GetBitmap(source, PixelFormatConverter(source.Format));
        }

        private static Bitmap GetBitmap(BitmapSource source, System.Drawing.Imaging.PixelFormat pixelFormat)
        {
            var bitmap = new Bitmap(source.PixelWidth, source.PixelHeight, pixelFormat);
            try
            {
                SetToBitmap(source, bitmap, 0, pixelFormat);
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
            return bitmap;
        }

        private static void SetToBitmap(BitmapSource src, Bitmap dst, int yOffset, System.Drawing.Imaging.PixelFormat pixelFormat)
        {
            var data = dst.LockBits(new Rectangle(0, yOffset, src.PixelWidth, src.PixelHeight),
                System.Drawing.Imaging.ImageLockMode.WriteOnly, pixelFormat);
            try
            {
                src.CopyPixels(System.Windows.Int32Rect.Empty,
                    data.Scan0 + yOffset * data.Stride, data.Height * data.Stride, data.Stride);
            }
            finally
            {
                dst.UnlockBits(data);
            }
        }
    }
}
