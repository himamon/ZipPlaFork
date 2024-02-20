using CSharpImageLibrary;
using LibAPNG;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Collections;

namespace ZipPla
{
    public static class ImageLoader
    {
        //private static readonly object psdLocker = new object();

        private static readonly string[] defaultSupportExtensions = new string[]
            { "bmp", "gif", "jpg", "jpeg", "png", "apng", "exf", "tif", "tiff", "ico", "emf", "wmf" };
        private static readonly string[] individualSupportExtensions = (new string[]
            { "psd", "dds", "tga" }).Concat(WindowsMediaPhoto.SupportExtensionsInLowerWithoutPeriod).ToArray();//, "xcf", "webp" };
        private static readonly string[] thumbnailSupportImageExtensions = new string[]
            { "pdn" };
        private static readonly string[] thumbnailSupportOtherExtensionsExpectTag = new string[]
            {  "txt", "ini", "html", "htm" }; //, "xml", "mht" };
        private static readonly string[] supportsFullReadingWithoutSusieFfmpeg;
        private static string[] supportsFullReadingWithoutFfmpeg;
        private static string supportsFullReadingStringWithoutFfmpeg;
        private static readonly string[] ffmpegSupportsWithoutPeriod;
        //private static string[] supportsGetImageInfoWithFfmpegWithPeriedInLower;
        private static string supportsFullReadingStringWithFfmpeg;
        private static readonly Regex defaultSupportPathRegex;//, supportsGetImageInfo;//, //supportsFullReadingPathRegex,
                                                               /*supportsImageAtLeastThumbnailReadingPathRegex, supportsAtLeastThumbnailReadingPathRegex, *///supportsGetImageInfo;
        private static string[] supportsImageAtLeastThumbnailReadingExtensionWithPeriodInLowerCase, supportsAtLeastThumbnailReadingExtensionWithPeriodInLowerCase;
        //public static readonly string SupportedFileFilter;
        private static ConcurrentDictionary<string, string> susieSupportsExtensionWithoutPeriodInLowerCaseToSpiName;
        static ImageLoader()
        {
            defaultSupportPathRegex = new Regex(@"\.(?:" + string.Join("|", defaultSupportExtensions) + ")$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            //var list = new List<string>(defaultSupportExtensions);
            //list.AddRange(individualSupportExtensions);
            //supportsFullReadingPathRegex = new Regex(@"\.(?:" + string.Join("|", list) + ")$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            supportsFullReadingWithoutSusieFfmpeg = defaultSupportExtensions.Concat(individualSupportExtensions).ToArray();

            ffmpegSupportsWithoutPeriod = (from withP in MovieThumbnailLoader.SupportedImageExtensionsWithPeriodInLowerCase select withP.Substring(1)).ToArray();

            //SusieReset();
        }

        private static bool allowUntested;
        //public static bool AllowUntestedPlugins { get { return allowUntestedPlugins; } }
        private static bool searchInstallationFolder;
        //public static bool SearchAlsoSusieInstallationFolder { get { return searchInstallationFolder; } }

        public static void SusieReset()
        {
            var config = new GeneralConfig();
            SusieReset(config.AllowUntestedPlugins, config.SearchAlsoSusieInstallationFolder);
        }
        public static void SusieResetIfSettingChanged(bool allowUntested, bool searchInstallationFolder)
        {
            if (ImageLoader.allowUntested != allowUntested || ImageLoader.searchInstallationFolder != searchInstallationFolder)
            {
                SusieReset(allowUntested, searchInstallationFolder);
            }
        }
        private static void SusieReset(bool allowUntested, bool searchInstallationFolder)
        {
            ImageLoader.allowUntested = allowUntested;
            ImageLoader.searchInstallationFolder = searchInstallationFolder;
            /*
            if (Environment.Is64BitProcess)
            {
                if (susieSupports == null) susieSupports = new string[0];
                else return;
            }
            else
            */
            {
                susieSupportsExtensionWithoutPeriodInLowerCaseToSpiName = GetSusieSupports(supportsFullReadingWithoutSusieFfmpeg, allowUntested, searchInstallationFolder);
            }
            var list = supportsFullReadingWithoutSusieFfmpeg.Concat(susieSupportsExtensionWithoutPeriodInLowerCaseToSpiName.Keys);
            
            supportsFullReadingWithoutFfmpeg = list.ToArray();
            supportsFullReadingStringWithoutFfmpeg = "*." + string.Join(";*.", supportsFullReadingWithoutFfmpeg);
            var onlyFfmpegSupports = ffmpegSupportsWithoutPeriod.Where(e => !list.Contains(e));
            //supportsFullReadingWithFfmpeg = supportsFullReadingWithoutFfmpeg.Concat(onlyFfmpegSupports).ToArray();
            supportsFullReadingStringWithFfmpeg = supportsFullReadingStringWithoutFfmpeg + ";*." + string.Join(";*.", onlyFfmpegSupports);
            //SupportedFileFilter = "*." + string.Join(";*.", list);

            var atLeastThumbnailList = new List<string>(list);
            atLeastThumbnailList.AddRange(thumbnailSupportImageExtensions);

            //supportsImageAtLeastThumbnailReadingPathRegex = new Regex(@"\.(?:" + string.Join("|", atLeastThumbnailList) + ")$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            supportsImageAtLeastThumbnailReadingExtensionWithPeriodInLowerCase = (from ext in atLeastThumbnailList select "." + ext).ToArray();
            //supportsGetImageInfoWithFfmpegWithPeriedInLower = ImageInfo.SupportExtensionsInLowerWithoutPeriod

            atLeastThumbnailList.AddRange(thumbnailSupportOtherExtensionsExpectTag);

            var tagFiles = from ext in TagLib.SupportedMimeType.AllExtensions
                           where !MovieThumbnailLoader.Supports("." + ext) && !PackedImageLoader.Supports("." + ext)
                           select ext.StartsWith(".") ? ext.Substring(1).ToLower() : ext.ToLower();
            atLeastThumbnailList.AddRange(tagFiles);

            //supportsAtLeastThumbnailReadingPathRegex = new Regex(@"\.(?:" + string.Join("|", atLeastThumbnailList) + ")$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            supportsAtLeastThumbnailReadingExtensionWithPeriodInLowerCase = (from ext in atLeastThumbnailList select "." + ext).ToArray();

            //list.AddRange(ImageInfo.SupportExtensionsInLowerWithoutPeriod);
            //supportsGetImageInfo = new Regex(@"\.(?:" + string.Join("|", list) + ")$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            
        }

        public static bool IndividualSupportFullReading(string path)
        {
            try
            {
                var ext = Path.GetExtension(path);
                if (string.IsNullOrEmpty(ext)) return false;
                ext = ext.Substring(1).ToLower();
                return defaultSupportExtensions.Contains(ext) || individualSupportExtensions.Contains(ext);
            }
            catch
            {
                return false;
            }
        }

        private static ConcurrentDictionary<string, string> GetSusieSupports(IEnumerable<string> notRequired, bool allowUntested, bool searchInstallationFolder)
        {
            var result = GetSusieSupports(allowUntested, searchInstallationFolder);
            foreach (var key in result.Keys.ToArray())
            {
                if (notRequired.Contains(key)) result.TryRemove(key, out var dummy);
            }
            return result;
        }

        private static ConcurrentDictionary<string, string> GetSusieSupports(bool allowUntested, bool searchInstallationFolder)
        {
            try
            {
                using (var susie = new garu.Util.Susie(allowUntested, searchInstallationFolder))
                {
                    return susie.GetExtensionToSpiPathDictionary();
                }
            }
            catch
            {
                return new ConcurrentDictionary<string, string>();
            }
        }

        public static bool IsLowLoad(string path)
        {
            try
            {
                var ext = Path.GetExtension(path).ToLower();
                switch(ext)
                {
                    case ".bmp":
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                    case ".ico":
                        return true;
                    default: return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static string GetSupportedFileFilter()
        {
            if(MovieThumbnailLoader.ffmpegExists())
            {
                return supportsFullReadingStringWithFfmpeg;
            }
            else
            {
                return supportsFullReadingStringWithoutFfmpeg;
            }
        }

        
        public static bool SupportsFullReading(string path, bool ffmpegExists)
        {
            var nullable = ffmpegExists as bool?;
            return SupportsFullReading(path, ref nullable, out var dummy);
        }
        

        public static bool SupportsFullReading(string path, ref bool? ffmpegExists)
        {
            return SupportsFullReading(path, ref ffmpegExists, out var dummy);
        }

        
        public static bool SupportsFullReading(string path, bool ffmpegExists, out bool requiredFfmpeg)
        {
            var nullable = ffmpegExists as bool?;
            return SupportsFullReading(path, ref nullable, out requiredFfmpeg);
        }
        

        public static bool SupportsFullReading(string path, ref bool? ffmpegExists, out bool requiredFfmpeg)
        {
            requiredFfmpeg = false;
            if (path == null) return false;
            try
            {
                var ext = Path.GetExtension(path);
                if (ext == "") return false;
                if (supportsFullReadingWithoutFfmpeg != null)
                {
                    if (supportsFullReadingWithoutFfmpeg.Contains(ext.Substring(1).ToLower())) return true;
                }
                else
                {
                    var ext2 = ext.Substring(1).ToLower();
                    if (supportsFullReadingWithoutSusieFfmpeg.Contains(ext2)) return true;
                    SusieReset();
                    if (supportsFullReadingWithoutFfmpeg.Contains(ext2)) return true;
                }
                
                requiredFfmpeg = MovieThumbnailLoader.SupportedImageExtensionsWithPeriodInLowerCase.Contains(ext);

                if (!requiredFfmpeg) return false;
                if (ffmpegExists == null) ffmpegExists = MovieThumbnailLoader.ffmpegExists();

                return (bool)ffmpegExists;
            }
            catch
            {
                return false;
            }
            //return path != null && supportsFullReadingPathRegex.IsMatch(path);
        }

        public static string GetExtensionWithPeriodInLowerSafety(string path)
        {
            if (path == null) return null;
            try
            {
                var extensionWithPeriodInLower = Path.GetExtension(path);
                if (string.IsNullOrEmpty(extensionWithPeriodInLower)) return null;
                return extensionWithPeriodInLower.ToLower();
            }
            catch
            {
                return null;
            }

        }
        
        public static bool SupportsGetImageInfo(string path, ref bool? ffmpegExists)
        {
            return SupportsGetImageInfoByExtensionWithPeriodInLower(GetExtensionWithPeriodInLowerSafety(path), ref ffmpegExists);
        }

        public static bool SupportsGetImageInfoByExtensionWithPeriodInLower(string extensionWithPeriodInLower, ref bool? ffmpegExists)
        {
            //return path != null && supportsGetImageInfo.IsMatch(path);

            if (extensionWithPeriodInLower == null) return false;

            if (supportsAtLeastThumbnailReadingExtensionWithPeriodInLowerCase.Contains(extensionWithPeriodInLower)) return true;

            var onlyFfmpeg = (from e in MovieThumbnailLoader.SupportedImageExtensionsWithPeriodInLowerCase
                              where !supportsAtLeastThumbnailReadingExtensionWithPeriodInLowerCase.Contains(e)
                              select e).ToArray();

            if (!onlyFfmpeg.Any()) return false;

            if (ffmpegExists == null) ffmpegExists = MovieThumbnailLoader.ffmpegExists();
            if ((bool)ffmpegExists) return onlyFfmpeg.Contains(extensionWithPeriodInLower);
            else return false;
        }


        /*
        public static bool SupportsAtLeastThumbnailReading(string path, bool? ffmpegExists)
        {
            //return path != null && supportsAtLeastThumbnailReadingPathRegex.IsMatch(path);
            if (path == null) return false;
            try
            {
                var ext = Path.GetExtension(path).ToLower();

                if (supportsAtLeastThumbnailReadingExtensionWithPeriodInLowerCase.Contains(ext)) return true;

                return MovieThumbnailLoader.SupportedImageExtensionsWithPeriodInLowerCase.Contains(ext)
                    && (ffmpegExists != null ? (bool)ffmpegExists : MovieThumbnailLoader.ffmpegExists());
            }
            catch
            {
                return false;
            }
        }
        */

        public static bool SupportsAtLeastThumbnailReading(string path, ref bool? ffmpegExists)
        {
            //return path != null && supportsAtLeastThumbnailReadingPathRegex.IsMatch(path);
            if (path == null) return false;
            try
            {
                var ext = Path.GetExtension(path).ToLower();

                if (supportsAtLeastThumbnailReadingExtensionWithPeriodInLowerCase.Contains(ext)) return true;

                return MovieThumbnailLoader.SupportedImageExtensionsWithPeriodInLowerCase.Contains(ext)
                    && (ffmpegExists ?? (bool)(ffmpegExists = MovieThumbnailLoader.ffmpegExists()));
            }
            catch
            {
                return false;
            }
        }

        public static bool SupportsImageAtLeastThumbnailReading(string path, bool ffmpegExists)
        {
            var nullable = ffmpegExists as bool?;
            return SupportsImageAtLeastThumbnailReading(path, ref nullable);
        }
        public static bool SupportsImageAtLeastThumbnailReading(string path, ref bool? ffmpegExists)
        {
            if (path == null) return false;
            //return path != null && supportsImageAtLeastThumbnailReadingPathRegex.IsMatch(path);
            try
            {
                var ext = Path.GetExtension(path).ToLower();

                if (supportsImageAtLeastThumbnailReadingExtensionWithPeriodInLowerCase.Contains(ext)) return true;

                if (!MovieThumbnailLoader.SupportedImageExtensionsWithPeriodInLowerCase.Contains(ext)) return false;

                if (ffmpegExists == null) ffmpegExists = MovieThumbnailLoader.ffmpegExists();

                return (bool)ffmpegExists;
                
            }
            catch
            {
                return false;
            }
        }

        public static bool SupportsImageAtLeastThumbnailReadingByExtension(string extensionWithPeriodInLowerCase, bool? ffmpegExists)
        {
            if (extensionWithPeriodInLowerCase == null) return false;
            if (supportsImageAtLeastThumbnailReadingExtensionWithPeriodInLowerCase.Contains(extensionWithPeriodInLowerCase)) return true;
            return MovieThumbnailLoader.SupportedImageExtensionsWithPeriodInLowerCase.Contains(extensionWithPeriodInLowerCase)
                && (ffmpegExists != null ? (bool)ffmpegExists : MovieThumbnailLoader.ffmpegExists());
        }

        public static bool SupportsImageAtLeastThumbnailReadingByExtension(string extensionWithPeriodInLowerCase, ref bool? ffmpegExists)
        {
            if (extensionWithPeriodInLowerCase == null) return false;
            if (supportsImageAtLeastThumbnailReadingExtensionWithPeriodInLowerCase.Contains(extensionWithPeriodInLowerCase)) return true;
            if (!MovieThumbnailLoader.SupportedImageExtensionsWithPeriodInLowerCase.Contains(extensionWithPeriodInLowerCase)) return false;
            if (ffmpegExists == null)
            {
                var exists = MovieThumbnailLoader.ffmpegExists();
                ffmpegExists = exists;
                return exists;
            }
            else
            {
                return (bool)ffmpegExists;
            }
        }

        private static DihedralFraction GetJpegOrientation(Stream stream)
        {
            var p = stream.Position;
            var tags = Exif.GetAll(stream);
            stream.Position = p;
            //stream.Seek(0, SeekOrigin.Begin);
            var value = tags?.FirstOrDefault(tag => tag.Tag == 0x0112)?.Value;
            return value == null ? new DihedralFraction() : ViewerFormImageFilter.GetOrientation((int)value);
        }

        private static Bitmap LoadRotateBitmap(Stream stream)
        {
            using (var seekable = SeekableStream.Seekablize(stream, long.MaxValue, leaveOpen: true))
            {
                var orientation = GetJpegOrientation(seekable);
                (seekable as SeekableStream)?.StopBuffering();
                return ViewerFormImageFilter.Rotate(new Bitmap(seekable), orientation);
            }
        }

        private static Bitmap GetFullBitmap(string filePath, bool setImageInfo, out ImageInfo imageInfo, bool animation)
        {
            return GetFullBitmap(filePath, setImageInfo, out imageInfo, out var dummy, animation);
        }

        private static bool IsJpegPath(string path)
        {
            if (path == null) return false;
            var length = path.Length;
            return length >= 4 && path.Substring(length - 4).ToLower() == ".jpg" || length >= 5 && path.Substring(length - 5).ToLower() == ".jpeg";
        }

        private static Bitmap GetFullBitmap(string filePath, bool setImageInfo, out ImageInfo imageInfo, out bool needToRefresh, bool animation)
        {
            if (defaultSupportPathRegex.IsMatch(filePath))
            {
                needToRefresh = false;
                // var sw = new System.Diagnostics.Stopwatch();
                //sw.Start();

                // 0.7 秒程度
                var ext = Path.GetExtension(filePath).ToLower();
                Bitmap img;
                if (ext == ".ico")
                {
                    img = new Bitmap(filePath);
                }
                else
                {
                    using (var s = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        if (animation && ext == ".gif")
                        {
                            img = GetGif(s);
                        }
                        else if (animation && (ext == ".png" || ext == ".apng"))
                        {
                            img = GetAPng(s);
                        }
                        else
                        {
                            img = IsJpegPath(filePath) ? LoadRotateBitmap(s) : new Bitmap(s);
                        }
                    }
                }

                if (setImageInfo) imageInfo = GetImageInfo(img);
                else imageInfo = null;
                return img;

                /*
                // 0.9 秒程度
                var result = BitmapResizer.Load(filePath);
                if (setImageInfo) imageInfo = GetImageInfo(result);
                else imageInfo = null;
                sw.Stop(); MessageBox.Show($"{sw.Elapsed}");
                return result;
                */


                // この方法だと Bitmap が Dispose されるまで filePath にロックが掛かる
                /*
                var img = new Bitmap(filePath);
                if (setImageInfo) imageInfo = GetImageInfo(img);
                else imageInfo = null;
                return img;
                */
            }
            else
            {
                var extension = Path.GetExtension(filePath).ToLower();
                switch(extension)
                {
                    case ".psd":
                        {
                            needToRefresh = false;
#if !AUTOBUILD
                            try
#endif
                            {
                                var psd = new System.Drawing.PSD.PsdFile();
                                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                                {
                                    //psd.Load(filePath); // このオーバーロードは内部で FileAccess.Read が指定されておらずエラーが出ることがある
                                    psd.Load(stream);
                                    imageInfo = new ImageInfo(psd.Columns, psd.Rows, psd.Depth * psd.ImageData.Length);

                                    //var result = System.Drawing.PSD.ImageDecoder.DecodeImage(psd);
                                    var result = RiosFastPsdDecoder.DecodeImage(psd);
                                    return result;
                                }
                            }
#if !AUTOBUILD
                            catch (Exception e)
                            {
                                MessageBox.Show($"psd error:\n\n{e.ToString()}");
                                throw;
                            }
#endif
                        }
                    case ".dds":
                    case ".tga":
                        needToRefresh = false;
                        using (var image = new ImageEngineImage(filePath))
                        {
                            var result = image.GetGDIBitmap(ignoreAlpha: false, mergeAlpha: false);
                            imageInfo = GetImageInfo(result);
                            return (Bitmap)result;
                        }
                    default:
                        var withoutPeriod = extension.Substring(1);
                        if (WindowsMediaPhoto.SupportExtensionsInLowerWithoutPeriod.Contains(withoutPeriod))
                        {
                            needToRefresh = false;
                            var result = WindowsMediaPhoto.Load(filePath);
                            imageInfo = GetImageInfo(result);
                            return result;
                        }
                        string spiPath;
                        if (susieSupportsExtensionWithoutPeriodInLowerCaseToSpiName.TryGetValue(withoutPeriod, out spiPath))
                        {
                            needToRefresh = true;
                            using (var susie = new garu.Util.Susie(spiPath))
                            {
                                var result = susie.GetPicture(filePath);
                                if (result != null)
                                {
                                    imageInfo = GetImageInfo(result);
                                    return (Bitmap)result;
                                }
                            }
                            if (withoutPeriod == "jp2" && MovieThumbnailLoader.ffmpegExists())
                            {
                                needToRefresh = false;
                                var result = MovieThumbnailLoader.GetImageByFfmpeg(filePath);
                                imageInfo = GetImageInfo(result);
                                return result;
                            }
                            else
                            {
                                throw new Exception();
                            }
                        }
                        if (MovieThumbnailLoader.SupportedImageExtensionsWithPeriodInLowerCase.Contains(extension))// && MovieThumbnailLoader.ffmpegExists())
                        {
                            needToRefresh = false;
                            var result = MovieThumbnailLoader.GetImageByFfmpeg(filePath);
                            imageInfo = GetImageInfo(result);
                            return result;
                        }
                        break;
                }
                throw new NotSupportedException();
            }
        }

        
        private static Bitmap GetFullBitmap(Stream stream, bool setImageInfo, out ImageInfo imageInfo, string filePathForGetFileType, bool animation)
        {
            if (filePathForGetFileType == null) throw new ArgumentNullException(nameof(filePathForGetFileType));
            if (defaultSupportPathRegex.IsMatch(filePathForGetFileType))
            {
                var ext = Path.GetExtension(filePathForGetFileType).ToLower();
                Bitmap img;
                if (animation && ext == ".gif")
                {
                    img = GetGif(stream);
                }
                else if (animation && (ext == ".png" || ext == ".apng"))
                {
                    img = GetAPng(stream);
                }
                else
                {
                    img = IsJpegPath(filePathForGetFileType) ? LoadRotateBitmap(stream) : new Bitmap(stream);
                }
                if (setImageInfo) imageInfo = GetImageInfo(img);
                else imageInfo = null;
                return img;
            }
            else
            {
                var extension = Path.GetExtension(filePathForGetFileType).ToLower();
                switch (extension)
                {
                    case ".psd":
                        {
                            var psd = new System.Drawing.PSD.PsdFile();
                            if (stream.CanSeek)
                            {
                                psd.Load(stream);
                                imageInfo = new ImageInfo(psd.Columns, psd.Rows, psd.Depth * psd.ImageData.Length);
                                return RiosFastPsdDecoder.DecodeImage(psd);
                            }
                            else
                            {
                                using (var ms = new MemoryStream())
                                {
                                    stream.CopyTo(ms);
                                    psd.Load(ms.ToArray());
                                    //psd.Load(ms);
                                    imageInfo = new ImageInfo(psd.Columns, psd.Rows, psd.Depth * psd.ImageData.Length);
                                    return RiosFastPsdDecoder.DecodeImage(psd);
                                }
                            }
                        }
                    case ".dds":
                    case ".tga":
                        if (stream.CanSeek)
                        {
                            using (var image = new ImageEngineImage(stream))
                            {
                                var result = image.GetGDIBitmap(ignoreAlpha: false, mergeAlpha: false);
                                imageInfo = GetImageInfo(result);
                                return (Bitmap)result;
                            }
                        }
                        else
                        {
                            using (var ms = new MemoryStream())
                            {
                                stream.CopyTo(ms);
                                using (var image = new ImageEngineImage(ms))
                                {
                                    var result = image.GetGDIBitmap(ignoreAlpha: false, mergeAlpha: false);
                                    imageInfo = GetImageInfo(result);
                                    return (Bitmap)result;
                                }
                            }
                        }
                    default:
                        var withoutPeriod = extension.Substring(1);
                        if (WindowsMediaPhoto.SupportExtensionsInLowerWithoutPeriod.Contains(withoutPeriod))
                        {
                            var result = WindowsMediaPhoto.Load(stream);
                            imageInfo = GetImageInfo(result);
                            return result;
                        }
                        string spiPath;
                        if (susieSupportsExtensionWithoutPeriodInLowerCaseToSpiName.TryGetValue(withoutPeriod, out spiPath))
                        {
                            using (var susie = new garu.Util.Susie(spiPath))
                            {
                                byte[] buf;
                                using (var ms = new MemoryStream())
                                {
                                    stream.CopyTo(ms);
                                    buf = ms.ToArray();
                                    var result = susie.GetPicture(filePathForGetFileType, buf);
                                    if (result != null)
                                    {
                                        imageInfo = GetImageInfo(result);
                                        return (Bitmap)result;
                                    }
                                    else if (withoutPeriod == "jp2" && MovieThumbnailLoader.ffmpegExists())
                                    {
                                        ms.Seek(0, SeekOrigin.Begin);
                                        var result2 = MovieThumbnailLoader.GetImageByFfmpeg(ms);
                                        imageInfo = GetImageInfo(result2);
                                        return result2;
                                    }
                                    else throw new Exception();
                                }
                            }
                        }

                        if (MovieThumbnailLoader.SupportedImageExtensionsWithPeriodInLowerCase.Contains(extension))// && MovieThumbnailLoader.ffmpegExists())
                        {
                            var result = MovieThumbnailLoader.GetImageByFfmpeg(stream);
                            imageInfo = GetImageInfo(result);
                            return result;
                        }


                        break;
                }
                throw new NotSupportedException();
            }
        }

        public static Bitmap GetFullBitmap(Stream stream, string filePathForGetFileType, bool animation = false)
        {
            return GetFullBitmap(stream, false, out var dummy, filePathForGetFileType, animation);
        }

        public static Bitmap GetFullBitmap(string filePath, bool animation = false)
        {
            return GetFullBitmap(filePath, false, out var dummy, animation);
        }

        //private static Bitmap getThumbnailBitmap(string filePath, bool setImageInfo, Size desiredSize, Func<Delegate, object> invoke, out ImageInfo imageInfo)
        private static Bitmap GetThumbnailBitmap(string filePath, bool setImageInfo, Size desiredSize, out ImageInfo imageInfo)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            switch (ext)
            {
                case ".pdn":
                    using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        if (setImageInfo)
                        {
                            var size = ImageInfo.GetPdnSizeAndSeekNext(fs);
                            imageInfo = new ImageInfo(size, 32);
                        }
                        else
                        {
                            ImageInfo.GetPdnSizeAndSeekNext(fs);
                            imageInfo = null;
                        }
                        var buf14 = new byte[14];
                        if (fs.Read(buf14, 0, 14) < 14) return null;
                        if (Encoding.ASCII.GetString(buf14).ToLower() != "<custom><thumb") return null;
                        var buf = new List<byte>();
                        var start = false;
                        var stop = false;
                        while (true)
                        {
                            var b = fs.ReadByte();
                            if (b < 0) return null;
                            if (b == 0x22)// '"'
                            {
                                if (start)
                                {
                                    stop = true;
                                    break;
                                }
                                else
                                {
                                    start = true;
                                }
                            }
                            else if (b == 0x3e) return null; // '>'
                            else if (start) buf.Add((byte)b);
                        }
                        if (!stop) return null;
                        var base64String = Encoding.ASCII.GetString(buf.ToArray());
                        buf = null;
                        using (var s = new MemoryStream(Convert.FromBase64String(base64String)))
                        {
                            return new Bitmap(s);
                        }
                    }
                case ".txt":
                case ".ini":
                    var text = GetText(filePath);
                    imageInfo = null;
                    if (text == null) return null;
                    var result = new Bitmap(desiredSize.Width, desiredSize.Height);
                    try
                    {
                        using (var g = Graphics.FromImage(result))
                        {
                            var font = SystemFonts.DefaultFont;
                            var rioSize = g.MeasureString("Rio", font);
                            DrawStringQuick(g, desiredSize, text, SystemFonts.DefaultFont, Color.Black, new PointF(rioSize.Width, rioSize.Height * 2));
                            //g.DrawString(text, SystemFonts.DefaultFont, Brushes.Black, new PointF(rioSize.Width, rioSize.Height * 2));
                        }
                        return result;
                    }
                    catch
                    {
                        result.Dispose();
                        throw;
                    }
                    
                case ".html":
                case ".htm":
                //case ".xml":
                //case ".mht":
                    // WebBrowser による実装
                    /*
                    if (invoke == null) throw new ArgumentNullException("invoke");
                    var textType = ext == ".xml";
                    var zoom = textType ? 1 : 2; // / Program.DisplayMagnification;
                    var page = GetWebPageImage(filePath, new Size(desiredSize.Width * zoom, desiredSize.Height * zoom), invoke);
                    imageInfo = null;
                    return page;
                    */

                    // HtmlRender による実装
                    const int zoom = 2; // / Program.DisplayMagnification;
                    var page = ConvertHtmlFromFileToImage(filePath, new SizeF(desiredSize.Width * zoom, desiredSize.Height * zoom),
                        stylesheetLoad: true, imageLoad: true);
                    imageInfo = null;
                    return page;
                default:
                    // 残りは TagLib で処理できる
                    imageInfo = null;
                    var file = TagLib.File.Create(filePath);
                    if (file == null) return null;
                    using (file)
                    {
                        var tag = file.Tag;
                        if (tag == null || tag.IsEmpty) return null;
                        var pictures = tag.Pictures;
                        if (pictures == null || pictures.Length <= 0) return null;
                        using (var s = new MemoryStream(pictures[0].Data.Data))
                        {
                            return new Bitmap(s);
                        }
                    }
            }
            throw new NotSupportedException();
        }

        private static void DrawStringQuick(Graphics g, Size canvasSize, string text, Font font, Color foreColor, PointF point)
        {
            var pointInt = new Point((int)Math.Round(point.X), (int)Math.Round(point.Y));
            var sb = new StringBuilder();
            var cursor = pointInt.Y;
            var height = canvasSize.Height;
            var flags = TextFormatFlags.NoPrefix | TextFormatFlags.Left | TextFormatFlags.Top;// | TextFormatFlags.ExpandTabs;
            using (var sr = new StringReader(text))
            {
                while (sr.Peek() >= 0 && cursor < height)
                {
                    var line = sr.ReadLine().Replace("\t", "    ");
                    cursor += TextRenderer.MeasureText(line, font, new Size(int.MaxValue, int.MinValue), flags).Height;
                    sb.AppendLine(line);
                }
                TextRenderer.DrawText(g, sb.ToString(), font, new Rectangle(pointInt.X, pointInt.Y, canvasSize.Width - pointInt.X, canvasSize.Height - pointInt.Y), foreColor, flags);
            }
        }

        //private static Bitmap getAtLeastThumbnailBitmap(string filePath, bool setImageInfo, Size desiredSize, Func<Delegate, object> invoke, out ImageInfo imageInfo)
        private static Bitmap GetAtLeastThumbnailBitmap(string filePath, bool setImageInfo, Size desiredSize, ref bool? ffmpegExists, out ImageInfo imageInfo, out bool needToRefresh)
        {
            if (SupportsFullReading(filePath, ref ffmpegExists, out var requiredFfmpeg))
            {
                return GetFullBitmap(filePath, setImageInfo, out imageInfo, out needToRefresh, animation: false);
            }
            else if (!requiredFfmpeg)
            {
                //return getThumbnailBitmap(filePath, setImageInfo, desiredSize, invoke, out imageInfo);
                needToRefresh = false;
                return GetThumbnailBitmap(filePath, setImageInfo, desiredSize, out imageInfo);
            }
            else throw new Exception();
        }

        /*
        public static Bitmap GetImageAtLeastThumbnailBitmap(string filePath, Size desiredSize, out ImageInfo imageInfo)
        {
            return GetAtLeastThumbnailBitmap(filePath, desiredSize, null, out imageInfo);
        }
        */

        //public static Bitmap GetAtLeastThumbnailBitmap(string filePath, Size desiredSize, Func<Delegate, object> invoke, out ImageInfo imageInfo)
        public static Bitmap GetAtLeastThumbnailBitmap(string filePath, Size desiredSize, bool ffmpegExists, out ImageInfo imageInfo)
        {
            var nullable = ffmpegExists as bool?;
            return GetAtLeastThumbnailBitmap(filePath, true, desiredSize, ref nullable, out imageInfo, out var dummy);
        }
        public static Bitmap GetAtLeastThumbnailBitmap(string filePath, Size desiredSize, ref bool? ffmpegExists, out ImageInfo imageInfo)
        {
            //return getAtLeastThumbnailBitmap(filePath, true, desiredSize, invoke, out imageInfo);
            return GetAtLeastThumbnailBitmap(filePath, true, desiredSize, ref ffmpegExists, out imageInfo, out var dummy);
        }

        public static Bitmap GetAtLeastThumbnailBitmap(string filePath, Size desiredSize, ref bool? ffmpegExists, out ImageInfo imageInfo, out bool needToRefresh)
        {
            //return getAtLeastThumbnailBitmap(filePath, true, desiredSize, invoke, out imageInfo);
            return GetAtLeastThumbnailBitmap(filePath, true, desiredSize, ref ffmpegExists, out imageInfo, out needToRefresh);
        }


        public static ImageInfo GetImageInfo(string filePath, bool colorDepthMustBeGot, ref bool? ffmpegExists)
        {
            if(ImageInfo.Supports(filePath))
            {
                var result = new ImageInfo(filePath);
                if (!colorDepthMustBeGot || result.BitPerPixel > 0) return result;

                if (!SupportsFullReading(filePath, ref ffmpegExists)) return result;
            }
            
            using (var img = GetFullBitmap(filePath, false, out var dummy, animation: false))
            {
                return GetImageInfo(img);
            }
        }

        public static ImageInfo GetImageInfo(Image img)
        {
            return new ImageInfo(img.Size, Image.GetPixelFormatSize(img.PixelFormat));
        }

        private static string GetText(string fileName)
        {
            return GetText(File.ReadAllBytes(fileName));
        }

        private static string GetText(byte[] data)
        {
            var encode = GetCode(data);
            if (encode != null)
            {
                int preambleLength = encode.GetPreamble().Length;
                return encode.GetString(data, preambleLength, data.Length - preambleLength);
            }
            else return null;
        }

        public static Encoding GetCode(byte[] data)
        {
            if (data.Length >= 2)
            {
                if ((data[0] == 0xfe) && (data[1] == 0xff))
                {
                    return new UnicodeEncoding(true, true);
                }
                if ((data[0] == 0xff) && (data[1] == 0xfe))
                {
                    if ((4 <= data.Length) &&
                        (data[2] == 0x00) && (data[3] == 0x00))
                    {
                        return new UTF32Encoding(false, true);
                    }
                    return new UnicodeEncoding(false, true);
                }
                if (data.Length >= 3)
                {
                    if ((data[0] == 0xef) && (data[1] == 0xbb) && (data[2] == 0xbf))
                    {
                        return new UTF8Encoding(true, true);
                    }
                    if (data.Length >= 4)
                    {
                        if ((data[0] == 0x00) && (data[1] == 0x00) &&
                            (data[2] == 0xfe) && (data[3] == 0xff))
                        {
                            return new UTF32Encoding(true, true);
                        }
                    }
                }

            }

            const byte bEscape = 0x1B;
            const byte bAt = 0x40;
            const byte bDollar = 0x24;
            const byte bAnd = 0x26;
            const byte bOpen = 0x28;
            const byte bB = 0x42;
            const byte bD = 0x44;
            const byte bJ = 0x4A;
            const byte bI = 0x49;
            int len = data.Length;
            byte b1, b2, b3, b4;
            bool isBinary = false;
            for (int i = 0; i < len; i++)
            {
                b1 = data[i];
                if (b1 <= 0x06 || b1 == 0x7F || b1 == 0xFF)
                {
                    isBinary = true;
                    if (b1 == 0x00 && i < len - 1 && data[i + 1] <= 0x7F)
                    {
                        return Encoding.Unicode;
                    }
                }
            }
            if (isBinary)
            {
                return null;
            }
            bool notJapanese = true;
            for (int i = 0; i < len; i++)
            {
                b1 = data[i];
                if (b1 == bEscape || 0x80 <= b1)
                {
                    notJapanese = false;
                    break;
                }
            }
            if (notJapanese)
            {
                return Encoding.ASCII;
            }
            for (int i = 0; i < len - 2; i++)
            {
                b1 = data[i];
                b2 = data[i + 1];
                b3 = data[i + 2];

                if (b1 == bEscape)
                {
                    if (b2 == bDollar && b3 == bAt)
                    {
                        return Encoding.GetEncoding(50220);
                    }
                    else if (b2 == bDollar && b3 == bB)
                    {
                        return Encoding.GetEncoding(50220);
                    }
                    else if (b2 == bOpen && (b3 == bB || b3 == bJ))
                    {
                        return Encoding.GetEncoding(50220);
                    }
                    else if (b2 == bOpen && b3 == bI)
                    {
                        return Encoding.GetEncoding(50220);
                    }
                    if (i < len - 3)
                    {
                        b4 = data[i + 3];
                        if (b2 == bDollar && b3 == bOpen && b4 == bD)
                        {
                            return Encoding.GetEncoding(50220);
                        }
                        if (i < len - 5 &&
                            b2 == bAnd && b3 == bAt && b4 == bEscape &&
                            data[i + 4] == bDollar && data[i + 5] == bB)
                        {
                            return Encoding.GetEncoding(50220);
                        }
                    }
                }
            }
            int sjis = 0;
            int euc = 0;
            int utf8 = 0;
            for (int i = 0; i < len - 1; i++)
            {
                b1 = data[i];
                b2 = data[i + 1];
                if (((0x81 <= b1 && b1 <= 0x9F) || (0xE0 <= b1 && b1 <= 0xFC)) &&
                    ((0x40 <= b2 && b2 <= 0x7E) || (0x80 <= b2 && b2 <= 0xFC)))
                {
                    sjis += 2;
                    i++;
                }
            }
            for (int i = 0; i < len - 1; i++)
            {
                b1 = data[i];
                b2 = data[i + 1];
                if (((0xA1 <= b1 && b1 <= 0xFE) && (0xA1 <= b2 && b2 <= 0xFE)) ||
                    (b1 == 0x8E && (0xA1 <= b2 && b2 <= 0xDF)))
                {
                    euc += 2;
                    i++;
                }
                else if (i < len - 2)
                {
                    b3 = data[i + 2];
                    if (b1 == 0x8F && (0xA1 <= b2 && b2 <= 0xFE) &&
                        (0xA1 <= b3 && b3 <= 0xFE))
                    {
                        euc += 3;
                        i += 2;
                    }
                }
            }
            for (int i = 0; i < len - 1; i++)
            {
                b1 = data[i];
                b2 = data[i + 1];
                if ((0xC0 <= b1 && b1 <= 0xDF) && (0x80 <= b2 && b2 <= 0xBF))
                {
                    utf8 += 2;
                    i++;
                }
                else if (i < len - 2)
                {
                    b3 = data[i + 2];
                    if ((0xE0 <= b1 && b1 <= 0xEF) && (0x80 <= b2 && b2 <= 0xBF) &&
                        (0x80 <= b3 && b3 <= 0xBF))
                    {
                        utf8 += 3;
                        i += 2;
                    }
                }
            }
            if (euc > sjis && euc > utf8)
            {
                return Encoding.GetEncoding(51932);
            }
            else if (sjis > euc && sjis > utf8)
            {
                return Encoding.GetEncoding(932);
            }
            else if (utf8 > euc && utf8 > sjis)
            {
                return Encoding.UTF8;
            }
            return null;
        }

        // WebBrowser を使う方法。パフォーマンスに問題があるので廃止
        /*
        public static Bitmap GetWebPageImage(string url, Size size, Func<Delegate, object> invoke)
        {
            const int completeCheckIntervalMilliseconds = 10;

            // ローカル前提
            if (!File.Exists(url)) throw new FileNotFoundException(null, url);
            int timeOutMilliseconds = 10 * 1000;
            
            Exception exception = null;
            Bitmap result = null;
            var th = new System.Threading.Thread(() =>
            {
                try
                {
                    using (var webBrowser = new WebBrowser())
                    {
                        webBrowser.ScrollBarsEnabled = false;

                        webBrowser.ScriptErrorsSuppressed = true;
                        // オンラインも考慮する場合、以下で別の方法が紹介されている
                        // http://d.hatena.ne.jp/chi-bd/20141210/1418220231

                        webBrowser.Width = size.Width;
                        webBrowser.Height = size.Height;
                        var completed = false;
                        webBrowser.DocumentCompleted += (sender, e) => completed = true;
                        webBrowser.Navigated += WebBrowser_Navigated;
                        webBrowser.Navigate(url);

                        do
                        {
                            System.Threading.Thread.Sleep(Math.Min(completeCheckIntervalMilliseconds, timeOutMilliseconds));
                            timeOutMilliseconds -= completeCheckIntervalMilliseconds;
                            if (timeOutMilliseconds <= 0)
                            {
                                throw exception != null ? exception : new TimeoutException();
                            }

                            Application.DoEvents();
                        }
                        while (!completed);

                            // Take Screenshot of the web pages full width + some padding
                            //webBrowser.Width = webBrowser.Document.Body.ScrollRectangle.Height;
                            // Take Screenshot of the web pages full height
                            //webBrowser.Height = webBrowser.Document.Body.ScrollRectangle.Height;

                            using (Graphics graphics = webBrowser.CreateGraphics())
                        {
                            Rectangle bounds = new Rectangle(0, 0, size.Width, size.Height);
                            result = new Bitmap(size.Width, size.Height, graphics);
                            try
                            {
                                webBrowser.DrawToBitmap(result, bounds);
                            }
                            catch
                            {
                                result.Dispose();
                                result = null;
                                throw;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }
            });
            th.SetApartmentState(System.Threading.ApartmentState.STA);
            th.Start();
            th.Join();

            if (exception != null) throw exception;

            return result;
        }

        [DllImport("urlmon.dll")]
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Error)]
        internal static extern int CoInternetSetFeatureEnabled(uint FeatureEntry, [MarshalAs(UnmanagedType.U4)] uint dwFlags, bool fEnable);
        private static void WebBrowser_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            const uint FEATURE_DISABLE_NAVIGATION_SOUNDS = 21;
            const uint SET_FEATURE_ON_PROCESS = 0x00000002;
            CoInternetSetFeatureEnabled(FEATURE_DISABLE_NAVIGATION_SOUNDS, SET_FEATURE_ON_PROCESS, true);
        }

        public static Bitmap GetWebPageImageo(string url, Size size, Func<Delegate, object> invoke)
        {
            const int completeCheckIntervalMilliseconds = 10;

            // ローカル前提
            if (!File.Exists(url)) throw new FileNotFoundException(null, url);
            int timeOutMilliseconds = 10 * 1000;

            WebBrowser webBrowser = null;

            

            var completed = false;
            Exception exception = null;
            invoke((MethodInvoker)(() =>
            {
                try
                {
                    webBrowser = new WebBrowser();
                    try
                    {
                        webBrowser.ScrollBarsEnabled = false;
                        webBrowser.ScriptErrorsSuppressed = true;
                        webBrowser.Width = size.Width;
                        webBrowser.Height = size.Height;
                        webBrowser.DocumentCompleted += (sender, e) => completed = true;
                        webBrowser.Navigate(url);
                    }
                    catch
                    {
                        webBrowser.Dispose();
                        webBrowser = null;
                        throw;
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }
            }));

            if (exception != null) throw exception;

            while (!completed)
            {
                System.Threading.Thread.Sleep(Math.Min(completeCheckIntervalMilliseconds, timeOutMilliseconds));
                timeOutMilliseconds -= completeCheckIntervalMilliseconds;
                if(timeOutMilliseconds <= 0)
                {
                    invoke((MethodInvoker)(() =>
                    {
                        try
                        {
                            webBrowser.Dispose();
                        }
                        catch (Exception e)
                        {
                            exception = e;
                        }
                    }));
                    throw exception != null ? exception : new TimeoutException();
                }
            }

            Bitmap result = null;
            invoke((MethodInvoker)(() =>
            {
                // Take Screenshot of the web pages full width + some padding
                //webBrowser.Width = webBrowser.Document.Body.ScrollRectangle.Height;
                // Take Screenshot of the web pages full height
                //webBrowser.Height = webBrowser.Document.Body.ScrollRectangle.Height;
                try
                {
                    using (Graphics graphics = webBrowser.CreateGraphics())
                    {
                        Rectangle bounds = new Rectangle(0, 0, size.Width, size.Height);
                        result = new Bitmap(size.Width, size.Height, graphics);
                        try
                        {
                            webBrowser.DrawToBitmap(result, bounds);
                        }
                        catch
                        {
                            result.Dispose();
                            result = null;
                            throw;
                        }
                    }
                }
                catch(Exception e)
                {
                    exception = e;
                }
                finally
                {
                    webBrowser.Dispose();
                }
            }));

            if (exception != null) throw exception;

            return result;
        }
        
        
        */
        
        private static Bitmap ConvertHtmlFromFileToImage(string fileName, SizeF size, bool stylesheetLoad, bool imageLoad)
        {
            var html = GetText(fileName);
            if (html == null) return null;
            Bitmap m_Bitmap = new Bitmap((int)Math.Round(size.Width), (int)Math.Round(size.Height));
            try
            {
                PointF point = new PointF(0, 0);
                //SizeF maxSize = size;
                //SizeF maxSize = new SizeF(float.MaxValue, size.Height);
                SizeF maxSize = SizeF.Empty;
                var fileUri = stylesheetLoad || imageLoad ? new Uri(fileName) : null;
                using (var g = Graphics.FromImage(m_Bitmap))
                {
                    TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.Render(g, html, point, maxSize,
                        stylesheetLoad: !stylesheetLoad ? null as EventHandler<TheArtOfDev.HtmlRenderer.Core.Entities.HtmlStylesheetLoadEventArgs>:
                        (sender, e) =>
                        {
                            try
                            {
                                var srcPath = e.Src;
                                if (string.IsNullOrEmpty(srcPath)) return;
                                srcPath = System.Net.WebUtility.UrlDecode(srcPath);
                                srcPath = new Uri(fileUri, srcPath).LocalPath;
                                if (File.Exists(srcPath))
                                {
                                    e.SetStyleSheetData = TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.ParseStyleSheet(GetText(srcPath));
                                }
                            }
                            catch { }
                        },
                        imageLoad: !imageLoad ? null as EventHandler<TheArtOfDev.HtmlRenderer.Core.Entities.HtmlImageLoadEventArgs>:
                        (sender, e) =>
                        {
                            try
                            {
                                var srcPath = e.Src;
                                if (string.IsNullOrEmpty(srcPath)) return;
                                srcPath = System.Net.WebUtility.UrlDecode(srcPath);
                                srcPath = new Uri(fileUri, srcPath).LocalPath;
                                if (File.Exists(srcPath))
                                {
                                    //e.SetStyleSheetData = TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.ParseStyleSheet(getText(srcPath));
                                    e.Handled = true;
                                    e.Callback(srcPath);
                                }

                            }
                            catch { }
                        }
                   );
                }
                return m_Bitmap;
            }
            catch
            {
                m_Bitmap.Dispose();
                throw;
            }
        }


        /*
        private static Bitmap ConvertHtmlFromStringToImage(string html, SizeF size)
        {
            Bitmap m_Bitmap = new Bitmap((int)Math.Round(size.Width), (int)Math.Round(size.Height));
            try
            {
                PointF point = new PointF(0, 0);
                SizeF maxSize = size;
                using (var g = Graphics.FromImage(m_Bitmap))
                {
                    //TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.Render(g, html, point, maxSize);
                    
                    maxSize.Width = float.MaxValue; TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.Render(g, html, point, maxSize, null, HtmlStylesheetLoad);
                }
                return m_Bitmap;
            }
            catch
            {
                m_Bitmap.Dispose();
                throw;
            }
        }
        */

        /*
    private static Uri HtmlStylesheetLoad_CurrentDirectory = null;
    private static void HtmlStylesheetLoad(object sender, TheArtOfDev.HtmlRenderer.Core.Entities.HtmlStylesheetLoadEventArgs e)
    {
        var cd = HtmlStylesheetLoad_CurrentDirectory;
        if (cd == null) return;
        try
        {
            var srcPath = e.Src;
            if (string.IsNullOrEmpty(srcPath)) return;
            srcPath = System.Net.WebUtility.UrlDecode(srcPath);
            srcPath = new Uri(cd, srcPath).LocalPath;
            if (File.Exists(srcPath))
            {
                e.SetStyleSheetData = TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.ParseStyleSheet(getText(srcPath));
            }
        }
        catch { }
    }
    */


        /*
        private static Bitmap ConvertHtmlFromFileToImageOld(string fileName, SizeF size)
        {
            var html = getText(fileName);
            if (html == null) return null;
            return ConvertHtmlFromStringToImage(html, size);
        }

        private static Bitmap ConvertHtmlFromStringToImage(string html, SizeF size)
        {
            Bitmap m_Bitmap = new Bitmap((int)Math.Round(size.Width), (int)Math.Round(size.Height));
            try
            {
                PointF point = new PointF(0, 0);
                SizeF maxSize = size;
                using (var g = Graphics.FromImage(m_Bitmap))
                {
                    //TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.Render(g, html, point, maxSize);
                    maxSize.Width = float.MaxValue;  TheArtOfDev.HtmlRenderer.WinForms.HtmlRender.Render(g, html, point, maxSize);
                }
                return m_Bitmap;
            }
            catch
            {
                m_Bitmap.Dispose();
                throw;
            }
        }
        */
        private static Bitmap GetAPng(Stream stream)
        {
            var result = APngLoader.GetAPng(stream);
            return result;
        }

        private static Bitmap GetGif(Stream stream)
        {
            //var tm = new TimeMeasure();
            //tm.Block("ana");
            MemoryStream ms;
            int frameCount;
            List<GifImageData> imageDatas;
            using (var seekable = SeekableStream.Seekablize(stream, long.MaxValue, leaveOpen: true))
            {
                var p = seekable.Position;
                GifFileData fileData;
                try
                {
                    fileData = GifAnalyzer.Analyz(seekable);
                }
                catch
                {
                    seekable.Position = p;
                    (seekable as SeekableStream)?.StopBuffering();
                    return new Bitmap(seekable);
                }
                seekable.Position = p;
                (seekable as SeekableStream)?.StopBuffering();
                if (fileData == null) return new Bitmap(seekable);
                imageDatas = fileData.ImageDatas;
                frameCount = imageDatas.Count;
                if (frameCount <= 1) return new Bitmap(seekable);
                //tm.Block("get");

                /*
                using (var image = Image.FromStream(seekable))
                {
                    var fd = new System.Drawing.Imaging.FrameDimension(image.FrameDimensionsList[0]);
                    if (image.GetFrameCount(fd) != frameCount) throw new Exception();
                    var width = fileData.iSWidth;
                    var height = fileData.iSHeight;
                    var frames = new Tuple<Bitmap, TimeSpan>[frameCount];
                    try
                    {
                        for (int i = 0; i < frameCount; i++)
                        {
                            image.SelectActiveFrame(fd, i);

                            // 単純な方法
                            //frames[i] = Tuple.Create(new Bitmap(image), TimeSpan.FromMilliseconds(imageDatas[i].iDelayTime * 10));

                            // 高速化した方法
                            frames[i] = Tuple.Create(CreateNewBitmap(image), TimeSpan.FromMilliseconds(imageDatas[i].iDelayTime * 10));
                        }
                    }
                    catch
                    {
                        for (int i = 0; i < frameCount; i++)
                        {
                            var frame = frames[i];
                            if (frame != null)
                            {
                                frame.Item1.Dispose();
                            }
                            else
                            {
                                break;
                            }
                        }
                        throw;
                    }
                    //tm.Dispose();
                    return new BitmapEx(frames);
                }
                */
                ms = new MemoryStream();
                try
                {
                    seekable.CopyTo(ms);
                }
                catch
                {
                    ms.Dispose();
                    throw;
                }
            }
            return new BitmapEx(new GifProvider(ms, frameCount), from d in imageDatas select TimeSpan.FromMilliseconds(d.iDelayTime * 10));
        }

        private class GifProvider : IEnumerator<Bitmap>
        {
            private Stream stream;
            private Image image;
            private System.Drawing.Imaging.FrameDimension frameDimension;
            private int frameCount;
            private int currentIndex;
            public GifProvider(Stream stream, int delayFrameCount)
            {
                this.stream = stream;
                try
                {
                    image = Image.FromStream(stream);
                    try
                    {
                        frameDimension = new System.Drawing.Imaging.FrameDimension(image.FrameDimensionsList[0]);
                        if (image.GetFrameCount(frameDimension) != delayFrameCount) throw new Exception();
                        frameCount = delayFrameCount;

                        currentIndex = -1;
                    }
                    catch
                    {
                        image.Dispose();
                        throw;
                    }
                }
                catch
                {
                    stream.Dispose();
                    throw;
                }
            }

            public Bitmap Current
            {
                get
                {
                    image.SelectActiveFrame(frameDimension, currentIndex);
                    return CreateNewBitmap(image, removeAlpha: true); // ここで 32 -> 24 にすることで遅延読み込みが無駄になることを防ぐ
                }
            }

            object IEnumerator.Current => Current as object;

            public void Dispose()
            {
                if (stream != null)
                {
                    stream.Dispose();
                    stream = null;
                    image.Dispose();
                }
            }

            public bool MoveNext()
            {
                return ++currentIndex < frameCount;
            }

            public void Reset()
            {
                currentIndex = 0;
            }
        }

        public static Bitmap CreateNewBitmap(Image image, bool removeAlpha)
        {
            var bitmap = image as System.Drawing.Bitmap;
            var isNewBitmap = bitmap == null;
            if (isNewBitmap) bitmap = new System.Drawing.Bitmap(image);
            if (removeAlpha)
            {
                var myBitmap = (Bitmap)bitmap;
                Bitmap changed;
                if (ViewerFormImageFilter.TryGetMaxColorDiff(myBitmap) == 0)
                {
                    changed = ViewerForm.ToBitmap8Unsafe(myBitmap, leaveOpenOriginal: true);
                }
                else
                {
                    changed = ViewerForm.ToBitmap24Unsafe(myBitmap, leaveOpenOriginal: true);
                }
                if (isNewBitmap)
                {
                    if (changed != myBitmap) myBitmap.Dispose();
                    return changed;
                }
                else if (changed != myBitmap)
                {
                    return changed;
                }
            }
            else if (isNewBitmap)
            {
                return (Bitmap)bitmap;
            }
            
            var pixelFormat = bitmap.PixelFormat;
            var result = new Bitmap(bitmap.Width, bitmap.Height, pixelFormat);
            try
            {
                if ((pixelFormat & System.Drawing.Imaging.PixelFormat.Indexed) == System.Drawing.Imaging.PixelFormat.Indexed)
                {
                    result.Palette = bitmap.Palette;
                }
                var d = result.LockBits(new Rectangle(0, 0, result.Width, result.Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, pixelFormat);
                try
                {
                    var dp = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, pixelFormat);
                    try
                    {
                        ViewerForm.CopyMemory(d.Scan0, dp.Scan0, d.Height * d.Stride);
                    }
                    finally
                    {
                        bitmap.UnlockBits(dp);
                    }
                }
                finally
                {
                    result.UnlockBits(d);
                }
            }
            catch
            {
                result.Dispose();
                throw;
            }
            return result;
        }
    }


    public class VirtualBitmapEx : IDisposable
    {
        public BitmapEx GetSourceForLock() { return source; }
        private readonly BitmapEx source;
        public readonly bool isEntity;
        public readonly int Width, Height;
        public readonly Size Size;
        public readonly long DataSizeInBytes;
        private readonly bool downScaleNearestNeighbor, upScaleNearestNeighbor;
        private VirtualBitmapEx(BitmapEx source, bool isEntity, long otherDataSize, Size size, bool downScaleNearestNeighbor, bool upScaleNearestNeighbor)
        {
            this.source = source;
            this.isEntity = isEntity;
            this.downScaleNearestNeighbor = downScaleNearestNeighbor;
            this.upScaleNearestNeighbor = upScaleNearestNeighbor;
            DataSizeInBytes = otherDataSize + source.GetDataSizeInBytes();
            FrameCount = source.FrameCount;
            Size = size;
            Width = size.Width;
            Height = size.Height;
        }
        public VirtualBitmapEx(BitmapEx source, long otherDataSize) : this(source, true, otherDataSize, source.Size, true, true) { }
        public VirtualBitmapEx(BitmapEx source, Size virtualSize, bool downScaleNearestNeighbor, bool upScaleNearestNeighbor) : this (source, false, 0, virtualSize, downScaleNearestNeighbor, upScaleNearestNeighbor) { }

        public static VirtualBitmapEx GetErrorImage(Size size)
        {
            return new VirtualBitmapEx(BitmapEx.ConvertToBitmapEx(Program.GetErrorImage(size.Width, size.Height)), 0);
        }

        public readonly int FrameCount;

        public System.Drawing.Imaging.PixelFormat PixelFormat { get { return source.PixelFormat; } }

        public void DrawFrame(System.Drawing.Imaging.BitmapData dst, int dstX, int dstY, DateTime now)
        {
            DrawFrame(dst, dstX, dstY, new Rectangle(0, 0, Width, Height), now, canUseParallel: true);
        }

        public void DrawFrame(System.Drawing.Imaging.BitmapData dst, int dstX, int dstY, Rectangle rect, DateTime now, bool canUseParallel)
        {
            var srcSize = source.Size;
            if (srcSize == Size)
            {
                if (rect.Width <= 0 || rect.Height <= 0) return;
                ViewerForm.lockBits(source.GetBitmap(source.GetCurrentIndex(now)), source, rect,
                    System.Drawing.Imaging.ImageLockMode.ReadOnly, "zoomed image (source, noscaling)", src =>
                    {
                        ViewerForm.DrawBitmapDataUnscaled(dst, dstX, dstY, src);
                    });
            }
            else
            {
                rect.Intersect(new Rectangle(rect.X - dstX, rect.Y - dstY, dst.Width, dst.Height));
                if (rect.Width <= 0 || rect.Height <= 0) return;
                var virtualWidth = (long)Size.Width;
                var virtualHeight = (long)Size.Height;
                var srcWidth = (long)srcSize.Width;
                var srcHeight = (long)srcSize.Height;
                var onSrcX = (int)(srcWidth * (2L * rect.X + 1) / (2L * virtualWidth));
                var onSrcY = (int)(srcHeight * (2L * rect.Y + 1) / (2L * virtualHeight));
                var onSrcWidth = (int)Math.Max(1, (srcWidth * rect.Width - virtualWidth / 2) / virtualWidth);
                var onSrcHeight = (int)Math.Max(1, (srcHeight * rect.Height - virtualHeight / 2) / virtualHeight);
                var onSrcRect = new Rectangle(onSrcX, onSrcY, onSrcWidth, onSrcHeight);

                onSrcRect.Intersect(new Rectangle(0, 0, srcSize.Width, srcSize.Height));
                var dstRect = new Rectangle(dstX, dstY, rect.Width, rect.Height);
                dstRect.Intersect(new Rectangle(0, 0, dst.Width, dst.Height));

                var scaleUp = dstRect.Width >= 2 * onSrcRect.Width && dstRect.Height >= 2 * onSrcRect.Width;


                // Parallel はスレッドの譲渡が発生するので Paint イベントが二重起動しないようにコントロールに特別な実装が必要
                // さもなければ並列化は使ってはいけない
                
                if (scaleUp && upScaleNearestNeighbor || !scaleUp && downScaleNearestNeighbor)
                {
                    ViewerForm.lockBits(source.GetBitmap(source.GetCurrentIndex(now)), source, onSrcRect,
                    System.Drawing.Imaging.ImageLockMode.ReadOnly, "zoomed image (source, nearest neightbor)", src =>
                    {
                        LongVectorImageResizer.NearestNeighbor(src, new Rectangle(0, 0, onSrcRect.Width, onSrcRect.Height), dst, dstRect, canUseParallel);
                    });
                }
                else
                {
                    ViewerForm.lockBits(source.GetBitmap(source.GetCurrentIndex(now)), source, onSrcRect,
                    System.Drawing.Imaging.ImageLockMode.ReadOnly, "zoomed image (source, quick graphic)", src =>
                    {
                        QuickGraphic.DrawImage(dst, dstRect, src, canUseParallel, backgroundWorker: null);
                    });
                }
            }
        }

        public void Start(DateTime dateTime) { source.Start(dateTime); }
        public DateTime GetNextChangeTime(DateTime dateTime) { return source.GetNextChangeTime(dateTime); }
        /*
        private DateTime startTime;
        public void Start(DateTime dateTime)
        {
            startTime = dateTime;
        }
        public int GetCurrentIndex(DateTime dateTime)
        {
            if (FrameCount == 1 || startTime == default(DateTime)) return 0;
            var subtract = (dateTime - startTime).Ticks % Period.Ticks;
            for (var i = 0; i < FrameCount; i++)
            {
                subtract -= Delays[i].Ticks;
                if (subtract < 0) return i;
            }
            return 0;
        }
        public DateTime GetNextChangeTime(DateTime dateTime)
        {
            if (FrameCount == 1 || startTime == default(DateTime)) return dateTime;
            var subtract = (dateTime - startTime).Ticks % Period.Ticks;
            for (var i = 0; i < FrameCount; i++)
            {
                subtract -= Delays[i].Ticks;
                if (subtract < 0) return dateTime + TimeSpan.FromTicks(-subtract);
            }
            return dateTime;
        }
        */

        private bool disposed = false;
        public void Dispose()
        {
            if (!disposed)
            {
                if (isEntity) source.Dispose();
                disposed = true;
            }
        }
    }
    public class BitmapEx : Bitmap
    {
        private readonly Bitmap[] bitmaps;
        public readonly TimeSpan[] Delays;
        public readonly TimeSpan Period;
        private IEnumerator<Bitmap> bitmapsPrivider;
        // http://qiita.com/razokulover/items/34962844e314bb4bfd04
        private static readonly TimeSpan DelayMinimum = TimeSpan.FromSeconds(1.0 / 150.0); // 144Hz に対応するように
        private static readonly TimeSpan DelayDefault = TimeSpan.FromSeconds(0.1);
        public BitmapEx(IEnumerator<Bitmap> bitmapsPrivider, IEnumerable<TimeSpan> delays) : base(null as System.Drawing.Bitmap)
        {
            
            // http://qiita.com/razokulover/items/34962844e314bb4bfd04
            Delays = (from delay in delays select delay >= DelayMinimum ? delay : DelayDefault).ToArray();

            FrameCount = Delays.Length;
            Period = TimeSpan.FromTicks((from d in Delays select d.Ticks).Sum());
            
            this.bitmapsPrivider = bitmapsPrivider;
            bitmaps = new Bitmap[FrameCount];
            ApplyList = new Queue<Func<Bitmap, Bitmap>>[FrameCount];
        }

        public static BitmapEx ConvertToBitmapEx(Bitmap bitmap) => bitmap as BitmapEx ??
                new BitmapEx((new Bitmap[1] { bitmap } as IEnumerable<Bitmap>).GetEnumerator(), new TimeSpan[1] { TimeSpan.Zero });

        public Bitmap GetBitmap(int index)
        {
            var result = bitmaps[index];
            if (result == null)
            {
                var firstNull = index - 1;
                while (firstNull >= 0 && bitmaps[firstNull] == null) firstNull--;
                firstNull++;
                var fc1 = FrameCount - 1;
                for (var i = firstNull; i <= index; i++)
                {
                    result = null;
                    try
                    {
                        bitmapsPrivider.MoveNext();
                        //var x = (new string[1] { "a" } as IEnumerable<string>).GetEnumerator().Current;
                        result = bitmapsPrivider.Current;
                    }
                    catch
                    {
                        if (bitmapsPrivider != null)
                        {
                            bitmapsPrivider.Dispose();
                            bitmapsPrivider = null;
                        }

                        //new Size(-210, -297);
                        int width, height;
                        if (i > 0)
                        {
                            var bitmap = bitmaps[0];
                            width = bitmap.Width;
                            height = bitmap.Height;
                        }
                        else
                        {
                            width = 210;
                            height = 297;
                        }

                        result?.Dispose();
                        result = Program.GetErrorImage(width, height);
                    }
                    bitmaps[i] = result;
                    if (i == fc1)
                    {
                        if (bitmapsPrivider != null)
                        {
                            bitmapsPrivider.Dispose();
                            bitmapsPrivider = null;
                        }
                    }
                }
            }

            var que = ApplyList[index];
            if (que != null)
            {
                var size = result;
                try
                {
                    while (que.Count > 0)
                    {
                        result = que.Dequeue()(result);
                    }
                }
#if DEBUG
                catch (Exception e)
                {
                    var message = e.ToString();
#else
                catch
                {
#endif
                    result.Dispose();
                    result = Program.GetErrorImage(size.Width, size.Height);
                }
                ApplyList[index] = null;
                bitmaps[index] = result;
            }

            return result;
        }

        protected override System.Drawing.Bitmap BaseBitmap { get => (System.Drawing.Bitmap)GetBitmap(0); set { } } // setter は base のコンストラクタでしか使わないので不要
        public readonly int FrameCount;

        private Queue<Func<Bitmap, Bitmap>>[] ApplyList;

        /// <summary>
        /// replacer が引数と異なるものを返す場合引数の dispose は呼び出し側で行うこと
        /// </summary>
        /// <param name="replacer"></param>
        public void ApplyToAll(Func<Bitmap, Bitmap> replacer)
        {
            for (var i = 0; i < FrameCount; i++)
            {
                var que = ApplyList[i] ?? new Queue<Func<Bitmap, Bitmap>>();
                que.Enqueue(replacer);
                ApplyList[i] = que;
                //bitmaps[i] = replacer(GetBitmap(i));
            }
            //BaseBitmap = (System.Drawing.Bitmap)(Image)GetBitmap(0);
        }

        /// <summary>
        /// CreateNew と Apply 開始時に false に設定される。その関数内で true にすると処理がキャンセルされる。CreateNew に限りキャンセル時はキャンセル前に作成されたインスタンスが Dispose される。
        /// </summary>
        public bool CancelApply;

        public long GetDataSizeInBytes()
        {
            // 遅延読み込みが無駄になるので廃止
            /*
            var result = 0L;
            for (var i = 0; i < FrameCount; i++)
            {
                var bmp = GetBitmap(i);
                var bytesPerPixel = Image.GetPixelFormatSize(bmp.PixelFormat) >> 3;
                var stride = (bmp.Width * bytesPerPixel + 3) & ~3L;
                result += stride * bmp.Height;
            }
            return result;
            */

            var bytesPerPixel = Image.GetPixelFormatSize(PixelFormat) >> 3;
            var stride = (Width * bytesPerPixel + 3) & ~3L;
            return stride * Height * FrameCount;
        }

        public static ulong GetDataSizeInBytes(BitmapEx bmpEx, Size newSize)
        {
            var result = 0UL;
            var newWidth = (ulong)newSize.Width;
            var newHeight = (ulong)newSize.Height;
            var fileCount = bmpEx.FrameCount;
            for (var i = 0; i < fileCount; i++)
            {
                var bmp = bmpEx.GetBitmap(i);
                var bytesPerPixel = (ulong)Image.GetPixelFormatSize(bmp.PixelFormat) >> 3;
                var stride = (newWidth * bytesPerPixel + 3) & ~3UL;
                result += stride * newHeight;
            }
            return result;
        }

        /// <summary>
        /// remaker は必ず新しいインスタンスを返すこと
        /// remaker が例外を投げた場合、それ以前に作られたインスタンスは自動的に破棄される
        /// </summary>
        /// <param name="remaker"></param>
        public BitmapEx CreateNew(Func<Bitmap, Bitmap> remaker)
        {
            var bitmaps = new Bitmap[FrameCount];
            //var delays = new TimeSpan[FrameCount];
            //var frames = new Tuple<Bitmap, TimeSpan>[FrameCount];
            try
            {
                CancelApply = false;
                for (var i = 0; i < FrameCount; i++)
                {
                    var oldBitmap = GetBitmap(i);
                    var newBitmap = remaker(oldBitmap);
                    if (CancelApply)
                    {
                        CancelApply = false;
                        newBitmap?.Dispose();
                        for (var j = 0; j < i; j++) bitmaps[j]?.Dispose();
                        return null;
                    }
                    if (newBitmap == oldBitmap) throw new Exception("remaker must create new instance.");
                    bitmaps[i] = newBitmap ?? throw new Exception("remaker must not return null.");
                }
            }
            catch
            {
                for (var i = 0; i < FrameCount; i++)
                {
                    bitmaps[i]?.Dispose();
                    //if (frame != null) frame.Item1.Dispose();
                }
                throw;
            }
            return new BitmapEx((bitmaps as IEnumerable<Bitmap>).GetEnumerator(), Delays);
        }

        private DateTime startTime;
        public void Start(DateTime dateTime)
        {
            startTime = dateTime;
        }
        public int GetCurrentIndex(DateTime dateTime)
        {
            if (FrameCount == 1 || startTime == default(DateTime)) return 0;
            var subtract = (dateTime - startTime).Ticks % Period.Ticks;
            for (var i = 0; i < FrameCount; i++)
            {
                subtract -= Delays[i].Ticks;
                if (subtract < 0) return i;
            }
            return 0;
        }
        public DateTime GetNextChangeTime(DateTime dateTime)
        {
            if (FrameCount == 1 || startTime == default(DateTime)) return dateTime;
            var subtract = (dateTime - startTime).Ticks % Period.Ticks;
            for (var i = 0; i < FrameCount; i++)
            {
                subtract -= Delays[i].Ticks;
                if (subtract < 0) return dateTime + TimeSpan.FromTicks(-subtract);
            }
            return dateTime;
        }

        private bool disposed = false;
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                base.Dispose(disposing);
                for (var i = 1; i < bitmaps.Length; i++)
                {
                    bitmaps[i]?.Dispose();
                }
                if (bitmapsPrivider != null)
                {
                    bitmapsPrivider.Dispose();
                    bitmapsPrivider = null;
                }
                disposed = true;
            }
        }
    }

    // シール型の System.Drawing.Bitmap を置き換える
    public class Bitmap : IDisposable, ICloneable
    {
        protected virtual System.Drawing.Bitmap BaseBitmap { get; set; }
        public Bitmap(System.Drawing.Bitmap systemBitmap)
        {
            BaseBitmap = systemBitmap;
        }

        public Bitmap(Image image)
        {
            BaseBitmap = new System.Drawing.Bitmap(image);
        }

        public Bitmap(Bitmap bitmap)
        {
            BaseBitmap = new System.Drawing.Bitmap(bitmap.BaseBitmap);
        }
        
        public Bitmap(Stream stream)
        {
            BaseBitmap = new System.Drawing.Bitmap(stream);
        }

        public Bitmap(string filename)
        {
            BaseBitmap = new System.Drawing.Bitmap(filename);
        }

        /*
        public Bitmap(Image image)
        {
            baseBitmap = new System.Drawing.Bitmap(image);
        }
        */

        public Bitmap(int width, int height)
        {
            BaseBitmap = new System.Drawing.Bitmap(width, height);
        }

        public Bitmap(int width, int height, System.Drawing.Imaging.PixelFormat format)
        {
            BaseBitmap = new System.Drawing.Bitmap(width, height, format);
        }

        public Bitmap(int width, int height, int stride, System.Drawing.Imaging.PixelFormat format, IntPtr scan0)
        {
            BaseBitmap = new System.Drawing.Bitmap(width, height, stride, format, scan0);
        }
        
        public static implicit operator System.Drawing.Image(Bitmap bitmap)
        {
            return bitmap.BaseBitmap;
        }
        
        public static explicit operator Bitmap(System.Drawing.Bitmap systemBitmap)
        {
            return new Bitmap(systemBitmap);
        }
        
        public object Clone()
        {
            return new Bitmap(BaseBitmap.Clone() as System.Drawing.Bitmap);
        }

        public int Width { get { return BaseBitmap.Width; } }
        public int Height { get { return BaseBitmap.Height; } }
        public Size Size { get { return BaseBitmap.Size; } }
        public System.Drawing.Imaging.PixelFormat PixelFormat { get { return BaseBitmap.PixelFormat; } }
        public System.Drawing.Imaging.ColorPalette Palette { get { return BaseBitmap.Palette; } set { BaseBitmap.Palette = value; } }
        public float HorizontalResolution { get { return BaseBitmap.HorizontalResolution; } }
        public float VerticalResolution { get { return BaseBitmap.VerticalResolution; } }
        public System.Drawing.Imaging.BitmapData LockBits(Rectangle rect, System.Drawing.Imaging.ImageLockMode flags, System.Drawing.Imaging.PixelFormat format)
        {
            return BaseBitmap.LockBits(rect, flags, format);
        }
        public System.Drawing.Imaging.BitmapData LockBits(Rectangle rect, System.Drawing.Imaging.ImageLockMode flags, System.Drawing.Imaging.PixelFormat format, System.Drawing.Imaging.BitmapData bitmapData)
        {
            return BaseBitmap.LockBits(rect, flags, format, bitmapData);
        }
        public void UnlockBits(System.Drawing.Imaging.BitmapData bitmapdata)
        {
            BaseBitmap.UnlockBits(bitmapdata);
        }
        public Color GetPixel(int x, int y)
        {
            return BaseBitmap.GetPixel(x, y);
        }
        public void SetPixel(int x, int y, Color color)
        {
            BaseBitmap.SetPixel(x, y, color);
        }
        public void Save(string filename)
        {
            BaseBitmap.Save(filename);
        }
        public void Save(string filename, System.Drawing.Imaging.ImageFormat format)
        {
            BaseBitmap.Save(filename, format);
        }
        public void Save(Stream stream, System.Drawing.Imaging.ImageFormat format)
        {
            BaseBitmap.Save(stream, format);
        }
        public IntPtr GetHbitmap()
        {
            return BaseBitmap.GetHbitmap();
        }
        public void SetResolution(float xDpi, float yDpi)
        {
            BaseBitmap.SetResolution(xDpi, yDpi);
        }

#region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
                    BaseBitmap.Dispose();
                }

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~Bitmap() {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }

#endregion
    }
    
}

