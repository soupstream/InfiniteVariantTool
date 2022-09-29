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
using InfiniteVariantTool.Core.Serialization;

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

        [TestMethod]
        public void TestOnlineLuaBundles()
        {
            TestLuaBundles(UserSettings.Instance.GameDirectory, Constants.OnlineCacheDirectory);
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
                LuaBundle bundle = LuaBundle.Unpack(data, Game.HaloInfinite);
                byte[] packed = bundle.Pack();

                Assert.IsTrue(data.SequenceEqual(packed));
            }
        }

        private IEnumerable<(string, byte[])> GatherLuaBundles(string dir)
        {
            foreach ((string filename, byte[] data) in TestUtil.GatherCacheFiles(dir))
            {
                BondReader br = new(data);
                var result = br.Read(true);
                foreach (var blob in result.Blobs)
                {
                    if (LuaBundleUtils.IsLuaBundle(blob.Value))
                    {
                        yield return (filename, blob.Value);
                    }
                }
            }
        }
    }
}
