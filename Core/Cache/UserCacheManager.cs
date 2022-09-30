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
        public List<VariantAsset> Entries { get; private set; }
        private string cacheDirectory;

        public UserCacheManager(string cacheDirectory)
        {
            this.cacheDirectory = cacheDirectory;
            Entries = new();
        }

        public async Task LoadEntries()
        {
            Entries.Clear();
            foreach (string filePath in VariantAsset.FindVariants(cacheDirectory))
            {
                Entries.Add(await VariantAsset.Load(filePath, false));
            }
        }

        public void RemoveVariant(string variantFilePath)
        {
            Entries.RemoveAll(entry => entry.FilePath == variantFilePath);

            string variantDirectory = Path.GetDirectoryName(variantFilePath)!;
            if (Directory.Exists(variantDirectory))
            {
                Directory.Delete(variantDirectory, true);
            }

            // remove empty parent directories
            string currentPath = Path.GetFullPath(Path.GetDirectoryName(variantDirectory)!);
            string basePath = Path.GetFullPath(cacheDirectory);
            while (!currentPath.Equals(basePath, StringComparison.InvariantCultureIgnoreCase) && Directory.Exists(currentPath)
                && !Directory.EnumerateFileSystemEntries(currentPath).Any())
            {
                Directory.Delete(currentPath);
                currentPath = Path.GetDirectoryName(currentPath)!;
            }
        }
    }
}
