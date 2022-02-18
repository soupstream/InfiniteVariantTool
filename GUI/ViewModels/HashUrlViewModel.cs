using InfiniteVariantTool.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteVariantTool.GUI
{
    public class HashUrlViewModel : ViewModel
    {
        public HashUrlViewModel()
        {
            url = "";
            offlineHash = "";
            onlineHash = "";
            lanHash = "";
        }

        private string GetHash(string url, EndpointType endpointType)
        {
            if (url == "")
            {
                return "";
            }
            return UrlHasher.TryHashUrl(url, endpointType)?.ToString() ?? "N/A";
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

                    OfflineHash = GetHash(url, EndpointType.Offline);
                    OnlineHash = GetHash(url, EndpointType.Online);
                    LanHash = GetHash(url, EndpointType.Lan);
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
