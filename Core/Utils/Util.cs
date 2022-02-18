using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Globalization;
using System.Threading;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using InfiniteVariantTool.Core.Serialization;
using InfiniteVariantTool.Core.Cache;
using InfiniteVariantTool.Core.Settings;
using System.Reflection;

namespace InfiniteVariantTool.Core.Utils
{
    public class Util
    {
        public static byte[] ListToBlob(XElement list)
        {
            List<byte> blob = new();
            foreach (var item in list.Elements())
            {
                blob.Add(byte.Parse(item.GetText()));
            }
            return blob.ToArray();
        }

        public static bool IsNodeGUID(XElement node)
        {
            if (node.Elements().Count() == 4)
            {
                BondType[] schema = { BondType.uint32, BondType.uint16, BondType.uint16, BondType.uint64 };
                for (int i = 0; i < 4; i++)
                {
                    if (node.Elements().ElementAt(i).Name != schema[i].ToString())
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public static bool ArrayStartsWith<T>(T[] target, T[] pattern) where T : IEquatable<T>
        {
            if (target.Length < pattern.Length)
            {
                return false;
            }

            for (int i = 0; i < pattern.Length; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(target[i], pattern[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ArrayContains<T>(T[] target, T[] pattern) where T : IEquatable<T>
        {
            for (int i = 0; i < target.Length - pattern.Length; i++)
            {
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (!EqualityComparer<T>.Default.Equals(target[i + j], pattern[j]))
                    {
                        break;
                    }
                    else if (j == pattern.Length - 1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsJson(byte[] data)
        {
            return data[0] == '{' && data[^1] == '}';
        }

        public static string NullTerminate(string str)
        {
            int idx = str.IndexOf('\0');
            if (idx == -1)
            {
                return str;
            }
            else
            {
                return str[..idx];
            }
        }

        public static bool NullableSequenceEqual<T>(IEnumerable<T>? first, IEnumerable<T>? second)
        {
            if (first != null && second != null)
            {
                return first.SequenceEqual(second);
            }
            else
            {
                return first == second;
            }
        }

        public static string GetBuildNumber()
        {
            string filename = Path.Combine(UserSettings.Instance.GameDirectory, Constants.OfflineCacheDirectory, "en-US", "other", "CacheMap.wcache");
            CacheMap cm = new(filename);
            foreach (var entry in cm.Map)
            {
                Match match = Regex.Match(entry.Value.Metadata.Url!, "^https://discovery-infiniteugc-intone.test.svc.halowaypoint.com/hi/manifests/builds/([^/]*)/game$");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            throw new Exception();
        }

        public static string MakeValidFilename(string filename, char replacement = '_')
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                filename = filename.Replace(c, replacement);
            }
            return filename;
        }

        public static string ReadResource(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = assembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(name));
            using Stream stream = assembly.GetManifestResourceStream(resourcePath) ?? throw new Exception();
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }
    }

    public class Culture : IDisposable
    {
        private readonly CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;

        public Culture(string cultureName)
            : this(CultureInfo.GetCultureInfo(cultureName))
        {
        }

        public Culture(CultureInfo culture)
        {
            Thread.CurrentThread.CurrentCulture = culture;
        }

        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
        }
    }

    public class FileNameDeduper
    {
        protected HashSet<string> names;

        public FileNameDeduper()
        {
            names = new();
        }

        public virtual string Dedupe(string filename)
        {
            filename = Path.GetRelativePath(".", filename).TrimEnd(Path.DirectorySeparatorChar);
            string basename = Path.Join(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename));
            string extension = Path.GetExtension(filename);
            return Dedupe(basename, extension);
        }

        public string Dedupe(string basename, string extension)
        {
            string fullname = basename + extension;
            for (int i = 1; names.Contains(fullname); i++)
            {
                fullname = basename + "." + i + extension;
            }
            names.Add(fullname);
            return fullname;
        }
    }

    public class DirectoryFileNameDeduper : FileNameDeduper
    {
        public DirectoryFileNameDeduper(string directory)
        {
            if (Directory.Exists(directory))
            {
                names = new(Directory.EnumerateFileSystemEntries(directory).Select(file => Path.Combine(directory, file)));
            }
            else
            {
                names = new();
            }
        }

        public override string Dedupe(string filename)
        {
            string basename = Path.Join(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename));
            string extension = Path.GetExtension(filename);
            return Dedupe(basename, extension);
        }
    }

    public class FileData
    {
        public string Name { get; set; }
        public byte[] Data { get; set; }

        public FileData(string name, byte[] data)
        {
            Name = name;
            Data = data;
        }

        public void Save()
        {
            File.WriteAllBytes(Name, Data);
        }

        public void Save(string path)
        {
            File.WriteAllBytes(Path.Join(path, Name), Data);
        }
    }
}
