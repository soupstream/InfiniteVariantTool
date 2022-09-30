using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Globalization;
using System.Buffers.Binary;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using InfiniteVariantTool.Core.Utils;
using System.Diagnostics.CodeAnalysis;

namespace InfiniteVariantTool.Core.Serialization
{
    public enum BondType
    {
        stop = 0,
        stop_base = 1,
        @bool = 2,
        uint8 = 3,
        uint16 = 4,
        uint32 = 5,
        uint64 = 6,
        @float = 7,
        @double = 8,
        @string = 9,
        @struct = 10,
        list = 11,
        set = 12,
        map = 13,
        int8 = 14,
        int16 = 15,
        int32 = 16,
        int64 = 17,
        wstring = 18,
        struct_v1 = 27,
        unavailable = 127,

        // my types
        struct_base = -1,
        root = -2,
        blob = -3,
        guid = -4
    }

    public enum BondCompressionType
    {
        deflate
    }

    public enum BondAttr
    {
        pad,
        file,
        type,
        key_type,
        value_type,
        id,
        compression
    }

    // Implements a reader for the Microsoft Bond compact binary format, with Halo Infinite-specific enhancements.
    // See https://github.com/microsoft/bond/blob/master/cpp/inc/bond/protocol/compact_binary.h
    public class BondReader
    {
        private MyBinaryReader br;
        private Stack<MyBinaryReader> brStack = new();
        private bool littleEndian = true;
        private bool extractBlobs = true;
        private Dictionary<string, byte[]> blobs;

        public int BlobLengthThreshold { get; set; } = 100;

        public bool EOF => br.EOF;

        public BondReader(byte[] data)
        {
            br = new MyBinaryReader(data, littleEndian);
            blobs = new();
        }

        public BondReader(string filename)
        {
            byte[] data = File.ReadAllBytes(filename);
            br = new MyBinaryReader(data, littleEndian);
            blobs = new();
        }

        public BondReadResult Save(string filename)
        {
            BondReadResult result = Read();
            result.ReadEmbeddedBond();
            result.Save(filename);
            return result;
        }

        public BondReadResult Read()
        {
            using (new Culture(CultureInfo.InvariantCulture))
            {
                XElement root = ReadStruct();

                if (!EOF && !ReadPadding(root))
                {
                    root = ReadCompressed(root);
                }
                BondReaderException.Assert(EOF, "trailing data");
                return new BondReadResult(root, blobs);
            }
        }

        public BondReadResult Read(bool readEmbeddedBond)
        {
            var result = Read();
            if (readEmbeddedBond)
            {
                result.MakeBlob("./list[@id='2']");
                result.ReadEmbeddedBond();
            }
            return result;
        }

        // read zero-padding after the main structure
        private bool ReadPadding(XElement root)
        {
            if (!br.EOF)
            {
                br.PushCursor();
                byte[] remainingData = br.ReadBytes(br.Size - br.Position);
                if (remainingData.All(b => b == 0))
                {
                    root.SetAttributeValue(BondAttr.pad.ToString(), br.Size);
                    return true;
                }
                else
                {
                    br.PopCursor();
                }
            }
            return false;
        }

        // some .mvar files have a blob of compressed bond data after the main structure
        private XElement ReadCompressed(XElement root)
        {
            br.SetLittleEndian(false);
            int size = br.ReadInt32();
            br.SetLittleEndian(true);

            // decompress data
            byte[] zipData = br.ReadBytes(size);
            var decompressedDataStream = new MemoryStream();
            using var decompressor = new InflaterInputStream(new MemoryStream(zipData));
            decompressor.CopyTo(decompressedDataStream);

            // read structure
            PushReader(decompressedDataStream.ToArray());
            var decompressedNode = ReadStruct();
            BondReaderException.Assert(EOF, "trailing data");
            decompressedNode.SetAttributeValue(BondAttr.compression.ToString(), BondCompressionType.deflate);
            PopReader();

            return new XElement(BondType.root.ToString(), root, decompressedNode);
        }

        private XElement ReadValue(BondType type)
        {
            var node = new XElement(type.ToString());

            int length;
            switch (type)
            {
                case BondType.stop:
                    break;
                case BondType.stop_base:
                    break;
                case BondType.@bool:
                    node.Add(br.ReadBool());
                    break;
                case BondType.uint8:
                    node.Add(br.ReadUInt8());
                    break;
                case BondType.uint16:
                case BondType.uint32:
                case BondType.uint64:
                    node.Add(br.ReadLEB128U());
                    break;
                case BondType.@float:
                    node.Add(br.ReadFloat());
                    break;
                case BondType.@double:
                    node.Add(br.ReadDouble());
                    break;
                case BondType.@string:
                    length = (int)br.ReadLEB128U();
                    node.Add(br.ReadString(length));
                    break;
                case BondType.@struct:
                    node = ReadStruct();
                    break;
                case BondType.list:
                case BondType.set:
                    node = ReadList(type);
                    break;
                case BondType.map:
                    node = ReadMap();
                    break;
                case BondType.int8:
                    node.Add(br.ReadInt8());
                    break;
                case BondType.int16:
                case BondType.int32:
                case BondType.int64:
                    node.Add(br.ReadLEB128());
                    break;
                case BondType.wstring:
                    length = (int)br.ReadLEB128U();
                    node.Add(br.ReadWString(length));
                    break;
                case BondType.struct_v1:
                    node = ReadStruct(1);
                    break;
                default:
                    throw new BondReaderException("Invalid type: " + (int)type);
            }
            return node;
        }

        private XElement ReadStruct(int version = 2)
        {
            XElement node;
            ulong? length = null;
            int? startPos = null;
            if (version == 1)
            {
                node = new XElement(BondType.struct_v1.ToString());
            }
            else if (version == 2)
            {
                node = new XElement(BondType.@struct.ToString());
                length = br.ReadLEB128U();
                startPos = br.Position;
            }
            else
            {
                throw new BondReaderException("Invalid struct version: " + version);
            }

            while (true)
            {
                XElement field = ReadField(out BondType type);
                if (type is BondType.stop)
                {
                    break;
                }
                else if (type is BondType.stop_base)
                {
                    XElement baseStruct = new XElement(BondType.struct_base.ToString(), node.Elements());
                    node.Elements().Remove();
                    node.Add(baseStruct);
                }
                else
                {
                    node.Add(field);
                }
            }

            if (version == 2)
            {
                int realLength = br.Position - startPos!.Value;
                if ((ulong)realLength != length!.Value)
                {
                    throw new BondReaderException(string.Format("Incorrect struct length at {0}; expected {1}, got {2}", br.Position, length, realLength));
                }
            }

            if (TryGetGuid(node, out Guid guid))
            {
                node = new XElement(BondType.guid.ToString(),
                    new XText(guid.ToString()));
            }

            return node;
        }

        private static bool IsNodeGuid(XElement node)
        {
            if (node.Elements().Count() == 4)
            {
                BondType[] schema = { BondType.uint32, BondType.uint16, BondType.uint16, BondType.uint64 };
                for (int i = 0; i < 4; i++)
                {
                    if (node.Elements().ElementAt(i).Name != schema[i].ToString())
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public static bool TryGetGuid(XElement node, [MaybeNullWhen(false)] out Guid guid)
        {
            if (!IsNodeGuid(node))
            {
                guid = default;
                return false;
            }

            if (uint.TryParse(node.Elements().ElementAt(0).GetText(), out uint n1)
                && ushort.TryParse(node.Elements().ElementAt(1).GetText(), out ushort n2)
                && ushort.TryParse(node.Elements().ElementAt(2).GetText(), out ushort n3)
                && ulong.TryParse(node.Elements().ElementAt(3).GetText(), out ulong n4))
            {
                byte[] n4Bytes = new byte[sizeof(ulong)];
                BinaryPrimitives.WriteUInt64LittleEndian(n4Bytes, n4);
                guid = new Guid((int)n1, (short)n2, (short)n3, n4Bytes);
                return true;
            }

            guid = default;
            return false;
        }

        private XElement ReadList(BondType containerType = BondType.list)
        {
            var node = new XElement(containerType.ToString());
            byte countType = br.ReadUInt8();
            int count = countType >> 5;
            BondType type = (BondType)(countType & 0x1f);
            node.SetAttributeValue(BondAttr.type.ToString(), type);
            if (count == 0)
            {
                count = (int)br.ReadLEB128U();
            }
            else
            {
                count -= 1;
            }

            if (type is BondType.stop or BondType.stop_base)
            {
                throw new BondReaderException("invalid list type: " + type.ToString());
            }

            // save large lists of bytes as raw data
            if (extractBlobs && containerType == BondType.list
                && type is BondType.int8 or BondType.uint8
                && count >= BlobLengthThreshold)
            {
                byte[] blob = br.ReadBytes(count);
                string tmpName = string.Format("blob_{0:D3}", blobs.Count);
                blobs[tmpName] = blob;
                node = new XElement(BondType.blob.ToString(),
                    new XAttribute(BondAttr.type.ToString(), type.ToString()));
                node.SetText(tmpName);
                return node;
            }

            for (int i = 0; i < count; i++)
            {
                node.Add(ReadValue(type));
            }

            return node;
        }

        private XElement ReadMap()
        {
            var node = new XElement(BondType.map.ToString());
            BondType keyType = (BondType)(br.ReadUInt8() & 0x1f);
            BondType valueType = (BondType)(br.ReadUInt8() & 0x1f);
            ulong count = br.ReadLEB128U();
            for (ulong i = 0; i < count; i++)
            {
                node.Add(ReadValue(keyType));
                node.Add(ReadValue(valueType));
            }
            node.SetAttributeValue(BondAttr.key_type.ToString(), keyType);
            node.SetAttributeValue(BondAttr.value_type.ToString(), valueType);
            return node;
        }

        private XElement ReadField(out BondType type)
        {
            byte idAndType = br.ReadUInt8();
            type = (BondType)(idAndType & 0x1f);
            int id = idAndType >> 5;

            if (id == 6)
            {
                id = br.ReadUInt8();
            }
            else if (id == 7)
            {
                id = br.ReadUInt16();
            }

            XElement node = ReadValue(type);

            // add id as first attribute
            List<XAttribute> attributes = new();
            attributes.Add(new XAttribute(BondAttr.id.ToString(), id));
            attributes.AddRange(node.Attributes());
            node.ReplaceAttributes(attributes);
            return node;
        }

        // helpers

        private void PushReader(byte[] data)
        {
            brStack.Push(br);
            br = new MyBinaryReader(data);
        }

        private MyBinaryReader PopReader()
        {
            var ret = br;
            br = brStack.Pop();
            return ret;
        }

        public static string SuggestFilename(string filename)
        {
            return filename + ".xml";
        }
    }

    public class BondReadResult
    {
        public XElement Doc { get; }
        public Dictionary<string, byte[]> Blobs { get; }
        public bool HasEmbeddedBond { get; private set; }

        public BondReadResult(XElement doc, Dictionary<string, byte[]> blobs)
        {
            Doc = doc;
            Blobs = blobs;
        }

        public void Save(string path)
        {
            path = Path.GetFullPath(path);
            string fileName = Path.GetFileName(path);
            SetFileName(fileName);
            string directory = Path.GetDirectoryName(path)!;
            foreach (var blob in Blobs)
            {
                File.WriteAllBytes(Path.Combine(directory, blob.Key), blob.Value);
            }
            Doc.SaveVersioned(path);
        }

        public void SetFileName(string fileName)
        {
            FileNameDeduper deduper = new();
            deduper.Dedupe(fileName);
            fileName = FileUtil.RemoveSpecificExtension(fileName, FileExtension.Xml.Value);
            foreach (XElement node in Doc.Descendants("blob"))
            {
                string blobText = node.GetText();
                if (blobText != "")
                {
                    byte[] blob = Blobs[blobText];
                    FileExtension fileType = FileUtil.DetectFileType(blob);
                    string newFileName = deduper.Dedupe(fileName + fileType.Value);
                    node.SetText(newFileName);
                    Blobs.Remove(blobText);
                    Blobs[newFileName] = blob;
                }
            }
        }

        // make a list of bytes a blob if it isn't already a blob
        public bool MakeBlob(string xpath, BondType type = BondType.uint8)
        {
            XElement? node = Doc.XPathSelectElement(xpath);
            if (node != null && node.Name == BondType.list.ToString())
            {
                string? actualType = node.Attribute(BondAttr.type.ToString())?.ToString();
                if (type.ToString() == actualType)
                {
                    byte[] data = ListToBlob(node);
                    node.Elements().Remove();
                    node.Name = BondType.blob.ToString();
                    string blobName = string.Format("blob_{0:D3}", Blobs.Count);
                    node.SetText(blobName);
                    Blobs[blobName] = data;
                    return true;
                }
            }
            return false;
        }

        public static byte[] ListToBlob(XElement listElement)
        {
            byte[] data = new byte[listElement.Elements().Count()];
            int i = 0;
            foreach (var elem in listElement.Elements())
            {
                data[i++] = (byte)int.Parse(elem.GetText());
            }
            return data;
        }

        public static BondReadResult Load(string filename)
        {
            XElement doc = XElement.Load(filename);
            Dictionary<string, byte[]> blobs = new();
            foreach (XElement node in doc.Descendants("blob"))
            {
                string blobFilename = node.GetText();
                if (blobFilename != "")
                {
                    blobFilename = Path.Combine(Path.GetDirectoryName(filename)!, blobFilename);
                    blobs[blobFilename] = File.ReadAllBytes(blobFilename);
                }
            }
            return new BondReadResult(doc, blobs);
        }

        public bool ReadEmbeddedBond()
        {
            var bondblobs = Blobs.Where(entry => FileUtil.DetectFileType(entry.Value) == FileExtension.Bin).ToArray();
            bool foundBondBlob = bondblobs.Any();
            foreach (var bondblob in bondblobs)
            {
                BondReader br = new(bondblob.Value);
                var blobNode = Doc.Descendants("blob").First(node => node.GetText() == bondblob.Key);
                BondReadResult result;
                try
                {
                    result = br.Read();
                }
                catch
                {
                    continue;
                }
                blobNode.RemoveNodes();
                blobNode.Add(result.Doc);
                Blobs.Remove(bondblob.Key);
                foreach (var childBlob in result.Blobs)
                {
                    Blobs[childBlob.Key] = childBlob.Value;
                }
                HasEmbeddedBond = true;
            }
            return foundBondBlob;
        }
    }

    public class BondReaderException : Exception
    {
        public BondReaderException() { }
        public BondReaderException(string message) : base(message) { }
        public BondReaderException(string message, Exception innerException) : base(message, innerException) { }
        public static void Assert(bool condition, string message) { if (!condition) throw new BondReaderException(message); }
    }
}
