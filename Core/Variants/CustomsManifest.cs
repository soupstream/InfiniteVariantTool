using InfiniteVariantTool.Core.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace InfiniteVariantTool.Core.Variants
{

    public class CustomsManifest : VariantManifest
    {
        public override VariantMetadataBase Base { get; }
        //public object CustomData { get; }
        public override List<VariantMetadataBase> MapLinks { get; }
        public override List<VariantMetadataBase> UgcGameVariantLinks { get; }
        public List<VariantMetadataBase> PlaylistLinks { get; }
        public override List<VariantMetadataBase> EngineGameVariantLinks { get; }
        public override List<string> Tags { get; }

        // my data
        public override VariantType Type => throw new NotImplementedException();

        public IEnumerable<(VariantType, VariantMetadataBase)> AllLinks =>
            MapLinks.Select(link => (VariantType.MapVariant, link))
            .Concat(UgcGameVariantLinks.Select(link => (VariantType.UgcGameVariant, link)))
            .Concat(EngineGameVariantLinks.Select(link => (VariantType.EngineGameVariant, link)));

        public CustomsManifest()
        {
            Base = new();
            MapLinks = new();
            UgcGameVariantLinks = new();
            PlaylistLinks = new();
            EngineGameVariantLinks = new();
            Tags = new();
        }

        public CustomsManifest(CustomsManifest other)
        {
            Base = new(other.Base);
            MapLinks = other.MapLinks.Select(link => new VariantMetadataBase(link)).ToList();
            UgcGameVariantLinks = other.UgcGameVariantLinks.Select(link => new VariantMetadataBase(link)).ToList();
            PlaylistLinks = other.PlaylistLinks.Select(link => new VariantMetadataBase(link)).ToList();
            EngineGameVariantLinks = other.EngineGameVariantLinks.Select(link => new VariantMetadataBase(link)).ToList();
            Tags = new(other.Tags);
        }

        public CustomsManifest(string filename)
            : this(XElement.Load(filename, LoadOptions.SetLineInfo))
        {

        }

        public CustomsManifest(XElement doc)
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
                d.ReadList(1, BondType.@struct, () =>
                {
                    VariantMetadataBase link = new(false);
                    link.Deserialize(d);
                    MapLinks.Add(link);
                });
                d.ReadList(2, BondType.@struct, () =>
                {
                    VariantMetadataBase link = new(false);
                    link.Deserialize(d);
                    EngineGameVariantLinks.Add(link);
                });
                d.ReadList(3, BondType.@struct, () =>
                {
                    VariantMetadataBase link = new(false);
                    link.Deserialize(d);
                    PlaylistLinks.Add(link);
                });
                d.ReadList(4, BondType.@struct, () =>
                {
                    VariantMetadataBase link = new(false);
                    link.Deserialize(d);
                    UgcGameVariantLinks.Add(link);
                });
                d.ReadList(5, BondType.@string, () =>
                {
                    Tags.Add(d.ReadString());
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
                s.WriteList(BondType.@struct, 1, () =>
                {
                    foreach (var link in MapLinks)
                    {
                        link.Serialize(s);
                    }
                });
                s.WriteList(BondType.@struct, 2, () =>
                {
                    foreach (var link in EngineGameVariantLinks)
                    {
                        link.Serialize(s);
                    }
                });
                s.WriteList(BondType.@struct, 3, () =>
                {
                    foreach (var link in PlaylistLinks)
                    {
                        link.Serialize(s);
                    }
                });
                s.WriteList(BondType.@struct, 4, () =>
                {
                    foreach (var link in UgcGameVariantLinks)
                    {
                        link.Serialize(s);
                    }
                });
                s.WriteList(BondType.@string, 5, () =>
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
