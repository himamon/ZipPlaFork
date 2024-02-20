using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ZipPla
{
    public delegate bool TryGetWeightDelegate1(double distance, out double weight);
    public delegate bool TryGetWeightDelegate2(double distanceAsSrc, double power, out double weight);

    public enum GammaConversion { Value1_0, Value2_2 }

    public static class LongVectorImageResizer
    {
        // 返却値が 1 よりも小さい最大の値を返す入力値の中央は (1/π)√(6ε * n^2 / (1+n^2))
        // 四捨五入を想定すると (1/π)√(3ε * n^2 / (1+n^2))
        // n を変化させて最小値をとると n = 1 で (1/π)√((3/2)ε)
        // なお、double.Epsilon は計算機イプシロンではなく正の最小の数なので注意
        private const double tryGetLanczosNWeight_ZeroThreshold = 5.4102333809763210874651254858134827373479997412536e-18;
        private static bool tryGetLanczosNWeight(double distance, out double weight, int n)
        {
            if (distance < 0) distance = -distance;
            if (distance < tryGetLanczosNWeight_ZeroThreshold)
            {
                weight = 1.0;
                return true;
            }
            else if (distance < n)
            {
                // 三角関数の計算時間がテーブル作成処理の半分を占める
                /*
                var pi_x = Math.PI * distance;
                var pi_x_n = pi_x / n;
                weight = (Math.Sin(pi_x) * Math.Sin(pi_x_n)) / (pi_x * pi_x_n);
                */

                // 三角関数の計算時間は無視できる、精度は 7 bit 程度
                /*
                var distance_n = distance / n;
                weight = fastSinPi(distance) * fastSinPi(distance_n) / ((Math.PI * Math.PI) * distance * distance_n);
                */

                // 三角関数の計算時間はテーブル作成処理の 1 割程度、ほぼ 17 bit 分の精度（±2^(-17-1) 以内の誤差）を保証
                var pi_x = Math.PI * distance;
                var pi_x_n = pi_x / n;
                weight = (fastSin(pi_x) * fastSin(pi_x_n)) / (pi_x * pi_x_n);
                
                return true;
            }
            else
            {
                weight = 0;
                return false;
            }
        }

#if DEBUG
        public static void fastSinErrorTest()
        {
            var ae = 0.0;
            var re = 0.0;
            for (var x = 0.0; x <  100; x += 0.0001)
            {
                var st = Math.Sin(x);
                var sa = fastSin(x);
                var e = Math.Abs(st - sa);
                if (e > ae) ae = e;
                e = e / Math.Abs(st);
                if (e > re) re = e;
            }
            System.Windows.Forms.MessageBox.Show($"log2 ae = {Math.Log(ae, 2)}, log2 re = {Math.Log(re, 2)}"); // おおよそ -18, -17 と表示される 
        }
#endif
        // sin をテーブルを基準に 3 次のテイラー展開で近似
        // 誤差の主要項の係数が sin になるため相対誤差が保証される
        // 係数を全て直書きして switch で分岐すると逆に遅くなった
        // つまり線形に検索する switch は数がかなり少なくても配列へのアクセスより遅いということか
        private const int fastSinTableDigits = 3; // 下記の fastSinTableCount = 6 で最大誤差 0.0000122333 ≒ 0.0001220703125 ＝ 1 / 2^(-13)
        private const int fastSinTableCount = 1 << fastSinTableDigits;
        private const double fastSinTableDx = (Math.PI / 2) / fastSinTableCount;
        private const double fastSinTableDxInv = fastSinTableCount / (Math.PI / 2);
        private static readonly double[] fastSinTable = Enumerable.Range(0, fastSinTableCount + 1).Select(n => Math.Sin(n * fastSinTableDx)).ToArray();
        private static double fastSin(double positiveX)
        {
            var index = (long)(positiveX * fastSinTableDxInv + 0.5);
            var i = index & ~(~0 << fastSinTableDigits);
            var v0 = fastSinTable[i];
            var v1 = fastSinTable[fastSinTableCount - i];
            var dx = positiveX - index * fastSinTableDx;
            switch (index & 3 << fastSinTableDigits)
            {
                case 0 << fastSinTableDigits: return v0 + dx * (v1 - dx * ((1.0 / 2.0) * v0 + dx * (1.0 / 6.0) * v1));
                case 1 << fastSinTableDigits: return v1 - dx * (v0 + dx * ((1.0 / 2.0) * v1 - dx * (1.0 / 6.0) * v0));
                case 2 << fastSinTableDigits: return -v0 - dx * (v1 - dx * ((1.0 / 2.0) * v0 + dx * (1.0 / 6.0) * v1));
                default: return dx * (v0 + dx * ((1.0 / 2.0) * v1 - dx * (1.0 / 6.0) * v0)) - v1;
            }
        }

        /*
        private static double fastSinPi(double x)
        {
            // sin(πx) == sin(πy/2 + π/2 + πn) == (-1)^n * cos(πy/2) となる (y, n) を -1 ≦ y ≦ 1, n ∈ {整数} の範囲で探す
            // x == θ + n で θ = y/2 + 1/2 が x の小数部分、n が整数部分とみなせるのでこれを利用
            var n = Math.Floor(x);
            var yHalf = (x - n) - 0.5;

            // この条件で |cos(πy/2) - (1 - 1.224 y^2 + 0.224 y^4)| < 0.001 なのでこれを利用する（※）
            const double t = 1.217508184; // Sinc 関数の計算に用いる場合相対誤差最小化が妥当
            // const double t = 1.224; // 絶対誤差の最小化
            var yHalf2 = yHalf * yHalf;
            if (((long)n & 1) == 0)
            {
                return 1 - yHalf2 * (t * 4 - ((t - 1) * 16) * yHalf2);
            }
            else
            {
                return -1 + yHalf2 * (t * 4 - ((t - 1) * 16) * yHalf2);
            }

            // ※ この場合の実際の誤差の最大値は 0.000920147 程度
            // 1 - (t * y^2 + (1 - t) * y^4) 型の近似で絶対誤差基準の t の最適値は t = 1.22400815 程度で
            // その時の誤差の最大値は  0.000918799 程度なので t = 1.224 が事実上の最適値とみなせる
            // 相対誤差については t = 1.2175081841 で最大相対誤差は 0.00370048 であり
            // t = 1.224 のときの最大相対誤差は 0.0119661 まで大きくなるので
            // 用途に応じて使い分けるかのが望ましい
            // t = 1.2175081841 での最大絶対誤差は 0.00215913 程度
            // 誤差の平均がゼロになるのは t = 1.22535 程度のとき
            // cos(πy/2) の代わりに sin(πy)/(πy) で同じことをしても
            // 絶対誤差は 0.007 程度までしか下げられないのでこの方法は偶然の産物と言ってよいのだろうか
            // それとも三角関数のゼロ点が変曲点になっていることから説明可能な必然の結果なのか
            // 近似関数がゼロ点で変曲点になるような t は t = 1.2
        }
        */
        
        public static Bitmap Lanczos(int n, Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Size size, bool parallel, Func<bool> cancel = null)
        {
            return CreateNew(src, pixelsPerPixelToBeLeft, gamma, size, (double distance, out double weight) =>
                tryGetLanczosNWeight(distance, out weight, n), parallel, cancel);
        }

        public static Bitmap Lanczos(int n, Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Size size, bool parallel, BackgroundWorker backgroundWorker)
        {
            return CreateNew(src, pixelsPerPixelToBeLeft, gamma, size, (double distance, out double weight) =>
                tryGetLanczosNWeight(distance, out weight, n), parallel, backgroundWorker);
        }

        public static void Lanczos(int n, Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Bitmap dst, Rectangle dstRect, bool parallel, BackgroundWorker backgroundWorker = null)
        {
            Draw(dst, dstRect, src, pixelsPerPixelToBeLeft, gamma, (double distance, out double weight) => tryGetLanczosNWeight(distance, out weight, n),
                parallel, backgroundWorker);
        }

#if !AUTOBUILD
        
        private static readonly double[] tryGetSincHalfCutWeight_Halfs = new double[] {
            0.613207 / 1, 1.55776 / 2, 2.53778 / 3, 3.52784 / 4, 4.52197 / 5, 5.51811 / 6,
            6.5154 / 7, 7.51338 / 8, 8.51183 / 9, 9.5106 / 10, 10.5096 / 11, 11.5088 / 12};
        private static bool tryGetSincHalfCutWeight(double distance, out double weight, int n)
        {
            if (distance < 0) distance = -distance;
            if (distance < tryGetLanczosNWeight_ZeroThreshold)
            {
                weight = 1.0;
                return true;
            }
            else if (distance < n)
            {
                distance *= tryGetSincHalfCutWeight_Halfs[n - 1];
                var pi_x = Math.PI * distance;
                var pi_x_n = pi_x / n;
                weight = (fastSin(pi_x) * fastSin(pi_x_n)) / (pi_x * pi_x_n);

                return true;
            }
            else
            {
                weight = 0;
                return false;
            }
        }

        public static Bitmap SincHalfCut(int n, Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Size size, bool parallel, Func<bool> cancel = null)
        {
            return CreateNew(src, pixelsPerPixelToBeLeft, gamma, size, (double distance, out double weight) =>
                tryGetSincHalfCutWeight(distance, out weight, n), parallel, cancel);
        }

        public static void SincHalfCut(int n, Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Bitmap dst, Rectangle dstRect, bool parallel, BackgroundWorker backgroundWorker = null)
        {
            Draw(dst, dstRect, src, pixelsPerPixelToBeLeft, gamma, (double distance, out double weight) => tryGetSincHalfCutWeight(distance, out weight, n),
                parallel, backgroundWorker);
        }

        private static bool tryGetFewAliasingLanczosNWeight(double distanceAsSrc, double power, out double weight, int n)
        {
            double distance;
            if (power >= 1)
            {
                distance = distanceAsSrc;
            }
            else
            {
                var x = (double)n / (n + 1);
                power *= power > x ? power : x;
                distance = distanceAsSrc * power;
            }
            if (distance < 0) distance = -distance;
            if (distance < tryGetLanczosNWeight_ZeroThreshold)
            {
                weight = 1.0;
                return true;
            }
            else if (distance < n)
            {
                var pi_x = Math.PI * distance;
                var pi_x_n = pi_x / n;
                weight = (fastSin(pi_x) * fastSin(pi_x_n)) / (pi_x * pi_x_n);

                return true;
            }
            else
            {
                weight = 0;
                return false;
            }
        }
        
        public static Bitmap FewAliasingLanczos(int n, Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Size size, bool parallel, Func<bool> cancel = null)
        {
            return CreateNew(src, pixelsPerPixelToBeLeft, gamma, size, (double distanceAsSrc, double power, out double weight) =>
                tryGetFewAliasingLanczosNWeight(distanceAsSrc, power, out weight, n), parallel, cancel);
        }

        public static void FewAliasingLanczos(int n, Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Bitmap dst, Rectangle dstRect, bool parallel, BackgroundWorker backgroundWorker = null)
        {
            Draw(dst, dstRect, src, pixelsPerPixelToBeLeft, gamma, (double distanceAsSrc, double power, out double weight) =>
                tryGetFewAliasingLanczosNWeight(distanceAsSrc, power, out weight, n), parallel, backgroundWorker);
        }

#endif

        public static Bitmap Spline4(Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Size size, bool parallel, Func<bool> cancel = null)
        {
            return CreateNew(src, pixelsPerPixelToBeLeft, gamma, size, TryGetSpline4Weight, parallel, cancel);
        }

        public static Bitmap Spline4(Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Size size, bool parallel, BackgroundWorker backgroundWorker)
        {
            return CreateNew(src, pixelsPerPixelToBeLeft, gamma, size, TryGetSpline4Weight, parallel, backgroundWorker);
        }

        public static void Spline4(Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Bitmap dst, Rectangle dstRect, bool parallel, BackgroundWorker backgroundWorker = null)
        {
            Draw(dst, dstRect, src, pixelsPerPixelToBeLeft, gamma, TryGetSpline4Weight, parallel, backgroundWorker);
        }

        public static Bitmap Spline16(Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Size size, bool parallel, Func<bool> cancel = null)
        {
            return CreateNew(src, pixelsPerPixelToBeLeft, gamma, size, TryGetSpline16Weight, parallel, cancel);
        }

        public static Bitmap Spline16(Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Size size, bool parallel, BackgroundWorker backgroundWorker)
        {
            return CreateNew(src, pixelsPerPixelToBeLeft, gamma, size, TryGetSpline16Weight, parallel, backgroundWorker);
        }

        public static void Spline16(Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Bitmap dst, Rectangle dstRect, bool parallel, BackgroundWorker backgroundWorker = null)
        {
            Draw(dst, dstRect, src, pixelsPerPixelToBeLeft, gamma, TryGetSpline16Weight, parallel, backgroundWorker);
        }

        public static Bitmap Spline36(Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Size size, bool parallel, Func<bool> cancel = null)
        {
            return CreateNew(src, pixelsPerPixelToBeLeft, gamma, size, TryGetSpline36Weight, parallel, cancel);
        }

        public static Bitmap Spline36(Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Size size, bool parallel, BackgroundWorker backgroundWorker)
        {
            return CreateNew(src, pixelsPerPixelToBeLeft, gamma, size, TryGetSpline36Weight, parallel, backgroundWorker);
        }

        public static void Spline36(Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Bitmap dst, Rectangle dstRect, bool parallel, BackgroundWorker backgroundWorker = null)
        {
            Draw(dst, dstRect, src, pixelsPerPixelToBeLeft, gamma, TryGetSpline36Weight, parallel, backgroundWorker);
        }
        
        public static Bitmap Spline64(Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Size size, bool parallel, Func<bool> cancel = null)
        {
            return CreateNew(src, pixelsPerPixelToBeLeft, gamma, size, TryGetSpline64Weight, parallel, cancel);
        }

        public static Bitmap Spline64(Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Size size, bool parallel, BackgroundWorker backgroundWorker)
        {
            return CreateNew(src, pixelsPerPixelToBeLeft, gamma, size, TryGetSpline64Weight, parallel, backgroundWorker);
        }

        public static void Spline64(Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Bitmap dst, Rectangle dstRect, bool parallel, BackgroundWorker backgroundWorker = null)
        {
            Draw(dst, dstRect, src, pixelsPerPixelToBeLeft, gamma, TryGetSpline64Weight, parallel, backgroundWorker);
        }

        public static Bitmap AreaAverage(Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Size size, bool parallel, BackgroundWorker backgroundWorker = null)
        {
            if (size.Width % src.Width == 0 && size.Height % src.Height == 0)
            {
                return nearestNeighbor(src, size, parallel, backgroundWorker, accurate: false);
            }
            else
            {
                return CreateNew(src, pixelsPerPixelToBeLeft, gamma, size, TryGetAreaAverageWeight, parallel, backgroundWorker);
            }
        }

        public static void AreaAverage(Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Bitmap dst, Rectangle dstRect, bool parallel, BackgroundWorker backgroundWorker = null)
        {
            if (dstRect.Width % src.Width == 0 && dstRect.Height % src.Height == 0)
            {
                DrawResizedBitmapByNearestNeighbor(src, 0, 0, src.Width, src.Height, dst,
                    dstRect.X, dstRect.Y, dstRect.Width, dstRect.Height,
                    parallel, backgroundWorker, accurate: false);
            }
            else
            {
                Draw(dst, dstRect, src, pixelsPerPixelToBeLeft, gamma, TryGetAreaAverageWeight, parallel, backgroundWorker);
            }
        }

        private static bool TryGetSpline4Weight(double distance, out double weight)
        {
            var absX = Math.Abs(distance);
            if (absX < 1)
            {
                weight = 1.0 - absX;
                return true;
            }
            weight = 0;
            return false;
        }
        private static bool TryGetSpline16Weight(double distance, out double weight)
        {
            var absX = Math.Abs(distance);
            if (absX <= 1)
            {
                weight = ((absX - 9.0 / 5.0) * absX - 1.0 / 5.0) * absX + 1.0;
                return true;
            }
            if (absX < 2)
            {
                weight = ((-1.0 / 3.0 * absX + 9.0 / 5.0) * absX - 46.0 / 15.0) * absX + 8.0 / 5.0;
                return true;
            }
            weight = 0;
            return false;
        }

        private static bool TryGetSpline36Weight(double distance, out double weight)
        {
            var absX = Math.Abs(distance);
            if (absX <= 1)
            {
                weight = ((13.0 / 11.0 * absX - 453.0 / 209.0) * absX - 3.0 / 209.0) * absX + 1.0;
                return true;
            }
            if (absX <= 2)
            {
                weight = ((-6.0 / 11.0 * absX + 612.0 / 209.0) * absX - 1038.0 / 209.0) * absX + 540.0 / 209.0;
                return true;
            }
            if (absX < 3)
            {
                weight = ((1.0 / 11.0 * absX - 159.0 / 209.0) * absX + 434.0 / 209.0) * absX - 384.0 / 209.0;
                return true;
            }

            weight = 0;
            return false;
        }

        private static bool TryGetSpline64Weight(double distance, out double weight)
        {
            var absX = Math.Abs(distance);
            if (absX <= 1)
            {
                weight = ((49.0 / 41.0 * absX - 6387.0 / 2911.0) * absX - 3.0 / 2911.0) * absX + 1.0;
                return true;
            }
            if (absX <= 2)
            {
                weight = ((-24.0 / 41.0 * absX + 9144.0 / 2911.0) * absX - 15504.0 / 2911.0) * absX + 8064.0 / 2911.0;
                return true;
            }
            if (absX <= 3)
            {
                weight = ((6.0 / 41.0 * absX - 3564.0 / 2911.0) * absX + 9726.0 / 2911.0) * absX - 8604.0 / 2911.0;
                return true;
            }
            if (absX < 4)
            {
                weight = ((-1.0 / 41.0 * absX + 807.0 / 2911.0) * absX - 3022.0 / 2911.0) * absX + 3720.0 / 2911.0;
                return true;
            }

            weight = 0;
            return false;
        }

        private static bool TryGetAreaAverageWeight(double distanceAsSrc, double power, out double weight)
        {
            weight = 0.5 + (0.5 - Math.Abs(distanceAsSrc)) * power;
            if (power > 1) power = 1;
            if (weight >= power)
            {
                weight = power;
                return true;
            }
            else
            {
                return weight > 0;
            }
        }

        public static Bitmap CreateNew(Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Size size, TryGetWeightDelegate1 tryGet, bool parallel, BackgroundWorker backgroundWorker)
        {
            return CreateNew(src, pixelsPerPixelToBeLeft, gamma, size, tryGet, null, parallel, backgroundWorker == null ? null as Func<bool> : () => backgroundWorker.CancellationPending);
        }
        public static Bitmap CreateNew(Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Size size, TryGetWeightDelegate2 tryGet, bool parallel, BackgroundWorker backgroundWorker)
        {
            return CreateNew(src, pixelsPerPixelToBeLeft, gamma, size, null, tryGet, parallel, backgroundWorker == null ? null as Func<bool> : () => backgroundWorker.CancellationPending);
        }
        public static Bitmap CreateNew(Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Size size, TryGetWeightDelegate1 tryGet, bool parallel, Func<bool> cancel)
        {
            return CreateNew(src, pixelsPerPixelToBeLeft, gamma, size, tryGet, null, parallel, cancel);
        }
        public static Bitmap CreateNew(Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Size size, TryGetWeightDelegate2 tryGet, bool parallel, Func<bool> cancel)
        {
            return CreateNew(src, pixelsPerPixelToBeLeft, gamma, size, null, tryGet, parallel, cancel);
        }
        private static Bitmap CreateNew(Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, Size size, TryGetWeightDelegate1 tryGet1, TryGetWeightDelegate2 tryGet2, bool parallel, Func<bool> cancel)
        {
            var pixelFormat = src.PixelFormat;
            var is8bpp = pixelFormat == PixelFormat.Format8bppIndexed;
            if (!is8bpp && pixelFormat != PixelFormat.Format24bppRgb)
            {
                throw new ArgumentException(null, "src");
            }
            var result = new Bitmap(size.Width, size.Height, pixelFormat);
            try
            {
                if (is8bpp) result.Palette = src.Palette;
                if (tryGet1 != null)
                {
                    Draw(result, src, pixelsPerPixelToBeLeft, gamma, tryGet1, parallel, cancel);
                }
                else
                {
                    Draw(result, src, pixelsPerPixelToBeLeft, gamma, tryGet2, parallel, cancel);
                }
                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }

        private static void Draw(Bitmap dst, Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, TryGetWeightDelegate1 tryGetWeight, bool parallel, Func<bool> cancel)
        {
            draw(dst, new Rectangle(Point.Empty, dst.Size), src, pixelsPerPixelToBeLeft, gamma, tryGetWeight, parallel, cancel);
        }
        private static void Draw(Bitmap dst, Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, TryGetWeightDelegate2 tryGetWeight, bool parallel, Func<bool> cancel)
        {
            draw(dst, new Rectangle(Point.Empty, dst.Size), src, pixelsPerPixelToBeLeft, gamma, tryGetWeight, parallel, cancel);
        }
        private static void Draw(Bitmap dst, Rectangle dstRect, Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, TryGetWeightDelegate1 tryGetWeight, bool parallel, BackgroundWorker backgroundWorker)
        {
            draw(dst, dstRect, src, pixelsPerPixelToBeLeft, gamma, tryGetWeight, parallel, backgroundWorker == null ? null as Func<bool> : () => backgroundWorker.CancellationPending);
        }
        private static void Draw(Bitmap dst, Rectangle dstRect, Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, TryGetWeightDelegate1 tryGetWeight, bool parallel, Func<bool> cancel)
        {
            draw(dst, dstRect, src, pixelsPerPixelToBeLeft, gamma, tryGetWeight, parallel, cancel);
        }
        private static void Draw(Bitmap dst, Rectangle dstRect, Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, TryGetWeightDelegate2 tryGetWeight, bool parallel, BackgroundWorker backgroundWorker)
        {
            draw(dst, dstRect, src, pixelsPerPixelToBeLeft, gamma, tryGetWeight, parallel, backgroundWorker == null ? null as Func<bool> : () => backgroundWorker.CancellationPending);
        }
        private static void Draw(Bitmap dst, Rectangle dstRect, Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, TryGetWeightDelegate2 tryGetWeight, bool parallel, Func<bool> cancel)
        {
            draw(dst, dstRect, src, pixelsPerPixelToBeLeft, gamma, tryGetWeight, parallel, cancel);
        }

        private static void draw(Bitmap dst, Rectangle dstRect, Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, TryGetWeightDelegate1 tryGetWeight, bool parallel, Func<bool> cancel)
        {
            draw(dst, dstRect, src, new Rectangle(Point.Empty, src.Size), GetSuitableStep(src.Size, dstRect.Size, pixelsPerPixelToBeLeft), gamma, tryGetWeight, parallel, cancel: cancel);
        }

        private static void draw(Bitmap dst, Rectangle dstRect, Bitmap src, int pixelsPerPixelToBeLeft, GammaConversion gamma, TryGetWeightDelegate2 tryGetWeight, bool parallel, Func<bool> cancel)
        {
            draw(dst, dstRect, src, new Rectangle(Point.Empty, src.Size), GetSuitableStep(src.Size, dstRect.Size, pixelsPerPixelToBeLeft), gamma, tryGetWeight, parallel, cancel: cancel);
        }

        public static Size GetSuitableStep(Size srcSize, Size dstSize, int pixelsPerPixelToBeLeft)
        {
            return new Size(GetSuitableStep(srcSize.Width, dstSize.Width, pixelsPerPixelToBeLeft), GetSuitableStep(srcSize.Height, dstSize.Height, pixelsPerPixelToBeLeft));
        }

        public static int GetSuitableStep(int srcLength, int dstLength, int pixelsPerPixelToBeLeft)
        {
            if (pixelsPerPixelToBeLeft <= 0 || pixelsPerPixelToBeLeft > int.MaxValue / dstLength) return 1;
            var step = srcLength / (pixelsPerPixelToBeLeft * dstLength);
            return step <= 1 ? 1 : step;
        }

        private static void draw(Bitmap dst, Rectangle dstRect, Bitmap src, Rectangle srcRect, Size srcStep, GammaConversion gamma, TryGetWeightDelegate1 tryGetWeight, bool parallel, Func<bool> cancel)
        {
            draw(dst, dstRect, src, srcRect, srcStep, gamma, tryGetWeight, null, parallel, cancel);
        }

        private static void draw(Bitmap dst, Rectangle dstRect, Bitmap src, Rectangle srcRect, Size srcStep, GammaConversion gamma, TryGetWeightDelegate2 tryGetWeight, bool parallel, Func<bool> cancel)
        {
            draw(dst, dstRect, src, srcRect, srcStep, gamma, null, tryGetWeight, parallel, cancel);
        }

        const int grayLinearDigits = 18;

        private static void draw(Bitmap dst, Rectangle dstRect, Bitmap src, Rectangle srcRect, Size srcStep, GammaConversion gamma, TryGetWeightDelegate1 tryGetWeight1, TryGetWeightDelegate2 tryGetWeight2, bool parallel, Func<bool> cancel)
        {
            var srcPixelFormat = src.PixelFormat;
            var dstPixelFormat = dst.PixelFormat;
            var dstBytesPerPixel = dstPixelFormat == PixelFormat.Format8bppIndexed ? 1 : 3;
            
            ulong[] hWeights = null;
            ulong[] vWeights = null;
            int srcBytesPerPixel;
            int dstRightBound;
            int dstBottomBound;
            int rx2 = 0, ry2 = 0;

            bool gammaIs1 = gamma == GammaConversion.Value1_0;
            /*
            var sw = new System.Diagnostics.Stopwatch();
            var s = "";

            getWeightArray(out rx2, false, tryGetWeight1, tryGetWeight2, srcRect.Width, dstRect.Width, srcStep.Width, 12);
            sw.Restart();
            getWeightArray(out rx2, false, tryGetWeight1, tryGetWeight2, srcRect.Width, dstRect.Width, srcStep.Width, 12);
            sw.Stop(); s += rx2 + " - " + sw.Elapsed + "\n";
            System.Windows.Forms.MessageBox.Show(s);
            */
            /*
            getWeightArrayOld(out rx2, false, tryGetWeight1, tryGetWeight2, srcRect.Width, dstRect.Width, srcStep.Width, 12);
            sw.Restart();
            getWeightArrayOld(out rx2, false, tryGetWeight1, tryGetWeight2, srcRect.Width, dstRect.Width, srcStep.Width, 12);
            sw.Stop(); s += rx2 + " - " + sw.Elapsed + "\n";

            System.Windows.Forms.MessageBox.Show(s);
            */
            //sw.Start();
            IntPtr toLinearPointer = IntPtr.Zero;
            if (srcPixelFormat == PixelFormat.Format24bppRgb)
            {
                if (gammaIs1)
                {
                    if (parallel)
                    {
                        Parallel.Invoke(
                            () => hWeights = getWeightArray(out rx2, false, tryGetWeight1, tryGetWeight2, srcRect.Width, dstRect.Width, srcStep.Width, 12),
                            () => vWeights = getWeightArray(out ry2, false, tryGetWeight1, tryGetWeight2, srcRect.Height, dstRect.Height, srcStep.Height, 0));
                    }
                    else
                    {
                        hWeights = getWeightArray(out rx2, false, tryGetWeight1, tryGetWeight2, srcRect.Width, dstRect.Width, srcStep.Width, 12);
                        vWeights = getWeightArray(out ry2, false, tryGetWeight1, tryGetWeight2, srcRect.Height, dstRect.Height, srcStep.Height, 0);
                    }
                }
                else
                {
                    if (parallel)
                    {
                        Parallel.Invoke(
                            () => hWeights = getWeightArray(out rx2, false, tryGetWeight1, tryGetWeight2, srcRect.Width, dstRect.Width, srcStep.Width, 7),
                            () => vWeights = getWeightArray(out ry2, false, tryGetWeight1, tryGetWeight2, srcRect.Height, dstRect.Height, srcStep.Height, 7));
                    }
                    else
                    {
                        hWeights = getWeightArray(out rx2, false, tryGetWeight1, tryGetWeight2, srcRect.Width, dstRect.Width, srcStep.Width, 7);
                        vWeights = getWeightArray(out ry2, false, tryGetWeight1, tryGetWeight2, srcRect.Height, dstRect.Height, srcStep.Height, 7);
                    }
                    toLinearPointer = toLinear131413Array2_2;
                }
                srcBytesPerPixel = 3;
                dstRightBound = dstRect.Width;
                dstBottomBound = dstRect.Height;
            }
            else if (srcPixelFormat == PixelFormat.Format8bppIndexed)
            {
                if (gammaIs1)
                {
                    if (parallel)
                    {
                        Parallel.Invoke(
                            () => hWeights = getWeightArray(out rx2, true, tryGetWeight1, tryGetWeight2, srcRect.Width, dstRect.Width, srcStep.Width, 7),
                            () => vWeights = getWeightArray(out ry2, true, tryGetWeight1, tryGetWeight2, srcRect.Height, dstRect.Height, srcStep.Height, 7));
                    }
                    else
                    {
                        hWeights = getWeightArray(out rx2, true, tryGetWeight1, tryGetWeight2, srcRect.Width, dstRect.Width, srcStep.Width, 7);
                        vWeights = getWeightArray(out ry2, true, tryGetWeight1, tryGetWeight2, srcRect.Height, dstRect.Height, srcStep.Height, 7);
                    }
                    dstBottomBound = (dstRect.Height + 1) / 2;
                }
                else
                {
                    if (parallel)
                    {
                        Parallel.Invoke(
                            () => hWeights = getWeightArray(out rx2, true, tryGetWeight1, tryGetWeight2, srcRect.Width, dstRect.Width, srcStep.Width, 31 - grayLinearDigits),
                            () => vWeights = getWeightArray(out ry2, false, tryGetWeight1, tryGetWeight2, srcRect.Height, dstRect.Height, srcStep.Height, 31 - grayLinearDigits));
                    }
                    else
                    {
                        hWeights = getWeightArray(out rx2, true, tryGetWeight1, tryGetWeight2, srcRect.Width, dstRect.Width, srcStep.Width, 31 - grayLinearDigits);
                        vWeights = getWeightArray(out ry2, false, tryGetWeight1, tryGetWeight2, srcRect.Height, dstRect.Height, srcStep.Height, 31 - grayLinearDigits);
                    }
                    dstBottomBound = dstRect.Height;
                    toLinearPointer = toLinear18Array2_2;
                }
                srcBytesPerPixel = 1;
                dstRightBound = (dstRect.Width + 1) / 2;
            }
            else throw new ArgumentException(null, "src");

            //sw.Stop(); s += sw.Elapsed + "\n"; sw.Restart();
            long txx, tx0, txd, tyy, ty0, tyd;
            getTransform(out txx, out tx0, out txd, dstRect.Width, srcRect.Width, srcStep.Width);
            getTransform(out tyy, out ty0, out tyd, dstRect.Height, srcRect.Height, srcStep.Height);

            var rx = rx2 / 2;
            var ry = ry2 / 2;

            int hStep = 0, vStep = 0;
            if (parallel)
            {
                var pc = Environment.ProcessorCount;
                if (pc <= 1)
                {
                    parallel = false;
                }
                else
                {
                    int srcRightBound, srcBottomBound;
                    if (srcPixelFormat == PixelFormat.Format8bppIndexed)
                    {
                        srcRightBound = (srcRect.Width + 1) / 2;
                        if (gammaIs1)
                        {
                            srcBottomBound = (srcRect.Height + 1) / 2;
                        }
                        else
                        {
                            srcBottomBound = srcRect.Height;
                        }
                    }
                    else
                    {
                        srcRightBound = srcRect.Width;
                        srcBottomBound = srcRect.Height;
                    }
                    long sampleW = (srcRightBound - 1) / srcStep.Width + 1;
                    long sampleH = (srcBottomBound - 1) / srcStep.Height + 1;
                    var c = Math.Pow((double)(sampleW * sampleH) / (rx2 * ry2 * (pc - 1)), 1.0 / 3.0);
                    hStep = Math.Max((int)(c * rx2 * dstRightBound / sampleW + 0.5), 1);
                    vStep = Math.Max((int)(c * ry2 * dstBottomBound / sampleH + 0.5), 1);
                    parallel = hStep * 2 <= dstRightBound || vStep * 2 <= dstBottomBound;
                }
            }

            //sw.Stop(); s += sw.Elapsed + "\n"; sw.Restart();
            var srcData = src.LockBits(srcRect, ImageLockMode.ReadOnly, srcPixelFormat);
            try
            {
                var dstData = dst.LockBits(dstRect, ImageLockMode.WriteOnly, dstPixelFormat);
                try
                {
                    int buffer1dSize, buffer2dSize;
                    unsafe
                    {
                        fixed (ulong* cx0 = hWeights)
                        fixed (ulong* cy0 = vWeights)
                        {
                            var toLinear = toLinearPointer == IntPtr.Zero ? null : (ulong*) toLinearPointer;

                            if (parallel)
                            {
                                getBufferSize(out buffer1dSize, out buffer2dSize, hStep, txx, txd, rx, ry);
                                var bufferSize = buffer1dSize + buffer2dSize;
                                var buuferByteSize = sizeof(ulong) * bufferSize;
                                var hCount = (dstRightBound - 1) / hStep + 1;
                                var vCount = (dstBottomBound - 1) / vStep + 1;
                                var cx0_ = cx0;
                                var cy0_ = cy0;
                                var cancelExists = cancel != null;
                                Parallel.For(0, hCount * vCount,
                                    () => Marshal.AllocCoTaskMem(buuferByteSize),
                                (ij, loop, pointer) =>
                                {
                                    if (cancelExists && cancel())
                                    {
                                        loop.Break();
                                        return pointer;
                                    }
                                    
                                    var buff1d = (ulong*)pointer;
                                    var buff2d = buff1d + buffer1dSize;

                                    var i = ij % hCount;
                                    var j = ij / hCount;

                                    var x0 = i * hStep;
                                    var x1 = Math.Min(x0 + hStep, dstRightBound);
                                    var y0 = j * vStep;
                                    var y1 = Math.Min(y0 + vStep, dstBottomBound);

                                    core(
                                        dstData.Scan0, x0, x1, y0, y1, dstData.Stride, dstBytesPerPixel, dstRect.Width, dstRect.Height,
                                        srcData.Scan0, txx, tx0, txd, srcStep.Width, tyy, ty0, tyd, srcStep.Height,
                                        srcData.Stride, srcBytesPerPixel, srcRect.Width, srcRect.Height,
                                        toLinear, rx, ry, cx0_, cy0_, buff1d, buff2d, cancel: null);

                                    return pointer;
                                }, pointer => Marshal.FreeCoTaskMem(pointer));
                            }
                            else
                            {
                                getBufferSize(out buffer1dSize, out buffer2dSize, dstRightBound, txx, txd, rx, ry);
                                fixed (ulong* buff1d = new ulong[buffer1dSize])
                                fixed (ulong* buff2d = new ulong[buffer2dSize])
                                {
                                    core(
                                        dstData.Scan0, 0, dstRightBound, 0, dstBottomBound, dstData.Stride, dstBytesPerPixel, dstRect.Width, dstRect.Height,
                                        srcData.Scan0, txx, tx0, txd, srcStep.Width, tyy, ty0, tyd, srcStep.Height,
                                        srcData.Stride, srcBytesPerPixel, srcRect.Width, srcRect.Height,
                                        toLinear, rx, ry, cx0, cy0, buff1d, buff2d, cancel);
                                }
                            }
                        }
                    }
                    //sw.Stop(); s += sw.Elapsed + "\n"; System.Windows.Forms.MessageBox.Show(s);
                }
                finally
                {
                    dst.UnlockBits(dstData);
                }
            }
            finally
            {
                src.UnlockBits(srcData);
            }
        }
        
        private static void getTransform(out long t, out long t0, out long td, int dst, int src, int step)
        {
            t = src * 2;
            t0 = src - dst + 2 * dst * step - 1;
            td = dst * step * 2;
        }

        private static void getBufferSize(out int buffer1dSize, out int buffer2dSize,
            int dstWidth,
            long txx, long txd,
            int rx, int ry
            )
        {
            var srcWidthUBound = (int)((txx * (dstWidth - 1) - 1) / txd + 2);
            buffer1dSize = srcWidthUBound + rx * 2;
            buffer2dSize = dstWidth * ry * 2;
        }

        private static unsafe void core(
            IntPtr dstScan0,
            long x0, long x1, long y0, long y1, long dstStride, long dstBytesPerPixel, long dstDataWidth, long dstDataHeight,
            IntPtr srcScan0,
            long txx, long tx0, long txd, long srcXStep,
            long tyy, long ty0, long tyd, long srcYStep,
            long srcStride, long srcBytesPerPixel, long srcDataWidth, long srcDataHeight,
            ulong* toLinear,
            int rx, int ry, ulong* cx0, ulong* cy0,
            ulong* buff1d, ulong* buff2d,
            Func<bool> cancel)
        {
            var dstScan0_ = (byte*)dstScan0;
            var dstX0Bytes = x0 * dstBytesPerPixel;
            var dstScan0X0 = dstScan0_ + dstX0Bytes;
            var dstX1Bytes = (dstDataWidth - x0) * dstBytesPerPixel - 1;
            var dstScan0X0Mirror01 = dstScan0_ + dstX1Bytes;
            var dstScan0Mirror = dstScan0_ + (dstDataHeight - 1) * dstStride;
            var dstScan0X0Mirror10 = dstScan0Mirror + dstX0Bytes;// + dstBytesPerPixel - 1;
            var dstScan0X0Mirror11 = dstScan0Mirror + dstX1Bytes;
            long dstWidth = x1 - x0;
            var dst24bpp = dstBytesPerPixel == 3;
            long rx2 = rx * 2;
            long ry2 = ry * 2;
            var srcScan0_ = (byte*)srcScan0;
            var srcX0 = (x0 * txx + tx0) / txd;
            var srcX0rx = srcX0 - rx;
            const long srcXL = 0;
            var srcXStart = Math.Max(srcX0rx, srcXL);
            var srcBytesPerXSample = srcBytesPerPixel * srcXStep;
            var srcBytesPerXSample11 = srcBytesPerXSample - srcBytesPerPixel + 1;
            var srcXStepIsOne = srcXStep == 1;
            var srcXStartBytes = srcXStart * srcBytesPerXSample;
            var srcX1 = ((x1 - 1) * txx + tx0) / txd + 1;
            var srcX1rx = srcX1 + rx;
            var srcStrideStep = srcStride * srcYStep;
            var srcXU = (srcDataWidth - 1) / srcXStep + 1;
            //var srcXStartBytesMirror = (srcXU - srcXStart) * srcBytesPerXSample - 1; // ミラーは逆からステップを数えないとピクセルがズレる
            var srcXStartBytesMirror = srcDataWidth * srcBytesPerPixel - srcXStart * srcBytesPerXSample - 1;
            var srcXStop = Math.Min(srcX1rx, srcXU);
            var srcY0 = (y0 * tyy + ty0) / tyd;
            var srcY0ry = srcY0 - ry;
            var srcY0ry2 = srcY0 - ry2;
            var src24bpp = srcBytesPerPixel == 3;
            const long srcYL = 0;
            var srcYStart = Math.Max(srcY0ry, srcYL);
            var srcY1 = ((y1 - 1) * tyy + ty0) / tyd + 1;
            var srcY1ry = srcY1 + ry;
            var srcYU = (srcDataHeight - 1) / srcYStep + 1;
            //var srcScan0Mirror = srcScan0_ + (srcYU - 1) * srcStrideStep; // ミラーは逆からステップを数えないとピクセルがズレる
            var srcScan0Mirror = srcScan0_ + srcDataHeight * srcStride - srcStrideStep; // カラーでミラーリングするなら→を追加 + srcBytesPerPixel - 1;
            var srcYStop = Math.Min(srcY1ry, srcYU);
            var srcYStop1 = srcYStop - 1;
            long dstY = y0;
            var buff2dSize = dstWidth * ry2;
            var iy0 = (2 - ry) * tyd - ty0 + tyy - 1;
            var cancelExists = cancel != null;
            const ulong unt = 1L | 1L << 21 | 1L << 43; // G だけ精度を高めるので RGB 一致のモノクロ画像入力には不向き
            const ulong unt2 = 1L | 1L << 22 | 1L << 43;
            var linearize = toLinear != null;
            int interShift = linearize ? 7 : getSuitabgleDigits(ry2); // 24bit の場合のみ使用
            int interBits = 20 - interShift;
            //ulong interDesignRoundOffset = unt2 << 19 | unt << (interBits - 1);
            ulong interDesignRoundOffset = unt2 << 19 | unt << (interShift - 1);
            ulong interBitsMask = ~(~0LU << (interBits + 1));
            ulong interMask = unt * interBitsMask | unt2 * interBitsMask;
            ulong interSignOffset = unt2 << (interBits - 1);
            var buff1d_srcX0rx = buff1d - srcX0rx;
            var buff1d_srcX0 = buff1d - srcX0;
            var buff2d_x0Ry2 = buff2d - x0 * ry2;
            var yUpScaling = tyy < ty0;
            var toLinear1 = linearize ? toLinear + 256 : null;
            var toLinear2 = linearize && src24bpp ? toLinear1 + 256 : null;
            for (var srcY = srcYStart; srcY < srcYStop; srcY++)
            {
                if (cancelExists && cancel()) return;
                var srcYStrideStep = srcY * srcStrideStep;
                var srcScanL = srcScan0_ + srcYStrideStep;
                var srcAdr = srcScanL + srcXStartBytes;
                //var buff2dHori = buff2d_x0 + ((srcY - srcY0ry2) % ry2) * dstWidth;
                var buff2dHori = buff2d_x0Ry2 + ((srcY - srcY0ry2) % ry2); // こちらのほうが呼び出し回数が多いところで空間的局所性が高い
                if (src24bpp)
                {
                    if (linearize)
                    {
                        if (srcXStepIsOne)
                        {
                            for (var srcX = srcXStart; srcX < srcXStop; srcX++)
                            {
                                buff1d_srcX0rx[srcX] = toLinear[*srcAdr++] | toLinear1[*srcAdr++] | toLinear2[*srcAdr++];
                            }
                        }
                        else
                        {
                            for (var srcX = srcXStart; srcX < srcXStop; srcX++)
                            {
                                buff1d_srcX0rx[srcX] = toLinear[*srcAdr++] | toLinear1[*srcAdr++] | toLinear2[*srcAdr];
                                srcAdr += srcBytesPerXSample11;
                            }
                        }

                        for (var dstX = x0; dstX < x1; dstX++)
                        {
                            var srcX = (dstX * txx + tx0) / txd;
                            var cx = cx0 + dstX * rx2;
                            var buff1dLocal = buff1d_srcX0 + srcX;
                            var v = interDesignRoundOffset;
                            
                            for (long i = 0; i < rx2; i++) v += cx[i] * buff1dLocal[i];
                            buff2dHori[dstX * ry2] = ((v >> interShift) & interMask) - interSignOffset;
                        }
                    }
                    else
                    {
                        if (srcXStepIsOne)
                        {
                            for (var srcX = srcXStart; srcX < srcXStop; srcX++)
                            {
                                // ここでは *srcAdr++ は効果がある
                                buff1d_srcX0rx[srcX] = *srcAdr++ | (ulong)*srcAdr++ << 22 | (ulong)*srcAdr++ << 43; // G に 0.5 のオフセットをつけるのは誤り
                            }
                        }
                        else
                        {
                            for (var srcX = srcXStart; srcX < srcXStop; srcX++)
                            {
                                buff1d_srcX0rx[srcX] = *srcAdr++ | (ulong)*srcAdr++ << 22 | (ulong)*srcAdr << 43;
                                srcAdr += srcBytesPerXSample11;
                            }
                        }

                        for (var dstX = x0; dstX < x1; dstX++)
                        {
                            var srcX = (dstX * txx + tx0) / txd;
                            var cx = cx0 + dstX * rx2;
                            var buff1dLocal = buff1d_srcX0 + srcX;
                            var v = interDesignRoundOffset;

                            // ここで 6 割の時間を消費
                            // x64 環境では long を使ったほうが高速
                            // ulong は long よりわずかに劣る
                            for (long i = 0; i < rx2; i++) v += cx[i] * buff1dLocal[i];
                            //for (long i = 0; i < rx2long; i++) v += *cx++ * *buff1dLocal++; // こちらのほうが低速

#if DEBUG
                        //v = interDesignRoundOffset + (buff1dLocal[0] << interShift);
#endif

                            //buff2dHori[dstX] = ((v >> interShift) & interMask) - interSignOffset;
                            buff2dHori[dstX * ry2] = ((v >> interShift) & interMask) - interSignOffset;
                        }
                    }
                }
                else
                {
                    var srcAdr01 = srcScanL + srcXStartBytesMirror;
                    var srcScanL1 = srcScan0Mirror - srcYStrideStep;
                    if (linearize)
                    {
                        if (srcXStepIsOne)
                        {
                            for (var srcX = srcXStart; srcX < srcXStop; srcX++)
                            {
                                buff1d_srcX0rx[srcX] = toLinear[*srcAdr++] | toLinear1[*srcAdr01--];
                            }
                        }
                        else
                        {
                            for (var srcX = srcXStart; srcX < srcXStop; srcX++)
                            {
                                buff1d_srcX0rx[srcX] = toLinear[*srcAdr] | toLinear1[*srcAdr01];
                                srcAdr += srcBytesPerXSample;
                                srcAdr01 -= srcBytesPerXSample;
                            }
                        }

                        for (var dstX = x0; dstX < x1; dstX++)
                        {
                            var srcX = (dstX * txx + tx0) / txd;
                            var cx = cx0 + dstX * rx2;
                            var buff1dLocal = buff1d_srcX0 + srcX;
                            const ulong s0 = 1UL << (31 - 1); // シフト前符号オフセット
                            const int shift = 31 - grayLinearDigits;
                            const ulong s1 = 1UL << (grayLinearDigits - 1); // シフト後符号オフセット
                            const ulong r0 = 1UL << (shift - 1); // 丸めオフセット
                            const ulong v0 = s0 | r0;
                            const ulong m0 = ~(~0 << (grayLinearDigits + 1));
                            var v = v0 | v0 << 32;

                            for (long i = 0; i < rx2; i++) v += cx[i] * buff1dLocal[i];

                            buff2dHori[dstX * ry2] = ((v >> shift) & (m0 | m0 << 32)) - (s1 | s1 << 32);
                        }
                    }
                    else
                    {
                        var srcAdr10 = srcScanL1 + srcXStartBytes;
                        var srcAdr11 = srcScanL1 + srcXStartBytesMirror;
                        if (srcXStepIsOne)
                        {
                            for (var srcX = srcXStart; srcX < srcXStop; srcX++)
                            {
                                buff1d_srcX0rx[srcX] = *srcAdr++ | (ulong)*srcAdr01-- << 16 | (ulong)*srcAdr10++ << 32 | (ulong)*srcAdr11-- << 48;
                            }
                        }
                        else
                        {
                            for (var srcX = srcXStart; srcX < srcXStop; srcX++)
                            {
                                buff1d_srcX0rx[srcX] = *srcAdr | (ulong)*srcAdr01 << 16 | (ulong)*srcAdr10 << 32 | (ulong)*srcAdr11 << 48;
                                srcAdr += srcBytesPerXSample;
                                srcAdr01 -= srcBytesPerXSample;
                                srcAdr10 += srcBytesPerXSample;
                                srcAdr11 -= srcBytesPerXSample;
                            }
                        }

                        for (var dstX = x0; dstX < x1; dstX++)
                        {
                            var srcX = (dstX * txx + tx0) / txd;
                            var cx = cx0 + dstX * rx2;
                            var buff1dLocal = buff1d_srcX0 + srcX;
                            //var v = 0x4080408040804080UL;
                            var v = 0x4040404040404040UL;

                            for (long i = 0; i < rx2; i++) v += cx[i] * buff1dLocal[i];

                            buff2dHori[dstX * ry2] = ((v >> 7) & 0x01ff01ff01ff01ff) - 0x0080008000800080;
                        }
                    }
                }

                // (srcY * tyd + iy0) / tyy で（リングバッファならぬ）ロールバッファの読み込み状態を基準に読める所まで進めることができるが、
                // 縮小時は srcY == srcYStep1 になっても最後まで到達しないので場合分けが必要
                // 拡大時は更に先まで行けてしまうので場合分けが必要 → 拡大時はソースがまばらなので到達しないこともある
                //var dstYBound = yUpScaling ? Math.Min((srcY * tyd + iy0) / tyy, y1) : srcY < srcYStop1 ? (srcY * tyd + iy0) / tyy : y1;
                var dstYBound = srcY < srcYStop1 ? Math.Min((srcY * tyd + iy0) / tyy, y1) : y1;
                for (; dstY < dstYBound; dstY++)
                {
                    var dstYStride = dstY * dstStride;
                    var dstAdr = dstScan0X0 + dstYStride;
                    var dstAdr01 = dstScan0X0Mirror01 + dstYStride;
                    var dstAdr10 = dstScan0X0Mirror10 - dstYStride;
                    var dstAdr11 = dstScan0X0Mirror11 - dstYStride;
                    var cy = cy0 + dstY * ry2;
                    var srcYsMod = ((dstY * tyy + ty0) / tyd - srcY0ry) % ry2;
                    var buff2dLeft = buff2d_x0Ry2 + srcYsMod;
                    var jLoop = ry2 - srcYsMod;
                    if (src24bpp)
                    {
                        if (linearize)
                        {
                            for (long dstX = x0; dstX < x1; dstX++)
                            {
                                var buff2dVert = buff2dLeft + dstX * ry2;
                                var v = unt2 << 19;

                                for (long j = 0; j < jLoop; j++) v += cy[j] * buff2dVert[j];
                                buff2dVert -= ry2;
                                for (long j = jLoop; j < ry2; j++) v += cy[j] * buff2dVert[j];

                                *dstAdr++ = fromLinearIn13Out20_gamma2_2(v & 0x1FFFFF);
                                *dstAdr++ = fromLinearIn14Out21_gamma2_2((v >> 21) & 0x3FFFFF);
                                *dstAdr++ = fromLinearIn13Out20_gamma2_2(v >> (21 + 22));
                            }
                        }
                        else
                        {
                            for (long dstX = x0; dstX < x1; dstX++)
                            {
                                var buff2dVert = buff2dLeft + dstX * ry2;
                                var v = ((unt2 << 11) + (unt2 << 19));

                                // ここで 1 割弱の時間を消費
                                for (long j = 0; j < jLoop; j++) v += cy[j] * buff2dVert[j];
                                buff2dVert -= ry2;
                                for (long j = jLoop; j < ry2; j++) v += cy[j] * buff2dVert[j];

#if DEBUG
                            //v = ((unt2 << 11) + (unt2 << 19)) + (buff2dVert[buff2dSize] << 11);
#endif

                                ulong s;
                                s = v & (3UL << 19);
                                *dstAdr++ = (byte)(s == 0 ? 0 : s == (3UL << 19) ? 255 : ((v >> 12) - 128));
                                s = v & (3UL << 41);
                                *dstAdr++ = (byte)(s == 0 ? 0 : s == (3UL << 41) ? 255 : ((v >> 34) - 128));
                                s = v & (3UL << 62);
                                *dstAdr++ = (byte)(s == 0 ? 0 : s == (3UL << 62) ? 255 : ((v >> 55) - 128));
                            }
                        }
                    }
                    else if (dst24bpp)
                    {
                        if (linearize)
                        {
                            for (long dstX = x0; dstX < x1; dstX++)
                            {
                                var buff2dVert = buff2dLeft + dstX * ry2;
                                var v = 0x4000000040000000UL;

                                for (long j = 0; j < jLoop; j++) v += cy[j] * buff2dVert[j];
                                buff2dVert -= ry2;
                                for (long j = jLoop; j < ry2; j++) v += cy[j] * buff2dVert[j];
                                
                                byte vb;
                                vb = fromLinearIn18Out31_gamma2_2(v & 0xFFFFFFFFUL);
                                *dstAdr++ = vb; *dstAdr++ = vb; *dstAdr++ = vb;
                                vb = fromLinearIn18Out31_gamma2_2(v >> 32);
                                *dstAdr01-- = vb; *dstAdr01-- = vb; *dstAdr01-- = vb;
                            }
                        }
                        else
                        {
                            for (long dstX = x0; dstX < x1; dstX++)
                            {
                                var buff2dVert = buff2dLeft + dstX * ry2;
                                //var v = 0x4080408040804080UL;
                                var v = 0x4040404040404040UL;

                                for (long j = 0; j < jLoop; j++) v += cy[j] * buff2dVert[j];
                                buff2dVert -= ry2;
                                for (long j = jLoop; j < ry2; j++) v += cy[j] * buff2dVert[j];

                                ulong s; byte vb;
                                s = v & 3UL << 14;
                                vb = (byte)(s == 0 ? 0 : s == (3UL << 14) ? 255 : ((v >> 7) - 128));
                                *dstAdr++ = vb; *dstAdr++ = vb; *dstAdr++ = vb;
                                s = v & 3UL << 30;
                                vb = (byte)(s == 0 ? 0 : s == (3UL << 30) ? 255 : ((v >> 23) - 128));
                                *dstAdr01-- = vb; *dstAdr01-- = vb; *dstAdr01-- = vb;
                                s = v & 3UL << 46;
                                vb = (byte)(s == 0 ? 0 : s == (3UL << 46) ? 255 : ((v >> 39) - 128));
                                *dstAdr10++ = vb; *dstAdr10++ = vb; *dstAdr10++ = vb;
                                s = v & 3UL << 62;
                                vb = (byte)(s == 0 ? 0 : s == (3UL << 62) ? 255 : ((v >> 55) - 128));
                                *dstAdr11-- = vb; *dstAdr11-- = vb; *dstAdr11-- = vb;
                            }
                        }
                    }
                    else
                    {
                        if (linearize)
                        {
                            for (long dstX = x0; dstX < x1; dstX++)
                            {
                                var buff2dVert = buff2dLeft + dstX * ry2;
                                var v = 0x4000000040000000UL;

                                for (long j = 0; j < jLoop; j++) v += cy[j] * buff2dVert[j];
                                buff2dVert -= ry2;
                                for (long j = jLoop; j < ry2; j++) v += cy[j] * buff2dVert[j];
                                
                                *dstAdr++ = fromLinearIn18Out31_gamma2_2(v & 0xFFFFFFFFUL);
                                *dstAdr01-- = fromLinearIn18Out31_gamma2_2(v >> 32);
                            }
                        }
                        else
                        {
                            for (long dstX = x0; dstX < x1; dstX++)
                            {
                                var buff2dVert = buff2dLeft + dstX * ry2;
                                //var v = 0x4080408040804080UL;
                                var v = 0x4040404040404040UL;

                                for (long j = 0; j < jLoop; j++) v += cy[j] * buff2dVert[j];
                                buff2dVert -= ry2;
                                for (long j = jLoop; j < ry2; j++) v += cy[j] * buff2dVert[j];

                                ulong s;
                                s = v & 3UL << 14;
                                *dstAdr++ = (byte)(s == 0 ? 0 : s == (3UL << 14) ? 255 : ((v >> 7) - 128));
                                s = v & 3UL << 30;
                                *dstAdr01-- = (byte)(s == 0 ? 0 : s == (3UL << 30) ? 255 : ((v >> 23) - 128));
                                s = v & 3UL << 46;
                                *dstAdr10++ = (byte)(s == 0 ? 0 : s == (3UL << 46) ? 255 : ((v >> 39) - 128));
                                s = v & 3UL << 62;
                                *dstAdr11-- = (byte)(s == 0 ? 0 : s == (3UL << 62) ? 255 : ((v >> 55) - 128));
                            }
                        }
                    }
                }
            }
        }

        private static readonly IntPtr toLinear131413Array2_2 = getToLinear131413Array2_2();
        private static IntPtr getToLinear131413Array2_2()
        {
            var result = Marshal.AllocCoTaskMem(3 * 256 * sizeof(ulong));
            unsafe
            {
                var adr = (ulong*)result;
                for (var i = 0; i < 256; i++)
                {
                    var v = (ulong)(toLinear13_gamma2_2(i) + 0.5);
                    adr[i] = v;
                    adr[i + 256] = (ulong)(toLinear14_gamma2_2(i) + 0.5) << 21;
                    adr[i + 512] = v << (21 + 22);
                }
            }
            return result;
        }

        private static readonly IntPtr toLinear18Array2_2 = getToLinear18Array2_2();
        private static IntPtr getToLinear18Array2_2()
        {
            var result = Marshal.AllocCoTaskMem(2 * 256 * sizeof(ulong));
            unsafe
            {
                var adr = (ulong*)result;
                for (var i = 0; i < 256; i++)
                {
                    var v = (ulong)(toLinear18_gamma2_2(i) + 0.5);
                    adr[i] = v;
                    adr[i + 256] = v << 32;
                }
            }
            return result;
        }

        private static byte fromLinearIn18Out31_gamma2_2(ulong linear)
        {
            if (linear <= 1541105410)
                if (linear <= 1174577076)
                    if (linear <= 1095304500)
                        if (linear <= 1078268425)
                            if (linear <= 1074654983)
                                if (linear <= 1073909565)
                                    if (linear <= 1073765195)
                                        if (linear <= 1073741824)
                                            return 0;
                                        else
                                            return 1;
                                    else
                                        if (linear <= 1073820078)
                                        return 2;
                                    else
                                        return 3;
                                else
                                    if (linear <= 1074201417)
                                    if (linear <= 1074036044)
                                        return 4;
                                    else
                                        return 5;
                                else
                                        if (linear <= 1074407272)
                                    return 6;
                                else
                                    return 7;
                            else
                                if (linear <= 1076086977)
                                if (linear <= 1075280710)
                                    if (linear <= 1074945766)
                                        return 8;
                                    else
                                        return 9;
                                else
                                    if (linear <= 1075660810)
                                    return 10;
                                else
                                    return 11;
                            else
                                    if (linear <= 1077080839)
                                if (linear <= 1076560057)
                                    return 12;
                                else
                                    return 13;
                            else
                                        if (linear <= 1077650062)
                                return 14;
                            else
                                return 15;
                        else
                            if (linear <= 1085057299)
                            if (linear <= 1081245979)
                                if (linear <= 1079655171)
                                    if (linear <= 1078936585)
                                        return 16;
                                    else
                                        return 17;
                                else
                                    if (linear <= 1080424779)
                                    return 18;
                                else
                                    return 19;
                            else
                                if (linear <= 1083045316)
                                if (linear <= 1082119316)
                                    return 20;
                                else
                                    return 21;
                            else
                                    if (linear <= 1084024482)
                                return 22;
                            else
                                return 23;
                        else
                                if (linear <= 1089734229)
                            if (linear <= 1087285748)
                                if (linear <= 1086144237)
                                    return 24;
                                else
                                    return 25;
                            else
                                if (linear <= 1088482270)
                                return 26;
                            else
                                return 27;
                        else
                                    if (linear <= 1092406093)
                            if (linear <= 1091042037)
                                return 28;
                            else
                                return 29;
                        else
                                        if (linear <= 1093826788)
                            return 30;
                        else
                            return 31;
                    else
                        if (linear <= 1126978731)
                        if (linear <= 1109221273)
                            if (linear <= 1101792764)
                                if (linear <= 1098432442)
                                    if (linear <= 1096839598)
                                        return 32;
                                    else
                                        return 33;
                                else
                                    if (linear <= 1100083383)
                                    return 34;
                                else
                                    return 35;
                            else
                                if (linear <= 1105388177)
                                if (linear <= 1103560919)
                                    return 36;
                                else
                                    return 37;
                            else
                                    if (linear <= 1107274857)
                                return 38;
                            else
                                return 39;
                        else
                            if (linear <= 1117610352)
                            if (linear <= 1113294537)
                                if (linear <= 1111227733)
                                    return 40;
                                else
                                    return 41;
                            else
                                if (linear <= 1115421980)
                                return 42;
                            else
                                return 43;
                        else
                                if (linear <= 1122171013)
                            if (linear <= 1119859936)
                                return 44;
                            else
                                return 45;
                        else
                                    if (linear <= 1124543854)
                            return 46;
                        else
                            return 47;
                    else
                            if (linear <= 1148721852)
                        if (linear <= 1137343811)
                            if (linear <= 1132035643)
                                if (linear <= 1129475907)
                                    return 48;
                                else
                                    return 49;
                            else
                                if (linear <= 1134658193)
                                return 50;
                            else
                                return 51;
                        else
                            if (linear <= 1142905235)
                            if (linear <= 1140092744)
                                return 52;
                            else
                                return 53;
                        else
                                if (linear <= 1145781526)
                            return 54;
                        else
                            return 55;
                    else
                                if (linear <= 1161128134)
                        if (linear <= 1154795543)
                            if (linear <= 1151726448)
                                return 56;
                            else
                                return 57;
                        else
                            if (linear <= 1157929364)
                            return 58;
                        else
                            return 59;
                    else
                                    if (linear <= 1167721402)
                        if (linear <= 1164392074)
                            return 60;
                        else
                            return 61;
                    else
                                        if (linear <= 1171116332)
                        return 62;
                    else
                        return 63;
                else
                    if (linear <= 1321218420)
                    if (linear <= 1239063043)
                        if (linear <= 1204656924)
                            if (linear <= 1189082342)
                                if (linear <= 1181696842)
                                    if (linear <= 1178103844)
                                        return 64;
                                    else
                                        return 65;
                                else
                                    if (linear <= 1185356274)
                                    return 66;
                                else
                                    return 67;
                            else
                                if (linear <= 1196735181)
                                if (linear <= 1192875246)
                                    return 68;
                                else
                                    return 69;
                            else
                                    if (linear <= 1200662343)
                                return 70;
                            else
                                return 71;
                        else
                            if (linear <= 1221313209)
                            if (linear <= 1212849101)
                                if (linear <= 1208719114)
                                    return 72;
                                else
                                    return 73;
                            else
                                if (linear <= 1217047071)
                                return 74;
                            else
                                return 75;
                        else
                                if (linear <= 1230050712)
                            if (linear <= 1225647696)
                                return 76;
                            else
                                return 77;
                        else
                                    if (linear <= 1234522435)
                            return 78;
                        else
                            return 79;
                    else
                        if (linear <= 1277888320)
                        if (linear <= 1257917778)
                            if (linear <= 1248351606)
                                if (linear <= 1243672709)
                                    return 80;
                                else
                                    return 81;
                            else
                                if (linear <= 1253099906)
                                return 82;
                            else
                                return 83;
                        else
                            if (linear <= 1267762908)
                            if (linear <= 1262805390)
                                return 84;
                            else
                                return 85;
                        else
                                if (linear <= 1272790497)
                            return 86;
                        else
                            return 87;
                    else
                            if (linear <= 1298985161)
                        if (linear <= 1288295312)
                            if (linear <= 1283056538)
                                return 88;
                            else
                                return 89;
                        else
                            if (linear <= 1293604801)
                            return 90;
                        else
                            return 91;
                    else
                                if (linear <= 1309959120)
                        if (linear <= 1304436550)
                            return 92;
                        else
                            return 93;
                    else
                                    if (linear <= 1315553026)
                        return 94;
                    else
                        return 95;
                else
                        if (linear <= 1421706304)
                    if (linear <= 1369132942)
                        if (linear <= 1344597865)
                            if (linear <= 1332764272)
                                if (linear <= 1326955452)
                                    return 96;
                                else
                                    return 97;
                            else
                                if (linear <= 1338645027)
                                return 98;
                            else
                                return 99;
                        else
                            if (linear <= 1356720371)
                            if (linear <= 1350622931)
                                return 100;
                            else
                                return 101;
                        else
                                if (linear <= 1362890327)
                            return 102;
                        else
                            return 103;
                    else
                        if (linear <= 1394832799)
                        if (linear <= 1381836712)
                            if (linear <= 1375448357)
                                return 104;
                            else
                                return 105;
                        else
                            if (linear <= 1388298147)
                            return 106;
                        else
                            return 107;
                    else
                            if (linear <= 1408122301)
                        if (linear <= 1401440805)
                            return 108;
                        else
                            return 109;
                    else
                                if (linear <= 1414877423)
                        return 110;
                    else
                        return 111;
                else
                            if (linear <= 1479008449)
                    if (linear <= 1449762066)
                        if (linear <= 1435585874)
                            if (linear <= 1428609077)
                                return 112;
                            else
                                return 113;
                        else
                            if (linear <= 1442636827)
                            return 114;
                        else
                            return 115;
                    else
                        if (linear <= 1464235916)
                        if (linear <= 1456961719)
                            return 116;
                        else
                            return 117;
                    else
                            if (linear <= 1471584784)
                        return 118;
                    else
                        return 119;
                else
                                if (linear <= 1509453590)
                    if (linear <= 1494080675)
                        if (linear <= 1486507038)
                            return 120;
                        else
                            return 121;
                    else
                        if (linear <= 1501729485)
                        return 122;
                    else
                        return 123;
                else
                                    if (linear <= 1525128178)
                    if (linear <= 1517253114)
                        return 124;
                    else
                        return 125;
                else
                                        if (linear <= 1533078903)
                    return 126;
                else
                    return 127;
            else
                if (linear <= 2217430622)
                if (linear <= 1838648814)
                    if (linear <= 1679931024)
                        if (linear <= 1608059764)
                            if (linear <= 1573971625)
                                if (linear <= 1557386243)
                                    if (linear <= 1549207817)
                                        return 128;
                                    else
                                        return 129;
                                else
                                    if (linear <= 1565640807)
                                    return 130;
                                else
                                    return 131;
                            else
                                if (linear <= 1590862491)
                                if (linear <= 1582378815)
                                    return 132;
                                else
                                    return 133;
                            else
                                    if (linear <= 1599422770)
                                return 134;
                            else
                                return 135;
                        else
                            if (linear <= 1643377173)
                            if (linear <= 1625564358)
                                if (linear <= 1616773589)
                                    return 136;
                                else
                                    return 137;
                            else
                                if (linear <= 1634432181)
                                return 138;
                            else
                                return 139;
                        else
                                if (linear <= 1661499101)
                            if (linear <= 1652399442)
                                return 140;
                            else
                                return 141;
                        else
                                    if (linear <= 1670676258)
                            return 142;
                        else
                            return 143;
                    else
                        if (linear <= 1756775947)
                        if (linear <= 1717728330)
                            if (linear <= 1698673813)
                                if (linear <= 1689263506)
                                    return 144;
                                else
                                    return 145;
                            else
                                if (linear <= 1708162052)
                                return 146;
                            else
                                return 147;
                        else
                            if (linear <= 1737095427)
                            if (linear <= 1727372753)
                                return 148;
                            else
                                return 149;
                        else
                                if (linear <= 1746896457)
                            return 150;
                        else
                            return 151;
                    else
                            if (linear <= 1797080585)
                        if (linear <= 1776770725)
                            if (linear <= 1766734002)
                                return 152;
                            else
                                return 153;
                        else
                            if (linear <= 1786886218)
                            return 154;
                        else
                            return 155;
                    else
                                if (linear <= 1817706346)
                        if (linear <= 1807353927)
                            return 156;
                        else
                            return 157;
                    else
                                    if (linear <= 1828137941)
                        return 158;
                    else
                        return 159;
                else
                    if (linear <= 2017684454)
                    if (linear <= 1925601656)
                        if (linear <= 1881487068)
                            if (linear <= 1859908791)
                                if (linear <= 1849239064)
                                    return 160;
                                else
                                    return 161;
                            else
                                if (linear <= 1870658093)
                                return 162;
                            else
                                return 163;
                        else
                            if (linear <= 1903384431)
                            if (linear <= 1892395815)
                                return 164;
                            else
                                return 165;
                        else
                                if (linear <= 1914453012)
                            return 166;
                        else
                            return 167;
                    else
                        if (linear <= 1970998762)
                        if (linear <= 1948139512)
                            if (linear <= 1936830457)
                                return 168;
                            else
                                return 169;
                        else
                            if (linear <= 1959528915)
                            return 170;
                        else
                            return 171;
                    else
                            if (linear <= 1994180160)
                        if (linear <= 1982549145)
                            return 172;
                        else
                            return 173;
                    else
                                if (linear <= 2005891898)
                        return 174;
                    else
                        return 175;
                else
                        if (linear <= 2114945316)
                    if (linear <= 2065664690)
                        if (linear <= 2041512386)
                            if (linear <= 2029557919)
                                return 176;
                            else
                                return 177;
                        else
                            if (linear <= 2053547946)
                            return 178;
                        else
                            return 179;
                    else
                        if (linear <= 2090142092)
                        if (linear <= 2077862708)
                            return 180;
                        else
                            return 181;
                    else
                            if (linear <= 2102502932)
                        return 182;
                    else
                        return 183;
                else
                            if (linear <= 2165532078)
                    if (linear <= 2140075075)
                        if (linear <= 2127469334)
                            return 184;
                        else
                            return 185;
                    else
                        if (linear <= 2152762627)
                        return 186;
                    else
                        return 187;
                else
                                if (linear <= 2191317028)
                    if (linear <= 2178383516)
                        return 188;
                    else
                        return 189;
                else
                                    if (linear <= 2204332702)
                    return 190;
                else
                    return 191;
            else
                    if (linear <= 2680490614)
                if (linear <= 2438252256)
                    if (linear <= 2325185163)
                        if (linear <= 2270646498)
                            if (linear <= 2243873550)
                                if (linear <= 2230610877)
                                    return 192;
                                else
                                    return 193;
                            else
                                if (linear <= 2257218729)
                                return 194;
                            else
                                return 195;
                        else
                            if (linear <= 2297750144)
                            if (linear <= 2284156942)
                                return 196;
                            else
                                return 197;
                        else
                                if (linear <= 2311426190)
                            return 198;
                        else
                            return 199;
                    else
                        if (linear <= 2381051988)
                        if (linear <= 2352952224)
                            if (linear <= 2339027147)
                                return 200;
                            else
                                return 201;
                        else
                            if (linear <= 2366960477)
                            return 202;
                        else
                            return 203;
                    else
                            if (linear <= 2409485115)
                        if (linear <= 2395226840)
                            return 204;
                        else
                            return 205;
                    else
                                if (linear <= 2423826893)
                        return 206;
                    else
                        return 207;
                else
                    if (linear <= 2556673849)
                    if (linear <= 2496791169)
                        if (linear <= 2467354060)
                            if (linear <= 2452761285)
                                return 208;
                            else
                                return 209;
                        else
                            if (linear <= 2482030662)
                            return 210;
                        else
                            return 211;
                    else
                        if (linear <= 2526564221)
                        if (linear <= 2511635663)
                            return 212;
                        else
                            return 213;
                    else
                            if (linear <= 2541576923)
                        return 214;
                    else
                        return 215;
                else
                        if (linear <= 2617905340)
                    if (linear <= 2587120681)
                        if (linear <= 2571855075)
                            return 216;
                        else
                            return 217;
                    else
                        if (linear <= 2602470743)
                        return 218;
                    else
                        return 219;
                else
                            if (linear <= 2649028446)
                    if (linear <= 2633424549)
                        return 220;
                    else
                        return 221;
                else
                                if (linear <= 2664717109)
                    return 222;
                else
                    return 223;
            else
                        if (linear <= 2944466474)
                if (linear <= 2809742033)
                    if (linear <= 2744434568)
                        if (linear <= 2712292453)
                            if (linear <= 2696349036)
                                return 224;
                            else
                                return 225;
                        else
                            if (linear <= 2728320938)
                            return 226;
                        else
                            return 227;
                    else
                        if (linear <= 2776917563)
                        if (linear <= 2760633418)
                            return 228;
                        else
                            return 229;
                    else
                            if (linear <= 2793287076)
                        return 230;
                    else
                        return 231;
                else
                    if (linear <= 2876417770)
                    if (linear <= 2842908572)
                        if (linear <= 2826282507)
                            return 232;
                        else
                            return 233;
                    else
                        if (linear <= 2859620302)
                        return 234;
                    else
                        return 235;
                else
                        if (linear <= 2910270210)
                    if (linear <= 2893301048)
                        return 236;
                    else
                        return 237;
                else
                            if (linear <= 2927325328)
                    return 238;
                else
                    return 239;
            else
                            if (linear <= 3084701256)
                if (linear <= 3013892779)
                    if (linear <= 2979007139)
                        if (linear <= 2961693721)
                            return 240;
                        else
                            return 241;
                    else
                        if (linear <= 2996406802)
                        return 242;
                    else
                        return 243;
                else
                    if (linear <= 3049123962)
                    if (linear <= 3031465142)
                        return 244;
                    else
                        return 245;
                else
                        if (linear <= 3066869310)
                    return 246;
                else
                    return 247;
            else
                                if (linear <= 3156896416)
                if (linear <= 3120625221)
                    if (linear <= 3102619869)
                        return 248;
                    else
                        return 249;
                else
                    if (linear <= 3138717380)
                    return 250;
                else
                    return 251;
            else
                                    if (linear <= 3193515397)
                if (linear <= 3175162399)
                    return 252;
                else
                    return 253;
            else
                                        if (linear <= 3211955479)
                return 254;
            else
                return 255;
        }
        private static byte fromLinearIn13Out20_gamma2_2(ulong linear)
        {
            if (linear <= 752867)
                if (linear <= 574007)
                    if (linear <= 535323)
                        if (linear <= 527009)
                            if (linear <= 525246)
                                if (linear <= 524731)
                                    if (linear <= 524469)
                                        if (linear <= 524288)
                                            return 0;
                                        else
                                            return 1;
                                    else
                                        if (linear <= 524602)
                                        return 2;
                                    else
                                        return 3;
                                else
                                    if (linear <= 524989)
                                    if (linear <= 524860)
                                        return 4;
                                    else
                                        return 5;
                                else
                                        if (linear <= 525118)
                                    return 6;
                                else
                                    return 7;
                            else
                                if (linear <= 525945)
                                if (linear <= 525551)
                                    if (linear <= 525388)
                                        return 8;
                                    else
                                        return 9;
                                else
                                    if (linear <= 525737)
                                    return 10;
                                else
                                    return 11;
                            else
                                    if (linear <= 526430)
                                if (linear <= 526176)
                                    return 12;
                                else
                                    return 13;
                            else
                                        if (linear <= 526708)
                                return 14;
                            else
                                return 15;
                        else
                            if (linear <= 530322)
                            if (linear <= 528462)
                                if (linear <= 527686)
                                    if (linear <= 527335)
                                        return 16;
                                    else
                                        return 17;
                                else
                                    if (linear <= 528062)
                                    return 18;
                                else
                                    return 19;
                            else
                                if (linear <= 529340)
                                if (linear <= 528889)
                                    return 20;
                                else
                                    return 21;
                            else
                                    if (linear <= 529818)
                                return 22;
                            else
                                return 23;
                        else
                                if (linear <= 532605)
                            if (linear <= 531410)
                                if (linear <= 530853)
                                    return 24;
                                else
                                    return 25;
                            else
                                if (linear <= 531994)
                                return 26;
                            else
                                return 27;
                        else
                                    if (linear <= 533908)
                            if (linear <= 533243)
                                return 28;
                            else
                                return 29;
                        else
                                        if (linear <= 534602)
                            return 30;
                        else
                            return 31;
                    else
                        if (linear <= 550779)
                        if (linear <= 542114)
                            if (linear <= 538489)
                                if (linear <= 536849)
                                    if (linear <= 536072)
                                        return 32;
                                    else
                                        return 33;
                                else
                                    if (linear <= 537655)
                                    return 34;
                                else
                                    return 35;
                            else
                                if (linear <= 540243)
                                if (linear <= 539352)
                                    return 36;
                                else
                                    return 37;
                            else
                                    if (linear <= 541164)
                                return 38;
                            else
                                return 39;
                        else
                            if (linear <= 546208)
                            if (linear <= 544102)
                                if (linear <= 543093)
                                    return 40;
                                else
                                    return 41;
                            else
                                if (linear <= 545140)
                                return 42;
                            else
                                return 43;
                        else
                                if (linear <= 548433)
                            if (linear <= 547305)
                                return 44;
                            else
                                return 45;
                        else
                                    if (linear <= 549591)
                            return 46;
                        else
                            return 47;
                    else
                            if (linear <= 561390)
                        if (linear <= 555837)
                            if (linear <= 553247)
                                if (linear <= 551998)
                                    return 48;
                                else
                                    return 49;
                            else
                                if (linear <= 554527)
                                return 50;
                            else
                                return 51;
                        else
                            if (linear <= 558551)
                            if (linear <= 557179)
                                return 52;
                            else
                                return 53;
                        else
                                if (linear <= 559955)
                            return 54;
                        else
                            return 55;
                    else
                                if (linear <= 567444)
                        if (linear <= 564353)
                            if (linear <= 562856)
                                return 56;
                            else
                                return 57;
                        else
                            if (linear <= 565883)
                            return 58;
                        else
                            return 59;
                    else
                                    if (linear <= 570661)
                        if (linear <= 569036)
                            return 60;
                        else
                            return 61;
                    else
                                        if (linear <= 572318)
                        return 62;
                    else
                        return 63;
                else
                    if (linear <= 645565)
                    if (linear <= 605475)
                        if (linear <= 588685)
                            if (linear <= 581085)
                                if (linear <= 577481)
                                    if (linear <= 575728)
                                        return 64;
                                    else
                                        return 65;
                                else
                                    if (linear <= 579267)
                                    return 66;
                                else
                                    return 67;
                            else
                                if (linear <= 584819)
                                if (linear <= 582936)
                                    return 68;
                                else
                                    return 69;
                            else
                                    if (linear <= 586736)
                                return 70;
                            else
                                return 71;
                        else
                            if (linear <= 596813)
                            if (linear <= 592683)
                                if (linear <= 590667)
                                    return 72;
                                else
                                    return 73;
                            else
                                if (linear <= 594731)
                                return 74;
                            else
                                return 75;
                        else
                                if (linear <= 601077)
                            if (linear <= 598928)
                                return 76;
                            else
                                return 77;
                        else
                                    if (linear <= 603259)
                            return 78;
                        else
                            return 79;
                    else
                        if (linear <= 624421)
                        if (linear <= 614676)
                            if (linear <= 610007)
                                if (linear <= 607724)
                                    return 80;
                                else
                                    return 81;
                            else
                                if (linear <= 612325)
                                return 82;
                            else
                                return 83;
                        else
                            if (linear <= 619480)
                            if (linear <= 617061)
                                return 84;
                            else
                                return 85;
                        else
                                if (linear <= 621933)
                            return 86;
                        else
                            return 87;
                    else
                            if (linear <= 634716)
                        if (linear <= 629499)
                            if (linear <= 626943)
                                return 88;
                            else
                                return 89;
                        else
                            if (linear <= 632090)
                            return 90;
                        else
                            return 91;
                    else
                                if (linear <= 640071)
                        if (linear <= 637376)
                            return 92;
                        else
                            return 93;
                    else
                                    if (linear <= 642801)
                        return 94;
                    else
                        return 95;
                else
                        if (linear <= 694602)
                    if (linear <= 668947)
                        if (linear <= 656974)
                            if (linear <= 651200)
                                if (linear <= 648365)
                                    return 96;
                                else
                                    return 97;
                            else
                                if (linear <= 654069)
                                return 98;
                            else
                                return 99;
                        else
                            if (linear <= 662890)
                            if (linear <= 659914)
                                return 100;
                            else
                                return 101;
                        else
                                if (linear <= 665901)
                            return 102;
                        else
                            return 103;
                    else
                        if (linear <= 681488)
                        if (linear <= 675146)
                            if (linear <= 672029)
                                return 104;
                            else
                                return 105;
                        else
                            if (linear <= 678299)
                            return 106;
                        else
                            return 107;
                    else
                            if (linear <= 687973)
                        if (linear <= 684713)
                            return 108;
                        else
                            return 109;
                    else
                                if (linear <= 691269)
                        return 110;
                    else
                        return 111;
                else
                            if (linear <= 722564)
                    if (linear <= 708293)
                        if (linear <= 701375)
                            if (linear <= 697970)
                                return 112;
                            else
                                return 113;
                        else
                            if (linear <= 704816)
                            return 114;
                        else
                            return 115;
                    else
                        if (linear <= 715356)
                        if (linear <= 711806)
                            return 116;
                        else
                            return 117;
                    else
                            if (linear <= 718942)
                        return 118;
                    else
                        return 119;
                else
                                if (linear <= 737421)
                    if (linear <= 729919)
                        if (linear <= 726224)
                            return 120;
                        else
                            return 121;
                    else
                        if (linear <= 733652)
                        return 122;
                    else
                        return 123;
                else
                                    if (linear <= 745070)
                    if (linear <= 741227)
                        return 124;
                    else
                        return 125;
                else
                                        if (linear <= 748950)
                    return 126;
                else
                    return 127;
            else
                if (linear <= 1082903)
                if (linear <= 898064)
                    if (linear <= 820612)
                        if (linear <= 785540)
                            if (linear <= 768905)
                                if (linear <= 760812)
                                    if (linear <= 756821)
                                        return 128;
                                    else
                                        return 129;
                                else
                                    if (linear <= 764840)
                                    return 130;
                                else
                                    return 131;
                            else
                                if (linear <= 777148)
                                if (linear <= 773008)
                                    return 132;
                                else
                                    return 133;
                            else
                                    if (linear <= 781325)
                                return 134;
                            else
                                return 135;
                        else
                            if (linear <= 802774)
                            if (linear <= 794082)
                                if (linear <= 789792)
                                    return 136;
                                else
                                    return 137;
                            else
                                if (linear <= 798409)
                                return 138;
                            else
                                return 139;
                        else
                                if (linear <= 811617)
                            if (linear <= 807177)
                                return 140;
                            else
                                return 141;
                        else
                                    if (linear <= 816095)
                            return 142;
                        else
                            return 143;
                    else
                        if (linear <= 858111)
                        if (linear <= 839056)
                            if (linear <= 829758)
                                if (linear <= 825166)
                                    return 144;
                                else
                                    return 145;
                            else
                                if (linear <= 834388)
                                return 146;
                            else
                                return 147;
                        else
                            if (linear <= 848507)
                            if (linear <= 843762)
                                return 148;
                            else
                                return 149;
                        else
                                if (linear <= 853290)
                            return 150;
                        else
                            return 151;
                    else
                            if (linear <= 877779)
                        if (linear <= 867868)
                            if (linear <= 862970)
                                return 152;
                            else
                                return 153;
                        else
                            if (linear <= 872804)
                            return 154;
                        else
                            return 155;
                    else
                                if (linear <= 887844)
                        if (linear <= 882792)
                            return 156;
                        else
                            return 157;
                    else
                                    if (linear <= 892934)
                        return 158;
                    else
                        return 159;
                else
                    if (linear <= 985430)
                    if (linear <= 940495)
                        if (linear <= 918968)
                            if (linear <= 908438)
                                if (linear <= 903231)
                                    return 160;
                                else
                                    return 161;
                            else
                                if (linear <= 913684)
                                return 162;
                            else
                                return 163;
                        else
                            if (linear <= 929654)
                            if (linear <= 924291)
                                return 164;
                            else
                                return 165;
                        else
                                if (linear <= 935055)
                            return 166;
                        else
                            return 167;
                    else
                        if (linear <= 962648)
                        if (linear <= 951493)
                            if (linear <= 945975)
                                return 168;
                            else
                                return 169;
                        else
                            if (linear <= 957051)
                            return 170;
                        else
                            return 171;
                    else
                            if (linear <= 973960)
                        if (linear <= 968285)
                            return 172;
                        else
                            return 173;
                    else
                                if (linear <= 979676)
                        return 174;
                    else
                        return 175;
                else
                        if (linear <= 1032892)
                    if (linear <= 1008844)
                        if (linear <= 997058)
                            if (linear <= 991224)
                                return 176;
                            else
                                return 177;
                        else
                            if (linear <= 1002931)
                            return 178;
                        else
                            return 179;
                    else
                        if (linear <= 1020788)
                        if (linear <= 1014796)
                            return 180;
                        else
                            return 181;
                    else
                            if (linear <= 1026820)
                        return 182;
                    else
                        return 183;
                else
                            if (linear <= 1057578)
                    if (linear <= 1045155)
                        if (linear <= 1039004)
                            return 184;
                        else
                            return 185;
                    else
                        if (linear <= 1051346)
                        return 186;
                    else
                        return 187;
                else
                                if (linear <= 1070160)
                    if (linear <= 1063849)
                        return 188;
                    else
                        return 189;
                else
                                    if (linear <= 1076512)
                    return 190;
                else
                    return 191;
            else
                    if (linear <= 1308870)
                if (linear <= 1190661)
                    if (linear <= 1135486)
                        if (linear <= 1108872)
                            if (linear <= 1095807)
                                if (linear <= 1089335)
                                    return 192;
                                else
                                    return 193;
                            else
                                if (linear <= 1102319)
                                return 194;
                            else
                                return 195;
                        else
                            if (linear <= 1122098)
                            if (linear <= 1115465)
                                return 196;
                            else
                                return 197;
                        else
                                if (linear <= 1128772)
                            return 198;
                        else
                            return 199;
                    else
                        if (linear <= 1162748)
                        if (linear <= 1149036)
                            if (linear <= 1142241)
                                return 200;
                            else
                                return 201;
                        else
                            if (linear <= 1155872)
                            return 202;
                        else
                            return 203;
                    else
                            if (linear <= 1176623)
                        if (linear <= 1169665)
                            return 204;
                        else
                            return 205;
                    else
                                if (linear <= 1183622)
                        return 206;
                    else
                        return 207;
                else
                    if (linear <= 1248449)
                    if (linear <= 1219227)
                        if (linear <= 1204862)
                            if (linear <= 1197741)
                                return 208;
                            else
                                return 209;
                        else
                            if (linear <= 1212024)
                            return 210;
                        else
                            return 211;
                    else
                        if (linear <= 1233756)
                        if (linear <= 1226471)
                            return 212;
                        else
                            return 213;
                    else
                            if (linear <= 1241082)
                        return 214;
                    else
                        return 215;
                else
                        if (linear <= 1278329)
                    if (linear <= 1263306)
                        if (linear <= 1255857)
                            return 216;
                        else
                            return 217;
                    else
                        if (linear <= 1270797)
                        return 218;
                    else
                        return 219;
                else
                            if (linear <= 1293517)
                    if (linear <= 1285902)
                        return 220;
                    else
                        return 221;
                else
                                if (linear <= 1301172)
                    return 222;
                else
                    return 223;
            else
                        if (linear <= 1437686)
                if (linear <= 1371942)
                    if (linear <= 1340073)
                        if (linear <= 1324388)
                            if (linear <= 1316608)
                                return 224;
                            else
                                return 225;
                        else
                            if (linear <= 1332210)
                            return 226;
                        else
                            return 227;
                    else
                        if (linear <= 1355924)
                        if (linear <= 1347978)
                            return 228;
                        else
                            return 229;
                    else
                            if (linear <= 1363913)
                        return 230;
                    else
                        return 231;
                else
                    if (linear <= 1404479)
                    if (linear <= 1388127)
                        if (linear <= 1380014)
                            return 232;
                        else
                            return 233;
                    else
                        if (linear <= 1396282)
                        return 234;
                    else
                        return 235;
                else
                        if (linear <= 1420999)
                    if (linear <= 1412718)
                        return 236;
                    else
                        return 237;
                else
                            if (linear <= 1429321)
                    return 238;
                else
                    return 239;
            else
                            if (linear <= 1506118)
                if (linear <= 1471565)
                    if (linear <= 1454541)
                        if (linear <= 1446092)
                            return 240;
                        else
                            return 241;
                    else
                        if (linear <= 1463032)
                        return 242;
                    else
                        return 243;
                else
                    if (linear <= 1488757)
                    if (linear <= 1480140)
                        return 244;
                    else
                        return 245;
                else
                        if (linear <= 1497417)
                    return 246;
                else
                    return 247;
            else
                                if (linear <= 1541348)
                if (linear <= 1523649)
                    if (linear <= 1514862)
                        return 248;
                    else
                        return 249;
                else
                    if (linear <= 1532477)
                    return 250;
                else
                    return 251;
            else
                                    if (linear <= 1559218)
                if (linear <= 1550262)
                    return 252;
                else
                    return 253;
            else
                                        if (linear <= 1568216)
                return 254;
            else
                return 255;
        }
        private static byte fromLinearIn14Out21_gamma2_2(ulong linear)
        {
            if (linear <= 1505185)
                if (linear <= 1147316)
                    if (linear <= 1069917)
                        if (linear <= 1053283)
                            if (linear <= 1049755)
                                if (linear <= 1049019)
                                    if (linear <= 1048757)
                                        if (linear <= 1048576)
                                            return 0;
                                        else
                                            return 1;
                                    else
                                        if (linear <= 1048890)
                                        return 2;
                                    else
                                        return 3;
                                else
                                    if (linear <= 1049312)
                                    if (linear <= 1049151)
                                        return 4;
                                    else
                                        return 5;
                                else
                                        if (linear <= 1049513)
                                    return 6;
                                else
                                    return 7;
                            else
                                if (linear <= 1051153)
                                if (linear <= 1050366)
                                    if (linear <= 1050039)
                                        return 8;
                                    else
                                        return 9;
                                else
                                    if (linear <= 1050737)
                                    return 10;
                                else
                                    return 11;
                            else
                                    if (linear <= 1052124)
                                if (linear <= 1051615)
                                    return 12;
                                else
                                    return 13;
                            else
                                        if (linear <= 1052679)
                                return 14;
                            else
                                return 15;
                        else
                            if (linear <= 1059912)
                            if (linear <= 1056190)
                                if (linear <= 1054637)
                                    if (linear <= 1053936)
                                        return 16;
                                    else
                                        return 17;
                                else
                                    if (linear <= 1055389)
                                    return 18;
                                else
                                    return 19;
                            else
                                if (linear <= 1057947)
                                if (linear <= 1057043)
                                    return 20;
                                else
                                    return 21;
                            else
                                    if (linear <= 1058903)
                                return 22;
                            else
                                return 23;
                        else
                                if (linear <= 1064478)
                            if (linear <= 1062087)
                                if (linear <= 1060973)
                                    return 24;
                                else
                                    return 25;
                            else
                                if (linear <= 1063256)
                                return 26;
                            else
                                return 27;
                        else
                                    if (linear <= 1067087)
                            if (linear <= 1065755)
                                return 28;
                            else
                                return 29;
                        else
                                        if (linear <= 1068474)
                            return 30;
                        else
                            return 31;
                    else
                        if (linear <= 1100843)
                        if (linear <= 1083505)
                            if (linear <= 1076252)
                                if (linear <= 1072971)
                                    if (linear <= 1071416)
                                        return 32;
                                    else
                                        return 33;
                                else
                                    if (linear <= 1074583)
                                    return 34;
                                else
                                    return 35;
                            else
                                if (linear <= 1079762)
                                if (linear <= 1077978)
                                    return 36;
                                else
                                    return 37;
                            else
                                    if (linear <= 1081604)
                                return 38;
                            else
                                return 39;
                        else
                            if (linear <= 1091696)
                            if (linear <= 1087482)
                                if (linear <= 1085464)
                                    return 40;
                                else
                                    return 41;
                            else
                                if (linear <= 1089559)
                                return 42;
                            else
                                return 43;
                        else
                                if (linear <= 1096148)
                            if (linear <= 1093892)
                                return 44;
                            else
                                return 45;
                        else
                                    if (linear <= 1098465)
                            return 46;
                        else
                            return 47;
                    else
                            if (linear <= 1122072)
                        if (linear <= 1110963)
                            if (linear <= 1105780)
                                if (linear <= 1103281)
                                    return 48;
                                else
                                    return 49;
                            else
                                if (linear <= 1108341)
                                return 50;
                            else
                                return 51;
                        else
                            if (linear <= 1116393)
                            if (linear <= 1113647)
                                return 52;
                            else
                                return 53;
                        else
                                if (linear <= 1119201)
                            return 54;
                        else
                            return 55;
                    else
                                if (linear <= 1134185)
                        if (linear <= 1128002)
                            if (linear <= 1125006)
                                return 56;
                            else
                                return 57;
                        else
                            if (linear <= 1131062)
                            return 58;
                        else
                            return 59;
                    else
                                    if (linear <= 1140623)
                        if (linear <= 1137372)
                            return 60;
                        else
                            return 61;
                    else
                                        if (linear <= 1143937)
                        return 62;
                    else
                        return 63;
                else
                    if (linear <= 1290493)
                    if (linear <= 1210279)
                        if (linear <= 1176685)
                            if (linear <= 1161479)
                                if (linear <= 1154268)
                                    if (linear <= 1150760)
                                        return 64;
                                    else
                                        return 65;
                                else
                                    if (linear <= 1157841)
                                    return 66;
                                else
                                    return 67;
                            else
                                if (linear <= 1168951)
                                if (linear <= 1165182)
                                    return 68;
                                else
                                    return 69;
                            else
                                    if (linear <= 1172785)
                                return 70;
                            else
                                return 71;
                        else
                            if (linear <= 1192948)
                            if (linear <= 1184684)
                                if (linear <= 1180652)
                                    return 72;
                                else
                                    return 73;
                            else
                                if (linear <= 1188783)
                                return 74;
                            else
                                return 75;
                        else
                                if (linear <= 1201479)
                            if (linear <= 1197180)
                                return 76;
                            else
                                return 77;
                        else
                                    if (linear <= 1205845)
                            return 78;
                        else
                            return 79;
                    else
                        if (linear <= 1248187)
                        if (linear <= 1228688)
                            if (linear <= 1219348)
                                if (linear <= 1214779)
                                    return 80;
                                else
                                    return 81;
                            else
                                if (linear <= 1223984)
                                return 82;
                            else
                                return 83;
                        else
                            if (linear <= 1238300)
                            if (linear <= 1233460)
                                return 84;
                            else
                                return 85;
                        else
                                if (linear <= 1243209)
                            return 86;
                        else
                            return 87;
                    else
                            if (linear <= 1268785)
                        if (linear <= 1258348)
                            if (linear <= 1253233)
                                return 88;
                            else
                                return 89;
                        else
                            if (linear <= 1263532)
                            return 90;
                        else
                            return 91;
                    else
                                if (linear <= 1279500)
                        if (linear <= 1274108)
                            return 92;
                        else
                            return 93;
                    else
                                    if (linear <= 1284961)
                        return 94;
                    else
                        return 95;
                else
                        if (linear <= 1388607)
                    if (linear <= 1337275)
                        if (linear <= 1313320)
                            if (linear <= 1301766)
                                if (linear <= 1296094)
                                    return 96;
                                else
                                    return 97;
                            else
                                if (linear <= 1307508)
                                return 98;
                            else
                                return 99;
                        else
                            if (linear <= 1325156)
                            if (linear <= 1319203)
                                return 100;
                            else
                                return 101;
                        else
                                if (linear <= 1331180)
                            return 102;
                        else
                            return 103;
                    else
                        if (linear <= 1362368)
                        if (linear <= 1349679)
                            if (linear <= 1343442)
                                return 104;
                            else
                                return 105;
                        else
                            if (linear <= 1355988)
                            return 106;
                        else
                            return 107;
                    else
                            if (linear <= 1375343)
                        if (linear <= 1368820)
                            return 108;
                        else
                            return 109;
                    else
                                if (linear <= 1381939)
                        return 110;
                    else
                        return 111;
                else
                            if (linear <= 1444555)
                    if (linear <= 1415999)
                        if (linear <= 1402158)
                            if (linear <= 1395346)
                                return 112;
                            else
                                return 113;
                        else
                            if (linear <= 1409043)
                            return 114;
                        else
                            return 115;
                    else
                        if (linear <= 1430131)
                        if (linear <= 1423029)
                            return 116;
                        else
                            return 117;
                    else
                            if (linear <= 1437307)
                        return 118;
                    else
                        return 119;
                else
                                if (linear <= 1474281)
                    if (linear <= 1459271)
                        if (linear <= 1451876)
                            return 120;
                        else
                            return 121;
                    else
                        if (linear <= 1466739)
                        return 122;
                    else
                        return 123;
                else
                                    if (linear <= 1489585)
                    if (linear <= 1481896)
                        return 124;
                    else
                        return 125;
                else
                                        if (linear <= 1497348)
                    return 126;
                else
                    return 127;
            else
                if (linear <= 2165530)
                if (linear <= 1795698)
                    if (linear <= 1640730)
                        if (linear <= 1570557)
                            if (linear <= 1537274)
                                if (linear <= 1521081)
                                    if (linear <= 1513096)
                                        return 128;
                                    else
                                        return 129;
                                else
                                    if (linear <= 1529140)
                                    return 130;
                                else
                                    return 131;
                            else
                                if (linear <= 1553766)
                                if (linear <= 1545483)
                                    return 132;
                                else
                                    return 133;
                            else
                                    if (linear <= 1562124)
                                return 134;
                            else
                                return 135;
                        else
                            if (linear <= 1605040)
                            if (linear <= 1587648)
                                if (linear <= 1579065)
                                    return 136;
                                else
                                    return 137;
                            else
                                if (linear <= 1596306)
                                return 138;
                            else
                                return 139;
                        else
                                if (linear <= 1622734)
                            if (linear <= 1613849)
                                return 140;
                            else
                                return 141;
                        else
                                    if (linear <= 1631694)
                            return 142;
                        else
                            return 143;
                    else
                        if (linear <= 1715759)
                        if (linear <= 1677634)
                            if (linear <= 1659030)
                                if (linear <= 1649842)
                                    return 144;
                                else
                                    return 145;
                            else
                                if (linear <= 1668294)
                                return 146;
                            else
                                return 147;
                        else
                            if (linear <= 1696544)
                            if (linear <= 1687051)
                                return 148;
                            else
                                return 149;
                        else
                                if (linear <= 1706113)
                            return 150;
                        else
                            return 151;
                    else
                            if (linear <= 1755112)
                        if (linear <= 1735282)
                            if (linear <= 1725482)
                                return 152;
                            else
                                return 153;
                        else
                            if (linear <= 1745158)
                            return 154;
                        else
                            return 155;
                    else
                                if (linear <= 1775250)
                        if (linear <= 1765142)
                            return 156;
                        else
                            return 157;
                    else
                                    if (linear <= 1785435)
                        return 158;
                    else
                        return 159;
                else
                    if (linear <= 1970503)
                    if (linear <= 1880596)
                        if (linear <= 1837524)
                            if (linear <= 1816456)
                                if (linear <= 1806038)
                                    return 160;
                                else
                                    return 161;
                            else
                                if (linear <= 1826951)
                                return 162;
                            else
                                return 163;
                        else
                            if (linear <= 1858904)
                            if (linear <= 1848175)
                                return 164;
                            else
                                return 165;
                        else
                                if (linear <= 1869711)
                            return 166;
                        else
                            return 167;
                    else
                        if (linear <= 1924921)
                        if (linear <= 1902602)
                            if (linear <= 1891560)
                                return 168;
                            else
                                return 169;
                        else
                            if (linear <= 1913722)
                            return 170;
                        else
                            return 171;
                    else
                            if (linear <= 1947554)
                        if (linear <= 1936198)
                            return 172;
                        else
                            return 173;
                    else
                                if (linear <= 1958989)
                        return 174;
                    else
                        return 175;
                else
                        if (linear <= 2065466)
                    if (linear <= 2017350)
                        if (linear <= 1993768)
                            if (linear <= 1982096)
                                return 176;
                            else
                                return 177;
                        else
                            if (linear <= 2005519)
                            return 178;
                        else
                            return 179;
                    else
                        if (linear <= 2041249)
                        if (linear <= 2029260)
                            return 180;
                        else
                            return 181;
                    else
                            if (linear <= 2053318)
                        return 182;
                    else
                        return 183;
                else
                            if (linear <= 2114858)
                    if (linear <= 2090002)
                        if (linear <= 2077694)
                            return 184;
                        else
                            return 185;
                    else
                        if (linear <= 2102390)
                        return 186;
                    else
                        return 187;
                else
                                if (linear <= 2140033)
                    if (linear <= 2127406)
                        return 188;
                    else
                        return 189;
                else
                                    if (linear <= 2152742)
                    return 190;
                else
                    return 191;
            else
                    if (linear <= 2617649)
                if (linear <= 2381134)
                    if (linear <= 2270739)
                        if (linear <= 2217489)
                            if (linear <= 2191348)
                                if (linear <= 2178399)
                                    return 192;
                                else
                                    return 193;
                            else
                                if (linear <= 2204378)
                                return 194;
                            else
                                return 195;
                        else
                            if (linear <= 2243952)
                            if (linear <= 2230680)
                                return 196;
                            else
                                return 197;
                        else
                                if (linear <= 2257305)
                            return 198;
                        else
                            return 199;
                    else
                        if (linear <= 2325286)
                        if (linear <= 2297850)
                            if (linear <= 2284254)
                                return 200;
                            else
                                return 201;
                        else
                            if (linear <= 2311527)
                            return 202;
                        else
                            return 203;
                    else
                            if (linear <= 2353047)
                        if (linear <= 2339125)
                            return 204;
                        else
                            return 205;
                    else
                                if (linear <= 2367050)
                        return 206;
                    else
                        return 207;
                else
                    if (linear <= 2496758)
                    if (linear <= 2438290)
                        if (linear <= 2409549)
                            if (linear <= 2395301)
                                return 208;
                            else
                                return 209;
                        else
                            if (linear <= 2423878)
                            return 210;
                        else
                            return 211;
                    else
                        if (linear <= 2467360)
                        if (linear <= 2452784)
                            return 212;
                        else
                            return 213;
                    else
                            if (linear <= 2482018)
                        return 214;
                    else
                        return 215;
                else
                        if (linear <= 2556543)
                    if (linear <= 2526485)
                        if (linear <= 2511580)
                            return 216;
                        else
                            return 217;
                    else
                        if (linear <= 2541473)
                        return 218;
                    else
                        return 219;
                else
                            if (linear <= 2586930)
                    if (linear <= 2571695)
                        return 220;
                    else
                        return 221;
                else
                                if (linear <= 2602248)
                    return 222;
                else
                    return 223;
            else
                        if (linear <= 2875388)
                if (linear <= 2743847)
                    if (linear <= 2680082)
                        if (linear <= 2648700)
                            if (linear <= 2633133)
                                return 224;
                            else
                                return 225;
                        else
                            if (linear <= 2664349)
                            return 226;
                        else
                            return 227;
                    else
                        if (linear <= 2711798)
                        if (linear <= 2695898)
                            return 228;
                        else
                            return 229;
                    else
                            if (linear <= 2727781)
                        return 230;
                    else
                        return 231;
                else
                    if (linear <= 2808947)
                    if (linear <= 2776230)
                        if (linear <= 2759996)
                            return 232;
                        else
                            return 233;
                    else
                        if (linear <= 2792547)
                        return 234;
                    else
                        return 235;
                else
                        if (linear <= 2842000)
                    if (linear <= 2825432)
                        return 236;
                    else
                        return 237;
                else
                            if (linear <= 2858652)
                    return 238;
                else
                    return 239;
            else
                            if (linear <= 3012309)
                if (linear <= 2943174)
                    if (linear <= 2909113)
                        if (linear <= 2892208)
                            return 240;
                        else
                            return 241;
                    else
                        if (linear <= 2926101)
                        return 242;
                    else
                        return 243;
                else
                    if (linear <= 2977573)
                    if (linear <= 2960331)
                        return 244;
                    else
                        return 245;
                else
                        if (linear <= 2994899)
                    return 246;
                else
                    return 247;
            else
                                if (linear <= 3082799)
                if (linear <= 3047385)
                    if (linear <= 3029805)
                        return 248;
                    else
                        return 249;
                else
                    if (linear <= 3065049)
                    return 250;
                else
                    return 251;
            else
                                    if (linear <= 3118553)
                if (linear <= 3100633)
                    return 252;
                else
                    return 253;
            else
                                        if (linear <= 3136557)
                return 254;
            else
                return 255;
        }


#if DEBUG
        public static double toLinear13_gamma2_2(double b)
#else
        private static double toLinear13_gamma2_2(double b)
#endif
        {
            const double gamma = 2.2;
            const int inBits = 13;
            const double bt = 7.340082380389742;
            const double t = 0.0004887902940293499;

            if (b < bt)
            {
                return b;
            }
            else
            {
                return ((1UL << inBits) - 1) * ((1 - t) * Math.Pow(b / 255, gamma) + t);
            }
        }


#if DEBUG
        public static double toLinear14_gamma2_2(double b)
#else
        private static double toLinear14_gamma2_2(double b)
#endif
        {
            const double gamma = 2.2;
            const int inBits = 14;
            const double bt = 4.118064975095984;
            const double t = 0.00013710658970538143;

            if (b < bt)
            {
                return b;
            }
            else
            {
                return ((1UL << inBits) - 1) * ((1 - t) * Math.Pow(b / 255, gamma) + t);
            }
        }

#if DEBUG
        public static double toLinear18_gamma2_2(double b)
#else
        private static double toLinear18_gamma2_2(double b)
#endif
        {
            const double gamma = 2.2;
            return ((1UL << 18) - 1) * Math.Pow(b / 255, gamma);
        }

#if DEBUG
        public static void gammaTableTest()
        {
            byte po18 = 0, po13 = 0, po14 = 0;
            for (var i = 0; i < 256; i++)
            {
               var o18 = fromLinearIn18Out31_gamma2_2((ulong)(toLinear18_gamma2_2(i) + (1UL << 17) + 0.5) << (31 - 18));
                if (o18 != i)
                {
                    System.Windows.Forms.MessageBox.Show($"18: {o18} != {i}");
                    break;
                }
                var o13 = fromLinearIn13Out20_gamma2_2((ulong)(toLinear13_gamma2_2(i) + (1UL << 12) + 0.5) << (20 - 13));
                if (o13 != i)
                {
                    System.Windows.Forms.MessageBox.Show($"13: {o13} != {i}");
                    break;
                }
                var o14 = fromLinearIn14Out21_gamma2_2((ulong)(toLinear14_gamma2_2(i) + (1UL << 13) + 0.5) << (21 - 14));
                if (o14 != i)
                {
                    System.Windows.Forms.MessageBox.Show($"14: {o14} != {i}");
                    break;
                }
                
                if (i > 0)
                {
                    if (o18 <= po18)
                    {
                        System.Windows.Forms.MessageBox.Show($"18p: {o18} <= {po18}");
                        break;
                    }
                    if (o13 <= po13)
                    {
                        System.Windows.Forms.MessageBox.Show($"13p: {o13} <= {po13}");
                        break;
                    }
                    if (o14 <= po14)
                    {
                        System.Windows.Forms.MessageBox.Show($"14p: {o14} <= {po14}");
                        break;
                    }

                }
                po18 = o18; po13 = o13; po14 = o14;
            }
            System.Windows.Forms.MessageBox.Show("finish");
        }

        const string returnCode = "\r\n"; 
        public static string CreateFromLinear(int bitsDiff, string name, Func<double,double> toLinear, ulong offset)
        {
            return
                $"private static byte {name}(ulong linear){returnCode}" +
                "{" + returnCode +
                CreateFromLinear("    ", 255, 0, 1UL << bitsDiff, toLinear, offset)
                + "}" + returnCode;
        }
        private static string CreateFromLinear(string indent, int upper, int lower, double valueDif, Func<double, double> toLinear, ulong offset)
        {
            if (upper > lower + 1)
            {
                var m0 = (upper + lower) / 2;
                var m1 = m0 + 1;
                var newIndent = indent + "    ";
                return
                    $"{indent}if (linear <= {(ulong)(valueDif * toLinear(Math.Sqrt(m0 * m1)) + 0.5) + offset}){returnCode}" +
                    CreateFromLinear(newIndent, m0, lower, valueDif, toLinear, offset) +
                    $"{indent}else{returnCode}" +
                    CreateFromLinear(newIndent, upper, m1, valueDif, toLinear, offset);
            }
            else
            {
                return
                    $"{indent}if (linear <= {(ulong)(valueDif * toLinear(Math.Sqrt(upper * lower)) + 0.5) + offset}){returnCode}" +
                    $"{indent}    return {lower};{returnCode}" +
                    $"{indent}else{returnCode}" +
                    $"{indent}    return {upper};{returnCode}";
            }
        }
#endif

        //private const double doubleEpsilon = 2.220446049250313080847263336181640625e-16; // 2^(1-53), 厳密値

        private static int getSuitabgleDigits(long r2)
        {
            if (r2 < 4) return 10;
            if (r2 < 64) return 11;
            return 12;
        }

        private static ulong[] getWeightArray(out int count, bool half, TryGetWeightDelegate1 tryGetWeight1, TryGetWeightDelegate2 tryGetWeight2, int srcLength, int dstLength, int step, int digitsWithoutSignBit)
        {
            if (dstLength <= 0 || srcLength <= 0)
            {
                count = 0;
                return new ulong[0];
            }

            var radius = getRadius(tryGetWeight1, tryGetWeight2, srcLength, dstLength, step);
            var minusRadius = -radius;
            count = radius * 2;

            var dstLength_ = (long)dstLength;
            var srcLength_ = (long)srcLength;

            long dstLength2 = dstLength_ * 2;
            long dstLengthStep = dstLength_ * step;
            long dstLength2Step = dstLength2 * step;
            double power = (double)dstLengthStep / srcLength_;
            var srcLengthPerStepCeiling = (srcLength_ + step - 1) / step;

            var resultLength = half ? (dstLength_ + 1) / 2 : dstLength_;
            var useTryGetWeight1 = tryGetWeight1 != null;
            var useDistanceAsDst = power < 1;

            var dstLength2Step1 = dstLength2Step - 1;
            
            var countl = (long)count;
            var result = new ulong[countl * resultLength];
            var resultI = new double[countl];

            if (digitsWithoutSignBit <= 0) digitsWithoutSignBit = getSuitabgleDigits(countl);
            var shift = 1 << digitsWithoutSignBit;

            // 1次元直流保証
            //var allowance = (1 << Math.Max(0, digitsWithoutSignBit - 9)) - 1;

            // 2次元直流保証
            //var allowance = (1 << Math.Max(0, digitsWithoutSignBit - 10)) - 1;

            // 直流優先
            const int allowance = 0;
            
            var bufferSize = countl / 2 - allowance; // 全ての数の端数が 0.5 だった場合に -count / 2 小さくなる
            var doubleBuffer = bufferSize <= 0 ? null : new double[bufferSize];
            var intBuffer = bufferSize <= 0 ? null : new int[bufferSize];

            var srcLengthMinusCount = srcLength_ - countl;
            var srcLengthMinus1 = srcLength_ - 1;

            for (long i = 0; i < resultLength; i++)
            {
                var iCount = i * countl;

                var iAsSrcTimesDstLength2 = (2 * i + 1) * srcLength_ - dstLength_;
                var iCeilingAsSrcPerStepMinusRadius = (iAsSrcTimesDstLength2 + dstLength2Step1) / dstLength2Step - radius; // 分子は負の数にできないので radius は通分してはいけない
                var iAsSrcPerStep = (double)iAsSrcTimesDstLength2 / dstLength2Step;
                
                if (0 <= iCeilingAsSrcPerStepMinusRadius)
                {
                    if (iCeilingAsSrcPerStepMinusRadius <= srcLengthMinusCount)
                    {
                        var jToDistanceOffset = iCeilingAsSrcPerStepMinusRadius - iAsSrcPerStep;

                        for (long j = 0; j < countl; j++)
                        {
                            double distance = jToDistanceOffset + j;
                            double weight;
                            if (useTryGetWeight1 ? tryGetWeight1(useDistanceAsDst ? power * distance : distance, out weight) : tryGetWeight2(distance, power, out weight))
                            {
                                resultI[j] = weight;
                            }
                            else
                            {
                                resultI[j] = 0;
                            }
                        }
                    }
                    else
                    {
                        Array.Clear(resultI, 0, count);
                        for (long j = 0; j < countl; j++)
                        {
                            var seek = iCeilingAsSrcPerStepMinusRadius + j;
                            double distance = (seek - iAsSrcPerStep);
                            double weight;
                            if (useTryGetWeight1 ? tryGetWeight1(useDistanceAsDst ? power * distance : distance, out weight) : tryGetWeight2(distance, power, out weight))
                            {
                                if (seek > srcLengthMinus1)
                                {
                                    resultI[j - (seek - srcLengthMinus1)] += weight;
                                }
                                else
                                {
                                    resultI[j] += weight;
                                }
                            }
                        }
                    }
                }
                else
                {
                    Array.Clear(resultI, 0, count);
                    if (iCeilingAsSrcPerStepMinusRadius <= srcLengthMinusCount)
                    {
                        for (long j = 0; j < countl; j++)
                        {
                            var seek = iCeilingAsSrcPerStepMinusRadius + j;
                            double distance = (seek - iAsSrcPerStep);
                            double weight;
                            if (useTryGetWeight1 ? tryGetWeight1(useDistanceAsDst ? power * distance : distance, out weight) : tryGetWeight2(distance, power, out weight))
                            {
                                if (seek < 0)
                                {
                                    resultI[j - seek] += weight;
                                }
                                else
                                {
                                    resultI[j] += weight;
                                }
                            }
                        }
                    }
                    else
                    {
                        for (long j = 0; j < countl; j++)
                        {
                            var seek = iCeilingAsSrcPerStepMinusRadius + j;
                            double distance = (seek - iAsSrcPerStep);
                            double weight;
                            if (useTryGetWeight1 ? tryGetWeight1(useDistanceAsDst ? power * distance : distance, out weight) : tryGetWeight2(distance, power, out weight))
                            {
                                if (seek < 0)
                                {
                                    resultI[j - seek] += weight;
                                }
                                else if (seek > srcLengthMinus1)
                                {
                                    resultI[j - (seek - srcLengthMinus1)] += weight;
                                }
                                else
                                {
                                    resultI[j] += weight;
                                }
                            }
                        }
                    }
                }

                Array.Copy(rigorousNormalization(shift, allowance, resultI, doubleBuffer, intBuffer), 0, result, i * countl, countl);
            }

            return result;
        }
        
        private static int getRadius(TryGetWeightDelegate1 tryGetWeight1, TryGetWeightDelegate2 tryGetWeight2, int srcLength, int dstLength, int step)
        {
            var result = 1;
            long dstLengthStep = dstLength * step;
            double weight;
            const double errorGuarantee = (1 + 1e-8);
            if (tryGetWeight1 != null)
            {
                if (dstLengthStep > srcLength)
                {
                    while (tryGetWeight1((result + 1) * errorGuarantee, out weight)) result++;
                }
                else
                {
                    var power = errorGuarantee * dstLengthStep / srcLength;
                    while (tryGetWeight1((result + 1) * power, out weight)) result++;
                }
            }
            else
            {
                var power = (double)dstLengthStep / srcLength;
                while (tryGetWeight2((result + 1) * errorGuarantee, power, out weight)) result++;
            }

            return result;
        }

        /*
        private static ulong[] getWeightArrayOld(out int count, bool half, TryGetWeightDelegate1 tryGetWeight1, TryGetWeightDelegate2 tryGetWeight2, int srcLength, int dstLength, int step, int digitsWithoutSignBit)
        {
            if (dstLength <= 0 || srcLength <= 0)
            {
                count = 0;
                return new ulong[0];
            }
            var dstLength_ = (long)dstLength;
            var srcLength_ = (long)srcLength;

            long dstLength2 = (long)dstLength * 2;
            long dstLengthStep = dstLength * step;
            long dstLength2Step = dstLength2 * step;
            double power = (double)dstLengthStep / srcLength;
            var srcLengthPerStepCeiling = (srcLength + step - 1) / step;

            var resultLength = half ? (dstLength_ + 1) / 2 : dstLength_;
            var listArray = new List<double>[resultLength];
            //var calcLength = Math.Min(resultLength, dstLength_ / gcd(dstLength_, srcLength_)); 先頭の端の処理が内部に影響をあたえるので誤り
            var useTryGetWeight1 = tryGetWeight1 != null;
            var useDistanceAsDst = power < 1;
            count = 0;
            for (long i = 0; i < resultLength; i++)
            {
                var weights = new List<double>();

                var iAsSrcTimesDstLength2 = (2 * i + 1) * srcLength_ - dstLength_;
                var iNearestAsSrcPerStep = (iAsSrcTimesDstLength2 + dstLengthStep) / dstLength2Step;
                var plus = iNearestAsSrcPerStep * dstLength2Step >= iAsSrcTimesDstLength2;
                var abs2 = 1;
                var iAsSrcPerStep = (double)iAsSrcTimesDstLength2 / dstLength2Step;
                var iNearestAsSrcPerStepInt = (int)iNearestAsSrcPerStep;

                while (true)
                {
                    var d = plus ? (abs2 >> 1) : -(abs2 >> 1);
                    var seek = iNearestAsSrcPerStepInt + d;
                    double distance = (seek - iAsSrcPerStep);
                    bool success;
                    double weight;
                    if (useTryGetWeight1)
                    {
                        success = tryGetWeight1(useDistanceAsDst ? power * distance : distance, out weight);
                    }
                    else
                    {
                        success = tryGetWeight2(distance, power, out weight);
                    }
                    if (success)
                    {
                        weights.Add(weight);
                    }
                    else
                    {
                        break;
                    }
                    abs2++;
                    plus = !plus;
                }

                var weightsCount = weights.Count;
                if ((weightsCount & 1) != 0)
                {
                    weights.Add(0);
                    weightsCount++;
                }

                if (count < weightsCount) count = weightsCount;

                listArray[i] = weights;
            };

            var countl = (long)count;
            var result = new ulong[countl * resultLength];

            if (digitsWithoutSignBit <= 0) digitsWithoutSignBit = getSuitabgleDigits(countl);
            var shift = 1 << digitsWithoutSignBit;
            var allowance = (1 << Math.Max(0, digitsWithoutSignBit - 9)) - 1;

            var bufferSize = countl / 2 - allowance; // 全ての数の端数が 0.5 だった場合に -count / 2 小さくなる
            var doubleBuffer = bufferSize <= 0 ? null : new double[bufferSize];
            var intBuffer = bufferSize <= 0 ? null : new int[bufferSize];

            for (long i = 0; i < resultLength; i++)
            {
                var weights = listArray[i];
                var iAsSrcTimesDstLength2 = (2 * i + 1) * srcLength_ - dstLength_;
                var iNearestAsSrcPerStep = (iAsSrcTimesDstLength2 + dstLengthStep) / dstLength2Step;
                var plus = iNearestAsSrcPerStep * dstLength2Step >= iAsSrcTimesDstLength2;
                var iNearestAsSrcPerStepInt = (int)iNearestAsSrcPerStep;

                var offset = ((plus ? countl : countl - 1) / 2);
                var doubleResult = new double[countl];
                var abs2 = 1;
                foreach (var weight in weights)
                {
                    var d = plus ? (abs2 >> 1) : -(abs2 >> 1);
                    var seek = iNearestAsSrcPerStepInt + d;
                    var index = d + offset;
                    if (seek < 0)
                    {
                        doubleResult[index - seek] += weight;
                    }
                    else if (seek >= srcLength)
                    {
                        doubleResult[index - (seek - srcLength + 1)] += weight;
                    }
                    else
                    {
                        doubleResult[index] = weight;
                    }

                    abs2++;
                    plus = !plus;
                }
                Array.Copy(rigorousNormalization(shift, allowance, doubleResult, doubleBuffer, intBuffer), 0, result, i * countl, countl);
            };
            
            return result;
        }
        */
        

        /*
        private static long gcd(long a, long b)
        {
            while (a != 0)
            {
                var a_ = a;
                a = b % a;
                b = a_;
            }
            return b;
        }
        */

        private static ulong[] rigorousNormalization(long total, long allowance, double[] input, double[] doubleBuffer, int[] intBuffer)
        {
            if (total == 0) return new ulong[input.Length];
            var sum = input.Sum() / total;
            if (sum == 0) return new ulong[input.Length];
            var result = new long[input.Length];
            for (var k = 0; k < input.Length; k++)
            {
                var v = input[k] / sum;
                input[k] = v;
                var i = (long)Math.Round(v);
                result[k] = i;
                total -= i;
            }

            if (allowance < 0) allowance = 0;

            var over = Math.Abs(total) - allowance;
            if (over > 0)
            {
                var bound = 0.0;
                var tail = over - 1;
                var over1 = over - 1;
                var targetValues = doubleBuffer;
                var targetIndices = intBuffer;

                if (total > 0)
                {
                    for (var k = 0; k < input.Length; k++)
                    {
                        var sub = input[k] - result[k];
                        if (sub > bound)
                        {
                            var insert = tail;
                            for (; insert < over1; insert++)
                            {
                                var insert1 = insert + 1;
                                var t = targetValues[insert1];
                                if (sub > t)
                                {
                                    targetIndices[insert] = targetIndices[insert1];
                                    targetValues[insert] = t;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            targetIndices[insert] = k;
                            targetValues[insert] = sub;
                            if (tail > 0) tail--;
                            else bound = targetValues[0];
                        }
                    }

                    for (var i = 0; i < over; i++)
                    {
                        result[targetIndices[i]]++;
                    }
                }
                else
                {
                    for (var k = 0; k < input.Length; k++)
                    {
                        var sub = input[k] - result[k];
                        if (sub < bound)
                        {
                            var insert = tail;
                            for (; insert < over1; insert++)
                            {
                                var insert1 = insert + 1;
                                var t = targetValues[insert1];
                                if (sub < t)
                                {
                                    targetIndices[insert] = targetIndices[insert1];
                                    targetValues[insert] = t;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            targetIndices[insert] = k;
                            targetValues[insert] = sub;
                            if (tail > 0) tail--;
                            else bound = targetValues[0];
                        }
                    }

                    for (var i = 0; i < over; i++)
                    {
                        result[targetIndices[i]]--;
                    }
                }
            }

            return result.Select(i =>(ulong)i).ToArray();
        }
        
#region 最近傍法の個別実装
        public static Bitmap NearestNeighbor(Bitmap src, Size newSize, bool parallel, BackgroundWorker backgroundWorker = null)
        {
            return nearestNeighbor(src, newSize, parallel, backgroundWorker,
                accurate: newSize.Width % src.Width != 0 || newSize.Height % src.Height != 0);
        }
        public static void NearestNeighbor(Bitmap src, Bitmap dst, Rectangle dstRectangle, bool parallel, BackgroundWorker backgroundWorker = null)
        {
            DrawResizedBitmapByNearestNeighbor(src, 0, 0, src.Width, src.Height, dst,
                dstRectangle.X, dstRectangle.Y, dstRectangle.Width, dstRectangle.Height,
                parallel, backgroundWorker, accurate: dstRectangle.Width % src.Width != 0 || dstRectangle.Height % src.Height != 0);
        }
        public static void NearestNeighbor(BitmapData src, Rectangle srcRectangle, BitmapData dst, Rectangle dstRectangle, bool parallel, BackgroundWorker backgroundWorker = null)
        {
            DrawResizedBitmapByNearestNeighbor(src, srcRectangle.X, srcRectangle.Y, srcRectangle.Width, srcRectangle.Height, dst,
                dstRectangle.X, dstRectangle.Y, dstRectangle.Width, dstRectangle.Height,
                parallel, backgroundWorker, accurate: dstRectangle.Width % srcRectangle.Width != 0 || dstRectangle.Height % srcRectangle.Height != 0);
        }

        private static Bitmap nearestNeighbor(Bitmap src, Size newSize, bool parallel, BackgroundWorker backgroundWorker, bool accurate, bool convertTo24bpp = false)
        {
            var result = new Bitmap(newSize.Width, newSize.Height,
                !convertTo24bpp && src.PixelFormat == PixelFormat.Format8bppIndexed ?
                PixelFormat.Format8bppIndexed : PixelFormat.Format24bppRgb);
            try
            {
                DrawResizedBitmapByNearestNeighbor(src, 0, 0, src.Width, src.Height, result, 0, 0, newSize.Width, newSize.Height,
                    parallel, backgroundWorker, accurate);
                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }

        private static void DrawResizedBitmapByNearestNeighbor(
            Bitmap src, int srcX, int srcY, int srcWidth, int srcHeight,
            Bitmap dst, int dstX, int dstY, int dstWidth, int dstHeight,
            bool parallel, BackgroundWorker backgroundWorker, bool accurate)
        {
            var backgroundWorkerExists = backgroundWorker != null;
            if (backgroundWorkerExists && backgroundWorker.CancellationPending) return;

            if (src.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                if (dst.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    dst.Palette = src.Palette;
                    var dstData = dst.LockBits(new Rectangle(dstX, dstY, dstWidth, dstHeight), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                    try
                    {
                        DrawResizedBitmapByNearestNeighbor(src, srcX, srcY, srcWidth, srcHeight, dstData, 0, 0, dstWidth, dstHeight, parallel, backgroundWorker, accurate);
                    }
                    finally
                    {
                        dst.UnlockBits(dstData);
                    }
                }
                else
                {
                    var dstData = dst.LockBits(new Rectangle(dstX, dstY, dstWidth, dstHeight), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                    try
                    {
                        DrawResizedBitmapByNearestNeighbor(src, srcX, srcY, srcWidth, srcHeight, dstData, 0, 0, dstWidth, dstHeight, parallel, backgroundWorker, accurate);
                    }
                    finally
                    {
                        dst.UnlockBits(dstData);
                    }
                }
            }
            else
            {
                var dstData = dst.LockBits(new Rectangle(dstX, dstY, dstWidth, dstHeight), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                try
                {
                    DrawResizedBitmapByNearestNeighbor(src, srcX, srcY, srcWidth, srcHeight, dstData, 0, 0, dstWidth, dstHeight, parallel, backgroundWorker, accurate);
                }
                finally
                {
                    dst.UnlockBits(dstData);
                }
            }
        }

        private static void myFor(bool parallel, int fromInclusive, int toExclusive, Func<int, bool> body)
        {
            if (parallel)
            {
                Parallel.For(fromInclusive, toExclusive, (i, loop) =>
                {
                    if (!body(i)) loop.Break();
                });
            }
            else
            {
                for (var i = fromInclusive; i < toExclusive; i++)
                {
                    if (!body(i)) break;
                }
            }
        }

        private static void DrawResizedBitmapByNearestNeighbor(
            Bitmap src, int srcX, int srcY, int srcWidth, int srcHeight,
            BitmapData dstData, int dstX, int dstY, int dstWidth, int dstHeight,
            bool parallel, BackgroundWorker backgroundWorker, bool accurate)
        {
            var backgroundWorkerExists = backgroundWorker != null;
            if (backgroundWorkerExists && backgroundWorker.CancellationPending) return;

            if (src.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                var srcData = src.LockBits(new Rectangle(srcX, srcY, srcWidth, srcHeight), ImageLockMode.ReadOnly, PixelFormat.Format8bppIndexed);
                try
                {
                    DrawResizedBitmapByNearestNeighbor(srcData, 0, 0, srcWidth, srcHeight, dstData, dstX, dstY, dstWidth, dstHeight, parallel, backgroundWorker, accurate);
                }
                finally
                {
                    src.UnlockBits(srcData);
                }
            }
            else
            {
                var srcData = src.LockBits(new Rectangle(srcX, srcY, srcWidth, srcHeight), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                try
                {
                    DrawResizedBitmapByNearestNeighbor(srcData, 0, 0, srcWidth, srcHeight, dstData, dstX, dstY, dstWidth, dstHeight, parallel, backgroundWorker, accurate);
                }
                finally
                {
                    src.UnlockBits(srcData);
                }
            }
        }

        private static void DrawResizedBitmapByNearestNeighbor(
            BitmapData srcData, int srcX, int srcY, int srcWidth, int srcHeight,
            BitmapData dstData, int dstX, int dstY, int dstWidth, int dstHeight,
            bool parallel, BackgroundWorker backgroundWorker, bool accurate)
        {
            var backgroundWorkerExists = backgroundWorker != null;
            if (backgroundWorkerExists && backgroundWorker.CancellationPending) return;

            int dstXStop = dstX + dstWidth;
            int dstYStop = dstY + dstHeight;

            if (srcData.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                if (dstData.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    unsafe
                    {
                        var srcStride = srcData.Stride;
                        var src0 = (byte*)srcData.Scan0 + srcX + srcY * srcStride;
                        var dst0 = (byte*)dstData.Scan0;
                        var dstStride = dstData.Stride;

                        if (accurate)
                        {
                            myFor(parallel, dstY, dstYStop, y =>
                            {
                                if (backgroundWorkerExists && backgroundWorker.CancellationPending)
                                {
                                    return false;
                                }
                                var dstLeft = dst0 + y * dstStride;
                                var ySrc = ((2 * (y - dstY) + 1) * srcHeight) / (2 * dstHeight);
                                var srcLeft = src0 + ySrc * srcStride;
                                for (var x = dstX; x < dstXStop; x++)
                                {
                                    var xSrc = ((2 * (x - dstX) + 1) * srcWidth) / (2 * dstWidth);
                                    var adrDst = dstLeft + x;
                                    var adrSrc = srcLeft + xSrc;
                                    var v = adrSrc[0];
                                    adrDst[0] = v;
                                }
                                return true;
                            });
                        }
                        else
                        {
                            myFor(parallel, dstY, dstYStop, y =>
                            {
                                if (backgroundWorkerExists && backgroundWorker.CancellationPending)
                                {
                                    return false;
                                }
                                var dstLeft = dst0 + y * dstStride;
                                var ySrc = ((y - dstY) * srcHeight) / dstHeight;
                                var srcLeft = src0 + ySrc * srcStride;
                                for (var x = dstX; x < dstXStop; x++)
                                {
                                    var xSrc = ((x - dstX) * srcWidth) / dstWidth;
                                    var adrDst = dstLeft + x;
                                    var adrSrc = srcLeft + xSrc;
                                    var v = adrSrc[0];
                                    adrDst[0] = v;
                                }
                                return true;
                            });
                        }
                    }
                }
                else
                {
                    unsafe
                    {
                        var srcStride = srcData.Stride;
                        var src0 = (byte*)srcData.Scan0 + srcX + srcY * srcStride;
                        var dst0 = (byte*)dstData.Scan0;
                        var dstStride = dstData.Stride;

                        if (accurate)
                        {
                            myFor(parallel, dstY, dstYStop, y =>
                            {
                                if (backgroundWorkerExists && backgroundWorker.CancellationPending)
                                {
                                    return false;
                                }
                                var dstLeft = dst0 + y * dstStride;
                                var ySrc = ((2 * (y - dstY) + 1) * srcHeight) / (2 * dstHeight);
                                var srcLeft = src0 + ySrc * srcStride;
                                for (var x = dstX; x < dstXStop; x++)
                                {
                                    var xSrc = ((2 * (x - dstX) + 1) * srcWidth) / (2 * dstWidth);
                                    var adrDst = dstLeft + x * 3;
                                    var adrSrc = srcLeft + xSrc;
                                    var v = adrSrc[0];
                                    adrDst[0] = v;
                                    adrDst[1] = v;
                                    adrDst[2] = v;
                                }
                                return true;
                            });
                        }
                        else
                        {
                            myFor(parallel, dstY, dstYStop, y =>
                            {
                                if (backgroundWorkerExists && backgroundWorker.CancellationPending)
                                {
                                    return false;
                                }
                                var dstLeft = dst0 + y * dstStride;
                                var ySrc = ((y - dstY) * srcHeight) / dstHeight;
                                var srcLeft = src0 + ySrc * srcStride;
                                for (var x = dstX; x < dstXStop; x++)
                                {
                                    var xSrc = ((x - dstX) * srcWidth) / dstWidth;
                                    var adrDst = dstLeft + x * 3;
                                    var adrSrc = srcLeft + xSrc;
                                    var v = adrSrc[0];
                                    adrDst[0] = v;
                                    adrDst[1] = v;
                                    adrDst[2] = v;
                                }
                                return true;
                            });
                        }
                    }
                }
            }
            else
            {
                unsafe
                {
                    var srcStride = srcData.Stride;
                    var src0 = (byte*)srcData.Scan0 + srcX * 3 + srcY * srcStride;
                    var dst0 = (byte*)dstData.Scan0;
                    var dstStride = dstData.Stride;

                    if (accurate)
                    {
                        myFor(parallel, dstY, dstYStop, y =>
                        {
                            if (backgroundWorkerExists && backgroundWorker.CancellationPending)
                            {
                                return false;
                            }
                            var dstLeft = dst0 + y * dstStride;
                            var ySrc = ((2 * (y - dstY) + 1) * srcHeight) / (2 * dstHeight);
                            var srcLeft = src0 + ySrc * srcStride;
                            for (var x = dstX; x < dstXStop; x++)
                            {
                                var xSrc = ((2 * (x - dstX) + 1) * srcWidth) / (2 * dstWidth);
                                var adrDst = dstLeft + x * 3;
                                var adrSrc = srcLeft + xSrc * 3;
                                adrDst[0] = adrSrc[0];
                                adrDst[1] = adrSrc[1];
                                adrDst[2] = adrSrc[2];
                            }
                            return true;
                        });
                    }
                    else
                    {
                        myFor(parallel, dstY, dstYStop, y =>
                        {
                            if (backgroundWorkerExists && backgroundWorker.CancellationPending)
                            {
                                return false;
                            }
                            var dstLeft = dst0 + y * dstStride;
                            var ySrc = ((y - dstY) * srcHeight) / dstHeight;
                            var srcLeft = src0 + ySrc * srcStride;
                            for (var x = dstX; x < dstXStop; x++)
                            {
                                var xSrc = ((x - dstX) * srcWidth) / dstWidth;
                                var adrDst = dstLeft + x * 3;
                                var adrSrc = srcLeft + xSrc * 3;
                                adrDst[0] = adrSrc[0];
                                adrDst[1] = adrSrc[1];
                                adrDst[2] = adrSrc[2];
                            }
                            return true;
                        });
                    }
                }
            }
        }
        
        #endregion
    }


}
