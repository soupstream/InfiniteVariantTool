using InfiniteVariantTool.Core.Cache;
using InfiniteVariantTool.Core.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace InfiniteVariantTool.Core.Variants
{
    public class EngineGameVariant : Variant
    {
        public override EngineGameVariantMetadata Metadata { get; }

        public EngineGameVariant(EngineGameVariantMetadata metadata, Dictionary<string, CacheFile> files)
            : base(VariantType.EngineGameVariant, files)
        {
            Metadata = metadata;
        }

        public EngineGameVariant(string metadataFilename)
            : base(VariantType.EngineGameVariant, new())
        {
            Metadata = new EngineGameVariantMetadata(XElement.Load(metadataFilename));
            LoadFiles(Path.GetDirectoryName(metadataFilename)!);
        }

        public override string Url => $"https://discovery-infiniteugc-intone.test.svc.halowaypoint.com/hi/engineGameVariants/{Metadata.Base.AssetId}/versions/{Metadata.Base.VersionId}";
        public override string MetadataFilename => MetadataFilenameStatic;
        public const string MetadataFilenameStatic = "engine_game_variant.xml";
    }

    public class EngineGameVariantMetadata : VariantMetadata
    {
        public override VariantMetadataBase Base { get; }

        // CustomData
        //// SubsetData
        public int StatBucketGameType { get; set; }
        public string EngineName { get; set; }
        public string VariantName { get; set; }
        //public object LocalizedData { get; set; }

        public override List<string> Tags { get; }

        // my data
        public override VariantType Type => VariantType.EngineGameVariant;

        public EngineGameVariantMetadata()
        {
            Base = new();
            EngineName = "";
            VariantName = "";
            Tags = new();
        }

        public EngineGameVariantMetadata(XElement doc)
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
                    d.ReadStruct(0, () =>
                    {
                        StatBucketGameType = d.ReadInt32(0);
                        EngineName = d.ReadString(1);
                        VariantName = d.ReadString(2);
                    });
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
                    s.WriteStruct(0, () =>
                    {
                        s.WriteInt32(StatBucketGameType, 0);
                        s.WriteString(EngineName, 1);
                        s.WriteString(VariantName, 2);
                    });
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
