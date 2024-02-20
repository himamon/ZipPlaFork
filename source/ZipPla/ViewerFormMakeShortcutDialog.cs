using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public partial class ViewerFormMakeShortcutDialog : Form
    {
        ViewerForm.CommandLineOptionInfo commandLineOptionInfo;
        string currentLocation;

        public ViewerFormMakeShortcutDialog(ViewerForm.CommandLineOptionInfo info, string currentLocation)
        {
            InitializeComponent();

            commandLineOptionInfo = info;
            this.currentLocation = currentLocation;

            SetMessages();

            Width = Math.Max(Math.Max(Math.Max(Math.Max(
                cbStartInPreviousImageFilterSetting.Left + cbStartInPreviousImageFilterSetting.Right,
                rbStartInPreviousFullscreenMode.Left + rbStartInPreviousFullscreenMode.Right),
                rbAlwaysStartInWindowMode.Left + rbAlwaysStartInWindowMode.Right),
                rbAlwaysStartInFullscreenMode.Left + rbAlwaysStartInFullscreenMode.Right),
                2 * Width - btnCancel.Left - btnCancel.Right) + Width - ClientSize.Width;
            
            if (info.OpenInPreviousImageFilterSetting) cbStartInPreviousImageFilterSetting.Checked = true;
            switch (info.InitialFullscreenMode)
            {
                case InitialFullscreenMode.ForceFullscreen: rbAlwaysStartInFullscreenMode.Checked = true; break;
                case InitialFullscreenMode.ForceWindow: rbAlwaysStartInWindowMode.Checked = true; break;
                case InitialFullscreenMode.Default: rbStartInPreviousFullscreenMode.Checked = true; break;
            }

            Program.PackToOwnerFormScreen(this);
        }

        private void SetMessages()
        {
            Text = Message.CreateShortcutFromCurrentSettings;
            cbStartInPreviousImageFilterSetting.Text = Message.OpenInPreviousRotationToneCurveSetting;
            rbStartInPreviousFullscreenMode.Text = Message.OpenInPreviousFullscreenMode;
            rbAlwaysStartInWindowMode.Text = Message.OpenInWindow;
            rbAlwaysStartInFullscreenMode.Text = Message.OpenInFullscreen;
            btnOK.Text = Message.Save;
            btnCancel.Text = Message.Cancel;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            commandLineOptionInfo.OpenInPreviousImageFilterSetting = cbStartInPreviousImageFilterSetting.Checked;
            commandLineOptionInfo.InitialFullscreenMode =
                rbAlwaysStartInFullscreenMode.Checked ? InitialFullscreenMode.ForceFullscreen :
                rbAlwaysStartInWindowMode.Checked ? InitialFullscreenMode.ForceWindow :
                InitialFullscreenMode.Default;
            if (ViewerForm.CreateShortcut(this, commandLineOptionInfo, currentLocation))
            {
                Close();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
