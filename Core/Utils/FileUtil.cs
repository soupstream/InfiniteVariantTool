using InfiniteVariantTool.Core.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteVariantTool.Core.Utils
{
    public static class FileUtil
    {
        public static FileExtension DetectFileType(byte[] data)
        {
            if (Util.ArrayStartsWith(data, Constants.pngSignature))
            {
                return FileExtension.Png;
            }
            else if (Util.ArrayStartsWith(data, Constants.jpgSignature))
            {
                return FileExtension.Jpg;
            }
            else if (IsJson(data))
            {
                return FileExtension.Json;
            }
            else if (IsStrings(data))
            {
                return FileExtension.Strings;
            }
            else if (LuaBundleUtils.IsLuaBundle(data))
            {
                return FileExtension.Luabundle;
            }
            else
            {
                return FileExtension.Bin;
            }
        }

        private static bool IsJson(byte[] data)
        {
            return data[0] == '{' && data[^1] == '}';
        }

        // heuristic to identify .strings blobs
        private static bool IsStrings(byte[] data)
        {
            if (data.Length < 64)
            {
                return false;
            }

            int strlen = 0;
            for (int i = 0; i < 64; i++)
            {
                // shouldn't start with 0 or have multiple 0s in a row
                if (data[i] == 0)
                {
                    if (i == 0 || data[i - 1] == 0)
                    {
                        return false;
                    }
                    if (strlen > 8)
                    {
                        return true;
                    }
                    strlen = 0;
                }
                else if (data[i] >= 32)
                {
                    strlen++;
                }
                else
                {
                    return false;
                }    
            }
            return true;
        }

        public static string MakeValidFilename(string filename, char replacement = '_')
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                filename = filename.Replace(c, replacement);
            }
            return filename;
        }

        public static async Task<string> GetBuildNumber(string gameDir)
        {
            FileVersionInfo exeInfo = FileVersionInfo.GetVersionInfo(Path.Combine(gameDir, Constants.GameExeName));
            string? version = exeInfo.ProductVersion;
            if (version != null)
            {
                return version[..^2];   // remove ".0" from end
            }
            else
            {
                // if exe doesn't have version info, parse it from version.txt
                string versionFilePath = Path.Combine(gameDir, "version.txt");
                int shortVersionLength = 6;
                using FileStream stream = File.OpenRead(versionFilePath);
                byte[] buffer = new byte[shortVersionLength];
                await stream.ReadAsync(buffer.AsMemory(0, shortVersionLength));
                string shortVersion = Encoding.ASCII.GetString(buffer);
                return "6.100" + shortVersion[..2] + ".1" + shortVersion[2..];
            }
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

        public static string RemoveSpecificExtension(string path, string extension)
        {
            if (path.EndsWith(extension, StringComparison.InvariantCultureIgnoreCase))
            {
                return path[..^extension.Length];
            }
            else
            {
                return path;
            }
        }
    }

    // file extension "enum"
    public class FileExtension
    {
        public readonly string Value;
        private FileExtension(string value)
        {
            Value = value;
        }

        public static readonly FileExtension Bin = new(".bin");
        public static readonly FileExtension Xml = new(".xml");
        public static readonly FileExtension Json = new(".json");
        public static readonly FileExtension Png = new(".png");
        public static readonly FileExtension Jpg = new(".jpg");
        public static readonly FileExtension Strings = new(".strings");
        public static readonly FileExtension Luabundle = new(".luabundle");
        public static readonly FileExtension DebugScriptSource = new(".debugscriptsource");
        public static readonly List<FileExtension> Extensions = new() { Bin, Xml, Json, Png, Jpg, Strings, Luabundle, DebugScriptSource };
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
