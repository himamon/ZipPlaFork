using iTextSharp.text.pdf;
using Microsoft.Deployment.Compression.Cab;
using SevenZipExtractor;
using SharpCompress.Archives;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ZipPla
{
    public class PasswordLockedException : Exception { }

    public enum PackedImageLoaderSearchMode { UntilFoundItem, UntilFoundLayer, Full}

    public enum PackedImageLoaderFileInfo { LastWriteTime = 1, Size = 2, All = LastWriteTime | Size }
    public enum PackedImageLoaderOnMemoryMode { None, OnMemory, Releasable }
    class PackedImageLoader : IDisposable
    {
        //const long SolidSizeBound = 128 * (1 << 20); // 書庫ファイルサイズ自体ではなく読み取りサイズの上限

        public enum PackType { Directory, Zip, ZipM, Rar, Rar5, Cab, Tar, Pdf, SevenZip, Lha, Unknown, Gz } // Gz はアーカイブ機能がないので対応させない
        public PackType Type { get; private set; }
        public readonly bool IsSupportedSingleFile;
        public PackedImageLoaderOnMemoryMode OnMemoryMode => stream is ReleasableStream releasableStream ?
            releasableStream.IsReleased ? PackedImageLoaderOnMemoryMode.OnMemory : PackedImageLoaderOnMemoryMode.Releasable : PackedImageLoaderOnMemoryMode.None;
        public void Release()
        {
            if (stream is ReleasableStream releasableStream && !releasableStream.IsReleased)
            {
                Task.Run((Action)releasableStream.Release);
            }
        }

        public static string GetSupportedArchiveFileFilter()
        {
            if (sevenZipExists)
            {
                return "*.zip;*.rar;*.pdf;*.7z;*.cab;*.tar;*.cbz;*.cbr;*.epub;*.lzh";
            }
            else
            {
                return "*.zip;*.rar;*.pdf;*.7z;*.cab;*.tar;*.cbz;*.cbr;*.epub";
            }
        }

        public static bool Supports(string path, bool forFile = true)
        {
            try
            {
                return path != null && Supports(path, Path.GetExtension(path).ToLower(), forFile);
            }
            catch
            {
                return false;
            }
        }

        private const bool SharpCompressSupportsMultiVolumeZip = false;

        public static bool Supports(string path, string extensionWithPeriodInLowerCase, bool forFile = true)
        {
            switch (extensionWithPeriodInLowerCase)
            {
                case ".zip":
                case ".rar":
                case ".7z":
                case ".cab":
                case ".tar":
                case ".cbz":
                case ".cbr":
                case ".epub":
                    switch (GetPartInfo(path, path.Length - extensionWithPeriodInLowerCase.Length))
                    {
                        case PartInfo.None: return true;
                        case PartInfo.NotFirst: return false;
                        case PartInfo.First: return forFile && supportsMultiVolume(extensionWithPeriodInLowerCase);
                    }
                    return false;

                case ".lzh":
                    return sevenZipExists;

                case ".pdf":
                    return true;

                default:
                    return false;
            }
        }

        private enum PartInfo { None, First, NotFirst }
        private static PartInfo GetPartInfo(string name, int baseNameLength)
        {
            var i = baseNameLength - 1;

            // 数値を取得
            var digit = false;
            var first = false;
            char c;
            while (i >= 4)
            {
                c = name[i];
                if ('0' <= c && c <= '9')
                {
                    if (digit)
                    {
                        if (first) first = c == '0';
                    }
                    else
                    {
                        digit = true;
                        first = c == '1';
                    }
                    i--;
                }
                else break;
            }
            if (!digit || i < 4) return PartInfo.None;

            c = name[i - 0]; if (c != 't' && c != 'T') return PartInfo.None;
            c = name[i - 1]; if (c != 'r' && c != 'R') return PartInfo.None;
            c = name[i - 2]; if (c != 'a' && c != 'A') return PartInfo.None;
            c = name[i - 3]; if (c != 'p' && c != 'P') return PartInfo.None;
            c = name[i - 4]; if (c != '.') return PartInfo.None;

            return first ? PartInfo.First : PartInfo.NotFirst;
        }

        private static bool supportsMultiVolume(string extensionWithPeriodInLowerCase)
        {
            if (SharpCompressSupportsMultiVolumeZip || sevenZipExists) return true;
            var c1 = extensionWithPeriodInLowerCase[1];
            return c1 != 'z' && c1 != 'e' && (c1 != 'c' || extensionWithPeriodInLowerCase[3] != 'z'); // ".7z" は c1 != 'c' を満たすので IndexOutOfRangeException は投げられない
        }

        //private readonly static string sevenZipPath_Installed = Path.Combine(Environment.GetFolderPath(
        //    !Environment.Is64BitProcess && Environment.Is64BitOperatingSystem ? Environment.SpecialFolder.ProgramFilesX86 : Environment.SpecialFolder.ProgramFiles), "7-Zip", "7z.dll");
        private readonly static string sevenZipPath_Local_xXX = Path.Combine(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), Environment.Is64BitProcess ? "7z-x64.dll" : "7z-x86.dll");
        private readonly static string sevenZipPath_Local = Path.Combine(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "7z.dll");
        //private readonly static string[] sevenZipPath_RegPaths = new string[] { @"HKEY_CURRENT_USER\Software\7-Zip" };
        private readonly static string[] sevenZipPath_RegPaths = new string[] { @"HKEY_CURRENT_USER\Software\7-Zip", @"HKEY_LOCAL_MACHINE\Software\7-Zip" };
        //private readonly static string sevenZipPath_Name = Environment.Is64BitProcess ? Environment.Is64BitOperatingSystem ? "Path" : "Path64" : Environment.Is64BitOperatingSystem ? "Path32" : "Path";
        private readonly static string sevenZipPath_Name = Environment.Is64BitProcess ? "Path64" : "Path32";
        public static string GetSevenZipPath()
        {
            bool dummy;
            return GetSevenZipPath(out dummy);
        }
        public static string GetSevenZipPath(out bool gotPathFromRegistry)
        {
            string result;
            try
            {
                if (File.Exists(sevenZipPath_Local_xXX))
                {
                    result = sevenZipPath_Local_xXX;
                    gotPathFromRegistry = false;
                }
                else if (File.Exists(sevenZipPath_Local))
                {
                    result = sevenZipPath_Local;
                    gotPathFromRegistry = false;
                }
                else
                {
                    result = null;
                    gotPathFromRegistry = false;
                    foreach (var path in sevenZipPath_RegPaths)
                    {
                        result = ZipPlaAddressBar.RegistryGetValueWithoutException(path, sevenZipPath_Name, null as string);
                        if (result != null)
                        {
                            // レジストリに無意味なデータがある可能性を想定
                            try
                            {
                                result = Path.Combine(result, "7z.dll");
                                if (File.Exists(result))
                                {
                                    gotPathFromRegistry = true;
                                    break;
                                }
                                else result = null;
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            catch
            {
                result = null;
                gotPathFromRegistry = false;
            }
            sevenZipExists = result != null;
            return result;
        }
        public static void CheckSevenZipExistence(System.Windows.Forms.Form owner)
        {
            bool gotPathFromRegistry;
            var path = GetSevenZipPath(out gotPathFromRegistry);
            //if (path != null && !gotPathFromRegistry)
            if (path != null) // 安全性をとってこちらを採用
                {
                try
                {
                    using (new SevenZipHandle(path))
                    {
                    }
                }
                catch
                {
                    string a, b;
                    if (Environment.Is64BitProcess)
                    {
                        a = "x86";
                        b = "x64";
                    }
                    else
                    {
                        a = "x64";
                        b = "x86";
                    }

                    var message =
                        Message.FollowingDllCouldNotBeLoaded + "\n" +
                        Message.ConfirmItIsNotFor1But2.Replace("$1", a).Replace("$2", b) + "\n" +
                        "\n" +
                        path;

                    System.Windows.Forms.MessageBox.Show(owner, message, null,
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Error);
                }
            }
        }

        private static bool sevenZipExists;

        private string path;
        private Stream stream;
        private string[] dirEntries;
        private ZipArchive zipArchive;
        private ZipArchiveEntry[] zipArchiveEntries;
        private IArchive iArchive;
        private IArchiveEntry[] iArchiveEntries;
        private ArchiveFile sevenZipArchive;
        private Entry[] sevenZipArchiveEntries;
        //private CabInfo cabArchive;
        //private IList<CabFileInfo> cabArchiveEntries;
        private CabEngine cabArchive;
        private IList<Microsoft.Deployment.Compression.ArchiveFileInfo> cabArchiveEntries;
        private List<Bitmap> entriesImageList;
        private IReader iReader;
        private List<Stream> entriesStreamListForImage;
        private List<Stream> entriesStreamList;
        private IReader iReaderForStream;
        private PdfReader pdfArchive;
        private bool pdfImageMustBeConvert = false;

        private int leftHierarchies;
        private PackedImageLoaderSearchMode searchMode;
        private Tuple<PackedImageLoader, List<PackedImageEntry>>[] children;

        bool animation = false;

        /// <summary>
        /// エントリー取得時にセットされる。HasAnyEntry では取得されない
        /// </summary>
        public int LoadedDepth { get; private set; } = 0;

        //public PackedImageLoader(string path) : this(path, 0, false) { }

        public PackedImageLoader(string path, int leftHierarchies, PackedImageLoaderSearchMode searchMode, 
            bool animation = false, PackedImageLoaderOnMemoryMode onMemory = PackedImageLoaderOnMemoryMode.None)
        {
            if (path != null && path.Length > 0 && path.Last() == ':') path += Path.DirectorySeparatorChar;
            this.path = path;
            this.leftHierarchies = leftHierarchies;
            this.searchMode = searchMode;
            if (Directory.Exists(path))
            {
                Type = PackType.Directory;
            }
            else if (File.Exists(path))
            {
                IsSupportedSingleFile = Supports(path, forFile: false);
                if (!IsSupportedSingleFile) onMemory = PackedImageLoaderOnMemoryMode.None;
                switch (onMemory)
                {
                    case PackedImageLoaderOnMemoryMode.OnMemory:
                        stream = new MemoryStream();
                        using (var fs = File.OpenRead(path)) fs.CopyTo(stream);
                        stream.Seek(0, SeekOrigin.Begin);
                        break;
                    case PackedImageLoaderOnMemoryMode.Releasable:
                        stream = new ReleasableStream(File.OpenRead(path));
                        break;
                    default:
                        stream = File.OpenRead(path);
                        break;
                }
                setType();
                if (!IsSupportedSingleFile)
                {
                    stream.Close();
                    stream.Dispose();
                    stream = null;

                    if (!SharpCompressSupportsMultiVolumeZip && Type == PackType.Zip)
                    {
                        Type = PackType.ZipM;
                    }
                }
            }
            else
            {
                throw new FileNotFoundException(null, path);
            }
            this.animation = animation;
        }
        
        /// <summary>
        /// pathToCheckType はそれが TAR かどうかの確認にのみ用いる。渡された stream は Dispose まで PackedImageLoder が管理する
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="pathToCheckType"></param>
        private PackedImageLoader(Stream stream, long streamLength, string pathToCheckType, int leftHierarchies, PackedImageLoaderSearchMode searchMode, bool animation)
        {
            this.stream = SeekableStream.Seekablize(stream, streamLength);
            path = pathToCheckType;
            this.leftHierarchies = leftHierarchies;
            this.searchMode = searchMode;
            setType();
            this.animation = animation;
        }

        /// <summary>
        /// 失敗したら stream を dispose
        /// </summary>
        private void setType()
        {
            // TAR は仕様上ファイルの先頭からファイル形式を確定することができない
            try
            {
                if (Path.GetExtension(path).ToLower() == ".tar")
                {
                    Type = PackType.Tar;
                }
                else
                {
                    Type = GetTypeFromStreamAndSeekFirst(stream);
                }
                if (Type >= PackType.Unknown)
                {
                    throw new NotSupportedException();
                }
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        public class PackedImageEntry
        {
            public int ChildIndex { get; private set; }
            public int Index { get; private set; }
            public string Path { get; private set; }
            public DateTime LastWriteTime { get; private set; }
            public long Size { get; private set; }

            public PackedImageEntry(int childIndex, int index, string path)
            {
                ChildIndex = childIndex;
                Index = index;
                Path = path;
                LastWriteTime = DateTime.MinValue;
                Size = -1;
            }

            public PackedImageEntry(int childIndex, int index, string path, DateTime lastWriteTime, long size)
            {
                ChildIndex = childIndex;
                Index = index;
                Path = path;
                LastWriteTime = lastWriteTime;
                Size = size;
            }
        }

        public static string[] GetSeparatorReplacedArray(List<PackedImageEntry> list, PackType type)
        {
            var count = list.Count;
            var replaced = new string[count];
            char s, r;
            if (type == PackType.Directory)
            {
                s = Path.DirectorySeparatorChar;
                r = Convert.ToChar(1);
            }
            else
            {
                s = Path.AltDirectorySeparatorChar;
                r = Convert.ToChar(2);
            }
            for (var i = 0; i < count; i++)
            {
                var name = list[i].Path;
                if (name.Contains(s))
                {
                    replaced[i] = name.Replace(s, r);
                }
                else
                {
                    replaced[i] = name;
                }
            }
            return replaced;
        }

        public static string[] GetSeparatorReplacedArray(PackedImageEntry[] list, PackType type)
        {
            var count = list.Length;
            var replaced = new string[count];
            char s, r;
            if (type == PackType.Directory)
            {
                s = Path.DirectorySeparatorChar;
                r = Convert.ToChar(1);
            }
            else
            {
                s = Path.AltDirectorySeparatorChar;
                r = Convert.ToChar(2);
            }
            for (var i = 0; i < count; i++)
            {
                var name = list[i].Path;
                if (name.Contains(s))
                {
                    replaced[i] = name.Replace(s, r);
                }
                else
                {
                    replaced[i] = name;
                }
            }
            return replaced;
        }

        // パフォーマンス改善版
        public List<PackedImageEntry> GetPackedImageEntries(PackedImageLoaderFileInfo neededFileInfos = 0, bool needToExecOnTask = false)
        {
            var fullEntriesList = GetPackedImageFullEntries(neededFileInfos, needToExecOnTask);

            if (Type == PackType.Pdf) return fullEntriesList;

            var replaced = GetSeparatorReplacedArray(fullEntriesList, Type);

            var count = fullEntriesList.Count;

            //var fullEntries = fullEntriesList.OrderBy(entry => entry.Path, new LogicalStringComparer()).ToArray();
            var fullEntries = Enumerable.Range(0, count).OrderBy(i => replaced[i], new LogicalStringComparer()).Select(i => fullEntriesList[i]).ToArray();

            replaced = null;

            if (count < 3) return fullEntries.ToList(); // ソートは必要

            var count1 = count - 1;
            var delete = new bool[count];
            var declesedCount = count;
            var path0 = fullEntries[0].Path;
            var path1 = fullEntries[1].Path;
            for (var i = 1; i < count1; i++)
            {
                var path2 = fullEntries[i + 1].Path;

                var len0 = path0.Length;
                var len1 = path1.Length;
                var len2 = path2.Length;
                int centerOffset;
                string first, second, center;
                int lenShort, lenCenter;
                if (len0 > len1 && len1 == len2)
                {
                    if (len0 - len1 < 2)
                    {
                        path0 = path1;
                        path1 = path2;
                        continue;
                    }
                    centerOffset = -1;
                    lenShort = len1;
                    lenCenter = len0;
                    center = path0;
                    first = path1;
                    second = path2;
                }
                else if (len0 < len1 && len0 == len2)
                {
                    if (len1 - len0 < 2)
                    {
                        path0 = path1;
                        path1 = path2;
                        continue;
                    }
                    centerOffset = 0;
                    lenShort = len0;
                    lenCenter = len1;
                    first = path0;
                    center = path1;
                    second = path2;
                }
                else if (len0 == len1 && len1 < len2)
                {
                    if (len2 - len0 < 2)
                    {
                        path0 = path1;
                        path1 = path2;
                        continue;
                    }
                    centerOffset = 1;
                    lenShort = len0;
                    lenCenter = len2;
                    first = path0;
                    second = path1;
                    center = path2;
                }
                else
                {
                    path0 = path1;
                    path1 = path2;
                    continue;
                }
                var digitStart = -1;
                var digitStop = -1;
                var j = 1;
                string preCenter = null, alt = null;
                for (; j < lenShort; j++)
                {
                    var cc = center[j];
                    if (first[j] == cc)
                    {
                        if (second[j] != cc)
                        {
                            preCenter = first;
                            alt = second;
                            break;
                        }
                    }
                    else if (second[j] == cc)
                    {
                        preCenter = second;
                        alt = first;
                        break;
                    }
                    else break;
                }
                if (preCenter == null)
                {
                    path0 = path1;
                    path1 = path2;
                    continue;
                }
                char c0;
                var jTemp = j;
                while (j >= 0 && '0' <= (c0 = preCenter[j]) && c0 <= '9')
                {
                    j--;
                }
                if (j == jTemp)
                {
                    path0 = path1;
                    path1 = path2;
                    continue;
                }
                j++;
                for (; j < lenShort; j++)
                {
                    c0 = preCenter[j];
                    var c1 = center[j];
                    var c0IsDigit = '0' <= c0 && c0 <= '9';
                    if (c0 == c1)
                    {
                        if (c0IsDigit)
                        {
                            if (digitStop < j) digitStart = j;
                            digitStop = j + 1;
                        }
                        continue;
                    }
                    if(digitStop == j && (c0IsDigit || '0' <= c1 && c1 <= '9'))
                    {
                        digitStart = -1;
                    }
                    break;
                }
                if (digitStart < 0)
                {
                    path0 = path1;
                    path1 = path2;
                    continue;
                }
                //var lenC = centerCenter ? len1 : len0;
                var lenBound = Math.Min(lenShort + 3, lenCenter);
                while(j < lenBound)
                {
                    var cc = center[j++];
                    if ('0' <= cc && cc <= '9') break;
                }
                if (j == lenBound)
                {
                    path0 = path1;
                    path1 = path2;
                    continue;
                }
                var digitStart2 = j - 1;
                while (j < lenBound)
                {
                    var cc = center[j++];
                    if (!('0' <= cc && cc <= '9')) break;
                }
                int nLeft, nRight;
                if (!int.TryParse(center.Substring(digitStart, digitStop - digitStart), out nLeft) || !int.TryParse(center.Substring(digitStart2, j - digitStart2 - 1), out nRight) ||
                   Math.Abs(nLeft - nRight) != 1)
                {
                    path0 = path1;
                    path1 = path2;
                    continue;
                }
                
                if(alt == center.Substring(0, digitStart) + center.Substring(digitStart2))
                {
                    declesedCount -= 2; // 削除の重複は起こらない。また起こっても問題にならない。
                    switch (centerOffset)
                    {
                        case -1:
                            delete[i] = true;
                            delete[i + 1] = true;
                            break;
                        case 0:
                            delete[i - 1] = true;
                            delete[i + 1] = true;
                            break;
                        case 1:
                            delete[i - 1] = true;
                            delete[i] = true;
                            break;

                    }
                }

                path0 = path1;
                path1 = path2;
            }

            if (declesedCount < count)
            {
                var result = new List<PackedImageEntry>(capacity: Math.Max(1, declesedCount));
                for (var i = 0; i < count; i++)
                {
                    if (!delete[i]) result.Add(fullEntries[i]);
                }
                return result;
            }
            else
            {
                return fullEntries.ToList();
            }
        }

        /*
        public List<PackedImageEntry> GetPackedImageEntries(PackedImageLoaderFileInfo neededFileInfos = 0, bool needToExecOnTask = false)
        {
            var fullEntriesList = GetPackedImageFullEntries(neededFileInfos, needToExecOnTask);

            if (Type == PackType.Pdf) return fullEntriesList;

            var replaced = GetSeparatorReplacedArray(fullEntriesList, Type);

            var count = fullEntriesList.Count;

            //var fullEntries = fullEntriesList.OrderBy(entry => entry.Path, new LogicalStringComparer()).ToArray();
            var fullEntries = Enumerable.Range(0, count).OrderBy(i => replaced[i], new LogicalStringComparer()).Select(i => fullEntriesList[i]).ToArray();

            replaced = null;

            if (count < 3) return fullEntries.ToList(); // ソートは必要

            var count1 = count - 1;
            var delete = new bool[count];
            var declesedCount = count;
            var path0 = fullEntries[0].Path;
            var path1 = fullEntries[1].Path;
            for (var i = 1; i < count1; i++)
            {
                var path2 = fullEntries[i + 1].Path;

                var len0 = path0.Length;
                var len1 = path1.Length;
                var centerCenter = len0 < len1;
                int len01;
                if (centerCenter)
                {
                    if (len1 - len0 < 2)
                    {
                        path0 = path1;
                        path1 = path2;
                        continue;
                    }
                    len01 = len0;
                }
                else
                {
                    if (len0 - len1 < 2)
                    {
                        path0 = path1;
                        path1 = path2;
                        continue;
                    }
                    len01 = len1;
                }
                var digitStart = -1;
                var digitStop = -1;
                var j = 1;
                for (; j < len01; j++)
                {
                    var c0 = path0[j];
                    var c1 = path1[j];
                    var c0IsDigit = '0' <= c0 && c0 <= '9';
                    if (c0 == c1)
                    {
                        if (c0IsDigit)
                        {
                            if (digitStop < j) digitStart = j;
                            digitStop = j + 1;
                        }
                        continue;
                    }
                    if (digitStop == j && (c0IsDigit || '0' <= c1 && c1 <= '9'))
                    {
                        digitStart = -1;
                    }
                    break;
                }
                if (digitStart < 0)
                {
                    path0 = path1;
                    path1 = path2;
                    continue;
                }
                var center = centerCenter ? path1 : path0;
                var lenC = centerCenter ? len1 : len0;
                len01 = Math.Min(len01 + 3, lenC);
                while (j < len01)
                {
                    var cc = center[j++];
                    if ('0' <= cc && cc <= '9') break;
                }
                if (j == len01)
                {
                    path0 = path1;
                    path1 = path2;
                    continue;
                }
                var digitStart2 = j - 1;
                while (j < lenC)
                {
                    var cc = center[j++];
                    if (!('0' <= cc && cc <= '9')) break;
                }
                int nMin, nMax;
                if (!int.TryParse(center.Substring(digitStart, digitStop - digitStart), out nMin) || !int.TryParse(center.Substring(digitStart2, j - digitStart2 - 1), out nMax) || nMin + 1 != nMax)
                {
                    path0 = path1;
                    path1 = path2;
                    continue;
                }

                if (path2 == center.Substring(0, digitStart) + center.Substring(digitStart2))
                {
                    declesedCount -= 2; // 削除の重複は起こらない。また起こっても問題にならない。
                    delete[centerCenter ? i - 1 : i] = true;
                    delete[i + 1] = true;
                }

                path0 = path1;
                path1 = path2;
            }

            if (declesedCount < count)
            {
                var result = new List<PackedImageEntry>(capacity: Math.Max(1, declesedCount));
                for (var i = 0; i < count; i++)
                {
                    if (!delete[i]) result.Add(fullEntries[i]);
                }
                return result;
            }
            else
            {
                return fullEntries.ToList();
            }
        }
        */

        public static bool PrioritizeSevenZip = false;

        private bool UseZipArchiveClass = true;

        public bool HasAnyEntry()
        {
            switch (Type)
            {
                case PackType.Directory:
                    return hasAnyDirEntry();
                case PackType.ZipM:
                    return hasAnySevenZipArchiveEntry(KnownSevenZipFormat.Zip, GetSevenZipPath());
                case PackType.Rar5:
                    return hasAnySevenZipArchiveEntry(KnownSevenZipFormat.Rar5, GetSevenZipPath());
                case PackType.Lha:
                    return hasAnySevenZipArchiveEntry(KnownSevenZipFormat.Lzh, GetSevenZipPath());
                case PackType.Pdf:
                    return hasAnyPdfEntry();
                default:
                    var sevenZipPath = !PrioritizeSevenZip || stream == null ? null : GetSevenZipPath();

                    if (sevenZipPath == null)
                    {
                        switch (Type)
                        {
                            case PackType.Zip: return UseZipArchiveClass ? hasAnyZipArchiveEntry() : hasAnyIArchiveEntry();
                            case PackType.Cab: return hasAnyCabEntry();
                            default: return hasAnyIArchiveEntry();
                        }
                    }
                    else
                    {
                        switch (Type)
                        {
                            case PackType.Zip: return hasAnySevenZipArchiveEntry(KnownSevenZipFormat.Zip, sevenZipPath);
                            case PackType.Rar: return hasAnySevenZipArchiveEntry(KnownSevenZipFormat.Rar, sevenZipPath);
                            case PackType.Tar: return hasAnySevenZipArchiveEntry(KnownSevenZipFormat.Tar, sevenZipPath);
                            case PackType.SevenZip: return hasAnySevenZipArchiveEntry(KnownSevenZipFormat.SevenZip, sevenZipPath);
                            case PackType.Cab: return hasAnySevenZipArchiveEntry(KnownSevenZipFormat.Cab, sevenZipPath);
                        }
                    }

                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// これをメインスレッドで呼び出し画像の取得をサブスレッドで呼び出す場合 needToExecOnTask = true とすること
        /// </summary>
        /// <param name="neededFileInfos"></param>
        /// <param name="needToExecOnTask"></param>
        /// <returns></returns>
        public List<PackedImageEntry> GetPackedImageFullEntries(PackedImageLoaderFileInfo neededFileInfos = 0, bool needToExecOnTask = false)
        {
            switch (Type)
            {
                case PackType.Directory:
                    return getDirEntries(neededFileInfos, needToExecOnTask);
                case PackType.ZipM:
                    return getSevenZipArchiveEntry(neededFileInfos, KnownSevenZipFormat.Zip, GetSevenZipPath(), needToExecOnTask);
                case PackType.Rar5:
                    return getSevenZipArchiveEntry(neededFileInfos, KnownSevenZipFormat.Rar5, GetSevenZipPath(), needToExecOnTask);
                case PackType.Lha:
                    return getSevenZipArchiveEntry(neededFileInfos, KnownSevenZipFormat.Lzh, GetSevenZipPath(), needToExecOnTask);
                case PackType.Pdf:
                    return getPdfEntries();
                default:
                    var sevenZipPath = !PrioritizeSevenZip || stream == null ? null : GetSevenZipPath();

                    if (sevenZipPath == null)
                    {
                        switch (Type)
                        {
                            case PackType.Zip:
                                return UseZipArchiveClass ? getZipArchiveEntries(neededFileInfos, needToExecOnTask) : getIArchiveEntries(neededFileInfos, needToExecOnTask);
                            case PackType.Rar:
                            case PackType.Tar:
                            case PackType.SevenZip:
                                return getIArchiveEntries(neededFileInfos, needToExecOnTask);
                            case PackType.Cab: return getCabEntries(neededFileInfos, needToExecOnTask);
                        }
                    }
                    else
                    {
                        switch (Type)
                        {
                            case PackType.Zip:
                                return getSevenZipArchiveEntry(neededFileInfos, KnownSevenZipFormat.Zip, sevenZipPath, needToExecOnTask);
                            case PackType.Rar:
                                return getSevenZipArchiveEntry(neededFileInfos, KnownSevenZipFormat.Rar, sevenZipPath, needToExecOnTask);
                            case PackType.Tar:
                                return getSevenZipArchiveEntry(neededFileInfos, KnownSevenZipFormat.Tar, sevenZipPath, needToExecOnTask);
                            case PackType.SevenZip:
                                return getSevenZipArchiveEntry(neededFileInfos, KnownSevenZipFormat.SevenZip, sevenZipPath, needToExecOnTask);
                            case PackType.Cab: return getSevenZipArchiveEntry(neededFileInfos, KnownSevenZipFormat.Cab, sevenZipPath, needToExecOnTask);
                        }
                    }
                    throw new NotSupportedException();
            }
        }
        
        private bool hasAnyDirEntry()
        {
            var ffmpegEixsts = MovieThumbnailLoader.ffmpegExists();
            var files = new DirectoryInfo(path).EnumerateFiles();
            if (files.Any(info => ImageLoader.SupportsFullReading(info.FullName, ffmpegEixsts))) return true;
            // Name でもよいが今後の更新で getDirEntries とギャップが出来る可能性を考慮
            var h = leftHierarchies - 1;
            return h >= 0 &&
                (
                     new DirectoryInfo(path).EnumerateDirectories().Any(cInfo =>
                     {
                         using (var cLoader = new PackedImageLoader(cInfo.FullName, h, PackedImageLoaderSearchMode.UntilFoundItem))
                         {
                             return cLoader.HasAnyEntry();
                         }
                     })
                 ||
                     files.Any(cInfo =>
                     {
                         var path = cInfo.FullName;
                         if (!Supports(path, forFile: true)) return false;
                         using (var cLoader = new PackedImageLoader(path, h, PackedImageLoaderSearchMode.UntilFoundItem))
                         {
                             return cLoader.HasAnyEntry();
                         }
                     })
                );
        }

        private List<PackedImageEntry> getDirEntries(PackedImageLoaderFileInfo neededFileInfos, bool needToExecOnTask)
        {
            var needAny = neededFileInfos != 0;
            FileInfo[] fileInfos = null;
            if (needAny)
            {
                fileInfos = new DirectoryInfo(path).GetFiles();
                dirEntries = new string[fileInfos.Length];
            }
            else
            {
                dirEntries = Directory.GetFiles(path);
            }

            var result = new List<PackedImageEntry>();
            var length = dirEntries.Length;
            var parentPathLength = this.path.Length;
            if(!this.path.EndsWith(Path.DirectorySeparatorChar.ToString()) && !this.path.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
            {
                parentPathLength++;
            }
            var needTime = (neededFileInfos & PackedImageLoaderFileInfo.LastWriteTime) == PackedImageLoaderFileInfo.LastWriteTime;
            var needSize = (neededFileInfos & PackedImageLoaderFileInfo.Size) == PackedImageLoaderFileInfo.Size;
            var ffmpegEixsts = MovieThumbnailLoader.ffmpegExists();
            if (needAny)
            {
                for (var i = 0; i < length; i++)
                {
                    var info = fileInfos[i];
                    var path = info.FullName;
                    dirEntries[i] = path;
                    if (ImageLoader.SupportsFullReading(path, ffmpegEixsts))
                    {
                        DateTime lastWriteTime;
                        if (needTime)
                        {
                            try
                            {
                                lastWriteTime = info.LastWriteTime;
                            }
                            catch
                            {
                                lastWriteTime = new DateTime();
                            }
                        }
                        else
                        {
                            lastWriteTime = DateTime.MinValue;
                        }

                        long size;
                        if (needSize)
                        {
                            try
                            {
                                size = info.Length;
                            }
                            catch
                            {
                                size = -1;
                            }
                        }
                        else
                        {
                            size = -1;
                        }

                        result.Add(new PackedImageEntry(-1, i, path.Substring(parentPathLength), lastWriteTime, size));
                    }
                }
            }
            else
            {
                for (var i = 0; i < length; i++)
                {
                    var path = dirEntries[i];
                    if (ImageLoader.SupportsFullReading(path, ffmpegEixsts))
                    {
                        result.Add(new PackedImageEntry(-1, i, path.Substring(parentPathLength)));
                    }
                }
            }
            
            var h = leftHierarchies - 1;
            if (h >= 0 && (searchMode == PackedImageLoaderSearchMode.Full || result.Count == 0))
            {
                var childrenList = new List<Tuple<PackedImageLoader, List<PackedImageEntry>>>();

                var stop = false;

                if (!stop)
                {
                    foreach (var path in Directory.GetDirectories(path))
                    {
                        var name = path.Substring(parentPathLength);
                        var res = addChildFile(result, childrenList, h, path, name, neededFileInfos, needToExecOnTask);
                        if (searchMode == PackedImageLoaderSearchMode.UntilFoundItem && res > 0)
                        {
                            stop = true;
                            break;
                        }
                    }
                }

                if (!stop)
                {
                    foreach (var path in dirEntries)
                    {
                        var name = path.Substring(parentPathLength);
                        if (Supports(name, forFile: true))
                        {
                            var res = addChildFile(result, childrenList, h, path, name, neededFileInfos, needToExecOnTask);
                            if (searchMode == PackedImageLoaderSearchMode.UntilFoundItem && res > 0)
                            {
                                stop = true;
                                break;
                            }
                        }
                    }
                }
                
                children = childrenList.ToArray();
            }

            // 他に画像がない場合
            /*
            var h = leftHierarchies - 1;
            if (h >= 0 && (alwaysLoadLowerHierarchies || result.Count == 0))
            {
                //System.Windows.Forms.MessageBox.Show($"{alwaysLoadLowerHierarchies} {result.Count}");
                var childrenList = new List<Tuple<PackedImageLoader, List<PackedImageEntry>>>();
                foreach (var path in dirEntries)
                {
                    var name = path.Substring(parentPathLength);
                    if (Supports(name))
                    {
                        addChildFile(result, childrenList, h, path, name, neededFileInfos);
                    }
                }
                foreach (var path in Directory.GetDirectories(path))
                {
                    var name = path.Substring(parentPathLength);
                    addChildFile(result, childrenList, h, path, name, neededFileInfos);
                }
                children = childrenList.ToArray();
            }
            */

            return result;
        }

        private int addChildFile(List<PackedImageEntry> thisEntries,
            List<Tuple<PackedImageLoader, List<PackedImageEntry>>> childrenList, int nextLeft,
            string path, string name, PackedImageLoaderFileInfo neededFileInfos, bool needToExecOnTask)
        {
            return addChildFile(thisEntries, childrenList, nextLeft, new PackedImageLoader(path, nextLeft, searchMode), name, neededFileInfos, needToExecOnTask);
        }

        private int addChildFile(List<PackedImageEntry> thisEntries,
            List<Tuple<PackedImageLoader, List<PackedImageEntry>>> childrenList, int nextLeft,
            Stream stream, long streamLength, string name, PackedImageLoaderFileInfo neededFileInfos, bool needToExecOnTask)
        {
            return addChildFile(thisEntries, childrenList, nextLeft, new PackedImageLoader(stream, streamLength, name, nextLeft, searchMode, animation), name, neededFileInfos, needToExecOnTask);
        }
        
        private int addChildFile(List<PackedImageEntry> thisEntries,
            List<Tuple<PackedImageLoader, List<PackedImageEntry>>> childrenList, int nextLeft,
            PackedImageLoader loader, string loaderName, PackedImageLoaderFileInfo neededFileInfos, bool needToExecOnTask)
        {
            try
            {
                var entries = loader.GetPackedImageFullEntries(neededFileInfos, needToExecOnTask);
                var count = entries.Count;
                if (count == 0)
                {
                    var l = loader;
                    loader = null;
                    l.Dispose(); // これに失敗したら再試行しないように変数 l を使用
                    return 0;
                }
                var childIndex = childrenList.Count;
                for (var i = 0; i < count; i++)
                {
                    var e = entries[i];
                    thisEntries.Add(new PackedImageEntry(childIndex, i, loaderName + Path.AltDirectorySeparatorChar + e.Path, e.LastWriteTime, e.Size));
                }
                var newDepth = loader.LoadedDepth + 1;
                if (newDepth > LoadedDepth) LoadedDepth = newDepth;
                childrenList.Add(Tuple.Create(loader, entries));
                return count;
            }
            catch// (Exception  e)
            {
                if (loader != null) loader.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Stream は Dispose する。必ず遅延割り当てで渡すこと。
        /// </summary>
        /// <param name="children"></param>
        /// <returns></returns>
        private bool hasAnyChildStreamEntry(IEnumerable<Tuple<Stream, long, string>> children, int nextLeft)
        {
            foreach (var child in children)
            {
                var stream = child.Item1; // 遅延割り当てのためこの直前で作成される
                var streamLength = child.Item2;
                var name = child.Item3;
                using (var loader = new PackedImageLoader(stream, streamLength, name, nextLeft, PackedImageLoaderSearchMode.UntilFoundItem, animation)) // エラーの有無にかかわらず stream は Dispose される
                {
                    if (loader.HasAnyEntry())
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool　hasAnyZipArchiveEntry()
        {
            try
            {
                if (zipArchive == null)
                {
                    zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
                }
                var ffmpegEixsts = MovieThumbnailLoader.ffmpegExists();
                var h = leftHierarchies - 1;
                var hNN = h >= 0;
                var zipEntries = hNN ? new List<ZipArchiveEntry>() : null;
                foreach (var entry in zipArchive.Entries)
                {
                    if (ImageLoader.SupportsFullReading(entry.FullName, ffmpegEixsts))
                    {
                        return true;
                    }
                    if (hNN) zipEntries.Add(entry);
                }
                return hNN && hasAnyChildStreamEntry(from e in zipEntries let name = e.FullName where(Supports(name, forFile: false))
                                                     select Tuple.Create(e.Open(), e.Length, name), h);
            }
            catch (InvalidDataException)
            {
                throw new PasswordLockedException();
            }
        }
        private List<PackedImageEntry> getZipArchiveEntries(PackedImageLoaderFileInfo neededFileInfos, bool needToExecOnTask)
        {
            try
            {
                if (zipArchive == null)
                {
                    zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
                }
                zipArchiveEntries = zipArchive.Entries.ToArray();
                var result = new List<PackedImageEntry>();
                var length = zipArchiveEntries.Length;
                //var needAny = neededFileInfos != 0;
                var needTime = (neededFileInfos & PackedImageLoaderFileInfo.LastWriteTime) == PackedImageLoaderFileInfo.LastWriteTime;
                var needSize = (neededFileInfos & PackedImageLoaderFileInfo.Size) == PackedImageLoaderFileInfo.Size;
                var ffmpegEixsts = MovieThumbnailLoader.ffmpegExists();
                for (var i = 0; i < length; i++)
                {
                    var entry = zipArchiveEntries[i];
                    var path = entry.FullName;
                    if (ImageLoader.SupportsFullReading(path, ffmpegEixsts))
                    {
                        result.Add(new PackedImageEntry(-1, i, path, needTime ? entry.LastWriteTime.DateTime : new DateTime(), needSize ? entry.Length : -1));
                    }
                }
                
                var h = leftHierarchies - 1;
                if (h >= 0 && (searchMode == PackedImageLoaderSearchMode.Full || result.Count == 0))
                {
                    var childrenList = new List<Tuple<PackedImageLoader, List<PackedImageEntry>>>();
                    foreach (var entry in zipArchiveEntries)
                    {
                        var name = entry.FullName;
                        if (Supports(name, forFile: false))
                        {
                            var res = addChildFile(result, childrenList, h, entry.Open(), entry.Length, name, neededFileInfos, needToExecOnTask);
                            if (searchMode == PackedImageLoaderSearchMode.UntilFoundItem && res > 0) break;
                        }
                    }
                    children = childrenList.ToArray();
                }

                return result;
            }
            catch (InvalidDataException)
            {
                throw new PasswordLockedException();
            }
        }

        private bool hasAnyIArchiveEntry()
        {
            try
            {
                if (iArchive == null)
                {
                    iArchive = stream != null ? ArchiveFactory.Open(stream) : ArchiveFactory.Open(path);
                }
                //var leftSize = long.MaxValue;
                //if (iArchive.IsSolid) leftSize = SolidSizeBound;

                var ffmpegEixsts = MovieThumbnailLoader.ffmpegExists();
                var h = leftHierarchies - 1;
                var hNN = h >= 0;
                var iArchiveEntries = hNN ? new List<IArchiveEntry>() : null;

                foreach (var entry in iArchive.Entries)
                {
                    //leftSize -= entry.Size;
                    //if (leftSize < 0) break;
                    if (ImageLoader.SupportsFullReading(entry.Key, ffmpegEixsts))
                    {
                        return true;
                    }
                    if (hNN) iArchiveEntries.Add(entry);
                }
                
                return hNN && hasAnyChildStreamEntry(from e in iArchiveEntries let name = e.Key
                                                     where Supports(name, forFile: false) select Tuple.Create(e.OpenEntryStream(), e.Size, name), h);
            }
            catch (Exception e)
            {
                if (e.ToString().Contains("Crypto"))
                {
                    throw new PasswordLockedException();
                }
                else
                {
                    throw;
                }
            }
        }

        private bool hasAnySevenZipArchiveEntry(KnownSevenZipFormat format, string sevenZipPath)
        {
            if (sevenZipArchive == null)
            {
                if (sevenZipPath != null)
                {
                    sevenZipArchive = stream != null ? new ArchiveFile(stream, format, sevenZipPath) : new ArchiveFile(path, sevenZipPath);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            //var sizeBound = long.MaxValue;
            //long size = 0;

            var ffmpegEixsts = MovieThumbnailLoader.ffmpegExists();
            var h = leftHierarchies - 1;
            var hNN = h >= 0;
            var sevenZipEntries = hNN ? new List<Entry>() : null;

            foreach (var entry in sevenZipArchive.EnumerableEntries)
            {
                //size += entry.Size;
                //if (sizeBound == long.MaxValue && entry.IsSolid) sizeBound = SolidSizeBound;
                //if (size > sizeBound) break;
                if (!entry.IsFolder && ImageLoader.SupportsFullReading(entry.FileName, ffmpegEixsts)) return true;
                if (hNN) sevenZipEntries.Add(entry);
            }

            return hNN && hasAnyChildStreamEntry(from e in sevenZipEntries
                                                 let name = e.FileName
                                                 where Supports(name, forFile: false)
                                                 select Tuple.Create(openSevenZipStream(e) as Stream, e.Size, name), h);
        }

        private static MemoryStream openSevenZipStream(Entry entry)
        {
            var result = new MemoryStream();
            try
            {
                entry.Extract(result);
                result.Seek(0, SeekOrigin.Begin);
                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }
        
        private List<PackedImageEntry> getIArchiveEntries(PackedImageLoaderFileInfo neededFileInfos, bool needToExecOnTask)
        {
            try
            {
                if (iArchive == null)
                {
                    iArchive = stream != null ? ArchiveFactory.Open(stream) : ArchiveFactory.Open(path);
                }
                //var leftSize = long.MaxValue;
                if (iArchive.IsSolid)
                {
                    OpenInnerImageStreamSharpCompress_IsSolid = true;
                    //leftSize = SolidSizeBound;
                }
                
                var needToIgnoreMacList = Type == PackType.Zip;
                //var macFileList = needToIgnoreMacList ? new List<PackedImageEntry>() : null;
                
                iArchiveEntries = iArchive.Entries.ToArray();
                var result = new List<PackedImageEntry>();
                var length = iArchiveEntries.Length;
                var needTime = (neededFileInfos & PackedImageLoaderFileInfo.LastWriteTime) == PackedImageLoaderFileInfo.LastWriteTime;
                var needSize = (neededFileInfos & PackedImageLoaderFileInfo.Size) == PackedImageLoaderFileInfo.Size;
                var ffmpegEixsts = MovieThumbnailLoader.ffmpegExists();
                for (var i = 0; i < length; i++)
                {
                    var entry = iArchiveEntries[i];
                    var size = entry.Size;
                    //leftSize -= size;
                    //if (leftSize < 0) break;
                    var path = entry.Key;
                    if (ImageLoader.SupportsFullReading(path, ffmpegEixsts))
                    {
                        DateTime dateTime;
                        if (needTime)
                        {
                            var dateTime_ = entry.LastModifiedTime;
                            if (dateTime_ != null) dateTime = (DateTime)dateTime_;
                            else dateTime = DateTime.MinValue;
                        }
                        else
                        {
                            dateTime = DateTime.MinValue;
                        }
                        var piEntry = new PackedImageEntry(-1, i, path, dateTime, needSize ? size : -1);

                        if (needToIgnoreMacList && maxosxIgnore(entry.Key))
                        {
                            //macFileList.Add(piEntry);
                            continue;
                        }

                        result.Add(piEntry);
                    }
                }

                /*
                if (needToIgnoreMacList && macFileList.Any())
                {
                    var noEntity = new List<PackedImageEntry>();
                    foreach (var piEntry in macFileList)
                    {
                        var entityPath = piEntry.Path.Substring(9);
                        if (result.All(entity => entity.Path != entityPath))
                        {
                            noEntity.Add(piEntry);
                        }
                    }
                    if (noEntity.Any())
                    {
                        result.AddRange(noEntity);
                    }
                }
                */
                
                var h = leftHierarchies - 1;
                if (h >= 0 && (searchMode == PackedImageLoaderSearchMode.Full || result.Count == 0))
                {
                    var childrenList = new List<Tuple<PackedImageLoader, List<PackedImageEntry>>>();
                    foreach (var entry in iArchiveEntries)
                    {
                        var name = entry.Key;
                        if (Supports(name, forFile: false))
                        {
                            var res = addChildFile(result, childrenList, h, entry.OpenEntryStream(), entry.Size, name, neededFileInfos, needToExecOnTask);
                            if (searchMode == PackedImageLoaderSearchMode.UntilFoundItem && res > 0) break;
                        }
                    }
                    children = childrenList.ToArray();
                }

                return result;
            }
            catch (Exception e)
            {
                if (e.ToString().Contains("Crypto"))
                {
                    throw new PasswordLockedException();
                }
                else
                {
                    throw;
                }
            }
        }

        private static bool maxosxIgnore(string key)
        {
            if (!key.StartsWith("__MACOSX/")) return false;
            var fileNameIndex = key.LastIndexOf('/') + 1;
            return fileNameIndex < key.Length && key[fileNameIndex] == '.';
        }

        private List<PackedImageEntry> getSevenZipArchiveEntry(PackedImageLoaderFileInfo neededFileInfos, KnownSevenZipFormat format,
            string sevenZipPath, bool needToExecOnTask)
        {
            if (needToExecOnTask)
            {
                using (var task = Task.Run(() => getSevenZipArchiveEntry_Base(neededFileInfos, format, sevenZipPath, needToExecOnTask)))
                {
                    task.Wait();
                    return task.Result;
                }
            }
            else return getSevenZipArchiveEntry_Base(neededFileInfos, format, sevenZipPath, needToExecOnTask);
        }

        private List<PackedImageEntry> getSevenZipArchiveEntry_Base(PackedImageLoaderFileInfo neededFileInfos, KnownSevenZipFormat format, string sevenZipPath, bool needToExecOnTask)
        {
            if (sevenZipArchive == null)
            {
                sevenZipArchive = stream != null ? new ArchiveFile(stream, format, sevenZipPath) : new ArchiveFile(path, sevenZipPath);
            }

            // RAR5 の Solid はパフォーマンスに甚大な影響は与えないので明示的なキャッシングは行わない

            //var sizeBound = long.MaxValue;
            //long totalSize = 0;
            
            //System.Windows.Forms.MessageBox.Show($"get entries");
            sevenZipArchiveEntries = sevenZipArchive.Entries.ToArray();
            //System.Windows.Forms.MessageBox.Show("got entries");
            var result = new List<PackedImageEntry>();
            var length = sevenZipArchiveEntries.Length;
            var needTime = (neededFileInfos & PackedImageLoaderFileInfo.LastWriteTime) == PackedImageLoaderFileInfo.LastWriteTime;
            var needSize = (neededFileInfos & PackedImageLoaderFileInfo.Size) == PackedImageLoaderFileInfo.Size;
            var ffmpegEixsts = MovieThumbnailLoader.ffmpegExists();
            for (var i = 0; i < length; i++)
            {
                var entry = sevenZipArchiveEntries[i];
                var size = entry.Size;
                //totalSize += size;
                //if (sizeBound == long.MaxValue && entry.IsSolid) sizeBound = SolidSizeBound;
                //if (totalSize > sizeBound) break;
                var path = entry.FileName;
                if (ImageLoader.SupportsFullReading(path, ffmpegEixsts))
                {
                    DateTime dateTime;
                    if (needTime)
                    {
                        var dateTime_ = entry.LastWriteTime;
                        if (dateTime_ != null) dateTime = (DateTime)dateTime_;
                        else dateTime = DateTime.MinValue;
                    }
                    else
                    {
                        dateTime = DateTime.MinValue;
                    }
                    result.Add(new PackedImageEntry(-1, i, path, dateTime, needSize ? size : -1));
                }
            }

            var h = leftHierarchies - 1;
            if (h >= 0 && (searchMode == PackedImageLoaderSearchMode.Full || result.Count == 0))
            {
                var childrenList = new List<Tuple<PackedImageLoader, List<PackedImageEntry>>>();
                foreach (var entry in sevenZipArchiveEntries)
                {
                    var name = entry.FileName;
                    if (Supports(name, forFile: false))
                    {
                        var res = addChildFile(result, childrenList, h, openSevenZipStream(entry), entry.Size, name, neededFileInfos, needToExecOnTask);
                        if (searchMode == PackedImageLoaderSearchMode.UntilFoundItem && res > 0) break;
                    }
                }
                children = childrenList.ToArray();
            }

            return result;
        }

        private bool hasAnyPdfEntry()
        {
            try
            {
                //if (pdfArchive == null) pdfArchive = new PdfReader(path, null, true);
                if (pdfArchive == null) pdfArchive = GetPdfReader(stream, null);
                return pdfArchive.NumberOfPages > 0;
            }
            catch (iTextSharp.text.exceptions.BadPasswordException)
            {
                throw new PasswordLockedException();
            }
            catch
            {
                throw new NotSupportedException();
            }
        }

        private List<PackedImageEntry> getPdfEntries()
        {
            try
            {
                //if (pdfArchive == null) pdfArchive = new PdfReader(path, null, true);
                if (pdfArchive == null) pdfArchive = GetPdfReader(stream, null);
                var result = new List<PackedImageEntry>();
                var length = pdfArchive.NumberOfPages;

                /*
                var maxDigit = (int)Math.Floor(Math.Log10(Math.Max(1, length - 1))) + 1;
                var format = "{0:D" + maxDigit + "}";
                for (var i = 0; i < length; i++)
                {
                    result.Add(new PackedImageEntry(i, string.Format(format, i)));
                }
                */
                for (var i = 0; i < length; i++)
                {
                    result.Add(new PackedImageEntry(-1, i, (i + 1).ToString()));
                }

                return result;
            }
            catch(iTextSharp.text.exceptions.BadPasswordException)
            {
                throw new PasswordLockedException();
            }
            catch// (Exception e)
            {
                throw new NotSupportedException();
            }
        }

        private PdfReader GetPdfReader(Stream isp, byte[] ownerPassword)
        {
            return PdfReaderBaseConstructor(
                byteSource: new MyRandomAccessSource(isp),
                partialRead: true,
                ownerPassword: ownerPassword,
                certificate: null,
                certificateKey: null,
                closeSourceOnConstructorError: false
            );
        }

        private static PdfReader PdfReaderBaseConstructor(
            iTextSharp.text.io.IRandomAccessSource byteSource,
            bool partialRead,
            byte[] ownerPassword,
            Org.BouncyCastle.X509.X509Certificate certificate,
            Org.BouncyCastle.Crypto.ICipherParameters certificateKey,
            bool closeSourceOnConstructorError)
        {
            return PdfReaderBaseConstructorInfo.Invoke(new object[] {
                byteSource,
                partialRead,
                ownerPassword,
                certificate,
                certificateKey,
                closeSourceOnConstructorError
            }) as PdfReader;
        }

        private static readonly System.Reflection.ConstructorInfo PdfReaderBaseConstructorInfo = GetPdfReaderBaseConstructorInfo(); // スレッドセーフ
        private static System.Reflection.ConstructorInfo GetPdfReaderBaseConstructorInfo()
        {
            return typeof(PdfReader).GetConstructor(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, new Type[] {
                typeof(iTextSharp.text.io.IRandomAccessSource),
                typeof(bool),
                typeof(byte[]),
                typeof(Org.BouncyCastle.X509.X509Certificate),
                typeof(Org.BouncyCastle.Crypto.ICipherParameters),
                typeof(bool)
            }, null);
        }
        
        private List<PackedImageEntry> getCabEntries(PackedImageLoaderFileInfo neededFileInfos, bool needToExecOnTask)
        {
            try
            {
                if (cabArchive == null)
                {
                    cabArchive = new CabEngine();
                    cabArchiveEntries = cabArchive.GetFileInfo(stream);
                }
                var result = new List<PackedImageEntry>();
                var length = cabArchiveEntries.Count;
                var needTime = (neededFileInfos & PackedImageLoaderFileInfo.LastWriteTime) == PackedImageLoaderFileInfo.LastWriteTime;
                var needSize = (neededFileInfos & PackedImageLoaderFileInfo.Size) == PackedImageLoaderFileInfo.Size;
                var ffmpegEixsts = MovieThumbnailLoader.ffmpegExists();
                for (var i = 0; i < length; i++)
                {
                    var entry = cabArchiveEntries[i];
                    var path = entry.Name;
                    if (ImageLoader.SupportsFullReading(path, ffmpegEixsts))
                    {
                        DateTime dateTime;
                        if (needTime)
                        {
                            dateTime = entry.LastWriteTime;
                        }
                        else
                        {
                            dateTime = DateTime.MinValue;
                        }
                        result.Add(new PackedImageEntry(-1, i, path, dateTime, needSize ? entry.Length : -1));
                    }
                }

                var h = leftHierarchies - 1;
                if (h >= 0 && (searchMode == PackedImageLoaderSearchMode.Full || result.Count == 0))
                {
                    var childrenList = new List<Tuple<PackedImageLoader, List<PackedImageEntry>>>();
                    foreach (var entry in cabArchiveEntries)
                    {
                        var name = entry.Name;
                        if (Supports(name, forFile: false))
                        {
                            var res = addChildFile(result, childrenList, h, cabArchive.Unpack(stream, name), entry.Length, name, neededFileInfos, needToExecOnTask);
                            if (searchMode == PackedImageLoaderSearchMode.UntilFoundItem && res > 0) break;
                        }
                    }
                    children = childrenList.ToArray();
                }

                return result;
            }
            catch// (Exception e)
            {
                throw new NotSupportedException();
            }
        }

        private bool hasAnyCabEntry()
        {
            try
            {
                if (cabArchive == null)
                {
                    cabArchive = new CabEngine();
                    cabArchiveEntries = cabArchive.GetFileInfo(stream);
                }
                var length = cabArchiveEntries.Count;
                var ffmpegEixsts = MovieThumbnailLoader.ffmpegExists();
                var h = leftHierarchies - 1;
                var hNN = h >= 0;
                var cabEntries = hNN ? new List<Microsoft.Deployment.Compression.ArchiveFileInfo>() : null;
                for (var i = 0; i < length; i++)
                {
                    var entry = cabArchiveEntries[i];
                    var path = entry.Name;
                    if (ImageLoader.SupportsFullReading(path, ffmpegEixsts))
                    {
                        return true;
                    }
                    if (hNN) cabEntries.Add(entry);
                }

                return hNN && hasAnyChildStreamEntry(from e in cabEntries
                                                     let name = e.Name
                                                     where Supports(name, forFile: false)
                                                     select Tuple.Create(cabArchive.Unpack(stream, name), e.Length, name), h);
            }
            catch
            {
                throw new NotSupportedException();
            }
        }

        public Stream GetStream(PackedImageEntry entry, out string extensionWithPeriodForPDF, bool callback = false)
        {
            var cIndex = entry.ChildIndex;
            if (cIndex < 0)
            {
                return GetStream(entry.Index, out extensionWithPeriodForPDF, callback);
            }
            else
            {
                var t = children[cIndex];
                return t.Item1.GetStream(t.Item2[entry.Index], out extensionWithPeriodForPDF, callback);
            }
        }

        private Stream GetStream(int physicalIndex, out string extensionWithPeriodForPDF, bool callback)
        {
            extensionWithPeriodForPDF = null;
            switch (Type)
            {
                case PackType.ZipM:
                case PackType.Rar5:
                case PackType.Lha:
                    return GetInnerImageStreamSevenZip(physicalIndex, seekToBegin: true, callback: callback);
                case PackType.Pdf:
                    Bitmap dummy;
                    pdfImageMustBeConvert = true; // コンバートが不要な場合得られるバイナリに違いはないので GetStream の場合は常に true に
                    return GetInnerImageStreamPdf(physicalIndex, getImage: false, extensionWithPeriodForPDF: out extensionWithPeriodForPDF, image: out dummy);
                default:
                    if (sevenZipArchive != null)
                    {
                        return GetInnerImageStreamSevenZip(physicalIndex, seekToBegin: true, callback: callback);
                    }
                    else
                    {
                        switch (Type)
                        {
                            case PackType.Zip:
                                if (zipArchive != null)
                                {
                                    return GetInnerImageStream(physicalIndex);
                                }
                                else
                                {
                                    return GetInnerImageStreamSharpCompress(physicalIndex);
                                }
                            case PackType.Rar:
                            case PackType.Tar:
                            case PackType.SevenZip:
                                //case PackType.Gz:
                                return GetInnerImageStreamSharpCompress(physicalIndex);
                            case PackType.Cab:
                                var cabEntrie = cabArchiveEntries[physicalIndex];
                                var name = cabEntrie.Name;
                                return cabArchive.Unpack(stream, name);
                        }
                    }
                    throw new NotSupportedException();
            }

        }

        public Bitmap OpenImageStream(PackedImageEntry entry)
        {
            var cIndex = entry.ChildIndex;
            if (cIndex < 0)
            {
                return OpenImageStream(entry.Index);
            }
            else
            {
                var t = children[cIndex];
                return t.Item1.OpenImageStream(t.Item2[entry.Index]);
            }
        }

        private Bitmap OpenImageStream(int physicalIndex)
        {
            switch (Type)
            {
                case PackType.Directory:
                    var path = dirEntries[physicalIndex];
                    return ImageLoader.GetFullBitmap(path, animation);
                case PackType.ZipM:
                case PackType.Rar5:
                case PackType.Lha:
                    return OpenInnerImageStreamSevenZip(physicalIndex);
                case PackType.Pdf:
                    return OpenInnerImageStreamPdf(physicalIndex);
                default:
                    if (sevenZipArchive != null)
                    {
                        return OpenInnerImageStreamSevenZip(physicalIndex);
                    }
                    else
                    {
                        switch (Type)
                        {
                            case PackType.Zip:
                                if (zipArchive != null)
                                {
                                    return OpenInnerImageStream(physicalIndex);
                                }
                                else
                                {
                                    return OpenInnerImageStreamSharpCompress(physicalIndex);
                                }
                            case PackType.Rar:
                            case PackType.Tar:
                            case PackType.SevenZip:
                                //case PackType.Gz:
                                return OpenInnerImageStreamSharpCompress(physicalIndex);
                            case PackType.Cab:
                                var cabEntrie = cabArchiveEntries[physicalIndex];
                                var name = cabEntrie.Name;
                                return GetImageAndDisposeOriginal(cabArchive.Unpack(stream, name), name);
                        }
                    }



                    throw new NotSupportedException();
            }
        }

        private Bitmap GetImageAndDisposeOriginal(Stream stream, string fileNameForType)
        {
            try
            {
                return ImageFromStream(stream, fileNameForType, animation);
            }
            finally
            {
                stream.Dispose();
            }
        }

        private Bitmap OpenInnerImageStream(int physicalIndex)
        {
            try
            {
                var entry = zipArchiveEntries[physicalIndex];
                return GetImageAndDisposeOriginal(entry.Open(), entry.Name);
            }
            catch (InvalidDataException)
            {
                throw new PasswordLockedException();
            }
        }

        private Stream GetInnerImageStream(int physicalIndex)
        {
            try
            {
                var entry = zipArchiveEntries[physicalIndex];
                return entry.Open();
            }
            catch (InvalidDataException)
            {
                throw new PasswordLockedException();
            }
        }

        /*
        private MemoryStream ConvertToMemoryStream(Stream stream)
        {
            var result = stream as MemoryStream;
            if (result != null) return result;
            try
            {
                result = new MemoryStream();
                stream.CopyTo(result);
                return result;
            }
            catch
            {
                result?.Dispose();
                throw;
            }
            finally
            {
                stream.Dispose();
            }
        }
        */

        private bool OpenInnerImageStreamSharpCompress_IsSolid = false;
        private Bitmap OpenInnerImageStreamSharpCompress(int physicalIndex)
        {
            try
            {
                if (!OpenInnerImageStreamSharpCompress_IsSolid)
                {
                    var entry = iArchiveEntries[physicalIndex];
                    try
                    {
                        //return GetImageAndDisposeOriginal(SeekableStream.Seekablize(entry.OpenEntryStream(), entry.Size), entry.Key);
                        return GetImageAndDisposeOriginal(entry.OpenEntryStream(), entry.Key);
                    }
                    catch (InvalidOperationException)
                    {
                        OpenInnerImageStreamSharpCompress_IsSolid = true;
                    }
                }

                if (entriesStreamListForImage == null)
                {
                    entriesStreamListForImage = new List<Stream>();
                    entriesImageList = new List<Bitmap>();
                    iReader = iArchive.ExtractAllEntries();
                }
                for (var i = entriesStreamListForImage.Count; i <= physicalIndex && iReader.MoveToNextEntry(); i++)
                {
                    var entry = iArchiveEntries[i];
                    if (ImageLoader.SupportsFullReading(entry.Key, ffmpegExists: true))
                    {
                        entriesStreamListForImage.Add(iReader.OpenEntryStream());
                    }
                    else
                    {
                        entriesStreamListForImage.Add(null);
                    }
                    entriesImageList.Add(null);
                }
                
                var image = entriesImageList[physicalIndex];
                if (image == null)
                {
                    var entry = iArchiveEntries[physicalIndex];
                    using (var stream = entriesStreamListForImage[physicalIndex])
                    {
                        image = ImageFromStream(stream, entry.Key, animation);
                        entriesImageList[physicalIndex] = image;
                        stream.Close();
                    }
                    entriesStreamListForImage[physicalIndex] = null;
                }
                
                return image.Clone() as Bitmap;
                //return ViewerForm.Clone(entriesImageList[physicalIndex]); // BitmapResizer でエラーが生じる
            }
            catch (Exception e)
            {
                if(e.ToString().Contains("Crypto"))
                {
                    throw new PasswordLockedException();
                }
                else
                {
                    throw;
                }
            }
        }

        private Stream GetInnerImageStreamSharpCompress(int physicalIndex)
        {
            try
            {
                if (!OpenInnerImageStreamSharpCompress_IsSolid)
                {
                    var entry = iArchiveEntries[physicalIndex];
                    try
                    {
                        return entry.OpenEntryStream();
                    }
                    catch (InvalidOperationException)
                    {
                        OpenInnerImageStreamSharpCompress_IsSolid = true;
                    }
                }

                if (entriesStreamList == null)
                {
                    entriesStreamList = new List<Stream>();
                    iReaderForStream = iArchive.ExtractAllEntries();
                }
                for (var i = entriesStreamList.Count; i <= physicalIndex && iReaderForStream.MoveToNextEntry(); i++)
                {
                    var stream = iReaderForStream.OpenEntryStream();
                    try
                    {
                        entriesStreamList.Add(stream);
                    }
                    catch
                    {
                        stream.Dispose();
                        throw;
                    }
                }

                var result = new MemoryStream();
                try
                {
                    entriesStreamList[physicalIndex].CopyTo(result);
                    result.Seek(0, SeekOrigin.Begin);
                    return result;
                }
                catch
                {
                    result.Dispose();
                    throw;
                }
            }
            catch (Exception e)
            {
                if (e.ToString().Contains("Crypto"))
                {
                    throw new PasswordLockedException();
                }
                else
                {
                    throw;
                }
            }
        }

        private Bitmap OpenInnerImageStreamSevenZip(int physicalIndex)
        {
            var entry = sevenZipArchiveEntries[physicalIndex];
            using (var ms = GetInnerImageStreamSevenZip(physicalIndex, entry, seekToBegin: true, callback: false))
            {
                return ImageFromStream(ms, entry.FileName, animation);
            }
        }

        private MemoryStream GetInnerImageStreamSevenZip(int physicalIndex, bool seekToBegin, bool callback)
        {
            return GetInnerImageStreamSevenZip(physicalIndex, sevenZipArchiveEntries[physicalIndex], seekToBegin, callback);
        }

        private MemoryStream GetInnerImageStreamSevenZip(int physicalIndex, Entry entry, bool seekToBegin, bool callback)
        {
            // 7z 自体がキャッシュ機構を備えている
            var ms = new MemoryStream();
            try
            {
                if(callback)
                {
                    SevenZipEntryExtractInTask(entry, ms);
                }
                else
                {
                    entry.Extract(ms);
                }
                
                if (seekToBegin) ms.Seek(0, SeekOrigin.Begin);
                return ms;
            }
            catch
            {
                ms.Dispose();
                throw;
            }
        }

        private static void SevenZipEntryExtractInTask(Entry entry, Stream dst)
        {
            //var t = new System.Threading.Thread(new System.Threading.ThreadStart(() => entry.Extract(dst)));
            //t.Start();
            //t.Join();

            using (var task = Task.Run(() => entry.Extract(dst)))
            {
                task.Wait();
                var e = task.Exception;
                if (e != null) throw e;
            }
        }

        private Bitmap OpenInnerImageStreamPdf(int physicalIndex)
        {
            Bitmap result;
            GetInnerImageStreamPdf(physicalIndex, getImage: true, extensionWithPeriodForPDF: out var dummy, image: out result).Dispose();
            //using (var ms = GetInnerImageStreamPdf(physicalIndex))
            {
                // pdf ↓ が必要なデータが入っていることは想定できず拡張子が分からないデメリットに見合わない
                //result0 = ImageFromStream(stream, filePathForGetFileType: null);
                if (result != null)
                {
                    return result;
                }
                else
                {
                    throw new ArgumentException();
                }
            }
        }


        private MemoryStream GetInnerImageStreamPdf(int physicalIndex, bool getImage, out string extensionWithPeriodForPDF, out Bitmap image)
        {
            try
            {
                //var sw = new System.Diagnostics.Stopwatch(); var s = "";  sw.Start();

                var page = pdfArchive.GetPageN(physicalIndex + 1); // ページは１オリジン
                var res = PdfReader.GetPdfObject(page.Get(PdfName.RESOURCES)) as PdfDictionary;
                var xobj = PdfReader.GetPdfObject(res.Get(PdfName.XOBJECT)) as PdfDictionary;
                if (xobj == null)
                {
                    throw new ArgumentException();
                }
                var cb = xobj.Keys.Count;

                //sw.Stop(); s += $"{sw.Elapsed}\n"; sw.Restart();

                foreach (var name in xobj.Keys)
                {
                    var obj = xobj.Get(name);
                    if (!obj.IsIndirect()) continue;

                    var tg = PdfReader.GetPdfObject(obj) as PdfDictionary;
                    var type = PdfReader.GetPdfObject(tg.Get(PdfName.SUBTYPE)) as PdfName;
                    if (!PdfName.IMAGE.Equals(type)) continue;

                    int XrefIndex = (obj as PRIndirectReference).Number;
                    var pdfStream = pdfArchive.GetPdfObject(XrefIndex) as PRStream;

                    if (!pdfImageMustBeConvert)
                    {
                        try
                        {
                            var stream = new MemoryStream(PdfReader.GetStreamBytesRaw(pdfStream));
                            try
                            {
                                if (getImage)
                                {
                                    image = new Bitmap(stream);
                                    extensionWithPeriodForPDF = null;
                                }
                                else
                                {
                                    image = null;
                                    stream = GetImageType(stream, out extensionWithPeriodForPDF);
                                }
                                return stream;
                            }
                            catch
                            {
                                stream.Close();
                                stream.Dispose();
                                throw;
                            }
                        }
                        catch (ArgumentException)
                        {
                            pdfImageMustBeConvert = true;
                        }

                    }

                    var result = RiosFastPdfImageDecoder.Decode(pdfStream, out var imageType);
                    if (getImage)
                    {
                        extensionWithPeriodForPDF = null;
                        try
                        {
                            switch (imageType)
                            {
                                case RiosFastPdfImageDecoder.Type.JP2:
#if DEBUG
                                    //using (var fs = new FileStream("x.jp2", FileMode.CreateNew, FileAccess.Write)) result.CopyTo(fs);
                                    //result.Seek(0, SeekOrigin.Begin);
#endif
                                    image = ImageLoader.GetFullBitmap(result, "x.jp2");
                                    break;
                                default:
                                    image = new Bitmap(result);
                                    break;
                            }
                        }
                        catch
                        {
                            result.Dispose();
                            throw;
                        }
                    }
                    else
                    {
                        image = null;
                        switch (imageType)
                        {
                            case RiosFastPdfImageDecoder.Type.JP2:
                                extensionWithPeriodForPDF = ".jp2";
                                break;
                            case RiosFastPdfImageDecoder.Type.JPG:
                                extensionWithPeriodForPDF = ".jpg";
                                break;
                            case RiosFastPdfImageDecoder.Type.PNG:
                                extensionWithPeriodForPDF = ".png";
                                break;
                            default:
                                extensionWithPeriodForPDF = ".tiff";
                                break;
                        }
                    }

                    return result;
                }
            }
            catch (iTextSharp.text.exceptions.BadPasswordException)
            {
                throw new PasswordLockedException();
            }
            catch (Exception ex)
            {
                throw new NotSupportedException(null, ex);
            }

            throw new ArgumentException();
        }

        private static MemoryStream GetImageType(MemoryStream ms, out string extension)
        {
            try
            {
                var imageOpened = false;
                try
                {
                    using (var img = Image.FromStream(ms, useEmbeddedColorManagement: false, validateImageData: false)) // 検証なし
                    {
                        imageOpened = true;
                        var format = img.RawFormat;
                        if (format.Equals(ImageFormat.Jpeg))
                        {
                            extension = ".jpg";
                            return ms;
                        }
                        if (format.Equals(ImageFormat.Tiff))
                        {
                            extension = ".tiff";
                            return ms;
                        }
                        if (format.Equals(ImageFormat.Bmp))
                        {
                            extension = ".bmp";
                            return ms;
                        }
                        if (format.Equals(ImageFormat.Emf))
                        {
                            extension = ".emf";
                            return ms;
                        }
                        if (format.Equals(ImageFormat.Gif))
                        {
                            extension = ".gif";
                            return ms;
                        }
                        if (format.Equals(ImageFormat.Icon))
                        {
                            extension = ".ico";
                            return ms;
                        }
                        if (format.Equals(ImageFormat.Png))
                        {
                            extension = ".png";
                            return ms;
                        }
                        if (format.Equals(ImageFormat.Wmf))
                        {
                            extension = ".wmf";
                            return ms;
                        }
                        extension = "";
                        return ms;
                    }
                }
                finally
                {
                    if (imageOpened)
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                    }
                }
            }
            catch
            {
                extension = "";
                return ms;
            }
        }

        private static Bitmap ImageFromStream(Stream stream, string filePathForGetFileType, bool animation)
        {
            Bitmap result;
            try
            {
                //result = Image.FromStream(stream);
                result = ImageLoader.GetFullBitmap(stream, filePathForGetFileType, animation);
            }
            catch (Exception e)
            {
                throw new ArgumentException(null, e);
            }
            if (result == null) throw new ArgumentException();
            return result;
        }

        private PackType GetTypeFromStreamAndSeekFirst(Stream stream)
        {
            const int bufferSize = 8;
            var buffer = new byte[bufferSize];
            var index = 0;
            while (index < bufferSize)
            {
                var read = stream.Read(buffer, index, bufferSize - index);
                if (read <= 0) throw new IOException($"index = {index}, stream = {stream}");
                index += read;
            }
            if (buffer[0] == 0x50 && buffer[1] == 0x4B && (buffer[2] == 0x03 && buffer[3] == 0x04 || buffer[2] == 0x07 && buffer[3] == 0x08))
            {
                /*
                UTF-8 フラグがあてにならないので直接文字コードをチェックする
                ただし UTF-8 は ASCII を包含するので一つ目のファイルが ASCII 内だが他に ASCII 外のファイルを含む場合は正しく判定できない
                最も多く想定されるのは ASCII 内のフォルダがルートに存在する場合なのでこれを回避するため
                ASCII 判定かつサイズ 0 の間は判定を繰り返す
                */
                var first = true;
                var foundUtf8 = false;
                while (true)
                {
                    var skip = first ? 14 : 22;
                    first = false;
                    var atEnd = false;
                    while (skip > 0)
                    {
                        var read = stream.Read(buffer, 0, Math.Min(bufferSize, skip));
                        if (read <= 0)
                        {
                            if (skip == 22)
                            {
                                atEnd = true;
                                break;
                            }
                            throw new IOException();
                        }
                        skip -= read;
                    }
                    if (atEnd) break;
                    var i = 0;
                    while (i < 8)
                    {
                        var read = stream.Read(buffer, i, 8 - i);
                        if (read <= 0) throw new IOException();
                        i += read;
                    }
                    var uncompressedSize = buffer[0] | (uint)buffer[1] << 8 | (uint)buffer[2] << 16 | (uint)buffer[3] << 24;
                    var fileNameLength = buffer[4] | buffer[5] << 8;
                    var extraFieldLength = buffer[6] | buffer[7] << 8;
                    var nameBuffer = new byte[fileNameLength];
                    i = 0;
                    while (i < fileNameLength)
                    {
                        var read = stream.Read(nameBuffer, i, fileNameLength - i);
                        if (read <= 0) throw new IOException();
                        i += read;
                    }
                    var code = ImageLoader.GetCode(nameBuffer);
                    foundUtf8 = code == Encoding.UTF8;

#if DEBUG
                    int preambleLength = code.GetPreamble().Length;
                    var s = code.GetString(nameBuffer, preambleLength, nameBuffer.Length - preambleLength);
#endif
                    if (code != Encoding.ASCII || uncompressedSize != 0)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        break;
                    }

                    skip = extraFieldLength;
                    while (skip > 0)
                    {
                        var read = stream.Read(buffer, 0, Math.Min(bufferSize, skip));
                        if (read == 0) throw new IOException();
                        skip -= read;
                    }
                }

                UseZipArchiveClass = !foundUtf8;

                /*
                if ((buffer[7] & 0x8) == 0x8) // UTF-8 フラグ
                {
                    UseZipArchiveClass = false; // UTF-8 フラグがあっても ZipArchive は対応できない
                    stream.Seek(-bufferSize, SeekOrigin.Current);
                }
                else // UTF-8 フラグのないファイルへの対応
                */
                /*
                {
                    var skip = 16;
                    while (skip > 0)
                    {
                        var read = stream.Read(buffer, 0, Math.Min(bufferSize, skip));
                        if (read == 0) throw new IOException();
                        skip -= read;
                    }
                    var i = 0;
                    while (i < 6)
                    {
                        var read = stream.Read(buffer, i, 6 - i);
                        if (read == 0) throw new IOException();
                        i += read;
                    }
                    var fileNameLength = buffer[2] | buffer[3] << 8;
                    var nameBuffer = new byte[fileNameLength];
                    i = 0;
                    while (i < fileNameLength)
                    {
                        var read = stream.Read(nameBuffer, i, fileNameLength - i);
                        if (read == 0) throw new IOException();
                        i += read;
                    }
                    var code = ImageLoader.GetCode(nameBuffer);
                    UseZipArchiveClass = code != Encoding.UTF8;

                    stream.Seek(0, SeekOrigin.Begin);
                }
                */

                return PackType.Zip;
            }
            else if (buffer[0] == 0x52 && buffer[1] == 0x61 && buffer[2] == 0x72 && buffer[3] == 0x21 && buffer[4] == 0x1A && buffer[5] == 0x07)
            {
                var buffer6 = buffer[6];
                if (buffer6 == 0x00)
                {
                    stream.Seek(-bufferSize, SeekOrigin.Current);
                    return PackType.Rar;
                }
                else if (buffer6 == 0x01 && buffer[7] == 0x00)
                {
                    stream.Seek(-bufferSize, SeekOrigin.Current);
                    return PackType.Rar5;
                }
                return PackType.Unknown;
            }
            else if (buffer[0] == 0x25 && buffer[1] == 0x50 && buffer[2] == 0x44 && buffer[3] == 0x46)
            {
                stream.Seek(-bufferSize, SeekOrigin.Current);
                return PackType.Pdf;
            }
            else if (buffer[0] == 0x37 && buffer[1] == 0x7A && buffer[2] == 0xBC && buffer[3] == 0xAF && buffer[4] == 0x27 && buffer[5] == 0x1C)
            {
                stream.Seek(-bufferSize, SeekOrigin.Current);
                return PackType.SevenZip;
            }
            else if (buffer[0] == 0x4D && buffer[1] == 0x53 && buffer[2] == 0x43 && buffer[3] == 0x46)
            {
                stream.Seek(-bufferSize, SeekOrigin.Current);
                return PackType.Cab;
            }
            /* Gz は非対応。対応させる場合ヘッダチェックにおける Lha との衝突に注意
            else if (fourByte.Take(2).SequenceEqual(new byte[2] { 0x1F, 0x8B }))
            {
                stream.Seek(-bufferSize, SeekOrigin.Current);
                return PackType.Gz;
            }*/
            else if (buffer[2] == 0x2D && buffer[3] == 0x6C)
            {
                stream.Seek(-bufferSize, SeekOrigin.Current);
                return PackType.Lha;
            }
            return PackType.Unknown;

            // ほとんどの場合に以下で TAR は判定可能だが、
            // 意図すれば TAR でありながら他のファイル形式のように見えるヘッダが作成可能
            /*
            else
            {

                var hundredByte = new byte[100];
                Array.Copy(fourByte, hundredByte, 4);
                stream.Read(hundredByte, 4, 100 - 4);
                var nullChar = hundredByte.ToList().IndexOf(0);

                if (nullChar >= 1)
                {
                    for (int i = nullChar + 1; i < 100; i++)
                    {
                        if (hundredByte[i] != 0)
                        {
                            return PackType.Unknown;
                        }
                    }
                }

                stream.Seek(-100, SeekOrigin.Current);
                return PackType.Tar;
            }
            */
        }

        /*
        private static PackType GetTypeFromStreamAndSeekFirst(Stream stream)
        {
            var fourByte = new byte[4];
            stream.Read(fourByte, 0, 4);
            if (fourByte[0] == 0x50 && fourByte[1] == 0x4B && (fourByte[2] == 0x03 && fourByte[3] == 0x04 || fourByte[2] == 0x07 && fourByte[3] == 0x08))
            {
                stream.Seek(-4, SeekOrigin.Current);
                return PackType.Zip;
            }
            else if (fourByte.SequenceEqual(new byte[4] { 0x52, 0x61, 0x72, 0x21 }))
            {
                stream.Read(fourByte, 0, 4);
                if (fourByte[0] == 0x1A && fourByte[1] == 0x07)
                {
                    var fourByte2 = fourByte[2];
                    if (fourByte2 == 0x00)
                    {
                        stream.Seek(-8, SeekOrigin.Current);
                        return PackType.Rar;
                    }
                    else if (fourByte2 == 0x01 && fourByte[3] == 0x00)
                    {
                        stream.Seek(-8, SeekOrigin.Current);
                        return PackType.Rar5;
                    }
                }
                return PackType.Unknown;
            }
            else if (fourByte.SequenceEqual(new byte[4] { 0x25, 0x50, 0x44, 0x46 }))
            {
                stream.Seek(-4, SeekOrigin.Current);
                return PackType.Pdf;
            }
            else if (fourByte.SequenceEqual(new byte[4] { 0x37, 0x7A, 0xBC, 0xAF }))// 残り2バイト 0x27 0x1C のチェックは SharpCompress に任せる
            {
                stream.Seek(-4, SeekOrigin.Current);
                return PackType.SevenZip;
            }
            else if (fourByte.SequenceEqual(new byte[] { 0x4D, 0x53, 0x43, 0x46 }))
            {
                stream.Seek(-4, SeekOrigin.Current);
                return PackType.Cab;
            }
           ／* Gz は非対応。対応させる場合ヘッダチェックにおける Lha との衝突に注意
            else if (fourByte.Take(2).SequenceEqual(new byte[2] { 0x1F, 0x8B }))
            {
                stream.Seek(-4, SeekOrigin.Current);
                return PackType.Gz;
            }*／
            else if (fourByte[2] == 0x2D && fourByte[3] == 0x6C)
            {
                stream.Seek(-4, SeekOrigin.Current);
                return PackType.Lha;
            }
            return PackType.Unknown;

            // ほとんどの場合に以下で TAR は判定可能だが、
            // 意図すれば TAR でありながら他のファイル形式のように見えるヘッダが作成可能
            ／*
            else
            {

                var hundredByte = new byte[100];
                Array.Copy(fourByte, hundredByte, 4);
                stream.Read(hundredByte, 4, 100 - 4);
                var nullChar = hundredByte.ToList().IndexOf(0);

                if (nullChar >= 1)
                {
                    for (int i = nullChar + 1; i < 100; i++)
                    {
                        if (hundredByte[i] != 0)
                        {
                            return PackType.Unknown;
                        }
                    }
                }

                stream.Seek(-100, SeekOrigin.Current);
                return PackType.Tar;
            }
            *／
        }
    */

        private bool Disposed = false;

        public void Dispose()
        {
            if (!Disposed)
            {
                if (entriesImageList != null)
                {
                    foreach (var s in entriesImageList)
                    {
                        if (s != null)
                        {
                            try
                            {
                                s.Dispose();
                            }
                            catch { }
                        }
                    }
                }

                if (entriesStreamList != null)
                {
                    foreach (var s in entriesStreamList)
                    {
                        if (s != null)
                        {
                            try
                            {
                                s.Close();
                                s.Dispose();
                            }
                            catch { }
                        }
                    }
                }

                if (entriesStreamListForImage != null)
                {
                    foreach (var s in entriesStreamListForImage)
                    {
                        if (s != null)
                        {
                            try
                            {
                                s.Close();
                                s.Dispose();
                            }
                            catch { }
                        }
                    }
                }

                if (zipArchive != null) zipArchive.Dispose();
                if (iArchive != null) iArchive.Dispose();
                if (sevenZipArchive != null) sevenZipArchive.Dispose();
                if (pdfArchive != null) pdfArchive.Dispose();

                if (iReader != null)
                {
                    try
                    {
                        iReader.Dispose();
                    }
                    catch { }
                }

                if (stream != null)
                {
                    stream.Close();
                    stream.Dispose();
                    stream = null;
                }
                Disposed = true;
            }
        }
        
        ~PackedImageLoader()
        {
            Dispose();
        }

        private class MyRandomAccessSource : iTextSharp.text.io.IRandomAccessSource
        {
            private readonly Stream raf;
            private readonly long length;
            public MyRandomAccessSource(Stream raf)
            {
                this.raf = raf;
                length = raf.Length;
            }
            public virtual int Get(long position)
            {
                if (position > length)
                    return -1;

                // Not thread safe!
                if (raf.Position != position)
                    raf.Seek(position, SeekOrigin.Begin);

                return raf.ReadByte();
            }
            public virtual int Get(long position, byte[] bytes, int off, int len)
            {
                if (position > length)
                    return -1;

                // Not thread safe!
                if (raf.Position != position)
                    raf.Seek(position, SeekOrigin.Begin);

                int n = raf.Read(bytes, off, len);
                return n == 0 ? -1 : n; //in .NET Streams return 0 on EOF, not -1
            }
            public virtual long Length
            {
                get
                {
                    return length;
                }
            }
            public virtual void Close()
            {
                raf.Close();
            }
            virtual public void Dispose()
            {
                Close();
            }
        }
    }

    public class SeekableStream : Stream
    {
        public static Stream Seekablize(Stream originalStream, long streamLength, bool leaveOpen = false) =>
            originalStream.CanSeek ? originalStream : new SeekableStream(originalStream, streamLength, leaveOpen);

        private readonly bool leaveOpen;
        private readonly Stream originalStream;
        private readonly MemoryStream buffStream;
        private readonly long streamLength;
        private long positionLookahead;
        private bool stopBuffering = false;
        private bool bufferDisposed = false;
        private SeekableStream(Stream unseekableStream, long streamLength, bool leaveOpen = false)
        {
            originalStream = unseekableStream;
            this.streamLength = streamLength;
            bufferSize = (int)Math.Min(streamLength, maxBufferSize);
            buffStream = new MemoryStream();
            this.leaveOpen = leaveOpen;
        }

        public override bool CanRead => originalStream.CanRead;
        public override bool CanSeek => !stopBuffering;
        public override bool CanWrite => false;
        public override long Length => streamLength;
        public override long Position
        {
            get => bufferDisposed ? originalStream.Position : buffStream.Position + positionLookahead;
            set => Seek(value, SeekOrigin.Begin);
        }
        public override void Flush() => originalStream.Flush();
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (bufferDisposed)
            {
                return originalStream.Read(buffer, offset, count);
            }
            var buffPosition = buffStream.Position;
            var loadedPosition = buffStream.Length;
            int result;
            if (buffPosition < loadedPosition)
            {
                var fromBuffCount = Math.Min(count, (int)(loadedPosition - buffPosition));
                result = buffStream.Read(buffer, offset, fromBuffCount);
                offset += result;
                count -= result;
            }
            else
            {
                result = 0;
                if (positionLookahead > 0)
                {
                    CopyStream(originalStream, stopBuffering ? null : buffStream, positionLookahead);
                    positionLookahead = 0;
                }
            }
            if (stopBuffering)
            {
                if (buffPosition + result >= loadedPosition)
                {
                    buffStream.Dispose();
                    bufferDisposed = true;
                    if (count > 0) result += originalStream.Read(buffer, offset, count);
                }
            }
            else
            {
                if (count > 0)
                {
                    var read = originalStream.Read(buffer, offset, count);
                    buffStream.Write(buffer, offset, read);
                    result += read;
                }
            }
            return result;
        }

        private bool CopyStream_bufferEmpty = true;
        private byte[] CopyStream_buffer;
        private const int maxBufferSize = 81920;
        private readonly int bufferSize;
        private void CopyStream(Stream src, Stream dst, long count)
        {
            if (CopyStream_bufferEmpty)
            {
                CopyStream_buffer = new byte[bufferSize];
                CopyStream_bufferEmpty = false;
            }
            if (dst == null)
            {
                while (count > 0)
                {
                    var read = src.Read(CopyStream_buffer, 0, (int)Math.Min(count, bufferSize));
                    if (read > 0)
                    {
                        count -= read;
                    }
                    else
                    {
                        throw new IOException();
                    }
                }
            }
            else
            {
                while (count > 0)
                {
                    var read = src.Read(CopyStream_buffer, 0, (int)Math.Min(count, bufferSize));
                    if (read > 0)
                    {
                        dst.Write(CopyStream_buffer, 0, read);
                        count -= read;
                    }
                    else
                    {
                        throw new IOException();
                    }
                }
            }
        }
        
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (bufferDisposed)
            {
                return originalStream.Seek(offset, origin);
            }
            switch (origin)
            {
                case SeekOrigin.Begin:
                    {
                        var loadedPosition = buffStream.Length;
                        if (offset <= loadedPosition)
                        {
                            positionLookahead = 0;
                            return buffStream.Seek(offset, SeekOrigin.Begin);
                        }
                        else
                        {
                            buffStream.Seek(0, SeekOrigin.End);
                            if (offset > streamLength) offset = streamLength;
                            positionLookahead = offset - loadedPosition;
                            return offset;
                        }
                    }
                case SeekOrigin.Current: return Seek(Position + offset, SeekOrigin.Begin);
                case SeekOrigin.End: return Seek(streamLength + offset, SeekOrigin.Begin);
                default: throw new ArgumentException();
            }
        }

        public override void SetLength(long value) => throw new NotImplementedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        public void StopBuffering() => stopBuffering = true;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!leaveOpen) originalStream.Dispose();
                if (!bufferDisposed)
                {
                    buffStream.Dispose();
                    bufferDisposed = true;
                }

            }
            base.Dispose(disposing);
        }
    }

    public class ReleasableStream : Stream
    {
        readonly object thisLock = new object();
        bool isReleased = false;
        public bool IsReleased => isReleased;
        Stream stream;
        public ReleasableStream(Stream originalStream)
        {
            stream = originalStream;
        }

        public void Release()
        {
            if (!isReleased)
            {
                var temp = null as Stream;
                lock (thisLock)
                {
                    if (!isReleased)
                    {
                        var memoryStream = new MemoryStream();
                        try
                        {
                            var position = stream.Position;
                            if (position > 0) stream.Seek(0, SeekOrigin.Begin);
                            try
                            {
                                stream.CopyTo(memoryStream);
                                memoryStream.Seek(position, SeekOrigin.Begin);
                            }
                            catch
                            {
                                stream.Seek(position, SeekOrigin.Begin);
                                throw;
                            }
                        }
                        catch
                        {
                            memoryStream.Dispose();
                            throw;
                        }
                        temp = stream;
                        stream = memoryStream;
                        isReleased = true;
                    }
                }
                temp?.Dispose();
            }
        }

        public override bool CanRead{ get { if (isReleased) return stream.CanRead; lock(thisLock) return stream.CanRead; } }

        public override bool CanSeek { get { if(isReleased) return stream.CanSeek; lock(thisLock) return stream.CanSeek; } }

        public override bool CanWrite { get { if (isReleased) return stream.CanWrite; lock (thisLock) return stream.CanWrite; } }

        public override long Length { get { if (isReleased) return stream.Length; lock (thisLock) return stream.Length; } }

        public override long Position
        {
            get { if (isReleased) return stream.Position; lock (thisLock) return stream.Position; }
            set { if (isReleased) stream.Position = value; else lock (thisLock) stream.Position = value; }
        }

        public override void Flush() { if (isReleased) stream.Flush(); else lock(thisLock) stream.Flush(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (isReleased) return stream.Read(buffer, offset, count); lock (thisLock) return stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (isReleased) return stream.Seek(offset, origin); lock (thisLock) return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            if (isReleased) stream.SetLength(value); else lock (thisLock) stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (isReleased) stream.Write(buffer, offset, count); else lock (thisLock) stream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (stream != null)
                {
                    if (isReleased)
                    {
                        stream?.Dispose();
                        stream = null;
                    }
                    else
                    {
                        lock (thisLock)
                        {
                            stream?.Dispose();
                            stream = null;
                            isReleased = true;
                        }
                    }
                }
            }
            base.Dispose(disposing);
        }
    }
}
