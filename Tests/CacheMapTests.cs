using InfiniteVariantTool.Core;
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
        public void TestCustomCacheMap()
        {
            CacheMap cm = new()
            {
                Map = new()
                {
                    {
                        998,
                        new CacheMapEntry()
                        {
                            Metadata = new()
                            {
                                Guid = new()
                            }
                        }
                    },
                    {
                        12345,
                        new CacheMapEntry()
                        {
                            Metadata = new()
                            {
                                Url = "https://example.com"
                            }
                        }
                    }
                },
                Language = "en-US"
            };

            XElement doc = cm.Serialize();
            CacheMap cm2 = new();
            cm2.Deserialize(doc);
        }

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
                BondReader br = new(data);
                XElement doc = br.Save(outputFile + ".xml").Doc;
                CacheMap cm = new(doc);
                XElement doc2 = cm.Save(outputFile + ".repacked.xml");

                Assert.IsTrue(XDocument.DeepEquals(doc, doc2));
            }
        }
    }
}
