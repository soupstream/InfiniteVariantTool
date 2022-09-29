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
using InfiniteVariantTool.Core.BondSchema;
using System.Threading.Tasks;

namespace InfiniteVariantTool.Tests
{
    [TestClass]
    public class CacheManagerTests
    {

        [TestMethod]
        public async Task TestLoadAllCaches()
        {
            var caches = await CacheManager.LoadAllCaches(() => Language.En);

            string outDir = GetType().Name;
            Directory.CreateDirectory(outDir);
            File.WriteAllText(Path.Combine(outDir, "game-manifest.json"),
                SchemaSerializer.SerializeJson(caches.Online.GameManifest));
            File.WriteAllText(Path.Combine(outDir, "customs-manifest.json"),
                SchemaSerializer.SerializeJson(caches.Online.CustomsManifest));
            Console.WriteLine(Path.GetFullPath(outDir));
        }
    }
}
