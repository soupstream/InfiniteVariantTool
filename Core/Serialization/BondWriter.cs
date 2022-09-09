using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml.Linq;
using System.Globalization;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using InfiniteVariantTool.Core.Utils;

namespace InfiniteVariantTool.Core.Serialization
{
    // Implements a writer for the Microsoft Bond compact binary format, with Halo Infinite-specific enhancements.
    // See https://github.com/microsoft/bond/blob/master/cpp/inc/bond/protocol/compact_binary.h
    public class BondWriter
    {
        private MyBinaryWriter bw = new();
        private Stack<MyBinaryWriter> bwStack = new();
        private XElement doc;
        private Dictionary<string, byte[]> blobs = new();
        private string? path;

        public BondWriter(string filename)
        {
            doc = XDocument.Load(filename).Root ?? throw new Exception();
            path = Path.GetDirectoryName(filename);
        }

        public BondWriter(XElement doc)
        {
            this.doc = doc;
            path = null;
        }

        public BondWriter(XElement doc, Dictionary<string, byte[]> blobs)
            : this(doc)
        {
            this.blobs = blobs;
        }

        public BondWriter(BondReadResult result)
        {
            doc = result.Doc;
            result.SetFileName("tmp");
            blobs = result.Blobs;
        }

        public byte[] Save(string filename)
        {
            var data = Write();
            File.WriteAllBytes(filename, data);
            return data;
        }

        public byte[] Write()
        {
            using (new Culture(CultureInfo.InvariantCulture))
            {
                WriteValue(doc);
                return bw.Data.ToArray();
            }
        }

        private void WriteValue(XElement node)
        {
            BondType type = GetBondType(node);

            switch (type)
            {
                case BondType.@bool:
                    bw.WriteBool(bool.Parse(node.GetText()));
                    break;
                case BondType.uint8:
                    bw.WriteUInt8(ParseNumber(node.GetText(), Convert.ToByte));
                    break;
                case BondType.uint16:
                    bw.WriteLEB128U(ParseNumber(node.GetText(), Convert.ToUInt16));
                    break;
                case BondType.uint32:
                    bw.WriteLEB128U(ParseNumber(node.GetText(), Convert.ToUInt32));
                    break;
                case BondType.uint64:
                    bw.WriteLEB128U(ParseNumber(node.GetText(), Convert.ToUInt64));
                    break;
                case BondType.@float:
                    bw.WriteFloat(float.Parse(node.GetText()));
                    break;
                case BondType.@double:
                    bw.WriteDouble(double.Parse(node.GetText()));
                    break;
                case BondType.@string:
                    string text = node.GetText();
                    bw.WriteLEB128U((uint)text.Length);
                    bw.WriteString(text);
                    break;
                case BondType.@struct:
                case BondType.struct_v1:
                    WriteStruct(node);
                    break;
                case BondType.root:
                    WriteRoot(node);
                    break;
                case BondType.list:
                case BondType.set:
                    WriteList(node);
                    break;
                case BondType.map:
                    WriteMap(node);
                    break;
                case BondType.int8:
                    bw.WriteInt8(ParseNumber(node.GetText(), Convert.ToSByte));
                    break;
                case BondType.int16:
                    bw.WriteLEB128(ParseNumber(node.GetText(), Convert.ToInt16));
                    break;
                case BondType.int32:
                    bw.WriteLEB128(ParseNumber(node.GetText(), Convert.ToInt32));
                    break;
                case BondType.int64:
                    bw.WriteLEB128(ParseNumber(node.GetText(), Convert.ToInt64));
                    break;
                case BondType.wstring:
                    string wtext = node.GetText();
                    bw.WriteLEB128U((uint)wtext.Length);
                    bw.WriteWString(wtext);
                    break;
                case BondType.blob:
                    WriteBlob(node);
                    break;
                case BondType.guid:
                    WriteGuid(node);
                    break;
                default:
                    throw new BondWriterException("Invalid type: " + type.ToString());
            }
        }

        private void WriteField(XElement node)
        {
            ushort id = ushort.Parse(node.Attribute(BondAttr.id.ToString())?.Value ?? "0");
            BondType type = GetBondType(node);

            // handle my custom types
            if (type is BondType.struct_base)
            {
                WriteStructBase(node);
                return;
            }

            type = GetBondRealType(type);

            byte idAndType = (byte)type;

            if (id <= 5)
            {
                idAndType |= (byte)(id << 5);
                bw.WriteUInt8(idAndType);
            }
            else if (id <= 0xff)
            {
                idAndType |= (byte)(6 << 5);
                bw.WriteUInt8(idAndType);
                bw.WriteUInt8((byte)id);
            }
            else
            {
                idAndType |= (byte)(7 << 5);
                bw.WriteUInt8(idAndType);
                bw.WriteUInt16(id);
            }

            WriteValue(node);
        }

        private void WriteRoot(XElement node)
        {
            foreach (XElement child in node.Elements())
            {
                WriteValue(child);
            }
        }

        private void WriteStructBase(XElement node)
        {
            foreach (XElement child in node.Elements())
            {
                WriteField(child);
            }
            bw.WriteUInt8((byte)BondType.stop_base);
        }

        private void WriteStruct(XElement node)
        {
            var type = GetBondRealType(GetBondType(node));
            int origSize = bw.Size;

            // push a new writer so we can write the size of the struct before the struct itself
            PushWriter();

            foreach (XElement child in node.Elements())
            {
                WriteField(child);
            }
            bw.WriteUInt8((byte)BondType.stop);

            // write struct
            var structData = PopWriter().Data;
            if (node.Attribute(BondAttr.compression.ToString()) is XAttribute compressAttr)
            {
                if (type is BondType.@struct)
                {
                    PushWriter();
                    bw.WriteLEB128U((uint)structData.Count);
                    bw.WriteBytes(structData);
                    structData = PopWriter().Data;
                }

                var compressionType = compressAttr.Value.ToEnum<BondCompressionType>();
                var compressedDataStream = new MemoryStream();
                if (compressionType is BondCompressionType.deflate)
                {
                    using var compressor = new DeflaterOutputStream(compressedDataStream);
                    new MemoryStream(structData.ToArray()).CopyTo(compressor);
                }
                else
                {
                    throw new BondWriterException("unknown compression type");
                }

                byte[] compressedData = compressedDataStream.ToArray();
                bw.SetLittleEndian(false);
                bw.WriteInt32(compressedData.Length);
                bw.SetLittleEndian(true);
                bw.WriteBytes(compressedData);
            }
            else
            {
                if (type is BondType.@struct)
                {
                    bw.WriteLEB128U((uint)structData.Count);
                }
                bw.WriteBytes(structData);
            }


            if (node.Attribute(BondAttr.pad.ToString()) is XAttribute nodeAttr)
            {
                int pad = int.Parse(nodeAttr.Value);
                int structSize = bw.Size - origSize;
                int padSize = pad - structSize;
                bw.WriteBytes(new byte[padSize]);
            }
        }

        private void WriteList(XElement node)
        {
            // write type/count
            var type = GetBondType(node, BondAttr.type);
            int count = node.Elements().Count();
            WriteListHeader(type, count);

            // write list
            foreach (XElement child in node.Elements())
            {
                var childType = GetBondRealType(GetBondType(child));

                BondWriterException.Assert(childType == type, "wrong type in list");
                WriteValue(child);
            }
        }

        private void WriteListHeader(BondType type, int count)
        {
            if (count < 7)
            {
                byte countAndType = (byte)(((count + 1) << 5) | (int)type);
                bw.WriteUInt8(countAndType);
            }
            else
            {
                bw.WriteUInt8((byte)type);
                bw.WriteLEB128U((uint)count);
            }
        }

        private void WriteMap(XElement node)
        {
            int count = node.Elements().Count() / 2;
            BondType keyType = GetBondType(node, BondAttr.key_type);
            BondType valueType = GetBondType(node, BondAttr.value_type);

            bw.WriteUInt8((byte)keyType);
            bw.WriteUInt8((byte)valueType);
            bw.WriteLEB128U((ulong)count);

            for (int i = 0; i < node.Elements().Count(); i += 2)
            {
                var keyNode = node.Elements().ElementAt(i);
                var valueNode = node.Elements().ElementAt(i + 1);
                var keyNodeType = GetBondType(keyNode);
                var valueNodeType = GetBondType(valueNode);
                BondWriterException.Assert(keyNodeType == keyType, "wrong key type in map");
                BondWriterException.Assert(valueNodeType == valueType, "wrong value type in map");
                WriteValue(keyNode);
                WriteValue(valueNode);
            }
        }

        private T ParseNumber<T>(string number, Func<string, int, T> parser)
        {
            int nbase = 10;
            if (number.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
            {
                nbase = 16;
            }
            return parser(number, nbase);
        }

        private void WriteBlob(XElement node)
        {
            IList<byte> blob;
            if (node.GetText() == "")
            {
                PushWriter();
                foreach (var child in node.Elements())
                {
                    WriteValue(child);
                }
                blob = PopWriter().Data;
            }
            else
            {
                blob = GetBlob(node.GetText());
            }
            BondType type = GetBondType(node, BondAttr.type);
            WriteListHeader(type, blob.Count);
            bw.WriteBytes(blob);
        }

        private void WriteGuid(XElement node)
        {
            Guid guid = new(node.GetText());

            var br = new MyBinaryReader(guid.ToByteArray(), BitConverter.IsLittleEndian);
            string n1 = br.ReadUInt32().ToString();
            string n2 = br.ReadUInt16().ToString();
            string n3 = br.ReadUInt16().ToString();
            br.SetLittleEndian(true);
            string n4 = br.ReadUInt64().ToString();

            // convert to struct node
            XElement guidNode = new(BondType.@struct.ToString());
            guidNode.ReplaceAttributes(node.Attributes());
            guidNode.Add(
                new XElement(BondType.uint32.ToString(),
                    new XAttribute(BondAttr.id.ToString(), 0),
                    new XText(n1)),
                new XElement(BondType.uint16.ToString(),
                    new XAttribute(BondAttr.id.ToString(), 1),
                    new XText(n2)),
                new XElement(BondType.uint16.ToString(),
                    new XAttribute(BondAttr.id.ToString(), 2),
                    new XText(n3)),
                new XElement(BondType.uint64.ToString(),
                    new XAttribute(BondAttr.id.ToString(), 3),
                    new XText(n4)));

            WriteStruct(guidNode);
        }

        private byte[] GetBlob(string filename)
        {
            if (path != null && !blobs.ContainsKey(filename))
            {
                string blobPath = Path.Combine(path!, filename);
                byte[] blob = File.ReadAllBytes(blobPath);

                blobs[filename] = blob;
                return blob;
            }
            else
            {
                return blobs[filename];
            }
        }

        // helpers

        private void PushWriter()
        {
            bwStack.Push(bw);
            bw = new MyBinaryWriter();
        }

        private MyBinaryWriter PopWriter()
        {
            var ret = bw;
            bw = bwStack.Pop();
            return ret;
        }

        private BondType GetBondType(XElement node)
        {
            BondType type = node.Name.ToString().ToEnum<BondType>()
                ?? throw new BondWriterException();
            return type;
        }

        private BondType GetBondRealType(BondType type)
        {
            switch (type)
            {
                case BondType.guid:
                    return BondType.@struct;
                case BondType.blob:
                    return BondType.list;
                case BondType.root:
                case BondType.struct_base:
                    throw new BondWriterException(type + " is not a real type");
            }
            return type;
        }

        private BondType GetBondType(XElement node, BondAttr attribute)
        {
            BondType type = node.Attribute(attribute.ToString())?.Value.ToEnum<BondType>()
                ?? throw new BondWriterException("Could not get type from attribute " + attribute);
            return type;
        }

        private TEnum GetBondAttribute<TEnum>(XElement node, BondAttr attribute) where TEnum : struct
        {
            string attrStr = node.Attribute(attribute.ToString())?.ToString() ?? throw new BondWriterException("element does not have attribute: " + attribute);
            return Enum.TryParse(attrStr, out TEnum result) ? result : throw new BondWriterException("failed to parse enum: " + attrStr);
        }

        public static string SuggestFilename(string filename)
        {
            if (filename.EndsWith(".xml"))
            {
                return filename[..^4];
            }
            return filename + ".bin";
        }
    }

    public class BondWriterException : Exception
    {
        public BondWriterException() { }
        public BondWriterException(string message) : base(message) { }
        public BondWriterException(string message, Exception innerException) : base(message, innerException) { }
        public static void Assert(bool condition, string message) { if (!condition) throw new BondWriterException(message); }
    }
}
