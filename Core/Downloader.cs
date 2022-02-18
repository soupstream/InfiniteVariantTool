using InfiniteVariantTool.Core.Cache;
using InfiniteVariantTool.Core.Serialization;
using InfiniteVariantTool.Core.Settings;
using InfiniteVariantTool.Core.Utils;
using InfiniteVariantTool.Core.Variants;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace InfiniteVariantTool.Core
{
    public class VariantDownloader
    {
        public Action<string>? DebugPrintFunc { get; set; } = null;
        public bool PreserveDirStructure { get; set; } = false;
        private CookieContainer cookies;
        private HttpClientHandler handler;
        private HttpClient client;
        private const int maxBackoff = 32;
        public HttpStatusCode? StatusCode { get; private set; }
        //private const string BrowserUserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:95.0) Gecko/20100101 Firefox/95.0";
        private const string GameUserAgent = "SHIVA-2043073184/6.10021.16272.0 (release; PC)";
        public VariantDownloader()
        {
            cookies = new();
            handler = new()
            {
                CookieContainer = cookies,
                AllowAutoRedirect = false
            };
            client = new(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(GameUserAgent);
        }

        public void DebugPrint(string msg)
        {
            DebugPrintFunc?.Invoke(msg);
        }

        public async Task<string> GetSpartanToken(string email, string password)
        {
            // redirects to Microsoft login page
            var res = await client.GetAsync("https://www.halowaypoint.com/sign-in");
            res.EnsureStatusCode(HttpStatusCode.Redirect);
            res = await client.GetAsync(res.Headers.Location);
            res.EnsureSuccessStatusCode();
            var resText = await res.Content.ReadAsStringAsync();

            // get token needed for login
            var match = Regex.Match(resText, "name=\"PPFT\".*?value=\"(.*?)\"");
            string ppft = match.Groups[1].Value;

            // get authentication url
            match = Regex.Match(resText, "urlPost:'(.*?)'");
            string postUrl = match.Groups[1].Value;

            // post logic credentials and follow one redirect
            var postData = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                {"i13", "0"},
                {"login", email},
                {"loginfmt", email},
                {"type", "11"},
                {"LoginOptions", "3"},
                {"lrt", ""},
                {"lrtPartition", ""},
                {"hisRegion", ""},
                {"hisScaleUnit", ""},
                {"passwd", password},
                {"ps", "2"},
                {"psRNGCDefaultType", ""},
                {"psRNGCEntropy", ""},
                {"psRNGCSLK", ""},
                {"canary", ""},
                {"ctx", ""},
                {"hpgrequestid", ""},
                {"PPFT", ppft},
                {"PPSX", "Pas"},
                {"NewUser", "1"},
                {"FoundMSAs", ""},
                {"fspost", "0"},
                {"i21", "0"},
                {"CookieDisclosure", "0"},
                {"IsFidoSupported", "0"},
                {"isSignupPost", "0"},
                {"i19", "16950"}
            }!);
            res = await client.PostAsync(postUrl, postData);
            res.EnsureStatusCode(HttpStatusCode.Redirect);
            res = await client.GetAsync(res.Headers.Location);
            res.EnsureStatusCode(HttpStatusCode.Redirect);

            // get cookie
            var spartanTokenCookie = cookies.GetCookies(new Uri("https://www.halowaypoint.com/"))["343-spartan-token"]
                ?? throw new HttpRequestException("343-spartan-token cookie not found");
            string spartanToken = HttpUtility.UrlDecode(spartanTokenCookie.Value);
            client.DefaultRequestHeaders.Add("X-343-Authorization-Spartan", spartanToken);
            return spartanToken;
        }

        public async Task<byte[]?> DownloadBytes(string url, string? language, string? outputFilename)
        {
            if (CheckForFile(outputFilename, url))
            {
                return await File.ReadAllBytesAsync(outputFilename);
            }
            byte[]? output = await DownloadFile(url, language, ContentType.Bin);
            if (output != null && outputFilename != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputFilename)!);
                File.WriteAllBytes(outputFilename, output);
            }
            return output;
        }

        public async Task<byte[]?> DownloadBond(string url, string? language, string? outputFilename)
        {
            if (CheckForFile(outputFilename, url))
            {
                return await File.ReadAllBytesAsync(outputFilename);
            }
            var output = await DownloadFile(url, language, ContentType.Bond);
            if (output != null && outputFilename != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputFilename)!);
                File.WriteAllBytes(outputFilename, output);
            }
            return output;
        }

        public async Task<BondReadResult?> DownloadBondAsXml(string url, string? language, string? outputFilename)
        {
            if (CheckForFile(outputFilename, url))
            {
                return BondReadResult.Load(outputFilename);
            }
            byte[]? data = await DownloadBond(url, language, null);
            if (data == null) return null;
            var output = new BondReader(data).Read();
            if (outputFilename != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputFilename)!);
                output.Save(outputFilename);
            }
            return output;
        }

        public async Task<string?> DownloadJson(string url, string? language, string? outputFilename)
        {
            if (CheckForFile(outputFilename, url))
            {
                return File.ReadAllText(outputFilename);
            }
            byte[]? data = await DownloadFile(url, language, ContentType.Json);
            if (data == null) return null;
            string output = Encoding.Default.GetString(data);
            if (outputFilename != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputFilename)!);
                File.WriteAllText(outputFilename, output);
            }
            return output;
        }

        private bool CheckForFile([NotNullWhen(true)] string? outputFilename, string url)
        {
            if (outputFilename != null)
            {
                DebugPrint(url);
                if (File.Exists(outputFilename))
                {
                    DebugPrint("Found " + outputFilename);
                    return true;
                }
            }
            return false;
        }

        private int Backoff(int retries)
        {
            if (retries == 0)
            {
                return 0;
            }
            return (int)Math.Min(maxBackoff, Math.Pow(2, retries - 1));
        }

        public async Task<byte[]?> DownloadFile(string url, string? language, ContentType contentType)
        {
            string? contentTypeStr = contentType switch
            {
                ContentType.Png => "image/png",
                ContentType.Jpg => "image/jpeg",
                ContentType.Json => "application/json",
                ContentType.Bond => "application/x-bond-compact-binary",
                ContentType.Bin => "application/octet-stream",
                _ => null
            };
            return await DownloadFile(url, language, contentTypeStr);
        }

        public async Task<byte[]?> DownloadFile(string url, string? language, string? contentType)
        {
            int retries = 0;
            while (true)
            {
                await Task.Delay(Backoff(retries) * 1000);
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                if (contentType != null)
                {
                    req.Headers.Accept.ParseAdd(contentType);
                }
                if (language != null)
                {
                    req.Headers.AcceptLanguage.ParseAdd(language);
                }
                HttpResponseMessage res;
                try
                {
                    res = await client.SendAsync(req);
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    retries++;
                    DebugPrint("Got exception: " + ex.Message + ", increased backoff to " + Backoff(retries));
                    continue;
                }
                if (res.StatusCode == HttpStatusCode.OK)
                {
                    return await res.Content.ReadAsByteArrayAsync();
                }
                else if (res.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.InternalServerError or HttpStatusCode.GatewayTimeout)
                {
                    retries++;
                    DebugPrint("Got " + res.StatusCode + ", increased backoff to " + Backoff(retries));
                }
                else
                {
                    DebugPrint("Got status code " + res.StatusCode + ", skipping");
                    StatusCode = res.StatusCode;
                    return null;
                }
            }
        }

        public async Task DownloadLanRetailVariants(string language, string outputDirectory)
        {
            await DownloadLanVariants("https://discovery-infiniteugc.svc.halowaypoint.com", language, outputDirectory);
        }

        public async Task DownloadLanTestVariants(string language, string outputDirectory)
        {
            await DownloadLanVariants("https://discovery-infiniteugc-intone.test.svc.halowaypoint.com", language, outputDirectory);
        }

        private async Task DownloadLanVariants(string baseUrl, string language, string outputDirectory)
        {
            string? endpointsJson = await DownloadJson("https://settings.svc.halowaypoint.com/settings/hipcxolocalds/6858ba34-18a8-4030-84b8-1df01ff8ad34", language, Path.Combine(outputDirectory, "api.json"));
            if (endpointsJson == null) return;
            UrlHasher.EndpointMap endpointMap = JsonConvert.DeserializeObject<UrlHasher.EndpointMap>(endpointsJson) ?? throw new Exception();
            string manifestPath = endpointMap.Endpoints.First(endpoint => endpoint.Key == "HIUGC_Discovery_GetManifestForLocalDs").Value.Path;
            string manifestUrl = baseUrl + manifestPath;
            await DownloadVariants(manifestUrl, language, outputDirectory);
        }

        public async Task DownloadOfflineVariants(string language, string outputDirectory)
        {
            // we aren't authorized to download the offline manifest directly from 343 so get it from the game files
            string buildNumber = Util.GetBuildNumber();
            string manifestUrl = $"https://discovery-infiniteugc-intone.test.svc.halowaypoint.com/hi/manifests/builds/{buildNumber}/game";
            ulong hash = UrlHasher.HashUrl(manifestUrl, EndpointType.Offline);
            string filePath = Path.Combine(UserSettings.Instance.GameDirectory, Constants.OfflineCacheDirectory, "en-US", "other", hash.ToString());
            CacheFile cacheFile = new(filePath, ContentType.Bin, true);
            await DownloadVariants(cacheFile.Bytes, manifestUrl, language, outputDirectory);
        }

        public async Task DownloadOnlineVariants(string buildNumber, string language, string outputDirectory)
        {
            await DownloadVariants($"https://discovery-infiniteugc.svc.halowaypoint.com/hi/manifests/builds/{buildNumber}/game", language, outputDirectory);
        }

        public async Task DownloadVariants(string manifestUrl, string language, string outputDirectory)
        {
            // determine filename
            string filename;
            if (PreserveDirStructure)
            {
                filename = UrlToFilePath(outputDirectory, manifestUrl);
            }
            else
            {
                filename = Path.Combine(outputDirectory, "manifest.xml");
            }

            // download manifest (or retrieve existing file)
            if (PreserveDirStructure)
            {
                byte[] manifestBytes = await DownloadBond(manifestUrl, language, filename) ?? throw new Exception();
                await DownloadVariants(manifestBytes, manifestUrl, language, outputDirectory);
            }
            else
            {
                var manifestXml = await DownloadBondAsXml(manifestUrl, language, filename) ?? throw new Exception();
                await DownloadVariants(manifestXml.Doc, manifestUrl, language, outputDirectory);
            }
        }

        public async Task DownloadVariants(byte[] manifestBytes, string manifestUrl, string language, string outputDirectory)
        {
            if (PreserveDirStructure)
            {
                string filename = UrlToFilePath(outputDirectory, manifestUrl);
                if (!File.Exists(filename))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filename)!);
                    await File.WriteAllBytesAsync(filename, manifestBytes);
                }
            }

            BondReadResult manifestXml = new BondReader(manifestBytes).Read();
            await DownloadVariants(manifestXml.Doc, manifestUrl, language, outputDirectory);
        }


        public async Task DownloadVariants(XElement manifestXml, string manifestUrl, string language, string outputDirectory)
        {
            Uri manifestUri = new(manifestUrl);
            string baseDiscoveryUrl = "https://" + manifestUri.Host;

            if (!PreserveDirStructure)
            {
                string filename = Path.Combine(outputDirectory, "manifest.xml");
                if (!File.Exists(filename))
                {
                    Directory.CreateDirectory(outputDirectory);
                    manifestXml.SaveVersioned(filename);
                }
            }

            GameManifest manifest = new(manifestXml);
            FileNameDeduper deduper = new();
            foreach (var variant in manifest.MapLinks)
            {
                await DownloadVariant(variant, baseDiscoveryUrl, "maps", MapVariant.MetadataFilenameStatic, typeof(MapVariantMetadata), language, deduper, outputDirectory);
            }

            foreach (var variant in manifest.EngineGameVariantLinks)
            {
                await DownloadVariant(variant, baseDiscoveryUrl, "engineGameVariants", EngineGameVariant.MetadataFilenameStatic, typeof(EngineGameVariantMetadata), language, deduper, outputDirectory);
            }

            foreach (var variant in manifest.UgcGameVariantLinks)
            {
                await DownloadVariant(variant, baseDiscoveryUrl, "ugcGameVariants", UgcGameVariant.MetadataFilenameStatic, typeof(UgcGameVariantMetadata), language, deduper, outputDirectory);
            }
        }

        private async Task DownloadVariant(VariantMetadataBase baseMetadata, string baseUrl, string variantTypeString, string metadataFileName, Type variantType, string language, FileNameDeduper deduper, string outputDirectory)
        {
            // determine metadata filename
            string url = $"{baseUrl}/hi/{variantTypeString}/{baseMetadata.AssetId}/versions/{baseMetadata.VersionId}";

            // download metadata (or retrieve existing file)
            BondReadResult metadataXml;
            if (PreserveDirStructure)
            {
                string metadataFilename = UrlToFilePath(outputDirectory, url);
                byte[]? metadataBytes = await DownloadBond(url, language, metadataFilename);
                if (metadataBytes == null) return;
                metadataXml = new BondReader(metadataBytes).Read();
            }
            else
            {
                string variantDirectoryName = Util.MakeValidFilename(baseMetadata.PublicName);
                outputDirectory = deduper.Dedupe(Path.Combine(outputDirectory, variantTypeString, variantDirectoryName));
                string metadataFilename = Path.Combine(outputDirectory, metadataFileName);
                var tmp = await DownloadBondAsXml(url, language, metadataFilename);
                if (tmp == null) return;
                metadataXml = tmp;
            }

            // download files
            VariantMetadata metadata = (VariantMetadata?)Activator.CreateInstance(variantType, metadataXml.Doc) ?? throw new Exception();
            foreach (var relativeFilePath in metadata.Base.FileRelativePaths)
            {
                // skip unneeded files
                if (VariantManager.LanguageCodes.Any(code => relativeFilePath.EndsWith("." + code.ShortCode))
                    || relativeFilePath.EndsWith("_guid.txt")
                    || VariantManager.LanguageCodes.Any(code => relativeFilePath.EndsWith("_" + code.ShortCode + ".bin")
                        && code.Code != language))
                {
                    continue;
                }
                
                // determine filename
                string fileUrl = metadata.Base.Prefix + relativeFilePath;
                string filename;
                if (PreserveDirStructure)
                {
                    filename = UrlToFilePath(outputDirectory, fileUrl);
                }
                else
                {
                    filename = Path.Combine(outputDirectory, "files", relativeFilePath.Replace('/', Path.DirectorySeparatorChar));
                }
                await DownloadBytes(fileUrl, language, filename);
            }

            if (PreserveDirStructure && metadata is UgcGameVariantMetadata ugcMetadata && ugcMetadata.EngineGameVariantLink != null)
            {
                await DownloadVariant(ugcMetadata.EngineGameVariantLink, baseUrl, "engineGameVariants", EngineGameVariant.MetadataFilenameStatic, typeof(EngineGameVariantMetadata), language, deduper, outputDirectory);
            }
        }

        private string UrlToFilePath(string baseDirectory, string url)
        {
            Uri uri = new(url);
            return Path.Combine(baseDirectory, uri.Host, uri.LocalPath[1..].Replace('/', Path.DirectorySeparatorChar));
        }
    }

    public static class HttpClientExtensions
    {
        public static void EnsureStatusCode(this HttpResponseMessage res, HttpStatusCode statusCode)
        {
            if (res.StatusCode != statusCode)
            {
                throw new HttpRequestException("Expected " + statusCode + ", got " + res.StatusCode);
            }
        }
    }
}
