using Bond.IO.Safe;
using Bond.Protocols;
using InfiniteVariantTool.Core.BondSchema;
using InfiniteVariantTool.Core.Serialization;
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
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InfiniteVariantTool.Core.Cache
{
    public enum ApiType
    {
        Online,
        Offline,
        Lan
    }

    public class CacheManager
    {
        bool localized;
        HttpClient client;
        string cacheMapPath;

        public ApiCall ApiManifestApiCall { get; private set; }
        public ApiCall GameManifestApiCall { get; private set; }
        public ApiCall CustomsManifestApiCall { get; private set; }

        public GameApi Api { get; private set; }
        public CustomsManifest CustomsManifest { get; private set; }
        public GameManifest GameManifest { get; private set; }
        public CacheMap CacheMap { get; private set; }

        public Language? Language { get; private set; }
        public string BuildNumber { get; private set; }
        public string Directory { get; private set; }
        public IEnumerable<string> Directories
        {
            get
            {
                if (localized)
                {
                    foreach (string subdir in new string[] { Language!.Code, "common" })
                    {
                        yield return string.Format(Directory, subdir);
                    }
                }
                else
                {
                    yield return Directory;
                }
            }
        }

        #region Construction

        private CacheManager(string directory)
        {
            this.Directory = directory;
            cacheMapPath = Path.Combine(directory, "CacheMap.wcache");

            // initialize with default values
            localized = false;
            BuildNumber = "";
            Api = new();

            client = new();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.AcceptLanguage.Clear();
            client.DefaultRequestHeaders.UserAgent.Clear();

            // initialized in Load()
            CacheMap = null!;
            CustomsManifest = null!;
            GameManifest = null!;
            ApiManifestApiCall = null!;
            GameManifestApiCall = null!;
            CustomsManifestApiCall = null!;
        }

        private async Task LoadManifests(string apiManifestUrl, string gameManifestEndpointId, string customsManifestEndpointId, Dictionary<string, string> parameters)
        {
            if (localized && Language != null)
            {
                cacheMapPath = String.Format(cacheMapPath, Language.Code);
            }
            if (File.Exists(cacheMapPath))
            {
                CacheMap = SchemaSerializer.DeserializeBond<CacheMap>(await File.ReadAllBytesAsync(cacheMapPath));
                Language = Language.TryFromCode(CacheMap.Language);

                // if build guid is needed, find it in cachemap
                if (parameters.TryGetValue("buildGuid", out var buildGuid) && buildGuid == "")
                {
                    foreach (var entry in CacheMap.Entries)
                    {
                        var matches = Regex.Matches(entry.Value.Metadata.Url, ".*/manifests/guids/([a-zA-Z0-9]*)/game$");
                        if (matches.Count > 0)
                        {
                            parameters["buildGuid"] = matches[0].Groups[1].Value;
                            break;
                        }
                    }
                }
            }
            else
            {
                CacheMap = new();
                if (Language != null)
                {
                    CacheMap.Language = Language.Code;
                }
            }
            ApiManifestApiCall = Api.CallUrl(apiManifestUrl) ?? throw new Exception("invalid api manifest url: " + apiManifestUrl);
            Api = new(await GetBondFile<ApiManifest>(ApiManifestApiCall));
            GameManifestApiCall = Api.Call(gameManifestEndpointId, parameters);
            CustomsManifestApiCall = Api.Call(customsManifestEndpointId, parameters);
            GameManifest = await TryGetCachedBondFile<GameManifest>(GameManifestApiCall) ?? new();
            CustomsManifest = await TryGetCachedBondFile<CustomsManifest>(CustomsManifestApiCall) ?? new();

            FixCustomsManifest();
        }

        // fix missing variants and version ID mismatches between game manifest and customs manifest
        private void FixCustomsManifest()
        {
            foreach (var type in VariantType.VariantTypes)
            {
                var customsManifestLinks = type.GetLinks(CustomsManifest);
                if (customsManifestLinks != null)
                {
                    var gameManifestLinks = type.GetLinks(GameManifest);

                    var customsManifestLinkMap = customsManifestLinks.ToDictionary(
                        link => (Guid)link.AssetId,
                        link => link);
                    var gameManifestLinkMap = gameManifestLinks.ToDictionary(
                        link => (Guid)link.AssetId,
                        link => link);

                    // fix version ID mismatches
                    foreach (var customsManifestLink in customsManifestLinks)
                    {
                        Guid assetId = (Guid)customsManifestLink.AssetId;
                        if (gameManifestLinkMap.TryGetValue(assetId, out var gameManifestLink))
                        {
                            customsManifestLink.SetGuids(null, (Guid)gameManifestLink.VersionId);
                            customsManifestLinkMap.Remove(assetId);
                            gameManifestLinkMap.Remove(assetId);
                        }
                    }

                    // fix missing variants
                    foreach (var customsManifestLink in customsManifestLinkMap.Values)
                    {
                        gameManifestLinks.Add(new BondAsset(customsManifestLink));
                    }
                }
            }
        }

        public static async Task<CacheManager> LoadOnlineCache(string gameDir, string buildNumber, string buildGuid)
        {
            CacheManager cache = new(Path.Combine(gameDir, "disk_cache", "webcache"));
            await cache.LoadManifests(
                "https://settings.svc.halowaypoint.com/settings/hipc/2b3d52f6-0ed1-4d50-ba8a-254c4c77bba2",
                "HIUGC_Discovery_GetManifestByBuildGuid",
                "HIUGC_Discovery_GetCustomGameManifest",
                new()
                {
                    { "buildNumber", buildNumber },
                    { "buildGuid", buildGuid },
                });
            return cache;
        }

        public static async Task<CacheManager> LoadLanCache(string gameDir, string buildNumber)
        {
            CacheManager cache = new(Path.Combine(gameDir, "server_disk_cache", "webcache"));
            await cache.LoadManifests(
                "https://settings.svc.halowaypoint.com/settings/hipcxolocalds/5f0d32ed-4458-46a2-8336-2968459eb826",
                "HIUGC_Discovery_GetManifestForLocalDs",
                "HIUGC_Discovery_GetCustomGameManifest",
                new());
            return cache;
        }

        public static async Task<CacheManager> LoadOfflineCache(string gameDir, Language language, string buildNumber)
        {
            CacheManager cache = new(Path.Combine(gameDir, "package", "pc", "{0}", "other"))
            {
                localized = true,
                Language = language,
            };
            await cache.LoadManifests(
                "https://settings-intone.test.svc.halowaypoint.com/settings/hi343ds/0ae7d2c7-4c03-4283-bfbf-b0012f677ace",
                "HIUGC_Discovery_GetManifestByBuildGuid",
                "HIUGC_Discovery_GetCustomGameManifest",
                new()
                {
                    { "buildNumber", buildNumber },
                    { "buildGuid", "" }
                });
            return cache;
        }

        public record CacheGroup(
            CacheManager Online,
            CacheManager Offline,
            CacheManager Lan);

        // load with user settings
        public static async Task<CacheGroup> LoadAllCaches(Func<Language> languagePicker)
        {
            string gameDir = UserSettings.Instance.GameDirectory;
            string buildNumber = await FileUtil.GetBuildNumber(gameDir);
            return await LoadAllCaches(gameDir, buildNumber, languagePicker);
        }

        public static async Task<CacheGroup> LoadAllCaches(string gameDir, string buildNumber, Func<Language> languagePicker)
        {
            // load offline cache first to get build GUID, then reload later with correct language if not English
            CacheManager offlineCache = await LoadOfflineCache(gameDir, Language.En, buildNumber);
            string buildGuid = offlineCache.GameManifestApiCall.Parameters["buildGuid"];
            CacheManager onlineCache = await LoadOnlineCache(gameDir, buildNumber, buildGuid);
            CacheManager lanCache = await LoadLanCache(gameDir, buildNumber);
            Language language = onlineCache.Language ?? lanCache.Language ?? languagePicker();
            onlineCache.Language ??= language;
            lanCache.Language ??= language;
            if (language != Language.En)
            {
                offlineCache = await LoadOfflineCache(gameDir, language, buildNumber);
            }
            return new CacheGroup(onlineCache, offlineCache, lanCache);
        }

        #endregion

        #region URLs
        public ApiCall GetDiscoveryApiCall(BondAsset asset, VariantType type)
        {
            return GetDiscoveryApiCall((Guid)asset.AssetId, (Guid)asset.VersionId, type);
        }

        public ApiCall GetDiscoveryApiCall(Guid assetId, Guid versionId, VariantType type)
        {
            return Api.Call(type.EndpointId, new()
            {
                { "assetId", assetId.ToString() },
                { "versionId", versionId.ToString() }
            });
        }

        #endregion

        #region Read cache

        // retrieve file from disk or API
        public async Task<byte[]> GetFile(ApiCall apiCall, MimeType mimeType)
        {
            return await TryGetCachedFile(apiCall, mimeType) ?? await DownloadFile(apiCall, mimeType);
        }

        public async Task<byte[]?> TryGetFile(ApiCall apiCall, MimeType mimeType)
        {
            byte[]? file = await TryGetCachedFile(apiCall, mimeType);
            if (file == null)
            {
                try
                {
                    file = await DownloadFile(apiCall, mimeType);
                }
                catch (HttpRequestException ex)
                {
                    if (ex.StatusCode != HttpStatusCode.NotFound)
                    {
                        throw;
                    }
                }
            }
            return file;
        }

        public async Task<byte[]?> TryGetCachedFile(ApiCall apiCall, MimeType mimeType)
        {
            if (FileIsCached(apiCall, mimeType, out var filePath))
            {
                CacheFile cacheFile = SchemaSerializer.DeserializeBond<CacheFile>(await File.ReadAllBytesAsync(filePath));
                return (byte[])(Array)cacheFile.Data;
            }
            return null;
        }

        public bool FileIsCached(ApiCall apiCall, MimeType mimeType, [MaybeNullWhen(false)] out string filePath)
        {
            ulong hash = apiCall.Hash;
            if (localized)
            {
                // localized cache is currently missing CacheMap.wcache so don't check it
                foreach (string dir in Directories)
                {
                    filePath = Path.Combine(dir, hash.ToString());
                    if (File.Exists(filePath))
                    {
                        return true;
                    }
                }
                filePath = null;
                return false;
            }
            else
            {
                filePath = Path.Combine(Directory, hash.ToString());
                return CacheMap.Entries.ContainsKey(hash)
                    && CacheMap.Entries[hash].Metadata.Headers.TryGetValue("Content-Type", out string? cachedContentType)
                    && cachedContentType == mimeType.Value
                    && File.Exists(filePath);
            }
        }

        public string? GetFilePathIfExists(ApiCall apiCall)
        {
            string hash = apiCall.Hash.ToString();
            foreach (string subdir in Directories)
            {
                string filePath = Path.Combine(subdir, hash);
                if (File.Exists(filePath))
                {
                    return filePath;
                }
            }
            return null;
        }

        public async Task<T> GetBondFile<T>(ApiCall apiCall)
        {
            return SchemaSerializer.DeserializeBond<T>(await GetFile(apiCall, MimeType.Bond));
        }

        public async Task<object> GetBondFile(ApiCall apiCall, Type type)
        {
            return SchemaSerializer.DeserializeBond(await GetFile(apiCall, MimeType.Bond), type);
        }

        public async Task<T?> TryGetCachedBondFile<T>(ApiCall apiCall) where T : class
        {
            if (await TryGetCachedFile(apiCall, MimeType.Bond) is byte[] data)
            {
                return SchemaSerializer.DeserializeBond<T>(data);
            }
            return null;
        }

        public async Task<byte[]> DownloadFile(ApiCall apiCall, MimeType mimeType)
        {
            ApiManifest.RetryPolicy retryPolicy = apiCall.ApiEndpoint.RetryPolicy;
            string url = apiCall.Url;
            //client.Timeout = TimeSpan.FromMilliseconds(retryPolicy.TimeoutMs);
            int retriesLeft = retryPolicy.RetryOptions?.MaxRetryCount ?? 0;
            float retryDelay = retryPolicy.RetryOptions?.RetryDelayMs ?? 0;
            Random random = new();
            do
            {
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    req.Headers.Add("Accept", mimeType.Value);
                    if (Language != null)
                    {
                        req.Headers.Add("Accept-Language", Language.Code);
                    }
                    req.Headers.Add("User-Agent", $"SHIVA-2043073184/{BuildNumber}.0 (release; PC)");
                    var res = await client.SendAsync(req);
                    res.EnsureSuccessStatusCode();
                    byte[] content = await res.Content.ReadAsByteArrayAsync();
                    await StoreFile(content, apiCall, mimeType);
                    return content;
                }
                catch (HttpRequestException ex)
                {
                    if (retryPolicy.RetryOptions == null)
                    {
                        throw ex;
                    }
                    else if (ex.StatusCode == HttpStatusCode.NotFound && !retryPolicy.RetryOptions.RetryIfNotFound)
                    {
                        throw ex;
                    }
                    else if (retriesLeft <= 0)
                    {
                        throw ex;
                    }
                }
                await Task.Delay((int)retryDelay + random.Next((int)retryPolicy.RetryOptions.RetryJitterMs));
                retryDelay *= retryPolicy.RetryOptions.RetryGrowth;
                retriesLeft--;
            } while (retriesLeft > 0);

            throw new TimeoutException("request timed out");
        }

        #endregion

        #region Write cache

        public async Task StoreFile(byte[] data, ApiCall apiCall, MimeType mimeType)
        {
            if (localized)
            {
                return;
            }

            // get or create cache map entry
            ulong hash = apiCall.Hash;
            CacheMap.Entry entry;
            if (CacheMap.Entries.ContainsKey(hash))
            {
                entry = CacheMap.Entries[hash];
                entry.Metadata.Url = apiCall.Url;
                entry.Metadata.Timestamp = 0;
            }
            else
            {
                var fileTime = DateTime.UtcNow.ToFileTime();
                entry = new()
                {
                    CreateTime = fileTime,
                    AccessTime = fileTime,
                    WriteTime = fileTime,
                    Metadata = new()
                    {
                        Headers = new()
                        {
                            { "Content-Type", mimeType.Value }
                        },
                        Url = apiCall.Url
                    },
                    Size = 0
                };
                CacheMap.Entries[hash] = entry;
            }

            // write file with metadata
            CacheFile cacheFile = new()
            {
                Metadata = entry.Metadata,
                Data = (sbyte[])(Array)data
            };

            string filePath = Path.Combine(Directory, hash.ToString());
            System.IO.Directory.CreateDirectory(Directory);
            byte[] cacheData = SchemaSerializer.SerializeBond(cacheFile);
            await File.WriteAllBytesAsync(filePath, cacheData);
            entry.Size = (ulong)cacheData.LongLength;
        }

        public async Task StoreBondFile(object src, ApiCall apiCall)
        {
            await StoreFile(SchemaSerializer.SerializeBond(src), apiCall, MimeType.Bond);
        }

        public void RemoveFile(ApiCall apiCall)
        {
            if (localized)
            {
                return;
            }

            var hash = apiCall.Hash;
            if (CacheMap.Entries.ContainsKey(hash))
            {
                CacheMap.Entries.Remove(hash);
            }
            string filePath = Path.Combine(Directory, hash.ToString());
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        // write cachemap and manifests to disk
        public async Task Flush()
        {
            if (localized)
            {
                return;
            }

            await StoreBondFile(GameManifest, GameManifestApiCall);
            await StoreBondFile(CustomsManifest, CustomsManifestApiCall);
            await File.WriteAllBytesAsync(cacheMapPath, SchemaSerializer.SerializeBond(CacheMap));
        }

        #endregion
    }
}
