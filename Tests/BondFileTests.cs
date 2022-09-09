using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using InfiniteVariantTool.Core;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Reflection;
using InfiniteVariantTool.Core.Settings;
using InfiniteVariantTool.Core.Serialization;

namespace InfiniteVariantTool.Tests
{
    [TestClass]
    public class BondFileTests
    {

        [TestMethod]
        public void TestOfflineCacheFiles()
        {
            TestCacheFiles(UserSettings.Instance.GameDirectory, Constants.OfflineCacheDirectory);
        }

        [TestMethod]
        public void TestOnlineCacheFiles()
        {
            TestCacheFiles(UserSettings.Instance.GameDirectory, Constants.OnlineCacheDirectory);
        }

        [TestMethod]
        public void TestCmsFiles()
        {
            TestCacheFiles(UserSettings.Instance.GameDirectory, Constants.CmsDirectory);
        }

        public void TestCacheFiles(string baseDirectory, string cacheDirectory)
        {
            string fullCachePath = Path.Combine(baseDirectory, cacheDirectory);
            string outputDirectory = Path.Combine(GetType().Name, cacheDirectory);
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
            Directory.CreateDirectory(outputDirectory);
            foreach ((string filename, byte[] data) in TestUtil.GatherCacheFiles(fullCachePath, true))
            {
                string outputFile = Path.Combine(outputDirectory, Path.GetRelativePath(fullCachePath, filename));
                Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);
                Console.WriteLine(filename);
                BondReader br = new(data);
                var unpacked = br.Read();
                unpacked.Save(outputFile + ".xml");
                BondWriter bw = new(outputFile + ".xml");
                byte[] repacked = bw.Save(outputFile);
                Assert.IsTrue(data.SequenceEqual(repacked));
            }
        }
    }
}
