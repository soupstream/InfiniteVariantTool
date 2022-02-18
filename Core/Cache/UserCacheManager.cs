using InfiniteVariantTool.Core.Variants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace InfiniteVariantTool.Core.Cache
{
    public class UserCacheManager
    {
        public List<UserVariantEntry> entries { get; private set; }
        private string cacheDirectory;

        public UserCacheManager(string cacheDirectory)
        {
            this.cacheDirectory = cacheDirectory;
            entries = new();
        }

        public void LoadEntries()
        {
            entries = GetEntries().ToList();
        }

        public IEnumerable<UserVariantEntry> GetEntries()
        {
            foreach (string filepath in Directory.GetFiles(cacheDirectory, "*.xml", SearchOption.AllDirectories))
            {
                if (VariantMetadata.TryLoadMetadata(filepath) is VariantMetadata metadata)
                {
                    yield return new UserVariantEntry(filepath, metadata);
                }
            }
        }

        public Variant LoadVariant(UserVariantEntry entry)
        {
            return Variant.Load(entry.Path);
        }

        public void RemoveVariant(string variantFilename)
        {
            entries.RemoveAll(entry => entry.Path.StartsWith(variantFilename));

            // remove variant metadata and files
            if (File.Exists(variantFilename))
            {
                File.Delete(variantFilename);
            }
            else
            {
                return;
            }
            string variantDirectory = Path.GetDirectoryName(variantFilename)!;
            string filesDirectory = Path.Combine(variantDirectory, "files");
            if (Directory.Exists(filesDirectory))
            {
                Directory.Delete(filesDirectory, true);
            }

            // remove empty parent directories
            string currentPath = Path.GetFullPath(variantDirectory);
            string basePath = Path.GetFullPath(cacheDirectory);
            while (!currentPath.Equals(basePath, StringComparison.InvariantCultureIgnoreCase) && Directory.Exists(currentPath)
                && !Directory.EnumerateFileSystemEntries(currentPath).Any())
            {
                Directory.Delete(currentPath);
                currentPath = Path.GetDirectoryName(currentPath)!;
            }
        }
    }

    public class UserVariantEntry
    {
        public string Path { get; set; }
        public VariantMetadata Metadata { get; set; }
        public bool? Enabled { get; set; }
        public UserVariantEntry(string path, VariantMetadata metadata)
        {
            Path = path;
            Metadata = metadata;
        }
    }
}
