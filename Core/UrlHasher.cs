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
using InfiniteVariantTool.Core.BondSchema;

namespace InfiniteVariantTool.Core
{
    public class UrlHasher
    {
        public static ulong HashUrl(string url)
        {
            return Hash(GetDataToHash(new Uri(url)));
        }

        public static ulong HashUrl(string path, string query, ApiManifest.Endpoint endpoint, ApiManifest.Authority authority, bool forceGeneric = false)
        {
            return Hash(GetDataToHash(path, query, endpoint, authority, forceGeneric));
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

        private static List<byte[]> GetDataToHash(Uri uri, ApiManifest.Endpoint endpoint, ApiManifest.Authority authority)
        {
            return GetDataToHash(uri.AbsolutePath, uri.Query, endpoint, authority, false);
        }
        private static List<byte[]> GetDataToHash(string path, string query, ApiManifest.Endpoint endpoint, ApiManifest.Authority authority, bool forceGeneric)
        {
            if (!forceGeneric && authority.AuthenticationMethods.Contains(0))
            {
                return new List<byte[]>()
                {
                    Encoding.UTF8.GetBytes(endpoint.EndpointId),
                    Encoding.UTF8.GetBytes(path),
                    Encoding.UTF8.GetBytes(query)
                };
            }
            else
            {
                return GetDataToHash(authority.Hostname!, path, query, authority.Scheme, authority.Port ?? throw new InvalidOperationException("expected port number, got null"));
            }
        }

        private static List<byte[]> GetDataToHash(Uri uri, int scheme = 2, int port = 443)
        {
            return GetDataToHash(uri.Host, uri.AbsolutePath, uri.Query, scheme, port);
        }

        private static List<byte[]> GetDataToHash(string host, string path, string query, int scheme = 2, int port = 443)
        {
            byte[] schemeBytes = new byte[sizeof(int)];
            byte[] portBytes = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(schemeBytes, scheme);
            BinaryPrimitives.WriteInt32LittleEndian(portBytes, port);
            return new List<byte[]>()
            {
                Encoding.UTF8.GetBytes(host),
                Encoding.UTF8.GetBytes(path),
                Encoding.UTF8.GetBytes(query),
                schemeBytes,
                portBytes
            };
        }
    }
}
