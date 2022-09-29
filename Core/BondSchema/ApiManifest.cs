using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InfiniteVariantTool.Core.BondSchema
{
    [Bond.Schema]
    public class ApiManifest
    {
        #region Schema

        [Bond.Id(0)]
        public Dictionary<string, Authority> Authorities { get; set; }
        [Bond.Id(1)]
        public Dictionary<string, RetryPolicy> RetryPolicies { get; set; }
        [Bond.Id(2)]
        public Dictionary<string, string> Settings { get; set; }
        [Bond.Id(3)]
        public Dictionary<string, Endpoint> Endpoints { get; set; }

        public ApiManifest()
        {
            Authorities = new();
            RetryPolicies = new();
            Settings = new();
            Endpoints = new();
        }

        [Bond.Schema]
        public class Authority
        {
            [Bond.Id(0)]
            public string AuthorityId { get; set; }
            [Bond.Id(1)]
            public int Scheme { get; set; }
            [Bond.Id(2), Bond.Type(typeof(Bond.Tag.nullable<string>))]
            public string? Hostname { get; set; }
            [Bond.Id(3), Bond.Type(typeof(Bond.Tag.nullable<ushort>))]
            public ushort? Port { get; set; }
            [Bond.Id(4)]
            public HashSet<int> AuthenticationMethods { get; set; }

            public Authority()
            {
                AuthorityId = "";
                AuthenticationMethods = new();
            }

            // copy constructor
            public Authority(Authority other)
            {
                AuthorityId = other.AuthorityId;
                Scheme = other.Scheme;
                Hostname = other.Hostname;
                Port = other.Port;
                AuthenticationMethods = new(other.AuthenticationMethods);
            }
        }

        [Bond.Schema]
        public class RetryPolicy
        {
            [Bond.Id(0)]
            public string RetryPolicyId { get; set; }
            [Bond.Id(1)]
            public uint TimeoutMs { get; set; }
            [Bond.Id(2), Bond.Type(typeof(Bond.Tag.nullable<RetryOptions>))]
            public RetryOptions? RetryOptions { get; set; }

            public RetryPolicy()
            {
                RetryPolicyId = "";
            }
        }

        [Bond.Schema]
        public class RetryOptions
        {
            [Bond.Id(0)]
            public byte MaxRetryCount { get; set; }
            [Bond.Id(1)]
            public uint RetryDelayMs { get; set; }
            [Bond.Id(2)]
            public float RetryGrowth { get; set; }
            [Bond.Id(3)]
            public uint RetryJitterMs { get; set; }
            [Bond.Id(4)]
            public bool RetryIfNotFound { get; set; }

            public RetryOptions()
            {
                MaxRetryCount = 3;
                RetryDelayMs = 2000;
                RetryGrowth = 2;
                RetryJitterMs = 150;
            }
        }

        [Bond.Schema]
        public class Endpoint
        {
            [Bond.Id(0)]
            public string AuthorityId { get; set; }
            [Bond.Id(1)]
            public string Path { get; set; }
            [Bond.Id(2), Bond.Type(typeof(Bond.Tag.nullable<string>))]
            public string? QueryString { get; set; }
            [Bond.Id(3)]
            public string RetryPolicyId { get; set; }
            [Bond.Id(4)]
            public string TopicName { get; set; }
            [Bond.Id(5)]
            public int AcknowledgementTypeId { get; set; }
            [Bond.Id(6)]
            public bool AuthenticationLifetimeExtensionSupported { get; set; }
            [Bond.Id(7)]
            public bool ClearanceAware { get; set; }

            public Endpoint()
            {
                AuthorityId = "";
                Path = "";
                RetryPolicyId = "";
                TopicName = "";
                EndpointId = "";
            }

            // copy constructor
            public Endpoint(Endpoint other)
            {
                AuthorityId = other.AuthorityId;
                Path = other.Path;
                QueryString = other.QueryString;
                RetryPolicyId = other.RetryPolicyId;
                TopicName = other.TopicName;
                AcknowledgementTypeId = other.AcknowledgementTypeId;
                AuthenticationLifetimeExtensionSupported = other.AuthenticationLifetimeExtensionSupported;
                ClearanceAware = other.ClearanceAware;
                EndpointId = other.EndpointId;
            }

            #region MyExtensions

            [JsonIgnore]
            public string EndpointId { get; set; }

            #endregion
        }
        #endregion
    }
}
