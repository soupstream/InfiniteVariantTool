using InfiniteVariantTool.Core.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace InfiniteVariantTool.Core
{
    public class LuaBundlePacker
    {
        private LuaBundle bundle;
        private MyBinaryWriter bw = new();
        private OodleLZ_Compressor compressor = OodleLZ_Compressor.Kraken;
        private OodleLZ_CompressionLevel compressionLevel;

        public LuaBundlePacker(string directory, OodleLZ_CompressionLevel compressionLevel = OodleLZ_CompressionLevel.Optimal)
        {
            bundle = ReadBundle(directory);
            this.compressionLevel = compressionLevel;
        }

        public LuaBundlePacker(LuaBundle bundle, OodleLZ_CompressionLevel compressionLevel = OodleLZ_CompressionLevel.Optimal)
        {
            this.bundle = bundle;
            this.compressionLevel = compressionLevel;
        }

        public byte[] Save(string filename)
        {
            byte[] data = Pack();
            File.WriteAllBytes(filename, data);
            return data;
        }

        public byte[] Pack()
        {
            CompressInt(bundle.Scripts.Count);
            foreach (LuaScript script in bundle.Scripts)
            {
                CompressLuaScript(script);
            }
            return bw.Data.ToArray();
        }

        private void CompressLuaScript(LuaScript script)
        {
            MyBinaryWriter headerWriter = new();
            headerWriter.WriteInt32(script.Metadata.Unk0);
            if (script.Compiled)
            {
                headerWriter.WriteInt32(0);
                headerWriter.WriteInt32(script.Data.Length);
                headerWriter.WriteInt32(script.Data2.Length);
                headerWriter.WriteInt32(script.Metadata.Globals!.Count);
                headerWriter.WriteInt32(script.Metadata.Globals2!.Count);
            }
            else
            {
                headerWriter.WriteInt32(script.Data.Length);
                headerWriter.WriteInt32(0);
                headerWriter.WriteInt32(0);
                headerWriter.WriteInt32(0);
                headerWriter.WriteInt32(0);
            }
            headerWriter.WriteUInt8(script.Metadata.Unk1);
            headerWriter.WriteString(script.Metadata.Name);
            headerWriter.WriteBytes(new byte[255 - script.Metadata.Name.Length]);
            headerWriter.WriteUInt64(0);
            headerWriter.WriteUInt64(script.Metadata.Checksum);
            CompressBlob(headerWriter.Data.ToArray());

            CompressBlob(script.Data);
            if (script.Compiled)
            {
                foreach (string global in script.Metadata.Globals!)
                {
                    CompressString(global);
                }
                CompressBlob(script.Data2);
                foreach (string global in script.Metadata.Globals2!)
                {
                    CompressString(global);
                }
            }
        }

        private void CompressInt(int value)
        {
            MyBinaryWriter writer = new();
            writer.WriteInt32(value);
            CompressBlob(writer.Data.ToArray());
        }

        private void CompressString(string str)
        {
            CompressInt(str.Length);
            CompressBlob(Encoding.UTF8.GetBytes(str));
        }

        private void CompressBlob(byte[] blob)
        {
            byte[] compressed = Oodle.Compress(blob, compressor, compressionLevel);
            int size = compressed.Length;
            bw.WriteInt64(size);
            bw.WriteBytes(compressed);
        }

        private LuaBundle ReadBundle(string directory)
        {
            List<LuaScript> scripts = new();
            foreach (LuaScriptMetadata metadata in ReadManifest(directory))
            {
                byte[] data = File.ReadAllBytes(Path.Combine(directory, metadata.File));
                if (metadata.Compiled)
                {
                    byte[] data2 = File.ReadAllBytes(Path.Combine(directory, metadata.File2));
                    scripts.Add(new LuaScript(metadata, data, data2));
                }
                else
                {
                    scripts.Add(new LuaScript(metadata, data));
                }
            }
            return new LuaBundle(scripts);
        }

        private List<LuaScriptMetadata> ReadManifest(string directory)
        {
            string filename = Path.Combine(directory, LuaBundleUnpacker.LuaManifestFileName);
            XmlSerializer xmlSerializer = new(typeof(LuaScriptMetadata));
            using XmlTextReader reader = new(new FileStream(filename, FileMode.Open, FileAccess.Read));
            List<LuaScriptMetadata> manifest = new();
            if (reader.ReadToDescendant("Script"))
            {
                do
                {
                    manifest.Add((LuaScriptMetadata)xmlSerializer.Deserialize(reader)!);
                } while (reader.ReadToNextSibling("Script"));
            }
            return manifest;
        }

        public static string SuggestFilename(string filename)
        {
            if (filename.EndsWith("_lua"))
            {
                filename = filename[..^4];
            }
            return filename + ".luabundle";
        }
    }
}
