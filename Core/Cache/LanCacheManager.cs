using InfiniteVariantTool.Core.Utils;
using System;
using System.Linq;
using System.Xml.Linq;

namespace InfiniteVariantTool.Core.Cache
{
    public class LanCacheManager : OnlineLanCacheManagerBase
    {
        public Guid? AssetId { get; set; }
        public Guid? VersionId { get; set; }
        public LanCacheManager(string basePath)
            : base(basePath)
        {

        }

        protected override string GameManifestUrl
        {
            get
            {
                if (AssetId == null || VersionId == null)
                {
                    throw new InvalidOperationException();
                }

                if (EndpointsFile != null)
                {
                    // find actual path in the endpoints file if available
                    var path = EndpointsFile.Doc.Descendants("string")
                        .FirstOrDefault(el => el.GetText() == "HIUGC_Discovery_GetManifestForLocalDs")?
                        .ElementsAfterSelf("struct")
                        .Descendants("string")
                        .FirstOrDefault(el => el.GetText().StartsWith("/hi/manifests/"))?
                        .GetText();
                    if (path != null)
                    {
                        return "https://discovery-infiniteugc.svc.halowaypoint.com" + path;
                    }
                }

                return $"https://discovery-infiniteugc.svc.halowaypoint.com/hi/manifests/{AssetId}/versions/{VersionId}";
            }
        }

        public override string EndpointsFileUrl => "https://settings.svc.halowaypoint.com/settings/hipcxolocalds/6858ba34-18a8-4030-84b8-1df01ff8ad34";
        protected override EndpointType EndpointType_ => EndpointType.Lan;
    }
}
