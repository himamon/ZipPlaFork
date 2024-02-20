using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ZipPla
{
    static class Message
    {
        public static readonly CultureInfo DefaultLanguage = new CultureInfo("en-US"); // 外部ファイルから言語ファイルを読み込むように仕様変更した後も、この言語だけは外部ファイルに依存せず使えるように
        private static CultureInfo _CurrentLanguage;
        public static readonly string LanguageFileFolder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "language"); 

        static Message()
        {
#if RUNTIME
           // Program.RunTimeMeasure?.Block("GetLanguageFile");
#endif
            // 内蔵言語データが存在しなければ出力
            string path;
            path = System.IO.Path.Combine(LanguageFileFolder, "!FileNameList.txt");
            if (!System.IO.File.Exists(path)) MakeNewTextFile(path, GetFileNameList());

#if RUNTIME
           // Program.RunTimeMeasure?.Block("SetDefaultLanguages");
#endif

            path = GetLanguageFilePath(new CultureInfo("ja-JP"));
#if AUTOBUILD
            if(!System.IO.File.Exists(path))
#endif
            {
                SetJapaneseMessages();
                //CurrentLanguage = new CultureInfo("ja-JP");
                //if (!System.IO.File.Exists(path)) MakeNewTextFile(path, GetCurrentMessages());
                //MakeNewTextFile(path, GetCurrentMessages(path));
                MakeNewTextFile(path, GetCurrentMessages());
            }

            path = GetLanguageFilePath(DefaultLanguage);
#if AUTOBUILD
            if (!System.IO.File.Exists(path))
#endif
            {
                SetEnglishMessages();
                //CurrentLanguage = DefaultLanguage;
                //if (!System.IO.File.Exists(path)) MakeNewTextFile(path, GetCurrentMessages());
                //MakeNewTextFile(path, GetCurrentMessages(path));
                MakeNewTextFile(path, GetCurrentMessages());
            }

#if RUNTIME
            //Program.RunTimeMeasure?.Block("LoadLanguage");
#endif

            try // 言語ファイルなどを実装すればエラーは付きものになる
            {
                var language = (new GeneralConfig()).Language;//INIManager.LoadIniStringWithError("Settings", "Language");
                if(!string.IsNullOrEmpty(language))
                {
                    try
                    {
                        CurrentLanguage = CultureInfoParse(language);
                        return;
                    }
                    catch { }
                }
                var availableLanguages = AvailableLanguages;
                var systemLanguage = SystemLanguage;
                if (availableLanguages.Contains(systemLanguage))
                {
                    CurrentLanguage = systemLanguage;
                }
                else
                {
                    CurrentLanguage = DefaultLanguage;
                }
            }
            catch (Exception error)
            {
                Program.AlertError(error);
            }
        }

        public static string UpdateCurrentLanguage(bool leftUnusedSentences)
        {
            var path = GetLanguageFilePath(CurrentLanguage);
            MakeNewTextFile(path, GetCurrentMessages(leftUnusedSentences ? path : null));
            return path;
        }

        public static CultureInfo SystemLanguage
        {
            get
            {
                return CultureInfo.CurrentUICulture;
                /*
                var currentUICulture = CultureInfo.CurrentUICulture;
                if (currentUICulture.IsNeutralCulture)
                {
                    return currentUICulture;
                }
                else
                {
                    return currentUICulture.Parent;
                }
                */
            }
        }

        public static CultureInfo[] AvailableLanguages
        {
            get
            {
                HashSet<CultureInfo> result;
                result = new HashSet<CultureInfo>();
                foreach (var file in System.IO.Directory.GetFiles(LanguageFileFolder, "*.lng"))
                {
                    try
                    {
                        var culture = new CultureInfo(System.IO.Path.GetFileNameWithoutExtension(file));
                        if (!culture.IsNeutralCulture)
                        {
                            result.Add(culture);
                        }
                    }
                    catch{ }

                }
                if(result.Count == 0)
                {
                    result = new HashSet<CultureInfo> { new CultureInfo("en-US"), new CultureInfo("ja-JP") };
                }
                if (!result.Contains(DefaultLanguage))
                {
                    result.Add(DefaultLanguage);
                }
                return (from info in result orderby info.ToString() select info).ToArray();
            }
        }

        public static CultureInfo CurrentLanguage
        {
            get
            {
                return _CurrentLanguage;
            }
            set
            {
                try
                {
                    SetCurrentLanguage(value);
                }
                catch (Exception error)
                {
                    Program.AlertError(error);
                }
            }
        }

        public static CultureInfo CultureInfoParse(string language)
        {
            var availableLanguages = AvailableLanguages;
            foreach (var cultureInfo in availableLanguages)
            {
                if(cultureInfo.ToString() == language)
                {
                    return cultureInfo;
                }
            }
            throw new Exception($"Language data of \"{language}\" is not found.");
        }

        private static void SetCurrentLanguage(CultureInfo language)
        {
            try
            {
                // あらかじめ組み込みの言語で埋めておく
                switch (language.ToString())
                {
                    case "ja-JP":
                        SetJapaneseMessages(); // 日本語もそのようにしておく
                        break;
                    default:
                        SetEnglishMessages(); // 英語はいかなる場合にも正常に表示できるようにしなければならない
                        break;
                }
                SetMessages(language);
            }
            catch
            {
                switch (language.ToString())
                {
                    case "en-US":
                        SetEnglishMessages(); // 英語はいかなる場合にも正常に表示できるようにしなければならない
                        break;
                    case "ja-JP":
                        SetJapaneseMessages(); // 日本語もそのようにしておく
                        break;
                    default:
                        throw;
                }
            }
            
            _CurrentLanguage = language;
        }

        private static string GetLanguageFilePath(CultureInfo language)
        {
            return System.IO.Path.Combine(LanguageFileFolder, language + ".lng");
        }

        private static Dictionary<string, System.Reflection.PropertyInfo> SetMessages_MessagePropertyInfos = GetMessagePropertyInfos();
        private static void SetMessages(CultureInfo language)
        {
            var path = GetLanguageFilePath(language);

            using (var r = new System.IO.StreamReader(path))
            {
                string line;

                while ((line = r.ReadLine()) != null)
                {
                    var equal = line.IndexOf("=");
                    if (equal <= 0 || line.Length - 1 <= equal) continue;
                    var name = line.Substring(0, equal);
                    var value = line.Substring(equal + 1);
                    
                    if (SetMessages_MessagePropertyInfos.TryGetValue(name, out var pi))
                    {
                        pi.SetValue(null, value);
                    }
                }
            }
        }

        private static Dictionary<string, System.Reflection.PropertyInfo> GetMessagePropertyInfos()
        {
            var thisType = typeof(Message);
            var properties = thisType.GetProperties(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            var result = new Dictionary<string, System.Reflection.PropertyInfo>();
            var stringType = typeof(string);
            foreach (var propertie in properties)
            {
                if (propertie.PropertyType == stringType)
                {
                    result[propertie.Name] = propertie;
                }
            }
            return result;
        }

        /*
        private static void SetMessagesOld(CultureInfo language)
        {
            var path = GetLanguageFilePath(language);

            // ファイル → FileStream → MemoryStream → Reader
            using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            using (var mr = new System.IO.MemoryStream((int)fs.Length))
            {
                fs.CopyTo(mr);
                mr.Seek(0, System.IO.SeekOrigin.Begin);

                using (var r = new System.IO.StreamReader(mr))

                // ファイル → FileStream → Reader
                //using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                //using (var r = new System.IO.StreamReader(fs))

                // ファイル → Reader
                //using (var r = new System.IO.StreamReader(path))
                {
                    string line;
                    Type thisType = typeof(Message);
                    while ((line = r.ReadLine()) != null)
                    {
                        var equal = line.IndexOf("=");
                        if (equal <= 0 || line.Length - 1 <= equal) continue;
                        var name = line.Substring(0, equal);
                        var value = line.Substring(equal + 1);

                        var pi = thisType.GetProperty(name);
                        if (pi != null)
                        {
                            pi.SetValue(null, value);
                        }
                    }
                }
            }
        }
        */

        private static bool MakeNewTextFile(string filepath, string text, bool append = false)
        {
            try
            {
                var directories = new Stack<string>();
                var directory = System.IO.Path.GetDirectoryName(filepath);
                while (true)
                {
                    if (string.IsNullOrEmpty(directory))
                    {
                        return false;
                    }
                    else if (System.IO.Directory.Exists(directory))
                    {
                        break;
                    }
                    directories.Push(directory);
                    directory = System.IO.Path.GetDirectoryName(directory);
                }
                while (directories.Count > 0)
                {
                    System.IO.Directory.CreateDirectory(directories.Pop());
                }
                using (var w = new System.IO.StreamWriter(filepath, append))
                {
                    w.Write(text);
                }
                return true;
            }
            catch { }
            return false;
        }

        public static string GetCurrentMessages(string updateBasePath = null)
        {
            var thisType = typeof(Message);
            var AllStaticSentencesInfo = thisType.GetProperties(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            System.Text.StringBuilder sb;

            try
            {
                if (updateBasePath != null && System.IO.File.Exists(updateBasePath))
                {
                    var existingLines = new List<string>();
                    using (var reader = new System.IO.StreamReader(updateBasePath))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            existingLines.Add(line);
                        }
                    }

                    var existingLinesCount = existingLines.Count;
                    var existingNames = new string[existingLinesCount];
                    for (var i = 0; i < existingLinesCount; i++)
                    {
                        var line = existingLines[i];
                        var equalsPos = line.IndexOf('=');
                        if (equalsPos >= 0)
                        {
                            existingNames[i] = line.Substring(0, equalsPos);
                        }
                    }

                    var newLines = new List<string>();
                    var insertPosition = 0;
                    var insertPositions = new List<int>();
                    foreach (var info in AllStaticSentencesInfo)
                    {
                        if (info.PropertyType != typeof(string)) continue;
                        var name = info.Name;
                        var existingIndex = Array.IndexOf(existingNames, name);
                        if (existingIndex >= 0)
                        {
                            if (existingIndex >= insertPosition)
                            {
                                insertPosition = existingIndex + 1;
                            }
                        }
                        else
                        {
                            newLines.Add($"{name}={info.GetValue(null)}");
                            insertPositions.Add(insertPosition);
                        }
                    }
                    var newLinesCount = newLines.Count;

                    sb = new System.Text.StringBuilder();
                    var newLineIndex = 0;
                    for (var i = 0; i < existingLinesCount; i++)
                    {
                        while (newLineIndex < newLinesCount)
                        {
                            insertPosition = insertPositions[newLineIndex];
                            if (insertPosition == i)
                            {
                                sb.AppendLine(newLines[newLineIndex++]);
                            }
                            else
                            {
                                break;
                            }
                        }
                        sb.AppendLine(existingLines[i]);
                    }
                    while (newLineIndex < newLinesCount)
                    {
                        sb.AppendLine(newLines[newLineIndex++]);
                    }
                    return sb.ToString();
                }
            }
            catch { }

            sb = new System.Text.StringBuilder();
            foreach (var member in AllStaticSentencesInfo)
            {
                if (member.PropertyType == typeof(string))
                {
                    sb.AppendLine($"{member.Name}={member.GetValue(null) as string}");
                }
            }
            return sb.ToString();
        }

        private static string GetFileNameList()
        {
            var result = "FileName\tEnglishName\tNativeName\r\n\r\n";
            foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                if (!culture.IsNeutralCulture)
                {
                    var cultureName = culture.Name;
                    if (!string.IsNullOrEmpty(cultureName))
                    {
                        result += $"{cultureName}.lng\t{culture.EnglishName}\t{culture.NativeName}\r\n";
                    }
                }
            }
            return result;
        }

        private static void SetJapaneseMessages()
        {
            _Start = "スタート(&S)";
            OpenHistory = "履歴を開く";
            OpenFolder = "フォルダーを開く";
            //OpenVirtualFolder = "仮想フォルダーを開く";
            OpenFile = "ファイルを開く";
            AllSupportedFiles = "全ての対応ファイル";
            VideoFilesRequireFfmpeg = "動画ファイル（ffmpeg.exe が必要）";
            NewVirtualFolder = "新しい仮想フォルダー";
            NewSmartFolder = "新しいスマートフォルダー";
            OpenBuiltInViewer = "内蔵ビューアーを開く";
            //ReloadAllItems = "全ての項目を再読み込み";
            Reload = "再読み込み";
            NewFolder = "新しいフォルダー";
            EditAddressBar = "アドレスバーを編集";
            EditFilter = "フィルターを編集";
            Exit = "終了";
            _View = "表示(&V)";
            _Behavior = "動作(&B)";
            Bookmark = "ブックマーク";
            FolderTree = "フォルダーツリー";
            Thumbnails = "サムネイル";
            Details = "詳細";
            Layout = "レイアウト";
            VerticalLayout = "サムネイル／詳細";
            AlternativeVerticalLayout = "詳細／サムネイル";
            HorizontalLayout = "サムネイル｜詳細";
            AlternativeHorizontalLayout = "詳細｜サムネイル";
            BuiltInViewerSettings = "内蔵ビューアーの設定";
            BlackBackground = "黒背景";
            GrayBackground = "灰色背景";
            WhiteBackground = "白背景";
            SelectBackgroundColor = "背景色を選択";
            ForceFirstPageToBeSingle = "最初のページを常に単ページで表示";
            OpenInPreviousFullscreenMode = "前回のフルスクリーンモードで開く";
            OpenInWindow = "ウィンドウで開く";
            OpenInFullscreen = "フルスクリーンで開く";
            OpenInPreviousRotationToneCurveSetting = "前回の回転／トーンカーブの設定で開く";
            SortFilesWithSameOrderAsThumbnailWindow = "サムネイルウィンドウのソートを使用";
            AllowReadAheadProcess = "先読み用のプロセスを使用";
            CreateShortcutFromCurrentSettings = "現在の設定でショートカットを作成";
            MaxCountCaptchaInAVideo = "動画内の最大キャプチャ数";
            FitOnScreen = "ウィンドウに合わせる";
            EverySecond = "1 秒間隔";
            //ThumbnailList = "サムネイルリスト";
            //FileList = "ファイルリスト";
            //PageInFileList = "ページ（ファイルリスト内）";
            //RatingInFileList = "レート（ファイルリスト内）";
            //DateAccessedInFileList = "アクセス日時（ファイルリスト内）";
            //DateModifiedInFileList = "更新日時（ファイルリスト内）";
            ArchiveFilesPdfFiles = "圧縮ファイル／ PDF ファイル";
            ImageFiles = "画像ファイル";
            VideoFiles = "動画ファイル";
            OtherFiles = "その他のファイル";
            //AtLeastOneKindOfItemsMustBeSelected = "全ての種類の項目を非表示にすることはできません。";
            Folders = "フォルダー";
            //IgnoreFolders = "フォルダーを非表示";
            SearchSubfoldersAsWell = "サブフォルダー内も表示";
            _Thumbnail = "サムネイル(&T)";
            //ReloadAllThumbnails = "全てのサムネイルを再読み込み";
            Tiny = "極小";
            Small = "小";
            Normal = "標準";
            Large = "大";
            Huge = "極大";
            AllRatios = "全ての比";
            //ShowFilename = "ファイル名を表示";
            ShowName = "名前を表示";
            Letterbox = "レターボックス（全て表示）";
            PanAndScan = "パンスキャン（中央部分）";
            ShowIcon = "アイコンを表示";
            ShowRating = "レートを表示";
            ShowTags = "タグを表示";
            NumberOfLinesOfName = "名前の行数";
            //FromTheLongestName = "最長の名前基準";
            Full = "全て";
            //FromThumbnailWidthLess = "サムネイル幅基準（少なめ）";
            //FromThumbnailWidthGreater = "サムネイル幅基準（多め）";
            AutomaticLess = "自動（少なめ）";
            AutomaticGreater = "自動（多め）";
            MinimumFrameThickness = "最小フレーム幅";
            //ZeroThickness = "なし";
            VeryThin = "極細";
            Thin = "細い";
            NormalThickness = "標準";
            Thick = "太い";
            VeryThick = "極太";
            Aligning = "並べ方";
            TotallyUniformed = "均一";
            HorizontallyUniformed = "両端揃え";
            AlignedLeft = "左揃え";
            AlignedOnCenter = "中央揃え";
            MouseWheelScrollAmount = "マウスホイールのスクロール量";
            OneLine = "1 行";
            _1Lines = "$1 行";
            ThumbnailCache = "サムネイルキャッシュ";
            ThumbnailCacheFirstDescription =
                @"サムネイルキャッシュを使用すると二度目以降のサムネイル表示が高速になります。";
            ThumbnailCacheFirstDescriptionForFirstLaunch =
                @"サムネイルキャッシュを使用すると二度目以降のサムネイル表示が高速になります。\n" +
                @"この設定は「サムネイル(T) > サムネイルキャッシュ」から変更できます。"; // サムネイルキャッシュのあとの...はフォントの関係で」と重なって j に見えるので書かない
            StoreCachesInAlternateDataStreamRecommended = "キャッシュを代替データストリームに保存（推奨）";
            ThumbnailCacheAdsDescription =
                @"＊元のファイルは変更されません。\n" +
                @"＊サムネイルキャッシュは元のファイルの名前変更／移動／削除に追従します。\n" +
                @"　元のファイルの名前変更等を ZipPla で行う必要はありません。\n" +
                @"＊これが選択されていない場合、既存のキャッシュは自動的に削除されます。";
            StoreCachesInSpecifiedFolder = "キャッシュを指定されたフォルダーに保存";
            ThumbnailCacheSpecifiedFolderDescription =
                @"＊代替データストリームと同様に元のファイルは変更されません。\n" +
                @"＊キャッシュを保存するフォルダーは自動的に作成されます。\n" +
                @"＊ZipPla でファイルの名前変更／移動／削除を行った場合に限り\n" +
                @"　キャッシュファイルもその操作に追従します。\n" +
                @"＊上記の場合を除き、作成されたキャッシュが自動的に削除されることはありません。\n" +
                @"＊フォルダーの指定には絶対パス及び元のファイルからの相対パスが使用できます。";
            LocationOfZipPlaExe = "ZipPla.exe の場所";
            LocationOfOriginalFile = "元のファイルの場所";
            Browse = "フォルダーの選択";
            NotUseThumbnailCache = "サムネイルキャッシュを使用しない";
            AdsNotSupportedWarningMessage =
                @"この場所のファイルシステムは代替データストリームをサポートしません。" +
                //@"この警告を消すにはサムネイルキャッシュの設定を代替データストリームを使用しないものに変更してください。"++ // 誤った日本語ではないが、同じ助詞が続くことでワードが警告を出す＋長くて読みにくい
                @"この警告を消すにはサムネイルキャッシュの設定を変更してください。" + // 代替データストリームを使うサムネイル設定が一つきりであればこちらで十分
                @"プロファイルカラーを利用すると、場所に応じて設定を自動的に切り替えることもできます。";
            /*CreateThumbnailCache = "サムネイルキャッシュを使用";
            AboutThumbnailCache = "サムネイルキャッシュについて";
            ThumbnailCacheDescription =
                @"サムネイルキャッシュを使用すると二度目以降のサムネイル表示が高速になります。\n" +
                @"\n" +
                @"サムネイルキャッシュは代替データストリームに保存されます。\n" +
                //@"\n" +
                @"＊元のファイルは変更されません。\n" +
               // @"\n" +
                @"＊サムネイルキャッシュは元のファイルの名前変更／移動／削除に追従します。\n" +
                @"　元のファイルの名前変更等を ZipPla で行う必要はありません。\n" +
                @"\n" +
                @"キャッシュを使用しない設定の場合、既存のキャッシュは自動的に削除されます。";*/
            SmartClip = "スマートクリップ（自動検出）";
            _Others = "その他(&O)";
            _ChangeCurrentProfileColor = "現在のプロファイルカラーを変更(&C)";
            Edit_Tags = "タグを編集(&T)";
            //_KeysMouseButtons = "キーボード／マウスボタン(&K)";
            _BasicOperationSettings = "基本操作設定(&B)";
            //_MouseGestures = "マウスジェスチャ(&M)";
            _MouseTouchGestures = "マウス／タッチジェスチャ(&M)";
            Common_Settings = "共通設定(&S)";

            ScreenEffectAtRightSide = "右の辺に画面効果";
            ScreenEffectAtLeftSide = "左の辺に画面効果";
            ScreenEffectAtNextPageSide = "次のページの辺に画面効果";
            ScreenEffectAtPreviousPageSide = "前のページの辺に画面効果";

            StartMenu = "「スタート」メニュー";
            MoveMenu = "「移動」メニュー";
            ViewMenu = "「表示」メニュー";
            SlideshowMenu = "「スライドショー」メニュー";
            VirtualFolderMenu = "「仮想フォルダー」メニュー";
            EditMenu = "「編集」メニュー";
            OthersMenu = "「その他」メニュー";

            NoOperation = "何もしない";

            //GoBackAltLeft = "戻る (Alt+←)";
            //GoForwardAltRight = "進む (Alt+→)";
            RightClickDownFlickToShowHistory = "右クリック／下フリックで履歴を表示";

            ClearMenuForAddressBarHistory = "パス履歴削除メニュー";
            DeleteCurrentPathFromHistory = "現在のパスを履歴から削除";
            ClearAllHistoryOfLocations = "パスの履歴を全削除";

            TypeInAsc = "種類（昇順）";
            TypeInDesc = "種類（降順）";
            NameInAsc = "名前（昇順）";
            NameInDesc = "名前（降順）";
            RatingInAsc = "レート（昇順）";
            RatingInDesc = "レート（降順）";
            CreatedInAsc = "作成日時（昇順）";
            CreatedInDesc = "作成日時（降順）";
            AccessedInAsc = "アクセス日時（昇順）";
            AccessedInDesc = "アクセス日時（降順）";
            ModifiedInAsc = "更新日時（昇順）";
            ModifiedInDesc = "更新日時（降順）";
            SizeInAsc = "サイズ（昇順）";
            SizeInDesc = "サイズ（降順）";
            Random = "ランダム";

            MiddleClickToClearPaste = "中クリックで消去／貼り付け";
            LeftFlickToClear = "左フリックで消去";
            RightFlickToPaste = "右フリックで貼り付け";
            MiddleClickLeftFlickToResetRating = "中クリック／左フリックでレート消去";

            EditMenuForFilter = "フィルター編集メニュー";
            DeleteCurrentFilterFromDropdown = "現在のフィルターをドロップダウンリストから削除";
            ClearAllDropdownItems = "ドロップダウンリストを全削除";
            EditDropdownOfFilters = "ドロップダウンリストを編集";

            _Add = "追加(&A)";
            //AddWithCurrentProfileColor = "現在のプロファイルカラーで追加";
            //AddWithNewProfileColor = "新しいプロファイルカラーで追加";
            BookmarkWithCurrentProfileColor = "現在のプロファイルカラーのブックマーク";
            AddWithCurrentProfileColor = "現在のプロファイルカラーで追加";
            BookmarkWithNewProfileColor = "新しいプロファイルカラーのブックマーク";
            AddWithNewProfileColor = "新しいプロファイルカラーで追加";
            OnlyCurrentProfileColor = "現在のプロファイルカラーのみ";
            OnlyNewProfileColor = "新しいプロファイルカラーのみ";
            Separator = "区切り";
            AddSeparator = "区切りを追加";

            SelectedColorIsUsedByFollowingBookmarks = "選択された色は以下のブックマークで既に使用されています。";
            //DoYouUseColorWhichIsCloseToOtherColors = "既に非常に近い色が使われています。このまま続けてもよろしいですか？";
            HowToAddUsedColorDescription =
                "使用されている色のブックマークを新たに追加するには、"+
                "その色のブックマークを開いてから目的の場所へ移動し、「追加(A) > 現在のプロファイルカラーで追加」機能を使用してください。";

            ChangeCurrentProfileColor = "現在のプロファイルカラーを変更";
            WillChangeCurrentProfileColor = "現在のプロファイルカラーを変更します。";
            ThisChangeAlsoAffectsFollowingBookmarks = "この変更は以下のブックマークにも影響します。";
            HowToCreateNewColorDescription = "現在のプロファイルカラーを変更するのではなく新しいプロファイルカラーを作成する場合には、" +
                "ブックマークにある「追加(A)」ボタンを右クリックして「新しいプロファイルカラーのブックマーク」または「新しいプロファイルカラーのみ」を選択してください。";

            Delete = "削除";
            DoYouDeleteFollowing1Items = "以下の $1 個の項目を削除しますか？";
            EditAlias = "別名を付ける";
            ProfileColor = "プロファイルカラー";
            ProfileColorOf1 = "$1 のプロファイルカラー";
            RealName = "元の名前に戻す";
            LoadOnlyProfileColor = "プロファイルカラーのみ読み込む";
            LoadProfileColor = "プロファイルカラーを読み込む";
            //SetToCurrentLocation = "現在の場所で上書き";
            //SetToCurrentLocationAndState = "現在の場所及び表示で上書き";

            FilterWithSelectedText = "選択テキストでフィルタリング";
            FilterWith1 = "\"$1\" でフィルタリング";
            CopySelectedText = "選択テキストをコピー";
            Copy1 = "\"$1\" をコピー";
            SearchSelectedTextOnGoogle = "選択テキストを Google で検索";
            Search1OnGoogle = "\"$1\" を Google で検索";
            NewCustomCommand = "新しいカスタムコマンド";
            OpenInBuiltInViewer = "内蔵ビューアーで開く";
            OpenIn1 = "$1 で開く";
            Open = "開く";
            //MoveFilesFolders = "ファイル／フォルダーの移動";
            //MoveSelectedItemsTo1 = "選択項目を $1 へ移動";
            MoveExtractSelectedItems = "選択項目の移動／抽出";
            MoveExtractSelectedItemsTo1 = "選択項目を $1 へ{移動|抽出}";
            AssociatedApplication = "関連付けプログラム";
            //MoveToThisFolder = "このフォルダーに移動";
            //MoveHere = "ここへ移動";
            OpenInCurrentWindow = "現在のウィンドウで開く";
            //MoveToLocationOfThis = "この項目のある場所へ移動";
            SubdivideAroundHere = "この前後を細分化して表示";
            //MoveLocation = "場所の移動";
            CurrentZipPla = "現在の ZipPla";
            ExecutableInClipboard = "クリップボードの実行ファイル";
            OpenInExplorer = "エクスプローラーで開く";
            CopyAddressAsText = "アドレスをテキストとしてコピー";
            ApplicationIsNotSpecified = "起動プログラムが設定されていません。";
            Rating = "レート";
            None = "なし";
            //MiddleClickOnStars = "レート領域で中クリック";
            Tag = "タグ";
            RightClickNotToClose = "右クリックで連続設定";
            RightFlickIsAlsoOk = "右フリックでも OK";
            SameAsAbove = "以下同様";
            UncheckAll = "全て解除";
            //AddTagsFromFileName = "ファイル名からタグを追加";
            AddTagsFromName = "名前からタグを追加";
            VirtualFolder = "仮想フォルダー";
            AddToNewVirtualFolder = "新規作成して追加";
            RemoveFromHistory = "履歴から除去";
            //AddToExistingVirtualFolder = "既存の仮想フォルダーに追加";
            AddTo1 = "\"$1\" に追加";
            NoVirtualFoldersInBookmark = "ブックマークに仮想フォルダーが登録されていません。"; // 英語は体言的なのでピリオドなしだが句点をつける
            //ReloadSelectedThumbnail = "このサムネイルを再読み込み";
            Cover = "表紙";
            SetCoverManually = "表紙の手動設定";
            SetCoverToThis = "これを表紙に設定";
            SetCoverOfParentFolderToThis = "これを親フォルダーの表紙に設定";
            AbortSettingCover = "表紙設定の中止";
            RightClick = "右クリック";
            ClearCoverSetting = "表紙設定の削除";
            PageSequence = "綴じ方";
            Default = "デフォルト";
            LeftToRight = "左綴じ";
            RightToLeft = "右綴じ";
            SinglePage = "単ページ";
            SinglePageWithoutScalingUp = "単ページ（拡大しない）";
            //ForcePageSpread = "強制見開き表示（左／右綴じ）";
            PrioritizePageSpread = "見開き表示優先（左／右綴じ）";
            AllowPageDivision = "分割表示を許可（左／右綴じ）";
            ClearPageSequenceSetting = "綴じ方設定の削除";
            Cut = "切り取り";
            Copy = "コピー";
            CopyCapturedImage = "キャプチャ画像をコピー";
            Paste = "貼り付け";
            Rename = "名前の変更";
            RemoveFromVirtualFolder = "仮想フォルダーから除去";
            //DeleteOnlyForVirtualFolder = "削除（仮想フォルダー内限定）";
            Properties = "プロパティ";

            TogglePageSequence = "綴じ方の切り替え";

            OrientSeekBarDirectionToPageSequence = "シークバーの向きを綴じ方に合わせる";

            ShowHintWhenChangingSettings = "設定変更時にヒントを表示";
            Hint = "ヒント";
            BuiltInViewerSavingSettingsBehaviorDescription =
                @"この設定は特定の方法で内蔵ビューアーが起動された場合等にリセットされます。\n" +
                @"設定を保持するには、サムネイルウィンドウの「動作(B) > 内蔵ビューアーの設定」または\n" +
                @"内蔵ビューアーの「スタート(S) > 現在の設定でショートカットを作成...」を利用してください。";
            ShowThisHintAgainNextTime = "次回もこのヒントを表示"; // わかりやすさのため「ヒントを表示」という言葉は省くべきではない。

            //_View = "表示(&V)";
            //_Image = "画像(&I)";
            _Move = "移動(&M)";
            S_lideshow = "スライドショー(&L)";
            Virtual_Folder = "仮想フォルダー(&F)";
            _Edit = "編集(&E)";

            OpenThumbnailWindow = "サムネイルウィンドウを開く";
            SelectCurrentImageInThumbnailWindow = "現在の画像をサムネイルウィンドウで選択";
            SwitchToThumbnailWindow = "サムネイルウィンドウに切り替える";
            CloneWindow = "ウィンドウを複製";
            Fullscreen = "フルスクリーン";
            Window = "ウィンドウ";
            ToggleFullscreenMode = "フルスクリーンモードの切り替え";

            //AlwaysAutomaticallyHideUI = "常に自動的に UI を隠す";
            AlwaysAutomaticallyHideUI = "ウィンドウモードでも UI を隠す";

            ToggleMagnifier = "拡大鏡の切り替え";
            ToggleMagnifierWithoutOperatingGuide = "拡大鏡の切り替え（操作ガイドなし）";
            ToggleMagnifierWithMinimalScreenEffects = "拡大鏡の切り替え（最小限の画面効果）";
            EnableMagnifier = "拡大鏡の開始";
            EnableMagnifierWithoutOperatingGuide = "拡大鏡の開始（操作ガイドなし）";
            DisableMagnifier = "拡大鏡の停止";
            DisableMagnifierWithoutOperatingGuide = "拡大鏡の停止（操作ガイドなし）";
            DisableMagnifierWithoutScreenEffects = "拡大鏡の停止（画面効果なし）";
            MagnifierOperatingGuide =
                @"移動：マウス移動, 矢印キー\n" +
                @"拡大：ホイール ↑, ↑+↓, Shift\n" +
                @"縮小：ホイール ↓, ←+→, Ctrl";

            //DefaultClickBehavior = "標準のクリック動作";
            NextPage = "次のページ";
            PreviousPage = "前のページ";
            WheelDown = "ホイール ↓";
            //CursorRight = "カーソル →";
            WheelUp = "ホイール ↑";
            //CursorLeft = "カーソル ←";
            //LeftClickOnRightHalf = "右半分をクリック";
            //LeftClickOnLeftHalf = "左半分をクリック";
            //LeftClickOnCenter = "中央をクリック";

            Specials = "特殊入力";

            LeftClickOnLeftSide = "左側をクリック";
            LeftClickOnRightSide = "右側をクリック";
            LeftClickOnTopSide = "上側をクリック";
            LeftClickOnBottomSide = "下側をクリック";
            LeftClickOnCenter = "中央をクリック";
            LeftClickOnTopLeft = "左上をクリック";
            LeftClickOnTopRight = "右上をクリック";
            LeftClickOnBottomLeft = "左下をクリック";
            LeftClickOnBottomRight = "右下をクリック";

            DoubleLeftClick = "ダブルクリック";

            NextPageAtLastPage = "終端で「次のページ」";
            PreviousPageAtFirstPage = "始端で「前のページ」";

            MoveForwardOnePage = "１ページ進む";
            MoveBackOnePage = "１ページ戻る";
            WheelDownOnBar = "シークバー上でホイール ↓";
            WheelUpOnBar = "シークバー上でホイール ↑";
            //CursorDown = "カーソル ↓";
            //CursorUp = "カーソル ↑";

            RightPage = "右のページ";
            LeftPage = "左のページ";
            MoveRightOnePage = "１ページ右へ";
            MoveLeftOnePage = "１ページ左へ";
            PositionRatio = "割合で移動";
            OpenNext = "次を開く";
            X2Button = "X2 クリック";
            OpenPrevious = "前を開く";
            X1Button = "X1 クリック";
            OpenRight = "右を開く";
            OpenLeft = "左を開く";

            NextPageOrAutoForwardScroll = "次のページまたはスクロール";
            PreviousPageOrAutoBackScroll = "前のページまたはスクロール";
            NextPageOrAutoForwardScrollWithoutOverwrap = "次のページまたはスクロール（オーバーラップなし）";
            PreviousPageOrAutoBackScrollWithoutOverwrap = "前のページまたはスクロール（オーバーラップなし）";
            AutoForwardScroll = "順方向スクロール";
            AutoBackScroll = "逆方向スクロール";
            AutoForwardScrollWithoutOverwrap = "順方向スクロール（オーバーラップなし）";
            AutoBackScrollWithoutOverwrap = "逆方向スクロール（オーバーラップなし）";

            RightPageOrAutoRightScroll = "右のページまたはスクロール";
            LeftPageOrAutoLeftScroll = "左のページまたはスクロール";
            RightPageOrAutoRightScrollWithoutOverwrap = "右のページまたはスクロール（オーバーラップなし）";
            LeftPageOrAutoLeftScrollWithoutOverwrap = "左のページまたはスクロール（オーバーラップなし）";
            AutoRightScroll = "右方向スクロール";
            AutoLeftScroll = "左方向スクロール";
            AutoRightScrollWithoutOverwrap = "右方向スクロール（オーバーラップなし）";
            AutoLeftScrollWithoutOverwrap = "左方向スクロール（オーバーラップなし）";
            
            //MiddleClickOnPosteriorHalfOfBar = "シークバー後半を中クリック";
            //MiddleClickOnAnteriorHalfOfBar = "シークバー前半を中クリック";

            BookFolderIsNotFound = "書庫／フォルダーが見つかりません。";

            Slideshow = "スライドショー";
            StartSlideshow = "スライドショーの開始";
            StopSlideshow = "スライドショーの停止";
            ToggleSlideshow = "スライドショーの切り替え";
            SpaceKey = "スペースキー";
            OneSecondIntervals = "1 秒間隔";
            _1SecondsIntervals = "$1 秒間隔";
            Repeat = "繰り返し";
            AllowToOpenNextItem = "終端で次を開く";

            RotateLeft = "左に回転";
            RotateRight = "右に回転";
            CancelRotationSetting = "回転の設定を解除";
            //Binarization = "二値化";
            //AutoContrastControl = "自動コントラスト調整";

            //RemoveMoire = "モアレ除去";
            ToneCurve = "トーンカーブ";
            ToneCurveSepiaTone = "トーンカーブ（セピア調）";
            ToneCurveUserDefined = "トーンカーブ（ユーザー定義）";

            ScalingAlgorithmNormal = "拡大縮小アルゴリズム（通常時）";
            ScalingAlgorithmMagnifier = "拡大縮小アルゴリズム（拡大鏡）";
            DefaultHighQuality = "標準（品質重視）";
            DefaultHighSpeed = "標準（速度重視）";
            NearestNeighbor = "最近傍法";
            AreaAverage = "面積平均法";
#if !AUTOBUILD
            //AntiMoire = "耐モアレ"; // 英語では AntiMoire だが意味が分かりにくいので翻訳しておく
#endif
            LinearizeColorSpace = "色空間を線形化";
            LinearizeColorSpaceDescription = "色空間の線形化は「標準（品質重視／速度重視）」では動作しません。\\n" +
                "また、「最近傍法」では結果に変化が表れません。";
            UseAreaAverageWhenUpsizing = "拡大時は面積平均法を使用";

            SaveCurrentPageSequence = "現在の綴じ方を保存";

            _1Items = "$1 個の項目";
            _1ItemsAreSelected = "$1 個の項目を選択";
            OnePage = "1 ページ";
            _1Pages = "$1 ページ";

            PropertiesOf1 = "$1 のプロパティ";
            Location = "場所";
            Icon = "アイコン";
            //FileName = "ファイル名";
            Name = "名前";
            FileType = "ファイルの種類";
            Length = "長さ";
            Size = "サイズ";
            DateCreated = "作成日時";
            DateAccessed = "アクセス日時";
            DateModified = "更新日時";
            Resolution = "解像度";
            AspectRatio = "比";
            Width = "幅";
            Height = "高さ";
            FPS = "FPS";
            NumberOfPages = "ページ数";
            ImageSize = "画像サイズ";
            VideoInfo = "動画情報";
            Position = "位置";
            Width1Height2BitsPerPixel3 = "$1（幅） x $2（高さ） x $3（色深度）";

            Page = "ページ";

            //File = "ファイル";
            //Folder = "フォルダー";
            Bytes = "バイト";

            EditTags = "タグを編集";
            TagName = "タグ名";
            NewTag1 = "新しいタグ$1";
            ProfileColorUnderEditing = "編集中のプロファイルカラー";
            Add = "追加";
            Color = "色";
            TagMustBeNoEmptyStringWhichDoesNotIncludeFollowingCharacters = "タグは以下の文字を含まない空でない文字列でなければなりません。";
            TagsCanNotShareSameName = "同じ名前のタグが既に存在しています。";

            FilteringString = "フィルター文字列";
            Alias = "別名";
            EditFilters = "フィルターを編集";
            InvariantDropdownList = "ドロップダウンリストを固定";
            NewFilteringString1 = "新しいフィルター文字列$1";
            Presets = "プリセット";
            FilterSampleDescription1 = "フォルダー";
            FilterSampleString1 = "\\";
            FilterSampleAlias1 = "$フォルダー";
            FilterSampleDescription2 = "\"教科書\" \"text book\" を含まない";
            FilterSampleString2 = "-教科書 -\"text book\"";
            FilterSampleAlias2 = "$教科書以外";
            FilterSampleDescription3 = "zip または rar ファイル";
            FilterSampleString3 = @"/\.(zip|rar)$/i";
            FilterSampleAlias3 = "$主要書庫";
            FilterSampleDescription4 = "レートが設定されていない";
            FilterSampleString4 = "-r>0";
            FilterSampleAlias4 = "$レートなし";
            FilterSampleDescription5 = "-x という文字列を含む";
            FilterSampleString5 = "\"-x\"";
            FilterSampleAlias5 = "";
            FilterSampleDescription6 = "ワイルドカード \"第?巻\" \"第??巻\" のいずれかを含む";
            FilterSampleString6 = "第?巻 +第??巻";
            FilterSampleAlias6= "";
            FilterSampleDescription7 = "ワイルドカード \"文庫* 第?*巻\" を含む";
            FilterSampleString7 = "\"文庫* 第?*巻\"";
            FilterSampleAlias7 = "";
            FilterSampleDescription8 = "正規表現 \"0\\d+\" にマッチしない";
            FilterSampleString8 = @"/^(?!.*(0\d+))/i";
            FilterSampleAlias8 = "";
            FilterSampleDescription9 = "";
            FilterSampleString9 = "";
            FilterSampleAlias9 = "";
            AliasMustBeNoEmptyStringWhichDoesNotIncludeSpaceAndFollowingCharacters = "別名は空白及び以下の文字を含まない空でない文字列でなければなりません。";
            DisplayNameCanNotShareSameName = "同じ表示名（別名またはそれが未入力ならフィルター文字列）が既に存在しています。";
            FilteringStringMustNotBeEmpty = "フィルター文字列は空にはできません。";

            //MouseGestures = "マウスジェスチャ";
            MouseTouchGestures = "マウス／タッチジェスチャ";
            Enabled = "有効";
            GestureToAddCommand = "ジェスチャでコマンド追加";
            Appearance = "外観";
            LineWidth = "幅";
            Gesture = "ジェスチャ";
            Command = "コマンド";
            InThumbnailWindowTouchGestureMustStartWithHorizontalDirection = "サムネイルウィンドウではタッチジェスチャは水平方向から始めてください。";

            //KeysMouseButtons = "キーボード／マウスボタン";
            BasicOperationSettings = "基本操作設定";
            Inputs = "入力";
            Abort = "中止";
            InputKeysMouseButtons = "キー／マウスボタンを入力...";
            UnassignedCommandsAreComplemented = "未設定のコマンドは補完されます。";
            ThisOperationCanBeAvoidedByAsigningNoOperationToTargetKeys = "この動作は対象のキーに「何もしない」を割り当てることで回避できます。";
            //IfYouWantToRestoreDefaultLeftClickBehaviorDeleteAllInputsIncludingLeftClick = "左クリックの動作をデフォルトに戻す場合は「クリック」を含む入力を全て削除してください。（「右クリック」等を削除する必要はありません）";
            WhenClickOnAndClickConflictTheFormerTakesPrecedence = "位置指定されたクリックと通常のクリックが競合した場合は前者が優先されます。競合を回避するには不要な入力を削除して下さい。「何もしない」コマンドでは競合を回避できません。";
            DoYouRestoreDefaultSettingOfKeysMouseButtons = "キーボード／マウスボタンを初期設定に戻します。";

            GoBack = "戻る";
            GoForward = "進む";
            GoUp = "上へ";
            ResetFilter = "フィルター消去";
            ScrollToTop = "一番上へスクロール";
            ScrollToBottom = "一番下へスクロール";
            ScrollToSelection = "選択項目へスクロール";

            SmartFolder = "スマートフォルダー";
            EditSmartFolder = "スマートフォルダーを編集";
            FolderPath = "フォルダーパス";
            Essential = "必須";
            Filter = "フィルター";
            CheckAll = "全てチェック";
            Save = "保存";
            Overwrite = "上書き";
            Editing1 = "編集中：$1";
            //NonEssentialFolderIsIgnoredIfItDoesNotExist = "「必須」でないフォルダーは存在しない場合無視されます。";
            IfEssentialFolderDoesNotExistErrorWillBeDisplayed = "「必須」のフォルダーが存在しない場合エラーが表示されます。";
            HintRightClickNameOfKdkFileOnAddressBarToEditItAgain = "ヒント：アドレスバー上の .kdk ファイルの名前を右クリックすると再度編集できます。";

            SelectInFolderTree = "フォルダーツリーで選択";

            CommonSettings = "共通設定";
            LanguageLanguage = "言語 (Language)";
            Edit = "編集";
            OpenLanguageFolder = "\"language\" フォルダーを開く";
            UpdateAndEditLanguageFile = "言語ファイルを更新して編集";
            TinyOnScreenKeyboard = "簡易仮想キーボード";
            HighlightSelectionWhenMouseLeaves = "マウス移動で選択項目を強調";
            ClearFilterWhenMovingLocation = "場所移動時にフィルターを消去";
            LoadLastPageViewed = "最後に見たページを読み込む";
            HowToOpenFile = "ファイルの開き方";
            ClickSamePointTwiceQuickly = "同じ位置で素早く二回クリック";
            ClickSameItemTwiceQuickly = "同じ項目を素早く二回クリック";
            ClickSelectedItem = "選択された項目をクリック";

            ContextMenu = "右クリックメニュー";
            Standard = "標準";
            OnlyTags = "タグのみ";

            FolderViewSettings = "フォルダーの表示設定";
            DisplayMixingWithFiles = "ファイルと区別せず表示";
            DisplayFirst = "先頭に表示";
            DisplayLast = "末尾に表示";
            DisplayRespectingToSortOfDirection = "ソート方向に合わせて表示";
            //ShowFolderSizeOnStatusBar = "フォルダーサイズをステータスバーに表示";

            /*
            ArrangementOfFolders = "フォルダーの並べ方";
            MixWithFiles = "ファイルと区別しない";
            DisplayFirst = "先頭に表示";
            DisplayLast = "末尾に表示";
            FitToDirectionOfSort = "ソート方向に合わせる";
            */
            //ItemSelectionFeedback = "選択項目のフィードバック";
            AutomaticSelectionSwitching = "選択項目の自動切り替え";
            InAppropriateCases = "適切な場合に";

            FolderTreeSynchronizing = "フォルダーツリーの同期";
            FolderTreeToAddresBar = "フォルダーツリーからアドレスバー";
            AddresBarToFolderTree = "アドレスバーからフォルダーツリー";
            TwoWay = "双方向";
            CollapseOthersWhenSynchronizing = "同期するときに他を折りたたむ";

            LoadWholeArchiveIntoMemory = "書庫全体をメモリにロード";
            ExceptReadAheadProcess = "先読み用プロセスを除く";
            LoadArchivesInArchive = "書庫内書庫の読み込み";
            NoneRecommended = "なし（推奨）";
            //OnlyIfThereIsNoOtherImage = "他に画像がない場合";
            TwoLevelUntilFound = "2 階層見つかるまで";
            OneLevelCompletelyNotRecommended = "1 階層全て（非推奨）";
            Always = "いつでも";
            DefaultPageSequence = "デフォルトの綴じ方";
            MaximumNumberOfHistories = "最大履歴数";
            BuiltInViewerMemoryUsage = "ビューアーのメモリ使用量";
            Minimum = "最小限";
            Around1 = "$1 前後";
            Automatic = "自動";
            BuiltInViewerMemoryUsageDescription =
                "＊メモリサイズの値は目安です。\\n" +
                "＊利用可能なメモリ容量よりも大きな値が指定された場合は\\n"  +
                "　「自動」と同じ動作をします。";
            SusiePlugins = "Susie プラグイン";
            AllowUntestedPlugins = "未検証のプラグインを許可";
            SearchNotOnlyZipPlaFolderButAlsoSusieInstallationFolder = @"ZipPla のフォルダーだけでなく\nSusie インストールフォルダーも検索";
            //HintMessage = "起動時のヒント";

            ZipPlaUpdateCheck = "ZipPla の更新チェック";
            CheckNow = "今すぐ確認";
            Checking = "確認中";
            ShowDialogBeforeConnection = "通信時に通知";
            //AskPermissionBeforeInternetConnection = "通信前にユーザーの許可を求める";
            OtherChecksOnStartup = "その他の起動時チェック";
            //CheckOnStartup = "Check on startup";
            //CheckOnStartup = "起動時にチェック";
            //DoNotShowAgain = "二度目以降は表示しない";
            DragAndDrop = "ドラッグ＆ドロップ";
            //ShowThumbnails = "サムネイルを表示";
            //OpenInZipPla = "ZipPla で開く";
            BrowseInZipPla = "ZipPla で閲覧";
            MoveOrCopyFilesAddItemToVirtualFolder = @"ファイルの移動／コピー\n／仮想フォルダーに追加";
            DynamicStringSelection = "右クリックメニューの文字列選択";
            DisplayName = "表示名";
            DisplayNameForNonSelection = "表示名（非選択時）";
            Applications = "起動プログラム";
            //Name = "名前";
            Params = "起動オプション";
            Multi = "複数";
            OpenOnClick = "クリックで起動";
            ShowInContextMenu = "右クリックメニュー";
            RenameItemByEditingTextBox = "テキストボックスの編集で名前を変更";
            AllFiles = "全てのファイル";
            //StartWithAccessKeyContainedInName = "名前に含まれるアクセスキーで起動";
            //EnableAccessKeysInAlsoBookmark = "アクセスキーの有効化（ブックマークにも影響）";
            AboutAccessKey = "アクセスキーについて";
            /*
            AccessKeyUsageDescription =
                @"起動プログラムをキーボードから直接起動する機能を提供します。\n" +
                @"例えば「内蔵ビューアーで開く」を「内蔵ビューアーで開く(&V)」に書き換えてこのチェックボックスをチェックすると、\n" +
                @"サムネイルウィンドウで画像ファイルを選択してキーボードの V キーを押すことで内蔵ビューアーが起動します。\n" +
                @"\n" +
                @"追加効果として、ブックマークでもアクセスキーが使えるようになります。";
                */
            ApplicationAccessKeyUsageDescription =
                @"起動プログラムの表示名に &A のような「アクセスキー」を含めることで、\n" +
                @"キーボードを使ってプログラムを起動することができるようになります。\n" +
                @"例えば「内蔵ビューアーで開く」を「内蔵ビューアーで開く(&V)」に書き換えると、\n" +
                @"画像ファイルを選択してキーボードの V キーを押すことで内蔵ビューアーが起動します。\n" +
                @"\n" +
                @"※ アクセスキーはブックマークにも設定可能です。";
            BookmarkAccessKeyUsageDescription =
                @"ブックマークの別名に &A のような「アクセスキー」を含めることで、\n" +
                @"キーボードを使ってブックマークを開くことができるようになります。\n" +
                @"例えばデスクトップのブックマークに「デスクトップ(&D)」という別名を付けると、\n" +
                @"キーボードの D キーを押すことでデスクトップが開かれます。\n" +
                @"\n" +
                @"※ アクセスキーは起動プログラムにも設定可能です。";

            DoYouRestoreDefaultSettingOfDynamicStringSelection = "右クリックメニューの文字列選択に関する設定を初期状態に戻します。";
            IfUrlIsRegisteredZipPlaAccessesInternet = "「右クリックメニューの文字列選択」に URL が登録されていると、そのページのアイコンを取得するため ZipPla は自動的にインターネットにアクセスします。";
            DoYouRestoreDefaultSettingsOfApplications = "起動プログラムを初期設定に戻します。";
            ExternalApplication = "外部プログラム";
            Explorer = "エクスプローラー";
            BuiltInViewer = "内蔵ビューアー";
            //TargetItemsAreSpecifiedByRegularExpression = @"対象の名前は /\.(zip|rar|pdf)$/i のような正規表現で指定してください。";
            ApplicationFilterDescription = @"ファイル／フォルダーの指定には以下の形式が使用できます。\n\n\n全てのファイル／フォルダー\n*;\\n\nZIP ファイル／フォルダー／RAR ファイル\nzip;\;rar\n\n正規表現\n/\.jpe?g$/i";

            //Viewer = "ビューアー";
            //ExternalViewer = "外部ビューアー";
            //BuiltInViewer = "内蔵ビューアー";
            //DefaultViewer = "デフォルトビューアー";
            
            OK = "OK";
            Cancel = "キャンセル";

            FileBroken = "ファイルが破損しています。";
            NoReadableFile = "読み込み可能なファイルがありません。";
            BuiltInViewerProcessStopsResponding = "内蔵ビューアーが応答しません。書籍データの読み込みに時間がかかっている、または書籍データが正常でない可能性があります。";

            //ToCopyAndPasteItemInCompressedFileKeepItOpenUntilPastingIsCompleted = "圧縮ファイル内の項目をコピー＆ペーストする場合、貼り付けが完了するまで元の圧縮ファイルを開いたままにしておく必要があります。";

            DoYouDiscardChangedSettings = "設定の変更を破棄してもよろしいですか？";
            DoYouDiscardChangedName = "名前の変更を破棄してもよろしいですか？";
            DoYouRenameThis = "名前を変更しますか？";
            DoYouRenameFollowing1Items = "以下の $1 個の項目の名前を変更しますか？";
            //DoYouOverwriteFollowing1Items = "以下の $1 個の項目を上書きしてもよろしいですか？";
            //DoYouSendSelectedItemsToRecycleBin = "選択項目をごみ箱へ移動してもよろしいですか？";
            //DoYouDeleteSelectedItemsPermanently = "選択項目を完全に削除してもよろしいですか？";
            DoYouSendFollowing1ItemsToRecycleBin = "以下の $1 個の項目をごみ箱に移動しますか？";
            DoYouDeleteFollowing1ItemsPermanently = "以下の $1 個の項目を完全に削除しますか？";
            DoYouRemoveFollowing1ItemsFromVirtualFolder = "以下の $1 個の項目を仮想フォルダーから除去しますか？";
            DoYouClearLocationHistory = "フォルダーの履歴を全て消去します。";
            DoYouClearFilterHistory = "フィルターの履歴を全て消去します。";

            Question = "問い合わせ";
            Information = "情報";
            Warning = "警告";
            Error = "エラー";

            FailedToLoadFile = "ファイルの読み込みに失敗しました。";

            RenameForRating = "レート情報を以下のようにファイル／フォルダー名に埋め込みます。";
            RenameForTags = "タグ情報を以下のようにファイル／フォルダー名に埋め込みます。";
            RenameForCover = "表紙情報を以下のようにファイル／フォルダー名に埋め込みます。";
            WillClearCoverSetting = "表紙情報を以下のようにファイル／フォルダー名から取り除きます。";
            RenameForPageSequenceRightToLeft = "現在の綴じ方（右綴じ）を以下のようにファイル／フォルダー名に埋め込みます。";
            RenameForPageSequenceLeftToRight = "現在の綴じ方（左綴じ）を以下のようにファイル／フォルダー名に埋め込みます。";
            RenameForPageSequenceSinglePage = "現在の綴じ方（単ページ）を以下のようにファイル／フォルダー名に埋め込みます。";
            WillClearPageSequenceSetting = "綴じ方の情報を以下のようにファイル／フォルダー名から取り除きます。";

            FollowingDllCouldNotBeLoaded = "以下の dll を読み込むことができませんでした。";
            ConfirmItIsNotFor1But2 = "現在の環境に適合する $2 用でなく $1 用を使用している可能性があります。";

            //_1IsNotFound = "\"$1\" が見つかりません。";
            _1CanNotBeOpend = "\"$1\" を開くことができません。";

            BeforeRename = "リネーム前";
            AfterRename = "リネーム後";

            //DoneCoverSetting = "表紙の設定が完了しました。";
            //DonePageSequenceSetting = "綴じ方の設定が完了しました。";
            
            _1AlreadyExists = "$1 は既に存在します。";
            ItIsInvalidFileName = "無効なファイル名です。";

            //NewVersionIsAvailableDoYouOpenDownloadPage = "ZipPla の新しいバージョンが公開されています。ダウンロードページを開きますか？";
            ZipPlaWillCheckForUpdate = "ZipPla の更新チェックを行います。";
            ShowThisDialogAgainNextTime = "次回もこのダイアログを表示";
            NewVersionIsAvailable = "ZipPla の新しいバージョンが公開されています。";
            DownloadPage = "ダウンロードページ";
            HowToDownload = "作者のページ";
            HowToDownloadURL = "https://sites.google.com/site/riostoolbox/zippla";
            ZipPlaFolder = "ZipPla のフォルダー";
            Close = "閉じる";
            NewVersionIsNotAvailable = "最新版を使用中です。";
            YouAreUsingUnreleasedVersion = "公開前のバージョンを使用中です。";

            History = "履歴";

            MiddleClick = "中クリック";
            MiddleClickTwoFingerTap = "中クリック／二本指タップ";
            RightClickPressAndTap = "右クリック／プレス＆タップ";

            TagSample1 = "タグ 1";
            TagSample2 = "タグ 2";
            TagSample3 = "タグ 3";

            RightClickDownFlickToOpenContextMenu = "右クリック／下フリックでメニューを開く";
            LeftClick = "クリック";

            IncreaseReferenceValue = "基準値を増やす";
            DecreaseReferenceValue = "基準値を減らす";
            EqualTo1 = "$1";
            GreaterThanOrEqualTo1 = "$1以上";
            AndEqualTo1 = "AND $1";
            AndGreaterThanOrEqualTo1 = "AND $1以上";
            OrEqualTo1 = "OR $1";
            OrGreaterThanOrEqualTo1 = "OR $1以上";

            Include1 = "\"$1\"";
            Exclude1 = "NOT \"$1\"";
            AndInclude1 = "AND \"$1\"";
            AndExclude1 = "AND NOT \"$1\"";
            OrInclude1 = "OR \"$1\"";
            OrExclude1 = "OR NOT \"$1\"";

            Tap = "タップ";
            RightFlick = "右フリック";
            LeftFlick = "左フリック";
            UpFlick = "上フリック";
            RightUpFlick = "右→上フリック";
            LeftUpFlick = "左→上フリック";

            MoveHere = "ここに移動";
            CopyHere = "ここにコピー";
            //IntegrityErrorBetweenTagsAndProfileColor = "タグとプロファイルカラーとの間の整合性に問題があります。ブックマークからThere is an integrity error between tags and profile color. It will be solved by clicking any bookmark item.";

            //NowLoading = "読み込み中";

            NewFolderCanBeCreatedButSomeOfThemWillNotBeDisplayedBecauseOfViewSetting = "新しいフォルダーは作成可能ですが、一部または全部の項目は現在の「動作」設定により非表示になります。";
            ItemsCanBeMovedCopiedButSomeOfThemWillNotBeDisplayedBecauseOfViewSetting = "移動／コピーは可能ですが、一部または全部の項目は現在の「動作」設定により非表示になります。";
            ItemsCanBeMovedCopiedButSomeOfThemWillNotBeDisplayedBecauseOfItsAttribute = "移動／コピーは可能ですが、一部または全部の項目はその属性により非表示になります。";


            FolderOpenDescription = "表示したいファイルやフォルダーをここにドラッグ＆ドロップするか、\\n「スタート(S) > フォルダーを開く...」でフォルダーを選択してください。";
            AddBookmarkDescription = @"←「追加」ボタンを右クリックか下フリック\n　すると専用のメニューが表示されます。";

            //Hint = "ヒント";
            //InTheFutureDoNotShowThisMessage = "今後このメッセージを表示しない";
            CheckExistenceOfFfmpegAtStartup = "起動時に ffmpeg.exe の存在をチェックする";

            RecommendFfmpeg =
                @"ffmpeg.exe が見つかりません。\n" +
                @"ffmpeg.exe を ZipPla.exe と同じフォルダーに置くと\n" +
                @"ZipPla で動画ファイル（mp4, wmv, avi, mkv, flv, webm 等）のサムネイルを表示できます。\n" +
                @"\n" +
                @"\n" +
                @"ffmpeg.exe は以下のページからダウンロードできます。\n" +
                @"このパソコンで使用する場合は $1-bit / Static 版を推奨します。\n" +
                @"\n" +
                @"https://ffmpeg.zeranoe.com/builds/\n" +
                @"\n" +
                @"ダウンロードした圧縮ファイル内の bin フォルダーの中に ffmpeg.exe が格納されています。";/*\n" +
                @"\n" +
                @"\n" +
                @"※ エクスプローラーの設定によっては\n" +
                @"　　ffmpeg.exe/ZipPla.exe ではなく ffmpeg/ZipPla と表示されることもあります。";*/
            
            RecommendFfmpegRemarkForHiddenExtension = @"※ 現在の PC の設定では、エクスプローラー上では\n" +
                @"　 ffmpeg.exe や ZipPla.exe の .exe の部分は表示されません。";

             RecommendNgen =
                @"ngen.exe により ZipPla の起動を高速化できます。実行しますか？\n" +
                @"\n" +
                @"ngen.exe はバックグラウンドで実行され、完了までに数秒から数十秒かかります。\n" +
                @"処理が完了した時点で ZipPla が終了されていなければ結果が通知されます。";
            SucceedInNgen = "ngen.exe の実行が完了しました。次回から ZipPla の起動が高速化されます。";
            FailInNgen = @"ngen.exe が正しく実行されませんでした。\n不具合報告のご協力をお願いいたします。\n\nエラーコード：$1";
            DoYouRemoveNativeImage = @"ngen.exe が ZipPla に適用されています。元の状態に戻しますか？";

            ConfigSaveErrorMessage =
                @"設定を保存する際に障害が発生したため設定ファイルのバックアップが作成されています。\n" +
                @"設定データの保護のため、所定の操作を行うまで ZipPla を起動することはできません。\n" +
                @"以下のメールアドレスまで問題をご報告いただけると幸いです。\n" +
                @"riostoolbox@gmail.com\n" +
                @"\n" +
                @"ご不便をおかけして申し訳ございません。";

            _Yes = "はい(&Y)";
            _No = "いいえ(&N)";
        }

        private static void SetEnglishMessages()
        {
            _Start = "&Start";
            OpenHistory = "Open history";
            OpenFolder = "Open folder";
            //OpenVirtualFolder = "Open virtual folder";
            OpenFile = "Open file";
            AllSupportedFiles = "All supported files";
            VideoFilesRequireFfmpeg = "Video files (require ffmpeg.exe)";
            NewVirtualFolder = "New virtual folder";
            NewSmartFolder = "New smart folder";
            OpenBuiltInViewer = "Open built-in viewer";
            //ReloadAllItems = "Reload all items";
            Reload = "Reload";
            NewFolder = "New folder";
            EditAddressBar = "Edit address bar";
            EditFilter = "Edit filter";
            Exit = "Exit";
            _View = "&View";
            _Behavior = "&Behavior";
            Bookmark = "Bookmark";
            FolderTree = "Folder tree";
            Thumbnails = "Thumbnails";
            Details = "Details";
            Layout = "Layout";
            VerticalLayout = "Thumbnails / Details";
            AlternativeVerticalLayout = "Details / Thumbnails";
            HorizontalLayout = "Thumbnails | Details";
            AlternativeHorizontalLayout = "Details | Thumbnails";
            BuiltInViewerSettings = "Built-in viewer settings";
            BlackBackground = "Black background";
            GrayBackground = "Gray background";
            WhiteBackground = "White background";
            SelectBackgroundColor = "Select background color";
            ForceFirstPageToBeSingle = "Force first page to be single";
            OpenInPreviousRotationToneCurveSetting = "Open in previous rotation / tone curve setting";
            SortFilesWithSameOrderAsThumbnailWindow = "Sort files with the same order as thumbnail window";
            AllowReadAheadProcess = "Allow read-ahead process";
            CreateShortcutFromCurrentSettings = "Create shortcut from current settings";
            OpenInWindow = "Open in window";
            OpenInPreviousFullscreenMode = "Open in previous fullscreen mode";
            OpenInFullscreen = "Open in fullscreen";
            MaxCountCaptchaInAVideo = "Max count captcha in a video";
            FitOnScreen = "Fit on the screen";
            EverySecond = "Every second";
            //Vertical = "Vertical";
            //AlternativeVertical = "Alternative vertical";
            //Horizontal = "Horizontal";
            //AlternativeHorizontal = "Alternative horizontal";
            //ThumbnailList = "Thumbnail list";
            //FileList = "File list";
            //PageInFileList = "Page time in file list";
            //RatingInFileList = "Rating in file list";
            //DateAccessedInFileList = "Date accessed in file list";
            //DateModifiedInFileList = "Date modified in file list";
            ArchiveFilesPdfFiles = "Archive files / PDF files";
            ImageFiles = "Image files";
            VideoFiles = "Video files";
            OtherFiles = "Other files";
            //AtLeastOneKindOfItemsMustBeSelected = "At least one kind of items must be selected.";
            Folders = "Folders";
            //IgnoreFolders = "Ignore filders";
            SearchSubfoldersAsWell = "Search subfolders as well";
            _Thumbnail = "&Thumbnail";
            //ReloadAllThumbnails = "Reload all thumbnails";
            Tiny = "Tiny";
            Small = "Small";
            Normal = "Normal";
            Large = "Large";
            Huge = "Huge";
            AllRatios = "All ratios";
            //ShowFilename = "Show filename";
            ShowName = "Show name";
            Letterbox = "Letterbox";
            PanAndScan = "Pan and Scan";
            SmartClip = "Smart clip";
            ShowIcon = "Show icon";
            ShowRating = "Show the rating";
            ShowTags = "Show tags";
            NumberOfLinesOfName = "The number of lines of name";
            //FromTheLongestName = "From the longest name";
            Full = "Full";
            //FromThumbnailWidthLess = "From thumbnail width (less)";
            //FromThumbnailWidthGreater = "From thumbnail width (greater)";
            AutomaticLess = "Automatic (less)";
            AutomaticGreater = "Automatic (greater)";
            MinimumFrameThickness = "Minimum frame thickness";
            //ZeroThickness = "Zero";
            VeryThin = "Very thin";
            Thin = "Thin";
            NormalThickness = "Normal";
            Thick = "Thick";
            VeryThick = "Very thick";
            Aligning = "Aligning";
            TotallyUniformed = "Totally uniformed";
            HorizontallyUniformed = "Horizontally uniformed";
            AlignedLeft = "Aligned left";
            AlignedOnCenter = "Aligned on center";
            MouseWheelScrollAmount = "Mouse wheel scroll amount";
            OneLine = "1 line";
            _1Lines = "$1 lines";
            ThumbnailCache  = "Thumbnail cache";
            /*
            ThumbnailCacheFirstDescription =
                @"By using the thumbnail cache,\n" +
                @"thumbnails you view are stored in some location for quick viewing later.";
            ThumbnailCacheFirstDescriptionForFirstLaunch =
                @"By using the thumbnail cache, thumbnails you view are stored in some location for\n" +
                @"quick viewing later. This option can be changed from ""Thumbnail > Thumbnail cache...""";
                */
            ThumbnailCacheFirstDescription =
                @"This option allow thumbnails you view to be stored in some location\n" +
                @"for quick viewing later.";
            ThumbnailCacheFirstDescriptionForFirstLaunch =
                @"This option allow thumbnails you view to be stored in some location\n" +
                @"for quick viewing later. It can be changed from ""Thumbnail > Thumbnail cache...""";
            StoreCachesInAlternateDataStreamRecommended  = "Store caches in Alternate Data Stream (recommended)";
            ThumbnailCacheAdsDescription =
                @"- The original file is not changed.\n" +
                @"- Each cache is renamed / moved / removed along with the original file.\n" +
                @"  You do not have to use ZipPla for the file operation.\n" +
                @"- When this is unchecked, existing caches are automatically removed.";
            StoreCachesInSpecifiedFolder = "Store caches in the specified folder";
            ThumbnailCacheSpecifiedFolderDescription =
                @"- Of course, the original file is not changed.\n" +
                @"- The folder is automatically created.\n" +
                @"- Each cache is renamed / moved / removed along with the original file\n" +
                @"  ONLY IF the file operation is performed by ZipPla.\n" +
                @"- You can either use an absolute path,\n" +
                @"  or a relative path from the original file location.\n" +
                @"- Even if this is unchecked, caches are NOT automatically removed.";
            LocationOfZipPlaExe = "Location of ZipPla.exe";
            LocationOfOriginalFile = "Location of the original file";
            Browse = "Browse";
            NotUseThumbnailCache = "Not use the thumbnail cache";
            AdsNotSupportedWarningMessage =
                @"The file system of this location does not support ADS(Alternate Data Stream). " +
                @"In order not to display this message, change the thumbnail cache setting to some mode other than ADS. " +
                @"If you want to use ADS mode for other locations, consider using ""Profile color.""";
            //CreateThumbnailCache = "Create thumbnail cache";
            //AboutThumbnailCache = "About thumbnail cache";
            /*
            ThumbnailCacheDescription =
                @"By using the thumbnail cache, thumbnails you view are stored\n" +
                @"in Alternate Data Streams for quick viewing later.\n" +
                //@"\n" +
                @" - The original file is not changed.\n" +
                //@"\n" +
                @" - Each cache is renamed/moved/removed along with the original file.\n" +
                @"    You do not have to use ZipPla for the file operation.\n" +
                @"\n" +
                @"When ""Create thumbnail cache"" is unchecked, existing thumbnails are automatically removed.";*/
            _Others = "&Others";
            _ChangeCurrentProfileColor = "&Change current profile color";
            Edit_Tags = "Edit &tags";
            //_KeysMouseButtons = "&Keys / mouse buttons";
            _BasicOperationSettings = "&Basic operation settings";
            //_MouseGestures = "&Mouse gestures";
            _MouseTouchGestures = "&Mouse/touch gestures";
            Common_Settings = "Common &settings";

            ScreenEffectAtRightSide = "Screen effect at right side";
            ScreenEffectAtLeftSide = "Screen effect at left side";
            ScreenEffectAtNextPageSide = "Screen effect at next page side";
            ScreenEffectAtPreviousPageSide = "Screen effect at previous page side";

            StartMenu = "\"Start\" menu";
            MoveMenu = "\"Move\" menu";
            ViewMenu = "\"View\" menu";
            SlideshowMenu = "\"Slideshow\" menu";
            VirtualFolderMenu = "\"Virtual folder\" menu";
            EditMenu = "\"Edit\" menu";
            OthersMenu = "\"Others\" menu";

            NoOperation = "No operation";

            //GoBackAltLeft = "Go back (Alt+←)";
            //GoForwardAltRight = "Go forward (Alt+→)";
            RightClickDownFlickToShowHistory = "Right-click/down-flick to show history";

            ClearMenuForAddressBarHistory = "Clear menu for address bar history";
            DeleteCurrentPathFromHistory = "Delete current path from history";
            ClearAllHistoryOfLocations = "Clear all history of locations";

            TypeInAsc = "Type in asc.";
            TypeInDesc = "Type in desc.";
            NameInAsc = "Name in asc.";
            NameInDesc = "Name in desc.";
            RatingInAsc = "Rating in asc.";
            RatingInDesc = "Rating in desc.";
            CreatedInAsc = "Created in asc.";
            CreatedInDesc = "Created in desc.";
            AccessedInAsc = "Accessed in asc.";
            AccessedInDesc = "Accessed in desc.";
            ModifiedInAsc = "Modified in asc.";
            ModifiedInDesc = "Modified in desc.";
            SizeInAsc = "Size in asc.";
            SizeInDesc = "Size in desc.";
            Random = "Random";

            MiddleClickToClearPaste = "Middle-click to clear/paste";
            LeftFlickToClear = "Left-flick to clear";
            RightFlickToPaste = "Right-flick to paste";
            MiddleClickLeftFlickToResetRating = "Middle-click/left-flick to reset the rating";

            EditMenuForFilter = "Edit menu for filter";
            DeleteCurrentFilterFromDropdown = "Delete current filter from the dropdown";
            ClearAllDropdownItems = "Clear all dropdown items";
            EditDropdownOfFilters = "Edit dropdown of filters";

            _Add = "&Add";
            //AddWithCurrentProfileColor = "Add with the current profile color";
            //AddWithNewProfileColor = "Add with a new profile color";
            AddWithCurrentProfileColor = "Add with current profile color";
            BookmarkWithCurrentProfileColor = "Bookmark with current profile color";
            AddWithNewProfileColor = "Add with new profile color";
            BookmarkWithNewProfileColor = "Bookmark with new profile color";
            OnlyCurrentProfileColor = "Only current profile color";
            OnlyNewProfileColor = "Only new profile color";
            Separator = "Separator";
            AddSeparator = "Add separator";

            SelectedColorIsUsedByFollowingBookmarks = "Selected color has already been used by the following bookmark(s).";
            //DoYouUseColorWhichIsCloseToOtherColors = "Selected color is very close to some existing color(s). Is it OK?";
            HowToAddUsedColorDescription =
                "To add a new bookmark colored by a used color, " +
                "open a existing bookmark colored by the color, move location, and use \"Add > With the current profile color\" function.";

            ChangeCurrentProfileColor = "Change current profile color";
            WillChangeCurrentProfileColor = "The current profile color will be changed.";
            ThisChangeAlsoAffectsFollowingBookmarks = "This change will also affect the following bookmark(s).";
            HowToCreateNewColorDescription = "To create a new profile color instead of changing the current profile color, right-click on \"Add\" above the bookmark and select \"Bookmark with new profile color\" or \"Only new profile color\".";

            Delete = "Delete";
            DoYouDeleteFollowing1Items = "Are you sure that you want to delete the following $1 items?";
            EditAlias = "Edit alias";
            ProfileColor = "Profile color";
            ProfileColorOf1 = "Profile color of $1";
            RealName = "Real name";
            LoadOnlyProfileColor = "Load only profile color";
            LoadProfileColor = "Load profile color";
            //SetToCurrentLocation = "Set to current location";
            //SetToCurrentLocationAndState = "Set to current location and state";

            FilterWithSelectedText = "Filter with selected text";
            FilterWith1 = "Filter with \"$1\"";
            CopySelectedText = "Copy selected text";
            Copy1 = "Copy \"$1\"";
            SearchSelectedTextOnGoogle = "Search selected text on Google";
            Search1OnGoogle = "Search \"$1\" on Google";
            NewCustomCommand = "New custom command";
            OpenInBuiltInViewer = "Open in built-in viewer";
            OpenIn1 = "Open in $1";
            Open = "Open";
            //MoveFilesFolders = "Move files/folders";
            //MoveSelectedItemsTo1 = "Move selected item(s) to $1";
            MoveExtractSelectedItems = "Move/Extract selected item(s)";
            MoveExtractSelectedItemsTo1 = "{Move|Extract} selected item(s) to $1";
            //MoveThisTo1 = "Move this to $1";
            //MoveTheseTo1 = "Move these to $1";
            AssociatedApplication = "Associated application";
            //MoveToThisFolder = "Move to this folder";
            //MoveHere = "Move here";
            OpenInCurrentWindow = "Open in current window";
            //MoveToLocationOfThis = "Move to the location of this";
            SubdivideAroundHere = "Subdivide around here";
            //MoveLocation = "Move location";
            CurrentZipPla = "Current ZipPla";
            ExecutableInClipboard = "Executable in clipboard";
            OpenInExplorer = "Open in Explorer";
            CopyAddressAsText = "Copy address as text";
            ApplicationIsNotSpecified = "Application is not specified.";
            Rating = "Rating";
            None = "None";
            //MiddleClickOnStars = "Middle-click on stars";
            Tag = "Tag";
            RightClickNotToClose = "Right-click NOT to close";
            RightFlickIsAlsoOk = "Right-flick is also OK";
            SameAsAbove = "Same as above...";
            UncheckAll = "Uncheck all";
            //AddTagsFromFileName = "Add tags from the file name";
            AddTagsFromName = "Add tags from the name";
            VirtualFolder = "Virtual folder";
            AddToNewVirtualFolder = "Add to new virtual folder";
            RemoveFromHistory = "Remove from history";
            //AddToExistingVirtualFolder = "Add to existing virtual folder";
            AddTo1 = "Add to \"$1\"";
            NoVirtualFoldersInBookmark = "No virtual folders in bookmark";
            //ReloadSelectedThumbnail = "Reload this thumbnail";
            Cover = "Cover";
            SetCoverManually = "Set cover manually";
            SetCoverToThis = "Set cover to this";
            SetCoverOfParentFolderToThis = "Set cover of parent folder to this";
            AbortSettingCover = "Abort setting cover";
            RightClick = "Right-click";
            ClearCoverSetting = "Clear cover setting";
            PageSequence = "Page sequence";
            Default = "Default";
            LeftToRight = "Left to right";
            RightToLeft = "Right to left";
            SinglePage = "Single page";
            SinglePageWithoutScalingUp = "Single page without scaling up";
            //ForcePageSpread = "Force page spread";
            PrioritizePageSpread = "Prioritize page spread";
            AllowPageDivision = "Allow page division";
            ClearPageSequenceSetting = "Clear page sequence setting";
            Cut = "Cut";
            Copy = "Copy";
            CopyCapturedImage = "Copy the captured image";
            Paste = "Paste";
            Rename = "Rename";
            RemoveFromVirtualFolder = "Remove from virtual folder";
            //DeleteOnlyForVirtualFolder = "Delete (only for virtual folder)";
            Properties = "Properties";

            TogglePageSequence = "Toggle page sequence";

            OrientSeekBarDirectionToPageSequence = "Orient seek bar direction to page sequence";

            ShowHintWhenChangingSettings = "Show a hint when changing settings";
            Hint = "Hint";
            BuiltInViewerSavingSettingsBehaviorDescription =
                @"This option will be reset when built-in viewer is started in a specific way.\n" +
                @"To keep the option, use ""Behavior > built-in viewer settings"" in thumbnail window\n" +
                @"or ""Start > Create shortcut from current settings..."" in built-in viewer.";
            ShowThisHintAgainNextTime = "Show this hint again next time"; ;

            //_View = "&View";
            //_Image = "&Image";
            _Move = "&Move";
            S_lideshow = "S&lideshow";
            Virtual_Folder = "Virtual &folder";
            _Edit = "&Edit";

            OpenThumbnailWindow = "Open thumbnail window";
            SelectCurrentImageInThumbnailWindow = "Select current image in thumbnail window";
            SwitchToThumbnailWindow = "Switch to thumbnail window";
            CloneWindow = "Clone the window";
            Fullscreen = "Fullscreen";
            Window = "Window";
            ToggleFullscreenMode = "Toggle fullscreen mode";

            AlwaysAutomaticallyHideUI = "Always automatically hide UI";

            ToggleMagnifier = "Toggle magnifier";
            ToggleMagnifierWithoutOperatingGuide = "Toggle magnifier without operating guide";
            ToggleMagnifierWithMinimalScreenEffects = "Toggle magnifier with minimal screen effects";
            EnableMagnifier = "Enable magnifier";
            EnableMagnifierWithoutOperatingGuide = "Enable magnifier without operating guide";
            DisableMagnifier = "Disable magnifier";
            DisableMagnifierWithoutOperatingGuide = "Disable magnifier without operating guide";
            DisableMagnifierWithoutScreenEffects = "Disable magnifier without screen effects";
            MagnifierOperatingGuide =
                "Move: Mouse move, Arrow keys\\n" +
                "Zoom in: Wheel ↑, ↑+↓, Shift\\n" +
                "Zoom out: Wheel ↓, ←+→, Ctrl";

            //DefaultClickBehavior = "Default click behavior";
            NextPage = "Next page";
            PreviousPage = "Previous page";
            WheelDown = "Wheel ↓";
            //CursorRight = "Cursor →";
            WheelUp = "Wheel ↑";
            //CursorLeft = "Cursor ←";
            //LeftClickOnRightHalf = "Click on right half";
            //LeftClickOnLeftHalf = "Click on left half";
            //LeftClickOnCenter = "Click on center";

            Specials = "Specials";

            LeftClickOnLeftSide = "Click on left side";
            LeftClickOnRightSide = "Click on right side";
            LeftClickOnTopSide = "Click on top side";
            LeftClickOnBottomSide = "Click on bottom side";
            LeftClickOnCenter = "Click on center";
            LeftClickOnTopLeft = "Click on top-left";
            LeftClickOnTopRight = "Click on top-right";
            LeftClickOnBottomLeft = "Click on bottom-left";
            LeftClickOnBottomRight = "Click on bottom-right";

            DoubleLeftClick = "Double-click";

            NextPageAtLastPage = "\"Next page\" at last page";
            PreviousPageAtFirstPage = "\"Previous page\" at first page";

            MoveForwardOnePage = "Move forward 1 page";
            MoveBackOnePage = "Move back 1 page";
            WheelDownOnBar = "Wheel ↓ on seek bar";
            WheelUpOnBar = "Wheel ↑ on seek bar";
            //CursorDown = "Cursor ↓";
            //CursorUp = "Cursor ↑";

            RightPage = "Right page";
            LeftPage = "Left page";
            MoveRightOnePage = "Move right 1 page";
            MoveLeftOnePage = "Move left 1 page";
            PositionRatio = "Ratio";
            OpenNext = "Open next";
            X2Button = "X2 button";
            OpenPrevious = "Open previous";
            X1Button = "X1 button";
            OpenRight = "Open right";
            OpenLeft = "Open left";
            
            NextPageOrAutoForwardScroll = "Next page or scroll";
            PreviousPageOrAutoBackScroll = "Previous page or scroll";
            NextPageOrAutoForwardScrollWithoutOverwrap = "Next page or scroll (w/o overwrap)";
            PreviousPageOrAutoBackScrollWithoutOverwrap = "Prev. page or scroll (w/o overwrap)";
            AutoForwardScroll = "Forward scroll";
            AutoBackScroll = "Back scroll";
            AutoForwardScrollWithoutOverwrap = "Forward scroll (w/o overwrap)";
            AutoBackScrollWithoutOverwrap = "Back scroll (w/o overwrap)";

            RightPageOrAutoRightScroll = "Right page or scroll";
            LeftPageOrAutoLeftScroll = "Left page or scroll";
            RightPageOrAutoRightScrollWithoutOverwrap = "Right page or scroll (w/o overwrap)";
            LeftPageOrAutoLeftScrollWithoutOverwrap = "Left page or scroll (w/o overwrap)";
            AutoRightScroll = "Right scroll";
            AutoLeftScroll = "Left scroll";
            AutoRightScrollWithoutOverwrap = "Right scroll (w/o overwrap)";
            AutoLeftScrollWithoutOverwrap = "Left scroll (w/o overwrap)";

            //MiddleClickOnPosteriorHalfOfBar = "Middle-click on posterior half of seek bar";
            //MiddleClickOnAnteriorHalfOfBar = "Middle-click on anterior half of seek bar";

            BookFolderIsNotFound = "Book/folder is not found.";

            Slideshow = "Slideshow";
            StartSlideshow = "Start slideshow";
            StopSlideshow = "Stop slideshow";
            ToggleSlideshow = "Toggle slideshow";
            SpaceKey = "Space";
            OneSecondIntervals = "1 sec intervals";
            _1SecondsIntervals = "$1 sec intervals";
            Repeat = "Repeat";
            AllowToOpenNextItem = "Allow to open next item";

            RotateLeft = "Rotate left";
            RotateRight = "Rotate right";
            CancelRotationSetting = "Cancel rotation setting";
            //Binarization = "Binarization";
            //AutoContrastControl = "Auto contrast control";
            //RemoveMoire = "Remove moire";
            ToneCurve = "Tone curve";
            ToneCurveSepiaTone = "Tone curve (sepia tone)";
            ToneCurveUserDefined = "Tone curve (user-defined)";
            
            ScalingAlgorithmNormal = "Scaling algorithm (normal)";
            ScalingAlgorithmMagnifier = "Scaling algorithm (magnifier)";
            DefaultHighQuality = "Default (high quality)";
            DefaultHighSpeed = "Default (high speed)";
            NearestNeighbor = "Nearest neighbor";
            AreaAverage = "Area average";
#if !AUTOBUILD
            //AntiMoire = "AntiMoire"; // "ZipPla" と同様固有名扱い、古くはこちら
            //AntiMoire = "Anti-moire"; // 一般名詞的に。分数との間に空白を入れるならこちらが妥当か
#endif
            LinearizeColorSpace = "Linearize color space";
            LinearizeColorSpaceDescription = "Linearization of color space is ignored when using\\n" +
                "\"Default (high quality / hight speed).\"\\n" +
                "And it has no effect when using \"Nearest neighbor.\"";
            UseAreaAverageWhenUpsizing = "Use area average when upsizing";

            SaveCurrentPageSequence = "Save current page sequence";

            _1Items = "$1 items";
            _1ItemsAreSelected = "$1 items are selected";
            OnePage = "1 page";
            _1Pages = "$1 pages";

            PropertiesOf1 = "Properties of $1";
            Location = "Location";
            Icon = "Icon";
            //FileName = "File name";
            Name = "Name";
            FileType = "File type";
            Length = "Length";
            Size = "Size";
            DateCreated = "Date created";
            DateAccessed = "Date accessed";
            DateModified = "Date modified";
            Resolution = "Resolution";
            AspectRatio = "Ratio";
            Width = "Width";
            Height = "Height";
            FPS = "FPS";
            NumberOfPages = "Page count";
            ImageSize = "Image size";
            VideoInfo = "Video info.";
            Position = "Position";
            Width1Height2BitsPerPixel3 = "$1(width) x $2(height) x $3(color depth)";

            Page = "Page";

            //File = "File";
            //Folder = "Folder";
            Bytes = "Bytes";

            EditTags = "Edit tags";
            TagName = "Tag name";
            NewTag1 = "New tag $1";
            ProfileColorUnderEditing = "Profile color under editing";
            Add = "Add";
            Color = "Color";
            TagMustBeNoEmptyStringWhichDoesNotIncludeFollowingCharacters = "Tag must be a non-empty string which does not include the following characters.";
            TagsCanNotShareSameName = "Tags cannot share the same name.";

            FilteringString = "Filtering string";
            Alias = "Alias";
            EditFilters = "Edit filters";
            InvariantDropdownList = "Invariant dropdown list";
            NewFilteringString1 = "New filtering string $1";
            Presets = "Presets";
            FilterSampleDescription1 = "Folder";
            FilterSampleString1 = "\\";
            FilterSampleAlias1 = "$Folder";
            FilterSampleDescription2 = "Not contain \"textbook\" \"text book\"";
            FilterSampleString2 = "-textbook -\"text book\"";
            FilterSampleAlias2 = "$ExceptTextbook";
            FilterSampleDescription3 = "ZIP or RAR file";
            FilterSampleString3 = @"/\.(zip|rar)$/i";
            FilterSampleAlias3 = "$MajorCompressedFile";
            FilterSampleDescription4 = "Not be rated";
            FilterSampleString4 = "-r>0";
            FilterSampleAlias4 = "$NoRating";
            FilterSampleDescription5 = "Contain \"-x\"";
            FilterSampleString5 = "\"-x\"";
            FilterSampleAlias5 = "";
            FilterSampleDescription6 = "Contain at least one of wildcards \"(?)\" \"(??)\"";
            FilterSampleString6 = "(?) +(??)";
            FilterSampleAlias6 = "";
            FilterSampleDescription7 = "Contain wildcard \"pocket edition*(?*)\"";
            FilterSampleString7 = "\"pocket edition*(?*)\"";
            FilterSampleAlias7 = "";
            FilterSampleDescription8 = "Regular expression \"0\\d+\" negative match";
            FilterSampleString8 = @"/^(?!.*(0\d+))/i";
            FilterSampleAlias8 = "";
            FilterSampleDescription9 = "";
            FilterSampleString9 = "";
            FilterSampleAlias9 = "";
            AliasMustBeNoEmptyStringWhichDoesNotIncludeSpaceAndFollowingCharacters = "Alias must be a non-empty string which does not include space and the following characters.";
            DisplayNameCanNotShareSameName = "Display name cannot share the same name.";
            FilteringStringMustNotBeEmpty = "Filtering string must not be empty.";

            //MouseGestures = "Mouse gestures";
            MouseTouchGestures = "Mouse/touch gestures";
            Enabled = "Enabled";
            GestureToAddCommand = "Gesture to add a command";
            Appearance = "Appearance";
            LineWidth = "Width";
            Gesture = "Gesture";
            Command = "Command";
            InThumbnailWindowTouchGestureMustStartWithHorizontalDirection = "In thumbnail window, touch gesture must start with a horizontal direction.";

            //KeysMouseButtons = "Keys / Mouse buttons";
            BasicOperationSettings = "Basic operation settings";
            Inputs = "Inputs";
            Abort = "Abort";
            InputKeysMouseButtons = "Input keys / mouse buttons...";
            UnassignedCommandsAreComplemented = "Unassigned command(s) are complemented.";
            //ThisOperationCanBeAvoidedByAsigningNoOperationToTargetKey = "This operation can be avoided by assigning \"No operation\" to the target key.";
            ThisOperationCanBeAvoidedByAsigningNoOperationToTargetKeys = "To stop this, assign \"No operation\" to the target key(s)."; // 字数削減
            //IfYouWantToRestoreDefaultLeftClickBehaviorDeleteAllInputsIncludingLeftClick = "If you want to restore default left-click behavior, delete all inputs including \"Click.\" (Not necessary to delete any other mouse inputs such as \"Right-click.\")";
            WhenClickOnAndClickConflictTheFormerTakesPrecedence = "When \"Click on ...\" and \"Click\" conflict, the former takes precedence. If you want to avoid this conflict, delete unnecessary one instead of using \"No operation\" command.";
            DoYouRestoreDefaultSettingOfKeysMouseButtons = "Are you sure that you want to restore default setting of keys / mouse buttons?";

            GoBack = "Go back";
            GoForward = "Go forward";
            GoUp = "Go up";
            ResetFilter = "Reset filter";
            ScrollToTop = "Scroll to top";
            ScrollToBottom = "Scroll to bottom";
            ScrollToSelection = "Scroll to selection";

            SmartFolder = "Smart folder";
            EditSmartFolder = "Edit smart folder";
            Essential = "Essential";
            FolderPath = "Folder path";
            Filter = "Filter";
            CheckAll = "Check all";
            Save = "Save";
            Overwrite = "Overwrite";
            Editing1 = "Editing: $1";
            // NonEssentialFolderIsIgnoredIfItDoesNotExist = "Non-\"essential\" folder is ignored if it does not exist.";
            IfEssentialFolderDoesNotExistErrorWillBeDisplayed = "If \"essential\" folder does not exist, an error message will be displayed.";
            HintRightClickNameOfKdkFileOnAddressBarToEditItAgain = "Hint: Right-click the name of a .kdk file on address bar to edit it again.";

            SelectInFolderTree = "Select in folder tree";

            CommonSettings = "Common settings";
            LanguageLanguage = "Language";
            Edit = "Edit";
            OpenLanguageFolder = "Open \"language\" folder";
            UpdateAndEditLanguageFile = "Update and edit language file";
            TinyOnScreenKeyboard = "Tiny on-screen keyboard";
            HighlightSelectionWhenMouseLeaves = "Highlight selection when mouse leaves";
            ClearFilterWhenMovingLocation = "Clear filter when moving location";
            LoadLastPageViewed = "Load last page viewed";
            HowToOpenFile = "How to open file";
            ClickSamePointTwiceQuickly = "Click the same point twice quickly"; // 基本形容詞・副詞辞典では same と不定冠詞 a の共起を一切認めていない
            ClickSameItemTwiceQuickly = "Click the same item twice quickly";
            ClickSelectedItem = "Click a selected item";

            ContextMenu = "Context menu";
            Standard = "Standard";
            OnlyTags = "Only tags";

            FolderViewSettings = "Folder view settings";
            DisplayMixingWithFiles = "Display mixing with files";
            DisplayFirst = "Display first";
            DisplayLast = "Display last";
            DisplayRespectingToSortOfDirection = "Display respecting to sort of direction";
            //ShowFolderSizeOnStatusBar = "Show folder size on status bar";

            /*
            ArrangementOfFolders = "Arrangement of folders";
            MixWithFiles = "Mix with files";
            DisplayFirst = "Display first";
            DisplayLast = "Display last";
            FitToDirectionOfSort = "Fit to direction of sort";
            */

            //ItemSelectionFeedback = "Item selection feedback";
            AutomaticSelectionSwitching = "Automatic selection switching";
            InAppropriateCases = "In appropriate cases";

            FolderTreeSynchronizing = "Folder tree synchronizing";
            FolderTreeToAddresBar = "Folder tree -> Addres bar";
            AddresBarToFolderTree = "Addres bar -> Folder tree";
            TwoWay = "Two-way";
            CollapseOthersWhenSynchronizing = "Collapse others when synchronizing";

            LoadWholeArchiveIntoMemory = "Load whole archive into memory";
            ExceptReadAheadProcess = "Except read ahead process";
            LoadArchivesInArchive = "Load archives in archive";
            NoneRecommended = "None (recommended)";
            //OnlyIfThereIsNoOtherImage = "Only if there is no other image";
            TwoLevelUntilFound = "2 level until found";
            OneLevelCompletelyNotRecommended = "1 level completely (NOT recommended)";
            Always = "Always";
            DefaultPageSequence = "Default page sequence";
            MaximumNumberOfHistories = "Max num of histories";
            BuiltInViewerMemoryUsage = "Viewer memory usage";
            Minimum = "Minimum";
            Around1 = "Around $1";
            Automatic = "Automatic";
            BuiltInViewerMemoryUsageDescription =
                "- The number of bytes is only a guide.\\n" +
                "- If a value larger than available memory capacity is specified,\\n" +
                "   viewer behaves the same as when \"Automatic\" is specified.";
            SusiePlugins = "Susie plug-ins";
            AllowUntestedPlugins = "Allow untested plug-ins";
            SearchNotOnlyZipPlaFolderButAlsoSusieInstallationFolder = @"Search not only ZipPla folder\nbut also Susie install folder";
            //HintMessage = "Hint message";
            ZipPlaUpdateCheck = "ZipPla update check";
            CheckNow = "Check now";
            Checking = "Checking";
            ShowDialogBeforeConnection = "Notify checking";
            //AskPermissionBeforeInternetConnection = "Ask permission before Internet connection";
            OtherChecksOnStartup = "Other checks on startup";
            //CheckOnStartup = "Check on startup";
            //DoNotShowAgain = "Do not show again";
            DragAndDrop = "Drag and drop";
            //ShowThumbnails = "Show thumbnails";
            //OpenInZipPla = "Open in ZipPla";
            BrowseInZipPla = "Browse in ZipPla";
            MoveOrCopyFilesAddItemToVirtualFolder = @"Move/copy files\nAdd items to *.sor";
            DynamicStringSelection = "Dynamic string selection";
            DisplayName = "Display name";
            DisplayNameForNonSelection = "for non-selection";
            Applications = "Applications";
            //Name = "Name";
            Params = "Params";
            Multi = "Multi";
            OpenOnClick = "Open on click";
            ShowInContextMenu = "Context menu";
            RenameItemByEditingTextBox = "Rename item by editing the text box";
            AllFiles = "All files";
            //StartWithAccessKeyContainedInName = "Start with access key contained in name";
            //EnableAccessKeysInAlsoBookmark = "Enable access keys (in also bookmark)";
            AboutAccessKey = "About access key";
            /*
            AccessKeyUsageDescription =
                @"It provides the function to start program directly from the keyboard.\n" +
                @"For example, by rewriting ""Open in built-in viewer"" to ""Open in built-in &viewer"" and checking this check box,\n" +
                @"you can open the selected image file in built-in viewer by pressing the V key on the keyboard.\n" +
                @"\n" +
                @"In addition, you can use access keys in also the bookmark.";
                */
            ApplicationAccessKeyUsageDescription =
                @"Include ""Access key"" like &A in display name of an application to launch it by the keyboard.\n" +
                @"For example, by rewriting ""Open in built-in viewer"" to ""Open in built-in &viewer"",\n" +
                @"you can open the selected image file in built-in viewer by pressing the V key.\n" +
                @"\n" +
                @"* Access key can be used to also open an bookmark.";
            BookmarkAccessKeyUsageDescription =
                @"Include ""Access key"" like &A in alias of a bookmark to open it by the keyboard.\n" +
                @"For example, by setting ""&My bookmark"" to the alias of a bookmark,\n" +
                @"you can open the bookmark by pressing the M key.\n" +
                @"\n" +
                @"* Access key can be used to also launch an application.";
            DoYouRestoreDefaultSettingOfDynamicStringSelection = "Are you sure that you want to restore default settings of dynamic string selection?";
            IfUrlIsRegisteredZipPlaAccessesInternet = "If a command containing a URL is registered, ZipPla will automatically access the Internet to get the favicon.";
            DoYouRestoreDefaultSettingsOfApplications = "Are you sure that you want to restore default settings of applications?";
            ExternalApplication = "External Application";
            Explorer = "Explorer";
            BuiltInViewer = "Built-in viewer";
            //TargetItemsAreSpecifiedByRegularExpression = @"Target items are specified by regular expression like /\.(zip|rar|pdf)$/i";
            ApplicationFilterDescription = @"The following patterns can be used to specify files/folders.\n\n\nAll files/folders\n*;\\n\nZIP files / all folders / RAR files\nzip;\;rar\n\nRegular expression\n/\.jpe?g$/i";

            //Viewer = "Viewer";
            //ExternalViewer = "External viewer";
            //BuiltInViewer = "Built-in viewer";
            //DefaultViewer = "Default viewer";

            OK = "OK";
            Cancel = "Cancel";

            FileBroken = "File is broken.";
            NoReadableFile = "No supported image data.";
            BuiltInViewerProcessStopsResponding = "Built-in viewer stops responding.";

            //ToCopyAndPasteItemInCompressedFileKeepItOpenUntilPastingIsCompleted = "When copying and pasting some item(s) in a compressed file, it is necessary to keep the the compressed file open until pasting is completed.";

            DoYouDiscardChangedSettings = "Are you sure that you want to discard changed settings?";
            DoYouDiscardChangedName = "Are you sure you that want to discard changed name?";
            DoYouRenameThis = "Are you sure that you want to rename this file/folder?";
            DoYouRenameFollowing1Items = "Are your sure that you want to rename the following $1 items?"; // 10 個以上でないと問い合わせない仕様なので複数形決め打ちで良い
            //DoYouOverwriteFollowing1Items = "Are you sure that you want to overwrite the following $1 item(s)?";

            //DoYouSendSelectedItemsToRecycleBin = "Are you sure that you want to send selected item(s) to the Recycle Bin?";
            //DoYouDeleteSelectedItemsPermanently = "Are you sure that you want to delete selected item(s) permanently?";
            DoYouSendFollowing1ItemsToRecycleBin = "Are you sure that you want to send the following $1 item(s) to the Recycle Bin?";
            DoYouDeleteFollowing1ItemsPermanently = "Are you sure that you want to delete the following $1 item(s) permanently?";
            DoYouRemoveFollowing1ItemsFromVirtualFolder = "Are you sure that you want to remove the following $1 items from virtual folder?"; // 10 個以上でないと問い合わせない仕様なので複数形決め打ちで良い


            DoYouClearLocationHistory = "All history of locations will be cleared.";
            DoYouClearFilterHistory = "All history of filters will be cleared.";

            Question = "Question";
            Information = "Information";
            Warning = "Warning";
            Error = "Error";

            FailedToLoadFile = "Failed to load file.";

            RenameForRating = "The file/folder will be renamed for the rating.";
            RenameForTags = "The file/folder will be renamed for tags.";
            RenameForCover = "The file/folder will be renamed for cover setting.";
            WillClearCoverSetting = "Cover setting will be remove from file/folder name.";
            RenameForPageSequenceRightToLeft = "The file/folder will be renamed for current page sequence (right to left).";
            RenameForPageSequenceLeftToRight = "The file/folder will be renamed for current page sequence (left to right).";
            RenameForPageSequenceSinglePage = "The file/folder will be renamed for current page sequence (single page).";
            WillClearPageSequenceSetting = "Page sequence setting will be remove from file/folder name.";

            FollowingDllCouldNotBeLoaded = "The following dll could not be loaded.";
            ConfirmItIsNotFor1But2 = "Confirm it is not for $1 but $2.";

            //_1IsNotFound = "\"$1\" is not found.";
            _1CanNotBeOpend = "\"$1\" cannot be opened.";

            BeforeRename = "Before rename";
            AfterRename = "After rename";

            //DoneCoverSetting = "Cover settings was completed.";
            //DonePageSequenceSetting = "Page sequence settings was completed.";

            _1AlreadyExists = "$1 already exists";
            ItIsInvalidFileName = "It is invalid file name.";

            //NewVersionIsAvailableDoYouOpenDownloadPage = "New version of ZipPla is available! Do you want to open the download page?";
            ZipPlaWillCheckForUpdate = "ZipPla will check for update.";
            ShowThisDialogAgainNextTime = "Show this dialog again next time";
            NewVersionIsAvailable = "New version of ZipPla is available!";
            DownloadPage = "Download page";
            HowToDownload = "How to download";
            HowToDownloadURL = "https://sites.google.com/site/riostoolbox/zippla/english";
            ZipPlaFolder = "ZipPla folder";
            Close = "Close";
            NewVersionIsNotAvailable = "There is no new version available at this time.";
            YouAreUsingUnreleasedVersion = "You are using unreleased version.";

            History = "History";

            MiddleClick = "Middle-click";
            MiddleClickTwoFingerTap = "Middle-click / two-finger tap";
            RightClickPressAndTap = "Right-click / press and tap";

            TagSample1 = "TAG 1";
            TagSample2 = "TAG 2";
            TagSample3 = "TAG 3";

            RightClickDownFlickToOpenContextMenu = "Right-click/down-flick to open the context menu";
            LeftClick = "Click";

            IncreaseReferenceValue = "Increase reference value";
            DecreaseReferenceValue = "Decrease reference value";
            EqualTo1 = "Equal to $1";
            GreaterThanOrEqualTo1 = "Greater than or equal to $1";
            AndEqualTo1 = "And equal to $1";
            AndGreaterThanOrEqualTo1 = "And greater than or equal to $1";
            OrEqualTo1 = "Or equal to $1";
            OrGreaterThanOrEqualTo1 = "Or greater than or equal to $1";

            Include1 = "Include \"$1\"";
            Exclude1 = "Exclude \"$1\"";
            AndInclude1 = "And include \"$1\"";
            AndExclude1 = "And exclude \"$1\"";
            OrInclude1 = "Or include \"$1\"";
            OrExclude1 = "Or exclude \"$1\"";

            Tap = "Tap";
            RightFlick = "Right-flick";
            LeftFlick = "Left-flick";
            UpFlick = "Up-flick";
            RightUpFlick = "Right->up-flick";
            LeftUpFlick = "Left->up-flick";

            MoveHere = "MoveHere";
            CopyHere = "CopyHere";

            //IntegrityErrorBetweenTagsAndProfileColor = "There is an integrity error between tags and profile color. It will be solved by clicking any bookmark item.";

            FolderOpenDescription = "Drag and drop a file or folder here to display it,\\nor select a folder from \"Start > Open folder...\"";
            AddBookmarkDescription = @"← Right-click or down-flick on ""Add"" button\n　 to show extra menu.";

            //NowLoading = "Loading";

            NewFolderCanBeCreatedButSomeOfThemWillNotBeDisplayedBecauseOfViewSetting = "New folder can be created but some of them will not be displayed because of current \"Behavior\" setting.";
            ItemsCanBeMovedCopiedButSomeOfThemWillNotBeDisplayedBecauseOfViewSetting = "Item(s) can be copied/moved but some of them will not be displayed because of current \"Behavior\" setting.";
            ItemsCanBeMovedCopiedButSomeOfThemWillNotBeDisplayedBecauseOfItsAttribute = "Item(s) can be copied/moved but some of them will not be displayed because of its attribute.";
            //Hint = "Hint";
            //InTheFutureDoNotShowThisMessage = "In the future, do not show this message.";
            CheckExistenceOfFfmpegAtStartup = "Check the existence of ffmpeg.exe at startup";

            RecommendFfmpeg =
                @"""ffmpeg.exe"" is not found.\n" +
                @"If you save ""ffmpeg.exe"" in the same folder as ""ZipPla.exe"",\n" +
                @"ZipPla shows thumbnails of video files. (mp4, wmv, avi, mkv, flv, webm, etc.)\n" +
                @"\n" +
                @"\n" +
                @"""ffmpeg.exe"" can be downloaded from the following page.\n" +
                @"$1-bit / Static version is recommended for your PC.\n" +
                @"\n" +
                @"https://ffmpeg.zeranoe.com/builds/\n" +
                @"\n" +
                @"The downloaded archive includes ""bin"" folder and it contains ""ffmpeg.exe""."; /*\n" +
                @"\n" +
                @"\n" +
                @"Remark:\n" +
                @"  Your Explorer might display the names of these executable files\n" +
                @"  not as ""ffmpeg.exe""/""ZipPla.exe"" but as ""ffmpeg""/""ZipPla"".";*/

            RecommendFfmpegRemarkForHiddenExtension = @"Remark:\n" +
                @"  Your Explorer is set to display the names of these executable files\n" +
                @"  not as ""ffmpeg.exe""/""ZipPla.exe"" but as ""ffmpeg""/""ZipPla"".";

            RecommendNgen =
                @"Are you sure that you want to execute ""ngen.exe"" to speed up the startup of ZipPla?\n" +
                @"\n" +
                @"""ngen.exe"" runs in the background and it takes several seconds to several tens of seconds to complete.\n" +
                @"If ZipPla is not terminated at the completion of processing, the result will be notified.";
            SucceedInNgen = "Processing of \"ngen.exe\" is completed. Starting up ZipPla will be faster on next time.";
            FailInNgen = @"Processing of ""ngen.exe"" failed. Please cooperate with trouble report.\n\nError code : $1";
            DoYouRemoveNativeImage = "Would you like to disable the effect of ngen.exe?";

            ConfigSaveErrorMessage =
                @"A backup of the config file has been created because a failure occurred when saving the setting.\n" +
                @"To protect setting data, ZipPla startup is suppressed until a predetermined operation is performed.\n" +
                @"I would appreciate it if you could report the problem to the following email address.\n" +
                @"riostoolbox@gmail.com\n" +
                @"\n" +
                @"I apologize for this inconvenience.";

            _Yes = "&Yes";
            _No = "&No";
        }


        // UI
        public static string _Start { get; private set; }
        public static string OpenHistory { get; private set; }
        public static string OpenFolder { get; private set; }
        //public static string OpenVirtualFolder { get; private set; }
        public static string OpenFile { get; private set; }
        public static string AllSupportedFiles { get; private set; }
        public static string VideoFilesRequireFfmpeg { get; private set; }
        public static string NewVirtualFolder { get; private set; }
        public static string NewSmartFolder { get; private set; }
        public static string OpenBuiltInViewer { get; private set; }
        //public static string ReloadAllItems { get; private set; }
        public static string Reload { get; private set; }
        public static string NewFolder { get; private set; }
        public static string EditAddressBar { get; private set; }
        public static string EditFilter { get; private set; }
        public static string Exit { get; private set; }
        public static string _View { get; private set; }
        public static string _Behavior { get; private set; }
        public static string Bookmark { get; private set; }
        public static string FolderTree { get; private set; }
        public static string Thumbnails { get; private set; }
        public static string Details { get; private set; }
        public static string Layout { get; private set; }
        public static string VerticalLayout { get; private set; }
        public static string AlternativeVerticalLayout { get; private set; }
        public static string HorizontalLayout { get; private set; }
        public static string AlternativeHorizontalLayout { get; private set; }
        public static string BuiltInViewerSettings { get; private set; }
        public static string ForceFirstPageToBeSingle { get; private set; }
        public static string OpenInPreviousFullscreenMode { get; private set; }
        public static string OpenInFullscreen { get; private set; }
        public static string OpenInWindow { get; private set; }
        public static string OpenInPreviousRotationToneCurveSetting { get; private set; }
        public static string SortFilesWithSameOrderAsThumbnailWindow { get; private set; }
        public static string AllowReadAheadProcess { get; private set; }
        public static string CreateShortcutFromCurrentSettings { get; private set; }
        public static string BlackBackground { get; private set; }
        public static string GrayBackground { get; private set; }
        public static string WhiteBackground { get; private set; }
        public static string SelectBackgroundColor { get; private set; }
        public static string MaxCountCaptchaInAVideo { get; private set; }
        public static string FitOnScreen { get; private set; }
        public static string EverySecond { get; private set; }
        //public static string ThumbnailList { get; private set; }
        //public static string FileList { get; private set; }
        //public static string PageInFileList { get; private set; }
        //public static string RatingInFileList { get; private set; }
        //public static string DateAccessedInFileList { get; private set; }
        //public static string DateModifiedInFileList { get; private set; }
        public static string ArchiveFilesPdfFiles { get; private set; }
        public static string ImageFiles { get; private set; }
        public static string VideoFiles { get; private set; }
        public static string OtherFiles { get; private set; }
        //public static string AtLeastOneKindOfItemsMustBeSelected { get; private set; }
        public static string Folders { get; private set; }
        //public static string IgnoreFolders { get; private set; }
        public static string SearchSubfoldersAsWell { get; private set; }
        public static string _Thumbnail { get; private set; }
        //public static string ReloadAllThumbnails { get; private set; }
        public static string Tiny { get; private set; }
        public static string Small { get; private set; }
        public static string Normal { get; private set; }
        public static string Large { get; private set; }
        public static string Huge { get; private set; }
        public static string AllRatios { get; private set; }
        public static string Letterbox { get; private set; }
        public static string PanAndScan { get; private set; }
        public static string SmartClip { get; private set; }
        public static string ShowIcon { get; private set; }
        public static string ShowRating { get; private set; }
        public static string ShowTags { get; private set; }

        public static string NumberOfLinesOfName { get; private set; }
        //public static string FromTheLongestName { get; private set; }
        public static string Full { get; private set; }
        //public static string FromThumbnailWidthLess { get; private set; }
        //public static string FromThumbnailWidthGreater { get; private set; }
        public static string AutomaticLess { get; private set; }
        public static string AutomaticGreater { get; private set; }
        public static string MinimumFrameThickness { get; private set; }
        //public static string ZeroThickness { get; private set; }
        public static string VeryThin { get; private set; }
        public static string Thin { get; private set; }
        public static string NormalThickness { get; private set; }
        public static string Thick { get; private set; }
        public static string VeryThick { get; private set; }
        public static string Aligning { get; private set; }
        public static string TotallyUniformed { get; private set; }
        public static string HorizontallyUniformed { get; private set; }
        public static string AlignedLeft { get; private set; }
        public static string AlignedOnCenter { get; private set; }
        public static string MouseWheelScrollAmount { get; private set; }
        public static string OneLine { get; private set; }
        public static string _1Lines { get; private set; }
        public static string ThumbnailCache { get; private set; }
        public static string ThumbnailCacheFirstDescription { get; private set; }
        public static string ThumbnailCacheFirstDescriptionForFirstLaunch { get; private set; }
        public static string StoreCachesInAlternateDataStreamRecommended { get; private set; }
        public static string ThumbnailCacheAdsDescription { get; private set; }
        public static string StoreCachesInSpecifiedFolder { get; private set; }
        public static string ThumbnailCacheSpecifiedFolderDescription { get; private set; }
        public static string LocationOfZipPlaExe { get; private set; }
        public static string LocationOfOriginalFile { get; private set; }
        public static string Browse { get; private set; }
        public static string NotUseThumbnailCache { get; private set; }
        public static string AdsNotSupportedWarningMessage { get; private set; }
        //public static string CreateThumbnailCache { get; private set; }
        //public static string AboutThumbnailCache { get; private set; }
        //public static string ThumbnailCacheDescription { get; private set; }
        //public static string ShowFilename { get; private set; }
        public static string ShowName { get; private set; }
        public static string _Others { get; private set; }
        public static string _ChangeCurrentProfileColor { get; private set; }
        public static string Edit_Tags { get; private set; }
        //public static string _KeysMouseButtons { get; private set; }
        public static string _BasicOperationSettings { get; private set; }
        //public static string _MouseGestures { get; private set; }
        public static string _MouseTouchGestures { get; private set; }
        public static string Common_Settings { get; private set; }


        public static string ScreenEffectAtRightSide { get; private set; }
        public static string ScreenEffectAtLeftSide { get; private set; }
        public static string ScreenEffectAtNextPageSide { get; private set; }
        public static string ScreenEffectAtPreviousPageSide { get; private set; }

        public static string StartMenu { get; private set; }
        public static string MoveMenu { get; private set; }
        public static string ViewMenu { get; private set; }
        public static string SlideshowMenu { get; private set; }
        public static string VirtualFolderMenu { get; private set; }
        public static string EditMenu { get; private set; }
        public static string OthersMenu { get; private set; }

        public static string NoOperation { get; private set; }

        //public static string GoBackAltLeft { get; private set; }
        //public static string GoForwardAltRight { get; private set; }
        public static string RightClickDownFlickToShowHistory { get; private set; }

        public static string ClearMenuForAddressBarHistory { get; private set; }
        public static string DeleteCurrentPathFromHistory { get; private set; }
        public static string ClearAllHistoryOfLocations { get; private set; }

        public static string TypeInAsc { get; private set; }
        public static string TypeInDesc { get; private set; }
        public static string NameInAsc { get; private set; }
        public static string NameInDesc { get; private set; }
        public static string RatingInAsc { get; private set; }
        public static string RatingInDesc { get; private set; }
        public static string CreatedInAsc { get; private set; }
        public static string CreatedInDesc { get; private set; }
        public static string AccessedInAsc { get; private set; }
        public static string AccessedInDesc { get; private set; }
        public static string ModifiedInAsc { get; private set; }
        public static string ModifiedInDesc { get; private set; }
        public static string SizeInAsc { get; private set; }
        public static string SizeInDesc { get; private set; }
        public static string Random { get; private set; }

        public static string MiddleClickToClearPaste { get; private set; }
        public static string LeftFlickToClear { get; private set; }
        public static string RightFlickToPaste { get; private set; }
        public static string MiddleClickLeftFlickToResetRating { get; private set; }

        public static string EditMenuForFilter { get; private set; }
        public static string DeleteCurrentFilterFromDropdown { get; private set; }
        public static string ClearAllDropdownItems { get; private set; }
        public static string EditDropdownOfFilters { get; private set; }
        
        public static string _Add { get; private set; }
        //public static string AddWithCurrentProfileColor { get; private set; }
        public static string AddWithCurrentProfileColor { get; private set; }
        public static string BookmarkWithCurrentProfileColor { get; private set; }
        //public static string AddWithNewProfileColor { get; private set; }
        public static string AddWithNewProfileColor { get; private set; }
        public static string BookmarkWithNewProfileColor { get; private set; }
        public static string OnlyCurrentProfileColor { get; private set; }
        public static string OnlyNewProfileColor { get; private set; }
        public static string Separator { get; private set; }
        public static string AddSeparator { get; private set; }

        public static string SelectedColorIsUsedByFollowingBookmarks { get; private set; }
        public static string HowToAddUsedColorDescription { get; private set; }
        //public static string DoYouUseColorWhichIsCloseToOtherColors { get; private set; }

        public static string ChangeCurrentProfileColor { get; private set; }
        public static string WillChangeCurrentProfileColor { get; private set; }
        public static string ThisChangeAlsoAffectsFollowingBookmarks { get; private set; }
        public static string HowToCreateNewColorDescription { get; private set; }

        public static string Delete { get; private set; }
        public static string DoYouDeleteFollowing1Items { get; private set; }
        public static string EditAlias { get; private set; }
        public static string ProfileColor { get; private set; }
        public static string ProfileColorOf1 { get; private set; }
        public static string RealName { get; private set; }
        public static string LoadOnlyProfileColor { get; private set; }
        public static string LoadProfileColor { get; private set; }
        //public static string SetToCurrentLocation { get; private set; }
        //public static string SetToCurrentLocationAndState { get; private set; }

        public static string FilterWithSelectedText { get; private set; }
        public static string FilterWith1 { get; private set; }
        public static string CopySelectedText { get; private set; }
        public static string Copy1 { get; private set; }
        public static string SearchSelectedTextOnGoogle { get; private set; }
        public static string Search1OnGoogle { get; private set; }
        public static string NewCustomCommand { get; private set; }
        public static string OpenInBuiltInViewer { get; private set; }
        public static string OpenIn1 { get; private set; }
        public static string Open { get; private set; }
        //public static string MoveFilesFolders { get; private set; }
        public static string MoveExtractSelectedItems { get; private set; }
        public static string MoveExtractSelectedItemsTo1 { get; private set; }
        //public static string MoveSelectedItemsTo1 { get; private set; }
        //public static string MoveThisTo1 { get; private set; }
        //public static string MoveTheseTo1 { get; private set; }
        public static string AssociatedApplication { get; private set; }
        //public static string MoveToThisFolder { get; private set; }
        public static string OpenInCurrentWindow { get; private set; }
        //public static string OpenHere { get; private set; }
        //public static string MoveToLocationOfThis { get; private set; }
        public static string SubdivideAroundHere { get; private set; }
        //public static string MoveLocation { get; private set; }
        public static string CurrentZipPla { get; private set; }
        public static string ExecutableInClipboard { get; private set; }
        public static string OpenInExplorer { get; private set; }
        public static string CopyAddressAsText { get; private set; }
        public static string ApplicationIsNotSpecified { get; private set; }
        public static string Rating { get; private set; }
        public static string None { get; private set; }
        //public static string MiddleClickOnStars { get; private set; }
        public static string Tag { get; private set; }
        public static string RightClickNotToClose { get; private set; }
        public static string RightFlickIsAlsoOk { get; private set; }
        public static string SameAsAbove { get; private set; }
        public static string UncheckAll { get; private set; }
        //public static string AddTagsFromFileName { get; private set; }
        public static string AddTagsFromName { get; private set; }
        public static string VirtualFolder { get; private set; }
        public static string AddToNewVirtualFolder { get; private set; }
        public static string RemoveFromHistory { get; private set; }
        //public static string AddToExistingVirtualFolder { get; private set; }
        public static string AddTo1 { get; private set; }
        public static string NoVirtualFoldersInBookmark { get; private set; }
        //public static string ReloadSelectedThumbnail { get; private set; }
        public static string Cover { get; private set; }
        public static string SetCoverManually { get; private set; }
        public static string SetCoverToThis { get; private set; }
        public static string SetCoverOfParentFolderToThis { get; private set; }
        public static string AbortSettingCover { get; private set; }
        public static string RightClick { get; private set; }
        public static string ClearCoverSetting { get; private set; }
        public static string PageSequence { get; private set; }
        public static string Default { get; private set; }
        public static string LeftToRight { get; private set; }
        public static string RightToLeft { get; private set; }
        public static string SinglePage { get; private set; }
        public static string SinglePageWithoutScalingUp { get; private set; }
        //public static string ForcePageSpread { get; private set; }
        public static string PrioritizePageSpread { get; private set; }
        public static string AllowPageDivision { get; private set; }
        public static string ClearPageSequenceSetting { get; private set; }
        public static string Cut { get; private set; }
        public static string Copy { get; private set; }
        public static string CopyCapturedImage { get; private set; }
        public static string Paste { get; private set; }
        public static string Rename { get; private set; }
        public static string RemoveFromVirtualFolder { get; private set; }
        public static string Properties { get; private set; }

        public static string TogglePageSequence { get; private set; }

        public static string OrientSeekBarDirectionToPageSequence { get; private set; }

        public static string ShowHintWhenChangingSettings { get; private set; }
        public static string Hint { get; private set; }
        public static string BuiltInViewerSavingSettingsBehaviorDescription { get; private set; }
        public static string ShowThisHintAgainNextTime { get; private set; }

        //public static string _View { get; private set; }
        //public static string _Image { get; private set; }
        public static string _Move { get; private set; }
        public static string S_lideshow { get; private set; }
        public static string Virtual_Folder { get; private set; }
        public static string _Edit { get; private set; }

        public static string OpenThumbnailWindow { get; private set; }
        public static string SelectCurrentImageInThumbnailWindow { get; private set; }
        public static string SwitchToThumbnailWindow { get; private set; }
        public static string CloneWindow { get; private set; }
        public static string Fullscreen { get; private set; }
        public static string Window { get; private set; }
        public static string ToggleFullscreenMode { get; private set; }

        public static string AlwaysAutomaticallyHideUI { get; private set; }

        public static string ToggleMagnifier { get; private set; }
        public static string ToggleMagnifierWithoutOperatingGuide { get; private set; }
        public static string ToggleMagnifierWithMinimalScreenEffects { get; private set; }
        public static string EnableMagnifier { get; private set; }
        public static string EnableMagnifierWithoutOperatingGuide { get; private set; }
        public static string DisableMagnifier { get; private set; }
        public static string DisableMagnifierWithoutOperatingGuide { get; private set; }
        public static string DisableMagnifierWithoutScreenEffects { get; private set; }
        public static string MagnifierOperatingGuide { get; private set; }

        //public static string MagnifierMiddleButtonZ { get; private set; }
        //public static string RightToLeftL { get; private set; }
        //public static string LeftToRightR { get; private set; }

        //public static string ForwardLeftButtonWheelDownCursorRight { get; private set; }
        //public static string BackWheelUpCursorLeft { get; private set; }

        //public static string ForwardLeftButtonOnLeftHalfInRightToLeftWheelDownCursorRight { get; private set; }
        //public static string ForwardLeftButtonOnRightHalfInLeftToRightWheelDownCursorRight { get; private set; }
        //public static string BackLeftButtonOnRightHalfInRightToLeftWheelUpCursorLeft { get; private set; }
        //public static string BackLeftButtonOnLeftHalfInLeftToRightWheelUpCursorLeft { get; private set; }

        //public static string ForwardOneWheelDownOnBarCursorDown { get; private set; }
        //public static string BackOneWheelDownOnBarCursorDown { get; private set; }
        //public static string Ratio { get; private set; }

        //public static string DefaultClickBehavior { get; private set; }
        public static string NextPage { get; private set; }
        public static string PreviousPage { get; private set; }
        public static string WheelDown { get; private set; }
        //public static string CursorRight { get; private set; }
        public static string WheelUp { get; private set; }

        public static string Specials { get; private set; }

        //public static string CursorLeft { get; private set; }
        public static string LeftClickOnLeftSide { get; private set; }
        public static string LeftClickOnRightSide { get; private set; }
        public static string LeftClickOnTopSide { get; private set; }
        public static string LeftClickOnBottomSide { get; private set; }
        public static string LeftClickOnCenter { get; private set; }
        public static string LeftClickOnTopLeft { get; private set; }
        public static string LeftClickOnTopRight { get; private set; }
        public static string LeftClickOnBottomLeft { get; private set; }
        public static string LeftClickOnBottomRight { get; private set; }

        public static string DoubleLeftClick { get; private set; }

        public static string NextPageAtLastPage { get; private set; }
        public static string PreviousPageAtFirstPage { get; private set; }

        public static string MoveForwardOnePage { get; private set; }
        public static string MoveBackOnePage { get; private set; }
        public static string WheelDownOnBar { get; private set; }
        public static string WheelUpOnBar { get; private set; }
        //public static string CursorDown { get; private set; }
        //public static string CursorUp { get; private set; }

        public static string RightPage { get; private set; }
        public static string LeftPage { get; private set; }
        public static string MoveRightOnePage { get; private set; }
        public static string MoveLeftOnePage { get; private set; }
        public static string PositionRatio { get; private set; }
        public static string OpenNext { get; private set; }
        public static string X2Button { get; private set; }
        public static string OpenPrevious { get; private set; }
        public static string X1Button { get; private set; }
        public static string OpenRight { get; private set; }
        public static string OpenLeft { get; private set; }

        //public static string MiddleClickOnPosteriorHalfOfBar { get; private set; }
        //public static string MiddleClickOnAnteriorHalfOfBar { get; private set; }
        public static string AutoForwardScroll { get; private set; }
        public static string AutoBackScroll { get; private set; }
        public static string AutoForwardScrollWithoutOverwrap { get; private set; }
        public static string AutoBackScrollWithoutOverwrap { get; private set; }
        public static string NextPageOrAutoForwardScroll { get; private set; }
        public static string PreviousPageOrAutoBackScroll { get; private set; }
        public static string NextPageOrAutoForwardScrollWithoutOverwrap { get; private set; }
        public static string PreviousPageOrAutoBackScrollWithoutOverwrap { get; private set; }
        
        public static string AutoRightScroll { get; private set; }
        public static string AutoLeftScroll { get; private set; }
        public static string AutoRightScrollWithoutOverwrap { get; private set; }
        public static string AutoLeftScrollWithoutOverwrap { get; private set; }
        public static string RightPageOrAutoRightScroll { get; private set; }
        public static string LeftPageOrAutoLeftScroll { get; private set; }
        public static string RightPageOrAutoRightScrollWithoutOverwrap { get; private set; }
        public static string LeftPageOrAutoLeftScrollWithoutOverwrap { get; private set; }

        public static string BookFolderIsNotFound { get; private set; }


        public static string Slideshow { get; private set; }
        public static string StartSlideshow { get; private set; }
        public static string StopSlideshow { get; private set; }
        public static string ToggleSlideshow { get; private set; }
        public static string SpaceKey { get; private set; }

        public static string OneSecondIntervals { get; private set; }
        public static string _1SecondsIntervals { get; private set; }
        public static string Repeat { get; private set; }
        public static string AllowToOpenNextItem { get; private set; }

        public static string RotateLeft { get; private set; }
        public static string RotateRight { get; private set; }
        public static string CancelRotationSetting { get; private set; }
        //public static string Binarization { get; private set; }
        //public static string AutoContrastControl { get; private set; }

        //public static string RemoveMoire { get; private set; }

        public static string ToneCurve { get; private set; }
        public static string ToneCurveSepiaTone { get; private set; }
        public static string ToneCurveUserDefined { get; private set; }


        public static string ScalingAlgorithmNormal { get; private set; }
        public static string ScalingAlgorithmMagnifier { get; private set; }
        public static string DefaultHighQuality { get; private set; }
        public static string DefaultHighSpeed { get; private set; }
        public static string NearestNeighbor { get; private set; }
        public static string AreaAverage { get; private set; }
#if !AUTOBUILD
        //public static string AntiMoire { get; private set; }
#endif
        public static string LinearizeColorSpace { get; private set; }
        public static string LinearizeColorSpaceDescription { get; private set; }
        public static string UseAreaAverageWhenUpsizing { get; private set; }

        public static string SaveCurrentPageSequence { get; private set; }

        public static string _1Items { get; private set; }
        public static string _1ItemsAreSelected { get; private set; }
        public static string OnePage { get; private set; }
        public static string _1Pages { get; private set; }
        
        public static string PropertiesOf1 { get; private set; }
        public static string Location { get; private set; }
        public static string Icon { get; private set; }
        //public static string FileName { get; private set; }
        public static string Name { get; private set; }
        public static string FileType { get; private set; }
        public static string Length { get; private set; }
        public static string Size { get; private set; }
        public static string DateCreated { get; private set; }
        public static string DateAccessed { get; private set; }
        public static string DateModified { get; private set; }
        public static string Resolution { get; private set; }
        public static string AspectRatio { get; private set; }
        public static string Width { get; private set; }
        public static string Height { get; private set; }
        public static string FPS { get; private set; }
        public static string NumberOfPages { get; private set; }
        public static string ImageSize { get; private set; }
        public static string VideoInfo { get; private set; }
        public static string Position { get; private set; }
        public static string Width1Height2BitsPerPixel3 { get; private set; }

        public static string Page { get; private set; }

        //public static string File { get; private set; }
        //public static string Folder { get; private set; }
        public static string Bytes { get; private set; }

        public static string EditTags { get; private set; }
        public static string TagName { get; private set; }
        public static string NewTag1 { get; private set; }
        public static string ProfileColorUnderEditing { get; private set; }
        public static string Add { get; private set; }
        //public static string Delete { get; private set; }
        public static string Color { get; private set; }
        public static string TagMustBeNoEmptyStringWhichDoesNotIncludeFollowingCharacters { get; private set; }
        public static string TagsCanNotShareSameName { get; private set; }


        public static string FilteringString { get; private set; }
        public static string Alias { get; private set; }
        public static string EditFilters { get; private set; }
        public static string InvariantDropdownList { get; private set; }
        public static string NewFilteringString1 { get; private set; }
        public static string Presets { get; private set; }
        public static string FilterSampleDescription1 { get; private set; }
        public static string FilterSampleString1 { get; private set; }
        public static string FilterSampleAlias1 { get; private set; }
        public static string FilterSampleDescription2 { get; private set; }
        public static string FilterSampleString2 { get; private set; }
        public static string FilterSampleAlias2 { get; private set; }
        public static string FilterSampleDescription3 { get; private set; }
        public static string FilterSampleString3 { get; private set; }
        public static string FilterSampleAlias3 { get; private set; }
        public static string FilterSampleDescription4 { get; private set; }
        public static string FilterSampleString4 { get; private set; }
        public static string FilterSampleAlias4 { get; private set; }
        public static string FilterSampleDescription5 { get; private set; }
        public static string FilterSampleString5 { get; private set; }
        public static string FilterSampleAlias5 { get; private set; }
        public static string FilterSampleDescription6 { get; private set; }
        public static string FilterSampleString6 { get; private set; }
        public static string FilterSampleAlias6 { get; private set; }
        public static string FilterSampleDescription7 { get; private set; }
        public static string FilterSampleString7 { get; private set; }
        public static string FilterSampleAlias7 { get; private set; }
        public static string FilterSampleDescription8 { get; private set; }
        public static string FilterSampleString8 { get; private set; }
        public static string FilterSampleAlias8 { get; private set; }
        public static string FilterSampleDescription9 { get; private set; }
        public static string FilterSampleString9 { get; private set; }
        public static string FilterSampleAlias9 { get; private set; }
        public static string AliasMustBeNoEmptyStringWhichDoesNotIncludeSpaceAndFollowingCharacters { get; private set; }
        public static string DisplayNameCanNotShareSameName { get; private set; }
        public static string FilteringStringMustNotBeEmpty { get; private set; }

        //public static string MouseGestures { get; private set; }
        public static string MouseTouchGestures { get; private set; }
        public static string Enabled { get; private set; }
        public static string GestureToAddCommand { get; private set; }
        public static string Appearance { get; private set; }
        public static string LineWidth { get; private set; }
        public static string Gesture { get; private set; }
        public static string Command { get; private set; }
        public static string InThumbnailWindowTouchGestureMustStartWithHorizontalDirection { get; private set; }


        //public static string KeysMouseButtons { get; private set; }
        public static string BasicOperationSettings { get; private set; }
        public static string Inputs { get; private set; }
        public static string Abort { get; private set; }
        public static string InputKeysMouseButtons { get; private set; }
        public static string UnassignedCommandsAreComplemented { get; private set; }
        public static string ThisOperationCanBeAvoidedByAsigningNoOperationToTargetKeys { get; private set; }
        //public static string IfYouWantToRestoreDefaultLeftClickBehaviorDeleteAllInputsIncludingLeftClick { get; private set; }
        public static string WhenClickOnAndClickConflictTheFormerTakesPrecedence { get; private set; }
        public static string DoYouRestoreDefaultSettingOfKeysMouseButtons { get; private set; }

        public static string GoBack { get; private set; }
        public static string GoForward { get; private set; }
        public static string GoUp { get; private set; }
        public static string ResetFilter { get; private set; }
        public static string ScrollToTop { get; private set; }
        public static string ScrollToBottom { get; private set; }
        public static string ScrollToSelection { get; private set; }

        public static string SmartFolder { get; private set; }
        public static string EditSmartFolder { get; private set; }
        public static string Essential { get; private set; }
        public static string FolderPath { get; private set; }
        public static string Filter { get; private set; }
        public static string CheckAll { get; private set; }
        //public static string UncheckAll { get; private set; }
        public static string Save { get; private set; }
        public static string Overwrite { get; private set; }
        public static string Editing1 { get; private set; }
        //public static string NonEssentialFolderIsIgnoredIfItDoesNotExist { get; private set; }
        public static string IfEssentialFolderDoesNotExistErrorWillBeDisplayed { get; private set; }
        public static string HintRightClickNameOfKdkFileOnAddressBarToEditItAgain { get; private set; }

        public static string SelectInFolderTree { get; private set; }

        public static string CommonSettings { get; private set; }
        public static string LanguageLanguage { get; private set; }
        public static string Edit { get; private set; }
        public static string OpenLanguageFolder { get; private set; }
        public static string UpdateAndEditLanguageFile { get; private set; }
        public static string TinyOnScreenKeyboard { get; private set; }
        public static string HighlightSelectionWhenMouseLeaves { get; private set; }
        public static string ClearFilterWhenMovingLocation { get; private set; }
        public static string LoadLastPageViewed { get; private set; }
        public static string HowToOpenFile { get; private set; }
        public static string ClickSamePointTwiceQuickly { get; private set; }
        public static string ClickSameItemTwiceQuickly { get; private set; }
        public static string ClickSelectedItem { get; private set; }
        public static string ContextMenu { get; private set; }
        public static string Standard { get; private set; }
        public static string OnlyTags { get; private set; }
        //public static string Explorer { get; private set; }

        public static string FolderViewSettings { get; private set; }
        public static string DisplayMixingWithFiles { get; private set; }
        public static string DisplayFirst { get; private set; }
        public static string DisplayLast { get; private set; }
        public static string DisplayRespectingToSortOfDirection { get; private set; }
        //public static string ShowFolderSizeOnStatusBar { get; private set; }


        /*
    public static string ArrangementOfFolders { get; private set; }
    public static string MixWithFiles { get; private set; }
    public static string DisplayFirst { get; private set; }
    public static string DisplayLast { get; private set; }
    public static string FitToDirectionOfSort { get; private set; }
    */

        public static string AutomaticSelectionSwitching { get; private set; }
        public static string InAppropriateCases { get; private set; }

        public static string FolderTreeSynchronizing { get; private set; }
        public static string FolderTreeToAddresBar { get; private set; }
        public static string AddresBarToFolderTree { get; private set; }
        public static string TwoWay { get; private set; }
        public static string CollapseOthersWhenSynchronizing { get; private set; }

        public static string LoadWholeArchiveIntoMemory { get; private set; }
        public static string NoneRecommended { get; private set; }
        public static string ExceptReadAheadProcess { get; private set; }
        public static string Always { get; private set; }
        public static string LoadArchivesInArchive { get; private set; }
        //public static string OnlyIfThereIsNoOtherImage { get; private set; }
        public static string TwoLevelUntilFound { get; private set; }
        public static string OneLevelCompletelyNotRecommended { get; private set; }
        public static string DefaultPageSequence { get; private set; }
        public static string MaximumNumberOfHistories { get; private set; }
        public static string BuiltInViewerMemoryUsage { get; private set; }
        public static string Minimum { get; private set; }
        public static string Around1 { get; private set; }
        public static string Automatic { get; private set; }
        public static string BuiltInViewerMemoryUsageDescription { get; private set; }
        public static string SusiePlugins { get; private set; }
        public static string AllowUntestedPlugins { get; private set; }
        public static string SearchNotOnlyZipPlaFolderButAlsoSusieInstallationFolder { get; private set; }
        //public static string HintMessage { get; private set; }
        public static string ZipPlaUpdateCheck { get; private set; }
        public static string CheckNow { get; private set; }
        public static string Checking { get; private set; }
        //public static string AskPermissionBeforeInternetConnection { get; private set; }
        public static string ShowDialogBeforeConnection { get; private set; }
        public static string OtherChecksOnStartup { get; private set; }
        //public static string DoNotShowAgain { get; private set; }
        public static string DragAndDrop { get; private set; }
        //public static string ShowThumbnails { get; private set; }
        //public static string OpenInZipPla { get; private set; }
        public static string BrowseInZipPla { get; private set; }
        public static string MoveOrCopyFilesAddItemToVirtualFolder { get; private set; }
        public static string DynamicStringSelection { get; private set; }
        public static string DisplayName { get; private set; }
        public static string DisplayNameForNonSelection { get; private set; }
        //public static string Command { get; private set; }
        public static string Applications { get; private set; }
        //public static string Name { get; private set; }
        public static string Params { get; private set; }
        public static string Multi { get; private set; }
        public static string OpenOnClick { get; private set; }
        public static string ShowInContextMenu { get; private set; }
        public static string RenameItemByEditingTextBox { get; private set; }
        public static string AllFiles { get; private set; }
        //public static string Default { get; private set; }
        //public static string StartWithAccessKeyContainedInName { get; private set; }
        //public static string EnableAccessKeysInAlsoBookmark { get; private set; }
        public static string AboutAccessKey { get; private set; }
        public static string ApplicationAccessKeyUsageDescription { get; private set; }
        public static string BookmarkAccessKeyUsageDescription { get; private set; }
        public static string DoYouRestoreDefaultSettingOfDynamicStringSelection { get; private set; }
        public static string IfUrlIsRegisteredZipPlaAccessesInternet { get; private set; }
        public static string DoYouRestoreDefaultSettingsOfApplications { get; private set; }
        //public static string AssociatedApplication { get; private set; }
        public static string ExternalApplication { get; private set; }
        public static string Explorer { get; private set; }
        //public static string MoveLocation { get; private set; }
        public static string BuiltInViewer { get; private set; }
        //public static string TargetItemsAreSpecifiedByRegularExpression { get; private set; }
        public static string ApplicationFilterDescription { get; private set; }


        //public static string Viewer { get; private set; }
        //public static string ExternalViewer { get; private set; }
        //public static string BuiltInViewer { get; private set; }
        //public static string DefaultViewer { get; private set; }

        public static string OK { get; private set; }
        public static string Cancel { get; private set; }

        public static string FileBroken { get; private set; }
        public static string NoReadableFile { get; private set; }
        public static string BuiltInViewerProcessStopsResponding { get; private set; }

        //public static string ToCopyAndPasteItemInCompressedFileKeepItOpenUntilPastingIsCompleted { get; private set; }

        public static string DoYouDiscardChangedSettings { get; private set; }
        
        public static string DoYouDiscardChangedName { get; private set; }
        
        public static string DoYouRenameThis { get; private set; }

        public static string DoYouRenameFollowing1Items { get; private set; }
        //public static string DoYouOverwriteFollowing1Items { get; private set; }

        //public static string DoYouSendSelectedItemsToRecycleBin { get; private set; }
        //public static string DoYouDeleteSelectedItemsPermanently { get; private set; }
        public static string DoYouSendFollowing1ItemsToRecycleBin { get; private set; }
        public static string DoYouDeleteFollowing1ItemsPermanently { get; private set; }
        public static string DoYouRemoveFollowing1ItemsFromVirtualFolder { get; private set; }

        public static string DoYouClearLocationHistory { get; private set; }

        public static string DoYouClearFilterHistory { get; private set; }

        public static string Question { get; private set; }
        public static string Information { get; private set; }
        public static string Warning { get; private set; }
        public static string Error { get; private set; }

        public static string FailedToLoadFile { get; private set; }

        public static string RenameForRating { get; private set; }
        public static string RenameForTags { get; private set; }
        public static string RenameForCover { get; private set; }
        public static string WillClearCoverSetting { get; private set; }
        public static string RenameForPageSequenceRightToLeft { get; private set; }
        public static string RenameForPageSequenceLeftToRight { get; private set; }
        public static string RenameForPageSequenceSinglePage { get; private set; }
        public static string WillClearPageSequenceSetting { get; private set; }


        public static string FollowingDllCouldNotBeLoaded { get; private set; }
        public static string ConfirmItIsNotFor1But2 { get; private set; }

        //public static string _1IsNotFound { get; private set; }
        public static string _1CanNotBeOpend { get; private set; }

        public static string BeforeRename { get; private set; }
        public static string AfterRename { get; private set; }

        //public static string DoneCoverSetting { get; private set; }
        //public static string DonePageSequenceSetting { get; private set; }

        public static string _1AlreadyExists { get; private set; }
        public static string ItIsInvalidFileName { get; private set; }

        //public static string NewVersionIsAvailableDoYouOpenDownloadPage { get; private set; }
        public static string ZipPlaWillCheckForUpdate { get; private set; }
        public static string ShowThisDialogAgainNextTime { get; private set; }
        public static string NewVersionIsAvailable { get; private set; }
        public static string DownloadPage { get; private set; }
        public static string HowToDownload { get; private set; }
        public static string HowToDownloadURL { get; private set; }
        public static string ZipPlaFolder { get; private set; }
        public static string Close { get; private set; }
        public static string NewVersionIsNotAvailable { get; private set; }
        public static string YouAreUsingUnreleasedVersion { get; private set; }

        public static string History { get; private set; }

        public static string MiddleClick { get; private set; }
        public static string MiddleClickTwoFingerTap { get; private set; }
        public static string RightClickPressAndTap { get; private set; }

        public static string TagSample1 { get; private set; }
        public static string TagSample2 { get; private set; }
        public static string TagSample3 { get; private set; }

        public static string RightClickDownFlickToOpenContextMenu { get; private set; }
        public static string LeftClick { get; private set; }

        public static string IncreaseReferenceValue { get; private set; }
        public static string DecreaseReferenceValue { get; private set; }
        public static string EqualTo1 { get; private set; }
        public static string GreaterThanOrEqualTo1 { get; private set; }
        public static string AndEqualTo1 { get; private set; }
        public static string AndGreaterThanOrEqualTo1 { get; private set; }
        public static string OrEqualTo1 { get; private set; }
        public static string OrGreaterThanOrEqualTo1 { get; private set; }

        public static string Include1 { get; private set; }
        public static string Exclude1 { get; private set; }
        public static string AndInclude1 { get; private set; }
        public static string AndExclude1 { get; private set; }
        public static string OrInclude1 { get; private set; }
        public static string OrExclude1 { get; private set; }

        public static string Tap { get; private set; }
        public static string RightFlick { get; private set; }
        public static string LeftFlick { get; private set; }
        public static string UpFlick { get; private set; }
        public static string RightUpFlick { get; private set; }
        public static string LeftUpFlick { get; private set; }


        public static string MoveHere { get; private set; }
        public static string CopyHere { get; private set; }

        //public static string NowLoading { get; private set; }

        public static string NewFolderCanBeCreatedButSomeOfThemWillNotBeDisplayedBecauseOfViewSetting { get; private set; }

        public static string ItemsCanBeMovedCopiedButSomeOfThemWillNotBeDisplayedBecauseOfViewSetting { get; private set; }

        public static string ItemsCanBeMovedCopiedButSomeOfThemWillNotBeDisplayedBecauseOfItsAttribute { get; private set; }

        //public static string IntegrityErrorBetweenTagsAndProfileColor { get; private set; }

        public static string FolderOpenDescription { get; private set; }
        public static string AddBookmarkDescription { get; private set; }

        //public static string Hint { get; private set; }
        //public static string InTheFutureDoNotShowThisMessage { get; private set; }
        public static string CheckExistenceOfFfmpegAtStartup { get; private set; }

        public static string RecommendFfmpeg { get; private set; }
        public static string RecommendFfmpegRemarkForHiddenExtension { get; private set; }
        
        public static string RecommendNgen { get; private set; }
        public static string SucceedInNgen { get; private set; }
        public static string FailInNgen { get; private set; }
        public static string DoYouRemoveNativeImage { get; private set; }

        public static string ConfigSaveErrorMessage { get; private set; }
        
        public static string _Yes { get; private set; }
        public static string _No { get; private set; }
    }
}