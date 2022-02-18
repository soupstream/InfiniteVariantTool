using InfiniteVariantTool.Core;
using InfiniteVariantTool.Core.Settings;
using InfiniteVariantTool.Core.Utils;
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
    [TestClass]
    public class VariantManagerTests
    {
        [TestMethod]
        public async Task TestVariantManager()
        {
            string outputDir = GetType().Name;
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
            Directory.CreateDirectory(Path.Combine(outputDir, VariantType.MapVariant.ToString()));
            Directory.CreateDirectory(Path.Combine(outputDir, VariantType.UgcGameVariant.ToString()));
            Directory.CreateDirectory(Path.Combine(outputDir, VariantType.EngineGameVariant.ToString()));

            VariantManager manager = new(UserSettings.Instance.GameDirectory);
            manager.LoadCache(() => "en-US");
            FileNameDeduper deduper = new();
            List<string> filenames = new();
            bool unpackedLinkedVariant = false;
            foreach (var entry in manager.GetVariantEntries(null, null, null, null, null))
            {
                var variant = await manager.GetVariant(entry.Metadata.AssetId, entry.Metadata.VersionId, entry.Type, entry.Metadata.PublicName, entry.Enabled, true, true, true, true);
                string filename = Path.Combine(
                    outputDir,
                    variant.Type.ToString(),
                    variant.Metadata.Base.PublicName.Replace(':', '_'));
                filename = deduper.Dedupe(filename);
                variant.Save(filename);
                if (variant is UgcGameVariant ugcGameVariant && ugcGameVariant.LinkedEngineGameVariant != null)
                {
                    unpackedLinkedVariant = true;
                    foreach (var dir in Directory.GetDirectories(filename))
                    {
                        filenames.Add(dir);
                    }
                }
                else
                {
                    filenames.Add(filename);
                }
            }

            manager.OfflineCache!.BasePath = Path.Combine(outputDir, Constants.OfflineCacheDirectory);
            manager.OnlineCache!.BasePath = Path.Combine(outputDir, Constants.OnlineCacheDirectory);
            manager.LanCache!.BasePath = Path.Combine(outputDir, Constants.LanCacheDirectory);
            Directory.CreateDirectory(Path.Combine(outputDir, Constants.OfflineCacheDirectory, "common", "other"));
            Directory.CreateDirectory(Path.Combine(outputDir, Constants.OfflineCacheDirectory, manager.OfflineCache.Language!, "other"));
            Directory.CreateDirectory(Path.Combine(outputDir, Constants.OnlineCacheDirectory, "webcache"));
            Directory.CreateDirectory(Path.Combine(outputDir, Constants.LanCacheDirectory, "webcache"));

            foreach (var filename in filenames)
            {
                var variant = Variant.Load(filename);
                manager.SaveVariant(variant);
            }

            await manager.Save();

            Assert.IsTrue(unpackedLinkedVariant);
        }
    }
}
