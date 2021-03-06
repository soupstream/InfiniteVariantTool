using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using InfiniteVariantTool.Core;
using System.Collections.Generic;
using System.IO;
using System;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Xml.XPath;
using InfiniteVariantTool.Core.Serialization;
using InfiniteVariantTool.Core.Cache;
using InfiniteVariantTool.Core.Settings;

namespace InfiniteVariantTool.Tests
{
    [TestClass]
    public class HashUrlTests
    {
        [TestMethod]
        public void TestBlobUrl()
        {
            string url = "https://blobs-infiniteugc-test.test.svc.halowaypoint.com/ugcstorage/map/4f196016-0101-4844-8358-2504f7c44656/6e5ad39c-7280-4058-9f09-1825d942f48e/images/screenshot1.png";
            ulong? hash = UrlHasher.TryHashUrl(url, EndpointType.Offline);
            Assert.AreEqual(hash, 3063513848438510U);
        }

        [TestMethod]
        public void TestGameManifestUrl()
        {
            string url = "https://discovery-infiniteugc-intone.test.svc.halowaypoint.com/hi/manifests/builds/6.10021.11755/game";
            ulong? hash = UrlHasher.TryHashUrl(url, EndpointType.Offline);
            Assert.AreEqual(hash, 12304663280626663446U);
        }

        [TestMethod]
        public void TestCustomsManifestUrl()
        {
            string url = "https://discovery-infiniteugc-intone.test.svc.halowaypoint.com/hi/projects/a9dc0785-2a99-4fec-ba6e-0216feaaf041";
            ulong? hash = UrlHasher.TryHashUrl(url, EndpointType.Offline);
            Assert.AreEqual(hash, 78037933673922871U);
        }

        [TestMethod]
        public void TestGamecmsUrl()
        {
            string url = "https://gamecms-hacs-intone.test.svc.halowaypoint.com/hi/images/guide/xo?flight={flightId}";
            ulong? hash = UrlHasher.TryHashUrl(url, EndpointType.Offline);
            Assert.AreEqual(hash, 32981699815514004U);
        }

        [TestMethod]
        public void TestLanManifestUrl()
        {
            string url = "https://discovery-infiniteugc.svc.halowaypoint.com/hi/manifests/6036e332-db64-4ead-a44b-5db9c71c6629/versions/4db50283-1ae5-4c33-9b61-6efeead0b4f6";
            ulong? hash = UrlHasher.TryHashUrl(url, EndpointType.Lan);
            Assert.AreEqual(hash, 12165909370489822250);
        }

        [TestMethod]
        public void TestNonHaloUrl()
        {
            string url = "http://www.ilovebees.com/";
            ulong? hash = UrlHasher.TryHashUrl(url, EndpointType.Offline);
            Assert.AreEqual(hash, 8528100170959807611U);
        }

        [TestMethod]
        public void TestMalformedUrl()
        {
            string url = "According to all known laws of aviation, there is no way a bee should be able to fly. Its wings are too small to get its fat little body off the ground.";
            ulong? hash = UrlHasher.TryHashUrl(url, EndpointType.Offline);
            Assert.IsNull(hash);
        }

        [TestMethod]
        public void TestCacheMaps()
        {
            string cacheDirectory = Path.Combine(UserSettings.Instance.GameDirectory, Constants.OfflineCacheDirectory);
            foreach (string filename in TestUtil.GatherCacheMapFiles(cacheDirectory))
            {
                Console.WriteLine(filename);
                TestCacheMap(filename);
            }
        }

        private void TestCacheMap(string filename)
        {
            BondReader br = new(filename);
            XElement doc = br.Read().Doc;
            CacheMap cm = new(doc);
            foreach (var entry in cm.Map)
            {
                if (entry.Value.Metadata.Url != null)
                {
                    ulong? actualHash = UrlHasher.TryHashUrl(entry.Value.Metadata.Url, EndpointType.Offline);
                    Assert.AreEqual(entry.Key, actualHash);
                }
            }
        }
    }
}
