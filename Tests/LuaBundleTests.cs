using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using InfiniteVariantTool.Core;
using System.Xml.Linq;
using System.Text;
using System.Reflection;
using InfiniteVariantTool.Core.Settings;
using InfiniteVariantTool.Core.Cache;

namespace InfiniteVariantTool.Tests
{
    [TestClass]
    public class LuaBundleTests
    {
        [TestMethod]
        public void TestOfflineLuaBundles()
        {
            TestLuaBundles(UserSettings.Instance.GameDirectory, Constants.OfflineCacheDirectory);
        }

        public void TestLuaBundles(string baseDirectory, string cacheDirectory)
        {
            string fullCachePath = Path.Combine(baseDirectory, cacheDirectory);
            string outputDirectory = Path.Combine(GetType().Name, cacheDirectory);
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
            Directory.CreateDirectory(outputDirectory);
            foreach ((string filename, byte[] data) in GatherLuaBundles(fullCachePath))
            {
                string outputFile = Path.Combine(outputDirectory, Path.GetRelativePath(fullCachePath, filename));
                string outputDir = Path.Combine(Path.GetDirectoryName(outputFile)!, Path.GetFileNameWithoutExtension(outputFile));
                Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);
                Console.WriteLine(filename);
                LuaBundleUnpacker unpacker = new(data);
                LuaBundle bundle = unpacker.Save(outputDir);

                LuaBundlePacker packer = new(outputDir);
                byte[] packed = packer.Save(outputFile);

                Assert.IsTrue(data.SequenceEqual(packed));
            }
        }

        private IEnumerable<(string, byte[])> GatherLuaBundles(string dir)
        {
            foreach ((string filename, byte[] data) in TestUtil.GatherCacheFiles(dir))
            {
                CacheFile cacheFile = new(data, ContentType.AutoDetect, null);
                cacheFile.Content.SetName(filename, new());
                if (cacheFile.Content is CacheFileContentBond bondContent)
                {
                    foreach (CacheFileContentBytes blob in bondContent.Blobs)
                    {
                        if (blob.Type == ContentType.Luabundle)
                        {
                            yield return (blob.Name, blob.Data);
                        }
                    }
                }
            }
        }
    }
}
