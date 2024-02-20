using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public enum ApplicationProviderUser { MouseClick, EnterKey, AccessKey, ContextMenu, DragAndDrop, OpenFileDialog, Error }
    public class FileInfoForApplicationProvider { public string Path; public bool IsDir; }
    public abstract class ApplicationProvider
    {
        protected Regex supportedFiles, openByClicking;
        //protected string menuTextSuffix = "...";
        protected bool escapeAmpersand = true;
        public virtual MultiFileMode GetMultiFileMode() { return MultiFileMode.Single; }
        public bool Supports(string path, bool isDir)
        {
            return Supports(new FileInfoForApplicationProvider { Path = path, IsDir = isDir });
        }
        public bool Supports(params FileInfoForApplicationProvider[] info)
        {
            if (info == null) return false;
            var count = info.Length;
            if (count == 0 || GetMultiFileMode() == MultiFileMode.Single && count > 1) return false;
            return info.All(i => Supports_Protected(i.Path, i.IsDir));
        }
        protected virtual bool Supports_Protected(string path, bool isDir)
        {
            if (isDir)
            {
                return supportedFiles == null || supportedFiles.IsMatch(path + Path.DirectorySeparatorChar);
            }
            else
            {
                return supportedFiles == null || supportedFiles.IsMatch(path);
            }
        }
        public bool IsDefaultFor(string path, bool isDir)
        {
            return IsDefaultFor(new FileInfoForApplicationProvider { Path = path, IsDir = isDir });
        }
        public bool IsDefaultFor(params FileInfoForApplicationProvider[] info)
        {
            if (info == null) return false;
            var count = info.Length;
            if (count == 0 || GetMultiFileMode() == MultiFileMode.Single && count > 1) return false;
            return info.All(i => IsDefaultFor_Protected(i.Path, i.IsDir));
        }
        protected virtual bool IsDefaultFor_Protected(string path, bool isDir)
        {
            if (isDir)
            {
                return openByClicking == null || openByClicking.IsMatch(path + Path.DirectorySeparatorChar);
            }
            else
            {
                return openByClicking == null || openByClicking.IsMatch(path);
            }
        }
        public ToolStripMenuItem GetToolStripMenuItem(Form form, params string[] path)
        {
            var result = escapeAmpersand ?
                GetToolStripMenuItem((GetMenuText(path) /*+ menuTextSuffix*/).Replace("&", "&&")) :// & エスケープあり
                GetToolStripMenuItem((GetMenuText(path) /*+ menuTextSuffix*/)); // & エスケープなし
            var icon = GetMenuIcon(path, out var iconIsStatic);
            if (icon != null)
            {
                result.Image = icon;
                if (!iconIsStatic)
                {
                    var icon2 = icon;
                    result.Disposed += (sender, e) =>
                    {
                        icon2.Dispose();
                    };
                }
            }
            result.Click += (sender, e) =>
            {
                try
                {
                    Exec(ApplicationProviderUser.ContextMenu, path);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(form, ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            return result;
        }
        protected virtual ToolStripMenuItem GetToolStripMenuItem(string escapedText) { return new ToolStripMenuItem(escapedText); }
        protected virtual string GetMenuText(string[] path) { return Message.OpenIn1.Replace("$1", GetApplicationName()); }
        protected virtual Image GetMenuIcon(string[] path, out bool iconIsStatic) { iconIsStatic = false; return null; }
        public abstract string GetApplicationName();
        public abstract void Exec(ApplicationProviderUser user, params string[] path);
        protected static string GetEntityPath(string path)
        {
            var altSepPos = path.IndexOf(Path.AltDirectorySeparatorChar);
            if (altSepPos < 0)
            {
                return path;
            }
            else
            {
                return path.Substring(0, altSepPos);
            }

        }
        public virtual Keys GetAccessKey() { return Keys.None; }
    }

    public class BuiltInViewerProvider : ApplicationProvider
    {
        public static readonly Image Icon = Properties.Resources.dark.ToBitmap();
        protected override Image GetMenuIcon(string[] path, out bool iconIsStatic)
        {
            iconIsStatic = true;
            return Icon;
        }

        private CatalogForm catalogForm;
        private string alias;
        public BuiltInViewerProvider(CatalogForm catalogForm, string alias, Regex supportedFiles, Regex openByClicking)
        {
            this.catalogForm = catalogForm;
            this.supportedFiles = supportedFiles;
            this.openByClicking = openByClicking;

            escapeAmpersand = false;
            this.alias = alias;
            accessKey = ExternalApplicationProvider.GetAccessKey(alias);
        }

        private Keys accessKey;
        public override Keys GetAccessKey()
        {
            return accessKey;
        }

        protected override bool IsDefaultFor_Protected(string path, bool isDir)
        {
            return TechnicallySupports(path, isDir) && base.IsDefaultFor_Protected(path, isDir);
        }

        public override void Exec(ApplicationProviderUser user, params string[] path)
        {
            if (path == null || path.Length != 1) return;
            catalogForm.OpenInBuiltInViewer(path[0]);
        }

        public override string GetApplicationName()
        {
            //return Message.BuiltInViewer;
            return alias ?? Message.OpenInBuiltInViewer;
        }

        protected override string GetMenuText(string[] path)
        {
            return alias ?? Message.OpenInBuiltInViewer;
        }

        protected override bool Supports_Protected(string path, bool isDir)
        {
            return TechnicallySupports(path, isDir) && base.Supports_Protected(path, isDir);
        }

        private bool TechnicallySupports(string path, bool isDir)
        {
            if (isDir || PackedImageLoader.Supports(path) || ImageLoader.SupportsFullReading(path, catalogForm.RecentFfmpegExists)) return catalogForm.IsNotEmptyAndIsNotLoadError(path);
            else
            {
                var altSepPos = path.IndexOf(Path.AltDirectorySeparatorChar);
                return altSepPos >= 0 && PackedImageLoader.Supports(path.Substring(0, altSepPos));
            }

        }
    }

    public class AssociationProvider : ApplicationProvider
    {
        protected override Image GetMenuIcon(string[] path, out bool iconIsStatic)
        {
            iconIsStatic = false;
            if (path == null || path.Length != 1) return null;
            try
            {
                return FileTypeManager.GetSmallIconBitmap(GetEntityPath(path[0]), useFileAttrinutes: false); // フォルダは false 必須
            }
            catch
            {
                return null;
            }
        }

        CatalogForm catalogForm;

        private string alias;
        public AssociationProvider(string alias, Regex supportedFiles, Regex openByClicking, CatalogForm catalogForm)
        {
            this.supportedFiles = supportedFiles;
            this.openByClicking = openByClicking;
            this.catalogForm = catalogForm;

            escapeAmpersand = false;
            this.alias = alias;
            accessKey = ExternalApplicationProvider.GetAccessKey(alias);
        }

        private Keys accessKey;
        public override Keys GetAccessKey()
        {
            return accessKey;
        }

        public override void Exec(ApplicationProviderUser user, params string[] path)
        {
            if (path == null || path.Length != 1) return;

            var entity = GetEntityPath(path[0]);

            try
            {
                VirtualFolder.AddBookmarkData(Program.HistorySorPath, entity, -1, limitOfItemsCount: Program.GetLimitOfHistoryCount(), deleteLostPath: true);
            }
            catch { }
            
            Tuple<string, string> associated;
            try
            {
                var command = AssociationManager.GetExecutable(entity);
                if (command != null)
                {
                    associated = ParseCommand(command);
                }
                else
                {
                    associated = null;
                }
            }
            catch
            {
                associated = null;
            }

            string target;
            string arguments;
            if (associated == null)
            {
                target = entity;
                arguments = null;
            }
            else
            {
                target = associated.Item1;
                var argumentsFormat = associated.Item2;
                arguments = GetArguments(argumentsFormat, entity);
                if (arguments == null) target = entity;
            }

            if (arguments == null)
            {
                var process = Process.Start(target);
                if (process != null) // ほとんど期待できない
                {
                    catalogForm.StopLookAheadProcess();
                    catalogForm.SetOpendByExternalApplication(process, path, selectionMirror: true);
                    catalogForm.PrepareLookAheadProcess();
                }
            }
            else
            {
                catalogForm.StopLookAheadProcess();
                // var process = Process.Start(target, arguments);
                var process = ExternalApplicationProvider.ProcessStartAtParentDirectory(target, arguments);
                catalogForm.SetOpendByExternalApplication(process, path, selectionMirror: true); // 辞書への追加までは同期的。辞書からの削除は非同期的
                catalogForm.PrepareLookAheadProcess();
            }
            
            /*
            if (KeyboardShortcut.GetKeyState(Keys.D) && KeyboardShortcut.GetKeyState(Keys.G))
            {
                var argumentsFormat = associated?.Item2;
                var s = $"t<{target}>";
                if (argumentsFormat != null) s += $"\nf<{argumentsFormat}>";
                if (arguments != null) s += $"\na<{arguments}>";
                var res = MessageBox.Show(catalogForm, s + "\n\nClick OK to copy above to clipboard.",
                    "DEBUG", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
                if (res == DialogResult.OK)
                {
                    Clipboard.SetText(s);
                }
            }
            */
        }

        private static string GetArguments(string argumentsFormat, string path)
        {
            /* http://pf-j.sakura.ne.jp/program/winreg/classes.htm
             * %1	ファイル名(フルパス)が入ります。DesktopBackground や Directory\Background など、選択した項目が存在しない場合に %1 を使用するとアクションの実行に失敗します。
             * %2	printto の場合プリンター名が入ります。
             * %3	printto の場合ドライバー名が入ります。
             * %4	printto の場合ポート番号が入ります。
             * %D	ファイルのデスクトップからの相対パスが入ります(IShellItem::GetDisplayName で SIGDN_DESKTOPABSOLUTEPARSING を指定して取得できるパスが入ります)。仮想アイテムである場合はGUID文字列が含まれるパスとなります。
             * %H	「0」が入ります。(詳細不明; 16進数)
             * %I	ITEMIDLIST(IDLIST_ABSOLUTE) データを指す共有メモリのハンドルと、そのハンドルを作成したプロセスIDが入ります。「:<handle>:<pid>」の形式(いずれも10進数)となります。利用できない場合は「:0」が入ります。
             * %L	ファイル名(フルパス)が入ります。DesktopBackground や Directory\Background など、選択した項目が存在しない場合に %L を使用するとアクションの実行に失敗します。
             * %S	ウィンドウの表示状態(SW_* の値)が入ります。起動時に「SW_SHOWNORMAL」が指定されていれば「1」が入ります。
             * %U	[Windows 7 以降] 対象の IShellItem から IPropertyStore を取得し、PKEY_ExpandoProperties に含まれる OriginURL プロパティーの値を %U にセットします。
             * %V	ファイル名(フルパス)が入ります。%L が使用できる場合は同じ値が、使用できない場合は %W と同じ値が入ります。
             * %W	作業ディレクトリ名が入ります(この値は NoWorkingDirectory の影響を受けません)。対象アイテムがファイルシステムではない場合にこの指定子があるとアクションの実行に失敗します。
             */

            if (string.IsNullOrEmpty(argumentsFormat))
            {
                return '"' + path + '"';
            }

            if (argumentsFormat.Contains("%U")) return null;

            var result = new StringBuilder();
            var formatLength = argumentsFormat.Length;
            var i = 0;
            while (i < formatLength)
            {
                if (i + 1 == formatLength)
                {
                    result.Append(argumentsFormat[i]);
                    break;
                }

                var two = argumentsFormat.Substring(i, 2);
                i += 2;
                switch (two)
                {
                    case "%1": result.Append(path); break;
                    case "%2": break;
                    case "%3": break;
                    case "%4": break;
                    case "%D": result.Append(GetRelativePath(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory, Environment.SpecialFolderOption.DoNotVerify), path)); break;
                    case "%H": result.Append('0'); break;
                    case "%I": result.Append(":0"); break;
                    case "%L": result.Append(path); break;
                    case "%S": result.Append('1'); break;
                    // case "%U":
                    case "%V": result.Append(path); break;
                    case "%W": result.Append(Application.StartupPath); break;
                    default:
                        if (two[1] != '%')
                        {
                            result.Append(two);
                        }
                        else
                        {
                            result.Append(two[0]);
                            i--;
                        }
                        break;
                }
            }
            return result.ToString();
        }

        private static string GetRelativePath(string current, string target)
        {
            if (string.IsNullOrEmpty(current)) return target;
            var cDirs = current.Split(Path.DirectorySeparatorChar);
            var tDirs = target.Split(Path.DirectorySeparatorChar);
            var cLen = cDirs.Length;
            var len = Math.Min(cLen, tDirs.Length);
            var i = 0;
            var p = 0;
            while (i < len && string.Compare(cDirs[i], tDirs[i], ignoreCase: true) == 0) p += tDirs[i++].Length + 1;
            if (p == 0) return target;
            var up = "";
            while (i++ < cLen) up += ".." + Path.DirectorySeparatorChar;
            return up + target.Substring(p);
        }

        public override string GetApplicationName()
        {
            //return Message.AssociatedApplication;
            return alias ?? Message.Open;
        }

        protected override string GetMenuText(string[] path)
        {
            return alias ?? Message.Open;
        }

        public static Tuple <string, string> ParseCommand(string command)
        {
            var inQ = false;
            var space = -1;
            for (var i = 0; i < command.Length - 1; i++)
            {
                var c = command[i];
                if (c == '"') inQ = !inQ;
                else if (c == ' ')
                {
                    if (!inQ)
                    {
                        space = i;
                        break;
                    }
                }
            }
            string path, args;
            if (space < 0)
            {
                path = command;
                args = null;
            }
            else
            {
                path = command.Substring(0, space);
                args = command.Substring(space + 1);
            }
            path = path.Replace("\"", "");
            if (File.Exists(path))
            {
                return new Tuple<string, string>(path, args);
            }
            else
            {
                return null;
            }
        }
    }

    public class MoveLocationProvider : ApplicationProvider
    {
        private static readonly Regex virtualOrSmartRegex =
            new Regex($"\\.(?:sor|{SmartFolder.ExtensionWithoutPeriodInLower})$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static readonly Image Icon = Properties.Resources.light.ToBitmap();
        protected override Image GetMenuIcon(string[] path, out bool iconIsStatic)
        {
            iconIsStatic = true;
            return Icon;
        }

        private CatalogForm catalogForm;
        private string alias;
        public MoveLocationProvider(CatalogForm catalogForm,string alias,  Regex supportedFiles, Regex openByClicking)
        {
            //menuTextSuffix = "";
            this.catalogForm = catalogForm;
            this.supportedFiles = supportedFiles;
            this.openByClicking = openByClicking;

            escapeAmpersand = false;
            this.alias = alias;
            accessKey = ExternalApplicationProvider.GetAccessKey(alias);
        }

        private Keys accessKey;
        public override Keys GetAccessKey()
        {
            return accessKey;
        }
        
        public override void Exec(ApplicationProviderUser user, params string[] path)
        {
            if (path == null || path.Length != 1) return;
            catalogForm.MoveLocation(user, path[0]);
        }

        protected override bool IsDefaultFor_Protected(string path, bool isDir)
        {
            return TechnicallySupports(path, isDir) && base.IsDefaultFor_Protected(path, isDir);
        }

        public override string GetApplicationName()
        {
            //return Message.MoveLocation;
            //return Message.CurrentZipPla;
            return alias ?? Message.OpenInCurrentWindow;
        }

        private static readonly Regex GetMenuText_MoveHereSuitable = new Regex($"\\.(?:sor|{SmartFolder.ExtensionWithoutPeriodInLower})$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        protected override string GetMenuText(string[] path)
        {
            //return alias ?? Message.OpenInCurrentWindow;
            
            if (Subdivide(path) && catalogForm.InMovie) return AppendAccessKey(Message.SubdivideAroundHere, accessKey);
            return alias ?? Message.OpenInCurrentWindow;
            /*
            if (path == null || path.Length != 1) return "";

            var path0 = path[0];

            if (path0 == null) return "";

            try
            {
                if(GetMenuText_MoveHereSuitable.IsMatch(path0) || Directory.Exists(path0))
                {
                    return Message.OpenInCurrentWindow;
                }
                else if (CatalogForm.IsSupportedByCatalogFormExceptFolder(path0)) // ファイルらしい名前のフォルダは存在し得る
                {
                    return Message.ShowThumbnails;
                }
                else
                {
                    return Message.MoveToLocationOfThis;
                }
            }
            catch
            {
                return "";
            }
            */
        }

        private static string AppendAccessKey(string text, Keys accessKey)
        {
            if (text == null) return null;
            if (accessKey == Keys.None) return text;
            var a = accessKey.ToString();
            var i = text.IndexOf(a, StringComparison.OrdinalIgnoreCase);
            if (i < 0)
            {
                return text + "(&" + a + ")";
            }
            else
            {
                return text.Substring(0, i) + "&" + text.Substring(i);
            }
        }

        protected override bool Supports_Protected(string path, bool isDir)
        {
            return TechnicallySupports(path, isDir) && base.Supports_Protected(path, isDir);
        }

        private static bool Subdivide(string[] path)
        {
            if (path == null || path.Length != 1) return false;
            var path0 = path[0];
            var altPos = path0.IndexOf(Path.AltDirectorySeparatorChar);
            if (altPos < 0 || altPos >= path0.Length - 1) return false;
            var parent = path0.Substring(0, altPos);
            return MovieThumbnailLoader.Supports(parent);
        }

        private bool TechnicallySupports(string path, bool isDir)
        {
            if (path == null) return false;
            return isDir || catalogForm.InVirtualDirectory || catalogForm.InSmartDirectory ||
                CatalogForm.IsSupportedByCatalogFormExceptFolder(path) || catalogForm.Subdividable(path);
        }
    }


    public class OpenInFileInClipboardProvider : ApplicationProvider
    {
        protected override Image GetMenuIcon(string[] pathArray, out bool iconIsStatic)
        {
            iconIsStatic = true; // 独自に Dispose するので
            return null;
        }

        private bool multiItems;
        public override MultiFileMode GetMultiFileMode()
        {
            return multiItems ? MultiFileMode.Together : MultiFileMode.Single;
        }

        private CatalogForm catalogForm;
        //private bool supportedFilesIsRegex, openByClickingIsRegex;
        public OpenInFileInClipboardProvider(CatalogForm catalogForm, bool multiItems, Regex supportedFiles, Regex openByClicking)
        {
            this.catalogForm = catalogForm;
            this.supportedFiles = supportedFiles;
            this.openByClicking = openByClicking;

            
            this.multiItems = multiItems;
            this.supportedFiles = supportedFiles;
            this.openByClicking = openByClicking;
            //menuText = GetName(path, Message.OpenIn1, alias); これでは言語切替に追従できない

            //supportedFilesIsRegex = !multiItems && supportedFiles != null && sRegex;
            //openByClickingIsRegex = !multiItems && openByClicking != null && oRegex;
        }

        // ExternalApplicationProvider を参考にしている
        public override void Exec(ApplicationProviderUser user, params string[] path)
        {
            if (path == null || path.Length == 0 || clipboardExecutablePath == null) return;
            try
            {
                VirtualFolder.AddBookmarkData(Program.HistorySorPath, path, Enumerable.Repeat(-1, path.Length).ToArray(), limitOfItemsCount: Program.GetLimitOfHistoryCount(), deleteLostPath: true);
            }
            catch { }

            string cmd;
            if (multiItems)
            {
                cmd = "\"" + string.Join("\" \"", path) + "\"";
            }
            else
            {
                //cmd = cmdParams + " \"" + string.Join("\" \"", from p in path select GetEntityPath(p)) + "\"";
                cmd = "\"" + path[0] + "\""; // 驚き最小の原則に則り GetEntityPath は極力使わない方針で
            }
            catalogForm.StopLookAheadProcess();
            // var process = Process.Start(clipboardExecutablePath, cmd);
            var process = ExternalApplicationProvider.ProcessStartAtParentDirectory(clipboardExecutablePath, cmd);
            catalogForm.SetOpendByExternalApplication(process, path, selectionMirror: true); // 辞書への追加までは同期的。辞書からの削除は非同期的
            catalogForm.PrepareLookAheadProcess();
        }

        protected override ToolStripMenuItem GetToolStripMenuItem(string escapedText)
        {
            return new OpenInFileInClipboardToolStripMenuItem(escapedText);
        }

        class OpenInFileInClipboardToolStripMenuItem : ToolStripMenuItem
        {
            public OpenInFileInClipboardToolStripMenuItem(string text) : base(text) { }

            public void SetIcon(string targetPath)
            {
                DisposeIcon();
                try
                {
                    Image = FileTypeManager.GetSmallIconBitmap(targetPath, useFileAttrinutes: false);
                }
                catch { }
            }

            public void DisposeIcon()
            {
                var image = Image;
                if (image != null)
                {
                    image.Dispose();
                    Image = null;
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) DisposeIcon();
                base.Dispose(disposing);
            }
        }

        protected override bool IsDefaultFor_Protected(string path, bool isDir)
        {
            return base.IsDefaultFor_Protected(path, isDir) && TechnicallySupports(path, isDir);// technicallySupports が高負荷なので後ろ
        }

        public override string GetApplicationName()
        {
            return Message.ExecutableInClipboard;
        }

        private static readonly Regex GetMenuText_MoveHereSuitable = new Regex($"\\.(?:sor|{SmartFolder.ExtensionWithoutPeriodInLower})$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        protected override string GetMenuText(string[] path)
        {
            try
            {
                return Message.OpenIn1.Replace("$1", Path.GetFileNameWithoutExtension(clipboardExecutablePath));
            }
            catch
            {
                return "";
            }
        }
        
        public static void Owner_Opening(object sender, CancelEventArgs e)
        {
            if (clipboardExecutablePath == null) return;
            var toolStrip = sender as ToolStrip;
            if (toolStrip == null) return;
            foreach (var item0 in toolStrip.Items)
            {
                if (item0 is OpenInFileInClipboardToolStripMenuItem item)
                {
                    item.SetIcon(clipboardExecutablePath);
                    return; // 複数はないと仮定
                }
            }
        }

        public static void Owner_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            var toolStrip = sender as ToolStrip;
            if (toolStrip == null) return;
            foreach (var item0 in toolStrip.Items)
            {
                if (item0 is OpenInFileInClipboardToolStripMenuItem item)
                {
                    item.DisposeIcon();
                    return; // 複数はないと仮定
                }
            }
        }

        protected override bool Supports_Protected(string path, bool isDir)
        {
            return base.Supports_Protected(path, isDir) && TechnicallySupports(path, isDir); // technicallySupports が高負荷なので後ろ
        }

        private static bool TechnicallySupports(string path, bool isDir)
        {
            if (path == null) return false;
            SetClipboardExecutablePath();
            return clipboardExecutablePath != null;
        }

        private static string clipboardExecutablePath;
        private static void SetClipboardExecutablePath()
        {
            try
            {
                var data = Clipboard.GetDataObject();
                if (data == null || !data.GetDataPresent(DataFormats.FileDrop))
                {
                    clipboardExecutablePath = null;
                    return;
                }
                var files = data.GetData(DataFormats.FileDrop) as string[];
                if (files == null || files.Length != 1)
                {
                    clipboardExecutablePath = null;
                    return;
                }
                var file = files[0];
                var ext = Path.GetExtension(file);
                if (string.IsNullOrEmpty(ext))
                {
                    clipboardExecutablePath = null;
                    return;
                }
                ext = ext.Substring(1).ToLower();
                if (ext != "exe" && ext != "bat" && ext != "cmd" && ext != "vbs" && ext != "js" && ext != "wsf" && ext != "lnk")
                {
                    clipboardExecutablePath = null;
                    return;
                }
                if (!File.Exists(file))
                {
                    clipboardExecutablePath = null;
                    return;
                }
                clipboardExecutablePath = file;
            }
            catch
            {
                clipboardExecutablePath = null;
            }
        }
    }

    public class ExternalApplicationProvider : ApplicationProvider
    {
        const string Sort = "sort", LastOpenedPage = "hitem";
        static readonly Regex SortRegex = new Regex(@"(?<=(?<!\$)(?:\$\$)*)\$\{" + Sort + @"\}", RegexOptions.Compiled);
        static readonly Regex LastOpenedPageRegex = new Regex(@"(?<=(?<!\$)(?:\$\$)*)\$\{" + LastOpenedPage + @"\}", RegexOptions.Compiled);

        private readonly string path, cmdParams, alias;//menuText;
        private readonly bool cmdContainsSrt, cmdContainsLastOpenedPage;
        private bool multiItems;
        protected override Image GetMenuIcon(string[] pathArray, out bool iconIsStatic)
        {
            iconIsStatic = false;
            if (path == null) return null;
            try
            {
                return FileTypeManager.GetSmallIconBitmap(path, useFileAttrinutes: false);
            }
            catch
            {
                return null;
            }
        }
        public override MultiFileMode GetMultiFileMode()
        {
            return multiItems ? MultiFileMode.Together : MultiFileMode.Single;
        }

        private CatalogForm catalogForm;
        private bool supportedFilesIsRegex, openByClickingIsRegex;
        public ExternalApplicationProvider(CatalogForm catalogForm, string path, string alias, string cmdParams, bool multiItems,
            Regex supportedFiles, bool sRegex, Regex openByClicking, bool oRegex)
        {
            escapeAmpersand = false;

            this.catalogForm = catalogForm;
            this.path = path;
            this.cmdParams = cmdParams;
            this.multiItems = multiItems;
            this.supportedFiles = supportedFiles;
            this.openByClicking = openByClicking;
            //menuText = GetName(path, Message.OpenIn1, alias); これでは言語切替に追従できない
            this.alias = alias;
            accessKey = GetAccessKey(alias);

            supportedFilesIsRegex = !multiItems && supportedFiles != null && sRegex;
            openByClickingIsRegex = !multiItems && openByClicking != null && oRegex;
            if ((supportedFilesIsRegex || openByClickingIsRegex) && cmdParams != null)
            {
                cmdContainsSrt = SortRegex.IsMatch(cmdParams);
                cmdContainsLastOpenedPage = LastOpenedPageRegex.IsMatch(cmdParams);
            }
        }
        
        // OpenInFileInClipboard でも同様の処理
        public override void Exec(ApplicationProviderUser user, params string[] path)
        {
            if (path == null || path.Length == 0) return;
            var page = -1;
            try
            {
                page = VirtualFolder.AddBookmarkData(Program.HistorySorPath, path, Enumerable.Repeat(-1, path.Length).ToArray(), limitOfItemsCount: Program.GetLimitOfHistoryCount(), deleteLostPath: true);
            }
            catch { }

            var contextMenu = user == ApplicationProviderUser.ContextMenu;
            string cmd;
            if (contextMenu && !supportedFilesIsRegex || !contextMenu && !openByClickingIsRegex || path.Length != 1)
            {
                cmd = null;
            }
            else
            {
                var regex = contextMenu ? supportedFiles : openByClicking;
                var groupNames = null as string[];
                var reserves = new List<Tuple<Regex, string>>();
                if (cmdContainsSrt && !(groupNames = regex.GetGroupNames()).Contains(Sort)) reserves.Add(Tuple.Create(SortRegex, catalogForm.GetSortModeString()));
                if (cmdContainsLastOpenedPage && !(groupNames ?? regex.GetGroupNames()).Contains(LastOpenedPage))
                {
                    var added = false;
                    if (page >= 0 && catalogForm.LoadLastViewedPageFromHistory && !path[0].Contains('/') && PackedImageLoader.Supports(path[0]))
                    {
                        try
                        {
                            using (var loader = new PackedImageLoader(path[0], catalogForm.packedImageLoaderLeftHierarchies,
                                catalogForm.packedImageLoaderSearchMode, false, PackedImageLoaderOnMemoryMode.None))
                            {
                                var entries = loader.GetPackedImageEntries();
                                if (page < entries.Count)
                                {
                                    reserves.Add(Tuple.Create(LastOpenedPageRegex, "/" + entries[page].Path));
                                    added = true;
                                }
                            }
                        }
                        catch { }
                    }
                    if (!added)
                    {
                        reserves.Add(Tuple.Create(LastOpenedPageRegex, ""));
                    }
                }
                cmd = Replace(regex, path[0], cmdParams, reserves);
                if (cmd == cmdParams) cmd = null;
            }
            if (cmd == null)
            {
                if (multiItems)
                {
                    cmd = cmdParams + " \"" + string.Join("\" \"", path) + "\"";
                }
                else
                {
                    //cmd = cmdParams + " \"" + string.Join("\" \"", from p in path select GetEntityPath(p)) + "\"";
                    cmd = cmdParams + " \"" + path[0] + "\""; // 驚き最小の原則に則り GetEntityPath は極力使わない方針で
                }
            }

            catalogForm.StopLookAheadProcess();
            var process = ProcessStartAtParentDirectory(this.path, cmd);
            catalogForm.SetOpendByExternalApplication(process, path, selectionMirror: true); // 辞書への追加までは同期的。辞書からの削除は非同期的
            catalogForm.PrepareLookAheadProcess();
        }

        public static Process ProcessStartAtParentDirectory(string fileName, string arguments)
        {
            try
            {
                var info = new ProcessStartInfo(fileName, arguments)
                {
                    WorkingDirectory = Path.GetDirectoryName(fileName)
                };
                return Process.Start(info);
            }
            catch
            {
                return Process.Start(fileName, arguments);
            }
        }

        private static string Replace(Regex regex, string src, string dst, List<Tuple<Regex, string>> reserves)
        {
            if (reserves != null)
            {
                foreach (var reserve in reserves)
                {
                    dst = reserve.Item1.Replace(dst, reserve.Item2.Replace("$", "$$$$"));
                }
            }

            var match = regex.Match(src);
            if (!match.Success) return dst;
            var result = regex.Replace(src, dst, 1);
            return result.Substring(match.Index, match.Length + result.Length - src.Length);
        }
        
        public override string GetApplicationName()
        {
            return GetName(path, Message.OpenIn1, alias);
        }

        public static string GetName(string path, string format, string alias)
        {
            return GetName(path, format, format, alias);
        }

        public static string GetName(string path, string fileFormat, string directoryFormat, string alias)
        {
            if (alias != null) return alias;
            if (string.IsNullOrEmpty(path)) return directoryFormat;
            string name = null;
            if (path.Last() == Path.DirectorySeparatorChar)
            {
                path = path.Substring(0, path.Length - 1);
                try
                {
                    if (Directory.Exists(path)) // path が存在しない場合の GetDisplayName のパフォーマンス低下リスク回避
                    {
                        name = FileTypeManager.GetDisplayName(path).Replace("&", "&&"); // path が存在しない場合例外ではなく空文字を返す
                        //name = addAmpersandForName(FileTypeManager.GetDisplayName(path).Replace("&", "&&"), path); // フォルダにもデフォルトでショートカットキーを割り当てる場合
                    }
                }
                catch
                {
                }
                if (string.IsNullOrEmpty(name))
                {
                    if (!string.IsNullOrEmpty(path) && path.Last() != ':')
                    {
                        try
                        {
                            //name = addAmpersandForName(Path.GetFileName(path).Replace("&", "&&"));
                            name = Path.GetFileName(path).Replace("&", "&&");
                        }
                        catch
                        {
                        }
                    }
                    if (string.IsNullOrEmpty(name))
                    {
                        name = path;
                    }
                }
                return directoryFormat.Replace("$1", name);
            }
            else
            {
                /*
                if (string.Compare(path, SettingForm.ExplorerPath, ignoreCase: true) == 0)
                {
                    return Message.OpenInExplorer;
                }
                else
                */
                {
                    try
                    {
                        //name = addAmpersandForName(Path.GetFileNameWithoutExtension(path).Replace("&", "&&"));
                        name = Path.GetFileNameWithoutExtension(path).Replace("&", "&&");
                    }
                    catch
                    {
                        name = path;
                    }
                    return fileFormat.Replace("$1", name);
                }
            }
        }

        private static string AddAmpersandForName(string originalName)
        {
            return AddAmpersandForName(originalName, null);
        }
        private static string AddAmpersandForName(string originalName, string path)
        {
            if (string.IsNullOrEmpty(originalName)) return originalName;
            var c = originalName.First();
            if ('a' <= c && c <= 'z' || 'A' <= c && c <= 'Z' || '0' <= c && c <= '9')
            {
                return "&" + originalName;
            }
            else if(!string.IsNullOrEmpty(path))
            {
                string fileName;
                try
                {
                    fileName = Path.GetFileName(path);
                }
                catch
                {
                    return originalName;
                }
                if (string.IsNullOrEmpty(fileName)) return originalName;
                c = fileName.First();
                if ('a' <= c && c <= 'z' || 'A' <= c && c <= 'Z' || '0' <= c && c <= '9')
                {
                    return originalName + "(&" + c + ")";
                }
                return originalName;
            }
            else
            {
                return originalName;
            }
        }

        private Keys accessKey;
        public override Keys GetAccessKey()
        {
            return accessKey;
        }

        public static Keys GetAccessKey(string name)
        {
            if (name == null) return Keys.None;
            name = name.Replace("&&", "");
            var len1 = name.Length - 1;
            for (var i = 0; i < len1; i++)
            {
                if (name[i] == '&')
                {
                    var result = CharToKey(name[i + 1]);
                    if (result != Keys.None) return result;
                }
            }
            return Keys.None;
        }

        private static Keys CharToKey(char c)
        {
            switch(c)
            {
                case 'a': case 'A': return Keys.A;
                case 'b': case 'B': return Keys.B;
                case 'c': case 'C': return Keys.C;
                case 'd': case 'D': return Keys.D;
                case 'e': case 'E': return Keys.E;
                case 'f': case 'F': return Keys.F;
                case 'g': case 'G': return Keys.G;
                case 'h': case 'H': return Keys.H;
                case 'i': case 'I': return Keys.I;
                case 'j': case 'J': return Keys.J;
                case 'k': case 'K': return Keys.K;
                case 'l': case 'L': return Keys.L;
                case 'm': case 'M': return Keys.M;
                case 'n': case 'N': return Keys.N;
                case 'o': case 'O': return Keys.O;
                case 'p': case 'P': return Keys.P;
                case 'q': case 'Q': return Keys.Q;
                case 'r': case 'R': return Keys.R;
                case 's': case 'S': return Keys.S;
                case 't': case 'T': return Keys.T;
                case 'u': case 'U': return Keys.U;
                case 'v': case 'V': return Keys.V;
                case 'w': case 'W': return Keys.W;
                case 'x': case 'X': return Keys.X;
                case 'y': case 'Y': return Keys.Y;
                case 'z': case 'Z': return Keys.Z;
                case '0': return Keys.D0;
                case '1': return Keys.D1;
                case '2': return Keys.D2;
                case '3': return Keys.D3;
                case '4': return Keys.D4;
                case '5': return Keys.D5;
                case '6': return Keys.D6;
                case '7': return Keys.D7;
                case '8': return Keys.D8;
                case '9': return Keys.D9;
                default: return Keys.None;
            }
        }

        protected override string GetMenuText(string[] path)
        {
            return GetApplicationName();
        }
    }

    public class MoveItemsProvider : ApplicationProvider
    {
        private string targetPath, targetPathInLowerWithSeparator, targetAlias;
        protected override Image GetMenuIcon(string[] pathArray, out bool iconIsStatic)
        {
            iconIsStatic = false;
            if (targetPath == null) return null;
            try
            {
                return FileTypeManager.GetSmallIconBitmap(targetPath, useFileAttrinutes: false);
            }
            catch
            {
                return null;
            }
        }
        public override MultiFileMode GetMultiFileMode()
        {
            return MultiFileMode.Together;
        }
        private CatalogForm catalogForm;
        public MoveItemsProvider(CatalogForm catalogForm, string targetPath, string targetAlias, Regex supportedFiles, Regex openByClicking)
        {
            escapeAmpersand = false;

            //menuTextSuffix = "";
            this.catalogForm = catalogForm;
            this.targetPath = targetPath;
            targetPathInLowerWithSeparator = targetPath.ToLower();
            if (targetPathInLowerWithSeparator.Length == 0 || targetPathInLowerWithSeparator.Last() != Path.DirectorySeparatorChar)
                targetPathInLowerWithSeparator += Path.DirectorySeparatorChar;
            this.targetAlias = targetAlias;
            this.supportedFiles = supportedFiles;
            this.openByClicking = openByClicking;

            accessKey = ExternalApplicationProvider.GetAccessKey(targetAlias);
        }

        private Keys accessKey;
        public override Keys GetAccessKey()
        {
            return accessKey;
        }

        public async override void Exec(ApplicationProviderUser user, params string[] path)
        {
            if (path == null || path.Length == 0) return;
            //MessageBox.Show($"move");

            var archive = GetArchivePath(path);
            if (archive == null) return;

            if (archive == "")
            {
                catalogForm.StopLookAheadProcess();
                var key = new object();
                catalogForm.AddToOpendByExternalApplicationDictionary(key, path);
                catalogForm.PrepareLookAheadProcess();
                await Task.Run(() =>
                {
                    try
                    {
                        MoveItems(path, targetPath, catalogForm);
                    }
                    catch (System.Runtime.InteropServices.COMException) { } // fo がエラーを表示するのでこちらでは不要
                catch (Exception e)
                    {
                        MessageBox.Show(catalogForm, e.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });
                catalogForm.RemoveFromOpendByExternalApplicationDictionary(key);
            }
            else
            {
                // アーカイブの Extract は元のファイルのロックを心配する必要はない
                await Task.Run(() =>
                {
                    try
                    {
                        ExtractItems(archive, path, targetPath, catalogForm);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(catalogForm, e.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });

            }
        }

        protected override bool IsDefaultFor_Protected(string path, bool isDir)
        {
            return TechnicallySupports(path, isDir) && base.IsDefaultFor_Protected(path, isDir);
        }

        private static readonly Regex SelectorParser2 = new Regex(@"\{(.*?)\|(.*?)\}", RegexOptions.Compiled);

        public override string GetApplicationName()
        {
            var replace = catalogForm.InArchive ? "$2" : "$1";
            if (targetAlias == null)
            {
                return ExternalApplicationProvider.GetName(targetPath, SelectorParser2.Replace(Message.MoveExtractSelectedItemsTo1, replace, int.MaxValue), null);
            }
            else
            {
                return ExternalApplicationProvider.GetName(targetPath, "", SelectorParser2.Replace(targetAlias, replace, int.MaxValue));
            }
        }

        protected override string GetMenuText(string[] path)
        {
            return GetApplicationName();
            /*
            if (path == null || path.Length == 0) return ExternalApplicationProvider.GetName(targetPath, Message.MoveTo1, targetAlias);
            if (path.Length == 1) return ExternalApplicationProvider.GetName(targetPath, Message.MoveThisTo1, targetAlias);
            return ExternalApplicationProvider.GetName(targetPath, Message.MoveTheseTo1, targetAlias);
            */
        }

        /// <summary>
        /// path が存在しなければそのまま返す。存在すれば suffix を付けて返す。それも存在すれば更に数字を付加する。
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetNewPath(string path, string suffix, bool isDir = false, IEnumerable<string> prohibited = null)
        {
            if (File.Exists(path) || Directory.Exists(path) || prohibited != null && prohibited.Contains(path))
            {
                var ext = isDir ? "" : Path.GetExtension(path);
                var basePath = path.Substring(0, path.Length - ext.Length);
                path = basePath + suffix + ext;
                var digit = 2;
                while (File.Exists(path) || Directory.Exists(path) || prohibited != null && prohibited.Contains(path))
                {
                    path = basePath + suffix + " (" + digit++ + ")" + ext;
                }
            }
            return path;
        }

        private static void MoveItems(IEnumerable<string> src, string dstLocation, IWin32Window owner = null)
        {
            CopyOrMoveItems(from s in src select Tuple.Create(s, Path.Combine(dstLocation, Path.GetFileName(s))), true, owner);
        }

        private static void CopyOrMoveItems(IEnumerable<string> src, string dstLocation, bool move, IWin32Window owner = null)
        {
            CopyOrMoveItems(from s in src select Tuple.Create(s, Path.Combine(dstLocation, Path.GetFileName(s))), move, owner);
        }

        private static void CopyOrMoveItems(IEnumerable<Tuple<string, string>> srcAndDst, bool move, IWin32Window owner = null)
        {
            try
            {
                using (var fo = new MsdnMag.FileOperation(callbackSink: null, owner: owner))
                {
                    var moveOrCopy = move ? (Action<string, string, string>)fo.MoveItem : fo.CopyItem;
                    var movedFiles = move ? null : new List<string>();
                    foreach (var tuple in srcAndDst)
                    {
                        var src = tuple.Item1;
                        var dst = tuple.Item2;
                        // このコマンドをコピーに使うことは想定されない
                        /*
                        if (!move && src == dst)
                        {
                            dst = GetNewPath(dst, " - " + Message.Copy, Directory.Exists(src), movedFiles);
                            movedFiles.Add(dst);
                        }
                        */
                        moveOrCopy(src, Path.GetDirectoryName(dst), Path.GetFileName(dst));
                    }
                    fo.PerformOperations();
                }
            }
            catch (System.Runtime.InteropServices.COMException) { } // fo がエラーを表示するのでこちらでは不要
        }
        
        private static string GetArchivePath(IEnumerable<string> src)
        {
            if (src == null || !src.Any()) return null;
            var first = true;
            var altPos = 0;
            foreach (var s in src)
            {
                var altPos0 = s.IndexOf(Path.AltDirectorySeparatorChar);
                if (first)
                {
                    altPos = altPos0;
                    first = false;
                }
                else if (altPos0 != altPos)
                {
                    return null;
                }
            }
            if (altPos < 0) return "";
            first = true;
            string archive = null;
            foreach (var s in src)
            {
                var archive0 = s.Substring(0, altPos);
                if (first)
                {
                    archive = archive0;
                    first = false;
                }
                else if (archive0 != archive)
                {
                    return null;
                }
            }
            return archive;
        }

        private static void ExtractItems(string archive, IReadOnlyList<string> src, string dstLocation, CatalogForm catalogForm)
        {
            var keyStart = archive.Length + 1;
            using (var loader = new PackedImageLoader(archive, catalogForm.packedImageLoaderLeftHierarchies, catalogForm.packedImageLoaderSearchMode))
            {
                var entries = loader.GetPackedImageEntries(neededFileInfos: PackedImageLoaderFileInfo.All, needToExecOnTask: true);
                var srcEntries = new PackedImageLoader.PackedImageEntry[src.Count];
                var index = 0;
                // 例外は早めに発生させておく
                foreach (var s in src)
                {
                    srcEntries[index++] = entries.First(e => e.Path == s.Substring(keyStart));
                }
                string digits = null;
                foreach (var e in srcEntries)
                {
                    var name = Path.GetFileName(e.Path);
                    var ms = loader.GetStream(e, out var extensionWithPeriodForPDF); // ms を途中で置き換えるので using は避ける
                    try
                    {
                        if (e.Size < 0)
                        {
                            if (digits == null)
                            {
                                digits = "0";
                                var count = entries.Count;
                                while (count >= 10)
                                {
                                    digits += "0";
                                    count /= 10;
                                }
                            }
                            string extension;
                            //ms = CatalogForm.GetImageType(CatalogForm.ConvertToMemoryStream(ms), out extension);
                            extension = extensionWithPeriodForPDF;
                            name = e.Path;
                            name = digits.Substring(name.Length) + name + extension;
                        }

                        var dst = GetNewPath(Path.Combine(dstLocation, name), " - " + Message.Copy);

                        using (var fs = new FileStream(dst, FileMode.CreateNew)) ms.CopyTo(fs);
                        
                        if (e.LastWriteTime != DateTime.MinValue)
                        {
                            File.SetLastWriteTime(dst, e.LastWriteTime);
                        }
                    }
                    finally
                    {
                        ms.Dispose();
                    }
                    
                }
            }
        }

        protected override bool Supports_Protected(string path, bool isDir)
        {
            return TechnicallySupports(path, isDir) && base.Supports_Protected(path, isDir);
        }

        private bool TechnicallySupports(string path, bool isDir)
        {
            if (path == null) return false;

            if (catalogForm.InDirectoryVirtualDirectoryOrSmartDirectory)
            {
                if (path.Contains(Path.AltDirectorySeparatorChar)) return false;

                // 移動できないケースでは false を返す
                // ただし、移動元や移動先が存在しないケースは実際に移動するときのエラーメッセージに任せる

                var srcInLower = path.ToLower();
                var dstInLower = targetPathInLowerWithSeparator + Path.GetFileName(srcInLower);

                var parent = Path.GetDirectoryName(path).ToLower();
                if (parent.Length == 0 || parent.Last() != Path.DirectorySeparatorChar) parent += Path.DirectorySeparatorChar;
                return !dstInLower.StartsWith(srcInLower);
            }
            else if (catalogForm.InArchive)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
    
    public class ExplorerProvider : ApplicationProvider
    {
        public static readonly Bitmap Icon;

        static ExplorerProvider()
        {
            try
            {
                //Icon = FileTypeManager.GetSmallIconBitmap(Environment.GetFolderPath(Environment.SpecialFolder.Windows), useFileAttrinutes: false);
                //Icon = FileTypeManager.GetSmallIconBitmap( "explorer.exe", useFileAttrinutes: false);
                Icon = FileTypeManager.GetSmallIconBitmap(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"), useFileAttrinutes: false);
            }
            catch { }
        }
        protected override Image GetMenuIcon(string[] path, out bool iconIsStatic)
        {
            iconIsStatic = true;
            return Icon;
        }

        private string alias;
        public ExplorerProvider(string alias, Regex supportedFiles, Regex openByClicking)
        {
            this.supportedFiles = supportedFiles;
            this.openByClicking = openByClicking;
            escapeAmpersand = false;
            this.alias = alias;
            accessKey = ExternalApplicationProvider.GetAccessKey(alias);
        }

        private Keys accessKey;
        public override Keys GetAccessKey()
        {
            return accessKey;
        }

        public override void Exec(ApplicationProviderUser user, params string[] path)
        {
            if (path == null || path.Length != 1) return;
            Process.Start("explorer", "/select,\"" + string.Join("\" \"", from p in path select GetEntityPath(p)) + "\"");
        }

        public override string GetApplicationName()
        {
            //return Message.Explorer;
            return alias ?? Message.OpenInExplorer;
        }

        protected override string GetMenuText(string[] path)
        {
            return alias ?? Message.OpenInExplorer;
        }
    }

    public enum MultiFileMode { Single, Individual, Together }


    public class ApplicationInfo : IEquatable<ApplicationInfo>
    {
        public const string BuiltInReader = "<Built-in viewer>";
        public const string AssociatedApplication = "<Associated application>";
        public const string MoveLocation = "<Move location>";
        public const string OpenInFileInClipboard = "<Open in item in clipboard>";
        //private const string Explorer = "<Explorer>"; // 互換性保持のため
        public const string Explorer = "<Explorer>";

        public string Path;
        public string Alias;
        public string CommandLineParameter;
        public string ShowInContextMenu;
        public string OpenByClicking;
        public bool MultiItems;

        public ApplicationInfo() { }

        public ApplicationInfo(string path, string alias, string cmd, string click, string contextMenu, bool multiItems)
        {
            Path = path;
            Alias = alias;
            CommandLineParameter = NullToEmpty(cmd);
            ShowInContextMenu = NullToEmpty(contextMenu);
            OpenByClicking = NullToEmpty(click);
            MultiItems = multiItems;
        }

        private static string NullToEmpty(string str)
        {
            if (str == null) return "";
            return str;
        }

        public static bool operator ==(ApplicationInfo a, ApplicationInfo b)
        {
            var aIsNull = a as object == null;
            return aIsNull && b as object == null || !aIsNull && a.Equals(b);
        }

        public static bool operator !=(ApplicationInfo a, ApplicationInfo b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            var b = obj as ApplicationInfo;
            //return b != null && Path == b.Path && CommandLineParameter == b.CommandLineParameter && ShowInContextMenu == b.ShowInContextMenu && OpenByClicking == b.OpenByClicking;
            return b != null && b.Equals(this);
        }

        public override int GetHashCode()
        {
            return GetStringHashCode(Path) ^ GetStringHashCode(CommandLineParameter) ^ GetStringHashCode(ShowInContextMenu) ^ GetStringHashCode(OpenByClicking) ^ MultiItems.GetHashCode();
        }

        private static int GetStringHashCode(string str)
        {
            return str != null ? str.GetHashCode() : 0;
        }

        private static readonly Regex ExtensionArray = new Regex(@"^[; ]*(?:(?:\.?[^\\\/:*?""<>|. ]+|\\|\*) *;[; ]*)*(?:\.?[^\\\/:*?""<>|. ]+|\\|\*)[; ]*$", RegexOptions.Compiled);
        private static readonly Regex NeverMatchRegex = new Regex(@"$^", RegexOptions.Compiled);
        private static Regex ParseItemGroup(string pattern, bool testMode) => ParseItemGroup(pattern, testMode, out var dummy);
        private static Regex ParseItemGroup(string pattern, bool testMode, out bool isRegex) // testMode は現状不使用だが拡張性のために残しておく
        {
            if (string.IsNullOrEmpty(pattern))
            {
                isRegex = false;
                return NeverMatchRegex;
            }
            try
            {
                if(pattern == @"*;\")
                {
                    isRegex = false;
                    return null;
                }
                else if (SearchManager.IsRegex.IsMatch(pattern))
                {
                    isRegex = true;
                    //return SearchManager.GetRegex(pattern, compile: !testMode);
                    return SearchManager.GetRegex(pattern);
                }
                else if (ExtensionArray.IsMatch(pattern))
                {
                    var es = pattern.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    var rp = @"(?:";

                    var len = es.Length;
                    for (var i = 0; i < len; i++)
                    {
                        var x = es[i].Trim();
                        if (x == @"\")
                        {
                            rp += @"\\";
                        }
                        else if (x == "*")
                        {
                            rp += @"[^\\]$";
                        }
                        else
                        {
                            if (x[0] == '.') x = x.Substring(1);
                            rp += @"\." + Regex.Escape(x) + "$";
                        }
                        if (i < len - 1) rp += "|";
                    }
                    rp += ")$";
                    isRegex = false;
                    return new Regex(rp, RegexOptions.IgnoreCase); // | (testMode ? 0 : RegexOptions.Compiled));
                }
            }
            catch { }
            isRegex = false;
            return null;
        }

        public static bool TryParseItemGroup(string pattern, out Regex result)
        {
            if(pattern == null)
            {
                result = null;
                return false;
            }
            else if(pattern == @"*;\")
            {
                result = null;
                return true;
            }
            else
            {
                result = ParseItemGroup(pattern, testMode: false);
                return result != null;
            }
        }
        
        public static bool Parsable(string pattern)
        {
            return pattern == @"*;\" || ParseItemGroup(pattern, testMode: true) != null;
        }

        public ApplicationProvider GetApplicationProvider(CatalogForm catalogForm)
        {
            Regex contextMenu, openByClicking;
            bool ocRegex;
            /*
            string name;
            try
            {
                name = System.IO.Path.GetFileNameWithoutExtension(Path);
            }
            catch
            {
                name = Path;
            }
            */
            contextMenu = ParseItemGroup(ShowInContextMenu, testMode: false, isRegex: out var cmRegex);
            if (ShowInContextMenu == OpenByClicking)
            {
                openByClicking = contextMenu;
                ocRegex = cmRegex;
            }
            else
            {
                openByClicking = ParseItemGroup(OpenByClicking, testMode: false, isRegex: out ocRegex);
            }
            
            if (Path == BuiltInReader)
            {
                return new BuiltInViewerProvider(catalogForm, Alias, contextMenu, openByClicking);
            }
            if (Path == AssociatedApplication)
            {
                return new AssociationProvider(Alias, contextMenu, openByClicking, catalogForm);
            }
            if (Path == MoveLocation)
            {
                return new MoveLocationProvider(catalogForm, Alias, contextMenu, openByClicking);
            }
            if (Path == OpenInFileInClipboard)
            {
                return new OpenInFileInClipboardProvider(catalogForm, MultiItems, contextMenu, openByClicking);
            }
            if (Path == Explorer)
            {
                return new ExplorerProvider(Alias, contextMenu, openByClicking);
                //return new ExternalApplicationProvider(catalogForm, SettingForm.ExplorerPath, null, "/select,", false, contextMenu, cmRegex, openByClicking, ocRegex);
            }
            if (Path != null && Path.Length > 0 && Path.Last() == System.IO.Path.DirectorySeparatorChar)
            {
                return new MoveItemsProvider(catalogForm, Path, Alias, contextMenu, openByClicking);
            }

            return new ExternalApplicationProvider(catalogForm, Path, Alias, CommandLineParameter, MultiItems, contextMenu, cmRegex, openByClicking, ocRegex);
        }

        public bool Equals(ApplicationInfo b)
        {
            return b != null && Path == b.Path && CommandLineParameter == b.CommandLineParameter && ShowInContextMenu == b.ShowInContextMenu && OpenByClicking == b.OpenByClicking && MultiItems == b.MultiItems;
        }
    }
}
