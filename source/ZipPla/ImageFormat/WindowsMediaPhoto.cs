using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows;
using System.Windows.Media.Imaging;

namespace ZipPla
{
    public static class WindowsMediaPhoto
    {
        public static readonly string[] SupportExtensionsInLowerWithoutPeriod = new string[] { "wdp", "hdf", "jxr" };
        
        public static Bitmap Load(string fileName)
        {
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                return Load(fs);
            }
        }

        public static Bitmap Load(Stream stream)
        {
            return BitmapResizer.GetBitmap(LoadAsBitmapImage(stream));
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

        public static void Save(string fileName, Bitmap bitmap, byte qualityLevel)
        {
            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                Save(fs, bitmap, qualityLevel);
            }
        }

        public static void Save(Stream stream, Bitmap bitmap, byte qualityLevel)
        {
            var encoder = new WmpBitmapEncoder();
            //encoder.ImageQualityLevel = (float)imageQualityLevel;
            encoder.UseCodecOptions = true;
            encoder.AlphaQualityLevel = encoder.QualityLevel = qualityLevel;
            encoder.Frames.Add(BitmapFrame.Create(BitmapResizer.GetBitmapSource(bitmap)));
            encoder.Save(stream);
        }

        /*

        private static System.Drawing.Bitmap GetBitmap(System.Windows.Media.Imaging.BitmapSource source)
        {
            var bitmap = new System.Drawing.Bitmap(source.PixelWidth, source.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                var data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                try
                {
                    source.CopyPixels(System.Windows.Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
                }
                finally
                {
                    bitmap.UnlockBits(data);
                }
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
            return bitmap;
        }

        // この方法が最も良いと思われる
        private static System.Windows.Media.Imaging.BitmapSource GetBitmapSource(System.Drawing.Bitmap bitmap)
        {
            var data = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                return System.Windows.Media.Imaging.BitmapSource.Create(
                    bitmap.Width, bitmap.Height,
                    (int)bitmap.HorizontalResolution, (int)bitmap.VerticalResolution,
                    System.Windows.Media.PixelFormats.Bgra32,
                    System.Windows.Media.Imaging.BitmapPalettes.BlackAndWhite,
                    data.Scan0,
                    bitmap.Height * data.Stride,
                    data.Stride);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
        }
        */

#if DEBUG
        // 正しく変換されるが無駄な計算
        private static BitmapSource GetBitmapSourceWithPngFormat(Bitmap bitmap)
        {
            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);
                return LoadAsBitmapImage(ms);
            }
        }

        // よく使われる方法らしいがアルファチャンネルが正しく変換されない
        private static BitmapSource GetBitmapSourceWithHBitmap(Bitmap bitmap)
        {
            var result = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap
            (
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            return result;
        }
#endif
    }
}
