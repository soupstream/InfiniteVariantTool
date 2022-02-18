using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InfiniteVariantTool.Core;
using System.IO;

namespace InfiniteVariantTool.Tests
{
    //[TestClass]
    public class DownloaderTests
    {
        [TestMethod]
        public async Task TestGetSpartanToken()
        {
            VariantDownloader downloader = new();
            Console.WriteLine(await downloader.GetSpartanToken(Secrets.Email, Secrets.Password));
        }

        // manifest currently missing from server
        // [TestMethod]
        public async Task TestDownloadLanRetailVariants()
        {
            string outputDirectory = Path.Combine(GetType().Name, "lan-retail");
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
            Directory.CreateDirectory(outputDirectory);
            VariantDownloader downloader = new()
            {
                DebugPrintFunc = DebugPrint
            };
            await downloader.GetSpartanToken(Secrets.Email, Secrets.Password);
            await downloader.DownloadLanRetailVariants("en-US", outputDirectory);
        }

        [TestMethod]
        public async Task TestDownloadLanTestVariants()
        {
            string outputDirectory = Path.Combine(GetType().Name, "lan-test");
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
            Directory.CreateDirectory(outputDirectory);
            VariantDownloader downloader = new()
            {
                DebugPrintFunc = DebugPrint
            };
            await downloader.GetSpartanToken(Secrets.Email, Secrets.Password);
            await downloader.DownloadLanTestVariants("en-US", outputDirectory);
        }

        [TestMethod]
        public async Task TestDownloadOnlineVariants()
        {
            string outputDirectory = Path.Combine(GetType().Name, "online");
            if (Directory.Exists(outputDirectory))
            {
                //Directory.Delete(outputDirectory, true);
            }
            Directory.CreateDirectory(outputDirectory);
            string buildNumber = TestUtil.GetBuildNumber();
            VariantDownloader downloader = new()
            {
                DebugPrintFunc = DebugPrint,
            };
            await downloader.GetSpartanToken(Secrets.Email, Secrets.Password);
            await downloader.DownloadOnlineVariants(buildNumber, "en-US", outputDirectory);
        }

        [TestMethod]
        public async Task TestDownloadOfflineVariants()
        {
            string outputDirectory = Path.Combine(GetType().Name, "offline");
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }
            Directory.CreateDirectory(outputDirectory);
            string buildNumber = TestUtil.GetBuildNumber();
            VariantDownloader downloader = new()
            {
                DebugPrintFunc = DebugPrint
            };
            //await downloader.GetSpartanToken(Secrets.Email, Secrets.Password);
            await downloader.DownloadOfflineVariants("en-US", outputDirectory);
        }

        // because visual studio was being difficult
        private void DebugPrint(string msg)
        {
            File.AppendAllText(Path.Combine(GetType().Name, "testlog.txt"), msg + Environment.NewLine);
            Console.WriteLine(msg);
        }
    }
}
