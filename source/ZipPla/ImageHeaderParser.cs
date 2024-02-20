using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ZipPla
{
    public class ImageInfo
    {
        public static readonly string[] SupportExtensionsInLowerWithoutPeriod = new string[] { "jpg", "jpeg", "png", "apng", "bmp", "gif", "wmf", "emf", "psd", "dds", "pdn" };
        private static readonly Regex supportsPathRegex;
        static ImageInfo()
        {
            supportsPathRegex = new Regex(@"\.(?:" + string.Join("|", SupportExtensionsInLowerWithoutPeriod) + ")$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
        
        public static bool Supports(string path)
        {
            return path != null && supportsPathRegex.IsMatch(path);
        }

        public static ImageInfo FromData(byte[] data)
        {
            using (var m = new MemoryStream(data))
            using (var r = new BinaryReader(m, Encoding.ASCII))
            {
                var magick = new string(r.ReadChars(5));
                if (magick != "IMAGE") throw new Exception("Not image data");
                return new ImageInfo(r.ReadInt32(), r.ReadInt32(), r.ReadInt32());
            }
        }

        public static bool TryParseData(byte[] data, out ImageInfo imageInfo)
        {
            try
            {
                imageInfo = FromData(data);
                return true;
            }
            catch
            {
                imageInfo = null;
                return false;
            }
        }

        public byte[] ToData()
        {
            using (var m = new MemoryStream())
            using (var w = new BinaryWriter(m, Encoding.ASCII))
            {
                w.Write("IMAGE".ToArray());
                w.Write(Size.Width);
                w.Write(Size.Height);
                w.Write(BitPerPixel);
                return m.ToArray();
            }
        }

        public readonly Size Size;
        public readonly int BitPerPixel;

        public ImageInfo(int width, int height, int bpp)
        {
            Size = new Size(width, height);
            BitPerPixel = bpp;
        }

        public ImageInfo(Size size, int bpp)
        {
            Size = size;
            BitPerPixel = bpp;
        }

        public ImageInfo(Size size)
        {
            Size = size;
        }

        public static ImageInfo FromImageInfo(ImageInfo info, ImageInfo respectedInfo)
        {
            if (info == null) return respectedInfo;
            if (respectedInfo != null)
            {
                var changed = false;
                var size = respectedInfo.Size;
                if (size.IsEmpty)
                {
                    changed = true;
                    size = info.Size;
                }
                var bpp = respectedInfo.BitPerPixel;
                if (bpp <= 0)
                {
                    changed = true;
                    bpp = info.BitPerPixel;
                }
                if (changed)
                {
                    return new ImageInfo(size, bpp);
                }
            }
            return info;
        }

        /*
        public ImageInfo(Image img)
        {
            Size = img.Size;
            BitPerPixel = Image.GetPixelFormatSize(img.PixelFormat);
        }
        */
        
        public ImageInfo (string path)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var buf16 = new byte[16];
                if (fs.Read(buf16, 0, 2) < 2) throw new NotSupportedException();

                switch (buf16[0] << 8 | buf16[1])
                {
                    case 0xFFd8: // JPEG
                        while (fs.Read(buf16, 0, 4) == 4)
                        {
                            /*
                            if(k==0)
                            {
                                MessageBox.Show($"{path}\n{buf[0]:X} {buf[1]:X} {buf[2]:X} {buf[3]:X}");

                            }:*/
                            if (buf16[0] != 0xff) throw new NotSupportedException();
                            var buf1 = buf16[1];
                            if ((buf1 & 0xf0) == 0xc0 && buf1 != 0xc4 && buf1 != 0xc8 && buf1 != 0xcc)
                            {
                                if (fs.Seek(1, SeekOrigin.Current) < 1 || fs.Read(buf16, 0, 4) < 4) throw new NotSupportedException();
                                /*
                                if (k == 0)
                                {
                                    k++;
                                    using (var img = new Bitmap(path))
                                    {
                                        MessageBox.Show($"{(buf[2] << 8) | buf[3]} x {(buf[0] << 8) | buf[1]}\n{img.Width} x {img.Height}");
                                    }
                                }
                                */

                                Size = new Size((buf16[2] << 8) | buf16[3], (buf16[0] << 8) | buf16[1]);
                                return;

                                //return new ImageInfo((buf[2] << 8) | buf[3], (buf[0] << 8) | buf[1], 0);
                            }
                            var seek = ((buf16[2] << 8) | buf16[3]) - 2;
                            if (fs.Seek(seek, SeekOrigin.Current) < seek) throw new NotSupportedException();
                        }
                        throw new NotSupportedException();
                    case 0x8950: // PNG/APNG
                        {

                            if (fs.Seek(14, SeekOrigin.Current) < 14 || fs.Read(buf16, 0, 10) < 10) throw new NotSupportedException();
                            int width = buf16[0] << 24 | buf16[1] << 16 | buf16[2] << 8 | buf16[3];
                            if (width <= 0) throw new NotSupportedException();
                            int height = buf16[4] << 24 | buf16[5] << 16 | buf16[6] << 8 | buf16[7];
                            if (height <= 0) throw new NotSupportedException();
                            var bpp = buf16[8];
                            if (bpp != 1 && bpp != 2 && bpp != 4 && bpp != 8 && bpp != 16) throw new NotSupportedException();
                            switch (buf16[9])
                            {
                                case 0: case 1: case 3:  break;
                                case 2: bpp *= 3; break;
                                case 4: bpp *= 2; break;
                                case 6: bpp *= 4; break;
                                default: throw new NotSupportedException();
                            }
                            /*
                                    if (k == 0)
                                    {
                                        k++;
                                        using (var img = new Bitmap(path))
                                        {
                                            MessageBox.Show($"{width} x {height} x {bpp}\n{img.Width} x {img.Height}");
                                        }
                                    }
                            */
                            Size = new Size(width, height);
                            BitPerPixel = bpp;
                            return;
                            //return new ImageInfo(width, height, bpp);
                        }
                    case 0x424d: // BMP
                        if (fs.Seek(12, SeekOrigin.Current) < 12 || fs.Read(buf16, 0, 16) < 16) throw new NotSupportedException();

                        //http://www.ruche-home.net/program/bmp/struct
                        switch (buf16[0] | buf16[1] << 8 | buf16[2] << 16 | buf16[3] << 32)
                        {
                            case 12:
                                {
                                    int width = buf16[4] | buf16[5] << 8;
                                    int height = buf16[6] | buf16[7] << 8;
                                    if (buf16[8] != 1 || buf16[9] != 0) throw new NotSupportedException();
                                    var bpp = buf16[10] | buf16[11] << 8;
                                    if (bpp != 1 && bpp != 4 && bpp != 8 && bpp != 16 && bpp != 24 && bpp != 32) throw new NotSupportedException();
                                    Size = new Size(width, height);
                                    BitPerPixel = bpp;
                                    return;
                                }
                            //case 40:
                            //case 108:
                            //case 124:
                            default:
                                {
                                    int width = buf16[4] | buf16[5] << 8 | buf16[6] << 16 | buf16[7] << 24;
                                    if (width <= 0) throw new NotSupportedException();
                                    int height = buf16[8] | buf16[9] << 8 | buf16[10] << 16 | buf16[11] << 24;
                                    if (height <= 0) throw new NotSupportedException();
                                    if (buf16[12] != 1 || buf16[13] != 0) throw new NotSupportedException();
                                    var bpp = buf16[14] | buf16[15] << 8;
                                    if (bpp != 1 && bpp != 4 && bpp != 8 && bpp != 16 && bpp != 24 && bpp != 32) throw new NotSupportedException();
                                    Size = new Size(width, height);
                                    BitPerPixel = bpp;
                                    return;
                                }
                        }

                    case 0x4749: // GIF
                        {
                            if (fs.Seek(4, SeekOrigin.Current) < 4 || fs.Read(buf16, 0, 5) < 5) throw new NotSupportedException();
                            int width = buf16[0] | buf16[1] << 8;
                            int height = buf16[2] | buf16[3] << 8;
                            var bpp = (buf16[4] & 7) + 1;
                            Size = new Size(width, height);
                            BitPerPixel = bpp;
                            return;
                            //return new ImageInfo(width, height, bpp);

                        }
                    case 0xd7cd: // WMF（ベクター画像なのでサイズにあまり意味はない）
                        {
                            if (fs.Read(buf16, 0, 4) < 4) throw new NotSupportedException();
                            if (buf16[0] != 0xc6 || buf16[1] != 0x9a || buf16[2] != 0 || buf16[3] != 0) throw new NotSupportedException();
                            if (fs.Read(buf16, 0, 8) < 8) throw new NotSupportedException();
                            int left = buf16[0] | buf16[1] << 8;
                            int top_ = buf16[2] | buf16[3] << 8;
                            int rigt = buf16[4] | buf16[5] << 8;
                            int botm = buf16[6] | buf16[7] << 8;
                            int width = Math.Abs(rigt - left); // こちらは 1 足さない
                            int height = Math.Abs(botm - top_); // こちらは 1 足さない
                            Size = new Size(width, height);
                            return;
                        }
                    case 0x0100: // EMF（ベクター画像なのでサイズにあまり意味はない）
                        {
                            if (fs.Read(buf16, 0, 2) < 2) throw new NotSupportedException();
                            if (buf16[0] != 0 || buf16[1] != 0) throw new NotSupportedException();
                            if (fs.Seek(4, SeekOrigin.Current) < 4 || fs.Read(buf16, 0, 16) < 16) throw new NotSupportedException();
                            int left = buf16[0] | buf16[1] << 8 | buf16[2] << 16 | buf16[3] << 24;
                            int top_ = buf16[4] | buf16[5] << 8 | buf16[6] << 16 | buf16[7] << 24;
                            int rigt = buf16[8] | buf16[9] << 8 | buf16[10] << 16 | buf16[11] << 24;
                            int botm = buf16[12] | buf16[13] << 8 | buf16[14] << 16 | buf16[15] << 24;
                            // Inclusive-inclusive bounds in device units
                            int width = Math.Abs(rigt - left) + 1; // デバイス単位
                            int height = Math.Abs(botm - top_) + 1;
                            
                            /*
                            if (fs.Read(buf16, 0, 16) < 16) throw new NotSupportedException();
                            int mcLeft = buf16[0] | buf16[1] << 8 | buf16[2] << 16 | buf16[3] << 24;
                            int mcTop_ = buf16[4] | buf16[5] << 8 | buf16[6] << 16 | buf16[7] << 24;
                            int mcRigt = buf16[8] | buf16[9] << 8 | buf16[10] << 16 | buf16[11] << 24;
                            int mcBotm = buf16[12] | buf16[13] << 8 | buf16[14] << 16 | buf16[15] << 24;
                            // Inclusive-inclusive bounds in device units
                            int mcWidth = Math.Abs(mcRigt - mcLeft) + 1; // 0.01 mm
                            int mcHeight = Math.Abs(mcBotm - mcTop_) + 1;

                            const int seek = 4 * 4 + 2 * 2 + 4 * 3;
                            if (fs.Seek(seek, SeekOrigin.Current) < seek || fs.Read(buf16, 0, 16) < 16) throw new NotSupportedException();
                            int szdw = buf16[0] | buf16[1] << 8 | buf16[2] << 16 | buf16[3] << 24; // 
                            int szdh = buf16[4] | buf16[5] << 8 | buf16[6] << 16 | buf16[7] << 24;
                            int szmw = buf16[8] | buf16[9] << 8 | buf16[10] << 16 | buf16[11] << 24;
                            int szmh = buf16[12] | buf16[13] << 8 | buf16[14] << 16 | buf16[15] << 24;
                            */

                            Size = new Size(width, height);
                            return;
                        }
                    case 0x3842: // PSD
                        {
                            if (fs.Read(buf16, 0, 2) < 2) throw new NotSupportedException();
                            if (buf16[0] != 0x50 || buf16[1] != 0x53) throw new NotSupportedException();
                            if(fs.Seek(10, SeekOrigin.Current) < 10 || fs.Read(buf16, 0, 8) < 8) throw new NotSupportedException();
                            int height = buf16[0] << 24 | buf16[1] << 16 | buf16[2] << 8 | buf16[3];
                            if (height <= 0 || height > 30000) throw new NotSupportedException();
                            int width = buf16[4] << 24 | buf16[5] << 16 | buf16[6] << 8 | buf16[7];
                            if (width <= 0 || width > 30000) throw new NotSupportedException();
                            Size = new Size(width, height);
                            return;
                        }
                    case 0x4444: //DDS
                        {
                            if (fs.Read(buf16, 0, 2) < 2) throw new NotSupportedException();
                            if (buf16[0] != 0x53 || buf16[1] != 0x20) throw new NotSupportedException();
                            if (fs.Seek(8, SeekOrigin.Current) < 8 || fs.Read(buf16, 0, 8) < 8) throw new NotSupportedException();
                            int height = buf16[0] | buf16[1] << 8 | buf16[2] << 16 | buf16[3] << 24;
                            if (height <= 0) throw new NotSupportedException();
                            int width = buf16[4] | buf16[5] << 8 | buf16[6] << 16 | buf16[7] << 24;
                            if (width <= 0) throw new NotSupportedException();
                            Size = new Size(width, height);
                            
                            if (fs.Seek(60, SeekOrigin.Current) < 60 || fs.Read(buf16, 0, 4) < 4) throw new NotSupportedException();
                            var pixelFormatFlag = buf16[0] | buf16[1] << 8 | buf16[2] << 16 | buf16[3] << 24;

                            if ((pixelFormatFlag & DDS_PIXELFORMAT.DDPF_RGB) != 0)
                            {
                                if ((pixelFormatFlag & DDS_PIXELFORMAT.DDPF_ALPHAPIXELS) != 0)
                                {
                                    BitPerPixel = 32;
                                }
                                else
                                {
                                    BitPerPixel = 24;
                                }
                            }
                            else if ((pixelFormatFlag & DDS_PIXELFORMAT.DDPF_FOURCC) != 0)
                            {
                                BitPerPixel = 32;
                            }
                            else if ((pixelFormatFlag & DDS_PIXELFORMAT.DDPF_FOURCC) == 0)
                            {
                                BitPerPixel = 24;
                            }
                            else
                            {
                                BitPerPixel = 0;
                            }

                            //var alpha = (pixelFormatFlag & 0x01) == 0x01;
                            //var color = (pixelFormatFlag & 0x04) == 0x04;

                            return;
                        }
                    case 0x5044: //PDN
                        {
                            Size = GetPdnSizeAndSeekNext(fs, buf16, twoOffset: true);
                            BitPerPixel = 32;
                            return;
                        }
                    default: throw new NotSupportedException();
                }
            }
        }

        private static class DDS_PIXELFORMAT
        {
            #region pixelformat values
            public const uint DDPF_ALPHAPIXELS = 0x00000001;
            public const uint DDPF_FOURCC = 0x00000004;
            public const uint DDPF_RGB = 0x00000040;
            public const uint DDPF_LUMINANCE = 0x00020000;
            #endregion

            #region fourccs
            public const uint FOURCC_DXT1 = 0x31545844;
            public const uint FOURCC_DXT2 = 0x32545844;
            public const uint FOURCC_DXT3 = 0x33545844;
            public const uint FOURCC_DXT4 = 0x34545844;
            public const uint FOURCC_DXT5 = 0x35545844;
            public const uint FOURCC_DX10 = 0x30315844;
            public const uint FOURCC_ATI1 = 0x31495441;
            public const uint FOURCC_ATI2 = 0x32495441;
            public const uint FOURCC_RXGB = 0x42475852;
            public const uint FOURCC_DOLLARNULL = 0x24;
            public const uint FOURCC_oNULL = 0x6f;
            public const uint FOURCC_pNULL = 0x70;
            public const uint FOURCC_qNULL = 0x71;
            public const uint FOURCC_rNULL = 0x72;
            public const uint FOURCC_sNULL = 0x73;
            public const uint FOURCC_tNULL = 0x74;
            #endregion
        }
        private static readonly Regex getPdnSizeAndSeekNext_pdnImageRegex = new Regex(@"(?:\bwidth=""(\d+)""|\bheight=""(\d+)""|.)*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static Size GetPdnSizeAndSeekNext(Stream fs, byte[] buf8, bool twoOffset)
        {
            if (twoOffset)
            {
                if (fs.ReadByte() != 0x4e) throw new NotSupportedException();
            }
            else
            {
                if (fs.Read(buf8, 0, 3) < 3) throw new NotSupportedException();
                if (!(buf8[0] == 0x50 && buf8[1] == 0x44 && buf8[2] == 0x4e)) throw new NotSupportedException();
            }
            var found = false;
            for (var i = 0; i < 10; i++)
            {
                var b = fs.ReadByte();
                if (b < 0) throw new NotSupportedException();
                if (b == 0x3c) // '<'
                {
                    found = true;
                    break;
                }
            }
            if (!found) throw new NotSupportedException();
            if (fs.Read(buf8, 0, 8) < 8) throw new NotSupportedException();
            var pdnImage = Encoding.ASCII.GetString(buf8);
            var length = pdnImage.Length;
            if (length == 8)
            {
                if (pdnImage.ToLower() != "pdnimage") throw new NotSupportedException();
            }
            else if(length > 8)
            {
                if (pdnImage.Substring(0, 8).ToLower() != "pdnimage" || pdnImage[8] != '\0') throw new NotSupportedException();
            }
            else throw new NotSupportedException();
            var pdnImageContent = new List<byte>();
            while (true)
            {
                var b = fs.ReadByte();
                if (b < 0) throw new NotSupportedException();
                if (b == 0x3e) break;// '>'
                pdnImageContent.Add((byte)b);
            }
            var contentText = Encoding.ASCII.GetString(pdnImageContent.ToArray());
            var m = getPdnSizeAndSeekNext_pdnImageRegex.Match(contentText);
            if (!m.Success) throw new NotSupportedException();
            var g = m.Groups;
            if (!int.TryParse(g[1].Value, out var width) || !int.TryParse(g[2].Value, out var height)) throw new NotSupportedException();
            return new Size(width, height);
        }

        public static Size GetPdnSizeAndSeekNext(Stream fs)
        {
            var buf8 = new byte[8];
            return GetPdnSizeAndSeekNext(fs, buf8, twoOffset: false);
        }
    }
}
