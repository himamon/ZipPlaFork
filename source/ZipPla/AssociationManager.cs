using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZipPla
{
    public static class AssociationManager
    {
        public static string GetExecutable(string path, string extra = "open")
        {
            var extension = Path.GetExtension(path);
            string typeRoot;

            // http://d.hatena.ne.jp/hotokediary/20150114/1421190538
            // https://www.glamenv-septzen.net/view/14
            // 上記を参考に FileExts 内に command と Progid は存在しないと仮定
            // また「4. HKCR\.***\OpenWithProgIdsのアルファベット順で一番上のキー」は安全性のため無視
            //（このメソッドが null を返しても大きな問題はないが誤ったパスを返すと正常に関連付けが起動できないため）
            if (extension.Any() && (
                TryGetCommandFromTypeRoot(ToDefaultTypeRoot(GetUserChoiceType(ToUserTypeRoot(extension))), extra, out var result) ||
                TryGetCommandFromTypeRoot(typeRoot = ToDefaultTypeRoot(extension), extra, out result) ||
                TryGetCommandFromTypeRoot(ToDefaultTypeRoot(GetDefaultType(typeRoot)), extra, out result)))
            {
                return result;
            }
            else
            {
                return null;
            }

            // 自然と思われる順
            // null を返さないことが最優先の場合に利用
            /*
            if (extension.Any() && (
                TryGetCommandFromTypeRoot(typeRoot = ToUserTypeRoot(extension), extra, out result) ||
                TryGetCommandFromType(GetUserChoiceType(typeRoot), extra, out result) ||
                TryGetCommandFromTypeRoot(typeRoot = ToDefaultTypeRoot(extension), extra, out result) ||
                TryGetCommandFromType(GetDefaultType(typeRoot), extra, out result)))
            {
                return result;
            }
            else
            {
                return null;
            }
            */
        }

        private static string ToUserTypeRoot(string type)
        {
            return type == null ? null :
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\" + type;
        }

        private static string GetUserChoiceType(string userTypeRoot)
        {
            return userTypeRoot == null ? null :
                Registry.GetValue(userTypeRoot + @"\UserChoice", "Progid", null) as string;
        }

        private static string ToDefaultTypeRoot(string type)
        {
            return @"HKEY_CLASSES_ROOT\" + type;
        }

        private static string GetDefaultType(string defaultTypeRoot)
        {
            return defaultTypeRoot == null ? null :
                Registry.GetValue(defaultTypeRoot, "", null) as string;
        }
        
        private static bool TryGetCommandFromTypeRoot(string typeRoot, string extra, out string result)
        {
            // <extra>\command が見つからなければ解決できないものとする
            if (typeRoot == null)
            {
                result = null;
                return false;
            }
            else
            {
                result = Registry.GetValue(typeRoot + @"\shell\" + extra + @"\command", "", null) as string;
                return true;
            }
            
            // <extra>\command が見つからなければ次の候補に移る
            /*
            result = typeRoot == null ? null :
                Registry.GetValue(typeRoot + @"\shell\" + extra + @"\command", "", null) as string;
            return result != null;
            */
        }

        private static bool TryGetCommandFromType(string type, string extra, out string result)
        {
            result = null;
            return type != null &&
                TryGetCommandFromTypeRoot(ToUserTypeRoot(type), extra, out result) ||
                TryGetCommandFromTypeRoot(ToDefaultTypeRoot(type), extra, out result);
        }
    }
}
