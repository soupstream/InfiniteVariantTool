using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Diagnostics.CodeAnalysis;
using InfiniteVariantTool.Core.Serialization;
using InfiniteVariantTool.Core.Utils;
using System.IO.Compression;
using System.Xml;
using System.Text;

namespace InfiniteVariantTool.Core
{
    [XmlRoot]
    public class LuaBundle
    {
        public Game Game { get; set; }

        [XmlArray("Scripts")]
        [XmlArrayItem("HICompiledLuaScript", typeof(HICompiledLuaScript))]
        [XmlArrayItem("HIUncompiledLuaScript", typeof(HIUncompiledLuaScript))]
        [XmlArrayItem("H5LuaScript", typeof(H5LuaScript))]
        public List<LuaScript> Scripts { get; set; }

        [XmlIgnore]

        public const string LuaManifestFileName = "lua_files.xml";

        public LuaBundle(List<LuaScript> scripts, Game game)
        {
            Scripts = scripts;
            Game = game;
        }

        // parameterless constructor for serialization
        public LuaBundle()
        {
            Scripts = null!;
        }

        public static LuaBundle Unpack(string filename, Game? game)
        {
            return Unpack(File.ReadAllBytes(filename), game);
        }

        public static LuaBundle Unpack(byte[] data, Game? game)
        {
            if (game == null)
            {
                game = LuaBundleUtils.DetectGame(data) ?? Game.HaloInfinite;
            }

            if (game == Game.Halo5)
            {
                data = LuaBundleUtils.ZlibDecompress(data);
            }
            MyBinaryReader br = new(data);

            if (game == Game.HaloInfinite)
            {
                // may have size of entire file at beginning
                br.SetLittleEndian(false);
                if (br.ReadInt32() != br.Size - sizeof(int))
                {
                    br.Skip(-sizeof(int));
                }
                br.SetLittleEndian(true);
            }

            int scriptCount = LuaBundleUtils.ReadInt(br, game.Value);
            List<LuaScript> scripts = new();
            FileNameDeduper deduper = new();
            for (int i = 0; i < scriptCount; i++)
            {
                LuaScript script = LuaScript.Unpack(br, game.Value);
                script.DedupeFileName(deduper);
                scripts.Add(script);
            }

            return new LuaBundle(scripts, game.Value);
        }

        public byte[] Pack()
        {
            MyBinaryWriter bw = new();

            LuaBundleUtils.WriteInt(bw, Game, Scripts.Count);
            foreach (LuaScript script in Scripts)
            {
                bw.WriteBytes(script.Pack());
            }
            byte[] data = bw.Data.ToArray();
            
            if (Game == Game.Halo5)
            {
                data = LuaBundleUtils.ZlibCompress(data);
            }

            return data;
        }

        public void Save(string directory)
        {
            Directory.CreateDirectory(directory);
            foreach (LuaScript script in Scripts)
            {
                script.Save(directory);
            }

            // create manifest
            XDocument doc = new();
            using (var writer = doc.CreateWriter())
            {
                var serializer = new XmlSerializer(GetType());
                serializer.Serialize(writer, this);
            }
            doc.SaveVersioned(Path.Combine(directory, LuaManifestFileName));
        }

        public static LuaBundle Load(string directory)
        {
            XmlSerializer serializer = new(typeof(LuaBundle));
            LuaBundle bundle;
            using (XmlReader reader = XmlReader.Create(Path.Combine(directory, LuaManifestFileName)))
            {
                bundle = (LuaBundle)serializer.Deserialize(reader)!;
            }
            
            foreach (LuaScript script in bundle.Scripts)
            {
                script.LoadContent(directory);
            }

            return bundle;
        }

        public bool MyEquals(LuaBundle? other)
        {
            return other != null
                && Scripts.Zip(other.Scripts).All(pair => pair.First.MyEquals(pair.Second));
        }
    }

    public abstract class LuaScript
    {
        public string Name { get; set; }
        public string File { get; set; }
        [XmlIgnore]
        public byte[] Content { get; set; }
        public int Unk0 { get; set; }


        public LuaScript(byte[] data, string name, string filename, int unk0)
        {
            Name = name;
            File = filename;
            Content = data;
            Unk0 = unk0;
        }

        // parameterless constructor for serialization
        public LuaScript()
        {
            Name = null!;
            File = null!;
            Content = null!;
        }

        public static LuaScript Unpack(MyBinaryReader br, Game game)
        {
            if (game == Game.Halo5)
            {
                int unk0 = br.ReadInt32();
                int scriptSize = br.ReadInt32();
                string name = br.ReadStringBuffer(64);
                byte[] scriptData = br.ReadBytes(scriptSize);
                return new H5LuaScript(scriptData, name, name, unk0);
            }
            else
            {
                MyBinaryReader headerReader = LuaBundleUtils.DecompressBlob(br, 296);
                int unk0 = headerReader.ReadInt32();
                int scriptSize = headerReader.ReadInt32();
                int serverScriptSize = headerReader.ReadInt32();
                int clientScriptSize = headerReader.ReadInt32();
                int serverGlobalsCount = headerReader.ReadInt32();
                int clientGlobalsCount = headerReader.ReadInt32();
                byte unk1 = headerReader.ReadUInt8();
                string name = headerReader.ReadStringBuffer(255);
                ulong unk2 = headerReader.ReadUInt64();
                ulong checksum = headerReader.ReadUInt64();

                if (scriptSize != 0)
                {
                    byte[] scriptData = LuaBundleUtils.DecompressBlob(br, scriptSize).Data;
                    return new HIUncompiledLuaScript(scriptData, name, name, checksum, unk0, unk1);
                }
                else if (serverScriptSize != 0)
                {
                    if (clientScriptSize == 0)
                    {
                        throw new Exception("missing client script");
                    }

                    string serverFilename = Path.ChangeExtension(name, ".server.luac");
                    string clientFilename = Path.ChangeExtension(name, ".client.luac");
                    DecompressScript(br, game, serverScriptSize, serverGlobalsCount, out byte[] serverScriptData, out List<string> serverGlobals);
                    DecompressScript(br, game, clientScriptSize, clientGlobalsCount, out byte[] clientScriptData, out List<string> clientGlobals);

                    return new HICompiledLuaScript(serverScriptData, clientScriptData, name, serverFilename, clientFilename, serverGlobals, clientGlobals, checksum, unk0, unk1);
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        public abstract byte[] Pack();

        public virtual void Save(string directory)
        {
            Directory.CreateDirectory(Path.Combine(directory, Path.GetDirectoryName(File)!));
            System.IO.File.WriteAllBytes(Path.Combine(directory, File), Content);
        }

        public virtual void LoadContent(string directory)
        {
            Content = System.IO.File.ReadAllBytes(Path.Combine(directory, File));
        }

        private static void DecompressScript(MyBinaryReader br, Game game, int scriptSize, int globalCount, out byte[] scriptData, out List<string> globals)
        {
            scriptData = LuaBundleUtils.DecompressBlob(br, scriptSize).Data;
            globals = new();
            for (int i = 0; i < globalCount; i++)
            {
                globals.Add(LuaBundleUtils.ReadString(br, game));
            }
        }

        public virtual void DedupeFileName(FileNameDeduper deduper)
        {
            File = deduper.Dedupe(File);
        }

        public virtual bool MyEquals(LuaScript? other)
        {
            return other != null
                && Name == other.Name
                && File == other.File
                && Content.SequenceEqual(other.Content)
                && Unk0 == other.Unk0;
        }
    }

    public abstract class HILuaScript : LuaScript
    {
        public ulong Checksum { get; set; }
        public byte Unk1 { get; set; }
        public HILuaScript(byte[] data, string name, string file, ulong checksum, int unk0, byte unk1)
            : base(data, name, file, unk0)
        {
            Checksum = checksum;
            Unk1 = unk1;
        }

        // parameterless constructor for serialization
        public HILuaScript()
            : base()
        {
        }

        public override bool MyEquals(LuaScript? other)
        {
            if (other is HILuaScript other2)
            {
                return base.Equals(other2)
                    && Checksum == other2.Checksum
                    && Unk1 == other2.Unk1;
            }
            return false;
        }
    }

    public class HIUncompiledLuaScript : HILuaScript
    {
        public HIUncompiledLuaScript(byte[] data, string name, string file, ulong checksum, int unk0, byte unk1)
            : base(data, name, file, checksum, unk0, unk1)
        { 
        }

        // parameterless constructor for serialization
        public HIUncompiledLuaScript()
            : base()
        {
        }

        public override byte[] Pack()
        {
            MyBinaryWriter bw = new();
            MyBinaryWriter headerWriter = new();
            headerWriter.WriteInt32(Unk0);
            headerWriter.WriteInt32(Content.Length);
            headerWriter.WriteInt32(0);
            headerWriter.WriteInt32(0);
            headerWriter.WriteInt32(0);
            headerWriter.WriteInt32(0);
            headerWriter.WriteUInt8(Unk1);
            headerWriter.WriteStringBuffer(Name, 255);
            headerWriter.WriteUInt64(0);
            headerWriter.WriteUInt64(Checksum);
            LuaBundleUtils.CompressBlob(bw, headerWriter.Data.ToArray());
            LuaBundleUtils.CompressBlob(bw, Content);
            return bw.Data.ToArray();
        }
    }

    public class HICompiledLuaScript : HILuaScript
    {
        [XmlIgnore]
        public byte[] ClientContent { get; set; }
        public string Clientfile { get; set; }
        public List<string> ServerGlobals { get; set; }
        public List<string> ClientGlobals { get; set; }

        public HICompiledLuaScript(byte[] serverData, byte[] clientData, string name, string serverFile, string clientFile, List<string> serverGlobals, List<string> clientGlobals, ulong checksum, int unk0, byte unk1)
            : base(serverData, name, serverFile, checksum, unk0, unk1)
        {
            ClientContent = clientData;
            Clientfile = clientFile;
            ServerGlobals = serverGlobals;
            ClientGlobals = clientGlobals;
        }

        // parameterless constructor for serialization
        public HICompiledLuaScript()
            : base()
        {
            ClientContent = null!;
            Clientfile = null!;
            ServerGlobals = null!;
            ClientGlobals = null!;
        }

        public override void DedupeFileName(FileNameDeduper deduper)
        {
            base.DedupeFileName(deduper);
            deduper.Dedupe(Clientfile);
        }

        public override void Save(string directory)
        {
            base.Save(directory);
            Directory.CreateDirectory(Path.Combine(directory, Path.GetDirectoryName(Clientfile)!));
            System.IO.File.WriteAllBytes(Path.Combine(directory, Clientfile), ClientContent);
        }

        public override void LoadContent(string directory)
        {
            base.LoadContent(directory);
            ClientContent = System.IO.File.ReadAllBytes(Path.Combine(directory, Clientfile));
        }
        public override byte[] Pack()
        {
            MyBinaryWriter bw = new();
            MyBinaryWriter headerWriter = new();
            headerWriter.WriteInt32(Unk0);
            headerWriter.WriteInt32(0);
            headerWriter.WriteInt32(Content.Length);
            headerWriter.WriteInt32(ClientContent.Length);
            headerWriter.WriteInt32(ServerGlobals.Count);
            headerWriter.WriteInt32(ClientGlobals.Count);
            headerWriter.WriteUInt8(Unk1);
            headerWriter.WriteStringBuffer(Name, 255);
            headerWriter.WriteUInt64(0);
            headerWriter.WriteUInt64(Checksum);
            LuaBundleUtils.CompressBlob(bw, headerWriter.Data.ToArray());
            LuaBundleUtils.CompressBlob(bw, Content);
            foreach (string global in ServerGlobals)
            {
                LuaBundleUtils.WriteString(bw, Game.HaloInfinite, global);
            }
            LuaBundleUtils.CompressBlob(bw, ClientContent);
            foreach (string global in ClientGlobals)
            {
                LuaBundleUtils.WriteString(bw, Game.HaloInfinite, global);
            }
            return bw.Data.ToArray();
        }

        public override bool MyEquals(LuaScript? other)
        {
            if (other is HICompiledLuaScript other2)
            {
                return base.MyEquals(other)
                    && ClientContent.SequenceEqual(other2.ClientContent)
                    && Clientfile == other2.Clientfile
                    && ServerGlobals.SequenceEqual(other2.ServerGlobals)
                    && ClientGlobals.SequenceEqual(other2.ClientGlobals);
            }
            return false;
        }
    }

    public class H5LuaScript : LuaScript
    {
        public H5LuaScript(byte[] data, string name, string file, int unk0)
            : base(data, name, file, unk0)
        {
        }

        // parameterless constructor for serialization
        public H5LuaScript()
            : base()
        {
        }

        public override byte[] Pack()
        {
            MyBinaryWriter bw = new();
            bw.WriteInt32(Unk0);
            bw.WriteInt32(Content.Length);
            bw.WriteStringBuffer(Name, 64);
            bw.WriteBytes(Content);
            return bw.Data.ToArray();
        }
    }

    public class LuaBundleUtils
    {

        public static byte[] ZlibDecompress(byte[] data)
        {
            using var inputStream = new MemoryStream(data);
            using var outputStream = new MemoryStream();
            using var compressor = new ZLibStream(inputStream, CompressionMode.Decompress);
            compressor.CopyTo(outputStream);
            return outputStream.ToArray();
        }

        public static byte[] ZlibCompress(byte[] data, CompressionMode mode = CompressionMode.Compress)
        {

            using var inputStream = new MemoryStream(data);
            using var outputStream = new MemoryStream();
            using var compressor = new ZLibStream(outputStream, mode);
            inputStream.CopyTo(compressor);
            compressor.Close();
            return outputStream.ToArray();
        }

        public static int ReadInt(MyBinaryReader br, Game game)
        {
            if (game == Game.HaloInfinite)
            {
                br = DecompressBlob(br, sizeof(int));
            }
            return br.ReadInt32();
        }

        public static string ReadString(MyBinaryReader br, Game game)
        {
            int length = ReadInt(br, game);
            if (game == Game.HaloInfinite)
            {
                br = DecompressBlob(br, length);
            }
            return br.ReadString(length);
        }

        public static MyBinaryReader DecompressBlob(MyBinaryReader br, long decompressedSize)
        {
            long size = br.ReadInt64();
            byte[] data = br.ReadBytes((int)size);
            return new MyBinaryReader(Oodle.Decompress(data, decompressedSize));
        }

        public static void WriteInt(MyBinaryWriter bw, Game game, int value)
        {
            if (game == Game.HaloInfinite)
            {
                MyBinaryWriter bw2 = new();
                bw2.WriteInt32(value);
                CompressBlob(bw, bw2.Data.ToArray());
            }
            else
            {
                bw.WriteInt32(value);
            }
        }

        public static void WriteString(MyBinaryWriter bw, Game game, string value)
        {
            if (game == Game.HaloInfinite)
            {
                WriteInt(bw, game, value.Length);
                CompressBlob(bw, Encoding.UTF8.GetBytes(value));
            }
            else
            {
                bw.WriteString(value);
            }
        }

        public static void CompressBlob(MyBinaryWriter bw, byte[] blob)
        {
            byte[] compressed = Oodle.Compress(blob, OodleLZ_Compressor.Kraken, OodleLZ_CompressionLevel.Optimal);
            int size = compressed.Length;
            bw.WriteInt64(size);
            bw.WriteBytes(compressed);
        }

        public static string SuggestUnpackFilename(string filename)
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

        public static string SuggestPackFilename(string filename)
        {
            filename = Path.TrimEndingDirectorySeparator(filename);
            if (filename.EndsWith("_lua"))
            {
                filename = filename[..^4];
            }
            return filename + ".luabundle";
        }

        public static Game? DetectGame(byte[] data)
        {
            // Decompress header to check if it's a Halo Infinite luabundle
            if (data.Length >= 18)
            {
                MyBinaryReader br = new(data);

                br.SetLittleEndian(false);
                if (br.ReadInt32() != data.Length - sizeof(int))
                {
                    br.Skip(-sizeof(int));
                }
                br.SetLittleEndian(true);

                long compressedSize = br.ReadInt64();
                if (compressedSize >= 0 && compressedSize <= 10)
                {
                    if (Oodle.TryDecompress(br.ReadBytes((int)compressedSize), sizeof(int), out byte[]? intBytes))
                    {
                        br = new(intBytes);
                        int scriptCount = br.ReadInt32();
                        if (scriptCount > 0 && scriptCount < 1000)
                        {
                            return Game.HaloInfinite;
                        }
                    }
                }
            }

            // if there's a Zlib header, it's a Halo 5 luabundle
            if (data.Length >= 2)
            {
                byte cmf = data[0];
                byte flg = data[1];
                int cm = cmf & 0xf;
                int cinfo = cmf >> 4;
                short zlibHeader = (short)((cmf << 8) | flg);
                if (cm == 8 && cinfo >= 1 && cinfo <= 7 && zlibHeader % 31 == 0)
                {
                    return Game.Halo5;
                }
            }

            return null;
        }

        // rough heuristic to check whether data is a lua bundle
        public static bool IsLuaBundle(byte[] data)
        {
            return DetectGame(data) != null;
        }
    }
}
