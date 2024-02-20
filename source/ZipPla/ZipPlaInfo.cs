using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ZipPla
{
    class ZipPlaInfo
    {
        public bool CoverInfoForArchive = true;

        // Length >= 1 であること
        private static readonly ZipPlaInfoParam[] zipPlaInfoParamsPrototype = new ZipPlaInfoParam[]
        {
            //new ZipPlaInfoParam("c", @"(\d{1,9})(\.\d+)?", // \d は全角の数字なども含む
            new ZipPlaInfoParam("c", @"([0-9]{1,9})(\.[0-9]+)?",
                (s)=>Tuple.Create(int.Parse(s[0]), string.IsNullOrEmpty(s[1])?0.0:double.Parse("0" + s[1])),
                (o)=>
                {
                    var t = o as Tuple<int, double>;
                    var v1 = t.Item1 < 0 ? 0 : t.Item1;
                    var v2 = t.Item2 < 0 ? 0.0 : t.Item2 > 0.999 ? 0.999 : t.Item2;
                    var result = v1 + v2.ToString("F3").Substring(1);
                    while(result.EndsWith("0")) result = result.Substring(0, result.Length - 1);
                    if(result.EndsWith(".")) result = result.Substring(0, result.Length - 1);
                    return result;
                }),
            new ZipPlaInfoParam("b", @"(r|l|n)?",
                (s)=> { var s0 = s[0].ToLower(); return s0 == "l" ? BindingMode.LeftToRight : s0 == "r" ? BindingMode.RightToLeft : BindingMode.SinglePage; },
                (o)=> {var b = (BindingMode)o; return b == BindingMode.LeftToRight ? "l" : b == BindingMode.RightToLeft ? "r" : "n"; }),
            new ZipPlaInfoParam("r", @"([1-5])",
                (s)=>int.Parse(s[0]),
                (o)=>o.ToString()),
            new ZipPlaInfoParam("t", @"((?:[^,]+,)*[^,]+,?)",
                //(s)=>(from ss in s[0].Split(new char[1] {','}, StringSplitOptions.RemoveEmptyEntries) select ss.Trim()).OrderBy(entry => entry, new LogicalStringComparer()).ToArray(),
                //(o)=>string.Join(",", (o as string[]).OrderBy(entry => entry, new LogicalStringComparer()).ToArray()))
                (s)=>(from ss in s[0].Split(new char[1] {','}, StringSplitOptions.RemoveEmptyEntries) select ss.Trim()).ToArray(),
                (o)=>string.Join(",", o as string[])),
            new ZipPlaInfoParam("d", @"(l|r)?",
                (s)=>s[0].ToLower() == "r" ? BindingMode.LeftToRight : BindingMode.RightToLeft,
                (o)=>((BindingMode)o) == BindingMode.LeftToRight ? "r" : "l"),
        };

        private void FixLeftToRightInfo()
        {
            if (zipPlaInfoParams[4].Value is bool v)
            {
                var p = zipPlaInfoParams[1];
                if(p.Value as bool? == null) p.Value = v;
                zipPlaInfoParams[4].Value = null;
            }
        }

        private readonly ZipPlaInfoParam[] zipPlaInfoParams;
        public Tuple<int, double> ThumbnailInfo
        {
            get => zipPlaInfoParams[0].Value as Tuple<int, double>;
            set { zipPlaInfoParams[0].Value = value; FixLeftToRightInfo(); }
        }
        public BindingMode? BindingModeForSet
        {
            get => zipPlaInfoParams[1].Value as BindingMode? ?? zipPlaInfoParams[4].Value as BindingMode?;
            set
            {
                zipPlaInfoParams[1].Value = value;
                zipPlaInfoParams[4].Value = null;
            }
        }
        public int? Rating
        {
            get => zipPlaInfoParams[2].Value as int?;
            set { zipPlaInfoParams[2].Value = (value == null || (0 < value && value <= 5)) ? value : null; FixLeftToRightInfo(); }
        }
        public string[] TagArray
        {
            get => zipPlaInfoParams[3].Value as string[];
            set { zipPlaInfoParams[3].Value = value; FixLeftToRightInfo(); }
        }
        private readonly Tuple<int, int>[] indexLengthPairs;
        private readonly string path;
        public Tuple<int,int,string>[] GetIndexLengthTagTorios()
        {
#if AUTOBUILD
            try
#endif
            {
                var tags = TagArray;
                if (tags == null) return new Tuple<int, int, string>[0];
                var tagsCount = tags.Length;
                if (tagsCount == 0) return new Tuple<int, int, string>[0];
                var result = new Tuple<int, int, string>[tagsCount];
                var pair = indexLengthPairs[3];
                var index0 = pair.Item1 + 2;
                var pathLength = path.Length;
                while (index0 < pathLength && path[index0] == ' ') index0++;
                var fullLength = pair.Item2 - 2;
                var targetStr = path.Substring(index0, fullLength);
                //if(targetStr.Length < fullLength)
                //{
                //    System.Windows.Forms.MessageBox.Show($"{targetStr}");
                //}
                var charArray = targetStr.ToArray();
                var j = 0;
                int index = 0;
                int index0FullLength = index0 + fullLength;
                for (var i = 0; i < fullLength; i++)
                {
                    if (charArray[i] == ',')
                    {
                        var stop = i;
                        while (stop > 0 && charArray[stop - 1] == ' ') stop--;
                        var length = stop - index;
                        if (length > 0)
                        {
                            result[j++] = new Tuple<int, int, string>(index + index0, length, targetStr.Substring(index, length));
                        }
                        while (i + 1 < fullLength && charArray[i + 1] == ' ') i++;
                        index = i + 1;
                    }
                }
                while (fullLength > 0 && charArray[fullLength - 1] == ' ') fullLength--;
                var len = fullLength - index;
                if (len > 0)
                {
                    result[j++] = new Tuple<int, int, string>(index + index0, len, targetStr.Substring(index, len));
                }
                return result;
            }
#if AUTOBUILD
            catch
            {
                return new Tuple<int, int, string>[0];
            }
#endif
        }

        private static readonly Regex canBeTagRegex = new Regex(@"^(?:[^\s,;\\/:*?""<>{}|]|[^\s,;\\/:*?""<>{}|][^\t,;\\/:*?""<>{}|]*[^\s,;\\/:*?""<>{}|])$", RegexOptions.Compiled);
        public static bool CanBeTag(string tag)
        {
            return canBeTagRegex.IsMatch(tag);
        }
        public static bool CanBeTag(IEnumerable<string> tags)
        {
            return tags.All(tag => canBeTagRegex.IsMatch(tag));
        }
        
        //private static Regex getOnlyRatingRegex = new Regex(@"\{zpi\$(?:r|[^}]*;r)=([1-5]).*\}[^\\\/]*[\\\/]?$", RegexOptions.Compiled);
        private static readonly Regex getOnlyRatingRegex = new Regex(@"\{zpi\$\s*(?:r|[^}\\\/]*;\s*r)\s*=\s*([1-5])\s*(?:;[^}\\\/]*)?\}[^\\\/]*[\\\/]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static int GetOnlyRating(string path)
        {
            var m = getOnlyRatingRegex.Match(path);
            if(m.Success)
            {
                return int.Parse(m.Groups[1].Value);
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// ランダムソートで使用することを想定し正確さよりも速さを優先
        /// </summary>
        public static string GetBasePathRoughly(string path)
        {
            if (path == null) return null;
            var start = path.IndexOf("{zpi$", StringComparison.OrdinalIgnoreCase);
            if (start < 0) return path;
            var startEnd = start + 5;
            if (path.Length <= startEnd) return path;
            if (path.IndexOf('\\', startEnd) >= 0) return path;
            var stop = path.IndexOf('}', startEnd);
            if (stop < 0) return path;
            var stopEnd = stop + 1;
            if (start > 0 && path[start - 1] == ' ') start--;
            return path.Substring(0, start) + path.Substring(stopEnd);
        }

        private static readonly Regex getOnlyPageSequenceFromFullNameRegex = new Regex(@"\{zpi\$\s*(?:b|[^}\\\/]*;\s*b)\s*=\s*(l|r|n)\s*(?:;[^}\\\/]*)?\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static BindingMode? GetOnlyBindingModeFromFullName(string fullName)
        {
            var m = getOnlyPageSequenceFromFullNameRegex.Matches(fullName);
            if (m.Count > 0)
            {
                switch(m[m.Count - 1].Groups[1].Value.ToLower())
                {
                    case "l": return BindingMode.LeftToRight;
                    case "r": return BindingMode.RightToLeft;
                    default: return BindingMode.SinglePage;
                }
            }
            else
            {
                return null;
            }
        }

        private static readonly Regex fileNameRegex;


        static ZipPlaInfo()
        {
            var pettern0 = "(?:";
            foreach (var info in zipPlaInfoParamsPrototype)
            {
                pettern0 += @"(?:" + info.MnemonicPatternPair + ")|";
            }
            pettern0 = pettern0.Substring(0, pettern0.Length - 1) + @")";
            //var fileNameRegexPattern = @"\s*\{\s*zpi\s*\$\s*((?:" + pettern0 + ";)*" + pettern0 + @")?;?\s*\}\s*"; // 前後の空白を吸収
            var fileNameRegexPattern = @"\s?\{\s*zpi\s*\$\s*((?:" + pettern0 + ";)*" + pettern0 + @")?;?\s*\}"; // 頭の空白を最大一つだけ吸収
            fileNameRegex = new Regex(fileNameRegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        public ZipPlaInfo(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }
            this.path = path;
            
            zipPlaInfoParams = new ZipPlaInfoParam[zipPlaInfoParamsPrototype.Length];
            indexLengthPairs = new Tuple<int, int>[zipPlaInfoParamsPrototype.Length];
            for (var i = 0; i < zipPlaInfoParamsPrototype.Length; i++)
            {
                zipPlaInfoParams[i] = zipPlaInfoParamsPrototype[i].Clone() as ZipPlaInfoParam;
            }

            //if (path.EndsWith(Path.DirectorySeparatorChar.ToString())) path = path.Substring(0, path.Length - 1);
            //var m = fileNameRegex.Match(Path.GetFileName(path));
            var pathArray = path.ToArray();
            int sepPos;
            for (sepPos = pathArray.Length - 2; sepPos >= 0; sepPos--) { var c = pathArray[sepPos]; if (c == '\\' || c == '/') break; }
            sepPos++;
            var m = fileNameRegex.Match(sepPos > 0 ? path.Substring(sepPos) : path);
            if (m.Success)
            {
                var index = m.Groups[1].Index;
                foreach (var paramString in m.Groups[1].Value.Split(new char[1] { ';' }, StringSplitOptions.None))
                {
                    for(var i = 0; i < zipPlaInfoParamsPrototype.Length; i++)
                    {
                        var info = zipPlaInfoParams[i];
                        var m2 = info.Regex.Match(paramString);
                        if (m2.Success)
                        {
                            info.SetValue(m2.Groups);
                            indexLengthPairs[i] = Tuple.Create(index + sepPos, paramString.Length);
                            break;
                        }
                    }
                    index += paramString.Length + 1;
                }
            }

        }

        public string GetPathOfCurrentInfo(bool isDir)
        {
            string basePrefix;
            string baseSuffix;
            var dirName = Path.GetDirectoryName(path);
            if (dirName != null) // ルートなどだと null になる
            {
                if (isDir)
                {
                    basePrefix = Path.Combine(dirName, fileNameRegex.Replace(Path.GetFileName(path), ""));
                    baseSuffix = "";
                }
                else
                {
                    basePrefix = Path.Combine(dirName, fileNameRegex.Replace(Path.GetFileNameWithoutExtension(path), ""));
                    baseSuffix = Path.GetExtension(path);
                }
            }
            else
            {
                basePrefix = fileNameRegex.Replace(path, "");
                baseSuffix = "";
            }
            
            var infoString = GetInfoString();
            if (string.IsNullOrEmpty(infoString))
            {
                return basePrefix + baseSuffix;
            }
            else
            {
                return basePrefix + " " + infoString + baseSuffix;
            }
        }

        private string GetInfoString()
        {
            var result = "{zpi$";
            var existsParam = false;
            foreach (var info in zipPlaInfoParams)
            {
                if (info.Value != null)
                {
                    existsParam = true;
                    result += info.GetInfoString() + ";";
                }
            }
            if (!existsParam)
            {
                return null;
            }
            return result.Substring(0, result.Length - 1) + "}";
        }

        private delegate object Parse(string[] str);
        private delegate string DeParse(object obj);

        private class ZipPlaInfoParam : ICloneable
        {
            public readonly string Mnemonic;
            public readonly string Pattern;
            public readonly string MnemonicPatternPair;
            public readonly Regex Regex;
            private readonly Parse Parse;
            private readonly DeParse DeParse;
            public object Value;
            public ZipPlaInfoParam(string mnemonic, string pattern, Parse parse, DeParse deparse)
            {
                Mnemonic = mnemonic;
                Pattern = pattern;
                Parse = parse;
                DeParse = deparse;
                MnemonicPatternPair = @"\s*" + mnemonic + @"\s*=\s*" + pattern + @"\s*";
                // static readonly ではないので Compiled は不適切に見えるが、このコンストラクタは static readonly な ZipPlaInfo.zipPlaInfoParamsPrototype の長さ分しか呼び出されないので問題ない
                Regex = new Regex("^" + MnemonicPatternPair + "$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                Value = null;
            }
            public ZipPlaInfoParam(string mnemonic, string pattern, string mnemonicPatternPair, Regex regex, Parse parse, DeParse deparse)
            {
                Mnemonic = mnemonic;
                Pattern = pattern;
                Parse = parse;
                DeParse = deparse;
                MnemonicPatternPair = mnemonicPatternPair;
                Regex = regex;
                Value = null;
            }

            public void SetValue(GroupCollection g)
            {
                var str = new string[g.Count - 1];
                for (var i = 0; i < str.Length; i++)
                {
                    str[i] = g[i + 1].Value;
                }
                Value = Parse(str);
            }

            public string GetInfoString()
            {
                return Mnemonic + "=" + DeParse(Value);
            }

            public object Clone()
            {
                return new ZipPlaInfoParam(Mnemonic, Pattern, MnemonicPatternPair, Regex, Parse, DeParse);
            }
        }
    }
}
