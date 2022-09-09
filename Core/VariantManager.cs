using InfiniteVariantTool.Core.BondSchema;
using InfiniteVariantTool.Core.Cache;
using InfiniteVariantTool.Core.Settings;
using InfiniteVariantTool.Core.Utils;
using InfiniteVariantTool.Core.Variants;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InfiniteVariantTool.Core
{
    public class VariantManager
    {
        private string gameDir;
        private Language language;
        private CacheManager PrimaryCache => OnlineCache;   // source of truth
        public CacheManager OnlineCache { get; }
        public CacheManager OfflineCache { get; }
        public CacheManager LanCache { get; }
        private UserCacheManager userCache;
        private List<CacheManager> caches;  // caches to update
        private List<VariantAsset> variantAssets;

        private VariantManager(string gameDir, Language language, UserCacheManager userCache, CacheManager onlineCache, CacheManager offlineCache, CacheManager lanCache)
        {
            this.gameDir = gameDir;
            if (!File.Exists(Path.Combine(gameDir, Constants.GameExeName)))
            {
                throw new FileNotFoundException("Game not found at " + gameDir);
            }
            this.language = language;

            this.userCache = userCache;
            OnlineCache = onlineCache;
            LanCache = lanCache;
            OfflineCache = offlineCache;
            caches = new() { onlineCache, lanCache };
            variantAssets = new();
            RefreshAssetList();
        }

        public static async Task<VariantManager> Load(Func<Language> languagePicker)
        {
            return await Load(UserSettings.Instance.GameDirectory, UserSettings.Instance.VariantDirectory, languagePicker);
        }

        public static async Task<VariantManager> Load(string gameDir, string userCacheDir, Func<Language> languagePicker)
        {
            if (!File.Exists(Path.Combine(gameDir, Constants.GameExeName)))
            {
                throw new FileNotFoundException("Game not found at " + gameDir);
            }
            var caches = await CacheManager.LoadAllCaches(gameDir, languagePicker);
            UserCacheManager userCache = new(userCacheDir);
            await userCache.LoadEntries();
            VariantManager variantManager = new(gameDir, caches.Online.Language!, userCache, caches.Online, caches.Offline, caches.Lan);
            return variantManager;
        }

        // get variant entry from game manifest
        public BondAsset? GetVariantEntry(Guid assetId, Guid versionId, VariantType type)
        {
            List<BondAsset> links = PrimaryCache.GameManifest.LinksByType(type);
            return links.Find(entry => (Guid)entry.AssetId == assetId && (Guid)entry.VersionId == versionId);
        }

        public async Task<VariantAsset> GetVariant(Guid assetId, Guid versionId, VariantType type)
        {
            var apiCall = PrimaryCache.GetDiscoveryApiCall(assetId, versionId, type);
            BondAsset variant = (BondAsset)await PrimaryCache.GetBondFile(apiCall, type.ClassType);
            VariantAsset variantAsset = new(variant);

            // filter out unneeded files
            IEnumerable<string> fileRelativePaths = variant.Files.FileRelativePaths
                .Where(path => !Language.Languages.Any(lang => path.EndsWith($".{lang.ShortCode}")))
                .Where(path => path.EndsWith($"_{language.ShortCode}.bin") || !Language.Languages.Any(lang => path.EndsWith($"_{lang.ShortCode}.bin")))
                .Where(path => !path.EndsWith("_guid.txt"));

            foreach (string relativePath in fileRelativePaths)
            {
                ApiCall fileApiCall = PrimaryCache.Api.Call(variant.Files.PrefixEndpoint.AuthorityId, new()
                {
                    { "path", variant.Files.Prefix + relativePath },
                });
                variantAsset.Files[relativePath] = await PrimaryCache.GetFile(fileApiCall, MimeType.OctetStream);
            }
            return variantAsset;
        }

        public async Task<VariantAsset> GetVariant(Guid assetId, Guid versionId, VariantType type, bool getLinkedVariant)
        {
            VariantAsset variant = await GetVariant(assetId, versionId, type);
            if (getLinkedVariant && variant.Variant is UgcGameVariant ugcVar && ugcVar.EngineGameVariantLink is BondAsset engineVar)
            {
                VariantAsset link = await GetVariant((Guid)engineVar.AssetId, (Guid)engineVar.VersionId, VariantType.EngineGameVariant);
                variant.LinkedVariants.Add(link);
            }
            return variant;
        }

        public async Task StoreVariant(VariantAsset variant)
        {
            VariantType type = variant.Type;
            foreach (CacheManager cache in caches)
            {
                var apiCall = cache.GetDiscoveryApiCall((Guid)variant.Variant.AssetId, (Guid)variant.Variant.VersionId, type);

                // store files in cache
                await cache.StoreBondFile(variant.Variant, apiCall);
                foreach (var file in variant.Files)
                {
                    ApiCall fileApiCall = PrimaryCache.Api.Call(variant.Variant.Files.PrefixEndpoint.AuthorityId, new()
                    {
                        { "path", variant.Variant.Files.Prefix + file.Key },
                    });
                    await cache.StoreFile(file.Value, fileApiCall, MimeType.OctetStream);
                }

                // add to game manifest
                BondAsset entry = new(variant.Variant);
                List<BondAsset> links = cache.GameManifest.LinksByType(type);
                int index = links.FindIndex(link => link.GuidsEqual(variant.Variant));
                if (index == -1)
                {
                    links.Add(entry);
                }
                else
                {
                    links[index] = entry;
                }

                // update customs manifest
                if (type != VariantType.EngineGameVariant)
                {
                    var customsLinks = cache.CustomsManifest.LinksByType(type);
                    if (customsLinks != null)
                    {
                        index = customsLinks.FindIndex(link => link.GuidsEqual(variant.Variant));
                        if (index != -1)
                        {
                            customsLinks[index] = entry;
                        }
                    }
                }
            }
        }

        public bool SetVariantEnabled(VariantAsset variant, bool enabled)
        {
            return SetVariantEnabled((Guid)variant.Variant.AssetId, (Guid)variant.Variant.VersionId, variant.Type, enabled);
        }

        public bool SetVariantEnabled(Guid assetId, Guid versionId, VariantType type, bool enabled)
        {
            List<BondAsset>? customsEntries = PrimaryCache.CustomsManifest.LinksByType(type);
            if (customsEntries == null)
            {
                return false;
            }    
            int index = customsEntries.FindIndex(link => link.GuidsEqual(assetId, versionId));
            if (enabled)
            {
                BondAsset? entry = GetVariantEntry(assetId, versionId, type);
                if (entry == null)
                {
                    return false;
                }
                else
                {
                    if (index == -1)
                    {
                        customsEntries.Add(entry);
                    }
                    else
                    {
                        customsEntries[index] = entry;
                    }
                    return true;
                }
            }
            else
            {
                if (index == -1)
                {
                    return false;
                }
                else
                {
                    customsEntries.RemoveAt(index);
                    return true;
                }
            }
        }

        public async Task RemoveVariant(VariantAsset variant)
        {
            await RemoveVariant((Guid)variant.Variant.AssetId, (Guid)variant.Variant.VersionId, variant.Type);
        }

        public async Task RemoveVariant(Guid assetId, Guid versionId, VariantType type)
        {
            foreach (CacheManager cache in caches)
            {
                ApiCall apiCall = cache.GetDiscoveryApiCall(assetId, versionId, type);
                BondAsset? metadata = await cache.TryGetCachedBondFile<BondAsset>(apiCall);
                if (metadata != null)
                {
                    foreach (string relativePath in metadata.Files.FileRelativePaths)
                    {
                        ApiCall fileApiCall = PrimaryCache.Api.Call(metadata.Files.PrefixEndpoint.AuthorityId, new()
                        {
                            { "path", metadata.Files.Prefix + relativePath },
                        });

                        cache.RemoveFile(fileApiCall);
                    }
                }
                cache.RemoveFile(apiCall);

                // remove from game manifest
                List<BondAsset> links = cache.GameManifest.LinksByType(type);
                links.RemoveAll(link => link.GuidsEqual(assetId, versionId));

                // remove from customs manifest
                if (type != VariantType.EngineGameVariant)
                {
                    List<BondAsset>? customsLinks = cache.CustomsManifest.LinksByType(type);
                    customsLinks?.RemoveAll(link => link.GuidsEqual(assetId, versionId));
                }
            }
        }

        public async Task Flush()
        {
            foreach (var cache in caches)
            {
                await cache.Flush();
            }
        }

        #region list variants

        private IEnumerable<VariantAsset> GetAssetList()
        {
            foreach (VariantType type in VariantType.VariantTypes)
            {
                var links = PrimaryCache.GameManifest.LinksByType(type);
                foreach (BondAsset link in links)
                {
                    VariantAsset variant = new(link, type);
                    yield return variant;
                }
            }
        }

        private void UpdateVariantsStatus(IEnumerable<VariantAsset> variants)
        {
            foreach (VariantType type in VariantType.VariantTypes)
            {
                var customsLinks = PrimaryCache.CustomsManifest.LinksByType(type);
                foreach (VariantAsset variant in variants)
                {
                    variant.Enabled = customsLinks?.Any(customsLink => customsLink.GuidsEqual(variant.Variant));
                }
            }
        }

        private void RefreshAssetList()
        {
            variantAssets.Clear();
            variantAssets.AddRange(GetAssetList());
            UpdateVariantsStatus(variantAssets);
            UpdateVariantsStatus(userCache.Entries);
        }

        public IEnumerable<VariantAsset> FilterVariants(VariantFilter filter)
        {
            return FilterVariants(variantAssets, filter);
        }

        public IEnumerable<VariantAsset> FilterUserVariants(VariantFilter filter)
        {
            return FilterVariants(userCache.Entries, filter);
        }

        public IEnumerable<VariantAsset> FilterVariants(IEnumerable<VariantAsset> variants, VariantFilter filter)
        {
            return variants.Where(variant => (filter.Types?.Any(type => type == variant.Type) is null or true)
                && (filter.AssetId?.Equals((Guid)variant.Variant.AssetId) is null or true)
                && (filter.VersionId?.Equals((Guid)variant.Variant.VersionId) is null or true)
                && (filter.Name?.Equals(variant.Variant.PublicName, StringComparison.CurrentCultureIgnoreCase) is null or true)
                && (filter.Enabled?.Equals(variant.Enabled) is null or true));
        }

        #endregion
    }

    public class VariantFilter
    {
        public Guid? AssetId { get; set; }
        public Guid? VersionId { get; set; }
        public string? Name { get; set; }
        public bool? Enabled { get; set; }
        public List<VariantType>? Types { get; set; }
        public VariantType? Type
        {
            set
            {
                if (value == null)
                {
                    Types = null;
                }
                else
                {
                    Types = new() { value };
                }
            }
        }
    }
}
