using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InfiniteVariantTool.Core.BondSchema;
using InfiniteVariantTool.Core.Variants;
using static InfiniteVariantTool.Core.BondSchema.ApiManifest;

namespace InfiniteVariantTool.Core
{
    public class GameApi
    {
        Dictionary<string, ApiEndpoint> EndpointMap { get; }
        public GameApi(ApiManifest apiManifest)
        {
            EndpointMap = new();
            Authority? openDiscoveryAuthority = null;

            // missing from offline manifest
            if (apiManifest.Authorities.TryGetValue("settings", out var settingsAuthority) && (settingsAuthority?.Hostname?.Contains("intone") ?? false))
            {
                foreach (string endpointName in new string[] { "HIUGC_Discovery_GetTagsInfo", "HIUGC_Discovery_GetCustomGameManifest" })
                {
                    if (apiManifest.Endpoints.TryGetValue(endpointName, out var flightEndpoint) && flightEndpoint.QueryString == "")
                    {
                        flightEndpoint.QueryString = "?flight={flightId}";
                    }
                }
            }

            foreach ((string endpointId, Endpoint endpoint) in apiManifest.Endpoints)
            {
                // handle special cases
                if (endpoint.AuthorityId is "iUgcFiles" or "iUgcSessionFiles")
                {
                    endpoint.EndpointId = endpoint.AuthorityId;
                }
                else if (endpoint.AuthorityId == "gamecms")
                {
                    endpoint.AuthorityId = "gamecms-hacs";
                }
                else
                {
                    endpoint.EndpointId = endpointId;
                }
                if (endpoint.Path == "")
                {
                    endpoint.Path = "{path}";
                }
                if (endpoint.AuthorityId == "HIUGC_Discovery_Authority" && VariantType.VariantTypes.Any(type => type.EndpointId == endpointId))
                {
                    // offline manifest seems to be missing HIUGC_Discovery_Authority_Open authority so variants are hashed wrong
                    if (openDiscoveryAuthority == null)
                    {
                        // create authority and add it to api manifest
                        openDiscoveryAuthority = new(apiManifest.Authorities["HIUGC_Discovery_Authority"]);
                        openDiscoveryAuthority.AuthorityId = "HIUGC_Discovery_Authority_Open";
                        openDiscoveryAuthority.AuthenticationMethods.Add(0);
                        apiManifest.Authorities["HIUGC_Discovery_Authority_Open"] = openDiscoveryAuthority;
                    }
                    endpoint.AuthorityId = "HIUGC_Discovery_Authority_Open";
                }

                Authority authority = apiManifest.Authorities[endpoint.AuthorityId];
                RetryPolicy retryPolicy = apiManifest.RetryPolicies[endpoint.RetryPolicyId];
                ApiEndpoint apiEndpoint = new(endpoint, authority, retryPolicy);
                EndpointMap[endpointId] = apiEndpoint;
                if (endpointId != endpoint.EndpointId)
                {
                    EndpointMap[endpoint.EndpointId] = apiEndpoint;
                }
            }
        }

        public GameApi()
        {
            EndpointMap = new();
        }

        public ApiCall Call(string endpointId, Dictionary<string, string> parameters)
        {
            return new ApiCall(EndpointMap[endpointId], parameters);
        }

        public ApiCall? CallUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                return null;
            }
            string path = Uri.UnescapeDataString(uri.AbsolutePath);
            string query = Uri.UnescapeDataString(uri.Query);
            foreach (var endpoint in EndpointMap.Values)
            {
                // check authority matches
                if (endpoint.Authority.Hostname == uri.Host)
                {
                    // check path matches
                    var parameters = ExtractParameters(path, endpoint.Endpoint.Path);
                    if (parameters != null)
                    {
                        // query string doesn't need to match but if it does, extract the parameters
                        if (endpoint.Endpoint.QueryString != null)
                        {
                            var queryParameters = ExtractParameters(query, endpoint.Endpoint.QueryString);
                            if (queryParameters != null)
                            {
                                foreach ((string key, string value) in queryParameters)
                                {
                                    parameters[key] = value;
                                }
                            }
                        }

                        return new ApiCall(endpoint, parameters);
                    }
                }
            }

            return new ApiCall(
                new ApiEndpoint(
                    new Endpoint()
                    {
                        Path = path,
                        QueryString = query
                    },
                    new Authority()
                    {
                        Hostname = uri.Host,
                        Scheme = 2,
                        Port = 443,
                    },
                    new RetryPolicy()
                    {
                        TimeoutMs = 10000,
                        RetryOptions = new()
                        {
                            MaxRetryCount = 1,
                            RetryDelayMs = 50,
                            RetryGrowth = 1,
                            RetryJitterMs = 10,
                            RetryIfNotFound = false
                        }
                    }),
                new());
        }

        private Dictionary<string, string>? ExtractParameters(string input, string template)
        {
            string pattern = "^" + Regex.Replace(Regex.Escape(template), @"\\{(.*?)}", @"(?<$1>.*)") + "$";
            var matches = Regex.Matches(input, pattern);
            if (matches.Any())
            {
                return matches.SelectMany(match => match.Groups.Values.Skip(1))
                    .Where(group => ("{" + group.Name + "}") != group.Value)
                    .ToDictionary(group => group.Name, group => group.Value);
            }
            else
            {
                return null;
            }
        }
    }

    public class ApiEndpoint
    {
        public Endpoint Endpoint { get; }
        public Authority Authority { get; }
        public RetryPolicy RetryPolicy { get; }
        public ApiEndpoint(Endpoint endpoint, Authority authority, RetryPolicy retryPolicy)
        {
            Endpoint = endpoint;
            Authority = authority;
            RetryPolicy = retryPolicy;
        }

        public ApiCall Call(Dictionary<string, string> parameters)
        {
            return new ApiCall(this, parameters);
        }
    }

    public class ApiCall
    {
        public ApiEndpoint ApiEndpoint { get; }
        public Dictionary<string, string> Parameters { get; }
        public ApiCall(ApiEndpoint endpoint, Dictionary<string, string> parameters)
        {
            ApiEndpoint = endpoint;
            Parameters = parameters;
        }

        public ulong Hash
        {
            get
            {
                string path = ApplyPath();
                string query = ApplyQuery().TrimStart('?');
                return UrlHasher.HashUrl(path, query, ApiEndpoint.Endpoint, ApiEndpoint.Authority);
            }
        }

        public string Url
        {
            get
            {
                string host = ApiEndpoint.Authority.Hostname!;
                string path = ApplyPath();
                string query = ApplyQuery();
                return $"https://{host}{path}{query}";
            }
        }

        private string ApplyPath()
        {
            return ApplyParameters(ApiEndpoint.Endpoint.Path, Parameters);
        }

        private string ApplyQuery()
        {
            string query = ApiEndpoint.Endpoint.QueryString ?? "";
            if (query == "")
            {
                return "";
            }

            // not sure how the game decides what to do with query parameters but this seems to work
            if (ApiEndpoint.Endpoint.ClearanceAware && !Parameters.ContainsKey("clearanceId") && !query.Contains("{flightId}"))
            {
                return "";
            }

            return ApplyParameters(query, Parameters);
        }

        private static string ApplyParameters(string str, Dictionary<string, string> parameters)
        {
            foreach ((string k, string v) in parameters)
            {
                str = str.Replace("{" + k + "}", v);
            }
            return str;
        }
    }
}
