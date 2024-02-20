using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ZipPla
{
    public class GifFileData
    {
        public enum GifVersion
        {
            GIF87a, GIF89a
        };
        public GifVersion Version; public int iSWidth; public int iSHeight;
        public System.Collections.Generic.List<GifImageData> ImageDatas = new List<GifImageData>();
    }
    public class GifImageData
    {
        public int iImgLeftPosition; public int iImgTopPosition; public int iImgWidth; public int iImgHeight; public bool bInterlace; public bool bLPSF; public int iSLCT; public byte[] LCT;
        public enum DisposalMothod : int
        {
            Nothing = 0,
            Override,
            DrawWithBackgroundColor,
            Playback
        }
        public DisposalMothod DM; public bool bUIF; public bool bTCF; public int iDelayTime; public int iTransColor;
    }
    public class GifAnalyzer
    {
        public static GifFileData Analyz(System.IO.Stream fs)
        {
            //int len = (int)fs.Length;
            //byte[] buf = new byte[len];
            //fs.Read(buf, 0, len);
            var buf = ToBytes(fs);
            var len = buf.Length;
            
            GifFileData Fdata = new GifFileData();

            IntPtr pt = Marshal.AllocHGlobal(len);
            try
            {
                Marshal.Copy(buf, 0, pt, len);
                unsafe
                {
                    Byte* pB = (Byte*)pt.ToPointer();
                    Byte* pBend = (Byte*)(pB + len);
                    if (*pB == 0x47 && *(pB + 1) == 0x49 && *(pB + 2) == 0x46)
                    {
                        pB += 3;
                    }
                    else
                    {
                        throw new Exception("Not GIF");
                    }
                    pB += 3; pB += 2; pB += 2; int n = *pB;
                    bool bGCT = ((n >> 7) & 0x1) == 1 ? true : false;
                    int iCRes = ((n >> 4) & 0x7); int iSGCT = n & 0x7;
                    pB++; pB++; pB++; if (bGCT == true)
                    {
                        pB += (2 << iSGCT) * 3;
                    }

                    GifImageData Idata;
                    Idata = new GifImageData();
                    while (pB < pBend)
                    {
                        switch (*pB)
                        {
                            case 0x21:
                                pB++;
                                switch (*pB)
                                {
                                    case 0xF9:
                                        pB = AnalyzGraficExt(pB, Idata);
                                        break;
                                    case 0xFF:
                                        pB = AnalyzAppliExt(pB);
                                        break;
                                    case 0xFE:
                                        pB = AnalyzCommentExt(pB);
                                        break;
                                }
                                break;
                            case 0x2c:
                                pB = AnalyzImage(pB, Idata);
                                Fdata.ImageDatas.Add(Idata);
                                Idata = new GifImageData();
                                break;
                            case 0x3b:
                                pB = pBend;
                                break;
                            default:
                                throw new Exception("Analyz");
                        }
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pt);
            }
            return Fdata;
        }
        private static byte[] ToBytes(System.IO.Stream stream)
        {
            if (stream is System.IO.MemoryStream || stream is System.IO.FileStream || stream is SeekableStream && stream.Length <= int.MaxValue)
            {
                var len = checked((int)(stream.Length - stream.Position));
                var result = new byte[len];
                var position = 0;
                while (position < len)
                {
                    var read = stream.Read(result, position, len - position);
                    if (read <= 0) throw new Exception("stream could not read to its length");
                    position += read;
                }
                return result;
            }
            else
            {
                const int bufferSize = 81920;
                var buffer = new byte[bufferSize];
                int read;
                var resultList = new List<byte>();
                while ((read = stream.Read(buffer, 0, bufferSize)) > 0)
                {
                    if (read == bufferSize)
                    {
                        resultList.AddRange(buffer);
                    }
                    else
                    {
                        var buff2 = new byte[read];
                        Array.Copy(buffer, buff2, read);
                        resultList.AddRange(buff2);
                    }
                }
                return resultList.ToArray();
            }
        }
        unsafe static private Byte* AnalyzGraficExt(Byte* pB, GifImageData Gdata)
        {
            pB++; pB++; int n = *pB;
            Gdata.DM = (GifImageData.DisposalMothod)((n >> 2) & 0x7);
            Gdata.bUIF = ((n >> 1) & 0x1) == 1 ? true : false;
            Gdata.bTCF = (n & 0x1) == 1 ? true : false;
            pB++; Gdata.iDelayTime = *((short*)pB);
            pB++;
            pB++; Gdata.iTransColor = *pB;
            pB++; if (*pB == 0)
            {
                pB++;
            }
            else
            {
                throw new Exception("AnalyzGraficExt");
            }
            return pB;
        }
        unsafe static private Byte* AnalyzAppliExt(Byte* pB)
        {
            pB++;
            pB++;
            pB += 11;
            while (*pB != 0)
            {
                int size;
                size = *pB;
                pB++;
                pB += size;
            }
            pB++;
            return pB;
        }
        unsafe static private Byte* AnalyzCommentExt(Byte* pB)
        {
            pB++;
            while (*pB != 0)
            {
                int size;
                size = *pB;
                pB++;
                pB += size;
            }
            pB++;
            return pB;
        }
        unsafe static private Byte* AnalyzImage(Byte* pB, GifImageData data)
        {
            pB++; data.iImgLeftPosition = *(short*)pB;
            pB += 2; data.iImgTopPosition = *(short*)pB;
            pB += 2; data.iImgWidth = *(short*)pB;
            pB += 2; data.iImgHeight = *(short*)pB;
            pB += 2; int n = *pB;
            bool bLCTF = ((n >> 7) & 0x1) == 1 ? true : false;
            data.bInterlace = ((n >> 6) & 0x1) == 1 ? true : false;
            data.bLPSF = ((n >> 5) & 0x1) == 1 ? true : false;
            data.iSLCT = 2 << (n & 0x7);
            pB++; if (bLCTF == true)
                pB += data.iSLCT * 3; pB++;
            while (*pB != 0)
            {
                int size;
                size = *pB;
                pB++;
                pB += size;
            }
            pB++;
            return pB;
        }
    }
}
