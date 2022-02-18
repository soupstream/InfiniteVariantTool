using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Diagnostics.CodeAnalysis;
using InfiniteVariantTool.Core.Serialization;
using InfiniteVariantTool.Core.Utils;

namespace InfiniteVariantTool.Core
{
    public class LuaBundleUnpacker
    {
        public const string LuaManifestFileName = "lua_files.xml";
        private MyBinaryReader br;
        public LuaBundleUnpacker(string filename)
        {
            br = new MyBinaryReader(File.ReadAllBytes(filename));
        }

        public LuaBundleUnpacker(byte[] data)
        {
            br = new MyBinaryReader(data);
        }

        public LuaBundle Save(string directory)
        {
            LuaBundle bundle = Unpack();
            foreach (LuaScript script in bundle.Scripts)
            {
                Directory.CreateDirectory(Path.Combine(directory, Path.GetDirectoryName(script.Metadata.File)!));
                File.WriteAllBytes(Path.Combine(directory, script.Metadata.File), script.Data);
                if (script.Compiled)
                {
                    File.WriteAllBytes(Path.Combine(directory, script.Metadata.File2!), script.Data2);
                }
            }
            bundle.CreateManifest().SaveVersioned(Path.Combine(directory, LuaManifestFileName));
            return bundle;
        }

        public LuaBundle Unpack()
        {
            // may have size of entire file at beginning
            br.SetLittleEndian(false);
            if (br.ReadInt32() != br.Size - sizeof(int))
            {
                br.Skip(-sizeof(int));
            }
            br.SetLittleEndian(true);

            int scriptCount = DecompressInt();
            List<LuaScript> scripts = new();
            FileNameDeduper deduper = new();
            for (int i = 0; i < scriptCount; i++)
            {
                LuaScript script = DecompressScript();
                script.Metadata.File = deduper.Dedupe(script.Metadata.File);
                if (script.Metadata.Compiled)
                {
                    script.Metadata.File2 = deduper.Dedupe(script.Metadata.File2);
                }
                scripts.Add(script);
            }
            return new LuaBundle(scripts);
        }

        // rough heuristic to check whether data is a lua bundle
        public static bool IsLuaBundle(byte[] data)
        {
            if (data.Length < 18)
            {
                return false;
            }
            MyBinaryReader br = new(data);

            br.SetLittleEndian(false);
            if (br.ReadInt32() != data.Length - sizeof(int))
            {
                br.Skip(-sizeof(int));
            }
            br.SetLittleEndian(true);

            long compressedSize = br.ReadInt64();
            if (compressedSize <= 0 || compressedSize > 10)
            {
                return false;
            }
            if (Oodle.TryDecompress(br.ReadBytes((int)compressedSize), sizeof(int), out byte[]? intBytes))
            {
                br = new(intBytes);
                int scriptCount = br.ReadInt32();
                return scriptCount > 0 && scriptCount < 1000;
            }
            else
            {
                return false;
            }
        }

        private LuaScript DecompressScript()
        {
            MyBinaryReader headerReader = DecompressBlob(296);
            int scriptType = headerReader.ReadInt32();
            int scriptSize = headerReader.ReadInt32();
            int compiledScriptSize = headerReader.ReadInt32();
            int compiledScript2Size = headerReader.ReadInt32();
            int globalsCount = headerReader.ReadInt32();
            int globals2Count = headerReader.ReadInt32();
            byte unk = headerReader.ReadUInt8();
            string name = Util.NullTerminate(headerReader.ReadString(255));
            ulong unk2 = headerReader.ReadUInt64();
            ulong checksum = headerReader.ReadUInt64();

            if (scriptSize != 0)
            {
                byte[] scriptData = DecompressBlob(scriptSize).Data;
                LuaScriptMetadata metadata = new(name, name, scriptType, checksum, unk);
                return new LuaScript(metadata, scriptData);
            }
            else if (compiledScriptSize != 0)
            {
                string filename = name + "c"; // give actual filename .luac extension
                byte[] scriptData = DecompressBlob(compiledScriptSize).Data;
                List<string> globals = new();
                for (int i = 0; i < globalsCount; i++)
                {
                    globals.Add(DecompressString());
                }

                byte[] script2Data = DecompressBlob(compiledScript2Size).Data;
                string filename2 = filename;
                List<string> globals2 = new();
                for (int i = 0; i < globals2Count; i++)
                {
                    globals2.Add(DecompressString());
                }

                LuaScriptMetadata metadata = new(name, filename, scriptType, checksum, unk, globals, filename2, globals2);
                return new LuaScript(metadata, scriptData, script2Data);
            }
            else
            {
                throw new Exception();
            }
        }

        private int DecompressInt()
        {
            return DecompressBlob(sizeof(int)).ReadInt32();
        }

        private string DecompressString()
        {
            int size = DecompressInt();
            return DecompressBlob(size).ReadString(size);
        }

        private MyBinaryReader DecompressBlob(long decompressedSize)
        {
            long size = br.ReadInt64();
            byte[] data = br.ReadBytes((int)size);
            return new MyBinaryReader(Oodle.Decompress(data, decompressedSize));
        }

        public static string SuggestFilename(string filename)
        {
            if (filename.EndsWith(".luabundle"))
            {
                filename = filename[..^10];
            }
            else if (filename.EndsWith(".debugscriptsource"))
            {
                filename = filename[..^18];
            }
            return filename + "_lua";
        }
    }

    public class LuaBundle
    {
        public List<LuaScript> Scripts { get; set; }
        public LuaBundle(List<LuaScript> scripts)
        {
            Scripts = scripts;
        }

        public XElement CreateManifest()
        {
            XDocument doc = new();
            using (var writer = doc.CreateWriter())
            {
                writer.WriteStartElement("Scripts");
                foreach (LuaScript script in Scripts)
                {
                    new XmlSerializer(script.Metadata.GetType()).Serialize(writer, script.Metadata);
                }

                writer.WriteEndElement();
            }

            foreach (XElement node in doc.Root!.Elements())
            {
                node.Attributes().Remove();
            }

            return doc.Root;
        }

        public bool MyEquals(LuaBundle? other)
        {
            return other != null
                && Scripts.Zip(other.Scripts).All(pair => pair.First.MyEquals(pair.Second));
        }
    }

    public class LuaScript
    {
        public LuaScriptMetadata Metadata { get; set; }
        public byte[] Data { get; set; }
        public byte[]? Data2 { get; set; }

        [MemberNotNullWhen(true, nameof(Data2))]
        public bool Compiled => Metadata.Compiled;

        // compiled script
        public LuaScript(LuaScriptMetadata metadata, byte[] data, byte[] data2)
        {
            Metadata = metadata;
            Data = data;
            Data2 = data2;
        }

        // uncompiled script
        public LuaScript(LuaScriptMetadata metadata, byte[] data)
        {
            Metadata = metadata;
            Data = data;
            Data2 = null;
        }

        public bool MyEquals(LuaScript? other)
        {
            return other != null
                && Metadata.MyEquals(other.Metadata)
                && Data.SequenceEqual(other.Data)
                && Util.NullableSequenceEqual(Data2, other.Data2);
        }
    }

    [XmlRoot("Script")]
    public class LuaScriptMetadata
    {
        public string Name { get; set; }
        public string File { get; set; }
        public List<string>? Globals { get; set; }
        public string? File2 { get; set; }
        public List<string>? Globals2 { get; set; }
        public int Unk0 { get; set; }
        public ulong Checksum { get; set; }
        public byte Unk1 { get; set; }

        [MemberNotNullWhen(true, nameof(Globals), nameof(File2), nameof(Globals2))]
        public bool Compiled { get; set; }

        // compiled script
        public LuaScriptMetadata(string name, string file, int type, ulong checksum, byte unk, List<string> globals, string file2, List<string> globals2)
        {
            Name = name;
            File = file;
            Globals = globals;
            File2 = file2;
            Globals2 = globals2;
            Unk0 = type;
            Checksum = checksum;
            Unk1 = unk;
            Compiled = true;
        }

        // uncompiled script
        public LuaScriptMetadata(string name, string file, int type, ulong checksum, byte unk)
        {
            Name = name;
            File = file;
            Unk0 = type;
            Checksum = checksum;
            Unk1 = unk;
            Globals = null;
            File2 = null;
            Globals2 = null;
            Compiled = false;
        }

        internal LuaScriptMetadata()
        {
            Name = null!;
            File = null!;
        }

        public bool MyEquals(LuaScriptMetadata? other)
        {
            return other != null
                && Name == other.Name
                && File == other.File
                && File2 == other.File2
                && Unk0 == other.Unk0
                && Checksum == other.Checksum
                && Unk1 == other.Unk1
                && Compiled == other.Compiled
                && Util.NullableSequenceEqual(Globals, other.Globals)
                && Util.NullableSequenceEqual(Globals2, other.Globals2);
        }
    }
}
