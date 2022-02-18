using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using InfiniteVariantTool.Core.Utils;

namespace InfiniteVariantTool.Core.Serialization
{
    public interface IBondXmlSerializable
    {
        void Serialize(BondXmlSerializer serializer, int? id);
        void Deserialize(BondXmlDeserializer deserializer, int? id);
        XElement Serialize();
        void Deserialize(XElement node);
    }

    public abstract class BondXmlSerializable : IBondXmlSerializable
    {
        public abstract void Deserialize(BondXmlDeserializer deserializer, int? id = null);
        public abstract void Serialize(BondXmlSerializer serializer, int? id = null);

        public void Deserialize(XElement node)
        {
            Deserialize(new BondXmlDeserializer(node));
        }

        public XElement Serialize()
        {
            BondXmlSerializer serializer = new();
            Serialize(serializer);
            return serializer.Doc;
        }

        public XElement Save(string filename)
        {
            var doc = Serialize();
            doc.SaveVersioned(filename);
            return doc;
        }

        public byte[] Pack()
        {
            var doc = Serialize();
            BondWriter bw = new(doc);
            return bw.Write();
        }

        public byte[] Pack(string filename)
        {
            byte[] data = Pack();
            File.WriteAllBytes(filename, data);
            return data;
        }
    }

    public class BondXmlSerializer
    {
        private XElement? doc;
        private Stack<XElement> stack;
        private XElement parent => stack.Peek();
        public bool IgnoreDefaultValues { get; set; } = false;

        public BondXmlSerializer()
        {
            stack = new();
        }

        public XElement Doc => doc ?? throw new Exception("No root node");

        public void WriteAttribute(BondAttr attribute, string value)
        {
            parent.SetAttributeValue(attribute.ToString(), value);
        }

        public void WriteAttribute(BondAttr attribute, int value)
        {
            WriteAttribute(attribute, value.ToString());
        }

        public void WriteAttribute(BondAttr attribute, BondType value)
        {
            WriteAttribute(attribute, value.ToString());
        }

        private void WriteElement(int? id, XElement element, string? value, Action? body)
        {
            stack.Push(element);

            if (id != null)
            {
                WriteAttribute(BondAttr.id, id.Value.ToString());
            }

            if (value != null)
            {
                parent.Add(new XText(value));
            }

            body?.Invoke();

            stack.Pop();

            if (stack.Count > 0)
            {
                parent.Add(element);
            }
            else
            {
                doc = element;
            }
        }

        private XElement WriteElement(int? id, BondType type, string? value, Action? body)
        {
            var element = new XElement(type.ToString());
            WriteElement(id, element, value, body);
            return element;
        }

        public void WriteNode(XElement node, int? id = null, Action? body = null)
        {
            WriteElement(id, node, null, body);
        }

        public void WriteBool(bool value, int? id = null, Action? body = null)
        {
            if (IgnoreDefaultValues && id != null && value == false) return;
            WriteElement(id, BondType.@bool, value.ToString().ToLower(), body);
        }

        public void WriteString(string value, int? id = null, Action? body = null)
        {
            if (IgnoreDefaultValues && id != null && value == "") return;
            WriteElement(id, BondType.@string, value, body);
        }

        public void WriteWString(string value, int? id = null, Action? body = null)
        {
            if (IgnoreDefaultValues && id != null && value == "") return;
            WriteElement(id, BondType.wstring, value, body);
        }

        public void WriteUInt8(byte value, int? id = null, Action? body = null)
        {
            if (IgnoreDefaultValues && id != null && value == 0) return;
            WriteElement(id, BondType.uint8, value.ToString(), body);
        }

        public void WriteInt8(sbyte value, int? id = null, Action? body = null)
        {
            if (IgnoreDefaultValues && id != null && value == 0) return;
            WriteElement(id, BondType.int8, value.ToString(), body);
        }

        public void WriteUInt16(ushort value, int? id = null, Action? body = null)
        {
            if (IgnoreDefaultValues && id != null && value == 0) return;
            WriteElement(id, BondType.uint16, value.ToString(), body);
        }

        public void WriteInt16(short value, int? id = null, Action? body = null)
        {
            if (IgnoreDefaultValues && id != null && value == 0) return;
            WriteElement(id, BondType.int16, value.ToString(), body);
        }

        public void WriteUInt32(uint value, int? id = null, Action? body = null)
        {
            if (IgnoreDefaultValues && id != null && value == 0) return;
            WriteElement(id, BondType.uint32, value.ToString(), body);
        }

        public void WriteInt32(int value, int? id = null, Action? body = null)
        {
            if (IgnoreDefaultValues && id != null && value == 0) return;
            WriteElement(id, BondType.int32, value.ToString(), body);
        }

        public void WriteUInt64(ulong value, int? id = null, Action? body = null)
        {
            if (IgnoreDefaultValues && id != null && value == 0) return;
            WriteElement(id, BondType.uint64, value.ToString(), body);
        }

        public void WriteInt64(long value, int? id = null, Action? body = null)
        {
            if (IgnoreDefaultValues && id != null && value == 0) return;
            WriteElement(id, BondType.int64, value.ToString(), body);
        }

        public void WriteFloat(float value, int? id = null, Action? body = null)
        {
            if (IgnoreDefaultValues && id != null && value == 0) return;
            WriteElement(id, BondType.@float, value.ToString(), body);
        }

        public void WriteDouble(double value, int? id = null, Action? body = null)
        {
            if (IgnoreDefaultValues && id != null && value == 0) return;
            WriteElement(id, BondType.@double, value.ToString(), body);
        }

        public void WriteStruct(int? id = null, Action? body = null, BondType structType = BondType.@struct)
        {
            WriteElement(id, structType, null, body);
        }

        public void WriteStructBase(Action? body = null)
        {
            WriteStruct(null, body, BondType.struct_base);
        }

        public void WriteList(BondType type, int? id = null, Action? body = null, BondType listType = BondType.list)
        {
            XElement element = WriteElement(id, listType, null, () =>
            {
                WriteAttribute(BondAttr.type, type);
                body?.Invoke();
            });
            if (IgnoreDefaultValues && id != null && !element.HasElements) element.Remove();
        }

        public void WriteSet(BondType type, int? id = null, Action? body = null)
        {
            WriteList(type, id, body, BondType.set);
        }

        public void WriteMap(BondType keyType, BondType valueType, int? id = null, Action? body = null)
        {
            XElement element = WriteElement(id, BondType.map, null, () =>
            {
                WriteAttribute(BondAttr.key_type, keyType);
                WriteAttribute(BondAttr.value_type, valueType);
                body?.Invoke();
            });
            if (IgnoreDefaultValues && id != null && !element.HasElements) element.Remove();
        }

        public void WriteGuid(Guid value, int? id = null, Action? body = null)
        {
            if (IgnoreDefaultValues && id != null && value == new Guid())
            {
                WriteStruct(id);
                return;
            }
            WriteElement(id, BondType.guid, value.ToString(), body);
        }

        public XElement WriteBlob(string filename, BondType elementType, int? id = null, Action? body = null)
        {
            return WriteElement(id, BondType.blob, filename, () =>
            {
                WriteAttribute(BondAttr.type, elementType);
                body?.Invoke();
            });
        }

        public XElement WriteBlob(XElement content, BondType elementType, int? id = null, Action? body = null)
        {
            return WriteElement(id, BondType.blob, null, () =>
            {
                WriteAttribute(BondAttr.type, elementType);
                body?.Invoke();
                WriteElement(null, content, null, null);
            });
        }
    }

    public class BondXmlDeserializer
    {
        private Stack<IEnumerator<XElement>> stack;
        private XElement current => stack.Peek().Current;
        private bool endOfElement = false;
        public bool UseDefaultValues { get; set; } = false;

        public BondXmlDeserializer(XElement doc)
        {
            stack = new();
            stack.Push(new List<XElement>() { doc }.GetEnumerator());
            Next();
        }

        private bool Next()
        {
            endOfElement = !stack.Peek().MoveNext();
            return endOfElement;
        }

        private bool ExpectId(int? id)
        {
            if (endOfElement)
            {
                if (UseDefaultValues) return false;
                throw new BondXmlException(current, string.Format("Expected id '{0}', got end of element", id));
            }
            if (GetNodeId() != id)
            {
                if (UseDefaultValues) return false;
                throw new BondXmlException(current, string.Format("Expected id '{0}', got '{1}'", id, GetNodeId()));
            }
            return true;
        }

        private void ExpectType(params BondType[] type)
        {
            if (endOfElement)
            {
                throw new BondXmlException(current, string.Format("Expected type '{0}', got end of element", type));
            }
            if (!type.Any(t => t.ToString() == current.Name))
            {
                throw new BondXmlException(current, string.Format("Expected type '{0}', got '{1}'", string.Join('|', type), current.Name));
            }
        }

        private XElement ReadElement(int? id, params BondType[] type)
        {
            ExpectId(id);
            ExpectType(type);
            XElement ret = current;
            Next();
            return ret;
        }

        private void ReadElement(int? id, BondType type, Action body)
        {
            ExpectId(id);
            ExpectType(type);
            stack.Push(current.Elements().GetEnumerator());
            Next();

            body();

            if (!endOfElement)
            {
                throw new BondXmlException(current, string.Format("Expected end of element, got type '{0}'", current.Name));
            }
            stack.Pop();
            Next();
        }

        private string ReadContent(int? id, BondType type)
        {
            return ReadElement(id, type).GetText();
        }

        private string ReadAttribute(string attribute)
        {
            return current.Attribute(attribute)?.Value ?? throw new BondXmlException(current, string.Format("Attribute '{0}' not found", attribute));
        }

        public void ExpectAttribute(BondAttr attribute, string value)
        {
            if (ReadAttribute(attribute.ToString()) != value)
            {
                throw new BondXmlException(current, string.Format("Expected attribute '{0}={1}', got '{0}={2}'", attribute, value, ReadAttribute(attribute.ToString())));
            }
        }

        public void ExpectAttribute(BondAttr attribute, BondType value)
        {
            ExpectAttribute(attribute, value.ToString());
        }

        public int? GetNodeId()
        {
            if (!endOfElement && current.Attribute("id") != null)
            {
                return int.Parse(ReadAttribute("id"));
            }
            return null;
        }

        public bool IsNodeId(int id)
        {
            return GetNodeId() == id;
        }

        public string? GetNodeType()
        {
            return endOfElement ? null : current.Name.ToString();
        }

        public string ReadString(int? id = null)
        {
            if (!ExpectId(id)) return "";
            return ReadContent(id, BondType.@string);
        }

        public string ReadWString(int? id = null)
        {
            if (!ExpectId(id)) return "";
            return ReadContent(id, BondType.wstring);
        }

        public bool ReadBool(int? id = null)
        {
            if (!ExpectId(id)) return false;
            return bool.Parse(ReadContent(id, BondType.@bool));
        }

        public byte ReadUInt8(int? id = null)
        {
            if (!ExpectId(id)) return 0;
            return byte.Parse(ReadContent(id, BondType.uint8));
        }

        public sbyte ReadInt8(int? id = null)
        {
            if (!ExpectId(id)) return 0;
            return sbyte.Parse(ReadContent(id, BondType.int8));
        }

        public ushort ReadUInt16(int? id = null)
        {
            if (!ExpectId(id)) return 0;
            return ushort.Parse(ReadContent(id, BondType.uint16));
        }

        public short ReadInt16(int? id = null)
        {
            if (!ExpectId(id)) return 0;
            return short.Parse(ReadContent(id, BondType.int16));
        }

        public uint ReadUInt32(int? id = null)
        {
            if (!ExpectId(id)) return 0;
            return uint.Parse(ReadContent(id, BondType.uint32));
        }

        public int ReadInt32(int? id = null)
        {
            if (!ExpectId(id)) return 0;
            return int.Parse(ReadContent(id, BondType.int32));
        }

        public ulong ReadUInt64(int? id = null)
        {
            if (!ExpectId(id)) return 0;
            return ulong.Parse(ReadContent(id, BondType.uint64));
        }

        public long ReadInt64(int? id = null)
        {
            if (!ExpectId(id)) return 0;
            return long.Parse(ReadContent(id, BondType.int64));
        }

        public float ReadFloat(int? id = null)
        {
            if (!ExpectId(id)) return 0;
            return float.Parse(ReadContent(id, BondType.@float));
        }

        public double ReadDouble(int? id = null)
        {
            if (!ExpectId(id)) return 0;
            return double.Parse(ReadContent(id, BondType.@double));
        }

        public void ReadStruct(int? id = null, Action? body = null, BondType structType = BondType.@struct)
        {
            ReadElement(id, structType, () =>
            {
                body?.Invoke();
            });
        }

        public void ReadStructBase(Action? body = null)
        {
            ReadStruct(null, body, BondType.struct_base);
        }

        public void ReadMap(int? id, BondType keyType, BondType valueType, Action? body)
        {
            if (!ExpectId(id)) return;
            ExpectAttribute(BondAttr.key_type, keyType);
            ExpectAttribute(BondAttr.value_type, valueType);
            ReadElement(id, BondType.map, () =>
            {
                if (body != null)
                {
                    while (!endOfElement)
                    {
                        body();
                    }
                }
            });
        }

        public void ReadList(int? id, BondType type, Action? body, BondType listType = BondType.list)
        {
            if (!ExpectId(id)) return;
            ExpectAttribute(BondAttr.type, type);
            ReadElement(id, listType, () =>
            {
                if (body != null)
                {
                    while (!endOfElement)
                    {
                        body();
                    }
                }
            });
        }

        public void ReadSet(int? id, BondType type, Action? body)
        {
            ReadList(id, type, body, BondType.set);
        }

        public XElement ReadBlob(int? id, BondType type)
        {
            ExpectAttribute(BondAttr.type, type);
            return ReadElement(id, BondType.blob, BondType.list);
        }

        public Guid ReadGuid(int? id)
        {
            if (current.Name == BondType.@struct.ToString())
            {
                Guid? guid = null;
                ReadStruct(id, () =>
                {
                    uint n1 = ReadUInt32(0);
                    ushort n2 = ReadUInt16(1);
                    ushort n3 = ReadUInt16(2);
                    ulong n4 = ReadUInt64(3);

                    byte[] n4Bytes = new byte[sizeof(ulong)];
                    BinaryPrimitives.WriteUInt64LittleEndian(n4Bytes, n4);
                    guid = new Guid((int)n1, (short)n2, (short)n3, n4Bytes);
                });
                return guid!.Value;
            }
            else
            {
                string guidStr = ReadContent(id, BondType.guid);
                return new Guid(guidStr);
            }
        }
    }

    public class BondXmlException : Exception
    {
        public BondXmlException(XElement element, string msg)
            : base(AddLineInfo(element, msg))
        {

        }

        private static string AddLineInfo(XElement element, string msg)
        {
            int line = (element as IXmlLineInfo)?.LineNumber ?? -1;
            int col = (element as IXmlLineInfo)?.LinePosition ?? -1;
            return string.Format("{0} at line {1}, position {2}", msg, line, col);
        }
    }
}
