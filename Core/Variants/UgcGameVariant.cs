using InfiniteVariantTool.Core.Cache;
using InfiniteVariantTool.Core.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace InfiniteVariantTool.Core.Variants
{
    public class UgcGameVariant : Variant
    {
        public override UgcGameVariantMetadata Metadata { get; }
        public EngineGameVariant? LinkedEngineGameVariant { get; set; }

        public UgcGameVariant(UgcGameVariantMetadata metadata, Dictionary<string, CacheFile> files)
            : base(VariantType.UgcGameVariant, files)
        {
            Metadata = metadata;
        }

        public UgcGameVariant(string metadataFilename)
            : base(VariantType.UgcGameVariant, new())
        {
            Metadata = new UgcGameVariantMetadata(XElement.Load(metadataFilename));
            LoadFiles(Path.GetDirectoryName(metadataFilename)!);
        }

        public override void Save(string path)
        {
            if (LinkedEngineGameVariant == null)
            {
                base.Save(path);
            }
            else
            {
                base.Save(Path.Combine(path, "ugc_game_variant"));
                LinkedEngineGameVariant.UnpackBondContent = UnpackBondContent;
                LinkedEngineGameVariant.UnpackLuaBundles = UnpackLuaBundles;
                LinkedEngineGameVariant.Save(Path.Combine(path, "engine_game_variant"));
            }
        }

        public override void GenerateAssetId()
        {
            base.GenerateAssetId();
            if (LinkedEngineGameVariant != null && Metadata.EngineGameVariantLink != null)
            {
                Metadata.EngineGameVariantLink.GenerateAssetId();
                LinkedEngineGameVariant.Metadata.Base.SetAssetId(Metadata.EngineGameVariantLink.AssetId);
            }
        }

        public override void GenerateVersionId()
        {
            base.GenerateVersionId();
            if (LinkedEngineGameVariant != null && Metadata.EngineGameVariantLink != null)
            {
                Metadata.EngineGameVariantLink.GenerateVersionId();
                LinkedEngineGameVariant.Metadata.Base.SetVersionId(Metadata.EngineGameVariantLink.VersionId);
            }
        }

        public override string Url => $"https://discovery-infiniteugc-intone.test.svc.halowaypoint.com/hi/ugcGameVariants/{Metadata.Base.AssetId}/versions/{Metadata.Base.VersionId}";
        public override string MetadataFilename => MetadataFilenameStatic;
        public const string MetadataFilenameStatic = "ugc_game_variant.xml";
    }

    public class UgcGameVariantMetadata : VariantMetadata
    {
        public override VariantMetadataBase Base { get; }

        // CustomData
        //public object KeyValues { get; set; }

        public override List<string> Tags { get; }
        public VariantMetadataBase? EngineGameVariantLink { get; set; }

        // my data
        public override VariantType Type => VariantType.UgcGameVariant;

        public UgcGameVariantMetadata()
        {
            Base = new();
            Tags = new();
        }

        public UgcGameVariantMetadata(XElement doc)
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
                d.ReadStruct(0);
                d.ReadList(1, BondType.wstring, () =>
                {
                    Tags.Add(d.ReadWString());
                });
                d.ReadList(2, BondType.@struct, () =>
                {
                    EngineGameVariantLink = new(false);
                    EngineGameVariantLink.Deserialize(d);
                });
            });
        }

        public override void Serialize(BondXmlSerializer s, int? id = null)
        {
            s.IgnoreDefaultValues = true;
            s.WriteStruct(null, () =>
            {
                Base.Serialize(s);
                s.WriteStruct(0);
                s.WriteList(BondType.wstring, 1, () =>
                {
                    foreach (string tag in Tags)
                    {
                        s.WriteWString(tag);
                    }
                });
                s.WriteList(BondType.@struct, 2, () =>
                {
                    EngineGameVariantLink?.Serialize(s);
                });
            });
        }
    }

}
