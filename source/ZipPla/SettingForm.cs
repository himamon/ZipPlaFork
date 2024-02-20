using Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsFormUtilities;

namespace ZipPla
{
    public enum DragAndDropAction { ShowThumbnails, MoveOrCopyFiles }

    public partial class SettingForm : Form
    {
        private CultureInfo[] availableLanguages;
        private CultureInfo preCultureInfo;
        //private bool prePageLeftToRight;
        private int preMaximumNumberOfHistories;
        private UpdateCheckMode preUpdateCheckMode;
        private bool preDoNotShowAgain;
        private bool preCheckNgen;
        public DragAndDropAction preDragAndDrop;
        public ulong preBuiltInViewerMemoryLimit;
        public bool preAllowUntestedPlugins;
        public bool preSearchAlsoSusieInstallationFolder;
        public bool preDynamicStringSelectionsEnabled;
        public bool preDynamicStringSelectionsAllowUserToRenameItem;
        //public bool preStartWithAccessKey;
        public DynamicStringSelectionInfo[] preDynamicStringSelections;
        public ApplicationInfo[] preApplications;

        public bool HistoryTrimmed = false;

        DataGridViewSorter dgvAppSorter, dgvDssSorter;

        //public bool NeedToReloadCatalog { get; private set; } = false;

        private bool SettingChanged
        {
            get
            {
                return !CurrentCultureInfo.Equals(preCultureInfo) || //CurrentPageLeftToRight != prePageLeftToRight ||
                    CurrentMaximumNumberOfHistories != preMaximumNumberOfHistories || CurrentUpdateCheckMode != preUpdateCheckMode || CurrentDoNotShowAgain != preDoNotShowAgain || CurrentCheckNgen != preCheckNgen ||
                    CurrentDragAndDrop != preDragAndDrop || CurrentDynamicStringSelectionsEnabled != preDynamicStringSelectionsEnabled ||
                    CurrentBuiltInViewerMemoryLimit != preBuiltInViewerMemoryLimit || CurrentAllowUntestedPlugins != preAllowUntestedPlugins || CurrentSearchAlsoSusieInstallationFolder != preSearchAlsoSusieInstallationFolder ||
                    CurrentDynamicStringSelectionsAllowUserToRenameItem != preDynamicStringSelectionsAllowUserToRenameItem || //CurrentStartWithAccessKey != preStartWithAccessKey ||
                    !CurrentDynamicStringSelections.SequenceEqual(preDynamicStringSelections) || !CurrentApplications.SequenceEqual(preApplications);
            }
            set
            {
                if (value)
                {
                    throw new Exception("SettingChanged can be set to only false.");
                }
                else
                {
                    preCultureInfo = CurrentCultureInfo;
                    //prePageLeftToRight = CurrentPageLeftToRight;
                    preMaximumNumberOfHistories = CurrentMaximumNumberOfHistories;
                    preUpdateCheckMode = CurrentUpdateCheckMode;
                    preDoNotShowAgain = CurrentDoNotShowAgain;
                    preCheckNgen = CurrentCheckNgen;
                    preDragAndDrop = CurrentDragAndDrop;
                    preApplications = CurrentApplications;
                    preDynamicStringSelectionsEnabled = CurrentDynamicStringSelectionsEnabled;
                    preBuiltInViewerMemoryLimit = CurrentBuiltInViewerMemoryLimit;
                    preAllowUntestedPlugins = CurrentAllowUntestedPlugins;
                    preSearchAlsoSusieInstallationFolder = CurrentSearchAlsoSusieInstallationFolder;
                    preDynamicStringSelectionsAllowUserToRenameItem = CurrentDynamicStringSelectionsAllowUserToRenameItem;
                    //preStartWithAccessKey = CurrentStartWithAccessKey;
                    preDynamicStringSelections = CurrentDynamicStringSelections;
                }
            }
        }
        private CultureInfo CurrentCultureInfo
        {
            get
            {
                var selectedIndex = cbLanguage.SelectedIndex;
                if (selectedIndex >= 0)
                {
                    return availableLanguages[selectedIndex];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                try
                {
                    var index = Array.IndexOf(availableLanguages, value);
                    cbLanguage.SelectedIndex = index >= 0 ? index : Array.IndexOf(availableLanguages, Message.DefaultLanguage);
                }
                catch (Exception error)
                {
                    cbLanguage.SelectedIndex = Array.IndexOf(availableLanguages, Message.DefaultLanguage);
                    MessageBox.Show(this, error.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        /*
        private bool CurrentPageLeftToRight
        {
            get
            {
                return rbLeftToRight.Checked;
            }
            set
            {
                if (value)
                {
                    rbLeftToRight.Checked = true;
                }
                else
                {
                    rbRightToLeft.Checked = true;
                }
            }
        }
        */
        private int CurrentMaximumNumberOfHistories
        {
            get
            {
                int result;
                if (cbHistory.SelectedItem == null || !int.TryParse(cbHistory.SelectedItem.ToString(), out result)) result = 0;
                return result;
            }
            set
            {
                cbHistory.SelectedItem = value.ToString();
            }
        }
        private UpdateCheckMode CurrentUpdateCheckMode
        {
            get
            {
                switch (cbAskPermissionBeforeConnection.CheckState)
                {
                    case CheckState.Checked: return UpdateCheckMode.WithDialog;
                    case CheckState.Unchecked: return UpdateCheckMode.Silent;
                    default: return UpdateCheckMode.Default;
                }
            }
            set
            {
                switch (value)
                {
                    case UpdateCheckMode.WithDialog: cbAskPermissionBeforeConnection.CheckState = CheckState.Checked; break;
                    case UpdateCheckMode.Silent: cbAskPermissionBeforeConnection.CheckState = CheckState.Unchecked; break;
                    case UpdateCheckMode.Default: cbAskPermissionBeforeConnection.CheckState = CheckState.Indeterminate; break;
                }
            }
        }
        private bool CurrentDoNotShowAgain
        {
            get
            {
                return !cbHint.Checked; // 仕様変更によりチェック＝常に調べる
            }
            set
            {
                cbHint.Checked = !value; // 仕様変更によりチェック＝常に調べる
            }
        }
        private bool CurrentCheckNgen
        {
            get
            {
                return cbNgen.Checked;
            }
            set
            {
                cbNgen.Checked = value;
            }
        }
        private DragAndDropAction CurrentDragAndDrop
        {
            get
            {
                if (rbMoveOrCopy.Checked) return DragAndDropAction.MoveOrCopyFiles;
                else return DragAndDropAction.ShowThumbnails;
            }
            set
            {
                if (value == DragAndDropAction.MoveOrCopyFiles) rbMoveOrCopy.Checked = true;
                else rbShowThumbnails.Checked = true;
            }
        }
        private bool CurrentDynamicStringSelectionsEnabled
        {
            get
            {
                return cbDynamicStringSelectionEnabled.Checked;
            }
            set
            {
                cbDynamicStringSelectionEnabled.Checked = value;
            }
        }
        private const ulong builtInViewerMemoryLimitBase = 256 << 20;
        private ulong CurrentBuiltInViewerMemoryLimit
        {
            get
            {
                var selectedIndex = cbViewerMemoryUsage.SelectedIndex;
                if (selectedIndex == 0) return 0L;
                if (selectedIndex < 0 || cbViewerMemoryUsage.Items.Count - 1 <= selectedIndex) return ulong.MaxValue;
                return (builtInViewerMemoryLimitBase >> 1) << selectedIndex;
            }
            set
            {
                var selectedIndex = 0;
                var count1 = cbViewerMemoryUsage.Items.Count - 1;
                while (value >= builtInViewerMemoryLimitBase && selectedIndex < count1)
                {
                    selectedIndex++;
                    value >>= 1;
                }
                cbViewerMemoryUsage.SelectedIndex = selectedIndex;
            }
        }
        private bool CurrentAllowUntestedPlugins
        {
            get
            {
                return cbAllowUntestedPlugins.Checked;
            }
            set
            {
                cbAllowUntestedPlugins.Checked = value;
            }
        }
        private bool CurrentSearchAlsoSusieInstallationFolder
        {
            get
            {
                return cbSearchAlsoSusieInstallationFolder.Checked;
            }
            set
            {
                cbSearchAlsoSusieInstallationFolder.Checked = value;
            }
        }
        private bool CurrentDynamicStringSelectionsAllowUserToRenameItem
        {
            get
            {
                return cbDynamicStringSelectionAllowUserToRename.Checked;
            }
            set
            {
                cbDynamicStringSelectionAllowUserToRename.Checked = value;
            }
        }
        /*
        private bool CurrentStartWithAccessKey
        {
            get
            {
                return cbAlwaysEnableAccessKey.Checked;
            }
            set
            {
                cbAlwaysEnableAccessKey.Checked = value;
            }
        }
        */
        private DynamicStringSelectionInfo[] CurrentDynamicStringSelections
        {
            get
            {
                return (from DataGridViewRowWithData row in dgvDynamicStringSelection.Rows
                        select
                            row.Path == DynamicStringSelectionInfo.CustomCommand ?
                                new DynamicStringSelectionInfo {
                                Type = row.Path,
                                DisplayName = row.Cells[tbcDisplayName.Index].Value as string,
                                DisplayNameForEmptyString = row.Cells[tbcDisplayNameForEmptyString.Index].Value as string,
                                Command = row.Cells[tbcCommand.Index].Value as string}
                            :
                                new DynamicStringSelectionInfo
                                {
                                    Type = row.Path
                                }).ToArray();
            }
            set
            {
                var count = value == null ? 0 : value.Length;
                var valueArray = new DataGridViewRow[count];
                for (var i = 0; i < count; i++)
                {
                    valueArray[i] = DynamicStringSelectionInfoToRow(value[i]);
                }
                dgvDynamicStringSelection.Rows.Clear();
                dgvDynamicStringSelection.Rows.AddRange(valueArray);
            }
        }
        private DataGridViewRow DynamicStringSelectionInfoToRow(DynamicStringSelectionInfo info)
        {
            switch (info.Type)
            {
                case DynamicStringSelectionInfo.ToFilter:
                    return getToFilterRow();
                case DynamicStringSelectionInfo.ToClipboard:
                    return getToClipboardRow();
                default:
                    return getCustomCommandRow(info.DisplayName, info.DisplayNameForEmptyString, info.Command);
            }
        }
        private ApplicationInfo[] CurrentApplications
        {
            get
            {
                return (from DataGridViewRowWithData row in dgvApplications.Rows select new ApplicationInfo(row.Path, row.Alias,
                   row.Cells[tbcCommandLineParameter.Index].Value as string, row.Cells[tbcOpenByClicking.Index].Value as string,
                     row.Cells[tbcShowInContextMenu.Index].Value as string, row.Cells[cbcMulti.Index].Value as bool? == true)).ToArray();
            }
            set
            {
                var count = value == null ? 0 : value.Length;
                //var valueArray = new DataGridViewRow[count];
                var valueList = new List<DataGridViewRow>();
                string errString = null;
                for (var i = 0; i < count; i++)
                {
                    try
                    {
                        valueList.Add(ApplicationInfoToRow(value[i]));
                        //valueArray[i] = ApplicationInfoToRow(value[i]);
                    }
                    catch(Exception e)
                    {
                        if (errString == null) errString = e.Message;
                        else errString += "\n\n" + e.Message;
                    }
                }
                dgvApplications.Rows.Clear();
                if (errString != null)
                {
                    MessageBox.Show(this, errString, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                dgvApplications.Rows.AddRange(valueList.ToArray());
            }
        }

        private DataGridViewRow ApplicationInfoToRow(ApplicationInfo info)
        {
            DataGridViewRow result;
            switch (info.Path)
            {
                case ApplicationInfo.AssociatedApplication:
                    result = getApplicationRow(info.Path, info.Alias, true, Message.AssociatedApplication, info.Alias ?? Message.Open, "", false, info.ShowInContextMenu, info.OpenByClicking);
                    result.Cells[cbcMulti.Index].Style.BackColor = Color.LightGray;
                    result.Cells[cbcMulti.Index].ReadOnly = true;
                    result.Cells[tbcCommandLineParameter.Index].Style.BackColor = Color.LightGray;
                    result.Cells[tbcCommandLineParameter.Index].ReadOnly = true;
                    break;
                case ApplicationInfo.BuiltInReader:
                    result = getApplicationRow(info.Path, info.Alias, true, Message.BuiltInViewer, info.Alias ?? Message.OpenInBuiltInViewer, "", false, info.ShowInContextMenu, info.OpenByClicking);
                    result.Cells[cbcMulti.Index].Style.BackColor = Color.LightGray;
                    result.Cells[cbcMulti.Index].ReadOnly = true;
                    result.Cells[tbcCommandLineParameter.Index].Style.BackColor = Color.LightGray;
                    result.Cells[tbcCommandLineParameter.Index].ReadOnly = true;
                    break;
                case ApplicationInfo.MoveLocation:
                    result = getApplicationRow(info.Path, info.Alias, true, Message.CurrentZipPla, info.Alias ?? Message.OpenInCurrentWindow, "", false, info.ShowInContextMenu, info.OpenByClicking);
                    result.Cells[cbcMulti.Index].Style.BackColor = Color.LightGray;
                    result.Cells[cbcMulti.Index].ReadOnly = true;
                    result.Cells[tbcCommandLineParameter.Index].Style.BackColor = Color.LightGray;
                    result.Cells[tbcCommandLineParameter.Index].ReadOnly = true;
                    break;
                case ApplicationInfo.OpenInFileInClipboard:
                    result = getApplicationRow(info.Path, null, false, null, Message.ExecutableInClipboard, "", info.MultiItems, info.ShowInContextMenu, info.OpenByClicking);
                    //result.Cells[cbcMulti.Index].Style.BackColor = Color.LightGray;
                    //result.Cells[cbcMulti.Index].ReadOnly = true;
                    result.Cells[tbcCommandLineParameter.Index].Style.BackColor = Color.LightGray;
                    result.Cells[tbcCommandLineParameter.Index].ReadOnly = true;
                    break;
                case ApplicationInfo.Explorer:
                    result = getApplicationRow(info.Path, info.Alias, true, Message.Explorer, info.Alias ?? Message.OpenInExplorer, "/select,", false, info.ShowInContextMenu, info.OpenByClicking);
                    result.Cells[cbcMulti.Index].Style.BackColor = Color.LightGray;
                    result.Cells[cbcMulti.Index].ReadOnly = true;
                    result.Cells[tbcCommandLineParameter.Index].Style.BackColor = Color.LightGray;
                    result.Cells[tbcCommandLineParameter.Index].ReadOnly = true;
                    break;

                /*
                case "<Explorer>": // 互換性保持のため
                    {
                        //var alias = Message.OpenInExplorer;
                        result = getApplicationRow(ExplorerPath, null, true, ExplorerPath, GetName(ExplorerPath, null), "/select,", false, info.ShowInContextMenu, info.OpenByClicking); break;
                    }
                    */
                default:
                    {
                        var path = info.Path;
                        var alias = info.Alias;
                        //var name = ExternalApplicationProvider.GetName(path, alias);
                        if (path != null && path.Length > 0 && path.Last() == Path.DirectorySeparatorChar)
                        {
                            result = getApplicationRow(path, alias, true, path.Substring(0, path.Length - 1), GetName(path, alias), info.CommandLineParameter, true, info.ShowInContextMenu, info.OpenByClicking);
                            result.Cells[cbcMulti.Index].Style.BackColor = Color.LightGray;
                            result.Cells[cbcMulti.Index].ReadOnly = true;
                            result.Cells[tbcCommandLineParameter.Index].Style.BackColor = Color.LightGray;
                            result.Cells[tbcCommandLineParameter.Index].ReadOnly = true;
                        }
                        else
                        {
                            result = getApplicationRow(path, alias, true, path, GetName(path, alias), info.CommandLineParameter, info.MultiItems, info.ShowInContextMenu, info.OpenByClicking);
                        }
                    }
                    break;
            }
            return result;
        }


        //readonly int noneDataGridViewHeight;
        readonly int dataGridViewMinimumHeightWithoutColumnHeader;

        public SettingForm()
        {
            InitializeComponent();

            new MessageForwarder(cbLanguage, ForwardedMessage.MouseWheel);
            new MessageForwarder(cbHistory, ForwardedMessage.MouseWheel);
            new MessageForwarder(cbViewerMemoryUsage, ForwardedMessage.MouseWheel);
            new MessageForwarder(dgvDynamicStringSelection, ForwardedMessage.MouseWheel);
            new MessageForwarder(dgvApplications, ForwardedMessage.MouseWheel);

            ToolStripOverwriter.SquarizeToolStripInClass(this);

            Program.SetDoubleBuffered(dgvApplications);
            Program.SetDoubleBuffered(dgvDynamicStringSelection);

            tbcDisplayName.HeaderCell.Style.WrapMode = DataGridViewTriState.False;
            tbcDisplayNameForEmptyString.HeaderCell.Style.WrapMode = DataGridViewTriState.False;
            tbcCommand.HeaderCell.Style.WrapMode = DataGridViewTriState.False;

            tbcName.HeaderCell.Style.WrapMode = DataGridViewTriState.False;
            tbcCommandLineParameter.HeaderCell.Style.WrapMode = DataGridViewTriState.False;
            cbcMulti.HeaderCell.Style.WrapMode = DataGridViewTriState.False;
            tbcShowInContextMenu.HeaderCell.Style.WrapMode = DataGridViewTriState.False;
            tbcOpenByClicking.HeaderCell.Style.WrapMode = DataGridViewTriState.False;

            // DPI 変更時用の処理
            Program.SetFormHeightByControlLocation(this, btnCancel); MaximumSize = Size.Empty; MinimumSize = Size;  btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            // 縦配置用
            //btnLanguageEdit.Left = cbLanguage.Right - btnLanguageEdit.Width;

            // 横配置用
            //btnLanguageEdit.Top = cbLanguage.Top - 1;
            //btnLanguageEdit.Height = cbLanguage.Height + 2;
            
            dgvApplications_SelectionChanged(null, null);
            dgvDynamicStringSelection_SelectionChanged(null, null);

            ResetCbLanguage();

            if (LoadSettings() == LoadSettings_Result.INIFileNotFound)
            {
                CurrentCultureInfo = Message.CurrentLanguage;
                //CurrentPageLeftToRight = LeftToRight;
            }


            dataGridViewMinimumHeightWithoutColumnHeader = ClientHeightWithoutColumnHeader(dgvDynamicStringSelection);
            var dgvApplicationsHeight = ClientHeightWithoutColumnHeader(dgvApplications);
            var noneDataGridViewHeight = Height - dataGridViewMinimumHeightWithoutColumnHeader - dgvApplicationsHeight;
            var idealInitialHeight = noneDataGridViewHeight + dataGridViewMinimumHeightWithoutColumnHeader * 5 / 2; // スクリーンの高さに影響されない場合の最小の高さ

            var workingArea = Screen.FromControl(Program.StartForm ?? this).WorkingArea;
            var newHeight = Math.Min(workingArea.Height, Math.Max(idealInitialHeight, noneDataGridViewHeight + RowsHeight(dgvDynamicStringSelection) + RowsHeight(dgvApplications)));
            Program.PackToRectangleOnScreen(this, newHeight, workingArea);

            //Program.SetInitialHeight(this, dgvApplications);

            dgvAppSorter = new DataGridViewSorter(dgvApplications);
            dgvAppSorter.RowsReleased += dgvApplications_SelectionChanged;

            dgvDssSorter = new DataGridViewSorter(dgvDynamicStringSelection);
            dgvDssSorter.RowsReleased += dgvDynamicStringSelection_SelectionChanged;


            var listener = DataGridViewScrollBarTouchFixer.GetGestureListener(this);
            new DataGridViewScrollBarTouchFixer(dgvDynamicStringSelection, listener);
            new DataGridViewScrollBarTouchFixer(dgvApplications, listener);
            
            setDataGridViewHeight_Start = true;
            setDataGridViewHeight();

            btnOK.Select();
        }

        static int ClientHeightWithoutColumnHeader(DataGridView dgv)
        {
            return dgv.ClientRectangle.Bottom - dgv.Columns[0].HeaderCell.Size.Height;
        }
        
        static int RowsHeight(DataGridView dgv)
        {
            if (dgv.Rows.Count > 0)
            {
                return dgv.Rows[0].Height * (2 * dgv.Rows.Count + 2) / 2;
            }
            else return 0;
        }

        bool setDataGridViewHeight_Start = false;
        int setDataGridViewHeight_TotalUsableOffset = int.MinValue;
        int setDataGridViewHeight_TopX = int.MinValue;
        int setDataGridViewHeight_TopY = int.MinValue;
        int[] setDataGridViewHeight_gaps;
        int[] setDataGridViewHeight_LayoutTargetOffset;
        void setDataGridViewHeight()
        {
            if (!setDataGridViewHeight_Start || WindowState == FormWindowState.Minimized || dgvAppSorter.InRowsHolded || dgvDssSorter.InRowsHolded) return;
            //if (!setDataGridViewHeight_Start) return;
            var targets = new DataGridView[2] { dgvDynamicStringSelection, dgvApplications };
            var layoutTargets = new Control[2] { gbDynamicStringSelection, gbApplications };
            var count = targets.Length;
            //var totalUsable = currentHeights.Sum();
            //int totalUsable;
            var needs = (from t in targets select Math.Max(RowsHeight(t), dataGridViewMinimumHeightWithoutColumnHeader)).ToArray();
            
            var totalNeed = needs.Sum();

            if(setDataGridViewHeight_TopY == int.MinValue)
            {
                var currentHeights = (from t in targets select ClientHeightWithoutColumnHeader(t)).ToArray();
                setDataGridViewHeight_TotalUsableOffset = Height - currentHeights.Sum();
                var p = layoutTargets[0].Location;
                setDataGridViewHeight_TopX = p.X;
                setDataGridViewHeight_TopY = p.Y;
                setDataGridViewHeight_gaps = new int[count];
                setDataGridViewHeight_LayoutTargetOffset = new int[count];

                for (var i = 0; i < count; i++)
                {
                    p = layoutTargets[i].Location;
                    setDataGridViewHeight_gaps[i] = i < count - 1 ? layoutTargets[i + 1].Location.Y - p.Y - layoutTargets[i].Height : 0;
                    setDataGridViewHeight_LayoutTargetOffset[i] = layoutTargets[i].Height - currentHeights[i];
                }
            }

            var totalUsable = Height - setDataGridViewHeight_TotalUsableOffset;

            while (totalUsable < totalNeed)
            {
                var dicreasable = (from n in needs where n > dataGridViewMinimumHeightWithoutColumnHeader select n).ToArray();
                var decCount = dicreasable.Length;
                if (decCount == 0) return;
                var dec = dicreasable.Min() - dataGridViewMinimumHeightWithoutColumnHeader;
                var totalDec = Math.Min(dec * dicreasable.Length, totalNeed - totalUsable);

                totalNeed -= totalDec;
                for (var i = 0; i < count; i++)
                {
                    if (needs[i] > dataGridViewMinimumHeightWithoutColumnHeader)
                    {
                        var share = (totalDec + (decCount >> 1)) / decCount;
                        needs[i] -= share;
                        totalDec -= share;
                        decCount--;
                    }
                }
            }

            var finalHeights = new int[count];

            totalUsable -= totalNeed;
            for (var i = 0; i < count; i++)
            {
                var denom = count - i;
                var share = (totalUsable + (denom >> 1)) / denom;
                finalHeights[i] = needs[i] + share;
                totalUsable -= share;
            }

            //var buttom = targets[count - 1].Location.Y + targets[count - 1].Height;

            SuspendLayout();

            var cursor = setDataGridViewHeight_TopY;
            for (var i = 0; i < count; i++)
            {
                layoutTargets[i].Location = new Point(setDataGridViewHeight_TopX, cursor);
                var newHeight = finalHeights[i] + setDataGridViewHeight_LayoutTargetOffset[i];
                layoutTargets[i].Height = newHeight;
                cursor += newHeight + setDataGridViewHeight_gaps[i];
            }
            
            ResumeLayout();
        }

        private void ResetCbLanguage()
        {
            availableLanguages = Message.AvailableLanguages;
            var items = cbLanguage.Items;
            var currentIndex = 0;
            var newIndex = 0;
            while(currentIndex < items.Count || newIndex < availableLanguages.Length)
            {
                if (currentIndex >= items.Count)
                {
                    items.Add(availableLanguages[newIndex++].NativeName);
                    currentIndex++;
                }
                else if (newIndex >= availableLanguages.Length)
                {
                    items.RemoveAt(currentIndex);
                }
                else
                {
                    var currentName = items[currentIndex].ToString();
                    var newName = availableLanguages[newIndex].NativeName;
                    if (currentName == newName)
                    {
                        currentIndex++;
                        newIndex++;
                    }
                    else if(currentName.CompareTo(newName) > 0)
                    {
                        items.Insert(currentIndex++, availableLanguages[newIndex++].NativeName);
                    }
                    else
                    {
                        items.RemoveAt(currentIndex);
                    }
                }
            }
        }

        private string ApplicationFilterDescription;
        private void SetMessages()
        {
            Text = Message.CommonSettings;
            gbLanguage.Text = Message.LanguageLanguage;
            btnLanguageEdit.Text = Message.Edit;
            openlanguageFolderToolStripMenuItem.Text = Message.OpenLanguageFolder;
            updateAndEditLanguageFileToolStripMenuItem.Text = Message.UpdateAndEditLanguageFile;
            //gbDefaultPageSequence.Text = Message.DefaultPageSequence;
            //rbLeftToRight.Text = Message.LeftToRight;
            //rbRightToLeft.Text = Message.RightToLeft;
            gbHistory.Text = Message.MaximumNumberOfHistories;
            //gbSubfolders.Text = Message.SubfolderHandling;
            //rbSubfolderIngnore.Text = Message.Ignore;
            //rbSubfolderSameWayAsArchives.Text = Message.InTheSameWayAsArchives;
            //rbSubfolderSearch.Text = Message.SearchSubfoldersAsWell;
            //gbViewer.Text = Message.Viewer;
            //lbExternalViewer.Text = Message.ExternalViewer + ":";
            //lbDefaultViewer.Text = Message.DefaultViewer + ":";
            //rbBuiltInViewer.Text = Message.BuiltInViewer;
            //rbExternalViewer.Text = Message.ExternalViewer;
            btnOK.Text = Message.OK;
            btnCancel.Text = Message.Cancel;
            gbZipPlaUpdateCheck.Text = Message.ZipPlaUpdateCheck;
            btnCheckNow.Text = updateChecking ? Message.Checking + "..." : Message.CheckNow;
            cbAskPermissionBeforeConnection.Text = Message.ShowDialogBeforeConnection;
            gbHint.Text = Message.OtherChecksOnStartup;
            //cbHint.Text = Message.DoNotShowAgain;
            gbDragAndDrop.Text = Message.DragAndDrop;
            rbShowThumbnails.Text = Message.BrowseInZipPla;//Message.OpenInZipPla; // Message.ShowThumbnails;
            rbMoveOrCopy.Text = Message.MoveOrCopyFilesAddItemToVirtualFolder.Replace(@"\n", "\n");
            
            //rbMoveOrCopy.Left = Math.Min(rbRightToLeft.Left,
            //    gbDragAndDrop.ClientSize.Width - rbMoveOrCopy.Width - (int)Math.Ceiling(1 * Program.DisplayMagnificationX));

            gbViewerMemoryUsage.Text = Message.BuiltInViewerMemoryUsage;
            var vmItems = cbViewerMemoryUsage.Items;
            var vmItemsCount1 = vmItems.Count - 1;
            vmItems[0] = Message.Minimum;
            for (var i = 1; i < vmItemsCount1; i++)
            {
                vmItems[i] = Message.Around1.Replace("$1", Program.GetFormatSizeString(((long)builtInViewerMemoryLimitBase >> 1) << i,
                    Message.Bytes, simpleForm: true, noFraction: true));
            }
            vmItems[vmItemsCount1] = Message.Automatic;

            gbSusiePlugins.Text = Message.SusiePlugins;
            cbAllowUntestedPlugins.Text = Message.AllowUntestedPlugins;
            cbSearchAlsoSusieInstallationFolder.Text = Message.SearchNotOnlyZipPlaFolderButAlsoSusieInstallationFolder.Replace(@"\n", "\n");
            
            void fitTwoCheckBoxes(GroupBox gb, params ButtonBase[] boxes)
            {
                var heights = (from box in boxes select box.Height).ToArray();
                var titleOffset = heights.Min();
                var totalSpace = gb.Height - titleOffset - heights.Sum();
                var space = totalSpace / (boxes.Length + 1);
                var y = titleOffset + (totalSpace - space * (boxes.Length - 1)) / 2;
                foreach (var box in boxes)
                {
                    box.Location = new Point(box.Location.X, y);
                    y += space + box.Height;
                }
            }
            fitTwoCheckBoxes(gbDragAndDrop, rbShowThumbnails, rbMoveOrCopy);
            fitTwoCheckBoxes(gbSusiePlugins, cbAllowUntestedPlugins, cbSearchAlsoSusieInstallationFolder);

            gbApplications.Text = Message.Applications;
            
            gbDynamicStringSelection.Text = Message.DynamicStringSelection;
            var displayName = Message.DisplayName;
            tbcDisplayName.MinimumWidth = TextRenderer.MeasureText(displayName + "  ", tbcDisplayName.HeaderCell.Style.Font).Width;
            tbcDisplayName.HeaderText = displayName;
            tbcDisplayNameForEmptyString.HeaderText = Message.DisplayNameForNonSelection;
            tbcCommand.HeaderText = Message.Command;

            cbDynamicStringSelectionEnabled.Text = Message.Enabled;
            cbDynamicStringSelectionAllowUserToRename.Text = Message.RenameItemByEditingTextBox;
            cbDynamicStringSelectionAllowUserToRename.Left = cbDynamicStringSelectionEnabled.Right + cbDynamicStringSelectionAllowUserToRename.Margin.Left;
            btnDynamicStringSelectionDefault.Text = Message.Default;
            btnDynamicStringSelectionAdd.Text = Message.Add;
            btnDynamicStringSelectionDelete.Text = Message.Delete;

            deleteToolStripMenuItem1.Text = Message.Delete;

            filterWithSelectedTextToolStripMenuItem.Text = Message.FilterWithSelectedText;
            copySelectedTextToolStripMenuItem.Text = Message.CopySelectedText;
            searchSelectedTextOnGoogleToolStripMenuItem.Text = Message.SearchSelectedTextOnGoogle;
            newCustomCommandToolStripMenuItem.Text = Message.NewCustomCommand;

            tbcName.HeaderText = Message.DisplayName;
            //using (var g = dgvApplications.CreateGraphics()) { tbcName.MinimumWidth = (int)Math.Ceiling(g.MeasureString(Message.Name, tbcName.HeaderCell.Style.Font).Width); }
            tbcCommandLineParameter.HeaderText = Message.Params;
            cbcMulti.HeaderText = Message.Multi;
            tbcShowInContextMenu.HeaderText = Message.ShowInContextMenu;
            tbcOpenByClicking.HeaderText = Message.OpenOnClick;
            //cbAlwaysEnableAccessKey.Text = Message.EnableAccessKeysInAlsoBookmark.Replace("&", "&&");

            //var w = btnDelete.Left - btnAdd.Right;
            //btnAccessKeyHint.Left = cbAlwaysEnableAccessKey.Right + w;
            btnAccessKeyHint.Text = null;
            btnAccessKeyHint.Width = btnAccessKeyHint.MinimumSize.Width;
            btnAccessKeyHint.Text = Message.AboutAccessKey;
            
            btnDefault.Text = Message.Default;
            btnAdd.Text = Message.Add;
            associatedApplicationToolStripMenuItem.Text = Message.AssociatedApplication;
            externalApplicationToolStripMenuItem.Text = Message.ExternalApplication;
            moveFilesfoldersToolStripMenuItem.Text = Message.MoveExtractSelectedItems;
            explorerToolStripMenuItem.Text = Message.Explorer;
            moveLocationToolStripMenuItem.Text = Message.CurrentZipPla;
            sendTheItemInTheClipboardToolStripMenuItem.Text = Message.ExecutableInClipboard;
            builtinBookReaderToolStripMenuItem.Text = Message.BuiltInViewer;
            btnDelete.Text = Message.Delete;
            deleteToolStripMenuItem.Text = Message.Delete;

            openFileDialogExecutableFile.Title = Message.ExternalApplication;

            ApplicationFilterDescription = Message.ApplicationFilterDescription;

            openFileDialogExecutableFile.Filter = "*.exe;*.bat;*.cmd;*.vbs;*.js;*.wsf;*.lnk|*.exe;*.bat;*.cmd;*.vbs;*.js;*.wsf;*.lnk" +
                "|" + Message.AllFiles + "(*.*)|*.*";

            setingMessage = true;
            dgvDynamicStringSelection.EndEdit();
            foreach (DataGridViewRowWithData row in dgvDynamicStringSelection.Rows)
            {
                switch (row.Path)
                {
                    case DynamicStringSelectionInfo.ToFilter:
                        row.Cells[tbcDisplayName.Index].Value = Message.FilterWith1;
                        row.Cells[tbcDisplayNameForEmptyString.Index].Value = Message.FilterWithSelectedText;
                        break;
                    case DynamicStringSelectionInfo.ToClipboard:
                        row.Cells[tbcDisplayName.Index].Value = Message.Copy1;
                        row.Cells[tbcDisplayNameForEmptyString.Index].Value = Message.CopySelectedText;
                        break;
                }
            }

            dgvApplications.EndEdit();
            foreach (DataGridViewRowWithData row in dgvApplications.Rows)
            {
                //row.Cells[tbcShowInContextMenu.Index].ToolTipText = Message.TargetItemsAreSpecifiedByRegularExpression;
                //row.Cells[tbcOpenByClicking.Index].ToolTipText = Message.TargetItemsAreSpecifiedByRegularExpression;
                switch (row.Path)
                {
                    case ApplicationInfo.AssociatedApplication:
                        //row.Cells[tbcName.Index].Value = Message.AssociatedApplication;
                        {
                            var alias = row.Alias;
                            var defaultName = getDefaultName(row);
                            var name = alias ?? defaultName;
                            if (alias != null)
                            {
                                if (name == defaultName) row.Alias = null;
                            }
                            var cell = row.Cells[tbcName.Index];
                            cell.Value = name;
                            cell.ToolTipText = Message.AssociatedApplication;
                        }
                        break;
                    case ApplicationInfo.BuiltInReader:
                        //row.Cells[tbcName.Index].Value = Message.BuiltInViewer;
                        {
                            var alias = row.Alias;
                            var defaultName = getDefaultName(row);
                            var name = alias ?? defaultName;
                            if (alias != null)
                            {
                                if (name == defaultName) row.Alias = null;
                            }
                            var cell = row.Cells[tbcName.Index];
                            cell.Value = name;
                            cell.ToolTipText = Message.BuiltInViewer;
                        }
                        break;
                    case ApplicationInfo.MoveLocation:
                        //row.Cells[tbcName.Index].Value = Message.CurrentZipPla;
                        {
                            var alias = row.Alias;
                            var defaultName = getDefaultName(row);
                            var name = alias ?? defaultName;
                            if (alias != null)
                            {
                                if (name == defaultName) row.Alias = null;
                            }
                            var cell = row.Cells[tbcName.Index];
                            cell.Value = name;
                            cell.ToolTipText = Message.CurrentZipPla;
                        }
                        break;
                    case ApplicationInfo.OpenInFileInClipboard:
                        row.Cells[tbcName.Index].Value = Message.ExecutableInClipboard;
                        break;
                    case ApplicationInfo.Explorer:
                        //row.Cells[tbcName.Index].Value = Message.Explorer;
                        {
                            var alias = row.Alias;
                            var defaultName = getDefaultName(row);
                            var name = alias ?? defaultName;
                            if (alias != null)
                            {
                                if (name == defaultName) row.Alias = null;
                            }
                            var cell = row.Cells[tbcName.Index];
                            cell.Value = name;
                            cell.ToolTipText = Message.CurrentZipPla;
                        }
                        break;
                    default:
                        {
                            //var path = row.Path;
                            var alias = row.Alias;
                            var name = GetName(row.Path, alias);
                            if (alias != null)
                            {
                                var defaultName = GetName(row.Path, null);
                                if (name == defaultName) row.Alias = null;
                            }
                            row.Cells[tbcName.Index].Value = name;
                            /*
                            if (path != null && path.Length > 0 && path.Last() == Path.DirectorySeparatorChar)
                            {
                                row.Cells[tbcName.Index].Value = Message.MoveTo1.Replace("$1", row.Alias);
                            }
                            */
                        }
                        break;
                }
            }
            setingMessage = false;
        }
        bool setingMessage = false;

        private static string getDefaultName(DataGridViewRowWithData row)
        {
            switch (row.Path)
            {
                case ApplicationInfo.AssociatedApplication: return Message.Open;
                case ApplicationInfo.BuiltInReader: return Message.OpenInBuiltInViewer;
                case ApplicationInfo.MoveLocation: return Message.OpenInCurrentWindow;
                case ApplicationInfo.Explorer: return Message.OpenInExplorer;
                default: return GetName(row.Path, null);
            }
        }

        private void SaveSettings()
        {
            var config = new GeneralConfig();
            config.Language = CurrentCultureInfo.ToString();
            //config.DefaultPageLeftToRight = CurrentPageLeftToRight;
            var currentMaximumNumberOfHistories = CurrentMaximumNumberOfHistories;
            var reducedMaximumNumberOfHistories = currentMaximumNumberOfHistories < config.MaximumNumberOfHistories;
            config.MaximumNumberOfHistories = currentMaximumNumberOfHistories;
            config.ShowHintOnlyOnce = CurrentDoNotShowAgain;
            config.CheckNgen = CurrentCheckNgen;
            config.DragAndDropAction = CurrentDragAndDrop;
            config.DynamicStringSelectionsEnabled = CurrentDynamicStringSelectionsEnabled;
            config.BuiltInViewerMemoryLimit = CurrentBuiltInViewerMemoryLimit;
            config.AllowUntestedPlugins = CurrentAllowUntestedPlugins;
            config.SearchAlsoSusieInstallationFolder = CurrentSearchAlsoSusieInstallationFolder;
            config.DynamicStringSelectionsAllowUserToRenameItem = CurrentDynamicStringSelectionsAllowUserToRenameItem;
            //config.StartWithAccessKey = CurrentStartWithAccessKey;
            config.DynamicStringSelections = CurrentDynamicStringSelections;
            config.Applications = CurrentApplications;
            var error = config.Save();
            if(error == null)
            {
                var updateConfig = new UpdateCheckConfig();
                updateConfig.Mode = CurrentUpdateCheckMode;
                error = updateConfig.Save();
            }
            if (error == null)
            {
                SettingChanged = false;
                if (reducedMaximumNumberOfHistories)
                {
                    try
                    {
                        HistoryTrimmed = VirtualFolder.TrimBookmarkData(Program.HistorySorPath, currentMaximumNumberOfHistories);
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
            }
            if (error != null)
            {
                MessageBox.Show(this, error.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private enum LoadSettings_Result { INIFileNotFound, INILoadError, Completed }
        private LoadSettings_Result LoadSettings()
        {
            /*
            if (!File.Exists(Configuration.XmlPath))
            {
                return LoadSettings_Result.INIFileNotFound;
            }
            */

            string language;
            //bool defaultPageLeftToRight;
            int maximumNumberOfHistories;
            bool doNotShowAgain, checkNgen;
            DragAndDropAction dragAndDrop;
            var config = new GeneralConfig();
            language = config.Language; if (string.IsNullOrEmpty(language)) language = Message.CurrentLanguage.ToString();
            //if (config.DefaultPageLeftToRight != null) defaultPageLeftToRight = (bool)config.DefaultPageLeftToRight; else defaultPageLeftToRight = LeftToRight;
            maximumNumberOfHistories = config.MaximumNumberOfHistories;
            doNotShowAgain = config.ShowHintOnlyOnce;
            checkNgen = config.CheckNgen;
            dragAndDrop = config.DragAndDropAction;

            var updateConfig = new UpdateCheckConfig();

            CultureInfo altLanguage = null;
            try
            {
                CurrentCultureInfo = Message.CultureInfoParse(language);
            }
            catch
            {
                altLanguage = Message.CurrentLanguage;
            }
            //CurrentPageLeftToRight = defaultPageLeftToRight;
            CurrentMaximumNumberOfHistories = maximumNumberOfHistories;
            CurrentDoNotShowAgain = doNotShowAgain;
            CurrentCheckNgen = checkNgen;
            bool enableNgen; try { enableNgen = NgenManager.ShouldBeInstalled(Application.ExecutablePath, defaultParentFolderName: "ZipPla_" + ZipPlaAssembly.Version) && NgenManager.GetNgenPath() != null; } catch { enableNgen = false; };
            if (!enableNgen) cbNgen.Enabled = false;
            CurrentDragAndDrop = dragAndDrop;
            CurrentDynamicStringSelections = config.DynamicStringSelections;
            CurrentDynamicStringSelectionsEnabled = config.DynamicStringSelectionsEnabled;
            CurrentBuiltInViewerMemoryLimit = config.BuiltInViewerMemoryLimit;
            CurrentAllowUntestedPlugins = config.AllowUntestedPlugins;
            CurrentSearchAlsoSusieInstallationFolder = config.SearchAlsoSusieInstallationFolder;
            CurrentDynamicStringSelectionsAllowUserToRenameItem = config.DynamicStringSelectionsAllowUserToRenameItem;
            //CurrentStartWithAccessKey = config.StartWithAccessKey;
            CurrentApplications = config.Applications;

            CurrentUpdateCheckMode = updateConfig.Mode ?? (updateConfig.LastCheckTime == DateTime.MinValue ? UpdateCheckMode.Default : UpdateCheckMode.Silent);

            //dgvDynamicStringSelection.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);

            SettingChanged = false;

            if (altLanguage != null) CurrentCultureInfo = altLanguage;

            return LoadSettings_Result.Completed;
        }

        private void SettingForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (updateChecking)
            {
                e.Cancel = true;
                return;
            }
            dgvApplications_CellValidating_canceled = false;
            dgvDynamicStringSelection_CellValidating_canceled = false;
            Validate();
            if (dgvApplications_CellValidating_canceled || dgvDynamicStringSelection_CellValidating_canceled)
            {
                e.Cancel = true;
                return;
            }
            if (SettingChanged)
            {
                if (MessageBox.Show(this, Message.DoYouDiscardChangedSettings, Message.Question, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                {
                    e.Cancel = true;
                }
                else if (!CurrentCultureInfo.Equals(preCultureInfo))
                {
                    Message.CurrentLanguage = preCultureInfo;
                    Program.multiLangForm.SetMessages();
                }
            }
        }
        
        private void btnOK_Click(object sender, EventArgs e)
        {
            //var preL2R = prePageLeftToRight;
            SaveSettings();
            //NeedToReloadCatalog = preL2R != prePageLeftToRight;
            Close();
            try
            {
                var index = cbLanguage.SelectedIndex;
                var newLang = availableLanguages[index];
                //if (!Message.CurrentLanguage.Equals(newLang)) // 言語ファイルの更新を想定してコメントアウト
                {
                    if(Message.AvailableLanguages.Contains(newLang))
                    {
                        Message.CurrentLanguage = newLang;
                    }
                    else
                    {
                        Message.CurrentLanguage = Message.DefaultLanguage;
                        MessageBox.Show(this, $"Language data of \"{newLang.NativeName}\" is not found.", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                Program.multiLangForm.SetMessages();
            }
            catch (Exception error)
            {
                MessageBox.Show(this, error.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cbLanguage_SelectedValueChanged(object sender, EventArgs e)
        {
            try
            {
                var index = cbLanguage.SelectedIndex;
                //var temp = Message.CurrentLanguage;
                if (cbLanguage.SelectedItem?.ToString() != availableLanguages[index].NativeName)
                {
                    return;
                }
                Message.CurrentLanguage = availableLanguages[index];
                SetMessages();
                Program.multiLangForm.SetMessages();
                //setCurrentLanguage(temp);
            }
            catch (Exception error)
            {
                MessageBox.Show(this, error.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void cbLanguage_MouseEnter(object sender, EventArgs e)
        {
            //cbLanguage.Focus();
        }
        

        private void cbLanguage_DropDown(object sender, EventArgs e)
        {
            ResetCbLanguage();
            if(cbLanguage.SelectedItem == null)
            {
                cbLanguage.SelectedItem = Message.DefaultLanguage.NativeName;
            }
        }

        private void openlanguageFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(Message.LanguageFileFolder);
            }
            catch (Exception error)
            {
                MessageBox.Show(this, error.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void updateAndEditLanguageFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //var temp = Message.CurrentLanguage;
            var save = CurrentCultureInfo;
            try
            {
                setCurrentLanguage(save);
                var path = Message.UpdateCurrentLanguage(true);
                //setCurrentLanguage(temp);
                System.Diagnostics.Process.Start("notepad", $"\"{path}\"");
            }
            catch(Exception error)
            {
                MessageBox.Show(this, error.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void setCurrentLanguage(CultureInfo language)
        {
            if (Message.AvailableLanguages.Contains(language))
            {
                Message.CurrentLanguage = language;
            }
            else
            {
                throw new Exception($"Language data of \"{language.NativeName}\" is not found.");
            }
        }

        private void btnLanguageEdit_Click(object sender, EventArgs e)
        {
            cmsLanguageEdit.Show(Cursor.Position);
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            var paths = (from info in CurrentApplications let path = info.Path where !string.IsNullOrEmpty(path) && (path.First() == '<') select path).ToArray();

            associatedApplicationToolStripMenuItem.Enabled = !paths.Contains(ApplicationInfo.AssociatedApplication);
            //externalApplicationToolStripMenuItem.Enabled = !paths.Contains(ApplicationInfo.);
            explorerToolStripMenuItem.Enabled = !paths.Contains(ApplicationInfo.Explorer);
            moveLocationToolStripMenuItem.Enabled = !paths.Contains(ApplicationInfo.MoveLocation);
            sendTheItemInTheClipboardToolStripMenuItem.Enabled = !paths.Contains(ApplicationInfo.OpenInFileInClipboard);
            builtinBookReaderToolStripMenuItem.Enabled = !paths.Contains(ApplicationInfo.BuiltInReader);

            // 表示項目が一つならいきなりそれを呼び出す処理
            // 現在表示項目は一つにならないので不要
            /*
            var cands = (from ToolStripItem item in cmsApplicationAdd.Items where item is ToolStripMenuItem && item.Enabled select item).ToArray();
            if (cands.Length == 1)
            {
                cands[0].PerformClick();
            }
            else
            */
            {
                cmsApplicationAdd.Show(Cursor.Position);
            }
        }
        
        public const string AnyPattern = @"*;\";

        private void associatedApplicationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addApplicationRow(ApplicationInfoToRow(GeneralConfig.GetDefaultApplications().First(i => i.Path == ApplicationInfo.AssociatedApplication)));

            //addApplicationRow(ApplicationInfo.AssociatedApplication, null, Message.AssociatedApplication, "", AnyPattern, AnyPattern, null, tbcCommandLineParameter);
        }

        private void externalApplicationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Program.SetInitialCondition(openFileDialogExecutableFile, CurrentExternalViewer);
            if (openFileDialogExecutableFile.ShowDialog() == DialogResult.OK)
            {
                var path = openFileDialogExecutableFile.FileName;
                addApplicationRow(path, null, true, path, GetName(path, null), "", false, AnyPattern, AnyPattern, tbcOpenByClicking);
            }
        }
        
        private void moveFilesfoldersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialogMoveFilesFolders.ShowDialog() == DialogResult.OK)
            {
                var path = folderBrowserDialogMoveFilesFolders.SelectedPath;
                var tooltip = path;
                if (string.IsNullOrEmpty(path)) return;
                if (path.Last() != Path.DirectorySeparatorChar) path += Path.DirectorySeparatorChar;
                else tooltip = tooltip.Substring(0, tooltip.Length - 1);
                addApplicationRow(path, null, true, tooltip, GetName(path, null), "", false, AnyPattern, "", null, tbcCommandLineParameter, cbcMulti);
            }
        }

        private static string GetName(string path, string alias)
        {
            return ExternalApplicationProvider.GetName(path, Message.OpenIn1, Message.MoveExtractSelectedItemsTo1, alias);
        }

        private void explorerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addApplicationRow(ApplicationInfoToRow(GeneralConfig.GetDefaultApplications().First(i => i.Path == ApplicationInfo.Explorer))); //;ApplicationInfo.Explorer)));
        }

        private void moveLocationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addApplicationRow(ApplicationInfoToRow(GeneralConfig.GetDefaultApplications().First(i => i.Path == ApplicationInfo.MoveLocation)));
        }

        private void sendTheItemInTheClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addApplicationRow(ApplicationInfoToRow(new ApplicationInfo(ApplicationInfo.OpenInFileInClipboard, null, null, "", AnyPattern, false)));
        }
        
        private void builtinBookReaderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addApplicationRow(ApplicationInfoToRow(GeneralConfig.GetDefaultApplications().First(i => i.Path == ApplicationInfo.BuiltInReader)));
        }

        private void addApplicationRow(string path, string alias, bool enabledAlias, string tooltip, string name, string param, bool multi, string support, string click, DataGridViewColumn edit = null, params DataGridViewColumn[] readOnly)
        {
            addApplicationRow(getApplicationRow(path, alias, enabledAlias, tooltip, name, param, multi, support, click), edit, readOnly);
        }
        private void addApplicationRow(DataGridViewRow row, DataGridViewColumn edit = null, params DataGridViewColumn[] readOnly)
        {
            var rows = dgvApplications.Rows;
            var currentRow = dgvApplications.CurrentRow;
            var currentIndex = currentRow != null ? currentRow.Index : rows.Count - 1;
            dgvApplications.Rows.Insert(++currentIndex, row);
            if (readOnly != null)
            {
                foreach (var c in readOnly)
                {
                    var cell = dgvApplications.Rows[currentIndex].Cells[c.Index];
                    cell.ReadOnly = true;

                    cell.Style.BackColor = Color.LightGray;
                }
            }
            if (edit != null)
            {
                dgvApplications.CurrentCell = dgvApplications.Rows[currentIndex].Cells[edit.Index];
                dgvApplications.BeginEdit(true);
            }
            else
            {
                foreach (DataGridViewRow r in rows) r.Selected = r == row;
            }
        }

        private DataGridViewRow getApplicationRow(string path, string alias, bool enabledAlias, string tooltip, string name, string param, bool multi, string support, string click)
        {
            var result = new DataGridViewRowWithData();
            result.CreateCells(dgvApplications);
            result.Path = path;
            result.Alias = alias;
            result.Cells[tbcName.Index].Value = name;
            if (!enabledAlias)
            {
                result.Cells[tbcName.Index].Style.BackColor = Color.LightGray;
                result.Cells[tbcName.Index].ReadOnly = true;
            }
            result.Cells[tbcName.Index].ToolTipText = tooltip;
            result.Cells[tbcCommandLineParameter.Index].Value = param;
            result.Cells[cbcMulti.Index].Value = multi;
            result.Cells[tbcShowInContextMenu.Index].Value = support;
            //result.Cells[tbcShowInContextMenu.Index].ToolTipText = Message.TargetItemsAreSpecifiedByRegularExpression;
            result.Cells[tbcOpenByClicking.Index].Value = click;
            //result.Cells[tbcOpenByClicking.Index].ToolTipText = Message.TargetItemsAreSpecifiedByRegularExpression;
            return result;
        }

        private DataGridViewRow getToFilterRow()
        {
            var result = new DataGridViewRowWithData();

            result.CreateCells(dgvDynamicStringSelection);

            result.Path = DynamicStringSelectionInfo.ToFilter;

            var cell = result.Cells[tbcDisplayName.Index];
            cell.Value = Message.FilterWith1;
            cell.Style.BackColor = Color.LightGray;
            cell.ReadOnly = true;

            cell = result.Cells[tbcDisplayNameForEmptyString.Index];
            cell.Value = Message.FilterWithSelectedText;
            cell.Style.BackColor = Color.LightGray;
            cell.ReadOnly = true;

            cell = result.Cells[tbcCommand.Index];
            cell.Value = "";
            cell.Style.BackColor = Color.LightGray;
            cell.ReadOnly = true;

            return result;
        }

        private DataGridViewRow getToClipboardRow()
        {
            var result = new DataGridViewRowWithData();

            result.CreateCells(dgvDynamicStringSelection);

            result.Path = DynamicStringSelectionInfo.ToClipboard;

            var cell = result.Cells[tbcDisplayName.Index];
            cell.Value = Message.Copy1;
            cell.Style.BackColor = Color.LightGray;
            cell.ReadOnly = true;

            cell = result.Cells[tbcDisplayNameForEmptyString.Index];
            cell.Value = Message.CopySelectedText;
            cell.Style.BackColor = Color.LightGray;
            cell.ReadOnly = true;

            cell = result.Cells[tbcCommand.Index];
            cell.Value = "";
            cell.Style.BackColor = Color.LightGray;
            cell.ReadOnly = true;

            return result;
        }
        
        private DataGridViewRow getCustomCommandRow(string displayName, string displayNameForEmptyString, string command)
        {
            var result = new DataGridViewRowWithData();

            result.CreateCells(dgvDynamicStringSelection);

            result.Path = DynamicStringSelectionInfo.CustomCommand;

            var cell = result.Cells[tbcDisplayName.Index];
            cell.Value = displayName;

            cell = result.Cells[tbcDisplayNameForEmptyString.Index];
            cell.Value = displayNameForEmptyString;

            cell = result.Cells[tbcCommand.Index];
            cell.Value = command;

            return result;
        }

        class DataGridViewRowWithData : DataGridViewRow
        {
            public string Path;
            public string Alias;
        }

        private void dgvApplications_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvAppSorter == null || !dgvAppSorter.InRowsHolded) btnDelete.Enabled = dgvApplications.SelectedRows.Count > 0;
        }

        private void dgvDynamicStringSelection_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvDssSorter == null || !dgvDssSorter.InRowsHolded) btnDynamicStringSelectionDelete.Enabled = CurrentDynamicStringSelectionsEnabled && dgvDynamicStringSelection.SelectedRows.Count > 0;
        }

        private void dgvApplications_MouseDown(object sender, MouseEventArgs e)
        {
            var location = e.Location;
            var hit = dgvApplications.HitTest(location.X, location.Y);
            if (hit.RowIndex < 0 || hit.ColumnIndex < 0)
            {
                Validate();
                foreach (DataGridViewRow row in dgvApplications.Rows)
                {
                    row.Selected = false;
                }
            }
            else if (e.Button == MouseButtons.Right && !dgvAppSorter.InRowsHolded && !dgvApplications.IsCurrentCellInEditMode)
            {
                dgvApplications_CellValidating_canceled = false;
                Validate();
                if (!dgvApplications_CellValidating_canceled)
                {
                    if (!dgvApplications.Rows[hit.RowIndex].Cells[hit.ColumnIndex].Selected)
                    {
                        foreach (DataGridViewRow row in dgvApplications.Rows)
                        {
                            row.Selected = row.Index == hit.RowIndex;
                        }
                    }
                    cmsDelete.Show(Cursor.Position);
                }
            }
        }

        private void dgvDynamicStringSelection_MouseDown(object sender, MouseEventArgs e)
        {
            var location = e.Location;
            var hit = dgvDynamicStringSelection.HitTest(location.X, location.Y);
            if (hit.RowIndex < 0 || hit.ColumnIndex < 0)
            {
                Validate();
                foreach (DataGridViewRow row in dgvDynamicStringSelection.Rows)
                {
                    row.Selected = false;
                }
            }
            else if (e.Button == MouseButtons.Right && !dgvDssSorter.InRowsHolded && !dgvDynamicStringSelection.IsCurrentCellInEditMode)
            {
                dgvDynamicStringSelection_CellValidating_canceled = false;
                Validate();
                if (!dgvDynamicStringSelection_CellValidating_canceled)
                {
                    if (!dgvDynamicStringSelection.Rows[hit.RowIndex].Cells[hit.ColumnIndex].Selected)
                    {
                        foreach (DataGridViewRow row in dgvDynamicStringSelection.Rows)
                        {
                            row.Selected = row.Index == hit.RowIndex;
                        }
                    }
                    cmsDynamicStringSelection.Show(Cursor.Position);
                }
            }
        }

        bool dgvApplications_CellValidating_canceled;
        private void dgvApplications_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (Program.CancelMovingeEditingCellByLeftOrRightKey(sender, e)) return;

            DataGridView dgv = (DataGridView)sender;

            //新しい行のセルでなく、セルの内容が変更されている時だけ検証する
            if (e.RowIndex == dgv.NewRowIndex || !dgv.IsCurrentCellDirty)
            {
                return;
            }

            if (e.ColumnIndex == tbcShowInContextMenu.Index || e.ColumnIndex == tbcOpenByClicking.Index)
            {
                var newName = $"{e.FormattedValue}";
                var trimedNewName = newName.Trim();
                if (trimedNewName == "" && e.ColumnIndex == tbcShowInContextMenu.Index) trimedNewName = AnyPattern;

                if (!ApplicationInfo.Parsable(trimedNewName))
                {
                    dgvAppSorter.DoMouseUp();
                    MessageBox.Show(this, ApplicationFilterDescription.Replace(@"\n", "\n"), null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    dgvApplications_CellValidating_canceled = true;
                    dgv.CancelEdit();
                }
            }
            /*
            else if (e.ColumnIndex == tbcName.Index)
            {
                var newName = $"{e.FormattedValue}";
                var trimedNewName = newName.Trim();
                var row = dgv.Rows[e.RowIndex] as DataGridViewRowWithData;
                if (trimedNewName == "" || trimedNewName == GetName(row.Path, null)) trimedNewName = null;
            }
            */
        }

        bool dgvDynamicStringSelection_CellValidating_canceled;
        string dgvDynamicStringSelection_preValidating = null;
        private void dgvDynamicStringSelection_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (Program.CancelMovingeEditingCellByLeftOrRightKey(sender, e)) return;

            dgvDynamicStringSelection_preValidating = dgvDynamicStringSelection[e.ColumnIndex, e.RowIndex].Value as string;
            // すべて受け付ける
            /*
            DataGridView dgv = (DataGridView)sender;

            //新しい行のセルでなく、セルの内容が変更されている時だけ検証する
            if (e.RowIndex == dgv.NewRowIndex || !dgv.IsCurrentCellDirty)
            {
                return;
            }

            var newName = $"{e.FormattedValue}";
            var trimedNewName = newName.Trim();
            */

        }


        private void dgvApplications_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || setingMessage) return;
            DataGridView dgv = (DataGridView)sender;
            if (e.ColumnIndex == tbcOpenByClicking.Index || e.ColumnIndex == tbcShowInContextMenu.Index)
            {
                var row = dgv.Rows[e.RowIndex];
                var cell = row.Cells[e.ColumnIndex];
                var newName = $"{cell.Value}";
                var trimedNewName = newName.Trim();
                if (trimedNewName == "" && e.ColumnIndex == tbcShowInContextMenu.Index) trimedNewName = AnyPattern;
                if (trimedNewName != newName)
                {
                    cell.Value = trimedNewName;
                }
            }
            else if (e.ColumnIndex == tbcName.Index)
            {
                var row = dgv.Rows[e.RowIndex] as DataGridViewRowWithData;
                var cell = row.Cells[e.ColumnIndex];
                var newName = $"{cell.Value}";
                var fixedNewName = newName.Trim();
                var defaultName = getDefaultName(row);// row.Path.Contains('<') ? cell.ToolTipText : GetName(row.Path, null);
                bool isDefault = fixedNewName == "" || fixedNewName == defaultName;
                if (isDefault)
                {
                    row.Alias = null;
                    cell.Value = defaultName;
                }
                else
                {
                    row.Alias = fixedNewName;
                    cell.Value = fixedNewName;
                }
            }
        }

        //public const string BookImagePattern = @"zip;rar;pdf;7z;tar;bmp;gif;jpg;jpeg;png;exf;tiff;tif;ico;emf;wmf";
        //public const string BookPattern = @"zip;rar;pdf;7z;tar;\";
        
        //public static readonly string BookAndBookPagePattern = @"/(\.(" + PackedImageLoader.SupportedArchiveFileFilter.Substring(2).Replace(";*.", "|") + @")|\\)$|\//i";
        public const string FilePattern = @"*";
        //public const string FolderPattern = @"sor;\";
        public const string FolderPattern = @"\";

        private void btnDelete_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvApplications.SelectedRows)
            {
                dgvApplications.Rows.Remove(row);
            }
        }

        private void btnDefault_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, Message.DoYouRestoreDefaultSettingsOfApplications, Message.Question, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                CurrentApplications = GeneralConfig.GetDefaultApplications();
            }
        }

        private void dgvDynamicStringSelection_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0 || setingMessage) return;
            DataGridView dgv = (DataGridView)sender;
            var rows = dgv.Rows;
            var row = rows[e.RowIndex];
            var cell = row.Cells[e.ColumnIndex];
            var newName = $"{cell.Value}";
            var trimedNewName = newName.Trim();
            if (trimedNewName != newName)
            {
                setingMessage = true;
                cell.Value = trimedNewName;
                setingMessage = false;
            }
            if (e.ColumnIndex == tbcCommand.Index)
            {
                ShowDynamiStringSelectionUrlWarning(e.RowIndex, calledByUrlAdder: false);
            }
        }

        private void ShowDynamiStringSelectionUrlWarning(int rowIndex, bool calledByUrlAdder)
        {
            var tbcCommandIndex = tbcCommand.Index;

            // 変更後が URL でなければリターン
            if (!calledByUrlAdder)
            {
                var row = dgvDynamicStringSelection.Rows[rowIndex] as DataGridViewRowWithData;
                if (row?.Path != DynamicStringSelectionInfo.CustomCommand) return;
                if (!DynamicStringSelectionCustomCommandToolStripMenuItem.IsUrlCommand(row.Cells[tbcCommandIndex].Value as string)) return;

                // 既に URL が登録されていればリターン
                if (DynamicStringSelectionCustomCommandToolStripMenuItem.IsUrlCommand(dgvDynamicStringSelection_preValidating)) return;
            }

            // 既に URL が登録されていればリターン
            var count = dgvDynamicStringSelection.RowCount;
            for (var i = 0; i < count; i++)
            {
                if (i == rowIndex) continue;
                var row2 = dgvDynamicStringSelection.Rows[i] as DataGridViewRowWithData;
                if (row2?.Path != DynamicStringSelectionInfo.CustomCommand) continue;
                if (DynamicStringSelectionCustomCommandToolStripMenuItem.IsUrlCommand(row2.Cells[tbcCommandIndex].Value as string)) return;
            }

            MessageBox.Show(this, Message.IfUrlIsRegisteredZipPlaAccessesInternet, Message.Information, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void deleteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            btnDynamicStringSelectionDelete.PerformClick();
        }

        private void cbDynamicStringSelectionEnabled_CheckedChanged(object sender, EventArgs e)
        {
            var enabled = cbDynamicStringSelectionEnabled.Checked;

            if(!enabled)
            {
                foreach (DataGridViewRow row in dgvDynamicStringSelection.Rows) row.Selected = false;
            }
            cbDynamicStringSelectionAllowUserToRename.Enabled = enabled;
            dgvDynamicStringSelection.Enabled = enabled;
            btnDynamicStringSelectionAdd.Enabled = enabled;
            //btnDynamicStringSelectionDefault.Enabled = enabled;
            btnDynamicStringSelectionDelete.Enabled = enabled && dgvDynamicStringSelection.SelectedRows.Count > 0;
        }

        private void btnDynamicStringSelectionDefault_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(this, Message.DoYouRestoreDefaultSettingOfDynamicStringSelection, Message.Question, MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
            {
                CurrentDynamicStringSelectionsEnabled = true;
                CurrentDynamicStringSelections = GeneralConfig.DefaultDynamicStringSelections;
            }
        }
        
        private static DynamicStringSelectionInfo getGoogleInfo()
        {
            return new DynamicStringSelectionInfo
            {
                Type = DynamicStringSelectionInfo.CustomCommand,
                DisplayName = Message.Search1OnGoogle,
                DisplayNameForEmptyString = Message.SearchSelectedTextOnGoogle,
                Command = "\"https://www.google.com/search?q=$1\""
            };
        }

        private void btnDynamicStringSelectionAdd_Click(object sender, EventArgs e)
        {
            var currentDynamicStringSelections = CurrentDynamicStringSelections;
            var types = (from info in currentDynamicStringSelections select info.Type).ToArray();

            filterWithSelectedTextToolStripMenuItem.Enabled = !types.Contains(DynamicStringSelectionInfo.ToFilter);
            copySelectedTextToolStripMenuItem.Enabled = !types.Contains(DynamicStringSelectionInfo.ToClipboard);
            searchSelectedTextOnGoogleToolStripMenuItem.Enabled = !currentDynamicStringSelections.Contains(getGoogleInfo());

            var cands = (from ToolStripItem item in cmsDDSAdd.Items where item is ToolStripMenuItem && item.Enabled select item).ToArray();
            if (cands.Length == 1)
            {
                cands[0].PerformClick();
            }
            else
            {
                cmsDDSAdd.Show(Cursor.Position);
            }
        }

        private void filterWithSelectedTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addDSSRow(getToFilterRow());
        }

        private void copySelectedTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addDSSRow(getToClipboardRow());
        }

        private void searchSelectedTextOnGoogleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowDynamiStringSelectionUrlWarning(
                rowIndex: addDSSRow(DynamicStringSelectionInfoToRow(getGoogleInfo())), calledByUrlAdder: true);
        }

        private void newCustomCommandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            addDSSRow(getCustomCommandRow("", "", ""), edit: tbcDisplayName);
        }

        private int addDSSRow(DataGridViewRow row, DataGridViewColumn edit = null)
        {
            var rows = dgvDynamicStringSelection.Rows;
            var currentRow = dgvDynamicStringSelection.CurrentRow;
            var currentIndex = currentRow != null ? currentRow.Index : rows.Count - 1;
            rows.Insert(++currentIndex, row);
            if (edit != null)
            {
                dgvDynamicStringSelection.CurrentCell = rows[currentIndex].Cells[edit.Index];
                dgvDynamicStringSelection.BeginEdit(true);
            }
            else
            {
                foreach (DataGridViewRow r in rows) r.Selected = r == row;
            }
            return currentIndex;
        }

        private void btnDynamicStringSelectionDelete_Click(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dgvDynamicStringSelection.SelectedRows)
            {
                dgvDynamicStringSelection.Rows.Remove(row);
            }
        }

        private void SettingForm_SizeChanged(object sender, EventArgs e)
        {
            setDataGridViewHeight();
        }

        private void dgvDynamicStringSelection_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            setDataGridViewHeight();
        }

        private void dgvDynamicStringSelection_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            setDataGridViewHeight();
        }

        private void dgvApplications_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            setDataGridViewHeight();
        }

        private void dgvApplications_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            setDataGridViewHeight();
        }

        private void cbHistory_MouseEnter(object sender, EventArgs e)
        {
            //cbHistory.Focus();
        }

        private void cbViewerMemoryUsage_MouseEnter(object sender, EventArgs e)
        {
            //cbViewerMemoryUsage.Focus();
        }
        
        private void btnViewerMemoryUsageHelp_Click(object sender, EventArgs e)
        {
            //MessageBox.Show(this, Message.BuiltInViewerMemoryUsageDescription.Replace("\\n", "\n"), Message.BuiltInViewerMemoryUsage, MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            ShowPopup(sender, Message.BuiltInViewerMemoryUsageDescription.Replace("\\n", "\n"));
        }

        private void ShowPopup(object sender, string message)
        {
            var p = MousePosition;
            if (sender is Control control)
            {
                var bounds = control.RectangleToScreen(control.DisplayRectangle);
                if (!bounds.Contains(p)) p = new Point((bounds.Left + bounds.Right) / 2, (bounds.Top + bounds.Bottom) / 2);
            }

            Help.ShowPopup(this, message, p);
        }

        private void cbNgen_CheckedChanged(object sender, EventArgs e)
        {
            if (!cbNgen.Checked)
            {
                var zipplaPath = Application.ExecutablePath;
                var yesIsClicked = false;
                try
                {
                    if (NgenManager.ShouldBeInstalled(zipplaPath, defaultParentFolderName: "ZipPla_" + ZipPlaAssembly.Version))
                    {
                        var ngenPath = NgenManager.GetNgenPath();
                        if (ngenPath != null &&
                            NgenManager.LostNativeImageExists(ngenPath, "ZipPla", zipplaPath) &&
                            MessageForm.Show(this, Message.DoYouRemoveNativeImage.Replace(@"\n", "\n"), Message.Question,
                            MessageForm.ShieldPrefix + Message._Yes, Message._No, MessageBoxIcon.Question) == 0)
                        {
                            yesIsClicked = true;
                            var result = NgenManager.UninstallByOtherProcess(zipplaPath, ngenPath, zipplaPath);
                            if (result != 0)
                            {
                                // 「エラー」をシステムが決めた言語で表示させるためにこちらは使わない
                                //MessageForm.Show(StartForm, Message.FailInNgen.Replace(@"\n", "\n").Replace("$1", result.ToString()), null,
                                //    "OK", MessageBoxIcon.Error);
                                MessageBox.Show(this, Message.FailInNgen.Replace(@"\n", "\n").Replace("$1", result.ToString()), null,
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
                catch (ObjectDisposedException) { }
                catch (Exception ex)
                {
                    // 最も重要な例外は UAC がキャンセルされた場合の System.ComponentModel.Win32Exception
                    if (yesIsClicked) MessageBox.Show(this, ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnCheckNow_Click(object sender, EventArgs e)
        {
            UpdateCheck();
        }

        private bool updateChecking = false;
        private async void UpdateCheck()
        {
            if (updateChecking)
            {
                return;
            }
            btnCheckNow.Text = Message.Checking + "...";
            updateChecking = true;
            btnCheckNow.Enabled = btnOK.Enabled = btnCancel.Enabled = false;
            await Program.CheckUpdateAsync(this);
            btnCheckNow.Text = Message.CheckNow;
            updateChecking = false;
            btnCheckNow.Enabled = btnOK.Enabled = btnCancel.Enabled = true;
        }

        private void btnAccessKeyHint_Click(object sender, EventArgs e)
        {
            ShowPopup(sender, Message.ApplicationAccessKeyUsageDescription.Replace("\\n", "\n"));
        }

        private void SettingForm_Shown(object sender, EventArgs e)
        {
            new BetterFormRestoreBounds(this);
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnDelete.PerformClick();
        }

        //public static readonly string ExplorerPath = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
    }

    public class GeneralConfig : Configuration
    {
        public string Language;
        //public bool? DefaultPageLeftToRight = null;
        public int MaximumNumberOfHistories = 100;
        public bool ShowHintOnlyOnce = false;
        public bool CheckNgen = false;
        public DragAndDropAction DragAndDropAction = DragAndDropAction.ShowThumbnails;
        public ulong BuiltInViewerMemoryLimit = long.MaxValue;
        public bool AllowUntestedPlugins = false;
        public bool SearchAlsoSusieInstallationFolder = false;
        public string ReadHintVersion = null;
        public bool DynamicStringSelectionsEnabled = true;
        public bool DynamicStringSelectionsAllowUserToRenameItem = false;
        //public bool StartWithAccessKey = false;
        public DynamicStringSelectionInfo[] DynamicStringSelections = DefaultDynamicStringSelections;
        public ApplicationInfo[] Applications = GetDefaultApplications();

        public static readonly DynamicStringSelectionInfo[] DefaultDynamicStringSelections = new DynamicStringSelectionInfo[]
        {
            new DynamicStringSelectionInfo { Type = DynamicStringSelectionInfo.ToFilter },
            new DynamicStringSelectionInfo { Type = DynamicStringSelectionInfo.ToClipboard },
        };
        
        public static ApplicationInfo[] GetDefaultApplications()
        {
            //DefaultApplications[3].Alias = Message.OpenInExplorer;
            return DefaultApplications;
        }

        private static readonly ApplicationInfo[] DefaultApplications = new ApplicationInfo[]
        {
            //new ApplicationInfo(ApplicationInfo.BuiltInReader, null, SettingForm.BookAndBookPagePattern, SettingForm.AnyPattern, false),
            //new ApplicationInfo(ApplicationInfo.BuiltInReader, null, null, SettingForm.AnyPattern, SettingForm.AnyPattern, false), // 内蔵ビューアの強化により画像でもこれをデフォルトに。ただしフィルタのチュートリアルの役割が持てないのが難点
            new ApplicationInfo(ApplicationInfo.BuiltInReader, null, null, SettingForm.FilePattern, SettingForm.AnyPattern, false), // フォルダを内蔵ビューアで直接開くメリットが希薄と感じられるためこちらに変更。
            new ApplicationInfo(ApplicationInfo.AssociatedApplication, null, null, SettingForm.FilePattern, SettingForm.AnyPattern, false),
            new ApplicationInfo(ApplicationInfo.MoveLocation, null, null, SettingForm.AnyPattern, SettingForm.AnyPattern, false),
            new ApplicationInfo(ApplicationInfo.Explorer, null, "/select,", "", SettingForm.AnyPattern, false),
            //new ApplicationInfo(SettingForm.ExplorerPath, null, "/select,", "", SettingForm.AnyPattern, false),
        };
    }
}
