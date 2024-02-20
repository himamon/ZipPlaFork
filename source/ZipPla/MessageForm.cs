using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public class MessageFormButtonClickEventArgs : EventArgs
    {
        private int index;
        public int Index { get { return index; } }

        public MessageFormButtonClickEventArgs(int index)
        {
            this.index = index;
        }
    }
    public delegate void MessageFormButtonClickEventHandler(Form sender, MessageFormButtonClickEventArgs e);

    public static class MessageForm
    {
        public static string ShieldPrefix = "<UACShieldIcon>";

        public static int Show(Control owner, string message, string caption,
            string button0,
            MessageBoxIcon icon)
        {
            return Show(owner, message, caption,
                new string[] { button0 },
                icon);
        }

        public static int Show(Control owner, string message, string caption,
            string button0,
            string button1,
            MessageBoxIcon icon)
        {
            return Show(owner, message, caption,
                new string[] { button0, button1 },
                icon);
        }

        public static int Show(Control owner, string message, string caption,
            string button0,
            string button1,
            string button2,
            MessageBoxIcon icon)
        {
            return Show(owner, message, caption,
                new string[] { button0, button1, button2 },
                icon);
        }

        public static int Show(Control owner, string message, string caption,
            string button0,
            string button1,
            string button2,
            string button3,
            MessageBoxIcon icon)
        {
            return Show(owner, message, caption,
                new string[] { button0, button1, button2, button3 },
                icon);
        }

        public static int Show(Control owner, string message, string caption,
            string button0,
            string button1,
            string button2,
            string button3,
            string button4,
            MessageBoxIcon icon)
        {
            return Show(owner, message, caption,
                new string[] { button0, button1, button2, button3, button4 },
                icon);
        }

        public static int Show(Control owner, string message, string caption,
            string checkBox, ref bool checkBoxChecked,
            string button0,
            MessageBoxIcon icon)
        {
            return Show(owner, message, caption,
                checkBox, ref checkBoxChecked,
                new string[] { button0 },
                icon);
        }

        public static int Show(Control owner, string message, string caption,
            string checkBox, ref bool checkBoxChecked,
            string button0,
            string button1,
            MessageBoxIcon icon)
        {
            return Show(owner, message, caption,
                checkBox, ref checkBoxChecked,
                new string[] { button0, button1 },
                icon);
        }

        public static void Show(Control owner, string message, string caption,
            string button0, MessageFormButtonClickEventHandler action0,
            MessageBoxIcon icon)
        {
            Show(owner, message, caption,
                new string[] { button0 },
                new MessageFormButtonClickEventHandler[] { action0 },
                icon);
        }

        public static void Show(Control owner, string message, string caption,
            string button0, MessageFormButtonClickEventHandler action0,
            string button1, MessageFormButtonClickEventHandler action1,
            MessageBoxIcon icon)
        {
            Show(owner, message, caption,
                new string[] { button0, button1 },
                new MessageFormButtonClickEventHandler[] { action0, action1 },
                icon);
        }

        public static void Show(Control owner, string message, string caption,
            string button0, MessageFormButtonClickEventHandler action0,
            string button1, MessageFormButtonClickEventHandler action1,
            string button2, MessageFormButtonClickEventHandler action2,
            MessageBoxIcon icon)
        {
            Show(owner, message, caption,
                new string[] { button0, button1, button2 },
                new MessageFormButtonClickEventHandler[] { action0, action1, action2 },
                icon);
        }

        public static void Show(Control owner, string message, string caption,
            string button0, MessageFormButtonClickEventHandler action0,
            string button1, MessageFormButtonClickEventHandler action1,
            string button2, MessageFormButtonClickEventHandler action2,
            string button3, MessageFormButtonClickEventHandler action3,
            MessageBoxIcon icon)
        {
            Show(owner, message, caption,
                new string[] { button0, button1, button2, button3 },
                new MessageFormButtonClickEventHandler[] { action0, action1, action2, action3 },
                icon);
        }

        public static void Show(Control owner, string message, string caption,
            string button0, MessageFormButtonClickEventHandler action0,
            string button1, MessageFormButtonClickEventHandler action1,
            string button2, MessageFormButtonClickEventHandler action2,
            string button3, MessageFormButtonClickEventHandler action3,
            string button4, MessageFormButtonClickEventHandler action4,
            MessageBoxIcon icon)
        {
            Show(owner, message, caption,
                new string[] { button0, button1, button2, button3, button4 },
                new MessageFormButtonClickEventHandler[] { action0, action1, action2, action3, action4 },
                icon);
        }

        /*
        public static int Show(string message, string caption,
            string button0,
            MessageBoxIcon icon)
        {
            return show(null, message, caption,
                new string[] { button0 },
                icon);
        }

        public static int Show(string message, string caption,
            string button0,
            string button1,
            MessageBoxIcon icon)
        {
            return show(null, message, caption,
                new string[] { button0, button1 },
                icon);
        }

        public static int Show(string message, string caption,
            string button0,
            string button1,
            string button2,
            MessageBoxIcon icon)
        {
            return show(null, message, caption,
                new string[] { button0, button1, button2 },
                icon);
        }

        public static int Show(string message, string caption,
            string button0,
            string button1,
            string button2,
            string button3,
            MessageBoxIcon icon)
        {
            return show(null, message, caption,
                new string[] { button0, button1, button2, button3 },
                icon);
        }

        public static int Show(string message, string caption,
            string button0,
            string button1,
            string button2,
            string button3,
            string button4,
            MessageBoxIcon icon)
        {
            return show(null, message, caption,
                new string[] { button0, button1, button2, button3, button4 },
                icon);
        }

        public static void Show(string message, string caption,
            string button0, MessageFormButtonClickEventHandler action0,
            MessageBoxIcon icon)
        {
            show(null, message, caption,
                new string[] { button0 },
                new MessageFormButtonClickEventHandler[] { action0 },
                icon);
        }

        public static void Show(string message, string caption,
            string button0, MessageFormButtonClickEventHandler action0,
            string button1, MessageFormButtonClickEventHandler action1,
            MessageBoxIcon icon)
        {
            show(null, message, caption,
                new string[] { button0, button1 },
                new MessageFormButtonClickEventHandler[] { action0, action1 },
                icon);
        }

        public static void Show(string message, string caption,
            string button0, MessageFormButtonClickEventHandler action0,
            string button1, MessageFormButtonClickEventHandler action1,
            string button2, MessageFormButtonClickEventHandler action2,
            MessageBoxIcon icon)
        {
            show(null, message, caption,
                new string[] { button0, button1, button2 },
                new MessageFormButtonClickEventHandler[] { action0, action1, action2 },
                icon);
        }

        public static void Show(string message, string caption,
            string button0, MessageFormButtonClickEventHandler action0,
            string button1, MessageFormButtonClickEventHandler action1,
            string button2, MessageFormButtonClickEventHandler action2,
            string button3, MessageFormButtonClickEventHandler action3,
            MessageBoxIcon icon)
        {
            show(null, message, caption,
                new string[] { button0, button1, button2, button3 },
                new MessageFormButtonClickEventHandler[] { action0, action1, action2, action3 },
                icon);
        }

        public static void Show(string message, string caption,
            string button0, MessageFormButtonClickEventHandler action0,
            string button1, MessageFormButtonClickEventHandler action1,
            string button2, MessageFormButtonClickEventHandler action2,
            string button3, MessageFormButtonClickEventHandler action3,
            string button4, MessageFormButtonClickEventHandler action4,
            MessageBoxIcon icon)
        {
            show(null, message, caption,
                new string[] { button0, button1, button2, button3, button4 },
                new MessageFormButtonClickEventHandler[] { action0, action1, action2, action3, action4 },
                icon);
        }
        */

        private static int Show(Control owner, string message, string caption, string[] buttons, MessageBoxIcon icon)
        {
            var dummy = false;
            return Show(owner, message, caption, null, ref dummy, buttons, icon);
        }

        private static int Show(Control owner, string message, string caption, string checkBox, ref bool checkBoxChecked, string[] buttons, MessageBoxIcon icon)
        {
            var result = -1;
            MessageFormButtonClickEventHandler action = (sender, e) => { result = e.Index; sender.Close(); };
            if (buttons == null || !buttons.Any()) buttons = new string[1] { "" };
            var actions = new MessageFormButtonClickEventHandler[buttons.Length];
            for (var i = 0; i < actions.Length; i++) actions[i] = action;
            Show(owner, message, caption, checkBox, ref checkBoxChecked, buttons, actions, icon);
            return result;
        }
        
        private static void Show(Control owner, string message, string caption, string[] buttons, MessageFormButtonClickEventHandler[] actions, MessageBoxIcon icon)
        {
            var dummy = false;
            Show(owner, message, caption, null, ref dummy, buttons, actions, icon);
        }

        private static void Show(Control owner, string message, string caption, string checkBox, ref bool checkBoxChecked, string[] buttons, MessageFormButtonClickEventHandler[] actions, MessageBoxIcon icon)
        {
            var iconData = GetSystemIcon(icon);

            if (message == null) message = "";

            if (buttons == null || !buttons.Any()) buttons = new string[1] { "" };
            
            var font = SystemFonts.MessageBoxFont;
            
            using (var form = new Form())
            {
                float dpiX, dpiY;
                var len1 = buttons.Length - 1;
                using (var g = form.CreateGraphics())
                {
                    dpiX = g.DpiX;
                    dpiY = g.DpiY;
                }

                form.FormBorderStyle = FormBorderStyle.FixedSingle;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                form.ShowIcon = false;

                PreviewKeyDownEventHandler previewKeyDown = (sender, e) =>
                {
                    if (e.KeyCode == Keys.Escape)
                    {
                        form.Close();
                    }
                };
                form.PreviewKeyDown += previewKeyDown;

                var right = 500;
                var bottom = 500;
                form.ClientSize = new Size(right, bottom);

                var buttonMarginX = (int)(dpiX * 8 / 96 + 0.5);
                var buttonRightMargin = (int)(dpiX * 11 / 96 + 0.5);
                var buttonButtomMarginY = (int)(dpiY * 11 / 96 + 0.5);
                right -= buttonRightMargin;
                var buttonMinimumSize = new Size((int)(dpiX * 88 / 96 + 0.5), (int)(dpiY * 26 / 96 + 0.5));
                var buttonArray = new Button[buttons.Length];
                int buttonAreaLeft = 0, buttonAreaTop = 0;
                for (var i = 0; i <= len1; i++)
                {
                    var button = new Button();
                    buttonArray[i] = button;
                    button.Font = font;
                    button.PreviewKeyDown += previewKeyDown;
                    button.MinimumSize = buttonMinimumSize;
                    form.Controls.Add(button);
                    button.AutoSize = true;
                    var text = buttons[i];
                    if (text.StartsWith(ShieldPrefix))
                    {
                        text = text.Substring(ShieldPrefix.Length);
                        SetShieldIcon(button, true);
                    }
                    button.Text = text;
                    button.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
                    button.Location = new Point(right = right - buttonMarginX - button.Width, bottom - buttonButtomMarginY - button.Height);
                    var i0 = i;
                    var action = actions != null && actions.Length > i ? actions[i] : null;
                    button.Click += (sender, e) =>
                    {
                        if (action == null)
                        {
                            form.Close();
                        }
                        else
                        {
                            action(form, new MessageFormButtonClickEventArgs(i0));
                        }
                    };

                    if (i == len1)
                    {
                        buttonAreaLeft = (int)(button.Left - buttonMarginX - dpiX * 24 / 96 + 0.5);
                        buttonAreaTop = (int)(button.Top - dpiY * 12 / 96 + 0.5);
                    }
                }

                // 逆順で form.Controls.Add するとフォーカスの移動順が不自然になるため正順で追加した後で再配置する
                var left = buttonArray[len1].Left;
                for (var i = 0; i <= len1; i++)
                {
                    var button = buttonArray[i];
                    button.Left = left;
                    left += button.Width + buttonMarginX;
                }
                
                Rectangle workingArea;
                if (owner == null)
                {
                    workingArea = Screen.PrimaryScreen.WorkingArea;
                }
                else
                {
                    //workingArea = Screen.FromRectangle(owner.DisplayRectangle).WorkingArea;
                    workingArea = Screen.FromControl(owner).WorkingArea;
                    if (owner is Form ownerForm && ownerForm.TopMost)
                    {
                        form.TopMost = true;
                    }
                }

                var currentClientSize = form.ClientSize;
                var buttonAreaHeight = currentClientSize.Height - buttonAreaTop;
                var maxClientSize = new Size(workingArea.Width - form.Width + currentClientSize.Width, workingArea.Height - form.Height + currentClientSize.Height);

                CheckBox checkBoxControl;
                int checkBoxRight;
                if (checkBox == null)
                {
                    checkBoxControl = null;
                    checkBoxRight = 0;
                }
                else
                {
                    checkBoxControl = new CheckBox()
                    {
                        Font = font,
                        Checked = checkBoxChecked
                    };
                    form.Controls.Add(checkBoxControl);
                    checkBoxControl.AutoSize = true;
                    checkBoxControl.Text = checkBox;
                    checkBoxControl.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
                    var checkBoxTopMargin = (buttonAreaHeight - checkBoxControl.Height) / 2;
                    var checkBoxLeftMargin = checkBoxTopMargin;
                    checkBoxControl.Location = new Point(checkBoxLeftMargin, buttonAreaTop + checkBoxTopMargin);
                    checkBoxRight = checkBoxControl.Right;
                }

                var textLeftMargin = (int)(dpiX * 9 / 96 + 0.5);
                var textRightMargin = (int)(dpiX * 35 / 96 + 0.5);
                var textTopMargin = (int)(dpiY * 26 / 96 + 0.5);
                var textButtomMargin = (int)(dpiY * 26 / 96 + 0.5);

                int iconLeft, iconTop, iconAreaRight, iconAreaBottom;
                if (iconData == null)
                {
                    iconLeft = 0;
                    iconTop = 0;
                    iconAreaRight = 0;
                    iconAreaBottom = 0;
                }
                else
                {
                    iconLeft = (int)(dpiX * 26 / 96 + 0.5);
                    iconTop = (int)(textTopMargin - (iconData.Height - font.Height) / 2.0 + 0.5);
                    iconAreaRight = (int)(iconLeft + iconData.Width - dpiX * 2 / 96 + 0.5);
                    iconAreaBottom = iconTop + iconData.Height + (int)(dpiY * 36 / 96 + 0.5);
                }

                var minIconTop = (int)(dpiY * 27 / 96 + 0.5);
                if (iconTop < minIconTop)
                {
                    textTopMargin += minIconTop - iconTop;
                    iconTop = minIconTop;
                }

                var maxMessageSize = new Size(maxClientSize.Width - iconAreaRight - textLeftMargin - textRightMargin, maxClientSize.Height - textTopMargin - textButtomMargin - buttonAreaHeight);
                var label = new LinkLabel()
                {
                    Font = font,
                    BackColor = Color.Transparent,
                    Size = Size.Empty,
                    AutoSize = true,
                    MaximumSize = maxMessageSize
                };
                label.LinkClicked += LinkLabel_LinkClicked;
                SetText(label, message);

                form.Controls.Add(label);

                var messageSize = label.Size;

                var iconBottom = iconData != null ? iconTop + iconData.Height : iconTop;
                textTopMargin = Math.Min(textTopMargin, Math.Max((int)(iconBottom - (messageSize.Height + dpiY * 3 / 96)), (int)(iconTop - dpiY / 96)));

                var clientWidthFromButtons = currentClientSize.Width - buttonAreaLeft + checkBoxRight;
                var clientWidth = Math.Min(maxClientSize.Width, Math.Max(clientWidthFromButtons, iconAreaRight + textLeftMargin + textRightMargin + messageSize.Width));
                var buttonAfford = len1 == 0 ? 0 : (clientWidth - clientWidthFromButtons) / len1;
                if (buttonAfford > 0)
                {
                    buttonAfford = Math.Min(buttonAfford, (int)(dpiX * 2 / 96 + 0.5));
                    for (var i = 0; i < len1; i++)
                    {
                        buttonArray[i].Left -= (len1 - i) * buttonAfford;
                    }
                }
                buttonArray = null;
                var messageAreaHeight = Math.Max(iconAreaBottom, textTopMargin + messageSize.Height + textButtomMargin);
                var clientHeight = Math.Min(maxClientSize.Height, messageAreaHeight + currentClientSize.Height - buttonAreaTop);

                form.ClientSize = new Size(clientWidth, clientHeight);

                var buttonRegionRect = new Rectangle(0, 0, clientWidth, messageAreaHeight);
                var iconRect = iconData == null ? default(Rectangle) : new Rectangle(iconLeft, iconTop, iconData.Width, iconData.Height);
                var messageLocation = new Point(iconAreaRight + textLeftMargin, textTopMargin);

                form.Paint += (sender, e) =>
                {
                    if (e.ClipRectangle.IntersectsWith(buttonRegionRect)) e.Graphics.FillRectangle(SystemBrushes.Window, buttonRegionRect);
                    if (iconData != null && e.ClipRectangle.IntersectsWith(iconRect)) e.Graphics.DrawIcon(iconData, iconRect);
                };

                
                label.Location = messageLocation;
                
                form.Location = new Point(workingArea.X + (workingArea.Width - form.Width) / 2, workingArea.Y + (workingArea.Height - form.Height) / 2);

                form.Text = caption;
                
                PlaySystemSound(icon);
                form.ShowDialog(owner);

                if (checkBoxControl != null) checkBoxChecked = checkBoxControl.Checked;
            }
        }

        private static readonly Regex SetText_LinkPattern = new Regex(@"https?:\/\/[a-z_.\/]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SetText_MailPattern = new Regex(@"[a-z_]+@[a-z_.]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static void SetText(LinkLabel linkLabel, string text)
        {
            var links = linkLabel.Links;
            links.Clear();
            linkLabel.Text = text;
            foreach (Match m in SetText_LinkPattern.Matches(text))
            {
                links.Add(m.Index, m.Length, m.Value);
            }
            foreach (Match m in SetText_MailPattern.Matches(text))
            {
                links.Add(m.Index, m.Length, $"mailto:{m.Value}");
            }
        }

        private static void LinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var link = e.Link;
            link.Visited = true;
            try
            {
                Process.Start(link.LinkData.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show((sender as Control)?.FindForm(), ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void PlaySystemSound(MessageBoxIcon icon)
        {
            switch (icon)
            {
                case MessageBoxIcon.Asterisk: SystemSounds.Asterisk.Play(); break;
                case MessageBoxIcon.Hand: SystemSounds.Hand.Play(); break; // Hand == Error
                case MessageBoxIcon.Exclamation: SystemSounds.Exclamation.Play(); break;
                case MessageBoxIcon.Question: SystemSounds.Question.Play(); break;
            }
        }

        private static Icon GetSystemIcon(MessageBoxIcon icon)
        {
            switch (icon)
            {
                case MessageBoxIcon.Asterisk: return SystemIcons.Asterisk;
                case MessageBoxIcon.Error: return SystemIcons.Error;
                case MessageBoxIcon.Exclamation: return SystemIcons.Exclamation;
                case MessageBoxIcon.Question: return SystemIcons.Question;
                default: return null;
            }
        }
        
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        private const int BCM_FIRST = 0x1600;
        private const int BCM_SETSHIELD = BCM_FIRST + 0x000C;

        private static void SetShieldIcon(Button targetButton, bool showShield)
        {
            if (targetButton == null)
            {
                throw new ArgumentNullException("targetButton");
            }
            
            if (Environment.OSVersion.Platform != PlatformID.Win32NT ||
                Environment.OSVersion.Version.Major < 6)
            {
                return;
            }
            
            targetButton.FlatStyle = FlatStyle.System;
            
            SendMessage(new HandleRef(targetButton, targetButton.Handle),
                BCM_SETSHIELD,
                IntPtr.Zero,
                showShield ? new IntPtr(1) : IntPtr.Zero);
        }
    }
}
