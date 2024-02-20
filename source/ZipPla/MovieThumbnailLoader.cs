using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ZipPla
{
    public static class MovieThumbnailLoader
    {
        public const int FfmpegCancelCheckInterval = 10;

        public static Bitmap GetThumbnail(string filePath, Func<Fraction, Size, Size> sizeProvider, Func<TimeSpan, TimeSpan> positionProvider, BackgroundWorker backgroundWorker, TimeSpan timeOut, ref MovieInfo movieInfo)
        {
            if(movieInfo == null) movieInfo = MovieInfo.FromFile(filePath);
            var size = movieInfo.SizeByPixel;
            if (size.IsEmpty) throw new Exception();
            bool originalSize;
            //if(!size.IsEmpty)
            {
                Fraction fraction;
                if (movieInfo.SAR == 1 || movieInfo.SAR == 0) fraction = (Fraction)size;
                else fraction = (Fraction)size * movieInfo.SAR;
                if(sizeProvider == null)
                {
                    originalSize = true;
                }
                else
                {
                    var temp = size;
                    size = sizeProvider(fraction, size);
                    originalSize = size == temp;
                }
            }
            TimeSpan position;
            if (positionProvider == null)
            {
                position = TimeSpan.MaxValue;
            }
            else
            {
                var duration = movieInfo.Duration;
                position = positionProvider(duration);

                // 指定時刻が終了時刻に極端に近いと期待通りに画像が表示されないことがある
                duration = duration.Subtract(TimeSpan.FromSeconds(1));
                if (position > duration) position = duration;
                if (position < TimeSpan.Zero) position = TimeSpan.Zero;
            }
            return GetThumbnail(filePath, size, originalSize, position, backgroundWorker);
        }

        public static Bitmap GetImageByFfmpeg(string filePath)
        {
            int outputSize;
            var cmd = $"-i \"{filePath}\" -an -vcodec bmp -f image2 pipe:1";
            
            var buf = readStdOutBinary(ffmpegPath, cmd, null, null, out outputSize);

            using (var ms = new MemoryStream(buf, 0, outputSize, writable: false, publiclyVisible: false))
            {
                try
                {
                    return new Bitmap(ms);
                }
                finally
                {
                    ms.Close();
                }
            }
        }

        public static Bitmap GetImageByFfmpeg(Stream stream)
        {
            // Don't use a pipe
            // http://stackoverflow.com/questions/23002316/ffmpeg-pipe0-could-not-find-codec-parameters

            var tempFileName = Path.GetTempFileName(); // この時点でファイルは作られる
            try
            {
                using (var s = new FileStream(tempFileName, FileMode.Open, FileAccess.Write))
                {
                    stream.CopyTo(s);
                }
                return GetImageByFfmpeg(tempFileName);
            }
            finally
            {
                File.Delete(tempFileName);
            }
        }

        //private static readonly object GetThumbnail_ffmpegLocker = new object();
        private static Bitmap GetThumbnail(string filePath, Size size, bool originalSize, TimeSpan position, BackgroundWorker backgroundWorker)
        {
            int outputSize;
            //var buf = readStdOutBinary(ffmpegPath, $"-ss {position.TotalSeconds} -i \"{filePath}\"" + (size.IsEmpty ? "" : $" -sws_flags fast_bilinear -s {size.Width}x{size.Height}") + " -vframes 1 -vcodec bmp -f image2 pipe:1",

            string cmd;
            if (position < TimeSpan.MaxValue)
            {
                cmd = $"-ss {position.TotalSeconds} -i \"{filePath}\"" + (originalSize ? "" : $" -s {size.Width}x{size.Height}") + " -vframes 1 -an -vcodec bmp -f image2 pipe:1";
            }
            else
            {
                cmd = $"-i \"{filePath}\" -vf " + (originalSize ? "" : $"scale={size.Width}x{size.Height},") + "thumbnail -vframes 1 -an -vcodec bmp -f image2 pipe:1";
            }

            byte[] buf;
            //lock (GetThumbnail_ffmpegLocker)
            {
                buf = readStdOutBinary(ffmpegPath, cmd,
                backgroundWorker, size.Width * size.Height * 4 + 124, out outputSize); // http://www.ruche-home.net/program/bmp/struct 実際には * 3 + 54 が多いが、 * 4 + 54 のケースもある
            }

            using (var ms = new MemoryStream(buf, 0, outputSize, writable: false, publiclyVisible: false))
            {
                try
                {
                    var result = new Bitmap(ms);
                    if(result.Size != size)
                    {
                        result.Dispose();
                        throw new Exception();
                    }
                    return result;
                }
                finally
                {
                    ms.Close();
                }
            }
        }
        
        private static byte[] readStdOutBinary(string fileName, string arguments, BackgroundWorker backgroundWorker, int buffSize, out int outputSize)
        {
            var psi = new ProcessStartInfo(fileName, arguments);
            psi.CreateNoWindow = true; // コンソール・ウィンドウを開かない
            psi.UseShellExecute = false; // シェル機能を使用しない
            psi.RedirectStandardOutput = true; // 標準出力をリダイレクト
            byte[] buf = new byte[buffSize];
            outputSize = 0;
            using (var p = Process.Start(psi)) // アプリの実行開始
            {
                try
                {
                    var stream = p.StandardOutput.BaseStream;
                    while (true)
                    {
                        int read;
                        if (backgroundWorker != null)
                        {
                            using (var readTask = stream.ReadAsync(buf, outputSize, buffSize - outputSize))
                            {
                                do
                                {
                                    if (backgroundWorker.CancellationPending)
                                    {
                                        p.Kill();
                                        return null;
                                    }
                                }
                                while (!readTask.Wait(FfmpegCancelCheckInterval));
                                read = readTask.Result;
                            }
                        }
                        else
                        {
                            read = stream.Read(buf, outputSize, buffSize - outputSize);
                        }
                        if (read > 0) outputSize += read;
                        else break;
                    }
                }
                catch
                {
                    p.Kill(); // using (var p ... ではこの効果はない
                    throw;
                }
            }
            return buf;
        }

        private static byte[] readStdOutBinary(string fileName, string arguments, Stream input, BackgroundWorker backgroundWorker, out int outputSize)
        {
            var psi = new ProcessStartInfo(fileName, arguments);
            psi.CreateNoWindow = true; // コンソール・ウィンドウを開かない
            psi.UseShellExecute = false; // シェル機能を使用しない
            psi.RedirectStandardOutput = true; // 標準出力をリダイレクト
            if(input != null)
            {
                psi.RedirectStandardInput = true;
            }
            const int subBufSize = 1024 * 1024;
            var subBuf = new byte[subBufSize];
            var buf = new byte[subBufSize];
            outputSize = 0;
            using (var p = Process.Start(psi)) // アプリの実行開始
            {
                try
                {
                    input?.CopyTo(p.StandardInput.BaseStream);
                    var stream = p.StandardOutput.BaseStream;
                    while (true)
                    {
                        int read;
                        if (backgroundWorker != null)
                        {
                            using (var readTask = stream.ReadAsync(subBuf, 0, subBufSize))
                            {
                                do
                                {
                                    if (backgroundWorker.CancellationPending)
                                    {
                                        p.Kill();
                                        return null;
                                    }
                                }
                                while (!readTask.Wait(FfmpegCancelCheckInterval));
                                read = readTask.Result;
                            }
                        }
                        else
                        {
                            read = stream.Read(subBuf, 0, subBufSize);
                        }
                        if (read > 0)
                        {
                            var nextOutputSize = outputSize + read;
                            if(subBuf.Length < nextOutputSize)
                            {
                                var nextBuf = new byte[nextOutputSize * 2];
                                Array.Copy(buf, nextBuf, outputSize);
                                buf = null;
                                buf = nextBuf;
                            }
                            Array.Copy(subBuf, 0, buf, outputSize, read);
                            outputSize = nextOutputSize;
                        }
                        else break;
                    }
                }
                catch
                {
                    p.Kill(); // using (var p ... ではこの効果はない
                    throw;
                }
            }
            return buf;
        }
#if SCREENCAPTCHA
        public static readonly string ffmpegPath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath)), "General\\ffmpeg.exe");
#else
        public static readonly string ffmpegPath = Path.Combine(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "ffmpeg.exe");
#endif
        private static readonly string[] supportedExtensions = new string[] {".mp4", ".wmv", ".mov", ".avi", ".mpg", ".mpeg", ".qt", ".asf", ".3gp", ".3gpp", ".3g2", ".rmvb", ".f4v", ".ts", ".mts", ".m2ts", ".m2v", ".m4v", ".vob", ".mkv", ".ogv", ".ogm", ".flv", ".webm", ".divx" };
        public static readonly string[] SupportedImageExtensionsWithPeriodInLowerCase = new string[] { ".webp", ".jp2", ".j2k", ".jpf", ".jpx", ".jpm", ".mj2" };
        //private static readonly Regex reSupports; // = new Regex(@"\.(mp4|wmv|mov|avi|mpe?g|3gp|f4v|m2ts|m2v|m4v|mkv|ogv|ogm|flv|webm)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static readonly string SupportedVideoFileFilter; // = "*.mp4;*.wmv;*.mov;*.avi;*.mpg;*.mpeg;*.3gp;*.f4v;*.m2ts;*.m2v;*.m4v;*.mkv;*.ogv;*.ogm;*.flv;*.webm";
        static MovieThumbnailLoader()
        {
            //reSupports = new Regex(@"\.(?:" + string.Join("|", supportedExtensions) + ")$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            SupportedVideoFileFilter = "*" + string.Join(";*", supportedExtensions);
        }

        public static bool ffmpegExists()
        {
            return File.Exists(ffmpegPath);
        }

        public static bool Supports(string path)
        {
            //return path != null && reSupports.IsMatch(path);
            try
            {
                return path != null && supportedExtensions.Contains(Path.GetExtension(path).ToLower());
            }
            catch
            {
                return false;
            }
        }
        
        public static bool SupportsByExtension(string extensionWithPeriodInLowerCase)
        {
            return extensionWithPeriodInLowerCase != null && supportedExtensions.Contains(extensionWithPeriodInLowerCase);
        }


    }

    public class MovieInfo
    {
        //private static readonly Regex sizePattern = new Regex(@"\bVideo\s*:[^:]*?([1-9]\d{0,9})x([1-9]\d{0,9})\b(?:\s*\[\s*(?:(?:SAR\s*(\d+):(\d+)|DAR\s*(\d+):(\d+)|[^\]]*)\s*)*\])?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex sizePattern = new Regex(@"\bVideo\s*:(?:[^:]|\d:)*?([1-9]\d{0,8})x([1-9]\d{0,8})", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex sarPattern = new Regex(@"\bVideo\s*:(?:[^:]|\d:)*?\b(?:SAR\s*(\d+):(\d+)|DAR\s*(\d+):(\d+))", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex fpsPattern = new Regex(@"\bVideo\s*:(?:(?:[^:]|\d:)*?(?:(\d+|\d+\.\d*|\d*\.\d+)\s*tbr|(\d+|\d+\.\d*|\d*\.\d+)\s*fps)\b)+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex duraPattern = new Regex(@"\bDuration\s*:\s*(\d\d):(\d\d):(\d\d)(?:\.(\d*))?\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        public static MovieInfo FromData(byte[] data)
        {
            using (var m = new MemoryStream(data))
            using (var r = new BinaryReader(m, Encoding.ASCII))
            {
                if (!r.ReadChars(5).SequenceEqual("MOVIE")) throw new Exception("Not movie data");
                return new MovieInfo() { SAR = new Fraction(r.ReadInt32(), r.ReadInt32()), SizeByPixel = new Size(r.ReadInt32(), r.ReadInt32()), FPS = r.ReadDouble(), FPSString = ReadString(r), Duration = new TimeSpan(r.ReadInt64()) };
            }
        }

        private static string ReadString(BinaryReader r)
        {
            var result = "";
            while(true) // EndOfStreamException が投げられるので無限ループはない
            {
                var c = r.ReadChar();
                if (c == '\0') return result;
                result += c;
            }
        }

        public byte[] ToData()
        {
            using (var m = new MemoryStream())
            using (var w = new BinaryWriter(m, Encoding.ASCII))
            {
                w.Write("MOVIE".ToArray());

                w.Write(SAR.Numerator);
                w.Write(SAR.Denominator);
                w.Write(SizeByPixel.Width);
                w.Write(SizeByPixel.Height);
                w.Write(FPS);
                w.Write(FPSString.ToArray());
                w.Write('\0');
                w.Write(Duration.Ticks);
                return m.ToArray();
            }
        }

        public static MovieInfo FromFile(string path)
        {
            var psi = new ProcessStartInfo(MovieThumbnailLoader.ffmpegPath, $"-i \"{path}\"");
            
            psi.CreateNoWindow = true; // コンソール・ウィンドウを開かない
            psi.UseShellExecute = false; // シェル機能を使用しない

            psi.RedirectStandardError = true; // 標準エラー出力をリダイレクト
            psi.StandardErrorEncoding = Encoding.UTF8;

            using (var p = Process.Start(psi)) // アプリの実行開始
            {
                try
                {
                    var output = p.StandardError.ReadToEnd(); // 標準出力の読み取り

                    var result = new MovieInfo();
                    var sizeMatch = sizePattern.Match(output);
                    if (sizeMatch.Success)
                    {
                        var g = sizeMatch.Groups;
                        result.SizeByPixel = new Size(int.Parse(g[1].Value), int.Parse(g[2].Value));
                    }
                    var sarMatch = sarPattern.Match(output);
                    if(sarMatch.Success)
                    {
                        int hori, vert;
                        var g = sarMatch.Groups;
                        if (int.TryParse(g[1].Value, out hori) && int.TryParse(g[2].Value, out vert)) result.SAR = (Fraction)hori / vert;
                        else if (int.TryParse(g[3].Value, out hori) && int.TryParse(g[4].Value, out vert)) result.SAR = (Fraction)(hori * result.SizeByPixel.Height) / (vert * result.SizeByPixel.Width);
                    }
                    var fpsMatch = fpsPattern.Match(output);
                    if(fpsMatch.Success)
                    {
                        double d;
                        if(double.TryParse(fpsMatch.Groups[1].Value, out d))
                        {
                            result.FPSString = fpsMatch.Groups[1].Value;
                            result.FPS = d;
                        }
                        else if(double.TryParse(fpsMatch.Groups[2].Value, out d))
                        {
                            result.FPSString = fpsMatch.Groups[2].Value;
                            result.FPS = d;
                        }
                    }
                    var duraMatch = duraPattern.Match(output);
                    if (duraMatch.Success)
                    {
                        var g = duraMatch.Groups;
                        var g4 = g[4].Value;
                        int miliSec;
                        if (string.IsNullOrEmpty(g4)) miliSec = 0;
                        else miliSec = int.Parse((g4 + "00").Substring(0, 3));
                        result.Duration = new TimeSpan(0, int.Parse(g[1].Value), int.Parse(g[2].Value), int.Parse(g[3].Value), int.Parse(g[4].Value));
                    }

                    return result;
                }
                catch
                {
                    p.Kill(); // using (var p ... ではこの効果はない
                    throw;
                }
            }
        }

        public static readonly string ToShortStringTemplate = "WQVGA / " + TimeSpan.Zero.ToString(@"mm\:ss");
        public string ToShortString()
        {
            if (Duration != default(TimeSpan))
            {
                if(SizeByPixel == default(Size))
                {
                    return Duration.ToString(@"hh\:mm\:ss");
                }
                else
                {
                    var ssn = GetSizeShortName(SizeByPixel);
                    if(string.IsNullOrEmpty(ssn))
                    {
                        return Duration.ToString(@"h\:mm\:ss");
                    }
                    else if(Duration < TimeSpan.FromHours(1))
                    {
                        //return Duration.ToString(@"mm\:ss") + $" / {ssn}";
                        return $"{ssn} / " + Duration.ToString(@"mm\:ss");
                    }
                    else
                    {
                        //return Duration.ToString(@"h\:mm" + $" / {ssn}");
                        return $"{ssn} / " + Duration.ToString(@"h\:mm");
                    }
                }
            }
            else if (SizeByPixel != default(Size))
            {
                return $"{SizeByPixel.Width}x{SizeByPixel.Height}";
            }
            else
            {
                return "";
            }
        }

        public string ToLongString()
        {
            var result = "";
            if(SizeByPixel != default(Size))
            {
                if (result != "") result += ", ";
                var ssn = GetSizeShortName(SizeByPixel);
                if(!(SizeByPixel.Width < 100 && SizeByPixel.Height < 100) && !string.IsNullOrEmpty(ssn))
                {
                    result += $"{ssn}({SizeByPixel.Width}x{SizeByPixel.Height})";
                }
                else
                {
                    result += $"{SizeByPixel.Width}x{SizeByPixel.Height}";
                }
            }
            if (SAR != 0)
            {
                if (result != "") result += ", ";
                result += $"SAR {SAR.Numerator}:{SAR.Denominator}";
            }
            if (FPS >= 0)
            {
                if (result != "") result += ", ";
                result += FPSString;
            }
            if (Duration != default(TimeSpan))
            {
                if (result != "") result += ", ";
                result += Duration.ToString(@"h\:mm\:ss\.") + Duration.Milliseconds;
            }
            return result;
        }

        private static readonly Tuple<Size, string>[] MajorSizeTable = new Tuple<Size, string>[]
        {
            Tuple.Create(new Size(320, 240), "QVGA"),
            Tuple.Create(new Size(480, 320), "HVGA"),
            Tuple.Create(new Size(640, 480), "VGA"),
            Tuple.Create(new Size(800, 600), "SVGA"),
            Tuple.Create(new Size(1024, 768), "XGA"),
            Tuple.Create(new Size(1280, 720), "HD"),
            Tuple.Create(new Size(1280, 720), "HD"),
            Tuple.Create(new Size(1600, 1200), "UXGA"),
            Tuple.Create(new Size(1920, 1080), "FHD"),
            Tuple.Create(new Size(2560, 1440), "WQHD"),
            Tuple.Create(new Size(3840, 2160), "4K"),
            Tuple.Create(new Size(8192, 4320), "8K"),
        };
        private string GetSizeShortName(Size size)
        {
            // http://pen64.com/document/resolution.html
            // https://ja.wikipedia.org/wiki/%E7%94%BB%E9%9D%A2%E8%A7%A3%E5%83%8F%E5%BA%A6
            // https://jp.gopro.com/support/articles/video-resolution-settings-and-format
            // http://www.quel.jp/etc/monitor-size/
            if (size.Width == 160 && size.Height == 144) return "GB";
            else if (size.Width == 240 && size.Height == 160) return "GBA";
            else if (size.Width == 256 && size.Height == 192) return "DS";
            else if (size.Width == 320 && size.Height == 240) return "QVGA";
            else if (size.Width == 400 && size.Height == 240) return "WQVGA";
            else if (size.Width == 480 && size.Height == 272) return "PSP";
            else if (size.Width == 352 && size.Height == 240) return "SIF";
            else if (size.Width == 352 && size.Height == 288) return "CIF";
            else if (size.Width == 480 && size.Height == 320) return "HVGA";
            else if (size.Width == 640 && size.Height == 480) return "VGA";
            else if (size.Width == 720 && size.Height == 480) return "SD";
            else if (size.Width == 720 && size.Height == 486) return "NTSC";
            else if (size.Width == 720 && size.Height == 540) return "NTSC"; // 同名
            else if (size.Width == 720 && size.Height == 576) return "PAL";
            else if (size.Width == 800 && size.Height == 480) return "WVGA";
            else if (size.Width == 848 && size.Height == 480) return "WVGA"; // 同名
            else if (size.Width == 854 && size.Height == 480) return "WiiU";
            else if (size.Width == 960 && size.Height == 544) return "Vita";
            else if (size.Width == 800 && size.Height == 600) return "SVGA";
            else if (size.Width == 940 && size.Height == 640) return "iPhn4";
            else if (size.Width == 1136 && size.Height == 640) return "iPhn5";
            else if (size.Width == 1024 && size.Height == 768) return "XGA";
            else if (size.Width == 1280 && size.Height == 720) return "HD";
            else if (size.Width == 1366 && size.Height == 768) return "FWXGA";
            else if (size.Width == 1024 && size.Height == 576) return "WSVGA";
            else if (size.Width == 1024 && size.Height == 600) return "WSVGA"; // 同名
            else if (size.Width == 1200 && size.Height == 600) return "WSVGA"; // 同名
            else if (size.Width == 1280 && size.Height == 800) return "WXGA";
            else if (size.Width == 1440 && size.Height == 900) return "WXGA+";
            else if (size.Width == 1440 && size.Height == 1080) return "DTTB";
            else if (size.Width == 1600 && size.Height == 1200) return "UXGA";
            else if (size.Width == 1920 && size.Height == 1080) return "FHD";
            else if (size.Width == 1920 && size.Height == 1200) return "WUXGA";
            else if (size.Width == 2560 && size.Height == 1440) return "WQHD";
            else if (size.Width == 2560 && size.Height == 1600) return "WQXGA";
            else if (size.Width == 3840 && size.Height == 2160) return "4K";
            else if (size.Width == 8192 && size.Height == 4320) return "8K";

            else if (size.Width == 8192 / 2 && size.Height == 2160) return "4K'";
            else if (size.Width == 3840 * 2 && size.Height == 4320) return "8K'";

            else if (size.Width < 100 && size.Height < 100) return $"{size.Width}x{size.Height}";

            else if (size.Width <= 320 && size.Height <= 320) return "Watch";

            else if (size.Width < 1000 && size.Width == size.Height) return $"{size.Width}^2";

            else if (size.Width >= 8192 * 3 / 2 && size.Height >= 4320 * 3 / 2) return $"{(int)Math.Round(Math.Sqrt((double)size.Width * size.Height) / (8192 * 4320 / 64))}K";

            else
            {
                double minLogDistance = double.PositiveInfinity;
                string nearestSize = "";
                foreach(var tpl in MajorSizeTable)
                {
                    var dw = Math.Log((double)tpl.Item1.Width / size.Width);
                    var dh = Math.Log((double)tpl.Item1.Height / size.Height);
                    var logDistance = dw * dw + dh * dh;
                    if(logDistance < minLogDistance)
                    {
                        minLogDistance = logDistance;
                        nearestSize = tpl.Item2;
                    }
                }
                return "~" + nearestSize;
            }
        }

        public Fraction SAR = 0;
        public Size SizeByPixel;
        public double FPS = -1;
        public string FPSString = "";
        public TimeSpan Duration;
    }

    public struct Fraction : IEquatable<Fraction>, IEquatable<int>, IComparable<Fraction>, IComparable<int>
    {
        private int numerator;
        public int Numerator { get { return numerator; } }
        private int denominator;
        public int Denominator { get { return denominator; } }

        public Fraction(int numerator, int denominator)
        {
            if (denominator == 0) throw new DivideByZeroException();
            this.numerator = numerator;
            this.denominator = denominator;
            positivate();
            reduction();
        }

        private void positivate()
        {
            if (denominator < 0)
            {
                numerator = -numerator;
                denominator = -denominator;
            }
        }

        private void reduction()
        {
            var n = numerator > 0 ? numerator : -numerator;
            var d = denominator;
            while (n > 0)
            {
                var temp = n;
                n = d % n;
                d = temp;
            }
            numerator /= d;
            denominator /= d;
        }

        public static implicit operator Fraction(int i)
        {
            return new Fraction { numerator = i, denominator = 1 };
        }

        public static explicit operator Fraction(Size size)
        {
            return new Fraction(size.Width, size.Height);
        }

        public static explicit operator double(Fraction f)
        {
            return (double)f.numerator / f.denominator;
        }

        public static explicit operator int(Fraction f)
        {
            return f.numerator / f.denominator;
        }


        public static implicit operator Size(Fraction f)
        {
            return new Size(f.numerator, f.denominator);
        }

        public static bool operator ==(Fraction f, Fraction g)
        {
            return f.numerator == g.numerator && f.denominator == g.denominator;
        }

        public static bool operator !=(Fraction f, Fraction g)
        {
            return f.numerator != g.numerator || f.denominator != g.denominator;
        }

        public static bool operator ==(Fraction f, int i)
        {
            return f.denominator == 1 && f.numerator == i;
        }

        public static bool operator !=(Fraction f, int i)
        {
            return f.denominator != 1 || f.numerator != i;
        }
        
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is int && this == (int)obj) return true;
            if (obj is Fraction && this == (Fraction)obj) return true;
            return false;
        }

        public override int GetHashCode()
        {
            return numerator ^ (denominator - 1); // 逆数が一致するのを避け、なおかつ恒等写像で実装された int のハッシュと整合性をとる
        }

        public override string ToString()
        {
            return denominator == 1 ? $"{numerator}" : $"{numerator}/{denominator}";
        }

        public bool Equals(Fraction other)
        {
            return other != null && other.numerator == numerator && other.denominator == denominator;
        }

        public int FloorOfPlusHalf()
        {
            if (numerator > 0)
            {
                return (numerator + (denominator >> 1)) / denominator;
            }
            else
            {
                return (numerator - ((denominator-1) >> 1)) / denominator;
            }
        }

        /*
        public static void RoundTest()
        {
            System.Windows.Forms.MessageBox.Show(
                $"{(new Fraction(1, 4)).FloorOfPlusHalf()} {(new Fraction(2, 4)).FloorOfPlusHalf()} {(new Fraction(3, 4)).FloorOfPlusHalf()},\n" +
                $"{(new Fraction(-1, 4)).FloorOfPlusHalf()} {(new Fraction(-2, 4)).FloorOfPlusHalf()} {(new Fraction(-3, 4)).FloorOfPlusHalf()},\n" +
                $"{(new Fraction(1, -4)).FloorOfPlusHalf()} {(new Fraction(2, -4)).FloorOfPlusHalf()} {(new Fraction(3, -4)).FloorOfPlusHalf()},\n" +
                $"{(new Fraction(-1, -4)).FloorOfPlusHalf()} {(new Fraction(-2, -4)).FloorOfPlusHalf()} {(new Fraction(-3, -4)).FloorOfPlusHalf()},\n" +
                $"{(new Fraction(1, 3)).FloorOfPlusHalf()} {(new Fraction(2, 3)).FloorOfPlusHalf()}\n" +
                $"{(new Fraction(-1, 3)).FloorOfPlusHalf()} {(new Fraction(-2, 3)).FloorOfPlusHalf()}\n" +
                $"{(new Fraction(1, -3)).FloorOfPlusHalf()} {(new Fraction(2, -3)).FloorOfPlusHalf()} ,\n" +
                $"{(new Fraction(-1, -3)).FloorOfPlusHalf()} {(new Fraction(-2, -3)).FloorOfPlusHalf()} {(new Fraction(0,1)).FloorOfPlusHalf()}\n"


                );
        }
        */

        public bool Equals(int other)
        {
            return denominator == 1 && numerator == other;
        }

        public static bool operator >(Fraction f, Fraction g)
        {
            return f.CompareTo(g) > 0;
        }

        public static bool operator <(Fraction f, Fraction g)
        {
            return f.CompareTo(g) < 0;
        }

        public int CompareTo(Fraction other)
        {
            if (other.numerator > 0)
            {
                return (this / other).CompareTo(1);
            }
            else
            {
                return -(this / other).CompareTo(1);
            }
        }

        public int CompareTo(int other)
        {
            return numerator.CompareTo(denominator * other);
        }

        public static Fraction operator /(Fraction f, int i)
        {
            if (i == 0) throw new DivideByZeroException();
            var temp = f.denominator;
            f.denominator = i;
            f.positivate();
            f.reduction();
            f.denominator *= temp;
            return f;
        }

        public static Fraction operator /(int i, Fraction f)
        {
            var temp = f.denominator;
            f.denominator = f.numerator;
            f.numerator = i;
            f.positivate();
            f.reduction();
            f.numerator *= temp;
            return f;
        }

        public static Fraction operator *(Fraction f, Fraction g)
        {
            var temp = f.denominator;
            f.denominator = g.denominator;
            g.denominator = temp;
            f.reduction();
            g.reduction();
            f.numerator *= g.numerator;
            f.denominator *= g.denominator;
            return f;
        }

        public static double operator /(double x, Fraction f)
        {
            return x * f.denominator / f.numerator;
        }
        

        public static Fraction operator /(Fraction f, Fraction g)
        {
            if (g.numerator == 0) throw new DivideByZeroException();
            var temp = f.denominator;
            var gNeg = g.numerator < 0;
            f.denominator = gNeg ? -g.numerator : g.numerator;
            g.numerator = gNeg ? -g.denominator : g.denominator;
            g.denominator = temp;
            f.reduction();
            g.reduction();
            f.numerator *= g.numerator;
            f.denominator *= g.denominator;
            return f;
        }
    }
}
