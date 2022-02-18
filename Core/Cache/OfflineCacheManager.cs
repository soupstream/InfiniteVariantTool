using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace InfiniteVariantTool.Core.Cache
{
    public class OfflineCacheManager : CacheManagerBase
    {
        public OfflineCacheManager(string basePath, string language)
            : base(basePath)
        {
            Language = language;
        }

        public override bool LoadCacheMap()
        {
            if (base.LoadCacheMap())
            {
                // find build number
                foreach (var entry in CacheMap!.Map)
                {
                    string url = entry.Value.Metadata.Url ?? "";
                    Match match = Regex.Match(url, "^https://discovery-infiniteugc-intone.test.svc.halowaypoint.com/hi/manifests/builds/([^/]*)/game$");
                    if (match.Success)
                    {
                        BuildNumber = match.Groups[1].Value;
                        break;
                    }
                }
                return true;
            }
            return false;
        }

        protected override string GetCacheFilePath(string url)
        {
            ulong hash = UrlHasher.HashUrl(url, EndpointType_);
            string localizedFilePath = Path.Combine(CacheFileDirs[0], hash.ToString());
            string commonFilePath = Path.Combine(CacheFileDirs[1], hash.ToString());
            if (File.Exists(localizedFilePath))
            {
                return localizedFilePath;
            }
            else if (File.Exists(commonFilePath))
            {
                return commonFilePath;
            }
            else if (url.StartsWith("https://blobs-"))
            {
                return commonFilePath;
            }
            else
            {
                return localizedFilePath;
            }
        }

        protected override CacheFileMetadata CreateCacheFileMetadata(string url, int? contentLength = null)
        {
            CacheFileMetadata metadata = new();
            metadata.Guid = new();
            if (url.StartsWith("https://blobs-"))
            {
                metadata.Headers = new();
                if (contentLength != null)
                {
                    metadata.Headers["Content-Length"] = contentLength.Value.ToString();
                }
                metadata.Headers["Content-Type"] = "application/octet-stream";
            }
            else
            {
                metadata.Headers = new()
                {
                    { "Content-Type", "application/x-bond-compact-binary" }
                };
            }
            metadata.Url = url;
            return metadata;
        }

        protected override string CacheMapPath => Path.Combine(BasePath, Language!, "other", "CacheMap.wcache");
        protected override string GameManifestPath
        {
            get
            {
                ulong hash = UrlHasher.HashUrl(GameManifestUrl, EndpointType.Offline);
                return Path.Combine(BasePath, Language!, "other", hash.ToString());
            }
        }
        protected override string CustomsManifestPath
        {
            get
            {
                ulong hash = UrlHasher.HashUrl(CustomsManifestUrl, EndpointType.Offline);
                return Path.Combine(BasePath, Language!, "other", hash.ToString());
            }
        }
        protected override string GameManifestUrl
        {
            get
            {
                if (BuildNumber == null)
                {
                    throw new InvalidOperationException();
                }
                return $"https://discovery-infiniteugc-intone.test.svc.halowaypoint.com/hi/manifests/builds/{BuildNumber}/game";
            }
        }
        protected override string CustomsManifestUrl => "https://discovery-infiniteugc-intone.test.svc.halowaypoint.com/hi/projects/a9dc0785-2a99-4fec-ba6e-0216feaaf041";
        public override string EndpointsFileUrl => "https://settings-intone.test.svc.halowaypoint.com/settings/hipc/e2a0a7c6-6efe-42af-9283-c2ab73250c48";
        protected override EndpointType EndpointType_ => EndpointType.Offline;
        protected override List<string> CacheFileDirs => new List<string>()
        {
            Path.Combine(BasePath, Language!, "other"),
            Path.Combine(BasePath, "common", "other")
        };
    }

}
