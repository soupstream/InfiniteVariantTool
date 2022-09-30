using InfiniteVariantTool.Core;
using InfiniteVariantTool.Core.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteVariantTool.GUI
{
    public class HashUrlViewModel : ViewModel
    {
        VariantManager variantManager;
        public HashUrlViewModel(VariantManager variantManager)
        {
            this.variantManager = variantManager;
            url = "";
            offlineHash = "";
            onlineHash = "";
            lanHash = "";
        }

        private string GetHash(CacheManager cache, string url)
        {
            if (url == "")
            {
                return "";
            }
            return cache.Api.CallUrl(url)?.Hash.ToString() ?? "N/A";
        }

        private string url;
        public string Url
        {
            get => url;
            set
            {
                if (value != url)
                {
                    url = value;
                    OnPropertyChange(nameof(Url));

                    OfflineHash = GetHash(variantManager.OfflineCache, url);
                    OnlineHash = GetHash(variantManager.OnlineCache, url);
                    LanHash = GetHash(variantManager.LanCache, url);
                }
            }
        }

        private string offlineHash;
        public string OfflineHash
        {
            get => offlineHash;
            set
            {
                if (value != offlineHash)
                {
                    offlineHash = value;
                    OnPropertyChange(nameof(OfflineHash));
                }
            }
        }

        private string onlineHash;
        public string OnlineHash
        {
            get => onlineHash;
            set
            {
                if (value != onlineHash)
                {
                    onlineHash = value;
                    OnPropertyChange(nameof(OnlineHash));
                }
            }
        }

        private string lanHash;
        public string LanHash
        {
            get => lanHash;
            set
            {
                if (value != lanHash)
                {
                    lanHash = value;
                    OnPropertyChange(nameof(LanHash));
                }
            }
        }
    }
}
