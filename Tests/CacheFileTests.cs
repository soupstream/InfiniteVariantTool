using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using InfiniteVariantTool.Core;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Reflection;
using InfiniteVariantTool.Core.Cache;
using InfiniteVariantTool.Core.Settings;

namespace InfiniteVariantTool.Tests
{
    [TestClass]
    public class CacheFileTests
    {

        [TestMethod]
        public void TestOfflineCacheFiles()
        {
            TestCacheFiles(UserSettings.Instance.GameDirectory, Constants.OfflineCacheDirectory, true);
        }

        [TestMethod]
        public void TestOnlineCacheFiles()
        {
            TestCacheFiles(UserSettings.Instance.GameDirectory, Constants.OnlineCacheDirectory, true);
        }

        [TestMethod]
        public void TestCmsFiles()
        {
            TestCacheFiles(UserSettings.Instance.GameDirectory, Constants.CmsDirectory, false);
        }

        [TestMethod]
        public void TestLanFiles()
        {
            TestCacheFiles(UserSettings.Instance.GameDirectory, Constants.LanCacheDirectory, true);
        }

        public void TestCacheFiles(string baseDirectory, string cacheDirectory, bool expectMetadata)
        {
            string fullCachePath = Path.Combine(baseDirectory, cacheDirectory);
            string outputDirectory = Path.Combine(GetType().Name, cacheDirectory);
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
            Directory.CreateDirectory(outputDirectory);
            foreach ((string filename, byte[] data) in TestUtil.GatherCacheFiles(fullCachePath, true, false))
            {
                string outputFile = Path.Combine(outputDirectory, Path.GetRelativePath(fullCachePath, filename));
                Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);
                Console.WriteLine(filename);
                CacheFile cacheFile = new(data, ContentType.AutoDetect, null);
                if (expectMetadata && !filename.Contains("gamecms") && !filename.EndsWith("CacheMap.wcache"))
                {
                    Assert.IsNotNull(cacheFile.Metadata);
                }

                ICacheFileContent content = cacheFile.ContentWithMetadata();
                content.SetName(outputFile + ".xml", new());
                content.Save();
                byte[] repacked = content.Pack();
                File.WriteAllBytes(outputFile, repacked);

                if (content is CacheFileContentBond bondContent && bondContent.Data.XPathSelectElement("//*[@compression='deflate']") != null)
                {
                    // if the file has compressed data it will be different when we recompress, so unpack again and compare the XML
                    CacheFile cacheFile2 = new(repacked, ContentType.AutoDetect, null);
                    ICacheFileContent content2 = cacheFile2.ContentWithMetadata();
                    content2.SetName(outputFile + ".xml", new());
                    Assert.IsTrue(XElement.DeepEquals((content2 as CacheFileContentBond)!.Data, bondContent.Data));
                }
                else
                {
                    Assert.IsTrue(data.SequenceEqual(repacked));
                }
            }
        }
    }
}
