﻿using Bond.IO.Safe;
using Bond.Protocols;
using InfiniteVariantTool.Core.BondSchema;
using InfiniteVariantTool.Core.Serialization;
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
using System.Threading.Tasks;

namespace InfiniteVariantTool.Core.Cache
{
    public class CacheManager
    {
        string directory;
        bool localized;
        string buildNumber;
        HttpClient client;
        string cacheMapPath;
        ApiCall apiManifestApiCall;
        ApiCall gameManifestApiCall;
        ApiCall customsManifestApiCall;

        public GameApi Api { get; private set; }
        public CustomsManifest CustomsManifest { get; private set; }
        public GameManifest GameManifest { get; private set; }
        public CacheMap CacheMap { get; private set; }

        public Language? Language { get; private set; }

        #region Construction

        private CacheManager(string directory)
        {
            this.directory = directory;
            cacheMapPath = Path.Combine(directory, "CacheMap.wcache");

            // initialize with default values
            localized = false;
            buildNumber = "";
            Api = new();

            client = new();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.AcceptLanguage.Clear();
            client.DefaultRequestHeaders.UserAgent.Clear();

            // initialized in Load()
            CacheMap = null!;
            CustomsManifest = null!;
            GameManifest = null!;
            apiManifestApiCall = null!;
            gameManifestApiCall = null!;
            customsManifestApiCall = null!;
        }

        private async Task LoadManifests(string apiManifestUrl, string gameManifestEndpointId, string customsManifestEndpointId, Dictionary<string, string> parameters)
        {
            if (File.Exists(cacheMapPath))
            {
                CacheMap = SchemaSerializer.DeserializeBond<CacheMap>(await File.ReadAllBytesAsync(cacheMapPath));
                Language = Language.TryFromCode(CacheMap.Language);
            }
            else
            {
                CacheMap = new();
                if (Language != null)
                {
                    CacheMap.Language = Language.Code;
                }
            }
            apiManifestApiCall = Api.CallUrl(apiManifestUrl) ?? throw new Exception("invalid api manifest url: " + apiManifestUrl);
            Api = new(await GetBondFile<ApiManifest>(apiManifestApiCall));
            gameManifestApiCall = Api.Call(gameManifestEndpointId, parameters);
            customsManifestApiCall = Api.Call(customsManifestEndpointId, parameters);
            GameManifest = await TryGetCachedBondFile<GameManifest>(gameManifestApiCall) ?? new();
            CustomsManifest = await TryGetCachedBondFile<CustomsManifest>(customsManifestApiCall) ?? new();
        }

        public static async Task<CacheManager> LoadOnlineCache(string gameDir, string buildNumber)
        {
            CacheManager cache = new(Path.Combine(gameDir, "disk_cache", "webcache"));
            await cache.LoadManifests(
                "https://settings.svc.halowaypoint.com/settings/hipc/e2a0a7c6-6efe-42af-9283-c2ab73250c48",
                "HIUGC_Discovery_GetManifestByBuild",
                "HIUGC_Discovery_GetCustomGameManifest",
                new()
                {
                    { "buildNumber", buildNumber }
                });
            return cache;
        }

        public static async Task<CacheManager> LoadLanCache(string gameDir, string buildNumber)
        {
            CacheManager cache = new(Path.Combine(gameDir, "server_disk_cache", "webcache"));
            await cache.LoadManifests(
                "https://settings.svc.halowaypoint.com/settings/hipcxolocalds/6858ba34-18a8-4030-84b8-1df01ff8ad34",
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
                "https://settings-intone.test.svc.halowaypoint.com/settings/hipc/e2a0a7c6-6efe-42af-9283-c2ab73250c48",
                "HIUGC_Discovery_GetManifestByBuild",
                "HIUGC_Discovery_GetCustomGameManifest",
                new()
                {
                    { "buildNumber", buildNumber }
                });
            return cache;
        }

        public record CacheGroup(
            CacheManager Online,
            CacheManager Offline,
            CacheManager Lan);

        public static async Task<CacheGroup> LoadAllCaches(string gameDir, Func<Language> languagePicker)
        {
            string buildNumber = FileUtil.GetBuildNumber(gameDir);
            CacheManager onlineCache = await LoadOnlineCache(gameDir, buildNumber);
            CacheManager lanCache = await LoadLanCache(gameDir, buildNumber);
            Language language = onlineCache.Language ?? lanCache.Language ?? languagePicker();
            onlineCache.Language ??= language;
            lanCache.Language ??= language;
            CacheManager offlineCache = await LoadOfflineCache(gameDir, language, buildNumber);
            return new CacheGroup(onlineCache, offlineCache, lanCache);
        }

        #endregion

        #region URLs

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
                foreach (string subdir in new string[] { Language!.Code, "common" })
                {
                    filePath = Path.Combine(string.Format(directory, subdir), hash.ToString());
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
                filePath = Path.Combine(directory, hash.ToString());
                return CacheMap.Entries.ContainsKey(hash)
                    && CacheMap.Entries[hash].Metadata.Headers.TryGetValue("Content-Type", out string? cachedContentType)
                    && cachedContentType == mimeType.Value
                    && File.Exists(filePath);
            }
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
            client.Timeout = TimeSpan.FromMilliseconds(retryPolicy.TimeoutMs);
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
                    req.Headers.Add("User-Agent", $"SHIVA-2043073184/{buildNumber}.0 (release; PC)");
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
                throw new NotImplementedException();
            }

            // write file with metadata
            CacheFile cacheFile = new()
            {
                Metadata = new()
                {
                    Headers = new()
                    {
                        { "Content-Type", mimeType.Value }
                    },
                    Url = apiCall.Url
                },
                Data = (sbyte[])(Array)data
            };
            ulong hash = apiCall.Hash;
            string filePath = Path.Combine(directory, hash.ToString());
            await File.WriteAllBytesAsync(filePath, SchemaSerializer.SerializeBond(cacheFile));

            // add entry to cache map
            var fileTime = DateTime.UtcNow.ToFileTime();
            CacheMap.Entry entry = new()
            {
                CreateTime = fileTime,
                AccessTime = fileTime,
                WriteTime = fileTime,
                Metadata = cacheFile.Metadata,
                Size = (ulong)data.LongLength
            };
            CacheMap.Entries[hash] = entry;
        }

        public async Task StoreBondFile(object src, ApiCall apiCall)
        {
            await StoreFile(SchemaSerializer.SerializeBond(src), apiCall, MimeType.Bond);
        }

        public void RemoveFile(ApiCall apiCall)
        {
            if (localized)
            {
                throw new NotImplementedException();
            }

            var hash = apiCall.Hash;
            if (CacheMap.Entries.ContainsKey(hash))
            {
                CacheMap.Entries.Remove(hash);
            }
            string filePath = Path.Combine(directory, hash.ToString());
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        // write cachemap and manifests to disk
        public async Task Flush()
        {
            await StoreBondFile(GameManifest, gameManifestApiCall);
            await StoreBondFile(CustomsManifest, customsManifestApiCall);
            await File.WriteAllBytesAsync(cacheMapPath, SchemaSerializer.SerializeBond(CacheMap));
        }

        #endregion
    }
}