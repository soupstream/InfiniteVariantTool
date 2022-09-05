using InfiniteVariantTool.Core.Cache;
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
        // the offline cache is currently busted so remove support for now
        public OfflineCacheManager? OfflineCache { get; private set; }
        public OnlineCacheManager? OnlineCache { get; private set; }
        public LanCacheManager? LanCache { get; private set; }
        public UserCacheManager? UserCache { get; private set; }

        [MemberNotNullWhen(true, nameof(OnlineCache), nameof(LanCache))]
        private bool CachesAreInitialized => OnlineCache != null && LanCache != null;

        public VariantManager(string gameDir)
        {
            this.gameDir = gameDir;
            if (!File.Exists(Path.Combine(gameDir, Constants.GameExeName)))
            {
                throw new FileNotFoundException("Game not found at " + gameDir);
            }
        }

        public void LoadCache(Func<string> languagePicker)
        {
            string buildNumber = Util.GetBuildNumber();
            OfflineCache = new(Path.Combine(gameDir, Constants.OfflineCacheDirectory), "en-US");
            OfflineCache.BuildNumber = buildNumber;
            OfflineCache.LoadGameManifest();

            OnlineCache = new(Path.Combine(gameDir, Constants.OnlineCacheDirectory));
            OnlineCache.LoadCacheMap();
            OnlineCache.BuildNumber = buildNumber;
            OnlineCache.LoadGameManifest();
            OnlineCache.LoadCustomsManifest();

            LanCache = new(Path.Combine(gameDir, Constants.LanCacheDirectory));
            LanCache.LoadCacheMap();
            LanCache.BuildNumber = buildNumber;
            LanCache.AssetId = OfflineCache.GameManifest!.Base.AssetId;
            LanCache.VersionId = OfflineCache.GameManifest.Base.VersionId;
            LanCache.LoadGameManifest();

            OnlineCache.Language ??= LanCache.Language ?? languagePicker();
            LanCache.Language ??= OnlineCache.Language;
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

            foreach (ICacheManager cache in new ICacheManager[] { OnlineCache, LanCache })
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
            byte[] data = await downloader.DownloadFile(url, OnlineCache!.Language, contentType)
                ?? throw new HttpRequestException("Received status " + downloader.StatusCode + " for: " + url);
            return new CacheFile(data, contentType, false);
        }

        public IEnumerable<UserVariantEntry> GetUserVariantEntries()
        {
            if (UserCache == null || !CachesAreInitialized || OnlineCache.CustomsManifest == null)
            {
                throw new InvalidOperationException();
            }

            foreach (var entry in UserCache.GetEntries())
            {
                bool? actualEnabled = null;
                if (entry.Metadata.Type != VariantType.EngineGameVariant)
                {
                    actualEnabled = OnlineCache.CustomsManifest.GetVariants(entry.Metadata.Base.AssetId, entry.Metadata.Base.VersionId, null, null).Any();
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
            if (!CachesAreInitialized || OnlineCache.GameManifest == null || OnlineCache.CustomsManifest == null)
            {
                throw new InvalidOperationException();
            }

            foreach (VariantEntry entry in OnlineCache.GameManifest.GetVariants(assetId, versionId, variantType, name))
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
                    bool actualEnabled = OnlineCache.CustomsManifest.GetVariants(entry.Metadata.AssetId, entry.Metadata.VersionId, null, null).Any();
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
                    string shortLanguageCode = LanguageCodeMap[OnlineCache.Language!].ShortCode;
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
            if (!CachesAreInitialized || OnlineCache.GameManifest == null)
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
                    string languageCode = LanguageCodeMap[OnlineCache.Language!].ShortCode;
                    path = path[..match.Groups[1].Index] + languageCode + ".bin";
                }

                string fileUrl = variant.Metadata.Base.Prefix + path;
                OnlineCache.SaveFile(entry.Value, fileUrl);
            }
            var metadataCacheFile = new CacheFile(variant.Metadata.Serialize());
            OnlineCache.SaveFile(metadataCacheFile, variant.Url);

            VariantMetadataBase newEntry = new(variant.Metadata.Base);
            newEntry.IsBaseStruct = false;
            newEntry.FileRelativePaths.RemoveAll(path => !path.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase));
            OnlineCache.GameManifest.AddVariant(variant.Type, newEntry);
        }

        public async Task RemoveVariant(Guid assetId, Guid versionId)
        {
            if (!CachesAreInitialized || OnlineCache.GameManifest == null || OnlineCache.CustomsManifest == null)
            {
                throw new InvalidOperationException();
            }

            List<(Guid, Guid)> variantsToRemove = new();
            await foreach (var variant in GetVariants(assetId, versionId, null, null, null, false, false, false))
            {
                foreach (string path in variant.Metadata.Base.FileRelativePaths)
                {
                    string fileUrl = variant.Metadata.Base.Prefix + path;
                    OnlineCache.RemoveFile(fileUrl);
                }
                OnlineCache.RemoveFile(variant.Url);
                variantsToRemove.Add((assetId, versionId));
            }

            foreach ((var assetId_, var versionId_) in variantsToRemove)
            {
                OnlineCache.GameManifest.RemoveVariant(assetId_, versionId_, null, null);
                OnlineCache.CustomsManifest.RemoveVariant(assetId_, versionId_, null, null);
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
            return $"https://discovery-infiniteugc.test.svc.halowaypoint.com/hi/{variantTypeString}/{assetId}/versions/{versionId}";
        }

        public async Task Save()
        {
            if (!CachesAreInitialized)
            {
                throw new InvalidOperationException();
            }

            //OfflineCache.EnableAllVariants();
            OnlineCache.Save();

            OnlineCache.GameManifest = new(OnlineCache.GameManifest!);
            OnlineCache.CustomsManifest = new(OnlineCache.CustomsManifest!);

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
            LanCache.GameManifest = new(OnlineCache.GameManifest!);
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
            if (!CachesAreInitialized || OnlineCache.CustomsManifest == null)
            {
                throw new InvalidOperationException();
            }

            VariantEntry gameManifestEntry = OnlineCache.GameManifest?.GetVariants(assetId, versionId, null, null).FirstOrDefault()
                ?? throw new Exception("Variant does not exist");
            OnlineCache.CustomsManifest.AddVariant(gameManifestEntry);
        }

        public void DisableVariant(Guid assetId, Guid versionId)
        {
            if (!CachesAreInitialized || OnlineCache.CustomsManifest == null)
            {
                throw new InvalidOperationException();
            }

            OnlineCache.CustomsManifest.RemoveVariant(assetId, versionId, null, null);
        }
    }
}
