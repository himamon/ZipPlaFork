using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ZipPla
{
    public class SmartFolder : IEquatable<SmartFolder>
    {
        public const string ExtensionWithoutPeriodInLower = "kdk";
        public readonly List<SmartFolderItem> Items = new List<SmartFolderItem>();
        public FileSystemCondition DefaultCondition = null;

        public SmartFolder() { }
        
        public SmartFolder(string smartFolderPath)
        {
            using (var r = new StreamReader(smartFolderPath))
            {
                string line = r.ReadLine();
                while (line != null)
                {
                    if (line == "[DefaultCondition]")
                    {
                        line = r.ReadLine();
                        if (DefaultCondition == null)
                        {
                            DefaultCondition = FileSystemCondition.FromStreamReader(r, ref line);
                        }
                    }
                    else if (line == "[Item]")
                    {
                        line = r.ReadLine();
                        var item = SmartFolderItem.FromStreamReader(r, ref line); // TryParse を持たない item.Condition は無視される
                        if (item != null)
                        {
                            item.Condition = FileSystemCondition.FromStreamReader(r, ref line);
                            Items.Add(item);
                        }
                    }
                    else line = r.ReadLine();
                }
            }
        }

        private static readonly Regex pathMatchRegex = new Regex($"\\.{ExtensionWithoutPeriodInLower}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static bool IsSmartFolderPath(string path)
        {
            return path != null && pathMatchRegex.IsMatch(path);
        }

        public void SaveToFile(string smartFolderPath)
        {
            using (var w = new StreamWriter(smartFolderPath, append: false))
            {
                if(DefaultCondition != null)
                {
                    w.WriteLine("[DefaultCondition]");
                    DefaultCondition.ToStreamWriter(w);
                }

                foreach(var item in Items)
                {
                    w.WriteLine("[Item]");
                    item.ToStreamWriter(w); // TryParse を持たない item.Condition は無視される
                    item.Condition?.ToStreamWriter(w);
                }
            }
        }

        private class Equivalence<KeyType, ValueType> :IEquatable<Equivalence<KeyType, ValueType>> where KeyType:IEquatable<KeyType>
        {
            public readonly KeyType Key;
            public readonly ValueType Value;

            public Equivalence(KeyType key, ValueType value)
            {
                Key = key;
                Value = value;
            }

            public bool Equals(Equivalence<KeyType, ValueType> other)
            {
                return other != null && Key.Equals(other.Key);
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Equivalence<KeyType, ValueType>);
            }

            public override int GetHashCode()
            {
                return Key.GetHashCode();
            }
        }

        public FileInfo[] GetFiles(string searchPattern, SearchOption searchOption, BackgroundWorker bw)
        {
            var result = new HashSet<Equivalence<string, FileInfo>>();
            foreach (var item in Items)
            {
                var condition = item.Condition;
                if (condition == null) condition = DefaultCondition;
                try
                {
                    if (searchOption == SearchOption.AllDirectories)
                    {
                        var infos = Program.GetFilesInAllDirectoriesOnErrorResumeNext(new DirectoryInfo(item.Path), searchPattern, bw);
                        if (bw?.CancellationPending == true) return null;
                        result.UnionWith(from info in infos
                                         where condition == null || condition.Test(info)
                                         select new Equivalence<string, FileInfo>(info.FullName, info));
                    }
                    else
                    {
                        result.UnionWith(from info in (new DirectoryInfo(item.Path)).GetFiles(searchPattern, SearchOption.TopDirectoryOnly)
                                         where condition == null || condition.Test(info)
                                         select new Equivalence<string, FileInfo>(info.FullName, info));
                    }
                }
                catch
                {
                    if (!item.ResumeOnError) throw;
                }
                if (bw?.CancellationPending == true) return null;
            }
            return (from e in result select e.Value).ToArray();
        }

        public DirectoryInfo[] GetDirectories(string searchPattern, BackgroundWorker bw) => GetDirectories(searchPattern, SearchOption.TopDirectoryOnly, bw);
        public DirectoryInfo[] GetDirectories(string searchPattern, SearchOption searchOption, BackgroundWorker bw)
        {
            var result = new HashSet<Equivalence<string, DirectoryInfo>>();
            foreach (var item in Items)
            {
                var condition = item.Condition;
                if (condition == null) condition = DefaultCondition;
                try
                {
                    result.UnionWith(from info in (new DirectoryInfo(item.Path)).GetDirectories(searchPattern, searchOption)
                                where condition == null || condition.Test(info)
                                select new Equivalence<string, DirectoryInfo>(info.FullName, info));
                }
                catch
                {
                    if (!item.ResumeOnError) throw;
                }
                if (bw?.CancellationPending == true) return null;
            }
            return (from e in result select e.Value).ToArray();
        }

        public bool Equals(SmartFolder other)
        {
            if (other == null) return false;
            return DefaultCondition == other.DefaultCondition && Items.SequenceEqual(other.Items);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SmartFolder);
        }

        public override int GetHashCode()
        {
            var result = DefaultCondition.GetHashCode();
            foreach(var item in Items)
            {
                if(item != null) result ^= item.GetHashCode();
            }
            return result ^ Items.Count;
        }
    }

    public class SmartFolderItem : IEquatable<SmartFolderItem>
    {
        public bool ResumeOnError;
        public string Path;
        public FileSystemCondition Condition;

        private static readonly ClassStreamReaderWriter<SmartFolderItem> rw = new ClassStreamReaderWriter<SmartFolderItem>(
            BindingFlags.GetField | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public, RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static SmartFolderItem FromStreamReader(StreamReader r, ref string line)
        {
            return rw.FromStreamReader(r, ref line);
        }

        public void ToStreamWriter(StreamWriter w)
        {
            rw.ToStreamWriter(w, this);
        }

        public bool Equals(SmartFolderItem other)
        {
            if (other == null) return false;
            return Path == other.Path && ResumeOnError == other.ResumeOnError && Condition == other.Condition;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SmartFolderItem);
        }

        public override int GetHashCode()
        {
            return Path.GetHashCode() ^ ResumeOnError.GetHashCode() ^ Condition.GetHashCode();
        }
    }

    public class FileSystemCondition : IEquatable<FileSystemCondition>
    {
        // public field の型 T は全て nullable かつ public static bool TryParse(string s, out T result) を持ちこれが ToString と対応していること
        // また変数名は IgnoreCase の意味で全て異なった名前であること
        public RegexPathFilter PathFilter;

        public bool Equals(FileSystemCondition other)
        {
            if (other == null) return false;
            return PathFilter != null ? PathFilter.Equals(other.PathFilter) : other.PathFilter == null;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as FileSystemCondition);
        }

        public override int GetHashCode()
        {
            return PathFilter != null ? PathFilter.GetHashCode() : 0;
        }

        public static bool operator ==(FileSystemCondition a, FileSystemCondition b)
        {
            return a as object != null ? a.Equals(b) : b as object == null;
        }

        public static bool operator !=(FileSystemCondition a, FileSystemCondition b)
        {
            return a as object != null ? !a.Equals(b) : b as object != null;
        }

        public bool Test(FileSystemInfo info)
        {
            return PathFilter.Test(info);
        }

        private static readonly ClassStreamReaderWriter<FileSystemCondition> rw = new ClassStreamReaderWriter<FileSystemCondition>(
            BindingFlags.GetField | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        
        public static FileSystemCondition FromStreamReader(StreamReader r, ref string line)
        {
            return rw.FromStreamReader(r, ref line);
        }

        public void ToStreamWriter(StreamWriter w)
        {
            rw.ToStreamWriter(w, this);
        }

    }

    public class ClassStreamReaderWriter<T> where T : new()
    {
        private readonly Func<object, object>[] memberGetValueArray;
        private readonly Action<object, object>[] memberSetValueArray;
        private readonly MethodInfo[] memberTryInfoParseArray;
        private readonly Regex[] lineRegexArray;
        private readonly string[] linePrefixArray;
        private readonly int memberCount;
        public ClassStreamReaderWriter(BindingFlags bindingAttr, RegexOptions options)
        {
            var memberGetValueList = new List<Func<object, object>>();
            var memberSetValueList = new List<Action<object, object>>();
            var memberTryParseList = new List<MethodInfo>();
            var lineRegexList = new List<Regex>();
            var linePrefixList = new List<string>();

            Action<Type, Func<object, object>, Action<object, object>, string> set = (memberType, memberGetValue, memberSetValue, memberName) =>
            {
                MethodInfo memberTryParseInfo;
                if (memberType == typeof(string))
                {
                    memberTryParseInfo = null;
                }
                else
                {
                    memberTryParseInfo = memberType.GetMethod("TryParse", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string), memberType.MakeByRefType() }, null);
                    if (memberTryParseInfo == null || memberTryParseInfo.ReturnType != typeof(bool)) return;
                }
                memberGetValueList.Add(memberGetValue);
                memberSetValueList.Add(memberSetValue);
                memberTryParseList.Add(memberTryParseInfo);
                lineRegexList.Add(new Regex(@"^\s*" + memberName + @"\s*=\s*(.*?)\s*$", options));
                linePrefixList.Add(memberName + "=");
            };
            if ((bindingAttr & BindingFlags.GetField) == BindingFlags.GetField)
            {
                foreach (var info in typeof(T).GetFields(bindingAttr & (BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)))
                {
                    set(info.FieldType, info.GetValue, info.SetValue, info.Name);
                }
            }
            if ((bindingAttr & BindingFlags.GetProperty) == BindingFlags.GetProperty)
            {
                foreach (var info in typeof(T).GetProperties(bindingAttr & (BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)))
                {
                    set(info.PropertyType, info.GetValue, info.SetValue, info.Name);
                }
            }
            memberGetValueArray = memberGetValueList.ToArray();
            memberSetValueArray = memberSetValueList.ToArray();
            memberTryInfoParseArray = memberTryParseList.ToArray();
            lineRegexArray = lineRegexList.ToArray();
            linePrefixArray = linePrefixList.ToArray();
            memberCount = memberGetValueArray.Length;
        }

        public T FromStreamReader(StreamReader r, ref string line)
        {
            T result = default(T);
            var readArray = new bool[memberCount];
            while (line != null)
            {
                var trimedLine = line.Trim();
                if (trimedLine != "")
                {
                    var setValue = false;
                    for (var i = 0; i < memberCount; i++)
                    {
                        var m = lineRegexArray[i].Match(trimedLine);
                        if (m.Success)
                        {
                            if (!readArray[i])
                            {
                                var body = m.Groups[1].Value;
                                object value = null;
                                var tryParse = memberTryInfoParseArray[i];
                                bool success;
                                if (tryParse == null)
                                {
                                    value = body;
                                    success = true;
                                }
                                else
                                {
                                    var prms = new object[] { body, null };
                                    if(success = (bool)tryParse.Invoke(null, prms))
                                    {
                                        value = prms[1];
                                    }

                                }
                                if (success)
                                {
                                    if (result == null) result = new T();
                                    memberSetValueArray[i](result, value);
                                    readArray[i] = true;
                                    setValue = true;
                                }
                            }
                            break;
                        }
                    }
                    if (!setValue)
                    {
                        return result;
                    }
                }
                line = r.ReadLine();
            }
            return result;
        }

        public void ToStreamWriter(StreamWriter w, T target)
        {
            for (var i = 0; i < memberCount; i++)
            {
                var valueObject = memberGetValueArray[i](target);
                if (valueObject == null) continue;
                w.WriteLine(linePrefixArray[i] + valueObject.ToString());
            }
        }
    }

    public class RegexPathFilter : IEquatable<RegexPathFilter>
    {
        private readonly string pattern;
        private readonly Regex regex;

        public bool Test(FileSystemInfo info)
        {
            return info is DirectoryInfo ? TestForDirectory(info.FullName) : TestForFile(info.FullName);
        }

        public bool Test(FileInfo info)
        {
            return TestForFile(info.FullName);
        }

        public bool Test(DirectoryInfo info)
        {
            return TestForDirectory(info.FullName);
        }
        
        public bool TestForFile(string path)
        {
            if (path == null) return false;
            return regex == null || regex.IsMatch(path);
        }

        public bool TestForDirectory(string path)
        {
            if (path == null) return false;
            var lastChar = path == "" ? '\0' : path.Last();
            if (!(lastChar == Path.DirectorySeparatorChar || lastChar == Path.AltDirectorySeparatorChar)) path += Path.DirectorySeparatorChar;
            return regex == null || regex.IsMatch(path);
        }


        public RegexPathFilter(string pattern)
        {
            if (pattern == null) throw new ArgumentNullException(nameof(pattern));
            if (ApplicationInfo.TryParseItemGroup(pattern, out regex))
            {
                this.pattern = pattern;
            }
            else
            {
                throw new ArgumentException(null, nameof(pattern));
            }
        }

        private RegexPathFilter(string pattern, Regex regex)
        {
            this.pattern = pattern;
            this.regex = regex;
        }

        public static bool TryParse(string s, out RegexPathFilter result)
        {
            if (ApplicationInfo.TryParseItemGroup(s, out var r))
            {
                result = new RegexPathFilter(s, r);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public override string ToString()
        {
            return pattern;
        }

        public bool Equals(RegexPathFilter other)
        {
            if (other == null) return false;
            return pattern == other.pattern;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RegexPathFilter);
        }

        public override int GetHashCode()
        {
            return pattern.GetHashCode();
        }
    }
}
