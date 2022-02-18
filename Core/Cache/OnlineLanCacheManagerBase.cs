using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace InfiniteVariantTool.Core.Cache
{
    public abstract class OnlineLanCacheManagerBase : CacheManagerBase
    {
        public OnlineLanCacheManagerBase(string basePath)
            : base(basePath)
        {
        }

        public override void Save()
        {
            if (CacheMap == null)
            {
                throw new InvalidOperationException();
            }

            if (EndpointsFile != null)
            {
                ulong hash = UrlHasher.HashUrl(EndpointsFileUrl, EndpointType_);
                if (!CacheMap.Map.ContainsKey(hash))
                {
                    CacheMap.Map[hash] = CreateManifestEntry();
                }
            }

            if (GameManifest != null)
            {
                ulong hash = UrlHasher.HashUrl(GameManifestUrl, EndpointType_);
                if (!CacheMap.Map.ContainsKey(hash))
                {
                    CacheMap.Map[hash] = CreateManifestEntry();
                }
            }

            if (CustomsManifest != null)
            {
                ulong hash = UrlHasher.HashUrl(CustomsManifestUrl, EndpointType_);
                if (!CacheMap.Map.ContainsKey(hash))
                {
                    CacheMap.Map[hash] = CreateManifestEntry();
                }
            }

            base.Save();
        }

        protected override CacheFileMetadata CreateCacheFileMetadata(string url, int? contentLength = null)
        {
            CacheFileMetadata metadata = new();
            metadata.Guid = new();
            string contentType;
            if (url.StartsWith("https://blobs-"))
            {
                if (url.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
                {
                    contentType = "image/png";
                }
                else if (url.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase))
                {
                    contentType = "image/jpeg";
                }
                else
                {
                    contentType = "application/octet-stream";
                }
            }
            else
            {
                contentType = "application/x-bond-compact-binary";
            }
            metadata.Headers["Content-Type"] = contentType;
            return metadata;
        }

        protected override string GetCacheFilePath(string url)
        {
            ulong hash = UrlHasher.HashUrl(url, EndpointType_);
            return Path.Combine(CacheFileDirs[0], hash.ToString());
        }

        public override async Task<CacheFile?> GetFile(string url, ContentType contentType)
        {
            url = url.Replace("-intone.test", "").Replace("-test.test", "");
            return await base.GetFile(url, contentType);
        }

        private CacheMapEntry CreateManifestEntry()
        {
            Random random = new();
            long time = new DateTimeOffset(DateTime.UtcNow).ToFileTime();
            return new CacheMapEntry()
            {
                Date1 = time,
                Date2 = time,
                Date3 = time,
                Metadata = new()
                {
                    Headers = new()
                    {
                        { "Content-Type", "application/x-bond-compact-binary" }
                    },
                    Guid = new()
                },
                Size = 0
            };
        }

        protected override string CacheMapPath => Path.Combine(BasePath, "webcache", "CacheMap.wcache");
        protected override string GameManifestPath
        {
            get
            {
                ulong hash = UrlHasher.HashUrl(GameManifestUrl, EndpointType_);
                return Path.Combine(BasePath, "webcache", hash.ToString());
            }
        }
        protected override string CustomsManifestPath
        {
            get
            {
                ulong hash = UrlHasher.HashUrl(CustomsManifestUrl, EndpointType_);
                return Path.Combine(BasePath, "webcache", hash.ToString());
            }
        }
        protected override string CustomsManifestUrl => "https://discovery-infiniteugc.svc.halowaypoint.com/hi/projects/a9dc0785-2a99-4fec-ba6e-0216feaaf041";
        protected override List<string> CacheFileDirs => new List<string>()
        {
            Path.Combine(BasePath, "webcache")
        };
    }

}
