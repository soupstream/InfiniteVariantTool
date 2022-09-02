using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using InfiniteVariantTool.Core;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using InfiniteVariantTool.Core.Settings;
using InfiniteVariantTool.Core.Variants;
using InfiniteVariantTool.Core.Cache;
using InfiniteVariantTool.Core.Serialization;
using InfiniteVariantTool.Core.Utils;

namespace InfiniteVariantTool.CLI
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand()
            {
                // cachefile
                new Command("cache", "Pack/unpack a cache file")
                {
                    new Command("unpack", "Unpack a cache file")
                    {
                        new Argument<FileInfo>("filename", "Cache file to unpack"),
                        new Option<FileInfo?>(new string[] { "--output", "-o" }, "Output file name")
                    }
                    .Also(cmd => cmd.SetHandler(
                        (FileInfo infile, FileInfo? outfile, InvocationContext ctx) => CacheFileUnpackHandler(infile, outfile, ctx),
                        cmd.Arguments[0],
                        cmd.Options[0])),

                    new Command("pack", "Pack a cache file")
                    {
                        new Argument<FileInfo>("filename", "Cache file to pack"),
                        new Option<FileInfo?>(new string[] { "--output", "-o" }, "Output file name")
                    }
                    .Also(cmd => cmd.SetHandler(
                        (FileInfo infile, FileInfo? outfile, InvocationContext ctx) => CacheFilePackHandler(infile, outfile, ctx),
                        cmd.Arguments[0],
                        cmd.Options[0])),
                },

                // luabundle
                new Command("luabundle", "Pack/unpack a lua bundle")
                {
                    new Command("unpack", "Unpack a lua bundle")
                    {
                        new Option<Game?>(new string[] { "--game", "-g" }, "Specify game (otherwise detect automatically)"),
                        new Argument<FileInfo>("filename", "Lua bundle file to unpack"),
                        new Option<DirectoryInfo?>(new string[] { "--output", "-o" }, "Output directory name")
                    }
                    .Also(cmd => cmd.SetHandler(
                        (Game? game, FileInfo infile, DirectoryInfo? outdir, InvocationContext ctx) => LuaBundleUnpackHandler(game, infile, outdir, ctx),
                        cmd.Options[0],
                        cmd.Arguments[0],
                        cmd.Options[1])),

                    new Command("pack", "Pack a lua bundle")
                    {
                        new Argument<DirectoryInfo>("dirname", "Lua bundle directory to pack"),
                        new Option<FileInfo?>(new string[] { "--output", "-o" }, "Output file name")
                    }
                    .Also(cmd => cmd.SetHandler(
                        (DirectoryInfo indir, FileInfo? outfile, InvocationContext ctx) => LuaBundlePackHandler(indir, outfile, ctx),
                        cmd.Arguments[0],
                        cmd.Options[0]))
                },

                // urlhash
                new Command("hash", "Hash a content URL")
                {
                    new Argument<EndpointType>("endpoint-type", "Type of endpoint"),
                    new Argument<string>("url", "Content URL to hash")
                }
                .Also(cmd => cmd.SetHandler(
                    (EndpointType endpointType, string url, InvocationContext ctx) => UrlHashHandler(url, endpointType, ctx),
                    cmd.Arguments[0],
                    cmd.Arguments[1])),

                // variants
                new Command("variant", "Tools for working with variants")
                {
                    new Command("list", "List cached variants")
                    {
                        new Option<Guid?>("--asset-id", "Asset ID of variant"),
                        new Option<Guid?>("--version-id", "Version ID of variant"),
                        new Option<VariantType?>("--type", "Type of variant"),
                        new Option<string?>("--name", "Name of variant"),
                        new Option<bool?>("--enabled", "Whether the variant is enabled"),
                    }
                    .Also(cmd => cmd.SetHandler(
                        async (Guid? assetId, Guid? versionId, VariantType? variantType, string? name, bool? enabled) =>
                            await VariantListHandler(assetId, versionId, variantType, name, enabled),
                        cmd.Options[0],
                        cmd.Options[1],
                        cmd.Options[2],
                        cmd.Options[3],
                        cmd.Options[4])),
                    new Command("extract", "Extract a variant")
                    {
                        new Option<Guid?>("--asset-id", "Asset ID of variant"),
                        new Option<Guid?>("--version-id", "Version ID of variant"),
                        new Option<VariantType?>("--type", "Type of variant"),
                        new Option<string?>("--name", "Name of variant"),
                        new Option<bool?>("--enabled", "Whether the variant is enabled"),
                        new Option<bool>("--generate-guids", "Generate a new asset ID and version ID"),
                        new Option<bool>("--download-lua", "Download debug script source if available"),
                        new Option<bool>("--extract-linked", "Also extract linked variants"),
                        new Option<string?>(new string[] { "--output", "-o" }, "Output directory name"),
                    }
                    .Also(cmd => cmd.SetHandler(
                        async (Guid? assetId, Guid? versionId, VariantType? variantType, string? name, bool? enabled, bool generateGuids, bool downloadLua, bool extractLinked, string outputDir, InvocationContext ctx) =>
                            await VariantExtractHandler(assetId, versionId, variantType, name, enabled, generateGuids, downloadLua, extractLinked, outputDir, ctx),
                        cmd.Options[0],
                        cmd.Options[1],
                        cmd.Options[2],
                        cmd.Options[3],
                        cmd.Options[4],
                        cmd.Options[5],
                        cmd.Options[6],
                        cmd.Options[7],
                        cmd.Options[8])),
                    new Command("save", "Save a variant")
                    {
                        new Option<bool>("--enable", "Also add the variant to the Custom Games menu"),
                        new Argument<string>("variant-dir", "Variant directory name")
                    }
                    .Also(cmd => cmd.SetHandler(
                        async (bool enable, string variantDir) => await VariantSaveHandler(enable, variantDir),
                        cmd.Options[0],
                        cmd.Arguments[0])),
                    new Command("enable", "Add a variant to the Custom Games menu")
                    {
                        new Option<Guid?>("--asset-id", "Asset ID of variant"),
                        new Option<Guid?>("--version-id", "Version ID of variant"),
                        new Option<VariantType?>("--type", "Type of variant"),
                        new Option<string?>("--name", "Name of variant"),
                        new Option<bool?>("--enabled", "Whether the variant is enabled"),
                    }
                    .Also(cmd => cmd.SetHandler(
                        async (Guid? assetId, Guid? versionId, VariantType? variantType, string? name, bool? enabled) =>
                            await VariantEnableHandler(assetId, versionId, variantType, name, enabled),
                        cmd.Options[0],
                        cmd.Options[1],
                        cmd.Options[2],
                        cmd.Options[3],
                        cmd.Options[4])),
                    new Command("disable", "Remove a variant from the Custom Games menu")
                    {
                        new Option<Guid?>("--asset-id", "Asset ID of variant"),
                        new Option<Guid?>("--version-id", "Version ID of variant"),
                        new Option<VariantType?>("--type", "Type of variant"),
                        new Option<string?>("--name", "Name of variant"),
                        new Option<bool?>("--enabled", "Whether the variant is enabled"),
                    }
                    .Also(cmd => cmd.SetHandler(
                        async (Guid? assetId, Guid? versionId, VariantType? variantType, string? name, bool? enabled) =>
                            await VariantDisableHandler(assetId, versionId, variantType, name, enabled),
                        cmd.Options[0],
                        cmd.Options[1],
                        cmd.Options[2],
                        cmd.Options[3],
                        cmd.Options[4])),
                },

                // settings
                new Command("settings", "InfiniteVariantTool settings")
                {
                    new Command("list", "List settings")
                    .Also(cmd => cmd.SetHandler(() => ListSettingsHandler())),
                    new Command("get", "Get setting")
                    {
                        new Argument<string>("name", "Name of the setting")
                    }
                    .Also(cmd => cmd.SetHandler((string name) => GetSettingHandler(name),
                        cmd.Arguments[0])),
                    new Command("set", "Set setting")
                    {
                        new Argument<string>("name", "Name of the setting"),
                        new Argument<string>("value", "Value to set")
                    }
                    .Also(cmd => cmd.SetHandler((string name, string value) => SetSettingHandler(name, value),
                        cmd.Arguments[0],
                        cmd.Arguments[1])),
                    new Command("reset", "Reset setting to default")
                    {
                        new Argument<string>("name", "Name of the setting")
                    }
                    .Also(cmd => cmd.SetHandler((string name) => ResetSettingHandler(name),
                        cmd.Arguments[0])),
                },
            };

            return await new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .UseExceptionHandler(ExceptionHandler)
                .Build()
                .InvokeAsync(args);
        }

        static void ExceptionHandler(Exception ex, InvocationContext ctx)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            switch (ex)
            {
                case IOException:
                case UnauthorizedAccessException:
                    Console.Error.WriteLine(ex.Message);
                    break;
                default:
                    Console.Error.WriteLine("Unhandled exception: {0}", ex.Message);
                    Console.Error.WriteLine(ex.StackTrace);
                    break;
            }
            Console.ResetColor();
            ctx.ExitCode = 1;
        }


        ///// Handlers /////

        static void CacheFileUnpackHandler(FileInfo infile, FileInfo? outfile, InvocationContext ctx)
        {
            CacheFile cm = new(infile.FullName, ContentType.AutoDetect, null);
            string outfilename = outfile?.FullName ?? CacheFile.SuggestFilename(infile.FullName, cm.Content.Type);
            cm.Save(outfilename);
            Console.WriteLine("Success: " + outfilename);
        }

        static void CacheFilePackHandler(FileInfo infile, FileInfo? outfile, InvocationContext ctx)
        {
            BondWriter bw = new(infile.FullName);
            string outfilename = outfile?.FullName ?? BondWriter.SuggestFilename(infile.FullName);
            bw.Save(outfilename);
            Console.WriteLine("Success: " + outfilename);
        }

        static void LuaBundleUnpackHandler(Game? game, FileInfo infile, DirectoryInfo? outdir, InvocationContext ctx)
        {
            LuaBundle bundle = LuaBundle.Unpack(infile.FullName, game);
            string outfilename = outdir?.FullName ?? LuaBundleUtils.SuggestUnpackFilename(infile.FullName);
            bundle.Save(outfilename);
            Console.WriteLine("Success: " + outfilename);
        }

        static void LuaBundlePackHandler(DirectoryInfo indir, FileInfo? outfile, InvocationContext ctx)
        {
            LuaBundle bundle = LuaBundle.Load(indir.FullName);
            string outfilename = outfile?.FullName ?? LuaBundleUtils.SuggestPackFilename(indir.FullName);
            File.WriteAllBytes(outfilename, bundle.Pack());
            Console.WriteLine("Success: " + outfilename);
        }

        static void UrlHashHandler(string url, EndpointType endpointType, InvocationContext ctx)
        {
            ulong? hash = UrlHasher.TryHashUrl(url, endpointType);
            if (hash == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine("Unable to hash URL");
                Console.ResetColor();
                ctx.ExitCode = 1;
            }
            else
            {
                Console.WriteLine(hash);
            }
        }

        static async Task VariantListHandler(Guid? assetId, Guid? versionId, VariantType? variantType, string? name, bool? enabled)
        {
            VariantManager manager = new(UserSettings.Instance.GameDirectory);
            manager.LoadCache(LanguageNotDetectedPicker);
            await foreach (var variant in manager.GetVariants(assetId, versionId, variantType, name, enabled, false, false, false))
            {
                PrintMetadata(variant.Type, variant.Metadata.Base);
                Console.WriteLine("Tags: [{0}]", string.Join(", ", variant.Metadata.Tags));
                if (variant.Enabled != null)
                {
                    Console.WriteLine("Enabled: " + variant.Enabled);
                }
                if (variant is UgcGameVariant ugcGameVariantMetadata)
                {
                    var engineLink = ugcGameVariantMetadata.Metadata.EngineGameVariantLink;
                    if (engineLink != null)
                    {
                        Console.WriteLine("Engine Game Variant:");
                        Console.WriteLine("    AssetId: " + engineLink.AssetId);
                        Console.WriteLine("    VersionId: " + engineLink.VersionId);
                        Console.WriteLine("    Name: " + engineLink.PublicName);
                        Console.WriteLine("    Description: " + engineLink.Description);
                    }
                }
            }
        }

        static async Task VariantExtractHandler(Guid? assetId, Guid? versionId, VariantType? variantType, string? name, bool? enabled,
            bool generateGuids, bool downloadLua, bool unpackLinked, string? outputDir, InvocationContext ctx)
        {
            VariantManager manager = new(UserSettings.Instance.GameDirectory);
            manager.LoadCache(LanguageNotDetectedPicker);
            var entries = manager.GetVariantEntries(assetId, versionId, variantType, name, enabled);
            if (!entries.Any())
            {
                Console.WriteLine("Error: no matching variants");
                ctx.ExitCode = 1;
            }
            else if (entries.Count() > 1)
            {
                Console.WriteLine("Error: multiple matching variants:");
                foreach (var item in entries)
                {
                    PrintMetadata(item.Type, item.Metadata);
                }
                ctx.ExitCode = 1;
            }
            else
            {
                var entry = entries.First();
                var variant = await manager.GetVariant(entry.Metadata.AssetId, entry.Metadata.VersionId, entry.Type, null, null, unpackLinked, true, true, downloadLua);
                if (generateGuids)
                {
                    variant.GenerateAssetId();
                    variant.GenerateVersionId();
                }
                if (outputDir == null)
                {
                    outputDir = Util.MakeValidFilename(variant.Metadata.Base.PublicName);
                }
                variant.Save(outputDir);
                Console.WriteLine("Success: " + outputDir);
            }
        }

        static async Task VariantSaveHandler(bool enable, string variantDir)
        {
            VariantManager manager = new(UserSettings.Instance.GameDirectory);
            manager.LoadCache(LanguageNotDetectedPicker);
            Variant variant = Variant.Load(variantDir);
            manager.SaveVariant(variant);
            if (enable)
            {
                manager.EnableVariant(variant.Metadata.Base.AssetId, variant.Metadata.Base.VersionId);
            }
            await manager.Save();
        }

        static async Task VariantEnableHandler(Guid? assetId, Guid? versionId, VariantType? variantType, string? name, bool? enabled)
        {
            VariantManager manager = new(UserSettings.Instance.GameDirectory);
            manager.LoadCache(LanguageNotDetectedPicker);
            Console.WriteLine("Enabled the following variants:");
            foreach (var entry in manager.GetVariantEntries(assetId, versionId, variantType, name, enabled))
            {
                manager.EnableVariant(entry.Metadata.AssetId, entry.Metadata.AssetId);
                PrintMetadata(entry.Type, entry.Metadata);
            }
            await manager.Save();
        }

        static async Task VariantDisableHandler(Guid? assetId, Guid? versionId, VariantType? variantType, string? name, bool? enabled)
        {
            VariantManager manager = new(UserSettings.Instance.GameDirectory);
            manager.LoadCache(LanguageNotDetectedPicker);
            Console.WriteLine("Disabled the following variants:");
            foreach (var entry in manager.GetVariantEntries(assetId, versionId, variantType, name, enabled))
            {
                manager.DisableVariant(entry.Metadata.AssetId, entry.Metadata.VersionId);
                PrintMetadata(entry.Type, entry.Metadata);
            }
            await manager.Save();
        }

        static void PrintMetadata(VariantType type, VariantMetadataBase metadata)
        {
            Console.WriteLine(new string('-', 48));
            Console.WriteLine("Type: " + type);
            Console.WriteLine("AssetId: " + metadata.AssetId);
            Console.WriteLine("VersionId: " + metadata.VersionId);
            Console.WriteLine("Name: " + metadata.PublicName);
            Console.WriteLine("Description: " + metadata.Description);
        }

        static string LanguagePicker(string? message = null)
        {
            foreach (var language in VariantManager.LanguageCodes)
            {
                if (UserSettings.Instance.Language == language.Code)
                {
                    return language.Code;
                }
            }

            if (message != null)
            {
                Console.WriteLine(message);
            }

            for (int i = 0; i < VariantManager.LanguageCodes.Count; i++)
            {
                Console.WriteLine("[{0}] {1}", i + 1, VariantManager.LanguageCodes[i].Name);
            }
            while (true)
            {
                Console.Write("Enter selection (1-{0}): ", VariantManager.LanguageCodes.Count);
                if (int.TryParse(Console.ReadLine(), out int result))
                {
                    if (result <= 0 || result > VariantManager.LanguageCodes.Count)
                    {
                        Console.WriteLine("Invalid selection, please try again");
                    }
                    else
                    {
                        string code = VariantManager.LanguageCodes[result - 1].Code;
                        UserSettings.Instance.Language = code;
                        UserSettings.Instance.Save();
                        return code;
                    }
                }
                else
                {
                    Console.WriteLine("Invalid number, please try again");
                }
            }
        }

        static string LanguageNotDetectedPicker()
        {
            return LanguagePicker("Could not detect the in-game language. Select your language:");
        }

        static void ListSettingsHandler()
        {
            foreach ((string name, SettingAttribute attr, object value) in UserSettings.Instance.GetSettingsInfo())
            {
                Console.WriteLine(name + " = " + value.ToString());
            }
        }

        static void GetSettingHandler(string name)
        {
            Console.WriteLine(UserSettings.Instance.Get(name));
        }

        static void SetSettingHandler(string name, string value)
        {
            if (UserSettings.Instance.Set(name, value))
            {
                UserSettings.Instance.Save();
            }
            else
            {
                Console.WriteLine("Invalid input");
            }
        }

        static void ResetSettingHandler(string name)
        {
            UserSettings.Instance.Reset(name);
            UserSettings.Instance.Save();
        }
    }

    static class CommandLineExtensions
    {
        // Kotlin-style Also method
        public static T Also<T>(this T self, Action<T> block)
        {
            block(self);
            return self;
        }
    }
}
