using InfiniteVariantTool.Core.Variants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace InfiniteVariantTool.Core.Cache
{
    public interface ICacheManager
    {
        CacheMap? CacheMap { get; }
        public GameManifest? GameManifest { get; }
        public CustomsManifest? CustomsManifest { get; }
        string BasePath { get; set; }
        bool LoadCacheMap();
        bool LoadGameManifest();
        bool LoadCustomsManifest();
        void Save();
        Task<CacheFile?> GetFile(string url, ContentType contentType);
        void SaveFile(CacheFile cacheFile, string url);
        void EnableAllVariants();
    }

    public abstract class CacheManagerBase : ICacheManager
    {
        public CacheMap? CacheMap { get; private set; }
        public GameManifest? GameManifest { get; set; }
        public CustomsManifest? CustomsManifest { get; set; }
        public CacheFile? EndpointsFile { get; set; }
        public string BasePath { get; set; }
        public string? Language { get; set; }
        public string? BuildNumber { get; set; }
        public bool Exists { get; set; }

        protected abstract string CacheMapPath { get; }
        protected abstract string GameManifestUrl { get; }
        protected abstract string GameManifestPath { get; }
        protected abstract string CustomsManifestUrl { get; }
        protected abstract string CustomsManifestPath { get; }
        public abstract string EndpointsFileUrl { get; }
        protected abstract EndpointType EndpointType_ { get; }
        protected abstract List<string> CacheFileDirs { get; }
        protected abstract string GetCacheFilePath(string url);
        protected abstract CacheFileMetadata CreateCacheFileMetadata(string url, int? contentLength = null);

        public CacheManagerBase(string basePath)
        {
            BasePath = basePath;
        }

        public virtual bool LoadCacheMap()
        {
            if (File.Exists(CacheMapPath))
            {
                CacheMap = new CacheMap(CacheMapPath);
                Language = CacheMap.Language;
                Exists = true;
                return true;
            }
            else
            {
                CacheMap = new();
                Exists = false;
                return false;
            }
        }

        public bool LoadGameManifest()
        {
            if (File.Exists(GameManifestPath))
            {
                CacheFile cacheFile = new(GameManifestPath, ContentType.Bond, true);
                GameManifest = new(cacheFile.Doc);
                return true;
            }
            else
            {
                GameManifest = new();
                return false;
            }
        }

        public bool LoadCustomsManifest()
        {
            if (File.Exists(CustomsManifestPath))
            {
                CacheFile cacheFile = new(CustomsManifestPath, ContentType.Bond, true);
                CustomsManifest = new(cacheFile.Doc);
                return true;
            }
            else
            {
                CustomsManifest = new();
                return false;
            }
        }

        public virtual async Task<CacheFile?> GetFile(string url, ContentType contentType)
        {
            ulong hash = UrlHasher.HashUrl(url, EndpointType_);
            foreach (string path in CacheFileDirs)
            {
                string filename = Path.Combine(path, hash.ToString());
                if (File.Exists(filename))
                {
                    byte[] data = await File.ReadAllBytesAsync(filename);
                    return new CacheFile(data, contentType, true);
                }
            }
            return null;
        }

        public virtual void Save()
        {
            if (CacheMap == null)
            {
                throw new InvalidOperationException();
            }

            if (EndpointsFile != null)
            {
                SaveFile(EndpointsFile, EndpointsFileUrl);
            }

            if (GameManifest != null)
            {
                var manifestCacheFile = new CacheFile(GameManifest.Serialize());
                SaveFile(manifestCacheFile, GameManifestUrl);
            }

            if (CustomsManifest != null)
            {
                var manifestCacheFile = new CacheFile(CustomsManifest.Serialize());
                SaveFile(manifestCacheFile, CustomsManifestUrl);
            }

            CacheMap.Language = Language ?? throw new Exception();
            CacheMap.Pack(CacheMapPath);
        }

        public void SaveFile(CacheFile cacheFile, string url)
        {
            if (CacheMap == null)
            {
                throw new InvalidOperationException();
            }

            ulong hash = UrlHasher.HashUrl(url, EndpointType_);
            string filePath = GetCacheFilePath(url);

            // pack just the content to get Content-Length (maybe not needed?)
            byte[] packed = cacheFile.Content.Pack();
            cacheFile = new CacheFile(packed, ContentType.Undefined, false);

            // add metadata
            CacheMapEntry entry;
            if (CacheMap.Map.TryGetValue(hash, out var existingEntry))
            {
                if (existingEntry.Metadata.Headers.ContainsKey("Content-Length"))
                {
                    existingEntry.Metadata.Headers["Content-Length"] = packed.Length.ToString();
                }
                cacheFile.Metadata = existingEntry.Metadata;
                entry = existingEntry;
            }
            else
            {
                cacheFile.Metadata = CreateCacheFileMetadata(url, packed.Length);
                entry = new()
                {
                    Metadata = cacheFile.Metadata
                };
                CacheMap.Map.Add(hash, entry);
            }
            entry.Metadata.Etag = "";
            entry.Metadata.FileId = 0;
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            entry.Size = (ulong)cacheFile.Pack(filePath).Length;
        }

        public void RemoveFile(string url)
        {
            if (CacheMap == null)
            {
                throw new InvalidOperationException();
            }

            ulong hash = UrlHasher.HashUrl(url, EndpointType_);
            string filePath = GetCacheFilePath(url);

            CacheMap.Map.Remove(hash);
            File.Delete(filePath);
        }

        public void EnableAllVariants()
        {
            if (CustomsManifest == null || GameManifest == null)
            {
                throw new InvalidOperationException();
            }
            CustomsManifest.MapLinks.Clear();
            CustomsManifest.MapLinks.AddRange(GameManifest.MapLinks);
            CustomsManifest.UgcGameVariantLinks.Clear();
            CustomsManifest.UgcGameVariantLinks.AddRange(GameManifest.UgcGameVariantLinks);
        }
    }
}
