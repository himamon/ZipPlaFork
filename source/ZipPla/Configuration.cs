using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Common
{
    public abstract class Configuration
    {
        //public static readonly string XmlPath = Directory.GetCurrentDirectory() + @"\config.xml";
        private static readonly string XmlPath = Path.Combine(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "config.xml");
        private static readonly string XmlBack = Path.ChangeExtension(XmlPath, "bak");
        private static readonly string RootName =
            ((AssemblyProductAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyProductAttribute))).Product.Replace(" ", "_") + "Config";
        private XElement xmlFile = null;
        private XElement xmlNode = null;
        private static bool loaded = false;
        private Exception lastException = null;

        public Configuration()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                if (prepareMode)
                {
                    var thisType = GetType();
                    SetXElementToThis(thisType, GetPreparedConfigration(thisType));
                }
                else
                {
                    xmlFile = null;
                    if (File.Exists(XmlPath))
                    {
                        xmlFile = XElement.Load(XmlPath);
                        if (xmlFile != null && xmlFile.Name != RootName) xmlFile = null;
                    }
                    if (xmlFile == null)
                    {
                        xmlFile = new XElement(RootName);
                    }
                    else
                    {
                        var thisType = GetType();
                        var first = true;
                        foreach (var xElement in xmlFile.Elements(thisType.Name))
                        //foreach (var xElement in xmlFile.Elements(thisName))
                        {
                            //if (xElement.Name == thisName)
                            {
                                if (first)
                                {
                                    try
                                    {
                                        SetXElementToThis(thisType, xElement);
                                    }
                                    catch { }
                                    if (!prepareMode) xmlNode = xElement;
                                    first = false;
                                }
                                else
                                {
                                    xElement.Remove();
                                }
                            }
                        }
                    }
                }
            }
            catch (NotPreparedException)
            {
                throw;
            }
            catch (Exception exception)
            {
                lastException = exception;
                xmlFile = null;
            }
            finally
            {
                loaded = false;
            }
        }
        
        public static bool ConfigFileExists() => File.Exists(XmlPath);
        public static bool ConfigDataProtection() => File.Exists(XmlBack);

        /// <summary>
        /// 先に ConfigFileExists() を行って下さい
        /// </summary>
        /// <returns></returns>
        public static DateTime GetLastWriteTimeWithoutFileCheckWithException() => File.GetLastWriteTime(XmlPath);

        public Exception GetLastException()
        {
            return lastException;
        }

        public Exception Save()
        {
#if DEBUG
            if (prepareMode) throw new Exception("Save can not be called in prepareMode.");
#else
            if (prepareMode) return new Exception("Save can not be called in prepareMode.");
#endif
            if (xmlFile != null)
            {
                try
                {
                    if (xmlNode == null)
                    {
                        xmlFile.Add(GetXElementFromThis());
                    }
                    else
                    {
                        xmlNode.ReplaceWith(GetXElementFromThis());
                    }

                    // 設定ファイルの保存
                    using (var ms = new MemoryStream())
                    {
                        xmlFile.Save(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        if (File.Exists(XmlPath))
                        {
                            using (var fs = new FileStream(XmlPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                            {
                                using (var fsb = new FileStream(XmlBack, FileMode.CreateNew, FileAccess.Write, FileShare.None)) fs.CopyTo(fsb);
                                fs.SetLength(0);
                                ms.CopyTo(fs);
                            }
                            File.Delete(XmlBack);

                            /*
                            File.Copy(XmlPath, XmlBack, overwrite: false); // 移動では一時的に設定ファイルが存在しない状態になるため危険
                            try
                            {
                                using (var fs = new FileStream(XmlPath, FileMode.Truncate, FileAccess.Write)) ms.CopyTo(fs);
                            }
                            catch
                            {
                                // Truncate したがその後の書き込みに失敗した、等のパターンで XmlBack からの復旧処理を行うか？

                                throw;
                            }
                            File.Delete(XmlBack);
                            */
                        }
                        else
                        {
                            try
                            {
                                using (var fs = new FileStream(XmlPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                                {
                                    if (File.Exists(XmlBack)) throw new IOException();
                                    ms.CopyTo(fs);
                                }
                            }
                            catch
                            {
                                try
                                {
                                    File.Delete(XmlPath);
                                }
                                catch { }
                                throw;
                            }

                            // バックアップが残される可能性があり危険
                            //using (var fs = new FileStream(XmlBack, FileMode.CreateNew, FileAccess.Write)) ms.CopyTo(fs);
                            //File.Move(XmlBack, XmlPath);
                        }
                    }

                    //xmlFile.Save(XmlPath);

                    return null;
                }
                catch (Exception exception)
                {
                    return lastException = exception;
                }
            }
            else
            {
                return lastException;
            }
        }

        public static DateTime GetLastWriteTime()
        {
            try
            {
                if (File.Exists(XmlPath))
                {
                    return GetLastWriteTimeWithoutFileCheckWithException();
                }
                else
                {
                    return new DateTime();
                }
            }
            catch
            {
                return new DateTime();
            }
        }

        private XElement GetXElementFromThis()
        {
            using (var stream = new MemoryStream())
            {
                var serializer = GetXmlSerializer(GetType());
                serializer.Serialize(stream, this);
                stream.Position = 0;
                using (var reader = XmlReader.Create(stream))
                {
                    return XElement.Load(reader);
                }
            }
        }

        //private static readonly Dictionary<Type, XmlSerializer> setXElementToThis_XmlSerializer = new Dictionary<Type, XmlSerializer>();
        private static readonly Dictionary<Type, XmlSerializer> setXElementToThis_XmlSerializer = new Dictionary<Type, XmlSerializer>();
        private static XmlSerializer GetXmlSerializer(Type type)
        {
            if (!setXElementToThis_XmlSerializer.TryGetValue(type, out var serializer))
            {
                var temp = loaded;
                loaded = true;
                serializer = new XmlSerializer(type);
                loaded = temp;
                setXElementToThis_XmlSerializer[type] = serializer;
            }
            return serializer;
        }

        public class NotPreparedException : Exception
        {
            public NotPreparedException(string message) : base(message) { }
        }

        private static Dictionary<Type, Configuration> GetPreparedConfigration_Dictionary = null;
        private static Configuration GetPreparedConfigration(Type type)
        {
            if (GetPreparedConfigration_Dictionary.TryGetValue(type, out var resultFromDictionary))
            {
                return resultFromDictionary;
            }
            else throw new NotPreparedException($"Not prepared ({type.Name})");
        }

        private static Task PrepareXmlSerializer_Task = null;
        public static void PrepareXmlSerializer(params Type[] configrationTypes)
        {
#if DEBUG
            foreach (var type in configrationTypes)
            {
                // IsSubclassOf: This method also returns false if c and the current Type are equal.
                if (!type.IsSubclassOf(typeof(Configuration)))
                {
                    throw new Exception($"{type.Name} is not Configration.");
                }
            }
#endif
            prepareMode = true;
            GetPreparedConfigration_Dictionary = new Dictionary<Type, Configuration>();
            if (configrationTypes.Length == 0) return;
            var nextTask = null as Task;

            loaded = true;

            PrepareXmlSerializer_Task = Task.Run(() =>
            {
                XElement xmlFile = null;
                Exception lastException = null;
                for (var i = 0; i < configrationTypes.Length; i++)
                {
                    var type = configrationTypes[i];
                    var serializer = GetXmlSerializer(type);
                    Configuration configration = null;
                    try
                    {
                        if (lastException != null) throw lastException;
                        if (i == 0 && File.Exists(XmlPath))
                        {
                            xmlFile = XElement.Load(XmlPath);
                            if (xmlFile != null && xmlFile.Name != RootName) xmlFile = null;
                        }
                        configration = GetConfigration(serializer, type, xmlFile);
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        if (configration == null)
                        {
                            configration = GetEmptyConfigration(type);
                        }
                        configration.lastException = ex;
                    }
                    GetPreparedConfigration_Dictionary.Add(type, configration);
                }
                loaded = false;
            });
        }

        public static void WaitPrepareTask()
        {
            if (PrepareXmlSerializer_Task != null)
            {
                PrepareXmlSerializer_Task.Wait();
                PrepareXmlSerializer_Task = null;
            }
        }

        private static Configuration GetEmptyConfigration(Type type)
        {
            var temp = loaded;
            loaded = true;
            var configration = Activator.CreateInstance(type) as Configuration;
            loaded = temp;
            return configration;
        }

        private static Configuration GetConfigration(XmlSerializer serializer, Type type, XElement xmlFile)
        {
            if (xmlFile == null) return GetEmptyConfigration(type);
            var xElement = xmlFile.Element(type.Name);
            return xElement != null ? GetConfigration(serializer, xElement) : GetEmptyConfigration(type);
        }

        private static Configuration GetConfigration(XmlSerializer serializer, XElement xElement)
        {
            using (var stream = new MemoryStream())
            {
                xElement.Save(stream);
                stream.Position = 0;
                return serializer.Deserialize(stream) as Configuration;
            }
        }

        private static bool prepareMode = false;

        public static void ClosePrepareMode()
        {
            if (prepareMode)
            {
#if DEBUG
                foreach (var pair in setXElementToThis_XmlSerializer)
                {
                    if (!(pair.Value is XmlSerializer)) throw new Exception($"{pair.Key.Name} is prepared but not loaded in prepareMode.");
                }
#endif
                prepareMode = false;
                GetPreparedConfigration_Dictionary = null;
            }
        }

        private void SetXElementToThis(Type thisType, XElement xElement)
        {
            var serializer = GetXmlSerializer(thisType);
            var configration = GetConfigration(serializer, xElement);
            SetXElementToThis(thisType, configration);
        }

        private void SetXElementToThis(Type thisType, object configration)
        {
                                                          //ZipPla.Program.RunTimeMeasure?.Block("after sirialize");
            foreach (var element in thisType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                element.SetValue(this, element.GetValue(configration));
            }

            foreach (var element in thisType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                element.SetValue(this, element.GetValue(configration));
            }

            var asConfigration = configration as Configuration;
            var newException = asConfigration.lastException;
            if (newException != null)
            {
                lastException = newException;
            }
        }

        /// <summary>
        /// DictionaryをKeyAndValueのListに変換する
        /// </summary>
        /// <typeparam name="TKey">Dictionaryのキーの型</typeparam>
        /// <typeparam name="TValue">Dictionaryの値の型</typeparam>
        /// <param name="dic">変換するDictionary</param>
        /// <returns>変換されたKeyAndValueの配列</returns>
        public static KeyAndValue<TKey, TValue>[] ConvertDictionaryToArray<TKey, TValue>(Dictionary<TKey, TValue> dic)
        {
            if (dic == null)
            {
                return null;
            }
            else
            {
                return (from pair in dic select new KeyAndValue<TKey, TValue>(pair)).ToArray();
            }
        }

        /// <summary>
        /// KeyAndValueのListをDictionaryに変換する
        /// </summary>
        /// <typeparam name="TKey">KeyAndValueのKeyの型</typeparam>
        /// <typeparam name="TValue">KeyAndValueのValueの型</typeparam>
        /// <param name="array">変換するKeyAndValueのIEnumerableクラス</param>
        /// <returns>変換されたDictionary</returns>
        public static Dictionary<TKey, TValue> ConvertArrayToDictionary<TKey, TValue>(IEnumerable<KeyAndValue<TKey, TValue>> array)
        {
            var dic = new Dictionary<TKey, TValue>();
            if (array != null) foreach (var pair in array) dic.Add(pair.Key, pair.Value);
            return dic;
        }
    }

    /// <summary>
    /// シリアル化できる、KeyValuePairに代わる構造体
    /// </summary>
    /// <typeparam name="TKey">Keyの型</typeparam>
    /// <typeparam name="TValue">Valueの型</typeparam>
    [Serializable]
    public struct KeyAndValue<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;

        public KeyAndValue(KeyValuePair<TKey, TValue> pair)
        {
            Key = pair.Key;
            Value = pair.Value;
        }
    }
}
