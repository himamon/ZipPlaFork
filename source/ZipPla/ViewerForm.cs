using Alteridem.WinTouch;
using Common;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public partial class ViewerForm : Form, IMultipleLanguages
    {
        class ViewerFormImageInfo : ImageInfo
        {
            public int? MaxColorDiff;

            public ViewerFormImageInfo(ImageInfo imageInfo, int? maxColorDiff) : base(imageInfo.Size, imageInfo.BitPerPixel)
            {
                MaxColorDiff = maxColorDiff;
            }
        }

        //private int[] EntryNumberArray = null;
        private PackedImageLoader.PackedImageEntry[] EntryArray = null;
        private string[] EntryLongPathArray = null;
        private BitmapEx[] PreFilteredImageArray = null;
        private ViewerFormImageInfo[] OriginalImageInfoArray = null;
        private Size[] ResizedSizeArray = null;
        private VirtualBitmapEx[] ResizedImageArray = null;
        private int currentPage_field = -1;
        private bool onePageModeForNext = false;
        private int pageDirection = -1;

        private bool TopMostInFullscreen = false;

        private ReadOnMemoryMode readOnMemoryMode = ReadOnMemoryMode.None;
        private ReadOnMemoryMode ReadOnMemoryMode
        {
            get => readOnMemoryMode;
            set
            {
                if (value != readOnMemoryMode)
                {
                    readOnMemoryMode = value;
                    readOnMemoryNoneToolStripMenuItem.Checked = value == ReadOnMemoryMode.None;
                    readOnMemoryExceptLookAheadToolStripMenuItem.Checked = value == ReadOnMemoryMode.ExceptLookAhead;
                    readOnMemoryAlwaysToolStripMenuItem.Checked = value == ReadOnMemoryMode.Always;
                }
            }
        }

        private ViewSettingHintMode viewSettingHintMode = ViewSettingHintMode.NeverShow;
        private ViewSettingHintMode ViewSettingHintMode
        {
            get => viewSettingHintMode;
            set
            {
                if (value != viewSettingHintMode)
                {
                    viewSettingHintMode = value;
                    showAHintWhenChangingSettingsToolStripMenuItem.Checked = value == ViewSettingHintMode.ShowWhenChangingSettings;
                }
            }
        }

        private int MaxPageCountInWindow = 2;
        
        private CoverBindingMode coverBindingMode = CoverBindingMode.Default;
        private CoverBindingMode CoverBindingMode
        {
            get
            {
                return coverBindingMode;
            }
            set
            {
                if (value != coverBindingMode)
                {
                    coverBindingMode = value;
                    forceFirstPageToBeSingleToolStripMenuItem.Checked = coverBindingMode == CoverBindingMode.ForceSingle;
                }
            }
        }
        
        ArchivesInArchiveMode archivesInArchiveMode = ZipPla.ArchivesInArchiveMode.Ignore;
        private ArchivesInArchiveMode ArchivesInArchiveMode
        {
            get
            {
                return archivesInArchiveMode;
            }
            set
            {
                if (value != archivesInArchiveMode)
                {
                    archivesInArchiveMode = value;
                    nonerecommendedToolStripMenuItem.Checked = value == ArchivesInArchiveMode.Ignore;
                    //onlyIfThereIsNoOtherImageToolStripMenuItem.Checked = value == ArchivesInArchiveMode.IfNoOther1Level;
                    untilFoundToolStripMenuItem.Checked = value == ArchivesInArchiveMode.UntilFound2Level;
                    alwaysLoadArchivesInArchiveToolStripMenuItem.Checked = value == ArchivesInArchiveMode.Always1Level;
                }
            }
        }

        //private MagnifierScrollMode MagnifierScrollMode = MagnifierScrollMode.Location;

        private int minPageCountInWindow = 1;
        private int MinPageCountInWindow
        {
            get { return minPageCountInWindow; }
            set
            {
                if (value != minPageCountInWindow)
                {
                    minPageCountInWindow = value;
                    forceTwoPageSpreadToolStripMenuItem.Checked = value > 1;
                }
            }
        }

        private int maxDivision = 1;
        private int MaxDivision
        {
            get { return maxDivision; }
            set
            {
                if (maxDivision != value)
                {
                    maxDivision = value;
                    if (value <= CurrentDividedPosition) CurrentDividedPosition = value - 1;
                    allowPageDivisionToolStripMenuItem.Checked = value > 1;
                }
            }
        }
        private int CurrentDividedPosition = 0;

        private GammaConversion resizeGammaNormal = GammaConversion.Value1_0;
        private GammaConversion ResizeGammaNormal
        {
            get { return resizeGammaNormal; }
            set
            {
                if (value != resizeGammaNormal)
                {
                    resizeGammaNormal = value;
                    linearizeColorSpaceforGamma22NormalToolStripMenuItem.Checked = value == GammaConversion.Value2_2;
                }
            }
        }

        private GammaConversion resizeGammaMagnifier = GammaConversion.Value1_0;
        private GammaConversion ResizeGammaMagnifier
        {
            get { return resizeGammaMagnifier; }
            set
            {
                if (value != resizeGammaMagnifier)
                {
                    resizeGammaMagnifier = value;
                    linearizeColorSpaceforGamma22MagnifierToolStripMenuItem.Checked = value == GammaConversion.Value2_2;
                }
            }
        }

        private struct MagnifyingInfo
        {
            private enum Mode { Value, Circumscribe, FitHorizontal, FitVertical }
            Mode mode;
            double value;

            public double GetPower(Size viewSize, Size imageSize)
            {
                switch(mode)
                {
                    case Mode.Circumscribe: return Math.Max((double)viewSize.Width / imageSize.Width, (double)viewSize.Height / imageSize.Height);
                    case Mode.FitHorizontal: return (double)viewSize.Width / imageSize.Width;
                    case Mode.FitVertical: return (double)viewSize.Height / imageSize.Height;

                    default: return value;
                }
            }

            public double? GetValue()
            {
                return mode == Mode.Value ? value as double? : null;
            }

            public static MagnifyingInfo FromValue(double value)
            {
                return new MagnifyingInfo() { mode = Mode.Value, value = value };
            }

            public static readonly MagnifyingInfo Circumscribe = new MagnifyingInfo() { mode = Mode.Circumscribe };
            public static readonly MagnifyingInfo FitHorizontal = new MagnifyingInfo() { mode = Mode.FitHorizontal };
            public static readonly MagnifyingInfo FitVertical = new MagnifyingInfo() { mode = Mode.FitVertical };

            public static bool operator ==(MagnifyingInfo a, MagnifyingInfo b)
            {
                var mode = a.mode;
                return mode == b.mode && (mode != Mode.Value || a.value == b.value);
            }
            public static bool operator !=(MagnifyingInfo a, MagnifyingInfo b)
            {
                var mode = a.mode;
                return mode != b.mode || (mode == Mode.Value && a.value != b.value);
            }

            public override bool Equals(object obj)
            {
                var info = obj as MagnifyingInfo?;
                if (info == null) return false;
                else return this == (MagnifyingInfo)obj;
            }

            public override int GetHashCode()
            {
                return mode == Mode.Value ? value.GetHashCode() : mode.GetHashCode(); 
            }
        }

        private MagnifyingInfo magnifyingPower = MagnifyingInfo.FromValue(2);
        private MagnifyingInfo MagnifyingPower
        {
            get { return magnifyingPower; }
            set
            {
                resetMagnifyingPower(value, checkChange: true, redraw: true);
            }
        }

#if DEBUG
        bool _DrawMeasure = true;
#else
        bool _DrawMeasure = false;
#endif
        bool DrawMeasure
        {
            get { return _DrawMeasure; }
            set
            {
                if (value != _DrawMeasure)
                {
                    _DrawMeasure = value;
                    pbPaintDrawOnlyMeasure();
                }
            }
        }


        private void resetMagnifyingPower(MagnifyingInfo value, bool checkChange = false, bool redraw = true)
        {
            if (!checkChange || value != magnifyingPower)
            {
                magnifyingPower = value;
                Magnifier_currentPageReady = -1;
                if (Magnifier_ZoomedPage != null)
                {
                    Magnifier_ZoomedPage.Dispose();
                    Magnifier_ZoomedPage = null;
                }
                if (redraw)
                {
                    if (ViewerMode == ViewerModeEnum.MagnifierOpening || ViewerMode == ViewerModeEnum.MagnifierClosing) ShwoMagnifierRectangle(invalidiate: false);
                    showCurrentPage();
                }
            }
        }

        ScalingAlgorithmPair StandardScalingAlgorithm;
        ScalingAlgorithmPair MagnifierScalingAlgorithm;

        /*
        enum SlideshowStepEnum { MoveNext, MovePrev, MoveForward1Page, MoveBack1Page }
        SlideshowStepEnum SlideshowStep = SlideshowStepEnum.MoveNext;
        enum SlideshowTerminationEnum { Stop, Repeat, Move }
        SlideshowTerminationEnum SlideshowTermination = SlideshowTerminationEnum.Repeat;
        */
        private bool SlideshowForward = true;
        private bool SlideshowMove1Page = false;
        private bool SlideshowRepeat = true;
        private bool SlideshowGlobal = false;

        private readonly ViewerFormImageFilter imageFilter = new ViewerFormImageFilter();

        private int currentPage
        {
            get { return currentPage_field; }
            set
            {
                if (value != currentPage_field)
                {
                    currentPage_field = value;
                }
                onePageModeForNext = false;
                CurrentDividedPosition = 0;
            }
        }

        private SeekBarMode SeekBarMode
        {
            get
            {
                return orientSeekBarDirectionToPageSequenceToolStripMenuItem.Checked ? SeekBarMode.SameAsPage : SeekBarMode.LeftToRight;
            }
            set
            {
                var changed = value != SeekBarMode;
                if (changed)
                {
                    orientSeekBarDirectionToPageSequenceToolStripMenuItem.Checked = value == SeekBarMode.SameAsPage;
                    setSeekBarDirection();
                }
            }
        }


        private BindingMode bindingModeField = BindingMode.RightToLeft;
        private BindingMode BindingMode
        {
            get
            {
                return bindingModeField;
            }
            set
            {
                ChangeBindingModeCore(value, redraw: true);
            }
        }

        private void ChangeBindingModeCore(BindingMode bindingMode, bool redraw)
        {
            var changed = bindingMode != bindingModeField;
            leftToRightToolStripMenuItem.Checked = bindingMode == BindingMode.LeftToRight;
            rightToLeftToolStripMenuItem.Checked = bindingMode == BindingMode.RightToLeft;
            singlePageToolStripMenuItem.Checked = bindingMode == BindingMode.SinglePage;
            singlePageWithoutScalingUpToolStripMenuItem.Checked = bindingMode == BindingMode.SinglePageWithoutScalingUp;
            if (changed)
            {
                bindingModeField = bindingMode;
                //MaxDivision = bindingMode == BindingMode.LeftToRight || bindingMode == BindingMode.RightToLeft ? 2 : 1;
                if (bindingModeField == BindingMode.SinglePage || bindingModeField == BindingMode.SinglePageWithoutScalingUp)
                {
                    MaxPageCountInWindow = 1;
                }
                else
                {
                    MaxPageCountInWindow = 2;
                }
                //SetForwerdBackMessage(value);
                SetCurrentImageLongPath();
                Magnifier_ZoomedPage?.Dispose();
                Magnifier_ZoomedPage = null;
                Magnifier_currentPageReady = -1;
                if (redraw) showCurrentPage();
                setSeekBarDirection();

            }
        }

        /*
        enum ScalingPattern { Normal, Divide, NoScalingUp }

        private ScalingPattern GetScalingPattern(BindingMode bindingMode)
        {
            switch (bindingMode)
            {
                case BindingMode.LeftToRight:
                case BindingMode.RightToLeft:
                    return pbView.Height > pbView.Width ? ScalingPattern.Divide : ScalingPattern.Normal;
                case BindingMode.SinglePageWithoutScalingUp:
                    return ScalingPattern.NoScalingUp;
                default:
                    return ScalingPattern.Normal;
            }
        }
        */

        private static bool NoScaling(BindingMode bindingMode)
        {
            return bindingMode != BindingMode.SinglePageWithoutScalingUp;
        }

        public static CommandLineOptionInfo CommandLineOptionAnalyzer(string[] args, int start)
        {
            var result = new CommandLineOptionInfo();

            var position = start;
            var lastRead = start - 1;
            for (; position < args.Length; position++)
            {
                var name = args[position];
                if (string.IsNullOrEmpty(name)) continue;
                string value;
                var eq = name.IndexOf(commandLineEqual);
                if (eq <= 0)
                {
                    break;
                }
                var position0 = position;
                if (eq > 0)
                {
                    if (eq < name.Length - commandLineEqual.Length)
                    {
                        value = name.Substring(eq + commandLineEqual.Length);
                        name = name.Substring(0, eq);
                    }
                    else if (++position < args.Length)
                    {
                        name = name.Substring(0, eq);
                        value = args[position];
                    }
                    else
                    {
                        position = position0;
                        break;
                    }
                }
                else if (++position < args.Length)
                {
                    value = args[position];
                    if (string.IsNullOrEmpty(value)) continue;
                    if (value == commandLineEqual)
                    {
                        if (++position < args.Length)
                        {
                            value = args[position];
                        }
                        else
                        {
                            position = position0;
                            break;
                        }
                    }
                    else if (value.Length > commandLineEqual.Length && value.StartsWith(commandLineEqual))
                    {
                        value = value.Substring(commandLineEqual.Length);
                    }
                    else
                    {
                        position = position0;
                        break;
                    }
                }
                else
                {
                    position = position0;
                    break;
                }
                if (string.IsNullOrEmpty(value) || value.Contains(commandLineEqual))
                {
                    position = position0;
                    break;
                }

                bool read;
                if (string.Compare(name, "c", ignoreCase: true) == 0)
                {
                    CoverBindingMode? r;
                    if (read = TryParse(value, ignoreCase: true, result: out r)) result.CoverBindingMode = r;
                }
                else if (string.Compare(name, "i", ignoreCase: true) == 0)
                {
                    bool? r;
                    if (read = TryParse(value, ignoreCase: true, result: out r) && r != null) result.OpenInPreviousImageFilterSetting = (bool)r;
                }
                else if (string.Compare(name, "f", ignoreCase: true) == 0)
                {
                    InitialFullscreenMode r;
                    if (read = TryParse(value, ignoreCase: true, result: out r)) result.InitialFullscreenMode = r;
                }
                else if (string.Compare(name, "b", ignoreCase: true) == 0)
                {
                    Color r;
                    if (read = ColorTryParse(value, result: out r)) result.BackColor = r;
                }
                else if (string.Compare(name, "a", ignoreCase: true) == 0)
                {
                    ArchivesInArchiveMode? r;
                    if (read = TryParse(value, ignoreCase: true, result: out r)) result.ArchivesInArchiveMode = r;
                }
                else if (string.Compare(name, "h", ignoreCase: true) == 0)
                {
                    bool? r;
                    if (read = TryParse(value, ignoreCase: true, result: out r)) result.AlwaysHideUI = r;
                }
                else if (string.Compare(name, "d", ignoreCase: true) == 0)
                {
                    BindingMode? r;
                    if (read = TryParse(value, ignoreCase: true, result: out r)) result.DefaultBindingMode = r;
                }
                else if (string.Compare(name, "w", ignoreCase: true) == 0)
                {
                    if (read = TryParse(value, ignoreCase: true, result: out ReadOnMemoryMode? r)) result.ReadOnMemoryMode = r;
                }
                else read = false;
                if (read) lastRead = position;
            }

            // Path の解釈
            var p = lastRead + 1;
            if (p < args.Length)
            {
                result.Path = args[p];
            }

            return result;
        }

        private static bool TryParse(string value, bool ignoreCase, out CoverBindingMode? result)
        {
            foreach (CoverBindingMode r in Enum.GetValues(typeof(CoverBindingMode)))
            {
                if (string.Compare(ToString(r), value, ignoreCase) == 0)
                {
                    result = r;
                    return true;
                }
            }
            if (string.Compare(ToString(null as CoverBindingMode?), value, ignoreCase) == 0)
            {
                result = null;
                return true;
            }
            result = null;
            return false;
        }
        private static string ToString(CoverBindingMode? value)
        {
            switch (value)
            {
                case CoverBindingMode.Default: return "d";
                case CoverBindingMode.ForceSingle: return "f";
                case null: return "n";
                default: return "";
            }
        }
        private static bool TryParse(string value, bool ignoreCase, out InitialFullscreenMode result)
        {
            foreach (InitialFullscreenMode r in Enum.GetValues(typeof(InitialFullscreenMode)))
            {
                if (string.Compare(ToString(r), value, ignoreCase) == 0)
                {
                    result = r;
                    return true;
                }
            }
            result = default(InitialFullscreenMode);
            return false;
        }
        private static string ToString(InitialFullscreenMode value)
        {
            switch (value)
            {
                case InitialFullscreenMode.Default: return "d";
                case InitialFullscreenMode.ForceFullscreen: return "f";
                case InitialFullscreenMode.ForceWindow: return "w";
                default: return "";
            }
        }
        private static bool TryParse(string value, bool ignoreCase, out BindingMode? result)
        {
            foreach (BindingMode r in Enum.GetValues(typeof(BindingMode)))
            {
                if (string.Compare(ToString(r), value, ignoreCase) == 0)
                {
                    result = r;
                    return true;
                }
            }
            if (string.Compare(ToString(null as BindingMode?), value, ignoreCase) == 0)
            {
                result = null;
                return true;
            }
            result = null;
            return false;
        }
        private static string ToString(BindingMode? value)
        {
            switch (value)
            {
                case BindingMode.LeftToRight: return "l";
                case BindingMode.RightToLeft: return "r";
                case BindingMode.SinglePage: return "s";
                case BindingMode.SinglePageWithoutScalingUp: return "d";
                case null: return "n";
                default: return "";
            }
        }
        private static bool TryParse(string value, bool ignoreCase, out ArchivesInArchiveMode? result)
        {
            foreach (ArchivesInArchiveMode r in Enum.GetValues(typeof(ArchivesInArchiveMode)))
            {
                if (string.Compare(ToString(r), value, ignoreCase) == 0)
                {
                    result = r;
                    return true;
                }
            }
            if (string.Compare(ToString(null as ArchivesInArchiveMode?), value, ignoreCase) == 0)
            {
                result = null;
                return true;
            }
            result = null;
            return false;
        }
        private static string ToString(ArchivesInArchiveMode? value)
        {
            switch (value)
            {
                case ArchivesInArchiveMode.Always1Level: return "c1";
                case ArchivesInArchiveMode.Ignore: return "c0";
                case ArchivesInArchiveMode.UntilFound2Level: return "f2";
                case ArchivesInArchiveMode.IfNoOther1Level: return "l1";
                case null: return "n";
                default: return "";
            }
        }
        private static bool TryParse(string value, bool ignoreCase, out bool? result)
        {
            foreach (var r in new bool?[3] { null, false, true })
            {
                if (string.Compare(ToString(r), value, ignoreCase) == 0)
                {
                    result = r;
                    return true;
                }
            }
            result = null;
            return false;
        }
        private static string ToString(bool? value)
        {
            switch (value)
            {
                case true: return "t";
                case false: return "f";
                default: return "n";
            }
        }
        private static bool ColorTryParse(string value, out Color result)
        {
            if (!string.IsNullOrEmpty(value))
            {
                uint argb;
                bool success;
                switch (value.First())
                {
                    case '#': success = uint.TryParse(value.Substring(1), System.Globalization.NumberStyles.HexNumber, null, out argb); break;
                    default: argb = 0; success = false;break;
                }
                if (success)
                {
                    result = Color.FromArgb((int)(argb | (uint)0xff000000));
                    return true;
                }
                else
                {
                    return Enum.TryParse(value, ignoreCase: true, result: out result);
                }
            }

            result = Color.Empty;
            return false;
        }
        private static string RGBToString(Color? color)
        {
            if (color == null) return "#000000";
            var c = (Color)color;
            return "#" + (c.R << 16 | c.G << 8 | c.B).ToString("x6"); // ToString は 0x を付けない
        }
        private static bool TryParse(string value, bool ignoreCase, out ReadOnMemoryMode? result)
        {
            foreach (ReadOnMemoryMode r in Enum.GetValues(typeof(ReadOnMemoryMode)))
            {
                if (string.Compare(ToString(r), value, ignoreCase) == 0)
                {
                    result = r;
                    return true;
                }
            }
            if (string.Compare(ToString(null as ReadOnMemoryMode?), value, ignoreCase) == 0)
            {
                result = null;
                return true;
            }
            result = null;
            return false;
        }
        private static string ToString(ReadOnMemoryMode? value)
        {
            switch (value)
            {
                case ReadOnMemoryMode.None: return "d";
                case ReadOnMemoryMode.ExceptLookAhead: return "e";
                case ReadOnMemoryMode.Always: return "a";
                case null: return "n";
                default: return "";
            }
        }

        private const string commandLineEqual = ":"; // パスとの衝突に注意

        /// <summary>
        /// 空でない場合先頭に空白を付加して返す。空なら null を返す
        /// </summary>
        /// 
        private static string GetCommandLineOption(CommandLineOptionInfo info, bool minimize)
        {
            var result = "-v";
            var def = minimize ? new CommandLineOptionInfo() : null;
            if (!minimize || info.CoverBindingMode != def.CoverBindingMode) addText(ref result, $"c{commandLineEqual}{ToString(info.CoverBindingMode)}");
            if (!minimize || info.OpenInPreviousImageFilterSetting != def.OpenInPreviousImageFilterSetting) addText(ref result, $"i{commandLineEqual}{ToString(info.OpenInPreviousImageFilterSetting)}");
            if (!minimize || info.InitialFullscreenMode != def.InitialFullscreenMode) addText(ref result, $"f{commandLineEqual}{ToString(info.InitialFullscreenMode)}");
            if (!minimize || info.BackColor != def.BackColor) addText(ref result, $"b{commandLineEqual}{RGBToString(info.BackColor)}");
            if (!minimize || info.ReadOnMemoryMode != def.ReadOnMemoryMode) addText(ref result, $"w{commandLineEqual}{ToString(info.ReadOnMemoryMode)}");
            if (!minimize || info.ArchivesInArchiveMode != def.ArchivesInArchiveMode) addText(ref result, $"a{commandLineEqual}{ToString(info.ArchivesInArchiveMode)}");
            if (!minimize || info.AlwaysHideUI != def.AlwaysHideUI) addText(ref result, $"h{commandLineEqual}{ToString(info.AlwaysHideUI)}");
            if (!minimize || info.DefaultBindingMode != def.DefaultBindingMode) addText(ref result, $"d{commandLineEqual}{ToString(info.DefaultBindingMode)}");

            return result;
        }

        // あまり長いコマンドラインはショートカットに登録できない
        /*
    private static string GetCommandLineOption(CommandLineOptionInfo info, bool minimize)
    {
        var result = "-v";
        var def = minimize ? new CommandLineOptionInfo() : null;
        if (!minimize || info.CoverBindingMode != def.CoverBindingMode) addText(ref result, $"{nameof(info.CoverBindingMode)}{commandLineEqual}{ToStringNullable(info.CoverBindingMode)}");
        if (!minimize || info.OpenInPreviousImageFilterSetting != def.OpenInPreviousImageFilterSetting) addText(ref result, $"{nameof(info.OpenInPreviousImageFilterSetting)}{commandLineEqual}{info.OpenInPreviousImageFilterSetting}");
        if (!minimize || info.InitialFullscreenMode != def.InitialFullscreenMode) addText(ref result, $"{nameof(info.InitialFullscreenMode)}{commandLineEqual}{info.InitialFullscreenMode}");
        if (!minimize || info.BackColor != def.BackColor) addText(ref result, $"{nameof(info.BackColor)}{commandLineEqual}{ToStringNullable(RGBToString(info.BackColor))}");
        if (!minimize || info.ArchivesInArchiveMode != def.ArchivesInArchiveMode) addText(ref result, $"{nameof(info.ArchivesInArchiveMode)}{commandLineEqual}{ToStringNullable(info.ArchivesInArchiveMode)}");
        if (!minimize || info.AlwaysHideUI != def.AlwaysHideUI) addText(ref result, $"{nameof(info.AlwaysHideUI)}{commandLineEqual}{ToStringNullable(info.AlwaysHideUI)}");
        if (!minimize || info.DefaultBindingMode != def.DefaultBindingMode) addText(ref result, $"{nameof(info.DefaultBindingMode)}{commandLineEqual}{ToStringNullable(info.DefaultBindingMode)}");

        return result;
    }

    private static string ToStringNullable(object target)
    {
        if (target == null) return "Null";
        else return target.ToString();
    }
    */

        private static void addText(ref string text, string text2)
        {
            //if (string.IsNullOrEmpty(text)) text = text2;
            //else
            text += " " + text2;
        }

        /// <summary>
        /// 例外を投げない
        /// </summary>
        /// <param name="path"></param>
        public static bool CreateShortcut(Form owner, CommandLineOptionInfo info, string defaultPath)
        {
            try
            {
                defaultPath = Program.ItsSelfOrExistingParentDirectory(defaultPath);
                using (var dialog = new SaveFileDialog())
                {
                    dialog.InitialDirectory = defaultPath;
                    dialog.FileName = $"ZipPla ({Message.BuiltInViewer}).lnk";
                    dialog.Filter = "*.lnk|*.lnk";
                    dialog.Title = Message.CreateShortcutFromCurrentSettings;
                    if (dialog.ShowDialog(owner) == DialogResult.OK)
                    {
                        CreateShortcut(dialog.FileName, info);
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(owner, e.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
        }

        /// <summary>
        /// 例外を投げる
        /// </summary>
        /// <param name="path"></param>
        public static void CreateShortcut(string path, CommandLineOptionInfo info)
        {
            var t = Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8"));
            dynamic shell = Activator.CreateInstance(t);
            try
            {
                //WshShortcutを作成
                var shortcut = shell.CreateShortcut(path);
                try
                {
                    shortcut.TargetPath = Application.ExecutablePath;
#if AUTOBUILD
                    shortcut.IconLocation = Application.ExecutablePath + ",1"; // ビルド後にソース同梱の dark.ico を埋め込む
#else
                    shortcut.IconLocation = Application.ExecutablePath + ",0";
#endif
                    shortcut.Arguments = GetCommandLineOption(info, minimize: false);
                    shortcut.Save();

                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
            }
        }

        public class CommandLineOptionInfo
        {
            public CoverBindingMode? CoverBindingMode = null;
            public bool OpenInPreviousImageFilterSetting = true;
            public InitialFullscreenMode InitialFullscreenMode = InitialFullscreenMode.Default;
            public Color? BackColor = null;
            public ArchivesInArchiveMode? ArchivesInArchiveMode = null;
            public bool? AlwaysHideUI = null;
            public BindingMode? DefaultBindingMode = null;
            public ReadOnMemoryMode? ReadOnMemoryMode = null;

            // これは Main に渡すためのもの。ViewerForm 内では解釈しない
            public string Path;
        }

        public ViewerForm(CommandLineOptionInfo info)
        {
            Constructor(info);

            Shown += (sender, e) =>
            {
                Program.StartCheckUpdateAndNgen(this, checkNgen);
                PackedImageLoader.CheckSevenZipExistence(this);
            };

            saveToHistory = true;

            OpenFile_UserOpen = true;
        }

        // 従来の直接起動コンストラクタ
        /*
        public ViewerForm(string path, int page, CommandLineOptionInfo info)
        {
            Constructor(info);

            Shown += (sender, e) =>
            {
                Program.StartCheckUpdateAndNgen(this, checkNgen);
                PackedImageLoader.CheckSevenZipExistence(this);
            };

            saveToHistory = true;
            ViewerForm_Shown_path = path;
            ViewerForm_Shown_page = page;
            Shown += ViewerForm_Shown_ForOpen;

            OpenFile_UserOpen = true;
        }
        
        string ViewerForm_Shown_path;
        int ViewerForm_Shown_page;
        private void ViewerForm_Shown_ForOpen(object sender, EventArgs e)
        {
            // 必ず呼び出されるわけではないので注意

            OpenFile(ViewerForm_Shown_path, ViewerForm_Shown_page);
            Shown -= ViewerForm_Shown_ForOpen;
        }
        */

        // 仮想的に先読みプロセスの動作をするコンストラクタ
        // フルスクリーン起動時のチラツキ軽減が目的

        public ViewerForm(string path, int page, CommandLineOptionInfo info)
        {
            //Program.SetPriority(ProcessPriorityClass.BelowNormal);
            this.ipcLookAheadInfo = new IpcLookAheadInfo(); // null でないことが重要
                                                            //this.ipcReportFromViewerToCatalog = ipcReportFromViewerToCatalog;

            if (info != null)
            {
                ipcLookAheadInfo.AlwaysHideUI = info.AlwaysHideUI ?? ipcLookAheadInfo.AlwaysHideUI;
                ipcLookAheadInfo.ArchivesInArchive = info.ArchivesInArchiveMode ?? ipcLookAheadInfo.ArchivesInArchive;
                ipcLookAheadInfo.BackColor = info.BackColor ?? ipcLookAheadInfo.BackColor;
                ipcLookAheadInfo.CoverBinding = info.CoverBindingMode ?? ipcLookAheadInfo.CoverBinding;
                ipcLookAheadInfo.DefaultBinding = info.DefaultBindingMode ?? ipcLookAheadInfo.DefaultBinding;
                ipcLookAheadInfo.InitialFullscreenMode = info.InitialFullscreenMode;
                ipcLookAheadInfo.ReadOnMemoryMode = info.ReadOnMemoryMode ?? ipcLookAheadInfo.ReadOnMemoryMode;
                //CommandLineOptionInfoForLookAhead = info;
            }
            ipcLookAheadInfo.Path = path;
            ipcLookAheadInfo.Page = page;
            ipcLookAheadInfo.Message = IpcLookAheadInfo.MessageEnum.Show;
            ipcLookAheadInfo.Accept = false;
            

            Constructor(info);
            //Constructor(initialFullscreenMode, alwaysHideUI, backColor: Color.Black, aia: ArchivesInArchiveMode.Ignore);

            if (LoadSettingToShown_fullscreen || LoadSettingToShown_maximized)
            {
                //Visible = false;
                ViewerForm_Shown_VisibleToFalse = true;
                WindowState = FormWindowState.Minimized;
            }

            //tmWatchIpc_Tick(null, null);

            //Load += (sender, e) => tmWatchIpc_Tick(sender, null);
            //ViewerForm_Load_CallTick = true;

            //tmWatchIpc.Start();
            //var visibleTrue = !LoadSettingToShown_fullscreen;
            Shown += (sender, e) =>
            {
                //WindowState = FormWindowState.Minimized;
                //Visible = false;
                tmWatchIpc_Tick(sender, null); // Load で呼び出すと読み込みが早かった場合にエラー。しかも表示を２回行うので時間も無駄になる
                Program.StartCheckUpdateAndNgen(this, checkNgen);
                PackedImageLoader.CheckSevenZipExistence(this);
            };

            //tmMemoryReducer.Enabled = true; // Start() と同じ

            saveToHistory = true;
        }

        IpcLookAheadInfo ipcLookAheadInfo = null;
        IpcReportFromViewerToCatalog ipcReportFromViewerToCatalog = null;
        public ViewerForm(IpcLookAheadInfo ipcLookAheadInfo, IpcReportFromViewerToCatalog ipcReportFromViewerToCatalog, InitialFullscreenMode initialFullscreenMode, bool? alwaysHideUI)
        {
            //Program.SetPriority(ProcessPriorityClass.BelowNormal);
            this.ipcLookAheadInfo = ipcLookAheadInfo;
            this.ipcReportFromViewerToCatalog = ipcReportFromViewerToCatalog;

            var info = new CommandLineOptionInfo();
            info.InitialFullscreenMode = initialFullscreenMode;
            info.AlwaysHideUI = alwaysHideUI;
            info.BackColor = Color.Black;
            info.ArchivesInArchiveMode = ArchivesInArchiveMode.Ignore;
            info.ReadOnMemoryMode = ReadOnMemoryMode.None;
            CommandLineOptionInfoForLookAhead = info;
            Constructor(info);
            //Constructor(initialFullscreenMode, alwaysHideUI, backColor: Color.Black, aia: ArchivesInArchiveMode.Ignore);

            tmWatchIpc.Start();

            //tmMemoryReducer.Enabled = true; // Start() と同じ
        }
        private CommandLineOptionInfo CommandLineOptionInfoForLookAhead = null;

        //private ContextMenuStripForMenu startContextMenuStripForMenu;

        MouseGesture mgView;
        KeyboardShortcut ksViewForm;

        Font MenuTextFont = null;

        private GestureListener gestureListener, gestureListenerEditMenu;

        private void Constructor(/*InitialFullscreenMode initialFullscreenMode = InitialFullscreenMode.Default,*/ CommandLineOptionInfo info)//, bool? alwaysHideUI = null, Color? backColor = null, ArchivesInArchiveMode? aia = null)
        {
            Configuration.PrepareXmlSerializer(
                typeof(ViewerFormConfig),
                //typeof(ColoredBookmarkConfig),
                typeof(GeneralConfig)
                //typeof(ZipTagConfig)
                );

            if (ipcLookAheadInfo == null && Program.CheckConfigException()) CloseWithoutSave();

            InitializeComponent();

            Configuration.WaitPrepareTask();

            closeWindowtoolStripMenuItem.Text = " "; // 特別な状況下で表示が乱れるのを防ぐ

            /*
#if AUTOBUILD
            antiMoire1ToolStripMenuItem.Visible = false;
            antiMoire2ToolStripMenuItem.Visible = false;
            antiMoire3ToolStripMenuItem.Visible = false;
            antiMoire4ToolStripMenuItem.Visible = false;
#endif
            */

            try
            {
                gestureListener = new GestureListener(this, new GestureConfig[] {
                    new GestureConfig(3, 1, 0), // ズーム
                    //new GestureConfig(4, 2 | 4 , 8 | 16), // パン、向き拘束と慣性なし
                    new GestureConfig(4, 2 | 4 | 16 , 8 ), // パン、向き拘束なし
                    
                    new GestureConfig(5, 1, 0), // 回転
                    new GestureConfig(6, 1, 0), // ツーフィンガータップ
                    //new GestureConfig(7, 1, 0), // プレスアンドタップ

                });
                gestureListener.Zoom += gestureListener_Zoom;
                gestureListener.Pan += TouchListener_Pan;
                gestureListener.Rotate += gestureListener_Rotate;
                gestureListener.TwoFingerTap += gestureListener_TwoFingerTap;
                //gestureListener.PressAndTap += gestureListener_PressAndTap;

                gestureListenerEditMenu = MiniControlTouchGesture.GetGestureListener(editToolStripMenuItem.DropDown);
                var menu = new MiniControlTouchGesture(editToolStripMenuItem.DropDown, gestureListenerEditMenu, onlyHorizontalStart: true);
                menu.Targets.Add(editToolStripMenuItem.DropDown);
                menu.TouchGestureCompleted += CatalogForm.TouchGestureCompletedForTagMenu;
                menu.TouchGestureStarting += CatalogForm.TouchGestureStartingForTagMenu;
            }
            catch { }

            // キー入力の暴発を防ぐ
            ActivateManager.SetSelectable(btnOpenLeft, false);
            ActivateManager.SetSelectable(btnOpenRight, false);

            new KeyMouseBinder(editToolStripMenuItem.DropDown, requiredBindingEditMenu);

            if (CatalogForm.MenuTextFontShouldBeReplaced())
            {
                MenuTextFont = new Font(menuStrip.Font.FontFamily, DefaultFont.SizeInPoints);
            }

            if (MenuTextFont != null)
            {
                menuStrip.Font = MenuTextFont;
                statusStrip.Font = MenuTextFont;
            }

            ZipPlaAddressBar.AddDrawToolStripMenuItemTriangleEventHandler(menuStrip);

            //Program.SetDoubleBuffered(pbView);

            btnOpenLeft.FlatStyle = FlatStyle.Flat;
            btnOpenLeft.FlatAppearance.BorderColor = BackColor;
            btnOpenRight.FlatStyle = FlatStyle.Flat;
            btnOpenRight.FlatAppearance.BorderColor = BackColor;

            openNewThumbnailWindowToolStripMenuItem.Image = MoveLocationProvider.Icon;
            switchToThumbnailWindowToolStripMenuItem.Image = MoveLocationProvider.Icon;
            cloneTheWindowToolStripMenuItem.Image = BuiltInViewerProvider.Icon;

            // 拡大鏡使用中のキー入力がカスタマイズされたショートカットキーに先行するように
            PreviewKeyDown += Control_PreviewKeyDown;
            foreach (Control control in Controls) control.PreviewKeyDown += Control_PreviewKeyDown;
            //mtbPage.PreviewKeyDown += Control_PreviewKeyDown; // pbView 外でカーソルが動いても意味がない

            ksViewForm = new KeyboardShortcut(this, mtbPage);
            ksViewForm.UseLButton = true;
            ksViewForm.UseMButton = true;
            ksViewForm.UseRButton = true;
            ksViewForm.UseX1Button = true;
            ksViewForm.UseX2Button = true;
            //ksViewForm.MouseAcceptControls = ksViewForm.LButtonAcceptControls = ksViewForm.WheelAcceptControls = new Control[] { pbView };
            //ksViewForm.RButtonAcceptControls = new Control[] { }; // mgView.Enabled の初期値に合わせて設定
            ksViewForm.MouseAcceptControls = ksViewForm.LButtonAcceptControls = ksViewForm.WheelAcceptControls = ksViewForm.RButtonAcceptControls = new Control[] { pbView };
            ksViewForm.WheelAccepted += ksViewForm_WheelAccepted;
            ksViewForm.KeyboardShortcutStarting += ksViewForm_KeyboardShortcutStarting;

            mgView = new MouseGesture(pbView);
            //mgView.EnabledChanged += mgView_EnabledChanged;
            mgView.MouseGestureCompleted += mgView_MouseGestureCompleted;
            mgView.MouseGestureStarting += mgView_MouseGestureStarting;

            ToolStripOverwriter.SquarizeToolStripInClass(this);

            Shown += (sender, e) =>
            {
                CheckMarkProvider.SetCheckMark(loadWholeArchiveIntoMemoryToolStripMenuItem.DropDown, DisplayCheckMark.Select);
                CheckMarkProvider.SetCheckMark(loadArchivesInArchiveToolStripMenuItem.DropDown, DisplayCheckMark.Select);
                CheckMarkProvider.SetCheckMark(startToolStripMenuItem.DropDown, fullScreenInStartToolStripMenuItem, DisplayCheckMark.Select);
                CheckMarkProvider.SetCheckMark(startToolStripMenuItem.DropDown, windowToolStripMenuItem, DisplayCheckMark.Select);
                
                CheckMarkProvider.SetCheckMark(viewToolStripMenuItem.DropDown, magnifierToolStripMenuItem, DisplayCheckMark.Check);
                CheckMarkProvider.SetCheckMark(viewToolStripMenuItem.DropDown, leftToRightToolStripMenuItem, DisplayCheckMark.Select);
                CheckMarkProvider.SetCheckMark(viewToolStripMenuItem.DropDown, rightToLeftToolStripMenuItem, DisplayCheckMark.Select);
                CheckMarkProvider.SetCheckMark(viewToolStripMenuItem.DropDown, singlePageToolStripMenuItem, DisplayCheckMark.Select);
                CheckMarkProvider.SetCheckMark(viewToolStripMenuItem.DropDown, singlePageWithoutScalingUpToolStripMenuItem, DisplayCheckMark.Select);
                CheckMarkProvider.SetCheckMark(viewToolStripMenuItem.DropDown, forceFirstPageToBeSingleToolStripMenuItem, DisplayCheckMark.Check);
                CheckMarkProvider.SetCheckMark(viewToolStripMenuItem.DropDown, forceTwoPageSpreadToolStripMenuItem, DisplayCheckMark.Check);
                CheckMarkProvider.SetCheckMark(viewToolStripMenuItem.DropDown, allowPageDivisionToolStripMenuItem, DisplayCheckMark.Check);
                
                CheckMarkProvider.SetCheckMark(viewToolStripMenuItem.DropDown, orientSeekBarDirectionToPageSequenceToolStripMenuItem, DisplayCheckMark.Check);
                CheckMarkProvider.SetCheckMark(viewToolStripMenuItem.DropDown, rotateLeftToolStripMenuItem, DisplayCheckMark.Check);
                CheckMarkProvider.SetCheckMark(viewToolStripMenuItem.DropDown, rotateRightToolStripMenuItem, DisplayCheckMark.Check);
                //CheckMarkProvider.SetCheckMark(viewToolStripMenuItem.DropDown, removeMoireToolStripMenuItem, DisplayCheckMark.Check);
                CheckMarkProvider.SetCheckMark(viewToolStripMenuItem.DropDown, pseudoColoringToolStripMenuItem, DisplayCheckMark.Check);
                CheckMarkProvider.SetCheckMark(viewToolStripMenuItem.DropDown, alwaysAutoHideUIToolStripMenuItem, DisplayCheckMark.Check);
                CheckMarkProvider.SetCheckMark(viewToolStripMenuItem.DropDown, showAHintWhenChangingSettingsToolStripMenuItem, DisplayCheckMark.Check);

                CheckMarkProvider.SetCheckMark(scalingAlgorithmNormalToolStripMenuItem.DropDown, DisplayCheckMark.Select);
                CheckMarkProvider.SetCheckMark(scalingAlgorithmNormalToolStripMenuItem.DropDown, linearizeColorSpaceforGamma22NormalToolStripMenuItem, DisplayCheckMark.Check);
                CheckMarkProvider.SetCheckMark(scalingAlgorithmNormalToolStripMenuItem.DropDown, useAreaAverageWhenUpsizingNormalToolStripMenuItem, DisplayCheckMark.Check);
                CheckMarkProvider.SetCheckMark(scalingAlgorithmMagnifierToolStripMenuItem.DropDown, DisplayCheckMark.Select);
                CheckMarkProvider.SetCheckMark(scalingAlgorithmMagnifierToolStripMenuItem.DropDown, linearizeColorSpaceforGamma22MagnifierToolStripMenuItem, DisplayCheckMark.Check);
                CheckMarkProvider.SetCheckMark(scalingAlgorithmMagnifierToolStripMenuItem.DropDown, useAreaAverageWhenUpsizingToolStripMenuItem, DisplayCheckMark.Check);

                CheckMarkProvider.SetCheckMark(ratioToolStripMenuItem.DropDown, DisplayCheckMark.Select);
                CheckMarkProvider.SetCheckMark(slideshowToolStripMenuItem.DropDown, DisplayCheckMark.Select);
                CheckMarkProvider.SetCheckMark(slideshowToolStripMenuItem.DropDown, repeatToolStripMenuItem, DisplayCheckMark.Check);
                CheckMarkProvider.SetCheckMark(slideshowToolStripMenuItem.DropDown, openNextOnTerminalToolStripMenuItem, DisplayCheckMark.Check);
                CheckMarkProvider.SetCheckMark(editToolStripMenuItem.DropDown, DisplayCheckMark.Select);
            };

            //ExtendedToolStripMenuItem.Replace(ref startToolStripMenuItem);

            ToolStripDropDownScroller.Enscrollable(virtualFoldersToolStripMenuItem.DropDown, ToolStripDropDownScroller.GetGestureListener(virtualFoldersToolStripMenuItem.DropDown));
            ToolStripDropDownScroller.Enscrollable(editToolStripMenuItem.DropDown, gestureListenerEditMenu);

            // DPI 変更時用の処理
            //mtbPage.Location = new Point(0, statusStrip.Location.Y - mtbPage.Height);
            pnlSeekbar.Location = new Point(0, statusStrip.Location.Y - pnlSeekbar.Height);

            Text = Program.Name;
            //currentImageLongPathToolStripStatusLabel.Text = "";
            pageToolStripStatusLabel.Text = "";
            imageSizeToolStripStatusLabel.Text = "";

            LoadSettingsAndSetMessages(info);

            pbView.MouseWheel += pbView_MouseWheel;
            statusStrip.MouseWheel += aroundSeekBar_MouseWheel;
            mtbPage.MouseWheel += aroundSeekBar_MouseWheel;
            /*
            pnlSeekbar.MouseWheel += aroundSeekBar_MouseWheel;
            mtbPage.MouseWheel += (sender, e) =>
            {
#if AUTOBUILD
                var delta = e.Delta / 120;
                if (delta == -1)
                {
                    movePageToMinimulForward();
                    e.Value = currentPage;
                }
                else if (delta == 1)
                {
                    movePageToMinimulBack();
                    e.Value = currentPage;
                }
                else
                {
                    e.Value = Math.Max(mtbPage.Minimum, Math.Min(mtbPage.Maximum, mtbPage.Value - delta));
                }
                e.Handled = true;
#endif
            };
            */

            //PrepareMemoryCheck();

        }

        private static bool requiredBindingEditMenu(ToolStripItem toolStripItem)
        {
            if (toolStripItem is PrefixEscapedToolStripMenuItem) return true;
            var items = toolStripItem.Owner.Items;
            var nextIndex = items.IndexOf(toolStripItem) + 1;
            return nextIndex < items.Count && items[nextIndex] is PrefixEscapedToolStripMenuItem;
        }

        private void LoadSettingsAndSetMessages(CommandLineOptionInfo info, bool reroadConfig = true, bool reloadLang = false)
        {
            // ショートカットキーの設定に合わせてメニューの文字列を変更するのでこの順で無ければならない
            LoadSettings(/*initialFullscreenMode, */info, reroadConfig, reloadLang);//?.AlwaysHideUI, info?.BackColor, info?.ArchivesInArchiveMode);
            SetMessages();
        }

        private void CloseAllMenu()
        {
            foreach (ToolStripMenuItem tsmi in menuStrip.Items)
            {
                if (tsmi.HasDropDown)
                {
                    var dd = tsmi.DropDown;
                    if (dd.Visible) dd.Close();
                }
            }
            if (openToolStripMenuItemAsContextMenuStrip_Frame != null)
            {
                if (openToolStripMenuItemAsContextMenuStrip_Frame.Visible)
                {
                    openToolStripMenuItemAsContextMenuStrip_Frame.Close();
                }
            }
        }

        double? gestureListener_Rotate_Degrees = null;
        private void gestureListener_Rotate(object sender, RotateEventArgs e)
        {
            CloseAllMenu();
            if (ViewerMode == ViewerModeEnum.Normal || ViewerMode == ViewerModeEnum.Magnifier)
            //if (ViewerMode == ViewerModeEnum.Normal || ViewerMode == ViewerModeEnum.Magnifier || ViewerMode == ViewerModeEnum.MagnifierOpening)
            {
                double deg;
                if (e.Begin)
                {
                    gestureListener_Rotate_Degrees = 0;
                    deg = 0;
                }
                else if (e.End)
                {
                    gestureListener_Rotate_Degrees = null;
                    return;
                }
                else
                {
                    if (gestureListener_Rotate_Degrees == null)
                    {
                        gestureListener_Rotate_Degrees = 0;
                        deg = e.Degrees;
                    }
                    else
                    {
                        deg = (double)gestureListener_Rotate_Degrees - e.Degrees;// フィルタと符号が逆
                        gestureListener_Rotate_Degrees = deg;
                    }
                }

                const double roteDegree = 45;
                var currentDegrees = imageFilter.RotationAngle;
                if (currentDegrees == 270 && deg < 0 || currentDegrees == 90 && deg > 0)
                {
                    gestureListener_Rotate_Degrees = 0;
                }
                else if (deg <= -roteDegree)
                {
                    imageFilter.RotationAngle = currentDegrees == 0 ? 270 : 0;
                    reloadForChangeFilter(forOnlyPostFilter: false);
                    gestureListener_Rotate_Degrees = deg + 90;
                }
                else if (gestureListener_Rotate_Degrees >= roteDegree)
                {
                    imageFilter.RotationAngle = currentDegrees == 0 ? 90 : 0;
                    reloadForChangeFilter(forOnlyPostFilter: false);
                    gestureListener_Rotate_Degrees = deg - 90;
                }
            }
            else
            {
                gestureListener_Rotate_Degrees = null;
            }
        }

        /*
        private void gestureListener_PressAndTap(object sender, PressAndTapEventArgs e)
        {
            optionToolStripMenuItem.Text = $"{e.Location}";
        }
        */
        
        private void gestureListener_TwoFingerTap(object sender, TwoFingerTapEventArgs e)
        {
            CloseAllMenu();
            if (ViewerMode == ViewerModeEnum.Magnifier)
            {
                MagnifierPhase3(toPhase4: true);
                MagnifierPhase4(toPhase1: false);
            }
            else if (ViewerMode == ViewerModeEnum.Normal)
            {
                /*
                if (MagnifyingPower <= 1)
                {
                    resetMagnifyingPower(2, checkChange: true, redraw: false);
                }
                */
                MagnifierPhase1(pbView.PointToClient(e.Location));
                MagnifierPhase2();
            }
            else if (ViewerMode == ViewerModeEnum.MagnifierOpening)
            {
                var value = magnifyingPower.GetValue();
                if (value == null || value > 1)
                {
                    MagnifierPhase2();
                }
                else
                {
                    MagnifierPhase2();
                    MagnifierPhase3(toPhase4: true);
                    MagnifierPhase4(toPhase1: false);
                }
            }
        }
        
        private void gestureListener_Zoom(object sender, ZoomEventArgs e)
        {
            CloseAllMenu();
            TouchListener_Pan_Delta = null;
            if (ViewerMode == ViewerModeEnum.CoverSetting) return;

            if (e.Begin)
            {
                if (gestureListener_Rotate_Degrees == null && (ViewerMode == ViewerModeEnum.Normal || ViewerMode == ViewerModeEnum.Magnifier))
                {
                    if (ViewerMode == ViewerModeEnum.Magnifier)
                    {
                        MagnifierPhase3(toPhase4: true);
                        MagnifierPhase4(toPhase1: false);
                    }
                    else
                    {
                        //resetMagnifyingPower(1, checkChange: true, redraw: false);
                    }

                    // 係数の設定
                    // 0.7 を基本とし、
                    // GetCenterOfMagnifierRectangle_DistanceCoef * longer / Math.Max(1, e.Distance) が 4 を上回るならそうならないように更に小さくする
                    var pbViewSize = pbView.Size;
                    var longer = Math.Max(1, Math.Max(pbViewSize.Width, pbViewSize.Height));
                    GetCenterOfMagnifierRectangle_DistanceCoef = Math.Min(0.7, 4.0 / longer * e.Distance);

                    MagnifyingInfo newMagPower;
                    MagnifierPhase1(GetCenterOfMagnifierRectangle(e, out newMagPower));

                    if (ViewerMode == ViewerModeEnum.MagnifierOpening)
                    {
                        resetMagnifyingPower(newMagPower, checkChange: true, redraw: true);
                    }

                }
            }
            else if (!e.End)
            {
                if (ViewerMode == ViewerModeEnum.MagnifierOpening)
                {
                    MagnifyingInfo newMagPower;
                    GetMagnifierRectangle_LastCursorPosition = GetCenterOfMagnifierRectangle(e, out newMagPower);
                    resetMagnifyingPower(newMagPower, checkChange: true, redraw: true);
                }
            }
            else
            {
                var value = magnifyingPower.GetValue();
                if (value == null || value > 1)
                {
                    MagnifierPhase2();
                }
                else
                {
                    MagnifierPhase2();
                    MagnifierPhase3(toPhase4: true);
                    MagnifierPhase4(toPhase1: false);
                }
            }
        }

        private double GetCenterOfMagnifierRectangle_DistanceCoef;
        private Point GetCenterOfMagnifierRectangle(ZoomEventArgs e, out MagnifyingInfo newMagnifyingPower)
        {
            var center = pbView.PointToClient(e.Location);
            var distance = e.Distance;
            var pbViewSize = getCurrentPbViewSize();
            var dicreasedLonger = GetCenterOfMagnifierRectangle_DistanceCoef * Math.Max(pbViewSize.Width, pbViewSize.Height);

            var imageSize_ = getCurrentVisibleImageSize(pbViewSize);

            var value = dicreasedLonger / Math.Max(1, distance);

            if (imageSize_ == null)
            {
                value = Math.Min(4, Math.Max(1, value));
                newMagnifyingPower = MagnifyingInfo.FromValue(value);

            }
            else
            {
                var imageSize = (Size)imageSize_;
                var fits = new List<double>();
                var mh = MagnifyingInfo.FitHorizontal.GetPower(pbViewSize, imageSize);
                if (mh < 4 && mh > 1) fits.Add(mh);
                var mv = MagnifyingInfo.FitVertical.GetPower(pbViewSize, imageSize);
                if (mv < 4 && mv > 1) fits.Add(mv);
                fits.Sort();
                var margin = 0.1;
                for (var i = fits.Count - 1; i >= 0; i--)
                {
                    var m = fits[i];
                    var mInv = 1 / m;
                    var vInv = 1 / value;
                    if (vInv <= mInv) break;
                    else if (vInv - margin <= mInv)
                    {
                        value = m;
                        break;
                    }
                    else
                    {
                        value = 1 / (vInv - margin);
                    }
                }

                // GetValue が 1 以下になる場合はその値で FromValue し直すことで拡大を行わないことを知らせる
                // 等倍の場合、他のページも等倍の可能性が高いのでフィットの必要性は低く、これを行う必要は認められない。
                if (value <= 1)
                {
                    newMagnifyingPower = MagnifyingInfo.FromValue(1);
                }
                else if (value == mh)
                {
                    newMagnifyingPower = MagnifyingInfo.FitHorizontal;
                }
                else if (value == mv)
                {
                    newMagnifyingPower = MagnifyingInfo.FitVertical;
                }
                else if (value < 4)
                {
                    newMagnifyingPower = MagnifyingInfo.FromValue(value);
                }
                else
                {
                    newMagnifyingPower = MagnifyingInfo.FromValue(4);
                }

            }

            // サイズの設定
            // 現在表示中の画像サイズが取得できなければ常に FromValue で。

            return center;
        }

        private VectorD? TouchListener_Pan_Delta = null;
        private bool TouchListener_Pan_HoldTrackBar = false;
        private void TouchListener_Pan(object sender, PanEventArgs e)
        {
            CloseAllMenu();
            if (gestureListener_Rotate_Degrees != null)
            {
                TouchListener_Pan_Delta = null;
                mgView.Clear();

                TouchListener_Pan_HoldTrackBar = false;

                return;
            }

            Point clientPoint;
            if (e.Begin && !TouchListener_Pan_HoldTrackBar && !mtbPage.Visible)
            {
                TouchListener_Pan_HoldTrackBar = showUnderUI(pbView.PointToClient(e.Location));
                if (TouchListener_Pan_HoldTrackBar) Cursor.Position = e.Location;
            }
            if (!e.End && !e.Inertia && mtbPage.Visible && (
                mtbPage.ClientRectangle.Contains(clientPoint = mtbPage.PointToClient(e.Location)) || TouchListener_Pan_HoldTrackBar))
            {
                mtbPage.Value = mtbPage.PointToValue(clientPoint);
                TouchListener_Pan_HoldTrackBar = true;
                return;
            }
            TouchListener_Pan_HoldTrackBar = false;

            switch (ViewerMode)
            {
                case ViewerModeEnum.Normal:
                    {
                        TouchListener_Pan_Delta = null;
                        var points = e.Info.location;
                        var point = pbView.PointToClient(new Point(points.x, points.y));
                        var contain = pbView.ClientRectangle.Contains(point);
                        
                        if (contain && e.Begin) mgView.GestureBegin(point);
                        else if (contain && !e.End && !e.Inertia) mgView.GestureContinue(point);
                        else mgView.GestureEnd(point, inNativeThread: true);
                    }
                    break;
                case ViewerModeEnum.MagnifierOpening:
                    TouchListener_Pan_Delta = null;
                    mgView.Clear();
                    break;
                case ViewerModeEnum.Magnifier:
                    mgView.Clear();
                    if (TouchListener_Pan_Delta == null)
                    {
                        TouchListener_Pan_Delta = new VectorD(e.PanOffset);
                    }
                    else
                    {
                        TouchListener_Pan_Delta += new VectorD(e.PanOffset);
                    }
                    pbPaintInvalidate();
                    //optionToolStripMenuItem.Text = $"{e.PanOffset.X} {e.PanOffset.Y}";
                    break;
                case ViewerModeEnum.MagnifierClosing:
                    TouchListener_Pan_Delta = null;
                    mgView.Clear();
                    break;
                case ViewerModeEnum.CoverSetting:
                    {
                        TouchListener_Pan_Delta = null;
                        mgView.Clear();
                        var points = e.Info.location;
                        var point = pbView.PointToClient(new Point(points.x, points.y));
                        pbView_Paint_Rectangle = GetCoverRectangleForDisplay(point);
                        pbPaintInvalidate();
                        if (e.End)
                        {
                            // TouchListener_Pan を抜けるまでタッチからクリックへの変換が行われない
                            Task.Run(() =>
                            {
                                try
                                {
                                    Invoke((MethodInvoker)(() =>
                                    {
                                        SetCoverForThumbnail(clientPoint: point);
                                    }));
                                }
                                catch (ObjectDisposedException) { }
                            });
                        }
                    }
                    break;
            }


            e.Handled = true;
        }
        
        private void ksViewForm_WheelAccepted(object sender, MouseAcceptedCancelEventArgs e)
        {
            // テキストボックス上でのホイール操作が statusStrip に伝わらないため
            e.Cancel = statusStrip.Visible && statusStrip.ClientRectangle.Contains(statusStrip.PointToClient(e.ScreenPoint));
        }

        private void ksViewForm_KeyboardShortcutStarting(KeyboardShortcut sender, KeyboardShortcutStartingEventArgs e)
        {
            if (e.ByProgram) return;
            if (e.StartingKey == Keys.RButton)
            {
                if (mgView != null && mgView.Enabled)
                {
                    e.Cancel = true;
                }
            }
            else if (ksViewForm != null && ksViewForm.Enabled && mgView.InGesturing && !mgView.InGesturing2)
            {
                mgView.Clear();
                e.InitialState = new Keys[1] { Keys.RButton };
            }
        }
        
        private void mgView_MouseGestureCompleted(MouseGesture sender, MouseGestureCompletedEventArgs e)
        {
            if (e.UserDirections.Length == 0 && !MouseDownForCoverSettingExit)
            {
                ksViewForm.InvokeMouseClick(e.Button);
            }
            MouseDownForCoverSettingExit = false;
        }

        private void mgView_MouseGestureStarting(MouseGesture sender, MouseGestureStartingEventArgs e)
        {
            if (ksViewForm != null && ksViewForm.InInput)
            {
                e.Cancel = true;
                //MessageBox.Show($"{ksViewForm.KeySequence.Count} {string.Join(", ", ksViewForm.KeySequence[0])}");
            }
        }
        
        /*
        private void mgView_EnabledChanged(object sender, EventArgs e)
        {
            return;
            if (mgView.Enabled)
            {
                ksViewForm.RButtonAcceptControls = new Control[0];
            }
            else
            {
                ksViewForm.RButtonAcceptControls = new Control[] { pbView };
            }
        }
        */

        private void aroundSeekBar_MouseWheel(object sender, MouseEventArgs e)
        {
            // pbView_MouseWheel 経由で呼ばれた場合座標が異なるので注意

            var delta = e.Delta / 120;
            if (delta == -1)
            {
                movePageToMinimulForward();
            }
            else if (delta == 1)
            {
                movePageToMinimulBack();
            }
            else
            {
                mtbPage.Value = Math.Max(mtbPage.Minimum, Math.Min(mtbPage.Maximum, mtbPage.Value - delta));
            }
        }

        MessageForwarder msView, msStatusStrip, msPage;

        ToolStripSpringTextBox currentImageLongPathToolStripTextBox = new ToolStripSpringTextBox();
        bool ViewerForm_Load_Called = false;
        //bool ViewerForm_Load_CallTick = false;
        private void ViewerForm_Load(object sender, EventArgs e)
        {

            var font = pageToolStripStatusLabel.Font; // Dispose してはならない
            Func<string, int> measure = text => TextRenderer.MeasureText(text, font).Width; // font が null でも動作するが戻り値が決まった値に成らないので注意
            var digitWithMaximumSize = CatalogForm.GetDigitWithMaximumSize(measure);

            //pageToolStripStatusLabel.Width = Program.DpiScalingX(70);// (70 * Program.DisplayMagnificationX4 + 2) / 4;
            pageToolStripStatusLabel.Width = CatalogForm.GetMaxWidthWithTemplates(measure, digitWithMaximumSize, " 00000 / 00000");
            imageSizeToolStripStatusLabel.Width = CatalogForm.GetMaxWidthWithTemplates(measure, digitWithMaximumSize, "00000x00000");

            currentImageLongPathToolStripTextBox.ReadOnly = true;
            if (MenuTextFont != null)
            {
                currentImageLongPathToolStripTextBox.Font = MenuTextFont;
            }
            //currentImageLongPathToolStripTextBox.MouseMove += currentImageLongPathToolStripTextBox_MouseMove;
            //currentImageLongPathToolStripTextBox.MouseEnter += currentImageLongPathToolStripTextBox_MouseEnter;
            //ActivateLeaveTracking();
            currentImageLongPathToolStripTextBox.MouseLeave += currentImageLongPathToolStripTextBox_MouseLeave;
            statusStrip.Items.Insert(0, currentImageLongPathToolStripTextBox);
            //MessageBox.Show(statusStrip.Items.Count.ToString());

            msView = new MessageForwarder(pbView, ForwardedMessage.MouseWheel);
            msStatusStrip = new MessageForwarder(statusStrip, ForwardedMessage.MouseWheel);
            msPage = new MessageForwarder(mtbPage, ForwardedMessage.MouseWheel);

            /*
            foreach (ToolStripItem item in statusStrip.Items) item.MouseDown += (sender2, e2) =>
            {
                EventHandlerForOpenNextOrPrevious(statusStrip, e2);
            };
            */

            if (LoadSettingToLoad_FormLocation != null)
            {
                Location = (Point)LoadSettingToLoad_FormLocation;
                LoadSettingToLoad_FormLocation = null;
            }

            // Location 変更後であること
            //if (ViewerForm_Load_CallTick) tmWatchIpc_Tick(sender, null);

            ViewerForm_Load_Called = true;
        }

        private void SetMessageForwarderStop(bool stop)
        {
            if (msView != null)
            {
                msView.Stop = stop;
                msStatusStrip.Stop = stop;
                msPage.Stop = stop;
            }
        }

        private Control[] SetMessages_LayoutControls;
        public void SetMessages()
        {
            if (SetMessages_LayoutControls == null)
            {
                SetMessages_LayoutControls = new Control[]
                {
                    menuStrip,
                    startToolStripMenuItem.DropDown,
                    loadArchivesInArchiveToolStripMenuItem.DropDown,
                    viewToolStripMenuItem.DropDown,
                    viewToolStripMenuItem.DropDown,
                    scalingAlgorithmNormalToolStripMenuItem.DropDown,
                    scalingAlgorithmMagnifierToolStripMenuItem.DropDown,
                    moveToolStripMenuItem.DropDown,
                    ratioToolStripMenuItem.DropDown,
                    slideshowToolStripMenuItem.DropDown,
                    editToolStripMenuItem.DropDown,
                    optionToolStripMenuItem.DropDown,
                };
            }

            foreach (var control in SetMessages_LayoutControls) control.SuspendLayout();

            startToolStripMenuItem.Text = Message._Start;
            viewToolStripMenuItem.Text = Message._View;
            //imageFilterToolStripMenuItem.Text = Message._Image;
            moveToolStripMenuItem.Text = Message._Move;
            editToolStripMenuItem.Text = Message._Edit;

            /*
            openFileContextMenuForMenuStripMenuItem.Text = Message.OpenFile + " (Ctrl+O)...";
            openNewThumbnailWindowContextMenuForMenuStripMenuItem.Text = Message.OpenNewThumbnailWindow + " (Ctrl+T)...";
            fullscreenContextMenuForMenuStripMenuItem.Text = Message.FullScreen + " (Ctrl+Shift+F)";
            windowContextMenuForMenuStripMenuItem.Text = Message.Window + " (Esc)";
            togglefullscreenmodeContextMenuForMenuStripMenuItem.Text = Message.ToggleFullScreenMode + " (F11)";
            exitContextMenuForMenuStripMenuItem.Text = Message.Exit + " (Ctrl+Q)";
            */
            openFileToolStripMenuItem.Text = Message.OpenFile + " ...";
            //openNewThumbnailWindowToolStripMenuItem.Text = Message.OpenNewThumbnailWindow;
            switchToThumbnailWindowToolStripMenuItem.Text = Message.SwitchToThumbnailWindow;
            cloneTheWindowToolStripMenuItem.Text = Message.CloneWindow;
            loadWholeArchiveIntoMemoryToolStripMenuItem.Text = Message.LoadWholeArchiveIntoMemory;
            readOnMemoryNoneToolStripMenuItem.Text = Message.NoneRecommended;
            readOnMemoryExceptLookAheadToolStripMenuItem.Text = Message.ExceptReadAheadProcess;
            readOnMemoryAlwaysToolStripMenuItem.Text = Message.Always;
            loadArchivesInArchiveToolStripMenuItem.Text = Message.LoadArchivesInArchive;
            nonerecommendedToolStripMenuItem.Text = Message.NoneRecommended;
            untilFoundToolStripMenuItem.Text = Message.TwoLevelUntilFound;
            alwaysLoadArchivesInArchiveToolStripMenuItem.Text = Message.OneLevelCompletelyNotRecommended;
            createViewerShortcutFromCurrentSettingsToolStripMenuItem.Text = Message.CreateShortcutFromCurrentSettings + "...";
            fullScreenInStartToolStripMenuItem.Text = Message.Fullscreen;
            windowToolStripMenuItem.Text = Message.Window;
            toggleFullScreenModeToolStripMenuItem.Text = Message.ToggleFullscreenMode;
            exitToolStripMenuItem.Text = Message.Exit;

            alwaysAutoHideUIToolStripMenuItem.Text = Message.AlwaysAutomaticallyHideUI;

            /*
            magnifierContextMenuForMenuStripMenuItem.Text = Message.Magnifier;
            magnifierContextMenuForMenuStripMenuItem.ShortcutKeyDisplayString = "";
            rightToLeftContextMenuForMenuStripMenuItem.Text = Message.RightToLeft;
            leftToRightContextMenuForMenuStripMenuItem.Text = Message.LeftToRight;
            */
            magnifierToolStripMenuItem.Text = Message.ToggleMagnifier;
            //magnifierToolStripMenuItem.ShortcutKeyDisplayString = Message.MiddleClick + ", Z";
            enableMagnifierToolStripMenuItem.Text = Message.EnableMagnifier;
            disableMagnifierToolStripMenuItem.Text = Message.DisableMagnifier;

            scalingAlgorithmNormalToolStripMenuItem.Text = Message.ScalingAlgorithmNormal;
            scalingAlgorithmMagnifierToolStripMenuItem.Text = Message.ScalingAlgorithmMagnifier;
            defaultScalingAlgorithmNormalToolStripMenuItem.Text = Message.DefaultHighQuality;
            default2ScalingAlgorithmNormalToolStripMenuItem.Text = Message.DefaultHighSpeed;
            defaultMagnifierToolStripMenuItem.Text = Message.DefaultHighSpeed;
            default0MagnifierToolStripMenuItem.Text = Message.DefaultHighQuality;
            nearestNeighborMagnifierToolStripMenuItem.Text = Message.NearestNeighbor;
            nearestNeighborNormalToolStripMenuItem.Text = Message.NearestNeighbor;
            areaAverageMagnifierToolStripMenuItem.Text = Message.AreaAverage;
            areaAverageNormalToolStripMenuItem.Text = Message.AreaAverage;

            /*
#if !AUTOBUILD
            antiMoire1ToolStripMenuItem.Text = Message.AntiMoire + " 3/4";
            antiMoire2ToolStripMenuItem.Text = Message.AntiMoire + " 4/5";
            antiMoire3ToolStripMenuItem.Text = Message.AntiMoire + " 5/6";
            antiMoire4ToolStripMenuItem.Text = Message.AntiMoire + " 6/7";
#endif
            */
            linearizeColorSpaceforGamma22NormalToolStripMenuItem.Text = Message.LinearizeColorSpace;
            linearizeColorSpaceforGamma22MagnifierToolStripMenuItem.Text = Message.LinearizeColorSpace;

            useAreaAverageWhenUpsizingNormalToolStripMenuItem.Text = Message.UseAreaAverageWhenUpsizing;
            useAreaAverageWhenUpsizingToolStripMenuItem.Text = Message.UseAreaAverageWhenUpsizing;

            leftToRightToolStripMenuItem.Text = Message.LeftToRight;
            rightToLeftToolStripMenuItem.Text = Message.RightToLeft;
            singlePageToolStripMenuItem.Text = Message.SinglePage;
            singlePageWithoutScalingUpToolStripMenuItem.Text = Message.SinglePageWithoutScalingUp;
            togglePageSequenceToolStripMenuItem.Text = Message.TogglePageSequence;
            //forceTwoPageSpreadToolStripMenuItem.Text = Message.ForcePageSpread;
            forceFirstPageToBeSingleToolStripMenuItem.Text = Message.ForceFirstPageToBeSingle;
            forceTwoPageSpreadToolStripMenuItem.Text = Message.PrioritizePageSpread;
            allowPageDivisionToolStripMenuItem.Text = Message.AllowPageDivision;

            orientSeekBarDirectionToPageSequenceToolStripMenuItem.Text = Message.OrientSeekBarDirectionToPageSequence;
            selectBackgroundColorToolStripMenuItem.Text = Message.SelectBackgroundColor + "...";
            showAHintWhenChangingSettingsToolStripMenuItem.Text = Message.ShowHintWhenChangingSettings;

            slideshowToolStripMenuItem.Text = Message.S_lideshow;
            startSlideshowToolStripMenuItem.Text = Message.StartSlideshow;
            stopSlideshowToolStripMenuItem.Text = Message.StopSlideshow;
            toggleSlideshowToolStripMenuItem.Text = Message.ToggleSlideshow;
            _1SecIntervalsToolStripMenuItem.Text = Message.OneSecondIntervals;
            _2SecIntervalsToolStripMenuItem.Text = Message._1SecondsIntervals.Replace("$1", "2");
            _3SecIntervalsToolStripMenuItem.Text = Message._1SecondsIntervals.Replace("$1", "3");
            _5SecIntervalsToolStripMenuItem.Text = Message._1SecondsIntervals.Replace("$1", "5");
            _10SecIntervalsToolStripMenuItem.Text = Message._1SecondsIntervals.Replace("$1", "10");
            _20SecIntervalsToolStripMenuItem.Text = Message._1SecondsIntervals.Replace("$1", "20");
            _30SecIntervalsToolStripMenuItem.Text = Message._1SecondsIntervals.Replace("$1", "30");
            _60SecIntervalsToolStripMenuItem.Text = Message._1SecondsIntervals.Replace("$1", "60");
            repeatToolStripMenuItem.Text = Message.Repeat;
            openNextOnTerminalToolStripMenuItem.Text = Message.AllowToOpenNextItem;

            virtualFoldersToolStripMenuItem.Text = Message.Virtual_Folder;

            rotateLeftToolStripMenuItem.Text = Message.RotateLeft;
            rotateRightToolStripMenuItem.Text = Message.RotateRight;
            cancelRotationSettingToolStripMenuItem.Text = Message.CancelRotationSetting;
            //binarizationToolStripMenuItem.Text = Message.Binarization;
            //autoContrastControlToolStripMenuItem.Text = Message.AutoContrastControl;

            //removeMoireToolStripMenuItem.Text = Message.RemoveMoire;

            nextPageToolStripMenuItem.Text = Message.NextPage;
            previousPageToolStripMenuItem.Text = Message.PreviousPage;
            rightPageToolStripMenuItem.Text = Message.RightPage;
            leftPageToolStripMenuItem.Text = Message.LeftPage;
            //SetForwerdBackMessage();
            //forward1pageContextMenuForMenuStripMenuItem.Text = Message.ForwardOneWheelDownOnBarCursorDown;
            //back1pageContextMenuForMenuStripMenuItem.Text = Message.BackOneWheelDownOnBarCursorDown;
            moveForward1PageToolStripMenuItem.Text = Message.MoveForwardOnePage;
            //moveForward1PageToolStripMenuItem.ShortcutKeyDisplayString = Message.WheelDownOnBar + ", " + Message.CursorDown;
            moveBack1PageToolStripMenuItem.Text = Message.MoveBackOnePage;
            //moveBack1PageToolStripMenuItem.ShortcutKeyDisplayString = Message.WheelUpOnBar + ", " + Message.CursorUp;
            moveRight1PageToolStripMenuItem.Text = Message.MoveRightOnePage;
            moveLeft1PageToolStripMenuItem.Text = Message.MoveLeftOnePage;

            ratioToolStripMenuItem.Text = Message.PositionRatio;

            openNextToolStripMenuItem.Text = Message.OpenNext;
            openPreviousToolStripMenuItem.Text = Message.OpenPrevious;
            openRightToolStripMenuItem.Text = Message.OpenRight;
            openLeftToolStripMenuItem.Text = Message.OpenLeft;

            //openNextToolStripMenuItem.ShortcutKeyDisplayString = Message.MiddleClickOnPosteriorHalfOfBar + ", Alt+" + Message.CursorRight; 
            //openPreviousToolStripMenuItem.ShortcutKeyDisplayString = Message.MiddleClickOnAnteriorHalfOfBar + ", Alt+" + Message.CursorLeft;

            /*
            setThumbnailManuallyContextMenuForMenuStripMenuItem.Text = Message.SetThumbnailManually;
            clearThumbnailSettingContextMenuForMenuStripMenuItem.Text = Message.ClearThumbnailSetting;
            saveCurrentPageSequenceContextMenuForMenuStripMenuItem.Text = Message.SaveCurrentPageSequence;
            clearPageSequenceSettingContextMenuForMenuStripMenuItem.Text = Message.ClearPageSequenceSetting;
            */

            noneToolStripMenuItem.Text = Message.None;
            tagEditorToolStripMenuItem.Text = Message.EditTags + "...";
            addTagsFromTheFileNameToolStripMenuItem.Text = Message.AddTagsFromName + "...";
            uncheckAllToolStripMenuItem.Text = Message.UncheckAll;
            //uncheckAllToolStripMenuItem.ShortcutKeyDisplayString = Message.RightClickNotToClose;
            clearPageSequenceSettingToolStripMenuItem.Text = Message.ClearPageSequenceSetting;
            saveCurrentPageSequenceToolStripMenuItem.Text = Message.SaveCurrentPageSequence;
            clearThumbnailSettingToolStripMenuItem.Text = Message.ClearCoverSetting;
            //setThumbnailManuallyToolStripMenuItem.Text = Message.SetThumbnailManually;

            optionToolStripMenuItem.Text = Message._Others;
            tagEditorToolStripMenuItem1.Text = Message.Edit_Tags + "...";
            preferenceToolStripMenuItem1.Text = Message.Common_Settings + "...";
            //mouseGestureToolStripMenuItem.Text = Message._MouseGestures + "...";
            mouseGestureToolStripMenuItem.Text = Message._MouseTouchGestures + "...";
            //keyboardShortcutToolStripMenuItem.Text = Message._KeysMouseButtons + "...";
            keyboardShortcutToolStripMenuItem.Text = Message._BasicOperationSettings + "...";

            fullScreenToolStripMenuItem.Text = FullScreen ? Message.Window : Message.Fullscreen;

            setShortcutKeyDisplayStrings(setKeyNames: true);

            for (var i = SetMessages_LayoutControls.Length - 1; i >= 0; i--)
            {
                var control = SetMessages_LayoutControls[i];
                control.ResumeLayout(performLayout: false);
                control.PerformLayout();
            }
        }

        /*
        private void SetForwerdBackMessage(bool? pageLeftToRight = null)
        {
            if (pageLeftToRight == null)
                pageLeftToRight = PageLeftToRight;

            if (pageLeftToRight == true)
            {
                //forwardContextMenuForMenuStripMenuItem.Text = Message.ForwardLeftButtonOnRightHalfInLeftToRightWheelDownCursorRight;
                //backContextMenuForMenuStripMenuItem.Text = Message.BackLeftButtonOnLeftHalfInLeftToRightWheelUpCursorLeft;
                nextPageToolStripMenuItem.ShortcutKeyDisplayString = Message.LeftClickOnRightHalf + " (" + Message.LeftToRight + "), " + Message.WheelDown + ", " + Message.CursorRight;
                previousPageToolStripMenuItem.ShortcutKeyDisplayString = Message.LeftClickOnLeftHalf + " (" + Message.LeftToRight + "), " + Message.WheelUp + ", " + Message.CursorLeft;

                //openNextToolStripMenuItem.ShortcutKeyDisplayString = Message.MiddleClickOnRightHalfOfBar + " (" + Message.LeftToRight + ")";
                //openPreviousToolStripMenuItem.ShortcutKeyDisplayString = Message.MiddleClickOnLeftHalfOfBar + " (" + Message.LeftToRight + ")";
            }
            else
            {
                //forwardContextMenuForMenuStripMenuItem.Text = Message.ForwardLeftButtonOnLeftHalfInRightToLeftWheelDownCursorRight;
                //backContextMenuForMenuStripMenuItem.Text = Message.BackLeftButtonOnRightHalfInRightToLeftWheelUpCursorLeft;
                nextPageToolStripMenuItem.ShortcutKeyDisplayString = Message.LeftClickOnLeftHalf + " (" + Message.RightToLeft + "), " + Message.WheelDown + ", " + Message.CursorRight;
                previousPageToolStripMenuItem.ShortcutKeyDisplayString = Message.LeftClickOnRightHalf + " (" + Message.RightToLeft + "), " + Message.WheelUp + ", " + Message.CursorLeft;


                //openNextToolStripMenuItem.ShortcutKeyDisplayString = Message.MiddleClickOnLeftHalfOfBar + " (" + Message.RightToLeft + ")";
                //openPreviousToolStripMenuItem.ShortcutKeyDisplayString = Message.MiddleClickOnRightHalfOfBar + " (" + Message.RightToLeft + ")";
            }
        }
        */

        private int NextPage
        {
            get
            {
                if (ResizedImageArray == null)
                {
                    return 0;
                }

                var totalWidth = 0;
                var stopPage = currentPage;
                var MaxPageCountInWindowForNext = onePageModeForNext ? 1 : MaxPageCountInWindow;
                var screenWidth = null as int?;
                for (var i = currentPage; i < currentPage + MaxPageCountInWindowForNext && i < ResizedImageArray.Length; i++)
                {
                    var img = ResizedImageArray[i];
                    if (img == null)
                    {
                        var imgSize = ResizedSizeArray[i];
                        if (imgSize.IsEmpty)
                        {
                            return currentPage;
                        }
                        totalWidth += imgSize.Width;
                    }
                    else
                    {
                        totalWidth += img.Width;
                    }
                    stopPage++;
                    var pbViewWidth = FullScreen_pseudoFullScreen ? screenWidth ?? (int)(screenWidth = Screen.FromControl(this).Bounds.Width) :
                        PseudoMaximized ? PseudoMaximizedSize.Width : ViewerForm_SizeChanged_pbBoundsMemoryForMinimize.Width; // pbView.Width;
                    if (totalWidth >= pbViewWidth)
                    {
                        if (totalWidth != pbViewWidth)
                        {
                            stopPage--;
                            break; // img == null でも stopPage を返す唯一のパターン
                        }
                        if (img == null) return currentPage;
                        break;
                    }
                    if (img == null) return currentPage;
                }
                return stopPage;
            }
        }

        private int PreviousPage
        {
            get
            {
                if (ResizedImageArray == null)
                {
                    return -1;
                }
                var totalWidth = 0;
                var stopPage = currentPage;
                var screenWidth = null as int?;
                for (var i = currentPage - 1; i >= currentPage - MaxPageCountInWindow && i >= 0; i--)
                {
                    var img = ResizedImageArray[i];
                    if (img == null)
                    {
                        var imgSize = ResizedSizeArray[i];
                        if (imgSize.IsEmpty)
                        {
                            if (MaxPageCountInWindow > 1)
                            {
                                return currentPage;
                            }
                            else
                            {
                                stopPage--;
                                break;
                            }
                        }
                        totalWidth += imgSize.Width;
                    }
                    else
                    {
                        totalWidth += img.Width;
                    }
                    stopPage--;
                    var pbViewWidth = FullScreen_pseudoFullScreen ? screenWidth ?? (int)(screenWidth = Screen.FromControl(this).Bounds.Width) :
                        PseudoMaximized ? PseudoMaximizedSize.Width : ViewerForm_SizeChanged_pbBoundsMemoryForMinimize.Width;
                    if (totalWidth >= pbViewWidth)
                    {
                        if (totalWidth != pbViewWidth)
                        {
                            stopPage++;
                            break; // img == null でも stopPage を返す唯一のパターン
                        }
                        if (img == null) return currentPage;
                        break;
                    }
                    if (img == null) return currentPage;
                }
                return stopPage;
            }
        }

        private bool FullScreen_pseudoFullScreen = false;
        private bool FullScreen
        {
            get
            {
                return FormBorderStyle == FormBorderStyle.None && WindowState == FormWindowState.Maximized;
            }
            set
            {
                // 中途半端な状態の修復効果を得るため、フルスクリーン化は現在の状態にかからわず実行する
                if (value)
                {
                    var tempPrevSize = prevSize;

                    if (LoadSettings_FormWindowStateFromSetteingFile == null)
                    {
                        prevFormWindowState = WindowState;
                    }
                    else
                    {
                        prevFormWindowState = (FormWindowState)LoadSettings_FormWindowStateFromSetteingFile;
                        LoadSettings_FormWindowStateFromSetteingFile = null;
                    }
                    prevFormBorderStyle = FormBorderStyle;
                    var unvisibled = false;
                    var temp = pbView_SizeChanged_SizeChanging;
                    pbView_SizeChanged_SizeChanging = true;
                    var needToCareMoving = false;
                    if (WindowState == FormWindowState.Maximized && ipcLookAheadInfo == null)
                    {
                        if (Visible)
                        {
                            Visible = false;
                            unvisibled = true;
                        }
                        //var temp2 = betterFormRestoreBound.StopMemoryNormalStateBound;
                        //betterFormRestoreBound.StopMemoryNormalStateBound = true;
                        WindowState = FormWindowState.Normal; // タスクバーが残る問題の回避
                        //betterFormRestoreBound.StopMemoryNormalStateBound = temp2;
                        needToCareMoving = true;
                    }

                    if (ipcLookAheadInfo != null)
                    {
                        pbView_SizeChanged_SizeChanging = temp;

                        FullScreen_pseudoFullScreen = true;
                    }
                    else
                    {
                        var currentDisplay = needToCareMoving ? Screen.FromControl(this).WorkingArea : Rectangle.Empty;
                        FormBorderStyle = FormBorderStyle.None;
                        if (needToCareMoving)
                        {
                            var memoriedDisplay = Screen.FromControl(this).WorkingArea;
                            if (currentDisplay != memoriedDisplay)
                            {
                                Bounds = BetterFormRestoreBounds.GetMovedWindowBounds(currentDisplay, Bounds, memoriedDisplay);
                                /*
                                var bounds = Bounds;

                                // 原点を基準に平行移動
                                bounds.X += currentDisplay.X - memoriedDisplay.X;
                                bounds.Y += currentDisplay.Y - memoriedDisplay.Y;

                                // 範囲内に押し込む
                                //var temp2 = betterFormRestoreBound.StopMemoryNormalStateBound;
                                //betterFormRestoreBound.StopMemoryNormalStateBound = true;
                                Bounds = Pack(bounds, currentDisplay);
                                //betterFormRestoreBound.StopMemoryNormalStateBound = temp2;
                                */
                            }
                        }
                        pbView_SizeChanged_SizeChanging = temp;
                        WindowState = FormWindowState.Maximized;

                        // ViewerForm_SizeChanged による書き換えを元に戻す
                        if (prevFormWindowState == FormWindowState.Maximized && prevSize.IsEmpty) prevSize = tempPrevSize;

                        if (unvisibled) Visible = true;
                        if (TopMostInFullscreen) TopMost = true;
                    }
                }

                // フルスクリーンでない場合、全画面かどうかが無意味に変更されないように条件をつける
                else if (FormBorderStyle == FormBorderStyle.None)
                {
                    //if (!prevClientSize.IsEmpty) ClientSize = prevClientSize;
                    if (prevFormWindowState == FormWindowState.Maximized && !prevSize.IsEmpty)
                    {
                        //var temp2 = betterFormRestoreBound.StopMemoryNormalStateBound;
                        //betterFormRestoreBound.StopMemoryNormalStateBound = true;
                        Size = prevSize;
                        //betterFormRestoreBound.StopMemoryNormalStateBound = temp2;
                    }
                    if (prevFormWindowState == FormWindowState.Normal && ipcLookAheadInfo == null) prevSize = Size.Empty;

                    var temp = pbView_SizeChanged_SizeChanging;
                    pbView_SizeChanged_SizeChanging = true;

                    if (FormBorderStyle == FormBorderStyle.None)
                    {
                        //var temp2 = betterFormRestoreBound.StopMemoryNormalStateBound;
                        //betterFormRestoreBound.StopMemoryNormalStateBound = true;
                        FormBorderStyle = prevFormBorderStyle;
                        //betterFormRestoreBound.StopMemoryNormalStateBound = temp2;
                    }

                    pbView_SizeChanged_SizeChanging = temp;
                    WindowState = prevFormWindowState;

                    TopMost = false;
                }
            }
        }
        private FormWindowState prevFormWindowState;
        private FormBorderStyle prevFormBorderStyle;
        private Size prevSize = Size.Empty;

        //Size? normalStateSize = null;

        //Point? ViewerForm_SizeChanged_Point = null;

        private Size ViewerForm_SizeChanged_CurrentSize = Size.Empty;
        private FormWindowState ViewerForm_SizeChanged_CurrentWindowState;
        private Rectangle ViewerForm_SizeChanged_pbBoundsMemoryForMinimize = Rectangle.Empty;
        private FormWindowState ViewerForm_SizeChanged_BeforeMinimizedState = FormWindowState.Normal;
        private void ViewerForm_SizeChanged(object sender, EventArgs e)
        {
            var ws = WindowState;
            var pre = ViewerForm_SizeChanged_CurrentWindowState;

            /*
            if (pre == FormWindowState.Maximized && ws == FormWindowState.Normal && ViewerForm_SizeChanged_Point != null)
            {
                Location = (Point)ViewerForm_SizeChanged_Point;
            }
            else if (ws == FormWindowState.Normal)
            {
                ViewerForm_SizeChanged_Point = Location;
            }
            */

            //if (ws != FormWindowState.Minimized) ViewerForm_SizeChanged_pbSizeMemoryForMinimize = pbView.Size;
            
            if (pre == FormWindowState.Maximized && ws == FormWindowState.Normal && normalStateLocation != null)
            {
                Location = (Point)normalStateLocation;
            }
            else if (ws == FormWindowState.Normal)
            {
                normalStateLocation = Location;
            }

            if (ws == FormWindowState.Minimized && pre != FormWindowState.Minimized)
            {
                ViewerForm_SizeChanged_BeforeMinimizedState = pre;
            }

            if (ws != ViewerForm_SizeChanged_CurrentWindowState)
            {
                ViewerForm_SizeChanged_CurrentWindowState = ws;
                if (ws != FormWindowState.Normal)
                {
                    if (prevSize.IsEmpty && !ViewerForm_SizeChanged_CurrentSize.IsEmpty)
                    {
                        prevSize = ViewerForm_SizeChanged_CurrentSize;
                    }
                    ViewerForm_SizeChanged_CurrentSize = Size.Empty;
                }
            }
            if (ws == FormWindowState.Normal && ipcLookAheadInfo == null)
            {
                //ViewerForm_SizeChanged_CurrentSize = ClientSize;
                if (!pbView_SizeChanged_SizeChanging)
                {
                    ViewerForm_SizeChanged_CurrentSize = Size;
                }
                prevSize = Size.Empty;

            }
            /*
            if (pre == FormWindowState.Normal&& ipcLookAheadInfo == null)
            {
                normalStateSize = Size;
            }
            */
        }

        const int DragEventKeyState_LeftButton = 1;
        private void ViewerForm_DragEnter(object sender, DragEventArgs e)
        {
            if (ExitAfterCoverSetting)
            {
                e.Effect = DragDropEffects.None;
                return;
            }
            try
            {
                if (e.KeyState == DragEventKeyState_LeftButton && e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] Files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
                    if (Files.Length == 1)
                    {
                        var path = ShortcutResolver.Exec(Files[0]);
                        if (Directory.Exists(path))
                        {
                            e.Effect = DragDropEffects.Copy;
                        }
                        else if (File.Exists(path))
                        {
                            if (PackedImageLoader.Supports(path) || ImageLoader.IndividualSupportFullReading(path))
                            {
                                e.Effect = DragDropEffects.Copy;
                            }
                            else
                            {
                                var ffmpegExists = null as bool?;
                                ImageLoader.SusieReset();
                                if (ImageLoader.SupportsFullReading(path, ref ffmpegExists))
                                {
                                    e.Effect = DragDropEffects.Copy;
                                }
                                else
                                {
                                    e.Effect = DragDropEffects.None;
                                }
                            }
                        }
                        else
                        {
                            e.Effect = DragDropEffects.None;
                        }
                    }
                    else
                    {
                        e.Effect = DragDropEffects.None;
                    }
                }
            }
            catch (Exception error)
            {
                Program.AlertError(error);
            }
        }

        private void ViewerForm_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                var path = ShortcutResolver.Exec(((string[])e.Data.GetData(DataFormats.FileDrop, false))[0]);
                removeHiddenConfigFromCatalogForm();
                OpenFile(path);
            }
            catch (Exception error)
            {
                Program.AlertError(error);
            }
        }

        private PackedImageLoader loader = null;
        private Size ViewSize;
        private string currentArchiveFilePath = null;
        private string currentStartingFilePath = null;
        private SortModeDetails currentSortModeDetails = null;
        //private PackedImageLoader nextLoader = null;
        //private int[] nextEntries = null;
        //private PackedImageLoader.PackedImageEntry[] nextEntries = null;
        //private string[] nextEntriesPath = null;
        class NextData : IDisposable
        {
            public readonly string[] EntriesPaths;
            public readonly PackedImageLoader.PackedImageEntry[] Entries;
            public readonly PackedImageLoader Loader;
            public NextData(string[] entriesPaths, PackedImageLoader.PackedImageEntry[] entries, PackedImageLoader loader)
            {
                EntriesPaths = entriesPaths;
                Entries = entries;
                Loader = loader;
            }

            bool disposed = false;
            public void Dispose()
            {
                if (!disposed)
                {
                    disposed = true;
                    Loader.Dispose();
                }
            }
        }
        NextData nextData; 
        private bool DoNotSaveSetting = false;
        private OpenResult OpenFile(string path) { return OpenFile(path, -1); }
        private string OpenTriedArchiveFilePath = null;
        private bool OpenFile_UserOpen = false;
        //private bool OpenFileFinishedButStartingIsNotCalled = false;
        private static readonly SortMode[] OpenFile_SuportedSortModes = new SortMode[] {
            SortMode.TypeInAsc, SortMode.TypeInDesc, SortMode.NameInAsc, SortMode.NameInDesc,
            SortMode.RatingInAsc, SortMode.RatingInDesc,
            SortMode.CreatedInAsc, SortMode.CreatedInDesc, SortMode.AccessedInAsc, SortMode.AccessedInDesc,
            SortMode.ModifiedInAsc, SortMode.ModifiedInDesc, SortMode.SizeInAsc, SortMode.SizeInDesc,
        };
        private OpenResult OpenFile(string path, int startPage, SortModeDetails sort = null, string startName = null, bool forLookAhead = false)//, BindingMode? binding = null)
        {
            ImageLoader.SusieReset();
            path = Program.GetFullPath(path);
            OpenTriedArchiveFilePath = path;
            if (bmwLoadEachPage.IsBusy)
            {
                bmwLoadEachPage.CancelAsync();
            }
            nextData?.Dispose();
            nextData = null;

            // currentArchiveFilePath を書き換える前に今開いているファイルを履歴に保存
            if (ipcLookAheadInfo == null && !DoNotSaveSetting && !string.IsNullOrEmpty(currentStartingFilePath) &&
                !string.IsNullOrEmpty(currentArchiveFilePath) && currentPage >= 0)
            {
                //var page = currentStartingFilePath == currentArchiveFilePath ? Math.Max(0, currentPage) : -1;
                if (saveToHistory)
                {
                    trySaveCurrentConditionToHistory(SelfLastAccessTimeUpdateMode.Reserve); // ここでアクセス日時が更新される
                }
            }

            string name = null;
            currentStartingFilePath = path;

            bool sortShouldBeUsed;

            char separator = Path.DirectorySeparatorChar;
            
            try
            {
                //if (ImageLoader.SupportsFullReading(path))
                if (!PackedImageLoader.Supports(path) && !Directory.Exists(path))
                {
                    var altPos = path.IndexOf(Path.AltDirectorySeparatorChar);
                    string container = null;
                    if (altPos >= 0 && altPos < path.Length - 1)
                    {
                        container = path.Substring(0, altPos);
                        try
                        {
                            if (!File.Exists(container)) container = null;
                        }
                        catch
                        {
                            container = null;
                        }
                    }
                    if (container == null)
                    {
                        if (ImageLoader.SupportsFullReading(path, ffmpegExists: true) && File.Exists(path))
                        {
                            currentArchiveFilePath = Path.GetDirectoryName(path);
                            if (currentArchiveFilePath.Last() == Path.DirectorySeparatorChar)
                            {
                                currentArchiveFilePath = currentArchiveFilePath.Substring(0, currentArchiveFilePath.Length - 1);
                            }
                            name = Path.GetFileName(path);
                            sortShouldBeUsed = true;
                        }
                        else
                        {
                            currentArchiveFilePath = path;
                            sortShouldBeUsed = false;
                        }
                    }
                    else
                    {
                        currentArchiveFilePath = container;
                        name = path.Substring(altPos + 1);
                        sortShouldBeUsed = true;
                        separator = Path.AltDirectorySeparatorChar;
                    }

                }
                else
                {
                    currentArchiveFilePath = path;
                    sortShouldBeUsed = false;
                }
            }
            catch
            {
                currentArchiveFilePath = path;
                name = null;
                sortShouldBeUsed = false;
            }

            currentSortModeDetails = sort?.Clone(); // 参照渡しでは requested も変更されてしまう

            PackedImageLoader nextLoaderBase;
            PackedImageLoader.PackedImageEntry[] nextEntriesBase;
            string[] nextEntriesPathBase;
            try
            {
                if (ipcLookAheadInfo == null)
                {
                    Program.TryUpdateLastAccessTime(path); // 先読みでなければファイルを開く前もアクセス日時を更新

                    // 先読み中でなければ選択ファイル変更のリクエスト
                    // 画像ファイルをドロップした場合にその親フォルダが指定される currentArchiveFilePath は不適切
                    if (!mtbPage_ValueChanged_SendRequestToChangeSelection_Stop)
                    {
                        if (!string.IsNullOrEmpty(path))
                        {
                            SendRequestToChangeSelection(path, requestedSortModeDetails?.SortMode); // 第二引数がない場合ページ送り用のソートモードが利用される
                        }
                    }
                }

                nextLoaderBase = getPackedImageLoader(currentArchiveFilePath, forLookAhead ? ViewerFormOnMemoryMode.ForLookAhead : ViewerFormOnMemoryMode.Default);
                var nextLoader = nextLoaderBase;
                if (!sortShouldBeUsed)
                {
                    // Full でない方は一部の画像をスキップする。
                    nextEntriesBase = applyFilterAndReturnItsSelf(nextLoader, nextLoader.GetPackedImageEntries(needToExecOnTask: true)/*, noOp: nextLoader.Type != PackedImageLoader.PackType.Directory*/).ToArray();
                    if (sort == null) currentSortModeDetails = new SortModeDetails();
                    currentSortModeDetails.SortMode = SortMode.NameInAsc;
                }
                else if (sort == null || sort.SortMode == SortMode.NameInAsc)
                {
                    // 画像ファイル指定の場合は NameInAsc でもスキップしない
                    // それが本当に最適か、ここの仕様設計は難しいが。
                    //nextEntriesBase = applyFilterAndReturnItsSelf(nextLoader.GetPackedImageFullEntries(needToExecOnTask: true), noOp: nextLoader.Type != PackedImageLoader.PackType.Directory)
                    //    .OrderBy(entry => entry.Path, new LogicalStringComparer()).ToArray();


                    nextEntriesBase = getNameSortedEntries(applyFilterAndReturnItsSelf(nextLoader, nextLoader.GetPackedImageFullEntries(needToExecOnTask: true)/*, noOp: nextLoader.Type != PackedImageLoader.PackType.Directory*/)
                        , nextLoader.Type, desc: false);
                    if (sort == null)
                    {
                        currentSortModeDetails = new SortModeDetails();
                        currentSortModeDetails.SortMode = SortMode.NameInAsc;
                    }
                }
                else if (sort.SortMode == SortMode.NameInDesc)
                {
                    //nextEntriesBase = applyFilterAndReturnItsSelf(nextLoader.GetPackedImageFullEntries(needToExecOnTask: true), noOp: nextLoader.Type != PackedImageLoader.PackType.Directory)
                    //    .OrderByDescending(entry => entry.Path, new LogicalStringComparer()).ToArray();


                    nextEntriesBase = getNameSortedEntries(applyFilterAndReturnItsSelf(nextLoader, nextLoader.GetPackedImageFullEntries(needToExecOnTask: true)/*, noOp: nextLoader.Type != PackedImageLoader.PackType.Directory*/)
                        , nextLoader.Type, desc: true);
                }
                else if (sort.SortMode == SortMode.Random && sort.RandomSeed != null)
                {
                    var temp = applyFilterAndReturnItsSelf(nextLoader, nextLoader.GetPackedImageFullEntries(needToExecOnTask: true)/*, noOp: nextLoader.Type != PackedImageLoader.PackType.Directory*/);
                    var withSep = currentArchiveFilePath + separator;
                    var seed = sort.RandomSeed;
                    var table = (from e in temp select CatalogForm.GetRandomIndex(withSep + e.Path, seed)).ToArray();
                    var order = new int[table.Length]; for (var i = 0; i < order.Length; i++) order[i] = i;
                    nextEntriesBase = (from i in order orderby table[i] select temp[i]).ToArray();
                }
                else if (OpenFile_SuportedSortModes.Contains(sort.SortMode))
                {
                    // 疑似安定ソートが必要なパターン
                    var modes = sort.SortMode == sort.PreSortMode ?
                        new SortMode[1] { sort.SortMode } :
                        new SortMode[2] { sort.PreSortMode, sort.SortMode };
                    PackedImageLoaderFileInfo needed = 0;
                    if (sort.SortMode == SortMode.ModifiedInAsc || sort.SortMode == SortMode.ModifiedInDesc ||
                       sort.PreSortMode == SortMode.ModifiedInAsc || sort.PreSortMode == SortMode.ModifiedInDesc)
                        needed |= PackedImageLoaderFileInfo.LastWriteTime;
                    if (sort.SortMode == SortMode.SizeInAsc || sort.SortMode == SortMode.SizeInDesc ||
                       sort.PreSortMode == SortMode.SizeInAsc || sort.PreSortMode == SortMode.SizeInDesc)
                        needed |= PackedImageLoaderFileInfo.Size;
                    //var sw = new System.Diagnostics.Stopwatch();
                    //sw.Start();
                    nextEntriesBase = applyFilterAndReturnItsSelf(nextLoader, nextLoader.GetPackedImageFullEntries(needed, needToExecOnTask: true)/*, noOp: nextLoader.Type != PackedImageLoader.PackType.Directory*/).ToArray();
                    //sw.Stop();MessageBox.Show(sw.Elapsed.ToString());
                    foreach (var sMode in modes)
                    {
                        switch (sMode)
                        {
                            case SortMode.TypeInAsc:
                                {
                                    var typeArray = CatalogForm.GetTypeNameArray((from e in nextEntriesBase select e.Path).ToArray());
                                    var order = new int[typeArray.Length]; for (var i = 0; i < order.Length; i++) order[i] = i;
                                    nextEntriesBase = order.
                                        OrderBy(x => typeArray[x], new LogicalStringComparer()).
                                        Select(i => nextEntriesBase[i]).ToArray();
                                    break;
                                }
                            case SortMode.TypeInDesc:
                                {
                                    var typeArray = CatalogForm.GetTypeNameArray((from e in nextEntriesBase select e.Path).ToArray());
                                    var order = new int[typeArray.Length]; for (var i = 0; i < order.Length; i++) order[i] = i;
                                    nextEntriesBase = order.
                                        OrderByDescending(x => typeArray[x], new LogicalStringComparer()).
                                        Select(i => nextEntriesBase[i]).ToArray();
                                    break;
                                }
                            case SortMode.NameInAsc:
                                //nextEntriesBase = nextEntriesBase.OrderBy(e => e.Path, new LogicalStringComparer()).ToArray();
                                nextEntriesBase = getNameSortedEntries(nextEntriesBase, nextLoader.Type, desc: false);
                                break;
                            case SortMode.NameInDesc:
                                //nextEntriesBase = nextEntriesBase.OrderByDescending(e => e.Path, new LogicalStringComparer()).ToArray();
                                nextEntriesBase = getNameSortedEntries(nextEntriesBase, nextLoader.Type, desc: true);
                                break;
                            case SortMode.RatingInAsc:
                                {
                                    var rateArray = (from e in nextEntriesBase let r = ZipPlaInfo.GetOnlyRating(e.Path) select r > 0 ? r : int.MaxValue).ToArray();
                                    var order = new int[rateArray.Length]; for (var i = 0; i < order.Length; i++) order[i] = i;
                                    nextEntriesBase = order.
                                        OrderBy(x => rateArray[x]).
                                        Select(i => nextEntriesBase[i]).ToArray();
                                    break;
                                }
                            case SortMode.RatingInDesc:
                                {
                                    var rateArray = (from e in nextEntriesBase let r = ZipPlaInfo.GetOnlyRating(e.Path) select r).ToArray();
                                    var order = new int[rateArray.Length]; for (var i = 0; i < order.Length; i++) order[i] = i;
                                    nextEntriesBase = order.
                                        OrderByDescending(x => rateArray[x]).
                                        Select(i => nextEntriesBase[i]).ToArray();
                                    break;
                                }
                            case SortMode.CreatedInAsc:
                                {
                                    if (nextLoader.Type != PackedImageLoader.PackType.Directory) break;
                                    var dateArray = (from e in nextEntriesBase
                                                     select Program.GetCreateTimeOfFile(currentArchiveFilePath + Path.DirectorySeparatorChar + e.Path)).ToArray();
                                    var order = new int[dateArray.Length]; for (var i = 0; i < order.Length; i++) order[i] = i;
                                    nextEntriesBase = order.
                                        OrderBy(x => dateArray[x]).
                                        Select(i => nextEntriesBase[i]).ToArray();
                                    break;
                                }
                            case SortMode.CreatedInDesc:
                                {
                                    if (nextLoader.Type != PackedImageLoader.PackType.Directory) break;
                                    var dateArray = (from e in nextEntriesBase
                                                     select Program.GetCreateTimeOfFile(currentArchiveFilePath + Path.DirectorySeparatorChar + e.Path)).ToArray();
                                    var order = new int[dateArray.Length]; for (var i = 0; i < order.Length; i++) order[i] = i;
                                    nextEntriesBase = order.
                                        OrderByDescending(x => dateArray[x]).
                                        Select(i => nextEntriesBase[i]).ToArray();
                                    break;
                                }
                            case SortMode.AccessedInAsc:
                                {
                                    if (nextLoader.Type != PackedImageLoader.PackType.Directory) break;
                                    var dateArray = (from e in nextEntriesBase
                                                     select Program.GetLastAccessTimeOfFile(currentArchiveFilePath + Path.DirectorySeparatorChar + e.Path)).ToArray();
                                    var order = new int[dateArray.Length]; for (var i = 0; i < order.Length; i++) order[i] = i;
                                    nextEntriesBase = order.
                                        OrderBy(x => dateArray[x]).
                                        Select(i => nextEntriesBase[i]).ToArray();
                                    break;
                                }
                            case SortMode.AccessedInDesc:
                                {
                                    if (nextLoader.Type != PackedImageLoader.PackType.Directory) break;
                                    var dateArray = (from e in nextEntriesBase
                                                     select Program.GetLastAccessTimeOfFile(currentArchiveFilePath + Path.DirectorySeparatorChar + e.Path)).ToArray();
                                    var order = new int[dateArray.Length]; for (var i = 0; i < order.Length; i++) order[i] = i;
                                    nextEntriesBase = order.
                                        OrderByDescending(x => dateArray[x]).
                                        Select(i => nextEntriesBase[i]).ToArray();
                                    break;
                                }
                            case SortMode.ModifiedInAsc:
                                {
                                    nextEntriesBase = nextEntriesBase.OrderBy(e => e.LastWriteTime).ToArray();
                                    break;
                                }
                            case SortMode.ModifiedInDesc:
                                {
                                    nextEntriesBase = nextEntriesBase.OrderByDescending(e => e.LastWriteTime).ToArray();
                                    break;
                                }
                            case SortMode.SizeInAsc:
                                {
                                    nextEntriesBase = nextEntriesBase.OrderBy(e => e.Size).ToArray();
                                    break;
                                }
                            case SortMode.SizeInDesc:
                                {
                                    nextEntriesBase = nextEntriesBase.OrderByDescending(e => e.Size).ToArray();
                                    break;
                                }
                            default:
                                {
                                    nextEntriesBase = nextEntriesBase.OrderBy(e => e.Path, new LogicalStringComparer()).ToArray();
                                    break;
                                }
                        }
                    }
                }
                else
                {
                    nextEntriesBase = applyFilterAndReturnItsSelf(nextLoader, nextLoader.GetPackedImageFullEntries(needToExecOnTask: true)/*, noOp: nextLoader.Type != PackedImageLoader.PackType.Directory*/)
                        .OrderBy(entry => entry.Path, new LogicalStringComparer()).ToArray();
                    currentSortModeDetails.SortMode = SortMode.NameInAsc;
                }

                //nextEntries = nextEntriesBase.Select(entry => entry.PhysicalIndex).ToArray();
                var nextEntries = nextEntriesBase;
                var currentArchiveFilePathWithSeparator = currentArchiveFilePath + Path.AltDirectorySeparatorChar;
                nextEntriesPathBase = nextEntriesBase.Select(entry => currentArchiveFilePathWithSeparator + entry.Path).ToArray();
                if (nextEntries.Length <= 0)
                {
                    nextLoader.Dispose();
                    nextLoader = null;
                    currentArchiveFilePath = null;
                    currentStartingFilePath = null;
                    mtbPage.Maximum = 0;
                    if (ipcLookAheadInfo == null)
                    {
                        if (OpenFile_UserOpen)
                        {
                            Program.ShowCursor();
                            MessageBox.Show(this, Message.NoReadableFile, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            showCurrentPage();
                            return OpenResult.ShowWarning;
                        }
                        else
                        {
                            Program.exitException = new Exception(Message.NoReadableFile);
                            CloseWithoutSave();
                            showCurrentPage();
                            return OpenResult.CloseForm;
                        }
                    }
                    showCurrentPage();
                    return OpenResult.NoOperation;
                }
            }
            catch (Exception error)
            {
                nextData?.Dispose();
                nextData = null;
                currentArchiveFilePath = null;
                currentStartingFilePath = null;
                mtbPage.Maximum = 0;
                if (ipcLookAheadInfo == null)
                {
                    Exception error2;

                    if (error is FileNotFoundException)
                    {
                        error2 = error;
                    }
                    else
                    {
#if DEBUG
                        error2 = new Exception(Message.FailedToLoadFile + "\n\n" + error.ToString());
#else
                        error2 = new Exception(Message.FailedToLoadFile);
#endif
                    }
                    if (OpenFile_UserOpen)
                    {
                        Program.ShowCursor();
#if DEBUG
                        MessageBox.Show(this, error2.ToString(), null, MessageBoxButtons.OK, MessageBoxIcon.Error);
#else
                        MessageBox.Show(this, error2.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif
                        showCurrentPage();
                        return OpenResult.ShowWarning;
                    }
                    else
                    {
                        Program.exitException = error2;
                        CloseWithoutSave();
                        return OpenResult.CloseForm;
                    }
                }
                showCurrentPage();
                return OpenResult.NoOperation;
            }

            if (startName != null) name = startName;
            if (name != null)
            {
                for (var i = 0; i < nextEntriesBase.Length; i++)
                {
                    var e = nextEntriesBase[i];
                    if (e.Path == name)
                    {
                        startPage = i;
                        break;
                    }
                }
            }
            
            if (saveToHistory)
            {
                try
                {
                    //Program.TryUpdateLastAccessTime(path); // 既にファイルを開いているのでここでは不可
                    /*
                    if (currentArchiveFilePath != null && currentArchiveFilePath != path)
                    {
                        VirtualFolder.AddBookmarkData(Program.HistorySorPath,
                            new string[] { currentArchiveFilePath, path },
                            new int[] { Math.Max(0, currentPage), Math.Max(0, startPage) }, limitOfItemsCount: Program.GetLimitOfHistoryCount(), deleteLostPath: true);
                        SendSorMessageToCatalogForm(
                            new string[] { currentArchiveFilePath, path },
                            new string[] { Program.HistorySorPath, Program.HistorySorPath },
                            new int[] { Math.Max(0, currentPage), Math.Max(0, startPage) },
                            new bool[] { false, false });
                    }
                    else
                    */
                    {
                        VirtualFolder.AddBookmarkData(Program.HistorySorPath, path, Math.Max(0, startPage), limitOfItemsCount: Program.GetLimitOfHistoryCount(), deleteLostPath: true);
                        SendSorMessageToCatalogForm(path, Program.HistorySorPath, Math.Max(0, startPage), requestToUpdateLastAccessTime: false, m: SelfLastAccessTimeUpdateMode.None);
                    }
                }
                catch { }
            }

            mtbPage.Maximum = Math.Max(0, nextEntriesBase.Length - 1);
            btnOpenLeft.Enabled = btnOpenRight.Enabled = true;
            mtbPage_ValueChanged_SendRequestToChangeSelection_Stop = true;
            if (startPage < 0 || startPage >= nextEntriesBase.Length)
            {
                // この順で実行しないと無駄な再描画が行われてしまう
                currentPage = 0;
                mtbPage.Value = 0;
            }
            else
            {
                currentPage = startPage;
                mtbPage.Value = startPage;
            }
            mtbPage_ValueChanged_SendRequestToChangeSelection_Stop = false;
            //ShowCurrentPageToStatusBar();

            // seemToBeBook は廃止
            /*
            if (requestedCoverBindingMode == null)
            {
                if (currentPage == 0 && seemToBeBook(nextLoader.Type, nextEntriesBase))
                {
                    onePageModeForNext = true;
                }
            }
            else
            */
            {
                onePageModeForNext = currentPage == 0 && CoverBindingMode/*requestedCoverBindingMode*/ == CoverBindingMode.ForceSingle;
            }
            CurrentDividedPosition = 0;

            //bmwLoadEachPage.RunWorkerAsyncWithInterrupt(nextEntries.Length, true); // キャンセルを待たない場合 OriginalImageArray を毎回破棄するようにする必要あり


            // bmwLoadEachPage が動き出す前に Paint が動いて古い ResizedImage が使われることの無いように
            clearResizedImageArray();

            // リロードでは不要、オープンでのみ使用
            IfTouchModifyingThenMoveOrigin(inverse: false);

            // currentArchiveFilePath が変更されてもいいようにデータを埋め込んでおく
            var arguments = new object[nextEntriesBase.Length];
            arguments[0] = Tuple.Create(currentArchiveFilePath, currentStartingFilePath, false, false); // loader が変化するので updateLoader はどちらでも同じ
            //OpenFileFinishedButStartingIsNotCalled = true;
            nextData = new NextData(nextEntriesPathBase, nextEntriesBase, nextLoaderBase);
            bmwLoadEachPage.RunWorkerAsyncWithInterrupt(arguments, true); // キャンセルを待たない場合 OriginalImageArray を毎回破棄するようにする必要あり

            if (ipcLookAheadInfo == null) OpenFile_UserOpen = true;

            return OpenResult.Success;
        }

        private static PackedImageLoader.PackedImageEntry[] getNameSortedEntries(List<PackedImageLoader.PackedImageEntry> list, PackedImageLoader.PackType type, bool desc)
        {
            var replaced = PackedImageLoader.GetSeparatorReplacedArray(list, type);

            if (desc)
            {
                return Enumerable.Range(0, list.Count).OrderByDescending(i => replaced[i], CatalogForm.GetSortArray_NaturalSort).Select(i => list[i]).ToArray();
            }
            else
            {
                return Enumerable.Range(0, list.Count).OrderBy(i => replaced[i], CatalogForm.GetSortArray_NaturalSort).Select(i => list[i]).ToArray();
            }
        }
        private static PackedImageLoader.PackedImageEntry[] getNameSortedEntries(PackedImageLoader.PackedImageEntry[] list, PackedImageLoader.PackType type, bool desc)
        {
            var replaced = PackedImageLoader.GetSeparatorReplacedArray(list, type);

            if (desc)
            {
                return Enumerable.Range(0, list.Length).OrderByDescending(i => replaced[i], CatalogForm.GetSortArray_NaturalSort).Select(i => list[i]).ToArray();
            }
            else
            {
                return Enumerable.Range(0, list.Length).OrderBy(i => replaced[i], CatalogForm.GetSortArray_NaturalSort).Select(i => list[i]).ToArray();
            }
        }

        private List<PackedImageLoader.PackedImageEntry> applyFilterAndReturnItsSelf(PackedImageLoader nextLoader, List<PackedImageLoader.PackedImageEntry> list/*, bool noOp*/)
        {
            if (searchManagerFromCatalogForm == null) return list;
            var noOp = nextLoader.Type != PackedImageLoader.PackType.Directory || currentArchiveFilePath == currentStartingFilePath;
            //if (noOp || searchManagerFromCatalogForm == null) return list;
            if (noOp) return list;
            else return list.Where(e => searchManagerFromCatalogForm.Match(e.Path)).ToList();
        }

        private void CloseWithoutSave()
        {
            DoNotSaveSetting = true;
            Close();
        }

        enum ViewerFormOnMemoryMode { Default, ForLookAhead, ForCheckOnlyEntries }
        private PackedImageLoader getPackedImageLoader(string path, ViewerFormOnMemoryMode mode)
        {
            var onMemory =
                ReadOnMemoryMode == ReadOnMemoryMode.None || mode == ViewerFormOnMemoryMode.ForCheckOnlyEntries ? PackedImageLoaderOnMemoryMode.None :
                ReadOnMemoryMode == ReadOnMemoryMode.Always || mode == ViewerFormOnMemoryMode.Default ? PackedImageLoaderOnMemoryMode.OnMemory :
                PackedImageLoaderOnMemoryMode.Releasable;

            return Program.GetPackedImageLoader(path, ArchivesInArchiveMode, animation: true, onMemory: onMemory);
        }

        /*
        private static readonly Regex seemToBeBook_IsNumber = new Regex(@"^\d$", RegexOptions.Compiled);
        private static bool seemToBeBook(PackedImageLoader.PackType type, PackedImageLoader.PackedImageEntry[] entries)
        {
            try
            {
                if (entries.Length < 2) return false;

                var testCount = Math.Min(100, entries.Length - 1);
                var testLength = testCount + 1;
                var matchCount = 0;
                for (var i = 1; i < testLength; i++)
                {
                    var a = entries[i - 1].Path;
                    var b = entries[i].Path;
                    var aIndex = 0;
                    var bIndex = 0;
                    var match = true;
                    while (aIndex < a.Length && bIndex < b.Length)
                    {
                        var aj = a[aIndex++];
                        var bj = b[bIndex++];
                        if (aj != bj) // 漢数字なども数値とみなされる
                        {
                            if (char.IsNumber(aj) && char.IsNumber(bj))
                            {
                                while (aIndex < a.Length && char.IsNumber(a[aIndex])) aIndex++;
                                while (bIndex < b.Length && char.IsNumber(b[bIndex])) bIndex++;
                            }
                            else
                            {
                                match = false;
                                break;
                            }
                        }
                    }
                    if (aIndex == a.Length && bIndex == b.Length && match) matchCount++;
                }

                // イレギュラー3つまでは許容する
                if (testCount <= 3) return matchCount >= 1;

                return 10 * matchCount > 8 * (testCount - 3);
            }
            catch
            {
                return false;
            }
        }
        */

        private void Reload(bool force = false, bool updateOriginal = false)
        {
            if (force || ViewSize != (FullScreen_pseudoFullScreen ? Screen.FromControl(this).Bounds.Size : PseudoMaximized ? PseudoMaximizedSize : pbView.Size))
            {
                if (string.IsNullOrEmpty(currentArchiveFilePath)) return;
                //nextLoader = loader;
                //bmwLoadEachPage.RunWorkerAsyncWithInterrupt(EntryNumberArray.Length, true); // OriginalImageArray の問題があるからキャンセルを待たないわけにはいかない

                // bmwLoadEachPage が動き出す前に Paint が動いて古い ResizedImage が使われることの無いように
                clearResizedImageArray();

                // currentArchiveFilePath が変更されてもいいようにデータを埋め込んでおく
                var arguments = new object[EntryArray.Length];
                arguments[0] = Tuple.Create(currentArchiveFilePath, currentStartingFilePath, true, updateOriginal);
                bmwLoadEachPage.RunWorkerAsyncWithInterrupt(arguments, true); // キャンセルを待たない場合 OriginalImageArray を毎回破棄するようにする必要あり

                //pbPaintInvalidate();
            }
        }

        private void clearResizedImageArray()
        {
            if (ResizedImageArray != null)
            {
                foreach (var img in ResizedImageArray)
                {
                    if (img != null)
                    {
                        img.Dispose();
                    }
                }
                ResizedImageArray = null;
            }
            ResizedSizeArray = null;
            usedMemory = 0;
        }

        private void bmwLoadEachPage_RunWorkerStarting(object sender, GenerarClasses.BackgroundMultiWorker.RunWorkerStartingEventArgs e)
        {
            //OpenFileFinishedButStartingIsNotCalled = false;

            var x = e.Arguments as object[];
            var xx = x[0] as Tuple<string, string, bool, bool>;
            currentArchiveFilePath = xx.Item1;
            currentStartingFilePath = xx.Item2;
            var reload = xx.Item3;
            //var binding = xx.Item4;
            var updateOriginal = xx.Item4;
            x[0] = null;
            clearResizedImageArray();

            bool updatedLoader;
            if (nextData != null)
            {
                var next = nextData;
                updatedLoader = loader != next.Loader;
                if (updatedLoader)
                {
                    loader?.Dispose();
                    if (TryUpdateLastAccessTimeAfterLoaderDisposed != null)
                    {
                        Program.TryUpdateLastAccessTime(TryUpdateLastAccessTimeAfterLoaderDisposed);
                    }
                    loader = next.Loader;
                }
                EntryArray = next.Entries;
                EntryLongPathArray = next.EntriesPaths;
            }
            else
            {
                updatedLoader = loader != null;
            }
            TryUpdateLastAccessTimeAfterLoaderDisposed = null;
            nextData = null;

            if (updatedLoader || updateOriginal)
            {
                if (PreFilteredImageArray != null)
                {
                    for (var i = 0; i < PreFilteredImageArray.Length; i++)
                    {
                        var img = PreFilteredImageArray[i];
                        if (img != null)
                        {
                            lock (img)
                            {
                                img.Dispose();
                                PreFilteredImageArray[i] = null; // lock 内で null に
                            }
                        }
                    }
                    PreFilteredImageArray = null;
                }
                //PreFilteredImageArray = new Bitmap[EntryNumberArray.Length];
                //OriginalImageInfoArray = new ImageInfo[EntryNumberArray.Length];
                PreFilteredImageArray = new BitmapEx[EntryArray.Length];
                OriginalImageInfoArray = new ViewerFormImageInfo[EntryArray.Length];
            }
            else
            {
                if (PreFilteredImageArray == null)
                {
                    //PreFilteredImageArray = new Bitmap[EntryNumberArray.Length];
                    //OriginalImageInfoArray = new ImageInfo[EntryNumberArray.Length];
                    PreFilteredImageArray = new BitmapEx[EntryArray.Length];
                    OriginalImageInfoArray = new ViewerFormImageInfo[EntryArray.Length];
                }
            }
            //ResizedImageArray = new Bitmap[EntryNumberArray.Length];
            //ResizedSizeArray = new Size[EntryNumberArray.Length];
            ResizedImageArray = new VirtualBitmapEx[EntryArray.Length];
            ResizedSizeArray = new Size[EntryArray.Length];
            ViewSize = FullScreen_pseudoFullScreen ? Screen.FromControl(this).Bounds.Size : PseudoMaximized ? PseudoMaximizedSize : ViewerForm_SizeChanged_pbBoundsMemoryForMinimize.Size;
            SetBackgroundMode();
            bmwLoadEachPage_EachRunWorkerCompleted_PrevPage = currentPage; // 無駄な優先順位変更を防止

            if (updatedLoader)
            {
                setTitleBar();
            }

            if (ipcLookAheadInfo == null) SetCurrentImageLongPath(); // ページ番号の描画よりも後で呼び出す

            Magnifier_ZoomedPage?.Dispose();
            Magnifier_ZoomedPage = null;
            Magnifier_currentPageReady = -1;

            if (!reload)
            {
                /*
                var zipPlaInfo = new ZipPlaInfo(currentArchiveFilePath);
                if (zipPlaInfo.PageLeftToRight == null)
                {
                    PageLeftToRight = SettingForm.LeftToRight;
                }
                else
                {
                    PageLeftToRight = (bool)zipPlaInfo.PageLeftToRight;
                }
                */

                // 常にリクエストに従う方法
                var bindingFromPath = ZipPlaInfo.GetOnlyBindingModeFromFullName(currentArchiveFilePath);
                if (bindingFromPath == null)
                {
                    var binding = requestedBindingMode;
                    BindingMode = binding ?? Program.GetBindingModeFromCulture(Message.CurrentLanguage);
                }
                else
                {
                    BindingMode = (BindingMode)bindingFromPath;
                }

                // 名前昇順以外は RightToLeft を禁止する方法 
                /*
                // 画像ファイル指定で開かれた場合 NameInAsc でも leftToRight を強制するという選択肢も考えられるが、
                // ファイル指定から書籍を読むという使い方も多いと考えられるため却下
                if (currentSortModeDetails == null || currentSortModeDetails.SortMode == SortMode.NameInAsc)
                {
                    var bindingFromPath = ZipPlaInfo.GetOnlyBindingModeFromFullName(currentArchiveFilePath);
                    if (bindingFromPath == null)
                    {
                        BindingMode = binding == null ? Program.GetBindingModeFromCulture(Message.CurrentLanguage) : (BindingMode)binding;
                    }
                    else
                    {
                        BindingMode = (BindingMode)bindingFromPath;
                    }
                }
                else
                {
                    BindingMode = BindingMode.LeftToRight;
                }
                */
            }
        }

        private void setTitleBar()
        {
            string text;
            try
            {
                if (string.IsNullOrEmpty(currentArchiveFilePath))
                {
                    text = Program.Name;
                }
                else
                {
                    var slideshow = getSlideshowEnabled() ? $" <{Message.Slideshow}>" : "";
                    //var filter = requestedFilterString == null ? "" : $" [{requestedFilterString}]";
                    var archiveFileName = Path.GetFileName(currentArchiveFilePath);
                    if (Directory.Exists(currentArchiveFilePath))
                    {
                        text = $"{archiveFileName + Path.DirectorySeparatorChar}{slideshow} - {Program.Name}";
                    }
                    else
                    {
                        text = $"{archiveFileName}{slideshow} - {Program.Name}";
                    }
                }
            }
            catch
            {
                text = Program.Name;
            }
            Text = text;
        }

        private void removeHiddenConfigFromCatalogForm()
        {
            requestedFilterString = null;
            requestedFilterStringWithoutAlias = null;
            searchManagerFromCatalogForm = null;

            requestedSortModeDetails = null;
            currentSortModeDetails = null;
        }

        private bool SetCurrentImageLongPath_Que = false;
        private void SetCurrentImageLongPath()
        {
            if (string.IsNullOrEmpty(currentArchiveFilePath))
            {
                return;
            }
            try
            {
#region １つめを表示
                //currentImageLongPathToolStripTextBox.Text = EntryLongPathArray[mtbPage.Value];
                //Program.TextBoxShowRight(currentImageLongPathToolStripTextBox);
#endregion

#region 全て表示
                var cPage = currentPage;
                var nextPage = NextPage;
                var pageCount = nextPage - currentPage;
                var pageDivision = 1;
                var cdPosition = CurrentDividedPosition;
                if (nextPage <= cPage)
                {
                    if (nextPage < cPage || MaxDivision <= 1 || cdPosition < 0 || (pageDivision = getPageDivison(cPage)) <= 1 || cdPosition >= pageDivision)
                    {
                        SetCurrentImageLongPath_Que = true;
                        return;
                    }
                    nextPage++;
                }
                SetCurrentImageLongPath_Que = false;
                int start, stop, delta;
                if (BindingMode == BindingMode.LeftToRight)
                {
                    start = cPage;
                    stop = nextPage;
                    delta = 1;
                }
                else
                {
                    start = nextPage - 1;
                    stop = cPage - 1;
                    delta = -1;
                }
                var isEntity = loader?.Type == PackedImageLoader.PackType.Directory;
                var baseText = isEntity ? EntryLongPathArray[start]?.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar) : EntryLongPathArray[start];
                var text = baseText;
                for (var i = start + delta; i != stop; i += delta)
                {
                    text += " | " + GetDifferentSuffix(baseText, isEntity ? EntryLongPathArray[i]?.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar) : EntryLongPathArray[i]);
                }

                // 現在の分割位置を追加する場合、分割内の移動が起こったときにこのメソッドが呼び出されるようにすること

                if (currentImageLongPathToolStripTextBox.Text != text)
                {
                    currentImageLongPathToolStripTextBox.Text = text;
                    Program.TextBoxShowRight(currentImageLongPathToolStripTextBox);
                }
#endregion
            }
            catch { }
        }

        private static readonly System.Text.RegularExpressions.Regex GetDifferentSuffix_Regex = new System.Text.RegularExpressions.Regex(@"^.\b.$", System.Text.RegularExpressions.RegexOptions.Compiled);
        private static string GetDifferentSuffix(string reference, string target)
        {
            if (reference == null || target == null) return target;
            var stop = Math.Min(reference.Length, target.Length) - 1;
            var i = 0;
            while (i < stop && reference[i] == target[i]) i++;
            do { i--; } while (i >= 0 && !GetDifferentSuffix_Regex.IsMatch(target.Substring(i, 2)));
            return target.Substring(i + 1);
        }

        private int bmwLoadEachPage_DoWork_DEBUG_WorkNumber = -1;
        private int bmwLoadEachPage_DoWork_DEBUG_Phase;

        private void bmwLoadEachPage_DoWork(object sender, GenerarClasses.BackgroundMultiWorker.EachDoWorkEventArgs e)
        {
            var rsa = ResizedSizeArray;
            if (rsa == null || rsa.Length <= e.WorkNumber)
            {
                return;
            }

            System.Diagnostics.Stopwatch sw = null;
            bmwLoadEachPage_DoWork_DEBUG_WorkNumber = e.WorkNumber;
            bmwLoadEachPage_DoWork_DEBUG_Phase = 0;
            // 読み込みが遅いファイルの主要項は排他処理しないといけない loader.OpenImageStream なのでマルチスレッドは有効ではない。
            // リソースの節約を度外視して高速化を目指すならスレッド分ストリームのコピーをメモリに展開する方法が想定される。
            try
            {
                LockManager.ForIReadOnlyList(PreFilteredImageArray, e.WorkNumber, item =>
                {
                    bmwLoadEachPage_DoWork_DEBUG_Phase = 1;
                    BitmapEx bmp;
                    ViewerFormImageInfo imageInfo;
                    if (item == null)
                    {
                        bmwLoadEachPage_DoWork_DEBUG_Phase = 2;
                        //var sw = new System.Diagnostics.Stopwatch(); sw.Start();
                        //var img = loader.OpenImageStream(EntryNumberArray[e.WorkNumber]);
                        var img = BitmapEx.ConvertToBitmapEx(loader.OpenImageStream(EntryArray[e.WorkNumber]));
                        ImageInfo imageInfo0;
                        int? maxColorDiff = 0;
                        try
                        {
                            //var originalRotate = ViewerFormImageFilter.GetOrientation(img);
                            bmwLoadEachPage_DoWork_DEBUG_Phase = 3;
                            //sw.Stop(); MessageBox.Show($"{sw.Elapsed}");
                            /*
                            try
                            {
                                img = ToBitmap24_Standard(img, PixelFormat.Format32bppRgb);
                            }
                            catch
                            {
                                throw;
                            }*/

                            //Thread.Sleep(500);



                            //var isColorImage = imageFilter.NeedToCheckWhetherColorOrNot() && ViewerMode != ViewerModeEnum.CoverSetting ?
                            //    ViewerFormImageFilter.TryGetWhetherColorOrNot(img) : null;

                            /*
                            var maxColorDiff = ViewerFormImageFilter.TryGetMaxColorDiff(img);
                            if (maxColorDiff == null && IsGrayScaleBitmap(img))
                            {
                                maxColorDiff = 0;
                            }
                            */

                            // アニメーションの遅延読み込み実装前
                            /*
                            foreach (var bitmap in img.Bitmaps)
                            {
                                var mcd = ViewerFormImageFilter.TryGetMaxColorDiff(bitmap);
                                if (mcd == null)
                                {
                                    maxColorDiff = null;
                                    break;
                                }
                                else if (mcd > maxColorDiff)
                                {
                                    maxColorDiff = mcd;
                                }
                            }
                            if (maxColorDiff == null)
                            {
                                if (img.Bitmaps.All(bitmap => IsGrayScaleBitmap(bitmap)))
                                {
                                    maxColorDiff = 0;
                                }
                                else if (img.FrameCount > 1)
                                {
                                    maxColorDiff = 255; // アニメーションの場合 null は許さない
                                }
                            }
                            */

                            // アニメーションの遅延読み込み実装後
                            maxColorDiff = ViewerFormImageFilter.TryGetMaxColorDiff(img); // TryGet は全てのフレームに大して同じ効果
                            if (maxColorDiff == null)
                            {
                                if (img.FrameCount > 1)
                                {
                                    // アニメーションの場合 null は許さない
                                    // try で彩度が確定しなければカラーで決め打ち
                                    maxColorDiff = 255;
                                }
                                else if (IsGrayScaleBitmap(img))
                                {
                                    maxColorDiff = 0;
                                }
                            }

                            imageInfo0 = ImageLoader.GetImageInfo(img); // img がピクセルフォーマット変換で Dispose される前に行う

                            //var sw = new System.Diagnostics.Stopwatch();
                            //sw.Start();
                            //img = ToBitmap24_Standard(img as Bitmap, PixelFormat.Format64bppPArgb);
                            if (maxColorDiff == 0)
                            {
                                //if (img.PixelFormat != PixelFormat.Format8bppIndexed) // 遅延読み込みのためできるだけ ApplyToAll は使いたくない
                                //  → ApplayToAll も遅延化したので問題ない＋白黒でもインデックスと画素値が対応しない画像がごくまれに存在する
                                {
                                    img.ApplyToAll(ToBitmap8Unsafe);
                                }
                            }
                            else
                            {
                                if (img.PixelFormat != PixelFormat.Format24bppRgb) // 遅延読み込みのためできるだけ ApplyToAll は使いたくない
                                {
                                    img.ApplyToAll(ToBitmap24Unsafe);
                                }
                            }
                            //bmp = ToBitmap24_Standard(img as Bitmap, PixelFormat.Format24bppRgb);
                            // bmp = ToBitmap24_ExStandard(img as Bitmap, PixelFormat.Format24bppRgb);
                            //sw.Stop(); MessageBox.Show($"{sw.Elapsed}");

                            if (ViewerMode != ViewerModeEnum.CoverSetting && imageFilter.PreFilterExists())
                            {
                                img.ApplyToAll(imageFilter.PreFilter);
                            }
                        }
                        catch
                        {
                            img.Dispose();
                            throw;
                        }
                        bmp = img;

                        PreFilteredImageArray[e.WorkNumber] = bmp;
                        imageInfo = new ViewerFormImageInfo(imageInfo0, maxColorDiff);
                        OriginalImageInfoArray[e.WorkNumber] = imageInfo;
                    }
                    else
                    {
                        bmwLoadEachPage_DoWork_DEBUG_Phase = 4;
                        bmp = item;
                        if (ViewerMode != ViewerModeEnum.CoverSetting)
                        {
                            try
                            {
                                imageInfo = OriginalImageInfoArray[e.WorkNumber];
                            }
                            catch
                            {
                                imageInfo = null;
                            }
                        }
                        else imageInfo = null;
                    }
                    //var zoom = Math.Min(ViewSize.Width / (double)img.Width, ViewSize.Height / (double)img.Height);
                    Action body = () =>
                    {
                        // img が Dispose された場合エラーになるが想定内

                        bmwLoadEachPage_DoWork_DEBUG_Phase = 5;
                        var resizedSize = GetResizedSize(bmp.Size);
                        //var result = new Bitmap((int)Math.Round(zoom * img.Width), (int)Math.Round(zoom * img.Height));

                        rsa[e.WorkNumber] = resizedSize;

                        // DrawImage が Src と Dst のサイズが一致する場合としない場合で異なる挙動をする
                        //var outWidth = rect.Width == bmp.Width ? rect.Width : bmp.Width - 1;
                        //var outHeight = rect.Height == bmp.Height ? rect.Height : bmp.Height - 1;

                        bmwLoadEachPage_DoWork_DEBUG_Phase = 6;
                        VirtualBitmapEx result;
                        if (bmp.FrameCount == 1/* || BitmapEx.GetDataSizeInBytes(bmp, resizedSize) < GetUserMemoryUBound() / 2*/)
                        {
                            BitmapEx entity;
                            // リサイズが変更も不要なら Graphics は使わない
                            if (resizedSize.Width == bmp.Width && resizedSize.Height == bmp.Height)
                            {
                                var bench = standardScalingAlgorithm_BenchRequest == e.WorkNumber && !bmwLoadEachPage.CancellationPending;
                                if (bench)
                                {
                                    standardScalingAlgorithm_BenchRequest = -1;

                                    var message = "Scaling is no excuted.";
                                    Invoke(((MethodInvoker)(() =>
                                    {
                                        MessageBox.Show(this, message,
                                            "Measurement of computation time", MessageBoxButtons.OK, MessageBoxIcon.Information);

                                    })));
                                }

                                var rect = new Rectangle(0, 0, resizedSize.Width, resizedSize.Height);

                                //result = Clone(bmp);
                                entity = bmp.CreateNew(Clone);
                            }
                            else
                            {
                                bmwLoadEachPage_DoWork_DEBUG_Phase = 10;


                                /*
                                result = new Bitmap(resizedSize.Width, resizedSize.Height, PixelFormat.Format24bppRgb);
                                var rect = new Rectangle(0, 0, resizedSize.Width, resizedSize.Height);
                                using (var g = Graphics.FromImage(result))
                                {
                                    if ((bmp.PixelFormat & PixelFormat.Alpha) != 0)
                                    {
                                        g.FillRectangle(Brushes.White, rect);
                                    }
                                    g.DrawImage(bmp, rect, new RectangleF(0, 0, bmp.Width, bmp.Height), GraphicsUnit.Pixel);
                                    // 等しくなければ、これで厳密にピクセルが一致することを 2x3 の市松模様画像で確認
                                }
                                */

                                //var sw = new System.Diagnostics.Stopwatch();  sw.Start();

                                //result = BitmapResizer.CreateNew(bmp, resizedSize); // 0.017
                                //result = MathematicalImageResizer.NearestNeighbor(bmp, resizedSize); // 0.0026
                                //result = MathematicalImageResizer.Lanczos3(bmp, resizedSize); // 0.027 
                                //result = MathematicalImageResizer.Lanczos4(bmp, resizedSize); // 0.035 
                                //result = MathematicalImageResizer.Lanczos2(bmp, resizedSize); // 0.018
                                //result = MathematicalImageResizer.AreaAverage(bmp, resizedSize); // 0.018
                                ScalingAlgorithm scalingAlgorithm;
                                if ((long)resizedSize.Width * resizedSize.Height > (long)bmp.Width * bmp.Height)
                                {
                                    scalingAlgorithm = StandardScalingAlgorithm.ScaleUp;
                                }
                                else
                                {
                                    scalingAlgorithm = StandardScalingAlgorithm.ScaleDown;
                                }

                                var gamma = ResizeGammaNormal;

                                var bench = standardScalingAlgorithm_BenchRequest == e.WorkNumber && !bmwLoadEachPage.CancellationPending;
                                if (bench)
                                {
                                    standardScalingAlgorithm_BenchRequest = -1;
                                    BitmapResizer.CreateNew(bmp, resizedSize).Dispose(); // キャッシュ関係での不公平が生じないように
                                    sw = new System.Diagnostics.Stopwatch();
                                    sw.Start();
                                }

                                const int pixelsPerPixelToBeLeft = int.MaxValue;
                                const bool parallel = true;
                                switch (scalingAlgorithm)
                                {
                                    case ScalingAlgorithm.HighSpeed: entity = bmp.CreateNew(bmpi => QuickGraphic.CreateNew(bmpi, resizedSize, parallel: parallel)); break;
                                    case ScalingAlgorithm.NearestNeighbor: entity = bmp.CreateNew(bmpi => LongVectorImageResizer.NearestNeighbor(bmpi, resizedSize, parallel)); break;
                                    case ScalingAlgorithm.AreaAverage: entity = bmp.CreateNew(bmpi => LongVectorImageResizer.AreaAverage(bmpi, pixelsPerPixelToBeLeft, gamma, resizedSize, parallel)); break;
                                    case ScalingAlgorithm.Lanczos1: entity = bmp.CreateNew(bmpi => LongVectorImageResizer.Lanczos(1, bmpi, pixelsPerPixelToBeLeft, gamma, resizedSize, parallel)); break;
                                    case ScalingAlgorithm.Lanczos2: entity = bmp.CreateNew(bmpi => LongVectorImageResizer.Lanczos(2, bmpi, pixelsPerPixelToBeLeft, gamma, resizedSize, parallel)); break;
                                    case ScalingAlgorithm.Lanczos3: entity = bmp.CreateNew(bmpi => LongVectorImageResizer.Lanczos(3, bmpi, pixelsPerPixelToBeLeft, gamma, resizedSize, parallel)); break;
                                    case ScalingAlgorithm.Lanczos4: entity = bmp.CreateNew(bmpi => LongVectorImageResizer.Lanczos(4, bmpi, pixelsPerPixelToBeLeft, gamma, resizedSize, parallel)); break;
                                    case ScalingAlgorithm.Spline4: entity = bmp.CreateNew(bmpi => LongVectorImageResizer.Spline4(bmpi, pixelsPerPixelToBeLeft, gamma, resizedSize, parallel)); break;
                                    case ScalingAlgorithm.Spline16: entity = bmp.CreateNew(bmpi => LongVectorImageResizer.Spline16(bmpi, pixelsPerPixelToBeLeft, gamma, resizedSize, parallel)); break;
                                    case ScalingAlgorithm.Spline36: entity = bmp.CreateNew(bmpi => LongVectorImageResizer.Spline36(bmpi, pixelsPerPixelToBeLeft, gamma, resizedSize, parallel)); break;
                                    case ScalingAlgorithm.Spline64: entity = bmp.CreateNew(bmpi => LongVectorImageResizer.Spline64(bmpi, pixelsPerPixelToBeLeft, gamma, resizedSize, parallel)); break;
                                    /*
#if !AUTOBUILD
                                    case ScalingAlgorithm.AntiMoire1: entity = bmp.CreateNew(bmpi => LongVectorImageResizer.FewAliasingLanczos(3, bmpi, pixelsPerPixelToBeLeft, gamma, resizedSize, parallel)); break;
                                    case ScalingAlgorithm.AntiMoire2: entity = bmp.CreateNew(bmpi => LongVectorImageResizer.FewAliasingLanczos(4, bmpi, pixelsPerPixelToBeLeft, gamma, resizedSize, parallel)); break;
                                    case ScalingAlgorithm.AntiMoire3: entity = bmp.CreateNew(bmpi => LongVectorImageResizer.FewAliasingLanczos(5, bmpi, pixelsPerPixelToBeLeft, gamma, resizedSize, parallel)); break;
                                    case ScalingAlgorithm.AntiMoire4: entity = bmp.CreateNew(bmpi => LongVectorImageResizer.FewAliasingLanczos(6, bmpi, pixelsPerPixelToBeLeft, gamma, resizedSize, parallel)); break;
#endif
                                    */
                                    default: entity = bmp.CreateNew(bmpi => BitmapResizer.CreateNew(bmpi, resizedSize)); break;
                                }

                                if (bench)
                                {
                                    sw.Stop();
                                    var time = sw.Elapsed;
                                    TimeSpan time2;
                                    if (scalingAlgorithm != ScalingAlgorithm.Default)
                                    {
                                        sw.Restart();
                                        using (var temp = bmp.CreateNew(bmpi => BitmapResizer.CreateNew(bmpi, resizedSize)))
                                        {
                                            sw.Stop();
                                            time2 = sw.Elapsed;
                                        }
                                    }
                                    else time2 = time;
                                    string algorithm;
                                    switch (scalingAlgorithm)
                                    {
                                        /*
#if !AUTOBUILD
                                        case ScalingAlgorithm.AntiMoire1: algorithm = "Anti-moire 3/4"; break;
                                        case ScalingAlgorithm.AntiMoire2: algorithm = "Anti-moire 4/5"; break;
                                        case ScalingAlgorithm.AntiMoire3: algorithm = "Anti-moire 5/6"; break;
                                        case ScalingAlgorithm.AntiMoire4: algorithm = "Anti-moire 6/7"; break;
#endif
                                        */
                                        default: algorithm = scalingAlgorithm.ToString(); break;
                                    }
                                    var linearize = isIndividuallyImplemented(scalingAlgorithm) ? "" : gamma == GammaConversion.Value1_0 ? "no linearized / " : "linearize γ = 2.2 → 1.0 / ";
                                    var message =
                                       algorithm + (bmp.PixelFormat == PixelFormat.Format24bppRgb ? $" ({linearize}Color)\n" : $" ({linearize}Grayscale)\n") +
                                        $"source:\n\t{bmp.Size}\n" +
                                        $"destination:\n\t{resizedSize}\n" +
                                        $"computation time:\n\t{time}\n" +
                                        // $"the ratio to \"System.Windows.Media.ScaleTransform\":\n\t{(double)100 * time.Ticks / time2.Ticks:F2} %";
                                        $"the ratio to \"BitmapScalingMode.Unspecified (Linear)\":\n\t{(double)100 * time.Ticks / time2.Ticks:F2} %";
                                    //$"the ratio to \"{BitmapResizer.LastBitmapScalingMode}\":\n\t{(double)100 * time.Ticks / time2.Ticks:F2} %";
                                    Invoke(((MethodInvoker)(() =>
                                    {
                                        var res = MessageBox.Show(this, message + "\n\nAre you sure you want to copy above data to clipboard?",
                                            "Measurement of computation time", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                                        if (res == DialogResult.Yes)
                                        {
                                            Clipboard.SetText(message);
                                        }
                                    })));
                                }
                            }

                            if (ViewerMode != ViewerModeEnum.CoverSetting)
                            {
                                if (entity.FrameCount == 1)
                                {
                                    if (imageInfo != null)
                                    {
                                        imageFilter.PostFilter(entity, ref imageInfo.MaxColorDiff);
                                    }
                                    else
                                    {
                                        int? dummy = null;
                                        imageFilter.PostFilter(entity, ref dummy);
                                    }
                                }
                                else
                                {
                                    int? dummy = imageInfo?.MaxColorDiff ?? 255;
                                    entity.ApplyToAll(img => { imageFilter.PostFilter(img, ref dummy); return img; });
                                }
                            }

                            bmwLoadEachPage_DoWork_DEBUG_Phase = 11;

                            //ピクセルオフセット確認用
                            //QuickGraphic.DrawImage(result, rect, img, null);

                            result = new VirtualBitmapEx(entity, bmp.GetDataSizeInBytes());
                        }
                        else
                        {
                            var bench = standardScalingAlgorithm_BenchRequest == e.WorkNumber && !bmwLoadEachPage.CancellationPending;
                            if (bench)
                            {
                                standardScalingAlgorithm_BenchRequest = -1;
                            }

                            result = GetVirtualBitmapExForStandard(bmp, resizedSize);

                            if (bench)
                            {
                                var message = "This function does not work for animation.";
                                //$"the ratio to \"{BitmapResizer.LastBitmapScalingMode}\":\n\t{(double)100 * time.Ticks / time2.Ticks:F2} %";
                                Invoke(((MethodInvoker)(() =>
                                {
                                    MessageBox.Show(this, message, "Measurement of computation time", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                })));
                            }
                        }
                        
                        e.Result = result;// new Bitmap(img, (int)Math.Round(zoom * img.Width), (int)Math.Round(zoom * img.Height));
                    };
                    if (item == bmp)
                    {
                        body();
                    }
                    else
                    {
                        lock (bmp)
                        {
                            body();
                        }
                    }
                });
            }
#if DEBUG
            catch (Exception ee)
#else
            catch
#endif
            {
#if DEBUG
                var errorMessage = ee.ToString();
#endif
                var errorSize = new Size(-210, -297);
                var resizedSize = GetResizedSize(errorSize);
                rsa[e.WorkNumber] = resizedSize;
                //e.Result = BitmapEx.ConvertToBitmapEx(Program.GetErrorImage(resizedSize.Width, resizedSize.Height));
                e.Result = VirtualBitmapEx.GetErrorImage(resizedSize);
            }
            finally
            {
                sw?.Stop();
                bmwLoadEachPage_DoWork_DEBUG_WorkNumber = -1;
            }

            /*
#if DEBUG
            catch// (Exception er)
            {
                //Program.AlertError(error);
                //MessageBox.Show($"{er}");
                e.Result = Program.GetErrorImage(Math.Max(1, (int)Math.Round(ViewSize.Height / Math.Sqrt(2))), ViewSize.Height);
            }
#else
            catch// (Exception er)
            {
                //MessageBox.Show($"{er}");
                e.Result = Program.GetErrorImage(Math.Max(1, (int)Math.Round(ViewSize.Height / Math.Sqrt(2))), ViewSize.Height);
            }
#endif
*/
        }


        // 入力のメモリをそのまま使えば高速だがマネージドではなくなる
        // 今の形では決して速くない
        /*
        private Bitmap ImageToBitmap24_2(Image img)
        {
            var bmp = img as Bitmap;
            if (bmp != null)
            {
                if (bmp.PixelFormat == PixelFormat.Format24bppRgb)
                {
                    return bmp;
                }
                else
                {
                    var result = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format24bppRgb);
                    var data = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    var resultData = result.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                    var buff = new byte[data.Height * data.Stride];
                    System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buff, 0, buff.Length);
                    System.Runtime.InteropServices.Marshal.Copy(buff, 0, resultData.Scan0, buff.Length);
                    result.UnlockBits(resultData);
                    bmp.UnlockBits(data);
                    bmp.Dispose();

                    //var result = new Bitmap(bmp.Width, bmp.Height, data.Stride, data.PixelFormat, data.Scan0);
                    //bmp.UnlockBits(data);
                    //bmp.Dispose();
                    return result;
                }
            }
            else
            {
                bmp = new Bitmap(img);
                img.Dispose();
                return ImageToBitmap24_2(bmp);
            }
        }
        */


        private static Bitmap Clone(Bitmap bmp)
        {
            //return bmp.Clone() as Bitmap; // 極めて高速だが Susie plugin 由来のデータなどをこれで複製するとその後 lockBits 等ができなくなる
            //return bmp.Clone(new Rectangle(0, 0, bmp.Width, bmp.Height), bmp.PixelFormat); // こちらも同様

            // 安全かつ高速（ifwebp-x64.spi で動作確認）
            Bitmap result = null;
            try
            {
                var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
                try
                {
                    result = new Bitmap(data.Width, data.Height, data.Stride, data.PixelFormat, data.Scan0);
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
                if ((bmp.PixelFormat & PixelFormat.Indexed) == PixelFormat.Indexed)
                {
                    result.Palette = bmp.Palette;
                }
                return result;
            }
            catch
            {
                result?.Dispose();
                throw;
            }
        }

#if DEBUG
        private Bitmap ToBitmap24_Standard(Bitmap bmp, PixelFormat pixelFormat)
        {
            var result = new Bitmap(bmp.Width, bmp.Height, pixelFormat);
            try
            {
                var rect = new Rectangle(Point.Empty, bmp.Size);
                var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, pixelFormat);
                try
                {
                    var resultData = result.LockBits(rect, ImageLockMode.WriteOnly, pixelFormat);
                    try
                    {
                        var buff = new byte[data.Height * data.Stride];
                        Marshal.Copy(data.Scan0, buff, 0, buff.Length);
                        Marshal.Copy(buff, 0, resultData.Scan0, buff.Length);
                    }
                    finally
                    {
                        result.UnlockBits(resultData);
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
                bmp.Dispose();
                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }

        /*
        private Bitmap ToBitmap24_ExStandard(Bitmap bmp, PixelFormat pixelFormat)
        {
            if (bmp.PixelFormat == pixelFormat)
            {
                return bmp;
            }
            var w = bmp.Width;
            var h = bmp.Height;
            var result = new Bitmap(w, h, pixelFormat);
            try
            {
                var fullRect = new Rectangle(0, 0, w, h);
                var lineRect = new Rectangle(0, 0, w, 1);
                var originalPixelFormat = bmp.PixelFormat;
                var data = bmp.LockBits(fullRect, ImageLockMode.ReadOnly, originalPixelFormat);
                try
                {
                    var dataScan0 = data.Scan0;
                    var dataStride = data.Stride;
                    var resultData = result.LockBits(fullRect, ImageLockMode.WriteOnly, pixelFormat);
                    try
                    {
                        var outScan0 = resultData.Scan0;
                        var outStride = resultData.Stride;
                        var buffSize = Math.Max(dataStride, outStride);
                        //Parallel.For(0, h, () => Tuple.Create(new Bitmap(w, 1, originalPixelFormat), new byte[buffSize]), (j, state, resouces) =>
                        var resouces = Tuple.Create(new Bitmap(w, 1, originalPixelFormat), new byte[buffSize]); for (var j = 0; j < h; j++)
                        {
                            var line = resouces.Item1;
                            var buff = resouces.Item2;
                            var lineData = line.LockBits(lineRect, ImageLockMode.WriteOnly, originalPixelFormat);
                            try
                            {
                                Marshal.Copy(dataScan0 + dataStride * j, buff, 0, dataStride);
                                Marshal.Copy(buff, 0, lineData.Scan0, dataStride);
                            }
                            finally
                            {
                                line.UnlockBits(lineData);
                            }
                            using (line = new Bitmap(line))
                            {
                                lineData = line.LockBits(lineRect, ImageLockMode.ReadOnly, pixelFormat);
                                try
                                {
                                    Marshal.Copy(lineData.Scan0, buff, 0, outStride);
                                    Marshal.Copy(buff, 0, outScan0 + outStride * j, outStride);
                                }
                                finally
                                {
                                    line.UnlockBits(lineData);
                                }
                            }
                        }
                        //    return resouces;
                        //}, resouces => resouces.Item1.Dispose());
                    }
                    finally
                    {
                        result.UnlockBits(resultData);
                    }
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
                bmp.Dispose();
                return result;
            }
            catch (Exception ee)
            {
                result.Dispose();
                throw;
            }
        }
        */

        private int[] ColorConverter(int b, int g, int r, int a, PixelFormat pixelFormat)
        {
            return ColorConverter(Color.FromArgb(a, r, g, b), pixelFormat);
        }

        private int[] ColorConverter(Color color, PixelFormat pixelFormat)
        {
            using (var bmp = new Bitmap(1, 1))
            {
                bmp.SetPixel(0, 0, color);
                var data = bmp.LockBits(new Rectangle(0, 0, 1, 1), ImageLockMode.ReadOnly, pixelFormat);
                try
                {
                    var pixelFormatSize = Image.GetPixelFormatSize(pixelFormat);
                    if (pixelFormatSize <= 32)
                    {
                        var buff = new byte[4];
                        System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buff, 0, Math.Min(4, pixelFormatSize / 8));
                        return new int[4] { buff[0], buff[1], buff[2], buff[3] };
                    }
                    else if (pixelFormatSize <= 64)
                    {
                        var buff = new short[4];
                        System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buff, 0, Math.Min(4, pixelFormatSize / 16));
                        return new int[4] { (ushort)buff[0], (ushort)buff[1], (ushort)buff[2], (ushort)buff[3] };
                    }
                    else return new int[4];
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
        }
#endif

        /*
        private Bitmap ToBitmap24(Bitmap bmp)
        {
            if (bmp == null) throw new ArgumentNullException("bmp");
            var pixelFormat = bmp.PixelFormat;
            if (pixelFormat == PixelFormat.Format24bppRgb)
            {
                return bmp;
            }
            if (!(
                pixelFormat == PixelFormat.Format32bppRgb ||
                pixelFormat == PixelFormat.Format32bppArgb ||
                pixelFormat == PixelFormat.Format32bppPArgb ||
                pixelFormat == PixelFormat.Format16bppArgb1555 ||
                pixelFormat == PixelFormat.Format16bppGrayScale ||
                pixelFormat == PixelFormat.Format16bppRgb555 ||
                pixelFormat == PixelFormat.Format16bppRgb565 ||
                pixelFormat == PixelFormat.Format1bppIndexed ||
                pixelFormat == PixelFormat.Format48bppRgb ||
                pixelFormat == PixelFormat.Format4bppIndexed ||
                pixelFormat == PixelFormat.Format64bppArgb ||
                pixelFormat == PixelFormat.Format64bppPArgb ||
                pixelFormat == PixelFormat.Format8bppIndexed //||
                //pixelFormat == PixelFormat_Format32bppCMYK
               ))
            {
                // これでパフォーマンスが上がるわけでもなく、失敗することが増えるだけ
                // pixelFormat = PixelFormat.Format24bppRgb;

                var bmp2 = new Bitmap(bmp);
                try
                {
                    var result = ToBitmap24(bmp2);
                    bmp.Dispose();
                    return result;
                }
                catch
                {
                    bmp2.Dispose();
                    throw;
                }
            }
            var w = bmp.Width;
            var h = bmp.Height;
            var rect = new Rectangle(0, 0, w, h);
            var dst = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            try
            {
                var dstData = dst.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                try
                {
                    var dstScan0 = dstData.Scan0;
                    var dstStride = dstData.Stride;
                    var buffOutSize = 3 * w;
                    
                    var srcData = bmp.LockBits(rect, ImageLockMode.ReadOnly, pixelFormat);
                    try
                    {
                        var srcScan0 = srcData.Scan0;
                        var srcStride = srcData.Stride;
                        var bpp = Image.GetPixelFormatSize(pixelFormat);
                        var buffInSize = (bpp * w + 7) / 8;

                        var palette = null as byte[][];
                        if ((pixelFormat & PixelFormat.Indexed) == PixelFormat.Indexed)
                        {
                            var indexSize = 1 << bpp;
                            var bmpPalette = bmp.Palette.Entries;
                            var stop = Math.Min(indexSize, bmpPalette.Length);
                            palette = new byte[indexSize][];
                            for (var i = 0; i < stop; i++)
                            {
                                var c = bmpPalette[i];
                                var a = c.A;
                                var ar = (255 - a) * 255 + 127;
                                palette[i] = new byte[3] { (byte)((a * c.B + ar) / 255), (byte)((a * c.G + ar) / 255), (byte)((a * c.R + ar) / 255) };
                            }
                        }

                        // 非線形変換（ガンマ 1 / 2.2 に近いが異なる）
                        // 使いまわしているが、毎回生成してもほとんどパフォーマンスに影響はない
                        if (ToBitmap24_RGBLookUp == null && bpp > 32)
                        {
                            using (var sample = new Bitmap(8193 / 3, 1, PixelFormat.Format48bppRgb)) //  8bppGrayScale がないのでカラーを利用
                            {
                                var sampleRect = new Rectangle(0, 0, 8193 / 3, 1);
                                var buff = new short[8193]; for (var i = 0; i < 8193; i++) buff[i] = (short)i;
                                var data = sample.LockBits(sampleRect, ImageLockMode.WriteOnly, PixelFormat.Format48bppRgb);
                                try
                                {
                                    Marshal.Copy(buff, 0, data.Scan0, 8193);
                                }
                                finally
                                {
                                    sample.UnlockBits(data);
                                }
                                ToBitmap24_RGBLookUp = new byte[8193];
                                data = sample.LockBits(sampleRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                                try
                                {
                                    Marshal.Copy(data.Scan0, ToBitmap24_RGBLookUp, 0, 8193);
                                }
                                finally
                                {
                                    sample.UnlockBits(data);
                                }
                            }
                        }
                        
                        int buffInLength;
                        switch (pixelFormat)
                        {
                            case PixelFormat.Format32bppRgb:
                                Parallel.For(0, h,
                                    () => new byte[buffInSize],
                                    (j, state, buff) =>
                                    {
                                        Marshal.Copy(srcScan0 + j * srcStride, buff, 0, buffInSize);
                                        int u = 3, v = 4;
                                        while (v < buffInSize)
                                        {
                                            buff[u++] = buff[v++];
                                            buff[u++] = buff[v++];
                                            buff[u++] = buff[v]; v += 2;
                                        }
                                        Marshal.Copy(buff, 0, dstScan0 + j * dstStride, buffOutSize);
                                        return buff;
                                    }, buff => { });
                                break;
                            case PixelFormat.Format32bppArgb:
                                Parallel.For(0, h,
                                    () => new byte[buffInSize],
                                    (j, state, buff) =>
                                    {
                                        Marshal.Copy(srcScan0 + j * srcStride, buff, 0, buffInSize);
                                        int u = 0, v = 0;
                                        while (v < buffInSize)
                                        {
                                            var a = (int)buff[v + 3];
                                            var ar = (255 - a) * 255 + 127;
                                            buff[u++] = (byte)((a * buff[v++] + ar) / 255);
                                            buff[u++] = (byte)((a * buff[v++] + ar) / 255);
                                            buff[u++] = (byte)((a * buff[v] + ar) / 255); v += 2;

                                        }
                                        Marshal.Copy(buff, 0, dstScan0 + j * dstStride, buffOutSize);
                                        return buff;
                                    }, buff => { });
                                break;
                            case PixelFormat.Format32bppPArgb:
                                Parallel.For(0, h,
                                    () => new byte[buffInSize],
                                    (j, state, buff) =>
                                    {
                                        Marshal.Copy(srcScan0 + j * srcStride, buff, 0, buffInSize);
                                        int u = 0, v = 0;
                                        while (v < buffInSize)
                                        {
                                            var ar = 255 - buff[v + 3];
                                            int value;
                                            value = buff[v++] + ar;
                                            buff[u++] = (byte)(value > 255 ? 255 : value);
                                            value = buff[v++] + ar;
                                            buff[u++] = (byte)(value > 255 ? 255 : value);
                                            value = buff[v] + ar; v += 2;
                                            buff[u++] = (byte)(value > 255 ? 255 : value);
                                        }
                                        Marshal.Copy(buff, 0, dstScan0 + j * dstStride, buffOutSize);
                                        return buff;
                                    }, buff => { });
                                break;
                            case PixelFormat.Format16bppArgb1555:
                                Parallel.For(0, h,
                                    () => Tuple.Create(new short[w], new byte[buffOutSize]),
                                    (j, state, buffs) =>
                                    {
                                        var buffIn = buffs.Item1;
                                        var buffOut = buffs.Item2;
                                        Marshal.Copy(srcScan0 + j * srcStride, buffIn, 0, w);
                                        var u = 0;
                                        for (var v = 0; v < w; v++)
                                        {
                                            var s = buffIn[v];
                                            if ((s & 0x8000) != 0)
                                            {
                                                buffOut[u++] = (byte)(s << 3 | 4);
                                                buffOut[u++] = (byte)((0xFF8 & (s >> 2)) | 4);
                                                buffOut[u++] = (byte)((0xFF8 & (s >> 7)) | 4);
                                            }
                                            else
                                            {
                                                buffOut[u++] = 255;
                                                buffOut[u++] = 255;
                                                buffOut[u++] = 255;
                                            }
                                        }
                                        Marshal.Copy(buffOut, 0, dstScan0 + j * dstStride, buffOutSize);
                                        return buffs;
                                    }, buffs => { });
                                break;
                            case PixelFormat.Format16bppGrayScale:
                                Parallel.For(0, h,
                                    () => Tuple.Create(new short[w], new byte[buffOutSize]),
                                    (j, state, buffs) =>
                                    {
                                        var buffIn = buffs.Item1;
                                        var buffOut = buffs.Item2;
                                        Marshal.Copy(srcScan0 + j * srcStride, buffIn, 0, w);
                                        var u = 0;
                                        for (var v = 0; v < w; v++)
                                        {
                                            var s = (int)(ushort)buffIn[v];
                                            var value = s <= 0 ? (byte)0 : s >= 8192 ? (byte)255 : ToBitmap24_RGBLookUp[s];
                                            buffOut[u++] = value;
                                            buffOut[u++] = value;
                                            buffOut[u++] = value;
                                        }
                                        Marshal.Copy(buffOut, 0, dstScan0 + j * dstStride, w);
                                        return buffs;
                                    }, buffs => { });
                                break;
                            case PixelFormat.Format16bppRgb555:
                                Parallel.For(0, h,
                                    () => Tuple.Create(new short[w], new byte[buffOutSize]),
                                    (j, state, buffs) =>
                                    {
                                        var buffIn = buffs.Item1;
                                        var buffOut = buffs.Item2;
                                        Marshal.Copy(srcScan0 + j * srcStride, buffIn, 0, w);
                                        var u = 0;
                                        for (var v = 0; v < w; v++)
                                        {
                                            var s = buffIn[v];
                                            buffOut[u++] = (byte)(s << 3 | 4);
                                            buffOut[u++] = (byte)((0xFF8 & (s >> 2)) | 4);
                                            buffOut[u++] = (byte)((0xFF8 & (s >> 7)) | 4);
                                        }
                                        Marshal.Copy(buffOut, 0, dstScan0 + j * dstStride, buffOutSize);
                                        return buffs;
                                    }, buffs => { });
                                break;
                            case PixelFormat.Format16bppRgb565:
                                Parallel.For(0, h,
                                    () => Tuple.Create(new short[w], new byte[buffOutSize]),
                                    (j, state, buffs) =>
                                    {
                                        var buffIn = buffs.Item1;
                                        var buffOut = buffs.Item2;
                                        Marshal.Copy(srcScan0 + j * srcStride, buffIn, 0, w);
                                        var u = 0;
                                        for (var v = 0; v < w; v++)
                                        {
                                            var s = buffIn[v];
                                            buffOut[u++] = (byte)(s << 3 | 4);
                                            buffOut[u++] = (byte)((0xFFC & (s >> 3)) | 2);
                                            buffOut[u++] = (byte)((0xFF8 & (s >> 8)) | 4);
                                        }
                                        Marshal.Copy(buffOut, 0, dstScan0 + j * dstStride, buffOutSize);
                                        return buffs;
                                    }, buffs => { });
                                break;
                            case PixelFormat.Format1bppIndexed:
                                buffInLength = (buffInSize + 3) >> 2;
                                Parallel.For(0, h,
                                    () => Tuple.Create(new int[buffInLength], new byte[buffOutSize]),
                                    (j, state, buffs) =>
                                    {
                                        var buffIn = buffs.Item1;
                                        var buffOut = buffs.Item2;
                                        Marshal.Copy(srcScan0 + j * srcStride, buffIn, 0, buffInLength);
                                        int u = 0, v = 0, shift = 0;
                                        for (var i = 0; i < w; i++)
                                        {
                                            var c = palette[(buffIn[v] >> shift) & 1];
                                            buffOut[u++] = c[0];
                                            buffOut[u++] = c[1];
                                            buffOut[u++] = c[2];
                                            shift++;
                                            if(shift == 32)
                                            {
                                                shift = 0;
                                                v++;
                                            }
                                        }
                                        Marshal.Copy(buffOut, 0, dstScan0 + j * dstStride, buffOutSize);
                                        return buffs;
                                    }, buffs => { });
                                break;
                            case PixelFormat.Format48bppRgb:
                                buffInLength = buffInSize >> 1;
                                Parallel.For(0, h,
                                    () => Tuple.Create(new short[buffInLength], new byte[buffOutSize]),
                                    (j, state, buffs) =>
                                    {
                                        var buffIn = buffs.Item1;
                                        var buffOut = buffs.Item2;
                                        Marshal.Copy(srcScan0 + j * srcStride, buffIn, 0, buffInLength);
                                        var u = 0;
                                        while (u < buffInLength)
                                        {
                                            int value;
                                            value = (ushort)buffIn[u];
                                            buffOut[u++] = value <= 0 ? (byte)0 : value >= 8192 ? (byte)255 : ToBitmap24_RGBLookUp[value];
                                            value = (ushort)buffIn[u];
                                            buffOut[u++] = value <= 0 ? (byte)0 : value >= 8192 ? (byte)255 : ToBitmap24_RGBLookUp[value];
                                            value = (ushort)buffIn[u];
                                            buffOut[u++] = value <= 0 ? (byte)0 : value >= 8192 ? (byte)255 : ToBitmap24_RGBLookUp[value];
                                        }
                                        Marshal.Copy(buffOut, 0, dstScan0 + j * dstStride, buffOutSize);
                                        return buffs;
                                    }, buffs => { });
                                break;
                            case PixelFormat.Format4bppIndexed:
                                buffInLength = (buffInSize + 1) >> 2;
                                Parallel.For(0, h,
                                    () => Tuple.Create(new int[buffInLength], new byte[buffOutSize]),
                                    (j, state, buffs) =>
                                    {
                                        var buffIn = buffs.Item1;
                                        var buffOut = buffs.Item2;
                                        Marshal.Copy(srcScan0 + j * srcStride, buffIn, 0, buffInLength);
                                        int u = 0, v = 0, shift = 0;
                                        for (var i = 0; i < w; i++)
                                        {
                                            var c = palette[(buffIn[v] >> shift) & 0xF];
                                            buffOut[u++] = c[0];
                                            buffOut[u++] = c[1];
                                            buffOut[u++] = c[2];
                                            shift += 4;
                                            if (shift == 32)
                                            {
                                                shift = 0;
                                                v++;
                                            }
                                        }
                                        Marshal.Copy(buffOut, 0, dstScan0 + j * dstStride, buffOutSize);
                                        return buffs;
                                    }, buffs => { });
                                break;
                            case PixelFormat.Format64bppArgb:
                                buffInLength = buffInSize >> 1;
                                Parallel.For(0, h,
                                    () => Tuple.Create(new short[buffInLength], new byte[buffOutSize]),
                                    (j, state, buffs) =>
                                    {
                                        var buffIn = buffs.Item1;
                                        var buffOut = buffs.Item2;
                                        Marshal.Copy(srcScan0 + j * srcStride, buffIn, 0, buffInLength);
                                        int u = 0, v = 0;
                                        while (v < buffInLength)
                                        {
                                            var a = ((ushort)buffIn[v + 3] + 16) >> 5;
                                            var ar = (255 - a) * 255 + 127;
                                            int value;
                                            value = (ushort)buffIn[v++];
                                            value = value <= 0 ? 0 : value >= 8192 ? 255 : ToBitmap24_RGBLookUp[value];
                                            value = (a * value + ar) / 255; // a < 0 になりうるので value も範囲外の可能性がある
                                            buffOut[u++] = (byte)(value <= 0 ? 0 : value >= 255 ? 255 : value);
                                            value = (ushort)buffIn[v++];
                                            value = value <= 0 ? 0 : value >= 8192 ? 255 : ToBitmap24_RGBLookUp[value];
                                            value = (a * value + ar) / 255; // a < 0 になりうるので value も範囲外の可能性がある
                                            buffOut[u++] = (byte)(value <= 0 ? 0 : value >= 255 ? 255 : value);
                                            value = (ushort)buffIn[v]; v += 2;
                                            value = value <= 0 ? 0 : value >= 8192 ? 255 : ToBitmap24_RGBLookUp[value];
                                            value = (a * value + ar) / 255; // a < 0 になりうるので value も範囲外の可能性がある
                                            buffOut[u++] = (byte)(value <= 0 ? 0 : value >= 255 ? 255 : value);
                                        }
                                        Marshal.Copy(buffOut, 0, dstScan0 + j * dstStride, buffOutSize);
                                        return buffs;
                                    }, buffs => { });
                                break;
                            case PixelFormat.Format64bppPArgb:
                                buffInLength = buffInSize >> 1;
                                Parallel.For(0, h,
                                    () => Tuple.Create(new short[buffInLength], new byte[buffOutSize]),
                                    (j, state, buffs) =>
                                    {
                                        var buffIn = buffs.Item1;
                                        var buffOut = buffs.Item2;
                                        Marshal.Copy(srcScan0 + j * srcStride, buffIn, 0, buffInLength);
                                        int u = 0, v = 0;
                                        while (v < buffInLength)
                                        {
                                            var ar = 255 - (((ushort)buffIn[v + 3] + 16) >> 5);
                                            int value;
                                            value = buffIn[v++];
                                            value = value <= 0 ? 0 : value >= 8192 ? 255 : ToBitmap24_RGBLookUp[value];
                                            value = value + ar; // 過多のチェックは当然。a < 0 の可能性より過小のチェックも必要
                                            buffOut[u++] = (byte)(value <= 0 ? 0 : value >= 255 ? 255 : value);
                                            value = buffIn[v++];
                                            value = value <= 0 ? 0 : value >= 8192 ? 255 : ToBitmap24_RGBLookUp[value];
                                            value = value + ar; // 過多のチェックは当然。a < 0 の可能性より過小のチェックも必要
                                            buffOut[u++] = (byte)(value <= 0 ? 0 : value >= 255 ? 255 : value);
                                            value = buffIn[v]; v += 2;
                                            value = value <= 0 ? 0 : value >= 8192 ? 255 : ToBitmap24_RGBLookUp[value];
                                            value = value + ar; // 過多のチェックは当然。a < 0 の可能性より過小のチェックも必要
                                            buffOut[u++] = (byte)(value <= 0 ? 0 : value >= 255 ? 255 : value);
                                        }
                                        Marshal.Copy(buffOut, 0, dstScan0 + j * dstStride, buffOutSize);
                                        return buffs;
                                    }, buffs => { });
                                break;
                            case PixelFormat.Format8bppIndexed:
                                Parallel.For(0, h,
                                    () => Tuple.Create(new byte[buffInSize], new byte[buffOutSize]),
                                    (j, state, buffs) =>
                                    {
                                        var buffIn = buffs.Item1;
                                        var buffOut = buffs.Item2;
                                        Marshal.Copy(srcScan0 + j * srcStride, buffIn, 0, buffInSize);
                                        var u = 0;
                                        for (var v = 0; v < w; v++)
                                        {
                                            var c = palette[buffIn[v]];
                                            buffOut[u++] = c[0];
                                            buffOut[u++] = c[1];
                                            buffOut[u++] = c[2];
                                        }
                                        Marshal.Copy(buffOut, 0, dstScan0 + j * dstStride, buffOutSize);
                                        return buffs;
                                    }, buffs => { });
                                break;
                            case PixelFormat_Format32bppCMYK:
                                Parallel.For(0, h,
                                    () => new byte[buffInSize],
                                    (j, state, buff) =>
                                    {
                                        Marshal.Copy(srcScan0 + j * srcStride, buff, 0, buffInSize);
                                        int u = 0, v = 0;
                                        while (v < buffInSize)
                                        {
                                            var c = (int)buff[v++];
                                            var m = (int)buff[v++];
                                            var y = (int)buff[v++];
                                            var k = (int)buff[v++];

                                            //buff[u++] = (byte)Math.Max(0, 255 - y - k);
                                            //buff[u++] = (byte)Math.Max(0, 255 - m - k);
                                            //buff[u++] = (byte)Math.Max(0, 255 - c - k);

                                            var kr = 255 - k;
                                            buff[u++] = (byte)(255 - ((y * kr + 127) / 255 + k));
                                            buff[u++] = (byte)(255 - ((m * kr + 127) / 255 + k));
                                            buff[u++] = (byte)(255 - ((c * kr + 127) / 255 + k));

                                            //buff[u++] = (byte)(Math.Pow(Math.Max(0, 255 - y - k) / 255.0, 1 / 2.0) * 255 + 0.5);
                                            //buff[u++] = (byte)(Math.Pow(Math.Max(0, 255 - m - k) / 255.0, 1 / 2.0) * 255 + 0.5);
                                            //buff[u++] = (byte)(Math.Pow(Math.Max(0, 255 - c - k) / 255.0, 1 / 2.0) * 255 + 0.5);


                                            //var kd = k / 255.0;
                                            //var krd = (1 - kd) / 255.0;
                                            //buff[u++] = (byte)(Math.Pow(1 - (y * krd + kd), 1 / 2.2) * 255 + 0.5);
                                            //buff[u++] = (byte)(Math.Pow(1 - (m * krd + kd), 1 / 2.2) * 255 + 0.5);
                                            //buff[u++] = (byte)(Math.Pow(1 - (c * krd + kd), 1 / 2.2) * 255 + 0.5);
                                        }
                                        Marshal.Copy(buff, 0, dstScan0 + j * dstStride, buffOutSize);
                                        return buff;
                                    }, buff => { });
                                break;
                            case PixelFormat.Format24bppRgb:
                                {
                                    var buff = new byte[buffInSize];
                                    for (var j = 0; j < h; j++)
                                    {
                                        Marshal.Copy(srcScan0 + j * srcStride, buff, 0, buffInSize);
                                        Marshal.Copy(buff, 0, dstScan0 + j * dstStride, buffOutSize);
                                    }
                                }
                                break;
                        }
                    }
                    finally
                    {
                        bmp.UnlockBits(srcData);
                    }
                    bmp.Dispose();
                    return dst;
                }
                finally
                {
                    dst.UnlockBits(dstData);
                }
            }
            catch
            {
                dst.Dispose();
                throw;
            }
        }
        */
        
        [DllImport("kernel32.dll")]
        public static extern void CopyMemory(IntPtr dst, IntPtr src, int size);
        
        private static unsafe bool IsGrayScaleBitmap(Bitmap bmp)
        {
            if (bmp == null) throw new ArgumentNullException("bmp");
            var pixelFormat = bmp.PixelFormat;
            if ((pixelFormat & PixelFormat.Indexed) == PixelFormat.Indexed)
            {
                var palette = bmp.Palette;
                if((palette.Flags & 2) == 2)
                {
                    return true;
                }
                else
                {
                    return palette.Entries.All(c => { var r = c.R; return r == c.G && r == c.B; });
                }
            }
            if (pixelFormat == PixelFormat.Format16bppGrayScale) return true;
            if (!(
                pixelFormat == PixelFormat.Format24bppRgb ||
                pixelFormat == PixelFormat.Format32bppRgb ||
                pixelFormat == PixelFormat.Format32bppArgb ||
                pixelFormat == PixelFormat.Format32bppPArgb ||
                pixelFormat == PixelFormat.Format16bppArgb1555 ||
                pixelFormat == PixelFormat.Format16bppRgb555 ||
                pixelFormat == PixelFormat.Format16bppRgb565 ||
                pixelFormat == PixelFormat.Format48bppRgb ||
                pixelFormat == PixelFormat.Format64bppArgb ||
                pixelFormat == PixelFormat.Format64bppPArgb
               ))
            {
                return false;
            }
            var w = bmp.Width;
            var h = bmp.Height;
            var rect = new Rectangle(0, 0, w, h);

            var srcData = bmp.LockBits(rect, ImageLockMode.ReadOnly, pixelFormat);
            try
            {
                var srcScan0 = srcData.Scan0;
                var srcStride = srcData.Stride;
                var bpp = Image.GetPixelFormatSize(pixelFormat);
                var buffInSize = (bpp * w + 7) / 8;
                
                var srcByteAdr = (byte*)srcScan0;
                var srcUShortAdr = (ushort*)srcScan0;
                var srcStrideForUShort = srcStride / 2;
                var srcUIntAdr = (uint*)srcScan0;
                var srcStrideForUInt = srcStride / 4;
                int buffInLength;
                var result = true;
                switch (pixelFormat)
                {
                    case PixelFormat.Format24bppRgb:
                        Parallel.For(0, h,
                            (j, loop) =>
                            {
                                var buffIn = srcByteAdr + j * srcStride;
                                int v = 0;
                                while (v < buffInSize)
                                {
                                    var b = buffIn[v++];
                                    if (b != buffIn[v++] || b != buffIn[v++])
                                    {
                                        result = false;
                                        loop.Break();
                                        return;
                                    }
                                }
                            });
                        return result;
                    case PixelFormat.Format32bppRgb:
                    case PixelFormat.Format32bppArgb:
                    case PixelFormat.Format32bppPArgb:
                    case PixelFormat_Format32bppCMYK:
                        Parallel.For(0, h,
                            (j, loop) =>
                            {
                                var buffIn = srcByteAdr + j * srcStride;
                                int v = 0;
                                while (v < buffInSize)
                                {
                                    var b = buffIn[v++];
                                    if (b != buffIn[v++] || b != buffIn[v])
                                    {
                                        result = false;
                                        loop.Break();
                                        return;
                                    }
                                    v += 2;
                                }
                            });
                        return result;
                    case PixelFormat.Format16bppArgb1555:
                        Parallel.For(0, h,
                            (j, loop) =>
                            {
                                var buffIn = srcUShortAdr + j * srcStrideForUShort;
                                for (var v = 0; v < w; v++)
                                {
                                    var s = buffIn[v];
                                    if ((s & 0x8000) != 0)
                                    {
                                        var b = (s << 3 | 4);
                                        if (b != ((0xFF8 & (s >> 2)) | 4) || b != ((0xFF8 & (s >> 7)) | 4))
                                        {
                                            result = false;
                                            loop.Break();
                                            return;
                                        }
                                    }
                                }
                            });
                        return result;
                    case PixelFormat.Format16bppRgb555:
                        Parallel.For(0, h,
                            (j, loop) =>
                            {
                                var buffIn = srcUShortAdr + j * srcStrideForUShort;
                                for (var v = 0; v < w; v++)
                                {
                                    var s = buffIn[v];
                                    var b = s << 3;
                                    if (b != (0xFF8 & (s >> 2)) || b != (0xFF8 & (s >> 7)))
                                    {
                                        result = false;
                                        loop.Break();
                                        return;
                                    }
                                }
                            });
                        return result;
                    case PixelFormat.Format16bppRgb565:
                        Parallel.For(0, h,
                            (j, loop) =>
                            {
                                var buffIn = srcUShortAdr + j * srcStrideForUShort;
                                for (var v = 0; v < w; v++)
                                {
                                    var s = buffIn[v];
                                    var b = s << 3;
                                    if (b != (0xFFC & (s >> 3)) || b != (0xFF8 & (s >> 8)))
                                    {
                                        result = false;
                                        loop.Break();
                                        return;
                                    }
                                }
                            });
                        return result;
                    case PixelFormat.Format48bppRgb:
                        buffInLength = buffInSize >> 1;
                        Parallel.For(0, h,
                            (j, loop) =>
                            {
                                var buffIn = srcUShortAdr + j * srcStrideForUShort;
                                var u = 0;
                                while (u < buffInLength)
                                {
                                    var b = buffIn[u++];
                                    if (b != buffIn[u++] || b != buffIn[u++])
                                    {
                                        result = false;
                                        loop.Break();
                                        return;
                                    }
                                }
                            });
                        return result;
                    case PixelFormat.Format64bppArgb:
                    case PixelFormat.Format64bppPArgb:
                        buffInLength = buffInSize >> 1;
                        Parallel.For(0, h,
                            (j, loop) =>
                            {
                                var buffIn = srcUShortAdr + j * srcStrideForUShort;
                                int v = 0;
                                while (v < buffInLength)
                                {
                                    var a = (buffIn[v + 3] + 16) >> 5;
                                    var ar = (255 - a) * 255 + 127;
                                    var b = buffIn[v++];
                                    if (h != buffIn[v++] || b != buffIn[v])
                                    {
                                        result = false;
                                        loop.Break();
                                        return;
                                    }
                                    v += 2;
                                }
                            });
                        return result;
                    default: return false;
                }
            }
            finally
            {
                bmp.UnlockBits(srcData);
            }
        }

        private const PixelFormat PixelFormat_Format32bppCMYK = (PixelFormat)8207;
        private static Bitmap ToBitmap24Unsafe(Bitmap bmp)
        {
            return ToBitmap24Unsafe(bmp, leaveOpenOriginal: false);
        }
        public static Bitmap ToBitmap24Unsafe(Bitmap bmp, bool leaveOpenOriginal)
        {
            return ToOtehrPixeoFormat(bmp, PixelFormat.Format24bppRgb, leaveOpenOriginal);
        }
        public static unsafe Bitmap ToOtehrPixeoFormat(Bitmap bmp, PixelFormat newPixelFormat, bool leaveOpenOriginal)
        {
            if (bmp == null) throw new ArgumentNullException(nameof(bmp));
            if (bmp.PixelFormat == newPixelFormat) return bmp;
            var w = bmp.Width;
            var h = bmp.Height;
            var rect = new Rectangle(0, 0, w, h);
            var dst = null as Bitmap;
            try
            {
                if ((bmp.PixelFormat & PixelFormat.Alpha) == PixelFormat.Alpha)
                {
                    dst = new Bitmap(w, h, newPixelFormat);
                    bmp.SetResolution(dst.HorizontalResolution, dst.VerticalResolution);
                    using (var g = Graphics.FromImage(dst))
                    {
                        g.FillRectangle(Brushes.White, rect);
                        g.DrawImageUnscaled(bmp, 0, 0);
                    }
                }
                else
                {
                    dst = new Bitmap(w, h, newPixelFormat);
                    var dstData = dst.LockBits(rect, ImageLockMode.WriteOnly, newPixelFormat);
                    bmp.SetResolution(dst.HorizontalResolution, dst.VerticalResolution);
                    try
                    {
                        var bmp2 = null as Bitmap;
                        var cmykBmp = null as Bitmap;
                        try
                        {
                            if (bmp.PixelFormat == PixelFormat_Format32bppCMYK)
                            {
                                cmykBmp = new Bitmap(bmp);
                                bmp2 = cmykBmp;
                            }
                            else
                            {
                                bmp2 = bmp;
                            }

                            var srcData = bmp2.LockBits(rect, ImageLockMode.ReadOnly, newPixelFormat);
                            try
                            {
                                CopyMemory(dstData.Scan0, srcData.Scan0, dstData.Stride * dstData.Height);
                            }
                            finally
                            {
                                bmp2.UnlockBits(srcData);
                            }
                        }
                        finally
                        {
                            cmykBmp?.Dispose();
                        }
                    }
                    finally
                    {
                        dst.UnlockBits(dstData);
                    }
                }

                if (!leaveOpenOriginal) bmp.Dispose();
                return dst;
            }
#if DEBUG
            catch (Exception ex)
            {
                var message = ex.ToString();
#else
            catch
            {
#endif
                dst?.Dispose();
                throw;
            }
        }
        
        private static byte[] ToBitmap24_RGBLookUp;
        private static unsafe Bitmap ToBitmap8Unsafe(Bitmap bmp)
        {
            return ToBitmap8Unsafe(bmp, leaveOpenOriginal: false);
        }
        public static unsafe Bitmap ToBitmap8Unsafe(Bitmap bmp, bool leaveOpenOriginal)
        {
            if (bmp == null) throw new ArgumentNullException(nameof(bmp));
            var pixelFormat = bmp.PixelFormat;
            const int Flag_IsGrayScale = 0x00000002;
            if (pixelFormat == PixelFormat.Format8bppIndexed && (bmp.Palette.Flags & Flag_IsGrayScale) == Flag_IsGrayScale)
            {
                var entries = bmp.Palette.Entries;
                var ok = true;
                for (var i = 0; i < entries.Length; i++)
                {
                    var c = entries[i];
                    if (c.A != 255 || c.R != i || c.G != i || c.B != i)
                    {
                        ok = false;
                        break;
                    }
                }
                if (ok) return bmp;
            }
            if (!(
                pixelFormat == PixelFormat.Format24bppRgb ||
                pixelFormat == PixelFormat.Format32bppRgb ||
                pixelFormat == PixelFormat.Format32bppArgb ||
                pixelFormat == PixelFormat.Format32bppPArgb ||
                pixelFormat == PixelFormat.Format16bppArgb1555 ||
                pixelFormat == PixelFormat.Format16bppGrayScale ||
                pixelFormat == PixelFormat.Format16bppRgb555 ||
                pixelFormat == PixelFormat.Format16bppRgb565 ||
                pixelFormat == PixelFormat.Format1bppIndexed ||
                pixelFormat == PixelFormat.Format48bppRgb ||
                pixelFormat == PixelFormat.Format4bppIndexed ||
                pixelFormat == PixelFormat.Format64bppArgb ||
                pixelFormat == PixelFormat.Format64bppPArgb ||
                pixelFormat == PixelFormat.Format8bppIndexed //||
                                                             //pixelFormat == PixelFormat_Format32bppCMYK
               ))
            {
                // これでパフォーマンスが上がるわけでもなく、失敗することが増えるだけ
                // pixelFormat = PixelFormat.Format24bppRgb;


                return ToBitmap24Unsafe(bmp, leaveOpenOriginal); // 8 が無理なら 24 に変換

                /*
                var bmp2 = new Bitmap(bmp);
                try
                {
                    var result = ToBitmap8Unsafe(bmp2);
                    bmp.Dispose();
                    return result;
                }
                catch
                {
                    bmp2.Dispose();
                    throw;
                }*/
            }
            var w = bmp.Width;
            var h = bmp.Height;
            var rect = new Rectangle(0, 0, w, h);
            var dst = new Bitmap(w, h, PixelFormat.Format8bppIndexed);
            try
            {
                var dstPallete = dst.Palette;
                var entries = dstPallete.Entries;
                for (var i = 0; i < entries.Length; i++)
                {
                    entries[i] = Color.FromArgb(i, i, i);
                }
                dst.Palette = dstPallete;

                var dstData = dst.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);
                try
                {
                    var dstScan0 = dstData.Scan0;
                    var dstStride = dstData.Stride;
                    var buffOutSize = w;

                    var srcData = bmp.LockBits(rect, ImageLockMode.ReadOnly, pixelFormat);
                    try
                    {
                        var srcScan0 = srcData.Scan0;
                        var srcStride = srcData.Stride;
                        var bpp = Image.GetPixelFormatSize(pixelFormat);
                        var buffInSize = (bpp * w + 7) / 8;

                        var palette = null as byte[,];
                        if ((pixelFormat & PixelFormat.Indexed) == PixelFormat.Indexed)
                        {
                            var indexSize = 1 << bpp;
                            var bmpPalette = bmp.Palette.Entries;
                            var stop = Math.Min(indexSize, bmpPalette.Length);
                            palette = new byte[indexSize, 3];
                            for (var i = 0; i < stop; i++)
                            {
                                var c = bmpPalette[i];
                                var a = c.A;
                                var ar = (255 - a) * 255 + 127;
                                palette[i, 0] = (byte)((a * c.B + ar) / 255);
                                palette[i, 1] = (byte)((a * c.G + ar) / 255);
                                palette[i, 2] = (byte)((a * c.R + ar) / 255);
                            }
                        }

                        // 非線形変換（ガンマ 1 / 2.2 に近いが異なる）
                        // 使いまわしているが、毎回生成してもほとんどパフォーマンスに影響はない
                        if (ToBitmap24_RGBLookUp == null && bpp > 32)
                        {
                            using (var sample = new Bitmap(8193 / 3, 1, PixelFormat.Format48bppRgb)) //  8bppGrayScale がないのでカラーを利用
                            {
                                var sampleRect = new Rectangle(0, 0, 8193 / 3, 1);
                                var buff = new short[8193]; for (var i = 0; i < 8193; i++) buff[i] = (short)i;
                                var data = sample.LockBits(sampleRect, ImageLockMode.WriteOnly, PixelFormat.Format48bppRgb);
                                try
                                {
                                    //Marshal.Copy(buff, 0, data.Scan0, 8193);
                                    fixed (short* buffAdr = buff)
                                    {
                                        CopyMemory(data.Scan0, (IntPtr)buffAdr, 8193 * sizeof(short));
                                    }
                                }
                                finally
                                {
                                    sample.UnlockBits(data);
                                }
                                ToBitmap24_RGBLookUp = new byte[8193];
                                data = sample.LockBits(sampleRect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                                try
                                {
                                    //Marshal.Copy(data.Scan0, ToBitmap24_RGBLookUp, 0, 8193);
                                    fixed (byte* ToBitmap24_RGBLookUpAdr = ToBitmap24_RGBLookUp)
                                    {
                                        CopyMemory((IntPtr)ToBitmap24_RGBLookUpAdr, data.Scan0, 8193 * sizeof(byte));
                                    }
                                }
                                finally
                                {
                                    sample.UnlockBits(data);
                                }
                            }
                        }

                        var srcByteAdr = (byte*)srcScan0;
                        var srcUShortAdr = (ushort*)srcScan0;
                        var srcStrideForUShort = srcStride / 2;
                        var srcUIntAdr = (uint*)srcScan0;
                        var srcStrideForUInt = srcStride / 4;
                        var dstByteAdr = (byte*)dstScan0;
                        int buffInLength;
                        switch (pixelFormat)
                        {
                            case PixelFormat.Format24bppRgb:
                                Parallel.For(0, h,
                                    j =>
                                    {
                                        var buffIn = srcByteAdr + j * srcStride;
                                        var buffOut = dstByteAdr + j * dstStride;
                                        int u = 0, v = 0;
                                        while (v < buffInSize)
                                        {
                                            buffOut[u++] = buffIn[v]; v += 3;
                                        }
                                    });
                                break;
                            case PixelFormat.Format32bppRgb:
                                Parallel.For(0, h,
                                    j =>
                                    {
                                        var buffIn = srcByteAdr + j * srcStride;
                                        var buffOut = dstByteAdr + j * dstStride;
                                        int u = 0, v = 0;
                                        while (v < buffInSize)
                                        {
                                            buffOut[u++] = buffIn[v]; v += 4;
                                        }
                                    });
                                break;
                            case PixelFormat.Format32bppArgb:
                                Parallel.For(0, h,
                                    j =>
                                    {
                                        var buffIn = srcByteAdr + j * srcStride;
                                        var buffOut = dstByteAdr + j * dstStride;
                                        int u = 0, v = 0;
                                        while (v < buffInSize)
                                        {
                                            var a = (int)buffIn[v + 3];
                                            var ar = (255 - a) * 255 + 127;
                                            buffOut[u++] = (byte)((a * buffIn[v] + ar) / 255); v += 4;

                                        }
                                    });
                                break;
                            case PixelFormat.Format32bppPArgb:
                                Parallel.For(0, h,
                                    j =>
                                    {
                                        var buffIn = srcByteAdr + j * srcStride;
                                        var buffOut = dstByteAdr + j * dstStride;
                                        int u = 0, v = 0;
                                        while (v < buffInSize)
                                        {
                                            var ar = 255 - buffIn[v + 3];
                                            int value;
                                            value = buffIn[v] + ar; v += 4;
                                            buffOut[u++] = (byte)(value > 255 ? 255 : value);
                                        }
                                    });
                                break;
                            case PixelFormat.Format16bppArgb1555:
                                Parallel.For(0, h,
                                    j =>
                                    {
                                        var buffIn = srcUShortAdr + j * srcStrideForUShort;
                                        var buffOut = dstByteAdr + j * dstStride;
                                        var u = 0;
                                        for (var v = 0; v < w; v++)
                                        {
                                            var s = buffIn[v];
                                            if ((s & 0x8000) != 0)
                                            {
                                                buffOut[u++] = (byte)(s << 3 | 4);
                                            }
                                            else
                                            {
                                                buffOut[u++] = 255;
                                            }
                                        }
                                    });
                                break;
                            case PixelFormat.Format16bppGrayScale:
                                fixed (byte* l = ToBitmap24_RGBLookUp)
                                {
                                    var lookup = l;
                                    Parallel.For(0, h,
                                        j =>
                                        {
                                            var buffIn = srcUShortAdr + j * srcStrideForUShort;
                                            var buffOut = dstByteAdr + j * dstStride;
                                            var u = 0;
                                            for (var v = 0; v < w; v++)
                                            {
                                                var s = (int)(ushort)buffIn[v];
                                                var value = s <= 0 ? (byte)0 : s >= 8192 ? (byte)255 : lookup[s];
                                                buffOut[u++] = value;
                                            }
                                        });
                                }
                                break;
                            case PixelFormat.Format16bppRgb555:
                                Parallel.For(0, h,
                                    j =>
                                    {
                                        var buffIn = srcUShortAdr + j * srcStrideForUShort;
                                        var buffOut = dstByteAdr + j * dstStride;
                                        var u = 0;
                                        for (var v = 0; v < w; v++)
                                        {
                                            var s = buffIn[v];
                                            buffOut[u++] = (byte)(s << 3 | 4);
                                        }
                                    });
                                break;
                            case PixelFormat.Format16bppRgb565:
                                Parallel.For(0, h,
                                    j =>
                                    {
                                        var buffIn = srcUShortAdr + j * srcStrideForUShort;
                                        var buffOut = dstByteAdr + j * dstStride;
                                        var u = 0;
                                        for (var v = 0; v < w; v++)
                                        {
                                            var s = buffIn[v];
                                            buffOut[u++] = (byte)((0xFFC & (s >> 3)) | 2); // ビット数の多い G を採用
                                        }
                                    });
                                break;
                            case PixelFormat.Format1bppIndexed:
                                fixed (byte* p0 = palette)
                                {
                                    var pallet0 = p0;
                                    var remainder = (w & 7);
                                    var eightOdd = remainder != 0;
                                    var stop = w >> 3;
                                    Parallel.For(0, h, j =>
                                    {
                                        var buffIn = srcByteAdr + j * srcStride;
                                        var buffOut = dstByteAdr + j * dstStride;
                                        var u = 0;
                                        for (var v = 0; v < stop; v++)
                                        {
                                            var b = buffIn[v];
                                            var c = pallet0 + 3 * (b >> 7);
                                            buffOut[u++] = c[0];
                                            c = pallet0 + 3 * ((b >> 6) & 1);
                                            buffOut[u++] = c[0];
                                            c = pallet0 + 3 * ((b >> 5) & 1);
                                            buffOut[u++] = c[0];
                                            c = pallet0 + 3 * ((b >> 4) & 1);
                                            buffOut[u++] = c[0];
                                            c = pallet0 + 3 * ((b >> 3) & 1);
                                            buffOut[u++] = c[0];
                                            c = pallet0 + 3 * ((b >> 2) & 1);
                                            buffOut[u++] = c[0];
                                            c = pallet0 + 3 * ((b >> 1) & 1);
                                            buffOut[u++] = c[0];
                                            c = pallet0 + 3 * (b & 1);
                                            buffOut[u++] = c[0];
                                        }
                                        if (eightOdd)
                                        {
                                            var b = buffIn[stop];
                                            var shift = 7;
                                            for (var i = 0; i < remainder; i++)
                                            {
                                                var c = pallet0 + 3 * ((b >> shift--) & 1);
                                                buffOut[u++] = c[0];
                                            }
                                        }
                                    });
                                }
                                break;
                            case PixelFormat.Format48bppRgb:
                                buffInLength = buffInSize >> 1;
                                Parallel.For(0, h,
                                    j =>
                                    {
                                        var buffIn = srcUShortAdr + j * srcStrideForUShort;
                                        var buffOut = dstByteAdr + j * dstStride;
                                        int u = 0, v = 0;
                                        while (u < buffInLength)
                                        {
                                            int value;
                                            value = buffIn[v]; v += 3;
                                            buffOut[u++] = value <= 0 ? (byte)0 : value >= 8192 ? (byte)255 : ToBitmap24_RGBLookUp[value];
                                        }
                                    });
                                break;
                            case PixelFormat.Format4bppIndexed:
                                fixed (byte* p0 = palette)
                                {
                                    var pallet0 = p0;
                                    var odd = (w & 1) == 1;
                                    var stop = w >> 1;
                                    Parallel.For(0, h, j =>
                                    {
                                        var buffIn = srcByteAdr + j * srcStride;
                                        var buffOut = dstByteAdr + j * dstStride;
                                        var u = 0;
                                        for (var v = 0; v < stop; v++)
                                        {
                                            var b = buffIn[v];
                                            var c = pallet0 + 3 * (b >> 4);
                                            buffOut[u++] = c[0];
                                            c = pallet0 + 3 * (b & 0xF);
                                            buffOut[u++] = c[0];
                                        }
                                        if (odd)
                                        {
                                            var b = buffIn[stop];
                                            var c = pallet0 + 3 * (b >> 4);
                                            buffOut[u++] = c[0];
                                        }
                                    });
                                }
                                break;
                            case PixelFormat.Format64bppArgb:
                                buffInLength = buffInSize >> 1;
                                Parallel.For(0, h,
                                    j =>
                                    {
                                        var buffIn = srcUShortAdr + j * srcStrideForUShort;
                                        var buffOut = dstByteAdr + j * dstStride;
                                        int u = 0, v = 0;
                                        while (v < buffInLength)
                                        {
                                            var a = (buffIn[v + 3] + 16) >> 5;
                                            var ar = (255 - a) * 255 + 127;
                                            int value;
                                            value = buffIn[v]; v += 4;
                                            value = value <= 0 ? 0 : value >= 8192 ? 255 : ToBitmap24_RGBLookUp[value];
                                            value = (a * value + ar) / 255; // a < 0 になりうるので value も範囲外の可能性がある
                                            buffOut[u++] = (byte)(value <= 0 ? 0 : value >= 255 ? 255 : value);
                                        }
                                    });
                                break;
                            case PixelFormat.Format64bppPArgb:
                                buffInLength = buffInSize >> 1;
                                Parallel.For(0, h,
                                    j =>
                                    {
                                        var buffIn = srcUShortAdr + j * srcStrideForUShort;
                                        var buffOut = dstByteAdr + j * dstStride;
                                        int u = 0, v = 0;
                                        while (v < buffInLength)
                                        {
                                            var ar = 255 - ((buffIn[v + 3] + 16) >> 5);
                                            int value;
                                            value = buffIn[v]; v += 4;
                                            value = value <= 0 ? 0 : value >= 8192 ? 255 : ToBitmap24_RGBLookUp[value];
                                            value = value + ar; // 過多のチェックは当然。a < 0 の可能性より過小のチェックも必要
                                            buffOut[u++] = (byte)(value <= 0 ? 0 : value >= 255 ? 255 : value);
                                        }
                                    });
                                break;
                            case PixelFormat.Format8bppIndexed:
                                fixed (byte* p0 = palette)
                                {
                                    var pallet0 = p0;
                                    Parallel.For(0, h, j =>
                                    {
                                        var buffIn = srcByteAdr + j * srcStride;
                                        var buffOut = dstByteAdr + j * dstStride;
                                        var u = 0;
                                        for (var v = 0; v < w; v++)
                                        {
                                            var c = pallet0 + 3 * buffIn[v];
                                            buffOut[u++] = c[0];
                                        }
                                    });
                                }
                                break;
                            case PixelFormat_Format32bppCMYK:
                                Parallel.For(0, h,
                                    j =>
                                    {
                                        var buffIn = srcByteAdr + j * srcStride;
                                        var buffOut = dstByteAdr + j * dstStride;
                                        int u = 0, v = 0;
                                        while (v < buffInSize)
                                        {
                                            var c = (int)buffIn[v]; v += 3;
                                            var k = (int)buffIn[v++];

                                            var kr = 255 - k;
                                            buffOut[u++] = (byte)(255 - ((c * kr + 127) / 255 + k));
                                        }
                                    });
                                break;
                        }
                    }
                    finally
                    {
                        bmp.UnlockBits(srcData);
                    }
                    if (!leaveOpenOriginal) bmp.Dispose();
                    return dst;
                }
                finally
                {
                    dst.UnlockBits(dstData);
                }
            }
            catch
            {
                dst.Dispose();
                throw;
            }
        }

        /*
        /// <summary>
        /// img を型変換だけするか、img を Dispose して別に 24 bit の Bitmap を返します
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        private Bitmap ImageToBitmap24(Image img)
        {
            var bmp = img as Bitmap;
            if(bmp == null)
            {
                bmp = new Bitmap(img);
                img.Dispose();
                return ImageToBitmap24(bmp);
            }
            if (bmp.PixelFormat == PixelFormat.Format24bppRgb)
            {
                return bmp;
            }
            var w = img.Width;
            var h = img.Height;
            var dst = new Bitmap(w, h, PixelFormat.Format24bppRgb);
            try
            {
                // 並列で自前実装。最も速い
                if (
                    bmp.PixelFormat == PixelFormat.Format32bppRgb ||
                    bmp.PixelFormat == PixelFormat.Format32bppArgb ||
                    bmp.PixelFormat == PixelFormat.Format32bppPArgb)
                {
                    var rect = new Rectangle(0, 0, w, h);
                    var dstData = dst.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                    try
                    {
                        var srcData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
                        var dstScan0 = dstData.Scan0;
                        var dstStride = dstData.Stride;
                        var srcScan0 = srcData.Scan0;
                        var srcStride = srcData.Stride;
                        var buffSize = 4 * w;
                        var buffOutSize = 3 * w;
                        try
                        {
                            if (bmp.PixelFormat == PixelFormat.Format32bppRgb)
                            {
                                // LockBits 時にピクセルフォーマットを変換した場合
                                // この部分は高速だが、LockBits に時間がかかる
                                // やはり並列の自前実装が最も速い。LockBits の２倍近くの速さ。
                                // なおアルファがあるものはそもそも LockBits では背景が黒くなるので
                                // 議論の意味がない
                                Parallel.For(0, h,
                                    () => new byte[buffSize],
                                    (j, state, buff) =>
                                    {
                                        System.Runtime.InteropServices.Marshal.Copy(srcScan0 + j * srcStride, buff, 0, buffSize);
                                        int u = 0, v = 0;
                                        while (v < buffSize)
                                        {
                                            buff[u++] = buff[v++];
                                            buff[u++] = buff[v++];
                                            buff[u++] = buff[v];
                                            v += 2;
                                        }
                                        System.Runtime.InteropServices.Marshal.Copy(buff, 0, dstScan0 + j * dstStride, buffOutSize);
                                        return buff;
                                    }, buff => { });
                            }
                            else if (bmp.PixelFormat == PixelFormat.Format32bppArgb)
                            {
                                //var sw = new System.Diagnostics.Stopwatch();
                                //sw.Start();
                                Parallel.For(0, h,
                                    () => new byte[buffSize],
                                    (j, state, buff) =>
                                    {
                                        System.Runtime.InteropServices.Marshal.Copy(srcScan0 + j * srcStride, buff, 0, buffSize);
                                        int u = 0, v = 0;
                                        while (v < buffSize)
                                        {
                                                


                                                // 0.10 秒
                                                var a = (int)buff[v + 3];
                                            var ar = 255 - a; ar = ar * 255 + 127;
                                            buff[u++] = (byte)((a * buff[v++] + ar) / 255);
                                            buff[u++] = (byte)((a * buff[v++] + ar) / 255);
                                            buff[u++] = (byte)((a * buff[v] + ar) / 255);
                                            v += 2;
                                        }
                                        System.Runtime.InteropServices.Marshal.Copy(buff, 0, dstScan0 + j * dstStride, buffOutSize);
                                        return buff;
                                    }, buff => { });
                                //sw.Stop(); MessageBox.Show($"{sw.Elapsed}");
                            }
                            else
                            {
                                Parallel.For(0, h,
                                    () => new byte[buffSize],
                                    (j, state, buff) =>
                                    {
                                        System.Runtime.InteropServices.Marshal.Copy(srcScan0 + j * srcStride, buff, 0, buffSize);
                                        int u = 0, v = 0;
                                        while (v < buffSize)
                                        {
                                            var a = 255 - buff[v + 3];
                                            buff[u++] = (byte)(buff[v++] + a);
                                            buff[u++] = (byte)(buff[v++] + a);
                                            buff[u++] = (byte)(buff[v] + a);
                                            v += 2;
                                        }
                                        System.Runtime.InteropServices.Marshal.Copy(buff, 0, dstScan0 + j * dstStride, buffOutSize);
                                        return buff;
                                    }, buff => { });
                            }
                        }
                        finally
                        {
                            bmp.UnlockBits(srcData);
                        }
                    }
                    finally
                    {
                        dst.UnlockBits(dstData);
                    }
                    img.Dispose();
                    return dst;
                }

                // アルファを持たないかインデックス付きなら LockBits の変換を利用。
                // インデックス付きは以下のようにグラフィックが使えないのでいずれにしても背景は白くならない
                // http://dobon.net/vb/bbs/log3-49/29128.html
                if (!((img.PixelFormat & PixelFormat.Alpha) == PixelFormat.Alpha ||
                    (img.PixelFormat & PixelFormat.PAlpha) == PixelFormat.PAlpha) ||
                    (img.PixelFormat & PixelFormat.Indexed) == PixelFormat.Indexed)
                {
                    var result = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format24bppRgb);
                    var data = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                    try
                    {
                        var buff = new byte[data.Height * data.Stride];
                        var resultData = result.LockBits(new Rectangle(Point.Empty, bmp.Size),
                            ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                        try
                        {
                            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buff, 0, buff.Length);
                            System.Runtime.InteropServices.Marshal.Copy(buff, 0, resultData.Scan0, buff.Length);
                        }
                        finally
                        {
                            result.UnlockBits(resultData);
                        }
                    }
                    finally
                    {
                        bmp.UnlockBits(data);
                    }
                    bmp.Dispose();
                    return result;
                }

                // 残りは Graphics を使う
                {
                    var length = w * 3;
                    var pallet = new byte[length];
                    for (var i = 0; i < pallet.Length; i++) pallet[i] = 255;
                    var rect = new Rectangle(0, 0, w, h);
                    var dstData = dst.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                    try
                    {
                        var pnt = dstData.Scan0;
                        var stride = dstData.Stride;
                        Parallel.For(0, h, y =>
                        {
                            System.Runtime.InteropServices.Marshal.Copy(pallet, 0, pnt + y * stride, length);
                        });
                    }
                    finally
                    {
                        dst.UnlockBits(dstData);
                    }

                    using (var g = Graphics.FromImage(dst))
                    {
                        g.DrawImageUnscaled(img, 0, 0);
                    }
                    img.Dispose();
                    return dst;
                }
            }
            catch
            {
                dst.Dispose();
                throw;
            }
        }
        */

        private Size GetResizedSize(Size originalSize)
        {
            var viewSize = ViewSize;

            var error = originalSize.Width <= 0;

            var oWidth = Math.Abs(originalSize.Width);
            var oHeight = Math.Abs(originalSize.Height);

            // 充填率を最大化する実装
            var sw = (ulong)oWidth;
            var sh = (ulong)oHeight;
            var dw = (ulong)ViewSize.Width;
            var dh = (ulong)ViewSize.Height;
            if (sw * dh > sh * dw)
            {
                if (ViewerMode != ViewerModeEnum.CoverSetting)
                {
                    // 分割
                    if (BindingMode != BindingMode.SinglePage && BindingMode != BindingMode.SinglePageWithoutScalingUp && MaxDivision > 1)
                    {
                        var n = sw * dh;
                        var d = dw * sh;
                        var r = n / d;
                        if (d * d * r * (r + 1) <= n * n)
                        {
                            r++;
                        }

                        viewSize.Width *= Math.Min(MaxDivision, (int)r);
                    }
                }
            }
            else
            {
                // 見開き
                var minPage = Math.Min(MinPageCountInWindow, MaxPageCountInWindow);
                if (minPage > 1)
                {
                    var n = dw * sh;
                    var d = sw * dh;
                    var r = n / d;
                    if (d * d * r * (r + 1) <= n * n)
                    {
                        r++;
                    }

                    viewSize.Width /= Math.Min(minPage, (int)r);
                }
            }

            // 強制見開きページ用
            /*
            var minPage = Math.Min(MinPageCountInWindow, MaxPageCountInWindow);
            if (minPage > 1)
            {
                viewSize.Width /= minPage;
            }
            else if(BindingMode != BindingMode.SinglePage && BindingMode != BindingMode.SinglePageWithoutScalingUp && MaxDivision > 1)
            {
                var a = (ulong)oWidth * (ulong)viewSize.Height;
                var b = (ulong)viewSize.Width * (ulong)oHeight;
                if (a * a > b * b * (ulong)MaxDivision)
                {
                    viewSize.Width *= MaxDivision;
                }

            }
            */
            

            Size result;

            if (oWidth * viewSize.Height > oHeight * viewSize.Width)
            {
                result = new Size(viewSize.Width, Math.Max(1, ((viewSize.Width * oHeight) + (oWidth >> 1)) / oWidth));
            }
            else
            {
                result = new Size(Math.Max(1, ((viewSize.Height * oWidth) + (oHeight >> 1)) / oHeight), viewSize.Height);
            }

            if (error || NoScaling(BindingMode))
            {
                return result;
            }
            else
            {
                if (result.Width >= oWidth && result.Height >= oHeight)
                {
                    result = new Size(oWidth, oHeight);
                }

                return result;
            }
        }

        int bmwLoadEachPage_EachRunWorkerCompleted_PrevPage = -1;
        private void bmwLoadEachPage_EachRunWorkerCompleted(object sender, GenerarClasses.BackgroundMultiWorker.EachRunWorkerCompletedEventArgs e)
        {
            Program.AlertError(e.Error);
            VirtualBitmapEx result;
            if (!e.Cancelled && (result = e.Result as VirtualBitmapEx) != null)
            {
                if (currentPage >= 0 && bmwLoadEachPage_EachRunWorkerCompleted_PrevPage != currentPage)
                {
                    SetBackgroundMode();
                }
                else
                {
                    SetBackgroundModeIfPausing();
                }
                bmwLoadEachPage_EachRunWorkerCompleted_PrevPage = currentPage;
                if (SetNewResizedImage(e.WorkNumber, result))
                {
                    if (currentPage <= e.WorkNumber && e.WorkNumber < currentPage + (onePageModeForNext ? 1 : MaxPageCountInWindow))
                    {
                        showCurrentPage(false);
                        //optionToolStripMenuItem.Text = $"page loaded: {MagnifierPhase2_Q}";
                    }
                }
                else
                {
                    var stop = Math.Min(ResizedImageArray.Length, currentPage + MaxPageCountInWindow);
                    var loadedRequired = true;
                    for (var i = Math.Max(0, currentPage - MaxPageCountInWindow); i < stop; i++)
                    {
                        if (ResizedImageArray[i] == null)
                        {
                            loadedRequired = false;
                            break;
                        }
                    }
                    if (loadedRequired) bmwLoadEachPage.ThreadCount = 0;
                }
            }
            else
            {
                bmwLoadEachPage_EachRunWorkerCompleted_PrevPage = -1;
            }

            if (SetCurrentImageLongPath_Que) SetCurrentImageLongPath();
        }

        private ulong usedMemory = 0;
        ulong BuiltInViewerMemoryLimit = ulong.MaxValue;
        ulong usedMemoryUBound = 0;

        //private bool is32bit = IntPtr.Size < 8;
        Microsoft.VisualBasic.Devices.ComputerInfo computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
        //private Process currentProcess = Process.GetCurrentProcess();
        private void SetMemoryUBound()
        {
            long memory;
            using (var mySelf = System.Diagnostics.Process.GetCurrentProcess())
            {
                memory = mySelf.WorkingSet64;
            }
            //optionToolStripMenuItem.Text = $"{memory}";
            var available = computerInfo.AvailablePhysicalMemory;
            //var userUbound = is32bit ? Math.Min(BuiltInViewerMemoryLimit, 3UL << 30) : BuiltInViewerMemoryLimit;
            var userUbound = GetUserMemoryUBound();
            if (available > userUbound) available = userUbound;
            var coef = ActiveForm == this ? 0.7 : 0.3;

            usedMemoryUBound = (ulong)(coef * usedMemory * (1 + (available / (ulong)memory)));
        }

        private ulong GetUserMemoryUBound_SystemUBound = 0;
        private ulong GetUserMemoryUBound()
        {
            if (GetUserMemoryUBound_SystemUBound <= 0)
            {
                GetUserMemoryUBound_SystemUBound = Math.Min(Environment.Is64BitProcess  ? (1UL << 60) : (3UL << 30), computerInfo.TotalPhysicalMemory);
            }
            return Math.Min(GetUserMemoryUBound_SystemUBound, BuiltInViewerMemoryLimit);
        }

        private bool SetNewResizedImage(int idx, VirtualBitmapEx image)
        {
            SetMemoryUBound();
            var size = GetImageSizeInByte(image);
            var existingImageCount = ReduceUsingMemory(usedMemoryUBound - Math.Min(usedMemoryUBound, size), idx); // 符号なしなので値が負になる計算を含まないように注意
            //var existingImageCount = ReduceUsingMemory(0, idx); // テスト用
            if (ResizedImageArray != null && (usedMemory + size <= usedMemoryUBound || existingImageCount < MaxPageCountInWindow * 4)) // 今＋前後それぞれ 1 ページ分は読んでおかないとページ送りチェックができない。後トラックバー。
            {
                ResizedImageArray[idx] = image;
                usedMemory += size;
                return true;
            }
            else
            {
                image.Dispose();
                bmwLoadEachPage.ReworkOrder(idx);
                return false;
            }
        }

        private int ReduceUsingMemory(ulong uBound, int protectedIndex = -1)
        {
            if (ResizedImageArray == null) return 0;
            var worksOrder = bmwLoadEachPage.WorksOrder;
            if (worksOrder == null) return ResizedImageArray != null ? ResizedImageArray.Length : 0;
            var protectedOrder = Array.IndexOf(worksOrder, protectedIndex);
            var disposeTarget = worksOrder.Length;
            var existingImageCount = ResizedImageArray.Count(img => img != null);
            var existingImageCountBound = (protectedIndex >= 0 ? -1 : 0) + MaxPageCountInWindow * 4;  // 今＋前後それぞれ 1 ページ分は読んでおかないとページ送りチェックができない。後トラックバー。
            while (usedMemory > uBound && existingImageCount > existingImageCountBound)
            {
                while (ResizedImageArray[worksOrder[--disposeTarget]] == null && disposeTarget >= 0/*&& disposeTarget > protectedOrder + 1*/) ;
                // protected というのは、次に挿入しようとしている画像。
                // これを消すとその分再び挿入することができて処理が繰り返しになってしまうことがある。
                // しかし、これを消してもなお挿入できないこともあって、その場合は protected よりも更に優先順位が高いものも消さないといけない
                // つまり、protected が disposeTarget になった場合、それを消した場合に再挿入されるかどうかを評価して、
                // 再挿入されない場合だけ実際に消す必要がある

                var idx = worksOrder[disposeTarget];


                var image = ResizedImageArray[idx];
                if (image == null) return existingImageCount;

                var newUsedMemory = usedMemory - GetImageSizeInByte(image);

                if (disposeTarget <= protectedOrder)
                {
                    // 外側の while 文より、ここで消すと再挿入の判定にページ数は使われない。
                    if (newUsedMemory <= uBound) return existingImageCount;


                    if (existingImageCount == existingImageCountBound + 1) return existingImageCount;
                }


                usedMemory = newUsedMemory;
                image.Dispose();
                ResizedImageArray[idx] = null;
                ResizedSizeArray[idx] = default(Size);
                existingImageCount--;
                bmwLoadEachPage.ReworkOrder(idx);

                LockManager.ForIReadOnlyList(PreFilteredImageArray, idx, item =>
                {
                    if (item != null)
                    {
                        item.Dispose();
                        PreFilteredImageArray[idx] = null;
                        OriginalImageInfoArray[idx] = null;
                    }
                });
            }
            return existingImageCount;
        }

        private ulong GetImageSizeInByte(VirtualBitmapEx image)
        {
            return (ulong)image.DataSizeInBytes;
            /*
            if (image.isEntity)
            {
                if (image.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    return (((ulong)image.Width + 3ul) & ~3ul) * (ulong)image.Height * (ulong)image.FrameCount;
                }
                else
                {
                    return (((ulong)image.Width * 3ul + 3ul) & ~3ul) * (ulong)image.Height * (ulong)image.FrameCount;
                }
            }
            else
            {
                return 0;
            }
            */
        }


        private void SetBackgroundModeIfPausing()
        {
            if (bmwLoadEachPage.ThreadCount == 0)
            {
                SetBackgroundMode();
            }
        }

        private void SetBackgroundMode()
        {
            if (ResizedImageArray == null) return;
            var length = ResizedImageArray.Length;
            var order = new int[length]; for (var i = 0; i < length; i++) order[i] = i;
            var clientCursorPosition = mtbPage.PointToClient(Cursor.Position);
            var onTrackbar = mtbPage.ClientRectangle.Contains(clientCursorPosition);
            var centerValue = onTrackbar ? mtbPage.PointToValue(clientCursorPosition) : currentPage;
            var stopPage = currentPage + MaxPageCountInWindow;

            Array.Sort(order, (a, b) =>
            {
                var aLevel = priorityLevel(a, stopPage);
                var bLevel = priorityLevel(b, stopPage);
                if (aLevel == bLevel)
                {
                    return Math.Abs(a - centerValue) - Math.Abs(b - centerValue);
                }
                else
                {
                    return aLevel - bLevel;
                }
            });
            bmwLoadEachPage.SetWorksOrder(order);
            ReduceUsingMemory(usedMemoryUBound);
            bmwLoadEachPage.ThreadCount = 1;
        }

        private int priorityLevel(int x, int stopPage)
        {
            var geCurrent = currentPage <= x;
            var lStop = x < stopPage;
            if (geCurrent && lStop)
            {
                return 0;
            }
            else if (!lStop && x < stopPage + MaxPageCountInWindow)
            {
                return 1;
            }
            else if (currentPage - MaxPageCountInWindow <= x && !geCurrent)
            {
                return 2;
            }
            else
            {
                return 3;
            }
        }

        private void bmwLoadEachPage_AllRunWorkersCompleted(object sender, GenerarClasses.BackgroundMultiWorker.AllRunWorkerCompletedEventArgs e)
        {
            Program.AlertError(e.Error);
        }

        private void fullScreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fullScreenModeChange(!FullScreen);
        }

        bool alwaysHideUI = false;
        bool AlwaysHideUI
        {
            get { return alwaysHideUI; }
            set
            {
                if (value != alwaysHideUI)
                {
                    alwaysHideUI = value;
                    alwaysAutoHideUIToolStripMenuItem.Checked = value;

                    if (!FullScreen)
                    {
                        HideUI = value;
                    }
                }
            }
        }
        bool hideUI = false;
        bool HideUI
        {
            get { return hideUI; }
            set
            {
                if (value != hideUI)
                {
                    hideUI = value;
                    SetHideUI();
                }
            }
        }

        void SetHideUI()
        {
            var temp = pbView_SizeChanged_SizeChanging;
            pbView_SizeChanged_SizeChanging = true;
            if (hideUI)
            {
                menuStrip_Bottom = menuStrip.Bounds.Bottom;
                menuStrip.Visible = false;
                //mtbPage_Top = pnlSeekbar.Bounds.Top;
                mtbPageTop_As_Reference_pbViewBottom = pnlSeekbar.Bounds.Top - pbView.Bottom;
                pnlSeekbar.Visible = false;
                statusStrip.Visible = false;


                var clientSize = ClientSize;
                pbView.Top = 0;
                pbView.Height = clientSize.Height;
                pbView.Left = 0;
                pbView.Width = clientSize.Width;
            }
            else
            {
                menuStrip_Bottom = 0;
                menuStrip.Visible = true;
                //mtbPage_Top = 0;
                mtbPageTop_As_Reference_pbViewBottom = 0;
                pnlSeekbar.Visible = true;
                pnlSeekbar_PreStatusStripVisibleTrue();
                statusStrip.Visible = true;
                mtbPage.ValueCanChangedByMouseButton = true;

                pbView.Top = menuStrip.Bottom;
                pbView.Height = pnlSeekbar.Top - pbView.Top;
            }
            pbView_SizeChanged_SizeChanging = temp;
            pbView_SizeChanged(null, null);
        }


        void fullScreenModeChange(bool fullScreen)
        {
            // fullscreen = true の二重がけは想定されていない。fullscreen = false は pbView の位置調整のために多重に呼び出されることがある
            if (fullScreen == true && FullScreen == true) return;

            var temp = pbView_SizeChanged_SizeChanging;
            pbView_SizeChanged_SizeChanging = true;
            if (fullScreen)
            {
                FullScreen = true;

                HideUI = true;
                
                closeWindowtoolStripMenuItem.Visible = true;
                menuStrip.Location = new Point(0, -closeWindowtoolStripMenuItem.Bounds.Y);
                fullScreenToolStripMenuItem.Text = Message.Window;
                
                pbView_SizeChanged_SizeChanging = temp;
                pbView_SizeChanged(null, null);
            }
            else
            {
                HideUI = AlwaysHideUI;

                if (ipcLookAheadInfo == null) FullScreen = false; // FullScreen = false が非表示を解除してしまうので
                else FullScreen_pseudoFullScreen = false;
                
                //menuStrip.Location = new Point(0, 0); // TopMost == true ならこちらでもよい
                menuStrip.Bounds = new Rectangle(0, 0, ClientRectangle.Width, menuStrip.Bounds.Height);
                
                pbView.Left = 0;
                //pbView.Width = Width; // 起動直後は Width の値が正しくないのでこれは不適切

                // 実装変更により不要に
                //pbView.Width = ClientRectangle.Width;

                closeWindowtoolStripMenuItem.Visible = false;


                fullScreenToolStripMenuItem.Text = Message.Fullscreen;
                pbView_SizeChanged_SizeChanging = temp;
                pbView_SizeChanged(null, null);

                ViewerForm_SizeChanged_CurrentSize = Size;


                // if (ShowIconRequired)// ウインドウの状況によっては毎回必要なので条件はコメントアウト
                if (ipcLookAheadInfo == null) // 代わりの条件
                {
                    //ShowIconRequired = false;
                    ShowIcon = false;
                    ShowIcon = true;
                }
            }
        }

        private bool SaveSettings()
        {
            try
            {
                var fullscreen = FullScreen;
                var windowState = WindowState;
                var maxmized = (fullscreen ? prevFormWindowState : windowState == FormWindowState.Minimized ? ViewerForm_SizeChanged_BeforeMinimizedState : windowState) == FormWindowState.Maximized;

                var config = new ViewerFormConfig();
                config.FullScreen = fullscreen;
                config.Maximized = maxmized;
                if (!fullscreen && !maxmized && windowState == FormWindowState.Normal)
                {
                    config.Size  = Size;
                    config.Point = Location;
                }
                else
                {
                    /*
                    if (!fullscreen && maxmized && betterFormRestoreBound is BetterFormRestoreBounds bfrb)
                    {
                        var bound = bfrb.BetterRestoreBounds;
                        config.Point = bound.Location;
                        config.Size = bound.Size;
                    }
                    else
                    */
                    {
                        var bounds = BetterFormRestoreBounds.GetMovedWindowBounds(this, normalStateLocation ?? Location, prevSize, betterFormRestoreBound.BoundsLastNoMinimized);
                        config.Point = bounds.Location;
                        config.Size = bounds.Size;


                        /*
                    if (fullscreen && maxmized && normalStateLocation is Point nsl)
                    {
                        var workingArea = Screen.FromControl(this).WorkingArea;
                        var rect = new Rectangle(nsl, prevSize);
                        var nslWorkingArea = Screen.FromRectangle(rect).WorkingArea;
                        if (workingArea != nslWorkingArea)
                        {
                            rect.X += workingArea.X - nslWorkingArea.X;
                            rect.Y += workingArea.Y - nslWorkingArea.Y;
                            rect = ViewerForm.Pack(rect, workingArea);
                        }
                        config.Point = rect.Location;
                        config.Size = rect.Size;
                    }
                    else
                    {
                        config.Point = normalStateLocation == null ? Location : (Point)normalStateLocation;
                        //config.Size = normalStateSize == null ? Size : (Size)normalStateSize; //prevSize;
                        config.Size = prevSize;
                    }
                    */
                    }

                }

                if (!fullscreen && (/*WindowState == FormWindowState.Normal ||*/ WindowState == FormWindowState.Maximized))
                {
                    //var pbViewSize = pbView.Size
                    var pbViewSize = pbView.Bounds;
                    //var bounds = Screen.PrimaryScreen.Bounds;
                    var bounds = Screen.FromControl(this).Bounds;

                    config.MaximizedSizeWidthMargin = bounds.Width - pbViewSize.Width;
                    if (HideUI)
                    {
                        // 非表示のコントロールのサイズは取得できないので menuStrip_Bottom などを使う
                        //var mtbPage_Top = pbView.Bottom + mtbPageTop_As_Reference_pbViewBottom;
                        config.MaximizedSizeHeightMargin = bounds.Height - pbViewSize.Height + (menuStrip_Bottom /*+ pbViewSize.Height*/ - (pbViewSize.Top + mtbPageTop_As_Reference_pbViewBottom));
                        config.MaximizedSizeHeightMarginInHideMode = bounds.Height - pbViewSize.Height;
                    }
                    else
                    {
                        config.MaximizedSizeHeightMargin = bounds.Height - pbViewSize.Height;
                        config.MaximizedSizeHeightMarginInHideMode = bounds.Height - pbViewSize.Height - (menuStrip.Bottom + statusStrip.Bottom - pnlSeekbar.Top);
                    }
                }

                config.NormalScalingAlgorithm = StandardScalingAlgorithm.ScaleDown;
                config.UseAreaAverageWhenNormalUpsizing = useAreaAverageWhenUpsizingNormalToolStripMenuItem.Checked;
                config.ResizeGammaNormal = convert(ResizeGammaNormal);
                config.MagnifierScalingAlgorithm = MagnifierScalingAlgorithm.ScaleDown;
                config.UseAreaAverageWhenMagnifierUpsizing = useAreaAverageWhenUpsizingToolStripMenuItem.Checked;
                config.ResizeGammaMagnifier = convert(ResizeGammaMagnifier);

                config.MouseGestureEnabled = mgView.Enabled;
                config.MouseGestureLineWidth = mgView.GetWidth();
                config.MouseGestureLineColor = mgView.GetColor();
                config.MouseGestureCommands = mgView.Actions != null && mgView.Actions.Length > 0 ?
                    (from a in mgView.Actions select new MouseGestureCommand() { Gesture = a.Gesture, Command = (Command)a.Key }).ToArray() : null;
                config.KeyboardShortcutCommands = ksViewForm.Actions != null && ksViewForm.Actions.Length > 0 ?
                     (from a in ksViewForm.Actions select new KeyboardShortcutCommand(a)).ToArray() : null;
                config.SeekBarMode = SeekBarMode;
                config.SlideshowInterval = tmSlideshow.Interval;
                config.SlideshowRepeat = SlideshowRepeat;
                config.SlideshowGlobal = SlideshowGlobal;
                config.ImageFilter = imageFilter;

                config.MaxDivision = MaxDivision;
                config.MinPageCountInWindow = MinPageCountInWindow;

                config.CoverBindingMode = CoverBindingMode;
                config.ArchivesInArchiveMode = ArchivesInArchiveMode;
                config.BackColor = pbView.BackColor;

                config.AlwaysHideUI = AlwaysHideUI;

                config.ViewSettingHintMode = ViewSettingHintMode;

                config.Save();

                if (saveToHistory)
                {
                    trySaveCurrentConditionToHistory(SelfLastAccessTimeUpdateMode.Force);
                }

                return true;
            }
            catch (Exception error)
            {
                Program.AlertError(error);
                return false;
            }
        }

        private static double convert(GammaConversion gamma)
        {
            switch (gamma)
            {
                case GammaConversion.Value2_2: return 2.2;
                default: return 1.0;
            }
        }
        private static GammaConversion convert(double gamma)
        {
            const double sqrt22 = 1.4832396974191325897422794881601426121959808638195;
            return gamma < sqrt22 ? GammaConversion.Value1_0 : GammaConversion.Value2_2;
        }

        private bool checkNgen = false;

        private bool PseudoMaximized = false;
        private Size PseudoMaximizedSize;
        private enum LoadSettings_Result { ConfigFileNotFound, ConfigLoadError, Completed }
        FormWindowState? LoadSettings_FormWindowStateFromSetteingFile = null;
        //Point? LoadSettings_LoadedLocation = null;
        DateTime LoadSettings_LoadedFileTimeStampForLookAhead = DateTime.MinValue;
        DateTime? LoadSettings_LoadedFileTimeStampForLookAhead_Setter = null;
        private ViewerFormConfig LoadSettings_LastLoadedConfig = null;
        private GeneralConfig LoadSettings_LastLoadedGeneralConfig = null;
        private LoadSettings_Result LoadSettings(CommandLineOptionInfo info, bool reloadConfig, bool reloadLang)// InitialFullscreenMode initialFullscreenMode, bool? initialAlwaysHide, Color? backColor, ArchivesInArchiveMode? aia)
        {
            bool fullscreen;
            bool maximized;
            Point? point;
            Size size, maximizedSize;
            bool mouseGestureEnabled;
            bool alwaysHide;
            double mouseGestureLineWidth;
            Color mouseGestureLineColor;
            SeekBarMode seekBarMode;
            MouseGestureCommand[] mgCommands;
            KeyboardShortcutCommand[] ksCommands;
            Rectangle? currentScreen = null;
            try
            {
                var independent = ipcLookAheadInfo == null;
                if (!independent)
                {
                    LoadSettings_LoadedFileTimeStampForLookAhead = LoadSettings_LoadedFileTimeStampForLookAhead_Setter ?? Configuration.GetLastWriteTime();
                    LoadSettings_LoadedFileTimeStampForLookAhead_Setter = null;
                }
                ViewerFormConfig config;
                if (reloadConfig || LoadSettings_LastLoadedConfig == null)
                {
                    config = new ViewerFormConfig();
                    LoadSettings_LastLoadedConfig = config;
                }
                else
                {
                    config = LoadSettings_LastLoadedConfig;
                }
                var initialFullscreenMode = info?.InitialFullscreenMode ?? InitialFullscreenMode.Default;
                fullscreen = initialFullscreenMode == InitialFullscreenMode.ForceFullscreen ? true : initialFullscreenMode == InitialFullscreenMode.ForceWindow ? false : config.FullScreen;
                var initialAlwaysHide = info?.AlwaysHideUI;
                alwaysHide = initialAlwaysHide != null ? (bool)initialAlwaysHide: config.AlwaysHideUI;
                maximized = config.Maximized;
                point = config.Point;
                size = config.Size; if (size == Size.Empty) size = Size;
                if (config.MaximizedSizeWidthMargin <= 0 || config.MaximizedSizeHeightMargin <= 0 || config.MaximizedSizeHeightMarginInHideMode <= 0)
                {
                    maximizedSize = pbView.Size;
                }
                else
                {
                    currentScreen = point != null ? Screen.FromRectangle(new Rectangle((Point)point, size)).Bounds : Screen.FromControl(this).Bounds;
                    var bounds = (Rectangle)currentScreen;
                    int maximizedHeight;
                    if (!alwaysHide)
                    {
                        maximizedHeight = bounds.Height - config.MaximizedSizeHeightMargin;
                    }
                    else
                    {
                        maximizedHeight = bounds.Height - config.MaximizedSizeHeightMarginInHideMode;
                    }
                    maximizedSize = new Size(bounds.Width - config.MaximizedSizeWidthMargin, maximizedHeight);
                    if (maximizedSize.Width <= 0 || maximizedSize.Height <= 0) maximizedSize = pbView.Size;
                }

                StandardScalingAlgorithm.ScaleDown = config.NormalScalingAlgorithm;
                if (config.UseAreaAverageWhenNormalUpsizing)
                {
                    useAreaAverageWhenUpsizingNormalToolStripMenuItem.Checked = true;
                    StandardScalingAlgorithm.ScaleUp = ScalingAlgorithm.AreaAverage;
                }
                else
                {
                    useAreaAverageWhenUpsizingNormalToolStripMenuItem.Checked = false;
                    StandardScalingAlgorithm.ScaleUp = config.NormalScalingAlgorithm;
                }
                MagnifierScalingAlgorithm.ScaleDown = config.MagnifierScalingAlgorithm;
                if (config.UseAreaAverageWhenNormalUpsizing)
                {
                    useAreaAverageWhenUpsizingToolStripMenuItem.Checked = true;
                    MagnifierScalingAlgorithm.ScaleUp = ScalingAlgorithm.AreaAverage;
                }
                else
                {
                    useAreaAverageWhenUpsizingToolStripMenuItem.Checked = false;
                    MagnifierScalingAlgorithm.ScaleUp = config.MagnifierScalingAlgorithm;
                }
                ResizeGammaNormal = convert(config.ResizeGammaNormal);
                ResizeGammaMagnifier = convert(config.ResizeGammaMagnifier);

                mouseGestureEnabled = config.MouseGestureEnabled;
                mouseGestureLineWidth = config.MouseGestureLineWidth;
                mouseGestureLineColor = config.MouseGestureLineColor;
                mgCommands = config.MouseGestureCommands;
                ksCommands = config.KeyboardShortcutCommands;
                seekBarMode = config.SeekBarMode;
                tmSlideshow.Interval = config.SlideshowInterval;
                SlideshowRepeat = config.SlideshowRepeat;
                SlideshowGlobal = config.SlideshowGlobal;
                if(info?.OpenInPreviousImageFilterSetting != false) imageFilter.Set(config.ImageFilter);
                initialViewerFormImageFilter = config.ImageFilter;

                requestedBindingMode = info?.DefaultBindingMode;
                
                pbView.BackColor = info?.BackColor ?? config.BackColor;
                
                ArchivesInArchiveMode = info?.ArchivesInArchiveMode ?? config.ArchivesInArchiveMode;

                ReadOnMemoryMode = info?.ReadOnMemoryMode ?? config.ReadOnMemoryMode;
                
                CoverBindingMode = info?.CoverBindingMode ?? config.CoverBindingMode;

                ViewSettingHintMode = config.ViewSettingHintMode;

                MaxDivision = Math.Max(1, Math.Min(2, config.MaxDivision));
                MinPageCountInWindow = Math.Max(1, Math.Min(2, config.MinPageCountInWindow));
                
                GeneralConfig generalConfig;
                if (reloadConfig || LoadSettings_LastLoadedGeneralConfig == null)
                {
                    generalConfig = new GeneralConfig();
                    LoadSettings_LastLoadedGeneralConfig = generalConfig;
                }
                else
                {
                    generalConfig = LoadSettings_LastLoadedGeneralConfig;
                }
                
                BuiltInViewerMemoryLimit = generalConfig.BuiltInViewerMemoryLimit;
                checkNgen = generalConfig.CheckNgen;

                if (reloadLang)
                {
                    try
                    {
                        var newLanguage = Message.CultureInfoParse(generalConfig.Language);
                        if (Message.CurrentLanguage != newLanguage)
                        {
                            Message.CurrentLanguage = newLanguage;
                        }
                    }
                    catch
                    {
                        try
                        {
                            if (Message.CurrentLanguage != Message.SystemLanguage)
                            {
                                Message.CurrentLanguage = Message.SystemLanguage;
                            }
                        }
                        catch
                        {
                            if (Message.CurrentLanguage != Message.DefaultLanguage)
                            {
                                Message.CurrentLanguage = Message.DefaultLanguage;
                            }
                        }
                    }

                }
            }
            catch (Exception error)
            {
                Program.AlertError(error);
                return LoadSettings_Result.ConfigLoadError;
            }

            // AlwaysHideUI の setter は fullscreen ではなく FullScreen を見るのでここでは使えない
            alwaysHideUI = alwaysHide;
            alwaysAutoHideUIToolStripMenuItem.Checked = alwaysHide;
            if (!alwaysHide && !fullscreen)
            {
                SetHideUI();
            }
            
            if (fullscreen) LoadSettings_FormWindowStateFromSetteingFile = maximized ? FormWindowState.Maximized : FormWindowState.Normal;


            LoadSettingToLoad_FormLocation = null;
            ViewerForm_SizeChanged_CurrentSize = Size.Empty;
            if (!fullscreen)
            {
                //normalStateSize = 

                //prevSize = Size = size;

                SetSizeWithPbSizeMemoryForMinimize(size);
                prevSize = size;

                if (!maximized) ViewerForm_SizeChanged_CurrentSize = size;
                if (point != null)
                {
                    var p = (Point)point;

                    // 最も近いスクリーンにパックする
                    var settingRect = new Rectangle(p, size);
                    settingRect = BetterFormRestoreBounds.Pack(settingRect, Screen.FromRectangle(settingRect).WorkingArea);
                    p = settingRect.Location;
                    normalStateLocation = p;

                    if (ViewerForm_Load_Called)
                    {
                        Location = p;
                    }
                    else
                    {
                        LoadSettingToLoad_FormLocation = p;
                    }
                    /*
                    SetOneTimeEventHandler(
                        () => Location = p,
                        eh => Load += eh,
                        eh => Load -= eh);
                        */
                }
            }
            else
            {
                //normalStateSize = size;
                prevSize = size;
                SetSizeWithPbSizeMemoryForMinimize(size);
                //Size = size;
                if (point != null)
                {
                    var p = (Point)point;
                    var settingRect = new Rectangle(p, size);
                    var rect = currentScreen ?? Screen.FromRectangle(settingRect).Bounds;
                    if (rect.IntersectsWith(settingRect))
                    //var rect = new Rectangle(p, size);
                    //if (Screen.AllScreens.Any(screen => screen.Bounds.IntersectsWith(rect)))
                    {
                        normalStateLocation = point;
                        if (ViewerForm_Load_Called)
                        {
                            Location = p;
                        }
                        else
                        {
                            LoadSettingToLoad_FormLocation = p;
                        }
                    }
                }

                //LoadSettings_FormWindowStateFromSetteingFile = maximized ? FormWindowState.Maximized : FormWindowState.Normal;
            }

            // ピクチャーボックスの位置調整などがあるので常に行う
            if (ViewerForm_Shown_Called)
            {
                LoadSettingToShown(fullscreen, maximized, maximizedSize, size);
            }
            else
            {
                LoadSettingToShown_Reserved = true;
                LoadSettingToShown_fullscreen = fullscreen;
                LoadSettingToShown_maximized = maximized;
                LoadSettingToShown_maximizedSize = maximizedSize;
                LoadSettingToShown_size = size;
            }

            mgView.Enabled = mouseGestureEnabled;
            mgView.Width = mouseGestureLineWidth;
            mgView.Color = mouseGestureLineColor;
            if (mgCommands != null)
            {
                var actions = new List<MouseGestureAction>();
                var template = getMouseGestureSettingTemplate(CommandSender.MouseGesture);
                foreach (var c in mgCommands)
                {
                    var gesture = c.Gesture;
                    if (gesture == null || gesture.Length == 0) continue;
                    var key = (int)c.Command;
                    var action = template.FirstOrDefault(t => t.Key == key);
                    if (action == default(MouseGestureSettingTemplate)) continue;
                    actions.Add(new MouseGestureAction(key, gesture, action.Action));
                }
                mgView.Actions = actions.ToArray();
            }
            else
            {
                mgView.Actions = null;
            }

            SeekBarMode = seekBarMode;

            // メニューへの反映は LoadSetting に引き続き呼び出される SetMessages で行われる
            if (ksCommands != null)
            {
                var actions = new List<KeyboardShortcutAction>();
                var template = getMouseGestureSettingTemplate(CommandSender.KeyboardShortcut);
                foreach (var c in ksCommands)
                {
                    var shortcut = c.GetShortcut();
                    if (shortcut == null) continue;
                    var command = c.Command;
                    var key = (int)command;
                    var action = template.FirstOrDefault(t => t.Key == key);
                    if (action == default(MouseGestureSettingTemplate)) continue;
                    actions.Add(new KeyboardShortcutAction(key, action.Action, ContinuousExecutionCommands.Contains(command), shortcut));
                }
                ksViewForm.Actions = actions.ToArray();
            }
            else
            {
                ksViewForm.Actions = null;
            }

            return LoadSettings_Result.Completed;
        }

        private void SetSizeWithPbSizeMemoryForMinimize(Size size)
        {
            if (Size != size)
            {
                Size = size;
            }
            else
            {
                ViewerForm_SizeChanged_pbBoundsMemoryForMinimize = pbView.Bounds;
            }
        }

        private Point? LoadSettingToLoad_FormLocation = null;

        private bool LoadSettingToShown_Reserved = false;
        private bool LoadSettingToShown_fullscreen;
        private bool LoadSettingToShown_maximized;
        private Size LoadSettingToShown_maximizedSize;
        private Size LoadSettingToShown_size;
        private void LoadSettingToShown(bool fullscreen, bool maximized, Size maximizedSize, Size size)
        {
            fullScreenModeChange(fullscreen);
            if (maximized && !fullscreen)
            {
                if (ipcLookAheadInfo != null)
                {
                    PseudoMaximized = true;
                    PseudoMaximizedSize = maximizedSize;
                }
                else
                {
                    WindowState = FormWindowState.Maximized;
                }
            }
            else
            {
                if (ipcLookAheadInfo != null)
                {
                    PseudoMaximized = false;
                    PseudoMaximizedSize = default(Size);
                }
                /*
                else
                {
                    WindowState = FormWindowState.Normal;
                }
                */
            }

            if (fullscreen && ipcLookAheadInfo == null)
            {
                prevSize = size;
            }
            else
            {
                if (fullscreen || maximized)
                {
                    prevSize = size;
                }
                else
                {
                    prevSize = Size.Empty;
                }
            }
        }

        private BetterFormRestoreBounds betterFormRestoreBound;

        bool ViewerForm_Shown_Called = false;
        bool ViewerForm_Shown_VisibleToFalse = false;
        private void ViewerForm_Shown(object sender, EventArgs e)
        {
            if (ViewerForm_Shown_VisibleToFalse)
            {
                Visible = false;
            }
            if (LoadSettingToShown_Reserved)
            {
                LoadSettingToShown_Reserved = false;
                LoadSettingToShown(
                    LoadSettingToShown_fullscreen,
                    LoadSettingToShown_maximized,
                    LoadSettingToShown_maximizedSize,
                    LoadSettingToShown_size);
            }

            betterFormRestoreBound = new BetterFormRestoreBounds(this);

            Configuration.ClosePrepareMode(); // Visible = false の先読みプロセスでも起動時にここに到達することを確認済み
            

            ViewerForm_Shown_Called = true;
        }

        public static void SetOneTimeEventHandler(EventHandler eventHandler, Action<EventHandler> adder, Action<EventHandler> remover) 
        {
            EventHandler withRemover = null;
            withRemover = (sender, e) =>
            {
                eventHandler(sender, e);
                remover(withRemover);
            };
            adder(withRemover);
        }

        public static void SetOneTimeEventHandler(Action eventHandler, Action<EventHandler> adder, Action<EventHandler> remover)
        {
            EventHandler withRemover = null;
            withRemover = (sender, e) =>
            {
                eventHandler();
                remover(withRemover);
            };
            adder(withRemover);
        }

        private void ViewerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (ipcLookAheadInfo == null && !DoNotSaveSetting)
            {
                SaveSettings();
            }
        }
        
        private void trySaveCurrentConditionToHistory(SelfLastAccessTimeUpdateMode m)
        {
            try
            {
                //VirtualFolder.AddBookmarkData(Program.HistorySorPath, currentArchiveFilePath, Math.Max(0,currentPage), limitOfItemsCount: Program.GetLimitOfHistoryCount(), deleteLostPath: true);
                //SendSorMessageToCatalogForm(currentArchiveFilePath, Program.HistorySorPath, Math.Max(0, currentPage), requestToUpdateLastAccessTime: true);

                //var page = currentStartingFilePath == currentArchiveFilePath ? Math.Max(0, currentPage) : -1;
                //VirtualFolder.AddBookmarkData(Program.HistorySorPath, currentStartingFilePath, page, limitOfItemsCount: Program.GetLimitOfHistoryCount(), deleteLostPath: true);
                //SendSorMessageToCatalogForm(currentStartingFilePath, Program.HistorySorPath, page, requestToUpdateLastAccessTime: true);

                var page = loader == null || loader.Type != PackedImageLoader.PackType.Directory ? Math.Max(0, currentPage) : -1;
                var targetPath = loader != null && loader.Type != PackedImageLoader.PackType.Directory ? currentArchiveFilePath : currentStartingFilePath;
                VirtualFolder.AddBookmarkData(Program.HistorySorPath, targetPath, page, limitOfItemsCount: Program.GetLimitOfHistoryCount(), deleteLostPath: true);
                SendSorMessageToCatalogForm(targetPath, Program.HistorySorPath, page, requestToUpdateLastAccessTime: true, m: m);
            }
            catch { }
        }

        //void f(object s, EventArgs e) { optionToolStripMenuItem.Text = pbView.Bounds.ToString(); }

        bool pbView_SizeChanged_SizeChanging = false;
        private void pbView_SizeChanged(object sender, EventArgs e)
        {
            //optionToolStripMenuItem.Text = pbView.Bounds.ToString();
            //pbView.LocationChanged -= f;
            //pbView.LocationChanged += f;

            var size = pbView.Bounds;
            if (size.Width > 0 && size.Height > 0)
            {
                ViewerForm_SizeChanged_pbBoundsMemoryForMinimize = size;

                if (!pbView_SizeChanged_SizeChanging && /*EntryNumberArray*/EntryArray != null && /*pbView.Width > 0 && pbView.Height > 0 &&*/
                    !FullScreen_pseudoFullScreen && !PseudoMaximized) // 最小化時にはリロードしない、擬似状態からの復帰時にはリロードしない
                {
                    Reload();
                }
            }
        }

        bool mtbPage_ValueChanged_SendRequestToChangeSelection_Stop = false;
        private void mtbPage_ValueChanged(object sender, EventArgs e)
        {
            if (mtbPage.Value != currentPage)
            {
                currentPage = mtbPage.Value;
                if (!mtbPage_ValueChanged_SendRequestToChangeSelection_Stop) showCurrentPage();
            }

            if (!mtbPage_ValueChanged_SendRequestToChangeSelection_Stop)
            {
                var elp = EntryLongPathArray;
                var ld = loader;
                if (elp != null && currentPage >= 0 && currentPage < elp.Length && ld != null)
                {
                    var path = elp[currentPage];
                    if (ld.Type == PackedImageLoader.PackType.Directory)
                    {
                        path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                    }

                    if (!string.IsNullOrEmpty(path))
                    {
                        SendRequestToChangeSelection(path);
                    }
                }
            }
            
            //ShowCurrentPageToStatusBar();
            if (ipcLookAheadInfo == null) SetCurrentImageLongPath(); // ページ番号の描画よりも後で呼び出す
        }
        
        private void ShowCurrentPageToStatusBar()
        {
            var oiia = OriginalImageInfoArray;
            int i;
            if (oiia != null && (i = mtbPage.Value) >= 0 && i < oiia.Length)
            //if (!string.IsNullOrEmpty(currentArchiveFilePath))
            {
                var imgInfo = oiia[i];
                Size s;
                if (imgInfo != null)
                {
                    try
                    {
                        s = imgInfo.Size;
                    }
                    catch // 関数が呼び出された後、ここに到達する前に img が Dispose された場合
                    {
                        s = Size.Empty;
                    }
                }
                else
                {
                    s = Size.Empty;
                }
                pageToolStripStatusLabel.Text =$" {i + 1} / {mtbPage.Maximum + 1}";
                imageSizeToolStripStatusLabel.Text = s.Width <= 0 || s.Height <= 0 ? "?x?" : $"{s.Width}x{s.Height}";
            }
            else
            {
                pageToolStripStatusLabel.Text = null;
                imageSizeToolStripStatusLabel.Text = null;
            }
        }

        Tuple<int, int, int, int, Size[]> bwMagnifierMaker_Q = null;
        bool showCurrentPage_PagePrepared = false;
        Size showCurrentPage_PrevDrawSize = Size.Empty;
        int shownImegesPage = -1;
        private void showCurrentPage(bool setBackgroundModeIfPausing = true)
        {
            if (ResizedImageArray == null) return;

            if (setBackgroundModeIfPausing) SetBackgroundModeIfPausing();


            if (pbView_SizeChanged_SizeChanging) return;

            var magNotMaked = !MagnifierCanvasPrepared();
            
            if (MagnifierPhase1_Q)
            {
                MagnifierPhase1_Q = false;
                MagnifierPhase1(MagnifierPhase1_QValue, ifNoopThenQ: true);
                //optionToolStripMenuItem.Text = $"phase1 by Q {MagnifierPhase1_Q}";
            }
            else if (ViewerMode == ViewerModeEnum.Magnifier && magNotMaked)
            {
                pbView_Paint_Rectangle = Rectangle.Empty;
                ViewerMode = ViewerModeEnum.Normal;
                MagnifierPhase1(GetMagnifierRectangle_LastCursorPosition, ifNoopThenQ: true);
                MagnifierPhase2_Q = true;
            }

            //if (ViewerMode == ViewerModeEnum.CoverSetting && pbView_Paint_Rectangle == Rectangle.Empty)
            if (ViewerMode == ViewerModeEnum.CoverSetting)
            {
                pbView_Paint_Rectangle = GetCoverRectangleForDisplay();
            }

            // 読み込みの表示を行うため画像が準備されているかどうかにかかわらず Paint する
            //if (currentPage < NextPage)
            {
                showCurrentPage_PagePrepared = true;
                //pbView.Invalidate(false); // 描画は Paint イベントハンドラで行われる
                pbPaintInvalidate();
            }

            //shownImegesPath = currentArchiveFilePath;
            shownImegesPath = currentStartingFilePath;

            shownImegesPage = currentPage;
            
            // 拡大モードの作成
            if (magNotMaked)
            {
                resetMagnifier();
            }
        }

        private Size getCurrentPbViewSize()
        {
            if (FullScreen_pseudoFullScreen)
            {
                return Screen.FromControl(this).Bounds.Size;
            }
            else if (PseudoMaximized)
            {
                return PseudoMaximizedSize;
            }
            else
            {
                return ViewerForm_SizeChanged_pbBoundsMemoryForMinimize.Size; //pbView.Size;
            }
        }

        /// <summary>
        /// ViewerMode == Normal の場合のサイズを返す
        /// </summary>
        /// <returns></returns>
        private Size? getCurrentVisibleImageSize(Size pbViewSize)
        {
            var totalWidth = 0;
            var startPage = currentPage;
            var stopPage = NextPage;
            var totalHeight = 0;
            for (var i = startPage; i < stopPage; i++)
            {
                var size = ResizedImageArray[i].Size;
                totalWidth += size.Width;
                if (size.Height > totalHeight)
                {
                    totalHeight = size.Height;
                }
            }

            var divisions = 1;
            var left = 0;
            var right = totalWidth;


            if (totalWidth <= 0)
            {
                if (MaxDivision > 1 && 0 <= startPage && startPage == stopPage && ResizedImageArray != null && ResizedImageArray.Length > startPage && getCurrentPageDivison() > 1)
                {
                    var img = ResizedImageArray[startPage];
                    if (img == null) return null;
                    var imageWidth = img.Width;
                    divisions = (imageWidth - 1) / pbViewSize.Width + 1;
                    if (divisions > MaxDivision) return null;
                    if (CurrentDividedPosition < 0 || CurrentDividedPosition >= divisions) return null;
                    stopPage = startPage + 1;
                    var p = BindingMode != BindingMode.RightToLeft ? CurrentDividedPosition : divisions - CurrentDividedPosition - 1;
                    left = (imageWidth * p + divisions / 2) / divisions;
                    right = (imageWidth * (p + 1) + divisions / 2) / divisions;
                    totalWidth = right - left;
                }
            }

            if (totalWidth <= 0 || totalHeight <= 0) return null;

            return new Size(totalWidth, totalHeight);
        }

        private double? getCurrentMagnifierPower()
        {
            var result = MagnifyingPower.GetValue();
            if (result != null) return result;

            var pbViewSize = getCurrentPbViewSize();
            var visibleSize = getCurrentVisibleImageSize(pbViewSize);
            if (visibleSize == null) return null;

            return magnifyingPower.GetPower(pbViewSize,(Size) visibleSize);
        }

        private void resetMagnifier()
        {
            var cPage = this.currentPage;
            var resizedImageArray = this.ResizedImageArray;
            var resizedSizeArray = this.ResizedSizeArray;
            var preFilteredImageArray = this.PreFilteredImageArray;
            if (cPage < 0 || resizedImageArray == null || resizedSizeArray == null || preFilteredImageArray == null) return;
            var arrayLength = resizedImageArray.Length;
            if (resizedSizeArray.Length != arrayLength || preFilteredImageArray.Length != arrayLength) return;
            if (cPage >= arrayLength || resizedImageArray[cPage] == null) return;
            var nextPage = NextPage;
            if (nextPage < cPage || nextPage == cPage && (MaxDivision <= 1 || getCurrentPageDivison() <= 1)) return;

            if (nextPage == cPage) nextPage++;
             var totalWidth = 0;
            var pageCount = nextPage - cPage;
            var zoomedSizeArray = new Size[pageCount];

            var pbViewHeight = FullScreen_pseudoFullScreen ? Screen.FromControl(this).Bounds.Height : PseudoMaximized ? PseudoMaximizedSize.Height : pbView.Height;
            var zoomedHeight = 0;
            var magPower_ = getCurrentMagnifierPower();
            if (magPower_ == null) return;
            var magPower = (double)magPower_;
            for (var i = cPage; i < nextPage; i++)
            {
                var img = preFilteredImageArray[i];
                var resizedSize = resizedSizeArray[i];
                var zoomedHeightI = (int)Math.Round(magPower * resizedSize.Height);
                if (zoomedHeightI > zoomedHeight) zoomedHeight = zoomedHeightI;
                //orgSize = new Size(Math.Abs(orgSize.Width), Math.Abs(orgSize.Height));
                if (img == null)
                {
                    //if (orgSize.Width >= 0 || orgSize.Height >= 0)
                    if (resizedSize.IsEmpty) // エラーで img == null の可能性を除外
                    {
                        pbView_Paint_Rectangle = Rectangle.Empty;
                        ViewerMode = ViewerModeEnum.Normal;
                        return;
                    }
                    /*
                    else
                    {
                        orgSize = new Size(-orgSize.Width, -orgSize.Height);
                    }
                    */
                }

                // 拡大画像生成待ちで待たされるので不適切
                /*
                lock (img)
                {
                    // ここはメインスレッドでしか実行されないので同じくメインスレッドでしか行われない Dispose は危険因子ではない
                    totalWidth += zoomedWidthArray[i - currentPage] = (int)Math.Round(zoomedHeight * img.Width / (double)img.Height);
                }
                */

                //var resizedSize = GetResizedSize(orgSize);
                //totalWidth += zoomedWidthArray[i - currentPage] = (int)Math.Round(zoomedHeight * size.Width / (double)size.Height);
                var zoomedWidth = (int)Math.Round(resizedSize.Width * magPower);
                totalWidth += zoomedWidth;
                zoomedSizeArray[i - cPage] = new Size(zoomedWidth, zoomedHeightI);
            }
            //Magnifier_VisiblePage = Tuple.Create(currentPage, nextPage);
            SetMagnifierCanvasInfo(cPage, nextPage);
            //bmwLoadEachPage.ThreadCount = 0;
            if (Magnifier_ZoomedPage != null) Magnifier_ZoomedPage.Dispose();
            Magnifier_ZoomedPage = null;

            if (bwMagnifierMaker.IsBusy)
            {
                bwMagnifierMaker.CancelAsync();
                bwMagnifierMaker_Q = Tuple.Create(totalWidth, zoomedHeight, nextPage, cPage, zoomedSizeArray);
            }
            else
            {
                bwMagnifierMaker.RunWorkerAsync(Tuple.Create(totalWidth, zoomedHeight, nextPage, cPage, zoomedSizeArray));
            }
        }

        private void pbPaintInvalidate()
        {
            pbView_Paint_OnlyDrawMeasure = false;
            pbView.Invalidate(false);

                /*
            if (pbView_CanPaint())
            {
                pbView.Invalidate(false);
            }
            else drawLoading(null);
            */
        }

        private void pbPaintDrawOnlyMeasure()
        {
            pbView_Paint_OnlyDrawMeasure = true;
            pbView.Invalidate(false);

            /*
        if (pbView_CanPaint())
        {
            pbView.Invalidate(false);
        }
        else drawLoading(null);
        */
        }

        /*
        private readonly HashSet<Bitmap> lockBits_LockedList = new HashSet<Bitmap>();
        private void lockBits(Bitmap bmp, Rectangle rect, ImageLockMode flags, Action<BitmapData> body)
        {
            bool locked;
            lock (lockBits_LockedList)
            {
                locked = lockBits_LockedList.Contains(bmp);
                if (!locked) lockBits_LockedList.Add(bmp);
            }
            if(!locked)
            {
                try
                {
                    var data = bmp.LockBits(rect, flags, PixelFormat.Format24bppRgb);
                    try
                    {
                        body(data);
                    }
                    finally
                    {
                        bmp.UnlockBits(data);
                    }
                }
                finally
                {
                    lockBits_LockedList.Remove(bmp);
                }
            }
        }
        */

        public static void lockBits(BitmapEx bmp, Rectangle rect, ImageLockMode flags, string purpose, Action<BitmapData> body)
        {
            lockBits(bmp, bmp, rect, flags, purpose, body);
        }

        public static void lockBits(Bitmap bmp, BitmapEx locker, Rectangle rect, ImageLockMode flags, string purpose, Action<BitmapData> body)
        {
            if (rect.IsEmpty) return;
            if (!new Rectangle(Point.Empty, bmp.Size).Contains(rect)) return;
            lock (locker)
            {
                BitmapData data;
                try
                {
                    data = bmp.LockBits(rect, flags, PixelFormat.Format24bppRgb);
                }
                catch (Exception e)
                {
                    string size;
                    try
                    {
                        size = $"({bmp.Width}, {bmp.Height})";
                    }
                    catch
                    {
                        size = "<error>";
                    }
                    throw new Exception($"Exception is thrown by lockBits\nbmp.Size = {size}\nrect = {rect}\npurpose = {purpose}", e);
                }
                try
                {
                    body(data);
                }
                finally
                {
                    bmp.UnlockBits(data);
                }
            }
        }

        Rectangle pbView_Paint_Rectangle = Rectangle.Empty;
        string pbView_Paint_RectangleMessage = null;
        readonly static Brush pbView_Paint_Brush = new SolidBrush(Color.FromArgb(160, 128, 128, 128));
        //readonly static Brush pbView_Paint_Brush_WithoutAlpha = new SolidBrush(Color.FromArgb(255, 128, 128, 128));
        private static Color HalfGrayBrusher(Color c) { return Color.FromArgb(
            ((160 * 128 + (255 - 160) * c.R + 127) / 255), ((160 * 128 + (255 - 160) * c.G + 127) / 255), ((160 * 128 + (255 - 160) * c.B + 127) / 255)); }
        private static unsafe void HalfGrayBrusher(IntPtr dataAdr, int size)
        {
#if DEBUG
            var data = (byte*)dataAdr; var stop = size / 3 * 3; for (var i = 0; i < stop; i++) data[i] = i % 3 == 2 ? (byte)255 : (byte)0;
#else
            var data = (byte*)dataAdr; var stop = size / 3 * 3; for (var i = 0; i < stop; i++) data[i] = (byte)((160 * 128 + (255 - 160) * data[i] + 127) / 255);
#endif
        }
        

        const bool parallelInPaint = false;
        bool pbView_Paint_Rectangle_FillAround = true;
        BitmapEx pbView_Paint_Canvas;
        bool drawLoading_ShowingMessage = false;
        int pbView_Paint_PrevWidth = 0;
        int pbView_Paint_PrevHeight = 0;
        bool pbView_Paint_OnlyDrawMeasure = false;
        //readonly object pbView_Paint_Canvas_Locker = new object();
        private void pbView_Paint(object sender, PaintEventArgs e)
        {
            if (FullScreen_pseudoFullScreen) return;
            
            Rectangle pbViewRect;

            //lock (pbView_Paint_Canvas_Locker)
            {
                var pbViewSize = pbView.Size;
                if (pbViewSize.Width <= 0 || pbViewSize.Height <= 0) return;

                if (pbView_Paint_Canvas == null)
                {
                    pbView_Paint_Canvas = BitmapEx.ConvertToBitmapEx(new Bitmap(pbViewSize.Width, pbViewSize.Height, PixelFormat.Format24bppRgb));
                    pbView_Paint_OnlyDrawMeasure = false;
                }
                else if (/*pbView_Paint_Canvas == null ||*/ pbView_Paint_Canvas.Width < pbViewSize.Width || pbView_Paint_Canvas.Height < pbViewSize.Height)
                {
                    //var width = pbView_Paint_Canvas == null ? pbViewSize.Width : Math.Max(pbView_Paint_Canvas.Width, pbViewSize.Width);
                    //var height = pbView_Paint_Canvas == null ? pbViewSize.Height : Math.Max(pbView_Paint_Canvas.Height, pbViewSize.Height);
                    var width = Math.Max(pbView_Paint_Canvas.Width, pbViewSize.Width);
                    var height = Math.Max(pbView_Paint_Canvas.Height, pbViewSize.Height);
                    pbView_Paint_Canvas.Dispose();
                    pbView_Paint_Canvas = BitmapEx.ConvertToBitmapEx(new Bitmap(width, height, PixelFormat.Format24bppRgb));
                    pbView_Paint_OnlyDrawMeasure = false;
                }

                if (pbViewSize.Width > pbView_Paint_PrevWidth || pbViewSize.Height > pbView_Paint_PrevHeight)
                {
                    var rect = new Rectangle(0, 0, pbViewSize.Width, pbViewSize.Height);
                    lockBits(pbView_Paint_Canvas, rect, ImageLockMode.WriteOnly, "Fill background of new canvas", data =>
                    {
                    //MessageBox.Show("X");
                    int drawX = pbViewSize.Width;
                        if (pbViewSize.Width > pbView_Paint_PrevWidth)
                        {
                            FillRectangle(data, pbView_Paint_PrevWidth, 0, pbView.BackColor,
                             pbViewSize.Width - pbView_Paint_PrevWidth, pbViewSize.Height, ParallelMode.ParallelForUIThread);
                            drawX = pbView_Paint_PrevWidth;
                        }
                        if (pbViewSize.Height > pbView_Paint_PrevHeight && drawX > 0) FillRectangle(data, 0, pbView_Paint_PrevHeight, pbView.BackColor,
                             drawX, pbViewSize.Height - pbView_Paint_PrevHeight, ParallelMode.ParallelForUIThread);
                    });
                    pbView_Paint_OnlyDrawMeasure = false;
                }
                pbView_Paint_PrevWidth = pbViewSize.Width;
                pbView_Paint_PrevHeight = pbViewSize.Height;

                //var sw = new System.Diagnostics.Stopwatch(); var s = ""; sw.Start();

                if (!pbView_Paint_OnlyDrawMeasure)
                {
                    //MessageBox.Show("A");
                    if (!pbView_PaintToCanvas(pbView_Paint_Canvas, ParallelMode.ParallelForUIThread))
                    {
                        if (!drawLoading_ShowingMessage)
                        {
                            var rect = new Rectangle(0, 0, pbViewSize.Width, pbViewSize.Height);
                            lockBits(pbView_Paint_Canvas, rect, ImageLockMode.ReadWrite, "Draw loading mask", data =>
                            {
                                FillRectangle(data, HalfGrayBrusher, rect, ParallelMode.ParallelForUIThread);
                            });
                            /*
                            using (var g = Graphics.FromImage(pbView_Paint_Canvas))
                            {
                                g.
                            }
                            */
                            drawLoading_ShowingMessage = true;
                            //sw.Stop(); s += sw.Elapsed + "\n"; sw.Restart();
                        }
                    }
                    else
                    {
                        drawLoading_ShowingMessage = false;
                    }
                }
                //MessageBox.Show("B");

                //sw.Stop(); s += sw.Elapsed + "\n"; sw.Restart();

                pbViewRect = pbView.ClientRectangle;

                e.Graphics.DrawImageUnscaled(pbView_Paint_Canvas, pbViewRect);

                if (DrawMeasure)
                {
                    drawMeasure(e.Graphics, pbView_Paint_Canvas);
                }

                drawMessageOnRectangle(e.Graphics); //, pbView_Paint_Canvas);
            }

            if (drowSideLine_Draw)
            {
                var rect = pbViewRect;
                if (!drowSideLine_DrawLeft)
                {
                    rect.X = rect.Right - drowSideLine_DrawWidth;
                }
                rect.Width = drowSideLine_DrawWidth;
                using (var b = new SolidBrush(drowSideLine_DrawColor))
                {
                    e.Graphics.FillRectangle(b, rect);
                }
            }

            //sw.Stop(); s += sw.Elapsed + "\n"; MessageBox.Show(s);
            /*
            pbView_PaintToGraphics(e.Graphics);
            */
        }

        private void drawMessageOnRectangle(Graphics g)//, Image baseImage)
        {
            string mess;
            var vm = ViewerMode;
            bool opening;
            if (((opening = vm == ViewerModeEnum.MagnifierOpening) || vm == ViewerModeEnum.MagnifierClosing) && (mess = pbView_Paint_RectangleMessage) != null)
            {
                var baseFont = pbView.Font ?? DefaultFont;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                var baseSize = baseFont.Size;
                using (var font = new Font(baseFont.FontFamily, baseSize * 2))
                {
                    var p = pbView_Paint_Rectangle.Location;
                    if (opening)
                    {
                        var size = g.MeasureString(mess, font);
                        p.Y -= (int)Math.Ceiling(size.Height);
                    }
                    else
                    {
                        p.Y += (int)(font.Size / 4);
                    }
                    g.DrawString(mess, font, Brushes.White, p);

                }
            }
        }

        /*
        private void drawMessageOnRectangle(Graphics g)//, Image baseImage)
        {
            string mess;
            var vm = ViewerMode;
            bool opening;
            if (((opening = vm == ViewerModeEnum.MagnifierOpening) || vm == ViewerModeEnum.MagnifierClosing) && (mess = pbView_Paint_RectangleMessage) != null)
            {
                var rect = pbView_Paint_Rectangle;
                rect.Intersect(pbView.ClientRectangle);
                if (rect.Width > 0 && rect.Height > 0 && mess != "")
                {
                    var baseFont = pbView.Font ?? DefaultFont;
                    var font = new Font(baseFont.FontFamily, baseFont.Size * 2);
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    var size = g.MeasureString(mess, font);
                    bool sizeChanged;
                    if (sizeChanged = size.Width > rect.Width)
                    {
                        var newSize = (float)Math.Max(baseFont.Size, (double)font.Size * rect.Width / size.Width);
                        font.Dispose();
                        font = new Font(baseFont.FontFamily, newSize);
                    }

                    if (opening)
                    {
                        //g.DrawString(mess, font, pbView_Paint_Brush, rect.Location);
                        g.DrawString(mess, font, pbView_Paint_Brush_WithoutAlpha, rect.Location);

                    }
                    else
                    {
                        //g.DrawString(mess, font, Brushes.Black, rect.Location);
                        //g.DrawString(mess, font, Brushes.White, rect.Location);
                        
                        if (baseImage is System.Drawing.Bitmap baseBitmap)
                        {
                            if (sizeChanged) size = g.MeasureString(mess, font);
                            rect.Width = (int)Math.Ceiling(size.Width);
                            rect.Height = (int)Math.Ceiling(size.Height);
                            rect.Intersect(new Rectangle(0, 0, baseBitmap.Width, baseBitmap.Height));
                            if (rect.Width > 0 && rect.Height > 0)
                            {
                                using (var back = baseBitmap.Clone(rect, baseBitmap.PixelFormat))
                                using (var b = new TextureBrush(back))
                                {
                                    g.DrawString(mess, font, b, rect.Location);
                                }
                            }
                        }
                    }


                    font.Dispose();
                }
            }
        }
*/

        private void drawMeasure(Graphics g, Bitmap backGround)
        {
            var w = pbView.Width;
            var h = pbView.Height;
            if (w <= 0 || h <= 0 || backGround.Width < w || backGround.Height < h) return;
            var p = pbView.PointToClient(Cursor.Position);
            var rect = new Rectangle(0, 0, w, h);
            if (!rect.Contains(p)) return;

            if (ZipTag.NearToWhite(backGround.GetPixel(p.X, p.Y)))
            {
                g.DrawLine(Pens.Black, new Point(p.X, 0), new Point(p.X, h));
                g.DrawLine(Pens.Black, new Point(0, p.Y), new Point(w, p.Y));
            }
            else
            {
                g.DrawLine(Pens.White, new Point(p.X, 0), new Point(p.X, h));
                g.DrawLine(Pens.White, new Point(0, p.Y), new Point(w, p.Y));
            }

        }

        private bool pbView_PaintToCanvas(BitmapEx canvas, ParallelMode parallelMode)
        {
            var pbViewSize = pbView.Size;

            if(string.IsNullOrEmpty(currentArchiveFilePath))
            {
                lockBits(canvas, new Rectangle(0, 0, pbViewSize.Width, pbViewSize.Height),
                    ImageLockMode.WriteOnly, "Fill background for blank data", dst =>
                {
                    FillRectangle(dst, 0, 0, pbView.BackColor, pbViewSize.Width, pbViewSize.Height, ParallelMode.Single);
                });
                return true;
            }

            var sizeChanged = showCurrentPage_PrevDrawSize != pbViewSize;
            var returnValueForNoDraw = !sizeChanged;
            var ret = sizeChanged && !showCurrentPage_PagePrepared; 
            showCurrentPage_PagePrepared = false;
            if (ret)
            {
                switch (ViewerMode)
                {
                    case ViewerModeEnum.Normal: break;
                    case ViewerModeEnum.MagnifierOpening:
                    case ViewerModeEnum.MagnifierClosing:
                        pbView_Paint_Rectangle = Rectangle.Empty;
                        ViewerMode = ViewerModeEnum.Normal;
                        break;
                    case ViewerModeEnum.Magnifier:
                        break;
                }
                return sizeChanged;
            }
            showCurrentPage_PrevDrawSize = pbViewSize;
            //if (sizeChanged) return false;
            
            ShowCurrentPageToStatusBar();
            var stopPage = NextPage;
            if (ViewerMode == ViewerModeEnum.Magnifier && 
                MagnifierCanvasPrepared() && currentPage >= 0 && Magnifier_ZoomedPage != null)
            {
                Rectangle mr;
                if (TouchListener_Pan_Delta == null)
                {
                    mr = GetMagnifierRectangle(false, null);
                }
                else
                {
                    mr = GetMagnifierRectangle(false, TouchListener_Pan_Delta);
                    TouchListener_Pan_Delta = VectorD.Zero;
                }
                var dr = new Rectangle(new Point((pbViewSize.Width - mr.Width) / 2, (pbViewSize.Height - mr.Height) / 2), mr.Size);
                // 拡大画像の背景はあらかじめ設定してある
                //g.DrawImage(Magnifier_ZoomedPage, dr, mr, GraphicsUnit.Pixel);
                //g.DrawImageUnscaled(Magnifier_ZoomedPage, dr.X - mr.X, dr.Y - mr.Y); // DebugMode では OutOfMemoryException が投げられることがある

                var mathX = dr.X - mr.X;
                var mathY = dr.Y - mr.Y;

                DateTime now;
                var magBitmaps = Magnifier_ZoomedPage.Bitmaps;
                var magOffsets = Magnifier_ZoomedPage.Offsets;
                ReserveNextInvadiateForAnimation(Magnifier_ZoomedPage, out now);

                var imageCount = Magnifier_ZoomedPage.ImageCount;

                lockBits(canvas, new Rectangle(0, 0, pbViewSize.Width, pbViewSize.Height),
                    ImageLockMode.WriteOnly, "zoomed image (canvas)", dst =>
                    {
                        var leftBound = Math.Max(0, mathX);
                        var topBound = Math.Max(0, mathY);
                        var rightBound = Math.Min(pbViewSize.Width, leftBound + mr.Width);
                        var bottomBound = Math.Min(pbViewSize.Height, topBound + mr.Height);
                        var imageHeight = bottomBound - topBound;

                        var color = pbView.BackColor;

                        for (var i = 0; i < imageCount; i++)
                        {
                            var offset = magOffsets[i];
                            var localMathX = mathX + offset.X;
                            var localMathY = mathY + offset.Y;
                            var magBitI = magBitmaps[i];
                            var localRect = new Rectangle(Math.Max(0, -localMathX), Math.Max(0, -localMathY), mr.Width, mr.Height);
                            localRect.Intersect(new Rectangle(0, 0, magBitI.Width, magBitI.Height));
                            var localLeftBound = Math.Max(0, localMathX);
                            var localTopBound = Math.Max(0, localMathY);
                            magBitI.DrawFrame(dst, localLeftBound, localTopBound, localRect, now, canUseParallel: true);

                            if (localTopBound > topBound)
                            {
                                FillRectangle(dst, localLeftBound, topBound, color, localRect.Width, localTopBound - topBound, ParallelMode.Single);
                            }
                            var localBottomBound = Math.Min(pbViewSize.Height, localTopBound + localRect.Height);
                            if (localBottomBound < bottomBound)
                            {
                                FillRectangle(dst, localLeftBound, localBottomBound, color, localRect.Width, bottomBound - localBottomBound, ParallelMode.Single);
                            }

                                /*
                                lockBits(magBitI.Bitmaps[magBitI.GetCurrentIndex(now)], localRect,
                                    ImageLockMode.ReadOnly, "zoomed image (source)", src =>
                                    {
                                        var localLeftBound = Math.Max(0, mathX + offset);
                                        DrawBitmapDataUnscaled(dst, localLeftBound, topBound, src);


                                    });
                                    */
                        }

                        if (topBound > 0) FillRectangle(dst, 0, 0, color, pbViewSize.Width, topBound, ParallelMode.Single);
                        if (leftBound > 0) FillRectangle(dst, 0, topBound, color, leftBound, imageHeight, ParallelMode.Single);
                        if (rightBound < pbViewSize.Width) FillRectangle(dst, rightBound, topBound, color,
                      pbViewSize.Width - rightBound, imageHeight, ParallelMode.Single);
                        if (bottomBound < pbViewSize.Height) FillRectangle(dst, 0, bottomBound, color,
                     pbViewSize.Width, pbViewSize.Height - bottomBound, ParallelMode.Single);
                    });
                return true;
            }
            else
            {
                var totalWidth = 0;
                var startPage = currentPage;
                //var imageHeight =　-1;
                for (var i = startPage; i < stopPage; i++)
                {
                    totalWidth += ResizedImageArray[i].Width;
                    /*
                    if (imageHeight == -1)
                    {
                        imageHeight = ResizedImageArray[i].Height;
                    }
                    else if (imageHeight != ResizedImageArray[i].Height)
                    {
                        // 高さ不一致は描画条件不成立（現在の実装ではここへの到達は想定されない）
                        return sizeChanged;
                    }
                    */
                }

                var divisions = 1;
                var left = 0;
                var right = totalWidth;

                if (totalWidth <= 0)
                {
                    if (MaxDivision > 1 && 0 <= startPage && startPage == stopPage && ResizedImageArray != null && ResizedImageArray.Length > startPage && getCurrentPageDivison() > 1)
                    {
                        var img = ResizedImageArray[startPage];
                        if (img == null) return sizeChanged;
                        var imageWidth = img.Width;
                        //imageHeight = img.Height;
                        divisions = (imageWidth - 1) / pbViewSize.Width + 1;
                        if (divisions > MaxDivision) return sizeChanged;
                        if (CurrentDividedPosition < 0 || CurrentDividedPosition >= divisions) return sizeChanged;
                        stopPage = startPage + 1;
                        var p = BindingMode != BindingMode.RightToLeft ? CurrentDividedPosition : divisions - CurrentDividedPosition - 1;
                        left = (imageWidth * p + divisions / 2) / divisions;
                        right = (imageWidth * (p + 1) + divisions / 2) / divisions;
                        totalWidth = right - left;
                    }
                }

                if (totalWidth <= 0) return sizeChanged;

                bool pageLeftToRight = BindingMode == BindingMode.LeftToRight && divisions <= 1; // 分割表示の場合の方向指定は left と right で指定
                int leftBound, rightBound;
                if (divisions == 1 && stopPage > startPage + 1 || !(startPage == 0 || stopPage == ResizedImageArray.Length) ||
                   !(BindingMode == BindingMode.LeftToRight || BindingMode == BindingMode.RightToLeft) || totalWidth > pbViewSize.Width / 2)
                {
                    if (stopPage == startPage + 2 && ResizedImageArray[startPage].Width <= pbViewSize.Width / 2 && ResizedImageArray[startPage + 1].Width <= pbViewSize.Width / 2)
                    {
                        leftBound = pbViewSize.Width / 2 - ResizedImageArray[BindingMode == BindingMode.LeftToRight ? startPage : (startPage + 1)].Width;
                    }
                    else
                    {
                        leftBound = (pbViewSize.Width - totalWidth) / 2;
                    }
                }
                else
                {
                    if (BindingMode == BindingMode.LeftToRight ^ startPage == 0)
                    {
                        leftBound = pbViewSize.Width / 2 - totalWidth;
                    }
                    else
                    {
                        leftBound = pbViewSize.Width / 2;
                    }
                }
                rightBound = leftBound + totalWidth;
                var offsetX = pageLeftToRight ? rightBound : leftBound;

                var minOffetY = pbViewSize.Height;
                var maxBottomBound = 0;
                for (var i = startPage; i < stopPage; i++)
                {
                    var resizedSize = ResizedSizeArray[i];
                    var imageHeight = resizedSize.Height;
                    int offsetY = (pbViewSize.Height - imageHeight) / 2;
                    int bottomBound = (pbViewSize.Height + imageHeight) / 2;
                    if (offsetY < minOffetY) minOffetY = offsetY;
                    if (bottomBound > maxBottomBound) maxBottomBound = bottomBound;
                }

                DateTime now;
                ReserveNextInvadiateForAnimation(currentPage, stopPage, out now);

                var bColor = pbView.BackColor;
                var color = pbView_Paint_Rectangle.IsEmpty || !pbView_Paint_Rectangle_FillAround ? bColor : HalfGrayBrusher(pbView.BackColor);

               // var totalWidthMemory = totalWidth;
                totalWidth = 0;
                var start = pageDirection > 0 ? currentPage : stopPage - 1;
                var stop = pageDirection > 0 ? stopPage : currentPage - 1;
                lockBits(canvas, new Rectangle(0, 0, pbViewSize.Width, pbViewSize.Height),
                    pbView_Paint_Rectangle.IsEmpty ? ImageLockMode.ReadOnly : ImageLockMode.ReadWrite, "normal image (canvas)", dst =>
                {
                    for (var i = start; i != stop; i += pageDirection)
                    {
                        var resizedSize = ResizedSizeArray[i];
                        var imageHeight = resizedSize.Height;
                        int offsetY = (pbViewSize.Height - imageHeight) / 2;
                        int bottomBound = (pbViewSize.Height + imageHeight) / 2;

                        var imgEx = ResizedImageArray[i];


                        var x = offsetX + (pageLeftToRight ? -totalWidth - imgEx.Width : totalWidth);
                        if (divisions == 1)
                        {
                            //DrawBitmapDataUnscaled(dst, x, offsetY, src);
                            imgEx.DrawFrame(dst, x, offsetY, now);

                            var imageWidth = resizedSize.Width;
                            if (offsetY > minOffetY) FillRectangle(dst, x, minOffetY, bColor, imageWidth, offsetY - minOffetY, ParallelMode.Single);
                            if (bottomBound < maxBottomBound) FillRectangle(dst, x, bottomBound, bColor,
                                imageWidth, maxBottomBound - bottomBound, ParallelMode.Single);

                        }
                        else
                        {
                            var imageWidth = right - left;
                            //DrawBitmapDataUnscaled(dst, x, offsetY, src, left, 0, imageWidth, imgEx.Height);
                            imgEx.DrawFrame(dst, x, offsetY, new Rectangle(left, 0, imageWidth, imgEx.Height), now, canUseParallel: true);

                            if (offsetY > minOffetY) FillRectangle(dst, x, minOffetY, bColor, imageWidth, offsetY - minOffetY, ParallelMode.Single);
                            if (bottomBound < maxBottomBound) FillRectangle(dst, x, bottomBound, bColor,
                                imageWidth, maxBottomBound - bottomBound, ParallelMode.Single);
                        }


                                /*
                                var img = imgEx.Bitmaps[imgEx.GetCurrentIndex(now)];
                                lockBits(img, new Rectangle(0, 0, img.Width, imageHeight),
                                    ImageLockMode.ReadOnly, "normal image (source)", src =>
                                    {
                                        var x = offsetX + (pageLeftToRight ? -totalWidth - img.Width : totalWidth);
                                        //g.DrawImageUnscaled(img, x, offsetY);
                                        if (divisions == 1)
                                        {
                                            DrawBitmapDataUnscaled(dst, x, offsetY, src);

                                            var imageWidth = resizedSize.Width;
                                            if (offsetY > minOffetY) FillRectangle(dst, x, minOffetY, bColor, imageWidth, offsetY - minOffetY, ParallelMode.Single);
                                            if (bottomBound < maxBottomBound) FillRectangle(dst, x, bottomBound, bColor,
                                                imageWidth, maxBottomBound - bottomBound, ParallelMode.Single);
                                        }
                                        else
                                        {
                                            var imageWidth = right - left;
                                            DrawBitmapDataUnscaled(dst, x, offsetY, src, left, 0, imageWidth, img.Height);

                                            if (offsetY > minOffetY) FillRectangle(dst, x, minOffetY, bColor, imageWidth, offsetY - minOffetY, ParallelMode.Single);
                                            if (bottomBound < maxBottomBound) FillRectangle(dst, x, bottomBound, bColor,
                                                imageWidth, maxBottomBound - bottomBound, ParallelMode.Single);
                                        }
                                });
                                */

                                totalWidth += imgEx.Width;
                    }

                    
                    if (minOffetY > 0) FillRectangle(dst, 0, 0, color, pbViewSize.Width, minOffetY, ParallelMode.Single);
                    if (leftBound > 0) FillRectangle(dst, 0, minOffetY, color, leftBound, maxBottomBound - minOffetY, ParallelMode.Single);
                    if (rightBound < pbViewSize.Width) FillRectangle(dst, rightBound, minOffetY, color,
                         pbViewSize.Width - rightBound, maxBottomBound - minOffetY, ParallelMode.Single);
                    if (maxBottomBound < pbViewSize.Height) FillRectangle(dst, 0, maxBottomBound, color,
                        pbViewSize.Width, pbViewSize.Height - maxBottomBound, ParallelMode.Single);

                    if (!pbView_Paint_Rectangle.IsEmpty)
                    {
                        if (pbView_Paint_Rectangle_FillAround)
                        {
                            FillRectangle(dst, HalfGrayBrusher, new Rectangle(leftBound, minOffetY, pbView_Paint_Rectangle.X - leftBound, maxBottomBound - minOffetY), parallelMode);
                            FillRectangle(dst, HalfGrayBrusher, new Rectangle(pbView_Paint_Rectangle.Right, minOffetY, rightBound - pbView_Paint_Rectangle.Right, maxBottomBound - minOffetY), parallelMode);
                            FillRectangle(dst, HalfGrayBrusher, new Rectangle(pbView_Paint_Rectangle.X, minOffetY, pbView_Paint_Rectangle.Width, pbView_Paint_Rectangle.Y - minOffetY), parallelMode);
                            FillRectangle(dst, HalfGrayBrusher, new Rectangle(pbView_Paint_Rectangle.X, pbView_Paint_Rectangle.Bottom, pbView_Paint_Rectangle.Width, maxBottomBound - pbView_Paint_Rectangle.Bottom), parallelMode);
                        }
                        else
                        {
                            FillRectangle(dst, HalfGrayBrusher, pbView_Paint_Rectangle, parallelMode);
                            //g.FillRectangle(pbView_Paint_Brush, pbView_Paint_Rectangle);
                        }

                        if (ViewerMode == ViewerModeEnum.MagnifierOpening || ViewerMode == ViewerModeEnum.MagnifierClosing)
                        {
                            int borderWidth = Program.DpiScaling(5);
                            var bHalf = borderWidth / 2;
                            var bHalf2 = borderWidth - bHalf; // 奇数を想定
                            //var borderColor = Color.Chartreuse
                            var borderColor = Color.Yellow;

                            if (magnifyingPower == MagnifyingInfo.FitHorizontal)
                            {
                                FillRectangle(dst, pbView_Paint_Rectangle.X - bHalf2, 0, borderColor, borderWidth, pbViewSize.Height, ParallelMode.Single);
                                FillRectangle(dst, pbView_Paint_Rectangle.Right - bHalf, 0, borderColor, borderWidth, pbViewSize.Height, ParallelMode.Single);
                            }
                            if (magnifyingPower == MagnifyingInfo.FitVertical)
                            {
                                FillRectangle(dst, 0, pbView_Paint_Rectangle.Y - bHalf2, borderColor, pbViewSize.Width, borderWidth, ParallelMode.Single);
                                FillRectangle(dst, 0, pbView_Paint_Rectangle.Bottom - bHalf, borderColor, pbViewSize.Width, borderWidth, ParallelMode.Single);
                            }
                        }

                    }

                });
                return true;
            }
        }

        DateTime ReserveNextInvadiateForAnimation_ReservedTime = DateTime.MaxValue;
        readonly HashSet<BitmapEx> ReserveNextInvadiateForAnimation_LastVisible = new HashSet<BitmapEx>();
        private void ReserveNextInvadiateForAnimation(MagnifierCanvas magCanvas, out DateTime now) // start < stop であること
        {
            var targets = (from t in magCanvas.Bitmaps where t.FrameCount > 1 select t.GetSourceForLock()).ToArray();
            ReserveNextInvadiateForAnimation(targets, out now);
        }
        private void ReserveNextInvadiateForAnimation(int start, int stop, out DateTime now) // start < stop であること
        {
            var targets = (from i in Enumerable.Range(start, stop - start) let t = ResizedImageArray[i] where t.FrameCount > 1 select t.GetSourceForLock()).ToArray();
            ReserveNextInvadiateForAnimation(targets, out now);
        }
        private void ReserveNextInvadiateForAnimation(BitmapEx[] targets, out DateTime now)
        {
            if (targets.Length <= 0)
            {
                ReserveNextInvadiateForAnimation_LastVisible.Clear();
                now = default(DateTime);
                ReserveNextInvadiateForAnimation_ReservedTime = default(DateTime);
                return;
            }
            now = DateTime.Now;
            foreach (var t in targets)
            {
                if (!ReserveNextInvadiateForAnimation_LastVisible.Contains(t)) t.Start(now);
            }
            ReserveNextInvadiateForAnimation_LastVisible.Clear();
            foreach (var t in targets) ReserveNextInvadiateForAnimation_LastVisible.Add(t);
            var now_ = now;
            var reserveTime = (from t in targets select t.GetNextChangeTime(now_)).Min();
            if (reserveTime > now)
            {
                ReserveNextInvadiateForAnimation_AsyncPart(reserveTime, now, targets);
            }
            else
            {
                ReserveNextInvadiateForAnimation_LastVisible.Clear();
                now = default(DateTime);
                ReserveNextInvadiateForAnimation_ReservedTime = default(DateTime);
                return;
            }
        }

        private async void ReserveNextInvadiateForAnimation_AsyncPart(DateTime reserveTime, DateTime now, BitmapEx[] targets)
        {
            ReserveNextInvadiateForAnimation_ReservedTime = reserveTime;
            await Task.Delay(reserveTime - now);
            if (ReserveNextInvadiateForAnimation_ReservedTime != reserveTime
                || !ReserveNextInvadiateForAnimation_LastVisible.SetEquals(targets)) return;
            pbView.Invalidate();
        }

        public static void DrawBitmapDataUnscaled(BitmapData dst, BitmapData src)
        {
            DrawBitmapDataUnscaled(dst, 0, 0, src);
        }

        public static void DrawBitmapDataUnscaled(BitmapData dst, int xDst, int yDst, BitmapData src)
        {
            DrawBitmapDataUnscaled(dst, xDst, yDst, src, 0, 0, src.Width, src.Height);
        }

        private static void DrawBitmapDataUnscaled(BitmapData dst, int xDst, int yDst, BitmapData src, int xSrc, int ySrc, int width, int height)
        {
            var rect = new Rectangle(-xDst, -yDst, dst.Width, dst.Height);
            rect.Intersect(new Rectangle(-xSrc, -ySrc, src.Width, src.Height));
            rect.Intersect(new Rectangle(0, 0, width, height));
            xDst += rect.X;
            yDst += rect.Y;
            xSrc += rect.X;
            ySrc += rect.Y;
            width = rect.Width;
            height = rect.Height;
            
            var pixelFormat = dst.PixelFormat;
            if (src.PixelFormat != pixelFormat) throw new InvalidEnumArgumentException();
            var bytePerPixel = Image.GetPixelFormatSize(pixelFormat) >> 3;
            var dstStride = dst.Stride;
            var srcStride = src.Stride;
            var dstLeft = dst.Scan0 + yDst * dstStride + xDst * bytePerPixel;
            var srcLeft = src.Scan0 + ySrc * srcStride + xSrc * bytePerPixel;
            var buffSize = width * bytePerPixel;
            //var buff = new byte[buffSize];
            for (var j = 0; j < height; j++)
            {
                //System.Runtime.InteropServices.Marshal.Copy(srcLeft, buff, 0, buffSize);
                //System.Runtime.InteropServices.Marshal.Copy(buff, 0, dstLeft, buffSize);
                CopyMemory(dstLeft, srcLeft, buffSize);
                dstLeft += dstStride;
                srcLeft += srcStride;
            }
        }

        private void FillRectangle(BitmapData dst, Action<IntPtr, int> editor, Rectangle rect, ParallelMode parallelMode)
        {
            FillRectangle(dst, rect.X, rect.Y, editor, rect.Width, rect.Height, parallelMode);
        }
        private unsafe void FillRectangle(BitmapData dst, int xDst, int yDst, Action<IntPtr, int> editor, int width, int height, ParallelMode parallelMode)
        {
            var rec = new Rectangle(0, 0, dst.Width, dst.Height);
            rec.Intersect(new Rectangle(xDst, yDst, width, height));
            xDst = rec.X;
            yDst = rec.Y;
            width = rec.Width;
            height = rec.Height;

            var pixelFormat = dst.PixelFormat;
            var bytePerPixel = Image.GetPixelFormatSize(pixelFormat) >> 3;
            var dstStride = dst.Stride;
            var dstLeft = dst.Scan0 + yDst * dstStride + xDst * bytePerPixel;
            var buffSize = width * bytePerPixel;

            /*
            ParallelFor(parallelMode, 0, height, () => new byte[buffSize], (j, buff) =>
            {
                var currentDstLeft = dstLeft + (j * dstStride);
                System.Runtime.InteropServices.Marshal.Copy(currentDstLeft, buff, 0, buffSize);
                editor(buff);
                System.Runtime.InteropServices.Marshal.Copy(buff, 0, currentDstLeft, buffSize);
            });
            */

            ParallelFor(parallelMode, 0, height, j =>
            {
                var currentDstLeft = dstLeft + (j * dstStride);
                editor(currentDstLeft, buffSize);
            });
        }

        private void FillRectangle(BitmapData dst, Color color, Rectangle rect, ParallelMode parallelMode)
        {
            FillRectangle(dst, rect.X, rect.Y, color, rect.Width, rect.Height, parallelMode);
        }
        private unsafe void FillRectangle(BitmapData dst, int xDst, int yDst, Color color, int width, int height, ParallelMode parallelMode)
        {
            var rec = new Rectangle(0, 0, dst.Width, dst.Height);
            rec.Intersect(new Rectangle(xDst, yDst, width, height));
            xDst = rec.X;
            yDst = rec.Y;
            width = rec.Width;
            height = rec.Height;

            var pixelFormat = dst.PixelFormat;
            var bytePerPixel = Image.GetPixelFormatSize(pixelFormat) >> 3;
            var dstStride = dst.Stride;
            var dstLeft = dst.Scan0 + yDst * dstStride + xDst * bytePerPixel;
            var buffSize = width * bytePerPixel;
            var buff = new byte[buffSize];
            if (bytePerPixel == 3)
            {
                var b = color.B;
                var g = color.G;
                var r = color.R;
                var i = 0;
                while (i < buffSize)
                {
                    buff[i++] = b;
                    buff[i++] = g;
                    buff[i++] = r;
                }
            }
            else throw new NotImplementedException();

            fixed (byte* a = buff)
            {
                var buffAdr = (IntPtr)a;

                ParallelFor(parallelMode, 0, height, j =>
                {
                    var currentDstLeft = dstLeft + (j * dstStride);
                    //System.Runtime.InteropServices.Marshal.Copy(buff, 0, currentDstLeft, buffSize);
                    CopyMemory(currentDstLeft, buffAdr, buffSize);
                });
            }
        }

        // UI スレッド上で Parallel は使えない（この例では待機を放棄している）
        // http://dalmore.blog7.fc2.com/blog-entry-35.html
        private enum ParallelMode { Single, Parallel, ParallelForUIThread }
        private static void ParallelFor<T>(ParallelMode parallelMode, int fromInclusive, int toExclusive, Func<T> localInit, Action<int, T> body, Action<T> localFinally = null)
        {
            switch(parallelMode)
            {
                case ParallelMode.Single:
                case ParallelMode.ParallelForUIThread:
                    {
                        var local = localInit();
                        for (var i = fromInclusive; i < toExclusive; i++) body(i, local);
                        localFinally?.Invoke(local);
                        break;
                    }
                case ParallelMode.Parallel:
                    {
                        Parallel.For(fromInclusive, toExclusive, localInit, (i, state, local) => { body(i, local); return local; }, localFinally);
                        break;
                    }
                    /*
                case ParallelMode.ParallelForUIThread:
                    {
                        var thread = new Thread(() =>
                             Parallel.For(fromInclusive, toExclusive, localInit, (i, state, local) => { body(i, local); return local; }, null)
                         );
                        thread.Start();
                        thread.Join();
                        break;
                    }*/
            }
        }
        private static void ParallelFor(ParallelMode parallelMode, int fromInclusive, int toExclusive, Action<int> body)
        {
            switch (parallelMode)
            {
                case ParallelMode.Single:
                case ParallelMode.ParallelForUIThread:
                    {
                        for (var i = fromInclusive; i < toExclusive; i++) body(i);
                        break;
                    }
                case ParallelMode.Parallel:
                    {
                        Parallel.For(fromInclusive, toExclusive, body);
                        break;
                    }
                    /*
                case ParallelMode.ParallelForUIThread:
                    {
                        var thread = new Thread(() =>
                            Parallel.For(fromInclusive, toExclusive, body)
                        );
                        thread.Start();
                        thread.Join();
                        break;
                    }
                    */
            }
        }

        private enum ViewerModeEnum { Normal, MagnifierOpening, Magnifier, MagnifierClosing, CoverSetting }

        private ViewerModeEnum viewerMode = ViewerModeEnum.Normal;
        private ViewerModeEnum ViewerMode
        {
            get
            {
                return viewerMode;
            }
            set
            {
                var preCover = viewerMode == ViewerModeEnum.CoverSetting;
                viewerMode = value;
                var cover = value == ViewerModeEnum.CoverSetting;
                magnifierToolStripMenuItem.Checked = viewerMode == ViewerModeEnum.Magnifier;
                if (preCover != cover)
                {
                    if(cover)
                    {
                        ksViewForm.LButtonAcceptControls = new Control[0];
                    }
                    else
                    {
                        ksViewForm.LButtonAcceptControls = new Control[] { pbView };
                    }
                }
            }
        }

        class MagnifierCanvas : IDisposable
        {
            private VirtualBitmapEx[] virtualBitmapExArray;
            public VirtualBitmapEx[] Bitmaps { get { return virtualBitmapExArray; } }
            private Point[] offsetArray;
            public Point[] Offsets { get { return offsetArray; } }
            public int ImageCount { get { return virtualBitmapExArray.Length; } }

            private MagnifierCanvas() { }

            public MagnifierCanvas(Size[] zoomedSizeArray/*, BitmapEx[] originalBitmapExArray, bool isEntity*/)
            {
                virtualBitmapExArray = new VirtualBitmapEx[zoomedSizeArray.Length];
                offsetArray = new Point[zoomedSizeArray.Length];
                try
                {
                    for (var i = 0; i < zoomedSizeArray.Length; i++)
                    {
                        //var original = originalBitmapExArray[i];
                        //if (original == null) continue;
                        var size = zoomedSizeArray[i];
                        var width = size.Width;
                        var height = size.Height;
                        /*
                        VirtualBitmapEx element;
                        if (isEntity)
                        {
                            element = new VirtualBitmapEx(original.CreateNew(f =>
                            {
                                var pixelFormat = f.PixelFormat;
                                var result = new Bitmap(width, height, pixelFormat);
                                if ((pixelFormat & PixelFormat.Indexed) == PixelFormat.Indexed) result.Palette = f.Palette;
                                return result;
                            }));
                        }
                        else
                        {
                            element = new VirtualBitmapEx(original, zoomedSizeArray[i]);
                        }
                        virtualBitmapExArray[i] = element;
                        */
                    }
                }
                catch
                {
                    /*
                    for (var i = 0; i < zoomedSizeArray.Length; i++)
                    {
                        var bitmapEx = virtualBitmapExArray[i];
                        if (bitmapEx == null) break;
                        bitmapEx.Dispose();
                    }
                    */
                    throw;
                }
            }

            public void Dispose()
            {
                for (var i = 0; i < virtualBitmapExArray.Length; i++)
                {
                    var bitmapEx = virtualBitmapExArray[i];
                    if (bitmapEx != null)
                    {
                        bitmapEx.Dispose();
                        virtualBitmapExArray[i] = null;
                    }
                }
            }

            public VirtualBitmapEx this[int i]
            {
                get { return virtualBitmapExArray[i]; }
                set
                {
                    virtualBitmapExArray[i] = value;
                }
            }

            public void SetErrorImage(int i, Size size)
            {
                var bitmapEx = virtualBitmapExArray[i];
                //var size = bitmapEx.Size;
                bitmapEx?.Dispose();
                // bitmapEx = new VirtualBitmapEx(BitmapEx.ConvertToBitmapEx(Program.GetErrorImage(size.Width, size.Height)));
                virtualBitmapExArray[i] = VirtualBitmapEx.GetErrorImage(size);
            }

            public static MagnifierCanvas GetErrorCanvas(Size size)
            {
                var result = new MagnifierCanvas();
                result.virtualBitmapExArray = new VirtualBitmapEx[1] { VirtualBitmapEx.GetErrorImage(size) };
                result.offsetArray = new Point[1] { new Point(0, 0) };
                return result;
            }

            /*
            public static MagnifierCanvas FromBitmapEx(BitmapEx bitmapEx)
            {
                var result = new MagnifierCanvas();
                result.virtualBitmapExArray = new VirtualBitmapEx[1] { new  VirtualBitmapEx(bitmapEx)};
                result.offsetArray = new Point[1] { new Point(0, 0) };
                return result;
            }
            */
        }

        //private Tuple<int, int> Magnifier_VisiblePage = null;
        private int Magnifier_currentPageReady = -1;
        private MagnifierCanvas Magnifier_ZoomedPage = null;
        private int Magnifier_ZoomedPage_StartPage = -1;
        private int Magnifier_ZoomedPage_StopPage = -1;

        private void SetMagnifierCanvasInfo(int start, int stop)
        {
            Magnifier_ZoomedPage_StartPage = start;
            Magnifier_ZoomedPage_StopPage = stop;
        }
        private bool MagnifierCanvasPrepared()
        {
            if (Magnifier_ZoomedPage == null) return false;
            if (currentPage < 0) return false;
            if (currentPage != Magnifier_ZoomedPage_StartPage) return false;
            if (ResizedImageArray == null || currentPage >= ResizedImageArray.Length || ResizedImageArray[currentPage] == null) return false;
            var nextPage = NextPage;
            if (nextPage < currentPage || nextPage == currentPage && (MaxDivision <= 1 || getCurrentPageDivison() <= 1)) return false;
            if (nextPage == currentPage) nextPage++;
            return Magnifier_ZoomedPage_StopPage == nextPage;
        }

        private void bwMagnifierMaker_DoWork(object sender, DoWorkEventArgs e)
        {
            var tplArgs = (Tuple<int, int, int, int, Size[]>)e.Argument;
            var totalWidth = tplArgs.Item1;
            var zoomedHeight = tplArgs.Item2;
            var nextPage = tplArgs.Item3;
            var currentPage = tplArgs.Item4;
            var zoomedSizeArray = tplArgs.Item5;

            var arrangedZoomedSizeArray = new Size[nextPage - currentPage];
            var originalBitmapExArray = new BitmapEx[nextPage - currentPage];
            //var canvasIndex = 0;
            var nextPage1 = nextPage - 1;
            for (var i = nextPage1; i >= currentPage; i--)
            {
                var canvasIndex = nextPage1 - i;
                arrangedZoomedSizeArray[canvasIndex] = zoomedSizeArray[i - currentPage];
                originalBitmapExArray[canvasIndex] = PreFilteredImageArray[i];
                canvasIndex++;
            }

            //BitmapEx canvasExArray = new Bitmap(totalWidth, zoomedHeight, PixelFormat.Format24bppRgb);
            var canvas = new MagnifierCanvas(arrangedZoomedSizeArray/*, originalBitmapExArray, isEntity*/);
            var offsets = canvas.Offsets;

            try
            {
                var cumulativeWidth = 0;
                var pageLeftToRight = BindingMode == BindingMode.LeftToRight;
                //Graphics graphics = null;
                //canvasIndex = 0;
                for (var i = nextPage1; i >= currentPage; i--)
                {
                    var canvasIndex = nextPage1 - i;
                    //var canvas1 = canvas[canvasIndex];
                    var imgEx = originalBitmapExArray[canvasIndex];
                    var zoomedSize = zoomedSizeArray[i - currentPage];
                    var zoomedWidth = zoomedSize.Width;
                    var zoomedHeightI = zoomedSize.Height;
                    var X = pageLeftToRight ? totalWidth - cumulativeWidth - zoomedWidth : cumulativeWidth;
                    offsets[canvasIndex] = new Point(X, (zoomedHeight - zoomedHeightI) / 2);
                    var size = new Size(zoomedWidth, zoomedHeightI);
                    if (imgEx != null)
                    {
                        lock (imgEx)
                        {
                            var isEntity = imgEx.FrameCount == 1/* && BitmapEx.GetDataSizeInBytes(imgEx, zoomedSize) < GetUserMemoryUBound() / 2*/;
                            if (isEntity)
                            {
                                //var rect = new Rectangle(X, (zoomedHeight - zoomedHeightI) / 2, zoomedWidth, zoomedHeightI);
                                //var rect = new Rectangle(0, (zoomedHeight - zoomedHeightI) / 2, zoomedWidth, zoomedHeightI);

                                /*
                                if (rect.Y > 0)
                                {
                                    canvas1.ApplyToAll(frame =>
                                    {
                                        lockBits(frame, new Rectangle(0, 0, zoomedWidth, rect.Y), ImageLockMode.WriteOnly, $"fillMagnifierU{i}", data =>
                                        {
                                            FillRectangle(data, 0, 0, pbView.BackColor, zoomedWidth, rect.Y, ParallelMode.Parallel);
                                        });
                                        return frame;
                                    });
                                }
                                if (rect.Bottom < zoomedHeight)
                                {
                                    canvas1.ApplyToAll(frame =>
                                    {
                                        lockBits(frame, new Rectangle(0, rect.Bottom, zoomedWidth, zoomedHeight - rect.Bottom), ImageLockMode.WriteOnly, $"fillMagnifierL{i}", data =>
                                        {
                                            FillRectangle(data, 0, 0, pbView.BackColor, zoomedWidth, zoomedHeight - rect.Bottom, ParallelMode.Parallel);
                                        });
                                        return frame;
                                    });
                                }
                                */

                                var gamma = ResizeGammaMagnifier;

                                // 0.1 秒、またキャンセルも可能
                                ScalingAlgorithm scalingAlgorithm;
                                if ((long)zoomedWidth * zoomedHeight > (long)imgEx.Width * imgEx.Height)
                                {
                                    scalingAlgorithm = MagnifierScalingAlgorithm.ScaleUp;
                                }
                                else
                                {
                                    scalingAlgorithm = MagnifierScalingAlgorithm.ScaleDown;
                                }
                                const int pixelsPerPixelToBeLeft = int.MaxValue;
                                const bool parallel = true;

                                /*
                                for (var ii = 0; ii < 200; ii++)
                                {
                                    if (bwMagnifierMaker?.CancellationPending == true) break;
                                    Thread.Sleep(10);
                                }
                                */
                                

                                var canvas1 = imgEx.CreateNew(img =>
                                {
                                    if (bwMagnifierMaker.CancellationPending)
                                    {
                                        imgEx.CancelApply = true;
                                        return null;
                                    }

                                    switch (scalingAlgorithm)
                                    {
                                        case ScalingAlgorithm.HighSpeed: return QuickGraphic.CreateNew(img, size, parallel, bwMagnifierMaker);
                                        //case ScalingAlgorithm.HighSpeed: LongVectorImageResizer.Spline4(img, 2, canvas, rect, parallel, bwMagnifierMaker); break;
                                        case ScalingAlgorithm.NearestNeighbor: return LongVectorImageResizer.NearestNeighbor(img, size, parallel, bwMagnifierMaker);
                                        case ScalingAlgorithm.AreaAverage: return LongVectorImageResizer.AreaAverage(img, pixelsPerPixelToBeLeft, gamma, size, parallel, bwMagnifierMaker);
                                        case ScalingAlgorithm.Lanczos1: return LongVectorImageResizer.Lanczos(1, img, pixelsPerPixelToBeLeft, gamma, size, parallel, bwMagnifierMaker);
                                        case ScalingAlgorithm.Lanczos2: return LongVectorImageResizer.Lanczos(2, img, pixelsPerPixelToBeLeft, gamma, size, parallel, bwMagnifierMaker);
                                        case ScalingAlgorithm.Lanczos3: return LongVectorImageResizer.Lanczos(3, img, pixelsPerPixelToBeLeft, gamma, size, parallel, bwMagnifierMaker);
                                        case ScalingAlgorithm.Lanczos4: return LongVectorImageResizer.Lanczos(4, img, pixelsPerPixelToBeLeft, gamma, size, parallel, bwMagnifierMaker);
                                        case ScalingAlgorithm.Spline4: return LongVectorImageResizer.Spline4(img, pixelsPerPixelToBeLeft, gamma, size, parallel, bwMagnifierMaker);
                                        case ScalingAlgorithm.Spline16: return LongVectorImageResizer.Spline16(img, pixelsPerPixelToBeLeft, gamma, size, parallel, bwMagnifierMaker);
                                        case ScalingAlgorithm.Spline36: return LongVectorImageResizer.Spline36(img, pixelsPerPixelToBeLeft, gamma, size, parallel, bwMagnifierMaker);
                                        case ScalingAlgorithm.Spline64: return LongVectorImageResizer.Spline64(img, pixelsPerPixelToBeLeft, gamma, size, parallel, bwMagnifierMaker);
                                        //default: QuickGraphic.DrawImage(canvas, rect, img, bwMagnifierMaker); break;

                                        // 品質の良さを重視する場合
                                        // 他のオプションがあるので現在は不採用
                                        // BitmapResizer の使用を止めるならこちらを採用か
                                        //default: LongVectorImageResizer.Lanczos(3, img, 4, GammaConversion.Value1_0, canvas, rect, parallel, bwMagnifierMaker); break;

                                        // キャンセルが必要なので BitmapResizer は使えないがそれと同等の出力であることを重視
                                        // BitmapResizer は BitmapScalingMode.Unspecified を使用しており、これが Linear であることは以下に明記されている
                                        // https://msdn.microsoft.com/en-us/library/system.windows.media.bitmapscalingmode(v=vs.110).aspx
                                        default: return LongVectorImageResizer.Spline4(img, int.MaxValue, GammaConversion.Value1_0, size, parallel, bwMagnifierMaker);
                                    }
                                });

                                if (canvas1 == null)
                                {
                                    canvas.Dispose();
                                    return; // キャンセルがかかった場合
                                }

                                /*
                                canvas1.ApplyToAll((frame, frameNumber) =>
                                {
                                    var img = imgEx.Bitmaps[frameNumber];
                                    switch (scalingAlgorithm)
                                    {
                                        case ScalingAlgorithm.HighSpeed: QuickGraphic.DrawImage(frame, rect, img, bwMagnifierMaker); break;
                                    //case ScalingAlgorithm.HighSpeed: LongVectorImageResizer.Spline4(img, 2, canvas, rect, parallel, bwMagnifierMaker); break;
                                    case ScalingAlgorithm.NearestNeighbor: LongVectorImageResizer.NearestNeighbor(img, frame, rect, parallel, bwMagnifierMaker); break;
                                        case ScalingAlgorithm.AreaAverage: LongVectorImageResizer.AreaAverage(img, pixelsPerPixelToBeLeft, gamma, frame, rect, parallel, bwMagnifierMaker); break;
                                        case ScalingAlgorithm.Lanczos1: LongVectorImageResizer.Lanczos(1, img, pixelsPerPixelToBeLeft, gamma, frame, rect, parallel, bwMagnifierMaker); break;
                                        case ScalingAlgorithm.Lanczos2: LongVectorImageResizer.Lanczos(2, img, pixelsPerPixelToBeLeft, gamma, frame, rect, parallel, bwMagnifierMaker); break;
                                        case ScalingAlgorithm.Lanczos3: LongVectorImageResizer.Lanczos(3, img, pixelsPerPixelToBeLeft, gamma, frame, rect, parallel, bwMagnifierMaker); break;
                                        case ScalingAlgorithm.Lanczos4: LongVectorImageResizer.Lanczos(4, img, pixelsPerPixelToBeLeft, gamma, frame, rect, parallel, bwMagnifierMaker); break;
                                        case ScalingAlgorithm.Spline4: LongVectorImageResizer.Spline4(img, pixelsPerPixelToBeLeft, gamma, frame, rect, parallel, bwMagnifierMaker); break;
                                        case ScalingAlgorithm.Spline16: LongVectorImageResizer.Spline16(img, pixelsPerPixelToBeLeft, gamma, frame, rect, parallel, bwMagnifierMaker); break;
                                        case ScalingAlgorithm.Spline36: LongVectorImageResizer.Spline36(img, pixelsPerPixelToBeLeft, gamma, frame, rect, parallel, bwMagnifierMaker); break;
                                        case ScalingAlgorithm.Spline64: LongVectorImageResizer.Spline64(img, pixelsPerPixelToBeLeft, gamma, frame, rect, parallel, bwMagnifierMaker); break;
                                    //default: QuickGraphic.DrawImage(canvas, rect, img, bwMagnifierMaker); break;

                                    // 品質の良さを重視する場合
                                    // 他のオプションがあるので現在は不採用
                                    // BitmapResizer の使用を止めるならこちらを採用か
                                    //default: LongVectorImageResizer.Lanczos(3, img, 4, GammaConversion.Value1_0, canvas, rect, parallel, bwMagnifierMaker); break;

                                    // キャンセルが必要なので BitmapResizer は使えないがそれと同等の出力であることを重視
                                    // BitmapResizer は BitmapScalingMode.Unspecified を使用しており、これが Linear であることは以下に明記されている
                                    // https://msdn.microsoft.com/en-us/library/system.windows.media.bitmapscalingmode(v=vs.110).aspx
                                    default: LongVectorImageResizer.Spline4(img, int.MaxValue, GammaConversion.Value1_0, frame, rect, parallel, bwMagnifierMaker); break;
                                    }
                                    return frame;
                                });
                                */

                                //sw.Stop(); MessageBox.Show($"{sw.Elapsed}");
                                try
                                {
                                    if (ViewerMode != ViewerModeEnum.CoverSetting)
                                    {
                                        ViewerFormImageInfo imageInfo;
                                        try
                                        {
                                            imageInfo = OriginalImageInfoArray[i];
                                        }
                                        catch
                                        {
                                            imageInfo = null;
                                        }
                                        if (canvas1.FrameCount == 1)
                                        {
                                            if (imageFilter != null)
                                            {
                                                imageFilter.PostFilter(canvas1, ref imageInfo.MaxColorDiff, bwMagnifierMaker);
                                            }
                                            else
                                            {
                                                int? dummy = null;
                                                imageFilter.PostFilter(canvas1, ref dummy, bwMagnifierMaker);
                                            }
                                        }
                                        else
                                        {
                                            int? dummy = imageInfo?.MaxColorDiff ?? 255;
                                            canvas1.ApplyToAll(img =>
                                            {
                                                if(bwMagnifierMaker.CancellationPending)
                                                {
                                                    canvas1.CancelApply = true;
                                                    return img; // Dispose が必要なので null は誤り
                                                }
                                                imageFilter.PostFilter(img, ref dummy, bwMagnifierMaker);
                                                return img;
                                            });
                                        }
                                    }
                                    if (bwMagnifierMaker.CancellationPending)
                                    {
                                        canvas1.Dispose();
                                        canvas.Dispose();
                                        return;
                                    }
                                }
                                catch
                                {
                                    canvas1.Dispose();
                                    throw;
                                }

                                canvas[canvasIndex] = new VirtualBitmapEx(canvas1, 0);
                            }
                            else
                            {
                                canvas[canvasIndex] = GetVirtualBitmapExForMagnifier(imgEx, size);
                            }
                        }
                    }
                    else
                    {
                        canvas.SetErrorImage(canvasIndex, size);
                    }
                    cumulativeWidth += zoomedWidth;
                }
                //graphics?.Dispose();
            }
            catch
            {
                //if(canvas != null) canvas.Dispose();
                canvas.Dispose();
                //canvas = MagnifierCanvas.FromBitmapEx(BitmapEx.ConvertToBitmapEx(Program.GetErrorImage(totalWidth, zoomedHeight)));
                canvas = MagnifierCanvas.GetErrorCanvas(new Size(totalWidth, zoomedHeight));
            }

            e.Result = Tuple.Create(canvas, currentPage);
        }

        private VirtualBitmapEx GetVirtualBitmapExForStandard(BitmapEx source, Size virtualSize)
        {
            return GetVirtualBitmapEx(source, virtualSize, StandardScalingAlgorithm);
        }

        private VirtualBitmapEx GetVirtualBitmapExForMagnifier(BitmapEx source, Size virtualSize)
        {
            return GetVirtualBitmapEx(source, virtualSize, MagnifierScalingAlgorithm);
        }

        private static VirtualBitmapEx GetVirtualBitmapEx(BitmapEx source, Size virtualSize, ScalingAlgorithmPair algorithm)
        {
            return new VirtualBitmapEx(source, virtualSize, algorithm.ScaleDown == ScalingAlgorithm.NearestNeighbor,
                algorithm.ScaleUp == ScalingAlgorithm.AreaAverage || algorithm.ScaleUp == ScalingAlgorithm.NearestNeighbor);
            //upScaleNearestNeighbor: true);
        }

        private void bwMagnifierMaker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var t = e.Result as Tuple<MagnifierCanvas, int>;
            if (Magnifier_ZoomedPage != null) Magnifier_ZoomedPage.Dispose();
            Magnifier_ZoomedPage = t?.Item1;
            Magnifier_currentPageReady = t == null ? -1 : t.Item2;


            //optionToolStripMenuItem.Text = $"mag loaded: {ViewerMode} {bwMagnifierMaker_Q == null} {MagnifierPhase2_Q}";

            if (bwMagnifierMaker_Q == null)
            {
                //bmwLoadEachPage.ThreadCount = 1;
                if (MagnifierPhase2_Q)
                {
                    MagnifierPhase2_Q = false;
                    MagnifierPhase2();
                }
            }
            else
            {
                bwMagnifierMaker.RunWorkerAsync(bwMagnifierMaker_Q);
                bwMagnifierMaker_Q = null;
            }
        }

        private void IfTouchModifyingThenMoveOrigin(bool inverse)
        {
            if (GetMagnifierRectangle_LastCursorPosition != null)
            {
                if (BindingMode != BindingMode.RightToLeft)
                {
                    if (!inverse)
                    {
                        GetMagnifierRectangle_LastCursorPosition = new Point(int.MinValue, int.MinValue);
                    }
                    else
                    {
                        GetMagnifierRectangle_LastCursorPosition = new Point(int.MaxValue, int.MaxValue);
                    }
                }
                else
                {
                    if (!inverse)
                    {
                        GetMagnifierRectangle_LastCursorPosition = new Point(int.MaxValue, int.MinValue);
                    }
                    else
                    {
                        GetMagnifierRectangle_LastCursorPosition = new Point(int.MinValue, int.MaxValue);
                    }
                }
            }
        }
        private bool MagnifierPhase1_Q = false;
        private Point? MagnifierPhase1_QValue = null;
        private void MagnifierPhase1(Point? initialCenterLocation, bool ifNoopThenQ = false)
        {
            var nextPage = NextPage;
            //if (currentPage < 0 || nextPage <= currentPage) return;
            if (currentPage < 0 || nextPage < currentPage || nextPage == currentPage && getCurrentPageDivison() <= 1 || ResizedImageArray == null ||
                ResizedImageArray.Length <= currentPage || ResizedImageArray[currentPage] == null)
            {
                if (ifNoopThenQ)
                {
                    MagnifierPhase1_Q = true;
                    MagnifierPhase1_QValue = initialCenterLocation;
                }
                return;
            }
            ViewerMode = ViewerModeEnum.MagnifierOpening;
            ksViewForm.Enabled = false;
            pbView_Paint_Rectangle_FillAround = true;
            GetMagnifierRectangle_LastCursorPosition = initialCenterLocation;
            ShwoMagnifierRectangle();
            Program.HideCursor();
        }

        bool MagnifierPhase2_Q = false;
        private void MagnifierPhase2()
        {
            pbView_Paint_RectangleMessage = null;
            if (ViewerMode != ViewerModeEnum.MagnifierOpening)
            {
                ksViewForm.Enabled = true;
                return;
            }
            MagnifierPhase1_Q = false;
            if (Magnifier_ZoomedPage == null || Magnifier_currentPageReady != currentPage || bwMagnifierMaker.IsBusy)
            {
                MagnifierPhase2_Q = true;
            }
            if (MagnifierPhase2_Q) return;
            pbView_Paint_Rectangle = Rectangle.Empty;
            ViewerMode = ViewerModeEnum.Magnifier;
            ksViewForm.Enabled = true;
            //pbView.Invalidate(false); // 描画は Paint イベントハンドラで行われる
            //GetMagnifierRectangle_LastCursorPosition = null;
            pbPaintInvalidate();
        }

        private void MagnifierPhase3(bool toPhase4)
        {
            if (ViewerMode != ViewerModeEnum.Magnifier) return;
            ViewerMode = ViewerModeEnum.MagnifierClosing;
            ksViewForm.Enabled = false;
            pbView_Paint_Rectangle_FillAround = false;
            //GetMagnifierRectangle_LastCursorPosition = null;
            if (!toPhase4) ShwoMagnifierRectangle();
        }

        private void MagnifierPhase4(bool toPhase1)
        {
            pbView_Paint_RectangleMessage = null;
            if (ViewerMode != ViewerModeEnum.MagnifierClosing)
            {
                ksViewForm.Enabled = true;
                return;
            }
            ViewerMode = ViewerModeEnum.Normal;
            ksViewForm.Enabled = true;
            pbView_Paint_Rectangle = Rectangle.Empty;
            //pbView.Invalidate(false); // 描画は Paint イベントハンドラで行われる
            if (!toPhase1)
            {
                pbPaintInvalidate();
                Program.ShowCursor();
            }
        }

        private void ShwoMagnifierRectangle(bool invalidiate = true)
        {
            pbView_Paint_Rectangle = GetMagnifierRectangle(true, null);
            //pbView.Invalidate(false); // 描画は Paint イベントハンドラで行われる
            if (invalidiate) pbPaintInvalidate();
        }

        Point? GetMagnifierRectangle_LastCursorPosition = null;
        //VectorD GetMagnifierRectangle_LastCursorDelta;
        // bool GetMagnifierRectangle_LastSmooth;


        private Rectangle GetMagnifierRectangle(bool PreviewMode, VectorD? delta)
        {
            return GetMagnifierRectangle(PreviewMode, delta, pbView.Size, out var touchMode, out var imageRectangle, out var magPower_);
        }

        /// <summary>
        /// PreviewMode == true なら切り替え途中の長方形を取得、false なら拡大して描画すべき長方形を取得
        /// </summary>
        /// <param name="PreviewMode"></param>
        /// <returns></returns>
        private Rectangle GetMagnifierRectangle(bool PreviewMode, VectorD? delta, Size pbViewSize, out bool touchMode, out Rectangle imageRectangle, out double? magPower_)
        {
            var totalWidth = 0;
            var nextPage = NextPage;

            touchMode = false;

            /*
            if (currentPage < 0 || nextPage <= currentPage)
            {
                return Rectangle.Empty;
            }
            */

            magPower_ = getCurrentMagnifierPower();
            if (magPower_ == null || currentPage < 0 || nextPage < currentPage || MaxDivision <= 1 && nextPage == currentPage ||
                ResizedImageArray == null || currentPage >= ResizedImageArray.Length || ResizedImageArray[currentPage] == null)
            {
                imageRectangle = default(Rectangle);
                return Rectangle.Empty;
            }
            var magPower = (double)magPower_;
            var division = 1;
            if (currentPage == nextPage)
            {
                nextPage++;
                division = getCurrentPageDivison();
                if (division <= 1 || CurrentDividedPosition < 0 || CurrentDividedPosition >= division)
                {
                    imageRectangle = default(Rectangle);
                    return Rectangle.Empty;
                }
            }
            for (var i = currentPage; i < nextPage; i++)
            {
                var img = ResizedImageArray[i];
                if (img == null)
                {
                    imageRectangle = default(Rectangle);
                    return Rectangle.Empty;
                }
                totalWidth += img.Width;
            }

            var divisionOffset = 0;
            if (division > 1)
            {
                int p;
                if (BindingMode != BindingMode.RightToLeft)
                {
                    p = CurrentDividedPosition;
                }
                else
                {
                    p = division - 1 - CurrentDividedPosition;
                }
                var right = (totalWidth * (p + 1) + division / 2) / division;
                var left = (totalWidth * p + division / 2) / division;
                totalWidth = right - left;
                divisionOffset = left;
            }
            
            var totalHeight = 0;
            for (var i = currentPage; i < nextPage; i++)
            {
                var h = ResizedImageArray[i].Height;
                if (totalHeight < h) totalHeight = h;
            }

            int offsetX;
            if (division == 1 && nextPage > currentPage + 1 || !(currentPage == 0 || nextPage == ResizedImageArray.Length) ||
               !(BindingMode == BindingMode.LeftToRight || BindingMode == BindingMode.RightToLeft) || totalWidth > pbViewSize.Width / 2)
            {
                if (nextPage == currentPage + 2 && ResizedImageArray[currentPage].Width <= pbViewSize.Width / 2 && ResizedImageArray[currentPage + 1].Width <= pbViewSize.Width / 2)
                {
                    offsetX = pbViewSize.Width / 2 - ResizedImageArray[BindingMode == BindingMode.LeftToRight ? currentPage : (currentPage + 1)].Width;
                }
                else
                {
                    offsetX = (pbViewSize.Width - totalWidth) / 2;
                }
            }
            else
            {
                if (BindingMode == BindingMode.LeftToRight ^ currentPage == 0)
                {
                    offsetX = pbViewSize.Width / 2 - totalWidth;
                }
                else
                {
                    offsetX = pbViewSize.Width / 2;
                }
            }
            
            int offsetY = (pbViewSize.Height - totalHeight) / 2;

            var pointScreen = Cursor.Position;
            var pointClient = pbView.PointToClient(pointScreen);
            PointD pointAsLocation;

            //PointD GetMagnifierRectangle_LastRectanglePosition;

            //var size = new Size((int)Math.Round((2 * pbViewSize.Width + MagnifyingPower) / (2 * MagnifyingPower)), (int)Math.Round((2 * pbViewSize.Height + MagnifyingPower) / (2 * MagnifyingPower)));
            var size = GetPreviewModeMagnifierRectangleSize(pbViewSize, magPower);

            var whInt = size.Width / 2;
            var hhInt = size.Height / 2;
            var cursorXMin = offsetX + whInt;
            var corsorXMax = offsetX + totalWidth - size.Width + whInt;
            var corsorYMin = offsetY + hhInt;
            var corsorYMax = offsetY + totalHeight - size.Height + hhInt;

            //if (!PreviewMode && delta == null || PreviewMode && GetMagnifierRectangle_LastCursorPosition == null)
            if (delta == null && GetMagnifierRectangle_LastCursorPosition == null)
            {
                pointAsLocation = pointClient;
                
                var pointTruncated = pbView.PointToScreen(new Point(
                        size.Width < totalWidth ? Math.Max(cursorXMin, Math.Min(corsorXMax, pointClient.X)) : offsetX + totalWidth / 2,
                        size.Height < totalHeight ? Math.Max(corsorYMin, Math.Min(corsorYMax, pointClient.Y)) : offsetY + totalHeight / 2));

                if (pointScreen != pointTruncated)
                {
                    Cursor.Position = pointTruncated;
                }

                //GetMagnifierRectangle_LastRectanglePosition = new PointD(pointTruncated.X - size.Width / 2/0, pointTruncated.Y - size.Height / 2.0);
            }
            else
            {
                touchMode = true;

                var wh = size.Width / 2.0;
                //var whMinus = wh - 0.25;
                var hh = size.Height / 2.0;
                //var hhMinus = hh - 0.25;

                if (GetMagnifierRectangle_LastCursorPosition != null)
                {
                    //var delta = new VectorD(pointClient, (Point)GetMagnifierRectangle_LastCursorPosition);

                    pointAsLocation = (Point)GetMagnifierRectangle_LastCursorPosition;

                    // 差分に不自然さがなければ現在位置を更新
                    //var preDelta = GetMagnifierRectangle_LastCursorDelta;
                    //GetMagnifierRectangle_LastCursorDelta = (VectorD)delta;
                    //var smooth = sharpness(delta, preDelta) < 1;

                    //if (!GetMagnifierRectangle_LastSmooth) delta += preDelta;

                    //if (smooth)
                    {

                        if (delta != null)
                        {
                            // 移動前にも Truncate
                            pointAsLocation.X = Math.Max(cursorXMin, Math.Min(corsorXMax, pointAsLocation.X));
                            pointAsLocation.Y = Math.Max(corsorYMin, Math.Min(corsorYMax, pointAsLocation.Y));

                            var magDelta = (VectorD)delta / magPower;
                            pointAsLocation -= magDelta;

                            //pointAsLocation.X = Math.Max(offsetX + wh, Math.Min(offsetX + totalWidth - wh, pointAsLocation.X));
                            //pointAsLocation.Y = Math.Max(offsetY + hh, Math.Min(offsetY + totalHeight - hh, pointAsLocation.Y));
                        }
                        

                        /*
                        var left = offsetX + wh;
                        var right = offsetX + totalWidth - -wh;
                        
                        if (pointAsLocation.X < offsetX)
                            pointAsLocation.X = offsetX;
                        else if (pointAsLocation.X > offsetX + totalWidth - size.Width)
                            pointAsLocation.X = offsetX + totalWidth - size.Width;

                        if (pointAsLocation.Y < offsetY)
                            pointAsLocation.Y = offsetY;
                        else if (pointAsLocation.Y > offsetY + totalHeight - size.Height)
                            pointAsLocation.Y = offsetY + totalHeight - size.Height;
                            */
                            
                    }

                    //GetMagnifierRectangle_LastSmooth = smooth;

                    GetMagnifierRectangle_LastCursorPosition = new PointD(Math.Max(cursorXMin, Math.Min(corsorXMax, pointAsLocation.X)),
                        Math.Max(corsorYMin, Math.Min(corsorYMax, pointAsLocation.Y))).Round();
                    //GetMagnifierRectangle_LastCursorPosition = pointAsLocation.Round();// pointClient;
                }
                else
                {
                    GetMagnifierRectangle_LastCursorPosition = pointClient;
                    //GetMagnifierRectangle_LastRectanglePosition = BindingMode != BindingMode.RightToLeft ?
                    //    new PointD(offsetX, offsetY) : new PointD(offsetX + totalWidth - size.Width, offsetY);
                    //GetMagnifierRectangle_LastCursorDelta = VectorD.Zero;

                    pointAsLocation = BindingMode != BindingMode.RightToLeft ?
                        new PointD(offsetX + wh, offsetY + hh) : new PointD(offsetX + totalWidth - wh, offsetY + hh);
                }

                //pointAsLocation = new PointD(GetMagnifierRectangle_LastRectanglePosition.X + size.Width / 2.0,
                //   GetMagnifierRectangle_LastRectanglePosition.Y + size.Height / 2.0);

            }

            if (PreviewMode)
            {
                var boundX = offsetX + totalWidth - size.Width;
                var boundY = offsetY + totalHeight - size.Height;

                imageRectangle = new Rectangle(offsetX, offsetY, totalWidth, totalHeight);

                return new Rectangle(
                    (int)Math.Round(Math.Max(offsetX, Math.Min(boundX, pointAsLocation.X - whInt/* size.Width / 2.0*/))),
                    (int)Math.Round(Math.Max(offsetY, Math.Min(boundY, pointAsLocation.Y - hhInt/*size.Height / 2.0*/))),
                    Math.Min(size.Width, totalWidth),
                    Math.Min(size.Height, totalHeight));
            }
            else
            {
                imageRectangle = default(Rectangle);
                return new Rectangle(
                    (int)Math.Round(Math.Max(0, Math.Min(totalWidth * magPower - pbViewSize.Width, (pointAsLocation.X - offsetX) * magPower - pbViewSize.Width / 2)
                        ) + divisionOffset * magPower),
                    (int)Math.Round(Math.Max(0, Math.Min(totalHeight * magPower - pbViewSize.Height, (pointAsLocation.Y - offsetY) * magPower - pbViewSize.Height / 2))),
                    (int)Math.Round(Math.Min(pbViewSize.Width, totalWidth * magPower)),
                    (int)Math.Round(Math.Min(pbViewSize.Height, totalHeight * magPower)));
            }
        }

        private bool CursorBounded()
        {
            return (viewerMode == ViewerModeEnum.Magnifier || viewerMode == ViewerModeEnum.MagnifierOpening || viewerMode == ViewerModeEnum.MagnifierClosing) && GetMagnifierRectangle_LastCursorPosition == null;
        }

        private enum ScrollResult { Scrolled, NotMoveAnymore, UnsupportedMode }
        private ScrollResult MagnifierAutoScroll(bool back, bool prioritizeHorizontal, double horizontalOverwrap, double verticalOverwrap)
        {
            if (horizontalOverwrap < 0) throw new ArgumentOutOfRangeException(nameof(horizontalOverwrap));
            if (verticalOverwrap < 0) throw new ArgumentOutOfRangeException(nameof(verticalOverwrap));

            if (ViewerMode != ViewerModeEnum.Magnifier) return ScrollResult.UnsupportedMode;

            if (GetMagnifierRectangle_LastCursorPosition == null)
            {
                GetMagnifierRectangle_LastCursorPosition = pbView.PointToClient(Cursor.Position);
            }

            var pbViewSize = pbView.Size;
            var magRectAsPreview = GetMagnifierRectangle(PreviewMode: true, delta: null, pbViewSize: pbViewSize, touchMode: out var touchMode, imageRectangle: out var imageRect, magPower_: out var magPower_);
            if (magPower_ == null) return ScrollResult.NotMoveAnymore;

            var shift = VectorD.Zero;

            for (var count = 0; count < 2; count++)
            {
                Interval interval;
                bool reverse;
                double overwrap;
                int bound;
                var first = count == 0;
                var vertical = first ^ prioritizeHorizontal;
                if (vertical)
                {
                    interval = Interval.FromVertical(magRectAsPreview);
                    interval.Start -= imageRect.Top;
                    reverse = back;
                    overwrap = verticalOverwrap;
                    bound = imageRect.Height;
                }
                else
                {
                    interval = Interval.FromHorizontal(magRectAsPreview);
                    interval.Start -= imageRect.Left;
                    reverse = BindingMode == BindingMode.RightToLeft ^ back;
                    overwrap = horizontalOverwrap;
                    bound = imageRect.Width;
                }
                var start0 = interval.Start;
                if (reverse) interval.Start = bound - interval.Stop;
                var notMoveAnymore = interval.Stop >= bound;
                if (notMoveAnymore)
                {
                    if (!first) return ScrollResult.NotMoveAnymore;
                    interval.Start = 0;
                }
                else
                {
                    var absOverwrap = overwrap < 1 ? overwrap * interval.Length : overwrap;

                    var shiftLocal = interval.Length - absOverwrap;

                    // interval.Length + n * fitted == bound なる整数 n が存在する shiftLocal 以下の fitted の中で最大のもの
                    var bl = bound - interval.Length;
                    if (bl > 0)
                    {
                        var n = Math.Ceiling(bl / shiftLocal);
                        //optionToolStripMenuItem.Text = n.ToString() + "  " + (xxxx++);
                        var fitted = bl / n;
                        shiftLocal = fitted;
                    }

                    interval.Start += Math.Max(1, (int)Math.Ceiling(shiftLocal));
                }
                if (reverse) interval.Start = bound - interval.Stop;
                var shift_ = interval.Start - start0;
                if (vertical) shift.Y = shift_; else shift.X = shift_;
                if (!notMoveAnymore) break;
            }
            if (shift.IsZero) return ScrollResult.NotMoveAnymore;
            TouchListener_Pan_Delta = -shift * (double)magPower_;
            pbPaintInvalidate();
            return ScrollResult.Scrolled;
        }
        //int xxxx = 0;

        // 16:10 で横が3段階にならないようにするには横のオーバーラップが 2 - 1.25√2 ≒ 0.232233 以下でなければならない
        // 縦を3段階にするには縦のオーバーラップが 0 より大きくなければならない
        private void MagnifierAutoScrollForwardWith20PercentOverwrap()
        {
            MagnifierAutoScroll(back: false, prioritizeHorizontal: false, horizontalOverwrap: 0.2, verticalOverwrap: 0.499999);
        }
        private void MagnifierAutoScrollBackWith20PercentOverwrap()
        {
            MagnifierAutoScroll(back: true, prioritizeHorizontal: false, horizontalOverwrap: 0.2, verticalOverwrap: 0.499999);
        }
        private void MagnifierAutoScrollForwardWith0PercentOverwrap()
        {
            MagnifierAutoScroll(back: false, prioritizeHorizontal: false, horizontalOverwrap: 0, verticalOverwrap: 0);
        }
        private void MagnifierAutoScrollBackWith0PercentOverwrap()
        {
            MagnifierAutoScroll(back: true, prioritizeHorizontal: false, horizontalOverwrap: 0, verticalOverwrap: 0);
        }
        private void MoveForwardOrMagnifierAutoScrollWith20PercentOverwrap()
        {
            var r = MagnifierAutoScroll(back: false, prioritizeHorizontal: false, horizontalOverwrap: 0.2, verticalOverwrap: 0.499999);
            if (r != ScrollResult.Scrolled) moveToNextPage();
        }
        private void MoveBackOrMagnifierAutoScrollWith20PercentOverwrap()
        {
            var r = MagnifierAutoScroll(back: true, prioritizeHorizontal: false, horizontalOverwrap: 0.2, verticalOverwrap: 0.499999);
            if (r != ScrollResult.Scrolled) MoveToPreviousPage();
        }
        private void MoveForwardOrMagnifierAutoScrollWith0PercentOverwrap()
        {
            var r = MagnifierAutoScroll(back: false, prioritizeHorizontal: false, horizontalOverwrap: 0, verticalOverwrap: 0);
            if (r != ScrollResult.Scrolled) moveToNextPage();
        }
        private void MoveBackOrMagnifierAutoScrollWith0PercentOverwrap()
        {
            var r = MagnifierAutoScroll(back: true, prioritizeHorizontal: false, horizontalOverwrap: 0, verticalOverwrap: 0);
            if (r != ScrollResult.Scrolled) MoveToPreviousPage();
        }

        private void MagnifierAutoScrollRightWith20PercentOverwrap()
        {
            if (BindingMode != BindingMode.RightToLeft)
            {
                MagnifierAutoScrollForwardWith20PercentOverwrap();
            }
            else
            {
                MagnifierAutoScrollBackWith20PercentOverwrap();
            }
        }
        private void MagnifierAutoScrollLeftWith20PercentOverwrap()
        {
            if (BindingMode == BindingMode.RightToLeft)
            {
                MagnifierAutoScrollForwardWith20PercentOverwrap();
            }
            else
            {
                MagnifierAutoScrollBackWith20PercentOverwrap();
            }
        }
        private void MagnifierAutoScrollRightWith0PercentOverwrap()
        {
            if (BindingMode != BindingMode.RightToLeft)
            {
                MagnifierAutoScrollForwardWith0PercentOverwrap();
            }
            else
            {
                MagnifierAutoScrollBackWith0PercentOverwrap();
            }
        }
        private void MagnifierAutoScrollLeftWith0PercentOverwrap()
        {
            if (BindingMode == BindingMode.RightToLeft)
            {
                MagnifierAutoScrollForwardWith0PercentOverwrap();
            }
            else
            {
                MagnifierAutoScrollBackWith0PercentOverwrap();
            }
        }
        private void MoveRightOrMagnifierAutoScrollWith20PercentOverwrap()
        {
            if (BindingMode != BindingMode.RightToLeft)
            {
                MoveForwardOrMagnifierAutoScrollWith20PercentOverwrap();
            }
            else
            {
                MoveBackOrMagnifierAutoScrollWith20PercentOverwrap();
            }
        }
        private void MoveLeftOrMagnifierAutoScrollWith20PercentOverwrap()
        {
            if (BindingMode == BindingMode.RightToLeft)
            {
                MoveForwardOrMagnifierAutoScrollWith20PercentOverwrap();
            }
            else
            {
                MoveBackOrMagnifierAutoScrollWith20PercentOverwrap();
            }
        }
        private void MoveRightOrMagnifierAutoScrollWith0PercentOverwrap()
        {
            if (BindingMode != BindingMode.RightToLeft)
            {
                MoveForwardOrMagnifierAutoScrollWith0PercentOverwrap();
            }
            else
            {
                MoveBackOrMagnifierAutoScrollWith0PercentOverwrap();
            }
        }
        private void MoveLeftOrMagnifierAutoScrollWith0PercentOverwrap()
        {
            if (BindingMode == BindingMode.RightToLeft)
            {
                MoveForwardOrMagnifierAutoScrollWith0PercentOverwrap();
            }
            else
            {
                MoveBackOrMagnifierAutoScrollWith0PercentOverwrap();
            }
        }

        private Size GetPreviewModeMagnifierRectangleSize(Size pbViewSize, double magPower)
        {
            // オートスクロールのオーバーラップ0での動作との兼ね合いで Ceiling
            return new Size((int)Math.Ceiling(pbViewSize.Width / magPower), (int)Math.Ceiling(pbViewSize.Height / magPower));
        }

        public struct PointD
        {
            public double X, Y;
            public PointD(double x, double y) { X = x; Y = y; }
            public static implicit operator PointD(Point point)
            {
                return new PointD(point.X, point.Y);
            }
            public Point Round()
            {
                return new Point(
                    (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, Math.Round(X))),
                    (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, Math.Round(Y))));
            }
        }

        public struct VectorD
        {
            public double X, Y;
            public VectorD(int x, int y) { X = x; Y = y; }
            public VectorD(Point point) { X = point.X; Y = point.Y; }
            public VectorD(Point point, Point origin) { X = point.X - origin.X; Y = point.Y - origin.Y; }

            public override string ToString()
            {
                return $"({X}, {Y})";
            }

            public static VectorD operator *(VectorD vector, double scalar)
            {
                vector.X *= scalar;
                vector.Y *= scalar;
                return vector;
            }

            public static VectorD operator *(double scalar, VectorD vector)
            {
                vector.X *= scalar;
                vector.Y *= scalar;
                return vector;
            }

            public static VectorD operator +(VectorD a, VectorD b)
            {
                a.X += b.X;
                a.Y += b.Y;
                return a;
            }

            public static PointD operator +(PointD a, VectorD b)
            {
                a.X += b.X;
                a.Y += b.Y;
                return a;
            }

            public static VectorD operator -(VectorD a)
            {
                a.X = -a.X;
                a.Y = -a.Y;
                return a;
            }

            public static PointD operator -(PointD a, VectorD b)
            {
                a.X -= b.X;
                a.Y -= b.Y;
                return a;
            }

            public static VectorD operator /(VectorD vector, double scalar)
            {
                vector.X /= scalar;
                vector.Y /= scalar;
                return vector;
            }

            public double Length2 { get { return X * X + Y * Y; } }

            public bool IsZero { get { return X == 0 && Y == 0; } }

            public static readonly VectorD Zero = new VectorD(0, 0);
        }

        private static double sharpness(VectorD delta, VectorD preDelta)
        {
            try
            {
                if (delta.IsZero)
                {
                    return preDelta.IsZero ? 0 : double.PositiveInfinity;
                }
                else if (preDelta.IsZero)
                {
                    return double.PositiveInfinity;
                }

                var del2 = delta.Length2;
                var pre2 = preDelta.Length2;
                if (del2 < pre2)
                {
                    var temp = delta;
                    delta = preDelta;
                    preDelta = temp;
                    pre2 = del2;
                }

                var r = (delta.X * preDelta.X + delta.Y * preDelta.Y) / pre2;
                var i = (delta.X * preDelta.Y - delta.Y * preDelta.X) / pre2;

                //var logR = Math.Log(r * r + i * i) / 2;
                //var logI = Math.Atan2(r, i);

                var logR = r - 1;
                var logI = i;

                return logR * logR + logI * logI;
            }
            catch
            {
                return double.PositiveInfinity;
            }
        }


        private void ShwoCoverRectangle()
        {
            pbView_Paint_Rectangle = GetCoverRectangleForDisplay();
            //pbView.Invalidate(false); // 描画は Paint イベントハンドラで行われる
            pbPaintInvalidate();
        }

        private static bool isDirSafety(string path)
        {
            try
            {
                return Directory.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        private void SetCoverForThumbnail(bool delete = false, Point? clientPoint = null)
        {
            //if (string.IsNullOrEmpty(currentArchiveFilePath)) return;

            Program.ShowCursor();

            var prevArchiveFilePath = getCurrentRenameTarget();
            if (string.IsNullOrEmpty(prevArchiveFilePath))
            {
                Program.ShowCursor();
                MessageBox.Show(this, (new FileNotFoundException(null, $"{currentArchiveFilePath}")).Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                ExitCoverSetting();
                //OpenFile(newArchiveFilePath); // こちらは不適切
                //OpenFile(prevArchiveFilePath, currentPage); // loader をまだ null にしていないので再読み込みは不要
                return;
            }
            var zipPlaInfo = new ZipPlaInfo(prevArchiveFilePath);
            zipPlaInfo.ThumbnailInfo = delete ? null : GetThumbnailInfoAsTuple(clientPoint);
            var newArchiveFilePath = zipPlaInfo.GetPathOfCurrentInfo(isDirSafety(prevArchiveFilePath));

            Program.ShowCursor();

            /*
            if (MessageBox.Show(this, (delete ? Message.WillClearCoverSetting : Message.RenameForCover) + "\r\n\r\n" +
                Message.BeforeRename + ":\r\n  " + Path.GetFileName(prevArchiveFilePath) + "\r\n" +
                Message.AfterRename + ":\r\n  " + Path.GetFileName(newArchiveFilePath) + "\r\n", Message.Question, MessageBoxButtons.OKCancel, MessageBoxIcon.Question)
                != DialogResult.OK)
            {
                ExitCoverSetting();
                return;
            }
            */
            if (MessageForm.Show(this, (delete ? Message.WillClearCoverSetting : Message.RenameForCover) + "\r\n\r\n" +
                Message.BeforeRename + ":\r\n  " + Path.GetFileName(prevArchiveFilePath) + "\r\n" +
                Message.AfterRename + ":\r\n  " + Path.GetFileName(newArchiveFilePath) + "\r\n", Message.Question, Message.OK, Message.Cancel, MessageBoxIcon.Question)
                != 0)
            {
                ExitCoverSetting();
                return;
            }

            if (loader != null)
            {
                loader.Dispose();
                loader = null;
            }
            try
            {
                Program.FileOrDirectoryMove(prevArchiveFilePath, newArchiveFilePath, retryOwner: this);
                //currentArchiveFilePath = newArchiveFilePath;
            }
            catch //(Exception error)
            {
                //MessageBox.Show(this, error.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error); // よくあることなので。
                ExitCoverSetting();
                //OpenFile(newArchiveFilePath); // こちらは不適切
                OpenFile(prevArchiveFilePath, currentPage, currentSortModeDetails); // loader を null にしてしまっているので再読み込みは必要
                return;
            }

            // 名前の変更を報告
            SendRenameMessageToCatalogForm(prevArchiveFilePath, newArchiveFilePath);

            //MessageBox.Show(this, Message.DoneCoverSetting, Message.Information, MessageBoxButtons.OK, MessageBoxIcon.Information);
            ExitCoverSetting();
            OpenFile(newArchiveFilePath, currentPage, currentSortModeDetails);
        }

        private void ExitCoverSetting()
        {
            if (ExitAfterCoverSetting)
            {
                Application.Exit();
            }

            ViewerMode = ViewerModeEnum.Normal;
            pbView_Paint_Rectangle = Rectangle.Empty;
            //pbView.Invalidate(false); // 描画は Paint イベントハンドラで行われる
            if (imageFilter.PreFilterExists() ||
                 (BindingMode != BindingMode.SinglePage && BindingMode != BindingMode.SinglePageWithoutScalingUp && MaxDivision > 1))
            {
                reloadForChangeFilter(forOnlyPostFilter: false);
            }
            else if (imageFilter.PostFilterExists())
            {
                reloadForChangeFilter(forOnlyPostFilter: true);
            }
            else
            {
                pbPaintInvalidate();
                //reloadForChangePreFilter();
            }
        }

        private void SendRenameMessageToCatalogForm(string prevArchiveFilePath, string newArchiveFilePath)
        {
            Program.ShowCursor();

            if (ipcReportFromViewerToCatalog != null)
            {
                try
                {
                    var rep = new IpcReportFromViewerToCatalog.OldAndNewString();
                    rep.Old = prevArchiveFilePath;
                    rep.New = newArchiveFilePath;

                    // フィールドとして作った PathRequiredReload が MarshalByRefObject の継承によってプロパティのように振る舞うのでこの方法は使えない。
                    //ipcReportFromViewerToCatalog.PathRequiredReload.Add(rep);

                    var repList = ipcReportFromViewerToCatalog.PathRequiredReload;
                    repList.Add(rep);
                    ipcReportFromViewerToCatalog.PathRequiredReload = repList;
                }
                catch (RemotingException)
                {
                    ipcReportFromViewerToCatalog = null;
                }
                catch (Exception error)
                {
                    Program.AlertError(error);
                }

            }
        }

        private void RequestToUpdateTags()
        {
            if (ipcReportFromViewerToCatalog != null)
            {
                try
                {
                    var rep = new IpcReportFromViewerToCatalog.OldAndNewString();
                    rep.RequestToReloadTags = true;

                    var repList = ipcReportFromViewerToCatalog.PathRequiredReload;
                    repList.Add(rep);
                    ipcReportFromViewerToCatalog.PathRequiredReload = repList;
                }
                catch (RemotingException)
                {
                    ipcReportFromViewerToCatalog = null;
                }
                catch (Exception error)
                {
                    Program.AlertError(error);
                }
            }
        }

        enum SelfLastAccessTimeUpdateMode { None, Reserve, Force }

        string TryUpdateLastAccessTimeAfterLoaderDisposed = null;
        private void SendSorMessageToCatalogForm(string archiveFilePath, string sorPath, int page, bool requestToUpdateLastAccessTime, SelfLastAccessTimeUpdateMode m)
        {
            SendSorMessageToCatalogForm(new string[] { archiveFilePath }, new string[] { sorPath }, new int[] { page }, new bool[] { requestToUpdateLastAccessTime }, m);
        }
        private void SendSorMessageToCatalogForm(IReadOnlyList<string> archiveFilePath, IReadOnlyList<string> sorPath, IReadOnlyList<int> page, IReadOnlyList<bool> requestToUpdateLastAccessTime, SelfLastAccessTimeUpdateMode m)
        {
            Program.ShowCursor();

            if (ipcReportFromViewerToCatalog != null)
            {
                try
                {
                    var count = archiveFilePath.Count;
                    var reps = new IpcReportFromViewerToCatalog.OldAndNewString[count];
                    for (var i = 0; i < count; i++)
                    {
                        var rep = new IpcReportFromViewerToCatalog.OldAndNewString();
                        rep.New = rep.Old = archiveFilePath[i];
                        rep.Sor = sorPath[i];
                        rep.NewPage = page[i];
                        rep.RequestToUpdateLastAccessTime = requestToUpdateLastAccessTime[i];
                        reps[i] = rep;
                    }

                    // フィールドとして作った PathRequiredReload が MarshalByRefObject の継承によってプロパティのように振る舞うのでこの方法は使えない。
                    //ipcReportFromViewerToCatalog.PathRequiredReload.Add(rep);

                    var repList = ipcReportFromViewerToCatalog.PathRequiredReload;
                    repList.AddRange(reps);
                    ipcReportFromViewerToCatalog.PathRequiredReload = repList;
                }
                catch (RemotingException)
                {
                    ipcReportFromViewerToCatalog = null;
                }
                catch (Exception error)
                {
                    Program.AlertError(error);
                }
            }
            else if (m != SelfLastAccessTimeUpdateMode.None)
            {
                try
                {
                    var count = archiveFilePath.Count;
                    for (var i = 0; i < count; i++)
                    {
                        if (requestToUpdateLastAccessTime[i])
                        {
                            if (m == SelfLastAccessTimeUpdateMode.Reserve)
                            {
                                TryUpdateLastAccessTimeAfterLoaderDisposed = archiveFilePath[i];
                                return;
                            }
                            else
                            {
                                if (loader != null)
                                {
                                    loader.Dispose();
                                    loader = null;
                                }
                                Program.TryUpdateLastAccessTime(archiveFilePath[i]);
                            }
                        }
                    }
                }
                catch (Exception error)
                {
                    Program.AlertError(error);
                }
            }
        }
        private void SendRequestToChangeSelection(string path, SortMode? viewerFormSortMode = null)
        {
            if (path != null && ipcReportFromViewerToCatalog != null && ipcLookAheadInfo == null && Visible)
            {
                try
                {
                    var rep = new IpcReportFromViewerToCatalog.OldAndNewString();
                    rep.New = path;
                    rep.RequestToChangeSelection = true;
                    rep.ViewerFormSortMode = viewerFormSortMode ?? (currentSortModeDetails == null ? SortMode.NameInAsc : currentSortModeDetails.SortMode);
                    var repList = ipcReportFromViewerToCatalog.PathRequiredReload;
                    repList = repList.Where(x => x != null).ToList();
                    repList.Add(rep);
                    ipcReportFromViewerToCatalog.PathRequiredReload = repList;
                }
                catch (RemotingException)
                {
                    ipcReportFromViewerToCatalog = null;
                }
                catch (Exception error)
                {
                    Program.AlertError(error);
                }

            }
        }

        private void SetPageDirectionToFileName(bool delete = false)
        {
            //if (string.IsNullOrEmpty(currentArchiveFilePath)) return;

            var prevArchiveFilePath = getCurrentRenameTarget();
            if (string.IsNullOrEmpty(prevArchiveFilePath)) return;
            var zipPlaInfo = new ZipPlaInfo(prevArchiveFilePath);
            var binding = BindingMode;
            zipPlaInfo.BindingModeForSet = delete ? null : (BindingMode?)binding;
            var newArchiveFilePath = zipPlaInfo.GetPathOfCurrentInfo(isDirSafety(prevArchiveFilePath));

            Program.ShowCursor();
            /*
            if (MessageBox.Show(
                (delete ? Message.WillClearPageSequenceSetting : binding == BindingMode.LeftToRight ? Message.RenameForPageSequenceLeftToRight :
                binding == BindingMode.RightToLeft ? Message.RenameForPageSequenceRightToLeft : Message.RenameForPageSequenceSinglePage) + "\r\n\r\n" +
                Message.BeforeRename + ":\r\n  " + Path.GetFileName(prevArchiveFilePath) + "\r\n" +
                Message.AfterRename + ":\r\n  " + Path.GetFileName(newArchiveFilePath) + "\r\n", Message.Question, MessageBoxButtons.OKCancel, MessageBoxIcon.Question)
                != DialogResult.OK)
            {
                return;
            }
            */
            if (MessageForm.Show(this,
                (delete ? Message.WillClearPageSequenceSetting : binding == BindingMode.LeftToRight ? Message.RenameForPageSequenceLeftToRight :
                binding == BindingMode.RightToLeft ? Message.RenameForPageSequenceRightToLeft : Message.RenameForPageSequenceSinglePage) + "\r\n\r\n" +
                Message.BeforeRename + ":\r\n  " + Path.GetFileName(prevArchiveFilePath) + "\r\n" +
                Message.AfterRename + ":\r\n  " + Path.GetFileName(newArchiveFilePath) + "\r\n", Message.Question, Message.OK, Message.Cancel, MessageBoxIcon.Question)
                != 0)
            {
                return;
            }

            if (loader != null)
            {
                loader.Dispose();
                loader = null;
            }
            try
            {
                Program.FileOrDirectoryMove(prevArchiveFilePath, newArchiveFilePath, retryOwner: this);
                //currentArchiveFilePath = newArchiveFilePath;
            }
            catch// (Exception error)
            {
                //MessageBox.Show(this, error.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error); // よくあることなので。
                //OpenFile(newArchiveFilePath); // こちらは不適切
                OpenFile(prevArchiveFilePath, currentPage, currentSortModeDetails); // loader を null にしてしまっているので再読み込みは必要
                return;
            }

            // 名前の変更を報告
            SendRenameMessageToCatalogForm(prevArchiveFilePath, newArchiveFilePath);

            //MessageBox.Show(this, Message.DonePageSequenceSetting, Message.Information, MessageBoxButtons.OK, MessageBoxIcon.Information);
            OpenFile(newArchiveFilePath, currentPage, currentSortModeDetails);
        }


        private Rectangle GetCoverRectangleForDisplay(Point? clientPoint = null)
        {
            GetCoverInfo_ClientPoint = clientPoint ?? pbView.PointToClient(Cursor.Position);
            var result = GetCoverInfo(1);
            return result != null ? (Rectangle)result : Rectangle.Empty;
        }

        private Tuple<int, double> GetThumbnailInfoAsTuple(Point? clientPoint = null)
        {
            GetCoverInfo_ClientPoint = clientPoint ?? pbView.PointToClient(Cursor.Position);
            return (Tuple<int, double>)GetCoverInfo(2);
        }

        Point GetCoverInfo_ClientPoint;
        private object GetCoverInfo(int getMode)
        {
            var totalWidth = 0;
            var nextPage = NextPage;
            if (currentPage < 0 || nextPage <= currentPage) return null;
            var pageCount = nextPage - currentPage;
            var Boundaries = new int[pageCount + 1];
            for (var i = nextPage - 1; i >= currentPage; i--)
            {
                var img = ResizedImageArray[i];
                if (img == null)
                {
                    return null;
                }
                Boundaries[pageCount - 1 - (i - currentPage)] = totalWidth;
                totalWidth += img.Width;
            }
            Boundaries[pageCount] = totalWidth;

            var pageLeftToRight = BindingMode == BindingMode.LeftToRight;

            if (pageLeftToRight)
            {
                var Boundaries2 = new int[pageCount + 1];
                for (var i = 0; i <= pageCount; i++)
                {
                    Boundaries2[i] = totalWidth - Boundaries[pageCount - i];
                }
                Boundaries = Boundaries2;
            }

            var pbViewSize = pbView.Size;
            //var totalHeight = ResizedImageArray[currentPage].Height;

            int offsetX;//, offsetY;
            if (pageCount > 1 || !(currentPage == 0 || nextPage == ResizedImageArray.Length) ||
               !(BindingMode == BindingMode.LeftToRight || BindingMode == BindingMode.RightToLeft) || totalWidth > pbViewSize.Width / 2)
            {
                if (pageCount == 2 && ResizedImageArray[currentPage].Width <= pbViewSize.Width / 2 && ResizedImageArray[currentPage + 1].Width <= pbViewSize.Width / 2)
                {
                    offsetX = pbViewSize.Width / 2 - ResizedImageArray[BindingMode == BindingMode.LeftToRight ? currentPage : (currentPage + 1)].Width;
                }
                else
                {
                    offsetX = (pbViewSize.Width - totalWidth) / 2;
                }
            }
            else
            {
                if (BindingMode == BindingMode.LeftToRight ^ currentPage == 0)
                {
                    offsetX = pbViewSize.Width / 2 - totalWidth;
                }
                else
                {
                    offsetX = pbViewSize.Width / 2;
                }
            }
            //offsetY = (pbViewSize.Height - totalHeight) / 2;

            //int offsetX = (pbViewSize.Width - totalWidth) / 2;
            //int offsetY = (pbViewSize.Height - totalHeight) / 2;

            for (var i = 0; i <= pageCount; i++)
            {
                Boundaries[i] += offsetX;
            }
            
            var clientPoint = GetCoverInfo_ClientPoint;

            var selectedPage = currentPage + pageCount - 1;
            for (var i = 1; i < pageCount; i++)
            {
                if (clientPoint.X < Boundaries[i]) break;
                selectedPage = currentPage + pageCount - i - 1;
            }

            var relativePhysicalIndex = pageCount - 1 - (selectedPage - currentPage);

            if (pageLeftToRight)
            {
                selectedPage = currentPage + pageCount - 1 - (selectedPage - currentPage);
            }

            int offsetX2 = Boundaries[relativePhysicalIndex];
            //int offsetY2 = offsetY;
            var totalHeight = ResizedImageArray[selectedPage].Height;
            int offsetY2 = (pbViewSize.Height - totalHeight) / 2;

            const double widthPerHeight = 0.7071067811865475244;

            // 原点：画像の左上、スケール：画面表示
            int selectedImageWidth = Boundaries[relativePhysicalIndex + 1] - Boundaries[relativePhysicalIndex];
            double onImageX, onImageY, onImageWidth, onImageHeight;
            if (selectedImageWidth >= totalHeight * widthPerHeight)
            {
                onImageWidth = totalHeight * widthPerHeight;
                onImageHeight = totalHeight;
                onImageX = Math.Max(0, Math.Min(selectedImageWidth - onImageWidth, clientPoint.X - offsetX2 - onImageWidth / 2.0));
                onImageY = 0;
            }
            else
            {
                onImageWidth = selectedImageWidth;
                onImageHeight = selectedImageWidth / widthPerHeight;
                onImageX = 0;
                onImageY = Math.Max(0, Math.Min(totalHeight - onImageHeight, clientPoint.Y - offsetY2 - onImageHeight / 2.0));
            }

            /*
            if (KeyboardShortcut.GetKeyState(Keys.A))
            {
                MessageBox.Show($"{new Rectangle((int)(offsetX2 + onImageX + 0.5), (int)(offsetY2 + onImageY + 0.5), (int)(onImageWidth + 0.5), (int)(onImageHeight + 0.5))}");
            }
            */

            switch (getMode)
            {
                case 1: // 表示上の四角形
                    return new Rectangle((int)(offsetX2 + onImageX + 0.5), (int)(offsetY2 + onImageY + 0.5), (int)(onImageWidth + 0.5), (int)(onImageHeight + 0.5));

                case 2: // 標準的な左上基準の割合文字列
                    var value = Math.Min(0.999, onImageWidth < selectedImageWidth ?
                        (onImageWidth < selectedImageWidth ? onImageX / (selectedImageWidth - onImageWidth) : 0) :
                        (onImageHeight < totalHeight ? onImageY / (totalHeight - onImageHeight) : 0)); // ToString は四捨五入（もしくは銀行丸め）を行う
                    return Tuple.Create(selectedPage, value);
            }

            return null;
        }

        private const double FourthRootOf2 = 1.1892071150027210667174999705604759152929720924638;
        private const double SquareRootOf2 = 1.4142135623730950488016887242096980785696718753769;
        private void pbView_MouseWheel(object sender, MouseEventArgs e)
        {
            if (statusStrip.Visible && statusStrip.ClientRectangle.Contains(statusStrip.PointToClient(pbView.PointToScreen(e.Location))))
            {
                // テキストボックス上でのホイール操作が statusStrip に伝わらないため
                // e の座標を使う場合補正が必要
                aroundSeekBar_MouseWheel(statusStrip, e);
                return;
            }
            if (e.Delta != 0) changeMagnifyingPowerInMagnifierOpeningOrClosing(e.Delta > 0);
        }

        private void changeMagnifyingPowerInMagnifierOpeningOrClosing(bool zoomUp)
        {
            if (UserMagnifierOpeningOrClosing && (ViewerMode == ViewerModeEnum.MagnifierOpening || ViewerMode == ViewerModeEnum.MagnifierClosing))
            {
                var currentPbViewSize = getCurrentPbViewSize();
                var currentImageSize_ = getCurrentVisibleImageSize(currentPbViewSize);
                if (currentImageSize_ == null) return;
                var currentImageSize = (Size)currentImageSize_;

                var p0 = magnifyingPower.GetPower(currentPbViewSize, currentImageSize);
                //var mpInt = (int)Math.Round(Math.Log(p0, SquareRootOf2));
                var mp = Math.Log(p0, SquareRootOf2);
                var p1 = Math.Pow(SquareRootOf2, zoomUp ? mp + 1 : mp - 1);

                const double LBound = 1.1; // 16:10 で √2:1 を拡大するには 4√2 / 5 = 1.131 必要。2^(1/4) =1.189 では不十分

                var currentHFit = MagnifyingInfo.FitHorizontal.GetPower(currentPbViewSize, currentImageSize);
                var currentVFit = MagnifyingInfo.FitVertical.GetPower(currentPbViewSize, currentImageSize);
                //double p2;
                const double aff = 1 + 1e-14; //double の計算機イプシロンは 1.220e-16 程度
                if (zoomUp) // 拡大
                {
                    if (currentHFit <= p0) currentHFit = double.PositiveInfinity;
                    if (currentVFit <= p0) currentVFit = double.PositiveInfinity;
                    currentHFit /= aff;
                    currentVFit /= aff;
                    p1 = Math.Min(Math.Min(currentHFit, currentVFit), p1);
                }
                else // 縮小
                {
                    if (currentHFit >= p0) currentHFit = 0;
                    if (currentVFit >= p0) currentVFit = 0;
                    currentHFit *= aff;
                    currentVFit *= aff;
                    p1 = Math.Max(Math.Max(currentHFit, currentVFit), p1);
                }

                if (p1 < LBound) // 
                {
                    MagnifyingPower = MagnifyingInfo.FromValue(LBound);
                }
                else if (p1 > 4)
                {
                    MagnifyingPower = MagnifyingInfo.FromValue(4);
                }
                else
                {
                    if (p1 == currentHFit) MagnifyingPower = MagnifyingInfo.FitHorizontal;
                    else if (p1 == currentVFit) MagnifyingPower = MagnifyingInfo.FitVertical;
                    else
                    {
                        MagnifyingPower = MagnifyingInfo.FromValue(p1);
                    }
                }
            }
        }

        private bool IsResizedForDivide(int page)
        {
            if (page < 0) return false;
            var resizedSizes = ResizedSizeArray;
            if (resizedSizes == null || resizedSizes.Length <= page) return false;
            return resizedSizes[page].Width > getCurrentPbViewSize().Width;
        }

        /// <summary>
        /// n はページ送り回数（≠ページの変化量）、負の数可
        /// </summary>
        private bool movePageNatural(int n)
        {
            // ナチュラル移動のみ
            if (n == 1)
            {
                IfTouchModifyingThenMoveOrigin(inverse: false);
            }
            else if (n == -1)
            {
                IfTouchModifyingThenMoveOrigin(inverse: true);
            }

            // n < -1 が必要な場合別に実装すること
            if (n == -1 && CurrentDividedPosition > 0)
            {
                return moveDividedPage(-1);
            }

            var oldCurrent = currentPage;
            var oldMode = onePageModeForNext;
            var elderPageIsOverwrap = false;
            if (n > 0)
            {
                for (var i = 0; i < n; i++)
                {
                    var nextPage = NextPage;
                    var cPage = currentPage;
                    if (nextPage <= cPage)
                    {
                        if (i == 0 && nextPage == cPage)
                        {
                            var currentPageDivision = getCurrentPageDivison();
                            if (CurrentDividedPosition < currentPageDivision - 1 || CurrentDividedPosition == currentPageDivision - 1 && IsResizedForDivide(cPage))
                            {
                                return moveDividedPage(n, currentPageDivision);
                            }
                        }
                        break;
                    }
                    currentPage = nextPage;
                }
            }
            else
            {
                var nextCurrent = int.MaxValue;
                for (var i = 0; i < -n; i++)
                {
                    nextCurrent = currentPage;
                    var prevPage = PreviousPage;
                    var cPage = currentPage;
                    if (prevPage >= cPage)
                    {
                        if (i == 0 && prevPage == cPage)
                        {
                            if (CurrentDividedPosition > 0 || CurrentDividedPosition == 0 && IsResizedForDivide(cPage - 1))
                            {
                                return moveDividedPage(n);
                            }
                        }
                        break;
                    }
                    currentPage = prevPage;
                    if (currentPage < nextCurrent && nextCurrent < NextPage) elderPageIsOverwrap = true;
                }
            }
            if (currentPage != oldCurrent && currentPage >= 0 && ResizedImageArray != null && currentPage < ResizedImageArray.Length)
            {
                //currentPage = nextPage;
                if (elderPageIsOverwrap) onePageModeForNext = true;
                showCurrentPage();
                mtbPage.Value = currentPage;
                return true;
            }
            else
            {
                currentPage = oldCurrent;
                onePageModeForNext = oldMode; // currentPage の変更で false になったものを元に戻す

                return false;
                //return moveDividedPage(n);
            }
        }

        /// <summary>
        /// n はページ送り回数（≠ページの変化量）、負の数可
        /// </summary>
        private bool isMovablePageNatural(int n, bool checkLoaded = false)
        {
            // n < -1 が必要な場合別に実装すること
            if (n == -1 && CurrentDividedPosition > 0)
            {
                return isMovableDividedPage(-1, checkLoaded);
            }

            var oldCurrent = currentPage;
            var oldMode = onePageModeForNext;
            var nextPage = currentPage;
            if (n > 0)
            {
                for (var i = 0; i < n; i++)
                {
                    var nextPage0 = NextPage;
                    var cPage = currentPage;
                    if (nextPage0 <= cPage)
                    {
                        if (i == 0 && nextPage0 == cPage)
                        {
                            var currentPageDivision = getCurrentPageDivison();
                            if (CurrentDividedPosition < currentPageDivision - 1 || CurrentDividedPosition == currentPageDivision - 1 && IsResizedForDivide(cPage))
                            {
                                return isMovableDividedPage(n, checkLoaded, currentPageDivision);
                            }
                        }
                        break;
                    }
                    currentPage = nextPage0;
                }
            }
            else
            {
                for (var i = 0; i < -n; i++)
                {
                    var prevPage = PreviousPage;
                    var cPage = currentPage;
                    if (prevPage >= cPage)
                    {
                        if (i == 0 && prevPage == cPage)
                        {
                            if (CurrentDividedPosition > 0 || CurrentDividedPosition == 0 && IsResizedForDivide(cPage - 1))
                            {
                                return isMovableDividedPage(n, checkLoaded);
                            }
                        }
                        break;
                    }
                    currentPage = prevPage;
                }
            }
            var result = currentPage != oldCurrent && currentPage >= 0 && ResizedImageArray != null && currentPage < ResizedImageArray.Length;
            if (!checkLoaded || !result)
            {
                currentPage = oldCurrent;
                onePageModeForNext = oldMode; // currentPage の変更で false になったものを元に戻す

                //return result || isMovableDividedPage(n, checkLoaded);
                return result;
            }
            else
            {
                var checkStop = NextPage;
                if (checkStop == currentPage) result = false;
                else
                {
                    for (var i = currentPage; i < checkStop; i++)
                    {
                        if (ResizedImageArray[i] == null)
                        {
                            result = false;
                            break;
                        }
                    }
                }
                //MessageBox.Show($"{currentPage}, {checkStop}, {result}");
                currentPage = oldCurrent;
                onePageModeForNext = oldMode; // currentPage の変更で false になったものを元に戻す

                //return result || isMovableDividedPage(n, checkLoaded);
                return result;
            }
            /*
            var nextPage = currentPage;
            if (n > 0)
            {
                for (var i = 0; i < n; i++)
                {
                    nextPage = NextPage;
                }
            }
            else
            {
                for (var i = 0; i < -n; i++)
                {
                    nextPage = PreviousPage;
                }
            }
            return nextPage != currentPage && nextPage >= 0 && ResizedImageArray != null && nextPage < ResizedImageArray.Length;
            */
        }

        /// <summary>
        /// 1ページ分戻る。ただし極力左右の位置関係が movePageNatural(-1) とは逆になるようにする
        /// </summary>
        private void movePageToMinimulBack()
        {
            if (CurrentDividedPosition > 0)
            {
                moveDividedPage(-1);
                return;
            }

            var naturalNext = NextPage;
            if(naturalNext <= currentPage + 1)
            {
                if (isMovablePage(-1, checkLoaded: true))
                {
                    //movePage(-1);
                    moveDividedPage(-1);
                    return;
                }
            }
            else
            {
                var oldCurrent = currentPage;
                var nextPage = currentPage - 1;
                if (nextPage >= -1 && ResizedImageArray != null && Math.Max(nextPage, 0) < ResizedImageArray.Length)
                {
                    if (nextPage == -1)
                    {
                        onePageModeForNext = true;
                    }
                    else
                    {
                        currentPage = nextPage;
                        if (oldCurrent >= NextPage)
                        {
                            currentPage = oldCurrent;
                            onePageModeForNext = true;
                        }
                    }
                    showCurrentPage();
                    mtbPage.Value = currentPage;
                    return;
                }
            }

            moveDividedPage(-1);
        }

        private bool isMovablePageToMinimulBack(bool checkLoaded = false)
        {
            //return isMovablePage(-1, checkLoaded;
            //return isMovableDividedPage(-1, checkLoaded);

            if (isMovableDividedPage(-1, checkLoaded)) return true;
            if (currentPage != 0) return false;
            var naturalNext = NextPage;
            if (naturalNext <= currentPage + 1) return false;
            var nextPage = currentPage - 1;
            if (ResizedImageArray == null || ResizedImageArray.Length == 0) return false;
            
            return !checkLoaded || ResizedImageArray[0] != null;

        }

        /// <summary>
        /// 1ページ分進める。ただし極力左右の位置関係が movePageNatural(1) とは逆になるようにする
        /// </summary>
        private void movePageToMinimulForward()
        {
            if (onePageModeForNext)
            {
                var nextPageInOmePageMode = NextPage;
                if (nextPageInOmePageMode <= currentPage)
                {
                    moveDividedPage(1);
                    return;
                }
                onePageModeForNext = false;
                var nextPageInNormalMode = NextPage;
                if(nextPageInNormalMode > nextPageInOmePageMode)
                {
                    // currentPage は変更せず表示を追加
                    showCurrentPage();
                }
                else
                {
                    movePageToMinimulForward();
                }
                return;
            }
            else
            {
                var naturalNext = NextPage;
                if (naturalNext <= currentPage)
                {
                    moveDividedPage(1);
                    return;
                }
                if (naturalNext > currentPage + 1)
                {
                    if (isMovablePage(1, checkLoaded: true))
                    {
                        movePage(1);
                        return;
                    }
                }
                else
                {
                    var nextPage = currentPage + 1;
                    if (nextPage != currentPage && nextPage >= 0 && ResizedImageArray != null && nextPage < ResizedImageArray.Length)
                    {
                        currentPage = nextPage;
                        onePageModeForNext = true;
                        showCurrentPage();
                        mtbPage.Value = currentPage;
                        return;
                    }
                }
            }
            moveDividedPage(1);
        }

        private bool isMovablePageToMinimulForward(bool checkLoaded = false)
        {
            if (onePageModeForNext)
            {
                var nextPageInOmePageMode = NextPage;
                if (nextPageInOmePageMode <= currentPage) return isMovableDividedPage(1, checkLoaded);
                onePageModeForNext = false;
                var nextPageInNormalMode = NextPage;
                if (nextPageInNormalMode > nextPageInOmePageMode)
                {
                    onePageModeForNext = true;
                    if (!checkLoaded)
                    {
                        return true;
                    }
                    else
                    {
                        for(var i = nextPageInOmePageMode; i < nextPageInNormalMode; i++)
                        {
                            if (!(0 <= i && ResizedImageArray != null &&
                                i < ResizedImageArray.Length && ResizedImageArray[i] != null))
                                return isMovableDividedPage(1, checkLoaded);
                        }
                        return true;
                    }
                }
                else
                {
                    var result = isMovablePageToMinimulForward(checkLoaded);
                    onePageModeForNext = true;
                    return result || isMovableDividedPage(1, checkLoaded);
                }
            }
            else
            {
                return isMovablePage(1, checkLoaded) || isMovableDividedPage(1, checkLoaded);
            }
        }

        private bool moveDividedPage(int n, int? currentPageDivision = null)
        {
            if (Math.Abs(n) == 1)
            {
                if (MaxDivision == 1)
                {
                    if (isMovablePage(n))
                    {
                        movePage(n);
                        return true;
                    }
                    else return false;
                }
                if (n == -1)
                {
                    var division = currentPageDivision ?? getCurrentPageDivison();
                    if (division < 1) return false;
                    if (Math.Min(CurrentDividedPosition, division - 1) == 0)
                    {
                        var prevDivision = getPageDivison(currentPage - 1);
                        if (prevDivision > 0)
                        {
                            if (isMovablePage(-1, checkLoaded: true))
                            {
                                movePage(-1, prevDivision - 1);
                                return true;
                            }
                        }
                    }
                    else
                    {
                        if (currentPage >= 0 && ResizedImageArray != null &&
                            currentPage < ResizedImageArray.Length && ResizedImageArray[currentPage] != null)
                        {
                            CurrentDividedPosition--;
                            showCurrentPage();
                            return true;
                        }
                    }
                }
                else
                {
                    var division = currentPageDivision ?? getCurrentPageDivison();
                    if (CurrentDividedPosition >= division - 1)
                    {
                        if (isMovablePage(1, checkLoaded: true))
                        {
                            movePage(1, 0);
                            return true;
                        }
                    }
                    else
                    {
                        if (currentPage >= 0 && ResizedImageArray != null &&
                            currentPage < ResizedImageArray.Length && ResizedImageArray[currentPage] != null)
                        {
                            CurrentDividedPosition++;
                            showCurrentPage();
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool isMovableDividedPage(int n, bool checkLoaded, int? currentPageDivision = null)
        {
            if (Math.Abs(n) == 1)
            {
                if (MaxDivision == 1) return isMovablePage(n, checkLoaded);

                if (n == -1)
                {
                    var division = currentPageDivision ?? getCurrentPageDivison();
                    if (division < 1) return false;

                    if (Math.Min(CurrentDividedPosition, division - 1) == 0)
                    {
                        var prevDivision = getPageDivison(currentPage - 1);
                        if (prevDivision > 0)
                        {
                            return isMovablePage(-1);
                        }
                    }
                    else
                    {
                        return !checkLoaded || currentPage >= 0 && ResizedImageArray != null &&
                            currentPage < ResizedImageArray.Length && ResizedImageArray[currentPage] != null;
                    }
                }
                else
                {
                    var division = currentPageDivision ?? getCurrentPageDivison();
                    if (CurrentDividedPosition >= division - 1)
                    {
                        return isMovablePage(1);
                    }
                    else
                    {
                        return !checkLoaded || currentPage >= 0 && ResizedImageArray != null &&
                            currentPage < ResizedImageArray.Length && ResizedImageArray[currentPage] != null;
                    }
                }
            }
            return false;
        }

        private int getCurrentPageDivison()
        {
            return getPageDivison(currentPage);
        }

        private int getPageDivison(int page)
        {
            if (page >= 0 && ResizedSizeArray != null && page < ResizedSizeArray.Length)
            {
                var w = Math.Abs(ResizedSizeArray[page].Width);
                if (w <= 0) return 0;
                var result = (w - 1) / pbView.Width + 1;
                if (result > MaxDivision) return 0;
                return result;
            }
            return 0;
        }

        /// <summary>
        /// n は単純なページの変化量、負の数可
        /// </summary>
        private void movePage(int n, int dividedPosition = -1)
        {
            var nextPage = currentPage + n;
            if (nextPage != currentPage && nextPage >= 0 && ResizedImageArray != null && nextPage < ResizedImageArray.Length)
            {
                currentPage = nextPage;
                if (dividedPosition >= 0 && dividedPosition < getCurrentPageDivison())
                {
                    CurrentDividedPosition = dividedPosition;
                }
                showCurrentPage();
                mtbPage.Value = currentPage;
            }
        }

        /// <summary>
        /// n は単純なページの変化量、負の数可
        /// </summary>
        private bool isMovablePage(int n, bool checkLoaded = false)
        {
            var nextPage = currentPage + n;
            var result = nextPage != currentPage && nextPage >= 0 && ResizedImageArray != null && nextPage < ResizedImageArray.Length;
            if (!checkLoaded || !result)
            {
                return result;
            }
            else
            {
                for (var i = currentPage; i < nextPage; i++)
                {
                    if (ResizedImageArray[i] == null) return false;
                }
                return true;
            }
        }

        //private bool ShowIconRequired = false;
        private bool SetCurrentImageLongPathRequired = false;
        private string shownImegesPath = null;
        private string requestedImagePath = null;
        private int requestedImagePage;
        //private Color requestedImageColor;
        //private ArchivesInArchiveMode? requestedArchivesInArchiveMode = null;
        private BindingMode? requestedBindingMode = null;
        //private CoverBindingMode? requestedCoverBindingMode = null;
        private bool? requestedIgnoreSavedFilerSetting = null;
        private SortModeDetails requestedSortModeDetails;
        private string requestedFilterString;
        private string requestedFilterStringWithoutAlias;
        private bool ExitAfterCoverSetting = false;
        private object tmWatchIpc_Tick_Running = new object();
        private ViewerFormImageFilter initialViewerFormImageFilter = null;
        private bool saveToHistory = false;
        private SearchManager searchManagerFromCatalogForm = null;
        private InitialFullscreenMode? requestedInitialFullscreenMode = null;
        private bool? requestedAlwaysHideUI = null;
        // private FormWindowState? tmWatchIpc_Tick_FormWindowStateFromSetteingFileMemory = null;
        // private bool tmWatchIpc_FullscreenMemory = false;
        // private bool tmWatchIpc_MaximizedMemory = false;
        //private bool tmWatchIpc_Tick_MemoryOriginalFullscreen = false;
        private bool tmWatchIpc_Stopped = false;
        private void tmWatchIpc_Tick(object sender, EventArgs e)
        {
            if (!ViewerForm_Shown_Called) return;

            if (tmWatchIpc_Stopped) return; // Timer.Stop() が完全にリアルタイムには起こらない可能性を考慮

            ExclusionControl.TryLock(tmWatchIpc_Tick_Running, async () =>
            {
                try
                {
                    if (!ipcLookAheadInfo.Accept)
                    {

                        var coverSetting = ipcLookAheadInfo.Message == IpcLookAheadInfo.MessageEnum.CoverSetting;
                        
                        var show = ipcLookAheadInfo.Message == IpcLookAheadInfo.MessageEnum.Show || coverSetting;
                        
                        var path = ipcLookAheadInfo.Path;
                        var page = ipcLookAheadInfo.Page;
                        var asia = ipcLookAheadInfo.ArchivesInArchive;
                        var rm = ipcLookAheadInfo.ReadOnMemoryMode;
                        var bind = ipcLookAheadInfo.DefaultBinding;
                        var coverBind = ipcLookAheadInfo.CoverBinding;
                        var ignFilter = ipcLookAheadInfo.IgnoreSavedFilerSetting;
                        var sort = ipcLookAheadInfo.SortModeDetails;
                        var fString = ipcLookAheadInfo.FilterString;
                        var fString2 = fString == null ? null : ipcLookAheadInfo.FilterStringWithoutAlias;
                        var color = ipcLookAheadInfo.BackColor;
                        var alwaysHideUI = ipcLookAheadInfo.AlwaysHideUI;
                        var iniFull = ipcLookAheadInfo.InitialFullscreenMode;
                        //var noFullscreen = true;

                        ipcLookAheadInfo.Accept = true; // これをすると通信が切られるので先にやってはいけない
                        if (show)
                        {
                            tmWatchIpc_Stopped = true;
                            tmWatchIpc.Stop();
                            //tmMemoryReducer.Enabled = false; // Stop() と同じ
                        }

                        var needToReload = false;
                        if (CommandLineOptionInfoForLookAhead != null)
                        {
                            var time = Configuration.GetLastWriteTime();
                            var reloadConfig = time != LoadSettings_LoadedFileTimeStampForLookAhead;
                            if (
                                requestedInitialFullscreenMode != iniFull && !(iniFull == InitialFullscreenMode.ForceFullscreen && FullScreen_pseudoFullScreen || iniFull == InitialFullscreenMode.ForceWindow && !FullScreen_pseudoFullScreen) ||
                                alwaysHideUI != requestedAlwaysHideUI || reloadConfig
                                )
                            {
                                LoadSettings_LoadedFileTimeStampForLookAhead_Setter = time;

                                //using (var m = new TimeMeasure())
                                {
                                    var pSize = FullScreen_pseudoFullScreen ? Screen.FromControl(this).Bounds.Size :
                                        PseudoMaximized ? PseudoMaximizedSize : ViewerForm_SizeChanged_pbBoundsMemoryForMinimize.Size;

                                    CommandLineOptionInfoForLookAhead.DefaultBindingMode = requestedBindingMode;
                                    CommandLineOptionInfoForLookAhead.BackColor = pbView.BackColor;
                                    CommandLineOptionInfoForLookAhead.ArchivesInArchiveMode = ArchivesInArchiveMode;
                                    CommandLineOptionInfoForLookAhead.CoverBindingMode = CoverBindingMode;
                                    CommandLineOptionInfoForLookAhead.InitialFullscreenMode = iniFull;
                                    CommandLineOptionInfoForLookAhead.AlwaysHideUI = alwaysHideUI;
                                    
                                    LoadSettingsAndSetMessages(CommandLineOptionInfoForLookAhead, reloadConfig, reloadConfig);

                                    var newSize = FullScreen_pseudoFullScreen ? Screen.FromControl(this).Bounds.Size :
                                        PseudoMaximized ? PseudoMaximizedSize : ViewerForm_SizeChanged_pbBoundsMemoryForMinimize.Size;

                                    if (pSize != newSize)
                                    {
                                        needToReload = true;
                                    }
                                }
                            }
                        }

                        requestedAlwaysHideUI = alwaysHideUI;
                        requestedInitialFullscreenMode = iniFull;

                        if (show)
                        {
                            //Program.SetPriority(ProcessPriorityClass.Normal);
                            ipcLookAheadInfo = null;
                            LoadSettings_LastLoadedConfig = null;
                            LoadSettings_LastLoadedGeneralConfig = null;
                        }

                        bool secondTerm = false;

                        do
                        {
                            var newOpen = false;
                            try
                            {
                                Invoke((MethodInvoker)(() =>
                                {
                                    /*
                                    if (!secondTerm && (path != shownImegesPath || page >= 0 && page != shownImegesPage))
                                    {
                                        */

                                    var filterChanged = fString != requestedFilterString || fString2 != requestedFilterStringWithoutAlias;

                                    // 送る側が sort の変化をチェックしないので受け側でもチェックしない
                                    if (!secondTerm && (path != requestedImagePath || page != requestedImagePage || asia != ArchivesInArchiveMode ||/*requestedArchivesInArchiveMode*/
                                        rm != ReadOnMemoryMode || bind != requestedBindingMode ||
                                        ignFilter != requestedIgnoreSavedFilerSetting || filterChanged ||
                                        coverBind != CoverBindingMode /*requestedCoverBindingMode*/ || color != pbView.BackColor))
                                    {

                                        var prevExists = !string.IsNullOrEmpty(currentArchiveFilePath); // こっちは shownImegesPath は不適切
                                        if (show && prevExists)
                                        {
                                            shownImegesPath = null;
                                        }

                                        if (ignFilter)
                                        {
                                            imageFilter.Set(null);
                                        }
                                        else if (requestedIgnoreSavedFilerSetting == true && initialViewerFormImageFilter != null)
                                        {
                                            imageFilter.Set(initialViewerFormImageFilter);
                                        }

                                        if (fString2 != requestedFilterStringWithoutAlias)
                                        {
                                            try
                                            {
                                                if (fString2 == null)
                                                {
                                                    searchManagerFromCatalogForm = null;
                                                }
                                                else
                                                {
                                                    searchManagerFromCatalogForm = new SearchManager(fString2);
                                                }
                                            }
                                            catch
                                            {
                                                searchManagerFromCatalogForm = null;
                                            }
                                        }

                                        var load = false;
                                        // ページ指定でページだけが変わってるパターン
                                        // 現状これはほぼ起こり得ないのでコメントアウト
                                        /*
                                        if (path == requestedImagePath && bind == requestedBindingMode && page >= 0 && page <= mtbPage.Maximum)
                                        {
                                            mtbPage.Value = page;
                                            load = true;
                                        }
                                        */

                                        var sortChanged = filterChanged ? true : null as bool?;

                                        // パスが変わっているが親が変わっていないパターン
                                        if (/*!OpenFileFinishedButStartingIsNotCalled && */!load && page < 0 && page == requestedImagePage && bind == requestedBindingMode)
                                        {
                                            if (sortChanged == null) sortChanged = sort as object == null ? requestedSortModeDetails as object != null : !sort.QuickEquals(requestedSortModeDetails);
                                            if(sortChanged == false)
                                            {
                                                var a = nextData?.EntriesPaths ?? EntryLongPathArray;
                                                if (a != null)
                                                {
                                                    var altPos = path.IndexOf(Path.AltDirectorySeparatorChar);
                                                    // 画像ファイルのパターン
                                                    if (altPos < 0)
                                                    {
                                                        var p = path.LastIndexOf(Path.DirectorySeparatorChar);
                                                        if (p >= 0)
                                                        {
                                                            var pg = Array.IndexOf(a, path.Substring(0, p) + Path.AltDirectorySeparatorChar + path.Substring(p + 1));
                                                            if (pg >= 0 && pg <= mtbPage.Maximum)
                                                            {
                                                                mtbPage.Value = pg;
                                                                currentStartingFilePath = path;
                                                                load = true;
                                                            }
                                                        }
                                                    }

                                                    // アーカイブ内のパターン
                                                    else
                                                    {
                                                        var pg = Array.IndexOf(a, path);
                                                        if (pg >= 0 && pg <= mtbPage.Maximum)
                                                        {
                                                            mtbPage.Value = pg;
                                                            load = true;
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        var fromCatalogFormEvent = e != null;

                                        if (fromCatalogFormEvent && pbView.BackColor != color)
                                        {
                                            pbView.BackColor = color;
                                        }
                                        /*
                                        if (noFullscreen)
                                        {
                                            Size = prevSize;
                                            LoadSettings_FormWindowStateFromSetteingFile = null;


                                            fullScreenModeChange(false);
                                            //tmWatchIpc_Tick_MemoryOriginalFullscreen = FullScreen;
                                            //fullScreenModeChange(false);
                                        }
                                        else
                                        {
                                            if(tmWatchIpc_Tick_MemoryOriginalFullscreen && !FullScreen) fullScreenModeChange(true);
                                        }
                                        */


                                        if (fromCatalogFormEvent)
                                        {
                                            requestedBindingMode = bind;
                                            //requestedArchivesInArchiveMode = asia;
                                            ArchivesInArchiveMode = asia;
                                            ReadOnMemoryMode = rm;
                                            requestedFilterString = fString;
                                            requestedFilterStringWithoutAlias = fString2;
                                            requestedSortModeDetails = sort;
                                        }

                                        OpenResult openResult = default(OpenResult);
                                        if (!load)
                                        {
                                            openResult = OpenFile(path, page, sort, forLookAhead: true);//, bind);
                                            newOpen = true;
                                            needToReload = false;
                                        }

                                        requestedImagePath = path;
                                        requestedImagePage = page;

                                        if (fromCatalogFormEvent)
                                        {
                                            onePageModeForNext = currentPage == 0 && coverBind == CoverBindingMode.ForceSingle;

                                            //coverBind

                                            //requestedArchivesInArchiveMode = asia; // OpenFile の前に
                                            //requestedBindingMode = bind; // OpenFile の前に
                                            //requestedCoverBindingMode = coverBind;
                                            CoverBindingMode = coverBind;
                                            requestedIgnoreSavedFilerSetting = ignFilter;
                                            //requestedSortModeDetails = sort;
                                            //requestedImageColor = color;
                                            //requestedFilterString = fString;
                                            //requestedFilterStringWithoutAlias = fString2;
                                        }

                                        // show で無くて OpenFile に失敗した場合次回も実行するように
                                        if (!show && !load && openResult == OpenResult.NoOperation)
                                        {
                                            requestedImagePath = null;
                                        }

                                        if (show && prevExists)
                                        {
                                            secondTerm = true;
                                            return;
                                        }
                                        else
                                        {
                                            secondTerm = false;
                                        }
                                    }
                                    else
                                    {
                                        if (!secondTerm)
                                        {
                                            OpenFile_UserOpen = true;
                                        }
                                        else
                                        {
                                            secondTerm = false;
                                        }
                                    }

                                    if (needToReload)
                                    {
                                        Reload();
                                    }

                                    if (show)
                                    {
                                        if (coverSetting)
                                        {
                                            pbView_Paint_RectangleMessage = null;
                                            ViewerMode = ViewerModeEnum.CoverSetting;
                                            ExitAfterCoverSetting = true;
                                            //openFileContextMenuForMenuStripMenuItem.Enabled = false;
                                            //openNewThumbnailWindowContextMenuForMenuStripMenuItem.Enabled = false;
                                            openFileToolStripMenuItem.Enabled = false;
                                            openNewThumbnailWindowToolStripMenuItem.Enabled = false;
                                            switchToThumbnailWindowToolStripMenuItem.Enabled = false;
                                            cloneTheWindowToolStripMenuItem.Enabled = false;
                                            slideshowToolStripMenuItem.Enabled = false;
                                            btnOpenLeft.Enabled = false;
                                            btnOpenRight.Enabled = false;
                                            //magnifierContextMenuForMenuStripMenuItem.Enabled = false;
                                            //magnifierToolStripMenuItem.Enabled = false;
                                            editToolStripMenuItem.Enabled = false;
                                        }
                                        
                                        loader?.Release();
                                        nextData?.Loader.Release();

                                        var tempPrevSize = prevSize;
                                        BeginUpdate(pbView);
                                        if (FullScreen_pseudoFullScreen)
                                        {

                                            var temp = pbView_SizeChanged_SizeChanging;
                                            pbView_SizeChanged_SizeChanging = true;// WindowState = FormWindowState.Maximized;// MessageBox.Show("a");
                                            Visible = true; //  FormBorderStyle = FormBorderStyle.None; より前に実行しないとタスクバーにボタンが現れない
                                            FormBorderStyle = FormBorderStyle.None;
                                            pbView_SizeChanged_SizeChanging = temp;
                                            WindowState = FormWindowState.Maximized;


                                            // 最初にこうすることでと表示のズレを回避。なお、それでリロードがかからないのでサイズがかわっているわけではない。
                                            //pbView.Top = Top;
                                            pbView.Top = 0;
                                            pbView.Height = Height;
                                            //pbView.Left = Left; // シングルディスプレイでは 0 が出ていたので問題がないと勘違いしていたが明らかに誤り
                                            pbView.Left = 0;
                                            pbView.Width = Width;

                                            fullScreenToolStripMenuItem.Text = Message.Window;

                                            FullScreen_pseudoFullScreen = false;

                                            menuStrip_Bottom = menuStrip.Bounds.Bottom;
                                            //mtbPage_Top = pnlSeekbar.Bounds.Top;
                                            mtbPageTop_As_Reference_pbViewBottom = pnlSeekbar.Bounds.Top - pbView.Bottom;

                                            MyActivate(finallyTopMost: true);
                                            /*
                                            /////
                                            showWithoutActivation = true;
                                            /////

                                            TopMost = true;
                                            Visible = true;
                                            Activate();

                                            try
                                            {
                                                Microsoft.VisualBasic.Interaction.AppActivate(System.Diagnostics.Process.GetCurrentProcess().Id);
                                            }
                                            catch { }

                                            /////
                                            showWithoutActivation = false;
                                            Activate();
                                            /////
                                            */


                                            prevSize = tempPrevSize;

                                            //ShowIconRequired = true;

                                            SetCurrentImageLongPathRequired = true;
                                        }
                                        else
                                        {

                                            if (PseudoMaximized)
                                            {
                                                PseudoMaximized = false;
                                                WindowState = FormWindowState.Maximized;
                                                prevSize = tempPrevSize;
                                            }

                                            //if (LoadSettings_LoadedLocation != null) Location = (Point)LoadSettings_LoadedLocation;
                                            MyActivate(finallyTopMost: false);
                                            /*
                                            /////
                                            showWithoutActivation = true;
                                            /////

                                            Visible = true;
                                            TopMost = true;
                                            Activate();
                                            TopMost = false;

                                            try
                                            {
                                                Microsoft.VisualBasic.Interaction.AppActivate(System.Diagnostics.Process.GetCurrentProcess().Id);
                                            }
                                            catch { }

                                            /////
                                            showWithoutActivation = false;
                                            Activate();
                                            /////
                                            */
                                            

                                            SetCurrentImageLongPath();

                                        }

                                        // 表示直後からキーボードショートカットを作動させるため
                                        // Hidden -> Visible = true の場合にのみ必要
                                        Task focus = null;
                                        focus = new Task(() =>
                                        {
                                            try
                                            {
                                                Invoke(((MethodInvoker)(() =>
                                                {
                                                    pbView.Focus();
                                                })));
                                            }
                                            catch (ObjectDisposedException) { }
                                            focus.Dispose();
                                        });
                                        focus.Start();
                                        
                                        EndUpdate(pbView);

                                        try
                                        {
                                            VirtualFolder.AddBookmarkData(Program.HistorySorPath, path, Math.Max(0, page), limitOfItemsCount: Program.GetLimitOfHistoryCount(), deleteLostPath: true);
                                            SendSorMessageToCatalogForm(path, Program.HistorySorPath, Math.Max(0, page), requestToUpdateLastAccessTime: false, m: SelfLastAccessTimeUpdateMode.None);
                                        }
                                        catch { }
                                        saveToHistory = true;
                                    }
                                }));
                            }
                            catch (ObjectDisposedException) { }
                            if (secondTerm && show && newOpen)
                            {
                                var leftTime = 10 * 1000;
                                do
                                {
                                    //await Task.Run(() => Thread.Sleep(10));
                                    await Task.Delay(10);
                                    leftTime -= 10;
                                    if (leftTime <= 0)
                                    {
#if !AUTOBUILD
                                        Program.ShowCursor();
                                        MessageBox.Show(this, $"{path}\n{shownImegesPath}\n{currentPage}\n{shownImegesPage}");
#endif
                                        break;
                                    }
                                }
                                while (path != shownImegesPath || currentPage != shownImegesPage);
                            }
                        }
                        while (secondTerm);
                    }
                }
#if DEBUG
                catch (RemotingException ex)
                {
                    MessageBox.Show(ex.ToString());
#else
                catch (RemotingException)
                {
#endif
                    tmWatchIpc_Stopped = true;
                    tmWatchIpc.Stop();
                    try
                    {
                        Invoke((MethodInvoker)(() =>
                        {
                            Application.Exit();
                        }));
                    }
                    catch (ObjectDisposedException) { }

                }
            });
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
        public const int WM_SETREDRAW = 0x000B;

        /// <summary>
        /// コントロール(子コントロールも含む)の描画を停止します。
        /// </summary>
        /// <param name="control">対象コントロール</param>
        public static void BeginUpdate(Control control)
        {
            SendMessage(control.Handle, WM_SETREDRAW, 0, 0);
        }

        /// <summary>
        /// コントロール(子コントロールも含む)の描画を開始します。
        /// </summary>
        /// <param name="control">対象コントロール</param>
        public static void EndUpdate(Control control)
        {
            SendMessage(control.Handle, WM_SETREDRAW, 1, 0);
            control.Refresh();
        }
        private void MyActivate(bool finallyTopMost)
        {
            //Thread.Sleep(100);

            /////
            //showWithoutActivation = true;
            /////

            Visible = true;
            TopMost = true;
            Activate();

            if (!(finallyTopMost && TopMostInFullscreen)) TopMost = false;
            
            /*
            try
            {
                using (var currentProcess = Process.GetCurrentProcess())
                {
                    WindowActivator.ActiveWindow(currentProcess.MainWindowHandle);
                    // WindowActivator.ActiveWindow(currentProcess.MainWindowHandle);
                }
            }
            catch { }
            */
        }
        
        /*
        public static void ActiveWindowWithSetWindowPos(IntPtr hWnd)
        {
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE);
            SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0,
                SWP_SHOWWINDOW | SWP_NOMOVE | SWP_NOSIZE);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd,
            int hWndInsertAfter, int x, int y, int cx, int cy, int uFlags);

        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_SHOWWINDOW = 0x0040;

        private const int HWND_TOPMOST = -1;
        private const int HWND_NOTOPMOST = -2;

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern bool BringWindowToTop(IntPtr hWnd);

        ///// ShowWithoutActivationプロパティのオーバーライド
        private bool showWithoutActivation = false;
        protected override bool ShowWithoutActivation { get { return showWithoutActivation; } }
        /////
        
        */

        private void closeWindowtoolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        int menuStrip_Bottom = 0;
        int mtbPageTop_As_Reference_pbViewBottom = 0;
        private void menuStrip_MouseLeave(object sender, EventArgs e)
        {
            if (HideUI && !ContextMenuForMenuStrip_Opend && menuStrip_Bottom == 0)// && !ContextMenuForMenuStripPostProcessing_worked)
            {
                menuStrip_Bottom = menuStrip.Bounds.Bottom;
                menuStrip.Visible = false;
                
                ksViewForm.IgnoreAnyMouseEventOnce = MouseButtons.Left;
            }
            //ContextMenuForMenuStripPostProcessing_worked = false;
        }
        
        private void hideUnderUI(object sender, EventArgs e)
        {
            if (HideUI && /*mtbPage_Top*/mtbPageTop_As_Reference_pbViewBottom == 0)
            {
                // mtbPage_Top = pnlSeekbar.Bounds.Top;
                mtbPageTop_As_Reference_pbViewBottom = pnlSeekbar.Bounds.Top - pbView.Bottom;
                pnlSeekbar.Visible = false;
                statusStrip.Visible = false;
                
                ksViewForm.IgnoreAnyMouseEventOnce = MouseButtons.Left;
            }
        }
        
        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            base.WndProc(ref m);
            const int WM_NCMOUSELEAVE = 0x02A2;
            //const int WM_MOUSELEAVE = 0x02A3;

            //if ((m.Msg & WM_MOUSELEAVE) == WM_MOUSELEAVE) hideUnderUI(null, null);
            // return;

            switch (m.Msg)
            {
                case WM_NCMOUSELEAVE:
                    //case WM_MOUSELEAVE:
                    // hideUnderUI(null, null);
                    hideUnderUIForMouseLeave(null, null); // 右下のウィンドウサイズ変更の箇所をクリックしても WM_NCMOUSELEAVE が発生するため。
                    break;
            }
        }
        
        /*
        private void currentImageLongPathToolStripTextBox_MouseMove(object sender, MouseEventArgs e)
        {
            currentImageLongPathToolStripTextBox.Text = "Move " + DateTime.Now;

        }

        private void currentImageLongPathToolStripTextBox_MouseEnter(object sender, EventArgs e)
        {
            currentImageLongPathToolStripTextBox.Text = "Enter " + DateTime.Now;

        }

        [DllImport("comctl32.dll", SetLastError = true)]
        private static extern bool _TrackMouseEvent(ref TRACKMOUSEEVENT tme);

        private struct TRACKMOUSEEVENT
        {
            public int cbSize;
            public int dwFlags;
            public IntPtr hwndTrack;
            public int dwHowerTime;
        }

        private void ActivateLeaveTracking()
        {
            const int TME_LEAVE = 0x00000002;
            const int TME_NONCLIENT = 0x00000010;

            TRACKMOUSEEVENT tme = new TRACKMOUSEEVENT();
            tme.hwndTrack = this.Handle;
            tme.dwHowerTime = 0;
            tme.dwFlags = TME_LEAVE | TME_NONCLIENT;
            tme.cbSize = Marshal.SizeOf(typeof(TRACKMOUSEEVENT));
#if AUTOBUILD
            _TrackMouseEvent(ref tme);
#else
            if (!_TrackMouseEvent(ref tme))
            {
                throw new Exception(Marshal.GetLastWin32Error().ToString());
            }
#endif

        }
        */
        
        private void currentImageLongPathToolStripTextBox_MouseLeave(object sender, EventArgs e)
        {
            if (HideUI && mtbPageTop_As_Reference_pbViewBottom == 0)
            {
                var cursorPosition = Cursor.Position;
                
                // 境界上に乗っていた場合にその後 MouseLeave が発生しなくなる現象への対策
                // ポインタが境界上に飛び込んだ場合の対処はタイマーに任せる
                var positionOnClient = statusStrip.PointToClient(cursorPosition);
                var bounds = currentImageLongPathToolStripTextBox.Bounds;
                if (bounds.Contains(positionOnClient))
                {
                    var dl = positionOnClient.X - bounds.Left + 1;
                    var dr = bounds.Right - positionOnClient.X;
                    var dt = positionOnClient.Y - bounds.Top + 1;
                    var db = bounds.Bottom - positionOnClient.Y;
                    var dm = Math.Min(Math.Min(Math.Min(dl, dr), dt), db);
                    /*
                    if (dm == dl) cursorPosition.X -= dl;
                    else if (dm == dr) cursorPosition.X += dr;
                    else if (dm == dt) cursorPosition.Y -= dt;
                    else cursorPosition.Y += db;
                    Cursor.Position = cursorPosition;
                    */
                    if (dm == db)
                    {
                        cursorPosition.Y += db;
                        Cursor.Position = cursorPosition;
                    }
                }

                if (!pbView.ClientRectangle.Contains(pbView.PointToClient(Cursor.Position)))
                {
                    hideUnderUI(null, null);
                }
            }
        }
        
        private void hideUnderUIForMouseLeave(object sender, EventArgs e)
        {
            if (HideUI && mtbPageTop_As_Reference_pbViewBottom == 0)
            {
                var cursorPosition = Cursor.Position;
                var clientPoint = PointToClient(cursorPosition);
                var x = pnlSeekbar.Left;
                var y = pnlSeekbar.Top;
                if (!new Rectangle(x, y, statusStrip.Right - x, statusStrip.Bottom - y).Contains(clientPoint) ||
                    !ActivateManager.InVisibleRegion(this, clientPoint, cursorPosition, checkParentsCount: 3))
                {
                    hideUnderUI(null, null);
                }
            }
        }

        /*
        private void currentImageLongPathToolStripTextBox_MouseLeave(object sender, EventArgs e)
        {
            if (FullScreen)
            {
                mtbPage_Top = mtbPage.Bounds.Top;
                mtbPage.Visible = false;
                statusStrip.Visible = false;
            }
        }
        */
        // 上が期待通りに働かないので下で対処
        private bool MouseInPbView = false;
        private void pbView_MouseEnter(object sender, EventArgs e)
        {
            MouseInPbView = true;
            hideUnderUI(null, null);
            //pbView.Focus();
            focusIfRightness(pbView);

            if (ViewerMode == ViewerModeEnum.MagnifierOpening || ViewerMode == ViewerModeEnum.Magnifier || ViewerMode == ViewerModeEnum.MagnifierClosing)
            {
                Program.HideCursor();
            }
        }

        Point pbView_MouseMove_prevPos;
        Point? pbView_MouseMove_prevContPos = null;
        bool pbView_PreviewKeyDown_To_pbView_MouseMove_StopShwoMagnifierRectangle = false;
        private void pbView_MouseMove(object sender, MouseEventArgs e)
        {
            var stopShowMag = pbView_PreviewKeyDown_To_pbView_MouseMove_StopShwoMagnifierRectangle;
            pbView_PreviewKeyDown_To_pbView_MouseMove_StopShwoMagnifierRectangle = false;
            if (pbView_MouseMove_prevPos == e.Location) return;
            var afterEnter = pbView_MouseMove_prevContPos == null;
            pbView_MouseMove_prevContPos = pbView_MouseMove_prevPos = e.Location;
            TouchListener_Pan_Delta = null;

            if (!afterEnter) ksViewForm.IgnoreAnyMouseEventOnce = MouseButtons.None;

            if (!(ViewerMode == ViewerModeEnum.MagnifierOpening || ViewerMode == ViewerModeEnum.Magnifier || ViewerMode == ViewerModeEnum.MagnifierClosing))
            {
                Program.ShowCursor();
            }
            //setMouseButtonShape(GetPositionOnView(e.Location));

            //var y = pbView.PointToScreen(e.Location).Y;
            if (!CursorBounded())
            {
                var y = e.Location.Y;
                if (menuStrip_Bottom > 0 && y < menuStrip_Bottom)
                {
                    menuStrip.Visible = true;
                    menuStrip_Bottom = 0;

                    hideUnderUI(null, null); // currentImageLongPathToolStripTextBox_MouseLeave の仕様が期待通りでないので
                }
                else if (showUnderUI(e.Location)) { }
            }

            if (ViewerMode == ViewerModeEnum.MagnifierOpening || ViewerMode == ViewerModeEnum.MagnifierClosing)
            {
                ShwoMagnifierRectangle();
            }
            else if (ViewerMode == ViewerModeEnum.Magnifier)
            {
                var showMag = TouchListener_Pan_Delta == null && GetMagnifierRectangle_LastCursorPosition == null;

                if (showMag)
                {
                    //pbView.Invalidate(false); // 描画は Paint イベントハンドラで行われる
                    pbPaintInvalidate();
                }
            }
            else if (ViewerMode == ViewerModeEnum.CoverSetting)
            {
                if (!stopShowMag) ShwoCoverRectangle();
            }
            else if (DrawMeasure) pbPaintDrawOnlyMeasure();
        }

        private bool showUnderUI(Point pbViewClientPoint)
        {
            //if (mtbPage_Top > 0 && pbViewClientPoint.Y >= mtbPage_Top)
            if (mtbPageTop_As_Reference_pbViewBottom != 0 && pbViewClientPoint.Y >= pbView.Bottom + mtbPageTop_As_Reference_pbViewBottom)
            {
                pnlSeekbar.Visible = true;
                pnlSeekbar_PreStatusStripVisibleTrue();
                statusStrip.Visible = true;
                if (SetCurrentImageLongPathRequired) SetCurrentImageLongPath();
                //mtbPage_Top = 0;
                mtbPageTop_As_Reference_pbViewBottom = 0;
                return true;
            }
            return false;
        }

        // 画面のちらつきを回避する
        private void pnlSeekbar_PreStatusStripVisibleTrue()
        {
            if (statusStrip_VisibleChanged_FirstVisible)
            {
                var currentBound = statusStrip.Bounds;
                var statusStripBound = new Rectangle(pnlSeekbar.Left, statusStrip.Parent.Bottom - currentBound.Height, pnlSeekbar.Width, currentBound.Height); // 二回目以降と完全に一致
                statusStrip.Bounds = statusStripBound;
            }
        }

        /*
        private void setMouseButtonShape(Position position)
        {
            switch (position)
            {
                case Position.Left: Cursor.Current = Cursors.PanWest; break;
                case Position.Right: Cursor.Current = Cursors.PanEast; break;
                case Position.Center:
                    switch (BindingMode)
                    {
                        case BindingMode.RightToLeft: Cursor.Current = Cursors.PanSE; break;
                        case BindingMode.LeftToRight: Cursor.Current = Cursors.PanSW; break;
                        default: Cursor.Current = Cursors.PanWest; break;
                    }
                    break;
                default: Cursor.Current = Cursors.Default; break;
            }
        }
        */

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (
                KeyboardShortcut.GetKeyState((int)Keys.D) < 0 &&
                KeyboardShortcut.GetKeyState((int)Keys.E) < 0 &&
                KeyboardShortcut.GetKeyState((int)Keys.B) < 0 &&
                KeyboardShortcut.GetKeyState((int)Keys.U) < 0 &&
                KeyboardShortcut.GetKeyState((int)Keys.G) < 0)
            {
                var i = bmwLoadEachPage_DoWork_DEBUG_WorkNumber;
                var index = i >= 0 ? (i + 1).ToString() : "<EMPTY>";
                Program.ShowCursor();
                MessageBox.Show(this,
                    $"This is \"Debug information\" for the developer.\n" +
                    $"Loading index = {index}\n" +
                    $"Phase number = {bmwLoadEachPage_DoWork_DEBUG_Phase}\n" +
                    $"\n" +
                    $"Hold down '7' key and 'Z' key, and click \"OK\" to prioritize 7z.dll."
                    ,
                    "Debug information", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (
                KeyboardShortcut.GetKeyState((int)Keys.D7) < 0 &&
                KeyboardShortcut.GetKeyState((int)Keys.Z) < 0)
                {
                    var sevenZipPath = PackedImageLoader.GetSevenZipPath();
                    if (sevenZipPath != null)
                    {
                        PackedImageLoader.PrioritizeSevenZip = true;
                        Program.ShowCursor();
                        MessageBox.Show(this,
                            $"Following dll will be preferentially used until the window is closed.\n" +
                            $"\n" +
                            $"{sevenZipPath}."
                            ,
                            "Debug information", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        var prevArchiveFilePath = getCurrentRenameTarget();
                        if (loader != null)
                        {
                            loader.Dispose();
                            loader = null;
                        }

                        OpenFile(prevArchiveFilePath, currentPage, currentSortModeDetails);
                    }
                    else
                    {
                        Program.ShowCursor();
                        MessageBox.Show(this,
                            $"7z.dll is not found."
                            ,
                            "Debug information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            else
            {
                Program.ShowAbout(this);
            }
        }
        
        bool ContextMenuForMenuStrip_Opend = false;
        /*
        bool ContextMenuForMenuStripPostProcessing_worked = false;
        private void ContextMenuForMenuStripPostProcessing()
        {
            if (menuStrip.Visible && menuStrip_Buttom == 0 && FullScreen)
            {
                if (!menuStrip.ClientRectangle.Contains(menuStrip.PointToClient(Cursor.Position)))
                {
                    menuStrip_Buttom = menuStrip.Bounds.Bottom;
                    menuStrip.Visible = false;
                }
            }
            ContextMenuForMenuStrip_Opend = false;
            ContextMenuForMenuStripPostProcessing_worked = true;
        }
        */

        private void ContextMenuForMenuStripPostProcessing2()
        {
            if (MouseButtons == MouseButtons.Left && pbView.ClientRectangle.Contains(pbView.PointToClient(Cursor.Position)))
            {
                ksViewForm.IgnoreAnyMouseEventOnce = MouseButtons.Left;
            }

            if (menuStrip.Visible && menuStrip_Bottom == 0 && HideUI)
            {
                if (!menuStrip.ClientRectangle.Contains(menuStrip.PointToClient(Cursor.Position)))
                {
                    menuStrip_Bottom = menuStrip.Bounds.Bottom;
                    menuStrip.Visible = false;
                }

                // ESC の機能の発動を抑制しつつ
                // ESC で閉じたときに MouseMove が動かなくなる問題を解消
                if (KeyboardShortcut.GetKeyState(Keys.Escape))
                {
                    KeyboardShortcut.SendEscapeKey();
                }
            }
            ContextMenuForMenuStrip_Opend = false;

            if (DrawMeasure) pbPaintDrawOnlyMeasure();
            
            SetMessageForwarderStop(false);
        }

        private void startToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            var c = currentPage;
            var ea = EntryLongPathArray;
            if (ea != null && c >= 0 && c < ea.Length)
            {
                openNewThumbnailWindowToolStripMenuItem.Text = Message.SelectCurrentImageInThumbnailWindow;
            }
            else
            {
                openNewThumbnailWindowToolStripMenuItem.Text = Message.OpenThumbnailWindow;
            }
            
            var fullscreen = FullScreen;

            // 有効無効で表現
            //fullScreenInStartToolStripMenuItem.Enabled = !fullscreen;
            //windowToolStripMenuItem.Enabled = fullscreen;

            // アイコンで表現
            fullScreenInStartToolStripMenuItem.Checked = fullscreen;
            windowToolStripMenuItem.Checked = !fullscreen;

            SetMessageForwarderStop(true);
        }

        private void startToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            ContextMenuForMenuStrip_Opend = true;
            fixDropDownLocation(sender);
        }

        private void startToolStripMenuItem_DropDownClosed(object sender, EventArgs e)
        {
            ContextMenuForMenuStripPostProcessing2();
        }
        
        private void viewToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            ContextMenuForMenuStrip_Opend = true;
            fixDropDownLocation(sender);
        }

        private void viewToolStripMenuItem_DropDownClosed(object sender, EventArgs e)
        {
            ContextMenuForMenuStripPostProcessing2();
        }
        
        private ToolStripMenuItem getParcentageToolStripMenuItem(int i)
        {
            switch(i)
            {
                case 0: return percentage000YoolStripMenuItem;
                case 1: return percentage010YoolStripMenuItem;
                case 2: return percentage020YoolStripMenuItem;
                case 3: return percentage030YoolStripMenuItem;
                case 4: return percentage040YoolStripMenuItem;
                case 5: return percentage050YoolStripMenuItem;
                case 6: return percentage060YoolStripMenuItem;
                case 7: return percentage070YoolStripMenuItem;
                case 8: return percentage080YoolStripMenuItem;
                case 9: return percentage090YoolStripMenuItem;
                case 10: return percentage100YoolStripMenuItem;
                default: throw new ArgumentOutOfRangeException("i");
            }
        }

        private void moveToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            var nextEnabled = isMovablePageNatural(1);
            var prevEnabled = isMovablePageNatural(-1);
            var l2r = BindingMode != BindingMode.RightToLeft;

            nextPageToolStripMenuItem.Enabled = nextEnabled;
            previousPageToolStripMenuItem.Enabled = prevEnabled;
            rightPageToolStripMenuItem.Enabled = l2r ? nextEnabled : prevEnabled;
            leftPageToolStripMenuItem.Enabled = l2r ? prevEnabled : nextEnabled;

            var next1Enabled = isMovablePageToMinimulForward(); // isMovablePage(1);
            var prev1Enabled = isMovablePageToMinimulBack(); // isMovablePage(-1);

            moveForward1PageToolStripMenuItem.Enabled = next1Enabled;
            moveBack1PageToolStripMenuItem.Enabled = prev1Enabled;
            moveRight1PageToolStripMenuItem.Enabled = l2r ? next1Enabled : prev1Enabled;
            moveLeft1PageToolStripMenuItem.Enabled = l2r ? prev1Enabled : next1Enabled;

            var openNeighborEnabled = getCurrentParent() != null && !ExitAfterCoverSetting;
            openNextToolStripMenuItem.Enabled = openNeighborEnabled;
            openPreviousToolStripMenuItem.Enabled = openNeighborEnabled;
            openRightToolStripMenuItem.Enabled = openNeighborEnabled;
            openLeftToolStripMenuItem.Enabled = openNeighborEnabled;

            var currentIndex = (int)Math.Round(10*getRatioByPage()); // ページ数が 10 よりも十分に大きい場合に高い妥当性
            var enabledExists = false;
            for (var i = 0; i <= 10; i++)
            {
                var item = getParcentageToolStripMenuItem(i);
                var enabled = isMovablePageByRatio(i / 10.0);
                item.Enabled = enabled;
                enabledExists = enabledExists || enabled;
                item.Checked = i == currentIndex;
            }

            // ショートカットキー確認のため無効化しない
            // ratioToolStripMenuItem.Enabled = enabledExists;
            
            SetMessageForwarderStop(true);
        }

        private string getCurrentParent()
        {
            if (string.IsNullOrEmpty(currentArchiveFilePath)) return null;
            try
            {
                return Path.GetDirectoryName(currentArchiveFilePath);
            }
            catch
            {
            }
            return null;
        }

        private void moveToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            ContextMenuForMenuStrip_Opend = true;
            fixDropDownLocation(sender);
        }

        private void moveToolStripMenuItem_DropDownClosed(object sender, EventArgs e)
        {
            ContextMenuForMenuStripPostProcessing2();
        }

        private void slideshowToolStripMenuItem_DropDownClosed(object sender, EventArgs e)
        {
            ContextMenuForMenuStripPostProcessing2();
        }

        private void slideshowToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            ContextMenuForMenuStrip_Opend = true;
            fixDropDownLocation(sender);
        }

        /*
        private void imageFilterToolStripMenuItem_DropDownClosed(object sender, EventArgs e)
        {
            ContextMenuForMenuStripPostProcessing2();
        }

        private void imageFilterToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            ContextMenuForMenuStrip_Opend = true;
            fixDropDownLocation(sender);
        }
        */

        private void slideshowToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            var opend = PreFilteredImageArray != null;
            var enabled = opend && getSlideshowEnabled();
            startSlideshowToolStripMenuItem.Enabled = opend && !enabled;
            stopSlideshowToolStripMenuItem.Enabled = enabled;
            toggleSlideshowToolStripMenuItem.Enabled = opend;

            _1SecIntervalsToolStripMenuItem.Checked = tmSlideshow.Interval == 1 * 1000;
            _2SecIntervalsToolStripMenuItem.Checked = tmSlideshow.Interval == 2 * 1000;
            _3SecIntervalsToolStripMenuItem.Checked = tmSlideshow.Interval == 3 * 1000;
            _5SecIntervalsToolStripMenuItem.Checked = tmSlideshow.Interval == 5 * 1000;
            _10SecIntervalsToolStripMenuItem.Checked = tmSlideshow.Interval == 10 * 1000;
            _20SecIntervalsToolStripMenuItem.Checked = tmSlideshow.Interval == 20 * 1000;
            _30SecIntervalsToolStripMenuItem.Checked = tmSlideshow.Interval == 30 * 1000;
            _60SecIntervalsToolStripMenuItem.Checked = tmSlideshow.Interval == 60 * 1000;

            repeatToolStripMenuItem.Checked = SlideshowRepeat;
            openNextOnTerminalToolStripMenuItem.Checked = SlideshowGlobal;

            SetMessageForwarderStop(true);
        }

        private CheckState[] editToolStripMenuItem_TagInitialCheckState;
        private bool editToolStripMenuItem_newTagExists;
        private bool editToolStripMenuItem_pathHasPageDirectionInfo; 
        private bool editToolStripMenuItem_pathHasThumbnailInfo;
        private bool editToolStripMenuItem_initialTagEmpty;
        private void editToolStripMenuItem_DropDownOpening(object sender, EventArgs e) { editToolStripMenuItem_DropDownOpening(sender, e, null); }
        private void editToolStripMenuItem_DropDownOpening(object sender, EventArgs e, ContextMenuStrip contextMenuStrip)
        {
            var items = editToolStripMenuItem.DropDownItems;
            var tagStartIndex = items.IndexOf(uncheckAllToolStripMenuItem) + 1;
            var i = tagStartIndex;
            while (true)
            {
                var item = items[i];
                if (item == tagEditorToolStripMenuItem) break;
                items.RemoveAt(i);
            }

            if(ViewerMode == ViewerModeEnum.CoverSetting)
            {
                setThumbnailManuallyToolStripMenuItem.Text = Message.AbortSettingCover;
                setThumbnailManuallyToolStripMenuItem.ShortcutKeyDisplayString = Message.RightClick;
            }
            else
            {
                setThumbnailManuallyToolStripMenuItem.Text = Message.SetCoverManually;
                setThumbnailManuallyToolStripMenuItem.ShortcutKeyDisplayString = null;
            }

            var currentTarget = getCurrentRenameTarget();
            var currentExists = !string.IsNullOrEmpty(currentTarget);
            var thumbnailSetting = ViewerMode == ViewerModeEnum.CoverSetting;
            if (!currentExists || thumbnailSetting)
            {
                noneToolStripMenuItem.Enabled = false;
                rating1ToolStripMenuItem.Enabled = false;
                rating2ToolStripMenuItem.Enabled = false;
                rating3ToolStripMenuItem.Enabled = false;
                rating4ToolStripMenuItem.Enabled = false;
                rating5ToolStripMenuItem.Enabled = false;
                noneToolStripMenuItem.Checked = true;
                rating1ToolStripMenuItem.Checked = false;
                rating2ToolStripMenuItem.Checked = false;
                rating3ToolStripMenuItem.Checked = false;
                rating4ToolStripMenuItem.Checked = false;
                rating5ToolStripMenuItem.Checked = false;

                uncheckAllToolStripMenuItem.Enabled = false;
                tagEditorToolStripMenuItem.Enabled = false;
                addTagsFromTheFileNameToolStripMenuItem.Enabled = false;

                clearPageSequenceSettingToolStripMenuItem.Enabled = false;
                saveCurrentPageSequenceToolStripMenuItem.Enabled = false;
                clearThumbnailSettingToolStripMenuItem.Enabled = false;

                if (currentExists)
                {
                    setThumbnailManuallyToolStripMenuItem.Enabled = true;

                    ZipPlaInfo zipPlaInfo;
                    zipPlaInfo = new ZipPlaInfo(currentTarget);

                    ToolStripMenuItem[] partialItems;
                    CheckState[] initialCheckStates;
                    var tags = (new ZipTagConfig()).Tags;
                    if (contextMenuStrip == null)
                    {
                        Program.SetTagsToToolStripMenuItems(editToolStripMenuItem, out partialItems, out initialCheckStates, tagStartIndex, tags, zipPlaInfo);
                    }
                    else
                    {
                        Program.SetTagsToToolStripMenuItems(editToolStripMenuItem, contextMenuStrip, out partialItems, out initialCheckStates, tagStartIndex, tags, zipPlaInfo);
                    }
                    foreach (var item in partialItems) item.Enabled = false;
                }
                else
                {
                    setThumbnailManuallyToolStripMenuItem.Enabled = false;
                }
            }
            else
            {
                ZipPlaInfo zipPlaInfo;
                zipPlaInfo = new ZipPlaInfo(currentTarget);
                noneToolStripMenuItem.Enabled = true;
                rating1ToolStripMenuItem.Enabled = true;
                rating2ToolStripMenuItem.Enabled = true;
                rating3ToolStripMenuItem.Enabled = true;
                rating4ToolStripMenuItem.Enabled = true;
                rating5ToolStripMenuItem.Enabled = true;
                noneToolStripMenuItem.Checked = zipPlaInfo.Rating != 1 && zipPlaInfo.Rating != 2 && zipPlaInfo.Rating != 3 && zipPlaInfo.Rating != 4 && zipPlaInfo.Rating != 5;
                rating1ToolStripMenuItem.Checked = zipPlaInfo.Rating == 1;
                rating2ToolStripMenuItem.Checked = zipPlaInfo.Rating == 2;
                rating3ToolStripMenuItem.Checked = zipPlaInfo.Rating == 3;
                rating4ToolStripMenuItem.Checked = zipPlaInfo.Rating == 4;
                rating5ToolStripMenuItem.Checked = zipPlaInfo.Rating == 5;
                
                ToolStripMenuItem[] partialItems;
                CheckState[] initialCheckStates;
                var tags = (new ZipTagConfig()).Tags;
                if (contextMenuStrip == null)
                {
                    Program.SetTagsToToolStripMenuItems(editToolStripMenuItem, out partialItems, out initialCheckStates, tagStartIndex, tags, zipPlaInfo);
                }
                else
                {
                    Program.SetTagsToToolStripMenuItems(editToolStripMenuItem, contextMenuStrip, out partialItems, out initialCheckStates, tagStartIndex, tags, zipPlaInfo);
                }

                editToolStripMenuItem_TagInitialCheckState = initialCheckStates;

                if (partialItems.Length > 0)
                {
                    partialItems[0].ShortcutKeyDisplayString = Message.RightClickNotToClose;
                    if(partialItems.Length > 1)
                    {
                        partialItems[1].ShortcutKeyDisplayString = Message.RightFlickIsAlsoOk;
                        if (partialItems.Length > 2)
                        {
                            partialItems[2].ShortcutKeyDisplayString = Message.SameAsAbove;
                        }
                    }
                }

                EventHandler eh1 = (sender2, e2) =>
                {
                    if (unckeckAllTags_Changing) return;
                    uncheckAllToolStripMenuItem.Enabled = partialItems.Any(it => it.CheckState != CheckState.Unchecked);
                    (sender2 as ToolStripMenuItem).Invalidate();
                };

                editToolStripMenuItem_newTagExists = getNewTagsSet(zipPlaInfo).Length > 0;
                editToolStripMenuItem_pathHasPageDirectionInfo = zipPlaInfo.BindingModeForSet != null;
                editToolStripMenuItem_pathHasThumbnailInfo = zipPlaInfo.ThumbnailInfo != null;
                editToolStripMenuItem_initialTagEmpty = !initialCheckStates.Any(item => item != CheckState.Unchecked);
                
                uncheckAllToolStripMenuItem.Enabled = !editToolStripMenuItem_initialTagEmpty;
                tagEditorToolStripMenuItem.Enabled = true;

                var isBook = currentTarget == currentArchiveFilePath;
                var pageSequensCanBeSaved = isBook && NoScaling(BindingMode);
                //var isBookAndNormal = isBook && ViewerMode == ViewerModeEnum.Normal;

                var itemsForEventHandler = contextMenuStrip == null ? editToolStripMenuItem.DropDownItems : contextMenuStrip.Items;

                EventHandler eh2 = (sender2, e2) =>
                {
                    if (unckeckAllTags_Changing) return;
                    if (getTagChanged(itemsForEventHandler.Count > 0 ? itemsForEventHandler : items)) // 外側が閉じた後に呼び出されることがあり、その場合 items の方に入っている
                    {
                        noneToolStripMenuItem.Enabled = false;
                        rating1ToolStripMenuItem.Enabled = false;
                        rating2ToolStripMenuItem.Enabled = false;
                        rating3ToolStripMenuItem.Enabled = false;
                        rating4ToolStripMenuItem.Enabled = false;
                        rating5ToolStripMenuItem.Enabled = false;

                        tagEditorToolStripMenuItem.Enabled = false;
                        addTagsFromTheFileNameToolStripMenuItem.Enabled = false;

                        clearPageSequenceSettingToolStripMenuItem.Enabled = false;
                        saveCurrentPageSequenceToolStripMenuItem.Enabled = false;
                        clearThumbnailSettingToolStripMenuItem.Enabled = false;
                        setThumbnailManuallyToolStripMenuItem.Enabled = false;
                    }
                    else
                    {
                        noneToolStripMenuItem.Enabled = true;
                        rating1ToolStripMenuItem.Enabled = true;
                        rating2ToolStripMenuItem.Enabled = true;
                        rating3ToolStripMenuItem.Enabled = true;
                        rating4ToolStripMenuItem.Enabled = true;
                        rating5ToolStripMenuItem.Enabled = true;

                        tagEditorToolStripMenuItem.Enabled = true;
                        addTagsFromTheFileNameToolStripMenuItem.Enabled = editToolStripMenuItem_newTagExists;

                        clearPageSequenceSettingToolStripMenuItem.Enabled = editToolStripMenuItem_pathHasPageDirectionInfo;
                        saveCurrentPageSequenceToolStripMenuItem.Enabled = pageSequensCanBeSaved;
                        clearThumbnailSettingToolStripMenuItem.Enabled = editToolStripMenuItem_pathHasThumbnailInfo;
                        //setThumbnailManuallyToolStripMenuItem.Enabled = isBookAndNormal;
                        setThumbnailManuallyToolStripMenuItem.Enabled = isBook;
                    }
                };

                foreach (var item in partialItems)
                {
                    //item.MouseUp += eh1;
                    //item.MouseUp += eh2;
                    item.CheckStateChanged += eh1;
                    item.CheckStateChanged += eh2;
                }

                //tagEditorToolStripMenuItem.Enabled = true;


                addTagsFromTheFileNameToolStripMenuItem.Enabled = editToolStripMenuItem_newTagExists;

                clearPageSequenceSettingToolStripMenuItem.Enabled = editToolStripMenuItem_pathHasPageDirectionInfo;
                saveCurrentPageSequenceToolStripMenuItem.Enabled = pageSequensCanBeSaved;
                clearThumbnailSettingToolStripMenuItem.Enabled = editToolStripMenuItem_pathHasThumbnailInfo;
                //setThumbnailManuallyToolStripMenuItem.Enabled = isBookAndNormal;
                setThumbnailManuallyToolStripMenuItem.Enabled = isBook;
            }

            ResetToolStripMenuItem(sender);
            SetMessageForwarderStop(true);
        }

        private bool getTagChanged(ToolStripItemCollection items)
        {
            if (editToolStripMenuItem_TagInitialCheckState == null) return false;
            //var items = editToolStripMenuItem.DropDownItems;
            var tagStartIndex = items.IndexOf(uncheckAllToolStripMenuItem) + 1;
            var i = tagStartIndex;
            var partialItems = GetTagToolStripMenuItems(items);
            var count = partialItems.Length;
            if (editToolStripMenuItem_TagInitialCheckState.Length != count) return false;
            for(var j = 0; j < count; j++)
            {
                if (editToolStripMenuItem_TagInitialCheckState[j] != partialItems[j].CheckState) return true;
            }
            return false;
        }

        private PrefixEscapedToolStripMenuItem[] GetTagToolStripMenuItems(ToolStripItemCollection items)
        {
            var tagStartIndex = items.IndexOf(uncheckAllToolStripMenuItem) + 1;
            var i = tagStartIndex;
            var partialItems = new List<PrefixEscapedToolStripMenuItem>();
            while (true)
            {
                var item = items[i++] as PrefixEscapedToolStripMenuItem;
                if (item == null) break;
                partialItems.Add(item);
            }
            return partialItems.ToArray(); ;
        }

        private void optionToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            ContextMenuForMenuStrip_Opend = true;
            fixDropDownLocation(sender);
        }
        
        private void optionToolStripMenuItem_DropDownClosed(object sender, EventArgs e)
        {
            ContextMenuForMenuStripPostProcessing2();
        }

        private void tagEditorToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (Program.ShowTagEditor(this))
            {
                RequestToUpdateTags();
            }
        }

        private void preferenceToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var pre = BuiltInViewerMemoryLimit;
            Program.ShowSetting(this, out BuiltInViewerMemoryLimit);
            if (pre > BuiltInViewerMemoryLimit)
            {
                SetMemoryUBound();
                ReduceUsingMemory(usedMemoryUBound);
            }
        }

        private void editToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            ContextMenuForMenuStrip_Opend = true;
            fixDropDownLocation(sender);
        }

        private void editToolStripMenuItem_DropDownClosed(object sender, EventArgs e)
        {
            ContextMenuForMenuStripPostProcessing2();
            editToolStripMenuItem_DropDownClosed_CommonPart(sender, e);
        }

        private void editToolStripMenuItem_DropDownClosed_CommonPart(object sender, EventArgs e)
        {
            if (getTagChanged(editToolStripMenuItem.DropDownItems))
            {
                Task.Run(() => Invoke((MethodInvoker)(() => SetTagToFileName())));
            }
        }


        private void SetTagToFileName()
        {
            //if (string.IsNullOrEmpty(currentArchiveFilePath)) return;

            var prevArchiveFilePath = getCurrentRenameTarget();
            if (string.IsNullOrEmpty(prevArchiveFilePath)) return;
            var zipPlaInfo = new ZipPlaInfo(prevArchiveFilePath);
            
            var checkedItems = GetTagToolStripMenuItems(editToolStripMenuItem.DropDownItems);
            var tagCount = checkedItems.Length;
            var tags = zipPlaInfo.TagArray?.ToList();
            if (tags == null) tags = new List<string>();
            for (var j = 0; j < tagCount; j++)
            {
                var checkedItem = checkedItems[j];
                var state = checkedItem.CheckState;
                if (state != CheckState.Indeterminate)
                {
                    var tagName = checkedItem.PlainText;
                    if (tags.Contains(tagName))
                    {
                        if (state == CheckState.Unchecked)
                        {
                            tags.Remove(tagName);
                        }
                    }
                    else
                    {
                        if (state == CheckState.Checked)
                        {
                            tags.Add(tagName);
                        }
                    }
                }
            }
            zipPlaInfo.TagArray = tags.Count == 0 ? null : tags.ToArray();

            var newArchiveFilePath = zipPlaInfo.GetPathOfCurrentInfo(isDirSafety(prevArchiveFilePath));

            Program.ShowCursor();
            /*
            if (MessageBox.Show(
                Message.RenameForTags + "\r\n\r\n" +
                Message.BeforeRename + ":\r\n  " + Path.GetFileName(prevArchiveFilePath) + "\r\n" +
                Message.AfterRename + ":\r\n  " + Path.GetFileName(newArchiveFilePath) + "\r\n", Message.Question, MessageBoxButtons.OKCancel, MessageBoxIcon.Question)
                != DialogResult.OK)
            {
                return;
            }
            */
            if (MessageForm.Show(this,
                Message.RenameForTags + "\r\n\r\n" +
                Message.BeforeRename + ":\r\n  " + Path.GetFileName(prevArchiveFilePath) + "\r\n" +
                Message.AfterRename + ":\r\n  " + Path.GetFileName(newArchiveFilePath) + "\r\n", Message.Question, Message.OK, Message.Cancel, MessageBoxIcon.Question)
                != 0)
            {
                return;
            }

            if (loader != null)
            {
                loader.Dispose();
                loader = null;
            }
            try
            {
                Program.FileOrDirectoryMove(prevArchiveFilePath, newArchiveFilePath, retryOwner: this);
                //currentArchiveFilePath = newArchiveFilePath;
            }
            catch// (Exception error)
            {
                //MessageBox.Show(this, error.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error); // よくあることなので。
                //OpenFile(newArchiveFilePath); // こちらは不適切
                OpenFile(prevArchiveFilePath, currentPage, currentSortModeDetails); // loader を null にしてしまっているので再読み込みは必要
                return;
            }

            // 名前の変更を報告
            SendRenameMessageToCatalogForm(prevArchiveFilePath, newArchiveFilePath);

            //MessageBox.Show(Message.DonePageSequenceSetting, Message.Information, MessageBoxButtons.OK, MessageBoxIcon.Information);
            OpenFile(newArchiveFilePath, currentPage, currentSortModeDetails);
        }

        private void noneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetRatingToFileName(0);
        }

        private void rating1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetRatingToFileName(1);
        }

        private void rating2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetRatingToFileName(2);
        }

        private void rating3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetRatingToFileName(3);
        }

        private void rating4ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetRatingToFileName(4);
        }

        private void rating5ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetRatingToFileName(5);
        }

        private string getCurrentRenameTarget()
        {
            if (currentArchiveFilePath == null) return null;
            if (currentStartingFilePath == currentArchiveFilePath) return currentArchiveFilePath;
            try
            {
                var loaderExists = loader != null;

                //if (Directory.Exists(currentArchiveFilePath))
                if (loaderExists ? loader.Type == PackedImageLoader.PackType.Directory : Directory.Exists(currentArchiveFilePath))
                {
                    var path = EntryLongPathArray[mtbPage.Value].Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                    if (!File.Exists(path)) return null;
                    return path;
                }
                else if (loaderExists || File.Exists(currentArchiveFilePath))// if (File.Exists(currentArchiveFilePath))
                {
                    return currentArchiveFilePath;
                }
                else return null;
            }
            catch
            {
                return null;
            }

        }

        private void SetRatingToFileName(int rating)
        {
            //if (string.IsNullOrEmpty(currentArchiveFilePath)) return;

            var prevArchiveFilePath = getCurrentRenameTarget();
            if (preferenceToolStripMenuItem1 == null) return;
            var zipPlaInfo = new ZipPlaInfo(prevArchiveFilePath);
            zipPlaInfo.Rating = (1 <= rating && rating <= 5) ? rating : null as int?;
            var newArchiveFilePath = zipPlaInfo.GetPathOfCurrentInfo(isDirSafety(prevArchiveFilePath));

            Program.ShowCursor();
            /*
            if (MessageBox.Show(this, 
                Message.RenameForRating + "\r\n\r\n" +
                Message.BeforeRename + ":\r\n  " + Path.GetFileName(prevArchiveFilePath) + "\r\n" +
                Message.AfterRename + ":\r\n  " + Path.GetFileName(newArchiveFilePath) + "\r\n", Message.Question, MessageBoxButtons.OKCancel, MessageBoxIcon.Question)
                != DialogResult.OK)
            {
                return;
            }*/
            if (MessageForm.Show(this,
                Message.RenameForRating + "\r\n\r\n" +
                Message.BeforeRename + ":\r\n  " + Path.GetFileName(prevArchiveFilePath) + "\r\n" +
                Message.AfterRename + ":\r\n  " + Path.GetFileName(newArchiveFilePath) + "\r\n", Message.Question, Message.OK, Message.Cancel, MessageBoxIcon.Question)
                != 0)
            {
                return;
            }

            if (loader != null)
            {
                loader.Dispose();
                loader = null;
            }
            try
            {
                Program.FileOrDirectoryMove(prevArchiveFilePath, newArchiveFilePath, retryOwner: this);
                //currentArchiveFilePath = newArchiveFilePath;
            }
            catch// (Exception error)
            {
                //MessageBox.Show(this, error.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error); // よくあることなので。
                //OpenFile(newArchiveFilePath); // こちらは不適切
                OpenFile(prevArchiveFilePath, currentPage, currentSortModeDetails); // loader を null にしてしまっているので再読み込みは必要
                return;
            }

            // 名前の変更を報告
            SendRenameMessageToCatalogForm(prevArchiveFilePath, newArchiveFilePath);

            //MessageBox.Show(this, Message.DonePageSequenceSetting, Message.Information, MessageBoxButtons.OK, MessageBoxIcon.Information);
            OpenFile(newArchiveFilePath, currentPage, currentSortModeDetails);
        }

        private bool unckeckAllTags_Changing = false;
        private void unckeckAllTags()
        {
            var dropDownItems = uncheckAllToolStripMenuItem.Owner.Items;
            //var dropDownItems = editToolStripMenuItem.DropDownItems;
            var i = dropDownItems.IndexOf(uncheckAllToolStripMenuItem);
            var count = dropDownItems.Count;
            unckeckAllTags_Changing = true;
            while (i < count)
            {
                var item = dropDownItems[i++] as ToolStripMenuItem;
                if (item == tagEditorToolStripMenuItem) break;
                item.Checked = false;
            }
            unckeckAllTags_Changing = false;

            if (!editToolStripMenuItem_initialTagEmpty)
            {
                noneToolStripMenuItem.Enabled = false;
                rating1ToolStripMenuItem.Enabled = false;
                rating2ToolStripMenuItem.Enabled = false;
                rating3ToolStripMenuItem.Enabled = false;
                rating4ToolStripMenuItem.Enabled = false;
                rating5ToolStripMenuItem.Enabled = false;

                tagEditorToolStripMenuItem.Enabled = false;
                addTagsFromTheFileNameToolStripMenuItem.Enabled = false;

                clearPageSequenceSettingToolStripMenuItem.Enabled = false;
                saveCurrentPageSequenceToolStripMenuItem.Enabled = false;
                clearThumbnailSettingToolStripMenuItem.Enabled = false;
                setThumbnailManuallyToolStripMenuItem.Enabled = false;
            }
            else
            {
                noneToolStripMenuItem.Enabled = true;
                rating1ToolStripMenuItem.Enabled = true;
                rating2ToolStripMenuItem.Enabled = true;
                rating3ToolStripMenuItem.Enabled = true;
                rating4ToolStripMenuItem.Enabled = true;
                rating5ToolStripMenuItem.Enabled = true;

                tagEditorToolStripMenuItem.Enabled = true;
                addTagsFromTheFileNameToolStripMenuItem.Enabled = editToolStripMenuItem_newTagExists;

                clearPageSequenceSettingToolStripMenuItem.Enabled = editToolStripMenuItem_pathHasPageDirectionInfo;
                saveCurrentPageSequenceToolStripMenuItem.Enabled = true;
                clearThumbnailSettingToolStripMenuItem.Enabled = editToolStripMenuItem_pathHasThumbnailInfo;
                setThumbnailManuallyToolStripMenuItem.Enabled = true;
            }
        }

        private void uncheckAllToolStripMenuItem_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                //editToolStripMenuItem.DropDown.AutoClose = false;
                (uncheckAllToolStripMenuItem.Owner as ToolStripDropDown).AutoClose = false;
            }
            if (e.Button == MouseButtons.Right || e.Button == MouseButtons.Left)
            {
                unckeckAllTags();
            }
        }

        private void uncheckAllToolStripMenuItem_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                //unckeckAllTags();
                //editToolStripMenuItem.DropDown.AutoClose = true;
                (uncheckAllToolStripMenuItem.Owner as ToolStripDropDown).AutoClose = true;
            }
            //uncheckAllToolStripMenuItem.Enabled = false;
            DisableUncheckAllToolStripMenuItemIfNeeded();
        }

        private void uncheckAllToolStripMenuItem_MouseLeave(object sender, EventArgs e)
        {
            (uncheckAllToolStripMenuItem.Owner as ToolStripDropDown).AutoClose = true;
            DisableUncheckAllToolStripMenuItemIfNeeded();
        }
        
        private void DisableUncheckAllToolStripMenuItemIfNeeded()
        {
            if (!uncheckAllToolStripMenuItem.Enabled) return;
            var dropDownItems = (uncheckAllToolStripMenuItem.Owner as ToolStripDropDown)?.Items;
            if (dropDownItems == null) return;
            var start = dropDownItems.IndexOf(uncheckAllToolStripMenuItem) + 1;
            if (start <= 0) return;
            var stop = dropDownItems.IndexOf(tagEditorToolStripMenuItem);
            if (stop < 0) return;
            for (var i = start; i < stop; i++)
            {
                var item = dropDownItems[i] as ToolStripMenuItem;
                if (item == null || item.CheckState != CheckState.Unchecked) return;
            }
            uncheckAllToolStripMenuItem.Enabled = false;
        }

        private void tagEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Program.ShowTagEditor(this))
            {
                RequestToUpdateTags();
            }
        }

        private void addTagsFromTheFileNameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentArchiveFilePath == null) return;
            var info = new ZipPlaInfo(currentArchiveFilePath);
            var newTagsSet = getNewTagsSet(info);
            if(newTagsSet != null && newTagsSet.Length > 0)
            {
                if (Program.ShowTagEditor(this, newTagsSet))
                {
                    RequestToUpdateTags();
                }
            }
        }

        private string[] getNewTagsSet(ZipPlaInfo info)
        {
            if (info == null || info.TagArray == null) return new string[0];
            var tagConfig = new ZipTagConfig();
            var tags = tagConfig.Tags;
            if (tags == null || tags.Length == 0) return new string[0];
            var newTagsSet = new HashSet<string>();
            var tagNames = (from tag in tags select tag.Name).ToArray();
            if (info.TagArray != null) foreach (var tag in info.TagArray) if (!tagNames.Contains(tag)) newTagsSet.Add(tag);
            return newTagsSet.ToArray();
        }

        private void clearPageSequenceSettingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetPageDirectionToFileName(true);
        }

        private void saveCurrentPageSequenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetPageDirectionToFileName();
        }

        private void clearThumbnailSettingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetCoverForThumbnail(true);
        }

        private void setThumbnailManuallyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var vm = ViewerMode;
            if (vm != ViewerModeEnum.CoverSetting)
            {
                //if (vm == ViewerModeEnum.Normal)
                {
                    if (imageFilter.PreFilterExists() ||
                        (BindingMode != BindingMode.SinglePage && BindingMode != BindingMode.SinglePageWithoutScalingUp && MaxDivision > 1))
                    {
                        reloadForChangeFilter(forOnlyPostFilter: false);
                    }
                    else if (imageFilter.PostFilterExists())
                    {
                        reloadForChangeFilter(forOnlyPostFilter: true);
                    }

                    pbView_Paint_RectangleMessage = null;
                    ViewerMode = ViewerModeEnum.CoverSetting;
                    ksViewForm.Enabled = true;
                    pbView_Paint_Rectangle_FillAround = true;
                    ShwoCoverRectangle();
                }
            }
            else
            {
                ExitCoverSetting();
            }
        }
        

        private void mtbPage_MouseMove(object sender, MouseEventArgs e)
        {
            mtbPage.ValueCanChangedByMouseButton = true;
            SetBackgroundMode();
        }

        private bool MouseDownForCoverSettingExit = false;
        private void pbView_MouseDown(object sender, MouseEventArgs e)
        {
            // キーボードショートカットに置き換え
            /*
            if (e.Button == MouseButtons.Middle)
            {
                if (ViewerMode == ViewerModeEnum.Normal)
                {
                    MagnifierPhase1();
                }
                else if (ViewerMode == ViewerModeEnum.Magnifier)
                {
                    MagnifierPhase3();
                }
            }
            else*/
            if (e.Button == MouseButtons.Left)
            {
                if (ksViewForm.IgnoreAnyMouseEventOnce != MouseButtons.Left)
                {
                    if (ViewerMode == ViewerModeEnum.CoverSetting)
                    {
                        SetCoverForThumbnail();
                        ksViewForm.IgnoreMouseEventOnce = e;
                    }
                    /*
                    else
                    {
                        if (!ksViewForm.ActionsContainsLButton)
                        {
                            defaultClickBehavior(e.Location);
                        }
                    }
                    */
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (ViewerMode == ViewerModeEnum.CoverSetting)
                {
                    ExitCoverSetting();
                    if (mgView.Enabled)
                    {
                        MouseDownForCoverSettingExit = true;
                    }
                    else
                    {
                        ksViewForm.IgnoreMouseEventOnce = e;
                    }
                }
            }
            //setMouseButtonShape(position);
        }

        /*
        private void defaultClickBehavior(Point clientPoint)
        {
            if (ViewerMode == ViewerModeEnum.Normal)// || ViewerMode == ViewerModeEnum.Magnifier)
            {
                var position = GetPositionOnView(clientPoint);
                switch (position)
                {
                    case Position.Left:
                        movePageNatural(BindingMode != BindingMode.RightToLeft ? -1 : 1);
                        break;
                    case Position.Right:
                        movePageNatural(BindingMode != BindingMode.RightToLeft ? 1 : -1);
                        break;
                    case Position.Center:
                        movePageToMinimulBack();
                        break;
                }
            }
        }
        */

        /*
        private void pbView_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                if (ViewerMode == ViewerModeEnum.MagnifierOpening)
                {
                    MagnifierPhase2();
                }
                else if (ViewerMode == ViewerModeEnum.MagnifierClosing)
                {
                    MagnifierPhase4();
                }
            }
        }
        */

        private void closeWindowtoolStripMenuItem_MouseEnter(object sender, EventArgs e)
        {
            // メニューバー上のボタンの色の変化をマウスカーソルの位置と正確に一致させるための処理
            if (FullScreen)
            {
                var tsmi = (ToolStripMenuItem)sender;
                tsmi.Select();
            }
            
            // 古い方法
            /*
            if (FullScreen)
            {
                var tsmi = (ToolStripMenuItem)sender;
                var temp = Cursor.Position;
                Cursor.Position = new Point(Width - 1 - tsmi.Width / 2, tsmi.Height / 2);
                Application.DoEvents();
                Cursor.Position = temp;
            }
            */
        }

        private void ChangePageDirection(BindingMode binding)
        {
            if(BindingMode != binding)
            {
                var sizeChanged = getNeedToResizeImage(binding, bindingModeField);
                //BindingMode = binding;
                ChangeBindingModeCore(binding, redraw: !sizeChanged);
                if (sizeChanged) Reload(force: true);
                ShowViewSettingHint();
            }
            
            //PageLeftToRight = pageLeftToRight;
        }

        private bool getNeedToResizeImage(BindingMode a, BindingMode b)
        {
            var aIsSpread = a == BindingMode.LeftToRight || a == BindingMode.RightToLeft;
            var bIsSpread = b == BindingMode.LeftToRight || b == BindingMode.RightToLeft;
            if (aIsSpread && bIsSpread) return false;
            if (MaxDivision > 1 || MinPageCountInWindow > 1) return true;
            return !((aIsSpread || a == BindingMode.SinglePage) && (bIsSpread || b == BindingMode.SinglePage));
        }
        
        /*
        private void ViewerForm_KeyDown(object sender, KeyEventArgs e)
        {
            bool Handled = true;
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    windowToolStripMenuItem_Click(null, null);
                    break;
                default:
                    Handled = false;
                    break;
            }
            if (Handled)
            {
                e.Handled = true;
            }
        }
        */

        private static readonly int Control_PreviewKeyDown_Speed = Program.DpiScaling(20);
        private bool KeysUpState = false;
        private bool KeysDownState = false;
        private bool KeysLeftState = false;
        private bool KeysRightState = false;
        private void Control_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            {
                var preUp = KeysUpState;
                var preDown = KeysDownState;
                var preLeft = KeysLeftState;
                var preRight = KeysRightState;

                var up = e.KeyCode == Keys.Up; ;
                var down = e.KeyCode == Keys.Down;
                var left  = e.KeyCode == Keys.Left;
                var right = e.KeyCode == Keys.Right;

                KeysUpState = KeysUpState || up;
                KeysDownState = KeysDownState || down;
                KeysLeftState = KeysLeftState || left;
                KeysRightState = KeysRightState || right;

                var vm = ViewerMode;
                if (vm == ViewerModeEnum.MagnifierOpening || vm == ViewerModeEnum.MagnifierClosing)
                {
                    var r = BoolToInt(KeysRightState);
                    var l = BoolToInt(KeysLeftState);
                    var d = BoolToInt(KeysDownState);
                    var u = BoolToInt(KeysUpState);

                    var sum = r + l + d + u;

                    if (sum == 1 || sum == 2 && (KeysRightState ^ KeysLeftState))
                    {
                        var xu = r - l;
                        var yu = d - u;

                        var currentPosition = Cursor.Position;
                        Cursor.Position = new Point(currentPosition.X + Control_PreviewKeyDown_Speed * xu, currentPosition.Y + Control_PreviewKeyDown_Speed * yu);
                    }
                    else if (sum == 2)
                    {
                        // 直前の操作をキャンセル
                        var xu = !preRight && right ? +1 : !preLeft && left ? -1 : 0;
                        var yu = !preDown && down ? +1 : !preUp && up ? -1 : 0;
                        if (xu != 0 || yu != 0)
                        {
                            var currentPosition = Cursor.Position;
                            pbView_PreviewKeyDown_To_pbView_MouseMove_StopShwoMagnifierRectangle = true;
                            Cursor.Position = new Point(currentPosition.X + Control_PreviewKeyDown_Speed * xu, currentPosition.Y + Control_PreviewKeyDown_Speed * yu);
                        }

                        // 上下で拡大、左右で縮小
                        changeMagnifyingPowerInMagnifierOpeningOrClosing(zoomUp: KeysUpState);
                    }
                }
            }
            else if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.LControlKey || e.KeyCode == Keys.RControlKey)
            {
                changeMagnifyingPowerInMagnifierOpeningOrClosing(zoomUp: true);
            }
            else if (e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.LShiftKey || e.KeyCode == Keys.RShiftKey)
            {
                changeMagnifyingPowerInMagnifierOpeningOrClosing(zoomUp: false);
            }
        }

        private void ViewerForm_KeyUp(object sender, KeyEventArgs e)
        {
            KeysUpState = KeysUpState && e.KeyCode != Keys.Up;
            KeysDownState = KeysDownState && e.KeyCode != Keys.Down;
            KeysLeftState = KeysLeftState && e.KeyCode != Keys.Left;
            KeysRightState = KeysRightState && e.KeyCode != Keys.Right;
        }

        private static int BoolToInt(bool b)
        {
            return b ? 1 : 0;
        }

        /*
        private void ViewerForm_KeyUp(object sender, KeyEventArgs e)
        {
            bool Handled = true;
            switch (e.KeyCode)
            {
                case Keys.Z:
                    if (ViewerMode == ViewerModeEnum.MagnifierOpening)
                    {
                        MagnifierPhase2();
                    }
                    else if (ViewerMode == ViewerModeEnum.MagnifierClosing)
                    {
                        MagnifierPhase4();
                    }
                    break;
                default:
                    Handled = false;
                    break;
            }
            if (Handled)
            {
                e.Handled = true;
            }
        }
        */

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileWithDialog();
        }

        private void openFileWithDialog()
        {
            if (ExitAfterCoverSetting) return;
            var path = Program.ItsSelfOrExistingParentDirectory(currentArchiveFilePath);
            if (!string.IsNullOrEmpty(path))
            {
                openFileDialog.InitialDirectory = path;
            }
            openFileDialog.FileName = "";
            var supportedImageFileFilter = ImageLoader.GetSupportedFileFilter();
            PackedImageLoader.CheckSevenZipExistence(this);
            var supportedArchiveFileFilter = PackedImageLoader.GetSupportedArchiveFileFilter();
            openFileDialog.Filter = Message.AllSupportedFiles + "|" + supportedArchiveFileFilter + ";" + supportedImageFileFilter + "|"
                + Message.ArchiveFilesPdfFiles + "|" + supportedArchiveFileFilter + "|"
                + Message.ImageFiles + "|" + supportedImageFileFilter;

            if (openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                removeHiddenConfigFromCatalogForm();
                OpenFile(openFileDialog.FileName);
            }
        }

        private void openNewThumbnailWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openThumbnailWindow();
        }
        
        private void switchToThumbnailWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switchToThumbnailWindow();
        }

        private void switchToThumbnailWindow()
        {
            SaveSettings();
            openThumbnailWindow(cancelFullscreen: false);
            CloseWithoutSave();
        }

        private void openThumbnailWindow() { openThumbnailWindow(cancelFullscreen: true); }
        private void openThumbnailWindow(bool cancelFullscreen)
        {
            if (ExitAfterCoverSetting) return;
            try
            {
                var c = currentPage;
                var ea = EntryLongPathArray;
                if (ea != null && c >= 0 && c < ea.Length)
                {
                    if (currentSortModeDetails != null)
                    {
                        var sort = currentSortModeDetails.SortMode;
                        if (sort == SortMode.NameInAsc || sort == SortMode.NameInDesc)
                        {
                            System.Diagnostics.Process.Start(Application.ExecutablePath, $"-c \"{ea[c]}\" -{sort}");
                        }
                        else if(sort == SortMode.Random && !string.IsNullOrEmpty(currentSortModeDetails.RandomSeed))
                        {
                            System.Diagnostics.Process.Start(Application.ExecutablePath, $"-c \"{ea[c]}\" -{sort} \"{currentSortModeDetails.RandomSeed}\"");
                        }
                        else
                        {
                            System.Diagnostics.Process.Start(Application.ExecutablePath, $"-c \"{ea[c]}\" -{currentSortModeDetails.PreSortMode} -{sort}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Process.Start(Application.ExecutablePath, $"-c \"{ea[c]}\"");
                    }
                }
                else if (string.IsNullOrEmpty(currentArchiveFilePath))
                {
                    System.Diagnostics.Process.Start(Application.ExecutablePath, "-c");
                }
                else
                {
                    System.Diagnostics.Process.Start(Application.ExecutablePath, "-c \"" + currentArchiveFilePath + "\"");
                }
                if (cancelFullscreen && FullScreen)
                {
                    fullScreenModeChange(false);
                }
            }
            catch (Exception error)
            {
                Program.AlertError(error);
            }
        }

        private void cloneTheWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cloneWindow();
        }

        private void cloneWindow()
        {
            if (ExitAfterCoverSetting) return;
            try
            {
                var longPath = null as string;
                var value = mtbPage.Value;
                if (value >= 0)
                {
                    var elpa = EntryLongPathArray;
                    if (elpa != null && value < elpa.Length)
                    {
                        longPath = elpa[value];
                        if (longPath == "") longPath = null;
                    }
                }

                var info = CreateCommandLineOptionInfo();
                info.OpenInPreviousImageFilterSetting = true;
                info.InitialFullscreenMode = InitialFullscreenMode.Default;
                var option = GetCommandLineOption(info, minimize: true);
                if (longPath != null) option += $" \"{longPath}\"";

                SaveSettings();

                Process.Start(Application.ExecutablePath, option);
            }
            catch (Exception error)
            {
                Program.AlertError(error);
            }
        }

        private void fullScreenInStartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fullScreenModeChangeToFullScreen();
        }

        private void fullScreenModeChangeToFullScreen()
        {
            fullScreenModeChange(true);
        }

        private void windowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fullScreenModeChangeToWindow();
        }

        private void fullScreenModeChangeToWindow()
        {
            fullScreenModeChange(false);
        }

        private void toggleFullScreenModeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toggleFullScreenMode();
        }

        private void toggleFullScreenMode()
        {
            fullScreenModeChange(!FullScreen);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }


        /*
        private void magnifierContextMenuForMenuStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ViewerMode == ViewerModeEnum.Normal)
            {
                MagnifierPhase1();
                MagnifierPhase2();
            }
            else if (ViewerMode == ViewerModeEnum.Magnifier)
            {
                MagnifierPhase3();
                MagnifierPhase4();
            }
        }

        private void rightToLeftContextMenuForMenuStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangePageDirection(false);
        }

        private void leftToRightContextMenuForMenuStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangePageDirection(true);
        }
        */

        private void magnifierToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToggleMagnifierWithoutScreenEffects();
        }


        private void ToggleMagnifierWithoutScreenEffects()
        {
            // if は省略できないので注意
            if (ViewerMode == ViewerModeEnum.Normal)
            {
                enableMagnifier();
            }
            else if (ViewerMode == ViewerModeEnum.Magnifier)
            {
                disableMagnifier();
            }
        }

        private void enableMagnifier()
        {
            if (ViewerMode == ViewerModeEnum.Normal)
            {
                MagnifierPhase1(null);
                MagnifierPhase2();
            }
        }

        private void disableMagnifier()
        {
            if (ViewerMode == ViewerModeEnum.Magnifier)
            {
                MagnifierPhase3(toPhase4: true);
                MagnifierPhase4(toPhase1: false);
            }
        }

        private void toggleMagnifierForKeyboardWithOperatingGuide() => toggleMagnifierForKeyboard(withOperatingGuide: true);
        private void toggleMagnifierForKeyboard() => toggleMagnifierForKeyboard(withOperatingGuide: false);
        private void toggleMagnifierForKeyboard(bool withOperatingGuide)
        {
            // if は省略できないので注意
            if (ViewerMode == ViewerModeEnum.Normal)
            {
                enableMagnifierForKeyboard(withOperatingGuide);
            }
            else if (ViewerMode == ViewerModeEnum.Magnifier)
            {
                disableMagnifierForKeyboard(withOperatingGuide);
            }
        }


        private void toggleMagnifierForKeyboardWithMinimalScreenEffects()
        {
            // if は省略できないので注意
            if (ViewerMode == ViewerModeEnum.Normal)
            {
                enableMagnifierForKeyboard();
            }
            else if (ViewerMode == ViewerModeEnum.Magnifier)
            {
                disableMagnifier();
                //disableMagnifierForKeyboard();
            }
        }

        bool userMagnifierOpeningOrClosing = false;
        bool UserMagnifierOpeningOrClosing
        {
            get { return userMagnifierOpeningOrClosing; }
            set
            {
                if (value != userMagnifierOpeningOrClosing)
                {
                    if (value)
                    {
                        userMagnifierOpeningOrClosing = true;
                        ksViewForm.WheelAcceptControls = new Control[0];
                    }
                    else
                    {
                        userMagnifierOpeningOrClosing = false;
                        ksViewForm.WheelAcceptControls = new Control[1] { pbView };
                    }
                }
            }
        }

        private void enableMagnifierForKeyboardWithOperatingGuide() => enableMagnifierForKeyboard(withOperatingGuide: true);
        private void enableMagnifierForKeyboard() => enableMagnifierForKeyboard(withOperatingGuide: false);
        private void enableMagnifierForKeyboard(bool withOperatingGuide)
        {
            var vm = ViewerMode;
            if (vm == ViewerModeEnum.Normal)
            {
                //if (ksViewForm.KeyboardShortcutExcutingWithKeyOrMouseDown()) UserMagnifierOpeningOrClosing = true;
                UserMagnifierOpeningOrClosing = true;
                if (withOperatingGuide) pbView_Paint_RectangleMessage = Message.MagnifierOperatingGuide.Replace("\\n", "\n");
                MagnifierPhase1(null);
                KeyUp += MagnifierPhase2_KeyUpEventHandler;
                MouseUp += MagnifierPhase2_KeyUpEventHandler;
                pbView.SizeChanged += MagnifierPhase2_KeyUpEventHandler;
                foreach (Control control in Controls)
                {
                    control.MouseUp += MagnifierPhase2_KeyUpEventHandler;
                }
            }
            else if(vm == ViewerModeEnum.Magnifier)
            {
                //if (ksViewForm.KeyboardShortcutExcutingWithKeyOrMouseDown()) UserMagnifierOpeningOrClosing = true;
                UserMagnifierOpeningOrClosing = true;
                MagnifierPhase3(toPhase4: true);
                MagnifierPhase4(toPhase1: true);
                if (withOperatingGuide) pbView_Paint_RectangleMessage = Message.MagnifierOperatingGuide.Replace("\\n", "\n");
                MagnifierPhase1(null);
                KeyUp += MagnifierPhase2_KeyUpEventHandler;
                pbView.MouseUp += MagnifierPhase2_KeyUpEventHandler;
                pbView.SizeChanged += MagnifierPhase2_KeyUpEventHandler;
                foreach (Control control in Controls)
                {
                    control.MouseUp += MagnifierPhase2_KeyUpEventHandler;
                }
            }
        }

        private void MagnifierPhase2_KeyUpEventHandler(object sender, MouseEventArgs e)
        {
            var key = ksViewForm.MouseButtonsToKeys(e.Button);
            if (key != Keys.None)
            {
                MagnifierPhase2_KeyUpEventHandler(sender, new KeyEventArgs(key));
            }
        }

        private void MagnifierPhase2_KeyUpEventHandler(object sender, KeyEventArgs e)
        {
           // UserMagnifierOpeningOrClosing = false;
            var actions = ksViewForm.Actions;
            if (actions == null || actions.Any(action =>
                IsEnableMagnifierCommand((Command)action.Key) && action.Shortcut.LastOrDefault()?.Contains(e.KeyCode) == true))
            {
                MagnifierPhase2_KeyUpEventHandler(sender, e as EventArgs);
            }
        }

        private static bool IsEnableMagnifierCommand(Command command)
        {
            return command == Command.ToggleMagnifier || command == Command.ToggleMagnifierWithoutOperatingGuide || command == Command.ToggleMagnifierWithMinimalScreenEffects ||
                command == Command.EnableMagnifier || command == Command.EnableMagnifierWithoutOperatingGuide;
        }

        private void MagnifierPhase2_KeyUpEventHandler(object sender, EventArgs e)
        {
            UserMagnifierOpeningOrClosing = false;
            MagnifierPhase2();
            KeyUp -= MagnifierPhase2_KeyUpEventHandler;
            pbView.MouseUp -= MagnifierPhase2_KeyUpEventHandler;
            pbView.SizeChanged -= MagnifierPhase2_KeyUpEventHandler;
            foreach (Control control in Controls)
            {
                control.MouseUp -= MagnifierPhase2_KeyUpEventHandler;
            }
        }

        private void disableMagnifierForKeyboardWithOperatingGuide() => disableMagnifierForKeyboard(withOperatingGuide: true);
        private void disableMagnifierForKeyboard() => disableMagnifierForKeyboard(withOperatingGuide: false);
        private void disableMagnifierForKeyboard(bool withOperatingGuide)
        {
            var vm = ViewerMode;
            if (vm == ViewerModeEnum.Magnifier)
            {
                //if (ksViewForm.KeyboardShortcutExcutingWithKeyOrMouseDown()) UserMagnifierOpeningOrClosing = true;
                UserMagnifierOpeningOrClosing = true;
                if (withOperatingGuide) pbView_Paint_RectangleMessage = Message.MagnifierOperatingGuide.Replace("\\n", "\n");
                MagnifierPhase3(toPhase4: false);
                KeyUp += MagnifierPhase4_KeyUpEventHandler;
                MouseUp += MagnifierPhase4_KeyUpEventHandler;
                pbView.SizeChanged += MagnifierPhase4_KeyUpEventHandler;
                foreach (Control control in Controls)
                {
                    control.MouseUp += MagnifierPhase4_KeyUpEventHandler;
                }
            }
            else if(vm == ViewerModeEnum.Normal)
            {
                //if (ksViewForm.KeyboardShortcutExcutingWithKeyOrMouseDown()) UserMagnifierOpeningOrClosing = true;
                UserMagnifierOpeningOrClosing = true;
                MagnifierPhase1(null);
                MagnifierPhase2();
                if (withOperatingGuide) pbView_Paint_RectangleMessage = Message.MagnifierOperatingGuide.Replace("\\n", "\n");
                MagnifierPhase3(toPhase4: false);
                KeyUp += MagnifierPhase4_KeyUpEventHandler;
                MouseUp += MagnifierPhase4_KeyUpEventHandler;
                pbView.SizeChanged += MagnifierPhase4_KeyUpEventHandler;
                foreach (Control control in Controls)
                {
                    control.MouseUp += MagnifierPhase4_KeyUpEventHandler;
                }
            }
        }

        private void MagnifierPhase4_KeyUpEventHandler(object sender, MouseEventArgs e)
        {
            var key = ksViewForm.MouseButtonsToKeys(e.Button);
            if (key != Keys.None)
            {
                MagnifierPhase4_KeyUpEventHandler(sender, new KeyEventArgs(key));
            }
        }

        private void MagnifierPhase4_KeyUpEventHandler(object sender, KeyEventArgs e)
        {
            var actions = ksViewForm.Actions;
            if (actions == null || actions.Any(action => IsDisableMagnifierWithEffectCommand((Command)action.Key) && action.Shortcut.LastOrDefault()?.Contains(e.KeyCode) == true))
            {
                MagnifierPhase4_KeyUpEventHandler(sender, e as EventArgs);
            }
        }

        private static bool IsDisableMagnifierWithEffectCommand(Command command)
        {
            return command == Command.ToggleMagnifier || command == Command.DisableMagnifier || command == Command.DisableMagnifierWithoutOperatingGuide
                || command == Command.ToggleMagnifierWithoutOperatingGuide;
        }

        private void MagnifierPhase4_KeyUpEventHandler(object sender, EventArgs e)
        {
            UserMagnifierOpeningOrClosing = false;
            MagnifierPhase4(toPhase1: false);
            KeyUp -= MagnifierPhase4_KeyUpEventHandler;
            MouseUp -= MagnifierPhase4_KeyUpEventHandler;
            pbView.SizeChanged -= MagnifierPhase4_KeyUpEventHandler;
            foreach (Control control in Controls)
            {
                control.MouseUp -= MagnifierPhase4_KeyUpEventHandler;
            }
        }

        private void rightToLeftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangePageDirection(BindingMode.RightToLeft);
        }

        private void leftToRightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangePageDirection(BindingMode.LeftToRight);
        }

        private void singlePageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangePageDirection(BindingMode.SinglePage);
        }

        private void singlePageWithoutScalingUpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangePageDirection(BindingMode.SinglePageWithoutScalingUp);
        }

        private void forceTwoPageSpreadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToggleMinPageCountInWindow();
        }

        private void ToggleMinPageCountInWindow()
        {
            MinPageCountInWindow = 3 - MinPageCountInWindow;
            //if (MinPageCountInWindow > 1) MaxDivision = 1; // 強制見開きページの場合必要、見開き優先なら不要
            if (BindingMode == BindingMode.SinglePage || BindingMode == BindingMode.SinglePageWithoutScalingUp)
            {
                showCurrentPage();
            }
            else
            {
                Reload(force: true);
            }
        }

        private void allowPageDivisionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToggleMaxDivision();
        }

        private void ToggleMaxDivision()
        {
            MaxDivision = 3 - MaxDivision;
            //if (MaxDivision > 1) MinPageCountInWindow = 1; // 強制見開きページの場合必要、見開き優先なら不要
            if (BindingMode == BindingMode.SinglePage || BindingMode == BindingMode.SinglePageWithoutScalingUp)
            {
                showCurrentPage();
            }
            else
            {
                Reload(force: true);
            }
        }

        private void nextPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            moveToNextPage();
        }

        private void moveToNextPage()
        {
            if (ResizedImageArray == null) return;
            var count = ResizedImageArray.Length;
            if (count == 0) return;
            var nextPage = NextPage;
            if (nextPage < count && (nextPage > currentPage || currentPage == nextPage && CurrentDividedPosition >= 0 && MaxDivision > 1))
            {
                movePageNatural(1);
            }
            else if (nextPage == count && CurrentDividedPosition + 1 == getCurrentPageDivison())
            {
                ksViewForm?.InvokeCommand(ExtendedKeys.NextPageAtLastPage);
            }
        }

        private void previousPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MoveToPreviousPage();
        }

        private void MoveToPreviousPage()
        {
            if (ResizedImageArray == null) return;
            var count = ResizedImageArray.Length;
            if (count == 0) return;
            if (currentPage > 0 || currentPage == 0 && CurrentDividedPosition > 0 && MaxDivision > 1)
            {
                movePageNatural(-1);
            }
            else if (currentPage == 0 && CurrentDividedPosition == 0)
            {
                ksViewForm?.InvokeCommand(ExtendedKeys.PreviousPageAtFirstPage);
            }
        }

        private void rightPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            moveToRightPage();
        }

        private void moveToRightPage()
        {
            if (BindingMode != BindingMode.RightToLeft)
            {
                moveToNextPage();
            }
            else
            {
                MoveToPreviousPage();
            }
        }

        private void leftPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            moveToLeftPage();
        }

        private void moveToLeftPage()
        {
            if(BindingMode != BindingMode.RightToLeft)
            {
                MoveToPreviousPage();
            }
            else
            {
                moveToNextPage();
            }
            
        }

        private void moveForward1PageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            movePageToMinimulForward();
            //movePage(1);
        }

        private void moveBack1PageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            movePageToMinimulBack();
            //movePage(-1);
        }

        private void moveRight1PageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            movePageToMinimulRight();
        }

        private void movePageToMinimulRight()
        {
            if (BindingMode != BindingMode.RightToLeft) movePageToMinimulForward(); else movePageToMinimulBack();
        }

        private void moveLeft1PageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            movePageToMinimulLeft();
        }

        private void movePageToMinimulLeft()
        {
            if (BindingMode != BindingMode.RightToLeft) movePageToMinimulBack(); else movePageToMinimulForward();
        }

        private void percentage000YoolStripMenuItem_Click(object sender, EventArgs e) { movePageByRatio(0.0); }
        private void percentage010YoolStripMenuItem_Click(object sender, EventArgs e) { movePageByRatio(0.1); }
        private void percentage020YoolStripMenuItem_Click(object sender, EventArgs e) { movePageByRatio(0.2); }
        private void percentage030YoolStripMenuItem_Click(object sender, EventArgs e) { movePageByRatio(0.3); }
        private void percentage040YoolStripMenuItem_Click(object sender, EventArgs e) { movePageByRatio(0.4); }
        private void percentage050YoolStripMenuItem_Click(object sender, EventArgs e) { movePageByRatio(0.5); }
        private void percentage060YoolStripMenuItem_Click(object sender, EventArgs e) { movePageByRatio(0.6); }
        private void percentage070YoolStripMenuItem_Click(object sender, EventArgs e) { movePageByRatio(0.7); }
        private void percentage080YoolStripMenuItem_Click(object sender, EventArgs e) { movePageByRatio(0.8); }
        private void percentage090YoolStripMenuItem_Click(object sender, EventArgs e) { movePageByRatio(0.9); }
        private void percentage100YoolStripMenuItem_Click(object sender, EventArgs e) { movePageByRatio(1.0); }



        private double getRatioByPage(int p = -1)
        {
            if (p < 0) p = currentPage;
            if (ResizedImageArray == null) return 0;
            var pageCount = ResizedImageArray.Length;
            return Math.Max(0, Math.Min(1, (double)p / pageCount));
        }

        private int getPageByRatio(double r)
        {
            if (ResizedImageArray == null) return -1;
            var pageCount = ResizedImageArray.Length;
            return Math.Max(0, Math.Min(pageCount - 1, (int)(r * pageCount)));
        }

        private void movePageByRatio000() { movePageByRatio(0.0); }
        private void movePageByRatio010() { movePageByRatio(0.1); }
        private void movePageByRatio020() { movePageByRatio(0.2); }
        private void movePageByRatio030() { movePageByRatio(0.3); }
        private void movePageByRatio040() { movePageByRatio(0.4); }
        private void movePageByRatio050() { movePageByRatio(0.5); }
        private void movePageByRatio060() { movePageByRatio(0.6); }
        private void movePageByRatio070() { movePageByRatio(0.7); }
        private void movePageByRatio080() { movePageByRatio(0.8); }
        private void movePageByRatio090() { movePageByRatio(0.9); }
        private void movePageByRatio100() { movePageByRatio(1.0); }

        private void movePageByRatio(double r)
        {
            var nextPage = getPageByRatio(r);
            if (nextPage < 0) return;
            currentPage = nextPage;
            if (nextPage == 0)
            {
                // seemToBook は廃止
                onePageModeForNext = CoverBindingMode == CoverBindingMode.ForceSingle;
                /*
                if (requestedCoverBindingMode == null)
                {
                    var ea = EntryArray;
                    if (ea != null)
                    {
                        var ld = loader;
                        if (ld != null)
                        {
                            if (seemToBeBook(ld.Type, ea))
                            {
                                onePageModeForNext = true;
                            }
                        }
                    }
                }
                else
                {
                    onePageModeForNext = requestedCoverBindingMode == CoverBindingMode.ForceSingle;
                }
                */
            }
            showCurrentPage();
            mtbPage.Value = currentPage;
        }

        private bool isMovablePageByRatio(double r)
        {
            var nextPage = getPageByRatio(r);
            return nextPage >= 0 && nextPage != currentPage;
        }


        private enum Position { Left, Right, Center, Out }
        private Position GetPositionOnView(Point? clientPoint = null)
        {
            Point exactClientPoint;
            if(clientPoint == null || clientPoint == Point.Empty)
            {
                exactClientPoint = pbView.PointToClient(Cursor.Position);
            }
            else
            {
                exactClientPoint = (Point)clientPoint;
            }
            var Width = pbView.Width;
            var Height = pbView.Height;
            var rectangle0 = new Rectangle(0, 0, Width, Height);

            if (rectangle0.Contains(exactClientPoint))
            {
                if (5 * exactClientPoint.X < 2 * Width)
                {
                    return Position.Left;
                }
                else if (5 * exactClientPoint.X >= 3 * Width)
                {
                    return Position.Right;
                }
                else
                {
                    return Position.Center;  
                }
            }
            else
            {
                return Position.Out;
            }
        }

        private void ViewerForm_Activated(object sender, EventArgs e)
        {
            //tmMemoryReducer.Enabled = false;
            SetMemoryUBound();
            ReduceUsingMemory(usedMemoryUBound);
            bmwLoadEachPage.ThreadCount = 1;
        }
        
        /*
        private void ViewerForm_HelpButtonClicked(object sender, CancelEventArgs e)
        {
            SetMemoryUBound();
            ReduceUsingMemory(usedMemoryUBound);
        }
        */

        private void mtbPage_MouseEnter(object sender, EventArgs e)
        {
            //mtbPage.Focus();
            focusIfRightness(mtbPage);
        }

        private void statusStrip_MouseEnter(object sender, EventArgs e)
        {
            focusIfRightness(mtbPage); // 自身ではなく mtbPage へフォーカス
        }

        private void focusIfRightness(Control control)
        {
            //if (!currentImageLongPathToolStripTextBox.Focused) control.Focus();
        }
        
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        private void fixDropDownLocation(object toolStripMenuItem)
        {
            if (!FullScreen) return;

            var tsmi = toolStripMenuItem as ToolStripMenuItem;
            if (tsmi == null) return;

            var bounds = tsmi.Bounds;
            var point = tsmi.Owner.PointToScreen(new Point(bounds.Left, bounds.Bottom - 1)); // Windows10(2016/09/01) 画面の解像度 100%, 200% 共 -1 で良いと確認

            var dropDown = tsmi.DropDown;

            var screenSize = Screen.FromControl(this).Bounds.Size;

            if (point.X + dropDown.Width <= screenSize.Width && point.Y + dropDown.Height <= screenSize.Height)
            {
                MoveWindow(dropDown.Handle, point.X, point.Y, dropDown.Width, dropDown.Height, true);
            }
        }
        
        private void mouseGestureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.OpenMouseGestureSetting(Icon, mgView, getMouseGestureSettingTemplate(CommandSender.MouseGesture), this, widthZoom: 1.4);
        }

        private void keyboardShortcutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(Program.OpenKeyboardShortcutSetting(Icon, ksViewForm, getMouseGestureSettingTemplate(CommandSender.KeyboardShortcut), ContinuousExecutionCommands.Select(enm => (int)enm).ToArray(),
                ViewerFormConfig.DefaultKeyboardShortcutCommands.Select(d => Tuple.Create(d.GetShortcut().Select(s => new HashSet<Keys>(s)).ToArray(), (int)d.Command)).ToArray(),
                useLButton: true, useMButton: true, useRButton: true, useX1Button: true, useX2Button:true,
                useLButtonWarningMessage: Message.WhenClickOnAndClickConflictTheFormerTakesPrecedence,
                useMButtonWarningMessage: null, useRButtonWarningMessage: null, owner: this))
            {
                setShortcutKeyDisplayStrings(setKeyNames: false);
            }
        }
        
        private enum CommandSender { MouseGesture = 1, KeyboardShortcut = 2 };
        public enum Command { NextPage, PreviousPage, MoveForwardOnePage, MoveBackOnePage, RightPage, LeftPage,
            MoveTo0Percent, MoveTo10Percent, MoveTo20Percent, MoveTo30Percent, MoveTo40Percent, MoveTo50Percent, MoveTo60Percent, MoveTo70Percent, MoveTo80Percent, MoveTo90Percent, MoveTo100Percent,
            MoveRightOnePage, MoveLeftOnePage, OpenNext, OpenPrevious, OpenRight, OpenLeft,
            EnableMagnifier, EnableMagnifierWithoutOperatingGuide, DisableMagnifier, DisableMagnifierWithoutOperatingGuide, DisableMagnifierWithoutScreenEffects, ToggleMagnifier, ToggleMagnifierWithoutOperatingGuide, ToggleMagnifierWithMinimalScreenEffects,
            MagnifierAutoScrollForward, MagnifierAutoScrollBack, MagnifierAutoScrollForwardWithoutOverwrap, MagnifierAutoScrollBackWithoutOverwrap,
            MoveForwardOrMagnifierAutoScroll, MoveBackOrMagnifierAutoScroll, MoveForwardOrMagnifierAutoScrollWithoutOverwrap, MoveBackOrMagnifierAutoScrollWithoutOverwrap,
            MagnifierAutoScrollRight, MagnifierAutoScrollLeft, MagnifierAutoScrollRightWithoutOverwrap, MagnifierAutoScrollLeftWithoutOverwrap,
            MoveRightOrMagnifierAutoScroll, MoveLeftOrMagnifierAutoScroll, MoveRightOrMagnifierAutoScrollWithoutOverwrap, MoveLeftOrMagnifierAutoScrollWithoutOverwrap,
            LeftToRight, RightToLeft, SinglePage, SinglePageWithoutScalingUp, TogglePageSequence,
            ToggleForceFirstPageToBeSingle, ToggleMinPageCountInWindow, ToggleMaxDivision,
            ToggleAlwaysHideUI, ToggleSeekBarMode, SelectBackgroundColor,
            ToggleRotateLeft, ToggleRotateRight, CancelRotationSetting, /*ToggleMoireRemover,*/ ToggleToneCurve,
            StartSlideshow, StopSlideshow, ToggleSlideshow,
            OpenFile, OpenThumbnailWindow, SwitchToThumbnailWindow, CloneWindow,
            ReadOnMemoryModeNone, ReadOnMemoryModeExceptLookAhead, ReadOnMemoryModeAlways,
            ArchivesInArchiveModeIgnore, ArchivesInArchiveModeUntilFound2Level, ArchivesInArchiveModeAlways1Level,
            FullScreen, Window, ToggleFullScreenMode, Exit,
            ScreenEffectAtRightSide, ScreenEffectAtLeftSide, ScreenEffectAtNextPageSide, ScreenEffectAtPreviousPageSide,
            StartMenu, MoveMenu, ViewMenu, SlideshowMenu, VirtualFolderMenu, EditMenu, OthersMenu,
            NOOP
        }
        private static readonly Command[] ContinuousExecutionCommands = new Command[] { Command.NextPage, Command.PreviousPage, Command.MoveForwardOnePage,
            Command.MoveBackOnePage, Command.RightPage,Command.LeftPage, Command.MoveRightOnePage,  Command.MoveLeftOnePage,
            Command.MagnifierAutoScrollForward, Command.MagnifierAutoScrollBack, Command.MagnifierAutoScrollForwardWithoutOverwrap, Command.MagnifierAutoScrollBackWithoutOverwrap,
            Command.MoveForwardOrMagnifierAutoScroll, Command.MoveBackOrMagnifierAutoScroll, Command.MoveForwardOrMagnifierAutoScrollWithoutOverwrap, Command.MoveBackOrMagnifierAutoScrollWithoutOverwrap,
            Command.MagnifierAutoScrollRight, Command.MagnifierAutoScrollLeft, Command.MagnifierAutoScrollRightWithoutOverwrap, Command.MagnifierAutoScrollLeftWithoutOverwrap,
            Command.MoveRightOrMagnifierAutoScroll, Command.MoveLeftOrMagnifierAutoScroll, Command.MoveRightOrMagnifierAutoScrollWithoutOverwrap, Command.MoveLeftOrMagnifierAutoScrollWithoutOverwrap
        };
        public class MouseGestureCommand { public MouseGestureDirection[] Gesture; public Command Command; }
        public class KeyboardShortcutCommand
        {
            public Keys[][] Shortcut;
            public Command Command;
            public KeyboardShortcutCommand() { } // シリアライザー用
            public KeyboardShortcutCommand(Command command, params System.Windows.Forms.Keys[] shortcut)
            {
                Shortcut = (from x in KeyboardShortcutAction.SimpleShortcutToFullShortcut(shortcut) select x.Select(k => (Keys)k).ToArray()).ToArray();
                Command = command;
            }
            public KeyboardShortcutCommand(KeyboardShortcutAction action)
            {
                Shortcut = (from x in action.Shortcut select x.Select(k => (Keys)k).ToArray()).ToArray();
                Command = (Command)action.Key;
            }
            public System.Windows.Forms.Keys[][] GetShortcut()
            {
                if (Shortcut == null) return null;
                return Shortcut.Select(ks => ks.Select(k => (System.Windows.Forms.Keys)k).ToArray()).ToArray();
            }
            
            // KeyboardShortcutSetting に似た役割の equalsCommands があるが、あちらは Hashset<Keys>[] を比較する
            private static bool ShortcutEquals(Keys[][] a, Keys[][] b)
            {
                if (a == null && b == null) return true;
                if (a == null || b == null) return false;
                var length = a.Length;
                if (length != b.Length) return false;
                for(var i = 0; i < length; i++)
                {
                    var ai = a[i];
                    var bi = b[i];
                    if (ai == null && bi == null) continue;
                    if (ai == null || bi == null) return false;
                    if (!((new HashSet<Keys>(ai)).SetEquals(bi))) return false;
                }
                return true;
            }
            /*
            public bool ShortcutEquals(KeyboardShortcutCommand other)
            {
                return ShortcutEquals(Shortcut, other.Shortcut);
            }
            */
            public static bool ShortcutEquals(Keys[][] a, Keys b)
            {
                if (a == null || a.Length != 1) return false;
                var a0 = a[0];
                return a0 != null && a0.Length == 1 && a0[0] == b;
            }

            public static bool ShortcutCompetes(Keys[][] a, Keys[][] b)
            {
                if (ShortcutEquals(a, b)) return true;
                
                if (ShortcutEquals(a, Keys.LButton))
                {
                    a = b;
                }
                else if (!ShortcutEquals(b, Keys.LButton)) return false;
                if (a == null || a.Length != 1) return false;
                var a0 = a[0];
                return a0 != null && a0.Length == 1 && ExtendedKeys.IsLButton((System.Windows.Forms.Keys)a0[0]);
            }
            
            /*
            private static bool ShortcutIntersect(Keys[][] a, Keys[][] b)
            {
                return a != null && a.Any(ai => ai != null && ai.Any(ak => b != null && b.Any(bi => bi != null && bi.Any(bk => ak == bk || ExtendedKeys.IsLButton((System.Windows.Forms.Keys)ak) && ExtendedKeys.IsLButton((System.Windows.Forms.Keys)bk)))));
            }*/

            public bool ShortcutCompetes(KeyboardShortcutCommand other)
            {
                return ShortcutCompetes(Shortcut, other.Shortcut);
            }

            public bool ShortcutEquals(KeyboardShortcutCommand other)
            {
                return ShortcutEquals(Shortcut, other.Shortcut);
            }

            private static HashSet<System.Windows.Forms.Keys>[] Convert(Keys[][] x)
            {
                return x?.Select(xi => xi == null ? new HashSet<System.Windows.Forms.Keys>() : new HashSet<System.Windows.Forms.Keys>(xi.Select(xk => (System.Windows.Forms.Keys)xk))).ToArray();
            }

            public bool ShortcutWeaklyCompetes(KeyboardShortcutCommand other)
            {
                return KeyboardShortcutSettingForm.WeaklyCompetesShortcut(Convert(Shortcut), Convert(other.Shortcut));
            }

            /*
            public bool ShortcutIntersect(KeyboardShortcutCommand other)
            {
                return ShortcutIntersect(Shortcut, other.Shortcut);
            }
            */

            public enum Keys
            {
                None = 0,
                LButton = 1,
                RButton = 2,
                Cancel = 3,
                MButton = 4,
                XButton1 = 5,
                XButton2 = 6,
                Back = 8,
                Tab = 9,
                LineFeed = 10,
                Clear = 12,
                Enter = 13,
                Return = 13,
                ShiftKey = 16,
                ControlKey = 17,
                Menu = 18,
                Pause = 19,
                CapsLock = 20,
                Capital = 20,
                HangulMode = 21,
                HanguelMode = 21,
                KanaMode = 21,
                JunjaMode = 23,
                FinalMode = 24,
                KanjiMode = 25,
                HanjaMode = 25,
                Escape = 27,
                IMEConvert = 28,
                IMENonconvert = 29,
                IMEAccept = 30,
                IMEAceept = 30,
                IMEModeChange = 31,
                Space = 32,
                Prior = 33,
                PageUp = 33,
                PageDown = 34,
                Next = 34,
                End = 35,
                Home = 36,
                Left = 37,
                Up = 38,
                Right = 39,
                Down = 40,
                Select = 41,
                Print = 42,
                Execute = 43,
                Snapshot = 44,
                PrintScreen = 44,
                Insert = 45,
                Delete = 46,
                Help = 47,
                D0 = 48,
                D1 = 49,
                D2 = 50,
                D3 = 51,
                D4 = 52,
                D5 = 53,
                D6 = 54,
                D7 = 55,
                D8 = 56,
                D9 = 57,
                A = 65,
                B = 66,
                C = 67,
                D = 68,
                E = 69,
                F = 70,
                G = 71,
                H = 72,
                I = 73,
                J = 74,
                K = 75,
                L = 76,
                M = 77,
                N = 78,
                O = 79,
                P = 80,
                Q = 81,
                R = 82,
                S = 83,
                T = 84,
                U = 85,
                V = 86,
                W = 87,
                X = 88,
                Y = 89,
                Z = 90,
                LWin = 91,
                RWin = 92,
                Apps = 93,
                Sleep = 95,
                NumPad0 = 96,
                NumPad1 = 97,
                NumPad2 = 98,
                NumPad3 = 99,
                NumPad4 = 100,
                NumPad5 = 101,
                NumPad6 = 102,
                NumPad7 = 103,
                NumPad8 = 104,
                NumPad9 = 105,
                Multiply = 106,
                Add = 107,
                Separator = 108,
                Subtract = 109,
                Decimal = 110,
                Divide = 111,
                F1 = 112,
                F2 = 113,
                F3 = 114,
                F4 = 115,
                F5 = 116,
                F6 = 117,
                F7 = 118,
                F8 = 119,
                F9 = 120,
                F10 = 121,
                F11 = 122,
                F12 = 123,
                F13 = 124,
                F14 = 125,
                F15 = 126,
                F16 = 127,
                F17 = 128,
                F18 = 129,
                F19 = 130,
                F20 = 131,
                F21 = 132,
                F22 = 133,
                F23 = 134,
                F24 = 135,
                NumLock = 144,
                Scroll = 145,
                LShiftKey = 160,
                RShiftKey = 161,
                LControlKey = 162,
                RControlKey = 163,
                LMenu = 164,
                RMenu = 165,
                BrowserBack = 166,
                BrowserForward = 167,
                BrowserRefresh = 168,
                BrowserStop = 169,
                BrowserSearch = 170,
                BrowserFavorites = 171,
                BrowserHome = 172,
                VolumeMute = 173,
                VolumeDown = 174,
                VolumeUp = 175,
                MediaNextTrack = 176,
                MediaPreviousTrack = 177,
                MediaStop = 178,
                MediaPlayPause = 179,
                LaunchMail = 180,
                SelectMedia = 181,
                LaunchApplication1 = 182,
                LaunchApplication2 = 183,
                OemSemicolon = 186,
                Oem1 = 186,
                Oemplus = 187,
                Oemcomma = 188,
                OemMinus = 189,
                OemPeriod = 190,
                Oem2 = 191,
                OemQuestion = 191,
                Oem3 = 192,
                Oemtilde = 192,
                Oem4 = 219,
                OemOpenBrackets = 219,
                OemPipe = 220,
                Oem5 = 220,
                OemCloseBrackets = 221,
                Oem6 = 221,
                OemQuotes = 222,
                Oem7 = 222,
                Oem8 = 223,
                Oem102 = 226,
                OemBackslash = 226,
                ProcessKey = 229,
                Packet = 231,
                Attn = 246,
                Crsel = 247,
                Exsel = 248,
                EraseEof = 249,
                Play = 250,
                Zoom = 251,
                NoName = 252,
                Pa1 = 253,
                OemClear = 254,
                KeyCode = 65535,
                Shift = 65536,
                Control = 131072,
                Alt = 262144,
                Modifiers = -65536,

                WheelUp = ExtendedKeys.WheelUp,
                WheelDown = ExtendedKeys.WheelDown,

                NextPageAtLastPage = ExtendedKeys.NextPageAtLastPage,
                PreviousPageAtFirstPage = ExtendedKeys.PreviousPageAtFirstPage,

                LButtonOnRight = ExtendedKeys.LButtonOnRight,
                LButtonOnTopRight = ExtendedKeys.LButtonOnTopRight,
                LButtonOnTop = ExtendedKeys.LButtonOnTop,
                LButtonOnTopLeft = ExtendedKeys.LButtonOnTopLeft,
                LButtonOnLeft = ExtendedKeys.LButtonOnLeft,
                LButtonOnBottomLeft = ExtendedKeys.LButtonOnBottomLeft,
                LButtonOnBottom = ExtendedKeys.LButtonOnBottom,
                LButtonOnBottomRight = ExtendedKeys.LButtonOnBottomRight,
                LButtonOnCenter = ExtendedKeys.LButtonOnCenter,

                LButtonDouble = ExtendedKeys.LButtonDouble,
            }
        }
        private MouseGestureSettingTemplate[] getMouseGestureSettingTemplate(CommandSender commandSender)
        {
            var result = new Tuple<MouseGestureSettingTemplate, CommandSender>[]
            {
                //Tuple.Create(new MouseGestureSettingTemplate( (int)Command.DefaultClickBehavior, Message.DefaultClickBehavior,
                //   withHideCursor(defaultClickBehavior, commandSender)), CommandSender.SingleLeftClick),
                
                Tuple.Create(new MouseGestureSettingTemplate( (int)Command.NextPage, Message.NextPage,
                   withHideCursor(moveToNextPage, commandSender)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(new MouseGestureSettingTemplate( (int)Command.PreviousPage, Message.PreviousPage,
                   withHideCursor(MoveToPreviousPage, commandSender)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(new MouseGestureSettingTemplate( (int)Command.MoveForwardOnePage, Message.MoveForwardOnePage,
                   withHideCursor(movePageToMinimulForward, commandSender)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create( new MouseGestureSettingTemplate( (int)Command.MoveBackOnePage, Message.MoveBackOnePage,
                   withHideCursor(movePageToMinimulBack, commandSender)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.RightPage, Message.RightPage,
                   withHideCursor(moveToRightPage, commandSender)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create( new MouseGestureSettingTemplate( (int)Command.LeftPage, Message.LeftPage,
                   withHideCursor(moveToLeftPage, commandSender)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(   new MouseGestureSettingTemplate( (int)Command.MoveRightOnePage, Message.MoveRightOnePage,
                   withHideCursor(movePageToMinimulRight, commandSender)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.MoveLeftOnePage, Message.MoveLeftOnePage,
                   withHideCursor(movePageToMinimulLeft, commandSender)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut), // コマンド実行時点での PageLeftToRight が必要なのでデリゲート自体を PageLeftToRight で選ぶのは誤り

                Tuple.Create( new MouseGestureSettingTemplate( (int)Command.MoveTo0Percent, Message.PositionRatio + " > 0 %",
                   withHideCursor(movePageByRatio000, commandSender)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create( new MouseGestureSettingTemplate( (int)Command.MoveTo10Percent, Message.PositionRatio + " > 10 %",
                   withHideCursor(movePageByRatio010, commandSender)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create( new MouseGestureSettingTemplate( (int)Command.MoveTo20Percent, Message.PositionRatio + " > 20 %",
                   withHideCursor(movePageByRatio020, commandSender)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create( new MouseGestureSettingTemplate( (int)Command.MoveTo30Percent, Message.PositionRatio + " > 30 %",
                   withHideCursor(movePageByRatio030, commandSender)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create( new MouseGestureSettingTemplate( (int)Command.MoveTo40Percent, Message.PositionRatio + " > 40 %",
                   withHideCursor(movePageByRatio040, commandSender)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create( new MouseGestureSettingTemplate( (int)Command.MoveTo50Percent, Message.PositionRatio + " > 50 %",
                   withHideCursor(movePageByRatio050, commandSender)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create( new MouseGestureSettingTemplate( (int)Command.MoveTo60Percent, Message.PositionRatio + " > 60 %",
                   withHideCursor(movePageByRatio060, commandSender)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create( new MouseGestureSettingTemplate( (int)Command.MoveTo70Percent, Message.PositionRatio + " > 70 %",
                   withHideCursor(movePageByRatio070, commandSender)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create( new MouseGestureSettingTemplate( (int)Command.MoveTo80Percent, Message.PositionRatio + " > 80 %",
                   withHideCursor(movePageByRatio080, commandSender)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create( new MouseGestureSettingTemplate( (int)Command.MoveTo90Percent, Message.PositionRatio + " > 90 %",
                   withHideCursor(movePageByRatio090, commandSender)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create( new MouseGestureSettingTemplate( (int)Command.MoveTo100Percent, Message.PositionRatio + " > 100 %",
                   withHideCursor(movePageByRatio100, commandSender)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),


                Tuple.Create( new MouseGestureSettingTemplate( (int)Command.OpenNext, Message.OpenNext,
                   toAction(OpenNext)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create( new MouseGestureSettingTemplate( (int)Command.OpenPrevious, Message.OpenPrevious,
                   toAction(OpenPrevious)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.OpenRight, Message.OpenRight,
                   toAction(OpenRight)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.OpenLeft, Message.OpenLeft,
                   toAction(OpenLeft)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),

                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.EnableMagnifier, Message.EnableMagnifier,
                   enableMagnifier), CommandSender.MouseGesture),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.DisableMagnifier, Message.DisableMagnifier,
                   disableMagnifier), CommandSender.MouseGesture),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.ToggleMagnifier, Message.ToggleMagnifier,
                   ToggleMagnifierWithoutScreenEffects), CommandSender.MouseGesture),

                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.EnableMagnifier, Message.EnableMagnifier,
                   enableMagnifierForKeyboardWithOperatingGuide), CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.EnableMagnifierWithoutOperatingGuide, Message.EnableMagnifierWithoutOperatingGuide,
                   enableMagnifierForKeyboard), CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.DisableMagnifier, Message.DisableMagnifier,
                   disableMagnifierForKeyboardWithOperatingGuide), CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.DisableMagnifierWithoutOperatingGuide, Message.DisableMagnifierWithoutOperatingGuide,
                   disableMagnifierForKeyboard), CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.DisableMagnifierWithoutScreenEffects, Message.DisableMagnifierWithoutScreenEffects,
                   disableMagnifier), CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.ToggleMagnifier, Message.ToggleMagnifier,
                   toggleMagnifierForKeyboardWithOperatingGuide), CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.ToggleMagnifierWithoutOperatingGuide, Message.ToggleMagnifierWithoutOperatingGuide,
                   toggleMagnifierForKeyboard), CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.ToggleMagnifierWithMinimalScreenEffects, Message.ToggleMagnifierWithMinimalScreenEffects,
                   toggleMagnifierForKeyboardWithMinimalScreenEffects), CommandSender.KeyboardShortcut),

                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.MagnifierAutoScrollForward, Message.AutoForwardScroll,
                   MagnifierAutoScrollForwardWith20PercentOverwrap), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.MagnifierAutoScrollBack, Message.AutoBackScroll,
                   MagnifierAutoScrollBackWith20PercentOverwrap), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.MagnifierAutoScrollForwardWithoutOverwrap, Message.AutoForwardScrollWithoutOverwrap,
                   MagnifierAutoScrollForwardWith0PercentOverwrap), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.MagnifierAutoScrollBackWithoutOverwrap, Message.AutoBackScrollWithoutOverwrap,
                   MagnifierAutoScrollBackWith0PercentOverwrap), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.MoveForwardOrMagnifierAutoScroll, Message.NextPageOrAutoForwardScroll,
                   MoveForwardOrMagnifierAutoScrollWith20PercentOverwrap), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.MoveBackOrMagnifierAutoScroll, Message.PreviousPageOrAutoBackScroll,
                   MoveBackOrMagnifierAutoScrollWith20PercentOverwrap), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.MoveForwardOrMagnifierAutoScrollWithoutOverwrap, Message.NextPageOrAutoForwardScrollWithoutOverwrap,
                   MoveForwardOrMagnifierAutoScrollWith0PercentOverwrap), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.MoveBackOrMagnifierAutoScrollWithoutOverwrap, Message.PreviousPageOrAutoBackScrollWithoutOverwrap,
                   MoveBackOrMagnifierAutoScrollWith0PercentOverwrap), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),

                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.MagnifierAutoScrollRight, Message.AutoRightScroll,
                   MagnifierAutoScrollRightWith20PercentOverwrap), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.MagnifierAutoScrollLeft, Message.AutoLeftScroll,
                   MagnifierAutoScrollLeftWith20PercentOverwrap), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.MagnifierAutoScrollRightWithoutOverwrap, Message.AutoRightScrollWithoutOverwrap,
                   MagnifierAutoScrollRightWith0PercentOverwrap), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.MagnifierAutoScrollLeftWithoutOverwrap, Message.AutoLeftScrollWithoutOverwrap,
                   MagnifierAutoScrollLeftWith0PercentOverwrap), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.MoveRightOrMagnifierAutoScroll, Message.RightPageOrAutoRightScroll,
                   MoveRightOrMagnifierAutoScrollWith20PercentOverwrap), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.MoveLeftOrMagnifierAutoScroll, Message.LeftPageOrAutoLeftScroll,
                   MoveLeftOrMagnifierAutoScrollWith20PercentOverwrap), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.MoveRightOrMagnifierAutoScrollWithoutOverwrap, Message.RightPageOrAutoRightScrollWithoutOverwrap,
                   MoveRightOrMagnifierAutoScrollWith0PercentOverwrap), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.MoveLeftOrMagnifierAutoScrollWithoutOverwrap, Message.LeftPageOrAutoLeftScrollWithoutOverwrap,
                   MoveLeftOrMagnifierAutoScrollWith0PercentOverwrap), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),

                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.LeftToRight, Message.LeftToRight,
                   () => ChangePageDirection(BindingMode.LeftToRight)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.RightToLeft, Message.RightToLeft,
                   () => ChangePageDirection(BindingMode.RightToLeft)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.SinglePage, Message.SinglePage,
                   () => ChangePageDirection(BindingMode.SinglePage)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.SinglePageWithoutScalingUp, Message.SinglePageWithoutScalingUp,
                   () => ChangePageDirection(BindingMode.SinglePageWithoutScalingUp)), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.TogglePageSequence, Message.TogglePageSequence,
                   togglePageSequence), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),

                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.ToggleForceFirstPageToBeSingle, Message.ForceFirstPageToBeSingle,
                   ToggleForceFirstPageToBeSingle), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                

                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.ToggleMinPageCountInWindow, Message.PrioritizePageSpread, //Message.ForcePageSpread,
                   ToggleMinPageCountInWindow), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.ToggleMaxDivision, Message.AllowPageDivision,
                   ToggleMaxDivision), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.ToggleAlwaysHideUI, Message.AlwaysAutomaticallyHideUI,
                   toggleAlwaysHideUI), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.ToggleSeekBarMode, Message.OrientSeekBarDirectionToPageSequence,
                   ToggleSeekBarMode), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.SelectBackgroundColor, Message.SelectBackgroundColor,
                   SelectBackgroundColor), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),

                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.ToggleRotateLeft, Message.RotateLeft,
                   toggleRotateLeft), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.ToggleRotateRight, Message.RotateRight,
                   toggleRotateRight), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.CancelRotationSetting, Message.CancelRotationSetting,
                   cancelRotationSetting), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),

                //Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.ToggleMoireRemover, Message.RemoveMoire,
                //   toggleMoireRemover), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.ToggleToneCurve, Message.ToneCurve,
                   togglePseudoColoring), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),

                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.StartSlideshow, Message.StartSlideshow,
                   slideshowStart), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.StopSlideshow, Message.StopSlideshow,
                   slideshowStop), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.ToggleSlideshow, Message.ToggleSlideshow,
                   slideshowToggle), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.OpenFile, Message.OpenFile,
                   openFileWithDialog), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.OpenThumbnailWindow, Message.OpenThumbnailWindow,
                   openThumbnailWindow), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.SwitchToThumbnailWindow, Message.SwitchToThumbnailWindow,
                   switchToThumbnailWindow), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),

                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.CloneWindow, Message.CloneWindow,
                   cloneWindow), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),

                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.ReadOnMemoryModeNone, Message.LoadWholeArchiveIntoMemory + " > " + Message.NoneRecommended,
                   SetReadOnMemoryModeNone), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.ReadOnMemoryModeExceptLookAhead, Message.LoadWholeArchiveIntoMemory + " > " + Message.ExceptReadAheadProcess,
                   SetReadOnMemoryModeExceptLookAhead), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.ReadOnMemoryModeAlways, Message.LoadWholeArchiveIntoMemory + " > " + Message.Always,
                   SetReadOnMemoryModeAlways), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),

                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.ArchivesInArchiveModeIgnore, Message.LoadArchivesInArchive + " > " + Message.NoneRecommended,
                   SetArchivesInArchiveModeIgnore), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.ArchivesInArchiveModeUntilFound2Level, Message.LoadArchivesInArchive + " > " + Message.TwoLevelUntilFound,
                   SetArchivesInArchiveModeUntilFound2Level), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.ArchivesInArchiveModeAlways1Level, Message.LoadArchivesInArchive + " > " + Message.OneLevelCompletelyNotRecommended,
                   SetArchivesInArchiveModeAlways1Level), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),

                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.FullScreen, Message.Fullscreen,
                   fullScreenModeChangeToFullScreen), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.Window, Message.Window,
                   fullScreenModeChangeToWindow), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(   new MouseGestureSettingTemplate( (int)Command.ToggleFullScreenMode, Message.ToggleFullscreenMode,
                   toggleFullScreenMode), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(   new MouseGestureSettingTemplate( (int)Command.Exit, Message.Exit,
                   Close), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),

                Tuple.Create(new MouseGestureSettingTemplate( (int)Command.ScreenEffectAtRightSide, Message.ScreenEffectAtRightSide,
                   drawRightSideLine), CommandSender.KeyboardShortcut),
                Tuple.Create(new MouseGestureSettingTemplate( (int)Command.ScreenEffectAtLeftSide, Message.ScreenEffectAtLeftSide,
                   drawLeftSideLine), CommandSender.KeyboardShortcut),
                Tuple.Create(new MouseGestureSettingTemplate( (int)Command.ScreenEffectAtNextPageSide, Message.ScreenEffectAtNextPageSide,
                   drawNextPageSideLine), CommandSender.KeyboardShortcut),
                Tuple.Create(new MouseGestureSettingTemplate( (int)Command.ScreenEffectAtPreviousPageSide, Message.ScreenEffectAtPreviousPageSide,
                   drawPrevPageSideLine), CommandSender.KeyboardShortcut),
                
                Tuple.Create(new MouseGestureSettingTemplate( (int)Command.StartMenu, Message.StartMenu,
                   openStartMenu), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(new MouseGestureSettingTemplate( (int)Command.MoveMenu, Message.MoveMenu,
                   openMoveMenu), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(new MouseGestureSettingTemplate( (int)Command.ViewMenu, Message.ViewMenu,
                   openViewMenu), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.SlideshowMenu, Message.SlideshowMenu,
                   openSlideshowMenu), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.VirtualFolderMenu, Message.VirtualFolderMenu,
                   openVirtualFolderMenu), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.EditMenu, Message.EditMenu,
                   openEditMenu), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),
                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.OthersMenu, Message.OthersMenu,
                   openOthersMenu), CommandSender.MouseGesture | CommandSender.KeyboardShortcut),

                Tuple.Create(  new MouseGestureSettingTemplate( (int)Command.NOOP, Message.NoOperation,
                   NOOP), CommandSender.KeyboardShortcut),

            };

            return (from t in result where (commandSender & t.Item2) != 0 select t.Item1).ToArray();
        }

        private void NOOP() { }

        private Action toAction(Func<OpenResult> func) { return () => func(); }
        private Action toAction(Func<Action, OpenResult> func) { return () => func(null); }

        private Action withHideCursor(Action baseAction, CommandSender sender)
        {
            if ((sender & CommandSender.KeyboardShortcut) != 0)
            {
                return () =>
                {
                    baseAction();
                    if (ksViewForm.PureKeyboardShortcutExcuting())
                    {
                        var p = Cursor.Position;
                        if (!pbView.ClientRectangle.Contains(pbView.PointToClient(p))) return;
                        if (FullScreen)
                        {
                            var b = menuStrip.RectangleToScreen(menuStrip.ClientRectangle);
                            if (p.Y < b.Bottom) return;
                            var t = pnlSeekbar.RectangleToScreen(pnlSeekbar.ClientRectangle);
                            if (p.Y >= t.Top) return;
                        }
                        if (MouseInPbView)
                        {
                            Program.HideCursor();
                        }
                    }
                };
            }
            else return baseAction;
        }

        void drawLeftSideLine() { drawSideLine(left: true); }
        void drawRightSideLine() { drawSideLine(left: false); }
        void drawNextPageSideLine() { drawSideLine(left: BindingMode == BindingMode.RightToLeft); }
        void drawPrevPageSideLine() { drawSideLine(left: BindingMode != BindingMode.RightToLeft); }
        void drawSideLine(bool left) { drawSideLine(left, Program.DpiScaling(5), Color.Red, TimeSpan.FromSeconds(0.5)); }

        DateTime drowSideLine_DrawStopTime = DateTime.MinValue;
        bool drowSideLine_DrawLeft;
        int drowSideLine_DrawWidth;
        Color drowSideLine_DrawColor;
        bool drowSideLine_Draw;
        async void drawSideLine(bool left, int width, Color color, TimeSpan time)
        {
            drowSideLine_DrawLeft = left;
            drowSideLine_DrawColor = color;
            drowSideLine_Draw = true;
            drowSideLine_DrawWidth = width;
            var now = DateTime.Now;
            var stopTime = now + time;
            drowSideLine_DrawStopTime = stopTime;
            pbView.Invalidate();
            await Task.Delay(time);
            if (drowSideLine_DrawStopTime <= stopTime)
            {
                drowSideLine_Draw = false;
                pbView.Invalidate();
            }
        }

        /*
        private void setShortcutKeyForDevelop()
        {
            var t = getMouseGestureSettingTemplate(CommandSender.KeyboardShortcut);
            ksViewForm.Actions = new KeyboardShortcutAction[]
            {
                setShortcutKeyForDevelopHelper(t, Command.NextPage, Keys.Right),
                setShortcutKeyForDevelopHelper(t, Command.PreviousPage, Keys.Left),
                setShortcutKeyForDevelopHelper(t, Command.MoveForwardOnePage, Keys.Down),
                setShortcutKeyForDevelopHelper(t, Command.MoveBackOnePage, Keys.Up),
            };
        }
        private KeyboardShortcutAction setShortcutKeyForDevelopHelper(MouseGestureSettingTemplate[] t, Command command, params Keys[] keys)
        {
            var intCommand = (int)command;
            var continuousExecution = ContinuousExecutionCommands.Contains(command);
            return new KeyboardShortcutAction(intCommand, t.First(e => e.Key == intCommand).Action, continuousExecution, keys);
        }
        */

        private void setShortcutKeyDisplayStrings(bool setKeyNames)
        {
            var kss = ksViewForm.Actions;
            if (kss == null) kss = new KeyboardShortcutAction[0];

            if (setKeyNames)
            {
                KeyboardShortcutAction.KeyNames = new Dictionary<Keys, string>
                {
                    { Keys.Escape, "Esc" },
                    { Keys.ControlKey, "Ctrl" },
                    { Keys.ShiftKey, "Shift" },
                    { Keys.Menu, "Alt" }, // Alt も別にある
                    { Keys.Delete, "Del" },
                    { Keys.Enter, "Enter" },// 別名の Return 回避
                    { Keys.Space, Message.SpaceKey },
                    { Keys.PageDown, "PageDown" }, // 別名の Next 回避
                    { Keys.Up, "↑" },
                    { Keys.Down, "↓" },
                    { Keys.Left, "←" },
                    { Keys.Right, "→" },
                    { Keys.OemMinus, "-" },
                    { Keys.Oem7, "^" },
                    { Keys.Oem5, @"\" },
                    { Keys.Oemtilde, "@" },
                    { Keys.OemOpenBrackets, "[" },
                    { Keys.Oemplus, ";" },
                    { Keys.Oem1, ":" },
                    { Keys.Oem6, "]" },
                    { Keys.Oemcomma, "," },
                    { Keys.OemPeriod, "." },
                    { Keys.OemQuestion, "/" },
                    { Keys.OemBackslash, @"\" },
                    { Keys.NumPad1, "N1" },
                    { Keys.NumPad2, "N2" },
                    { Keys.NumPad3, "N3" },
                    { Keys.NumPad4, "N4" },
                    { Keys.NumPad5, "N5" },
                    { Keys.NumPad6, "N6" },
                    { Keys.NumPad7, "N7" },
                    { Keys.NumPad8, "N8" },
                    { Keys.NumPad9, "N9" },
                    { Keys.NumPad0, "N0" },
                    { Keys.D1, "1" },
                    { Keys.D2, "2" },
                    { Keys.D3, "3" },
                    { Keys.D4, "4" },
                    { Keys.D5, "5" },
                    { Keys.D6, "6" },
                    { Keys.D7, "7" },
                    { Keys.D8, "8" },
                    { Keys.D9, "9" },
                    { Keys.D0, "0" },
                    { Keys.LButton, Message.LeftClick },
                    { Keys.MButton, Message.MiddleClick },
                    { Keys.RButton, Message.RightClick },
                    { Keys.XButton1, Message.X1Button },
                    { Keys.XButton2, Message.X2Button },
                    { ExtendedKeys.WheelUp, Message.WheelUp },
                    { ExtendedKeys.WheelDown, Message.WheelDown },

                    { ExtendedKeys.NextPageAtLastPage, Message.NextPageAtLastPage },
                    { ExtendedKeys.PreviousPageAtFirstPage, Message.PreviousPageAtFirstPage },

                    { ExtendedKeys.LButtonOnRight, Message.LeftClickOnRightSide },
                    { ExtendedKeys.LButtonOnTopRight, Message.LeftClickOnTopRight },
                    { ExtendedKeys.LButtonOnTop, Message.LeftClickOnTopSide },
                    { ExtendedKeys.LButtonOnTopLeft, Message.LeftClickOnTopLeft },
                    { ExtendedKeys.LButtonOnLeft, Message.LeftClickOnLeftSide },
                    { ExtendedKeys.LButtonOnBottomLeft, Message.LeftClickOnBottomLeft },
                    { ExtendedKeys.LButtonOnBottom, Message.LeftClickOnBottomSide },
                    { ExtendedKeys.LButtonOnBottomRight, Message.LeftClickOnBottomRight },
                    { ExtendedKeys.LButtonOnCenter, Message.LeftClickOnCenter },

                    { ExtendedKeys.LButtonDouble, Message.DoubleLeftClick },
                    /*
                    { Keys.Up, Message.CursorUp },
                    { Keys.Down, Message.CursorDown },
                    { Keys.Left, Message.CursorLeft },
                    { Keys.Right, Message.CursorRight },
                    */
                };

                foreach (var ks in kss) ks.SetNameFromShortcut();
            }

            openFileToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.OpenFile);
            openNewThumbnailWindowToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.OpenThumbnailWindow);
            switchToThumbnailWindowToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.SwitchToThumbnailWindow);
            cloneTheWindowToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.CloneWindow);
            cloneTheWindowToolStripMenuItem.Visible = !string.IsNullOrEmpty(cloneTheWindowToolStripMenuItem.ShortcutKeyDisplayString);

            readOnMemoryNoneToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.ReadOnMemoryModeNone);
            readOnMemoryExceptLookAheadToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.ReadOnMemoryModeExceptLookAhead);
            readOnMemoryAlwaysToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.ReadOnMemoryModeAlways);

            nonerecommendedToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.ArchivesInArchiveModeIgnore);
            untilFoundToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.ArchivesInArchiveModeUntilFound2Level);
            alwaysLoadArchivesInArchiveToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.ArchivesInArchiveModeAlways1Level);

            fullScreenInStartToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.FullScreen);
            windowToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.Window);
            toggleFullScreenModeToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.ToggleFullScreenMode);
            exitToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.Exit);

            nextPageToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(/*Message.WheelDown,*/ kss,
                Command.NextPage, Command.MoveForwardOrMagnifierAutoScroll, Command.MoveForwardOrMagnifierAutoScrollWithoutOverwrap);
            previousPageToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(/*Message.WheelUp,*/ kss,
                Command.PreviousPage, Command.MoveBackOrMagnifierAutoScroll, Command.MoveBackOrMagnifierAutoScrollWithoutOverwrap);
            /*
            if (ksViewForm.ActionsContainsLButton)
            {
                rightPageToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.RightPage);
                leftPageToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.LeftPage);
            }
            else
            {
                rightPageToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(Message.LeftClickOnRightHalf, kss, Command.RightPage);
                leftPageToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(Message.LeftClickOnLeftHalf, kss, Command.LeftPage);
            }
            */
            rightPageToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss,
                Command.RightPage, Command.MoveRightOrMagnifierAutoScroll, Command.MoveRightOrMagnifierAutoScrollWithoutOverwrap);
            leftPageToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss,
                Command.LeftPage, Command.MoveLeftOrMagnifierAutoScroll, Command.MoveLeftOrMagnifierAutoScrollWithoutOverwrap);

            percentage000YoolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.MoveTo0Percent);
            percentage010YoolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.MoveTo10Percent);
            percentage020YoolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.MoveTo20Percent);
            percentage030YoolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.MoveTo30Percent);
            percentage040YoolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.MoveTo40Percent);
            percentage050YoolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.MoveTo50Percent);
            percentage060YoolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.MoveTo60Percent);
            percentage070YoolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.MoveTo70Percent);
            percentage080YoolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.MoveTo80Percent);
            percentage090YoolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.MoveTo90Percent);
            percentage100YoolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.MoveTo100Percent);

            moveForward1PageToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(Message.WheelDownOnBar, kss, Command.MoveForwardOnePage);
            //if (ksViewForm.ActionsContainsLButton)
            {
                moveBack1PageToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(Message.WheelUpOnBar, kss, Command.MoveBackOnePage);
            }
            //else
            //{
                //moveBack1PageToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(Message.LeftClickOnCenter + ", " + Message.WheelUpOnBar, kss, Command.MoveBackOnePage);
            //}
            moveRight1PageToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.MoveRightOnePage);
            moveRight1PageToolStripMenuItem.Visible = !string.IsNullOrEmpty(moveRight1PageToolStripMenuItem.ShortcutKeyDisplayString);
            moveLeft1PageToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.MoveLeftOnePage);
            moveLeft1PageToolStripMenuItem.Visible = !string.IsNullOrEmpty(moveLeft1PageToolStripMenuItem.ShortcutKeyDisplayString);
            openNextToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(/*Message.MiddleClickOnPosteriorHalfOfBar, */kss, Command.OpenNext);
            openPreviousToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(/*Message.MiddleClickOnAnteriorHalfOfBar,*/ kss, Command.OpenPrevious);
            openRightToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.OpenRight);
            openRightToolStripMenuItem.Visible = !string.IsNullOrEmpty(openRightToolStripMenuItem.ShortcutKeyDisplayString);
            openLeftToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.OpenLeft);
            openLeftToolStripMenuItem.Visible = !string.IsNullOrEmpty(openLeftToolStripMenuItem.ShortcutKeyDisplayString);

            enableMagnifierToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.EnableMagnifier, Command.EnableMagnifierWithoutOperatingGuide);
            enableMagnifierToolStripMenuItem.Visible = !string.IsNullOrEmpty(enableMagnifierToolStripMenuItem.ShortcutKeyDisplayString);
            disableMagnifierToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.DisableMagnifier, Command.DisableMagnifierWithoutOperatingGuide, Command.DisableMagnifierWithoutScreenEffects);
            disableMagnifierToolStripMenuItem.Visible = !string.IsNullOrEmpty(disableMagnifierToolStripMenuItem.ShortcutKeyDisplayString);
            magnifierToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.ToggleMagnifier, Command.ToggleMagnifierWithoutOperatingGuide, Command.ToggleMagnifierWithMinimalScreenEffects);
            //magnifierToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(Message.MiddleClick, kss, Command.ToggleMagnifier);

            rightToLeftToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.RightToLeft);
            leftToRightToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.LeftToRight);
            singlePageToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.SinglePage);
            singlePageWithoutScalingUpToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.SinglePageWithoutScalingUp);
            togglePageSequenceToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.TogglePageSequence);
            togglePageSequenceToolStripMenuItem.Visible = !string.IsNullOrEmpty(togglePageSequenceToolStripMenuItem.ShortcutKeyDisplayString);

            forceFirstPageToBeSingleToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.ToggleForceFirstPageToBeSingle);

            forceTwoPageSpreadToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.ToggleMinPageCountInWindow);
            allowPageDivisionToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.ToggleMaxDivision);

            alwaysAutoHideUIToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.ToggleAlwaysHideUI);
            orientSeekBarDirectionToPageSequenceToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.ToggleSeekBarMode);
            selectBackgroundColorToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.SelectBackgroundColor);

            rotateLeftToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.ToggleRotateLeft);
            rotateRightToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.ToggleRotateRight);
            cancelRotationSettingToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.CancelRotationSetting);
            cancelRotationSettingToolStripMenuItem.Visible = !string.IsNullOrEmpty(cancelRotationSettingToolStripMenuItem.ShortcutKeyDisplayString);

            //removeMoireToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.ToggleMoireRemover);

            pseudoColoringToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.ToggleToneCurve);

            startSlideshowToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.StartSlideshow);
            stopSlideshowToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.StopSlideshow);
            toggleSlideshowToolStripMenuItem.ShortcutKeyDisplayString = getShortcutKeyDisplayStrings(kss, Command.ToggleSlideshow);
        }

        private static string getShortcutKeyDisplayStrings(KeyboardShortcutAction[] actions, Command command)
        {
            var intCommand = (int)command;
            return string.Join(", ", from ks in actions where ks.Key == intCommand select ks.Name);
        }
        private static string getShortcutKeyDisplayStrings(KeyboardShortcutAction[] actions, Command command1, Command command2)
        {
            var intCommand1 = (int)command1;
            var intCommand2 = (int)command2;
            return string.Join(", ", from ks in actions let key = ks.Key where key == intCommand1 || key == intCommand2 select ks.Name);
        }
        private static string getShortcutKeyDisplayStrings(KeyboardShortcutAction[] actions, Command command1, Command command2, Command command3)
        {
            var intCommand1 = (int)command1;
            var intCommand2 = (int)command2;
            var intCommand3 = (int)command3;
            return string.Join(", ", from ks in actions let key = ks.Key where key == intCommand1 || key == intCommand2 || key == intCommand3 select ks.Name);
        }
        private static string getShortcutKeyDisplayStrings(string last, KeyboardShortcutAction[] actions, Command command)
        {
            var intCommand = (int)command;
            //return stringJoin(", ", first, from ks in actions where ks.Key == intCommand select ks.Name);
            return stringJoin(", ", from ks in actions where ks.Key == intCommand select ks.Name, last);
        }
        private static string stringJoin(string separator, IEnumerable<string> others, params string[] lasts)
        {
            if (others == null || !others.Any()) return string.Join(separator, lasts);
            else return string.Join(separator, others) + separator + string.Join(separator, lasts);
        }

        private void openNextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenNext();
        }

        private void openPreviousToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenPrevious();
        }

        private void openRightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenRight();
        }

        private void openLeftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenLeft();
        }

        enum OpenResult { Success, NoOperation, ShowWarning, CloseForm }
        private OpenResult OpenRight() { return OpenNextOrPrevious(next: BindingMode != BindingMode.RightToLeft, loop: false); }
        private OpenResult OpenLeft() { return OpenNextOrPrevious(next: BindingMode == BindingMode.RightToLeft, loop: false); }
        private OpenResult OpenNext(Action notFoundAction = null) { return OpenNextOrPrevious(next: true, loop: false, notFoundAction: notFoundAction); }
        private OpenResult OpenNextWithLoop(Action notFoundAction = null) { return OpenNextOrPrevious(next: true, loop: true, notFoundAction: notFoundAction); }
        private OpenResult OpenPrevious(Action notFoundAction = null) { return OpenNextOrPrevious(next: false, loop: false, notFoundAction: notFoundAction); }
        private OpenResult OpenPreviousWithLoop(Action notFoundAction = null) { return OpenNextOrPrevious(next: false, loop: true, notFoundAction: notFoundAction); }
        class OpenNextOrPrevious_Comparer : IComparer<FileSortInfo>
        {
            private static readonly LogicalStringComparer LogicalStringComparer = new LogicalStringComparer();
            private SortMode sortMode, preSortMode;
            private string randomSeed;
            public OpenNextOrPrevious_Comparer(SortModeDetails sortModeDetails) : base()
            {
                if (sortModeDetails == null)
                {
                    sortMode = SortMode.NameInAsc;
                }
                else
                {
                    sortMode = sortModeDetails.SortMode;
                    preSortMode = sortModeDetails.PreSortMode;
                    randomSeed = sortModeDetails.RandomSeed;
                }
            }
            public int Compare(FileSortInfo x, FileSortInfo y)
            {
                switch (sortMode)
                {
                    case SortMode.NameInAsc: return LogicalStringComparer.Compare(x.Name, y.Name);
                    case SortMode.NameInDesc: return -LogicalStringComparer.Compare(x.Name, y.Name);
                    case SortMode.Random: return x.GetRandomIndex(randomSeed).CompareTo(y.GetRandomIndex(randomSeed));
                    default:
                        //MessageBox.Show($"{x.Type}, {y.Type}");
                        var result = Compare(x, y, sortMode);
                        if (result != 0) return result;
                        result = Compare(x, y, preSortMode);
                        if (result != 0) return result;
                        //MessageBox.Show($"{sortMode} {preSortMode}");
                        //return result;
                        return LogicalStringComparer.Compare(x.Name, y.Name);
                }
               
            }
            private static int Compare(FileSortInfo x, FileSortInfo y, SortMode sortMode)
            {
                switch (sortMode)
                {
                    case SortMode.AccessedInAsc: return DateTime.Compare(x.LastAccessTime, y.LastAccessTime);
                    case SortMode.AccessedInDesc: return -DateTime.Compare(x.LastAccessTime, y.LastAccessTime);
                    case SortMode.CreatedInAsc: return DateTime.Compare(x.CreationTime, y.CreationTime);
                    case SortMode.CreatedInDesc: return -DateTime.Compare(x.CreationTime, y.CreationTime);
                    case SortMode.ModifiedInAsc: return DateTime.Compare(x.LastWriteTime, y.LastWriteTime);
                    case SortMode.ModifiedInDesc: return -DateTime.Compare(x.LastWriteTime, y.LastWriteTime);
                    case SortMode.RatingInAsc: return CompareRatingAsc(x.Rating, y.Rating);
                    case SortMode.RatingInDesc: return CompareRatingDesc(x.Rating, y.Rating);
                    case SortMode.SizeInAsc: return SizeCompareInAsc(x.Size, y.Size);
                    case SortMode.SizeInDesc: return SizeCompareInDesc(x.Size, y.Size);
                    case SortMode.TypeInAsc: return string.Compare(x.Type, y.Type);
                    case SortMode.TypeInDesc: return -string.Compare(x.Type, y.Type);

                    // preSort　用
                    case SortMode.NameInAsc: return LogicalStringComparer.Compare(x.Name, y.Name);
                    case SortMode.NameInDesc: return -LogicalStringComparer.Compare(x.Name, y.Name);

                }
                return 0;
                //throw new NotImplementedException();
            }
            private static int CompareRatingAsc(int ratingX, int ratingY)
            {
                if (ratingX <= 0) return ratingY <= 0 ? 0 : 1;
                else return ratingY <= 0 ? -1 : ratingX - ratingY;
            }
            private static int CompareRatingDesc(int ratingX, int ratingY)
            {
                if (ratingX <= 0) return ratingY <= 0 ? 0 : 1;
                else return ratingY <= 0 ? -1 : ratingY - ratingX;
            }
            private static int SizeCompareInAsc(long sizeX, long sizeY)
            {
                if (sizeX < 0) return sizeY < 0 ? 0 : 1;
                if (sizeY < 0) return -1;
                if (sizeX < sizeY) return -1;
                if (sizeX > sizeY) return +1;
                return 0;
            }
            private static int SizeCompareInDesc(long sizeX, long sizeY)
            {
                if (sizeX < 0) return sizeY < 0 ? 0 : 1;
                if (sizeY < 0) return -1;
                if (sizeX < sizeY) return +1;
                if (sizeX > sizeY) return -1;
                return 0;
            }
        }
        class FileSortInfo
        {
            private FileSystemInfo info;
            public FileSortInfo(FileSystemInfo info)
            {
                this.info = info;
            }
            public string FullName { get { return info.FullName; } }
            public string Name { get { return info.Name; } }
            public DateTime LastAccessTime { get { return info.LastAccessTime; } }
            public DateTime CreationTime { get { return info.CreationTime; } }
            public DateTime LastWriteTime { get { return info.LastWriteTime; } }

            public string Type;

            private long size = -2;
            public long Size
            {
                get
                {
                    if (size == -2)
                    {
                        var fileInfo = info as FileInfo;
                        if (fileInfo != null) size = fileInfo.Length;
                        else size = -1;
                    }
                    return size;
                }
            }

            private int rating = -1;
            public int Rating
            {
                get
                {
                    if (rating == -1)
                    {
                        rating = ZipPlaInfo.GetOnlyRating(info.Name);
                        if (rating <= 0) rating = 0;
                    }
                    return rating;
                    
                }
            }

            private bool randomIndexNotExists = true;
            private UBigInteger randomIndex;
            public UBigInteger GetRandomIndex(string seedForFirstCall)
            {
                if (randomIndexNotExists)
                {
                    randomIndex = CatalogForm.GetRandomIndex(info.FullName, seedForFirstCall);
                    randomIndexNotExists = false;
                }
                return randomIndex;
            }
        }
        private OpenResult OpenNextOrPrevious(bool next, bool loop, Action notFoundAction = null)
        {
            if (ExitAfterCoverSetting) return OpenResult.NoOperation;

            var currentParent = getCurrentParent();
            var current = currentArchiveFilePath;
            if (currentParent == null) return OpenResult.NoOperation;

            var sign = next ? 1 : -1;

            try
            {
                var isDir = Directory.Exists(current);
                var currentInfo = new FileSortInfo(isDir ? new DirectoryInfo(current) : new FileInfo(current) as FileSystemInfo);

                var parent = new DirectoryInfo(currentParent);
                
                var comparer = new OpenNextOrPrevious_Comparer(requestedSortModeDetails);
                
                var allFiles = parent.GetFiles()
                    .Where(info => PackedImageLoader.Supports(info.Name))
                    .Select(info => new FileSortInfo(info)).ToArray();
                var allFolders = parent.GetDirectories().Select(info => new FileSortInfo(info)).ToArray();

                if (requestedSortModeDetails != null && (requestedSortModeDetails.SortMode == SortMode.TypeInAsc || requestedSortModeDetails.SortMode == SortMode.TypeInDesc ||
                    requestedSortModeDetails.PreSortMode == SortMode.TypeInAsc || requestedSortModeDetails.PreSortMode == SortMode.TypeInDesc))
                {
                    var nameArray = new string[1 + allFiles.Length + allFolders.Length];
                    nameArray[0] = isDir ? currentInfo.Name + Path.DirectorySeparatorChar : currentInfo.Name;
                    var i = 1;
                    foreach (var info in allFiles) nameArray[i++] = info.Name;
                    foreach (var info in allFolders) nameArray[i++] = info.Name + Path.DirectorySeparatorChar;
                    nameArray = CatalogForm.GetTypeNameArray(nameArray);
                    currentInfo.Type = nameArray[0];
                    //MessageBox.Show($"{currentInfo.Type} {currentInfo.Name}");
                    i = 1;
                    foreach (var info in allFiles) info.Type = nameArray[i++];
                    foreach (var info in allFolders) info.Type = nameArray[i++];
                }

                if (searchManagerFromCatalogForm != null) allFiles = allFiles.Where(info => searchManagerFromCatalogForm.Match(info.Name)).ToArray();
                var files = (from item in allFiles where sign * comparer.Compare(currentInfo, item) < 0 select item).ToArray();
                if (searchManagerFromCatalogForm != null) allFolders = allFolders.Where(info => searchManagerFromCatalogForm.Match(info.Name)).ToArray();
                var folders = (from item in allFolders where sign * comparer.Compare(currentInfo, item) < 0 select item).ToArray();
                var paths = new FileSortInfo[files.Length + folders.Length];
                var index = 0;
                foreach (var file in files) paths[index++] = file;
                foreach (var folder in folders) paths[index++] = folder;
                //if (currentSortModeDetails == null || currentSortModeDetails.SortMode == SortMode.nameina)
                Array.Sort(paths, comparer);

                //MessageBox.Show($"{paths.Length}");

                while (true)
                {
                    int start, stop, delta;
                    if (next)
                    {
                        start = 0; stop = index; delta = 1;
                    }
                    else
                    {
                        start = index - 1; stop = -1; delta = -1;
                    }

                    for (var i = start; i != stop; i += delta)
                    {
                        var path = paths[i];
                        try
                        {
                            using (var loader = getPackedImageLoader(path.FullName, ViewerFormOnMemoryMode.ForCheckOnlyEntries))
                            {
                                //if(loader.GetPackedImageFullEntries().Count > 0) // FullEntries の方が処理が少なく、こちらが非ゼロなら Entries も非ゼロ
                                if (loader.HasAnyEntry()) // 根本的な改善
                                {
                                    return OpenFile(path.FullName);
                                }
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    if (loop)
                    {
                        files = (from item in allFiles where sign * comparer.Compare(currentInfo, item) > 0 select item).ToArray();
                        folders = (from item in allFolders where sign * comparer.Compare(currentInfo, item) > 0 select item).ToArray();
                        //paths = files.Concat(folders).OrderBy(p => p, LogicalStringComparer).ToArray();
                        paths = new FileSortInfo[files.Length + folders.Length];
                        index = 0;
                        foreach (var file in files) paths[index++] = file;
                        foreach (var folder in folders) paths[index++] = folder;
                        //index = paths.Length;
                        Array.Sort(paths, comparer);
                        loop = false;
                        continue;
                    }

                    break;
                }

                notFoundAction?.Invoke();
                Program.ShowCursor();
                MessageBox.Show(this, Message.BookFolderIsNotFound, Message.Information, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return OpenResult.ShowWarning;
            }
            catch(Exception e)
            {
                notFoundAction?.Invoke();
                Program.ShowCursor();
#if DEBUG
                MessageBox.Show(this, e.ToString(), null, MessageBoxButtons.OK, MessageBoxIcon.Error);
#else
                MessageBox.Show(this, e.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif
                return OpenResult.ShowWarning;
            }

        }

        /*
        private void mtbPage_MouseDown(object sender, MouseEventArgs e)
        {
            EventHandlerForOpenNextOrPrevious(sender, e);
        }

        private void statusStrip_MouseDown(object sender, MouseEventArgs e)
        {
            EventHandlerForOpenNextOrPrevious(sender, e);
        }
        */

            /*
        private void EventHandlerForOpenNextOrPrevious(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                var control = sender as Control;
                if (control != null)
                {
                    var p = control.PointToClient(Cursor.Position);
                    if ((2 * p.X < control.Left + control.Right) ^ mtbPage.ValueRightToLeft) OpenPrevious();
                    else OpenNext();
                }
            }
        }
        */
        
        private void ShowViewSettingHint()
        {
            if (ViewSettingHintMode == ViewSettingHintMode.ShowWhenChangingSettings)
            {
                var showThisHintAgain = true;
                MessageForm.Show(this, Message.BuiltInViewerSavingSettingsBehaviorDescription.Replace("\\n", "\n"), Message.Hint, Message.ShowThisHintAgainNextTime, ref showThisHintAgain, Message.OK, MessageBoxIcon.Information);
                if (!showThisHintAgain) ViewSettingHintMode = ViewSettingHintMode.NeverShow;
            }
        }

        private void viewToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            rotateLeftToolStripMenuItem.Checked = imageFilter.RotationAngle == 270;
            rotateRightToolStripMenuItem.Checked = imageFilter.RotationAngle == 90;

            //removeMoireToolStripMenuItem.Checked = imageFilter.MoireRemoverEnabled;
            pseudoColoringToolStripMenuItem.Checked = imageFilter.ToneCurveEnabled;
            if (ViewerFormImageFilter.TestGetToneCorveFile())
            {
                pseudoColoringToolStripMenuItem.Text = Message.ToneCurveUserDefined;
            }
            else
            {
                pseudoColoringToolStripMenuItem.Text = Message.ToneCurveSepiaTone;
            }

            if (ViewerMode == ViewerModeEnum.CoverSetting)
            {
                magnifierToolStripMenuItem.Enabled = false;
                enableMagnifierToolStripMenuItem.Enabled = false;
                disableMagnifierToolStripMenuItem.Enabled = false;

                rotateLeftToolStripMenuItem.Enabled = false;
                rotateRightToolStripMenuItem.Enabled = false;
                cancelRotationSettingToolStripMenuItem.Enabled = false;
                pseudoColoringToolStripMenuItem.Enabled = false;
            }
            else
            {
                var magEnabled = !string.IsNullOrEmpty(currentArchiveFilePath);
                magnifierToolStripMenuItem.Enabled = magEnabled;
                enableMagnifierToolStripMenuItem.Enabled = magEnabled && ViewerMode == ViewerModeEnum.Normal;
                disableMagnifierToolStripMenuItem.Enabled = magEnabled && ViewerMode == ViewerModeEnum.Magnifier;

                rotateLeftToolStripMenuItem.Enabled = true;
                rotateRightToolStripMenuItem.Enabled = true;
                cancelRotationSettingToolStripMenuItem.Enabled = imageFilter.RotationAngle != 0;
                pseudoColoringToolStripMenuItem.Enabled = true;
            }
            
            SetMessageForwarderStop(true);
        }

        private void enableMagnifierToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableMagnifier();
        }

        private void disableMagnifierToolStripMenuItem_Click(object sender, EventArgs e)
        {
            disableMagnifier();
        }

        private void togglePageSequenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            togglePageSequence();
        }

        private void togglePageSequence()
        {
            // 2 つずつ独立に
            switch (BindingMode)
            {
                case BindingMode.LeftToRight: ChangePageDirection(BindingMode.RightToLeft); break;
                case BindingMode.RightToLeft: ChangePageDirection(BindingMode.LeftToRight); break;
                case BindingMode.SinglePage: ChangePageDirection(BindingMode.SinglePageWithoutScalingUp); break;
                default: ChangePageDirection(BindingMode.SinglePage); break;
            }

            // 4 つを順番に
            /*
            switch(BindingMode)
            {
                case BindingMode.LeftToRight: ChangePageDirection(BindingMode.RightToLeft); break;
                case BindingMode.RightToLeft: ChangePageDirection(BindingMode.SinglePage); break;
                case BindingMode.SinglePage: ChangePageDirection(BindingMode.SinglePageWithoutScalingUp); break;
                default: ChangePageDirection(BindingMode.LeftToRight); break;
            }
            */
        }

        private void pbView_MouseLeave(object sender, EventArgs e)
        {
            MouseInPbView = false;
            Program.ShowCursor();
            if (DrawMeasure) pbPaintDrawOnlyMeasure();
            pbView_MouseMove_prevContPos = null;
        }

        private void setSeekBarDirection()
        {
            switch(SeekBarMode)
            {
                case SeekBarMode.LeftToRight: mtbPage.InversedSlider = false; break;
                case SeekBarMode.RightToLeft: mtbPage.InversedSlider = true; break;
                case SeekBarMode.SameAsPage: mtbPage.InversedSlider = BindingMode == BindingMode.RightToLeft; break;
                case SeekBarMode.ReverseOfPage: mtbPage.InversedSlider = BindingMode != BindingMode.RightToLeft; break;
            }
        }

        private void orientSeekBarDirectionToPageSequenceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToggleSeekBarMode();
        }

        private void ToggleSeekBarMode()
        {
            SeekBarMode = SeekBarMode == SeekBarMode.LeftToRight ? SeekBarMode.SameAsPage : SeekBarMode.LeftToRight;
        }

        private void btnOpenLeft_Paint(object sender, PaintEventArgs e)
        {
            ControlPainter.FillLeftTriangle(sender as Control, e);
        }

        private void btnOpenRight_Paint(object sender, PaintEventArgs e)
        {
            ControlPainter.FillRightTriangle(sender as Control, e);
        }

        private void btnOpenLeft_Click(object sender, EventArgs e)
        {
            if (mtbPage.ValueCanChangedByMouseButton)
            {
                if (mtbPage.InversedSlider) OpenNext();
                else OpenPrevious();
            }
            else mtbPage.ValueCanChangedByMouseButton = true;
        }

        private void btnOpenRight_Click(object sender, EventArgs e)
        {
            if (mtbPage.ValueCanChangedByMouseButton)
            {
                if (mtbPage.InversedSlider) OpenPrevious();
                else OpenNext();
            }
            else mtbPage.ValueCanChangedByMouseButton = true;
        }

        private void setSlideshow(int intervalMilliseconds)
        {
            if (intervalMilliseconds <= 0)
            {
                if (tmSlideshow_LoadWaiting)
                {
                    tmSlideshow_StopReserved = true;
                }
                else
                {
                    tmSlideshow_StopReserved = false;
                    tmSlideshow.Stop();
                }
            }
            else
            {
                tmSlideshow_LoadWaiting = false;
                tmSlideshow_StopReserved = false;
                tmSlideshow.Interval = intervalMilliseconds;
                tmSlideshow.Start();
            }
        }

        private bool tmSlideshow_LoadWaiting = false;
        private bool tmSlideshow_StopReserved = false;
        private void tmSlideshow_Tick(object sender, EventArgs e)
        {
            if (!ViewerForm_Shown_Called) return;

            if (PreFilteredImageArray == null || PreFilteredImageArray.Length <= 0)
            {
                //setSlideshow(0);
                slideshowStop();
                return;
            }
            Func<bool> isMovable;
            Action move;
            if(SlideshowForward)
            {
                if (NextPage >= PreFilteredImageArray.Length)
                {
                    if(SlideshowGlobal)
                    {
                        if (SlideshowRepeat)
                        {
                            OpenNextWithLoop(notFoundAction: slideshowStop);
                        }
                        else
                        {
                            OpenNext(notFoundAction: slideshowStop);
                        }
                    }
                    else
                    {
                        if (SlideshowRepeat)
                        {
                            // 読み込みの表示が出る可能性があるがシークバーを動かさないと読み込みが始まらない
                            // 修正するなら、先読みの優先順位を決める距離の定義を変えた上で
                            // isMovablePageNatural と movePageNatural に Loop のオーバーロードを加えて状況に応じてそれを呼び出し
                            // （ただし単純に先読みの優先順位を変えると初めから開いたときに無駄に後ろの方を読み込むことになるので
                            // 　スライドショーが動いているときのみスライドショーの方向に合わせて最小限の変更をするべき）
                            mtbPage.Value = 0;
                        }
                        else
                        {
                            //setSlideshow(0);
                            slideshowStop();
                        }
                    }
                    return;
                }
                if (SlideshowMove1Page)
                {
                    isMovable = () => isMovablePageToMinimulForward(checkLoaded: true);
                    move = movePageToMinimulForward;
                }
                else
                {
                    isMovable = () => isMovablePageNatural(1, checkLoaded: true);
                    move = () => movePageNatural(1);
                }
            }
            else
            {
                if (currentPage <= 0)
                {
                    if (SlideshowGlobal)
                    {
                        if (SlideshowRepeat)
                        {
                            OpenPreviousWithLoop(notFoundAction: slideshowStop);
                        }
                        else
                        {
                            OpenPrevious(notFoundAction: slideshowStop);
                        }
                    }
                    else
                    {
                        if (SlideshowRepeat)
                        {
                            // 読み込みの表示が出る可能性があるがシークバーを動かさないと読み込みが始まらない
                            // 修正するなら、先読みの優先順位を決める距離の定義を変えた上で
                            // isMovablePageNatural と movePageNatural に Loop のオーバーロードを加えて状況に応じてそれを呼び出し
                            // （ただし単純に先読みの優先順位を変えると初めから開いたときに無駄に後ろの方を読み込むことになるので
                            // 　スライドショーが動いているときのみスライドショーの方向に合わせて最小限の変更をするべき）
                            mtbPage.Value = PreFilteredImageArray.Length - 1;
                        }
                        else
                        {
                            //setSlideshow(0);
                            slideshowStop();
                        }
                    }
                    return;
                }
                if (SlideshowMove1Page)
                {
                    isMovable = () => isMovablePageToMinimulBack(checkLoaded: true);
                    move = movePageToMinimulBack;
                }
                else
                {
                    isMovable = () => isMovablePageNatural(-1, checkLoaded: true);
                    move = () => movePageNatural(-1);
                }
            }

            if (isMovable())
            {
                move();
            }
            else
            {
                tmSlideshow_LoadWaiting = true;
                tmSlideshow.Stop();
                GenerarClasses.BackgroundMultiWorker.EachRunWorkerCompletedEventHandler eh = null;
                eh = (sender2, e2) =>
                {
                    if (tmSlideshow_StopReserved)
                    {
                        tmSlideshow_LoadWaiting = false;
                        tmSlideshow_StopReserved = false;
                    }
                    else
                    {
                        slideshowStart();
                        tmSlideshow_Tick(sender, e);
                    }
                    bmwLoadEachPage.EachRunWorkerCompleted -= eh;
                    /*
                    if (isMovable())
                    {
                        move();
                        tmSlideshow_LoadWaiting = false;
                        if (tmSlideshow_StopReserved)
                        {
                            tmSlideshow_StopReserved = false;
                        }
                        else
                        {
                            tmSlideshow.Start();
                        }
                        bmwLoadEachPage.EachRunWorkerCompleted -= eh;
                    }
                    */
                };
                bmwLoadEachPage.EachRunWorkerCompleted += eh;
            }
        }

        private void _1SecIntervalsToolStripMenuItem_Click(object sender, EventArgs e) { tmSlideshow.Interval = 1 * 1000; }
        private void _2SecIntervalsToolStripMenuItem_Click(object sender, EventArgs e) { tmSlideshow.Interval = 2 * 1000; }
        private void _3SecIntervalsToolStripMenuItem_Click(object sender, EventArgs e) { tmSlideshow.Interval = 3 * 1000; }
        private void _5SecIntervalsToolStripMenuItem_Click(object sender, EventArgs e) { tmSlideshow.Interval = 5 * 1000; }
        private void _10SecIntervalsToolStripMenuItem_Click(object sender, EventArgs e) { tmSlideshow.Interval = 10 * 1000; }
        private void _20SecIntervalsToolStripMenuItem_Click(object sender, EventArgs e) { tmSlideshow.Interval = 20 * 1000; }
        private void _30SecIntervalsToolStripMenuItem_Click(object sender, EventArgs e) { tmSlideshow.Interval = 30 * 1000; }
        private void _60SecIntervalsToolStripMenuItem_Click(object sender, EventArgs e) { tmSlideshow.Interval = 60 * 1000; }

        private void startSlideshowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            slideshowStart();
        }

        private void slideshowStart()
        {
            slideshowStart(force: false);
        }
        private void slideshowStart(bool force)
        {
            if (ExitAfterCoverSetting) return;

            var opend = PreFilteredImageArray != null;
            if (opend && (force || !getSlideshowEnabled()))
            {
                setSlideshow(tmSlideshow.Interval);
            }
            setTitleBar();
        }

        private void stopSlideshowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            slideshowStop();
        }

        private void slideshowStop()
        {
            if (ExitAfterCoverSetting) return;
            setSlideshow(0);
            setTitleBar();
        }

        private void toggleSlideshowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            slideshowToggle();
        }

        private void slideshowToggle()
        {
            if (ExitAfterCoverSetting) return;
            if (!getSlideshowEnabled()) slideshowStart(force: true);
            else slideshowStop();
        }

        private void repeatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SlideshowRepeat = !SlideshowRepeat;
        }

        private bool getSlideshowEnabled()
        {
            return !tmSlideshow_StopReserved && (tmSlideshow_LoadWaiting || tmSlideshow.Enabled);
        }
        
        private void openNextOnTerminalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SlideshowGlobal = !SlideshowGlobal;
        }

        private void closeWindowtoolStripMenuItem_Paint(object sender, PaintEventArgs e)
        {
            if (closeWindowtoolStripMenuItem.Text != null)
            {
                closeWindowtoolStripMenuItem.AutoSize = false;
                closeWindowtoolStripMenuItem.Text = null;
                closeWindowtoolStripMenuItem.Width = closeWindowtoolStripMenuItem.Height;
            }
            ControlPainter.FillCross(sender as ToolStripItem, e);
        }

        /*
        private void imageFilterToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            rotateLeftToolStripMenuItem.Checked = imageFilter.RotationAngle == 270;
            rotateRightToolStripMenuItem.Checked = imageFilter.RotationAngle == 90;
            //binarizationToolStripMenuItem.Checked = imageFilter.PixelValueConversion == PixelValueConversion.Binarization;
            //autoContrastControlToolStripMenuItem.Checked = imageFilter.PixelValueConversion == PixelValueConversion.AutoContrastControl;
        }
        */

        private void rotateLeftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toggleRotateLeft();
        }

        private void toggleRotateLeft()
        {
            if (ViewerMode != ViewerModeEnum.CoverSetting)
            {
                imageFilter.RotationAngle = imageFilter.RotationAngle == 270 ? 0 : 270;
                reloadForChangeFilter(forOnlyPostFilter: false);
            }
        }

        private void rotateRightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toggleRotateRight();
        }

        private void toggleRotateRight()
        {
            if (ViewerMode != ViewerModeEnum.CoverSetting)
            {
                imageFilter.RotationAngle = imageFilter.RotationAngle == 90 ? 0 : 90;
                reloadForChangeFilter(forOnlyPostFilter: false);
            }
        }

        private void cancelRotationSettingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cancelRotationSetting();
        }

        private void cancelRotationSetting()
        {
            if (ViewerMode != ViewerModeEnum.CoverSetting && imageFilter.RotationAngle != 0)
            {
                imageFilter.RotationAngle = 0;
                reloadForChangeFilter(forOnlyPostFilter: false);
            }
        }
        
        private void pseudoColoringToolStripMenuItem_Click(object sender, EventArgs e)
        {
            togglePseudoColoring();
        }

        private void togglePseudoColoring()
        {
            if (ViewerMode != ViewerModeEnum.CoverSetting)
            {
                imageFilter.ToneCurveEnabled = !imageFilter.ToneCurveEnabled;
                reloadForChangeFilter(forOnlyPostFilter: true);
            }
        }

        /*
        private void removeMoireToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toggleMoireRemover();
        }

        private void toggleMoireRemover()
        {
            if (ViewerMode != ViewerModeEnum.CoverSetting)
            {
                imageFilter.MoireRemoverEnabled = !imageFilter.MoireRemoverEnabled;
                reloadForChangeFilter(forOnlyPostFilter: true);
            }
        }
        */

        /*
        private void binarizationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toggleBinarization();
        }

        private void toggleBinarization()
        {
            imageFilter.PixelValueConversion = imageFilter.PixelValueConversion == PixelValueConversion.Binarization ? PixelValueConversion.None : PixelValueConversion.Binarization;
            reloadForChangeFilter();
        }

        private void autoContrastControlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toggleAutoContrastControl();
        }

        private void toggleAutoContrastControl()
        {
            imageFilter.PixelValueConversion = imageFilter.PixelValueConversion == PixelValueConversion.AutoContrastControl ? PixelValueConversion.None : PixelValueConversion.AutoContrastControl;
            reloadForChangeFilter();
        }
        */

        private void reloadForChangeFilter(bool forOnlyPostFilter)
        {
            drawLoading_ShowingMessage = true;
            Reload(force: true, updateOriginal: !forOnlyPostFilter);
        }

        private bool statusStrip_VisibleChanged_FirstVisible = true;
        private void statusStrip_VisibleChanged(object sender, EventArgs e)
        {
            if (statusStrip_VisibleChanged_FirstVisible && ipcLookAheadInfo == null && statusStrip.Visible)
            {
                statusStrip_VisibleChanged_FirstVisible = false;
                Program.TextBoxShowRight(currentImageLongPathToolStripTextBox); // statusStrip の位置調整を行ってもなお必要
            }
        }

        private void openStartMenu()
        {
            openToolStripMenuItem(startToolStripMenuItem, Cursor.Position, startToolStripMenuItem_DropDownOpening, null, scrollable: false, mouseGestureEnabled: false);
        }

        private void openMoveMenu()
        {
            openToolStripMenuItem(moveToolStripMenuItem, Cursor.Position, moveToolStripMenuItem_DropDownOpening, null, scrollable: false, mouseGestureEnabled: false);
        }

        private void openViewMenu()
        {
            openToolStripMenuItem(viewToolStripMenuItem, Cursor.Position, viewToolStripMenuItem_DropDownOpening, null, scrollable: false, mouseGestureEnabled: false);
        }
        
        private void openSlideshowMenu()
        {
            openToolStripMenuItem(slideshowToolStripMenuItem, Cursor.Position, slideshowToolStripMenuItem_DropDownOpening, null, scrollable: false, mouseGestureEnabled: false);
        }

        private void openVirtualFolderMenu()
        {
            openToolStripMenuItem(virtualFoldersToolStripMenuItem, Cursor.Position, virtualFoldersToolStripMenuItem_DropDownOpening, null, scrollable: true, mouseGestureEnabled: false);
        }

        private void openOthersMenu()
        {
            openToolStripMenuItem(optionToolStripMenuItem, Cursor.Position, null, null, scrollable: false, mouseGestureEnabled: false);
        }

        private void editToolStripMenuItem_DropDownOpeningForContextMenuStrip(object sender, EventArgs e) { editToolStripMenuItem_DropDownOpening(sender, e, openToolStripMenuItemAsContextMenuStrip_Frame); }
        private void openEditMenu()
        {
            openToolStripMenuItem(editToolStripMenuItem, Cursor.Position, editToolStripMenuItem_DropDownOpeningForContextMenuStrip, editToolStripMenuItem_DropDownClosed_CommonPart, scrollable: true, mouseGestureEnabled: true);
        }

        private void openToolStripMenuItemByKeyboard(ToolStripMenuItem toolStripMenuItem)
        {
            if (FullScreen)
            {
                menuStrip.Visible = true;
                menuStrip_Bottom = 0;
                hideUnderUI(null, null); // currentImageLongPathToolStripTextBox_MouseLeave の仕様が期待通りでないので
            }
            toolStripMenuItem.ShowDropDown();
        }

        private void openToolStripMenuItem(ToolStripMenuItem toolStripMenuItem, Point screenLocation, EventHandler dropDownOpening, EventHandler dropDownClosed, bool scrollable, bool mouseGestureEnabled)
        {
            if (ksViewForm.PureKeyboardShortcutExcuting())
            {
                openToolStripMenuItemByKeyboard(toolStripMenuItem);
            }
            else
            {
                openToolStripMenuItemAsContextMenuStrip(toolStripMenuItem, screenLocation, dropDownOpening, dropDownClosed, scrollable, mouseGestureEnabled);
            }
        }

        private ContextMenuStrip openToolStripMenuItemAsContextMenuStrip_Frame = null;
        private MiniControlTouchGesture openToolStripMenuItemAsContextMenuStrip_Frame_MiniControlTouchGesture;
        private GestureListener openToolStripMenuItemAsContextMenuStrip_Frame_GestureListener;
        private void openToolStripMenuItemAsContextMenuStrip(ToolStripMenuItem toolStripMenuItem, Point screenLocation, EventHandler dropDownOpening, EventHandler dropDownClosed, bool scrollable, bool mouseGestureEnabled)
        {
            if (openToolStripMenuItemAsContextMenuStrip_Frame == null)
            {
                openToolStripMenuItemAsContextMenuStrip_Frame = new ContextMenuStrip();
                openToolStripMenuItemAsContextMenuStrip_Frame.Closed += openToolStripMenuItemAsContextMenuStrip_Frame_Closed;

                new KeyMouseBinder(openToolStripMenuItemAsContextMenuStrip_Frame, requiredBindingEditMenu);

                try
                {
                    openToolStripMenuItemAsContextMenuStrip_Frame_GestureListener = MiniControlTouchGesture.GetGestureListener(openToolStripMenuItemAsContextMenuStrip_Frame);
                    openToolStripMenuItemAsContextMenuStrip_Frame_MiniControlTouchGesture = new MiniControlTouchGesture(openToolStripMenuItemAsContextMenuStrip_Frame, openToolStripMenuItemAsContextMenuStrip_Frame_GestureListener, onlyHorizontalStart: true);
                    openToolStripMenuItemAsContextMenuStrip_Frame_MiniControlTouchGesture.Targets.Add(openToolStripMenuItemAsContextMenuStrip_Frame);
                    openToolStripMenuItemAsContextMenuStrip_Frame_MiniControlTouchGesture.TouchGestureCompleted += CatalogForm.TouchGestureCompletedForTagMenu;
                    openToolStripMenuItemAsContextMenuStrip_Frame_MiniControlTouchGesture.TouchGestureStarting += CatalogForm.TouchGestureStartingForTagMenu;
                }
                catch { }
            }
            dropDownOpening(toolStripMenuItem, null);
            var items = toolStripMenuItem.DropDownItems;
            var copy = new ToolStripItem[items.Count];
            items.CopyTo(copy, 0);
            items.Clear(); 
            var contextMenuStrip = openToolStripMenuItemAsContextMenuStrip_Frame;
            var frameItems = contextMenuStrip.Items;
            frameItems.AddRange(copy);
            ToolStripDropDownClosedEventHandler eh = null;
            eh = (sender, e) =>
            {
                frameItems.Clear();
                items.AddRange(copy);
                dropDownClosed?.Invoke(toolStripMenuItem, null);
                contextMenuStrip.Closed -= eh;
            };
            contextMenuStrip.Closed += eh;
            if (scrollable)
            {
                ToolStripDropDownScroller.EnscrollableOneTime(contextMenuStrip, openToolStripMenuItemAsContextMenuStrip_Frame_GestureListener, enterFocus: true);
            }
            if (openToolStripMenuItemAsContextMenuStrip_Frame_MiniControlTouchGesture != null)
            {
                openToolStripMenuItemAsContextMenuStrip_Frame_MiniControlTouchGesture.Enabled = mouseGestureEnabled;
            }
            contextMenuStrip.Show(screenLocation);
            
        }

        private void openToolStripMenuItemAsContextMenuStrip_Frame_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            if (MouseButtons == MouseButtons.Left && pbView.ClientRectangle.Contains(pbView.PointToClient(Cursor.Position)))
            {
                ksViewForm.IgnoreAnyMouseEventOnce = MouseButtons.Left;
            }
            if (DrawMeasure) pbPaintDrawOnlyMeasure();
            SetMessageForwarderStop(false);
        }

        private static bool isIndividuallyImplemented(ScalingAlgorithm algorithm)
        {
            return algorithm == ScalingAlgorithm.Default || algorithm == ScalingAlgorithm.HighSpeed || algorithm == ScalingAlgorithm.NearestNeighbor;
        }

        private void scalingAlgorithmNormalToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            defaultScalingAlgorithmNormalToolStripMenuItem.Checked = StandardScalingAlgorithm.ScaleDown == ScalingAlgorithm.Default;
            default2ScalingAlgorithmNormalToolStripMenuItem.Checked = StandardScalingAlgorithm.ScaleDown == ScalingAlgorithm.HighSpeed;
            nearestNeighborNormalToolStripMenuItem.Checked = StandardScalingAlgorithm.ScaleDown == ScalingAlgorithm.NearestNeighbor;
            areaAverageNormalToolStripMenuItem.Checked = StandardScalingAlgorithm.ScaleDown == ScalingAlgorithm.AreaAverage;
            lanczos1NormalToolStripMenuItem.Checked = StandardScalingAlgorithm.ScaleDown == ScalingAlgorithm.Lanczos1;
            lanczos2NormalToolStripMenuItem.Checked = StandardScalingAlgorithm.ScaleDown == ScalingAlgorithm.Lanczos2;
            lanczos3NormalToolStripMenuItem.Checked = StandardScalingAlgorithm.ScaleDown == ScalingAlgorithm.Lanczos3;
            lanczos4NormalToolStripMenuItem.Checked = StandardScalingAlgorithm.ScaleDown == ScalingAlgorithm.Lanczos4;
            spline4NormalToolStripMenuItem.Checked = StandardScalingAlgorithm.ScaleDown == ScalingAlgorithm.Spline4;
            spline16NormalToolStripMenuItem.Checked = StandardScalingAlgorithm.ScaleDown == ScalingAlgorithm.Spline16;
            spline36NormalToolStripMenuItem.Checked = StandardScalingAlgorithm.ScaleDown == ScalingAlgorithm.Spline36;
            spline64NormalToolStripMenuItem.Checked = StandardScalingAlgorithm.ScaleDown == ScalingAlgorithm.Spline64;
            /*
#if !AUTOBUILD
            antiMoire1ToolStripMenuItem.Checked = StandardScalingAlgorithm.ScaleDown == ScalingAlgorithm.AntiMoire1;
            antiMoire2ToolStripMenuItem.Checked = StandardScalingAlgorithm.ScaleDown == ScalingAlgorithm.AntiMoire2;
            antiMoire3ToolStripMenuItem.Checked = StandardScalingAlgorithm.ScaleDown == ScalingAlgorithm.AntiMoire3;
            antiMoire4ToolStripMenuItem.Checked = StandardScalingAlgorithm.ScaleDown == ScalingAlgorithm.AntiMoire4;
#endif
            */
        }

        int standardScalingAlgorithm_BenchRequest = -1;

        private void setStandardScalingAlgorithm(ScalingAlgorithm algorithm)
        {
            var MSR =
                KeyboardShortcut.GetKeyState(Keys.M) &&
                KeyboardShortcut.GetKeyState(Keys.S) &&
                KeyboardShortcut.GetKeyState(Keys.R);
            if (ResizeGammaNormal != GammaConversion.Value1_0 && isIndividuallyImplemented(algorithm) &&
                !isIndividuallyImplemented(StandardScalingAlgorithm.ScaleDown))
            {
                MessageBox.Show(this, Message.LinearizeColorSpaceDescription.Replace("\\n", "\n"),
                    Message.Information, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            if (MSR)
            {
                StandardScalingAlgorithm.ScaleDown = algorithm;
                if (!useAreaAverageWhenUpsizingNormalToolStripMenuItem.Checked)
                    StandardScalingAlgorithm.ScaleUp = algorithm;
                standardScalingAlgorithm_BenchRequest = currentPage;
                reloadForChangeFilter(forOnlyPostFilter: true);
            }
            else
            {
                if (StandardScalingAlgorithm.ScaleDown != algorithm)
                {
                    StandardScalingAlgorithm.ScaleDown = algorithm;
                    if (!useAreaAverageWhenUpsizingNormalToolStripMenuItem.Checked)
                        StandardScalingAlgorithm.ScaleUp = algorithm;
                    reloadForChangeFilter(forOnlyPostFilter: true);
                }
            }
        }

        private void defaultScalingAlgorithmNormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setStandardScalingAlgorithm(ScalingAlgorithm.Default);
        }

        private void default2ScalingAlgorithmNormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setStandardScalingAlgorithm(ScalingAlgorithm.HighSpeed);
        }

        private void nearestNeighborNormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setStandardScalingAlgorithm(ScalingAlgorithm.NearestNeighbor);
        }

        private void areaAverageNormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setStandardScalingAlgorithm(ScalingAlgorithm.AreaAverage);
        }

        private void lanczos1NormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setStandardScalingAlgorithm(ScalingAlgorithm.Lanczos1);
        }

        private void lanczos2NormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setStandardScalingAlgorithm(ScalingAlgorithm.Lanczos2);
        }

        private void lanczos3NormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setStandardScalingAlgorithm(ScalingAlgorithm.Lanczos3);
        }

        private void lanczos4NormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setStandardScalingAlgorithm(ScalingAlgorithm.Lanczos4);
        }

        private void spline4NormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setStandardScalingAlgorithm(ScalingAlgorithm.Spline4);
        }

        private void spline16NormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setStandardScalingAlgorithm(ScalingAlgorithm.Spline16);
        }

        private void spline36NormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setStandardScalingAlgorithm(ScalingAlgorithm.Spline36);
        }

        private void spline64NormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setStandardScalingAlgorithm(ScalingAlgorithm.Spline64);
        }

        /*
        private void antiMoire1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
#if !AUTOBUILD
            setStandardScalingAlgorithm(ScalingAlgorithm.AntiMoire1);
#endif
        }

        private void antiMoire2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
#if !AUTOBUILD
            setStandardScalingAlgorithm(ScalingAlgorithm.AntiMoire2);
#endif
        }

        private void antiMoire3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
#if !AUTOBUILD
            setStandardScalingAlgorithm(ScalingAlgorithm.AntiMoire3);
#endif
        }

        private void antiMoire4ToolStripMenuItem_Click(object sender, EventArgs e)
        {
#if !AUTOBUILD
            setStandardScalingAlgorithm(ScalingAlgorithm.AntiMoire4);
#endif
        }
        */

        private void linearizeColorSpaceforGamma22NormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isIndividuallyImplemented(StandardScalingAlgorithm.ScaleDown))
            {
                if (ResizeGammaNormal == GammaConversion.Value2_2)
                {
                    ResizeGammaNormal = GammaConversion.Value1_0;
                }
                else
                {
                    ResizeGammaNormal = GammaConversion.Value2_2;
                    MessageBox.Show(this, Message.LinearizeColorSpaceDescription.Replace("\\n", "\n"),
                        Message.Information, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (useAreaAverageWhenUpsizingNormalToolStripMenuItem.Checked) reloadForChangeFilter(forOnlyPostFilter: true);
            }
            else
            {
                if (ResizeGammaNormal == GammaConversion.Value2_2)
                {
                    ResizeGammaNormal = GammaConversion.Value1_0;
                }
                else
                {
                    ResizeGammaNormal = GammaConversion.Value2_2;
                }
                reloadForChangeFilter(forOnlyPostFilter: true);
            }
        }
        
        private void useAreaAverageWhenUpsizingNormalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (useAreaAverageWhenUpsizingNormalToolStripMenuItem.Checked)
            {
                useAreaAverageWhenUpsizingNormalToolStripMenuItem.Checked = false;
                if (StandardScalingAlgorithm.ScaleUp != StandardScalingAlgorithm.ScaleDown)
                {
                    StandardScalingAlgorithm.ScaleUp = StandardScalingAlgorithm.ScaleDown;
                    reloadForChangeFilter(forOnlyPostFilter: true);
                    if (ViewerMode == ViewerModeEnum.Normal)
                    {
                        reloadForChangeFilter(forOnlyPostFilter: true);
                    }
                }
            }
            else
            {
                useAreaAverageWhenUpsizingNormalToolStripMenuItem.Checked = true;
                if (StandardScalingAlgorithm.ScaleUp != ScalingAlgorithm.AreaAverage)
                {
                    StandardScalingAlgorithm.ScaleUp = ScalingAlgorithm.AreaAverage;
                    reloadForChangeFilter(forOnlyPostFilter: true);
                    if (ViewerMode == ViewerModeEnum.Normal)
                    {
                        reloadForChangeFilter(forOnlyPostFilter: true);
                    }
                }
            }
        }

        private void scalingAlgorithmMagnifierToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            defaultMagnifierToolStripMenuItem.Checked = MagnifierScalingAlgorithm.ScaleDown == ScalingAlgorithm.HighSpeed;
            default0MagnifierToolStripMenuItem.Checked = MagnifierScalingAlgorithm.ScaleDown == ScalingAlgorithm.Default;
            nearestNeighborMagnifierToolStripMenuItem.Checked = MagnifierScalingAlgorithm.ScaleDown == ScalingAlgorithm.NearestNeighbor;
            areaAverageMagnifierToolStripMenuItem.Checked = MagnifierScalingAlgorithm.ScaleDown == ScalingAlgorithm.AreaAverage;
            lanczos1MagnifierToolStripMenuItem.Checked = MagnifierScalingAlgorithm.ScaleDown == ScalingAlgorithm.Lanczos1;
            lanczos2MagnifierToolStripMenuItem.Checked = MagnifierScalingAlgorithm.ScaleDown == ScalingAlgorithm.Lanczos2;
            lanczos3MagnifierToolStripMenuItem.Checked = MagnifierScalingAlgorithm.ScaleDown == ScalingAlgorithm.Lanczos3;
            lanczos4MagnifierToolStripMenuItem.Checked = MagnifierScalingAlgorithm.ScaleDown == ScalingAlgorithm.Lanczos4;
            spline4MagnifierToolStripMenuItem.Checked = MagnifierScalingAlgorithm.ScaleDown == ScalingAlgorithm.Spline4;
            spline16MagnifierToolStripMenuItem.Checked = MagnifierScalingAlgorithm.ScaleDown == ScalingAlgorithm.Spline16;
            spline36MagnifierToolStripMenuItem.Checked = MagnifierScalingAlgorithm.ScaleDown == ScalingAlgorithm.Spline36;
            spline64MagnifierToolStripMenuItem.Checked = MagnifierScalingAlgorithm.ScaleDown == ScalingAlgorithm.Spline64;
        }

        private void setMagnifierScalingAlgorithm(ScalingAlgorithm algorithm)
        {
            if (ResizeGammaMagnifier != GammaConversion.Value1_0 && isIndividuallyImplemented(algorithm) &&
                !isIndividuallyImplemented(MagnifierScalingAlgorithm.ScaleDown))
            {
                MessageBox.Show(this, Message.LinearizeColorSpaceDescription.Replace("\\n", "\n"),
                    Message.Information, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            if (MagnifierScalingAlgorithm.ScaleDown != algorithm)
            {
                MagnifierScalingAlgorithm.ScaleDown = algorithm;
                if (!useAreaAverageWhenUpsizingNormalToolStripMenuItem.Checked)
                    MagnifierScalingAlgorithm.ScaleUp = algorithm;
                //reloadForChangeFilter(forOnlyPostFilter: true);
                resetMagnifyingPower(magnifyingPower);
            }
        }
        private void defaultMagnifierToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setMagnifierScalingAlgorithm(ScalingAlgorithm.HighSpeed);
        }

        private void default0MagnifierToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setMagnifierScalingAlgorithm(ScalingAlgorithm.Default);
        }

        private void nearestNeighborMagnifierToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setMagnifierScalingAlgorithm(ScalingAlgorithm.NearestNeighbor);
        }

        private void areaAverageMagnifierToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setMagnifierScalingAlgorithm(ScalingAlgorithm.AreaAverage);
        }

        private void lanczos1MagnifierToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setMagnifierScalingAlgorithm(ScalingAlgorithm.Lanczos1);
        }

        private void lanczos2MagnifierToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setMagnifierScalingAlgorithm(ScalingAlgorithm.Lanczos2);
        }

        private void lanczos3MagnifierToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setMagnifierScalingAlgorithm(ScalingAlgorithm.Lanczos3);
        }

        private void lanczos4MagnifierToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setMagnifierScalingAlgorithm(ScalingAlgorithm.Lanczos4);
        }

        private void spline4MagnifierToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setMagnifierScalingAlgorithm(ScalingAlgorithm.Spline4);
        }

        private void spline16MagnifierToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setMagnifierScalingAlgorithm(ScalingAlgorithm.Spline16);
        }

        private void spline36MagnifierToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setMagnifierScalingAlgorithm(ScalingAlgorithm.Spline36);
        }

        private void spline64MagnifierToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setMagnifierScalingAlgorithm(ScalingAlgorithm.Spline64);
        }

        private void linearizeColorSpaceforGamma22MagnifierToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (isIndividuallyImplemented(MagnifierScalingAlgorithm.ScaleDown))
            {
                if (ResizeGammaMagnifier == GammaConversion.Value2_2)
                {
                    ResizeGammaMagnifier = GammaConversion.Value1_0;
                }
                else
                {
                    ResizeGammaMagnifier = GammaConversion.Value2_2;
                    MessageBox.Show(this, Message.LinearizeColorSpaceDescription.Replace("\\n", "\n"),
                        Message.Information, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (useAreaAverageWhenUpsizingToolStripMenuItem.Checked) resetMagnifyingPower(magnifyingPower);
            }
            else
            {
                if (ResizeGammaMagnifier == GammaConversion.Value2_2)
                {
                    ResizeGammaMagnifier = GammaConversion.Value1_0;
                }
                else
                {
                    ResizeGammaMagnifier = GammaConversion.Value2_2;
                }
                resetMagnifyingPower(magnifyingPower);
            }
        }

        private void useAreaAverageWhenUpsizingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (useAreaAverageWhenUpsizingToolStripMenuItem.Checked)
            {
                useAreaAverageWhenUpsizingToolStripMenuItem.Checked = false;
                if (MagnifierScalingAlgorithm.ScaleUp != MagnifierScalingAlgorithm.ScaleDown)
                {
                    MagnifierScalingAlgorithm.ScaleUp = MagnifierScalingAlgorithm.ScaleDown;
                    resetMagnifyingPower(magnifyingPower);
                }
            }
            else
            {
                useAreaAverageWhenUpsizingToolStripMenuItem.Checked = true;
                if (MagnifierScalingAlgorithm.ScaleUp != ScalingAlgorithm.AreaAverage)
                {
                    MagnifierScalingAlgorithm.ScaleUp = ScalingAlgorithm.AreaAverage;
                    resetMagnifyingPower(magnifyingPower);
                }
            }
        }

        Point? normalStateLocation = null;
        //FormWindowState ViewerForm_LocationChanged_LastWindowState = FormWindowState.Normal;
        //Point? ViewerForm_LocationChanged_LastScreenOrigin = null;
        private void ViewerForm_LocationChanged(object sender, EventArgs e)
        {
            var ws = WindowState;

            if (ws == FormWindowState.Normal)
            {
                normalStateLocation = Location;
            }
            /*
            var screen = Screen.FromControl(this).Bounds.Location;
            if (ws == FormWindowState.Maximized && ViewerForm_LocationChanged_LastWindowState == FormWindowState.Maximized &&
                ViewerForm_LocationChanged_LastScreenOrigin != null && ViewerForm_LocationChanged_LastScreenOrigin != screen && normalStateLocation != null)
            {
                var nsl = (Point)normalStateLocation;
                var pre = (Point)ViewerForm_LocationChanged_LastScreenOrigin;
                normalStateLocation = new Point(nsl.X + screen.X - pre.X, nsl.Y + screen.Y - pre.Y);

                this.screen
            }

            ViewerForm_LocationChanged_LastWindowState = ws;
            ViewerForm_LocationChanged_LastScreenOrigin = screen;
            */
        }

        private void alwaysAutoHideUIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toggleAlwaysHideUI();
        }

        private void toggleAlwaysHideUI()
        {
            AlwaysHideUI = !AlwaysHideUI;
            ShowViewSettingHint();
        }

        class ToolStripMenuItemWithPath : ToolStripMenuItem
        {
            public ToolStripMenuItemWithPath(string text, string sorPath, string targetLongPath) : base(text.Replace("&", "&&"))
            {
                SorPath = sorPath;
                TargetLongPath = targetLongPath;
            }
            public string SorPath;
            public string TargetLongPath;
        }

        private void virtualFoldersToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            var items = virtualFoldersToolStripMenuItem.DropDownItems;
            items.Clear();

            var config = new ColoredBookmarkConfig();

            var ex = config.GetLastException();
            if (ex != null)
            {
                MessageBox.Show(this, ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var bookmarks = config.Bookmarks;
            var bookmarkColors = config.BookmarkColors;
            List<ToolStripItem> newItems = new List<ToolStripItem>();
            try
            {
                if (bookmarks != null && bookmarks.Length == bookmarkColors?.Length)
                {
                    var c = currentPage;
                    var ea = EntryLongPathArray;
                    string targetLongPath = null;
                    if (ea != null && c >= 0 && c < ea.Length)
                    {
                        var isEntity = loader?.Type == PackedImageLoader.PackType.Directory;
                        targetLongPath = ea[c];
                        if (targetLongPath == "") targetLongPath = null;
                        if (targetLongPath != null && isEntity)
                        {
                            targetLongPath = targetLongPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                        }
                    }

                    for (var i = 0; i < bookmarks.Length; i++)
                    {
                        var bookmark = bookmarks[i];
                        if (bookmark.SpecialRole == SimpleBookmarkSpecialRole.None)
                        {
                            if (bookmark.Location is string path && Path.GetExtension(path).ToLower() == ".sor" && File.Exists(path))
                            {
                                if (targetLongPath != null)
                                {
                                    //var tsmi = new ToolStripMenuItemWithPath(Message.AddTo1.Replace("$1", Path.GetFileName(path)), path, targetLongPath);
                                    var tsmi = new ToolStripMenuItemWithPath(Message.AddTo1.Replace("$1", bookmark.ToString()), path, targetLongPath);
                                    tsmi.Click += virtualFolderSorToolStripMenuItem_Click;
                                    tsmi.BackColor = bookmarkColors[i];
                                    newItems.Add(tsmi);
                                }
                                else
                                {
                                    var tsmi = new ToolStripMenuItem(Message.AddTo1.Replace("$1", Path.GetFileName(path)));
                                    tsmi.Enabled = false;
                                    newItems.Add(tsmi);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex2)
            {
                items.Clear();
#if DEBUG
                MessageBox.Show(this, ex2.ToString(), null, MessageBoxButtons.OK, MessageBoxIcon.Error);
#else
                MessageBox.Show(this, ex2.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif
                return;
            }

            if (newItems.Count > 0)
            {
                var newItemsArray = newItems.ToArray();
                ForeColorManager.Set(newItemsArray);
                items.AddRange(newItemsArray);
            }
            else
            {
                var item = new ToolStripMenuItem(Message.NoVirtualFoldersInBookmark);
                item.Enabled = false;
                items.Add(item);
            }

            ResetToolStripMenuItem(sender);
            SetMessageForwarderStop(true);
        }

        private static void ResetToolStripMenuItem(object sender)
        {
            var tsmi = (ToolStripMenuItem)sender;
            var dropDown = tsmi.DropDown;
            var maxSize = dropDown.MaximumSize;
            var owner = tsmi.Owner;
            var area = Screen.FromControl(owner).WorkingArea;
            var buttonBound = owner.RectangleToScreen(tsmi.Bounds);
            dropDown.MaximumSize = new Size(maxSize.Width, Math.Max(buttonBound.Height * 2 * SystemInformation.MouseWheelScrollLines, area.Bottom - (buttonBound.Bottom - 1)));
        }

        private void virtualFolderSorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var item = sender as ToolStripMenuItemWithPath;
            var sorPath = item.SorPath;
            var targetLongPath = item.TargetLongPath;
            var altPos = targetLongPath.IndexOf(Path.AltDirectorySeparatorChar);

            try
            {
                // 実体を登録するパターン

                // ページ指定は LongPath を使うかページ番号を使うか検討中
                // ページ番号を指定してサムネイルは変更しないのが最もシンプルな方法
                // サムネイルを変更する場合必然的にキャッシュの検討が必要になる
                // すなわち、キャッシュは諦めるか、特別な方法を使うか、複数キャッシュが格納できるように拡張するか
                // しかし一ファイル一キャッシュを崩すことによるデメリットは多大
                if (altPos >= 0) targetLongPath = targetLongPath.Substring(0, altPos);
                VirtualFolder.AddBookmarkData(sorPath, targetLongPath, page: -1, limitOfItemsCount: int.MaxValue, deleteLostPath: false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void optionToolStripMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            SetMessageForwarderStop(true);
        }

        private void readOnMemoryNoneToolStripMenuItem_Click(object sender, EventArgs e) => SetReadOnMemoryModeExceptLookAhead();

        private void SetReadOnMemoryModeNone() => changeReadOnMemoryMode(ReadOnMemoryMode.None);
        
        private void readOnMemoryExceptLookAheadToolStripMenuItem_Click(object sender, EventArgs e) => SetReadOnMemoryModeExceptLookAhead();

        private void SetReadOnMemoryModeExceptLookAhead() => changeReadOnMemoryMode(ReadOnMemoryMode.ExceptLookAhead);

        private void readOnMemoryAlwaysToolStripMenuItem_Click(object sender, EventArgs e) => SetReadOnMemoryModeAlways();

        private void SetReadOnMemoryModeAlways() => changeReadOnMemoryMode(ReadOnMemoryMode.Always);

        private void changeReadOnMemoryMode(ReadOnMemoryMode mode)
        {
            if (mode != ReadOnMemoryMode)
            {
                ReadOnMemoryMode = mode;
                ResetAndShowHintForChangeReadingMode();
            }
        }
        
        private void nonerecommendedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetArchivesInArchiveModeIgnore();
        }

        private void SetArchivesInArchiveModeIgnore()
        {
            changeArchivesInArchiveMode(ArchivesInArchiveMode.Ignore);
        }

        private void untilFoundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetArchivesInArchiveModeUntilFound2Level();
        }

        private void SetArchivesInArchiveModeUntilFound2Level()
        {
            changeArchivesInArchiveMode(ArchivesInArchiveMode.UntilFound2Level);
        }
        
        private void alwaysLoadArchivesInArchiveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetArchivesInArchiveModeAlways1Level();
        }

        private void SetArchivesInArchiveModeAlways1Level()
        {
            changeArchivesInArchiveMode(ArchivesInArchiveMode.Always1Level);
        }

        private void changeArchivesInArchiveMode(ArchivesInArchiveMode mode)
        {
            if (mode != ArchivesInArchiveMode)
            {
                ArchivesInArchiveMode = mode;
                ResetAndShowHintForChangeReadingMode();
            }
        }

        private void ResetAndShowHintForChangeReadingMode()
        {
            if (loader != null)
            {
                loader.Dispose();
                loader = null;
            }
            if (!string.IsNullOrEmpty(currentArchiveFilePath))
            {
                var ea = EntryArray;
                var cp = currentPage;
                var path = null as string;
                if (cp >= 0 && ea != null && ea.Length > cp)
                {
                    var e = ea[cp];
                    if (e != null)
                    {
                        path = e.Path;
                        if (path == "") path = null;
                    }
                }
                OpenFile(currentArchiveFilePath, 0, currentSortModeDetails, path);
            }
            ShowViewSettingHint();
        }

        private void selectBackgroundColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SelectBackgroundColor();
        }

        private void SelectBackgroundColor()
        {
            using (var cd = new ColorDialog())
            {
                var cc = cd.CustomColors;
                var current = pbView.BackColor;
                cc[cc.Length - 1] = current.R | current.G << 8 | current.B << 16;
                cd.CustomColors = cc;

                cd.Color = current;
                cd.AllowFullOpen = true;
                cd.SolidColorOnly = false;
                if (cd.ShowDialog(this) == DialogResult.OK)
                {
                    var newColor = cd.Color;
                    if (current.R != newColor.R || current.G != newColor.G || current.B != newColor.B)
                    {
                        pbView.BackColor = newColor;
                        ShowViewSettingHint();
                    }
                }
            }
        }

        private void forceFirstPageToBeSingleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToggleForceFirstPageToBeSingle();
        }

        private int tiMemoryReducer_Tick_100msCounter = 0;
        private void tiMemoryReducer_Tick(object sender, EventArgs e)
        {
            if (!ViewerForm_Shown_Called) return;

            if (tiMemoryReducer_Tick_100msCounter >= 4)
            {
                tiMemoryReducer_Tick_100msCounter = 0;
                SetMemoryUBound();
                ReduceUsingMemory(usedMemoryUBound);
            }
            else
            {
                tiMemoryReducer_Tick_100msCounter++;
            }

            // ToolStripTextBox の MouseEnter の隙間を埋める
            hideUnderUIForMouseLeave(sender, e);
        }

        private void createViewerShortcutFromCurrentSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CreateViewerShortcutFromCurrentSettings();
        }

        private void CreateViewerShortcutFromCurrentSettings()
        {
            string currentPath;
            try
            {
                var ld = loader;
                if (ld == null)
                {
                    currentPath = null;
                }
                else
                {
                    currentPath = currentArchiveFilePath;
                    if (!string.IsNullOrEmpty(currentPath) && ld.Type != PackedImageLoader.PackType.Directory)
                    {
                        currentPath = Path.GetDirectoryName(currentPath);
                    }
                }
            }
            catch
            {
                currentPath = null;
            }
            
            using (var makeShortcutForm = new ViewerFormMakeShortcutDialog(CreateCommandLineOptionInfo(), currentPath))
            {
                makeShortcutForm.ShowDialog(this);
            }
        }

        CommandLineOptionInfo CreateCommandLineOptionInfo() => new CommandLineOptionInfo
        {
            AlwaysHideUI = AlwaysHideUI,
            ArchivesInArchiveMode = ArchivesInArchiveMode,
            BackColor = pbView.BackColor,
            CoverBindingMode = CoverBindingMode,
            DefaultBindingMode = BindingMode,
            ReadOnMemoryMode = ReadOnMemoryMode,
            OpenInPreviousImageFilterSetting = false,
            InitialFullscreenMode = InitialFullscreenMode.Default
        };

        private void mtbPage_VisibleChanged(object sender, EventArgs e)
        {
            if (mtbPage.Visible)
            {
                mtbPage.ValueCanChangedByMouseButton = false;
            }
        }

        private void mtbPage_MouseDown(object sender, MouseEventArgs e)
        {
            mtbPage.ValueCanChangedByMouseButton = true;
        }

        private void btnOpenLeftRight_MouseMove(object sender, MouseEventArgs e)
        {
            mtbPage.ValueCanChangedByMouseButton = true;
        }

        private void showAHintWhenChangingSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ViewSettingHintMode = ViewSettingHintMode == ViewSettingHintMode.ShowWhenChangingSettings ? ViewSettingHintMode.NeverShow : ViewSettingHintMode.ShowWhenChangingSettings;
        }

        /*
        private void ViewerForm_Deactivate(object sender, EventArgs e)
        {
            //tmMemoryReducer.Enabled = true;
        }
        */

        private void ToggleForceFirstPageToBeSingle()
        {
            CoverBindingMode = CoverBindingMode == CoverBindingMode.Default ? CoverBindingMode.ForceSingle : CoverBindingMode.Default;

            if (currentPage == 0)
            {
                if (CoverBindingMode == CoverBindingMode.ForceSingle)
                {
                    movePageToMinimulBack();
                }
                else
                {
                    onePageModeForNext = false;
                    showCurrentPage();
                }
            }

            ShowViewSettingHint();
        }

        private void virtualFoldersToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
        {
            ContextMenuForMenuStrip_Opend = true;
            fixDropDownLocation(sender);
        }

        private void virtualFoldersToolStripMenuItem_DropDownClosed(object sender, EventArgs e)
        {
            ContextMenuForMenuStripPostProcessing2();
        }

        /*
        private void menuStrip_MouseMove(object sender, MouseEventArgs e)
        {
            //if (!menuStrip.Visible)
            {
                var newPoint = menuStrip.PointToClient(pbView.PointToScreen(e.Location));
                pbView_MouseMove(pbView, new MouseEventArgs(e.Button, e.Clicks, newPoint.X,newPoint.Y, e.Delta));
            }
        }
        */
    }

    /*
    class myTrackBarMouseWheelEventArgs : MouseEventArgs
    {
        public bool Handled = false;
        public int Value;
        public myTrackBarMouseWheelEventArgs(MouseEventArgs e) : base(e.Button, e.Clicks, e.X, e.Y, e.Delta)
        {
        }
    }
    delegate void myTrackBarMouseWheelEventHandler(myTrackBarOld sender, myTrackBarMouseWheelEventArgs e);

    class myTrackBarOld : TrackBar
    {
        public new event myTrackBarMouseWheelEventHandler MouseWheel;

        private int ValueForced;
        private readonly double DisplayMagnificationX;
        private readonly double DisplayMagnificationY;
        public myTrackBarOld()
        {
            ValueForced = Minimum - 1;
            using (var g = CreateGraphics())
            {
                DisplayMagnificationX = g.DpiX / 96.0;
                DisplayMagnificationY = g.DpiY / 96.0;
            }

            Maximum = base.Maximum;
        }
        
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            //var delta = e.Delta / 120;
            if(MouseWheel != null)
            {
                var e2 = new myTrackBarMouseWheelEventArgs(e);
                MouseWheel(this, e2);
                if(e2.Handled)
                {
                    ValueForced = Math.Max(Minimum, Math.Min(Maximum, e2.Value));
                }
            }

            base.OnMouseWheel(e);
        }

        protected override void OnValueChanged(EventArgs e)
        {
            if (ValueForced >= Minimum)
            {
                Value = ValueForced;
            }
            ValueForced = Minimum - 1;

            if (maximum != base.Maximum)
            {
                int baseValue;
                if (valueRightToLeft)
                {
                    baseValue = maximum + 1;
                }
                else
                {
                    baseValue = maximum;
                }
                if (baseValue != base.Value) base.Value = baseValue;
            }


            base.OnValueChanged(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Value = PointToValue(e.Location);
            }
            base.OnMouseDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Up || e.KeyCode == Keys.Down || e.KeyCode == Keys.Left || e.KeyCode == Keys.Right)
            {
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        public int PointToValue(Point clientCoordinate)
        {
            int x = clientCoordinate.X, y = clientCoordinate.Y;
            var margin = 5 + 8 * DisplayMagnificationX;
            var right = Width - margin - 1;
            int result;
            if (x <= margin)
            {
                result = Minimum;
            }
            else if (x >= right)
            {
                result = Maximum;
            }
            else
            {
                result =(int)Math.Round((Maximum - Minimum) * (x - margin) / (right - margin) + Minimum);
            }
            if(valueRightToLeft)
            {
                return maximum - (result - Minimum);
            }
            else
            {
                return result;
            }
        }
        
        private bool valueRightToLeft = false;

        /// <summary>
        /// Minimum, Enabled を外部から操作する場合は使えない
        /// </summary>
        public bool ValueRightToLeft
        {
            get { return valueRightToLeft; }
            set
            {
                if(value != valueRightToLeft)
                {
                    var currentValue = Value;
                    valueRightToLeft = value;
                    Value = currentValue;
                }
            }
        }
        public new int Value
        {
            get
            {
                if (maximum == base.Maximum)
                {
                    if (valueRightToLeft)
                    {
                        return Maximum - (base.Value - Minimum);
                    }
                    else
                    {
                        return base.Value;
                    }
                }
                else
                {
                    return maximum;
                }
            }
            set
            {
                if (maximum == base.Maximum)
                {
                    if (valueRightToLeft)
                    {
                        base.Value = Maximum - (value - Minimum);
                    }
                    else
                    {
                        base.Value = value;
                    }
                }
                else
                {
                    if (valueRightToLeft)
                    {
                        base.Value = maximum + 1;
                    }
                    else
                    {
                        base.Value = maximum;
                    }
                }
            }
        }

        private int maximum;
        public new int Maximum
        {
            get
            {
                return maximum;
            }
            set
            {
                if(value > Minimum)
                {
                    base.Maximum = maximum = value;
                    //Enabled = true;
                }
                else
                {
                    maximum = value;
                    var min = Minimum;
                    var max = min + 1;
                    base.Maximum = max;
                    base.Value = valueRightToLeft ? max : min;
                    //Enabled = false;
                }
            }
        }
        
    }

    */

    public class ViewControl : Control, ISupportInitialize
    {

        public ViewControl() : base()
        {
            // ダブルバッファリングによる描画の高速化
            
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            
            SetStyle(ControlStyles.Opaque, true);

        }

        /*
        public void PerformPaint()
        {
            using (var g = CreateGraphics())
            {
                OnPaint(new PaintEventArgs(g, DisplayRectangle));
            }
        }

        public new void Invalidate(bool dummy)
        {
            PerformPaint();
        }
        */

#region Paint 中にスレッドの譲渡が起こっても Paint が二重で動作することがないようにする実装
        bool invalidating = false;
        bool painting = false;
        bool reserveInvalidate = false;
        bool reserveInvalidateWithChildren = false;
        public new void Invalidate(bool invalidateChildren)
        {
            if (painting)
            {
                reserveInvalidate = true;
                reserveInvalidateWithChildren = reserveInvalidateWithChildren && invalidateChildren;
                return;
            }
            if (invalidating) return;

            invalidating = true;
            base.Invalidate(invalidateChildren);
        }
        public new void Invalidate()
        {
            Invalidate(false);
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            // ここで引っかかった場合一瞬画面が暗くなるが
            // 画像を表示するだけならダブルバッファリングによりそのようなことはほとんど起こらない
            if (painting)
            {
                reserveInvalidate = true;
                return;
            }

            painting = true;

            base.OnPaint(e);

            painting = false;
            invalidating = false;

            if (reserveInvalidate)
            {
                reserveInvalidate = false;
                var invalidateChildren = reserveInvalidateWithChildren;
                reserveInvalidateWithChildren = false;
                Invalidate(invalidateChildren);
            }
        }
#endregion

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
           //base.OnPaintBackground(pevent);
        }


#region ISupportInitialize
        //private bool initializing = false;
        public void BeginInit()
        {
            //initializing = true;
        }

        public void EndInit()
        {
            //initializing = false;
        }
#endregion
    }

    class ExtendedToolStripMenuItem : ToolStripMenuItem
    {
        private ExtendedToolStripMenuItem(ToolStripMenuItem toolStripMenuItem)
        {
            var ownerItems = toolStripMenuItem.Owner.Items;

            var index = ownerItems.IndexOf(toolStripMenuItem);

            DropDownItems.AddRange((from ToolStripItem item in toolStripMenuItem.DropDownItems select item).ToArray());

            ownerItems.RemoveAt(index);
            ownerItems.Insert(index, this);
            
        }

        public static void Replace(ref ToolStripMenuItem toolStripMenuItem)
        {
            toolStripMenuItem = new ExtendedToolStripMenuItem(toolStripMenuItem);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool MoveWindow( IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        protected override void OnDropDownOpened(EventArgs e)
        {
            var bounds = Bounds;
            var point = Owner.PointToScreen(new Point(bounds.Left, bounds.Bottom - 1));
            var dropDownLocation = DropDownLocation;

            if (point != dropDownLocation)
            {
                var dropDown = DropDown;
                
                MoveWindow(DropDown.Handle, point.X, point.Y, dropDown.Width, dropDown.Height, true);
            }
            
            base.OnDropDownOpened(e);
        }
    }

    /*
    class ContextMenuForMenuStrip : ContextMenu
    {
        public bool CancelClose = false;

        public bool CloseCancelable = false;

        private bool waiting = false;

        public event EventHandler DropDownClosed;
        
        public async void Show(ToolStripMenuItem toolStripMenuItem)
        {
            if (waiting) return;

            var bounds = toolStripMenuItem.Bounds;
            var parent = toolStripMenuItem.GetCurrentParent();
            var parentBounds = parent.Bounds;

            if (CloseCancelable)
            {
                do
                {
                    CancelClose = false;
                    Show(parent, new Point(bounds.Left, parentBounds.Height));
                    Application.DoEvents();
                }
                while (CancelClose);
            }
            else
            {
                Show(parent, new Point(bounds.Left, parentBounds.Height));
            }

            DropDownClosed?.Invoke(this, null);

            if (bounds.Contains(parent.PointToClient(Cursor.Position)))
            {
                waiting = true;
                await Task.Run(()=>
                {
                    while (((Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left))
                    {
                        Thread.Sleep(50);
                    }
                });
                waiting = false;
            }
        }
    }
    */

    public enum SeekBarMode { LeftToRight, RightToLeft, SameAsPage, ReverseOfPage }
    public enum ZoomMode { LeftToRight, RightToLeft, SameAsPage, ReverseOfPage }
    public enum ViewSettingHintMode { ShowWhenChangingSettings, NeverShow }
    public enum ReadOnMemoryMode { None, ExceptLookAhead, Always }
    //public enum MagnifierScrollMode { Location, Variation }

    public enum ScalingAlgorithm { Default, HighSpeed, NearestNeighbor, AreaAverage, Lanczos1, Lanczos2, Lanczos3, Lanczos4,
        Spline4, Spline16, Spline36, Spline64
#if !AUTOBUILD
            //, AntiMoire1, AntiMoire2, AntiMoire3, AntiMoire4
#endif
    }
    public struct ScalingAlgorithmPair
    {
        public ScalingAlgorithm ScaleUp;
        public ScalingAlgorithm ScaleDown;
    }

    public class ViewerFormConfig : Configuration
    {
        public bool FullScreen;
        public bool Maximized;
        public Point? Point;
        public Size Size;
        public int MaximizedSizeWidthMargin;
        public int MaximizedSizeHeightMargin;
        public int MaximizedSizeHeightMarginInHideMode;

        public ArchivesInArchiveMode ArchivesInArchiveMode = ArchivesInArchiveMode.Ignore;

        public CoverBindingMode CoverBindingMode = CoverBindingMode.Default;

        public bool AlwaysHideUI = false;
        public SerializableRGB BackColor = Color.Black;

        public ScalingAlgorithm NormalScalingAlgorithm = ScalingAlgorithm.Default;
        public double ResizeGammaNormal = 1.0;
        public bool UseAreaAverageWhenNormalUpsizing = false;
        public ScalingAlgorithm MagnifierScalingAlgorithm = ScalingAlgorithm.HighSpeed;
        public double ResizeGammaMagnifier = 1.0;
        public bool UseAreaAverageWhenMagnifierUpsizing = false;

        public int MinPageCountInWindow = 1;
        public int MaxDivision = 1;

        public SeekBarMode SeekBarMode = SeekBarMode.LeftToRight;

        public int SlideshowInterval = 5000;
        public bool SlideshowRepeat = true;
        public bool SlideshowGlobal = false;

        public bool MouseGestureEnabled = false;
        public double MouseGestureLineWidth = 5;
        public SerializableRGB MouseGestureLineColor = Color.Red;
        public ViewerForm.MouseGestureCommand[] MouseGestureCommands = null;

        public ReadOnMemoryMode ReadOnMemoryMode = ReadOnMemoryMode.None;

        public ViewerFormImageFilter ImageFilter = null;

        public ViewSettingHintMode ViewSettingHintMode = ViewSettingHintMode.ShowWhenChangingSettings;

        private ViewerForm.KeyboardShortcutCommand[] keyboardShortcutCommands;
        public ViewerForm.KeyboardShortcutCommand[] KeyboardShortcutCommands
        {
            get
            {
                return GetSupplementCommands(keyboardShortcutCommands);
            }
            set
            {
                keyboardShortcutCommands = value;
            }
        }

        public static readonly ViewerForm.KeyboardShortcutCommand[] DefaultKeyboardShortcutCommands = new ViewerForm.KeyboardShortcutCommand[]
        {
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.OpenFile, Keys.ControlKey, Keys.O),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.OpenThumbnailWindow, Keys.ControlKey, Keys.T),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.SwitchToThumbnailWindow, Keys.ControlKey, Keys.ShiftKey, Keys.T),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.FullScreen, Keys.ControlKey, Keys.ShiftKey, Keys.F),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.Window, Keys.Escape),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.ToggleFullScreenMode, Keys.F11),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.Exit, Keys.ControlKey, Keys.W),
            
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.ToggleMagnifier, Keys.MButton),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.ToggleMagnifier, Keys.Z),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.ToggleRotateLeft, Keys.ControlKey, Keys.L),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.ToggleRotateRight, Keys.ControlKey, Keys.R),
            //new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.ToggleToneCurve, Keys.M),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.ToggleToneCurve, Keys.C),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.LeftToRight, Keys.L),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.RightToLeft, Keys.R),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.SinglePage, Keys.S),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.SinglePageWithoutScalingUp, Keys.D),

            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.ToggleForceFirstPageToBeSingle, Keys.F),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.ToggleMinPageCountInWindow, Keys.W),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.ToggleMaxDivision, Keys.V),

            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.ToggleAlwaysHideUI, Keys.H),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.ToggleSeekBarMode, Keys.B),

            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.SelectBackgroundColor, Keys.ControlKey, Keys.ShiftKey, Keys.C),

            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.NextPage, ExtendedKeys.WheelDown),
            //new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.NextPage, Keys.Right),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.PreviousPage, ExtendedKeys.WheelUp),
            //new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.PreviousPage, Keys.Left),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.RightPage, ExtendedKeys.LButtonOnRight),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.LeftPage, ExtendedKeys.LButtonOnLeft),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.MoveForwardOnePage, Keys.Down),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.MoveBackOnePage, ExtendedKeys.LButtonOnCenter),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.MoveBackOnePage, Keys.Up),

            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.OpenNext, Keys.XButton2),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.OpenNext, Keys.Menu, Keys.Right),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.OpenPrevious, Keys.XButton1),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.OpenPrevious, Keys.Menu, Keys.Left),

            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.MoveTo0Percent, Keys.Home),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.MoveTo10Percent, Keys.D1),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.MoveTo20Percent, Keys.D2),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.MoveTo30Percent, Keys.D3),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.MoveTo40Percent, Keys.D4),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.MoveTo50Percent, Keys.D5),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.MoveTo60Percent, Keys.D6),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.MoveTo70Percent, Keys.D7),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.MoveTo80Percent, Keys.D8),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.MoveTo90Percent, Keys.D9),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.MoveTo100Percent, Keys.End),
            
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.ToggleSlideshow, Keys.Space),

            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.MoveForwardOrMagnifierAutoScroll, Keys.Right),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.MoveBackOrMagnifierAutoScroll, Keys.Left),


            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.ScreenEffectAtNextPageSide, ExtendedKeys.NextPageAtLastPage),
            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.ScreenEffectAtPreviousPageSide, ExtendedKeys.PreviousPageAtFirstPage),

            new ViewerForm.KeyboardShortcutCommand(ViewerForm.Command.EditMenu, Keys.RButton),

        };

        public static ViewerForm.KeyboardShortcutCommand[] GetSupplementCommands(ViewerForm.KeyboardShortcutCommand[] baseSetting)
        {
            bool dummy;
            return GetSupplementCommands(baseSetting, out dummy);
        }
        public static ViewerForm.KeyboardShortcutCommand[] GetSupplementCommands(ViewerForm.KeyboardShortcutCommand[] baseSetting, out bool changed)
        {
            if (baseSetting == null) baseSetting = new ViewerForm.KeyboardShortcutCommand[0];
            var sup = DefaultKeyboardShortcutCommands.Where(d =>
            {
                // useMouse(d) が呼び出される回数の期待値が 1 を下回る場合 var useMouseD = useMouse(d) を計算しておく方法はよくない
                //return baseSetting.All(r => !(r.Command == d.Command && UseMouse(r) == UseMouse(d)) && !r.ShortcutCompetes(d)); // 位置指定同士は別ボタン扱いで補完される

                //return  baseSetting.All(r => !(r.Command == d.Command && UseMouse(r) == UseMouse(d)) && !r.ShortcutIntersect(d));

                return baseSetting.All(r => !(r.Command == d.Command && UseMouse(r) == UseMouse(d)) && !r.ShortcutWeaklyCompetes(d));
            });
            changed = sup.Any();
            if (changed)
            {
                return baseSetting.Concat(sup).ToArray();
            }
            else
            {
                return baseSetting;
            }
        }
        private static bool UseMouse(ViewerForm.KeyboardShortcutCommand command)
        {
            return UseMouse(command.GetShortcut());
        }
        public static bool UseMouse(IEnumerable<IEnumerable<Keys>> shortcut)
        {
            return shortcut.Any(keys => keys.Any(key => key == Keys.XButton1 || key == Keys.XButton2 || ExtendedKeys.IsLButton(key) ||
            key == Keys.RButton || key == Keys.MButton || key == ExtendedKeys.WheelUp || key == ExtendedKeys.WheelDown));
        }
    }
}

public static class ExclusionControl
{
    public static void TryLock(object obj, Action body)
    {
        bool lockWasTaken = false;
        var temp = obj;
        try
        {
            Monitor.TryEnter(temp, ref lockWasTaken);
            body();
        }
        finally
        {
            if (lockWasTaken)
            {
                Monitor.Exit(temp);
            }
        }
    }


}

public class ToolStripSpringTextBox : ToolStripTextBox
{
    public override Size GetPreferredSize(Size constrainingSize)
    {
        // Use the default size if the text box is on the overflow menu
        // or is on a vertical ToolStrip.
        if (IsOnOverflow || Owner.Orientation == Orientation.Vertical)
        {
            return DefaultSize;
        }

        // Declare a variable to store the total available width as 
        // it is calculated, starting with the display width of the 
        // owning ToolStrip.
        Int32 width = Owner.DisplayRectangle.Width;

        // Subtract the width of the overflow button if it is displayed. 
        if (Owner.OverflowButton.Visible)
        {
            width = width - Owner.OverflowButton.Width -
                Owner.OverflowButton.Margin.Horizontal;
        }

        // Declare a variable to maintain a count of ToolStripSpringTextBox 
        // items currently displayed in the owning ToolStrip. 
        Int32 springBoxCount = 0;

        foreach (ToolStripItem item in Owner.Items)
        {
            // 非表示はカウントしない
            if (!item.Visible) continue;

            // Ignore items on the overflow menu.
            if (item.IsOnOverflow) continue;

            if (item is ToolStripSpringTextBox)
            {
                // For ToolStripSpringTextBox items, increment the count and 
                // subtract the margin width from the total available width.
                springBoxCount++;
                width -= item.Margin.Horizontal;
            }
            else
            {
                // For all other items, subtract the full width from the total
                // available width.
                width = width - item.Width - item.Margin.Horizontal;
            }
        }

        // If there are multiple ToolStripSpringTextBox items in the owning
        // ToolStrip, divide the total available width between them. 
        if (springBoxCount > 1) width /= springBoxCount;

        // If the available width is less than the default width, use the
        // default width, forcing one or more items onto the overflow menu.
        if (width < DefaultSize.Width) width = DefaultSize.Width;

        // Retrieve the preferred size from the base class, but change the
        // width to the calculated width. 
        Size size = base.GetPreferredSize(constrainingSize);
        size.Width = width;
        return size;
    }
}
