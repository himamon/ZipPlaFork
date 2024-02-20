#define USEFUZZYCOMPARE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ZipPla
{
    public class SearchManager
    {
        private readonly Regex[] regexArray;
#if USEFUZZYCOMPARE
        private readonly FuzzyWildcard[] fuzzyWildcardArray;
#endif
        private readonly string[] simpleMatchArray;
        private readonly int[] ratingStarsArray;
        private enum boolOperator { And, Or, Not, OrNot }
        private readonly boolOperator[] boolOperatorArray;
        private enum compareOperator
        { RatingEqual, RatingLessThanOrEqual, RatingLessThan, RatingGreaterThan, RatingGreaterThanOrEqual }
        private readonly compareOperator[] compareOperatorArray;

        // Compiled は開放されないので static readonly 以外で使うのは適切ではない
        // https://msdn.microsoft.com/ja-jp/library/8zbs0h2f(v=vs.110).aspx
        /*
        public static Regex GetCompiledRegex(string text, bool compile = true)
        {
            return new Regex(IsRegex.Replace(text, "$1"), (IsIgnoreCaseRegex.IsMatch(text) ?
                    RegexOptions.IgnoreCase : RegexOptions.None) | (compile ? RegexOptions.Compiled : 0));
        }
        */

        public static Regex GetRegex(string text)
        {
            return new Regex(IsRegex.Replace(text, "$1"), (IsIgnoreCaseRegex.IsMatch(text) ?
                    RegexOptions.IgnoreCase : RegexOptions.None));
        }
        
        public SearchManager(string text)
        {
            if (IsRegex.IsMatch(text))
            {
                regexArray = new Regex[1] { GetRegex(text) };
                simpleMatchArray = new string[1];
#if USEFUZZYCOMPARE
                fuzzyWildcardArray = new FuzzyWildcard[1];
#endif
                boolOperatorArray = new boolOperator[1] { boolOperator.And };
            }
            else
            {
                var array = Split(text);
                var length = array.Length;
                regexArray = new Regex[length];
                simpleMatchArray = new string[length];
#if USEFUZZYCOMPARE
                fuzzyWildcardArray = new FuzzyWildcard[length];
#endif
                ratingStarsArray = new int[length];
                boolOperatorArray = new boolOperator[length];
                compareOperatorArray = new compareOperator[length];
                for (var i = 0; i < length; i++)
                {
                    var str = array[i];
                    var strLen = str.Length;
                    if (strLen > 2 && str.StartsWith("+-"))
                    {
                        str = str.Substring(2);
                        boolOperatorArray[i] = boolOperator.OrNot;
                    }
                    else if (strLen > 1 && str.StartsWith("+"))
                    {
                        str = str.Substring(1);
                        boolOperatorArray[i] = boolOperator.Or;
                    }
                    else if (strLen > 1 && str.StartsWith("-"))
                    {
                        str = str.Substring(1);
                        boolOperatorArray[i] = boolOperator.Not;
                    }
                    else
                    {
                        boolOperatorArray[i] = boolOperator.And;
                    }

                    var ratingMatch = IsRatingFilter.Match(str);
                    if (ratingMatch.Success)
                    {
                        ratingStarsArray[i] = int.Parse(ratingMatch.Groups[2].Value);
                        switch (ratingMatch.Groups[1].Value)
                        {
                            case "=":
                                compareOperatorArray[i] = compareOperator.RatingEqual;
                                break;
                            case "<":
                                compareOperatorArray[i] = compareOperator.RatingLessThan;
                                break;
                            case "<=":
                                compareOperatorArray[i] = compareOperator.RatingLessThanOrEqual;
                                break;
                            case ">":
                                compareOperatorArray[i] = compareOperator.RatingGreaterThan;
                                break;
                            case ">=":
                                compareOperatorArray[i] = compareOperator.RatingGreaterThanOrEqual;
                                break;
                        }
                        //regexArray[i] = null;
                    }
                    else
                    {
                        string wildcard = str.Replace("\"", "");
                        if (includesInvalidPathChars(wildcard)) throw new FormatException();
                        wildcard = wildcardTrim(wildcard);
                        if (wildcard.Contains("*") || wildcard.Contains("?"))
                        {
#if USEFUZZYCOMPARE
                            fuzzyWildcardArray[i] = new FuzzyWildcard("*" + wildcard + "*");
#else
                            regexArray[i] = wildcardToRegex(wildcard);
#endif
                        }
                        else
                        {
                            simpleMatchArray[i] = wildcard.ToLower();
                        }
                    }
                }
            }
        }

        private static readonly Regex IsRatingFilter = new Regex(@"^r(=|<|>|<=|>=)([0-9]+)$", RegexOptions.Compiled);
        public static readonly Regex IsRatingFilterWithOrWithoutOperator = new Regex(@"^((?:\+|\+-|-)?)r(=|<|>|<=|>=)([0-9]+)$", RegexOptions.Compiled);

        public static string TrimAndPutQuotationIfNeeded(string text, IEnumerable<string> stopwords)
        {
            text = text.Trim();
            if (
                text.Contains(' ') ||
                text.StartsWith("+") ||
                text.StartsWith("-") ||
                IsRatingFilter.IsMatch(text) ||
                stopwords != null && stopwords.Contains(text)
                )
            {
                return $"\"{text}\"";
            }
            else
            {
                return text;
            }
        }

        public bool Match(string name)
        {
            if (name == null) name = "";
            var count = regexArray.Length;
            var result = true;
            for (var i = 0; i < count; i++)
            {
                var op = boolOperatorArray[i];
                switch (op)
                {
                    case boolOperator.And:
                    case boolOperator.Not:
                        if (!result) continue;
                        break;
                    case boolOperator.Or:
                    case boolOperator.OrNot:
                        if (result) continue;
                        break;
                }

                bool r;
                var simple = simpleMatchArray[i];
                // IndexOf(*, StringComparision.Ordinal) は Contains と同じ
                // 今は IgnoreCase が必要なので OrdinalIgnoreCase を使う
                // Ordinal から始まるものが最もパフォーマンスが高く間違いが起こりにくい
                // http://dobon.net/vb/dotnet/string/stringindexof.html
#if USEFUZZYCOMPARE
                if (simple != null) r = FuzzyWildcard.IndexOf(name, simple) >= 0;
#else
                if (simple != null) r = name.IndexOf(simple, StringComparison.OrdinalIgnoreCase) >= 0;
#endif
                else
                {
#if USEFUZZYCOMPARE
                    var wild = fuzzyWildcardArray[i];
                    if (wild != null) r = wild.IsMatch(name);
                    else
#endif
                    {
                        var regex = regexArray[i];
                        if (regex != null) r = regex.IsMatch(name);
                        else
                        {
                            var rating = ZipPlaInfo.GetOnlyRating(name);
                            if (rating < 0) r = false;
                            else
                            {
                                r = true;
                                switch (compareOperatorArray[i])
                                {
                                    case compareOperator.RatingEqual:
                                        r = rating == ratingStarsArray[i];
                                        break;
                                    case compareOperator.RatingLessThan:
                                        r = rating < ratingStarsArray[i];
                                        break;
                                    case compareOperator.RatingLessThanOrEqual:
                                        r = rating <= ratingStarsArray[i];
                                        break;
                                    case compareOperator.RatingGreaterThan:
                                        r = rating > ratingStarsArray[i];
                                        break;
                                    case compareOperator.RatingGreaterThanOrEqual:
                                        r = rating >= ratingStarsArray[i];
                                        break;
                                }
                            }
                        }
                    }
                }

                switch (op)
                {
                    case boolOperator.And:
                        result = result && r;
                        break;
                    case boolOperator.Or:
                        result = result || r;
                        break;
                    case boolOperator.Not:
                        result = result && !r;
                        break;
                    case boolOperator.OrNot:
                        result = result || !r;
                        break;
                }
            }
            return result;
        }

        //private static readonly Regex IsRegex = new Regex(Path.DirectorySeparatorChar != '/' ? @"^\/(.+)\/[imgy]*$" : @"^\\(.+)\\[imgy]*$", RegexOptions.Compiled); // フルパス検索を実装する場合
        public static readonly Regex IsRegex = new Regex(@"^\/(.+)\/[imgy]*$", RegexOptions.Compiled);
        private static readonly Regex IsIgnoreCaseRegex = new Regex(@"i[imgy]*$", RegexOptions.Compiled);

        /*
        public static Regex[] TextToRegexArrayConverter(string text)
        {
            if (IsRegex.IsMatch(text))
            {
                return new Regex[1] { new Regex(IsRegex.Replace(text, "$1"), (IsIgnoreCaseRegex.IsMatch(text) ? RegexOptions.IgnoreCase : RegexOptions.None) | RegexOptions.Compiled) };
            }
            else
            {
                var array = split(text);
                var length = array.Length;
                var result = new Regex[length];
                for (var i = 0; i < length; i++)
                {
                    var wildcard = array[i];
                    if (includesInvalidPathChars(wildcard)) throw new FormatException();
                    result[i] = wildcardToRegex(wildcard);
                }
                return result;
            }
        }
        */

        public static string[] Split(string str)
        {
            var strArray = str.ToCharArray();
            var length = strArray.Length;
            var outOfDoubleQuotation = true;
            for (var i = 0; i < length; i++)
            {
                var c = strArray[i];
                if (c == '"')
                {
                    outOfDoubleQuotation = !outOfDoubleQuotation;
                    //strArray[i] = '\t';
                }
                else if (outOfDoubleQuotation && c == ' ')
                {
                    strArray[i] = '\t';
                }
            }
            if (!outOfDoubleQuotation) throw new FormatException();
            return (new string(strArray)).Split(new char[1] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string[] Split(string str, out string[] fullTrimmed)
        {
            var simpleSplit = Split(str);
            fullTrimmed = new string[simpleSplit.Length];
            for (var i = 0; i < simpleSplit.Length; i++)
            {
                fullTrimmed[i] =TrimOperatorAndDoubleQuotations(simpleSplit[i]);
            }
            return simpleSplit;
        }
        
        public static string TrimOperatorAndDoubleQuotations(string str)
        {
            return TrimDoubleQuotations(TrimOperator(str));
        }

        public static string TrimOperator(string str)
        {
            if (str == null) return null;
            var length = str.Length;
            if (length < 2) return str;
            var c = str[0];
            if (c == '+')
            {
                if (str[1] == '-')
                {
                    if (length >= 3)
                    {
                        return str.Substring(2);
                    }
                    else
                    {
                        return str;
                    }
                }
                else
                {
                    return str.Substring(1);
                }
            }
            else if (c == '-')
            {
                return str.Substring(1);
            }
            else
            {
                return str;
            }
        }

        public static string TrimDoubleQuotations(string str)
        {
            if (str != null && str.Length >= 3 && str.First() == '"' && str.Last() == '"')
            {
                return str.Substring(1, str.Length - 2);
            }
            else
            {
                return str;
            }
        }

        private static readonly char[] invalidPathChars = Path.GetInvalidPathChars();
        private static bool includesInvalidPathChars(string str)
        {
            return str.IndexOfAny(invalidPathChars) >= 0;
        }

        private static string wildcardTrim(string wildcard)
        {
            var questions = "";
            var stop = wildcard.Length - 1;
            for (var i = 0; true; i++)
            {
                var c = wildcard[i];
                if (c == '?')
                {
                    questions += "?";
                }
                else if (c != '*')
                {
                    wildcard = questions + wildcard.Substring(i);
                    break;
                }
                if (i == stop)
                {
                    wildcard = questions;
                    break;
                }
            }
            questions = "";
            for (var i = wildcard.Length - 1; true; i--)
            {
                var c = wildcard[i];
                if (c == '?')
                {
                    questions += "?";
                }
                else if (c != '*')
                {
                    wildcard = wildcard.Substring(0, i + 1) + questions;
                    break;
                }
                if (i == 0)
                {
                    wildcard = questions;
                    break;
                }
            }
            return wildcard;
        }

        private static readonly Regex eachCharRegex = new Regex(".", RegexOptions.Compiled);
        private static Regex wildcardToRegex(string wildcard)
        {
            // 動的に作られる正規表現は Compiled とするのは不適切
            // また高負荷時にはパフォーマンスの逆転もあるので Compiled のメリットはほとんどない
            return new Regex(eachCharRegex.Replace(wildcard, wildcardToRegex_charConverter), RegexOptions.IgnoreCase); // | RegexOptions.Compiled);
        }

        private static string wildcardToRegex_charConverter(Match m)
        {
            var c = m.Value;
            switch(c)
            {
                case "?":return ".";
                case "*": return ".*";
                default: return Regex.Escape(c);
            }
        }
    }

    public class FuzzyWildcard
    {
        private int[] qes;
        private bool[] ast;
        private string[] exa;
        private int[] lef;
        private int atl;

        public FuzzyWildcard(string wildcard)
        {
            var qesList = new List<int>();
            var astList = new List<bool>();
            var exaList = new List<string>();
            var len = wildcard.Length;
            var i = 0;
            while (true)
            {
                var q = 0;
                var a = false;
                while (i < len)
                {
                    var c = wildcard[i];
                    if (c == '?')
                    {
                        q++;
                    }
                    else if (c == '*')
                    {
                        a = true;
                    }
                    else break;
                    i++;
                }
                qesList.Add(q);
                if (a) atl = astList.Count;
                astList.Add(a);
                
                if (i >= len) break;

                var exaStart = i;
                while (i < len)
                {
                    var c = wildcard[i];
                    if(c == '?' || c == '*') break;
                    i++;
                }
                exaList.Add(wildcard.Substring(exaStart, i - exaStart));
            }
            qes = qesList.ToArray();
            ast = astList.ToArray();
            exa = exaList.ToArray();

            var j = exa.Length;
            lef = new int[j];
            if(j > 0)
            {
                var l = qes[j--] + exa[j].Length;
                lef[j] = l;
                while (j > 0)
                {
                    l += qes[j--] + exa[j].Length;
                    lef[j] = l;
                }
            }
        }
        private static readonly System.Globalization.CompareInfo ci = System.Globalization.CultureInfo.CurrentCulture.CompareInfo;
        private const System.Globalization.CompareOptions stringComparison =
            System.Globalization.CompareOptions.IgnoreCase |
            System.Globalization.CompareOptions.IgnoreKanaType |
            System.Globalization.CompareOptions.IgnoreNonSpace |
            System.Globalization.CompareOptions.IgnoreWidth;
        public bool IsMatch(string text)
        {
            var count = exa.Length;
            var len = text.Length;
            var i = 0;
            var j = 0;
            while (true)
            {
                i += qes[j];

                if (j >= count)
                {
                    if (ast[j]) return len - i  >= qes[j];
                    else return len - i == qes[j];
                }

                var c = len - i - lef[j];
                if (c < 0) return false;
                var e = exa[j];
                var eLen = e.Length;
                if (ast[j])
                {
                    var p = ci.IndexOf(text, e, i, c + eLen, stringComparison);
                    if (p >= 0) i = p + eLen;
                    else return false;
                }
                else
                {
                    if (ci.IndexOf(text, e, i, eLen, stringComparison) == i) i += eLen;
                    else return false;
                }

                j++;
            }
        }
        /*
        public bool IsMatch(string text)
        {
            var count = exa.Length;
            var len = text.Length;
            var i = 0;
            var j = 0;
            while(true)
            {
                i += qes[j];
                var c = len - i - lef[j];
                if (c < 0) return false;
                var e = exa[j];
                var eLen = e.Length;
                if (ast[j])
                {
                    var p = ci.IndexOf(text, e, i, c + eLen, stringComparison);
                    if (p >= 0) i = p + eLen;
                    else return false;
                }
                else
                {
                    if (ci.IndexOf(text, e, i, eLen, stringComparison) == i) i += eLen;
                    else return false;
                }
                if (++j >= count) break;
            }
            if (ast[j]) return len - i >= qes[j];
            else return len - i == qes[j];
        }
        */

        public static int Compare(string string1, string string2)
        {
            return ci.Compare(string1, string2, stringComparison);
        }

        public static int IndexOf(string source, string value)
        {
            return ci.IndexOf(source, value, stringComparison);
        }
    }
}
