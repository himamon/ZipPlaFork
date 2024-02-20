using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Common;
using System.Reflection;
using System.Diagnostics;

namespace ZipPla
{
    interface IMultipleLanguages
    {
        void SetMessages();
    }

    static class Program
    {
        public static CatalogForm catalogForm { get; private set; }
        private static Form startForm;
        public static Form StartForm { get { return startForm; } }
        public static IMultipleLanguages multiLangForm { get; private set; }
        public static readonly string Name = Application.ProductName;
        
        public static readonly float DisplayMagnificationX;
        public static readonly float DisplayMagnificationY;
        public static readonly float DisplayMagnification;
        static Program()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (var form = new Control())
            {
                using (var g = (form).CreateGraphics())
                {
                    DisplayMagnificationX = g.DpiX / 96;
                    DisplayMagnificationY = g.DpiY / 96;
                    DisplayMagnification = (DisplayMagnificationX + DisplayMagnificationY) / 2;
                }
            }
        }
        public static int DpiScalingX(double x)
        {
            return (int)Math.Round(DisplayMagnificationX * x);
        }
        public static int DpiScalingY(double y)
        {
            return (int)Math.Round(DisplayMagnificationY * y);
        }
        public static int DpiScaling(double v)
        {
            return (int)Math.Round(DisplayMagnification * v);
        }

        public static void StartCheckUpdateAndNgen(Control owner, bool checkNgen)
        {
            var dummy = UpdateCheck.FromVector(owner, "http://www.vector.co.jp/soft/dl/winnt/art/se513079.html", Message.HowToDownloadURL,
                @"(ZipPla_\d+\.\d+\.\d+\.\d+\.zip)", checkNgen ? StartCheckUpdateAndNgen_PostAction : null as Action<bool>);
        }

        public static async Task CheckUpdateAsync(Control owner)
        {
            await UpdateCheck.FromVector(owner, "http://www.vector.co.jp/soft/dl/winnt/art/se513079.html", Message.HowToDownloadURL,
                @"(ZipPla_\d+\.\d+\.\d+\.\d+\.zip)", byUser: true);
        }

        private static async void StartCheckUpdateAndNgen_PostAction(bool messageShown)
        {
            if (!messageShown)
            {
                var zipplaPath = Application.ExecutablePath;
                var yesIsClicked = false;
                try
                {
                    if (NgenManager.ShouldBeInstalled(zipplaPath, defaultParentFolderName: "ZipPla_" + ZipPlaAssembly.Version))
                    {
                        var ngenPath = NgenManager.GetNgenPath();
                        if (ngenPath != null &&
                            !await NgenManager.IsInstalledAsync(zipplaPath, zipplaPath, "ZipPla") &&
                            MessageForm.Show(StartForm, Message.RecommendNgen.Replace(@"\n", "\n"), Message.Information,
                            MessageForm.ShieldPrefix + Message._Yes, Message._No, MessageBoxIcon.Information) == 0)
                        {
                            yesIsClicked = true;
                            var result = await NgenManager.InstallByOtherProcess(zipplaPath, ngenPath, zipplaPath);
                            if (result == 0)
                            {
                                MessageForm.Show(StartForm, Message.SucceedInNgen.Replace(@"\n", "\n"), Message.Information,
                                    Message.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                // 「エラー」をシステムが決めた言語で表示させるためにこちらは使わない
                                //MessageForm.Show(StartForm, Message.FailInNgen.Replace(@"\n", "\n").Replace("$1", result.ToString()), null,
                                //    "OK", MessageBoxIcon.Error);
                                MessageBox.Show(StartForm, Message.FailInNgen.Replace(@"\n", "\n").Replace("$1", result.ToString()), null,
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
                catch (ObjectDisposedException) { }
                catch (Exception ex)
                {
                    // 最も重要な例外は UAC がキャンセルされた場合の System.ComponentModel.Win32Exception
                    if (yesIsClicked) MessageBox.Show(StartForm, ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public static Exception exitException = null;

        public static bool CheckConfigException()
        {
            try
            {
                //if (File.Exists(Configuration.XmlBack))
                if (Configuration.ConfigDataProtection())
                {
                    MessageForm.Show(startForm, Message.ConfigSaveErrorMessage.Replace("\\n", "\n"), Message.Error, Message.OK, MessageBoxIcon.Error);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch(Exception e)
            {
                MessageForm.Show(startForm, e.Message, Message.Error, Message.OK, MessageBoxIcon.Error);
                return true;
            }
        }

#if RUNTIME
        public static TimeMeasure RunTimeMeasure;
#endif

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
#if LAB
            ZipPlaLab.Start();
            return;
#endif
#if RUNTIME
            RunTimeMeasure = new TimeMeasure();
            RunTimeMeasure.Block("Start");
#endif

            //GPSizeThumbnail.fileCacheTest(); return;
            //ViewerFormImageFilter.GetFromLinear(); return;
            //LockManager.Test(); return;
            //Clipboard.SetText(KeyboardShortcut.GetKeysString());return;


            //Clipboard.SetText(LongVectorImageResizer.CreateFromLinear(31 - 18, "fromLinearIn18Out31_gamma2_2", LongVectorImageResizer.toLinear18_gamma2_2)); MessageBox.Show("OK"); return;
            //Clipboard.SetText(LongVectorImageResizer.CreateFromLinear(20 - 13, "fromLinearIn13Out20_gamma2_2", LongVectorImageResizer.toLinear13_gamma2_2, 1UL << 19)); MessageBox.Show("OK"); return;
            //Clipboard.SetText(LongVectorImageResizer.CreateFromLinear(21 - 14, "fromLinearIn14Out21_gamma2_2", LongVectorImageResizer.toLinear14_gamma2_2, 1UL << 20)); MessageBox.Show("OK"); return;
            //LongVectorImageResizer.gammaTableTest(); return;

            //MessageBox.Show($"{ViewerFormImageFilter.ParallelBreakTestCode()}"); return;

            //string[] cmds = Environment.GetCommandLineArgs();
            //string[] cmds = (from rawPath in Environment.GetCommandLineArgs() select GetTargetIfItIsLink(rawPath)).ToArray();

            //LongVectorImageResizer.fastSinErrorTest(); return;

            var rawCommands = Environment.GetCommandLineArgs();

            if (NgenManager.CommandLineAcceptor("ZipPla", rawCommands, out var exitCode))
            {
                Environment.Exit(exitCode);
                return;
            }

            Directory.SetCurrentDirectory(Application.StartupPath);

            string[] cmds = ShortcutResolver.Exec(rawCommands).ToArray();


#if VEIWER
            startForm = new ViewerForm(null);
#else
            
            /*
            if (cmds.Length > 1 && cmds[1] == "-mode:thumbnail-cache")
            {
                try
                {
                    if (cmds.Length == 4 && cmds[2] == "create")
                    {
                        GPSizeThumbnail.
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                
                return;
            }
            */
            bool? ffmpegExists = null;
            if (cmds.Length == 2)
            {
                if (cmds[1] == "-mode:creating-language-files")
                {
                    var x = Message.DefaultLanguage;
                    return;
                }
                else if (string.Compare(cmds[1], "-v", ignoreCase: true) == 0)
                {
                    startForm = new ViewerForm(null);
                }
                else if (string.Compare(cmds[1], "-c", ignoreCase: true) == 0)
                {
                    startForm = new CatalogForm();
                }
                else if (Directory.Exists(cmds[1]) || (MovieThumbnailLoader.Supports(cmds[1]) && (bool)(ffmpegExists = MovieThumbnailLoader.ffmpegExists())))
                {
                    startForm = new CatalogForm(cmds[1]);
                }
                /*
                else if ((PackedImageLoader.Supports(cmds[1]) || ImageLoader.SupportsFullReading(cmds[1], ref ffmpegExists)) && File.Exists(cmds[1]))
                {
                    startForm = new ViewerForm(cmds[1], -1);
                }
                else if(File.Exists(cmds[1]))
                {
                    startForm = new CatalogForm(cmds[1]);
                }
                */
                else if (File.Exists(cmds[1]))
                {
                    if (PackedImageLoader.Supports(cmds[1]) || ImageLoader.SupportsFullReading(cmds[1], ref ffmpegExists))
                    {
                        startForm = new ViewerForm(cmds[1], -1, null);
                    }
                    else
                    {
                        startForm = new CatalogForm(cmds[1]);
                    }
                }
                else
                {
                    startForm = new CatalogForm(ItsSelfOrExistingParentDirectory(cmds[1]));
                }
            }
            else if (cmds.Length == 3)
            {
                if (string.Compare(cmds[1], "-c", ignoreCase: true) == 0)
                {
                    startForm = new CatalogForm(cmds[2]);
                }
                else if (!setViewerFormFromCommandLine(cmds))
                {
                    startForm = new CatalogForm();
                }
            }
            else if (cmds.Length == 4)
            {
                if (cmds[1] == "-LookAheadMode" || cmds[1] == "-LookAheadModeW" || cmds[1] == "-LookAheadModeF" ||
                    cmds[1] == "-LookAheadModeH" || cmds[1] == "-LookAheadModeWH" || cmds[1] == "-LookAheadModeFH")
                {
                    IpcClientChannel channel = new IpcClientChannel();
                    ChannelServices.RegisterChannel(channel, true);
                    var currentIpcGUID = cmds[2];
                    var currentIpcChannePortName = IpcLookAheadInfo.ChannelName + "_" + currentIpcGUID;
                    var guid = cmds[3];
                    var lookAheadURL = "ipc://" + currentIpcChannePortName + "/" + guid;
                    var reportFromViewerToCatalogURL = "ipc://" + currentIpcChannePortName + "/" + currentIpcGUID;
                    IpcLookAheadInfo ipcLookAheadInfo;
                    IpcReportFromViewerToCatalog ipcReportFromViewerToCatalog;
                    try
                    {
                        ipcLookAheadInfo = Activator.GetObject(typeof(IpcLookAheadInfo), lookAheadURL) as IpcLookAheadInfo;
                        ipcReportFromViewerToCatalog = Activator.GetObject(typeof(IpcReportFromViewerToCatalog), reportFromViewerToCatalogURL) as IpcReportFromViewerToCatalog;
                    }
                    catch (Exception error)
                    {
                        AlertError(error);
                        return;
                    }
                    startForm = new ViewerForm(ipcLookAheadInfo,
                        ipcReportFromViewerToCatalog, cmds[1].StartsWith("-LookAheadModeW") ? InitialFullscreenMode.ForceWindow :
                        cmds[1].StartsWith("-LookAheadModeF") ? InitialFullscreenMode.ForceFullscreen : InitialFullscreenMode.Default,
                        cmds[1].Last() == 'H');
                }
                else
                {
                    if(string.Compare(cmds[1], "-c", ignoreCase: true) == 0)
                    {
                        var cmds3 = cmds[3];
                        SortMode sortMode;
                        if (!string.IsNullOrEmpty(cmds3) && cmds3[0] == '-' && Enum.TryParse(cmds3.Substring(1), out sortMode))
                        {
                            startForm = new CatalogForm(cmds[2], sortMode);
                        }
                        else
                        {
                            startForm = new CatalogForm();
                        }
                    }
                    else if (!setViewerFormFromCommandLine(cmds))
                    {
                        startForm = new CatalogForm();
                    }
                }
            }
            else if (cmds.Length == 5)
            {
                if (string.Compare(cmds[1], "-c", ignoreCase: true) == 0)
                {
                    var cmds3 = cmds[3];
                    SortMode sortMode;
                    if (!string.IsNullOrEmpty(cmds3) && cmds3[0] == '-' && Enum.TryParse(cmds3.Substring(1), out sortMode))
                    {
                        var cmds4 = cmds[4];
                        if (sortMode == SortMode.Random)
                        {
                            startForm = new CatalogForm(cmds[2], sortMode: sortMode, randomSeed: cmds4);
                        }
                        else
                        {
                            SortMode sortMode2;
                            if (!string.IsNullOrEmpty(cmds4) && cmds4[0] == '-' && Enum.TryParse(cmds4.Substring(1), out sortMode2))
                            {
                                startForm = new CatalogForm(cmds[2], sortMode: sortMode2, preSortMode: sortMode);
                            }
                            else
                            {
                                startForm = new CatalogForm();
                            }
                        }
                    }
                    else
                    {
                        startForm = new CatalogForm();
                    }
                }
                else if (!setViewerFormFromCommandLine(cmds))
                {
                    startForm = new CatalogForm();
                }
            }
            else if (!setViewerFormFromCommandLine(cmds))
            {
                startForm = new CatalogForm();
            }
#endif

#if RUNTIME
            RunTimeMeasure.Block("AfterFormCreated");
#endif

            multiLangForm = StartForm as IMultipleLanguages;
            catalogForm = StartForm as CatalogForm;
            try
            {
                Application.Run(StartForm);
            }
            catch (ObjectDisposedException) { }
            catch (TargetInvocationException) { }
            catch (Exception error)
            {
                AlertError(error);
            }

            if(exitException != null)
            {
                MessageBox.Show(exitException.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        private static bool setViewerFormFromCommandLine(string[] cmds)
        {
            if (cmds != null && cmds.Length >= 2 && string.Compare(cmds[1], "-v", ignoreCase: true) == 0)
            {
                var info = ViewerForm.CommandLineOptionAnalyzer(cmds, 2);
                if (info != null)
                {
                    var path = info.Path;
                    if (string.IsNullOrEmpty(path))
                    {
                        startForm = new ViewerForm(info);
                    }
                    else
                    {
                        startForm = new ViewerForm(path, -1, info);
                    }
                }
                else
                {
                    // cmds[3] 以降は無視されるが、これにより表示されるエラーメッセージで
                    // ユーザーにコマンドラインのミスを通知できる
                    startForm = new ViewerForm(cmds[2], -1, null);
                }
                return true;
            }
            else return false;
        }

        private static Process currentProcess;
        private static ProcessPriorityClass currentPriority;
        private static bool currentProcessIsNotSet = true;
        private static bool processCanBeAccessed = true;
        /// <summary>
        /// ユーザーがプロセス優先度を変更したときの動作は保証しない（GetPriority() の結果と異なる優先度を設定しようとした場合のみ実際に変更される）
        /// </summary>
        /// <param name="priority"></param>
        public static void SetPriority(ProcessPriorityClass priority)
        {
            if (processCanBeAccessed && priority != GetPriority())
            {
                try
                {
                    currentProcess.PriorityClass = priority;
                    currentPriority = priority;
                }
                catch (PlatformNotSupportedException)
                {
                }
                catch (InvalidEnumArgumentException)
                {
                    throw;
                }
                catch
                {
                    processCanBeAccessed = false;
                    currentProcess.Dispose();
                    currentProcess = null;
                }
            }
        }
        /// <summary>
        /// ユーザーがプロセス優先度を変更したときの動作は保証しない（一度でも get または set した後はそのときの優先度が取得される）
        /// </summary>
        /// <returns></returns>
        public static ProcessPriorityClass GetPriority()
        {
            if (!processCanBeAccessed) return ProcessPriorityClass.Normal;
            if (currentProcessIsNotSet)
            {
                currentProcess = Process.GetCurrentProcess();
                try
                {
                    currentPriority = currentProcess.PriorityClass;
                }
                catch
                {
                    processCanBeAccessed = false;
                    currentProcess.Dispose();
                    currentProcess = null;
                    return ProcessPriorityClass.Normal;
                }
                currentProcessIsNotSet = false;
            }
            return currentPriority;
        }

        public static string GetFullPath(string path)
        {
            try
            {
                if (Path.IsPathRooted(path))
                {
                    return path;
                }
                else
                {
                    if (path.Contains(Path.DirectorySeparatorChar) || !Directory.Exists(path))
                    {
                        return Path.GetFullPath(path);
                    }
                    else
                    {
                        return Path.GetFullPath(path + Path.DirectorySeparatorChar);
                    }
                }
            }
            catch
            {
                return path;
            }
        }

        public static void SetInitialHeightAndPackToOwnerFormScreen(Form form, DataGridView dgv)
        {
            if (dgv.Rows.Count > 0)
            {
                var regionHeight = dgv.ClientRectangle.Bottom - dgv.Columns[0].HeaderCell.Size.Height;
                var rowsHeight = dgv.Rows[0].Height * (2 * dgv.Rows.Count + 2) / 2;
                var originalHeight = form.Height;
                var workingArea = Screen.FromControl(startForm ?? form).WorkingArea;
                var newHeight = Math.Min(workingArea.Height, originalHeight + rowsHeight - regionHeight);

                PackToRectangleOnScreen(form, Math.Max(newHeight, originalHeight), workingArea);
            }
        }

        public static void PackToOwnerFormScreen(Form form) => PackToRectangleOnScreen(form, Screen.FromControl(startForm ?? form).WorkingArea);
        public static void PackToRectangleOnScreen(Form form, Rectangle rect)
        {
            EventHandler load = null;
            load += (sender, e) =>
            {
                var originalBound = form.Bounds;
                //var newBound = BetterFormRestoreBounds.Pack(originalBound, rect);
                var newBound = BetterFormRestoreBounds.GetMovedWindowBounds(rect, originalBound);
                if (newBound != originalBound) form.Bounds = newBound;
                form.Shown -= load;
            };
            form.Load += load;
        }

        public static void PackToRectangleOnScreen(Form form, int desiredHeight, Rectangle rect)
        {
            EventHandler load = null;
            load += (sender, e) =>
            {
                var bounds = form.Bounds;
                var newBounds = bounds;
                newBounds.Height = desiredHeight;
                //newBounds = BetterFormRestoreBounds.Pack(newBounds, rect);
                newBounds = BetterFormRestoreBounds.GetMovedWindowBounds(rect, newBounds);
                if (newBounds != bounds) form.Bounds = newBounds;
                form.Shown -= load;
            };
            form.Load += load;
        }

        public static int DpiScalingMargin { get { return DpiScalingY(12); } } // static field にすると正しい値を返さない
        public static void SetFormHeightByControlLocationAndPackToOwnerScreen(Form form, Control control)
        {
            SetFormHeightByControlLocation(form, control);
            PackToOwnerFormScreen(form);
        }
        public static void SetFormHeightByControlLocation(Form form, Control control)
        {
            control.Anchor = (control.Anchor & (~AnchorStyles.Bottom)) | AnchorStyles.Top;
            if (!form.MinimumSize.IsEmpty) form.MinimumSize = new Size(form.MinimumSize.Width, 0);
            if (!form.MaximumSize.IsEmpty) form.MaximumSize = new Size(form.MaximumSize.Width, int.MaxValue);
            form.ClientSize = new Size(form.ClientSize.Width, control.Bottom + DpiScalingMargin /*DisplayMagnificationY4 * 3*/);
            form.MinimumSize = new Size(form.MinimumSize.Width, form.Size.Height);
            if (!form.MaximumSize.IsEmpty) form.MaximumSize = new Size(form.MaximumSize.Width, form.Size.Height);
        }

        public static void AlertError(Exception error)
        {
            if (error != null)
            {
                if(error is ObjectDisposedException || error is TargetInvocationException || StartForm?.IsDisposed == true)
                {
                    Environment.Exit(0);
                    return;
                }

                var act = new Action(() =>
                {
                    DialogResult r;
#if DEBUG
                    r = MessageBox.Show(error.Message + "\r\n\r\n" + error.StackTrace, error.ToString(), MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
#else
                    r = MessageBox.Show(error.Message, null, MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
#endif
                    switch (r)
                    {
                        case DialogResult.Abort:
                            Environment.Exit(error.HResult);
                            break;
                        case DialogResult.Retry:
                            try
                            {
                                System.Diagnostics.Process.Start(Application.ExecutablePath);
                            }
                            catch { }
                            Environment.Exit(error.HResult);
                            break;
                    }
                });
                if (StartForm != null && StartForm.Visible)
                {
                    try
                    {
                        lock (StartForm)
                        {
                            StartForm.Invoke((MethodInvoker)(() =>
                            {
                                act();
                            }));
                        }
                    }
                    catch
                    {
                        act();
                    }
                }
                else
                {
                    act();
                }
            }
        }

        public static void OpenWithExplorer(string path)
        {
            try
            {
                if (File.Exists(path) || Directory.Exists(path))
                {
                    System.Diagnostics.Process.Start("EXPLORER.EXE", @"/select,""" + path + @"""");
                }
                else
                {
                    while (!String.IsNullOrEmpty(Path.GetDirectoryName(path)))
                    {
                        if (System.IO.Directory.Exists(path))
                        {
                            System.Diagnostics.Process.Start("EXPLORER.EXE", path);
                            return;
                        }
                        path = Path.GetDirectoryName(path);
                    }
                }
            }
            catch { }
        }

        public static void SetInitialCondition(FileDialog fileDialog, string currentPath)
        {
            var initialDirectory = ItsSelfOrExistingParentDirectory(currentPath);
            if (!string.IsNullOrEmpty(initialDirectory))
            {
                fileDialog.InitialDirectory = initialDirectory;
            }
            string initialFileName = null;
            try
            {
                initialFileName = Path.GetFileName(currentPath);
            }
            catch (UriFormatException) { }
            if (!string.IsNullOrEmpty(initialFileName))
            {
                fileDialog.FileName = initialFileName;
            }
            else
            {
                try
                {
                    fileDialog.FileName = Path.GetFileName(fileDialog.FileName);
                }
                catch (UriFormatException) { }
            }
        }

        public static void SetInitialCondition(FolderBrowserDialog folderBrouserDialog, string currentPath)
        {
            try
            {
                while (!string.IsNullOrEmpty(currentPath) && !Directory.Exists(currentPath)) currentPath = Path.GetDirectoryName(currentPath);
                if (!string.IsNullOrEmpty(currentPath))
                {
                    folderBrouserDialog.SelectedPath = currentPath;
                }
            }
            catch { }
        }

        public static string ItsSelfOrExistingParentDirectory(string path, Func<string, bool> directoryExists = null)
        {
            try
            {
                if (directoryExists == null) directoryExists = Directory.Exists;
                while (!string.IsNullOrEmpty(path) && !directoryExists(path)) path = Path.GetDirectoryName(path);
                return path;
            }
            catch
            {
                return "";
            }
        }

        private static int Distance2(Color x, Color y)
        {
            var dr = x.R - y.R;
            var dg = x.G - y.G;
            var db = x.B - y.B;
            return dr * dr + dg * dg + db * db;
        }

        private static unsafe double Distance2(byte* adr, double[] center)
        {
            var db = adr[0] - center[0];
            var dg = adr[1] - center[1];
            var dr = adr[2] - center[2];
            return db * db + dg * dg + dr * dr;
        }

        public static Rectangle GetBlankAvoidRectangle(Bitmap image, int testWidth, int testHeight, int threshold, bool symmetry)
        {

            double zoom = Math.Max(testWidth / (double)image.Width, testHeight / (double)image.Height);
            Bitmap bmp = null;
            var blankAvoidRectangle = new Rectangle(new Point(), image.Size);
            try
            {
                if (zoom < 1)
                {
                    testWidth = (int)Math.Round(zoom * image.Width);
                    testHeight = (int)Math.Round(zoom * image.Height);
                    bmp = new Bitmap(testWidth, testHeight, PixelFormat.Format24bppRgb);
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        g.DrawImage(image, new Rectangle(new Point(), bmp.Size), new Rectangle(new Point(), image.Size), GraphicsUnit.Pixel); // 実測で、そのまま読み込むと 0.0257 秒、この方法では 0.007 秒
                    }
                }
                else
                {
                    zoom = 1;

                    testWidth = image.Width;
                    testHeight = image.Height;
                    bmp = new Bitmap(image);
                }

                // GetPixel の処理時間は大きい画像で LockBits の 1 / 1000 程度。今は縮小されているが 1 / 10 は見込めるので処理時間は問題にならない
                var neColor = bmp.GetPixel(0, 0);
                var nwColor = bmp.GetPixel(testWidth - 1, 0);
                var seColor = bmp.GetPixel(0, testHeight - 1);
                var swColor = bmp.GetPixel(testWidth - 1, testHeight - 1);

                int threshold32 = (threshold * 3) * (threshold * 3);
                var nMustBeChecked = Distance2(neColor, nwColor) <= threshold32;
                var eMustBeChecked = Distance2(neColor, seColor) <= threshold32;
                var sMustBeChecked = Distance2(swColor, seColor) <= threshold32;
                var wMustBeChecked = Distance2(swColor, nwColor) <= threshold32;

                if (nMustBeChecked || eMustBeChecked || sMustBeChecked || wMustBeChecked)
                {

                    var _img = bmp.LockBits(new Rectangle(0, 0, testWidth, testHeight), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    try
                    {
                        var Stride = _img.Stride;
                        unsafe
                        {
                            byte* adr = (byte*)_img.Scan0;

                            if (nMustBeChecked)
                            {
                                var centerColor = new double[3] { (neColor.B + nwColor.B) / 2.0, (neColor.G + nwColor.G) / 2.0, (neColor.R + nwColor.R) / 2.0 };
                                var cont = true;
                                for (var y = 0; cont && y < testHeight; y++)
                                {
                                    int yStride = y * Stride;
                                    for (var x = 0; cont && x < testWidth; x++)
                                    {
                                        int pos = x * 3 + yStride;
                                        if (2 * Distance2(adr + pos, centerColor) > threshold32)
                                        {
                                            blankAvoidRectangle.Height -= blankAvoidRectangle.Y = (int)Math.Round(y / zoom);
                                            cont = false;
                                        }
                                    }
                                }
                                if(cont)
                                {
                                    blankAvoidRectangle.Y = blankAvoidRectangle.Y;
                                    blankAvoidRectangle.Height = 0;
                                }
                            }

                            if (sMustBeChecked)
                            {
                                var centerColor = new double[3] { (swColor.B + seColor.B) / 2.0, (swColor.G + seColor.G) / 2.0, (swColor.R + seColor.R) / 2.0 };
                                var cont = true;
                                for (var y = testHeight - 1; cont && y >= 0; y--)
                                {
                                    var resultCand = 1 - blankAvoidRectangle.Y + (int)Math.Round(y / zoom);
                                    if (resultCand <= 0) return Rectangle.Empty;
                                    int yStride = y * Stride;
                                    for (var x = 0; cont && x < testWidth; x++)
                                    {
                                        int pos = x * 3 + yStride;
                                        if (2 * Distance2(adr + pos, centerColor) > threshold32)
                                        {
                                            blankAvoidRectangle.Height = resultCand;
                                            cont = false;
                                        }
                                    }
                                }
                                if (cont)
                                {
                                    blankAvoidRectangle.Y = 0;
                                    blankAvoidRectangle.Height = 0;
                                }
                            }

                            if (eMustBeChecked)
                            {
                                var centerColor = new double[3] { (neColor.B + seColor.B) / 2.0, (neColor.G + seColor.G) / 2.0, (neColor.R + seColor.R) / 2.0 };
                                var cont = true;
                                for (var x = 0; cont && x < testWidth; x++)
                                {
                                    int x3 = x * 3;
                                    for (var y = 0; cont && y < testHeight; y++)
                                    {
                                        int pos = x3 + y * Stride;
                                        if (2 * Distance2(adr + pos, centerColor) > threshold32)
                                        {
                                            blankAvoidRectangle.Width -= blankAvoidRectangle.X = (int)Math.Round(x / zoom);
                                            cont = false;
                                        }
                                    }
                                }
                                if (cont)
                                {
                                    blankAvoidRectangle.X = blankAvoidRectangle.Width;
                                    blankAvoidRectangle.Height = 0;
                                }
                            }

                            if (wMustBeChecked)
                            {
                                var centerColor = new double[3] { (nwColor.B + swColor.B) / 2.0, (nwColor.G + swColor.G) / 2.0, (nwColor.R + swColor.R) / 2.0 };
                                var cont = true;
                                for (var x = testWidth - 1; cont && x >= 0; x--)
                                {
                                    var resultCand = 1 - blankAvoidRectangle.X + (int)Math.Round(x / zoom);
                                    if (resultCand <= 0) return Rectangle.Empty;
                                    int x3 = x * 3;
                                    for (var y = 0; cont && y < testHeight; y++)
                                    {
                                        int pos = x3 + y * Stride;
                                        if (2 * Distance2(adr + pos, centerColor) > threshold32)
                                        {
                                            blankAvoidRectangle.Width = resultCand;
                                            cont = false;
                                        }
                                    }
                                }
                                if (cont)
                                {
                                    blankAvoidRectangle.X = 0;
                                    blankAvoidRectangle.Height = 0;
                                }
                            }
                        }
                    }
                    finally
                    {
                        bmp.UnlockBits(_img);
                    }

                    if (symmetry)
                    {
                        var holDec = Math.Min(blankAvoidRectangle.X, image.Width - blankAvoidRectangle.Right);
                        var verDec = Math.Min(blankAvoidRectangle.Y, image.Height - blankAvoidRectangle.Bottom);

                        blankAvoidRectangle.X = holDec;
                        blankAvoidRectangle.Width = image.Width - 2 * holDec;
                        blankAvoidRectangle.Y = verDec;
                        blankAvoidRectangle.Height = image.Height - 2 * verDec;
                    }
                }
            }
            finally
            {
                if (bmp != null) bmp.Dispose();
            }

            return blankAvoidRectangle;
        }

        public static Bitmap GetErrorImage(int width, int height)
        {
            Bitmap result = new Bitmap(width, height);
            using (var g = Graphics.FromImage(result))
            {
                g.FillRectangle(Brushes.Black, 0, 0, width, height);

                int penWidth = Math.Max(2, 2 * (int)Math.Round(Math.Min(height, width) / 40.0));
                using (var redPen = new Pen(Color.Crimson, penWidth))
                {
                    g.DrawRectangle(redPen, penWidth / 2, penWidth / 2, width - penWidth, height - penWidth);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    g.DrawLine(redPen, 0, 0, width, height);
                    g.DrawLine(redPen, width, 0, 0, height);
                }
            }
            return result;
        }
        
        private static Dictionary<Size, Bitmap> GetLightLogoImageBuffer = new Dictionary<Size, Bitmap>();
        public static Bitmap GetLightLogoImage(Size size)
        {
            if(!GetLightLogoImageBuffer.ContainsKey(size))
            {
                GetLightLogoImageBuffer[size] = GetLogoImage(size.Width, size.Height,
                Color.FromArgb(0xFF, 0xA0, 0x7A),
                Color.White,
                Color.FromArgb(0x70, 0xAD, 0x47),
                Color.FromArgb(0x44, 0x72, 0xC4));
            }
            return GetLightLogoImageBuffer[size];
        }

        private static Dictionary<Size, Bitmap> GetDarkLogoImageBuffer = new Dictionary<Size, Bitmap>();
        public static Bitmap GetDarkLogoImage(Size size)
        {
            if (!GetDarkLogoImageBuffer.ContainsKey(size))
            {
                GetDarkLogoImageBuffer[size] = GetLogoImage(size.Width, size.Height,
                Color.FromArgb(0xD9, 0xD9, 0xD9),
                Color.Black,
                Color.FromArgb(0xFF, 0x7C, 0x80), 
                Color.FromArgb(0xFF, 0xE6, 0x99));
            }
            return GetDarkLogoImageBuffer[size];
        }

        private static Bitmap GetLogoImage(int width, int height, Color frameColor, Color zColor, Color leftColor, Color rightColor)
        {
            float frameThickness = 113 / 982f;
            const float zVerticalThickness = 189 / 982f;
            const float zHorizontalThickness = 302 / 982f;

            Bitmap result = new Bitmap(width, height);
            using (var g = Graphics.FromImage(result))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;

                using (var brush = new SolidBrush(zColor))
                {
                    g.FillRectangle(brush, 0, 0, width, height);
                }

                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var slope = (1 - 2 * (frameThickness + zVerticalThickness)) / (1 - 2 * frameThickness - zHorizontalThickness);
                var boundHeight = (frameThickness + zVerticalThickness + slope * (1 - frameThickness - zHorizontalThickness)) * height;

                using (var brush = new SolidBrush(leftColor))
                {
                    g.FillPolygon(brush, new PointF[]
                    {
                        new PointF(0 - 0.5f, (frameThickness+zVerticalThickness)*height - 0.5f),
                        new PointF((1-frameThickness - zHorizontalThickness) * width - 0.5f, (frameThickness+zVerticalThickness)*height - 0.5f),
                        new PointF(0 - 0.5f, boundHeight - 0.5f),
                    });
                }

                using (var brush = new SolidBrush(rightColor))
                {
                    g.FillPolygon(brush, new PointF[]
                    {
                        new PointF(width - 0.5f, (1-frameThickness-zVerticalThickness)*height - 0.5f),
                        new PointF((frameThickness + zHorizontalThickness) * width - 0.5f, (1-frameThickness-zVerticalThickness)*height - 0.5f),
                        new PointF(width - 0.5f, height - boundHeight - 0.5f)
                    });
                }
                
                using (var brush = new SolidBrush(frameColor))
                {
                    g.FillRectangle(brush, 0 - 0.5f, 0 - 0.5f, frameThickness * width, height);
                    g.FillRectangle(brush, (1 - frameThickness) * width - 0.5f, 0 - 0.5f, frameThickness * width, height);
                    g.FillRectangle(brush, 0 - 0.5f, 0 - 0.5f, width, frameThickness * height);
                    g.FillRectangle(brush, 0 - 0.5f, (1 - frameThickness) * height - 0.5f, width, frameThickness * height);
                }
            }
            return result;
        }

        public static void FileOrDirectoryMove(string srcPath, string dstPath, Form retryOwner = null)
        {
            while (true)
            {
                try
                {
                    if (Directory.Exists(srcPath))
                    {
                        Directory.Move(srcPath, dstPath);
                    }
                    else
                    {
                        File.Move(srcPath, dstPath);
                    }
                    return;
                }
                catch(Exception e)
                {
                    if(retryOwner == null)
                    {
                        throw;
                    }
                    else
                    {
#if DEBUG
                        var r = MessageBox.Show(retryOwner, e.ToString() + "\n\n" + Message.BeforeRename + ": " + srcPath + "\n" + Message.AfterRename + ": " + dstPath,
                            null, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
#else
                        var r = MessageBox.Show(retryOwner, e.Message, null, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
#endif
                        if(r == DialogResult.Retry)
                        {
                            continue;
                        }
                        else
                        {
                            throw;
                        }
                    }
                    
                }
            }
        }

        private static PropertyInfo SetDoubleBuffered_PropertyInfo = null;
        public static void SetDoubleBuffered(Control control)
        {
            if (control == null) throw new ArgumentNullException("control");
            if(SetDoubleBuffered_PropertyInfo == null)
            {
                SetDoubleBuffered_PropertyInfo = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            SetDoubleBuffered_PropertyInfo.SetValue(control, value: true);
        }

        public static string[] EncodeDisplayIndices(DataGridViewColumnCollection col)
        {
            return (from DataGridViewColumn c in col orderby c.DisplayIndex select c.Name.Substring(3)).ToArray();
        }

        public static void DecodeDisplayIndices(DataGridViewColumnCollection col, string[] data)
        {
            if (data == null) return;
            var dataLength = data.Length;
            var columnUBound = col.Count - 1;
            for (var i = 0; i < dataLength; i++)
            {
                var found = (from DataGridViewColumn c in col where c.Name == "tbc" + data[i] select c).FirstOrDefault();
                if (found != default(DataGridViewColumn))
                {
                    found.DisplayIndex = Math.Min(i, columnUBound);
                }
            }
        }


        /*
        /// <summary>
        /// 12 が上限。 13! > int.MaxValue
        /// </summary>
        /// <param name="col"></param>
        /// <returns></returns>
        public static int EncodeDisplayIndices(DataGridViewColumnCollection col)
        {
            var code = 0;
            var m = 1;
            var colArray = (from DataGridViewColumn c in col select c).ToArray();
            var count = colArray.Length;

            // DarumaOtoshi がより適切がより適切
            var range = new List<int>(count); for (var i = 0; i < count; i++) range.Add(i);
            for (var i = 0; i < count; i++)
            {
                var p = range.IndexOf(colArray.First(c => c.DisplayIndex == i).Index);
                range.RemoveAt(p);
                code += m * p;
                m *= (count - i);
            }
            
            return code;
        }
        /// <summary>
        /// 12 が上限。 13! > int.MaxValue
        /// </summary>
        /// <param name="col"></param>
        /// <param name="code"></param>
        public static void DecodeDisplayIndices(DataGridViewColumnCollection col, int code)
        {
            if (code < 0) return;
            var colArray = (from DataGridViewColumn c in col select c).ToArray();
            var count = colArray.Length;
            
            // DarumaOtoshi がより適切
            var range = new List<int>(count); for (var i = 0; i < count; i++) range.Add(i);
            for(var i = 0; i < count; i++)
            {
                var p = code % (count - i);
                code /= (count - i);
                colArray[range[p]].DisplayIndex = i;
                range.RemoveAt(p);
            }
        }
        */

        public static bool FileExistsWithoutException(string path)
        {
            try
            {
                return File.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        private static int CursorShowLevel = 0;
        public static void HideCursor()
        {
            while (CursorShowLevel >= 0)
            {
                CursorShowLevel--;
                Cursor.Hide();
            }
        }

        public static void ShowCursor()
        {
            while (CursorShowLevel < 0)
            {
                CursorShowLevel++;
                Cursor.Show();
            }
        }

        public static void ShowAbout(Form form = null)
        {
            if (form == null)
            {
                form = StartForm;
            }
            using (var aboutForm = new AboutForm())
            {
                aboutForm.Icon = form.Icon;
                aboutForm.ShowDialog(form);
            }
        }

        public static void ShowSetting(ViewerForm form, out ulong BuiltInViewerMemoryLimit)
        {
            /*
            if (form == null)
            {
                form = startForm;
            }
            */
            using (var settingForm = new SettingForm())
            {
                settingForm.Icon = form.Icon;
                settingForm.ShowDialog(form);
                BuiltInViewerMemoryLimit = settingForm.preBuiltInViewerMemoryLimit;
                //return settingForm.NeedToReloadCatalog;

                ImageLoader.SusieResetIfSettingChanged(settingForm.preAllowUntestedPlugins, settingForm.preSearchAlsoSusieInstallationFolder);
            }
        }

        public static void ShowSetting(CatalogForm form, out ApplicationProvider[] ap,
            /*out bool startWithAccessKey,*/ out bool DynamicStringSelectionEnabled, out bool DynamicStringSelectionsAllowUserToRenameItem,
            out DynamicStringSelectionInfo[] dss, out DragAndDropAction dragAndDropAction, out bool historyTrimmed)
        {
            using (var settingForm = new SettingForm())
            {
                settingForm.Icon = form.Icon;
                settingForm.ShowDialog(form);
                ap = (from info in settingForm.preApplications select info.GetApplicationProvider(form)).ToArray();
                //startWithAccessKey = settingForm.preStartWithAccessKey;
                DynamicStringSelectionEnabled = settingForm.preDynamicStringSelectionsEnabled;
                DynamicStringSelectionsAllowUserToRenameItem = settingForm.preDynamicStringSelectionsAllowUserToRenameItem;
                dss = settingForm.preDynamicStringSelections;
                dragAndDropAction = settingForm.preDragAndDrop;
                historyTrimmed = settingForm.HistoryTrimmed;
                //return settingForm.NeedToReloadCatalog;

                ImageLoader.SusieResetIfSettingChanged(settingForm.preAllowUntestedPlugins, settingForm.preSearchAlsoSusieInstallationFolder);
            }
        }

        public static bool ShowTagEditor(Form form = null, IReadOnlyList<string> newTags = null)
        {
            if (form == null)
            {
                form = StartForm;
            }
            using (var tagEditForm = new TagEditForm(newTags))
            {
                tagEditForm.Icon = form.Icon;
                tagEditForm.ShowDialog(form);
                return tagEditForm.Edited;
            }
        }
        
        public static string[] GetFormatSizeStringTemplates(string byteInString)
        {
            if (byteInString == null) byteInString = "";
            else byteInString = " " + byteInString;
            string[] units = { byteInString, " KB", " MB", " GB", " TB", " PB", " EB" }; // これ以上は long では表現できない, " ZB", " YB" };
            var result = new string[units.Length];
            result[0] = "000" + units[0];
            //const string sizeInString = "999.9";
            const string sizeInString = "0.00";
            for (var unitIndex = 1; unitIndex < units.Length; unitIndex++)
            {
                result[unitIndex] = $"{sizeInString}{units[unitIndex]}";
            }
            return result;
        }
        public static string GetFormatSizeString(long size, string byteInString, bool simpleForm = false, bool noFraction = false)
        {
            if (byteInString == null) byteInString = "";
            else byteInString = " " + byteInString;
            string[] units = { byteInString, " KB", " MB", " GB", " TB", " PB", " EB" }; // これ以上は long では表現できない, " ZB", " YB" };
            int unitIndex = 0;
            double sizeInDouble = size;
            while (unitIndex + 1 < units.Length && sizeInDouble >= 1000) // Windows の表示に合わせる
            {
                sizeInDouble /= 1024;
                unitIndex++;
            }
            var sizeInString = noFraction || unitIndex == 0 || sizeInDouble >= 100 ? $"{sizeInDouble:F0}" :
                sizeInDouble < 10 ? $"{sizeInDouble:F2}" : $"{sizeInDouble:F1}";

            if (simpleForm)
            {
                return $"{sizeInString}{units[unitIndex]}";
            }
            else
            {
                return $"{sizeInString}{units[unitIndex]} ({size:N0}{byteInString})";
            }
        }

        public static void TextBoxShowRight(ToolStripTextBox textBox)
        {
            textBox.Select(0, 0);
            textBox.ScrollToCaret();
            textBox.Select(textBox.Text.Length, 0);
            textBox.ScrollToCaret();
        }

        public static long GetDirectoryFileSize(DirectoryInfo hDirectoryInfo)
        {
            long lTotalSize = 0;
            foreach (FileInfo cFileInfo in hDirectoryInfo.GetFiles("*",SearchOption.AllDirectories))
            {
                lTotalSize += cFileInfo.Length;
            }
            return lTotalSize;
        }

        public static void FileSaveCheckWithException(string directoryPath, string fileName)
        {
            if(string.IsNullOrEmpty(Path.GetFileNameWithoutExtension(fileName)))
            {
                throw new Exception(Message.ItIsInvalidFileName);
            }
            var path = Path.Combine(directoryPath, fileName);
            if (Path.GetFileName(path).IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new Exception(Message.ItIsInvalidFileName);
            }
            else if(File.Exists(path) || Directory.Exists(path))
            {
                throw new Exception(Message._1AlreadyExists.Replace("$1", $"\"{path}\""));
            }
        }

        public static readonly string HistorySorPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "History.sor");
        public static int GetLimitOfHistoryCount()
        {
            return (new GeneralConfig()).MaximumNumberOfHistories;
            /*
            try
            {
                return int.Parse(INIManager.LoadIniStringWithError("Settings", "MaximumNumberOfHistories"));
            }
            catch
            {
                return 10000;
            }
            */
        }

        public static DateTime? TryUpdateLastAccessTime(string path)
        {
            FileAttributes attr = default(FileAttributes);
            var readOnly = false;
            try
            {
                var isFile = File.Exists(path);
                if (isFile)
                {
                    attr = File.GetAttributes(path);

                    if ((attr & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        File.SetAttributes(path, attr & (~FileAttributes.ReadOnly));
                        readOnly = true;
                    }
                }
                var now = DateTime.Now;
                if (isFile)
                {
                    File.SetLastAccessTime(path, now);
                }
                else
                {
                    Directory.SetLastAccessTime(path, now);
                }
                return now;
            }
            catch
            {
                return null;
            }
            finally
            {
                if (readOnly)
                {
                    try
                    {
                        File.SetAttributes(path, attr);
                    }
                    catch { }
                }
            }
        }


        internal static void SetTagsToToolStripMenuItems(ToolStripMenuItem owner, out ToolStripMenuItem[] items, out CheckState[] initialCheckStates, int index, ZipTag[] tags, params ZipPlaInfo[] zipPlaInfos)
        {
            SetTagsToToolStripMenuItems(owner, owner.Owner as System.Windows.Forms.ContextMenuStrip, out items, out initialCheckStates, index, tags, zipPlaInfos);
        }

        internal static void SetTagsToToolStripMenuItems(ToolStripMenuItem owner, System.Windows.Forms.ContextMenuStrip parent, out ToolStripMenuItem[] items, out CheckState[] initialCheckStates, int index, ZipTag[] tags, params ZipPlaInfo[] zipPlaInfos)
        {
            if (tags == null) tags = new ZipTag[0];
            items = new ToolStripMenuItem[tags.Length];
            initialCheckStates = new CheckState[tags.Length];
            //var parent = owner.Owner as System.Windows.Forms.ContextMenuStrip;
            for (var i = 0; i < tags.Length; i++)
            {
                var tag = tags[i];
                var initState = zipPlaInfos.All(zpi => zpi.TagArray != null && zpi.TagArray.Contains(tag.Name)) ? CheckState.Checked :
                    zipPlaInfos.All(zpi => !(zpi.TagArray != null && zpi.TagArray.Contains(tag.Name))) ? CheckState.Unchecked : CheckState.Indeterminate;
                var item = new PrefixEscapedToolStripMenuItem(tag.Name, initState);

                CheckMarkProvider.SetCheckMark(owner.DropDown, item, DisplayCheckMark.CheckThreeState);

                item.BackColor = tag.BackColor;
                item.ForeColor = tag.ForeColor;

                var normalColor = tag.ForeColor;
                if (normalColor == Color.White)
                {
                    item.ForeColor = normalColor;
                    ForeColorManager.Set(item);
                    /*
                    EventHandler ev = (sender, e) => CatalogForm.SetMenuItemColor(item, normalColor);
                    item.DropDownOpening += ev;
                    item.DropDownClosed += ev;
                    item.MouseEnter += ev;
                    item.MouseLeave += ev;
                    */
                }


                //MessageBox.Show($"{zipPlaInfos[0].TagArray.Length} {zipPlaInfos[0].TagArray[0]}");
                initialCheckStates[i] = initState;
                //item.Click += (sender2, e2) =>
                //{
                //};
                if (parent == null)
                {
                    item.MouseDown += (sender2, e2) =>
                    {
                        if (e2.Button == MouseButtons.Right)
                        {
                            owner.DropDown.AutoClose = false;
                        }
                        if (e2.Button == MouseButtons.Right || e2.Button == MouseButtons.Left)
                        {
                            item.ToggleCheck();// itemCheckToggle(item, initState);
                        }
                    };
                    item.MouseUp += (sender2, e2) =>
                    {
                        if (e2.Button == MouseButtons.Right)
                        {
                            owner.DropDown.AutoClose = true;
                        }
                    };
                    item.MouseLeave += (sender2, e2) =>
                    {
                        owner.DropDown.AutoClose = true;
                    };
                    items[i] = item;
                }
                else
                {
                    item.MouseDown += (sender2, e2) =>
                    {
                        if (e2.Button == MouseButtons.Right)
                        {
                            parent.AutoClose = false;
                            owner.DropDown.AutoClose = false;
                        }
                        if (e2.Button == MouseButtons.Right || e2.Button == MouseButtons.Left)
                        {
                            item.ToggleCheck();// itemCheckToggle(item, initState);
                        }
                    };
                    item.MouseUp += (sender2, e2) =>
                    {
                        if (e2.Button == MouseButtons.Right)
                        {
                            parent.AutoClose = true;
                            owner.DropDown.AutoClose = true;
                        }
                    };
                    item.MouseLeave += (sender2, e2) =>
                    {
                        parent.AutoClose = true;
                        owner.DropDown.AutoClose = true;
                    };
                    items[i] = item;
                }
            }
            var dropDownItems = owner.DropDownItems;
            if (index == dropDownItems.Count)
            {
                dropDownItems.AddRange(items);
            }
            else
            {
                var count = dropDownItems.Count;
                var temp = new ToolStripItem[count];
                dropDownItems.CopyTo(temp, 0);
                dropDownItems.Clear();
                var temp1 = new ToolStripItem[count + items.Length];
                Array.Copy(temp, temp1, index);
                Array.Copy(items, 0, temp1, index, items.Length);
                Array.Copy(temp, index, temp1, index + items.Length, count - index);
                dropDownItems.AddRange(temp1);
            }
        }

        /*
        private static void itemCheckToggle(ToolStripMenuItem item, CheckState initState)
        {
            switch (item.CheckState)
            {
                case CheckState.Unchecked:
                    item.CheckState = CheckState.Checked;
                    break;
                case CheckState.Indeterminate:
                    item.CheckState = CheckState.Unchecked;
                    break;
                default:
                    item.CheckState = initState == CheckState.Indeterminate ? CheckState.Indeterminate : CheckState.Unchecked;
                    break;
            }
        }
        */

        public static DirectoryInfo[] GetDirectoriesInAllDirectoriesOnErrorResumeNext(DirectoryInfo dirInfo, string searchPattern, BackgroundWorker bw)
        {
            var result = new List<DirectoryInfo>();
            GetDirectoriesInAllDirectoriesOnErrorResumeNext(result, dirInfo, searchPattern, bw);
            return result.ToArray();
        }

        private static void GetDirectoriesInAllDirectoriesOnErrorResumeNext(List<DirectoryInfo> subDirectoryInfo, DirectoryInfo dirInfo, string searchPattern, BackgroundWorker bw)
        {
            try
            {
                subDirectoryInfo.AddRange(dirInfo.GetDirectories(searchPattern, SearchOption.TopDirectoryOnly));
            }
            catch { }
            foreach (var child in dirInfo.GetDirectories())
            {
                if (bw.CancellationPending) return;
                try
                {
                    GetDirectoriesInAllDirectoriesOnErrorResumeNext(subDirectoryInfo, child, searchPattern, bw);
                }
                catch { }
            }
        }

        public static FileInfo[] GetFilesInAllDirectoriesOnErrorResumeNext(DirectoryInfo dirInfo, string searchPattern, BackgroundWorker bw)
        {
            var result = new List<FileInfo>();
            GetFilesInAllDirectoriesOnErrorResumeNext(result, dirInfo, searchPattern, bw);
            return result.ToArray();
        }

        private static void GetFilesInAllDirectoriesOnErrorResumeNext(List<FileInfo> fileInfos, DirectoryInfo dirInfo, string searchPattern, BackgroundWorker bw)
        {
            try
            {
                fileInfos.AddRange(dirInfo.GetFiles(searchPattern, SearchOption.TopDirectoryOnly));
            }
            catch { }
            foreach (var child in dirInfo.GetDirectories())
            {
                if (bw.CancellationPending) return;
                try
                {
                    GetFilesInAllDirectoriesOnErrorResumeNext(fileInfos, child, searchPattern, bw);
                }
                catch { }
            }
        }

        public static DateTime GetCreateTimeOfFile(string fileName)
        {
            try
            {
                return File.GetCreationTime(fileName);
            }
            catch
            {
                return new DateTime();
            }
        }

        public static DateTime GetCreateTime(FileSystemInfo info)
        {
            try
            {
                return info.CreationTime;
            }
            catch
            {
                return new DateTime();
            }
        }

        public static DateTime GetLastWriteTime(FileSystemInfo info)
        {
            try
            {
                return info.LastWriteTime;
            }
            catch
            {
                return new DateTime();
            }
        }

        public static DateTime GetLastAccessTimeOfFile(string path)
        {
            try
            {
                return File.GetLastAccessTime(path);
            }
            catch
            {
                return new DateTime();
            }
        }

        public static DateTime GetLastAccessTimeOfDirectory(string path)
        {
            try
            {
                return Directory.GetLastAccessTime(path);
            }
            catch
            {
                return new DateTime();
            }
        }

        public static DateTime GetLastAccessTime(FileSystemInfo info)
        {
            try
            {
                return info.LastAccessTime;
            }
            catch
            {
                return new DateTime();
            }
        }
        
        public static void LinkFileCanceler(object sender, CancelEventArgs e)
        {
            if (e == null) return;
            var dialog = sender as FileDialog;
            if (dialog != null) return;
            var fileName = dialog.FileName;
            if (string.IsNullOrEmpty(fileName)) return;
            fileName = fileName.ToLower();
            var strFilter = dialog.Filter;
            if (string.IsNullOrEmpty(strFilter)) return;
            var filters = strFilter.Split('|');
            var index = 2 * dialog.FilterIndex + 1;
            if (index < 1 || index >= filters.Length) return;
            var strExtensions = filters[index];
            if (string.IsNullOrEmpty(strExtensions)) return;
            var extensions = strExtensions.Split(';');
            if (!extensions.Any(ex => ex != null && ex.StartsWith("*.") && fileName.EndsWith(ex.Substring(1).ToLower())))
            {
                e.Cancel = true;
            }
        }

        public static bool OpenMouseGestureSetting(Icon icon, MouseGesture mouseGesture, MouseGestureSettingTemplate[] mouseGestureSettingTemplates, IWin32Window owner, double widthZoom = 1)
        {
            using (var mouseGestureSettingForm = new MouseGestureSettingForm(mouseGesture, mouseGestureSettingTemplates, allowTouchGesture: true, allowVStartTouchGesture: owner is ViewerForm))
            {
                mouseGestureSettingForm.Icon = icon;
                mouseGestureSettingForm.Text = Message.MouseTouchGestures;

                mouseGestureSettingForm.EnabledText = Message.Enabled;
                mouseGestureSettingForm.GestureToAddCommandText = Message.GestureToAddCommand;
                mouseGestureSettingForm.AppearanceText = Message.Appearance;
                mouseGestureSettingForm.WidthText = Message.LineWidth;
                mouseGestureSettingForm.ColorText = Message.Color;
                mouseGestureSettingForm.GestureText = Message.Gesture;
                mouseGestureSettingForm.CommandText = Message.Command;
                mouseGestureSettingForm.DeleteText = Message.Delete;
                mouseGestureSettingForm.OKText = Message.OK;
                mouseGestureSettingForm.CancelText = Message.Cancel;
                mouseGestureSettingForm.DoYouDiscardChangedSettingsText = Message.DoYouDiscardChangedSettings;
                mouseGestureSettingForm.QuestionText = Message.Question;

                if (widthZoom != 1) mouseGestureSettingForm.Width = (int)Math.Round(mouseGestureSettingForm.Width * widthZoom);

                mouseGestureSettingForm.ShowDialog(owner);

                return mouseGestureSettingForm.Edited;

            }
        }

        /// <summary>
        /// パフォーマンス向上のため useLButtonWarningMessage 以外の Warning は指定しても無効に。他を使用する場合適切にコメントアウトを解除すること
        /// </summary>
        /// <param name="icon"></param>
        /// <param name="keyboardShortcut"></param>
        /// <param name="mouseGestureSettingTemplates"></param>
        /// <param name="contKeys"></param>
        /// <param name="defaultCommands"></param>
        /// <param name="useLButton"></param>
        /// <param name="useMButton"></param>
        /// <param name="useRButton"></param>
        /// <param name="useX1Button"></param>
        /// <param name="useX2Button"></param>
        /// <param name="useLButtonWarningMessage"></param>
        /// <param name="useMButtonWarningMessage"></param>
        /// <param name="useRButtonWarningMessage"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        public static bool OpenKeyboardShortcutSetting(Icon icon, KeyboardShortcut keyboardShortcut, MouseGestureSettingTemplate[] mouseGestureSettingTemplates, int[] contKeys,
            Tuple<HashSet<Keys>[],int>[] defaultCommands, bool useLButton, bool useMButton, bool useRButton, bool useX1Button, bool useX2Button,
            string useLButtonWarningMessage, string useMButtonWarningMessage, string useRButtonWarningMessage, IWin32Window owner)
        {
            using (var keyboardShortcutSettingForm = new KeyboardShortcutSettingForm(keyboardShortcut, mouseGestureSettingTemplates, contKeys, defaultCommands))
            {
                keyboardShortcutSettingForm.Icon = icon;
                keyboardShortcutSettingForm.UseLButton = useLButton;
                keyboardShortcutSettingForm.UseMButton = useMButton;
                keyboardShortcutSettingForm.UseRButton = useRButton;
                keyboardShortcutSettingForm.UseX1Button = useX1Button;
                keyboardShortcutSettingForm.UseX2Button = useX2Button;
                keyboardShortcutSettingForm.UseLButtonWarningMessage = useLButtonWarningMessage;
                keyboardShortcutSettingForm.UseMButtonWarningMessage = useMButtonWarningMessage;
                keyboardShortcutSettingForm.UseRButtonWarningMessage = useRButtonWarningMessage;

                //keyboardShortcutSettingForm.Text = Message.KeysMouseButtons;
                keyboardShortcutSettingForm.Text = Message.BasicOperationSettings;
                keyboardShortcutSettingForm.DoYouRestoreDefaultSettingOfKeyboardShortcutText = Message.DoYouRestoreDefaultSettingOfKeysMouseButtons;
                keyboardShortcutSettingForm.ShortcutText = Message.Inputs;
                keyboardShortcutSettingForm.CommandText = Message.Command;
                keyboardShortcutSettingForm.DefaultText = Message.Default;
                keyboardShortcutSettingForm.AddText = Message.Add;
                keyboardShortcutSettingForm.DeleteText = Message.Delete;
                keyboardShortcutSettingForm.OKText = Message.OK;
                keyboardShortcutSettingForm.CancelText = Message.Cancel;
                keyboardShortcutSettingForm.DoYouDiscardChangedSettingsText = Message.DoYouDiscardChangedSettings;
                keyboardShortcutSettingForm.QuestionText = Message.Question;
                keyboardShortcutSettingForm.AbortText = Message.Abort;
                keyboardShortcutSettingForm.InputKeyboardShortcutText = Message.InputKeysMouseButtons;
                keyboardShortcutSettingForm.InformationText = Message.Information;
                keyboardShortcutSettingForm.UnassignedCommandsIsComplementedText = Message.UnassignedCommandsAreComplemented + "\n" +
                    Message.ThisOperationCanBeAvoidedByAsigningNoOperationToTargetKeys;


                keyboardShortcutSettingForm.ShowDialog(owner);

                return keyboardShortcutSettingForm.Edited;

            }
        }
        
        public static BindingMode GetBindingModeFromCulture(CultureInfo culture)
        {

            var lang = Message.CurrentLanguage;

            return !culture.Equals(new CultureInfo("ja-JP")) && // 日本語は本は縦書きが主流だが ja, ja-JP とも TextInfo.IsRightToLeft は false になる。
                !culture.Equals(new CultureInfo("zh-TW")) && // 台湾も日本と同じ。
                                                          //!lang.ToString().StartsWith("zh") && // 中国は現代は横書きが主流なのでコメントアウト。ちなみに韓国語も横書き。
                !culture.TextInfo.IsRightToLeft ? BindingMode.LeftToRight : BindingMode.RightToLeft;
        }

        public static PackedImageLoader GetPackedImageLoader(string path, ArchivesInArchiveMode? aia,
            bool animation = false, PackedImageLoaderOnMemoryMode onMemory = PackedImageLoaderOnMemoryMode.None)
        {
            switch (aia)
            {
                case ArchivesInArchiveMode.IfNoOther1Level: return new PackedImageLoader(
                    path, 1, PackedImageLoaderSearchMode.UntilFoundLayer, animation: animation, onMemory: onMemory);
                case ArchivesInArchiveMode.Always1Level: return new PackedImageLoader(
                    path, 1, PackedImageLoaderSearchMode.Full, animation: animation, onMemory: onMemory);
                case ArchivesInArchiveMode.UntilFound2Level: return new PackedImageLoader(
                    path, 2, PackedImageLoaderSearchMode.UntilFoundItem, animation: animation, onMemory: onMemory);
                default: return new PackedImageLoader(path, 0, PackedImageLoaderSearchMode.Full, animation: animation, onMemory: onMemory);
            }
        }

        public static string GetNPageString(int N)
        {
            if (N == 1)
            {
                return Message.OnePage;
            }
            else
            {
                return Message._1Pages.Replace("$1", N.ToString());
            }
        }
        public static string GetNPageString(string N)
        {
            if (N == "1")
            {
                return Message.OnePage;
            }
            else
            {
                return Message._1Pages.Replace("$1", N);
            }
        }
        
        public static bool CancelMovingeEditingCellByLeftOrRightKey(object sender, DataGridViewCellValidatingEventArgs e)
        {
            var dataGridView = (DataGridView)sender;
            if (dataGridView[e.ColumnIndex, e.RowIndex].Selected)
            {
                if (dataGridView.EditingControl is DataGridViewTextBoxEditingControl textBoxEditingControl)
                {
                    if (KeyboardShortcut.GetKeyState(Keys.Home) || KeyboardShortcut.GetKeyState(Keys.End))
                    {
                        textBoxEditingControl.SelectionLength = 0;
                        e.Cancel = true;
                    }
                    else if (textBoxEditingControl.SelectionLength == 0)
                    {
                        var selectionStart = textBoxEditingControl.SelectionStart;
                        e.Cancel = selectionStart == 0 && KeyboardShortcut.GetKeyState(Keys.Left) ||
                            selectionStart == textBoxEditingControl.TextLength && KeyboardShortcut.GetKeyState(Keys.Right);
                    }
                }
            }
            return e.Cancel;
        }
    }

    public class ForeColorManager : IDisposable
    {
        public static ForeColorManager Set(ToolStripItem toolStripItem)
        {
            return new ForeColorManager(toolStripItem);
        }

        public static ForeColorManager[] Set(params ToolStripItem[] toolStripItems)
        {
            return (from toolStripItem in toolStripItems select new ForeColorManager(toolStripItem)).ToArray();
        }

        public static ForeColorManager[] Set(IEnumerable<ToolStripItem> toolStripItems)
        {
            return (from toolStripItem in toolStripItems select new ForeColorManager(toolStripItem)).ToArray();
        }

        /*
        /// <summary>
        /// ドロップダウンが開かれたことを通知します。自前のドロップダウンを開くときに使用します。
        /// </summary>
        public void SetDropDownOpend()
        {
            dropDowned = true;
        }

        /// <summary>
        /// ドロップダウンが閉じられたことを通知します。自前のドロップダウンを閉じるときに使用します。
        /// </summary>
        public void SetDropDownClosed()
        {
            dropDowned = false;
        }
        */

        private ToolStripItem toolStripItem;
        private bool dropDowned = false;
        private Color originalForeColor;
        private bool managerForeColorChanging = false;
        private ForeColorManager(ToolStripItem toolStripItem)
        {
            this.toolStripItem = toolStripItem;
            originalForeColor = toolStripItem.ForeColor;
            toolStripItem.ForeColorChanged += toolStripItem_ForeColorChanged;
            toolStripItem.Paint += toolStripItem_Paint;
            if (toolStripItem is ToolStripDropDownItem toolStripDropDownItem)
            {
                toolStripDropDownItem.DropDownOpened += toolStripDropDownItem_DropDownOpened;
                toolStripDropDownItem.DropDownClosed += toolStripDropDownItem_DropDownClosed;
            }
        }
        
        private void toolStripDropDownItem_DropDownOpened(object sender, EventArgs e)
        {
            dropDowned = true;
        }

        private void toolStripDropDownItem_DropDownClosed(object sender, EventArgs e)
        {
            dropDowned = false;
        }

        private void toolStripItem_ForeColorChanged(object sender, EventArgs e)
        {
            if (!managerForeColorChanging)
            {
                originalForeColor = toolStripItem.ForeColor;
            }
        }

        private void toolStripItem_Paint(object sender, PaintEventArgs e)
        {
            if (!managerForeColorChanging)
            {
                //var foreColor = dropDowned || toolStripItem.Selected ? Color.Black : ZipTag.GetForeColor(toolStripItem.BackColor);
                var foreColor = dropDowned || toolStripItem.Selected ? SystemColors.WindowText : ZipTag.GetForeColor(toolStripItem.BackColor);
                if (foreColor != toolStripItem.ForeColor)
                {
                    managerForeColorChanging = true;
                    toolStripItem.ForeColor = foreColor;
                    managerForeColorChanging = false;
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
                    toolStripItem.ForeColor = originalForeColor;
                    toolStripItem.ForeColorChanged -= toolStripItem_ForeColorChanged;
                    toolStripItem.Paint -= toolStripItem_Paint;
                    if (toolStripItem is ToolStripDropDownItem toolStripDropDownItem)
                    {
                        toolStripDropDownItem.DropDownOpened -= toolStripDropDownItem_DropDownOpened;
                        toolStripDropDownItem.DropDownClosed -= toolStripDropDownItem_DropDownClosed; ;
                    }
                }

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~ForeColorManager() {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }

    public class PrefixEscapedToolStripMenuItem : ToolStripMenuItem
    {
        private string plainText;
        public string PlainText
        {
            get
            {
                return plainText;
            }
        }

        private CheckState initialCheckState;

        public PrefixEscapedToolStripMenuItem(string text, CheckState initialCheckState) : base(text?.Replace("&", "&&"))
        {
            plainText = text;
            CheckState = initialCheckState;
            this.initialCheckState = initialCheckState;
        }

        public void ToggleCheck()
        {
            switch (CheckState)
            {
                case CheckState.Unchecked:
                    CheckState = CheckState.Checked;
                    break;
                case CheckState.Indeterminate:
                    CheckState = CheckState.Unchecked;
                    break;
                default:
                    CheckState = initialCheckState == CheckState.Indeterminate ? CheckState.Indeterminate : CheckState.Unchecked;
                    break;
            }
        }
    }
    
    public class IpcLookAheadInfo : MarshalByRefObject
    {
        public string Path;
        public int Page;
        public ArchivesInArchiveMode ArchivesInArchive;
        public BindingMode DefaultBinding;
        public CoverBindingMode CoverBinding;
        public Color BackColor;
        public SortModeDetails SortModeDetails;
        public bool IgnoreSavedFilerSetting;
        public string FilterString;
        public string FilterStringWithoutAlias;
        public enum MessageEnum { Wait, Show, CoverSetting }
        public MessageEnum Message = MessageEnum.Wait;
        public InitialFullscreenMode InitialFullscreenMode;
        public bool AlwaysHideUI;
        public ReadOnMemoryMode ReadOnMemoryMode;
        public bool Accept = true;

        public static string ChannelName = "ipcZipPla";

        public override object InitializeLifetimeService() => null;
    }

    [Serializable]
    public class SortModeDetails :IEquatable<SortModeDetails>
    {
        public SortMode SortMode, PreSortMode;
        public string RandomSeed;

        public bool ProbablyEquals(SortModeDetails other) => Equals(other);

        public SortModeDetails Clone()
        {
            return new SortModeDetails { SortMode = SortMode, PreSortMode = PreSortMode, RandomSeed = RandomSeed };
        }

        public bool Equals(SortModeDetails other)
        {
            return other as object != null && other.SortMode == SortMode && other.PreSortMode == PreSortMode && other.RandomSeed == RandomSeed;
        }

        public bool QuickEquals(SortModeDetails other)
        {
            return Equals(other);
        }

        public static bool operator ==(SortModeDetails a, SortModeDetails b)
        {
            if (a as object == null) return b as object == null;
            return a.Equals(b);
        }

        public static bool operator !=(SortModeDetails a, SortModeDetails b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SortModeDetails);
        }

        public override int GetHashCode()
        {
            if (RandomSeed == null)
            {
                return SortMode.GetHashCode() ^ PreSortMode.GetHashCode();
            }
            else
            {
                return SortMode.GetHashCode() ^ PreSortMode.GetHashCode() ^ RandomSeed.GetHashCode();
            }
        }
    }


    public class IpcReportFromViewerToCatalog : MarshalByRefObject
    {
        [Serializable]
        public class OldAndNewString
        {
            public string Old, New, Sor = null;
            public int NewPage;
            public bool RequestToUpdateLastAccessTime;
            public bool RequestToChangeSelection;
            public SortMode ViewerFormSortMode;
            public bool RequestToReloadTags;
        }
        public List<OldAndNewString> PathRequiredReload = new List<OldAndNewString>();

        /// <summary>
        /// 自動的に切断されるのを回避する
        /// </summary>
        public override object InitializeLifetimeService()
        {
            return null;
        }
    }

    static class ToolStripDropDownScroller
    {
        /// <summary>
        /// ToolStripDropDown をスクロール可能にします。末端以外に適用する場合は別途 MouseEnter 等の処理が必要です。
        /// </summary>
        /// <param name="tsdd">スクロール可能にする ToolStripDropDown</param>
        public static void Enscrollable(ToolStripDropDown tsdd, bool enterFocus = false)
        {
            Descrollable(tsdd);
            tsdd.MouseWheel += MouseWheelEventHandler;
            tsdd.KeyUp += KeyUpEventHandler;
            tsdd.Opened += OpendEventHander;
            if (enterFocus) tsdd.MouseEnter += FocusEventHandler;
        }

        public static void Enscrollable(ToolStripDropDown tsdd, Alteridem.WinTouch.GestureListener gestureListener, bool enterFocus = false)
        {
            Enscrollable(tsdd, enterFocus);
            if (gestureListener != null) gestureListener.Pan += GestureListener_Pan;
        }

        public static Alteridem.WinTouch.GestureListener GetGestureListener(Control control)
        {
            try
            {
                return new Alteridem.WinTouch.GestureListener(control, new Alteridem.WinTouch.GestureConfig[] {
                    //new GestureConfig(3, 1, 0), // ズーム
                    new Alteridem.WinTouch.GestureConfig(4, 2 | 4 , 8 | 16 ), // パン、向き拘束と慣性なし
                });
            }
            catch
            {
                return null;
            }
        }

        static bool GestureListener_Pan_Scrolled = false;
        static int GestureListener_Pan_StartingLocation;
        static int GestureListener_Pan_PrevTotalDelta;
        private static void GestureListener_Pan(object sender, Alteridem.WinTouch.PanEventArgs e)
        {
            var gestureListener = (Alteridem.WinTouch.GestureListener)sender;
            var tsdd = (ToolStripDropDown)gestureListener.Parent;
            var items = tsdd.Items;
            if (items.Count > 0)
            {
                var location = e.Location.Y;
                if (e.Begin)
                {
                    GestureListener_Pan_Scrolled = false;
                    e.Handled = true;
                    GestureListener_Pan_StartingLocation = location;
                    GestureListener_Pan_PrevTotalDelta = 0;
                }
                if (e.Handled) return;
                var height = items[0].Height;
                var totalDelta = (int)Math.Round((double)(location - GestureListener_Pan_StartingLocation) / height);
                var prevTotalDelta = GestureListener_Pan_PrevTotalDelta;
                GestureListener_Pan_PrevTotalDelta = totalDelta;
                if (totalDelta != prevTotalDelta)
                {
                    GestureListener_Pan_Scrolled = true;
                    var sub = totalDelta - prevTotalDelta;
                    var up = sub > 0;
                    PerformScroll(tsdd, up, up ? sub : -sub);
                }
                if (!e.End)
                {
                    e.Handled = true;
                }
                else if (GestureListener_Pan_Scrolled)
                {
                    GestureListener_Pan_Scrolled = false;
                    e.Handled = true;
                }
            }
        }

        private static void FocusEventHandler(object sender, EventArgs e)
        {
            var tsdd = (ToolStripDropDown)sender;
            tsdd.Focus();
        }

        public static void Descrollable(ToolStripDropDown tsdd)
        {
            tsdd.MouseWheel -= MouseWheelEventHandler;
            tsdd.KeyUp -= KeyUpEventHandler;
            tsdd.Opened -= OpendEventHander;
            tsdd.MouseEnter -= FocusEventHandler;
        }

        public static void EnscrollableOneTime(ToolStripDropDown tsdd, Alteridem.WinTouch.GestureListener gestureListener, bool enterFocus)
        {
            Enscrollable(tsdd, enterFocus);
            tsdd.Closed -= DescrollableAfterClose;
            tsdd.Closed += DescrollableAfterClose;
            if (gestureListener != null)
            {
                gestureListener.Pan -= GestureListener_Pan;
                gestureListener.Pan += GestureListener_Pan;
                ToolStripDropDownClosedEventHandler eh = null;
                eh = (sender, e) =>
                {
                    gestureListener.Pan -= GestureListener_Pan;
                    tsdd.Closed -= eh;
                };
                tsdd.Closed += eh;
            }
        }

        private static void DescrollableAfterClose(object sender, ToolStripDropDownClosedEventArgs e)
        {
            var tsdd = (ToolStripDropDown)sender;
            Descrollable(tsdd);
            tsdd.Closed -= DescrollableAfterClose;
        }

        private static void PerformScroll(ToolStripDropDown tsdd, bool up, int count)
        {
            Type t = typeof(ToolStripDropDownMenu);
            FieldInfo p = t.GetField((up ? "upScrollButton" : "downScrollButton"),
                BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
            if (p != null)
            {   //ボタンのオブジェクトを取得、スクロール処理のメソッドも確保
                ToolStripControlHost scrollButton = p.GetValue(tsdd) as ToolStripControlHost;
                MethodInfo m = t.GetMethod("ScrollInternal",
                    BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Instance,
                    null, new Type[] { typeof(bool) }, null);
                for (int i = 0; i < count; i++)
                {
                    if (scrollButton != null && scrollButton.Visible && scrollButton.Enabled && m != null)
                    {   //該当ボタンが押せる状態であれば、スクロール用メソッドを呼び出す
                        m.Invoke(tsdd, new object[] { up });
                    }
                }
            }
        }

        private static void MouseWheelEventHandler(object sender, MouseEventArgs e)
        {
            //５件分スクロールさせ、処理済み状態にする
            PerformScroll(sender as ToolStripDropDown, e.Delta > 0, 5);
            ((HandledMouseEventArgs)e).Handled = true;
        }

        private static void KeyUpEventHandler(object sender, KeyEventArgs e)
        {
            var tsdd = sender as ToolStripDropDown;

            //まず、選択中のメニュー項目、およびコンテキストメニューの表示領域を把握する
            ToolStripItem item = null;
            foreach (ToolStripItem i in tsdd.Items)
            {
                if (i.Selected) { item = i; break; }
            }
            Rectangle displayRect = tsdd.RectangleToClient(tsdd.Bounds);
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            {   //矢印キー関連は何もしない
            }
            else if (e.KeyCode == Keys.Apps)
            {   //アプリケーションキー関連も何もしない
            }
            else if (e.KeyCode == Keys.PageUp)
            {   //１０件分上スクロール
                PerformScroll(tsdd, true, 10);
                if (item != null && displayRect.Contains(item.Bounds) == false)
                {   //現在選択中のアイテムが表示領域外ならば、アイテム末尾から表示領域内のアイテムを探索
                    for (int i = tsdd.Items.Count - 1; i >= 0; i--)
                    {
                        item = tsdd.Items[i];
                        if (displayRect.Contains(item.Bounds) && item.CanSelect)
                        {   //表示領域内のアイテムを選択する
                            item.Select();
                            break;
                        }
                    }
                }
            }
            else if (e.KeyCode == Keys.PageDown)
            {   //１０件分下スクロール
                PerformScroll(tsdd, false, 10);
                if (item != null && displayRect.Contains(item.Bounds) == false)
                {   //現在選択中のアイテムが表示領域外ならば、アイテム先頭から表示領域内のアイテムを探索
                    for (int i = 0; i < tsdd.Items.Count; i++)
                    {
                        item = tsdd.Items[i];
                        if (displayRect.Contains(item.Bounds) && item.CanSelect)
                        {   //表示領域内のアイテムを選択する
                            item.Select();
                            break;
                        }
                    }
                }
            }
            else
            {   //アクセスキーによる操作とみなし、表示領域外の場合は選択中アイテムの位置へスクロール移動する
                if (item != null && displayRect.Contains(item.Bounds) == false)
                {
                    Type t = typeof(ToolStripDropDownMenu);
                    MethodInfo m = t.GetMethod("ChangeSelection",
                        BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Instance,
                        null, new Type[] { typeof(ToolStripItem) }, null);
                    if (m != null)
                    {
                        m.Invoke(tsdd, new object[] { item });
                    }
                }
            }
        }
        
        private static void OpendEventHander(object sender, EventArgs e)
        {
            (sender as ToolStripDropDown).Focus();
        }
    }
    
}
