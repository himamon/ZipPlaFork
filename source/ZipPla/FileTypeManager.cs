using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ZipPla
{

    public static class FileTypeManager
    {
        public static string GetDisplayName(string fileName)
        {
            SHFILEINFOW shinfo = new SHFILEINFOW();
            Win32.SHGetFileInfoW(
                    fileName,
                    FileAttributesFlags.FILE_ATTRIBUTE_NORMAL,
                    ref shinfo,
                    (uint)Marshal.SizeOf(shinfo),
                    ShellFileInfoFlags.SHGFI_DISPLAYNAME 
                );
            return shinfo.szDisplayName;
        }

        public static string GetTypeName(string fileName, bool useFileAttrinutes)
        {
            SHFILEINFOW shinfo = new SHFILEINFOW();
            Win32.SHGetFileInfoW(
                    fileName,
                    FileAttributesFlags.FILE_ATTRIBUTE_NORMAL,
                    ref shinfo,
                    (uint)Marshal.SizeOf(shinfo),
                    useFileAttrinutes ?
                        ShellFileInfoFlags.SHGFI_TYPENAME | ShellFileInfoFlags.SHGFI_USEFILEATTRIBUTES :
                        ShellFileInfoFlags.SHGFI_TYPENAME
                );
            return shinfo.szTypeName;
        }

        public static Bitmap GetSmallIconBitmap(string fileName, bool useFileAttrinutes)
        {
            return getIconBitmap(fileName, useFileAttrinutes, isSmall: true);
        }

        public static Bitmap GetLargeIconBitmap(string fileName, bool useFileAttrinutes)
        {
            return getIconBitmap(fileName, useFileAttrinutes, isSmall: false);
        }

        public static Tuple<string, Bitmap> GetTypeNameAndSmallIconBitmap(string fileName, bool useFileAttrinutes)
        {
            return getTypeNameAndIconBitmap(fileName, useFileAttrinutes, isSmall: true);
        }

        private static Bitmap getIconBitmap(string fileName, bool useFileAttrinutes, bool isSmall)
        {
            SHFILEINFOW shinfo = new SHFILEINFOW();
            try
            {
                var flag = ShellFileInfoFlags.SHGFI_ICON;
                if (useFileAttrinutes) flag |= ShellFileInfoFlags.SHGFI_USEFILEATTRIBUTES;
                if (isSmall) flag |= ShellFileInfoFlags.SHGFI_SMALLICON;
                Win32.SHGetFileInfoW(fileName, FileAttributesFlags.FILE_ATTRIBUTE_NORMAL,
                    ref shinfo, (uint)Marshal.SizeOf(shinfo), flag);
                if (shinfo.hIcon == IntPtr.Zero)
                {
                    return null;
                }
                else
                {
                    var unm = Icon.FromHandle(shinfo.hIcon);
                    return (Bitmap)unm.ToBitmap();
                }
            }
            finally
            {
                if (shinfo.hIcon != IntPtr.Zero)
                    DestroyIcon(shinfo.hIcon); // DestroyIcon が必要なものは返さない
            }
        }

        private static Tuple<string, Bitmap> getTypeNameAndIconBitmap(string fileName, bool useFileAttrinutes, bool isSmall)
        {
            SHFILEINFOW shinfo = new SHFILEINFOW();
            try
            {
                var flag = ShellFileInfoFlags.SHGFI_TYPENAME | ShellFileInfoFlags.SHGFI_ICON;
                if (useFileAttrinutes) flag |= ShellFileInfoFlags.SHGFI_USEFILEATTRIBUTES;
                if (isSmall) flag |= ShellFileInfoFlags.SHGFI_SMALLICON;
                Win32.SHGetFileInfoW(fileName, FileAttributesFlags.FILE_ATTRIBUTE_NORMAL,
                    ref shinfo, (uint)Marshal.SizeOf(shinfo), flag);
                Bitmap iconBitmap;
                if (shinfo.hIcon == IntPtr.Zero)
                {
                    iconBitmap = null;
                }
                else
                {
                    var unm = Icon.FromHandle(shinfo.hIcon);
                    iconBitmap = (Bitmap)unm.ToBitmap();
                }
                return Tuple.Create(shinfo.szTypeName, iconBitmap);
            }
            finally
            {
                if (shinfo.hIcon != IntPtr.Zero)
                    DestroyIcon(shinfo.hIcon); // DestroyIcon が必要なものは返さない
            }
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DestroyIcon(IntPtr hIcon);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHFILEINFOW
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        public enum ShellFileInfoFlags : uint
        {
            SHGFI_ICON = 0x000000100,
            SHGFI_DISPLAYNAME = 0x000000200,
            SHGFI_TYPENAME = 0x000000400,
            SHGFI_ATTRIBUTES = 0x000000800,
            SHGFI_ICONLOCATION = 0x000001000,
            SHGFI_EXETYPE = 0x000002000,
            SHGFI_SYSICONINDEX = 0x000004000,
            SHGFI_LINKOVERLAY = 0x000008000,
            SHGFI_SELECTED = 0x000010000,
            SHGFI_ATTR_SPECIFIED = 0x000020000,
            SHGFI_LARGEICON = 0x000000000,
            SHGFI_SMALLICON = 0x000000001,
            SHGFI_OPENICON = 0x000000002,
            SHGFI_SHELLICONSIZE = 0x000000004,
            SHGFI_PIDL = 0x000000008,
            SHGFI_USEFILEATTRIBUTES = 0x000000010
        }

        public enum FileAttributesFlags : uint
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

        public class Win32
        {
            //
            [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            public static extern IntPtr SHGetFileInfoW(
                string pszPath,
                FileAttributesFlags dwFileAttributes,
                ref SHFILEINFOW psfi,
                uint cbSizeFileInfo,
                ShellFileInfoFlags uFlags);
        }
    }

    public static class SmartStaticIconProvider
    {
        private const string SMALL_PREFIX = "<small>:";
        private const string LARGE_PREFIX = "<large>:";

        private static readonly Dictionary<string, Bitmap> groupIconDictionary = new Dictionary<string, Bitmap>();
        private static readonly Dictionary<string, Tuple<DateTime, Bitmap>> uniqueIconDictionary = new Dictionary<string, Tuple<DateTime, Bitmap>>();

        public static void ClearIconCache()
        {
            if(defaultSmallDirectoryIcon != null)
            {
                defaultSmallDirectoryIcon.Dispose();
                defaultSmallDirectoryIcon = null;
            }

            if(defaultLargeDirectoryIcon != null)
            {
                defaultLargeDirectoryIcon.Dispose();
                defaultLargeDirectoryIcon = null;
            }

            foreach (var bmp in groupIconDictionary.Values) bmp.Dispose();
            groupIconDictionary.Clear();

            foreach (var tpl in uniqueIconDictionary.Values) tpl.Item2.Dispose();
            uniqueIconDictionary.Clear();
        }

        public static Bitmap GetSmall(string iconPath, DateTime lastWriteTime, bool isDir)
        {
            return GetIcon(iconPath, lastWriteTime, true, isDir);
        }

        public static Bitmap GetLarge(string iconPath, DateTime lastWriteTime, bool isDir)
        {
            return GetIcon(iconPath, lastWriteTime, false, isDir);
        }

        private static Bitmap GetIcon(string iconPath, DateTime lastWriteTime, bool isSmall, bool isDir)
        {
            string key;
            bool uniqueIcon;
            if (isDir)
            {
                var iconPathToLower = iconPath.ToLower();
                if (DirMightHaveSpecialIcon(iconPathToLower)) key = iconPathToLower;
                else return GetDefaultDirectoryIcon(isSmall);
                uniqueIcon = false;
            }
            else
            {
                key = GetExtension(iconPath).ToLower();
                uniqueIcon = key == ".exe" || key == ".ico" || key == ".lnk" || key == ".url"; // .lnk は矢印が必要なので実体は使わない
                if (uniqueIcon)
                {
                    key = iconPath.ToLower();
                }
            }

            key = (isSmall ? SMALL_PREFIX : LARGE_PREFIX) + key;

            Bitmap result = null;
            DateTime lastWriteTime0;
            bool got;
            if (uniqueIcon)
            {
                Tuple<DateTime, Bitmap> output;
                got = uniqueIconDictionary.TryGetValue(key, out output);
                if (got)
                {
                    lastWriteTime0 = output.Item1;
                    if (lastWriteTime0 == lastWriteTime)
                    {
                        result = output.Item2;
                    }
                    else
                    {
                        output.Item2.Dispose();
                        uniqueIconDictionary.Remove(key); // 直後にまた代入されるがその前に例外が投げられる可能性があるため
                        got = false;
                    }
                }
                else
                {
                    lastWriteTime0 = default(DateTime);
                }
            }
            else
            {
                got = groupIconDictionary.TryGetValue(key, out result);
                lastWriteTime0 = default(DateTime);
            }
            if (!got)
            {
                result = isSmall ?
                    FileTypeManager.GetSmallIconBitmap(iconPath, useFileAttrinutes: !isDir) :
                    FileTypeManager.GetLargeIconBitmap(iconPath, useFileAttrinutes: !isDir);
                if (result == null) return null;
                if (isDir)
                {
                    if (IsDefaultDirectoryIcon(result, isSmall))
                    {
                        result.Dispose();
                        result = GetDefaultDirectoryIcon(isSmall);
                        if (result == null) return null;
                    }
                }
                if (uniqueIcon)
                {
                    uniqueIconDictionary[key] = Tuple.Create(lastWriteTime, result);
                }
                else
                {
                    groupIconDictionary[key] = result;
                }
            }
            return result;
        }


        private static bool IsDefaultDirectoryIcon(Bitmap icon, bool isSmall)
        {
            return false;
        }

        private static string defaultDirectoryPath = null;
        public static string DefaultDirectoryPath
        {
            get
            {
                if(defaultDirectoryPath == null)
                {
                    try
                    {
                        defaultDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.System);
                    }
                    catch
                    {
                        defaultDirectoryPath = "";
                    }
                }
                return defaultDirectoryPath;
            }
        }

        private static Bitmap defaultSmallDirectoryIcon = null;
        private static Bitmap defaultLargeDirectoryIcon = null;
        private static Bitmap GetDefaultDirectoryIcon(bool isSmall)
        {
            if(isSmall)
            {
                if(defaultSmallDirectoryIcon == null)
                {
                    defaultSmallDirectoryIcon = FileTypeManager.GetSmallIconBitmap(DefaultDirectoryPath, useFileAttrinutes: false);
                }
                return defaultSmallDirectoryIcon;
            }
            else
            {
                if(defaultLargeDirectoryIcon == null)
                {
                    defaultLargeDirectoryIcon = FileTypeManager.GetLargeIconBitmap(DefaultDirectoryPath, useFileAttrinutes: false);
                }
                return defaultLargeDirectoryIcon;
            }
        }

        private static bool DirMightHaveSpecialIcon(string pathInLowerCase)
        {
            return SimpleBookmark.HasDesktopIni(pathInLowerCase) == true;
            //return SimpleBookmark.MightBeSpecialDirectory(pathInLowerCase);
        }

        private static string GetExtension(string path)
        {
            if (path == null) return "";
            var start = path.Length - 1;
            for(var i = start; i >= 0; i--)
            {
                var c = path[i];
                if (c == '.') return path.Substring(i);
                if (c == System.IO.Path.DirectorySeparatorChar || c == System.IO.Path.AltDirectorySeparatorChar)
                {
                    if (c == start) return c.ToString();
                    else return "";
                }
            }
            return "";
        }
    }
    

    /*
    public class Win32FileInfo
    {
        public static bool GetFileInfo(string fileName, ref CItem item)
        {
            if (!System.IO.File.Exists(fileName))
                return false;
            SHFILEINFOW shinfo = new SHFILEINFOW();
            Win32.SHGetFileInfoW(
                    fileName,
                    FileAttributesFlags.FILE_ATTRIBUTE_NORMAL,
                    ref shinfo,
                    (uint)Marshal.SizeOf(shinfo),
                    ShellFileInfoFlags.SHGFI_ICON |
                    ShellFileInfoFlags.SHGFI_LARGEICON |
                    ShellFileInfoFlags.SHGFI_TYPENAME |
                    ShellFileInfoFlags.SHGFI_DISPLAYNAME
                );
            item.image = Imaging.CreateBitmapSourceFromHIcon(
                    shinfo.hIcon,
                    new Int32Rect(0, 0, 32, 32),
                    BitmapSizeOptions.FromEmptyOptions()
                );
            item.type = shinfo.szTypeName;
            item.name = shinfo.szDisplayName;
            return true;
        }
    }

    public class CItem
    {
        public BitmapSource image { get; set; }
        public string name { get; set; }
        public string type { get; set; }
    }
    */
}
