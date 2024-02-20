using Alteridem.WinTouch;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
//using TouchLibrary;

namespace ZipPla
{
    public class ZipPlaAddressBarDirectoryButtonContextMenuStripOpeningEventArgs : EventArgs
    {
        public string Path;
        public ContextMenuStrip ContextMenuStrip;
        public bool IsDir, IsFile;
    }
    public delegate void ZipPlaAddressBarDirectoryButtonContextMenuStripOpeningEventHandler(
        ZipPlaAddressBar sender, ZipPlaAddressBarDirectoryButtonContextMenuStripOpeningEventArgs e);

    public enum ZipPlaAddressBarCommandSender { Program, TextChanged, Leave, MoveByButton }
    public class ZipPlaAddressBarMakeButtonStartingEventArgs : EventArgs
    {
        public ZipPlaAddressBarMakeButtonStartingEventArgs(string text, ZipPlaAddressBarCommandSender commandSender)
        {
            Text = text;
            this.commandSender = commandSender;
        }

        public string Text;
        private ZipPlaAddressBarCommandSender commandSender;
        public ZipPlaAddressBarCommandSender CommandSender
        {
            get { return commandSender; }
        }
    }
    public delegate void ZipPlaAddressBarMakeButtonStartingEventHandler(
        ZipPlaAddressBar sender, ZipPlaAddressBarMakeButtonStartingEventArgs e);

    public class ZipPlaAddressBarTextChangedByButtonEventArgs : EventArgs
    {
        private bool dropDown;
        public bool DropDown { get { return dropDown; } }

        public ZipPlaAddressBarTextChangedByButtonEventArgs(bool dropDown)
        {
            this.dropDown = dropDown;
        }
    }
    public delegate void ZipPlaAddressBarTextChangedByButtonEventHandler(
        ZipPlaAddressBar sender, ZipPlaAddressBarTextChangedByButtonEventArgs e);

    public class ZipPlaAddressBarDragEventArgs : DragEventArgs
    {
        private bool isDropDown;
        public bool IsDropDown { get { return isDropDown; } }
        private bool hasDropDown;
        public bool HasDropDown { get { return hasDropDown; } }
        private string path;
        public string Path { get { return path; } }

        public readonly Action FinallyAction;
        
        public ZipPlaAddressBarDragEventArgs(DragEventArgs e, string path, bool isDropDown, bool hasDropDown, Action finallyAction = null) : base(e.Data, e.KeyState, e.X, e.Y, e.AllowedEffect, e.Effect)
        {
            this.isDropDown = isDropDown;
            this.hasDropDown = hasDropDown;
            this.path = path;
            FinallyAction = finallyAction;
        }
        
        public void SetTo(DragEventArgs e)
        {
            e.Effect = Effect;
        }
    }
    public delegate void ZipPlaAddressBarDragEventHandler(
        ZipPlaAddressBar sender, ZipPlaAddressBarDragEventArgs e);


    public class ZipPlaAddressBar : System.ComponentModel.Component
    {
        //const int gap = 2;

        private readonly Form ownerForm;
        private readonly Action focusRobber;
        private readonly Control baseControl;
        private string text;
        private readonly List<Button> buttonList = new List<Button>();
        //private int nextDeleteIndex;
        
        private readonly HashSet<System.Windows.Forms.ContextMenuStrip> visibleContextMenuStripSet = new HashSet<System.Windows.Forms.ContextMenuStrip>();
        public System.Windows.Forms.ContextMenuStrip[] VisibleContextMenuStripSet { get { return visibleContextMenuStripSet.ToArray(); } }
        public bool VisibleContextMenuStripSetHasElement { get { return visibleContextMenuStripSet.Any(); } }

        public event ZipPlaAddressBarDirectoryButtonContextMenuStripOpeningEventHandler DirectoryButtonContextMenuStripOpening;

        public event EventHandler CurrentLocationButtonClick;
        public event ZipPlaAddressBarTextChangedByButtonEventHandler TextChangedByButton;
        public event EventHandler TextChanged;
        public event EventHandler TextChangedByBaseControl;
        public event EventHandler MouseEnter;
        public event EventHandler MouseLeave;
        
        public event ZipPlaAddressBarDragEventHandler SubControlDragEnter;
        public event ZipPlaAddressBarDragEventHandler SubControlDragDrop;

        public bool AllowDrop = false;

        public event MouseEventHandler DragDropStarting;
        public event MouseEventHandler DragDropFinished;

        public string Text
        {
            get { return text; }
            set
            {
                if (value != text) SetText(text: value, makeButtons: true, makeEvent: true);
            }
        }

        public void SetText(string text, bool makeButtons, bool makeEvent)
        {

            this.text = text;
            if (makeButtons)
            {
                if (!baseControl.Focused)
                {
                    makeButtonList(ZipPlaAddressBarCommandSender.TextChanged);
                }
                else
                {
                    clearButtons();
                    SetBaseControlText(text);
                }
            }
            if (makeEvent) TextChanged?.Invoke(this, null);
        }

        public void MakeButtons()
        {
            makeButtonList(ZipPlaAddressBarCommandSender.Program);
        }

       // LowLevelMouseHook.LowLevelMouseHook lowLevelMouseHook;

        public ZipPlaAddressBar(Form owner, Control textBox, Action focusRobber)
        {
            ownerForm = owner;
            baseControl = textBox;
            this.focusRobber = focusRobber;

            baseControl.Leave += BaseControl_Leave;
            baseControl.Enter += BaseControl_Enter;
            baseControl.SizeChanged += BaseControl_SizeChanged;
            baseControl.TextChanged += BaseControl_TextChanged;

            DragDropStarting += Self_DragDropStarting;
            DragDropFinished += Self_DragDropFinished;

            //lowLevelMouseHook = new LowLevelMouseHook.LowLevelMouseHook();
            //lowLevelMouseHook.MouseWheel += LowLevelMouseHook_MouseWheel;
            
        }

        // LowLevelMouseHook は安定しない＋エクスプローラーはドラッグ中のホイール操作を許さないのでそれに合わせる
        /*
        private ContextMenuStripEx LowLevelMouseHook_MouseWheel_DraggingInDropDown = null;
        private MouseEventArgs LowLevelMouseHook_MouseWheel_EventArgs = null;
        private System.Threading.Tasks.Task LowLevelMouseHook_MouseWheel_Task = null;
        private void LowLevelMouseHook_MouseWheel(object sender, MouseEventArgs e)
        {
            // このイベントハンドラが遅いと lowLevelMouseHook がタイムアウトして使えなくなるので
            if (
                LowLevelMouseHook_MouseWheel_DraggingInDropDown == null) return;

            if (LowLevelMouseHook_MouseWheel_Task == null)
            {
                LowLevelMouseHook_MouseWheel_Task = new System.Threading.Tasks.Task(LowLevelMouseHook_MouseWheel_Action);
            }
            else if (LowLevelMouseHook_MouseWheel_Task.Status == System.Threading.Tasks.TaskStatus.Running)
            {
                return;
            }
            else
            {
                LowLevelMouseHook_MouseWheel_Task.Dispose();
                LowLevelMouseHook_MouseWheel_Task = new System.Threading.Tasks.Task(LowLevelMouseHook_MouseWheel_Action);
            }

            LowLevelMouseHook_MouseWheel_EventArgs = e;

            LowLevelMouseHook_MouseWheel_Task.Start();
        }
        private void LowLevelMouseHook_MouseWheel_Action()
        {
            try
            {
                ownerForm?.Invoke(((MethodInvoker)(() =>
                {
                    // このイベントハンドラが遅いと lowLevelMouseHook がタイムアウトして使えなくなるので
                    if (
                LowLevelMouseHook_MouseWheel_EventArgs != null &&
                LowLevelMouseHook_MouseWheel_DraggingInDropDown != null &&
                !LowLevelMouseHook_MouseWheel_DraggingInDropDown.IsDisposed)
                    {
                        LowLevelMouseHook_MouseWheel_DraggingInDropDown.PerformScroll(LowLevelMouseHook_MouseWheel_EventArgs);
                    }
                })));
            }
            catch (ObjectDisposedException) { }

        }

        protected override void Dispose(bool disposing)
        {
            // デストラクタからの呼び出し (disposing == false) でも実行
            if (lowLevelMouseHook != null)
            {
                lowLevelMouseHook.Dispose();
                lowLevelMouseHook = null;
            }

            base.Dispose(disposing);
        }
        */
        
        bool dragOuting = false;
        public bool DragOuting { get { return dragOuting; } }

        private void Self_DragDropFinished(object sender, MouseEventArgs e)
        {
            dragOuting = false;
            AbsoluteNeverOpen = false;
        }

        private void Self_DragDropStarting(object sender, MouseEventArgs e)
        {
            dragOuting = true;
            AbsoluteNeverOpen = true;
        }

        public bool Enabled
        {
            get
            {
                return baseControl.Enabled;
            }
            set
            {
                baseControl.Enabled = value;
                refreshSeparatorButtonsEnabled();
            }
        }

        private void refreshSeparatorButtonsEnabled()
        {
            var value = baseControl.Enabled;
            var notFirst = false;
            foreach (var button in buttonList)
            {
                PushButton pushButton;
                if (notFirst && value && (pushButton = button as PushButton) != null)
                {
                    try
                    {
                        var path = getSeparatorButtonPath(button as PushButton);
                        button.Enabled = localHasAnyDirectory(path);
                    }
                    catch { }
                }
                else
                {
                    button.Enabled = value;
                }

                notFirst = true;
            }
        }

        private bool BaseControl_TextChanged_StopRead = false;
        private void BaseControl_TextChanged(object sender, EventArgs e)
        {
            if (!BaseControl_TextChanged_StopRead)
            {
                text = baseControl.Text;
                TextChangedByBaseControl?.Invoke(sender, e);
                TextChanged?.Invoke(sender, e);
            }
        }
        private void SetBaseControlText(string text)
        {
            if (baseControl.Text != text)
            {
                var temp = BaseControl_TextChanged_StopRead;
                BaseControl_TextChanged_StopRead = true;
                baseControl.Text = text;
                BaseControl_TextChanged_StopRead = temp;
            }
        }

        private void BaseControl_SizeChanged(object sender, EventArgs e)
        {
            fitVisibleButtonCount();
        }

        private void BaseControl_Enter(object sender, EventArgs e)
        {
            if (!BaseControl_TextChanged_StopRead)
            {
                clearButtons();
                SetBaseControlText(text);
            }
        }

        private void BaseControl_Leave(object sender, EventArgs e)
        {
            if (!BaseControl_TextChanged_StopRead)
            {
                makeButtonList(ZipPlaAddressBarCommandSender.Leave);
            }
        }

        public Func<string, bool> DirectoryExistsOverrider;
        private bool localDirectoryExists(string path)
        {
            if (DirectoryExistsOverrider == null)
            {
                return Directory.Exists(path);
            }
            else
            {
                return DirectoryExistsOverrider(path);
            }
        }
        /// <summary>
        /// 同時に HasAnyDirectoryOverrider も設定して下さい
        /// 監視には非対応です。真のフォルダがなくなった時点で Enabled = false になります。
        /// 利用する場合はその部分を修正して下さい
        /// </summary>
        public Func<string, string[]> GetDirectoriesOverrider;
        private string[] localGetDirectories(string path, bool isNRoot)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    var noDrives = GetNoDrives();
                    return Directory.GetLogicalDrives().Where(sub => (DriveToUInt32(sub) & noDrives) == 0).ToArray();
                }
                else
                {
                    if (path.Last() != Path.DirectorySeparatorChar) path += Path.DirectorySeparatorChar;
                    if (GetDirectoriesOverrider == null)
                    {
                        if (isNRoot)
                        {
                            return (from info in NetShare.GetNetShares(path) where isNetworkRoot(info) select path + info.shi1_netname).ToArray();
                        }
                        else
                        {
                            return Directory.GetDirectories(path);
                        }
                    }
                    else
                    {
                        return GetDirectoriesOverrider(path);
                    }
                }
            }
            catch
            {
                return new string[0];
            }
        }
        
        private static bool isNetworkRoot(NetShare.SHARE_INFO_1 info)
        {
            return info.shi1_type == 0;
        }

        private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
        private static bool isNetworkRoot(string path)
        {
            if (path == null || path.Length <= 2 || path[0] != Path.DirectorySeparatorChar || path[1] != Path.DirectorySeparatorChar) return false;

            var foundSeparator = false;
            for (var i = 2; i < path.Length; i++)
            {
                if (foundSeparator) return false;
                var c = path[i];
                if (c == Path.DirectorySeparatorChar)
                {
                    if (i > 2)
                    {
                        foundSeparator = true;
                    }
                    else return false;
                }
                else if (InvalidFileNameChars.Contains(c)) return false;
            }
            return true;
        }

        private static uint GetNoDrives()
        {
            const string CurrentUser = @"HKEY_CURRENT_USER";
            const string LocalMachine = @"HKEY_LOCAL_MACHINE";
            const string SubPath = @"\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer";
            const string Name = @"NoDrives";
            var u = unchecked((uint)RegistryGetValueWithoutException(CurrentUser + SubPath, Name, 0)); // コンパイラオプションによっては unchecked は不要
            var m = unchecked((uint)RegistryGetValueWithoutException(LocalMachine + SubPath, Name, 0)); // コンパイラオプションによっては unchecked は不要
            return u | m;
        }

        public static T RegistryGetValueWithoutException<T>(string keyName, string valueName, T defaultValue)
        {
            try
            {
                var value = Microsoft.Win32.Registry.GetValue(keyName, valueName, defaultValue);
                return value is T ? (T)value : defaultValue;
            }
            catch (System.Security.SecurityException) // ユーザーには、レジストリ キーからの読み取りに必要な権限がありません。
            {
                return defaultValue;
            }
            catch (IOException) // RegistryKey を含む、指定した値が削除対象としてマークされています。
            {
                return defaultValue;
            }
            catch (ArgumentException) // keyName 有効なレジストリ ルートで始まっていません。
            {
                return defaultValue;
            }
        }

        private static uint DriveToUInt32(string drivePath)
        {
            if (drivePath == null || !(drivePath.Length == 1 || drivePath.Length >= 2 && drivePath[1] == ':')) return 0;
            var c = drivePath[0];
            if ('A' <= c && c <= 'Z') return 1u << (c - 'A');
            else if ('a' <= c && c <= 'z') return 1u << (c - 'a');
            else return 0;
        }

        private static string addDirectorySeparatorChar(string path)
        {
            if (path == null || !path.Any()) return Path.DirectorySeparatorChar.ToString();
            if (path.Last() != Path.DirectorySeparatorChar) return path + Path.DirectorySeparatorChar;
            return path;
        }

        private static string removeDirectorySeparatorChar(string path)
        {
            if (path == null || !path.Any() || path.Last() != Path.DirectorySeparatorChar) return path;
            return path.Substring(0, path.Length - 1);
        }

        /// <summary>
        /// Directory.EnumerateXXX(path).Any(condition) の形の実装を推奨します。
        /// 同時に GetDirectoriesOverrider も設定して下さい。
        /// フォルダ監視には非対応です。フォルダがなくなった時点で Enabled = false になります。
        /// 利用する場合はその部分を修正して下さい
        /// </summary>
        public Func<string, bool> HasAnyDirectoryOverrider;
        private bool localHasAnyDirectory(string path)
        {
            try
            {
                if(HasAnyDirectoryOverrider == null)
                {
                    if (isNetworkRoot(path))
                    {
                        return NetShare.GetNetShares(path).Any(info => isNetworkRoot(info) && !NetworkDirectoryShouldBeHidden(info.shi1_netname));
                    }
                    else
                    {
                        return Directory.EnumerateDirectories(path).Any(dir => !DirectoryShouldBeHidden(dir));
                    }
                }
                else
                {
                    return HasAnyDirectoryOverrider(path);
                }
            }
            catch
            {
                return false;
            }
        }

        public event ZipPlaAddressBarMakeButtonStartingEventHandler ZipPlaAddressBarMakeButtonStarting;

        //private Bitmap RightTriangle = null;
        //private Bitmap DownTriangle = null;
        private int prevButtonHeight = 0;
        private void makeButtonList(ZipPlaAddressBarCommandSender commandSender)
        {
            if (!ownerForm.Visible)
            {
                EventHandler eh = null;
                eh = (sender, e) =>
                {
                    makeButtonList(commandSender);
                    ownerForm.Shown -= eh;
                };
                ownerForm.Shown += eh;
                return;
            }

            clearButtons();

            try
            {
                if (text == null) text = "";
                if (ZipPlaAddressBarMakeButtonStarting != null)
                {
                    var e = new ZipPlaAddressBarMakeButtonStartingEventArgs(text, commandSender);
                    ZipPlaAddressBarMakeButtonStarting(this, e);
                    text = e.Text;
                    if (text == null) text = "";
                }
                //else
                if (text == "" || localDirectoryExists(text))
                {
                    SetBaseControlText("");
                }
                else
                {
                    throw new DirectoryNotFoundException();
                }
            }
            catch
            {
                SetBaseControlText(text);
                return;
            }

            var location0 = baseControl.Location;
            var location = location0;
            var y = location.Y;
            var height = baseControl.Height;
            //location.X += 1;
            var splited = text == null ? new string[0] : text.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.None);
            if(text.StartsWith(@"\\"))
            {
                var newSprited = new string[splited.Length - 2];
                Array.Copy(splited, 3, newSprited, 1, splited.Length - 3);
                newSprited[0] = @"\\" + splited[2];
                splited = newSprited;
            }
            var splitedLastIndex = splited.Length - 1;
            var enabled = Enabled;

            //ownerForm.SuspendLayout();

            buttonList.Add(null); // 先頭のボタン用
            var buttonHeight = prevButtonHeight;

            var path = "";
            var fileSystemWatcherUseless = false;
            var allowDrop = AllowDrop && SubControlDragEnter != null && SubControlDragDrop != null;
            for (var i = 0; i <= splitedLastIndex; i++)
            {
                var directory = splited[i];
                path += directory + Path.DirectorySeparatorChar;

                if (string.IsNullOrEmpty(directory)) break;

                var button = new DirectoryButton(ownerForm);
                button.FlatStyle = FlatStyle.Popup;
                button.BackColor = SystemColors.Window;
                button.Size = new Size();
                button.AutoSize = true;
                button.Path = path;
                button.TextAlign = ContentAlignment.MiddleLeft;
                button.Text = getDisplayName(path);

                button.Location = location;

                if (!directory.StartsWith(@"\\"))
                {
                    if (i < splitedLastIndex) button.Click += directoryButton_Click;
                    else button.Click += currentLocationButtonClick;
                }
                button.MouseDown += Button_MouseDown;
                button.MouseLeave += Button_MouseLeave;
                button.MouseUp += Button_MouseUp;
                if (allowDrop)
                {
                    button.AllowDrop = true;
                    button.DragEnter += Button_DragEnter;
                    button.DragLeave += Button_ColorReset;
                    button.MouseLeave += Button_ColorReset;
                    button.DragDrop += Button_DragDrop;
                }
                ownerForm.Controls.Add(button);
                buttonHeight = button.Height;
                button.FullWidth = button.Width;

                if (i == 0)
                {
                    // 先頭分
                    location.X += buttonHeight * 3 / 4;
                    button.Location = new Point(button.Location.X + buttonHeight * 3 / 4, y - (button.Height - height) / 2);
                }
                else
                {
                    button.Location = new Point(button.Location.X, y - (button.Height - height) / 2);
                }
                button.BringToFront();
                location.X += button.Width;
                if (!enabled) button.Enabled = false;
                if (MouseEnter != null) button.MouseEnter += MouseEnter;
                if (MouseLeave != null) button.MouseLeave += MouseLeave;

                button.DragDropStarting += DragDropStarting;
                button.DragDropFinished += DragDropFinished;
                buttonList.Add(button);
                
                var separatorButton = new PushButton();
                separatorButton.FlatStyle = FlatStyle.Popup;
                separatorButton.BackColor = Color.FromKnownColor(KnownColor.Control);
                //separatorButton.Size = new Size((10 * buttonHeight + 1) / 14, buttonHeight);
                separatorButton.Size = new Size(buttonHeight * 3 / 4, buttonHeight);
                //if (RightTriangle == null) RightTriangle = getTriangle(separatorButton.ClientSize, Direction.Right);
                //separatorButton.ImageAlign = ContentAlignment.MiddleCenter;
                //separatorButton.Image = RightTriangle;
                separatorButton.Paint += separatorButton_Paint;
                separatorButton.Location = location;
                separatorButton.Click += separatorButton_Click;
                if (allowDrop)
                {
                    separatorButton.AllowDrop = true;
                    separatorButton.DragEnter += separatorButton_DragEnter;
                }
                ownerForm.Controls.Add(separatorButton);
                separatorButton.Location = new Point(separatorButton.Location.X, y - (separatorButton.Height - height) / 2);
                separatorButton.BringToFront();
                location.X += separatorButton.Width;
                if (!enabled) separatorButton.Enabled = false;
                if (MouseEnter != null) separatorButton.MouseEnter += MouseEnter;
                if (MouseLeave != null) separatorButton.MouseLeave += MouseLeave;
                
                if (!fileSystemWatcherUseless)
                {
                    var fileSystemWatcher = new FileSystemWatcher();
                    try
                    {
                        if (!localHasAnyDirectory(path))
                        {
                            separatorButton.Enabled = false;
                        }

                        fileSystemWatcher.Path = path;
                        fileSystemWatcher.NotifyFilter = NotifyFilters.DirectoryName;
                        fileSystemWatcher.IncludeSubdirectories = false;
                        fileSystemWatcher.SynchronizingObject = ownerForm;
                        fileSystemWatcher.Created += (sender, e) =>
                        {
                            if (!separatorButton.Enabled && Enabled)
                            {
                                separatorButton.Enabled = true;
                            }
                        };
                        fileSystemWatcher.Deleted += (sender, e) =>
                        {
                            if (separatorButton.Enabled)
                            {
                                var empty = false;
                                try
                                {
                                    empty = !localHasAnyDirectory((sender as FileSystemWatcher).Path);
                                }
                                catch { }
                                if (empty)
                                {
                                    separatorButton.Enabled = false;
                                }
                            }
                        };
                        fileSystemWatcher.EnableRaisingEvents = true;

                        separatorButton.Disposed += (sender, e) =>
                        {
                            if (fileSystemWatcher != null)
                            {
                                fileSystemWatcher.EnableRaisingEvents = false;
                                fileSystemWatcher.Dispose();
                                fileSystemWatcher = null;
                            }
                        };
                    }
                    catch
                    {
                        fileSystemWatcher.EnableRaisingEvents = false;
                        fileSystemWatcherUseless = true;
                        fileSystemWatcher.Dispose();
                    }
                }
                
                buttonList.Add(separatorButton);
                
            }
            //ownerForm.ResumeLayout();

            // 先頭
            {
                var separatorButton = new PushButton();
                separatorButton.FlatStyle = FlatStyle.Popup;
                separatorButton.BackColor = Color.FromKnownColor(KnownColor.Control);
                if (buttonHeight > 0)
                {
                    separatorButton.Size = new Size(buttonHeight * 3 / 4, buttonHeight);
                }
                separatorButton.Paint += separatorButton_Paint;
                separatorButton.Location = location0;
                separatorButton.Click += separatorButton_Click;
                if (allowDrop)
                {
                    separatorButton.AllowDrop = true;
                    separatorButton.DragEnter += separatorButton_DragEnter;
                }
                ownerForm.Controls.Add(separatorButton);
                if (buttonHeight == 0)
                {
                    separatorButton.AutoSize = true;
                    separatorButton.Text = " ";
                    buttonHeight = separatorButton.Height;
                    separatorButton.AutoSize = false;
                    separatorButton.Text = null;
                    separatorButton.Size = new Size(buttonHeight * 3 / 4, buttonHeight);
                }
                prevButtonHeight = buttonHeight;
                separatorButton.Location = new Point(separatorButton.Location.X, y - (separatorButton.Height - height) / 2);
                separatorButton.BringToFront();
                if (!enabled) separatorButton.Enabled = false;
                if (MouseEnter != null) separatorButton.MouseEnter += MouseEnter;
                if (MouseLeave != null) separatorButton.MouseLeave += MouseLeave;
                
                buttonList[0] = separatorButton;
            }
            

            //nextDeleteIndex = 0;
            fitVisibleButtonCount();
        }

        private void Button_DragDrop(object sender, DragEventArgs e)
        {
            var button = (DirectoryButton)sender;
            var e2 = new ZipPlaAddressBarDragEventArgs(e, removeDirectorySeparatorChar(button.Path), isDropDown: false, hasDropDown: false, finallyAction: () =>
            {
                Button_ColorReset_Stop = false;
                resetColor(button);
            });
            foreach (var cms in VisibleContextMenuStripSet) cms.Close();
            SubControlDragDrop?.Invoke(this, e2);
        }

        private void separatorButton_DragEnter(object sender, DragEventArgs e)
        {
            if (dragOuting) return;

            Button_ColorReset_Stop = false;

            var path = getSeparatorButtonPath(sender as PushButton);
            if (path == null) path = "";
            var e2 = new ZipPlaAddressBarDragEventArgs(e, removeDirectorySeparatorChar(path), isDropDown: false, hasDropDown: true);
            SubControlDragEnter?.Invoke(this, e2);
            if (e2.Effect == DragDropEffects.Link) e2.Effect = DragDropEffects.Move;
            e2.SetTo(e);
            if (e2.Effect != DragDropEffects.None)
            {
                var button = (PushButton)sender;
                button.PerformClick();
            }
        }

        private void Button_DragEnter(object sender, DragEventArgs e)
        {
            if (dragOuting) return;

            var button = (DirectoryButton)sender;
            var buttonIndex = buttonList.IndexOf(button);
            if (buttonIndex <= 0 || (buttonList[buttonIndex - 1] as PushButton)?.Pushed != true)
            {
                foreach (var cms in VisibleContextMenuStripSet)
                {
                    cms.Close();
                }
            }

            var e2 = new ZipPlaAddressBarDragEventArgs(e, removeDirectorySeparatorChar(button.Path), isDropDown: false, hasDropDown: false);
            SubControlDragEnter?.Invoke(this, e2);
            if (e2.Effect == DragDropEffects.Link) e2.Effect = DragDropEffects.None;
            e2.SetTo(e);
            if (e2.Effect != DragDropEffects.None)
            {
                button.BackColor = SystemColors.Highlight;
                button.ForeColor = SystemColors.HighlightText;
            }
        }

        private bool Button_ColorReset_Stop = false;
        private void Button_ColorReset(object sender, EventArgs e)
        {
            if (Button_ColorReset_Stop || Control.MouseButtons == MouseButtons.None) return;
            var button = (DirectoryButton)sender;
            resetColor(button);
        }

        private static void resetColor(Control control)
        {
            // ボタンのデフォルト値であると確認済み
            try
            {
                control.BackColor = SystemColors.Window;
                control.ForeColor = SystemColors.ControlText;
            }
            catch { } // ObjectDisposedException を想定
        }

        private class DirectoryButton : Button
        {
            public int FullWidth;

            private string text;
            public new string Text
            {
                get => text;
                set
                {
                    base.Text = text = value;
                }
            }
            public new Size Size
            {
                set
                {
                    base.Size = value;
                    var width = value.Width;
                    if (width < FullWidth && !string.IsNullOrEmpty(text))
                    {
                        var font = Font ?? DefaultFont;
                        var size = new Size(int.MaxValue, int.MinValue);
                        var textWidth = TextRenderer.MeasureText(text, font, size, TextFormatFlags.NoPrefix).Width;
                        var goal = textWidth - (FullWidth - width);
                        var a = 0;
                        var b = text.Length;
                        var aText = "...";
                        var aWidth = TextRenderer.MeasureText(aText, font, size, TextFormatFlags.NoPrefix).Width;
                        if (aWidth > goal)
                        {
                            base.Text = "...";
                            return;
                        }
                        var bWidth = TextRenderer.MeasureText(text + "...", font, size, TextFormatFlags.NoPrefix).Width;
                        if (bWidth <= goal)
                        {
                            base.Text = text;
                            return;
                        }
                        while (a + 1 < b)
                        {
                            var c = Math.Max(a + 1, ((bWidth - goal) * a + (goal - aWidth) * b) / (bWidth - aWidth));
                            var cText = text.Substring(0, c) + "...";
                            var cWidth = TextRenderer.MeasureText(cText, font, size, TextFormatFlags.NoPrefix).Width;
                            if (cWidth <= goal)
                            {
                                a = c;
                                aWidth = cWidth;
                                aText = cText;
                            }
                            else
                            {
                                b = c;
                                bWidth = cWidth;
                            }
                        }
                        base.Text = aText;
                    }
                    else
                    {
                        base.Text = text;
                    }
                }
            }

            public string Path;
            private Point? MouseDownPoint = null;
            private Form baseForm;

            public event MouseEventHandler DragDropStarting;
            public event MouseEventHandler DragDropFinished;

            public DirectoryButton(Form baseForm)
            {
                this.baseForm = baseForm;
                UseMnemonic = false;
            } 

            protected override void OnMouseDown(MouseEventArgs mevent)
            {
                if (mevent.Button == MouseButtons.Left || mevent.Button == MouseButtons.Right) MouseDownPoint = mevent.Location;
                else MouseDownPoint = null;
                base.OnMouseDown(mevent);
            }

            protected override void OnMouseUp(MouseEventArgs mevent)
            {
                MouseDownPoint = null;
                base.OnMouseUp(mevent);
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                // MouseDownPoint = null;


                if (MouseDownPoint != null && Path != null)
                {
                    MouseDownPoint = null;
                    try
                    {
                        string path;
                        var len = Path.Length;
                        if (len >= 2 && Path[len - 2] == ':')
                        {
                            path = Path;
                        }
                        else
                        {
                            path = removeDirectorySeparatorChar(Path);
                        }

                        var dataObject = new DataObject(DataFormats.FileDrop, new string[1] { path });
                        //DragDropStarting?.Invoke(this, mevent);
                        DragDropStarting?.Invoke(this, null);
                        try
                        {
                            DoDragDrop(dataObject, DragDropEffects.Copy | DragDropEffects.Link);
                        }
                        finally
                        {
                            //DragDropFinished?.Invoke(this, mevent);
                            DragDropFinished?.Invoke(this, null);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(baseForm, ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }


                base.OnMouseLeave(e);
            }

            protected override void OnMouseMove(MouseEventArgs mevent)
            {
                if (MouseDownPoint != null && Path != null && !ClientRectangle.Contains(mevent.Location))
                {
                    MouseDownPoint = null;
                    try
                    {
                        string path;
                        var len = Path.Length;
                        if (len >= 2 && Path[len - 2] == ':')
                        {
                            path = Path;
                        }
                        else
                        {
                            path = removeDirectorySeparatorChar(Path);
                        }

                        var dataObject = new DataObject(DataFormats.FileDrop, new string[1] { path });
                        DragDropStarting?.Invoke(this, mevent);
                        try
                        {
                            DoDragDrop(dataObject, DragDropEffects.Copy | DragDropEffects.Link);
                        }
                        finally
                        {
                            DragDropFinished?.Invoke(this, mevent);
                        }
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show(baseForm, ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                base.OnMouseMove(mevent);
            }

            protected override void OnQueryContinueDrag(QueryContinueDragEventArgs qcdevent)
            {
                if ((qcdevent.KeyState & ((int)(Keys.LButton | Keys.RButton))) == ((int)(Keys.LButton | Keys.RButton)))
                {
                    qcdevent.Action = DragAction.Cancel;
                }

                base.OnQueryContinueDrag(qcdevent);
            }
        }

        public Func<string, string> GetDisplayNameOverrider;
        private string getDisplayName(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            path = path.Substring(0, path.Length - 1);
            var result = null as string;
            if (GetDisplayNameOverrider != null)
            {
                try
                {
                    result = GetDisplayNameOverrider(path);
                }
                catch { }
            }
            if (result == null)
            {
                if (path.Length > 0 && path.Last() == ':') result = path;
                else
                {
                    try
                    {
                        result = Path.GetFileName(path);
                    }
                    catch
                    {
                        result = path;
                    }
                }
            }
            return result;
        }

        private void currentLocationButtonClick(object sender, EventArgs e)
        {
            CurrentLocationButtonClick?.Invoke(this, e);
        }

        private class PushButton : Button
        {
            private bool pushed = false;
            public bool Pushed
            {
                get { return pushed; }
                set
                {
                    if (value != pushed)
                    {
                        pushedOnPushed = pushed && ClientRectangle.Contains(PointToClient(Cursor.Position)) && MouseButtons == MouseButtons.Left;
                        pushed = value;
                        Invalidate();
                        //Refresh();
                    }
                }
            }

            private bool pushedOnPushed = false;
            
            protected override void OnMouseLeave(EventArgs e)
            {
                pushedOnPushed = false;
                base.OnMouseLeave(e);
            }

            protected override void OnMouseUp(MouseEventArgs mevent)
            {
                if (mevent.Button != MouseButtons.Left) pushedOnPushed = false;
                else if (pushed)
                {
                    pushedOnPushed = true;
                }
                base.OnMouseUp(mevent);
            }

            protected override void OnKeyUp(KeyEventArgs kevent)
            {
                pushedOnPushed = false;
                base.OnKeyUp(kevent);
            }

            protected override void OnClick(EventArgs e)
            {
                if (pushedOnPushed)
                {
                    pushedOnPushed = false;
                }
                else
                {
                    base.OnClick(e);
                }
            }
        }

        private static readonly PointF[] separatorButton_Paint_Right =
            new PointF[] { new PointF(0.7f, 1 / 2f), new PointF(0.4f, 1 / 3f), new PointF(0.4f, 2 / 3f) };
        private static readonly PointF[] separatorButton_Paint_Down =
            new PointF[] { new PointF(0.5f, 0.5f + 9 / 80f), new PointF(5 / 18f, 0.5f - 9 / 80f), new PointF( 13 / 18f, 0.5f - 9 / 80f) };
        private static void separatorButton_Paint(object sender, PaintEventArgs e)
        {
            var button = sender as PushButton; if (button == null) return;
            FullScaledFillPolygon(button, e, button.Pushed ? separatorButton_Paint_Down : separatorButton_Paint_Right);

        }

        private static void FullScaledFillPolygon(Control control, PaintEventArgs e,  IEnumerable<PointF> scaledPoints)
        {
            if (e == null || scaledPoints == null) return;
            var g = e.Graphics;
            var b = control.Enabled ? SystemBrushes.ControlText : Brushes.LightGray; //SystemBrushes.GrayText;
            var rect = control.ClientRectangle;
            int x = rect.X, y = rect.Y, w = rect.Width, h = rect.Height;
            var points = (from p in scaledPoints select new PointF(x + p.X * w - 1, y + p.Y * h - 1)).ToArray();
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.FillPolygon(b, points);
        }


        private enum Direction { Right, Down };
        private static Bitmap getTriangle(Size size, Direction direction)
        {
            const int a = 3;
            var w = a * size.Width;
            var h = a * size.Height;
            PointF[] vertices;
            switch(direction)
            {
                case Direction.Right:
                    vertices = new PointF[] { new PointF(3 / 5f * w - 1 + 1f / 12, 1 / 2f * h - 1), new PointF(2 / 5f * w - 1 + 1f / 12, 1 / 3f * h - 1), new PointF(2 / 5f * w - 1 + 1f / 12, 2 / 3f * h - 1) };
                    break;
                case Direction.Down:
                    vertices = new PointF[] { new PointF(1 / 2f * w - 1, 3 / 5f * h - 1 + 1f / 12), new PointF(1 / 3f * w - 1, 2 / 5f * h - 1 + 1f / 12), new PointF(2 / 3f * w - 1, 2 / 5f * h - 1 + 1f / 12) };
                    break;
                default: throw new NotImplementedException();
            }
            using (var aresult = new Bitmap(w, h))
            {
                using (var g = Graphics.FromImage(aresult))
                {
                    g.FillPolygon(Brushes.Black, vertices);
                }
                var result = new Bitmap(w / a, h / a);
                using (var g = Graphics.FromImage(result))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
                    g.DrawImage(aresult, new RectangleF(-1.5f + 1f/(a), -1.5f + 1f / (a), w / a, w / a));
                }
                return result;
            }
        }

        private object Button_MouseRButtonDownInfo = null;
        private void Button_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Button_MouseRButtonDownInfo = sender;

                if (Button_ShowCms_ContextMenuStrip == null || !Button_ShowCms_ContextMenuStrip.Visible)
                {
                    var dropDownItem = sender as ZipPlaAddresBarToolStripMenuItem;
                    if (dropDownItem != null)
                    {
                        var cms = dropDownItem.Owner as ContextMenuStripEx;
                        if (cms != null) cms.AutoClose = false;
                    }
                }
                //focusRobber();
            }
        }

        private void Button_MouseUp(object sender, MouseEventArgs e)
        {
            if (Button_ShowCms_ContextMenuStrip == null || !Button_ShowCms_ContextMenuStrip.Visible)
            {
                var dropDownItem = sender as ZipPlaAddresBarToolStripMenuItem;
                if (dropDownItem != null)
                {
                    var cms = dropDownItem.Owner as ContextMenuStripEx;
                    if (cms != null) cms.AutoClose = true;
                }
            }

            if (sender == Button_MouseRButtonDownInfo)
            {
                Button_ShowCms(sender, e);
                Button_MouseRButtonDownInfo = null;
            }
            else
            {
                Button_MouseRButtonDownInfo = null;
            }
        }

        private void Button_MouseLeave(object sender, EventArgs e)
        {
            Button_MouseRButtonDownInfo = null;

            if (Button_ShowCms_ContextMenuStrip == null || !Button_ShowCms_ContextMenuStrip.Visible)
            {
                var dropDownItem = sender as ZipPlaAddresBarToolStripMenuItem;
                if (dropDownItem != null)
                {
                    var cms = dropDownItem.Owner as ContextMenuStripEx;
                    if (cms != null) cms.AutoClose = true;
                }
            }
        }


        private ContextMenuStrip Button_ShowCms_ContextMenuStrip = null;
        private ToolStripMenuItem Button_ShowCms_OpenInExplorer = null;
        private ToolStripMenuItem Button_ShowCms_CopyAddressAsText = null;
        private bool Button_ShowCms_IsDir = false;
        private bool Button_ShowCms_FromDropDown = false;
        private string Button_ShowCms_Path = null;
        private void Button_ShowCms(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (Button_ShowCms_ContextMenuStrip == null)
                {
                    Button_ShowCms_OpenInExplorer = new ToolStripMenuItem(Message.OpenInExplorer + "...");
                    Button_ShowCms_OpenInExplorer.Click += Button_MouseDown_OpenInExplorer_Click;
                    Button_ShowCms_CopyAddressAsText = new ToolStripMenuItem(Message.CopyAddressAsText);
                    Button_ShowCms_CopyAddressAsText.Click += Button_MouseDown_CopyAddressAsText_Click;

                    Button_ShowCms_ContextMenuStrip = new ContextMenuStripEx();
                    Button_ShowCms_ContextMenuStrip.Items.Add(Button_ShowCms_OpenInExplorer);
                    Button_ShowCms_ContextMenuStrip.Items.Add(Button_ShowCms_CopyAddressAsText);
                    Button_ShowCms_ContextMenuStrip.Closed += Button_MouseDown_ContextMenuStrip_Closed;
                }
                else
                {
                    Button_ShowCms_OpenInExplorer.Text = Message.OpenInExplorer + "...";
                    Button_ShowCms_CopyAddressAsText.Text = Message.CopyAddressAsText;
                }

                var directoryButton = sender as DirectoryButton;
                Button_ShowCms_FromDropDown = directoryButton == null;
                ZipPlaAddresBarToolStripMenuItem dropDownItem;
                if (!Button_ShowCms_FromDropDown)
                {
                    Button_ShowCms_Path = removeDirectorySeparatorChar(directoryButton.Path);
                    dropDownItem = null;
                }
                else
                {
                    dropDownItem = sender as ZipPlaAddresBarToolStripMenuItem;
                    Button_ShowCms_Path = removeDirectorySeparatorChar(dropDownItem.Path);
                }

                bool fileExists;
                try
                {
                    Button_ShowCms_IsDir = Directory.Exists(Button_ShowCms_Path);
                    fileExists = File.Exists(Button_ShowCms_Path);
                    Button_ShowCms_OpenInExplorer.Enabled = Button_ShowCms_IsDir || fileExists;
                }
                catch
                {
                    Button_ShowCms_OpenInExplorer.Enabled = false;
                    fileExists = false;
                }
                
                
                // visible に追加する前に
                if (Button_ShowCms_FromDropDown)
                {

                    AbsoluteNeverOpen = true;

                    if (dropDownItem.HasDropDown)
                    {
                        dropDownItem.DropDown.Close();
                    }

                    foreach (var cms2 in VisibleContextMenuStripSet)
                    {
                        // AutoClose を false にした後、他の ContextMenuStrip を開くと
                        // その後 AutoClose = true が無効になり Leave イベントも発生しないため
                        // この時点で cms2 の役割は終わり、表示を残すだけの目的で以下のように処理する
                        cms2.AutoClose = false;
                        //BeginControlUpdate(cms2);

                        //var owner = dropDownItem.Owner as ContextMenuStripEx;
                        var owner = cms2 as ContextMenuStripEx;
                        if (owner != null)
                        {
                            foreach (ZipPlaAddresBarToolStripMenuItem brother in owner.Items)
                            {
                                brother.DropDownNeverOpen = true;
                                brother.FixedSelect = brother == dropDownItem; // DragOut 抑止の効果を含む
                            }
                        }
                    }
                }
                
                visibleContextMenuStripSet.Add(Button_ShowCms_ContextMenuStrip);

                DirectoryButtonContextMenuStripOpening?.Invoke(this, new ZipPlaAddressBarDirectoryButtonContextMenuStripOpeningEventArgs()
                {
                    Path = Button_ShowCms_Path,
                    ContextMenuStrip = Button_ShowCms_ContextMenuStrip,
                    IsDir = Button_ShowCms_IsDir,
                    IsFile = fileExists
                });

                Button_ShowCms_ContextMenuStrip.Show(Cursor.Position);
                
            }
        }

        private bool AbsoluteNeverOpen = false;

        private void Button_MouseDown_ContextMenuStrip_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            visibleContextMenuStripSet.Remove(Button_ShowCms_ContextMenuStrip);

            // visible から削除した後で
            if (Button_ShowCms_FromDropDown)
            {
                foreach (var cms2 in VisibleContextMenuStripSet)
                {
                    //EndControlUpdate(cms2);
                    cms2.AutoClose = true;
                    cms2.Close();
                }
            }

            AbsoluteNeverOpen = false;
        }

        private void Button_MouseDown_OpenInExplorer_Click(object sender, EventArgs e)
        {
            if (Button_ShowCms_IsDir)
            {
                try
                {
                    System.Diagnostics.Process.Start(Button_ShowCms_Path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ownerForm, ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                try
                {
                    System.Diagnostics.Process.Start("EXPLORER.EXE", $"/select,\"{Button_ShowCms_Path}\"");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ownerForm, ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void Button_MouseDown_CopyAddressAsText_Click(object sender, EventArgs e)
        {
            try
            {
                var text = Button_ShowCms_Path.Last() == ':' ? Button_ShowCms_Path + Path.DirectorySeparatorChar : Button_ShowCms_Path;
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ownerForm, ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string getSeparatorButtonPath(PushButton button)
        {
            //var separator = false;
            var path = null as string;
            foreach (var b in buttonList)
            {
                /*
                if (separator)
                {
                    //path += Path.DirectorySeparatorChar;
                }
                else
                {
                    path += b.Text;
                }
                if (b == button)
                {
                    break;
                }
                separator = !separator;
                */
                var dirButton = b as DirectoryButton;
                if (dirButton != null) path = dirButton.Path;
                else if (b == button) break;
            }
            return path;
        }

        //private static readonly MethodInfo OnGiveFeedbackMethodInfo = typeof(Control).GetMethod("OnGiveFeedback", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[1] { typeof(GiveFeedbackEventArgs) }, null);

        private void separatorButton_Click(object sender, EventArgs e)
        {
            var button0 = sender as PushButton;
            var path = getSeparatorButtonPath(button0);
            var cms = new ContextMenuStripEx();
            cms.Opened += (sender2, e2) => (sender2 as ContextMenuStripEx).Focus();
            var index = buttonList.IndexOf(button0);
            string curPath = null;
            if (0 <= index && index < buttonList.Count - 1)
            {
                curPath = (buttonList[index + 1] as DirectoryButton).Path;
            }
            cms.Items.AddRange(getDirectorySeekingMenuItemArray2(path, curPath, sender, e, cms));
            //if (RightTriangle == null) RightTriangle = getTriangle(button0.Size, Direction.Right);
            //if (DownTriangle == null) DownTriangle = getTriangle(button0.Size, Direction.Down);
            cms.Opened += (sender2, e2) =>
            {
                //button0.Image = DownTriangle;
                button0.Pushed = true;
            };
            cms.Closing += (sender2, e2) =>
            {
                //button0.Image = RightTriangle; ;
                button0.Pushed = false;
            };
            if (AllowDrop && SubControlDragEnter != null && SubControlDragDrop != null)
            {
                cms.AllowDrop = true;
                cms.DragEnter += (sender2, e2) =>
                {
                    if (dragOuting)
                    {
                        e2.Effect = DragDropEffects.None;
                        return;
                    }
                    var e3 = new ZipPlaAddressBarDragEventArgs(e2, path, isDropDown: false, hasDropDown: false);
                    SubControlDragEnter?.Invoke(this, e3);
                    if (e3.Effect == DragDropEffects.Link) e3.Effect = DragDropEffects.None;
                    e3.SetTo(e2);

                    //LowLevelMouseHook_MouseWheel_DraggingInDropDown = cms;
                };

                /*
                cms.DragOver += (sender2, e2) =>
                {
                    e2.Effect = e2.Effect == DragDropEffects.Move ? DragDropEffects.Copy : DragDropEffects.Move;
                };
                */

                cms.DragOver += DropDown_DragOver;
                cms.DragDrop += DropDown_DragDrop;
                cms.DragLeave += DropDown_DragLeave;
                
            }

            var maxSize = cms.MaximumSize;
            var area = Screen.FromControl(button0).WorkingArea;
            var buttonBound = ownerForm.RectangleToScreen(button0.Bounds);
            cms.MaximumSize = new Size(maxSize.Width, Math.Max(buttonBound.Height * 2 * SystemInformation.MouseWheelScrollLines, area.Bottom - (buttonBound.Bottom - 1)));

            var loc0 = button0.Location;
            visibleContextMenuStripSet.Add(cms);
            cms.Closed += (sender2, e2) => visibleContextMenuStripSet.Remove(cms);
            //cms.Show(ownerForm.PointToScreen(new Point(button0.Left, button0.Bottom)));
            //cms.Show(ownerForm.PointToScreen(new Point(button0.Left, button0.Bottom - 1))); // 外観の問題
            cms.Show(new Point(buttonBound.Left, buttonBound.Bottom - 1)); // 外観の問題
        }

        private void DropDown_DragLeave(object sender, EventArgs e)
        {
            var mouseButtons = Control.MouseButtons;
            if (mouseButtons != MouseButtons.Left && mouseButtons != MouseButtons.Right) // 主目的はこの後のマウスアップで Click が暴発するのを防ぐこと
            {
                // 開いているものを閉じる
                //foreach (var cms in VisibleContextMenuStripSet) cms.Close();

                // 一度クリックが効かないようにする
                var cmsEx = sender as ContextMenuStripEx;
                if (cmsEx != null)
                {
                    cmsEx.CancelClickOnce = true;
                }
                else
                {
                    foreach (var cms in VisibleContextMenuStripSet) cms.Close();
                }
            }
            //LowLevelMouseHook_MouseWheel_DraggingInDropDown = null;
        }
        
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(
            HandleRef hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private const int WM_SETREDRAW = 0x000B;

        /// <summary>
        /// コントロールの再描画を停止させる
        /// </summary>
        /// <param name="control">対象のコントロール</param>
        public static void BeginControlUpdate(Control control)
        {
            SendMessage(new HandleRef(control, control.Handle),
                WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// コントロールの再描画を再開させる
        /// </summary>
        /// <param name="control">対象のコントロール</param>
        public static void EndControlUpdate(Control control)
        {
            SendMessage(new HandleRef(control, control.Handle),
                WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
            control.Invalidate();
        }

        private void DropDown_DragDrop(object sender, DragEventArgs e)
        {
            if (dragOuting) return;
            var dropDown = (ToolStripDropDown)sender;
            var item = GetItem(dropDown, e);
            if (item != null)
            {
                if (item.HasDropDown)
                {
                    item.DropDown.Close();
                }
                foreach (var cms in VisibleContextMenuStripSet)
                {
                    cms.AutoClose = false;
                    BeginControlUpdate(cms);
                }
                var e3 = new ZipPlaAddressBarDragEventArgs(e, item.Path, isDropDown: true, hasDropDown: item.HasDropDown, finallyAction: () =>
                {
                    foreach (var cms in VisibleContextMenuStripSet)
                    {
                        EndControlUpdate(cms);
                        cms.AutoClose = true;
                        cms.Close();
                    }
                });
                SubControlDragDrop?.Invoke(this, e3);
            }
            else
            {
                foreach (var cms in VisibleContextMenuStripSet) cms.Close();
            }
        }

        private static ZipPlaAddresBarToolStripMenuItem GetItem(ToolStripDropDown dropDown, DragEventArgs e)
        {
            var items = dropDown.Items;
            var count = items.Count;
            var point = dropDown.PointToClient(new Point(e.X, e.Y));
            foreach (ZipPlaAddresBarToolStripMenuItem item in items)
            {
                if (item.Bounds.Contains(point))
                {
                    return item;
                }
            }
            return null;
        }

        private static ZipPlaAddresBarToolStripMenuItem GetItem(ToolStripDropDown dropDown, MouseEventArgs e)
        {
            var items = dropDown.Items;
            var count = items.Count;
            var point = new Point(e.X, e.Y);
            foreach (ToolStripMenuItem item in items)
            {
                if (item.Bounds.Contains(point))
                {
                    return item as ZipPlaAddresBarToolStripMenuItem;
                }
            }
            return null;
        }

        private ZipPlaAddresBarToolStripMenuItem DropDown_DragOver_LastOver = null;
        private void DropDown_DragOver(object sender, DragEventArgs e)
        {
            if (dragOuting)
            {
                e.Effect = DragDropEffects.None;
                return;
            }
            var dropDown = sender as ContextMenuStripEx;
            if (dropDown == null)
            {
                e.Effect = DragDropEffects.None;
                return;
            }
            var item = GetItem(dropDown, e);
            if (item != null)
            {
                if (item != DropDown_DragOver_LastOver)
                {
                    DropDown_DragOver_LastOver = item;
                    var hasDropDown = item.HasDropDown;
                    var e3 = new ZipPlaAddressBarDragEventArgs(e, item.Path, isDropDown: true, hasDropDown: hasDropDown);
                    SubControlDragEnter?.Invoke(this, e3);

                    if (e3.Effect == DragDropEffects.Link)
                    {
                        e.Effect = DragDropEffects.None;
                    }
                    else
                    {
                        e3.SetTo(e);
                    }

                    if (e3.Effect != DragDropEffects.None)
                    {
                        item.Select();

                        if (hasDropDown && !item.DropDown.Visible)
                        {
                            var owner = item.Owner;
                            if (owner != null)
                            {
                                foreach (ToolStripMenuItem brother in owner.Items)
                                {
                                    if (brother.HasDropDown)
                                    {
                                        var dd = brother.DropDown;
                                        if (dd.Visible)
                                        {
                                            dd.Close();
                                        }
                                    }
                                }
                            }

                            item.ShowDropDown();
                        }
                    }
                }

                var cientPoint = dropDown.PointToClient(new Point(e.X, e.Y)); if (cientPoint.Y < item.Height)
                {
                    dropDown.PerformScroll(up: true, count: 1);
                }
                else if (cientPoint.Y >= dropDown.Height - item.Height)
                {
                    dropDown.PerformScroll(up: false, count: 1);
                }
            }
        }

        /*
        private void Cms_DragEnter(object sender, DragEventArgs e)
        {
            var e2 = new ZipPlaAddressBarDragEventArgs(e, null, isDropDown: false, hasDropDown: false);
            SubControlDragEnter?.Invoke(this, e2);
            e2.SetTo(e);
        }
        */

        //private DragDropEffects lastItemEffect;

        private static bool NetworkDirectoryShouldBeHidden(string pureDirectoryName) => pureDirectoryName == "print$";
        private static bool DirectoryShouldBeHidden(string path) => DirectoryShouldBeHidden(new DirectoryInfo(path));
        private static bool DirectoryShouldBeHidden(DirectoryInfo directoryInfo) => (directoryInfo.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0;

        public static LogicalStringComparer LogicalStringComparer = new LogicalStringComparer();
        private ToolStripItem[] getDirectorySeekingMenuItemArray2(string path, string curPath, object sender, EventArgs e, ContextMenuStripEx cms)
        {
            var isNRoot = isNetworkRoot(path);
            var directories = localGetDirectories(path, isNRoot);
            var itemsList = new List<ToolStripItem>(directories.Length);
            var curPathInLower = curPath?.ToLower();
            var allowDrop = AllowDrop && SubControlDragEnter != null && SubControlDragDrop != null;
            foreach (var subdir in directories)
            {
                if (subdir == null)
                {
                    itemsList.Add(new ToolStripSeparator());
                    continue;
                }
                try
                {
                    if (path == null) // ルートの場合
                    {
                        if (!Directory.Exists(subdir)) continue;
                    }
                    else
                    {
                        if (isNRoot)
                        {
                            if (NetworkDirectoryShouldBeHidden(Path.GetFileName(subdir)))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (DirectoryShouldBeHidden(subdir)) continue;
                        }
                    }
                }
                catch
                {
                    continue;
                }
                //var name = Path.GetFileName(subdir);
                var childPath = addDirectorySeparatorChar(subdir);
                var item = new ZipPlaAddresBarToolStripMenuItem(getDisplayName(childPath), subdir, ownerForm, this);
                item.MouseDown += Button_MouseDown;
                item.MouseUp += Button_MouseUp;
                item.MouseLeave += Button_MouseLeave;
                item.DragDropStarting += DragDropStarting;
                item.DragDropFinished += DragDropFinished;
                if (childPath.ToLower() == curPathInLower)
                {
                    item.Font = new Font(item.Font, FontStyle.Bold);
                    item.CheckState = CheckState.Indeterminate;
                    CheckMarkProvider.SetCheckMark(cms, item, DisplayCheckMark.Select);
                }
                else
                {
                    item.Font = new Font(item.Font, FontStyle.Regular);
                }
                item.MouseUp += (sender2, e2) =>
                {
                    //var mb = Control.MouseButtons;
                    //if (mb == MouseButtons.Left || mb == MouseButtons.None)
                    if (e2.Button == MouseButtons.Left)
                    {
                        var goToNext = item.DropDownItems.Count > 0;
                        if (goToNext)
                        {
                            var itemLocation = ZipPlaAddresBarToolStripMenuItem.Read(item);
                            var itemHeight = item.Height;
                            var clickWidth = (8 * itemHeight) / 5;
                            var nextRectangle = new Rectangle(itemLocation.X - clickWidth, itemLocation.Y, clickWidth, itemHeight);
                            goToNext = nextRectangle.Contains(Cursor.Position);
                        }
                        if (!goToNext)
                        {
                            text = subdir;
                            makeButtonList(ZipPlaAddressBarCommandSender.MoveByButton);
                            TextChangedByButton?.Invoke(this, new ZipPlaAddressBarTextChangedByButtonEventArgs(dropDown: true));
                            TextChanged?.Invoke(sender, e);
                            cms.Close();
                            focusRobber();
                        }
                    }
                    /*
                    else
                    {
                        cms.Close();
                        otherControl.Focus();
                    }
                    */
                };

                var hasDropDown = localHasAnyDirectory(subdir);// item.HasDropDown;
                
                if (hasDropDown)
                {

                    item.DropDownOpening += (sender2, e2) =>
                    {
                        item.DropDownItems.Clear();
                        item.DropDownItems.AddRange(getDirectorySeekingMenuItemArray2(subdir, null, sender, e, cms));

                    };
                    var dropDownCms = new ContextMenuStripEx();
                    {
                        var first = true;
                        dropDownCms.Opened += (sender2, e2) =>
                        {
                            first = true;
                        };
                        dropDownCms.MouseEnter += (sender2, e2) =>
                        {
                            if (first)
                            {
                                first = false;
                                (sender2 as ContextMenuStripEx).Focus();
                            }
                        };
                    }
                    dropDownCms.Opening += (sender2, e2) =>
                    {
                        if (AbsoluteNeverOpen)
                        {
                            e2.Cancel = true;
                            return;
                        }
                        visibleContextMenuStripSet.Add(dropDownCms);
                    };
                    //dropDownCms.Closing += (sender2, e2) => visibleContextMenuStripSet.Remove(dropDownCms);
                    dropDownCms.Closed += (sender2, e2) => visibleContextMenuStripSet.Remove(dropDownCms);
                    item.NewDropDown = dropDownCms;
                    item.DropDownItems.Add("");
                    item.Paint += DrawToolStripMenuItemTriangle;


                    var dd = dropDownCms;
                    dd.AllowDrop = true;
                    dd.DragEnter += (sender3, e3) =>
                    {
                        if (dragOuting)
                        {
                            e3.Effect = DragDropEffects.None;
                            return;
                        }
                        var e4 = new ZipPlaAddressBarDragEventArgs(e3, subdir, isDropDown: true, hasDropDown: true);
                        SubControlDragEnter?.Invoke(this, e4);
                        if (e4.Effect == DragDropEffects.Link) e4.Effect = DragDropEffects.None;
                        e4.SetTo(e3);

                        //LowLevelMouseHook_MouseWheel_DraggingInDropDown = dd;
                    };
                    dd.DragOver += DropDown_DragOver;
                    dd.DragDrop += DropDown_DragDrop;
                    dd.DragLeave += DropDown_DragLeave;
                }
                item.Name = subdir;
                itemsList.Add(item);
            }

            if (path == null)
            {
                return itemsList.ToArray();
            }
            else
            {
                return itemsList.OrderBy(item => item.Text, LogicalStringComparer).ToArray();
            }
        }
        
        private class ZipPlaAddresBarToolStripMenuItem : ToolStripMenuItem
        {
            ZipPlaAddressBar parent;
            public readonly string Path;
            public ZipPlaAddresBarToolStripMenuItem(string text, string path, Form baseForm, ZipPlaAddressBar parent) : base(text)
            {
                Path = path;
                this.baseForm = baseForm;
                this.parent = parent;
            }

            public static Point Read(ToolStripMenuItem tsmi)
            {
                return (tsmi as ZipPlaAddresBarToolStripMenuItem).DropDownLocation;
            }

            #region 外部への D&D 実装

            private Point? MouseDownPoint = null;
            private Form baseForm;
            public event MouseEventHandler DragDropStarting;
            public event MouseEventHandler DragDropFinished;

            protected override void OnMouseDown(MouseEventArgs mevent)
            {
                MouseDownPoint = mevent.Button == MouseButtons.Left | mevent.Button == MouseButtons.Right ? mevent.Location : null as Point?;
                base.OnMouseDown(mevent);
            }

            protected override void OnMouseUp(MouseEventArgs mevent)
            {
                MouseDownPoint = null;
                base.OnMouseUp(mevent);
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                dragOut(null);
                base.OnMouseLeave(e);
            }

            protected override void OnMouseMove(MouseEventArgs mevent)
            {
                dragOut(mevent);
                base.OnMouseMove(mevent);
            }

            private void dragOut(MouseEventArgs mevent)
            {
                if (MouseDownPoint != null && Path != null && fixedSelect == null)
                {
                    if (mevent == null || !ContentRectangle.Contains(mevent.Location))
                    {
                        MouseDownPoint = null;
                        try
                        {
                            string path;
                            var len = Path.Length;
                            if (len >= 2 && Path[len - 2] == ':')
                            {
                                path = Path;
                            }
                            else
                            {
                                path = removeDirectorySeparatorChar(Path);
                            }

                            var dataObject = new DataObject(DataFormats.FileDrop, new string[1] { path });
                            DragDropStarting?.Invoke(this, mevent);
                            if (HasDropDown)
                            {
                                DropDown.Close();
                            }
                            fixedSelect = true;
                            DropDownNeverOpen = true;
                            foreach (var cms in parent.VisibleContextMenuStripSet)
                            {
                                cms.AutoClose = false; // 極力消えないように
                                //BeginControlUpdate(cms); 裏側にドロップできてしまう
                            }
                            try
                            {
                                DoDragDrop(dataObject, DragDropEffects.Copy | DragDropEffects.Link);
                            }
                            finally
                            {
                                try
                                {
                                    DragDropFinished?.Invoke(this, mevent);
                                }
                                finally
                                {
                                    fixedSelect = null;
                                    //DropDownNeverOpen = false; // 閉じるので不要＋戻すと閉じる前に子が開いてしまうことがある
                                    foreach (var cms in parent.VisibleContextMenuStripSet)
                                    {
                                        //EndControlUpdate(cms); 裏側にドロップできてしまう
                                        cms.AutoClose = true;
                                        cms.Close();
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(baseForm, ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }

            private bool dropDownNeverOpen = false;
            public bool DropDownNeverOpen
            {
                get { return dropDownNeverOpen; }
                set
                {

                    //AbsoluteNeverOpen で行う
                    /*
                    dropDownNeverOpen = value;
                    if (HasDropDown)
                    {
                        var dropDown = DropDown as ContextMenuStripEx;
                        if (dropDown != null)
                        {
                            //dropDown.NeverOpen = value;
                            setAllNeverOpen(dropDown, value);
                            
                        }
                    }
                   */
                }
            }

            /*
            void setAllNeverOpen(ContextMenuStripEx ex, bool value)
            {
                if (ex == null) return;
                ex.NeverOpen = value;
                var items = ex.Items;
                if (items != null)
                {
                    for (var i = 0; i < items.Count; i++)
                    {
                        var item = items[i] as ZipPlaAddresBarToolStripMenuItem;
                        if (item != null)
                        {
                            setAllNeverOpen(item.DropDown as ContextMenuStripEx, value);
                        }
                    }
                }
            }
                */

            // DropDownNeverOpen が書き換えられる前に DropDown が書き換えられることもある
            public ToolStripDropDown NewDropDown
            {
                set
                {
                    //AbsoluteNeverOpen で行う
                    /*
                    var ex = value as ContextMenuStripEx;
                    if (ex != null)
                    {
                        setAllNeverOpen(ex, dropDownNeverOpen);
                        //ex.NeverOpen = dropDownNeverOpen;
                    }
                    */
                    base.DropDown = value;
                }
            }
            
            /*
            private void CancelEventHandler_Cancel(object sender, CancelEventArgs e)
            {
                e.Cancel = true;
            }
            */

            private bool? fixedSelect = null;
            /// <summary>
            /// これが null でない間 DragOut は発生しない
            /// </summary>
            public bool? FixedSelect { get { return fixedSelect; } set { fixedSelect = value; } }
            public override bool Selected
            {
                get
                {
                    return fixedSelect ?? base.Selected;
                }
            }

            protected override void OnQueryContinueDrag(QueryContinueDragEventArgs qcdevent)
            {
                if ((qcdevent.KeyState & ((int)(Keys.LButton | Keys.RButton))) == (int)(Keys.LButton | Keys.RButton))
                {
                    qcdevent.Action = DragAction.Cancel;
                }
                
                base.OnQueryContinueDrag(qcdevent);
            }
        #endregion
        }

        private void directoryButton_Click(object sender, EventArgs e)
        {
            text = removeDirectorySeparatorChar((sender as DirectoryButton).Path);//getButtonPath(sender as Button);
            makeButtonList(ZipPlaAddressBarCommandSender.MoveByButton);
            TextChangedByButton?.Invoke(this, new ZipPlaAddressBarTextChangedByButtonEventArgs(dropDown: false));
            TextChanged?.Invoke(sender, e);
            focusRobber();
        }

        /*
        private string getButtonPath(Button button)
        {
            var result = "";
            var skip = true;
            foreach (var btn in buttonList)
            {
                if (skip = !skip) continue;
                if (button != btn)
                {
                    result += btn.Text + Path.DirectorySeparatorChar;
                }
                else
                {
                    return result + btn.Text;
                }
            }
            throw new Exception("Program Logic error.");
        }
        */

        //private const int allowance = 32;
        /*
        private void fitVisibleButtonCount()
        {
            if (buttonList.Count == 0) return;
            var baseRight = baseControl.Right;
            var lastButton = buttonList.Last();
            var allowance = (32 * lastButton.Height + 23 / 2) / 23;
            var buttonRight = lastButton.Right;
            var shift = 0;
            while (nextDeleteIndex < buttonList.Count && buttonRight + allowance - shift > baseRight)
            {
                var button = buttonList[nextDeleteIndex++];
                if (button.Visible)
                {
                    shift += button.Width;
                    var temp = BaseControl_TextChanged_StopRead;
                    BaseControl_TextChanged_StopRead = true;
                    button.Visible = false;
                    BaseControl_TextChanged_StopRead = temp;
                }
            }
            if (shift > 0)
            {
                for (var i = 0; i < buttonList.Count; i++)
                {
                    var button = buttonList[i];
                    button.Location = new Point(button.Location.X - shift, button.Location.Y);
                }
            }
            else
            {
                while (nextDeleteIndex > 0 && buttonRight + allowance + shift + buttonList[nextDeleteIndex - 1].Width <= baseRight)
                {
                    var button = buttonList[nextDeleteIndex - 1];
                    shift += button.Width;
                    button.Visible = true;
                    nextDeleteIndex--;
                }
                if (shift > 0)
                {
                    for (var i = 0; i < buttonList.Count; i++)
                    {
                        var button = buttonList[i];
                        button.Location = new Point(button.Location.X + shift, button.Location.Y);
                    }
                }
            }
        }
        */
        private void fitVisibleButtonCount()
        {
            var buttonListCount = buttonList.Count;
            if (buttonListCount == 0) return;

            var baseRight = baseControl.Right;
            var lastButton = buttonList.Last();
            var buttonHeight = lastButton.Height;
            var allowance = (32 * buttonHeight + 23 / 2) / 23;
            var regionRight = baseRight - allowance;
            var regionLeft = baseControl.Left;
            var regionWidth = regionRight - regionLeft;
            var totalWidth = 0;
            var showButtonCount = 0;
            const int forceShowButtonCount = 4;
            for (var i = buttonListCount - 1; i >= 0; i--)
            {
                var button = buttonList[i];
                var buttonWidth = button is DirectoryButton dButton ? dButton.FullWidth : button.Width;
                var newTotalWidth = totalWidth + buttonWidth;
                if (newTotalWidth > regionWidth && showButtonCount >= forceShowButtonCount) break;
                totalWidth = newTotalWidth;
                showButtonCount++;
            }

            var startIndex = buttonListCount - showButtonCount;

            if (totalWidth > regionWidth)
            {
                var dButtonList = new List<DirectoryButton>();
                var widthList = new List<int>();
                var usableWidth = regionWidth;
                for (var i = startIndex; i < buttonListCount; i++)
                {
                    var button = buttonList[i];
                    if (button is DirectoryButton dButton)
                    {
                        dButtonList.Add(dButton);
                        widthList.Add(dButton.FullWidth);
                    }
                    else usableWidth -= button.Width;
                }
                CutValues(widthList, usableWidth);
                for (var i = 0; i < dButtonList.Count; i++)
                {
                    var dButton = dButtonList[i];
                    dButton.AutoSize = false;
                    dButton.Size = new Size(widthList[i], buttonHeight);
                }


                /*
                var minWidth = 4 * buttonHeight;
                for (var i = startIndex; i < buttonListCount; i++)
                {
                    if (buttonList[i] is DirectoryButton dButton)
                    {
                        var newButtonWidth = dButton.FullWidth - (totalWidth - regionWidth);
                        if (newButtonWidth >= minWidth)
                        {
                            dButton.AutoSize = false;
                            //dButton.AutoEllipsis = true;
                            dButton.Size = new Size(newButtonWidth, buttonHeight);
                            // totalWidth = regionWidth; // あとで使う場合コメントアウトを外す
                            while (++i < buttonListCount)
                            {
                                if (buttonList[i] is DirectoryButton dButton2) dButton2.Size = new Size(dButton2.FullWidth, buttonHeight);
                            }
                            break;
                        }
                        else if (dButton.FullWidth > minWidth)
                        {
                            dButton.AutoSize = false;
                            //dButton.AutoEllipsis = true;
                            dButton.Size = new Size(minWidth, buttonHeight);
                            totalWidth -= dButton.FullWidth - minWidth;
                        }
                    }
                }
                */

            }
            else
            {
                for (var i = startIndex; i < buttonListCount; i++)
                {
                    if (buttonList[i] is DirectoryButton dButton)
                    {
                        dButton.Size = new Size(dButton.FullWidth, buttonHeight);
                    }
                }

            }

            for (var i = 0; i < startIndex; i++)
            {
                buttonList[i].Visible = false;
            }
            /*
            var prevButtonRight = regionLeft;
            for (var i = 0; i < startIndex; i++)
            {
                var button = buttonList[i];
                button.Visible = false;
                prevButtonRight -= button.Width;
                button.Left = prevButtonRight;
            }
            */

            var nextButtonLeft = regionLeft;
            for (var i = startIndex; i < buttonListCount; i++)
            {
                var button = buttonList[i];
                button.Visible = true;
                button.Left = nextButtonLeft;
                nextButtonLeft += button.Width;
            }
        }

        private static void CutValues(List<int> values, int boundOfTotal)
        {
            if (values.Count == 0) return;
            var left = values.Sum() - boundOfTotal;
            while (left > 0)
            {
                var max = int.MinValue;
                var second = int.MinValue;
                var maxCount = 0;
                foreach (var value in values)
                {
                    if (value > max)
                    {
                        second = max;
                        max = value;
                        maxCount = 1;
                    }
                    else if (value == max)
                    {
                        maxCount++;
                    }
                    else if (value > second)
                    {
                        second = value;
                    }
                }
                if (second > int.MinValue)
                {
                    var red = (max - second) * maxCount;
                    if (red <= left)
                    {
                        left -= red;
                        for (var i = 0; i < values.Count; i++)
                        {
                            if (values[i] == max) values[i] = second;
                        }
                        continue;
                    }
                }
                var d = left / maxCount;
                var r = left % maxCount;
                for (var i = 0; i < values.Count; i++)
                {
                    var value = values[i];
                    if (value == max)
                    {
                        if (r > 0)
                        {
                            values[i] = value - (d + 1);
                            r--;
                        }
                        else values[i] = value - d;
                    }
                }
                return;
            }
        }

        private void clearButtons()
        {
            var temp = BaseControl_TextChanged_StopRead;
            BaseControl_TextChanged_StopRead = true;
            foreach (var button in buttonList.ToArray())
            {
                //var u = ownerForm.Controls.Contains(button); // ここでは true
                button.Dispose();
                //var x = ownerForm.Controls.Contains(button); // ここでは false
                
                //ownerForm.Controls.Remove(button); // こちらでは Disposed が実行されない
            }
            BaseControl_TextChanged_StopRead = temp;
            buttonList.Clear();
        }

        public static void DrawToolStripMenuItemTriangle(object sender, PaintEventArgs e)
        {
            var toolStripMenuItem = sender as ToolStripMenuItem;
            if (toolStripMenuItem?.HasDropDownItems != true) return;

            var content = toolStripMenuItem.ContentRectangle;
            var height = content.Height;

#if !DEBUG
            if (height <= 20) return;
#endif

            const float normalizedHeight = 0.4f;
            const int absRight = 9;
            
            var c = (content.Top + content.Bottom) * 0.5f;
            var half = height * (normalizedHeight * 0.5f);
            var right = content.Right - absRight;
            var left = right - half;

#if !DEBUG
            e.Graphics.FillPolygon(toolStripMenuItem.Enabled ? SystemBrushes.MenuText : ThumbnailCacheSettingForm.DisabledBrush, new PointF[]
            {
                new PointF(left, c - half ),
                new PointF(left, c + half ),
                new PointF(right, c),
            });
#else
            e.Graphics.FillPolygon(toolStripMenuItem.Enabled ? Brushes.Red : ThumbnailCacheSettingForm.DisabledBrush, new PointF[]
            {
                new PointF(left, c - half ),
                new PointF(left, c + half ),
                new PointF(right, c),
            });
#endif
        }

        public static void AddDrawToolStripMenuItemTriangleEventHandler(MenuStrip toolStrip)
        {
            foreach (ToolStripItem toolStripItem in toolStrip.Items)
            {
                var toolStripMenuItem = toolStripItem as ToolStripMenuItem;
                if (toolStripMenuItem != null)
                {
                    foreach (ToolStripItem item in toolStripMenuItem.DropDownItems) AddDrawToolStripMenuItemTriangleEventHandler(item);
                }
            }
        }

        public static void AddDrawToolStripMenuItemTriangleEventHandler(System.Windows.Forms.ContextMenuStrip toolStrip)
        {
            foreach (ToolStripItem item in toolStrip.Items)
            {
                AddDrawToolStripMenuItemTriangleEventHandler(item);
            }
        }

        public static void AddDrawToolStripMenuItemTriangleEventHandler(ToolStripItem toolStripItem)
        {
            var toolStripMenuItem = toolStripItem as ToolStripMenuItem;
            if (toolStripMenuItem != null && toolStripMenuItem.HasDropDownItems)
            {
                toolStripMenuItem.Paint += DrawToolStripMenuItemTriangle;
                foreach(ToolStripItem item in toolStripMenuItem.DropDownItems) AddDrawToolStripMenuItemTriangleEventHandler(item);
            }
        }

        #region ホイールでスクロール

        // 子メニューにも適用する場合、ToolStripMenuItem のインスタンスの DropDown プロパティを
        // これに置き換える必要がある。
        // また、環境依存への対応のため、開いたときにフォーカスを移すようにした方がいいかもしれない
        class ContextMenuStripEx : ContextMenuStrip
        {
            private static readonly FieldInfo upScrollButtonInfo = typeof(ToolStripDropDownMenu).GetField(
                    "upScrollButton" , BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);
            private static readonly FieldInfo downScrollButtonInfo = typeof(ToolStripDropDownMenu).GetField(
                    "downScrollButton", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);

            //private ControlTouchListener touchListener;
            //private TouchAnalyzer touchAnalyzer;
            private GestureListener gestureListener;

            public ContextMenuStripEx(): base()
            {
                Renderer = new escapeAmpersand();
                //touchAnalyzer = new TouchAnalyzer(touchListener = new ControlTouchListener(this));
                //touchAnalyzer.Pan += TouchAnalyzer_Pan;
                gestureListener = new GestureListener(this, new GestureConfig[] {
                    //new GestureConfig(3, 1, 0), // ズーム
                    new GestureConfig(4, 2 | 4 , 8 | 16 ), // パン、向き拘束と慣性なし
                });
                gestureListener.Pan += GestureListener_Pan;
            }

            bool GestureListener_Pan_Scrolled = false;
            int GestureListener_Pan_StartingLocation;
            int GestureListener_Pan_PrevTotalDelta;
            private void GestureListener_Pan(object sender, PanEventArgs e)
            {
                if (Items.Count > 0)
                {
                    var location = e.Location.Y;
                    if (e.Begin)
                    {
                        GestureListener_Pan_Scrolled = false;
                        e.Handled = true;
                        GestureListener_Pan_StartingLocation = location;
                        GestureListener_Pan_PrevTotalDelta = 0;
                    }
                    var height = Items[0].Height;
                    var totalDelta = (int)Math.Round((double)(location - GestureListener_Pan_StartingLocation) / height);
                    var prevTotalDelta = GestureListener_Pan_PrevTotalDelta;
                    GestureListener_Pan_PrevTotalDelta = totalDelta;
                    if (totalDelta != prevTotalDelta)
                    {
                        GestureListener_Pan_Scrolled = true;
                        PerformScroll(totalDelta - prevTotalDelta);
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

            /*
            bool TouchAnalyzer_Pan_Scrolled = false;
            private void TouchAnalyzer_Pan(TouchAnalyzer sender, PanEventArgs e)
            {
                if (Items.Count > 0)
                {
                    if (e.Condition == TouchGestureCondition.Begin)
                    {
                        TouchAnalyzer_Pan_Scrolled = false;
                        e.Handled = true;
                    }
                    var height = Items[0].Height;
                    var totalDelta = CastHelper.Round(e.TotalVerticalMotion / height);
                    var prevTotalDelta = CastHelper.Round(e.PreviousTotalVerticalMotion / height);
                    if (totalDelta != prevTotalDelta)
                    {
                        TouchAnalyzer_Pan_Scrolled = true;
                        PerformScroll(totalDelta - prevTotalDelta);
                    }
                    if (e.Condition == TouchGestureCondition.Ongoing)
                    {
                        e.Handled = true;
                    }
                    else if (e.Condition == TouchGestureCondition.Complete && TouchAnalyzer_Pan_Scrolled)
                    {
                        TouchAnalyzer_Pan_Scrolled = false;
                        e.Handled = true;
                    }
                }
            }
            
            private bool disposed = false;
            protected override void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        touchAnalyzer.Dispose();
                        touchListener.Dispose();
                    }

                    disposed = true;

                    base.Dispose(disposing);
                }
            }
            */

            private class escapeAmpersand : ToolStripProfessionalRenderer
            {
                protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
                {
                    if (e.Item is ToolStripItem) e.Text = e.Text.Replace("&", "&&");
                    base.OnRenderItemText(e);
                }
            }
            
            private bool neverOpen = false;
            public bool NeverOpen
            {
                get
                {
                    /*
                    if (neverOpen) return true;
                    
                    var cms = this;
                    ZipPlaAddresBarToolStripMenuItem parent;
                    while ((parent = cms?.OwnerItem as ZipPlaAddresBarToolStripMenuItem) != null)
                    {
                        if (parent.DropDownNeverOpen)
                        {
                            NeverOpen = true;
                            return true;
                        }
                        cms = parent.Owner as ContextMenuStripEx;
                    }
                    
                    return false;
                    */
                    return neverOpen;
                }
                set
                {
                    if (neverOpen != value)
                    {
                        neverOpen = value;
                        /*
                        if (neverOpen && Visible)
                        {
                            Close();
                        }
                        */
                    }
                }
            }

            protected override void OnOpening(CancelEventArgs e)
            {
                if (!NeverOpen)
                {
                    base.OnOpening(e);
                }
                else
                {
                    e.Cancel = true;
                }
            }
            
            protected override void OnOpened(EventArgs e)
            {
                if (!NeverOpen)
                {
                    base.OnOpened(e);
                }
                else
                {
                    Close();
                }
            }

            private static MethodInfo ScrollInternalInfo = typeof(ToolStripDropDownMenu).GetMethod("ScrollInternal",
                BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Instance, null, new Type[] { typeof(bool) }, null);
            public void PerformScroll(bool up, int count)
            {
                var p = up ? upScrollButtonInfo : downScrollButtonInfo;
                if (p != null && ScrollInternalInfo != null)
                {   //ボタンのオブジェクトを取得、スクロール処理のメソッドも確保
                    ToolStripControlHost scrollButton = p.GetValue(this) as ToolStripControlHost;
                    for (int i = 0; i < count; i++)
                    {
                        if (scrollButton != null && scrollButton.Visible && scrollButton.Enabled)
                        {   //該当ボタンが押せる状態であれば、スクロール用メソッドを呼び出す
                            ScrollInternalInfo.Invoke(this, new object[] { up });
                        }
                    }
                }
            }

            public void PerformScroll(int directedCount)
            {
                if (directedCount > 0)
                {
                    PerformScroll(up: true, count: directedCount);
                }
                else if (directedCount < 0)
                {
                    PerformScroll(up: false, count: -directedCount);
                }
            }

            public void PerformScroll(MouseEventArgs e)
            {
                var up = e.Delta > 0;
                const int WHEEL_DELTA = 120;

                // 回転分
                var scroll = Math.Abs(e.Delta) * SystemInformation.MouseWheelScrollLines / WHEEL_DELTA;
                

                // 毎回 SystemInformation.MouseWheelScrollLines 行ずつ
                //int scroll = SystemInformation.MouseWheelScrollLines;
                
                PerformScroll(up, scroll);
            }

            protected override void OnMouseWheel(MouseEventArgs e)
            {
                //５件分スクロールさせ、処理済み状態にする
                //PerformScroll(e.Delta > 0, 5);

                PerformScroll(e);

                ((HandledMouseEventArgs)e).Handled = true;
                //本来の処理
                base.OnMouseWheel(e);
            }

            protected override void OnKeyUp(KeyEventArgs e)
            {
                //まず、選択中のメニュー項目、およびコンテキストメニューの表示領域を把握する
                ToolStripItem item = null;
                foreach (ToolStripItem i in this.Items)
                {
                    if (i.Selected) { item = i; break; }
                }
                Rectangle displayRect = this.RectangleToClient(this.Bounds);
                if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
                {   //矢印キー関連は何もしない
                }
                else if (e.KeyCode == Keys.Apps)
                {   //アプリケーションキー関連も何もしない
                }
                else if (e.KeyCode == Keys.PageUp)
                {   //１０件分上スクロール
                    PerformScroll(true, 10);
                    if (item != null && displayRect.Contains(item.Bounds) == false)
                    {   //現在選択中のアイテムが表示領域外ならば、アイテム末尾から表示領域内のアイテムを探索
                        for (int i = this.Items.Count - 1; i >= 0; i--)
                        {
                            item = this.Items[i];
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
                    PerformScroll(false, 10);
                    if (item != null && displayRect.Contains(item.Bounds) == false)
                    {   //現在選択中のアイテムが表示領域外ならば、アイテム先頭から表示領域内のアイテムを探索
                        for (int i = 0; i < this.Items.Count; i++)
                        {
                            item = this.Items[i];
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
                            m.Invoke(this, new object[] { item });
                        }
                    }
                }
                base.OnKeyUp(e);
            }

            public bool CancelClickOnce = false;

            protected override void OnMouseUp(MouseEventArgs mea)
            {
                if (CancelClickOnce)
                {
                    CancelClickOnce = MouseButtons != MouseButtons.None;
                }
                else
                {
                    base.OnMouseUp(mea);
                }
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                if (CancelClickOnce) CancelClickOnce = MouseButtons != MouseButtons.None;
                base.OnMouseEnter(e);
            }
        }
        #endregion
    }
    public class NetShare
    {
        const int NERR_Success = 0;
        const uint MAX_PREFERRED_LENGTH = 0xFFFFFFFF;

        enum DataInformationLevel
        {
            /// <summary>
            /// 共有の名前を取得します。関数から制御が返ると、bufptr パラメータが指すバッファに、複数の 構造体からなる 1 つの配列が格納されます。
            /// </summary>
            Level0 = 0,
            /// <summary>
            /// リソースの名前、タイプ、リソースに関連付けられているコメントなど、共有リソースに関する情報を取得します。
            /// 関数から制御が返ると、bufptr パラメータが指すバッファに、複数の 構造体からなる 1 つの配列が格納されます。
            /// </summary>
            Level1 = 1,
            /// <summary>
            /// リソースの名前、タイプ、アクセス許可、パスワード、接続の数など、共有リソースに関する情報を取得します。関数から制御が返ると、bufptr パラメータが指すバッファに、複数の 構造体からなる 1 つの配列が格納されます。
            /// </summary>
            Level2 = 2,
            /// <summary>
            /// リソースの名前、タイプ、アクセス許可、接続の数、他の固有情報など、共有リソースに関する情報を取得します。関数から制御が返ると、bufptr パラメータが指すバッファに、複数の 構造体からなる 1 つの配列が格納されます。
            /// </summary>
            Level502 = 502,
        }

        /// <summary>
        /// 特定のサーバー上の各共有資源に関する情報を取得します。Windows 95/98 では使用できません。
        /// </summary>
        /// <param name="ServerName">この関数を実行するリモートサーバーの名前を表す、Unicode 文字列へのポインタを指定します。この文字列の先頭は "\\" でなければなりません。このパラメータで NULL を指定すると、ローカルコンピュータが使われます。</param>
        /// <param name="level">データの情報レベルを指定します。次の値のいずれかを指定します。</param>
        /// <param name="bufPtr">1 個のバッファへのポインタを指定します。関数から制御が返ると、このバッファに、指定したデータが格納されます。このデータの形式は、level パラメータの値によって異なります。このバッファはシステムによって割り当てられたものであり、NetApiBufferFree 関数を使って解放しなければなりません。この関数が失敗して ERROR_MORE_DATA が返った場合でも、このバッファを解放しなければならないことに注意してください。</param>
        /// <param name="prefmaxlen">取得するべきデータの最大の長さ（上限）をバイト単位で指定します。このパラメータが MAX_PREFERRED_LENGTH の場合、この関数はデータが必要とする量のメモリを割り当てます。このパラメータで他の値を指定すると、その値は、この関数が返すバイト数に制限を加えることがあります。バッファサイズが不足して一部のエントリを格納できない場合は、ERROR_MORE_DATA が返ります。詳細については、MSDN ライブラリの「Network Management Function Buffers」と「Network Management Function Buffer Lengths」を参照してください。</param>
        /// <param name="entriesread">1 つの DWORD 値へのポインタを指定します。関数から制御が返ると、この値に、実際に列挙された要素の数が格納されます。</param>
        /// <param name="totalentries">1 つの DWORD 値へのポインタを指定します。関数から制御が返ると、この値に、現在のレジューム位置以降で列挙できるはずのエントリの総数が格納されます。</param>
        /// <param name="resume_handle">引き続き既存の共有を検索するために使われるレジュームハンドルを保持している、1 つの DWORD 値へのポインタを指定します。このハンドルは最初の呼び出しのときに 0 であるべきで、それ以降の呼び出しでも変更しないでください。resume_handle パラメータで NULL を指定すると、レジュームハンドルは格納されません。</param>
        /// <returns>関数が成功すると、NERR_Success が返ります。関数が失敗すると、Win32 API のエラーコードが返ります。エラーコードのリストについては、MSDN ライブラリの「System Error Codes」を参照してください。</returns>
        /// <remarks>特定の共有が、DFS ルート内の DFS リンクであるかどうかを示す値を取得するには、情報レベル 1005 を指定して NetShareGetInfo 関数を呼び出してください。</remarks>
        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode)]
        static extern int NetShareEnum(
          StringBuilder serverName,
          DataInformationLevel level,
          ref IntPtr bufPtr,
          uint prefmaxlen,
          out int entriesread,
          out int totalentries,
          ref int resume_handle
        );

        /// <summary>
        /// NetApiBufferAllocate が割り当てたメモリを解放します。Windows NT/2000 上で他のネットワーク管理関数が返したメモリを解放するには、この関数を使ってください。
        /// </summary>
        /// <param name="Buffer">既に他のネットワーク管理関数が返したバッファへのポインタを指定します。</param>
        /// <returns>関数が成功すると、NERR_Success が返ります。</returns>
        [DllImport("Netapi32.dll", SetLastError = true)]
        static extern int NetApiBufferFree(IntPtr Buffer);

        /// <summary>
        /// The SHARE_INFO_1 structure contains information about the shared resource, including the name and type of the resource, and a comment associated with the resource.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct SHARE_INFO_1
        {
            /// <summary>
            /// Pointer to a Unicode string specifying the share name of a resource. Calls to the NetShareSetInfo function ignore this member.
            /// </summary>
            public string shi1_netname;
            /// <summary>
            /// A bitmask of flags that specify the type of the shared resource. Calls to the NetShareSetInfo function ignore this member.
            /// </summary>
            public uint shi1_type;
            /// <summary>
            /// Pointer to a Unicode string specifying an optional comment about the shared resource.
            /// </summary>
            public string shi1_remark;

            public SHARE_INFO_1(string sharename, uint sharetype, string remark)
            {
                shi1_netname = sharename;
                shi1_type = sharetype;
                shi1_remark = remark;
            }

            public override string ToString()
            {
                return shi1_netname;
            }
        }

        /// <summary>
        /// サーバ上の共有ファイル一覧を取得。
        /// </summary>
        /// <param name="Server">対象サーバ。</param>
        /// <returns></returns>
        public static List<SHARE_INFO_1> GetNetShares(string server)
        {
            int entriesread;
            IntPtr bufPtr = GetBufPtr(server, out entriesread);
            try
            {
                return GetShareInfos(bufPtr, entriesread);
            }
            finally
            {
                NetApiBufferFree(bufPtr);
            }
        }

        private static List<SHARE_INFO_1> GetShareInfos(IntPtr bufPtr, int entriesread)
        {
            IntPtr currentPtr = bufPtr;
            List<SHARE_INFO_1> shareInfos = new List<SHARE_INFO_1>();
            int nStructSize = Marshal.SizeOf(typeof(SHARE_INFO_1));

            for (int i = 0; i < entriesread; i++)
            {
                SHARE_INFO_1 shi1 = (SHARE_INFO_1)Marshal.PtrToStructure(currentPtr, typeof(SHARE_INFO_1));
                shareInfos.Add(shi1);
                //currentPtr = new IntPtr(currentPtr.ToInt32() + nStructSize)
                currentPtr = currentPtr + nStructSize;
            }
            return shareInfos;
        }

        private static IntPtr GetBufPtr(string server, out int entriesread)
        {
            int totalentries;
            int resume_handle = 0;
            IntPtr bufPtr = IntPtr.Zero;

            int result = NetShareEnum(
              new StringBuilder(server), DataInformationLevel.Level1, ref bufPtr
              , MAX_PREFERRED_LENGTH, out entriesread, out totalentries, ref resume_handle
            );

            if (result != NERR_Success)
            {
                throw new Win32Exception(result);
            }
            return bufPtr;
        }
    }

    // http://wiki.dobon.net/index.php?.NET%A5%D7%A5%ED%A5%B0%A5%E9%A5%DF%A5%F3%A5%B0%B8%A6%B5%E6%2F111
    public class LogicalStringComparer :
        System.Collections.IComparer,
        System.Collections.Generic.IComparer<string>
    {
        [System.Runtime.InteropServices.DllImport("shlwapi.dll",
            CharSet = System.Runtime.InteropServices.CharSet.Unicode,
            ExactSpelling = true)]
        public static extern int StrCmpLogicalW(string x, string y);
        
        public int Compare(string x, string y)
        {
            return StrCmpLogicalW(x, y);
        }

        public int Compare(object x, object y)
        {
            return Compare(x.ToString(), y.ToString());
        }
    }
}
