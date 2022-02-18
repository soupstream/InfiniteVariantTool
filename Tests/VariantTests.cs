using InfiniteVariantTool.Core;
using InfiniteVariantTool.Core.Cache;
using InfiniteVariantTool.Core.Settings;
using InfiniteVariantTool.Core.Variants;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace InfiniteVariantTool.Tests
{
    [TestClass]
    public class VariantTests
    {
        [TestMethod]
        public void TestOfflineVariants()
        {
            string outputDir = Path.Combine(GetType().Name, Constants.OfflineCacheDirectory);
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
            Directory.CreateDirectory(outputDir);

            HashSet<string> mapVariantFilenames = new();
            HashSet<string> ugcVariantFilenames = new();
            HashSet<string> engineVariantFilenames = new();
            foreach (string cacheFilename in TestUtil.GatherCacheMapFiles(Path.Combine(UserSettings.Instance.GameDirectory, Constants.OfflineCacheDirectory)))
            {
                CacheMap cm = new(cacheFilename);
                foreach (var entry in cm.Map)
                {
                    if (entry.Value.Metadata.Url!.StartsWith("https://discovery-"))
                    {
                        HashSet<string> filenameSet;
                        if (entry.Value.Metadata.Url.Contains("/maps/"))
                        {
                            filenameSet = mapVariantFilenames;
                        }
                        else if (entry.Value.Metadata.Url.Contains("/ugcGameVariants/"))
                        {
                            filenameSet = ugcVariantFilenames;
                        }
                        else if (entry.Value.Metadata.Url.Contains("/engineGameVariants/"))
                        {
                            filenameSet = engineVariantFilenames;
                        }
                        else
                        {
                            continue;
                        }
                        string path = Path.Combine(Path.GetDirectoryName(cacheFilename)!, entry.Key.ToString());
                        if (File.Exists(path))
                        {
                            filenameSet.Add(path);
                        }
                        else
                        {
                            path = Path.Combine(UserSettings.Instance.GameDirectory, Constants.OfflineCacheDirectory, "common", "other", entry.Key.ToString());
                            if (File.Exists(path))
                            {
                                filenameSet.Add(path);
                            }
                        }
                    }
                }
            }

            foreach (string filename in mapVariantFilenames)
            {
                CacheFile cacheFile = new(filename, ContentType.Bond, true);
                MapVariantMetadata metadata = new(cacheFile.Doc);
                CheckVariant(metadata, cacheFile, filename);
            }

            foreach (string filename in ugcVariantFilenames)
            {
                CacheFile cacheFile = new(filename, ContentType.Bond, true);
                UgcGameVariantMetadata metadata = new(cacheFile.Doc);
                CheckVariant(metadata, cacheFile, filename);
            }

            foreach (string filename in engineVariantFilenames)
            {
                CacheFile cacheFile = new(filename, ContentType.Bond, true);
                EngineGameVariantMetadata metadata = new(cacheFile.Doc);
                CheckVariant(metadata, cacheFile, filename);
            }
        }
        
        private void CheckVariant(VariantMetadata metadata, CacheFile cacheFile, string filename)
        {
            Console.WriteLine(filename);
            string relativeFilename = Path.GetRelativePath(UserSettings.Instance.GameDirectory, filename);
            string outputFilename = Path.Combine(GetType().Name, relativeFilename);
            if (!Directory.Exists(Path.GetDirectoryName(outputFilename)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputFilename)!);
            }
            XElement doc = metadata.Save(outputFilename + ".xml");
            Assert.IsTrue(XNode.DeepEquals(doc, cacheFile.Doc));
        }
    }
}
