using InfiniteVariantTool.Core.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace InfiniteVariantTool.Core.Variants
{
    public class GameManifest : VariantManifest
    {
        public override VariantMetadataBase Base { get; }

        // CustomData
        public string BranchName { get; set; }
        public string BuildNumber { get; set; }
        public int Kind { get; set; }
        public string ContentVersion { get; set; }
        public Guid? BuildGuid { get; set; }

        public override List<string> Tags { get; }
        public override List<VariantMetadataBase> MapLinks { get; }
        public override List<VariantMetadataBase> UgcGameVariantLinks { get; }
        public List<VariantMetadataBase> PlaylistLinks { get; }
        public override List<VariantMetadataBase> EngineGameVariantLinks { get; }

        // my data
        public override VariantType Type => throw new NotImplementedException();

        public GameManifest()
        {
            Base = new();
            BranchName = "";
            BuildNumber = "";
            ContentVersion = "";
            Tags = new();
            MapLinks = new();
            UgcGameVariantLinks = new();
            PlaylistLinks = new();
            EngineGameVariantLinks = new();
        }

        public GameManifest(GameManifest other)
        {
            Base = new(other.Base);
            BranchName = other.BranchName;
            BuildNumber = other.BuildNumber;
            Kind = other.Kind;
            ContentVersion = other.ContentVersion;
            BuildGuid = other.BuildGuid;
            Tags = new(other.Tags);
            MapLinks = other.MapLinks.Select(link => new VariantMetadataBase(link)).ToList();
            UgcGameVariantLinks = other.UgcGameVariantLinks.Select(link => new VariantMetadataBase(link)).ToList();
            PlaylistLinks = other.PlaylistLinks.Select(link => new VariantMetadataBase(link)).ToList();
            EngineGameVariantLinks = other.EngineGameVariantLinks.Select(link => new VariantMetadataBase(link)).ToList();
        }

        public GameManifest(string filename)
            : this(XElement.Load(filename, LoadOptions.SetLineInfo))
        {

        }

        public GameManifest(XElement doc)
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
                    BranchName = d.ReadString(0);
                    BuildNumber = d.ReadString(1);
                    Kind = d.ReadInt32(2);
                    ContentVersion = d.ReadString(3);
                    if (d.GetNodeType() != null)
                    {
                        BuildGuid = d.ReadGuid(4);
                    }
                });
                d.ReadList(1, BondType.@string, () =>
                {
                    Tags.Add(d.ReadString());
                });
                d.ReadList(2, BondType.@struct, () =>
                {
                    VariantMetadataBase link = new(false);
                    link.Deserialize(d);
                    MapLinks.Add(link);
                });
                d.ReadList(3, BondType.@struct, () =>
                {
                    VariantMetadataBase link = new(false);
                    link.Deserialize(d);
                    UgcGameVariantLinks.Add(link);
                });
                d.ReadList(4, BondType.@struct, () =>
                {
                    VariantMetadataBase link = new(false);
                    link.Deserialize(d);
                    PlaylistLinks.Add(link);
                });
                d.ReadList(5, BondType.@struct, () =>
                {
                    VariantMetadataBase link = new(false);
                    link.Deserialize(d);
                    EngineGameVariantLinks.Add(link);
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
                    s.WriteString(BranchName, 0);
                    s.WriteString(BuildNumber, 1);
                    s.WriteInt32(Kind, 2);
                    s.WriteString(ContentVersion, 3);
                    if (BuildGuid != null)
                    {
                        s.WriteGuid(BuildGuid.Value, 4);
                    }
                });
                s.WriteList(BondType.@string, 1, () =>
                {
                    foreach (string tag in Tags)
                    {
                        s.WriteWString(tag);
                    }
                });
                s.WriteList(BondType.@struct, 2, () =>
                {
                    foreach (var link in MapLinks)
                    {
                        link.Serialize(s);
                    }
                });
                s.WriteList(BondType.@struct, 3, () =>
                {
                    foreach (var link in UgcGameVariantLinks)
                    {
                        link.Serialize(s);
                    }
                });
                s.WriteList(BondType.@struct, 4, () =>
                {
                    foreach (var link in PlaylistLinks)
                    {
                        link.Serialize(s);
                    }
                });
                s.WriteList(BondType.@struct, 5, () =>
                {
                    foreach (var link in EngineGameVariantLinks)
                    {
                        link.Serialize(s);
                    }
                });
            });
        }
    }
}
