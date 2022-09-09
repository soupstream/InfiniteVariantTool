using InfiniteVariantTool.Core;
using InfiniteVariantTool.Core.BondSchema;
using InfiniteVariantTool.Core.Cache;
using InfiniteVariantTool.Core.Serialization;
using InfiniteVariantTool.Core.Settings;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace InfiniteVariantTool.Tests
{
    [TestClass]
    public class CacheMapTests
    {

        [TestMethod]
        public void TestOfflineCacheMaps()
        {
            TestCacheMaps(UserSettings.Instance.GameDirectory, Constants.OfflineCacheDirectory);
        }

        [TestMethod]
        public void TestOnlineCacheMaps()
        {
            TestCacheMaps(UserSettings.Instance.GameDirectory, Constants.OnlineCacheDirectory);
        }

        public void TestCacheMaps(string baseDirectory, string cacheDirectory)
        {
            string fullCachePath = Path.Combine(baseDirectory, cacheDirectory);
            string outputDirectory = Path.Combine(GetType().Name, cacheDirectory);
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
            Directory.CreateDirectory(outputDirectory);

            foreach (string filename in TestUtil.GatherCacheMapFiles(fullCachePath))
            {
                string outputFile = Path.Combine(outputDirectory, Path.GetRelativePath(fullCachePath, filename));
                Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);
                Console.WriteLine(filename);

                byte[] data = File.ReadAllBytes(filename);
                CacheMap cm = SchemaSerializer.DeserializeBond<CacheMap>(data);
                byte[] repacked = SchemaSerializer.SerializeBond(cm);
                

                Assert.IsTrue(data.SequenceEqual(repacked));
            }
        }
    }
}
