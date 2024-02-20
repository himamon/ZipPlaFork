using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ZipPla
{
    public class ToneCurves
    {
        readonly uint[] vTable, rTable, gTable, bTable;
        static readonly HashSet<string> BlackList = new HashSet<string>();

        ToneCurves() { }
        ToneCurves(uint[] vTable, uint[] rTable, uint[] gTable, uint[] bTable)
        {
            this.vTable = vTable;
            this.rTable = rTable;
            this.gTable = gTable;
            this.bTable = bTable;
        }

        public static ToneCurves FromFile(string path) => FromFile(path, testMode: false);
        public static bool FromFileTest(string path) => FromFile(path, testMode: true) != null;

        static ToneCurves FromFile(string path, bool testMode)
        {
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return FromStream(fs, testMode);
            }
        }

        static ToneCurves FromStream(Stream stream, bool testMode)
        {
            Tuple<int, int>[] vPairs = null, rPairs = null, gPairs = null, bPairs = null;
            var index = 0;
            var blackListKey = new StringBuilder();
            using (var reader = new StreamReader(stream))
            {
                while (index < 4)
                {
                    var line = reader.ReadLine();
                    var pairs = LineToIntegerPairs(line, testMode);
                    if (pairs != null)
                    {
                        if (!testMode)
                        {
                            switch (index)
                            {
                                case 0: vPairs = pairs; break;
                                case 1: rPairs = pairs; break;
                                case 2: gPairs = pairs; break;
                                case 3: bPairs = pairs; break;
                            }
                        }
                        blackListKey.AppendLine(line);
                        index++;
                    }
                }
            }
            var key = null as string;
            var success = index >= 4 && !BlackList.Contains(key = blackListKey.ToString());
            if (testMode) return success ? new ToneCurves() : null;
            if (!success) throw new Exception();
            try
            {
                return new ToneCurves(Interpolate(vPairs, 2), Interpolate(rPairs, extraIndexBits - 1),
                    Interpolate(gPairs, extraIndexBits - 1), Interpolate(bPairs, extraIndexBits - 1));
            }
            catch
            {
                BlackList.Add(key);
                throw;
            }
        }

        const int extraIndexBits = 2;
        static uint[] Interpolate(Tuple<int, int>[] pairs, int extraValueBits)
        {
            var n = pairs.Length - 1;
            var x = new int[n + 1];
            var a = new int[n + 1];
            for (var i = 0; i <= n; i++)
            {
                var pair = pairs[i];
                x[i] = pair.Item1;
                a[i] = pair.Item2;
            }
            var h = new int[n];
            for (var i = 0; i < n; i++) h[i] = x[i + 1] - x[i];
            var s = new int[n];
            for (var i = 0; i < n; i++) s[i] = a[i + 1] - a[i];
            var A = new double[n];
            for (var i = 1; i < n; i++)
            {
                A[i] = (3 * (s[i] * h[i - 1] - s[i - 1] * h[i])) / (double)(h[i] * h[i - 1]);
            }
            var l = new double[n + 1]; l[0] = 1; l[n] = 1;
            var m = new double[n];
            var z = new double[n + 1];
            for (var i = 1; i < n; i++)
            {
                l[i] = 2 * (x[i + 1] - x[i - 1]) - h[i - 1] * m[i - 1];
                m[i] = h[i] / l[i];
                z[i] = (A[i] - h[i - 1] * z[i - 1]) / l[i];
            }
            var b = new double[n];
            var c = new double[n + 1];
            var d = new double[n];
            for (var j = n - 1; j >= 0; j--)
            {
                c[j] = z[j] - m[j] * c[j + 1];
                b[j] = (s[j] * 3 - h[j] * (c[j + 1] + 2 * c[j]) * h[j]) / (3 * h[j]);
                d[j] = (c[j + 1] - c[j]) / (3 * h[j]);
            }

            var inputScale = 1.0 / (1 << extraIndexBits);
            var outputScale = (double)(1 << extraValueBits);
            var outputBound = 255U << extraValueBits;
            var count = (255 << extraIndexBits) + 1;
            var result = new uint[count + 1];
            var k = 0;
            var xk = x[0];
            var xk1 = x[1];
            var ak = a[0];
            var bk = b[0];
            var ck = c[0];
            var dk = d[0];
            for (var v = 0; v < count; v++)
            {
                var vd = v * inputScale;
                if (vd > xk1)
                {
                    k++;
                    xk = xk1;
                    xk1 = x[k + 1];
                    ak = a[k];
                    bk = b[k];
                    ck = c[k];
                    dk = d[k];
                }
                var e = vd - xk;
                var r = ak + (bk + (ck + dk * e) * e) * e;
                if (r < 0)
                {
                    result[v] = 0;
                }
                else
                {
                    var ri = (uint)(r * outputScale + 0.5);
                    result[v] = ri < outputBound ? ri : outputBound;
                }
            }
            result[count] = result[count - 1];

            return result;
        }

        const string LineDigitPattern = "-1|[0-9]|[1-9][0-9]|1[0-9][0-9]|2[0-4][0-9]|25[0-5]";
        static readonly Regex LineRegex = new Regex(
            @"^\s*(?:(" + LineDigitPattern + @")\s+(" + LineDigitPattern + @")(?:\s+|$))+$", RegexOptions.Compiled);
        static Tuple<int, int>[] LineToIntegerPairs(string line, bool testMode)
        {
            var m = LineRegex.Match(line);
            if (m.Success)
            {
                var g = m.Groups;
                var g1 = g[1].Captures;
                var g2 = g[2].Captures;
                var count = g1.Count;
                var resultList = new List<Tuple<int, int>>();
                for (var i = 0; i < count; i++)
                {
                    var v1 = g1[i].Value; if (v1 == "-1") continue;
                    var v2 = g2[i].Value; if (v2 == "-1") continue;
                    if (!testMode) resultList.Add(Tuple.Create(int.Parse(v1), int.Parse(v2)));
                }
                return resultList.ToArray();
            }
            return null;
        }

        public unsafe void ApplyToValueForBrightness(byte* bgr)
        {
            var vInRoundIndex = ((1920620U * bgr[0] + 9841690U * bgr[1] + 5014906U * bgr[2])
                + (1 << (23 - extraIndexBits))) >> (24 - extraIndexBits);
            var r = rTable[vInRoundIndex];
            var g = gTable[vInRoundIndex];
            var b = bTable[vInRoundIndex];
            var v2 = GetValue2(r, g, b);
            var vOut = vTable[v2];
            if (vOut == 0)
            {
                var vOutByte = (byte)((vOut + (1 << 1)) >> 2);
                bgr[0] = vOutByte;
                bgr[1] = vOutByte;
                bgr[2] = vOutByte;
            }
            else
            {
                var v4 = v2 << 1;
                uint vOutByte;
                vOutByte = (b * vOut + v2) / v4;
                bgr[0] = vOutByte < 256 ? (byte)vOutByte : (byte)255;
                vOutByte = (g * vOut + v2) / v4;
                bgr[1] = vOutByte < 256 ? (byte)vOutByte : (byte)255;
                vOutByte = (r * vOut + v2) / v4;
                bgr[2] = vOutByte < 256 ? (byte)vOutByte : (byte)255;
            }
        }
        
        static uint GetValue2(uint r, uint g, uint b)
            => b > g ? r > b ? r + g : r > g ? b + g : b + r : r > g ? r + b : r > b ? g + b : g + r;
    }
}
