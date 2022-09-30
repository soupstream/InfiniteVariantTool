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
using InfiniteVariantTool.Core.BondSchema;

namespace InfiniteVariantTool.CLI
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand()
            {
                // bond
                new Command("bond", "Pack/unpack a bond file")
                {
                    new Command("unpack", "Unpack a bond file")
                    {
                        new Argument<FileInfo>("filename", "Bond file to unpack"),
                        new Option<FileInfo?>(new string[] { "--output", "-o" }, "Output file name"),
                    }
                    .Also(cmd => cmd.SetHandler(
                        (FileInfo infile, FileInfo? outfile, InvocationContext ctx) => CacheFileUnpackHandler(infile, outfile, true, ctx),
                        cmd.Arguments[0],
                        cmd.Options[0])),

                    new Command("pack", "Pack a bond file")
                    {
                        new Argument<FileInfo>("filename", "Bond file to pack"),
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
                    new Argument<ApiType>("api-type", "Type of API"),
                    new Argument<string>("url", "Content URL to hash")
                }
                .Also(cmd => cmd.SetHandler(
                    async (ApiType apiType, string url, InvocationContext ctx) => await UrlHashHandler(url, apiType, ctx),
                    cmd.Arguments[0],
                    cmd.Arguments[1])),

                // variants
                new Command("variant", "Tools for working with variants")
                {
                    new Command("list", "List cached variants")
                    {
                        new Option<Guid?>("--asset-id", "Asset ID of variant"),
                        new Option<Guid?>("--version-id", "Version ID of variant"),
                        new Option<VariantTypeEnum?>("--type", "Type of variant"),
                        new Option<string?>("--name", "Name of variant"),
                        new Option<bool?>("--enabled", "Whether the variant is enabled"),
                        new Option<bool?>("--user", "Show only user-installed variants"),
                    }
                    .Also(cmd => cmd.SetHandler(
                        async (Guid? assetId, Guid? versionId, VariantTypeEnum? variantType, string? name, bool? enabled, bool? user) =>
                            await VariantListHandler(assetId, versionId, variantType, name, enabled, user),
                        cmd.Options[0],
                        cmd.Options[1],
                        cmd.Options[2],
                        cmd.Options[3],
                        cmd.Options[4],
                        cmd.Options[5])),
                    new Command("extract", "Extract a variant")
                    {
                        new Option<Guid?>("--asset-id", "Asset ID of variant"),
                        new Option<Guid?>("--version-id", "Version ID of variant"),
                        new Option<VariantTypeEnum?>("--type", "Type of variant"),
                        new Option<string?>("--name", "Name of variant"),
                        new Option<bool?>("--enabled", "Whether the variant is enabled"),
                        new Option<bool>("--generate-guids", "Generate a new asset ID and version ID"),
                        new Option<bool>("--extract-linked", "Also extract linked variants"),
                        new Option<string?>(new string[] { "--output", "-o" }, "Output directory name"),
                    }
                    .Also(cmd => cmd.SetHandler(
                        async (Guid? assetId, Guid? versionId, VariantTypeEnum? variantType, string? name, bool? enabled, bool generateGuids, bool extractLinked, string outputDir, InvocationContext ctx) =>
                            await VariantExtractHandler(assetId, versionId, variantType, name, enabled, generateGuids, extractLinked, outputDir, ctx),
                        cmd.Options[0],
                        cmd.Options[1],
                        cmd.Options[2],
                        cmd.Options[3],
                        cmd.Options[4],
                        cmd.Options[5],
                        cmd.Options[6],
                        cmd.Options[7])),
                    new Command("install", "Install a variant")
                    {
                        new Option<bool>("--enable", "Also add the variant to the Custom Games menu"),
                        new Argument<string>("variant-file", "Variant json file")
                    }
                    .Also(cmd => cmd.SetHandler(
                        async (bool enable, string variantFile) => await VariantInstallHandler(enable, variantFile),
                        cmd.Options[0],
                        cmd.Arguments[0])),
                    new Command("enable", "Add a variant to the Custom Games menu")
                    {
                        new Option<Guid?>("--asset-id", "Asset ID of variant"),
                        new Option<Guid?>("--version-id", "Version ID of variant"),
                        new Option<VariantTypeEnum?>("--type", "Type of variant"),
                        new Option<string?>("--name", "Name of variant"),
                        new Option<bool?>("--enabled", "Whether the variant is enabled"),
                    }
                    .Also(cmd => cmd.SetHandler(
                        async (Guid? assetId, Guid? versionId, VariantTypeEnum? variantType, string? name, bool? enabled) =>
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
                        new Option<VariantTypeEnum?>("--type", "Type of variant"),
                        new Option<string?>("--name", "Name of variant"),
                        new Option<bool?>("--enabled", "Whether the variant is enabled"),
                    }
                    .Also(cmd => cmd.SetHandler(
                        async (Guid? assetId, Guid? versionId, VariantTypeEnum? variantType, string? name, bool? enabled) =>
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

        static void CacheFileUnpackHandler(FileInfo infile, FileInfo? outfile, bool? unpackEmbedded, InvocationContext ctx)
        {
            BondReader br = new(infile.FullName);
            string outfilename = outfile?.FullName ?? (infile + FileExtension.Xml.Value);
            var result = br.Read();
            if (unpackEmbedded == true)
            {
                result.ReadEmbeddedBond();
            }
            result.Save(outfilename);
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

        static async Task UrlHashHandler(string url, ApiType apiType, InvocationContext ctx)
        {
            var caches = await CacheManager.LoadAllCaches(LanguageNotDetectedPicker);
            CacheManager cache = apiType switch
            {
                ApiType.Offline => caches.Offline,
                ApiType.Online => caches.Online,
                ApiType.Lan => caches.Lan,
                _ => throw new ArgumentException(apiType.ToString())
            };
            var apiCall = cache.Api.CallUrl(url);
            if (apiCall == null)
            {
                Console.Error.WriteLine("Invalid URL");
            }
            else
            {
                Console.WriteLine(apiCall.Hash);
            }
        }

        static async Task VariantListHandler(Guid? assetId, Guid? versionId, VariantTypeEnum? variantType, string? name, bool? enabled, bool? user)
        {
            VariantManager manager = await VariantManager.Load(LanguageNotDetectedPicker);
            VariantFilter filter = new()
            {
                AssetId = assetId,
                VersionId = versionId,
                Name = name,
                Enabled = enabled,
                Type = VariantType.FromEnum(variantType)
            };

            foreach (var variant in user == true ? manager.FilterUserVariants(filter) : manager.FilterVariants(filter))
            {
                PrintVariant(variant);
            }
        }

        static async Task VariantExtractHandler(Guid? assetId, Guid? versionId, VariantTypeEnum? variantType, string? name, bool? enabled,
            bool generateGuids, bool unpackLinked, string? outputDir, InvocationContext ctx)
        {
            VariantManager manager = await VariantManager.Load(LanguageNotDetectedPicker);
            VariantFilter filter = new()
            {
                AssetId = assetId,
                VersionId = versionId,
                Name = name,
                Enabled = enabled,
                Type = VariantType.FromEnum(variantType)
            };

            var entries = manager.FilterVariants(filter);
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
                    PrintVariant(item);
                }
                ctx.ExitCode = 1;
            }
            else
            {
                var entry = entries.First();
                var variant = await manager.GetVariant((Guid)entry.Variant.AssetId, (Guid)entry.Variant.VersionId, entry.Type, true, unpackLinked);
                if (generateGuids)
                {
                    variant.GenerateGuids();
                }
                if (outputDir == null)
                {
                    outputDir = FileUtil.MakeValidFilename(variant.Variant.PublicName);
                }
                await variant.Save(outputDir);
                await manager.Flush();
                Console.WriteLine("Success: " + outputDir);
            }
        }

        static async Task VariantInstallHandler(bool enable, string variantFilePath)
        {
            VariantManager manager = await VariantManager.Load(LanguageNotDetectedPicker);
            VariantAsset variant = await VariantAsset.Load(variantFilePath, true);
            await manager.StoreVariant(variant);
            if (enable)
            {
                manager.SetVariantEnabled(variant, true);
            }
            await manager.Flush();
        }

        static async Task VariantSetEnabled(VariantFilter filter, bool enabled)
        {
            VariantManager manager = await VariantManager.Load(LanguageNotDetectedPicker);
            foreach (var variant in manager.FilterVariants(filter))
            {
                manager.SetVariantEnabled(variant, enabled);
                PrintVariant(variant);
            }
            await manager.Flush();
        }

        static async Task VariantEnableHandler(Guid? assetId, Guid? versionId, VariantTypeEnum? variantType, string? name, bool? enabled)
        {
            VariantFilter filter = new()
            {
                AssetId = assetId,
                VersionId = versionId,
                Name = name,
                Enabled = enabled,
                Type = VariantType.FromEnum(variantType)
            };

            Console.WriteLine("Enabled the following variants:");
            await VariantSetEnabled(filter, true);
        }

        static async Task VariantDisableHandler(Guid? assetId, Guid? versionId, VariantTypeEnum? variantType, string? name, bool? enabled)
        {
                VariantFilter filter = new()
                {
                    AssetId = assetId,
                    VersionId = versionId,
                    Name = name,
                    Enabled = enabled,
                    Type = VariantType.FromEnum(variantType)
                };

                Console.WriteLine("Disabled the following variants:");
                await VariantSetEnabled(filter, false);
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

        static Language LanguagePicker(string? message = null)
        {
            if (Language.Languages.Find(lang => lang.Code == UserSettings.Instance.Language) is Language match)
            {
                return match;
            }

            if (message != null)
            {
                Console.WriteLine(message);
            }

            for (int i = 0; i < Language.Languages.Count; i++)
            {
                Console.WriteLine("[{0}] {1}", i + 1, Language.Languages[i].Name);
            }
            while (true)
            {
                Console.Write("Enter selection (1-{0}): ", Language.Languages.Count);
                if (int.TryParse(Console.ReadLine(), out int result) && result >= 1 && result <= Language.Languages.Count)
                {
                    Language selectedLang = Language.Languages[result - 1];
                    UserSettings.Instance.Language = selectedLang.Code;
                    UserSettings.Instance.Save();
                    return selectedLang;
                }
                else
                {
                    Console.WriteLine("Invalid selection, please try again");
                }
            }
        }

        static Language LanguageNotDetectedPicker()
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
