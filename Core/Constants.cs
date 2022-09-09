using System.Collections.Generic;
using System.IO;

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

    public class MimeType
    {
        public readonly string Value;
        private MimeType(string value)
        {
            Value = value;
        }

        public static readonly MimeType OctetStream = new("application/octet-stream");
        public static readonly MimeType Bond = new("application/x-bond-compact-binary");
        public static readonly MimeType Json = new("application/json");
    }

    public class Language
    {
        public readonly string ShortCode;
        public readonly string Code;
        public readonly string Name;
        private Language(string shortCode, string code, string name)
        {
            ShortCode = shortCode;
            Code = code;
            Name = name;
        }

        private Language(string shortCode)
        {
            ShortCode = shortCode;
            Code = "";
            Name = "";
        }

        public static Language? TryFromCode(string code)
        {
            if (code == "")
            {
                return null;
            }
            return Languages.Find(lang => lang.Code == code);
        }

        public static Language FromCode(string code)
        {
            return TryFromCode(code) ?? throw new KeyNotFoundException();
        }

        public static Language FromShortCode(string shortCode)
        {
            return Languages.Find(lang => lang.ShortCode == shortCode) ?? throw new KeyNotFoundException();
        }

        public static readonly Language En = new("en", "en-US", "English");
        public static readonly Language Jpn = new("jpn", "ja-JP", "Japanese");
        public static readonly Language De = new("de", "de-DE", "German");
        public static readonly Language Fr = new("fr", "fr-FR", "French");
        public static readonly Language Sp = new("sp", "es-ES", "Spanish");
        public static readonly Language Mx = new("mx", "es-MX", "Spanish (Mexico)");
        public static readonly Language It = new("it", "it-IT", "Italian");
        public static readonly Language Kor = new("kor", "ko-KR", "Korean");
        public static readonly Language Cht = new("cht", "zh-TW", "Chinese (Traditional)");
        public static readonly Language Chs = new("chs", "zh-CN", "Chinese (Simplified)");
        public static readonly Language Pl = new("pl", "pl-PL", "Polish");
        public static readonly Language Ru = new("ru", "ru-RU", "Russian");
        public static readonly Language Nl = new("nl", "nl-NL", "Dutch");
        public static readonly Language Br = new("br", "pt-BR", "Portuguese (Brazil)");
        public static readonly Language Dk = new("dk");
        public static readonly Language Fi = new("fi");
        public static readonly Language No = new("no");
        public static readonly Language Pt = new("pt");
        public static readonly List<Language> Languages = new() { En, Jpn, De, Fr, Sp, Mx, It, Kor, Cht, Chs, Pl, Ru, Nl, Br, Dk, Fi, No, Pt };
    }
}
