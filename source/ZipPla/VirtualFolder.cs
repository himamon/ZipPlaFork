using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ZipPla
{
    public static class VirtualFolder
    {
        public static bool IsExistingBookmarkPath(string path)
        {
            try
            {
                return IsBookmarkPath(path) && File.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        public static bool DirectoryOrBookmarkExists(string path)
        {
            // エラー OK とする
            return Path.GetExtension(path).ToUpper() == ".SOR" && File.Exists(path) || Directory.Exists(path);
        }

        public static string[] GetDirectoriesAndBookmarks(string path)
        {
            var result1 = Directory.GetFiles(path, "*.sor");
            var result2 = Directory.GetDirectories(path);
            var result1Length = result1.Length;
            var count1 = 0;
            for(var i = 0; i < result1Length; i++)
            {
                var subPath = result1[i];
                if (Path.GetExtension(subPath).ToUpper() == ".SOR")
                {
                    result1[count1++] = subPath;
                }
            }
            var count2 = result2.Length;
            var result = new string[count1 + count2];
            Array.Copy(result1, 0, result, 0, count1);
            result1 = null;
            Array.Copy(result2, 0, result, count1, count2);
            result2 = null;
            return result;
        }

        public static bool IsBookmarkPath(string path)
        {
            try
            {
                // 削除コマンドが実行可能なものを追加する場合 CatalogForm の該当箇所を変更すること
                return Path.GetExtension(path).ToUpper() == ".SOR";
            }
            catch
            {
                return false;
            }
        }

        private static bool EndsWithDirectorySeparatorChar(string path)
        {
            return !string.IsNullOrEmpty(path) && path.Last() == Path.DirectorySeparatorChar;
        }

        public static int GetPageFromBookmark(string path, bool isDir, string bmkPath)
        {
            GetBookmarkFiles(out var bmkFilePaths, out var bmkFilePages, out var bmkFolderPaths, out var bmkFolderPages, bmkPath);
            
            if (isDir)
            {
                var index = Array.IndexOf(bmkFolderPaths, path);
                return index >= 0 ? bmkFolderPages[index] : -1;
            }
            else
            {
                var index = Array.IndexOf(bmkFilePaths, path);
                return index >= 0 ? bmkFilePages[index] : -1;
            }
        }

        public static void GetPageFromBookmark(out int[] filePages, out int[] folderPages, string[] filePaths, string[] folderPaths, string bmkPath)
        {
            GetBookmarkFiles(out var bmkFilePaths, out var bmkFilePages, out var bmkFolderPaths, out var bmkFolderPages, bmkPath);

            filePages = folderPages = null;
            for (var i = 0; i < 2; i++)
            {
                string[] paths, bmkPaths;
                int[] pages, bmkPages;
                if (i == 0)
                {
                    paths = filePaths;
                    bmkPaths = bmkFilePaths;
                    pages = filePages = new int[paths.Length];
                    bmkPages = bmkFilePages;
                }
                else
                {
                    paths = folderPaths;
                    bmkPaths = bmkFolderPaths;
                    pages = folderPages = new int[paths.Length];
                    bmkPages = bmkFolderPages;
                }

                var indices = EetSortedIndices(paths);
                var bmkIndices = EetSortedIndices(bmkPaths);

                var pos = 0;
                var bmkPos = 0;
                while (pos < indices.Length && bmkPos < bmkIndices.Length)
                {
                    var index = indices[pos];
                    var bmkIndex = bmkIndices[bmkPos];

                    //var comp = string.Compare(paths[index], bmkPaths[bmkIndex]);
                    var comp = paths[index].CompareTo(bmkPaths[bmkIndex]); // ICmmparable<string> のメソッドを直接利用するのが最も確実
                    
                    if (comp == 0)
                    {
                        pages[index] = bmkPages[bmkIndex];
                        pos++;
                        bmkPos++;
                    }
                    else if (comp < 0)
                    {
                        pages[index] = -1;
                        pos++;
                    }
                    else
                    {
                        bmkPos++;
                    }
                }
                while (pos < pages.Length)
                {
                    pages[indices[pos++]] = -1;
                }
            }

        }

        private static int[] EetSortedIndices<T>(IReadOnlyList<T> data) where T : IComparable<T>
        {
            var count = data.Count;
            var result = new int[count];
            for (var i = 0; i < count; i++) result[i] = i;
            return result.OrderBy(i => data[i]).ToArray();
        }
        
        public static void GetBookmarkFiles(out string[] filePaths, out int[] filePages, out string[] folderPaths, out int[] folderPages, string bmkPath)
        {
            var filePathList = new List<string>();
            var filePageList = new List<int>();
            var folderPathList = new List<string>();
            var folderPageList = new List<int>();

            if (!File.Exists(bmkPath)) throw new FileNotFoundException(null, bmkPath);
            using (var r = new StreamReader(bmkPath))
            {
                string path = null;
                int page = -1;
                for(var line = r.ReadLine(); true; line = r.ReadLine())
                {
                    if(line == "[Item]" || line == null)
                    {
                        if(path != null)
                        {
                            if(EndsWithDirectorySeparatorChar(path))

                            {
                                folderPathList.Add(path.Substring(0, path.Length - 1));
                                folderPageList.Add(page);
                            }
                            else
                            {
                                filePathList.Add(path);
                                filePageList.Add(page);
                            }
                        }
                        if (line != null)
                        {
                            path = null;
                            page = -1;
                            continue;
                        }
                        else break;
                    }
                    var separatorPosition = line.IndexOf('=');
                    if (separatorPosition < 0) continue;
                    var value = line.Substring(separatorPosition + 1);
                    switch(line.Substring(0,separatorPosition))
                    {
                        case "Path": if (path == null) path = value; break;
                        case "Page": if (page < 0) if (!int.TryParse(value, out page)) page = -1; break;
                    }
                }
            }

            filePaths = filePathList.ToArray();
            filePages = filePageList.ToArray();
            folderPaths = folderPathList.ToArray();
            folderPages = folderPageList.ToArray();
        }

        public static void AddBookmarkData(string bmkPath, string path, int page, int limitOfItemsCount = int.MaxValue, bool deleteLostPath = false)
        {
            AddBookmarkData(bmkPath, new string[] { path }, new int[] { page }, limitOfItemsCount, deleteLostPath);
        }
        public static int AddBookmarkData(string bmkPath, IReadOnlyList<string> paths, IReadOnlyList<int> pages, int limitOfItemsCount = int.MaxValue, bool deleteLostPath = false)
        {
            AddDeleteBookmarkData(bmkPath, paths, pages, limitOfItemsCount, out var dummy, deleteLostPath, false,
            replacePaths: null, firstMatchedPage: out var firstMatchedPage);
            return firstMatchedPage;
        }

        public static void DeleteBookmarkData(string bmkPath, IReadOnlyList<string> paths)
        {
            AddDeleteBookmarkData(bmkPath, paths, null, limitOfItemsCount: int.MaxValue, trimmed: out var dummy,
                deleteLostPath: false, deleteOrReplace: true, replacePaths: null, firstMatchedPage: out var dummy2);
        }

        public static bool TrimBookmarkData(string bmkPath, int limitOfItemsCount)
        {
            AddDeleteBookmarkData(bmkPath, new string[0], null, limitOfItemsCount, out var trimmed, deleteLostPath: true,
                deleteOrReplace: false, replacePaths: null, firstMatchedPage: out var dummy);
            return trimmed;
        }

        /// <summary>
        /// VirtualFolder の要素を置換します。path と replacePats について、ファイルかどうかが一致しなければなりません。
        /// </summary>
        /// <param name="bmkPath"></param>
        /// <param name="path"></param>
        /// <param name="replacePath"></param>
        public static void ReplaceBookmarkData(string bmkPath, string path, string replacePath)
        {
            ReplaceBookmarkData(bmkPath, new string[1] { path }, new string[1] { replacePath });
        }

        /// <summary>
        /// VirtualFolder の要素を置換します。paths と replacePaths の同じインデックス毎、ファイルかどうかが一致しなければなりません。
        /// </summary>
        /// <param name="bmkPath"></param>
        /// <param name="paths"></param>
        /// <param name="replacePaths"></param>
        public static void ReplaceBookmarkData(string bmkPath, IReadOnlyList<string> paths, IReadOnlyList<string> replacePaths)
        {
            AddDeleteBookmarkData(bmkPath, paths, null, limitOfItemsCount: int.MaxValue, trimmed: out var dummy,
                deleteLostPath: false, deleteOrReplace: true, replacePaths: replacePaths, firstMatchedPage: out var dummy2);
        }

        private static bool EntityFileExists(string path)
        {
            var altPos = path.IndexOf(Path.AltDirectorySeparatorChar);
            return File.Exists(altPos < 0 ? path : path.Substring(0, altPos));
        }

        private static void AddDeleteBookmarkData(string bmkPath, IReadOnlyList<string> paths, IReadOnlyList<int> pages, int limitOfItemsCount, out bool trimmed, bool deleteLostPath,
            bool deleteOrReplace, IReadOnlyList<string> replacePaths, out int firstMatchedPage)
        {
            var edited = false;
            firstMatchedPage = -1;

            var filePathList = new List<string>();
            var filePageList = new List<int>();
            var folderPathList = new List<string>();
            var folderPageList = new List<int>();

            var delete = deleteOrReplace && replacePaths == null;
            var replace = deleteOrReplace && replacePaths != null;

            if (File.Exists(bmkPath))
            {
                GetBookmarkFiles(out var filePaths, out var filePages, out var folderPaths, out var folderPages, bmkPath);
                if (deleteLostPath)
                {
                    var fileCount = filePaths.Length;
                    for(var i = 0; i < fileCount; i++)
                    {
                        if (EntityFileExists(filePaths[i]))
                        {
                            filePathList.Add(filePaths[i]);
                            filePageList.Add(filePages[i]);
                        }
                        else edited = true;
                    }
                    var folderCount = folderPaths.Length;
                    for (var i = 0; i < folderCount; i++)
                    {
                        if (Directory.Exists(folderPaths[i]))
                        {
                            folderPathList.Add(folderPaths[i]);
                            folderPageList.Add(folderPages[i]);
                        }
                        else edited = true;
                    }
                }
                else
                {
                    filePathList.AddRange(filePaths);
                    filePageList.AddRange(filePages);
                    folderPathList.AddRange(folderPaths);
                    folderPageList.AddRange(folderPages);
                }
            }
            else
            {
                trimmed = false;
                if (deleteOrReplace) return;

                // なければ作る
                edited = true;
            }

            var newCount = paths.Count;
            var newPathSet = new HashSet<string>();
            for (var i = 0; i < newCount; i++)
            {
                var path = paths[i];
                var replacedPath = replace ? replacePaths[i] : null;
                bool isDir;
                if (!EndsWithDirectorySeparatorChar(path))
                {
                    if(Directory.Exists(path))
                    {
                        path += Path.DirectorySeparatorChar;
                        isDir = true;
                    }
                    else
                    {
                        isDir = false;
                    }
                }
                else
                {
                    isDir = true;
                }
                //var page = pages == null ? -1 : pages[i];
                int page;
                if ((isDir || PackedImageLoader.Supports(replacedPath ?? path)) && pages != null)
                {
                    page = pages[i];
                }
                else
                {
                    page = -1;
                }
                if (isDir)
                {
                    path = path.Substring(0, path.Length - 1);
                    var existingFolderCount = folderPathList.Count;

                    var found = false;
                    for (var j = 0; j < existingFolderCount; j++)
                    {
                        if (string.Compare(path, folderPathList[j], ignoreCase: true) == 0)
                        {
                            if (firstMatchedPage < 0) firstMatchedPage = folderPageList[j];
                            if (delete || found)
                            {
                                folderPathList.RemoveAt(j);
                                folderPageList.RemoveAt(j);
                                j--;
                                existingFolderCount--;
                            }
                            else if (replace)
                            {
                                folderPathList[j] = replacedPath;
                                if (page >= 0) folderPageList[j] = page;
                                newPathSet.Add(replacedPath);
                            }
                            else
                            {
                                folderPathList[j] = path; // 大文字小文字の補正
                                folderPageList[j] = page;
                                newPathSet.Add(path);
                            }
                            found = true;
                        }
                    }
                    if (!found && !deleteOrReplace)
                    {
                        edited = true;
                        folderPathList.Add(path);
                        folderPageList.Add(page);
                        newPathSet.Add(path);
                    }

                    /*
                    var needToAdd = !deleteOrReplace;
                    for (var j = 0; j < existingFolderCount; j++)
                    {
                        if (path == folderPathList[j])
                        {
                            needToAdd = false;
                            edited = true;
                            if (delete || replace && contains(path, folderPathList, j))
                            {
                                folderPathList.RemoveAt(j);
                                folderPageList.RemoveAt(j);
                                j--;
                                existingFolderCount--;
                            }
                            else if(replace)
                            {
                                folderPathList[j] = replacedPath;
                                if (page >= 0) folderPageList[j] = page;
                                newPathSet.Add(replacedPath);
                            }
                            else
                            {
                                folderPageList[j] = page;
                                newPathSet.Add(path);
                            }
                        }
                    }
                    if(needToAdd)
                    {
                        edited = true;
                        folderPathList.Add(path);
                        folderPageList.Add(page);
                        newPathSet.Add(path);
                    }
                    */
                }
                else
                {
                    var existingFileCount = filePathList.Count;
                    var found = false;
                    for (var j = 0; j < existingFileCount; j++)
                    {
                        if (string.Compare(path, filePathList[j], ignoreCase: true) == 0)
                        {
                            if (firstMatchedPage < 0) firstMatchedPage = filePageList[j];
                            edited = true;
                            if (delete || found)
                            {
                                filePathList.RemoveAt(j);
                                filePageList.RemoveAt(j);
                                j--;
                                existingFileCount--;
                            }
                            else if (replace)
                            {
                                filePathList[j] = replacedPath;
                                if (page >= 0) filePageList[j] = page;
                                newPathSet.Add(replacedPath);
                            }
                            else
                            {
                                filePathList[j] = path; // 大文字小文字の補正
                                filePageList[j] = page;
                                newPathSet.Add(path);
                            }
                            found = true;
                        }
                    }
                    if (!found && !deleteOrReplace)
                    {
                        edited = true;
                        filePathList.Add(path);
                        filePageList.Add(page);
                        newPathSet.Add(path);
                    }

                    /*
                    var needToAdd = !deleteOrReplace;
                    for (var j = 0; j < existingFileCount; j++)
                    {
                        if (path == filePathList[j])
                        {
                            needToAdd = false;
                            edited = true;
                            if (delete || replace && contains(path, filePathList, j))
                            {
                                filePathList.RemoveAt(j);
                                filePageList.RemoveAt(j);
                                j--;
                                existingFileCount--;
                            }
                            else if (replace)
                            {
                                filePathList[j] = replacedPath;
                                if (page >= 0) filePageList[j] = page;
                                newPathSet.Add(replacedPath);
                            }
                            else
                            {
                                filePageList[j] = page;
                                newPathSet.Add(path);
                            }
                        }
                    }
                    if (needToAdd)
                    {
                        edited = true;
                        filePathList.Add(path);
                        filePageList.Add(page);
                        newPathSet.Add(path);
                    }
                    */
                }
            }

            trimmed = filePathList.Count + folderPathList.Count > limitOfItemsCount;
            if (trimmed)
            {
                edited = true;
                var AccessDates = new List<DateTime>();
                AccessDates.AddRange(from path in filePathList select newPathSet.Contains(path) ? DateTime.MaxValue : Program.GetLastAccessTimeOfFile(path));//  File.GetLastAccessTime(path));
                AccessDates.AddRange(from path in folderPathList select newPathSet.Contains(path) ? DateTime.MaxValue : Program.GetLastAccessTimeOfDirectory(path));//File.GetLastAccessTime(path));
                while (AccessDates.Count > limitOfItemsCount)
                {
                    var targetAccessDate = DateTime.MaxValue;
                    var targetIndex = -1;
                    var count = AccessDates.Count;
                    for(var i = 0; i < count; i++)
                    {
                        if(AccessDates[i] < targetAccessDate)
                        {
                            targetAccessDate = AccessDates[i];
                            targetIndex = i;
                        }
                    }
                    AccessDates.RemoveAt(targetIndex);
                    if(targetIndex < filePathList.Count)
                    {
                        filePathList.RemoveAt(targetIndex);
                    }
                    else
                    {
                        folderPathList.RemoveAt(targetIndex - filePathList.Count);
                    }
                }
            }

            if (edited) SaveBookmarkData(bmkPath, filePathList.ToArray(), filePageList.ToArray(), folderPathList.ToArray(), folderPageList.ToArray());
        }

        /*
        private static bool contains(string search, IEnumerable<string> strs, int exceptedIndex)
        {
            var index = 0;
            foreach (var str in strs)
            {
                if (index++ != exceptedIndex && string.Compare(search, str, ignoreCase: true) == 0) return true;
            }
            return false;
        }
        */

        private static void SaveBookmarkData(string bmkPath, string[] filePaths, int[] filePages, string[] folderPaths, int[] folderPages)
        {
            var fileCount = filePaths == null ? 0 : filePaths.Length;
            var folderCount = folderPaths == null ? 0 : folderPaths.Length;
            using (var w = new StreamWriter(bmkPath, append: false))
            {
                for (var i = 0; i < fileCount; i++)
                {
                    w.WriteLine("[Item]");
                    w.WriteLine($"Path={filePaths[i]}");
                    if(filePages != null && filePages[i] >= 0) w.WriteLine($"Page={filePages[i]}");
                }
                for (var i = 0; i < folderCount; i++)
                {
                    w.WriteLine("[Item]");
                    w.WriteLine($"Path={folderPaths[i]}{Path.DirectorySeparatorChar}");
                    if (folderPages != null && folderPages[i] >= 0) w.WriteLine($"Page={folderPages[i]}");
                }
            }
        }
    }
}
