using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
            pbLight.Image = Program.GetLightLogoImage(pbLight.Size);
            pbDark.Image = Program.GetDarkLogoImage(pbDark.Size);

            Program.SetFormHeightByControlLocationAndPackToOwnerScreen(this, btnOK);

            Text = "About " + Program.Name;
            lbZipPla.Text = Program.Name;

            btnOK.Text = "OK"; // デザイナのコードはプリプロセッサに消される

            var thisAssembly = Assembly.GetExecutingAssembly();
            var version = thisAssembly.GetName().Version.ToString();
            if (!string.IsNullOrEmpty(ZipPlaAssembly.BetaString)) version += " " + ZipPlaAssembly.BetaString;
            var copyright = Attribute.GetCustomAttribute(thisAssembly, typeof(AssemblyCopyrightAttribute)) as AssemblyCopyrightAttribute;

            const string mailAddress = @"riostoolbox@gmail.com";
            const string url = @"https://sites.google.com/site/riostoolbox/";

            lbVersion.Text = lbVersion.Text.Replace("%VERSION%", version.ToString());
            llLicense.Text = llLicense.Text.Replace("%COPYRIGHT%", copyright.Copyright);
            llLicense.Text = llLicense.Text.Replace("%URL%", url);
            llLicense.Text = llLicense.Text.Replace("%MAILADDRESS%", mailAddress);

            AddLinkLabel(llLicense, mailAddress, "mailto:" + mailAddress);
            AddLinkLabel(llLicense, url, url);

            AddLinkLabel(llLicense, "http://www.gnu.org/licenses/", "http://www.gnu.org/licenses/");

            btnOK.Select();
        }

        private void SetPictureToBox(Image image, PictureBox pb)
        {
            //補間方法を指定して画像を縮小して描画する

            //描画先とするImageオブジェクトを作成する
            Bitmap canvas = new Bitmap(pb.Width, pb.Height);
            //ImageオブジェクトのGraphicsオブジェクトを作成する
            using (Graphics g = Graphics.FromImage(canvas))
            {
                //補間方法として高品質双三次補間を指定する
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                //画像を縮小して描画する
                g.DrawImage(image, 0, 0, canvas.Width, canvas.Height);
            }

            //PictureBox1に表示する
            pb.SizeMode = PictureBoxSizeMode.Normal;
            pb.Image = canvas;
        }

        private void AddLinkLabel(LinkLabel linkLabel, string word, string link)
        {
            int startIndex = linkLabel.Text.IndexOf(word, 0, linkLabel.Text.Length);
            linkLabel.Links.Add(startIndex, word.Length, link);
        }

        private void llLicense_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            e.Link.Visited = true;
            System.Diagnostics.Process.Start(e.Link.LinkData.ToString());
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
