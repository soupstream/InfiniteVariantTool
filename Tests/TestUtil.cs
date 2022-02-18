using InfiniteVariantTool.Core;
using InfiniteVariantTool.Core.Cache;
using InfiniteVariantTool.Core.Settings;
using InfiniteVariantTool.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InfiniteVariantTool.Tests
{
    class TestUtil
    {
        public static IEnumerable<(string, byte[])> GatherCacheFiles(string dir, bool verbose = false, bool skipNonBond = true)
        {
            foreach (string f in Directory.GetFiles(dir))
            {
                byte[] data = File.ReadAllBytes(f);
                bool skip = false;

                if (f.EndsWith(".xml"))
                {
                    skip = true;
                    if (verbose)
                    {
                        Console.WriteLine("Skipping XML file: " + f);
                    }
                }

                if (Util.ArrayStartsWith(data, Constants.pngSignature))
                {
                    if (skipNonBond)
                    {
                        skip = true;
                    }
                    if (verbose)
                    {
                        Console.WriteLine((skipNonBond ? "Skipping " : "") + "PNG file: " + f);
                    }
                }
                else if (Util.ArrayStartsWith(data, Constants.jpgSignature))
                {
                    if (skipNonBond)
                    {
                        skip = true;
                    }
                    if (verbose)
                    {
                        Console.WriteLine((skipNonBond ? "Skipping " : "") + "JPG file: " + f);
                    }
                }
                else if (data.First() == '{' && data.Last() == '}')
                {
                    if (skipNonBond)
                    {
                        skip = true;
                    }
                    if (verbose)
                    {
                        Console.WriteLine((skipNonBond ? "Skipping " : "") + "JSON file: " + f);
                    }
                }

                if (!skip)
                {
                    yield return (f, data);
                }
            }
            foreach (string d in Directory.GetDirectories(dir))
            {
                foreach ((string, byte[]) f in GatherCacheFiles(d, verbose, skipNonBond))
                {
                    yield return f;
                }
            }
        }

        public static IEnumerable<string> GatherCacheMapFiles(string dir)
        {
            foreach (string f in Directory.GetFiles(dir))
            {
                if (f.EndsWith("CacheMap.wcache"))
                {
                    yield return f;
                }
            }
            foreach (string d in Directory.GetDirectories(dir))
            {
                foreach (string f in GatherCacheMapFiles(d))
                {
                    yield return f;
                }
            }
        }

        public static string GetBuildNumber()
        {
            string filename = Path.Combine(UserSettings.Instance.GameDirectory, Constants.OfflineCacheDirectory, "en-US", "other", "CacheMap.wcache");
            CacheMap cm = new(filename);
            foreach (var entry in cm.Map)
            {
                Match match = Regex.Match(entry.Value.Metadata.Url!, "^https://discovery-infiniteugc-intone.test.svc.halowaypoint.com/hi/manifests/builds/([^/]*)/game$");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }
            throw new Exception();
        }
    }

    internal class ConsoleRedirector : IDisposable
    {
        private StringWriter _consoleOutput = new StringWriter();
        private TextWriter _originalConsoleOutput;
        public ConsoleRedirector()
        {
            this._originalConsoleOutput = Console.Out;
            Console.SetOut(_consoleOutput);
        }
        public void Dispose()
        {
            Console.SetOut(_originalConsoleOutput);
            Console.Write(this.ToString());
            this._consoleOutput.Dispose();
        }
        public override string ToString()
        {
            return this._consoleOutput.ToString();
        }
    }
}
