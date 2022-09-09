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
    /*
    [TestClass]
    public class VariantManifestTests
    {
        [TestMethod]
        public void TestOfflineManifests()
        {
            string outputDir = Path.Combine(GetType().Name, Constants.OfflineCacheDirectory);
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
            string buildNumber = TestUtil.GetBuildNumber();
            foreach (string filename in TestUtil.GatherCacheMapFiles(Path.Combine(UserSettings.Instance.GameDirectory, Constants.OfflineCacheDirectory)))
            {
                string relativeDir = Path.GetDirectoryName(Path.GetRelativePath(UserSettings.Instance.GameDirectory, filename))!;
                TestManifests(UserSettings.Instance.GameDirectory, relativeDir, EndpointType.Offline,
                    "https://discovery-infiniteugc-intone.test.svc.halowaypoint.com/hi/projects/a9dc0785-2a99-4fec-ba6e-0216feaaf041");
                TestManifests(UserSettings.Instance.GameDirectory, relativeDir, EndpointType.Offline,
                    $"https://discovery-infiniteugc-intone.test.svc.halowaypoint.com/hi/manifests/builds/{buildNumber}/game");
            }
        }

        [TestMethod]
        public void TestOnlineManifests()
        {
            string outputDir = Path.Combine(GetType().Name, Constants.OnlineCacheDirectory);
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
            string buildNumber = TestUtil.GetBuildNumber();
            TestManifests(UserSettings.Instance.GameDirectory, Path.Combine(Constants.OnlineCacheDirectory, "webcache"), EndpointType.Online,
                    $"https://discovery-infiniteugc.svc.halowaypoint.com/hi/projects/a9dc0785-2a99-4fec-ba6e-0216feaaf041");
            TestManifests(UserSettings.Instance.GameDirectory, Path.Combine(Constants.OnlineCacheDirectory, "webcache"), EndpointType.Online,
                    $"https://discovery-infiniteugc.svc.halowaypoint.com/hi/manifests/builds/{buildNumber}/game");
        }

        [TestMethod]
        public void TestLanManifests()
        {
            string outputDir = Path.Combine(GetType().Name, Constants.LanCacheDirectory);
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
            (Guid assetId, Guid versionId) = GetManifestId();
            TestManifests(UserSettings.Instance.GameDirectory, Path.Combine(Constants.LanCacheDirectory, "webcache"), EndpointType.Lan,
                    $"https://discovery-infiniteugc.svc.halowaypoint.com/hi/manifests/{assetId}/versions/{versionId}");
        }

        private (Guid, Guid) GetManifestId()
        {
            string buildNumber = TestUtil.GetBuildNumber();
            ulong hash = UrlHasher.HashUrl($"https://discovery-infiniteugc-intone.test.svc.halowaypoint.com/hi/manifests/builds/{buildNumber}/game", EndpointType.Offline);
            string filename = Path.Combine(UserSettings.Instance.GameDirectory, Constants.OfflineCacheDirectory, "en-US", "other", hash.ToString());
            CacheFile manifestFile = new(filename, ContentType.Bond, true);
            GameManifest manifest = new(manifestFile.Doc);
            return (manifest.Base.AssetId, manifest.Base.VersionId);
        }

        private void TestManifests(string baseDir, string relativeDir, EndpointType endpointType, string url)
        {
            string outputDir = Path.Combine(GetType().Name, relativeDir);
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            string absoluteDir = Path.Combine(baseDir, relativeDir);
            ulong hash = UrlHasher.HashUrl(url, endpointType);
            string manifestPath = Path.Combine(absoluteDir, hash.ToString());
            Console.WriteLine(url);
            Console.WriteLine(manifestPath);
            CacheFile manifestFile = new(manifestPath, ContentType.Bond, true);
            VariantMetadata manifest;
            string fileName;
            if (url.Contains("/manifests/"))
            {
                manifest = new GameManifest(manifestFile.Doc);
                fileName = "game-manifest.xml";
            }
            else if (url.Contains("/projects/"))
            {
                manifest = new CustomsManifest(manifestFile.Doc);
                fileName = "customs-manifest.xml";
            }
            else
            {
                throw new ArgumentException();
            }
            XElement manifestDoc = manifest.Save(Path.Combine(outputDir, fileName));
            Assert.IsTrue(XNode.DeepEquals(manifestDoc, manifestFile.Doc));
        }
    }
    */
}
