using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InfiniteVariantTool.Core
{
    public class Updater
    {
        private const string gitHubUser = "soupstream";
        private const string gitHubRepo = "InfiniteVariantTool";
        public static string GitHubRepoUrl => $"https://github.com/{gitHubUser}/{gitHubRepo}";
        public static string GitHubReleaseUrl => GitHubRepoUrl + "/releases/latest";
        private GitHubClient client;

        public Updater()
        {
            client = new GitHubClient(new ProductHeaderValue("infinite-variant-tool"));
        }

        public static Version CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version!;

        public async Task<Version?> GetLatestVersion()
        {
            var releases = await client.Repository.Release.GetAll(gitHubUser, gitHubRepo);
            if (releases.Any())
            {
                return ParseVersion(releases[0].TagName);
            }
            return null;
        }

        // looser version parsing rules
        private Version? ParseVersion(string versionStr)
        {
            // look for version number with regex and pick largest match
            string? match = Regex.Matches(versionStr, "[0-9.]+")
                .Aggregate((string?)null, (max, cur) => (max?.Length ?? 0) > cur.Value.Length ? max : cur.Value);
            if (match != null)
            {
                return new Version(match).ZeroMissingFields();
            }
            return null;
        }
    }

    public static class VersionExtensions
    {
        public static Version ZeroMissingFields(this Version version)
        {
            return new Version(
                Math.Max(0, version.Major),
                Math.Max(0, version.Minor),
                Math.Max(0, version.Build),
                Math.Max(0, version.Revision));
        }
    }
}
