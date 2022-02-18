using System.Xml.Linq;
using InfiniteVariantTool.Core.Serialization;
using InfiniteVariantTool.Core.Utils;

namespace InfiniteVariantTool.Core.Cache
{
    // todo: attribute-based serialization would be nice

    public class CacheMap : BondXmlSerializable
    {
        public OrderedDictionary<ulong, CacheMapEntry> Map { get; set; }
        public sbyte Unk { get; set; }
        public string Language { get; set; }

        public CacheMap()
        {
            Map = new();
            Unk = 2;
            Language = "";
        }

        public CacheMap(XElement doc)
            : this()
        {
            Deserialize(doc);
        }

        public CacheMap(string filename)
            : this()
        {
            BondReader br = new(filename);
            XElement doc = br.Read().Doc;
            Deserialize(doc);
        }

        public new XElement Save(string filename)
        {
            XElement doc = Serialize();
            doc.SaveVersioned(filename);
            return doc;
        }

        public new byte[] Pack()
        {
            XElement doc = Serialize();
            BondWriter bw = new(doc);
            return bw.Write();
        }

        public override void Serialize(BondXmlSerializer s, int? id = null)
        {
            s.WriteStruct(id, () =>
            {
                s.WriteMap(BondType.uint64, BondType.@struct, 0, () =>
                {
                    foreach (var entry in Map)
                    {
                        s.WriteUInt64(entry.Key);
                        entry.Value.Serialize(s);
                    }
                });
                s.WriteInt8(Unk, 1);
                s.WriteString(Language, 2);
            });
        }
        public override void Deserialize(BondXmlDeserializer d, int? id = null)
        {
            OrderedDictionary<ulong, CacheMapEntry> tmpMap = new();
            d.ReadStruct(id, () =>
            {
                d.ReadMap(0, BondType.uint64, BondType.@struct, () =>
                {
                    ulong key = d.ReadUInt64();
                    CacheMapEntry value = new();
                    value.Deserialize(d);
                    tmpMap[key] = value;
                });
                Map = tmpMap;

                Unk = d.ReadInt8(1);
                Language = d.ReadString(2);
            });
        }
    }

    public class CacheMapEntry : BondXmlSerializable
    {
        public long Date1 { get; set; }
        public long Date2 { get; set; }
        public long Date3 { get; set; }
        public CacheFileMetadata Metadata { get; set; }
        public ulong Size { get; set; }

        public CacheMapEntry()
        {
            Date1 = 0;
            Date2 = 0;
            Date3 = 0;
            Metadata = new();
            Size = 0;
        }

        public override void Serialize(BondXmlSerializer s, int? id = null)
        {
            s.WriteStruct(id, () =>
            {
                s.WriteInt64(Date1, 0);
                s.WriteInt64(Date2, 1);
                s.WriteInt64(Date3, 2);
                Metadata.Serialize(s, 3);
                s.WriteUInt64(Size, 4);
            });
        }

        public override void Deserialize(BondXmlDeserializer d, int? id = null)
        {
            CacheFileMetadata tmpMetadata = new();
            d.ReadStruct(id, () =>
            {
                Date1 = d.ReadInt64(0);
                Date2 = d.ReadInt64(1);
                Date3 = d.ReadInt64(2);
                tmpMetadata.Deserialize(d, 3);
                Size = d.ReadUInt64(4);
            });
            Metadata = tmpMetadata;
        }
    }
}
