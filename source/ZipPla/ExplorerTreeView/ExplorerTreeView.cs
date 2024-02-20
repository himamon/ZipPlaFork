using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WilsonProgramming
{
    public partial class ExplorerTreeView : UserControl
    {
        public ExplorerTreeView()
        {
#if RUNTIME
            ZipPla.Program.RunTimeMeasure.Block("ExplorerTreeView Initialize");
#endif
            InitializeComponent();

            treeWnd.BorderStyle = base.BorderStyle;
#if RUNTIME
            ZipPla.Program.RunTimeMeasure.Block("InitializeComponent");
#endif
        }

        //Form owner;

        bool initialized = false;
        public bool Initialized { get { return initialized; } }
        //private Form owner;
        public void Initialize(string loadingString)
        {
            if (!initialized)
            {
                treeWnd.AfterExpand += treeWnd_AfterExpand;
                treeWnd.BeforeCollapse += treeWnd_BeforeCollapse;

                // Set the TreeView image list to the system image list.
                SystemImageList.SetTVImageList(treeWnd.Handle);
                LoadRootNodes(loadingString);
                initialized = true;

                //owner = FindForm();
            }
        }

        bool TrySelect_Excuting;

        /// <summary>
        /// 開いたパスを返す。\\localhost は マシン名に置き換えるので完全に開いても path と戻り値が異なることがある
        /// </summary>
        /// <param name="path"></param>
        /// <param name="loadingString"></param>
        /// <returns></returns>
        public string TrySelect(string path, string loadingString = null)
        {
            if (TrySelect_Excuting) return null;
            TrySelect_Excuting = true;
            try
            {
                Initialize(loadingString);
                if (string.IsNullOrEmpty(path)) return path;

#if AUTOBUILD
                try
#endif
                {
                    int subPos = 0;
                    TreeNode node = null;

                    if (path.Length < 2 || path.Substring(0, 2) != @"\\")
                    {
                        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

                        if (string.Compare(path, desktopPath, ignoreCase: true) == 0)
                        {
                            treeWnd.SelectedNode = tvwRoot;
                            return path;
                        }

                        desktopPath += Path.DirectorySeparatorChar;


                        // デスクトップ以下
                        if (path.StartsWith(desktopPath, StringComparison.OrdinalIgnoreCase))
                        {
                            node = tvwRoot;
                            subPos = desktopPath.Length;
                        }
                        else
                        {
                            // ユーザーフォルダ
                            var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                            if (string.Compare(path, userFolder, ignoreCase: true) == 0)
                            {
                                if (!tvwRoot.IsExpanded) tvwRoot.Expand(); // ルートは開いて良い
                                try
                                {
                                    treeWnd.SelectedNode = (
                                        from TreeNode n in tvwRoot.Nodes
                                        let item = n.Tag as ShellItem
                                        where string.Compare(item.Path, userFolder, ignoreCase: true) == 0
                                        select n).First();

                                    return path;
                                }
                                catch
                                {
                                    return null;
                                }
                            }

                            if (!tvwRoot.IsExpanded) tvwRoot.Expand(); // ルートは開いて良い

                            var pcName = ShellItem.GetFolderDisplayName(ShellAPI.CSIDL.CSIDL_DRIVES);

                            TreeNode pcNode;
                            try
                            {
                                pcNode = (
                                from TreeNode n in tvwRoot.Nodes
                                let item = n.Tag as ShellItem
                                where item.Path == "" & item.DisplayName == pcName
                                select n).First();
                            }
                            catch
                            {
                                return null;
                            }

                            if (path.Length >= 2 && path.Last() == ':') path += Path.DirectorySeparatorChar;

                            //if (!pcNode.IsExpanded) pcNode.Expand();
                            var pcNodeIsNotExpanded = !pcNode.IsExpanded;
                            if (pcNodeIsNotExpanded) treeWnd.RefreshNode(pcNode); // 閉じている間はフォルダ監視が働かないのでダミーかどうかは無関係
                            node =

                                // PC 仮想フォルダ内にあるもの（ドライブのルートを含む）
                                (
                                    from TreeNode n in pcNode.Nodes
                                    let p = (n.Tag as ShellItem).Path
                                    where string.Compare(path, p, ignoreCase: true) == 0
                                    select n).FirstOrDefault();

                            if (node != null)
                            {
                                if (pcNodeIsNotExpanded) treeWnd.ExpandWithoutRefresh(pcNode);
                                treeWnd.SelectedNode = node;
                                return path;
                            }
                            else
                            {
                                // ユーザーフォルダ以下で PC 仮想フォルダ内から辿れるもの
                                node = (
                                    from TreeNode n in pcNode.Nodes
                                    let p = (n.Tag as ShellItem).Path
                                    where p.Any() && p.Last() != Path.DirectorySeparatorChar && path.StartsWith(p + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                                    select n).FirstOrDefault();

                                if (node != null)
                                {
                                    if (pcNodeIsNotExpanded) treeWnd.ExpandWithoutRefresh(pcNode);
                                    var subPath = (node.Tag as ShellItem).Path;
                                    subPos = subPath.Length + 1;
                                }
                                else
                                {
                                    TreeNode userNode;
                                    try
                                    {
                                        userNode = (
                                            from TreeNode n in tvwRoot.Nodes
                                            let item = n.Tag as ShellItem
                                            where string.Compare(item.Path, userFolder, ignoreCase: true) == 0
                                            select n).First();
                                    }
                                    catch
                                    {
                                        return null;
                                    }
                                    //if (!userNode.IsExpanded) userNode.Expand();
                                    var userNodeIsNotExpanded = !userNode.IsExpanded;
                                    if (userNodeIsNotExpanded) treeWnd.RefreshNode(userNode);

                                    node =

                                    // ユーザーフォルダにあるもの
                                    (
                                        from TreeNode n in userNode.Nodes
                                        let p = (n.Tag as ShellItem).Path
                                        where p.Any() && p.Last() != Path.DirectorySeparatorChar && string.Compare(p, path, ignoreCase: true) == 0
                                        select n).FirstOrDefault();

                                    if (node != null)
                                    {
                                        if (userNodeIsNotExpanded) treeWnd.ExpandWithoutRefresh(userNode);
                                        treeWnd.SelectedNode = node;
                                        return path;
                                    }

                                    node =

                                    // ユーザーフォルダから辿れるもの
                                    (
                                        from TreeNode n in userNode.Nodes
                                        let p = (n.Tag as ShellItem).Path
                                        where p.Any() && p.Last() != Path.DirectorySeparatorChar && path.StartsWith(p + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                                        select n).FirstOrDefault();

                                    if (node != null)
                                    {
                                        if (userNodeIsNotExpanded) treeWnd.ExpandWithoutRefresh(userNode);
                                        var subPath = (node.Tag as ShellItem).Path;
                                        subPos = subPath.Length + 1;
                                    }
                                    else
                                    {
                                        node =
                                        // ドライブのルートで PC 仮想フォルダ内から辿れるもの
                                        (
                                            from TreeNode n in pcNode.Nodes
                                            let p = (n.Tag as ShellItem).Path
                                            where p.Any() && p.Last() == Path.DirectorySeparatorChar && path.StartsWith(p, StringComparison.OrdinalIgnoreCase)
                                            select n).FirstOrDefault();

                                        if (node != null)
                                        {
                                            if (pcNodeIsNotExpanded) treeWnd.ExpandWithoutRefresh(pcNode);
                                            var subPath = (node.Tag as ShellItem).Path;
                                            if (!subPath.Any() || subPath.Last() != Path.DirectorySeparatorChar)
                                            {
                                                subPath += Path.DirectorySeparatorChar;
                                            }
                                            subPos = subPath.Length;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if (string.Compare(path, @"\\localhost", ignoreCase: true) == 0 || path.StartsWith(@"\\localhost\", StringComparison.OrdinalIgnoreCase))
                        {
                            path = @"\\" + Environment.MachineName + path.Substring(11);
                        }

                        if (!tvwRoot.IsExpanded) tvwRoot.Expand();
                        TreeNode networkNode;
                        try
                        {
                            var networkName = ShellItem.GetFolderDisplayName(ShellAPI.CSIDL.CSIDL_NETWORK);

                            networkNode = (
                            from TreeNode n in tvwRoot.Nodes
                            let item = n.Tag as ShellItem
                            where item.Path == "" & item.DisplayName == networkName
                            select n).First();
                        }
                        catch
                        {
                            return null;
                        }

                        //if (!networkNode.IsExpanded) networkNode.Expand();
                        var networkNodeIsNotExpanded = !networkNode.IsExpanded;
                        if (networkNodeIsNotExpanded) treeWnd.RefreshNode(networkNode);

                        var cutPath = path.Substring(2);
                        node =

                            // PC 仮想フォルダ内にあるもの（ドライブのルートを含む）
                            (
                                from TreeNode n in networkNode.Nodes
                                let d = (n.Tag as ShellItem).DisplayName
                                where string.Compare(cutPath, d, ignoreCase: true) == 0
                                select n).FirstOrDefault();

                        if (node != null)
                        {
                            if (networkNodeIsNotExpanded) treeWnd.ExpandWithoutRefresh(networkNode);
                            treeWnd.SelectedNode = node;
                            return path;
                        }

                        node =

                            // PC 仮想フォルダ内にあるもの（ドライブのルートを含む）
                            (
                                from TreeNode n in networkNode.Nodes
                                let d = (n.Tag as ShellItem).DisplayName
                                where cutPath.StartsWith(d + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                                select n).FirstOrDefault();

                        if (node != null)
                        {
                            if (networkNodeIsNotExpanded) treeWnd.ExpandWithoutRefresh(networkNode);
                            subPos = (node.Tag as ShellItem).DisplayName.Length + 3;
                        }
                    }

                    if (node == null) return null;

                    while (true)
                    {
                        var nextPos = path.IndexOf(Path.DirectorySeparatorChar, subPos + 1);
                        var finish = nextPos < 0;
                        if (finish) nextPos = path.Length;
                        var subPath = path.Substring(0, nextPos);

                        if (!node.IsExpanded) node.Expand();

                        var nextNode = (
                            from TreeNode n in node.Nodes
                            let item = n.Tag as ShellItem
                            where string.Compare(subPath, item.Path, ignoreCase: true) == 0
                            select n).FirstOrDefault();

                        if (nextNode == default(TreeNode))
                        {
                            treeWnd.SelectedNode = node;
                            return path.Substring(0, subPos);
                        }

                        if (finish)
                        {
                            treeWnd.SelectedNode = nextNode;
                            return path;
                        }

                        subPos = nextPos;
                        node = nextNode;
                    }
                }
#if AUTOBUILD
                catch
                {
                    return null;
                }
#endif
            }
            finally
            {
                TrySelect_Excuting = false;
            }
        }

        public TreeViewDrawMode DrawMode
        {
            get
            {
                return treeWnd.DrawMode;
            }
            set
            {
                treeWnd.DrawMode = value;
            }
        }

        public event DrawTreeNodeEventHandler DrawNode
        {
            add
            {
                treeWnd.DrawNode += value;
            }
            remove
            {
                treeWnd.DrawNode -= value;
            }
        }

        public event DragEventHandler TreeWndDragDrop
        {
            add
            {
                treeWnd.DragDrop += value;
            }
            remove
            {
                treeWnd.DragDrop -= value;
            }
        }

        public event DragEventHandler TreeWndDragEnter
        {
            add
            {
                treeWnd.DragEnter += value;
            }
            remove
            {
                treeWnd.DragEnter -= value;
            }
        }

        public event DragEventHandler TreeWndDragOver
        {
            add
            {
                treeWnd.DragOver += value;
            }
            remove
            {
                treeWnd.DragOver -= value;
            }
        }

        public event ItemDragEventHandler TreeWndItemDrag
        {
            add
            {
                treeWnd.ItemDrag += value;
            }
            remove
            {
                treeWnd.ItemDrag -= value;
            }
        }

        public event MouseEventHandler TreeWndMouseMove
        {
            add
            {
                treeWnd.MouseMove += value;
            }
            remove
            {
                treeWnd.MouseMove -= value;
            }
        }

        public event MouseEventHandler TreeWndMouseUp
        {
            add
            {
                treeWnd.MouseUp += value;
            }
            remove
            {
                treeWnd.MouseUp -= value;
            }
        }

        public event MouseEventHandler TreeWndMouseDown
        {
            add
            {
                treeWnd.MouseDown += value;
            }
            remove
            {
                treeWnd.MouseDown -= value;
            }
        }

        public event EventHandler TreeWndMouseLeave
        {
            add
            {
                treeWnd.MouseLeave += value;
            }
            remove
            {
                treeWnd.MouseLeave -= value;
            }
        }

        public event KeyEventHandler TreeWndKeyDown
        {
            add
            {
                treeWnd.KeyDown += value;
            }
            remove
            {
                treeWnd.KeyDown -= value;
            }
        }
        
        public event QueryContinueDragEventHandler TreeWndQueryContinueDrag
        {
            add
            {
                treeWnd.QueryContinueDrag += value;
            }
            remove
            {
                treeWnd.QueryContinueDrag -= value;
            }
        }

        public override bool AllowDrop
        {
            get
            {
                return treeWnd.AllowDrop;
            }

            set
            {
                treeWnd.AllowDrop = value;
            }
        }

        public bool ShowLines
        {
            get
            {
                return treeWnd.ShowLines;
            }
            set
            {
                treeWnd.ShowLines = value;
            }
        }

        public bool ShowRootLines
        {
            get
            {
                return treeWnd.ShowRootLines;
            }
            set
            {
                treeWnd.ShowRootLines = value;
            }
        }

        public new BorderStyle BorderStyle
        {
            get
            {
                return base.BorderStyle;
            }
            set
            {
                base.BorderStyle = value;
                treeWnd.BorderStyle = value;
            }
        }

        public TreeNode SelectedNode
        {
            get
            {
                return treeWnd.SelectedNode;
            }
            set
            {
                treeWnd.SelectedNode = value;
            }
        }

        public bool HideSelection
        {
            get
            {
                return treeWnd.HideSelection;
            }
            set
            {
                treeWnd.HideSelection = value;
            }
        }


        public event TreeNodeMouseClickEventHandler NodeMouseClick
        {
            add
            {
                treeWnd.NodeMouseClick += value;
            }
            remove
            {
                treeWnd.NodeMouseClick -= value;
            }
        }

        public new Point PointToScreen(Point p)
        {
            return treeWnd.PointToScreen(p);
        }

        public new Point PointToClient(Point p)
        {
            return treeWnd.PointToClient(p);
        }

        private readonly Dictionary<string, FileSystemWatcher> fileSystemWatcherSet = new Dictionary<string, FileSystemWatcher>();


        private void treeWnd_AfterExpand(object sender, TreeViewEventArgs e)
        {
            var node = e.Node;
            var shItem = node.Tag as ShellItem;
            if (shItem == null || !shItem.IsFolder) return;
            var path = shItem.Path;
            if (string.IsNullOrEmpty(path)) return;
            var pathInLower = path.ToLower();
            if (fileSystemWatcherSet.ContainsKey(pathInLower)) return;
            FileSystemWatcher watcher;
            try
            {
                if (Directory.Exists(path))
                {
                    watcher = new FileSystemWatcher(path);
                }
                else return;
            }
            catch
            {
                return;
            }
            var nodes = e.Node.Nodes;
            FileSystemEventHandler eventHandler = (sender2, e2) =>
            {
                Task.Run(() => // デッドロック回避
                {
                    try
                    {
                        Invoke((MethodInvoker)(() =>
                        {
                            if (TrySelect_Excuting || updating) return;
                            if (!fileSystemWatcherSet.ContainsKey(pathInLower)) return;

                            if (!node.IsExpanded) return;

                            if (e2.ChangeType == WatcherChangeTypes.Created)
                            {
                                if (contains(nodes, e2.FullPath)) return;
                            }
                            else if (e2.ChangeType == WatcherChangeTypes.Deleted)
                            {
                                if (!contains(nodes, e2.FullPath)) return;
                            }

                            treeWnd.RefreshNodeGently(node);
                        }));
                    }
                    catch (ObjectDisposedException)
                    {
                        return; // プログラム終了時なので stop 戻す必要はない
                    }
                });
            };
            RenamedEventHandler eventHandler2 = (sender2, e2) =>
            {
                Task.Run(() => // デッドロック回避
                {
                    try
                    {
                        Invoke((MethodInvoker)(() =>
                        {
                            if (TrySelect_Excuting || updating) return;
                            if (!fileSystemWatcherSet.ContainsKey(pathInLower)) return;

                            if (!node.IsExpanded) return;

                            if (contains(nodes, e2.FullPath))
                            {
                                // https://connect.microsoft.com/VisualStudio/feedback/details/520436/renamedeventargs-oldfullpath-throws-pathtoolongexception の問題に対処
                                string oldFullPath;
                                try { oldFullPath = e2.OldFullPath; } catch (PathTooLongException) { return; }

                                if (!contains(nodes, oldFullPath)) return;
                            }

                            treeWnd.RefreshNodeGently(node);
                        }));
                    }
                    catch (ObjectDisposedException)
                    {
                        return; // プログラム終了時なので stop 戻す必要はない
                    }
                });
            };
            watcher.Created += eventHandler;
            watcher.Deleted += eventHandler;
            watcher.Changed += eventHandler;
            watcher.Renamed += eventHandler2;
            fileSystemWatcherSet.Add(pathInLower, watcher);
            watcher.EnableRaisingEvents = true;
        }

        bool updating = false;

        public void BeginUpdate()
        {
            updating = true;
            treeWnd.BeginUpdate();
        }

        public void EndUpdate()
        {
            treeWnd.EndUpdate();
            updating = false;
        }

        private void treeWnd_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            // 削除される場合も展開されていたら呼び出されるように ExplorerTreeViewWnd を実装済み

            var node = e.Node;

            // 子孫を巻き込んで折りたたむ場合は基点しか呼び出されないので注意
            foreach (TreeNode subNode in node.Nodes)
            {
                if (subNode.IsExpanded) treeWnd_BeforeCollapse(sender, new TreeViewCancelEventArgs(subNode, e.Cancel, e.Action));
            }

            var shItem = node.Tag as ShellItem;
            if (shItem == null || !shItem.IsFolder) return;
            var path = shItem.Path;
            if (string.IsNullOrEmpty(path)) return;
            var pathInLower = path.ToLower();

            FileSystemWatcher watcher;
            if (fileSystemWatcherSet.TryGetValue(pathInLower, out watcher))
            {
                watcher.Dispose();
                fileSystemWatcherSet.Remove(pathInLower);
            }
        }

        /*
        private void treeWnd_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            //if (stop) return;
            var node = e.Node;
            var shItem = node.Tag as ShellItem;
            if (shItem == null || !shItem.IsFolder) return;
            var path = shItem.Path;
            FileSystemWatcher watcher;
            try
            {
                if (Directory.Exists(path))
                {
                    watcher = new FileSystemWatcher(path);
                }
                else return;
            }
            catch
            {
                return;
            }
            if (string.IsNullOrEmpty(path)) return;
            var pathInLower = path.ToLower();
            if (fileSystemWatcherSet.ContainsKey(pathInLower)) return;
            var nodes = e.Node.Nodes;
            FileSystemEventHandler eventHandler = (sender2, e2) =>
            {
                try
                {
                    Invoke((MethodInvoker)(() =>
                    {
                        if (e2.ChangeType == WatcherChangeTypes.Created)
                        {
                            if (contains(nodes, e2.FullPath)) return;
                        }
                        else if (e2.ChangeType == WatcherChangeTypes.Deleted)
                        {
                            if (!contains(nodes, e2.FullPath)) return;
                        }

                        treeWnd.RefreshNodeGently(node);
                        if (e2.ChangeType == WatcherChangeTypes.Deleted)
                        {
                            disposeWatcher(e2.FullPath);
                        }
                    }));
                }
                catch (ObjectDisposedException)
                {
                    return; // プログラム終了時なので stop 戻す必要はない
                }
            };
            watcher.Created += eventHandler;
            watcher.Deleted += eventHandler;
            watcher.Changed += eventHandler;
            fileSystemWatcherSet.Add(pathInLower, watcher);
            watcher.EnableRaisingEvents = true;
        }
        */

        private static bool contains(TreeNodeCollection nodes, string path)
        {
            foreach(TreeNode node in nodes)
            {
                var item = node.Tag as ShellItem;
                if (item != null && string.Compare(item.Path, path, ignoreCase: true) == 0) return true;
            }
            return false;
        }
        
        /*
        private void treeWnd_AfterCollapse(object sender, TreeViewEventArgs e)
        {
            //if (stop) return;
            var shItem = e.Node.Tag as ShellItem;
            if (shItem == null) return;
            var path = shItem.Path;
            disposeWatcher(path);
        }

        private void disposeWatcher(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            FileSystemWatcher watcher;
            path = path.ToLower();
            if (fileSystemWatcherSet.TryGetValue(path, out watcher))
            {
                watcher.Dispose();
                fileSystemWatcherSet.Remove(path);
            }
            if (path.Last() != Path.DirectorySeparatorChar) path += Path.DirectorySeparatorChar;
            List<string> deleteList = null;
            foreach (var pair in fileSystemWatcherSet)
            {
                var key = pair.Key;
                if (key != null && key.StartsWith(path))
                {
                    pair.Value.Dispose();
                    if (deleteList == null) deleteList = new List<string>();
                    deleteList.Add(key);
                }
            }
            if (deleteList != null)
            {
                foreach (var delete in deleteList)
                {
                    fileSystemWatcherSet.Remove(delete);
                }
            }
        }
        */

        TreeNode tvwRoot = null;
        /// <summary>
        /// Loads the root TreeView nodes.
        /// </summary>
        private void LoadRootNodes(string loadingString)
        {
            //using (var tm = new ZipPla.TimeMeasure())
            {
                // Create the root shell item.
                var first = tvwRoot == null;
                if (first)
                {
                    //tm.Block("var m_shDesktop = new ShellItem();");
                    var m_shDesktop = new ShellItem();

                    //tm.Block("tvwRoot = ExplorerTreeViewWnd.CreateNode(m_shDesktop);");
                    tvwRoot = ExplorerTreeViewWnd.CreateNode(m_shDesktop);
                    //tm.Block("treeWnd.Nodes.Add(tvwRoot);");
                    treeWnd.Nodes.Add(tvwRoot);

                    if (!string.IsNullOrEmpty(loadingString))
                    {
                        using (var g = treeWnd.CreateGraphics())
                        using (var b = new SolidBrush(treeWnd.ForeColor))
                        {
                            g.DrawString(loadingString, treeWnd.Font, b, g.DpiX / 96, g.DpiY / 96);
                        }
                    }
                }
                else
                {
                    //tm.Block("treeWnd.RefreshNode(tvwRoot);");
                    treeWnd.RefreshNode(tvwRoot);
                }

                //tm.Block("tvwRoot.Expand();");
                tvwRoot.Expand();

            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
        }
    }
}
