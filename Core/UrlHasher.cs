using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Newtonsoft.Json;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using InfiniteVariantTool.Core.Utils;

namespace InfiniteVariantTool.Core
{
    // endpoint file URLs
    // offline: https://settings-intone.test.svc.halowaypoint.com/settings/hipc/e2a0a7c6-6efe-42af-9283-c2ab73250c48
    // online:  https://settings.svc.halowaypoint.com/settings/hipc/e2a0a7c6-6efe-42af-9283-c2ab73250c48
    // lan:     https://settings.svc.halowaypoint.com/settings/hipcxolocalds/6858ba34-18a8-4030-84b8-1df01ff8ad34

    public enum EndpointType
    {
        Offline,
        Online,
        Lan
    }

    public class UrlHasher
    {
        private static Dictionary<string, EndpointMap> endpointFileCache = new();

        public static ulong? TryHashUrl(string url, EndpointType endpointType)
        {
            if (MyUri.TryParse(url, out MyUri? uri))
            {
                return HashUrl(uri, endpointType);
            }
            return null;
        }

        public static ulong HashUrl(string url, EndpointType endpointType)
        {
            return TryHashUrl(url, endpointType) ?? throw new Exception("Failed to hash URL");
        }

        private static ulong? HashUrl(MyUri uri, EndpointType endpointType)
        {
            // load endpoint file
            string endpointFilename = endpointType switch
            {
                EndpointType.Online => "endpoints-online.json",
                EndpointType.Offline => "endpoints-offline.json",
                EndpointType.Lan => "endpoints-lan.json",
                _ => throw new ArgumentException(),
            };

            EndpointMap endpointMap;
            if (endpointFileCache.ContainsKey(endpointFilename))
            {
                endpointMap = endpointFileCache[endpointFilename];
            }
            else
            {
                endpointMap = JsonConvert.DeserializeObject<EndpointMap>(Util.ReadResource(endpointFilename))
                    ?? throw new JsonException();
                endpointFileCache[endpointFilename] = endpointMap;
            }

            // hash url
            return endpointMap.Endpoints
                .Where(endpoint => MatchesAuthority(uri, endpoint.Value, endpointMap)
                    && MatchesPath(uri, endpoint.Value) && MatchesQueryString(uri, endpoint.Value))
                .Select(endpoint => Hash(GetDataToHash(uri, endpointMap.Authorities[endpoint.Value.AuthorityId], endpoint.Value, endpoint.Key)))
                .Distinct()
                .Cast<ulong?>()
                .SingleOrDefault() ?? Hash(GetDataToHash(uri));
        }

        // FNV-1a
        private static ulong Hash(byte[] data)
        {
            if (data.Length == 0)
            {
                return 0;
            }
            ulong offsetBasis = 0xcbf29ce484222325;
            ulong prime = 0x100000001b3;
            ulong hash = offsetBasis;
            foreach (byte b in data)
            {
                hash ^= b;
                hash *= prime;
            }
            return hash;
        }

        private static ulong Hash(List<byte[]> data)
        {
            ulong hash = 0;
            foreach (byte[] b in data)
            {
                hash ^= Hash(b);
            }
            return hash;
        }

        private static bool MatchesAuthority(MyUri uri, Endpoint endpoint, EndpointMap endpointMap)
        {
            if (endpoint.AuthorityId == "gamecms")
            {
                endpoint.AuthorityId = "gamecms-hacs";
            }
            if (endpointMap.Authorities.TryGetValue(endpoint.AuthorityId, out Authority? authority))
            {
                return authority.Hostname == uri.Host;
            }
            return false;
        }

        private static bool MatchesPath(MyUri uri, Endpoint endpoint)
        {
            if (endpoint.Path == "")
            {
                return true;
            }
            string expectedPath = "^" + Regex.Replace(endpoint.Path, "\\{.*?\\}", "[^/]*") + "$";
            return Regex.IsMatch(uri.Path, expectedPath);
        }

        private static bool MatchesQueryString(MyUri uri, Endpoint endpoint)
        {
            if (endpoint.QueryString == "")
            {
                return true;
            }
            string queryString = endpoint.QueryString.StartsWith("?") ? endpoint.QueryString[1..] : endpoint.QueryString;
            string expectedQueryString = "^" + Regex.Replace(queryString, "\\{.*?\\}", "[^&]*") + "$";
            return Regex.IsMatch(uri.Query, expectedQueryString);
        }

        private static List<byte[]> GetDataToHash(MyUri uri, int scheme = 2, int port = 443)
        {
            byte[] schemeBytes = new byte[sizeof(int)];
            byte[] portBytes = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(schemeBytes, scheme);
            BinaryPrimitives.WriteInt32LittleEndian(portBytes, port);
            return new List<byte[]>()
            {
                Encoding.UTF8.GetBytes(uri.Host),
                Encoding.UTF8.GetBytes(uri.Path),
                Encoding.UTF8.GetBytes(uri.Query),
                schemeBytes,
                portBytes
            };
        }

        private static List<byte[]> GetDataToHash(MyUri uri, Authority authority, Endpoint endpoint, string endpointName)
        {
            if (authority.AuthenticationMethods.Contains(0))
            {
                if (endpoint.AuthorityId == "iUgcFiles")
                {
                    endpointName = endpoint.AuthorityId;
                }
                return new List<byte[]>()
                {
                    Encoding.UTF8.GetBytes(endpointName),
                    Encoding.UTF8.GetBytes(uri.Path),
                    Encoding.UTF8.GetBytes(uri.Query)
                };
            }
            else
            {
                return GetDataToHash(uri, authority.Scheme, authority.Port ?? throw new ArgumentNullException());
            }
        }

        // built-in Uri doesn't quite work how I need it to
        public class MyUri
        {
            public string Scheme { get; set; }
            public string Host { get; set; }
            public string Path { get; set; }
            public string Query { get; set; }
            public string Fragment { get; set; }
            public MyUri() { throw new NotImplementedException(); }
            public MyUri(string scheme, string host, string path, string query, string fragment)
            {
                Scheme = scheme;
                Host = host;
                Path = path;
                Query = query;
                Fragment = fragment;
            }
            public static bool TryParse(string uri, [MaybeNullWhen(false)] out MyUri result)
            {
                // https://www.rfc-editor.org/rfc/rfc3986#appendix-B
                Match match = Regex.Match(uri, @"^(([^:/?#]+):)?(//([^/?#]*))?([^?#]*)(\?([^#]*))?(#(.*))?");
                if (match.Success)
                {
                    string scheme = match.Groups[2].Value;
                    string host = match.Groups[4].Value;
                    string path = match.Groups[5].Value;
                    string query = match.Groups[7].Value;
                    string fragment = match.Groups[9].Value;

                    if (scheme != "" && host != "")
                    {
                        result = new MyUri(scheme, host, path, query, fragment);
                        return true;
                    }
                }
                result = null;
                return false;
            }
        }

        public class Endpoint
        {
            public string AuthorityId = default!;
            public string Path = default!;
            public string QueryString = default!;
        }

        public class Authority
        {
            public string AuthorityId = default!;
            public int Scheme = default!;
            public string Hostname = default!;
            public int? Port = default!;
            public List<int> AuthenticationMethods = default!;
        }

        public class EndpointMap
        {
            public Dictionary<string, Authority> Authorities = default!;
            public Dictionary<string, Endpoint> Endpoints = default!;
        }
    }
}
