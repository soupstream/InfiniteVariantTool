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
            string gameDir = UserSettings.Instance.GameDirectory;
            string outputDir = GetType().Name;
            string userCacheDir = Path.Combine(outputDir, "usercache");
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
            Directory.CreateDirectory(userCacheDir);

            // load variant manager
            VariantManager manager = await VariantManager.Load(() => Language.En);

            // enumerate entries and grab a variant
            VariantAsset? fiestaEntry = null;
            foreach (var variant in manager.FilterVariants(new()))
            {
                if (variant.Variant.PublicName == "Fiesta:Slayer")
                {
                    fiestaEntry = variant;
                }
                PrintVariant(variant);
            }
            Assert.IsNotNull(fiestaEntry);

            // save a variant
            VariantAsset fiestaVariant = await manager.GetVariant((Guid)fiestaEntry.Variant.AssetId, (Guid)fiestaEntry.Variant.VersionId, fiestaEntry.Type, true, true);
            string fiestaVariantDir = Path.Combine(userCacheDir, "fiesta_slayer");
            await fiestaVariant.Save(fiestaVariantDir);

            // load variant
            string fiestaVariantFile = Path.Combine(fiestaVariantDir, "EngineGameVariant", "EngineGameVariant.json");
            await VariantAsset.Load(fiestaVariant.FilePath!, true);

            // store variant
            string fakeGameDir = Path.Combine(outputDir, "game");
            Directory.CreateDirectory(Path.Combine(fakeGameDir, "disk_cache", "webcache"));
            Directory.CreateDirectory(Path.Combine(fakeGameDir, "server_disk_cache", "webcache"));
            Directory.CreateDirectory(Path.Combine(fakeGameDir, "package", "pc", "common", "other"));
            Directory.CreateDirectory(Path.Combine(fakeGameDir, "package", "pc", "en-US", "other"));
            VariantManager manager2 = await VariantManager.Load(fakeGameDir, userCacheDir, manager.OfflineCache.BuildNumber, () => Language.En);
            await manager2.StoreVariant(fiestaVariant);
            manager2.SetVariantEnabled(fiestaVariant, false);
            manager2.SetVariantEnabled(fiestaVariant, true);
            await manager2.Flush();

            // remove variant
            await manager2.RemoveVariant(fiestaVariant);
            await manager2.Flush();
        }

        static void PrintVariant(VariantAsset variant)
        {
            Console.WriteLine(new string('-', 48));
            Console.WriteLine("Type: " + variant.Type.ClassType.Name);
            Console.WriteLine("AssetId: " + variant.Variant.AssetId);
            Console.WriteLine("VersionId: " + variant.Variant.VersionId);
            Console.WriteLine("Name: " + variant.Variant.PublicName);
            Console.WriteLine("Description: " + variant.Variant.Description);
            if (variant.Enabled != null)
            {
                Console.WriteLine("Enabled: " + variant.Enabled);
            }
        }
    }
}
