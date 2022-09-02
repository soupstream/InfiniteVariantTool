using System.IO;
using System.Reflection;

namespace InfiniteVariantTool.Core
{
    public static class Constants
    {
        public static readonly string GameExeName = "HaloInfinite.exe";
        public static readonly string OfflineCacheDirectory = Path.Combine("package", "pc");
        public static readonly string OnlineCacheDirectory = "disk_cache";
        public static readonly string LanCacheDirectory = "server_disk_cache";
        public static readonly string CmsDirectory = "__cms__";
        public static readonly byte[] pngSignature = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        public static readonly byte[] jpgSignature = new byte[] { 0xff, 0xd8, 0xff };
        public static readonly string AppName = "InfiniteVariantTool";
    }

    public enum Game
    {
        HaloInfinite,
        Halo5
    }
}
