using InfiniteVariantTool.Core.Cache;
using InfiniteVariantTool.Core.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace InfiniteVariantTool.Core.Variants
{
    public class MapVariant : Variant
    {
        public override MapVariantMetadata Metadata { get; }

        public MapVariant(MapVariantMetadata metadata, Dictionary<string, CacheFile> files)
            : base(VariantType.MapVariant, files)
        {
            Metadata = metadata;
        }

        public MapVariant(string metadataFilename)
            : base(VariantType.MapVariant, new())
        {
            Metadata = new MapVariantMetadata(XElement.Load(metadataFilename));
            LoadFiles(Path.GetDirectoryName(metadataFilename)!);
        }

        public override string Url => $"https://discovery-infiniteugc-intone.test.svc.halowaypoint.com/hi/maps/{Metadata.Base.AssetId}/versions/{Metadata.Base.VersionId}";
        public override string MetadataFilename => MetadataFilenameStatic;
        public const string MetadataFilenameStatic = "map_variant.xml";
    }


    public class MapVariantMetadata : VariantMetadata
    {
        public override VariantMetadataBase Base { get; }

        // CustomData
        public int NumOfObjectsOnMap { get; set; }
        public int TagLevelId { get; set; }
        public bool IsBaked { get; set; }

        public override List<string> Tags { get; }

        // my data
        public override VariantType Type => VariantType.MapVariant;

        public MapVariantMetadata()
        {
            Base = new();
            Tags = new();
        }

        public MapVariantMetadata(XElement doc)
            : this()
        {
            Deserialize(doc);
        }

        public override void Deserialize(BondXmlDeserializer d, int? id = null)
        {
            d.UseDefaultValues = true;
            d.ReadStruct(null, () =>
            {
                Base.Deserialize(d);
                d.ReadStruct(0, () =>
                {
                    NumOfObjectsOnMap = d.ReadInt32(0);
                    TagLevelId = d.ReadInt32(1);
                    IsBaked = d.ReadBool(2);
                });
                d.ReadList(1, BondType.wstring, () =>
                {
                    Tags.Add(d.ReadWString());
                });
            });
        }

        public override void Serialize(BondXmlSerializer s, int? id = null)
        {
            s.IgnoreDefaultValues = true;
            s.WriteStruct(null, () =>
            {
                Base.Serialize(s);
                s.WriteStruct(0, () =>
                {
                    s.WriteInt32(NumOfObjectsOnMap, 0);
                    s.WriteInt32(TagLevelId, 1);
                    s.WriteBool(IsBaked, 2);
                });
                s.WriteList(BondType.wstring, 1, () =>
                {
                    foreach (string tag in Tags)
                    {
                        s.WriteWString(tag);
                    }
                });
            });
        }
    }
}
