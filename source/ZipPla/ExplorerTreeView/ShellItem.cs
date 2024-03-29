using System;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;
using System.Linq;

namespace WilsonProgramming
{
    class ShellItem : IComparable, IComparable<ShellItem>
    {
        #region Private Member Variables

        // Sets a flag specifying whether or not we've got the IShellFolder interface for the Desktop.
        private static Boolean m_bHaveRootShell = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor. Creates the ShellItem object for the Desktop.
        /// </summary>
        public ShellItem()
        {
            // new ShellItem() can only be called once.
            if (m_bHaveRootShell)
                throw new Exception("The Desktop shell item already exists so cannot be created again.");

            // Obtain the root IShellFolder interface.
            int hRes = ShellAPI.SHGetDesktopFolder(ref m_shRootShell);
            if (hRes != 0)
                Marshal.ThrowExceptionForHR(hRes);

            // Now get the PIDL for the Desktop shell item.
            hRes = ShellAPI.SHGetSpecialFolderLocation(IntPtr.Zero, ShellAPI.CSIDL.CSIDL_DESKTOP, ref m_pIDL);
            if (hRes != 0)
                Marshal.ThrowExceptionForHR(hRes);

            // Now retrieve some attributes for the root shell item.
            ShellAPI.SHFILEINFO shInfo = new ShellAPI.SHFILEINFO();
            ShellAPI.SHGetFileInfo(m_pIDL, 0, out shInfo, (uint)Marshal.SizeOf(shInfo), 
                ShellAPI.SHGFI.SHGFI_DISPLAYNAME | 
                ShellAPI.SHGFI.SHGFI_PIDL | 
                ShellAPI.SHGFI.SHGFI_SMALLICON | 
                ShellAPI.SHGFI.SHGFI_SYSICONINDEX
            );

            // Set the arributes to object properties.
            DisplayName  = shInfo.szDisplayName;
            IconIndex    = shInfo.iIcon;
            IsFolder     = true;
            HasSubFolder = true;
            Path         = GetPath();

            // Internal with no set{} mutator.
            m_shShellFolder  = RootShellFolder;
            m_bHaveRootShell = true;
        }

        /// <summary>
        /// Constructor. Create a sub-item shell item object.
        /// </summary>
        /// <param name="shDesktop">IShellFolder interface of the Desktop</param>
        /// <param name="pIDL">The fully qualified PIDL for this shell item</param>
        /// <param name="shParent">The ShellItem object for this item's parent</param>
        public ShellItem(ShellAPI.IShellFolder shDesktop, IntPtr pIDL, ShellItem shParent, string path, IntPtr m_pIDL)
        {
            // We need the Desktop shell item to exist first.
            if (m_bHaveRootShell == false)
                throw new Exception("The root shell item must be created before creating a sub-item");

            // Create the FQ PIDL for this new item.
            //m_pIDL = ShellAPI.ILCombine(shParent.PIDL, pIDL);
            this.m_pIDL = m_pIDL;

            /*
            // Get the properties of this item.
            ShellAPI.SFGAOF uFlags = ShellAPI.SFGAOF.SFGAO_FOLDER | ShellAPI.SFGAOF.SFGAO_HASSUBFOLDER;

            // Here we get some basic attributes.
            shDesktop.GetAttributesOf(1, out m_pIDL, out uFlags);
            IsFolder = Convert.ToBoolean(uFlags & ShellAPI.SFGAOF.SFGAO_FOLDER);
            HasSubFolder = Convert.ToBoolean(uFlags & ShellAPI.SFGAOF.SFGAO_HASSUBFOLDER);
            */
            

            // Now we want to get extended attributes such as the icon index etc.
            ShellAPI.SHFILEINFO shInfo = new ShellAPI.SHFILEINFO();
            ShellAPI.SHGFI vFlags =
                ShellAPI.SHGFI.SHGFI_SMALLICON |
                ShellAPI.SHGFI.SHGFI_SYSICONINDEX |
                ShellAPI.SHGFI.SHGFI_PIDL |
                ShellAPI.SHGFI.SHGFI_DISPLAYNAME;
            ShellAPI.SHGetFileInfo(m_pIDL, 0, out shInfo, (uint)Marshal.SizeOf(shInfo), vFlags);
            DisplayName = shInfo.szDisplayName;
            IconIndex = shInfo.iIcon;
            //Path      = path != "" ? path : GetPath();
            Path = path;
            
            if (Path != "")
            {
                IsFolder = System.IO.Directory.Exists(Path);
                HasSubFolder = IsFolder && new System.IO.DirectoryInfo(Path).EnumerateDirectories().Any();
            }
            else
            {
                // Get the properties of this item.
                ShellAPI.SFGAOF uFlags = ShellAPI.SFGAOF.SFGAO_FOLDER | ShellAPI.SFGAOF.SFGAO_HASSUBFOLDER;

                // Here we get some basic attributes.
                shDesktop.GetAttributesOf(1, out m_pIDL, out uFlags);
                IsFolder = Convert.ToBoolean(uFlags & ShellAPI.SFGAOF.SFGAO_FOLDER);
                HasSubFolder = Convert.ToBoolean(uFlags & ShellAPI.SFGAOF.SFGAO_HASSUBFOLDER);
            }
            
            
            /*
            // 再チェック
            if (IsFolder)
            {
                if (System.IO.Directory.Exists(Path))
                {
                    if (HasSubFolder)
                    {
                        HasSubFolder = new System.IO.DirectoryInfo(Path).EnumerateDirectories().Any();
                    }
                }
            }
            */
           
            // Create the IShellFolder interface for this item.
            if (IsFolder)
            {
                uint hRes = shParent.m_shShellFolder.BindToObject(pIDL, IntPtr.Zero, ref ShellAPI.IID_IShellFolder, out m_shShellFolder);
                if (hRes != 0)
                    Marshal.ThrowExceptionForHR((int)hRes);
            }
        }

        #endregion

        #region Destructor

        ~ShellItem()
        {
            // Release the IShellFolder interface of this shell item.
            if (m_shShellFolder != null)
                Marshal.ReleaseComObject(m_shShellFolder);

            // Free the PIDL too.
            if (!m_pIDL.Equals(IntPtr.Zero))
                Marshal.FreeCoTaskMem(m_pIDL);

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the system path for this shell item.
        /// </summary>
        /// <returns>A path string.</returns>
        public string GetPath()
        {
            StringBuilder strBuffer = new StringBuilder(256);
            ShellAPI.SHGetPathFromIDList(
                m_pIDL, 
                strBuffer
            );
            return strBuffer.ToString();
        }

        public static string GetPath(IntPtr pIDL, ShellItem shParent, out IntPtr m_pIDL)
        {
            // Create the FQ PIDL for this new item.
            m_pIDL = ShellAPI.ILCombine(shParent.PIDL, pIDL);
            StringBuilder strBuffer = new StringBuilder(256);
            ShellAPI.SHGetPathFromIDList(
                m_pIDL,
                strBuffer
            );
            return strBuffer.ToString();
        }


        /// <summary>
        /// Retrieves an array of ShellItem objects for sub-folders of this shell item.
        /// </summary>
        /// <returns>ArrayList of ShellItem objects.</returns>
        public ArrayList GetSubFolders()
        {
            // Make sure we have a folder.
            if (IsFolder == false)
                throw new Exception("Unable to retrieve sub-folders for a non-folder.");

            ArrayList arrChildren = new ArrayList();
            try
            {
                // Get the IEnumIDList interface pointer.
                ShellAPI.IEnumIDList pEnum = null;
                uint hRes = ShellFolder.EnumObjects(IntPtr.Zero, ShellAPI.SHCONTF.SHCONTF_FOLDERS, out pEnum);

                if (pEnum == null)
                {
                    return arrChildren;
                }

                if (hRes != 0)
                    Marshal.ThrowExceptionForHR((int)hRes);
                
                IntPtr pIDL = IntPtr.Zero;
                Int32 iGot = 0;

                // Grab the first enumeration.
                pEnum.Next(1, out pIDL, out iGot);

                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                var desktop = Path == desktopPath;
                //var ignoreDesktop = Path == "" && DisplayName == "PC";
                const bool ignoreDesktop = true;

                string[] ignoreDisplayNameList;
                try
                {
                    ignoreDisplayNameList = new string[]
                    {
                        //"コントロール パネル",
                        GetFolderDisplayName("::{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}"), // コントロールパネル
                        GetFolderDisplayName(ShellAPI.CSIDL.CSIDL_CONTROLS), // すべてのコントロールパネル項目
                        GetFolderDisplayName(ShellAPI.CSIDL.CSIDL_PRINTERS),
                        GetFolderDisplayName(ShellAPI.CSIDL.CSIDL_FONTS),
                        GetFolderDisplayName(ShellAPI.CSIDL.CSIDL_BITBUCKET),
                    }.Where(name => !string.IsNullOrEmpty(name)).ToArray();
                }
                catch
                {
                    ignoreDisplayNameList = null;
                }
                
                // Then continue with all the rest.
                while (!pIDL.Equals(IntPtr.Zero) && iGot == 1)
                {
                    IntPtr m_pIDL;
                    var path = GetPath(pIDL, this, out m_pIDL);
                    if ((System.IO.Path.GetExtension(path).ToLower() != ".zip" || System.IO.Directory.Exists(path)) && !(desktop && isRoot(path)) && !(ignoreDesktop && path == desktopPath))
                    {
                        var item = new ShellItem(m_shRootShell, pIDL, this, path, m_pIDL);
                        
                        if (path != "" || ignoreDisplayNameList == null || !ignoreDisplayNameList.Contains(item.DisplayName))
                        {
                            arrChildren.Add(item);
                        }
                    }

                    // Free the PIDL and reset counters.
                    Marshal.FreeCoTaskMem(pIDL);
                    pIDL = IntPtr.Zero;
                    iGot = 0;

                    // Grab the next item.
                    pEnum.Next(1, out pIDL, out iGot);
                }

                // Free the interface pointer.
                if (pEnum != null)
                    Marshal.ReleaseComObject(pEnum);

                arrChildren.Sort();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message, null,
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error
                );
            }

            return arrChildren;
        }
        

        [DllImport("Shell32.dll")]
        static extern void ILFree(IntPtr pidl);
        public static string GetFolderDisplayName(ShellAPI.CSIDL csidl)
        {
            var m_pIDL = IntPtr.Zero;
            var hRes = ShellAPI.SHGetSpecialFolderLocation(IntPtr.Zero, csidl, ref m_pIDL);
            if (hRes != 0) return null;

            try
            {
                ShellAPI.SHFILEINFO shInfo = new ShellAPI.SHFILEINFO();
                ShellAPI.SHGetFileInfo(m_pIDL, 0, out shInfo, (uint)Marshal.SizeOf(shInfo),
                ShellAPI.SHGFI.SHGFI_PIDL |
                ShellAPI.SHGFI.SHGFI_SMALLICON |
                ShellAPI.SHGFI.SHGFI_DISPLAYNAME);
                return shInfo.szDisplayName;
            }
            catch
            {
                return null;
            }
            finally
            {
                ILFree(m_pIDL);
            }
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr ILCreateFromPath(string pszPath);
        public static string GetFolderDisplayName(string path)
        {
            var m_pIDL = ILCreateFromPath(path);
            if (m_pIDL == IntPtr.Zero) return null;

            try
            {
                ShellAPI.SHFILEINFO shInfo = new ShellAPI.SHFILEINFO();
                ShellAPI.SHGetFileInfo(m_pIDL, 0, out shInfo, (uint)Marshal.SizeOf(shInfo),
                ShellAPI.SHGFI.SHGFI_PIDL |
                ShellAPI.SHGFI.SHGFI_SMALLICON |
                ShellAPI.SHGFI.SHGFI_DISPLAYNAME);
                return shInfo.szDisplayName;
            }
            catch
            {
                return null;
            }
            finally
            {
                ILFree(m_pIDL);
            }
        }

        private static bool isRoot(string path)
        {
            return System.IO.Path.IsPathRooted(path) && path == System.IO.Path.GetPathRoot(path);
        }

        public int CompareTo(ShellItem other)
        {
            if (other == null) return 1;
            var otherPath = other.Path;
            if (otherPath == null) return Path == null ? 0 : 1;
            //var rooted = System.IO.Path.IsPathRooted(Path);
            //if (rooted != System.IO.Path.IsPathRooted(otherPath)) return rooted ? 1 : -1;
            return StrCmpLogicalW(Path, otherPath);
        }

        public int CompareTo(object obj)
        {
            return CompareTo(obj as ShellItem);
        }

        [System.Runtime.InteropServices.DllImport("shlwapi.dll",
            CharSet = System.Runtime.InteropServices.CharSet.Unicode,
            ExactSpelling = true)]
        private static extern int StrCmpLogicalW(string x, string y);

        #endregion

        #region Properties

        /// <summary>
        /// Gets or set the display name for this shell item.
        /// </summary>
        public string DisplayName
        {
            get { return m_strDisplayName; }
            set { m_strDisplayName = value; }
        }
        string m_strDisplayName = "";

        /// <summary>
        /// Gets or sets the system image list icon index for this shell item.
        /// </summary>
        public Int32 IconIndex
        {
            get { return m_iIconIndex; }
            set { m_iIconIndex = value; }
        }
        Int32 m_iIconIndex = -1;

        /// <summary>
        /// Gets the IShellFolder interface of the Desktop.
        /// </summary>
        public ShellAPI.IShellFolder RootShellFolder
        {
            get { return m_shRootShell; }
        }
        static ShellAPI.IShellFolder m_shRootShell = null;

        /// <summary>
        /// Gets the IShellFolder interface of this shell item.
        /// </summary>
        public ShellAPI.IShellFolder ShellFolder
        {
            get { return m_shShellFolder; }
        }
        ShellAPI.IShellFolder m_shShellFolder = null;

        /// <summary>
        /// Gets the fully qualified PIDL for this shell item.
        /// </summary>
        public IntPtr PIDL
        {
            get { return m_pIDL; }
        }
        IntPtr m_pIDL = IntPtr.Zero;

        /// <summary>
        /// Gets or sets a boolean indicating whether this shell item is a folder.
        /// </summary>
        public bool IsFolder
        {
            get { return m_bIsFolder; }
            set { m_bIsFolder = value; }
        }
        bool m_bIsFolder = false;

        /// <summary>
        /// Gets or sets a boolean indicating whether this shell item has any sub-folders.
        /// </summary>
        public bool HasSubFolder
        {
            get { return m_bHasSubFolder; }
            set { m_bHasSubFolder = value; }
        }
        bool m_bHasSubFolder = false;

        /// <summary>
        /// Gets or sets the system path for this shell item.
        /// </summary>
        public string Path
        {
            get { return m_strPath;  }
            set { m_strPath = value; }
        }
        string m_strPath = "";

        #endregion
    }
}
