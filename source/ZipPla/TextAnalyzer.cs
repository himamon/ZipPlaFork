using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ZipPla
{
    public static class TextAnalyzer
    {
        private static readonly Regex GetImportantRange_NgPattern = new Regex(
            @"^anthology$|^magazine$|^journal$|^text$|^textbook$|^book$|^comic$|^cartoon$|^toon$|^manga$|^novel$|^lightweight novel$|light novel^$|^lightnovel$|^artbook$|^pictures$|^photobook$|^photo book$|" +
            @"^movie$|^film$|^video$|^drama$|^cartoon$|^anim(?:e|ation)$|" +
            @"^アンソロジー$|^ｱﾝｿﾛｼﾞｰ$|^雑誌$|^ジャーナル$|^ｼﾞｬｰﾅﾙ$|^テキスト$|^教科書$|^参考書$|^本$|^書籍$|^..誌$|^(?:\d\d.|..|[a-z]\d\d|v)?(?:漫画|マンガ|ﾏﾝｶﾞ|コミック|ｺﾐｯｸ|小説|ライトノベル|ﾗｲﾄﾉﾍﾞﾙ|ラノベ|ﾗﾉﾍﾞ|画集|イラスト集|ｲﾗｽﾄ集|写真集|映画|シネマ|ｼﾈﾏ|邦画|洋画|動画|ドラマ|ﾄﾞﾗﾏ|カートゥーン|ｶｰﾄｩｰﾝ|アニメ(?:ーション)?|ｱﾆﾒ(?:ｰｼｮﾝ)?)$|" +
            @"^[a-z](?:\d+|v)$|^(\s|　)*$|^[0-9 !#$%&'()=\-^~\\|`@{\[+;*:}\]<>,.\/?_]*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex[] GetImportantRange_InportantPatterns = new Regex[] {
            new Regex(@"^(?:\D|(?:\([^)]*?\))|(?:（[^)]*?）)|(?:【[^)]*?】))*?[\[［].+?[(（](?:\s|　)*(.+?)(?:\s|　)*(?:[,，、､×／].+?)?[)）](?:\s|　)*[\]］]", RegexOptions.Compiled),
            new Regex(@"^(?:\D|(?:\([^)]*?\))|(?:（[^)]*?）)|(?:【[^)]*?】))*?[\[［](?:\s|　)*(.+?)(?:\s|　)*(?:[,，、､×／].+?)?[\]］]", RegexOptions.Compiled),
            new Regex(@"(?:^|\s|[)\]）］　])(?:\s|　)*([^()\[\]（）【】［］.0-9]*[^\s　])(?:\s|　)*(?:\s|$|[(\[（［　])", RegexOptions.Compiled),
            new Regex(@"[(（【](?:\s|　)*(.+?)(?:\s|　)*(?:[,，、､×／].+?)?[)）】]", RegexOptions.Compiled),
            //new Regex(@"()", RegexOptions.Compiled), // 選択無しで終わる番人
            new Regex(@"(.*)", RegexOptions.Compiled), // 全選択で終わる番人
        };

        public static Group GetImportantRange(string str)
        {
            var isDir = str.EndsWith(@"\");
            /*
            if (!(isDir || PackedImageLoader.SupportedArchivesPath.IsMatch(str)))
            {
                str = "";
            }
            */
            if (isDir) str = str.Substring(0, str.Length - 1);
            else
            {
                var pos = str.LastIndexOf('.');
                if(pos >= 0)
                {
                    str = str.Substring(0, pos);
                }
            }
            Group result = null;
            foreach (var r in GetImportantRange_InportantPatterns)
            {
                var m = r.Match(str);
                if (m.Success)
                {
                    var group = m.Groups;
                    if (group.Count <= 1) continue;
                    result = group[1];
                    if (GetImportantRange_NgPattern.IsMatch(result.Value)) continue;
                    return result;
                }
            }
            return result;
        }

        public static string TextToWildcard(IEnumerable<string> strings)
        {
            if (strings == null) return "";
            var array = strings is string[] ? strings as string[] : strings.ToArray();
            if (array.Length == 0) return "";
            var array0 = array[0];
            var matchLength = array0.Length;
            var suffixIsNeeded = false;
            for (var i = 1; i < array.Length; i++)
            {
                var arrayi = array[i];
                if(matchLength > arrayi.Length)
                {
                    suffixIsNeeded = true;
                    matchLength = arrayi.Length;
                }
                var lastSpacePos = -1;
                for(var j = 0; j < matchLength; j++)
                {
                    if (array0[j] != arrayi[j])
                    {
                        suffixIsNeeded = true;
                        matchLength = lastSpacePos + 1;
                        break;
                    }
                    if (array0[j] == ' ') lastSpacePos = j;
                }
            }
            if (suffixIsNeeded)
            {
                return array0.Substring(0, matchLength) + "*";
            }
            else
            {
                return array0.Substring(0, matchLength);
            }
        }
    }
}
