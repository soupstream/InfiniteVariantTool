using System;

namespace InfiniteVariantTool.Core.Cache
{
    public class OnlineCacheManager : OnlineLanCacheManagerBase
    {
        public OnlineCacheManager(string basePath)
            : base(basePath)
        {
        }

        protected override string GameManifestUrl
        {
            get
            {
                if (BuildNumber == null)
                {
                    throw new InvalidOperationException();
                }
                return $"https://discovery-infiniteugc.svc.halowaypoint.com/hi/manifests/builds/{BuildNumber}/game";
            }
        }
        public override string EndpointsFileUrl => "https://settings.svc.halowaypoint.com/settings/hipc/e2a0a7c6-6efe-42af-9283-c2ab73250c48";
        protected override EndpointType EndpointType_ => EndpointType.Online;
    }
}
