using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Linq;
using System.Collections.Concurrent;

namespace garu.Util
{
	public delegate void SusieMenuBuilder(
		string shortName, string longName, EventHandler handler);

	public class Susie : IDisposable
	{
		readonly List<SusiePlugin> items = new List<SusiePlugin>();

		string[] versions;
		public string[] Versions { get { return versions; } }
        
        //public string[] SupportExtensions { get; private set; }

		public Susie(bool allowUntested, bool searchInstallationFolder) { Reset(null, allowUntested, searchInstallationFolder); }
        public Susie(string spiPath) { Reset(spiPath, false, false); }

        private void Reset(string spiPath, bool allowUntested, bool searchInstallationFolder)
		{
			Dispose(true);
			items.Clear();
            if (spiPath == null)
            {
                Load(Application.StartupPath, allowUntested);
                if (searchInstallationFolder)
                {
                    RegistryKey regkey = Registry.CurrentUser.OpenSubKey(
                        @"Software\Takechin\Susie\Plug-in", false);
                    if (regkey != null)
                    {
                        Load(regkey.GetValue("Path") as string, allowUntested);
                        regkey.Close();
                    }
                }
            }
            else
            {
                var ex = LoadOne(spiPath);
                if (ex != null) throw ex;
            }

            versions = items.Select(spi => spi.Version).ToArray();

            /*
            var extensions = new HashSet<string>();
            foreach (var item in items) extensions.UnionWith(
                from f in item.SupportedExtensions
                where f.Length > 2 && f.StartsWith("*.")
                let res = f.Substring(2)
                where !res.Contains('.')
                select res.ToLower());
            SupportExtensions = extensions.ToArray();
            */
		}

        public ConcurrentDictionary<string, string> GetExtensionToSpiPathDictionary()
        {
            var result = new ConcurrentDictionary<string, string>();
            foreach (var item in items)
            {
                var spiName = item.FullName;
                foreach (var extension in item.SupportedExtensions)
                {
                    if (extension.Length > 2 && extension.StartsWith("*."))
                    {
                        var e2 = extension.Substring(2);
                        if (!e2.Contains('.'))
                        {
                            e2 = e2.ToLower();
                            if (!result.ContainsKey(e2))
                            {
                                result[e2] = spiName;
                                //result.Add(e2, spiName);
                            }
                        }
                    }
                }
            }
            return result;
        }
        
		void Load(string folder, bool allowUntested)
        {
            string[] files;
            var is64 = Environment.Is64BitProcess;
            try
            {
                files = Directory.GetFiles(folder, is64 ? "*.*" : "*.spi");
            }
            catch
            {
                return;
            }
            foreach (string spiPath in files)
            {
                var len = spiPath.Length;
                if (len <= 4) continue;
                var ext = spiPath.Substring(len - 4, 4).ToLower();
                if ((ext == ".spi" || is64 && ext == ".sph") && (allowUntested || isTestedPlugin(spiPath)))
                {

                    LoadOne(spiPath);
                }
            }
        }

        private static readonly Tuple<long, byte[]>[] testedFilesInfo =
            Environment.Is64BitProcess ? new Tuple<long, byte[]>[]
            {
                Tuple.Create(130560L, new byte[16] {0xBD,0x5C,0x6D,0x0E,0x2E,0x47,0xB0,0x25,0x95,0x97,0xBD,0x6E,0x39,0xE9,0xEF,0xC1}), // ifBPG-x64.spi
                Tuple.Create(272896L, new byte[16] {0xC2,0x20,0x64,0x29,0xBE,0x8C,0xA6,0xB9,0xAD,0x96,0x3E,0x2F,0x5F,0x5C,0x01,0x06}), // ifwebp-x64.spi
                Tuple.Create(204800L, new byte[16] {0x8C,0x27,0xB6,0x51,0x7E,0x24,0x92,0x28,0xDC,0xE2,0x4A,0xC6,0xFF,0xDE,0x48,0xB4}), // ifxcf-x64.spi
                Tuple.Create(195716L, new byte[16] {0xE4,0xBD,0x6B,0xE4,0x65,0x4B,0x92,0x80,0xC5,0x89,0x6B,0x05,0x34,0x72,0x2D,0x2F}), // ifjpeg2k.sph
            }
            : new Tuple<long, byte[]>[]
            {
                Tuple.Create(125952L, new byte[16] {0x7F,0x60,0x14,0x75,0xE4,0xCA,0xA7,0xAE,0xCE,0xE7,0xBE,0x6E,0x4C,0x8B,0x9C,0x09}), // ifBPG.spi
                Tuple.Create(183296L, new byte[16] {0x10,0xD6,0x40,0x93,0xE5,0x27,0x08,0x4D,0x2F,0xEC,0xDB,0x24,0xCE,0x28,0xC9,0x1A}), // ifwebp.spi
                Tuple.Create(176128L, new byte[16] {0xA4,0x20,0xB9,0x93,0xB5,0x37,0x96,0xE3,0x2D,0x95,0xE0,0xE1,0x7C,0xEB,0x0C,0x12}), // ifxcf.spi
                Tuple.Create(182916L, new byte[16] {0xBB,0x76,0xDF,0x65,0xBA,0xE2,0x03,0x33,0x13,0x98,0x21,0x7F,0xDD,0x40,0x60,0xF8}), // ifjpeg2k.spi
            };
        private static readonly System.Security.Cryptography.MD5Cng getHash_SHA1 = new System.Security.Cryptography.MD5Cng();
        static bool isTestedPlugin(string path)
        {
            try
            {
                var size = new FileInfo(path).Length;
                var data = testedFilesInfo.FirstOrDefault(i => i.Item1 == size);
                if (data == null) return false;
                byte[] md5;
                using (var s = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    md5 = getHash_SHA1.ComputeHash(s);
                }
                return md5.SequenceEqual(data.Item2);
            }
            catch
            {
                return false;
            }
        }

        Exception LoadOne(string spiPath)
        {
            SusiePlugin spi;
            try
            {
                spi = SusiePlugin.Load(spiPath);
            }
            catch (Exception e)
            {
                return e;
            }
            if (spi != null)
            {
                if (!items.Exists(delegate (SusiePlugin i)
                {
                    return i.Version == spi.Version;
                }))
                {
                    items.Add(spi);
                    return null;
                }
                else
                {
                    spi.Dispose();
                    return new Exception("Duplicate spi");
                }
            }
            return new Exception("Spi load error");
        }

		public Bitmap GetPicture(string file)
		{
			Bitmap bmp = null;
			try {
				byte[] buf = File.ReadAllBytes(file);
                var headerBuf = null as byte[];
				items.Find(delegate(SusiePlugin spi) {
					bmp = spi.GetPicture(file, buf, ref headerBuf);
					return bmp != null;
				});
			}
			catch {
			}
			return bmp;
		}

        /*
        public void BuildConfigMenu(IntPtr parent, SusieConfigType fnc,
			SusieMenuBuilder builder)
		{
			items.ForEach(delegate(SusiePlugin spi) {
				EventHandler handler = spi.GetConfigHandler(parent, fnc);
				if (handler != null) {
					builder(spi.Name, spi.Version, handler);
				}
			});
		}
        */

		~Susie()
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
			if (disposing) {
				items.ForEach(delegate(SusiePlugin spi) {
					spi.Dispose();
				});
				items.Clear();
			}
		}


        public Bitmap GetPicture(string file, byte[] buf)
        {
            Bitmap bmp = null;
            try
            {
                var headerBuf = null as byte[];
                items.Find(delegate (SusiePlugin spi) {
                    bmp = spi.GetPicture(file, buf, ref headerBuf);
                    return bmp != null;
                });
            }
            catch
            {
            }
            return bmp;
        }
    }
}
