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
using InfiniteVariantTool.Core.Serialization;
using InfiniteVariantTool.Core.Utils;

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
            foreach ((string filename, byte[] data) in TestUtil.GatherCacheFiles(fullCachePath, true, true))
            {
                string outputFile = Path.Combine(outputDirectory, Path.GetRelativePath(fullCachePath, filename));
                Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);
                Console.WriteLine(filename);
                BondReader br = new(filename);
                var result = br.Read(true);
                result.Save(outputFile + ".xml");
                Assert.IsTrue(!result.Blobs.Any(blob => blob.Key.EndsWith(FileExtension.Bin.Value))  // shouldn't be any bond blobs
                    || result.Doc.XPathSelectElement("./list[@id='1']/struct/string[@id='4']")?.GetText().EndsWith("WorstCaseEncodedSize.bin") == true);    // has a blob of garbage so ignore

                BondWriter bw = new(outputFile + ".xml");
                byte[] repacked = bw.Write();

                if (result.Doc.XPathSelectElement("//*[@compression='deflate']") != null)
                {
                    // if the file has compressed data it will be different when we recompress, so unpack again and compare the XML
                    br = new(repacked);
                    var result2 = br.Read(true);
                    result2.SetFileName(Path.GetFileName(outputFile + ".xml"));
                    Assert.IsTrue(XElement.DeepEquals(result.Doc, result2.Doc));
                }
                else
                {
                    Assert.IsTrue(data.SequenceEqual(repacked));
                }
            }
        }
    }
}
