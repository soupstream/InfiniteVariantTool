﻿using InfiniteVariantTool.Core;
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
        public static IEnumerable<(string, byte[])> GatherCacheFiles(string dir, bool verbose = false, bool skipNonBond = true, bool includeGamecms = true)
        {
            foreach (string f in Directory.GetFiles(dir))
            {
                bool skip = false;

                string fileExt = Path.GetExtension(f);
                if (fileExt is not "" or ".bin")
                {
                    skip = true;
                    if (verbose)
                    {
                        Console.WriteLine($"Skipping {fileExt} file: {f}");
                    }
                }
                else
                {
                    byte[] data = File.ReadAllBytes(f);
                    if (skipNonBond)
                    {
                        FileExtension ext = FileUtil.DetectFileType(data);
                        if (ext != FileExtension.Bin)
                        {
                            Console.WriteLine($"Skipping {ext.Value} file: {f}");
                            skip = true;
                        }
                    }

                    if (!skip)
                    {
                        yield return (f, data);
                    }
                }
            }
            foreach (string d in Directory.GetDirectories(dir))
            {
                if (!(d.EndsWith("gamecms") || d.EndsWith("gamecmscache")) || includeGamecms)
                {
                    foreach ((string, byte[]) f in GatherCacheFiles(d, verbose, skipNonBond, includeGamecms))
                    {
                        yield return f;
                    }
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

        public static async Task<string> GetBuildNumber()
        {
            return await FileUtil.GetBuildNumber(UserSettings.Instance.GameDirectory);
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
