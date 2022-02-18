using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace InfiniteVariantTool.Core
{
    public static class Oodle
    {
        public const string DllName = "oo2core_9_win64.dll";

        [DllImport(DllName)]
        public static extern long OodleLZ_Decompress(
            byte[] compBuf,
            long compBufSize,
            byte[] rawBuf,
            long rawLen,
            bool fuzzSafe = true,
            bool checkCRC = false,
            OodleLZ_Verbosity verbosity = OodleLZ_Verbosity.None,
            IntPtr decBufBase = default,
            long decBufSize = 0,
            OodleDecompressCallback? fpCallback = null,
            object? callbackUserData = null,
            byte[]? decoderMemory = null,
            long decoderMemorySize = 0,
            OodleLZ_Decode_ThreadPhase threadPhase = OodleLZ_Decode_ThreadPhase.Unthreaded);

        [DllImport(DllName)]
        public static extern long OodleLZ_Compress(
            OodleLZ_Compressor compressor,
            byte[] rawBuf,
            long rawLen,
            byte[] compBuf,
            OodleLZ_CompressionLevel level,
            IntPtr pOptions = default,
            IntPtr dictionaryBase = default,
            IntPtr lrm = default,
            byte[]? scratchMem = null,
            long scratchSize = 0);

        [DllImport(DllName)]
        public static extern long OodleLZ_GetCompressedBufferSizeNeeded(OodleLZ_Compressor compressor, long rawLen);

        public static bool TryDecompress(byte[] compressedData, long decompressedSize, [MaybeNullWhen(false)] out byte[] decompressedData)
        {
            byte[] decompressBuffer = new byte[decompressedSize];
            if (0 != OodleLZ_Decompress(compressedData, compressedData.Length, decompressBuffer, decompressedSize))
            {
                decompressedData = decompressBuffer;
                return true;
            }
            decompressedData = null;
            return false;
        }

        public static byte[] Decompress(byte[] compressedData, long decompressedSize)
        {
            if (TryDecompress(compressedData, decompressedSize, out byte[]? decompressedData))
            {
                return decompressedData;
            }
            else
            {
                throw new OodleException("Failed to decompress data");
            }
        }

        public static byte[] Compress(byte[] data, OodleLZ_Compressor compressor, OodleLZ_CompressionLevel level)
        {
            long compressionBufferSize = OodleLZ_GetCompressedBufferSizeNeeded(compressor, data.Length);
            byte[] compressionBuffer = new byte[compressionBufferSize];
            long compressedSize = OodleLZ_Compress(compressor, data, data.Length, compressionBuffer, level);
            if (compressedSize != 0)
            {
                return compressionBuffer[..(int)compressedSize];
            }
            else
            {
                throw new OodleException("Failed to compress data");
            }
        }
    }

    public delegate OodleDecompressCallbackRet OodleDecompressCallback(object userdata, IntPtr rawBuf, long rawLen, IntPtr compBuf, long compBufferSize , long rawDone, long compUsed);

    public enum OodleLZ_Compressor
    {
        Invalid = -1,
        None = 3,
        Kraken = 8,
        Leviathan = 13,
        Mermaid = 9,
        Selkie = 11,
        Hydra = 12
    }

    public enum OodleLZ_Verbosity
    {
        None = 0,
        Minimal = 1,
        Some = 2,
        Lots = 3
    }

    public enum OodleDecompressCallbackRet
    {
        Continue = 0,
        Cancel = 1,
        Invalid = 2
    }

    public enum OodleLZ_Decode_ThreadPhase
    {
        ThreadPhase1 = 1,
        ThreadPhase2 = 2,
        ThreadPhaseAll = 3,
        Unthreaded = ThreadPhaseAll
    }

    public enum OodleLZ_CompressionLevel
    {
        None = 0,
        SuperFast = 1,
        VeryFast = 2,
        Fast = 3,
        Normal = 4,
        Optimal1 = 5,
        Optimal2 = 6,
        Optimal3 = 7,
        Optimal4 = 8,
        Optimal5 = 9,
        HyperFast1 = -1,
        HyperFast2 = -2,
        HyperFast3 = -3,
        HyperFast4 = -4,
        HyperFast = HyperFast1,
        Optimal = Optimal2,
        Max = Optimal5,
        Min = HyperFast4
    }

    public class OodleException : Exception
    {
        public OodleException() { }
        public OodleException(string message) : base(message) { }
        public OodleException(string message, Exception innerException) : base(message, innerException) { }
    }
}
