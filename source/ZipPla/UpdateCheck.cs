using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public static class UpdateCheck
    {
        public static async Task FromVector(Control owner, string vectorUri, string howToDownloadUri, string fileNamePattern = null, Action<bool> postAction = null, bool byUser = false)
        {
            var messageShown = false;
            try
            {
                if (needToCheck(owner, force: byUser, dialog: out var dialog))
                {
                    if (dialog) byUser = true;
                    var fileName = await GetFileNameFromVector(vectorUri, fileNamePattern);
                    if (newFileName(fileName, out var developmentVersion))
                    {
                        messageShown = true;
                        MethodInvoker action = () =>
                        MessageForm.Show(owner, fileName + "\r\n\r\n" + Message.NewVersionIsAvailable, Message.Information,
                            Message.HowToDownload, (sender, e) =>
                            {
                                try
                                {
                                    System.Diagnostics.Process.Start(howToDownloadUri);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            },
                            Message.DownloadPage, (sender, e) =>
                            {
                                try
                                {
                                    System.Diagnostics.Process.Start(vectorUri);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            },
                            Message.ZipPlaFolder, (sender, e) =>
                            {
                                try
                                {
                                    //Program.OpenWithExplorer(Application.ExecutablePath); // 既に開いていても改めて開く
                                    System.Diagnostics.Process.Start(Application.StartupPath); // 既に開いていればそれを表示
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            },
                            Message.Close, (sender, e) =>
                            {
                                try
                                {
                                    sender.Close();
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                }
                            },
                            MessageBoxIcon.Information);
                        if (owner == null) action(); else owner.Invoke(action);

                        /*
                        var res = MessageBox.Show(
                            fileName + "\r\n\r\n" + Message.NewVersionIsAvailableDoYouOpenDownloadPage, Message.Information, MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if(res == DialogResult.Yes)
                        {
                            System.Diagnostics.Process.Start(vectorUri);
                        }
                        */
                    }
                    else if (byUser)
                    {
                        MessageForm.Show(owner, developmentVersion ? Message.YouAreUsingUnreleasedVersion : Message.NewVersionIsNotAvailable, Message.Information, Message.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                if (byUser) MessageBox.Show(ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            postAction?.Invoke(messageShown);
        }
        
        private static readonly Regex regexGetVersion = new Regex(@"(\d+)\.(\d+)\.(\d+)\.(\d+)", RegexOptions.Compiled);
        private static bool newFileName(string fileName, out bool developmentVersion)
        {
            var m = regexGetVersion.Match(fileName);
            if(m.Success)
            {
                developmentVersion = false;

                var f1 = int.Parse(m.Groups[1].Value);
                var f2 = int.Parse(m.Groups[2].Value);
                var f3 = int.Parse(m.Groups[3].Value);
                var f4 = int.Parse(m.Groups[4].Value);

                var thisAssembly = Assembly.GetExecutingAssembly();
                var version = thisAssembly.GetName().Version;
                if (version.Major < f1) return true;
                if (version.Major > f1) { developmentVersion = true; return false; }
                if (version.Minor < f2) return true;
                if (version.Minor > f2) { developmentVersion = true; return false; }
                if (version.Build < f3) return true;
                if (version.Build > f3) { developmentVersion = true; return false; }
                if (version.Revision < f4) return true;
                if (version.Revision > f4) { developmentVersion = true; return false; }
                return false;
            }
            else
            {
                throw new FormatException("File name");
            }
        }
        
        public static int VersionStringComp(string a, string b)
        {
            if (a == null) return b == null ? 0 : -1;
            if (b == null) return 1;
            Match aM, bM = null;
            if((aM = regexGetVersion.Match(a)).Success && (bM = regexGetVersion.Match(b)).Success)
            {
                for(var i = 1; i <= 4; i++)
                {
                    var ad = int.Parse(aM.Groups[i].Value);
                    var bd = int.Parse(bM.Groups[i].Value);
                    if (ad > bd) return +1;
                    if (ad < bd) return -1;
                }
                return 0;
            }
            throw new ArgumentException(null, bM == null ? "a" : "b");
        }

        private static bool needToCheck(Control owner, bool force, out bool dialog)
        {
            var config = new UpdateCheckConfig();

            var lastCheckTime = config.LastCheckTime;
            var first = lastCheckTime == DateTime.MinValue;

            if (config.Mode == null) config.Mode = first ? UpdateCheckMode.Default : UpdateCheckMode.Silent;

            if (force)
            {
                config.LastCheckTime = DateTime.Now;
                config.Save();
                dialog = false;
                return true;
            }

            if (config.Mode == UpdateCheckMode.Never)
            {
                dialog = false;
                return false;
            }

            var now = DateTime.Now;

            if (first)
            {
                config.LastCheckTime = now;
                config.Save();
                dialog = false;
                return false;
            }

            if (lastCheckTime > now) lastCheckTime = DateTime.MinValue;
            //lastCheckTime = DateTime.MinValue;

            var lastUpdateTime = now.Date;
            while (true)
            {
                switch (lastUpdateTime.DayOfWeek)
                {
                    case DayOfWeek.Tuesday:
                    case DayOfWeek.Thursday:
                    case DayOfWeek.Saturday:
                        break;
                    default:
                        lastUpdateTime -= TimeSpan.FromDays(1);
                        continue;
                }
                break;
            }
            if (lastUpdateTime > lastCheckTime)
            {
                bool returnTrue;
                if (config.Mode == UpdateCheckMode.Silent)
                {
                    dialog = false;
                    returnTrue = true;
                }
                else
                {
                    dialog = true;
                    var checkBoxChecked = config.Mode == UpdateCheckMode.WithDialog;
                    returnTrue = MessageForm.Show(owner, Message.ZipPlaWillCheckForUpdate, Message.Information, Message.ShowThisDialogAgainNextTime, ref checkBoxChecked, Message.OK, Message.Cancel, MessageBoxIcon.Information) == 0;
                    if (returnTrue)
                    {
                        config.Mode = checkBoxChecked ? UpdateCheckMode.WithDialog : UpdateCheckMode.Silent;
                    }
                }
                if (returnTrue)
                {
                    config.LastCheckTime = now;
                    config.Save();
                    return true;
                }
            }
            dialog = false;
            return false;

        }
        
        //private static readonly Regex regexGetFileNameFromVector = new Regex(@"^\s*<tr><th>ファイル：<\/th><td>(.*?)&nbsp;\/&nbsp;.*?&nbsp;/&nbsp;.*?<\/td><\/tr>$", RegexOptions.Compiled | RegexOptions.Multiline);
        private static async Task<string> GetFileNameFromVector(string vectorUri, string matchPattern = null)
        {
            /*
            var source = await Task.Run(() =>
            {
                using (var wc = new System.Net.WebClient())
                {
                    return wc.DownloadString(vectorUri);
                }
            });
            */
            string source;
            using (var wc = new System.Net.WebClient())
            {
                source = await wc.DownloadStringTaskAsync(vectorUri);
            }

            if (matchPattern == null) matchPattern = @"^\s*<tr><th>ファイル：<\/th><td>(.*?)&nbsp;\/&nbsp;.*?&nbsp;/&nbsp;.*?<\/td><\/tr>$";
            //var regexGetFileNameFromVector = new Regex(matchPattern, RegexOptions.Compiled | RegexOptions.Multiline);
            //var m = regexGetFileNameFromVector.Match(source);
            var m = Regex.Match(source, matchPattern, RegexOptions.Multiline);
            if(m.Success)
            {
                return m.Groups[1].Value;
            }
            throw new FormatException("Html");
        }

        /*
        private static async Task<string> GetFileNameFromVectorByBrowser(string vectorUri)
        {
            var document = await GetDocument(vectorUri);
            var table = document.GetElementById("download_list").Parent.GetElementsByTagName("table")[0];
            foreach(HtmlElement tr in table.GetElementsByTagName("tr"))
            {
                var th = tr.GetElementsByTagName("th");
                if (th.Count != 1 || th[0].InnerText != "ファイル：") continue;
                var td = tr.GetElementsByTagName("td");
                if (td.Count != 1) break;
                var tdText = td[0].InnerText;
                var slashPos = tdText.IndexOf(" / ");
                if (slashPos < 0) break;
                return tdText.Substring(0, slashPos);
            }
            throw new FormatException("Html");
        }

        [DllImport("urlmon.dll")]
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Error)]
        internal static extern int CoInternetSetFeatureEnabled(uint FeatureEntry, [MarshalAs(UnmanagedType.U4)] uint dwFlags, bool fEnable);
        private static async Task<HtmlDocument> GetDocument(string uri)
        {
            using (var webBrowser = new WebBrowser())
            {
                webBrowser.ScriptErrorsSuppressed = true;
                webBrowser.Navigated += (sender, e) =>
                {
                    const uint FEATURE_DISABLE_NAVIGATION_SOUNDS = 21;
                    const uint SET_FEATURE_ON_PROCESS = 0x00000002;
                    CoInternetSetFeatureEnabled(FEATURE_DISABLE_NAVIGATION_SOUNDS, SET_FEATURE_ON_PROCESS, true);
                };
                var loading = true;
                {
                    webBrowser.DocumentCompleted += (sender, e) => loading = false;
                    webBrowser.Url = new Uri(uri);
                    await Task.Run(() => { while (loading) { Thread.Sleep(50); } });
                }
                return webBrowser.Document;
            }
        }
        */
    }

    public enum UpdateCheckMode { Default, Never, WithDialog, Silent }

    public class UpdateCheckConfig : Configuration
    {
        public UpdateCheckMode? Mode = null;
        public DateTime LastCheckTime = DateTime.MinValue;
    }
}
