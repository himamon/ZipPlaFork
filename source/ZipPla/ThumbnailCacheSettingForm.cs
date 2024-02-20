using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public partial class ThumbnailCacheSettingForm : Form
    {
        public string CacheRoot { get; private set; }

        bool firstSetting;

        public ThumbnailCacheSettingForm(string cacheRoot, Color color, bool firstSetting = false)
        {
            InitializeComponent();

            this.firstSetting = firstSetting;

            SetMessages();


            //Program.SetFormHeightByControlLocationAndPackToOwnerScreen(this, btnCancel); // ピクチャーボックスのレイアウトが崩れる
            Program.SetFormHeightByControlLocation(this, btnCancel);

            pictureBox.Width = pictureBox.Height = btnOK.Height - 2;
            pictureBox.Top = btnOK.Bottom - pictureBox.Height - 1;
            pictureBox.Left = Left + Program.DpiScalingMargin;
            pictureBox.BackColor = color;

            Program.PackToOwnerFormScreen(this);

            CacheRoot = cacheRoot;
            if (firstSetting)
            {
                // 全て非選択で開始する場合
                /*
                rbNoCache.AutoCheck = rbADS.AutoCheck = rbFile.AutoCheck = false;

                // AutoCheck == true の場合ここで全 false が解除されるので注意
                setAdsEnabled(false);
                setFileEnabled(false);
                
                rbNoCache.Checked = false;
                rbADS.Checked = false;
                rbFile.Checked = false;

                rbNoCache.AutoCheck = rbADS.AutoCheck = rbFile.AutoCheck = true;
                tbFile.Text = CatalogForm.DefaultThumbnailCache;

                btnOK.Enabled = false;
                */

                // 推奨設定を選択済みで開始する場合
                rbADS.Checked = true;
                rbFile_CheckedChanged(rbFile, null);
                tbFile.Text = CatalogForm.DefaultThumbnailCache;
                
                // キャンセルボタン非表示
                btnOK.Left = btnCancel.Right - btnOK.Width;
                btnCancel.Visible = false;

                // キャンセルボタン無効化
                //btnCancel.Enabled = false;

                // プロファイルカラー非表示
                pictureBox.Visible = false;
            }
            else if (cacheRoot == null)
            {
                rbNoCache.Checked = true;
                rbADS_CheckedChanged(rbADS, null);
                rbFile_CheckedChanged(rbFile, null);
                tbFile.Text = CatalogForm.DefaultThumbnailCache;
            }
            else if (cacheRoot == GPSizeThumbnail.AlternateDataStream)
            {
                rbADS.Checked = true;
                rbFile_CheckedChanged(rbFile, null);
                tbFile.Text = CatalogForm.DefaultThumbnailCache;
            }
            else
            {
                rbFile.Checked = true;
                rbADS_CheckedChanged(rbADS, null);
                tbFile.Text = cacheRoot;
            }

        }

        public void SetMessages()
        {
            Text = Message.ThumbnailCache;
            lbFirstDescription.Text = (firstSetting ? Message.ThumbnailCacheFirstDescriptionForFirstLaunch :
                Message.ThumbnailCacheFirstDescription).Replace(@"\n", "\n");
            rbADS.Text = Message.StoreCachesInAlternateDataStreamRecommended;
            lbAdsDescription.Text = Message.ThumbnailCacheAdsDescription.Replace(@"\n", "\n");
            rbFile.Text = Message.StoreCachesInSpecifiedFolder;

            var r = tbFile.Right;
            var w = btnBrowse.Left - btnOriginal.Right;

            btnZipPla.Text = Message.LocationOfZipPlaExe;
            btnOriginal.Text = Message.LocationOfOriginalFile;
            btnBrowse.Text = Message.Browse;
            btnBrowse.Left = r - btnBrowse.Width;
            btnOriginal.Left = btnBrowse.Left - btnOriginal.Width - w;
            btnZipPla.Left = btnOriginal.Left - btnZipPla.Width - w;

            lbFileDescription.Text = Message.ThumbnailCacheSpecifiedFolderDescription.Replace(@"\n", "\n");
            rbNoCache.Text = Message.NotUseThumbnailCache;
            btnOK.Text = Message.OK;
            btnCancel.Text = Message.Cancel;

        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            var current = GetCurrentCacheRoot();
            try
            {
                if (current != null && current != GPSizeThumbnail.AlternateDataStream) Path.GetFullPath(current); // 例外が投げられないかチェックする
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            CacheRoot = current;
            Close();
        }

        public static readonly Color DisabledColor = Color.DarkGray;
        public static readonly Brush DisabledBrush = Brushes.DarkGray;

        private void rbADS_CheckedChanged(object sender, EventArgs e)
        {
            setAdsEnabled((sender as RadioButton).Checked);

            if (firstSetting)
            {
                btnOK.Enabled = true;
            }
        }

        private void setAdsEnabled(bool ckd)
        {
            lbAdsDescription.ForeColor = ckd ? SystemColors.ControlText : DisabledColor;
        }

        private void rbFile_CheckedChanged(object sender, EventArgs e)
        {
            var ckd = (sender as RadioButton).Checked;
            setFileEnabled(ckd);

            if (firstSetting)
            {
                btnOK.Enabled = true;
            }
        }

        private void setFileEnabled(bool ckd)
        {
            tbFile.Enabled = ckd;
            btnZipPla.Enabled = ckd;
            btnOriginal.Enabled = ckd;
            btnBrowse.Enabled = ckd;
            lbFileDescription.ForeColor = ckd ? SystemColors.ControlText : DisabledColor;
        }

        private void rbNoCache_CheckedChanged(object sender, EventArgs e)
        {
            if (firstSetting)
            {
                btnOK.Enabled = true;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ThumbnailCacheSettingForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (GetCurrentCacheRoot() != CacheRoot &&
                MessageBox.Show(this, Message.DoYouDiscardChangedSettings, Message.Question, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                e.Cancel = true;
            }
        }

        private string GetCurrentCacheRoot()
        {
            if (rbADS.Checked)
            {
                return GPSizeThumbnail.AlternateDataStream;
            }
            else if (rbFile.Checked)
            {
                return tbFile.Text;
            }
            else return null;
        }
        
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                Program.SetInitialCondition(dialog, tbFile.Text);
                if(dialog.ShowDialog(this) == DialogResult.OK)
                {
                    tbFile.Text = dialog.SelectedPath;
                }
            }
        }

        private void btnZipPla_Click(object sender, EventArgs e)
        {
            tbFile.Text = CatalogForm.DefaultThumbnailCache;
        }

        private void btnOriginal_Click(object sender, EventArgs e)
        {
            tbFile.Text = "ZipPlaThumb";
        }

        private void lbAdsDescription_Click(object sender, EventArgs e)
        {
            rbADS.Checked = true;
        }

        private void lbFileDescription_Click(object sender, EventArgs e)
        {
            rbFile.Checked = true;
        }

        private void ThumbnailCacheSettingForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (rbADS.Top <= e.Y && e.Y < lbAdsDescription.Bottom) rbADS.Checked = true;
            else if (rbFile.Top <= e.Y && e.Y < lbFileDescription.Bottom) rbFile.Checked = true;
            else if (rbNoCache.Top <= e.Y && e.Y < pictureBox.Bottom && e.X < btnOK.Left - btnOK.Width) rbNoCache.Checked = true;
        }

        // 閉じるボタン非表示
        protected override CreateParams CreateParams
        {
            [System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityAction.Demand,
                Flags = System.Security.Permissions.SecurityPermissionFlag.UnmanagedCode)]
            get
            {
                if (firstSetting)
                {
                    const int CS_NOCLOSE = 0x200;
                    CreateParams cp = base.CreateParams;
                    cp.ClassStyle = cp.ClassStyle | CS_NOCLOSE;

                    return cp;
                }
                else return base.CreateParams;
            }
        }
    }
}
