using InfiniteVariantTool.Core.Cache;
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
        public record LanguageInfo(
            string ShortCode,
            string Code,
            string Name);

        public static List<LanguageInfo> LanguageCodes { get; } = new()
        {
            new("en",  "en-US", "English"),
            new("jpn", "ja-JP", "Japanese"),
            new("de",  "de-DE", "German"),
            new("fr",  "fr-FR", "French"),
            new("sp",  "es-ES", "Spanish"),
            new("mx",  "es-MX", "Spanish (Mexico)"),
            new("it",  "it-IT", "Italian"),
            new("kor", "ko-KR", "Korean"),
            new("cht", "zh-TW", "Chinese (Traditional)"),
            new("chs", "zh-CN", "Chinese (Simplified)"),
            new("pl",  "pl-PL", "Polish"),
            new("ru",  "ru-RU", "Russian"),
            new("nl",  "nl-NL", "Dutch"),
            new("br",  "pt-BR", "Portuguese (Brazil)"),

            new("dk", "", ""),
            new("fi", "", ""),
            new("no", "", ""),
            new("pt", "", "")
        };
        public static Dictionary<string, LanguageInfo> LanguageCodeMap { get; } = LanguageCodes
            .Where(info => info.Code != "")
            .ToDictionary(
                info => info.Code,
                info => info);

        private string gameDir;
        public OfflineCacheManager? OfflineCache { get; private set; }
        public OnlineCacheManager? OnlineCache { get; private set; }
        public LanCacheManager? LanCache { get; private set; }
        public UserCacheManager? UserCache { get; private set; }

        [MemberNotNullWhen(true, nameof(OfflineCache), nameof(OnlineCache), nameof(LanCache))]
        private bool CachesAreInitialized => OfflineCache != null && OnlineCache != null && LanCache != null;

        public VariantManager(string gameDir)
        {
            this.gameDir = gameDir;
            if (!File.Exists(Path.Combine(gameDir, Constants.GameExeName)))
            {
                throw new FileNotFoundException("Game not found at " + gameDir);
            }
        }

        public string? LoadLanguage()
        {
            // try to infer language
            string? language = null;
            OnlineCache = new(Path.Combine(gameDir, Constants.OnlineCacheDirectory));
            if (OnlineCache.LoadCacheMap())
            {
                language = OnlineCache.Language;
            }
            LanCache = new(Path.Combine(gameDir, Constants.LanCacheDirectory));
            if (LanCache.LoadCacheMap())
            {
                language = LanCache.Language;
            }

            return language;
        }

        public void LoadCache(Func<string> languagePicker)
        {
            LoadCache(LoadLanguage() ?? languagePicker());
        }

        public void LoadCache(string language)
        {
            // Finish loading cache
            OfflineCache = new(Path.Combine(gameDir, Constants.OfflineCacheDirectory), language);
            OfflineCache.LoadCacheMap();
            OfflineCache.LoadGameManifest();
            OfflineCache.LoadCustomsManifest();

            if (OnlineCache == null)
            {
                OnlineCache = new(Path.Combine(gameDir, Constants.OnlineCacheDirectory));
                OnlineCache.LoadCacheMap();
            }
            OnlineCache.Language = language;
            OnlineCache.BuildNumber = OfflineCache.BuildNumber;
            OnlineCache.LoadGameManifest();
            OnlineCache.LoadCustomsManifest();

            if (LanCache == null)
            {
                LanCache = new(Path.Combine(gameDir, Constants.LanCacheDirectory));
                OnlineCache.LoadCacheMap();
            }
            LanCache.Language = language;
            LanCache.AssetId = OfflineCache.GameManifest!.Base.AssetId;
            LanCache.VersionId = OfflineCache.GameManifest!.Base.VersionId;
            LanCache.BuildNumber = OfflineCache.BuildNumber;
            LanCache.LoadGameManifest();

            MergeManifests();
        }

        private void MergeManifests()
        {
            if (!CachesAreInitialized || OfflineCache.GameManifest == null || OfflineCache.CustomsManifest == null || OnlineCache.GameManifest == null || OnlineCache.CustomsManifest == null || OnlineCache.CacheMap == null)
            {
                throw new InvalidOperationException();
            }

            // if customs manifest has different version IDs than game manifest, switch to game manifest's version IDs
            foreach (var entry in OfflineCache.CustomsManifest.GetVariants(null, null, null, null))
            {
                var matches = OfflineCache.GameManifest.GetVariants(entry.Metadata.AssetId, null, entry.Type, null);
                if (!matches.Any(matchEntry => matchEntry.Metadata.VersionId == entry.Metadata.VersionId))
                {
                    foreach (var match in matches)
                    {
                        entry.Metadata.SetAssetId(match.Metadata.AssetId);
                        entry.Metadata.SetVersionId(match.Metadata.VersionId);
                    }
                }
            }

            // if online customs manifest has any entries missing from offline customs manifest, add them
            if (!OnlineCache.CustomsManifest.CompareContentsByGuid(OfflineCache.CustomsManifest))
            {
                foreach (var entry in OnlineCache.CustomsManifest.GetVariants(null, null, null, null))
                {
                    if (!OfflineCache.CustomsManifest.GetVariants(entry.Metadata.AssetId, null, entry.Type, null).Any())
                    {
                        foreach (var match in OfflineCache.GameManifest.GetVariants(entry.Metadata.AssetId, null, entry.Type, null))
                        {
                            OfflineCache.CustomsManifest.AddVariant(match);
                        }
                    }
                }
            }
        }

        public UserCacheManager LoadUserCache(string modDir)
        {
            UserCache = new(modDir);
            UserCache.LoadEntries();
            return UserCache;
        }

        public async Task<CacheFile?> GetFile(string url, ContentType contentType)
        {
            if (!CachesAreInitialized)
            {
                throw new InvalidOperationException();
            }

            foreach (ICacheManager cache in new ICacheManager[] { OfflineCache, OnlineCache, LanCache })
            {
                CacheFile? cacheFile = await cache.GetFile(url, contentType);
                if (cacheFile != null)
                {
                    return cacheFile;
                }
            }
            return null;
        }

        public async Task<CacheFile> GetOrDownloadFile(string url, ContentType contentType)
        {
            if (await GetFile(url, contentType) is CacheFile result)
            {
                return result;
            }

            VariantDownloader downloader = new();
            byte[] data = await downloader.DownloadFile(url, OfflineCache!.Language, contentType)
                ?? throw new HttpRequestException("Received status " + downloader.StatusCode + " for: " + url);
            return new CacheFile(data, contentType, false);
        }

        public IEnumerable<UserVariantEntry> GetUserVariantEntries()
        {
            if (UserCache == null || !CachesAreInitialized || OfflineCache.CustomsManifest == null)
            {
                throw new InvalidOperationException();
            }

            foreach (var entry in UserCache.GetEntries())
            {
                bool? actualEnabled = null;
                if (entry.Metadata.Type != VariantType.EngineGameVariant)
                {
                    actualEnabled = OfflineCache.CustomsManifest.GetVariants(entry.Metadata.Base.AssetId, entry.Metadata.Base.VersionId, null, null).Any();
                }
                entry.Enabled = actualEnabled;
                yield return entry;
            }
        }

        public void RemoveUserVariant(string path)
        {
            if (UserCache == null)
            {
                throw new InvalidOperationException();
            }
            UserCache.RemoveVariant(path);
        }

        public IEnumerable<VariantEntry> GetVariantEntries(Guid? assetId, Guid? versionId, VariantType? variantType, string? name, bool? enabled)
        {
            if (!CachesAreInitialized || OfflineCache.GameManifest == null || OfflineCache.CustomsManifest == null)
            {
                throw new InvalidOperationException();
            }

            foreach (VariantEntry entry in OfflineCache.GameManifest.GetVariants(assetId, versionId, variantType, name))
            {
                if (entry.Type == VariantType.EngineGameVariant)
                {
                    if (enabled == null)
                    {
                        yield return entry;
                    }
                }
                else
                {
                    bool actualEnabled = OfflineCache.CustomsManifest.GetVariants(entry.Metadata.AssetId, entry.Metadata.VersionId, null, null).Any();
                    if (enabled == null || enabled == actualEnabled)
                    {
                        entry.Enabled = actualEnabled;
                        yield return entry;
                    }
                }
            }
        }

        public async IAsyncEnumerable<Variant> GetVariants(Guid? assetId, Guid? versionId, VariantType? variantType, string? name, bool? enabled,
            bool getFiles, bool downloadMissingFiles, bool downloadLuaSource)
        {
            foreach (VariantEntry entry in GetVariantEntries(assetId, versionId, variantType, name, enabled))
            {
                string url = GetVariantUrl(entry.Type, entry.Metadata.AssetId, entry.Metadata.VersionId);
                CacheFile metadataCacheFile = await GetOrDownloadFile(url, ContentType.Bond);
                VariantMetadata metadata = VariantMetadata.FromXml(entry.Type, metadataCacheFile.Doc);
                Dictionary<string, CacheFile> files;
                if (getFiles)
                {
                    files = await GetVariantFiles(metadata, downloadMissingFiles, downloadLuaSource);
                }
                else
                {
                    files = new();
                }
                var variant = Variant.FromMetadata(metadata, files);
                variant.Enabled = entry.Enabled;
                yield return variant;
            }
        }

        public async Task<Variant> GetVariant(Guid? assetId, Guid? versionId, VariantType? variantType, string? name, bool? enabled, bool includeLinkedVariant,
            bool getFiles, bool downloadMissingFiles, bool downloadLuaSource)
        {
            Variant? ret = null;
            await foreach (var variant in GetVariants(assetId, versionId, variantType, name, enabled, getFiles, downloadMissingFiles, downloadLuaSource))
            {
                if (ret != null)
                {
                    throw new Exception("More than one match");
                }
                ret = variant;
            }
            if (ret == null)
            {
                throw new Exception("No matches");
            }

            if (includeLinkedVariant && ret is UgcGameVariant ugcGameVariant && ugcGameVariant.Metadata.EngineGameVariantLink != null)
            {
                Guid linkAssetId = ugcGameVariant.Metadata.EngineGameVariantLink.AssetId;
                Guid linkVersionId = ugcGameVariant.Metadata.EngineGameVariantLink.VersionId;
                await foreach (var variant in GetVariants(linkAssetId, linkVersionId, VariantType.EngineGameVariant, null, null, getFiles, downloadMissingFiles, downloadLuaSource))
                {
                    ugcGameVariant.LinkedEngineGameVariant = (EngineGameVariant)variant;
                    break;
                }
            }

            return ret;
        }

        private async Task<Dictionary<string, CacheFile>> GetVariantFiles(VariantMetadata metadata, bool downloadMissingFiles = true, bool downloadLuaSource = true)
        {
            if (!CachesAreInitialized)
            {
                throw new InvalidOperationException();
            }

            Dictionary<string, CacheFile> files = new();
            foreach (string path in metadata.Base.FileRelativePaths)
            {
                bool includeFile = false;
                ContentType contentType = ContentType.Bin;
                if (path.StartsWith("CustomGamesUIMarkup"))
                {
                    string shortLanguageCode = LanguageCodeMap[OfflineCache.Language!].ShortCode;
                    if (path.EndsWith($"_{shortLanguageCode}.bin"))
                    {
                        includeFile = true;
                        contentType = ContentType.Undefined;
                    }
                }
                else if (path.EndsWith(".mvar") || path.EndsWith(".bin"))
                {
                    includeFile = true;
                    contentType = ContentType.Undefined;
                }
                else if (path.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase)
                    || (downloadLuaSource && path.EndsWith(".debugscriptsource")))
                {
                    includeFile = true;
                    contentType = ContentType.Undefined;
                }
                if (includeFile)
                {
                    string fileUrl = metadata.Base.Prefix + path;
                    if (downloadMissingFiles)
                    {
                        files[path] = await GetOrDownloadFile(fileUrl, contentType);
                    }
                    else
                    {
                        if (await GetFile(fileUrl, contentType) is CacheFile result)
                        {
                            files[path] = result;
                        }
                    }
                }
            }

            return files;
        }

        public void SaveVariant(Variant variant)
        {
            if (!CachesAreInitialized || OfflineCache.GameManifest == null)
            {
                throw new InvalidOperationException();
            }

            foreach (var entry in variant.Files)
            {
                string path = entry.Key;

                // switch language-specific files to user's language
                var match = Regex.Match(entry.Key, @"_([a-z]{2,3})\.bin$");
                if (match.Success)
                {
                    string languageCode = LanguageCodeMap[OfflineCache.Language!].ShortCode;
                    path = path[..match.Groups[1].Index] + languageCode + ".bin";
                }

                string fileUrl = variant.Metadata.Base.Prefix + path;
                OfflineCache.SaveFile(entry.Value, fileUrl);
            }
            var metadataCacheFile = new CacheFile(variant.Metadata.Serialize());
            OfflineCache.SaveFile(metadataCacheFile, variant.Url);

            VariantMetadataBase newEntry = new(variant.Metadata.Base);
            newEntry.IsBaseStruct = false;
            newEntry.FileRelativePaths.RemoveAll(path => !path.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase));
            OfflineCache.GameManifest.AddVariant(variant.Type, newEntry);
        }

        public async Task RemoveVariant(Guid assetId, Guid versionId)
        {
            if (!CachesAreInitialized || OfflineCache.GameManifest == null || OfflineCache.CustomsManifest == null)
            {
                throw new InvalidOperationException();
            }

            List<(Guid, Guid)> variantsToRemove = new();
            await foreach (var variant in GetVariants(assetId, versionId, null, null, null, false, false, false))
            {
                foreach (string path in variant.Metadata.Base.FileRelativePaths)
                {
                    string fileUrl = variant.Metadata.Base.Prefix + path;
                    OfflineCache.RemoveFile(fileUrl);
                }
                OfflineCache.RemoveFile(variant.Url);
                variantsToRemove.Add((assetId, versionId));
            }

            foreach ((var assetId_, var versionId_) in variantsToRemove)
            {
                OfflineCache.GameManifest.RemoveVariant(assetId_, versionId_, null, null);
                OfflineCache.CustomsManifest.RemoveVariant(assetId_, versionId_, null, null);
            }
        }

        public string GetVariantUrl(VariantType variantType, Guid assetId, Guid versionId)
        {
            string variantTypeString = variantType switch
            {
                VariantType.MapVariant => "maps",
                VariantType.UgcGameVariant => "ugcGameVariants",
                VariantType.EngineGameVariant => "engineGameVariants",
                _ => throw new ArgumentException()
            };
            return $"https://discovery-infiniteugc-intone.test.svc.halowaypoint.com/hi/{variantTypeString}/{assetId}/versions/{versionId}";
        }

        public async Task Save()
        {
            if (!CachesAreInitialized)
            {
                throw new InvalidOperationException();
            }

            //OfflineCache.EnableAllVariants();
            OfflineCache.Save();

            OnlineCache.GameManifest = new(OfflineCache.GameManifest!);
            OnlineCache.CustomsManifest = new(OfflineCache.CustomsManifest!);

            ConvertMetadataUrlsToOnline(OnlineCache.GameManifest.PlaylistLinks);
            ConvertMetadataUrlsToOnline(OnlineCache.GameManifest.EngineGameVariantLinks);
            ConvertMetadataUrlsToOnline(OnlineCache.GameManifest.MapLinks);
            ConvertMetadataUrlsToOnline(OnlineCache.GameManifest.UgcGameVariantLinks);

            ConvertMetadataUrlsToOnline(OnlineCache.CustomsManifest.PlaylistLinks);
            ConvertMetadataUrlsToOnline(OnlineCache.CustomsManifest.EngineGameVariantLinks);
            ConvertMetadataUrlsToOnline(OnlineCache.CustomsManifest.MapLinks);
            ConvertMetadataUrlsToOnline(OnlineCache.CustomsManifest.UgcGameVariantLinks);

            OnlineCache.Save();
            LanCache.EndpointsFile = await GetFile(LanCache.EndpointsFileUrl, ContentType.Bond);
            LanCache.GameManifest = new(OfflineCache.GameManifest!);
            LanCache.Save();
        }

        private void ConvertMetadataUrlsToOnline(List<VariantMetadataBase> metadatas)
        {
            foreach (var metadata in metadatas)
            {
                metadata.Prefix = ConvertUrlToOnline(metadata.Prefix);
            }
        }

        private string ConvertUrlToOnline(string url)
        {
            return url.Replace("-test.test", "").Replace("-intone.test", "");
        }

        public void EnableVariant(Guid assetId, Guid versionId)
        {
            if (!CachesAreInitialized || OfflineCache.CustomsManifest == null)
            {
                throw new InvalidOperationException();
            }

            VariantEntry gameManifestEntry = OfflineCache.GameManifest?.GetVariants(assetId, versionId, null, null).FirstOrDefault()
                ?? throw new Exception("Variant does not exist");
            OfflineCache.CustomsManifest.AddVariant(gameManifestEntry);
        }

        public void DisableVariant(Guid assetId, Guid versionId)
        {
            if (!CachesAreInitialized || OfflineCache.CustomsManifest == null)
            {
                throw new InvalidOperationException();
            }

            OfflineCache.CustomsManifest.RemoveVariant(assetId, versionId, null, null);
        }
    }
}
