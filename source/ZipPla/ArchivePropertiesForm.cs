using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public partial class ArchivePropertiesForm : Form
    {
        public string FilePath { get; private set; }

        private bool showCanceled = false;

        public ArchivePropertiesForm(string path, int pageCount, ArchivesInArchiveMode aia, ImageInfo imageInfo, MovieInfo movieInfo)
        {
            InitializeComponent();

            Program.SetFormHeightByControlLocationAndPackToOwnerScreen(this, btnOK);

            SetMessages();

            FilePath = path;

            try
            {
                var fileName = Path.GetFileName(path);

                Text = Regex.Replace(fileName, "^(.*)$", Message.PropertiesOf1);

                tbLocation.Text = Path.GetDirectoryName(path);


                var virtualData = false;
                var virtualParent = "";
                var virtualChild = "";
                var inArchive = false;
                var inMovie = false;

                var altSepPos = path.IndexOf(Path.AltDirectorySeparatorChar);
                if (altSepPos >= 0)
                {
                    virtualData = true;
                    virtualParent = path.Substring(0, altSepPos);
                    virtualChild = path.Substring(altSepPos + 1);
                    inArchive = PackedImageLoader.Supports(virtualParent);
                    inMovie = MovieThumbnailLoader.Supports(virtualParent);
                }

                lbType2.Text = GetFileType(path, virtualParent);

                if(virtualData)
                {
                    tbFileName.Text = Path.GetFileNameWithoutExtension(fileName);
                    tbExtension.Text = Path.GetExtension(fileName);

                    tbLocation.Text = virtualParent;

                    tbFileName.ReadOnly = true;

                    if (inArchive)
                    {
                        var ext = Path.GetExtension(virtualChild);
                        tbFileName.Text = virtualChild.Substring(0, virtualChild.Length - ext.Length);
                        tbExtension.Text = ext;

                        using (var loader = Program.GetPackedImageLoader(virtualParent, aia))
                        {
                            var entry = loader.GetPackedImageEntries(PackedImageLoaderFileInfo.All).First(e => e.Path == virtualChild);
                            using (var img = loader.OpenImageStream(entry))
                            {
                                imageInfo = ImageLoader.GetImageInfo(img);
                            }
                            if(entry.Size >= 0)
                            {
                                lbSize2.Text = Program.GetFormatSizeString(entry.Size, Message.Bytes);
                            }
                            else
                            {
                                // PDF
                                lbSize2.Text = "-";
                                lbFileName.Text = Message.Position;
                                tbExtension.Visible = false;
                                tbFileName.Width = tbLocation.Width;
                            }

                            lbLaccessDate2.Text = "-";
                            try
                            {
                                try
                                {
                                    lbModifiedDate2.Text = entry.LastWriteTime.ToString("F", Message.CurrentLanguage);
                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    lbModifiedDate2.Text = entry.LastWriteTime.ToString("s");
                                }
                            }
                            catch
                            {
                                lbModifiedDate2.Text = "<ERROR>";
                            }
                        }
                    }
                    else if (inMovie)
                    {
                        tbFileName.Text = virtualChild;
                        tbExtension.Text = "";
                        
                        lbFileName.Text = Message.Position;

                        tbExtension.Visible = false;
                        tbFileName.Width = tbLocation.Width;

                        var fi = new FileInfo(virtualParent);
                        lbSize2.Text = Program.GetFormatSizeString(fi.Length, Message.Bytes);
                        lbLaccessDate2.Text = GetDateTimeString(Program.GetLastAccessTime(fi));
                        lbModifiedDate2.Text = GetDateTimeString(Program.GetLastWriteTime(fi));
                    }
                    else throw new NotSupportedException();
                }
                else if (File.Exists(path))
                {
                    tbFileName.Text = Path.GetFileNameWithoutExtension(fileName);
                    tbExtension.Text = Path.GetExtension(fileName);

                    var fi = new FileInfo(path);
                    lbSize2.Text = Program.GetFormatSizeString(fi.Length, Message.Bytes);
                    lbLaccessDate2.Text = GetDateTimeString(Program.GetLastAccessTime(fi));
                    lbModifiedDate2.Text = GetDateTimeString(Program.GetLastWriteTime(fi));
                }
                else if(Directory.Exists(path))
                {
                    tbFileName.Text = Path.GetFileName(fileName);
                    tbExtension.Text = "\\";

                    var fi = new DirectoryInfo(path);
                    lbSize2.Text = Program.GetFormatSizeString(Program.GetDirectoryFileSize(fi), Message.Bytes);
                    lbLaccessDate2.Text = GetDateTimeString(Program.GetLastAccessTime(fi));
                    lbModifiedDate2.Text = GetDateTimeString(Program.GetLastWriteTime(fi));
                }
                else
                {
                    throw new FileNotFoundException(null, path);
                }

                //if (ImageLoader.SupportsAtLeastThumbnailReading(path) || inArchive)
                //bool? supportsFullReading = null;
                var ffmpegExists = null as bool?;
                if (inArchive || ImageLoader.SupportsGetImageInfo(path, ref ffmpegExists)) //(bool)(supportsFullReading = ImageLoader.SupportsFullReading(path)) || ImageInfo.Supports(path))
                {
                    bool? supportFullReading = null;
                    if(imageInfo == null || (imageInfo.BitPerPixel == 0 && (bool)(supportFullReading = ImageLoader.SupportsFullReading(path, ref ffmpegExists))))
                    {
                        if (!virtualData)
                        {
                            imageInfo = ImageLoader.GetImageInfo(path, colorDepthMustBeGot: supportFullReading != null ? (bool)supportFullReading : ImageLoader.SupportsFullReading(path, ref ffmpegExists), ffmpegExists: ref ffmpegExists);
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                    lbNumberOfPages.Text = Message.ImageSize + ":";
                    var size = imageInfo.Size;
                    if (imageInfo.BitPerPixel > 0)
                    {
                        lbNumberOfPages2.Text = Message.Width1Height2BitsPerPixel3.Replace("$1", $"{size.Width}").Replace("$2", $"{size.Height}").Replace("$3", $"{imageInfo.BitPerPixel}");
                    }
                    else
                    {
                        lbNumberOfPages2.Text = Message.Width1Height2BitsPerPixel3.Replace("$1", $"{size.Width}").Replace("$2", $"{size.Height}").Replace("$3", "?");
                    }
                }
                else if(PackedImageLoader.Supports(path))
                {
                    if (pageCount < 0)
                    {
                        using (var loader = Program.GetPackedImageLoader(path, aia))
                        {
                            pageCount = loader.GetPackedImageEntries().Count;
                        }
                    }
                    lbNumberOfPages.Text = Message.NumberOfPages + ":";
                    lbNumberOfPages2.Text = Program.GetNPageString(pageCount);
                }
                else if(MovieThumbnailLoader.Supports(virtualData ? virtualParent : path) && MovieThumbnailLoader.ffmpegExists())
                {
                    if (movieInfo == null)
                    {
                        try
                        {
                            if (!virtualData)
                            {
                                movieInfo = MovieInfo.FromFile(path);
                            }
                            else
                            {
                                movieInfo = MovieInfo.FromFile(virtualParent);
                            }
                        }
                        catch
                        {
                            throw new Exception(Message.FailedToLoadFile);
                        }
                        if(movieInfo == null)
                        {
                            throw new Exception(Message.FailedToLoadFile);
                        }
                    }
                    lbNumberOfPages.Text = Message.VideoInfo + ":";
                    lbNumberOfPages2.Text = movieInfo.ToLongString();
                }
                else
                {
                    lbNumberOfPages.Visible = lbNumberOfPages2.Visible = false;
                }
            }
            catch(Exception error)
            {
                MessageBox.Show(this, error.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                //Shown += new EventHandler((sender , e) => { Close(); }); // コンストラクタ中で Close を呼んではいけないため
                showCanceled = true; // 代替案
            }
        }

        private static string GetDateTimeString(DateTime dateTime)
        {
            if(dateTime == null || dateTime == DateTime.MinValue)
            {
                return "-";
            }
            else
            {
                return dateTime.ToString("F", Message.CurrentLanguage);
            }
        }

        public new void Show()
        {
            
            if (showCanceled)
            {
                return;
            }
            else
            {
                base.Show();
            }
        }

        public new void ShowDialog(IWin32Window owner)
        {
            if (showCanceled)
            {
                return;
            }
            else
            {
                base.ShowDialog(owner);
            }
        }

        private void SetMessages()
        {
            lbLocation.Text = Message.Location + ":";
            lbFileName.Text = Message.Name + ":";
            lbType.Text = Message.FileType + ":";
            lbSize.Text = Message.Size + ":";
            lbLaccessDate.Text = Message.DateAccessed + ":";
            lbModifiedDate.Text = Message.DateModified + ":";
            btnOK.Text = Message.OK;
        }

        private string GetFileType(string path, string virtualParent)
        {
            if (File.Exists(path))
            {
                return FileTypeManager.GetTypeName(path, useFileAttrinutes: false); // IconUtility.TypeName(path);
                /*
                var ext = Path.GetExtension(path);
                if (!string.IsNullOrEmpty(ext))
                {
                    return ext.Substring(1).ToUpper() + " " + Message.File;
                }
                else
                {
                    return Message.File;
                }
                */
            }
            else if (Directory.Exists(path))
            {
                return FileTypeManager.GetTypeName(path, useFileAttrinutes: false);
                //return Message.Folder;
            }
            else if (!string.IsNullOrEmpty(virtualParent))
            {
                bool? ffmpegExists = null;
                if(MovieThumbnailLoader.Supports(virtualParent) && (bool)(ffmpegExists = MovieThumbnailLoader.ffmpegExists()))
                {
                    return FileTypeManager.GetTypeName(virtualParent, useFileAttrinutes: false);
                    /*
                    var ext = Path.GetExtension(virtualParent);
                    if (!string.IsNullOrEmpty(ext))
                    {
                        return ext.Substring(1).ToUpper() + " " + Message.File;
                    }
                    else
                    {
                        throw new FileNotFoundException(null, path);
                    }
                    */
                }
                else if(ImageLoader.SupportsAtLeastThumbnailReading(path, ref ffmpegExists))
                {
                    return FileTypeManager.GetTypeName(path, useFileAttrinutes: true);
                }
                else
                {
                    return "-"; // PDF
                }
                /*
                else if(PackedImageLoader.SupportedArchivesPath.IsMatch(virtualParent))
                {
                    var ext = Path.GetExtension(path);
                    if (!string.IsNullOrEmpty(ext))
                    {
                        return ext.Substring(1).ToUpper() + " " + Message.File;
                    }
                    else
                    {
                        return "-"; // PDF
                    }
                }
                else
                {
                    throw new FileNotFoundException(null, path);
                }
                */
            }
            else
            {
                throw new FileNotFoundException(null, path);
            }
        }
        
        private void btnOK_Click(object sender, EventArgs e)
        {
            string newFilePath;
            {
                try
                {
                    newFilePath = GetFilePath();
                }
                catch (Exception error)
                {
                    MessageBox.Show(this, error.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if (!sameAsPath(FilePath, newFilePath))
            {
                try
                {
                    Program.FileSaveCheckWithException(tbLocation.Text, tbFileName.Text + (tbExtension.Text != "\\" ? tbExtension.Text : ""));
                }
                catch (Exception error)
                {
                    MessageBox.Show(this, error.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (MessageBox.Show(this, Message.DoYouRenameThis, Message.Question, MessageBoxButtons.OKCancel, MessageBoxIcon.Question ) == DialogResult.OK)
                {
                    FilePath = newFilePath;
                }
            }
            
            Close();
        }

        private static bool sameAsPath(string a, string b)
        {
            return a.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar) == b.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        // 例外を投げるので注意
        private string GetFilePath()
        {
            if (tbExtension.Text != "\\")
            {
                return Path.Combine(tbLocation.Text, tbFileName.Text + tbExtension.Text);
            }
            else
            {
                return Path.Combine(tbLocation.Text, tbFileName.Text);
            }
        }

        private void ArchivePropertiesForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            bool pathChanged;
            try
            {
                pathChanged = !sameAsPath(FilePath, GetFilePath());
            }
            catch
            {
                pathChanged = true;
            }
            if (pathChanged)
            {
                if(MessageBox.Show(this, Message.DoYouDiscardChangedName, Message.Question, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    e.Cancel = true;
                }
            }
        }

        private void tbFileName_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                btnOK_Click(sender, e);
                e.Handled = true;
            }
        }

        private void ArchivePropertiesForm_Shown(object sender, EventArgs e)
        {
            // Load イベントでは無効
            tbFileName.Focus();
            //tbFileName.SelectAll(); // 初めから全選択
        }
    }
}
