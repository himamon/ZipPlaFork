using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public abstract class DynamicStringSelectionToolStripMenuItem : ToolStripMenuItem
    {
        protected bool IconIsStatic = false;
        private string selectedText;
        public string SelectedText
        {
            get { return selectedText; }
            set
            {
                selectedText = value;
                SetText(selectedText);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing && !IconIsStatic)
            {
                var img = Image;
                
                if (img != null)
                {
                    Image = null;
                    img.Dispose();
                }
            }
        }

        protected abstract void SetText(string selectedText);

        protected static string EscapePrefix(string baseText)
        {
            return baseText?.Replace("&", "&&");
        }
    }

    public class SelectionToFilterToolStripMenuItem : DynamicStringSelectionToolStripMenuItem
    {
        public static Image Icon;

        private CatalogForm catalogForm;
        public SelectionToFilterToolStripMenuItem(CatalogForm catalogForm)
        {
            this.catalogForm = catalogForm;
            IconIsStatic = true;
            Image = Icon;
        }

        protected override void SetText(string selectedText)
        {
            if(string.IsNullOrEmpty(selectedText))
            {
                Enabled = false;
                Text = EscapePrefix(Message.FilterWithSelectedText);
            }
            else
            {
                Enabled = true;
                Text = EscapePrefix(Message.FilterWith1.Replace("$1", selectedText));
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            catalogForm.StringToFilterForDynamicStringSelection(SelectedText);
        }
    }

    public class SelectionToClipBoardToolStripMenuItem : DynamicStringSelectionToolStripMenuItem
    {
        public static Image Icon;
        
        public SelectionToClipBoardToolStripMenuItem()
        {
            IconIsStatic = true;
            Image = Icon;
        }

        protected override void SetText(string selectedText)
        {
            if (string.IsNullOrEmpty(selectedText))
            {
                Enabled = false;
                Text = EscapePrefix(Message.CopySelectedText);
            }
            else
            {
                Enabled = true;
                Text = EscapePrefix(Message.Copy1.Replace("$1", selectedText));
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            var selectedText = SelectedText;
            if (!string.IsNullOrEmpty(selectedText))
            {
                try
                {
                    Clipboard.SetText(selectedText);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }


    public class DynamicStringSelectionCustomCommandToolStripMenuItem : DynamicStringSelectionToolStripMenuItem
    {
        private string displayName, displayNameForEmptyString, command;
        private CatalogForm catalogForm;

        public DynamicStringSelectionCustomCommandToolStripMenuItem (string displayName, string displayNameForEmptyString, string command, CatalogForm catalogForm)
        {
            this.displayName = displayName != null ? displayName : "";
            this.displayNameForEmptyString = displayNameForEmptyString != null ? displayNameForEmptyString : "";
            this.command = command != null ? command : "";
            this.catalogForm = catalogForm;

            IconIsStatic = false;
            var psi = parseCommandLine(command);
            if (psi != null)
            {
                try
                {
                    var path = psi.FileName;
                    if (File.Exists(path))
                    {
                        Image = FileTypeManager.GetSmallIconBitmap(path, useFileAttrinutes: false);
                    }
                    else
                    {
                        Task.Run(async () =>
                        {
                            var image = await getFaviconTaskAsync(path);
                            if (image != null)
                            {
                                catalogForm.Invoke(((MethodInvoker)(() =>
                                {
                                    if (IsDisposed)
                                    {
                                        image.Dispose();
                                    }
                                    else
                                    {
                                        Image = image;
                                    }
                                })));
                            }
                        });
                    }
                }
                catch
                {
                }
            }
        }

        public static bool IsUrlCommand(string command)
        {
            var firstArg = GetFirstArg(command);
            if (string.IsNullOrEmpty(firstArg)) return false;
            try
            {
                return !string.IsNullOrEmpty(new Uri(firstArg).GetLeftPart(UriPartial.Authority));
            }
            catch
            {
                return false;
            }

        }

        private async Task<Image> getFaviconTaskAsync(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            try
            {
                Uri u = new Uri(url);
                var authority = u.GetLeftPart(UriPartial.Authority);
                if (string.IsNullOrEmpty(authority)) return null;
                var faviconUrl = authority + "/favicon.ico";
                byte[] data = null;
                using (var wc = new System.Net.WebClient())
                {
                    var fu = new Uri(faviconUrl);
                    wc.Headers.Add("User-Agent", "ZipPla"); // Yahoo 等、ユーザーエージェントがないと応答しないページがある
                    data = await wc.DownloadDataTaskAsync(fu);
                }
                if (data == null || data.Length <= 0) return null;
                using (var s = new MemoryStream(data))
                using (var img = Image.FromStream(s))
                {
                    return new Bitmap(img);
                }
            }
#if DEBUG
            catch(Exception e)
            {
                MessageBox.Show(e.ToString());
#else
            catch
            {
#endif
                return null;
            }
        }

        protected override void SetText(string selectedText)
        {
            if (string.IsNullOrEmpty(selectedText) && !string.IsNullOrEmpty(displayNameForEmptyString))
            {
                Enabled = false;
                Text = EscapePrefix(displayNameForEmptyString);
            }
            else
            {
                Enabled = true;
                if (selectedText == null) selectedText = "";
                Text = EscapePrefix(displayName.Replace("$1", selectedText));
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);

            var selectedText = SelectedText;
            if (selectedText == null) selectedText = "";
            try
            {
                var psi = parseCommandLine(command.Replace("$1", selectedText));
                if (psi != null)
                {
                    System.Diagnostics.Process.Start(psi);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(catalogForm, ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static string GetFirstArg(string command)
        {
            if (string.IsNullOrEmpty(command)) return "";
            var fileNameStop = parseCommandLine_GetFileNameStop(command);
            if (fileNameStop < 0) return "";
            return command.Substring(0, fileNameStop).Replace("\"", "");
        }

        static System.Diagnostics.ProcessStartInfo parseCommandLine(string command)
        {
            if (string.IsNullOrEmpty(command)) return null;
            var fileNameStop = parseCommandLine_GetFileNameStop(command);
            if (fileNameStop < 0) return null;
            var fileName = command.Substring(0, fileNameStop).Replace("\"", "");
            var param = command.Substring(fileNameStop).TrimStart();

            if(param != "")
            {
                return new System.Diagnostics.ProcessStartInfo(fileName, param);
            }
            else
            {
                return new System.Diagnostics.ProcessStartInfo(fileName);
            }
        }

        static int parseCommandLine_GetFileNameStop(string command)
        {
            command = command.Trim();

            var ca = command.ToArray();
            var length = ca.Length;
            if (length == 0) return -1;
            var inDC = false;
            var fileNameStop = 0;
            for (fileNameStop = 0; fileNameStop < length; fileNameStop++)
            {
                var c = ca[fileNameStop];
                if (c == '"')
                {
                    inDC = !inDC;
                }
                else if (!inDC && c == ' ')
                {
                    break;
                }
            }

            return fileNameStop;
        }
    }


    public class DynamicStringSelectionInfo : IEquatable<DynamicStringSelectionInfo>
    {
        public const string ToFilter = "<To filter>";
        public const string ToClipboard = "<To clipboard>";
        public const string CustomCommand = "<Custom Command>";

        public string Type;
        public string DisplayName;
        public string DisplayNameForEmptyString;
        public string Command;

        public DynamicStringSelectionInfo() { }

        public bool Equals(DynamicStringSelectionInfo other)
        {
            return other != null && Type == other.Type && DisplayName == other.DisplayName && DisplayNameForEmptyString == other.DisplayNameForEmptyString && Command == other.Command;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DynamicStringSelectionInfo);
        }

        public override int GetHashCode()
        {
            return GetHashOfString(Type) ^ GetHashOfString(DisplayName) ^ GetHashOfString(DisplayNameForEmptyString) ^ GetHashOfString(Command);
        }

        private static int GetHashOfString(string str)
        {
            return str == null ? 0 : str.GetHashCode();
        }

        public DynamicStringSelectionToolStripMenuItem GetDynamicStringSelectionToolStripMenuItem(CatalogForm catalogForm)
        {
            if (Type == ToFilter) return new SelectionToFilterToolStripMenuItem(catalogForm);
            if (Type == ToClipboard) return new SelectionToClipBoardToolStripMenuItem();
            return new DynamicStringSelectionCustomCommandToolStripMenuItem(DisplayName, DisplayNameForEmptyString, Command, catalogForm);
        }
    }
}
