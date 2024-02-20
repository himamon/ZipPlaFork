using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace ZipPla
{
    // サムネイルデータの仕様説明のためのクラス
    // エラー処理は行わない。実際には使わない。リリース版ではコンパイルしない。
    // そもそも無条件に全ての画像を読み込むような使い方は想定していない。
#if DEBUG
    class ZipPlaThumbnail
    {
        public uint Version;
        public DateTime LastWriteTime;
        public int Index;
        public byte[] GeneralPurposeData;
        public List<Size> ReducedSize = new List<Size>();
        public List<Fraction> CacheSAR = new List<Fraction>();
        public List<Image> ThumbnailImage = new List<Image>();

        public ZipPlaThumbnail(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                // サムネイルファイルの仕様のバージョン。現在は 1
                Version = reader.ReadUInt32();

                // 元データの更新日時。サムネイルが最新のものであるかの判定に使う
                LastWriteTime = new DateTime(reader.ReadInt64());

                // サムネイル画像を指定するための数値。書籍のページ数、動画の秒数等。-1 で未指定を表す
                Index = reader.ReadInt32();

                // サムネイル以外の情報。書籍の全ページ数、画像のサイズや色数、動画の長さ等
                // 現在の ZipPla では generalPurposeDataSize が負の数や 128 を超える数になった場合不正なデータとみなす
                var generalPurposeDataSize = reader.ReadInt32();
                GeneralPurposeData = reader.ReadBytes(generalPurposeDataSize);

                // サムネイル画像。小さい方から順に格納される
                // ここに必要なサイズが見つかればそれを利用し、
                // 見つからなければ元データらからサムネイルを作成してここに加える
                // 読み込んだデータを再度サイズ調整する際に高域の周波数成分にロスが生じることを想定し
                // 実際に必要な最大のサイズよりも 1.4 倍程度余裕を持った画像を利用する
                while (stream.Position < stream.Length)
                {
                    // 縮小後の画像サイズ。等倍の場合に限り width == -1, height == -1 となる
                    // 等倍のキャッシュは、動画データのように等倍のサムネイルデータを読み込むコストが
                    // 元データ全てを読み込むコストに比べて著しく低いと認められる場合に作成される
                    var width = reader.ReadInt32();
                    var height = reader.ReadInt32();
                    ReducedSize.Add(width > 0 && height > 0 ? new Size(width, height) : Size.Empty);

                    // キャッシュデータのピクセルの横÷縦の比。
                    // 約分されていなくても読み込めるが、ZipPla が作成するデータは全て約分済み
                    // 現在の実装では ReducedSize が Size.Empty である場合のみ比が 1 以外になるようにデータが作成される
                    // この条件が満たされないとキャッシュデータ読み込み時に必要サイズが正しく評価されない
                    CacheSAR.Add(new Fraction(numerator: reader.ReadInt32(), denominator: reader.ReadInt32()));

                    // 画像データ
                    // ヘッダを含めた完全なデータを格納
                    // 現在は JPEG XR を使用しているため実際には Image.FromStream(memoryStream) は使用できない
                    // 画像サイズに余裕を持たせるため高域の周波数成分を持つノイズは問題にならないので
                    // 比較的圧縮率の高い非可逆圧縮を利用する
                    var imageDataSize = reader.ReadUInt64();
                    var imageData = reader.ReadBytes((int)imageDataSize);
                    using (var memoryStream = new MemoryStream(imageData))
                    {
                        ThumbnailImage.Add(Image.FromStream(memoryStream));
                    }
                }
            }
        }
    }
#endif

    // NTFS でないときのエラーによるペナルティ、NTFS であるかをチェックするためのコスト、NTFS 以外が対象になる頻度を
    // 総合的に考慮し、NTFS であるかどうかのチェックは行わない
    public class GPSizeThumbnail
    {
        public const string AlternateDataStream = "<ADS>";
        public const string STREAM_NAME = "ZipPla.Thumbnail"; // "$ZIPPLA_THUMBNAIL";
        public const string CACHE_FILE_SUFFIX = "." + STREAM_NAME; // "$ZIPPLA_THUMBNAIL";
        // 将来のファイルシステムで代替データストリームとの互換性を考慮した機能が追加された場合
        // 使用頻度の高い Zone.Identifier と同様の書式にしておくことでその恩恵を受けられることが期待できる
        private const double SIZE_MARGIN = 1.4142135623730950488; // Lanczos3 の周波数特性より、完全再現するならこのレベルは必要
        private const int COMMON_RATIO = 2;
        private const int AREA_UBOUND = 160 * 120; // JPEG のサムネイルサイズ
        
        //private const long JPEG_QUALITY = 90; // 最低でも SIZE_MARGIN だけ縮小されるのでモスキートノイズは気にならない
        private const byte JXR_QUALITY = 30; // 大きい方が高圧縮、JPEG の 90 の半分程度に調整、255 まで設定できるが 100 程度を超えると正常に圧縮されない
        private const int DATA_UBOUND = 128; // 安全のため。増やしてもバージョンを変える必要はない
        private const uint VERSION = 2;
        // バージョン(uint)、更新日時(long)、インデックス(int)、
        // 汎用データ長(int)、汎用データ、
        // 以下縮小後幅(uint)、拡大縮小後高さ(uint)、縮小後ピクセル比横（int）、縮小後ピクセル比縦（int）、ファイルサイズ(long)、実体、の繰り返し
        // ただしオリジナルサイズは高さ・幅共 uint.MaxValue で表す
        //
        // ファイルサイズ情報を含めないことはフォルダに対してもサムネイル情報を付加する前提から正当化される
        // 
        // バージョン履歴（概要）
        // 0: 初期
        // 1: 内部フォーマットを JPEG → JPEG XR に
        // 2: アスペクト比を保存するように

        private static readonly ConcurrentDictionary<string, DateTime> editingInfo = new ConcurrentDictionary<string, DateTime>();
        public static void ClearBlackList()
        {
            editingInfo.Clear();
        }
        public static void EnterEditing(string path)
        {
            var pathInLower = path.ToLower();
            editingInfo[pathInLower] = DateTime.MaxValue;
        }
        public static void ExitedEditing(string path)
        {
            var pathInLower = path.ToLower();
            try
            {
                //editingInfo[pathInLower] = File.GetLastWriteTime(path);
                editingInfo[pathInLower] = File.GetLastAccessTime(path);
            }
            catch { }
        }
        public static bool Editing(string path)
        {
            var pathInLower = path.ToLower();
            DateTime time;
            try
            {
                //return editingInfo.TryGetValue(pathInLower, out time) && File.GetLastWriteTime(path) <= time;
                return editingInfo.TryGetValue(pathInLower, out time) && File.GetLastAccessTime(path) <= time;
            }
            catch
            {
                return false;
            }
        }



        /*
    public static void ClearBlackList()
    {
        lock (blackList)
        {
            blackList.Clear();
        }
    }

    private static readonly Dictionary<string, int> blackList = new Dictionary<string, int>(); // スレッドセーフでない
    public static bool Editing(string path, int weightDecrease = 1)
    {
        //return path != null && blackList.Contains(path.ToLower());
        if (path != null)
        {
            var lower = path.ToLower();
            int weight;
            lock (blackList)
            {
                if (blackList.TryGetValue(lower, out weight))
                {
                    if (weight > weightDecrease)
                    {
                        if (weightDecrease > 0) blackList[lower] = weight - weightDecrease;
                    }
                    else
                    {
                        blackList.Remove(lower);
                    }
                    return true;
                }
            }
        }
        return false;
    }

    public static void AddBlackListWeight(string path, int weightIncrease = 1)
    {
        if (path != null)
        {
            var lower = path.ToLower();
            int weight;
            lock (blackList)
            {
                if (blackList.TryGetValue(lower, out weight))
                {
                    blackList[lower] = weight + weightIncrease;
                }
                else
                {
                    blackList[lower] = weightIncrease;
                }
            }
        }
    }
    */

        /*
        public static void fileCacheTest()
        {
            MessageBox.Show(GetCommonFolderRoot(@"\\localhost\aa\bc\d"));
        }
        */

        private static bool IsPathRooted(string path)
        {
            return Path.IsPathRooted(path) || path.StartsWith(@"\\");
        }

        private static string RemoveRootedString(string path)
        {
            var result = new StringBuilder(path.Length - 1); // 多くの場合に一文字だけ削られる
            bool start = false;
            foreach (var c in path)
            {
                switch (c)
                {
                    case ':': break;
                    default:
                        if (c == Path.DirectorySeparatorChar)
                        {
                            if(start) result.Append(c);
                        }
                        else
                        {
                            result.Append(c);
                            start = true;
                        }
                        break;
                }
            }
            return result.ToString();
        }

        private static string GetCachePath(string filePath, string cacheRoot)
        {
            if (IsPathRooted(cacheRoot))
            {
                return Path.Combine(cacheRoot, RemoveRootedString(filePath) + CACHE_FILE_SUFFIX);
            }
            else
            {
                cacheRoot = GetFullPath(Path.GetDirectoryName(filePath), cacheRoot);
                return Path.Combine(cacheRoot, Path.GetFileName(filePath) + CACHE_FILE_SUFFIX);
            }
        }

        /*
        private static string GetRelativePath(string current, string target)
        {
            var cDirs = current.Split(Path.DirectorySeparatorChar);
            var tDirs = target.Split(Path.DirectorySeparatorChar);
            var cLen = cDirs.Length;
            var len = Math.Min(cLen, tDirs.Length);
            var i = 0;
            var p = 0;
            while (i < len && cDirs[i] == tDirs[i]) p += tDirs[i++].Length + 1;
            var up = "";
            while (i++ < cLen) up += ".." + Path.DirectorySeparatorChar;
            return up + target.Substring(p);
        }
        */

        private static string GetFullPath(string current, string target)
        {
            if (current.Length > 0 && current.Last() != Path.DirectorySeparatorChar) current += Path.DirectorySeparatorChar;
            return new Uri(new Uri(current), target).LocalPath;
        }

        private static void CreateDirectoryWithItsParents(string target)
        {
            var stack = new Stack<string>();
            while (!Directory.Exists(target))
            {
                stack.Push(target);
                var t2 = Path.GetDirectoryName(target);
                if (string.IsNullOrEmpty(t2) || t2 == target) throw new DirectoryNotFoundException();
                target = t2;
            }
            while (stack.Count > 0) Directory.CreateDirectory(stack.Pop());
        }
        

        private static void DeleteParents(string alreadyDeletedTarget, string keep)
        {
            alreadyDeletedTarget = Path.GetDirectoryName(alreadyDeletedTarget);
            while (!keep.StartsWith(alreadyDeletedTarget))
            {
                if (ContainsAnyEntry(alreadyDeletedTarget)) return;
                Directory.Delete(alreadyDeletedTarget);
                alreadyDeletedTarget = Path.GetDirectoryName(alreadyDeletedTarget);
            }
        }

        private static bool ContainsAnyEntry(string dirPath)
        {
            return Directory.EnumerateFileSystemEntries(dirPath).Any();
        }

        public static bool TryGetWithIgnoredIndex(string cacheRoot, string fileName, out byte[] gpData)
        {
            return TryGet(cacheRoot, fileName, null, out gpData);
        }
        public static bool TryGet(string cacheRoot, string fileName, int? index, out byte[] gpData)
        {
            gpData = null;
            if (cacheRoot == AlternateDataStream)
            {
                if (!NTFSMultiStream.Exists(fileName, STREAM_NAME)) return false;
                try
                {
                    var lastWriteTime = File.GetLastWriteTime(fileName);
                    using (var s = new NTFSMultiStream(fileName, STREAM_NAME, FileAccess.Read))
                    using (var r = new BinaryReader(s))
                    {
                        getHeaderData(r, index, lastWriteTime, out gpData);
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                try
                {
                    var cachePath = GetCachePath(fileName, cacheRoot);
                    if (!File.Exists(cachePath)) return false;
                    var lastWriteTime = File.GetLastWriteTime(fileName);
                    using (var s = new FileStream(cachePath, FileMode.Open, FileAccess.Read))
                    using (var r = new BinaryReader(s))
                    {
                        getHeaderData(r, index, lastWriteTime, out gpData);
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        private static void getHeaderData(BinaryReader r, int? index, DateTime lastWriteTime, out byte[] gpData)
        {
            if (r.ReadUInt32() != VERSION) throw new Exception();
            if (r.ReadInt64() != lastWriteTime.Ticks) throw new Exception();
            var indexOfData = r.ReadInt32();
            if (index != null && index != indexOfData) throw new Exception();
            var dataLength = r.ReadInt32();
            if (dataLength < 0 || dataLength > DATA_UBOUND) throw new Exception();

            gpData = r.ReadBytes(dataLength);
            if (gpData.Length != dataLength)
            {
                gpData = null;
                throw new Exception();
            }
        }

        public static bool TryGet(string cacheRoot, string fileName, Size requiredSize, bool letterBox, out Bitmap thumbnail, out byte[] gpData)
        {
            return TryGet(cacheRoot, fileName, -1, requiredSize, letterBox, out thumbnail, out gpData);
        }

        public static bool TryGet(string cacheRoot, string fileName, int index, Size requiredSize, bool letterBox, out Bitmap thumbnail, out byte[] gpData)
        {
            Fraction dummy;
            return TryGet(cacheRoot, fileName, index, requiredSize, letterBox, out thumbnail, out dummy, true, out gpData);
        }

        public static bool TryGet(string cacheRoot, string fileName, int index, Size requiredSize, bool letterBox, out Bitmap thumbnail, out Fraction pixelRatio, out byte[] gpData)
        {
            return TryGet(cacheRoot, fileName, index, requiredSize, letterBox, out thumbnail, out pixelRatio, false, out gpData);
        }

        private static bool TryGet(string cacheRoot, string fileName, int index, Size requiredSize, bool letterBox, out Bitmap thumbnail, out Fraction pixelRatio, bool pixelRatioMustBe1, out byte[] gpData)
        {
            thumbnail = null;
            gpData = null;
            pixelRatio = 0;

            var ads = cacheRoot == AlternateDataStream;
            var cachePath = ads ? null : GetCachePath(fileName, cacheRoot);
            if (ads && !NTFSMultiStream.Exists(fileName, STREAM_NAME) || !ads && !File.Exists(cachePath))
            {
                return false;
            }
            try
            {
                var lastWriteTime = File.GetLastWriteTime(fileName);
                using (var s = ads ? new NTFSMultiStream(fileName, STREAM_NAME, FileAccess.Read) : new FileStream(cachePath, FileMode.Open, FileAccess.Read) as Stream)
                using (var r = new BinaryReader(s))
                {
                    var fileSize = s.Length;

                    getHeaderData(r, index, lastWriteTime, out gpData);

                    /*
                    //MessageBox.Show($"{fileSize}");
                    var dataVersion = r.ReadUInt32();
                    if (dataVersion != VERSION) return false;
                    if (r.ReadInt64() != lastWriteTime.Ticks) return false;
                    if (r.ReadInt32() != index) return false;
                    var dataLength = r.ReadInt32();
                    if (dataLength < 0 || dataLength > DATA_UBOUND) return false;

                    gpData = r.ReadBytes(dataLength);
                    if (gpData.Length != dataLength)
                    {
                        gpData = null;
                        return false;
                    }
                    */
                    
                    fileSize -= (4 + 8 + 4 + 4) + gpData.Length;

                    var requiredWidthWithMargin = (int)Math.Round(requiredSize.Width * SIZE_MARGIN);
                    var requiredHeightWithMargin = (int)Math.Round(requiredSize.Height * SIZE_MARGIN);

                    while (fileSize > 0)
                    {
                        var width = r.ReadUInt32();
                        var height = r.ReadUInt32();
                        var numerator = r.ReadInt32(); if (numerator <= 0) return false;
                        var denominator = r.ReadInt32(); if (denominator <= 0) return false;
                        if (pixelRatioMustBe1 && numerator != denominator) return false;
                        var length = (int)r.ReadInt64();
                        if (length <= 0) return false;
                        if (
                            // pixelRatio が 1 でないのは width と height が MaxValue のときに限るので考慮する必要はない
                            // pixelRatio が 1 でないモニタなどを想定して縮小された異方性キャッシュを考える場合は
                            // width と height の一方を減らして実効的な解像度を取得すればよいが、
                            // その際は TrySet で作成されるものが TryGet で受け入れられるようによく検討する必要がある
                            // TrySet に失敗するのは良いが、成功したのに TryGet が受け入れないと無限ループの危険がある
                            letterBox && (width >= requiredWidthWithMargin || height >= requiredHeightWithMargin) || 
                            !letterBox && width >= requiredWidthWithMargin && height >= requiredHeightWithMargin
                            )
                        {
                            var buf = r.ReadBytes(length);
                            using (var m = new MemoryStream(buf))
                            {
                                thumbnail = WindowsMediaPhoto.Load(m); //new Bitmap(m);
                            }
                            pixelRatio = new Fraction(numerator, denominator);
                            break;
                        }
                        else
                        {
                            s.Seek(length, SeekOrigin.Current);
                        }
                        fileSize -= (4 + 4 + 4 + 4 + 8) + length;
                    }
                }
                //MessageBox.Show($"{thumbnail.Width}x{thumbnail.Height}");
                return thumbnail != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryDeleteAlternateDataStream(string fileName)
        {
            if (!NTFSMultiStream.Exists(fileName, STREAM_NAME)) return false;
            var readOnly = false;
            FileAttributes att = default(FileAttributes);
            try
            {
                return editFile(fileName, func: () => // 代替ストリームの削除は deleted イベントを起こさない
                {
                    att = File.GetAttributes(fileName);
                    readOnly = (att & FileAttributes.ReadOnly) == FileAttributes.ReadOnly; ;
                    if (readOnly)
                    {
                        readOnly = false;
                        File.SetAttributes(fileName, att & ~FileAttributes.ReadOnly);
                        readOnly = true;
                    }
                    return NTFSMultiStream.Delete(fileName, STREAM_NAME);
                });
            }
            catch
            {
                return false;
            }
            finally
            {
                if(readOnly)
                {
                    if (readOnly) File.SetAttributes(fileName, att);
                }
            }
        }

        public static void TryMove(string cacheRoot, string oldPath, string newPath)
        {
            if (cacheRoot == null || cacheRoot == AlternateDataStream) return;
            try
            {
                var cachePathOld = GetCachePath(oldPath, cacheRoot);
                if(File.Exists(cachePathOld))
                {
                    if (newPath == null)
                    {
                        File.Delete(cachePathOld);
                    }
                    else
                    {
                        var cachePathNew = GetCachePath(newPath, cacheRoot);
                        if (File.Exists(cachePathNew)) File.Delete(cachePathNew);
                        File.Move(cachePathOld, cachePathNew);
                    }
                    if (Path.IsPathRooted(cacheRoot) && cachePathOld.StartsWith(cacheRoot))
                    {
                        DeleteParents(cachePathOld, cacheRoot);
                    }
                }
            }
            catch
            {
            }
        }

        public static void TryDelete(string cacheRoot, string path)
        {
            TryMove(cacheRoot, path, null);
        }

        private struct SizeD
        {
            public double Width, Height;

            public SizeD(double width, double height)
            {
                Width = width;
                Height = height;
            }

            public static implicit operator SizeD(Size size)
            {
                return new SizeD(size.Width, size.Height);
            }
            
            public static SizeD operator *(SizeD sizeD, double x)
            {
                return new SizeD(sizeD.Width * x, sizeD.Height * x);
            }

            public static SizeD operator *(double x, SizeD sizeD)
            {
                return new SizeD(sizeD.Width * x, sizeD.Height * x);
            }

            public static SizeD operator /(SizeD sizeD, double x)
            {
                return new SizeD(sizeD.Width / x, sizeD.Height / x);
            }

            public Size Round()
            {
                return new Size((int)Math.Round(Width), (int)Math.Round(Height));
            }

            public bool LessThanOrEqualTo(Size providedSize, bool letterBox)
            {
                if(letterBox)
                {
                    return Width <= providedSize.Width || Height <= providedSize.Height;
                }
                else
                {
                    return Width <= providedSize.Width && Height <= providedSize.Height;
                }
            }
        }

        private static SizeD getEffectiveSize(Size size, Fraction pixelRatio)
        {
            double effectiveWidth, effectiveHeight;
            if (pixelRatio > 1)
            {
                effectiveWidth = size.Width;
                effectiveHeight = size.Height / (double)pixelRatio;
            }
            else
            {
                effectiveWidth = size.Width * (double)pixelRatio;
                effectiveHeight = size.Height;
            }
            return new SizeD(effectiveWidth, effectiveHeight);
        }

        private static readonly char DirectorySeparatorChar = Path.DirectorySeparatorChar;
        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
        private static string GetCommonFolderRoot(string commonFolderPath)
        {
            if (commonFolderPath != null && commonFolderPath.Length >= 5 && commonFolderPath[0] == DirectorySeparatorChar &&
               commonFolderPath[1] == DirectorySeparatorChar && !InvalidFileNameChars.Contains(commonFolderPath[2]))
            {
                var index = 3;
                var count = 0;
                char c = '\0';
                while (index < commonFolderPath.Length)
                {
                    c = commonFolderPath[index];
                    if (c == DirectorySeparatorChar)
                    {
                        count++;
                        if (count == 2)
                        {
                            return commonFolderPath.Substring(0, index);
                        }
                    }
                    else if (InvalidFileNameChars.Contains(c)) return null;
                    index++;
                }
                if (count == 1 && c != DirectorySeparatorChar) return commonFolderPath;
            }
            return null;
        }
        
        private static bool TryTestAdsSave(string path)
        {
            bool result;
            const string testStreamFileName = "testStream";
            if (NTFSMultiStream.Exists(path, testStreamFileName))
            {
                result = true;
            }
            else
            {
                var att = File.GetAttributes(path);
                var readOnly = (att & FileAttributes.ReadOnly) == FileAttributes.ReadOnly;
                try
                {
                    if (readOnly)
                    {
                        readOnly = false;
                        File.SetAttributes(path, att & ~FileAttributes.ReadOnly);
                        readOnly = true;
                    }
                    var writeTime = File.GetLastWriteTime(path);
                    var successWrite = false;
                    try
                    {
                        NTFSMultiStream.WriteFile(path, testStreamFileName, new byte[] { });
                        successWrite = true;
                    }
                    catch { }
                    try
                    {
                        if (NTFSMultiStream.Exists(path, testStreamFileName))
                        {
                            NTFSMultiStream.Delete(path, testStreamFileName);
                            result = successWrite;
                        }
                        else
                        {
                            result = false;
                        }
                    }
                    finally
                    {
                        if (writeTime != File.GetLastWriteTime(path))
                            File.SetLastWriteTime(path, writeTime);
                    }
                }
                finally
                {
                    if (readOnly) File.SetAttributes(path, att);
                }
            }
            return result;
        }

        // Dictionary はスレッドセーフでないため HashSet を使う
        private static HashSet<string> GetDriveFormatString_NTFSSet = new HashSet<string>();
        private static HashSet<string> GetDriveFormatString_NoNTFSSet = new HashSet<string>();
        private static string GetDriveFormatString(string driveName, bool checkNew = false)
        {
            var commonFolderRoot = GetCommonFolderRoot(driveName);
            if (commonFolderRoot != null)
            {
                if (!checkNew)
                {
                    if (GetDriveFormatString_NTFSSet.Contains(commonFolderRoot))
                    {
                        return "NTFS";
                    }
                    else if (GetDriveFormatString_NoNTFSSet.Contains(commonFolderRoot))
                    {
                        return "UNKNOWN";
                    }
                }
                else
                {
                    GetDriveFormatString_NTFSSet.Remove(commonFolderRoot);
                    GetDriveFormatString_NoNTFSSet.Remove(commonFolderRoot);
                }

                // commonFolderRoot が ADS を持たないがその子が持つケースが存在する場合
                // \\localhost\D 等はこれに該当しないためコメントアウト
                //var testTarget = commonFolderRoot != driveName ? driveName : (new DirectoryInfo(driveName).EnumerateFileSystemInfos())
                //    .FirstOrDefault(info => (info.Attributes & FileAttributes.System) != FileAttributes.System)?.FullName;
                //if (testTarget == null) return null;

                var testTarget = commonFolderRoot;
                
                if (TryTestAdsSave(testTarget))
                {
                    GetDriveFormatString_NTFSSet.Add(commonFolderRoot);
                    return "NTFS";
                }
                else
                {
                    GetDriveFormatString_NoNTFSSet.Add(commonFolderRoot);
                    return "UNKNOWN";
                }
            }
            return new DriveInfo(driveName).DriveFormat;
        }
        
        public static long SupportADSSize(string driveName, bool checkNew)
        {
            var driveFormatString = GetDriveFormatString(driveName, checkNew);

            // GetDriveFormatString が取得できないケースは commonFolderRoot が空なのでチェックする必要がない
            //if (driveFormatString == null) return long.MaxValue;

            return SupportADSSizeFromFormatString(driveFormatString);
        }
        private static long SupportADSSizeFromFormatString(string formatString)
        {
            // ReFS ではサポートされないという記述
            // https://blogs.msdn.microsoft.com/b8_ja/2012/01/20/windows-refs/

            // 最新の ReFS ではサポートされるという記述
            // http://yamanxworld.blogspot.jp/2013/10/windows-server-2012-r2-refs.html

            // 公式でもクライアント OS で使う ReFS は代替データストリームに対応しているという記述がある
            // https://msdn.microsoft.com/ja-jp/library/hh831724(v=ws.11).aspx

            // ただし上にもあるが 128KB の制限付き。通常の使い方では概ねこれを超えることはないし超えたからと言ってエラーになるわけではない。
            // http://bakins-bits.com/wordpress/?p=140

            switch (formatString.ToUpper())
            {
                case "NTFS": return long.MaxValue;
                case "REFS": return 128 * 1000; // 余裕を持って 128 * 1024 にはしない
                default: return 0;
            }
        }
        

        //public static readonly ImageCodecInfo TrySet_ImageCodecInfo = GetEncoderInfo(ImageFormat.Jpeg);
        //public static readonly EncoderParameters TrySet_EncoderParameters = GetJpegEncoderParameters(JPEG_QUALITY);
        private static readonly HashSet<string> TrySet_Creating = new HashSet<string>(); // スレッドセーフ
        public static bool TrySet(string cachRoot, string fileName, Size requiredSize, bool letterBox, bool fullSizeCache, Bitmap thumbnail, byte[] gpData)
        {
            return TrySet(cachRoot, fileName, -1, requiredSize, letterBox, fullSizeCache, thumbnail, gpData);
        }
        public static bool TrySet(string cachRoot, string fileName, int index, Size requiredSize, bool letterBox, bool fullSizeCache, Bitmap thumbnail, byte[] gpData)
        {
            Bitmap dummy;
            Fraction dummy2;
            return TrySet(cachRoot, fileName, index, requiredSize, letterBox, fullSizeCache, thumbnail, 1, gpData, getBiggest: false,
                biggestBitmap: out dummy, biggestBitmapPixelRatio: out dummy2);
        }
        public static bool TrySet(string cachRoot, string fileName, int index, Size requiredSize, bool letterBox, bool fullSizeCache, Bitmap thumbnail, byte[] gpData, out Bitmap biggestBitmap)
        {
            Fraction dummy;
            return TrySet(cachRoot, fileName, index, requiredSize, letterBox, fullSizeCache, thumbnail, 1, gpData, getBiggest: true,
                biggestBitmap: out biggestBitmap, biggestBitmapPixelRatio: out dummy);
        }
        public static bool TrySet(string cachRoot, string fileName, int index, Size requiredSize, bool letterBox, bool fullSizeCache, Bitmap thumbnail,
            Fraction pixelRatio, byte[] gpData, out Bitmap biggestBitmap, out Fraction biggestBitmapPixelRatio)
        {
            return TrySet(cachRoot, fileName, index, requiredSize, letterBox, fullSizeCache, thumbnail, pixelRatio, gpData, getBiggest: true,
                biggestBitmap: out biggestBitmap, biggestBitmapPixelRatio: out biggestBitmapPixelRatio);
        }
        private static bool TrySet(string cacheRoot, string fileName, int index, Size requiredSize, bool letterBox, bool fullSizeCache, Bitmap thumbnail,
            Fraction pixelRatio, byte[] gpData, bool getBiggest, out Bitmap biggestBitmap, out Fraction biggestBitmapPixelRatio)
        {
            if (pixelRatio == 0) pixelRatio = 1;

            biggestBitmap = null;
            biggestBitmapPixelRatio = 0;
            //MemoryStream memoryStream = null;


            lock (TrySet_Creating) // 一番最初に
            {
                if (TrySet_Creating.Contains(fileName)) return false;
                TrySet_Creating.Add(fileName);
            }
            var readOnly = false;
            FileAttributes att = default(FileAttributes);
            try
            {
                var dataLength = gpData != null ? gpData.Length : 0;
                if (dataLength > DATA_UBOUND) return false;

                var ads = cacheRoot == AlternateDataStream;

                //var driveInfo = new DriveInfo(fileName);
                long sizeBound;
                if (ads)
                {
                    sizeBound = SupportADSSize(fileName, checkNew: false) - ((4 + 8 + 4 + 4) + dataLength);
                    if (sizeBound <= 0) return false;
                }
                else
                {
                    sizeBound = long.MaxValue;
                }
                
                Size[] resizedSize;
                Fraction[] resizedPixelRatio;
                if (!GetResizedInfo(requiredSize, letterBox, thumbnail.Size, pixelRatio, fullSizeCache, out resizedSize, out resizedPixelRatio)) return false;
                
                var maxSizeIndex = resizedSize.Length - 1;

                var maxSizeIsFullSize = resizedSize[maxSizeIndex] == thumbnail.Size;

                
                MemoryStream[] compressed = new MemoryStream[maxSizeIndex + 1];
                try
                {

                    // 一旦先にチェック
                    if (ads)
                    {
                        att = File.GetAttributes(fileName);
                        readOnly = (att & FileAttributes.ReadOnly) == FileAttributes.ReadOnly; ;
                        if (readOnly)
                        {
                            readOnly = false;
                            File.SetAttributes(fileName, att & ~FileAttributes.ReadOnly);
                            readOnly = true;
                        }
                    }

                    var isFile = File.Exists(fileName);
                    //string fileNameLowerCase = null;
                    DateTime lastWriteTime0 = default(DateTime);
                    if (ads)
                    {
                        /*
                        fileNameLowerCase = fileName.ToLower();
                        lock (blackList)
                        {
                            blackList[fileNameLowerCase] = 1;
                        }
                        */
                        EnterEditing(fileName);
                        try
                        {
                            // 積極的な例外発生
                            if (isFile)
                            {
                                lastWriteTime0 = File.GetLastWriteTime(fileName);
                                File.SetLastWriteTime(fileName, lastWriteTime0);
                            }
                            else
                            {
                                lastWriteTime0 = Directory.GetLastWriteTime(fileName);
                                Directory.SetLastWriteTime(fileName, lastWriteTime0);
                            }
                        }
                        finally
                        {
                            ExitedEditing(fileName);
                        }
                    }
                    
                    // 大きい方から順番に縮小していく
                    // 最初の一回以外の縮小のコストが大幅に削減される
                    // 量子化は統計上公比 √(1/4) の等比級数になるので問題にならない
                    // 周波数歪みも画像の公比と縮小アルゴリズムの周波数特性から問題にならない
                    var prevBitmap = thumbnail;
                    var dontDisposePrev = true;
                    for (var i = maxSizeIndex; i >= 0; i--)
                    {
                        sizeBound -= 4 + 4 + 4 + 4 + 8;
                        if (sizeBound <= 0)
                        {
                            if (!dontDisposePrev) prevBitmap.Dispose();
                            throw new Exception(); // compressed や biggestBitmap の Dispose などが必要なので
                        }
                        var resizedSizei = resizedSize[i];
                        var width = resizedSizei.Width;
                        var height = resizedSizei.Height;

                        var img = BitmapResizer.CreateNew(prevBitmap, width, height);
                        try
                        {
                            var m = new MemoryStream();
                            //img.Save(m, TrySet_ImageCodecInfo, TrySet_EncoderParameters);
                            WindowsMediaPhoto.Save(m, img, JXR_QUALITY);

                            if (sizeBound < long.MaxValue / 2)
                            {
                                sizeBound -= m.Position;
                                if (sizeBound < 0) // これで終わりの可能性があるので等号は含めない
                                {
                                    throw new Exception();
                                }
                            }
                            compressed[i] = m;
                        }
                        catch
                        {
                            img.Dispose();
                            if (!dontDisposePrev) prevBitmap.Dispose();
                            throw;
                        }

                        /*
                        var img = new Bitmap(width, height);
                        try
                        {
                            using (var g = Graphics.FromImage(img))
                            {
                                g.DrawImage(prevBitmap, 0, 0, width, height);
                            }
                            var m = new MemoryStream();
                            //img.Save(m, TrySet_ImageCodecInfo, TrySet_EncoderParameters);
                            WindowsMediaPhoto.Save(m, img, JXR_QUALITY);

                            if (sizeBound < long.MaxValue / 2)
                            {
                                sizeBound -= m.Position;
                                if (sizeBound < 0) // これで終わりの可能性があるので等号は含めない
                                {
                                    throw new Exception();
                                }
                            }

                            compressed[i] = m;

                        }
                        catch
                        {
                            img.Dispose();
                            if (!dontDisposePrev) prevBitmap.Dispose();
                            throw;
                        }
                        */


                        if (!dontDisposePrev) prevBitmap.Dispose();

                        if(getBiggest && i == maxSizeIndex)
                        {
                            biggestBitmap = img;
                            biggestBitmapPixelRatio = resizedPixelRatio[i];
                            prevBitmap = img;
                            dontDisposePrev = true;
                        }
                        else
                        {
                            prevBitmap = img;
                            dontDisposePrev = false;
                        }
                    }
                    if (!dontDisposePrev) prevBitmap.Dispose();
                    
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var w = new BinaryWriter(memoryStream))
                        {
                            w.Write(VERSION);

                            // できるだけ直前の更新日時を取得
                            DateTime lastWriteTime;
                            if (isFile) lastWriteTime = File.GetLastWriteTime(fileName);
                            else lastWriteTime = Directory.GetLastWriteTime(fileName);
                            if (ads)
                            {
                                if (lastWriteTime != lastWriteTime0) throw new Exception(); // biggestBitmap などの処理があるので例外を投げる
                            }
                            //var fileInfo = new FileInfo(fileName);
                            //var lastWriteTime = fileInfo.LastWriteTime;
                            //fileInfo = null;

                            w.Write(lastWriteTime.Ticks);
                            w.Write(index);

                            if (gpData == null || gpData.Length == 0)
                            {
                                w.Write(0);
                            }
                            else
                            {
                                w.Write(gpData.Length);
                                w.Write(gpData);
                            }

                            for (var i = 0; i <= maxSizeIndex; i++)
                            {
                                if (!maxSizeIsFullSize || i < maxSizeIndex)
                                {
                                    var resizedSizei = resizedSize[i];
                                    w.Write((uint)resizedSizei.Width);
                                    w.Write((uint)resizedSizei.Height);
                                }
                                else
                                {
                                    w.Write(uint.MaxValue);
                                    w.Write(uint.MaxValue);
                                }

                                var resizedPixelRatioi = resizedPixelRatio[i];
                                w.Write((uint)resizedPixelRatioi.Numerator);
                                w.Write((uint)resizedPixelRatioi.Denominator);

                                var m = compressed[i];
                                w.Write(m.Position);
                                m.Seek(0, SeekOrigin.Begin);
                                m.WriteTo(memoryStream);
                                m.Dispose();
                                compressed[i] = null;
                            }

                            memoryStream.Seek(0, SeekOrigin.Begin);

                            StreamWriteResult result = StreamWriteResult.Success;

                            if (ads)
                            {
                                // 書き込みチェック
                                /*
                                lock (blackList)
                                {
                                    if (!blackList.ContainsKey(fileNameLowerCase))
                                    {
                                        blackList[fileNameLowerCase] = 1;
                                    }
                                    else
                                    {
                                        blackList[fileNameLowerCase]++;
                                    }
                                }
                                */
                                EnterEditing(fileName);
                                try
                                {
                                    if (isFile)
                                    {
                                        File.SetLastWriteTime(fileName, lastWriteTime);
                                    }
                                    else
                                    {
                                        Directory.SetLastWriteTime(fileName, lastWriteTime);
                                    }
                                }
                                finally
                                {
                                    ExitedEditing(fileName);
                                }

                                // 既に存在する場合、そのまま上書きすると Changed イベントが１回多く発生する
                                // 削除ではイベントは何も発生しない
                                if (NTFSMultiStream.Exists(fileName, STREAM_NAME))
                                {
                                    try
                                    {
                                        NTFSMultiStream.Delete(fileName, STREAM_NAME);
                                    }
                                    catch { }
                                }

                                editFile(fileName, 
                                action: () =>
                                {
                                    try
                                    {
                                        var buf = new byte[memoryStream.Length];
                                        memoryStream.Read(buf, 0, buf.Length);
                                        result = NTFSMultiStream.WriteFile(fileName, STREAM_NAME, buf);
                                    }
                                    finally
                                    {
                                    // 読み込み専用属性を治す前に更新日時を変更するために editFile の中で
                                    if (result != StreamWriteResult.NotEdited)
                                        {
                                            /*
                                            lock (blackList)
                                            {
                                                if (!blackList.ContainsKey(fileNameLowerCase))
                                                {
                                                    blackList[fileNameLowerCase] = 1;
                                                }
                                                else
                                                {
                                                    blackList[fileNameLowerCase]++;
                                                }
                                            }
                                            */
                                            if (isFile)
                                            {
                                                File.SetLastWriteTime(fileName, lastWriteTime);
                                            }
                                            else
                                            {
                                                Directory.SetLastWriteTime(fileName, lastWriteTime);
                                            }
                                        }
                                    }

                                });
                            }
                            else // ファイルに保存
                            {
                                var cachePath = GetCachePath(fileName, cacheRoot);
                                //DeleteItemAndParents(cachePath, false, cachePath);
                                CreateDirectoryWithItsParents(Path.GetDirectoryName(cachePath));
                                using (var fileStream = new FileStream(cachePath, FileMode.Create, FileAccess.Write))
                                {
                                    memoryStream.CopyTo(fileStream);
                                }
                            }
                            return true;
                        }
                    }
                }
                finally
                {
                    for (var i = 0; i <= maxSizeIndex; i++)
                    {
                        var c = compressed[i];
                        if (c != null) c.Dispose();
                    }
                }

            }
            catch
            {
                if (biggestBitmap != null)
                {
                    biggestBitmap.Dispose();
                    biggestBitmap = null;
                }
                return false;
            }
            finally
            {
                if(readOnly)
                {
                    File.SetAttributes(fileName, att);
                }
                lock (TrySet_Creating)
                {
                    TrySet_Creating.Remove(fileName);
                }
            }
        }

        /*
        static bool fileIsLocked(string fileName)
        {
            try
            {
                File.SetLastWriteTime(fileName, File.GetLastWriteTime(fileName));
                return false;
            }
            catch
            {
                return true;
            }
        }

        static bool directoryIsLocked(string directoryName)
        {
            try
            {
                Directory.SetLastWriteTime(directoryName, Directory.GetLastWriteTime(directoryName));
                return false;
            }
            catch
            {
                return true;
            }
        }
        */


        private static bool GetResizedInfo(Size requiredSize, bool letterBox, Size originalSize, Fraction pixelRatio, bool cacheFullSize,
            out Size[] resizedSize, out Fraction[] resizedPixelRatio)
        {
            resizedSize = null;
            resizedPixelRatio = null;

            var aspect = (Fraction)originalSize * pixelRatio;

            // クリティカルな方が厳密に元の値 * SIZE_MARGIN になるように
            SizeD resizedMaxSize;
            if (letterBox ^ (Fraction)requiredSize > aspect) // 幅フィット
            {
                resizedMaxSize = new SizeD(requiredSize.Width, (double)(requiredSize.Width / aspect)) * SIZE_MARGIN;
            }
            else
            {
                resizedMaxSize = new SizeD((double)(requiredSize.Height * aspect), requiredSize.Height) * SIZE_MARGIN;
            }
            var resizedMaxSizeInt = resizedMaxSize.Round();

            /*
            var wZoom = (double)requiredSize.Width / aspect.Numerator;
            var hZoom = (double)requiredSize.Height / aspect.Denominator;
            var zoom = letterBox ? Math.Min(wZoom, hZoom) : Math.Max(wZoom, hZoom);
            var resizedMaxSize = new SizeD(aspect.Numerator, aspect.Denominator) * zoom;
            */

            // 容量節約のため、必要でない限りオリジナルに近いサイズは作らない
            if (!cacheFullSize && (
                resizedMaxSizeInt.Width * COMMON_RATIO > originalSize.Width ||
                resizedMaxSizeInt.Height * COMMON_RATIO > originalSize.Height))
            {
                return false;
            }

            var resizedSizeList = new List<Size>();
            var resizedPixelRatioList = new List<Fraction>();

            // 容量節約のため、オリジナルに近いサイズにでも縮小する
            // 容量節約が目的であり、オリジナルに近いサイズを作らないこととの整合性は重要でない
            if (resizedMaxSizeInt.Width > originalSize.Width || resizedMaxSizeInt.Height > originalSize.Height)
            {
                resizedSizeList.Add(originalSize);
                resizedPixelRatioList.Add(pixelRatio);

                var wZoom = (double)originalSize.Width / aspect.Numerator;
                var hZoom = (double)originalSize.Height / aspect.Denominator;
                var zoom = Math.Min(wZoom, hZoom);
                resizedMaxSize = new SizeD(aspect.Numerator, aspect.Denominator) * (zoom / COMMON_RATIO);
                resizedMaxSizeInt = resizedMaxSize.Round();
            }

            while (resizedMaxSizeInt.Width * resizedMaxSizeInt.Height >= AREA_UBOUND)
            {
                resizedSizeList.Add(resizedMaxSizeInt);
                resizedPixelRatioList.Add(1);
                resizedMaxSize /= COMMON_RATIO;
                resizedMaxSizeInt = resizedMaxSize.Round();
            }


            if (resizedSizeList.Count == 0)
            {
                if (originalSize.Width * originalSize.Height >= AREA_UBOUND)
                {
                    // オリジナルが大きく要求サイズが小さい場合は必ずキャッシュが必要
                    var z2 = Math.Sqrt(AREA_UBOUND / (resizedMaxSize.Width * resizedMaxSize.Height));
                    resizedMaxSizeInt = new Size((int)Math.Ceiling(resizedMaxSize.Width * z2), (int)Math.Ceiling(resizedMaxSize.Height * z2));

                    if (resizedMaxSizeInt.Width > originalSize.Width || resizedMaxSizeInt.Height > originalSize.Height)
                    {
                        // オリジナルサイズよりも大きいサイズは返さないことを保証
                        //（アスペクト比の違いから一方の辺の長さの逆転はありうる）
                        resizedSizeList.Add(originalSize);
                        resizedPixelRatioList.Add(pixelRatio);
                    }
                    else
                    {
                        resizedSizeList.Add(resizedMaxSizeInt);
                        resizedPixelRatioList.Add(1);
                    }
                }
                else
                {
                    // オリジナルも小さい場合、そのキャッシュが必要かどうかは cacheFullSize に依存
                    if (cacheFullSize)
                    {
                        resizedSizeList.Add(originalSize);
                        resizedPixelRatioList.Add(pixelRatio);
                    }
                    else return false;
                }
            }
            resizedSizeList.Reverse();
            resizedSize = resizedSizeList.ToArray();
            resizedPixelRatioList.Reverse();
            resizedPixelRatio = resizedPixelRatioList.ToArray();
            return true;
        }

        /*
        private static Size[] GetSizeArray(Size requiredSize, Size originalSize)
        {
            var reversedResult = new List<Size>();
            var zoom = 1;
            while (true)
            {
                var size = new Size(requiredSize.Width / zoom, requiredSize.Height / zoom);
                if (size.Width * size.Height < AREA_UBOUND)
                {
                    if (zoom == 1)
                    {
                        var m = originalSize.Width * originalSize.Height;
                        if (m < AREA_UBOUND)
                        {
                            reversedResult.Add(originalSize);
                        }
                        else
                        {
                            var z2 = Math.Sqrt((double)AREA_UBOUND / m);
                            reversedResult.Add(new Size((int)Math.Ceiling(originalSize.Width * z2), (int)Math.Ceiling(originalSize.Height * z2)));
                        }
                    }
                    break;
                }
                reversedResult.Add(size);
                zoom *= COMMON_RATIO;
            }
            var result = new Size[reversedResult.Count];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = reversedResult[result.Length - i - 1];
            }
            return result;
        }
        */

        /*
        private static EncoderParameters GetJpegEncoderParameters(long quality)
        {
            var eps = new EncoderParameters(1);
            var ep = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
            eps.Param[0] = ep;
            return eps;
        }

        private static ImageCodecInfo GetEncoderInfo(ImageFormat f)
        {
            return ImageCodecInfo.GetImageEncoders().FirstOrDefault(enc => enc.FormatID == f.Guid);
        }
        */

        private static void editFile(string fileName, Action action)
        {
            editFile<object>(fileName, () => { action(); return null; });
        }

        private static T editFile<T>(string fileName, Func<T> func)
        {
            EnterEditing(fileName);
            try { return func(); } finally { ExitedEditing(fileName); }
            /*
            var fileNameLowerCase = fileName.ToLower();
            //blackList.Add(fileNameLowerCase);
            
            if (preAction != null)
            {
                if(preBlackListWeight != null)
                {
                    var preBlackListWeightInt = (int)preBlackListWeight;
                    lock (blackList)
                    {
                        blackList[fileNameLowerCase] = preBlackListWeightInt;
                    }
                }
                preAction();
            }

            if (blackListWeight > 0)
            {
                lock (blackList)
                {
                    if (preAction == null || !blackList.ContainsKey(fileNameLowerCase))
                    {
                        blackList[fileNameLowerCase] = blackListWeight;
                    }
                    else
                    {
                        blackList[fileNameLowerCase] += blackListWeight;
                    }
                }
            }

            return func();
            */
        }
    }
    
    public enum StreamWriteResult { Success, NotEdited, FaildInEditing }

    public class NTFSMultiStream : Stream
    {
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        private SafeFileHandle safeFileHandle;
        private IntPtr fileHandle;
        private FileStream fileStream;

        public NTFSMultiStream(string fileName, string streamName, FileAccess access)
        {
            fileHandle = GetFileHandle(fileName, streamName, access);
            safeFileHandle = new SafeFileHandle(fileHandle, ownsHandle: true);
            fileStream = new FileStream(safeFileHandle, access);
        }
        
        private static IntPtr GetFileHandle(string fileName, string streamName, FileAccess access)
        {
            var fullName = fileName + ":" + streamName;
            IntPtr result;

            switch (access)
            {
                case FileAccess.Write:
                    result = CreateFile(fullName,
                                DesiredAccess.GENERIC_WRITE,
                                ShareMode.FILE_SHARE_NONE, 0,
                                CreationDisposition.CREATE_ALWAYS,
                                FlagsAndAttributes.FILE_ATTRIBUTE_NORMAL,
                                IntPtr.Zero);
                    break;
                case FileAccess.Read:
                    result = CreateFile(fullName,
                                DesiredAccess.GENERIC_READ,
                                ShareMode.FILE_SHARE_READ, 0,
                                CreationDisposition.OPEN_EXISTING,
                                FlagsAndAttributes.FILE_ATTRIBUTE_NORMAL,
                                IntPtr.Zero);
                    break;
                default: throw new NotSupportedException();
            }
            if (result == INVALID_HANDLE_VALUE) throw new IOException();
            return result;
        }

        public override bool CanRead
        {
            get
            {
                return fileStream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return fileStream.CanSeek;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return fileStream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                return fileStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return fileStream.Position;
            }

            set
            {
                fileStream.Position = value;
            }
        }
        
        /*
        public static bool IsNTFS(string fileName)
        {
            try
            {
                var info = new DriveInfo(fileName);
                return info.DriveFormat.ToLower() == "ntfs";
            }
            catch
            {
                return false;
            }
        }
        */
        
        public static bool Exists(string fileName, string streamName)
        {
            var fullName = fileName + ":" + streamName;
            try
            {
                var fileHandle = CreateFile(fullName,
                                    DesiredAccess.GENERIC_EXISTENCE,
                                    ShareMode.FILE_SHARE_READ, 0,
                                    CreationDisposition.OPEN_EXISTING,
                                    FlagsAndAttributes.FILE_ATTRIBUTE_NORMAL,
                                    IntPtr.Zero);
                if(fileHandle == INVALID_HANDLE_VALUE)
                {
                    return false;
                }
                else
                {
                    (new SafeFileHandle(fileHandle, ownsHandle: true)).Dispose();
                    //CloseHandle(fileHandle);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool Delete(string fileName, string streamName)
        {
            var fullName = fileName + ":" + streamName;
            return DeleteFile(fullName);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (fileStream != null)
                {
                    try
                    {
                        fileStream.Dispose();
                    }
                    finally
                    {
                        fileStream = null;
                        safeFileHandle.Dispose();
                        //CloseHandle(fileHandle); // Dispose と CloseHandle の両方を行ってはいけない
                    }
                }
            }
            base.Dispose(disposing);
        }

        public static StreamWriteResult WriteFile(string fileName,string streamName, byte[] buff)
        {
            var fileHandle = GetFileHandle(fileName, streamName, FileAccess.Write);
            if (fileHandle == INVALID_HANDLE_VALUE) return StreamWriteResult.NotEdited;
            try
            {
                uint written = 0;
                var result = WriteFile(fileHandle, buff, (uint)buff.Length, ref written, IntPtr.Zero);
                if (result && written == buff.Length) return StreamWriteResult.Success;
                else return StreamWriteResult.FaildInEditing;
            }
            finally
            {
                CloseHandle(fileHandle);
            }
        }

#region Win32 API宣言

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr CreateFile(
            string lpFileName, // ファイル名
            DesiredAccess dwDesiredAccess, // アクセスモード
            ShareMode dwShareMode, // 共有モード
            int lpSecurityAttributes, // セキュリティ記述子
            CreationDisposition dwCreationDisposition, // 作成方法
            FlagsAndAttributes dwFlagsAndAttributes, // ファイル属性
            IntPtr hTemplateFile // テンプレートファイルのハンドル
            );

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool DeleteFile(
            string lpFileName // ファイル名
            );

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern unsafe bool CloseHandle
        (
            IntPtr hObject // handle to object
        );

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool WriteFile(
         IntPtr hFile,
         byte[] aBuffer,
         uint cbToWrite,
         ref uint cbThatWereWritten,
         IntPtr pOverlapped);

        public override void Flush()
        {
            fileStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return fileStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            fileStream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return fileStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            fileStream.Write(buffer, offset, count);
        }


#region 列挙体

        private enum DesiredAccess : uint
        {
            GENERIC_EXISTENCE = 0x00000000,
            GENERIC_READ = 0x80000000,
            GENERIC_WRITE = 0x40000000,
            GENERIC_EXECUTE = 0x20000000
        }

        private enum ShareMode : uint
        {
            FILE_SHARE_NONE = 0x00000000,
            FILE_SHARE_READ = 0x00000001,
            FILE_SHARE_WRITE = 0x00000002,
            FILE_SHARE_DELETE = 0x00000004
        }

        private enum CreationDisposition : uint
        {
            CREATE_NEW = 1,
            CREATE_ALWAYS = 2,
            OPEN_EXISTING = 3,
            OPEN_ALWAYS = 4,
            TRUNCATE_EXISTING = 5
        }

        private enum FlagsAndAttributes : uint
        {
            FILE_ATTRIBUTE_ARCHIVE = 0x00000020,
            FILE_ATTRIBUTE_ENCRYPTED = 0x00004000,
            FILE_ATTRIBUTE_HIDDEN = 0x00000002,
            FILE_ATTRIBUTE_NORMAL = 0x00000080,
            FILE_ATTRIBUTE_NOT_CONTENT_INDEXED = 0x00002000,
            FILE_ATTRIBUTE_OFFLINE = 0x00001000,
            FILE_ATTRIBUTE_READONLY = 0x00000001,
            FILE_ATTRIBUTE_SYSTEM = 0x00000004,
            FILE_ATTRIBUTE_TEMPORARY = 0x00000100
        }

#endregion

#endregion
    }
}
