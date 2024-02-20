using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.PSD;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipPla
{
    public static class RiosFastPsdDecoder
    {
        public static Bitmap DecodeImage(PsdFile psdFile)
        {
            var w = psdFile.Columns;
            var h = psdFile.Rows;
            Bitmap bitmap = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            try
            {
                var img = bitmap.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                try
                {
                    unsafe
                    {
                        var adr = (byte*)img.Scan0;
                        byte[] data0, data1, data2, data3;
                        var stride = img.Stride;
                        var data = psdFile.ImageData;
                        var linearIndex = 0;
                        switch (psdFile.ColorMode)
                        {
                            case PsdFile.ColorModes.RGB:
                                data0 = data[0];
                                data1 = data[1];
                                data2 = data[2];
                                for (var y = 0; y < h; y++)
                                {
                                    var left = adr + stride * y;
                                    for (var x = 0; x < w; x++)
                                    {
                                        var pixel = left + 3 * x;
                                        pixel[0] = data2[linearIndex];
                                        pixel[1] = data1[linearIndex];
                                        pixel[2] = data0[linearIndex];
                                        linearIndex++;
                                    }
                                }
                                break;
                            case PsdFile.ColorModes.CMYK:
                                data0 = data[0];
                                data1 = data[1];
                                data2 = data[2];
                                data3 = data[3];
                                for (var y = 0; y < h; y++)
                                {
                                    var left = adr + stride * y;
                                    for (var x = 0; x < w; x++)
                                    {
                                        CMYKToRGB(data0[linearIndex], data1[linearIndex], data2[linearIndex], data3[linearIndex], left + 3 * x);
                                        linearIndex++;
                                    }
                                }
                                break;
                            case PsdFile.ColorModes.Multichannel:
                                data0 = data[0];
                                data1 = data[1];
                                data2 = data[2];
                                for (var y = 0; y < h; y++)
                                {
                                    var left = adr + stride * y;
                                    for (var x = 0; x < w; x++)
                                    {
                                        // ライブラリは第4引数が間違っている
                                        //CMYKToRGB(data0[linearIndex], data1[linearIndex], data2[linearIndex], 255, left + 3 * x);
                                        // 高速化のためこちらを使う
                                        CMYToRGB(data0[linearIndex], data1[linearIndex], data2[linearIndex], left + 3 * x);
                                        linearIndex++;
                                    }
                                }
                                break;
                            case PsdFile.ColorModes.Grayscale:
                            case PsdFile.ColorModes.Duotone:
                                data0 = data[0];
                                for (var y = 0; y < h; y++)
                                {
                                    var left = adr + stride * y;
                                    for (var x = 0; x < w; x++)
                                    {
                                        var pixel = left + 3 * x;
                                        var color = data0[linearIndex];
                                        pixel[0] = color;
                                        pixel[1] = color;
                                        pixel[2] = color;
                                        linearIndex++;
                                    }
                                }
                                break;
                            case PsdFile.ColorModes.Indexed:
                                data0 = data[0];
                                data1 = psdFile.ColorModeData;
                                for (var y = 0; y < h; y++)
                                {
                                    var left = adr + stride * y;
                                    for (var x = 0; x < w; x++)
                                    {
                                        var pixel = left + 3 * x;
                                        var index = data0[linearIndex];
                                        pixel[0] = data1[index];
                                        pixel[1] = data1[index + 256];
                                        pixel[2] = data1[index + 512];
                                        linearIndex++;
                                    }
                                }
                                break;
                            case PsdFile.ColorModes.Lab:
                                data0 = data[0];
                                data1 = data[1];
                                data2 = data[2];
                                for (var y = 0; y < h; y++)
                                {
                                    var left = adr + stride * y;
                                    for (var x = 0; x < w; x++)
                                    {
                                        LabToRGB(data0[linearIndex], data1[linearIndex], data2[linearIndex], left + 3 * x);
                                        linearIndex++;
                                    }
                                }
                                break;
                        }
                    }
                    return bitmap;
                }
                finally
                {
                    bitmap.UnlockBits(img);
                }
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
        }

        private static unsafe void CMYKToRGB(Byte c, Byte m, Byte y, Byte k, byte* adr)
        {
            int k2 = k;
            int color;
            color = (k2 * y + 127) / 255;
            adr[0] = (byte)(color < 0 ? 0 : color > 255 ? 255 : color);
            color = (k2 * m + 127) / 255;
            adr[1] = (byte)(color < 0 ? 0 : color > 255 ? 255 : color);
            color = (k2 * c + 127) / 255;
            adr[2] = (byte)(color < 0 ? 0 : color > 255 ? 255 : color);

            /*
            color = k2 - ((255 - y) * k2 + 127) / 255; // blue
            adr[0] = (byte)(color < 0 ? 0 : color > 255 ? 255 : color);
            color = k2 - ((255 - m) * k2 + 127) / 255; // green
            adr[1] = (byte)(color < 0 ? 0 : color > 255 ? 255 : color);
            color = k2 - ((255 - c) * k2 + 127) / 255; // red
            adr[2] = (byte)(color < 0 ? 0 : color > 255 ? 255 : color);
            */
        }

        private static unsafe void CMYToRGB(Byte c, Byte m, Byte y, byte* adr)
        {
            const int k2 = 255;
            int color;
            color = (k2 * y + 127) / 255;
            adr[0] = (byte)(color < 0 ? 0 : color > 255 ? 255 : color);
            color = (k2 * m + 127) / 255;
            adr[1] = (byte)(color < 0 ? 0 : color > 255 ? 255 : color);
            color = (k2 * c + 127) / 255;
            adr[2] = (byte)(color < 0 ? 0 : color > 255 ? 255 : color);

            /*
            color = k2 - ((255 - y) * k2 + 127) / 255; // blue
            adr[0] = (byte)(color < 0 ? 0 : color > 255 ? 255 : color);
            color = k2 - ((255 - m) * k2 + 127) / 255; // green
            adr[1] = (byte)(color < 0 ? 0 : color > 255 ? 255 : color);
            color = k2 - ((255 - c) * k2 + 127) / 255; // red
            adr[2] = (byte)(color < 0 ? 0 : color > 255 ? 255 : color);
            */
        }

        private static unsafe void LabToRGB(Byte lb, Byte ab, Byte bb, byte* adr)
        {
            Double exL = lb;
            Double exA = ab;
            Double exB = bb;

            const Double lCoef = 256.0 / 100.0;
            const Double aCoef = 256.0 / 256.0;
            const Double bCoef = 256.0 / 256.0;

            Int32 l = (Int32)(exL / lCoef);
            Int32 a = (Int32)(exA / aCoef - 128.0);
            Int32 b = (Int32)(exB / bCoef - 128.0);

            // For the conversion we first convert values to XYZ and then to RGB
            // Standards used Observer = 2, Illuminant = D65

            const Double refX = 95.047;
            const Double refY = 100.000;
            const Double refZ = 108.883;

            Double varY = (l + 16.0) / 116.0;
            Double varX = a / 500.0 + varY;
            Double varZ = varY - b / 200.0;

            varY = Math.Pow(varY, 3) > 0.008856 ? Math.Pow(varY, 3) : (varY - 16 / 116) / 7.787;
            varX = Math.Pow(varX, 3) > 0.008856 ? Math.Pow(varX, 3) : (varX - 16 / 116) / 7.787;
            varZ = Math.Pow(varZ, 3) > 0.008856 ? Math.Pow(varZ, 3) : (varZ - 16 / 116) / 7.787;

            Double x = refX * varX;
            Double y = refY * varY;
            Double z = refZ * varZ;

            XYZToRGB(x, y, z, adr);
        }

        private static unsafe void XYZToRGB(Double x, Double y, Double z, byte* adr)
        {
            // Standards used Observer = 2, Illuminant = D65
            // ref_X = 95.047, ref_Y = 100.000, ref_Z = 108.883
            Double varX = x / 100.0;
            Double varY = y / 100.0;
            Double varZ = z / 100.0;

            Double varR = varX * 3.2406 + varY * (-1.5372) + varZ * (-0.4986);
            Double varG = varX * (-0.9689) + varY * 1.8758 + varZ * 0.0415;
            Double varB = varX * 0.0557 + varY * (-0.2040) + varZ * 1.0570;

            varR = varR > 0.0031308 ? 1.055 * (Math.Pow(varR, 1 / 2.4)) - 0.055 : 12.92 * varR;
            varG = varG > 0.0031308 ? 1.055 * (Math.Pow(varG, 1 / 2.4)) - 0.055 : 12.92 * varG;
            varB = varB > 0.0031308 ? 1.055 * (Math.Pow(varB, 1 / 2.4)) - 0.055 : 12.92 * varB;

            Int32 nRed = (Int32)(varR * 256.0);
            Int32 nGreen = (Int32)(varG * 256.0);
            Int32 nBlue = (Int32)(varB * 256.0);

            nRed = nRed > 0 ? nRed : 0;
            nRed = nRed < 255 ? nRed : 255;

            nGreen = nGreen > 0 ? nGreen : 0;
            nGreen = nGreen < 255 ? nGreen : 255;

            nBlue = nBlue > 0 ? nBlue : 0;
            nBlue = nBlue < 255 ? nBlue : 255;
            
            adr[0] = (byte)(nBlue < 0 ? 0 : nBlue > 255 ? 255 : nBlue);
            adr[1] = (byte)(nGreen < 0 ? 0 : nGreen > 255 ? 255 : nGreen);
            adr[2] = (byte)(nRed < 0 ? 0 : nRed > 255 ? 255 : nRed);
        }
    }
}
