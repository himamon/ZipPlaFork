using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace garu.Util
{
	public enum SusieConfigType
	{
		About = 0,
		Config = 1,
	}

	public class SusiePlugin : IDisposable
	{
		Win32.BITMAPFILEHEADER bf;
		IntPtr hMod;
		string name;
		public string Name { get { return name; } }

        string fullName;
        public string FullName { get { return fullName; } }

        //bool isSph;
        //public bool IsSph { get { return isSph; } }

		// 00IN,00AM 必須
		// int _export PASCAL GetPluginInfo(
		//     int infono, LPSTR dw, int len);
		const string GET_PLUGIN_INFO = "GetPluginInfo";
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		delegate int GetPluginInfoHandler(int infono, StringBuilder buf, int buflen);
		GetPluginInfoHandler getPluginInfo;
		// for GetPluginInfo(Type)
		const int GETINFO_TYPE = 0;
		string type;
		public string Type { get { return type; } }
		public const string TYPE_SINGLE = "00IN";
		public const string TYPE_MULTI = "00AM";
		// for GetPluginInfo(Version)
		const int GETINFO_VERSION = 1;
		string version;
		public string Version { get { return version; } }
		// for GetPluginInfo(Filter)
		const int GETINFO_FILTER = 2;
		string filter;
		public string Filter { get { return filter; } }
        string[] supportedExtensions;
        public string[] SupportedExtensions { get { return supportedExtensions; } }

		// 00IN,00AM 必須
		// int _export PASCAL IsSupported(
		//     LPSTR file, DWORD dw);
		const string IS_SUPPORTED = "IsSupported";
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		delegate int IsSupportedHandler(string filename, [In]byte[] dw);
        IsSupportedHandler isSupported;
        private delegate bool MyIsSupportedDelegate(string filename, byte[] data, ref byte[] header);
        MyIsSupportedDelegate IsSupported;

        // sph 用。sph では IsSupported も利用できるが spi で利用しようとすると致命的なエラーになるため、
        // ユーザーが誤って spi の拡張子を sph にする可能性を考慮すると x64 プロセスで IsSupported を呼び出すべきではない
        const string IS_SUPPORTED_W = "IsSupportedW";
        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        delegate int IsSupportedWHandler(string filename, [In]byte[] dw);
        IsSupportedWHandler isSupportedW;


        // 00IN,00AM 任意
        // int _export PASCAL ConfigurationDlg(
        //     HWND parent, int fnc)
        /*
        const string CONFIGURATION_DLG = "ConfigurationDlg";
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		delegate int ConfigurationDlgHandler(IntPtr parent, SusieConfigType fnc);
		ConfigurationDlgHandler configurationDlg;
		public EventHandler GetConfigHandler(IntPtr parent, SusieConfigType fnc)
		{
			if (configurationDlg == null) return null;
			return delegate { configurationDlg(parent, fnc); };
		}
        */

        // 00IN 任意
        // int _export PASCAL GetPictureInfo(
        //     LPSTR strb, long len,
        //     unsigned int flag,PictureInfo *lpInfo);

        // 00IN 必須
        // int _export PASCAL GetPicture(
        //     LPSTR strb, long len,
        //     unsigned int flag, HANDLE *pHBInfo, HANDLE *pHBm,
        //     FARPROC lpPrgressCallback, long lData);
        const string GET_PICTURE = "GetPicture";
		[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
		delegate int GetPictureHandler(
			[In]byte[] buf, int len, InputFlag flag, out IntPtr pHBInfo,
			out IntPtr pHBm, int lpProgressCallback, int lData);
		GetPictureHandler getPicture;
		enum InputFlag
		{
			File = 0,
			Memory = 1,
		}

		// 00IN 任意
		// int _export PASCAL GetPreview(
		//     LPSTR strb, long len,
		//     unsigned int flag, HANDLE *pHBInfo, HANDLE *pHBm,
		//     FARPROC lpPrgressCallback, long lData);

		// 00AM 必須
		// int _export PASCAL GetArchiveInfo(
		//     LPSTR strb, long len,
		//     unsigned int flag, HLOCAL *lphInf)

		// 00AM 必須
		// int _export PASCAL GetFileInfo(
		//     LPSTR strb, long len,
		//     LPSTR file, unsigned int flag, fileInfo *lpInfo)

		// 00AM 必須
		// int _export PASCAL GetFile(
		//     LPSTR src, long len,
		//     LPSTR dest, unsigned int flag,
		//     FARPROC prgressCallback, long lData)

		public static SusiePlugin Load(string filename)
		{
            //var sw = new System.Diagnostics.Stopwatch(); sw.Start();
			SusiePlugin spi = new SusiePlugin();
            try
            {
                spi.fullName = filename;
                spi.name = Path.GetFileName(filename);
                spi.hMod = Win32.LoadLibrary(filename);
                if (spi.hMod == IntPtr.Zero)
                {
                    spi.Dispose();
                    return null;
                }

                IntPtr addr;

                // 00IN,00AM 必須 GetPluginInfo()
                addr = Win32.GetProcAddress(spi.hMod, GET_PLUGIN_INFO);
                if (addr == IntPtr.Zero)
                {
                    spi.Dispose();
                    return null;
                }
                spi.getPluginInfo = (GetPluginInfoHandler)Marshal.
                    GetDelegateForFunctionPointer(addr, typeof(GetPluginInfoHandler));
                StringBuilder strb = new StringBuilder(256);
                spi.getPluginInfo(GETINFO_TYPE, strb, strb.Capacity);
                spi.type = strb.ToString();
                strb.Length = 0;
                spi.getPluginInfo(GETINFO_VERSION, strb, strb.Capacity);
                spi.version = strb.ToString();
                StringBuilder filter = new StringBuilder();
                StringBuilder ext = new StringBuilder(256);
                var exts = new HashSet<string>();
                for (int i = GETINFO_FILTER; ; i += 2)
                {
                    ext.Length = 0;
                    if (spi.getPluginInfo(i, ext, ext.Capacity) == 0) break;
                    strb.Length = 0;
                    if (spi.getPluginInfo(i + 1, strb, strb.Capacity) == 0) break;
                    exts.UnionWith(ext.ToString().Split(';'));
                    filter.Append(strb).Append('|').Append(ext).Append('|');
                }
                var supportedExtensions = exts.ToArray();
                spi.supportedExtensions = supportedExtensions;
                spi.filter = filter.ToString(0, filter.Length - 1);

                if ((spi.IsSupported = spi.getIndividualImplementedIsSupported()) == null)
                {
                    if (Environment.Is64BitProcess)
                    {
                        var isSph = Path.GetExtension(filename).ToLower() == ".sph";
                        //spi.isSph = isSph;
                        if (isSph)
                        {
                            addr = Win32.GetProcAddress(spi.hMod, IS_SUPPORTED_W);
                            if (addr == IntPtr.Zero)
                            {
                                // 使えるプラグインは使う仕様のため寛容であることは望ましくない

                                // 拡張子 sph の spi を許さないパターン
                                spi.Dispose();
                                return null;

                                // 許すパターン
                                //isSph = false;
                            }
                            else
                            {
                                spi.isSupportedW = (IsSupportedWHandler)Marshal.
                                    GetDelegateForFunctionPointer(addr, typeof(IsSupportedWHandler));
                                spi.IsSupported = spi.isSupportedWBySpi;
                            }
                        }

                        if (!isSph)
                        {
                            spi.IsSupported = spi.getIsSupportedByExtension();
                        }
                    }
                    else
                    {
                        // 00IN,00AM 必須 IsSupported()
                        addr = Win32.GetProcAddress(spi.hMod, IS_SUPPORTED);
                        if (addr == IntPtr.Zero)
                        {
                            spi.Dispose();
                            return null;
                        }
                        spi.isSupported = (IsSupportedHandler)Marshal.
                            GetDelegateForFunctionPointer(addr, typeof(IsSupportedHandler));
                        spi.IsSupported = spi.isSupportedBySpi;
                    }
                }

                // 00IN,00AM 任意 ConfigurationDlg()
                /*
                addr = Win32.GetProcAddress(spi.hMod, CONFIGURATION_DLG);
                if (addr != IntPtr.Zero)
                {
                    spi.configurationDlg = (ConfigurationDlgHandler)Marshal.
                        GetDelegateForFunctionPointer(addr, typeof(ConfigurationDlgHandler));
                }
                */

                if (spi.type == TYPE_SINGLE)
                {
                    // 00IN 必須 GetPicture()
                    addr = Win32.GetProcAddress(spi.hMod, GET_PICTURE);
                    if (addr == IntPtr.Zero)
                    {
                        spi.Dispose();
                        return null;
                    }
                    spi.getPicture = (GetPictureHandler)Marshal.
                        GetDelegateForFunctionPointer(addr, typeof(GetPictureHandler));
                }
                // TYPE_SINGLE 以外は無視する
                /*
                else if (spi.type == TYPE_MULTI)
                {
                    // 00AM 必須 GetArchiveInfo()
                    // 00AM 必須 GetFileInfo()
                    // 00AM 必須 GetFile()
                }
                */
                else
                {
                    spi.Dispose();
                    return null;
                }

                // sw.Stop(); System.Windows.Forms.MessageBox.Show($"{sw.Elapsed}\n{spi.name}");
                // 最大 0.003 秒程度 3000 画像で 1 秒程度
                // 画像ごとに呼び出しても問題のない
                return spi;
            }
            catch
            {
                spi.Dispose();
                throw;
            }
		}

        private MyIsSupportedDelegate getIndividualImplementedIsSupported()
        {
            if (version == null) return null;
            if (version.StartsWith("BPG to DIB"))
            {
                return isSupported＿BPG;
            }
            if (version.StartsWith("WebP to DIB"))
            {
                return isSupported＿WebP;
            }
            // xcf は動作確認済みだが *.xcf.gz などにも対応していて処理が複雑なので isSupportedByExtension を利用
            return null;
        }

        private MyIsSupportedDelegate getIsSupportedByExtension()
        {
            isSupportedByExtension_Regex = wildcardToRegex(supportedExtensions);
            return isSupportedByExtension;
        }

        private static bool isSupported＿BPG(string filename, byte[] dw, ref byte[] dummy)
        {
            return dw != null && dw.Length > 4 &&
                dw[0x0] == 0x42 && dw[0x1] == 0x50 && dw[0x2] == 0x47 && dw[0x3] == 0xFB;
        }

        private static bool isSupported＿WebP(string filename, byte[] dw, ref byte[] dummy)
        {
            return dw != null && dw.Length > 16 &&
                dw[0x0] == 0x52 && dw[0x1] == 0x49 && dw[0x2] == 0x46 && dw[0x3] == 0x46 &&
                dw[0x8] == 0x57 && dw[0x9] == 0x45 && dw[0xa] == 0x42 && dw[0xb] == 0x50 &&
                dw[0xc] == 0x56 && dw[0xd] == 0x50 && dw[0xe] == 0x38;
        }

        private static Regex wildcardToRegex(IEnumerable<string> wildcards)
        {
            var patterns = (from wildcard in wildcards select wildcardToRegex_eachCharRegex.Replace(wildcard, wildcardToRegex_evaluator));
            var pattern = $"^(?:(?:{string.Join(")|(?:", patterns)}))$";
            return new Regex(pattern, RegexOptions.IgnoreCase);
        }
        private static Regex wildcardToRegex_eachCharRegex = new Regex(".", RegexOptions.Compiled);
        private static string wildcardToRegex_evaluator(Match m)
        {
            var s = m.Value;
            switch (s)
            {
                case "?": return ".";
                case "*": return ".*";
                default: return Regex.Escape(s);
            }
        }
        
        private const int isSupportedBySpi_BufSize = 2048;
        private void setHeader(byte[] dw, ref byte[] header)
        {
            if (header == null)
            {
                header = new byte[isSupportedBySpi_BufSize];
                var copySize = Math.Min(dw.Length, isSupportedBySpi_BufSize);
                Array.Copy(dw, header, copySize);
            }
        }
        private bool isSupportedBySpi(string filename, byte[] dw, ref byte[] header)
        {
            setHeader(dw, ref header);
            return isSupported(filename, header) != 0;
        }
        private bool isSupportedWBySpi(string filename, byte[] dw, ref byte[] header)
        {
            setHeader(dw, ref header);
            return isSupportedW(filename, header) != 0;
        }

        private Regex isSupportedByExtension_Regex;
        private bool isSupportedByExtension(string filename, byte[] dw, ref byte[] dummy)
        {
            return filename != null && isSupportedByExtension_Regex.IsMatch(filename);
        }
        
        public Bitmap GetPicture(string file, byte[] buf, ref byte[] header)
		{
			if (type != TYPE_SINGLE) return null;
			if (!IsSupported(file, buf, ref header)) return null;
			IntPtr hBInfo, hBm;
            var errorCode = getPicture(buf, buf.Length, InputFlag.Memory, out hBInfo, out hBm, 0, 0);
            if (errorCode != 0) return null;
			try {
				IntPtr pBInfo = Win32.LocalLock(hBInfo);
				IntPtr pBm = Win32.LocalLock(hBm);
				makeBitmapFileHeader(pBInfo);
				byte[] mem = new byte[bf.bfSize];
				GCHandle handle = GCHandle.Alloc(bf, GCHandleType.Pinned);
				try {
					Marshal.Copy(handle.AddrOfPinnedObject(), mem, 0, Marshal.SizeOf(bf));
				}
				finally {
					handle.Free();
				}
				Marshal.Copy(pBInfo, mem, Marshal.SizeOf(bf), (int)bf.bfOffBits - Marshal.SizeOf(bf));
				Marshal.Copy(pBm, mem, (int)bf.bfOffBits, (int)(bf.bfSize - bf.bfOffBits));
				using (MemoryStream ms = new MemoryStream(mem)) {
					return new Bitmap(ms);
				}
			}
			finally {
				Win32.LocalUnlock(hBInfo);
				Win32.LocalFree(hBInfo);
				Win32.LocalUnlock(hBm);
				Win32.LocalFree(hBm);
			}
		}

		void makeBitmapFileHeader(IntPtr pBInfo)
		{
			Win32.BITMAPINFOHEADER bi = (Win32.BITMAPINFOHEADER)
				Marshal.PtrToStructure(pBInfo, typeof(Win32.BITMAPINFOHEADER));
			bf.bfSize = (uint)((((bi.biWidth * bi.biBitCount + 0x1f) >> 3) & ~3) * bi.biHeight);
			bf.bfOffBits = (uint)(Marshal.SizeOf(bf) + Marshal.SizeOf(bi));
			if (bi.biBitCount <= 8) {
				uint palettes = bi.biClrUsed;
				if (palettes == 0)
					palettes = 1u << bi.biBitCount;
				bf.bfOffBits += palettes << 2;
			}
			bf.bfSize += bf.bfOffBits;
			bf.bfType = Win32.BM;
			bf.bfReserved1 = 0;
			bf.bfReserved2 = 0;
		}

		~SusiePlugin()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing)
		{
			if (hMod != IntPtr.Zero) {
				Win32.FreeLibrary(hMod);
				hMod = IntPtr.Zero;
			}
		}
	}
}
