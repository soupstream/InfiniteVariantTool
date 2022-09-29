using InfiniteVariantTool.Core;
using InfiniteVariantTool.Core.BondSchema;
using InfiniteVariantTool.Core.Cache;
using InfiniteVariantTool.Core.Serialization;
using InfiniteVariantTool.Core.Settings;
using InfiniteVariantTool.Core.Variants;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteVariantTool.Tests
{
    [TestClass]
    public class BondSchemaTests
    {
        CacheManager.CacheGroup caches;
        string gameDir;
        string outputDir;
        public BondSchemaTests()
        {
            caches = CacheManager.LoadAllCaches(() => Language.En).Result;
            gameDir = UserSettings.Instance.GameDirectory;
            outputDir = GetType().Name;
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
            Directory.CreateDirectory(outputDir);
        }

        public string GetOutputPath(string path)
        {
            string relativePath = Path.GetRelativePath(gameDir, path);
            string fullPath = Path.Combine(outputDir, relativePath);
            string parentDir = Path.GetDirectoryName(fullPath)!;
            Directory.CreateDirectory(parentDir);
            return fullPath;
        }

        public void TestManifest<T>(CacheManager cache, ApiCall apiCall)
        {
            string manifestPath = cache.GetFilePathIfExists(apiCall) ?? throw new Exception("file not found for " + apiCall.Url);
            byte[] data = File.ReadAllBytes(manifestPath);
            CacheFile cacheFile = SchemaSerializer.DeserializeBond<CacheFile>(data);
            T manifest = SchemaSerializer.DeserializeBond<T>(cacheFile.UData);
            byte[] repacked = SchemaSerializer.SerializeBond(manifest!);

            if (!cacheFile.UData.SequenceEqual(repacked))
            {
                // write files for debugging
                string outputPath = GetOutputPath(manifestPath);
                string json = SchemaSerializer.SerializeJson(manifest!);
                File.WriteAllText(outputPath + ".json", json);
                SchemaSerializer.DeserializeXml(repacked).Save(outputPath + ".xml");
                File.WriteAllBytes(GetOutputPath(manifestPath), repacked);
                File.WriteAllBytes(outputPath + "_orig", cacheFile.UData);
                SchemaSerializer.DeserializeXml(cacheFile.UData).Save(outputPath + "_orig.xml");
                Assert.Fail();
            }
        }

        public void TestVariants<T>(CacheManager cache)
        {
            VariantType type = VariantType.FromClassType(typeof(T));
            List<BondAsset> links = type.GetLinks(cache.GameManifest);
            foreach (var link in links)
            {
                var apiCall = cache.GetDiscoveryApiCall(link, type);
                TestManifest<T>(cache, apiCall);
            }
        }

        // game manifest

        [TestMethod]
        public void TestOfflineGameManifest()
        {
            TestManifest<GameManifest>(caches.Offline, caches.Offline.GameManifestApiCall);
        }

        [TestMethod]
        public void TestOnlineGameManifest()
        {
            TestManifest<GameManifest>(caches.Online, caches.Online.GameManifestApiCall);
        }

        [TestMethod]
        public void TestLanGameManifest()
        {
            TestManifest<GameManifest>(caches.Lan, caches.Lan.GameManifestApiCall);
        }

        // customs manifest

        [TestMethod]
        public void TestOfflineCustomsManifest()
        {
            TestManifest<CustomsManifest>(caches.Offline, caches.Offline.CustomsManifestApiCall);
        }

        [TestMethod]
        public void TestOnlineCustomsManifest()
        {
            TestManifest<CustomsManifest>(caches.Online, caches.Online.CustomsManifestApiCall);
        }

        // api manifest

        [TestMethod]
        public void TestOfflineApiManifest()
        {
            TestManifest<ApiManifest>(caches.Offline, caches.Offline.ApiManifestApiCall);
        }

        [TestMethod]
        public void TestOnlineApiManifest()
        {
            TestManifest<ApiManifest>(caches.Online, caches.Online.ApiManifestApiCall);
        }

        [TestMethod]
        public void TestLanApiManifest()
        {
            TestManifest<ApiManifest>(caches.Lan, caches.Lan.ApiManifestApiCall);
        }

        // ugc variants

        [TestMethod]
        public void TestOfflineUgcVariants()
        {
            TestVariants<UgcGameVariant>(caches.Offline);
        }

        // engine variants

        [TestMethod]
        public void TestOfflineEngineVariants()
        {
            TestVariants<EngineGameVariant>(caches.Offline);
        }

        // map variants

        [TestMethod]
        public void TestOfflineMapVariants()
        {
            TestVariants<MapVariant>(caches.Offline);
        }
    }
}
