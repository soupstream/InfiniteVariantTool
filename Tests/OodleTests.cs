using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using InfiniteVariantTool.Core;
using System.Xml.Linq;

namespace InfiniteVariantTool.Tests
{
    [TestClass]
    public class OodleTests
    {

        [TestMethod]
        public void TestOodle()
        {
            byte[] data = GenerateCompressibleData(1024 * 1024 * 100); // 100 MB
            byte[] compressed = Oodle.Compress(data, OodleLZ_Compressor.Kraken, OodleLZ_CompressionLevel.Fast);
            Console.WriteLine(string.Format("Compressed {0} bytes to {1} bytes", data.Length, compressed.Length));
            Assert.IsTrue(compressed.Length > 0 && compressed.Length < data.Length);
            byte[] decompressed = Oodle.Decompress(compressed, data.Length);
            Assert.IsTrue(decompressed.SequenceEqual(data));
        }

        private static byte[] GenerateCompressibleData(int size)
        {
            Random rand = new();
            byte[] data = new byte[size];
            for (int i = 0; i < size; i++)
            {
                // pick ASCII characters to make data compressible
                data[i] = (byte)rand.Next(32, 128);
            }
            return data;
        }
    }
}
