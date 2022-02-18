using InfiniteVariantTool.Core.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace InfiniteVariantTool.Core.Variants
{
    public abstract class VariantMetadata : BondXmlSerializable
    {
        public abstract VariantMetadataBase Base { get; }
        public abstract List<string> Tags { get; }
        public abstract VariantType Type { get; }

        public static VariantMetadata FromXml(VariantType type, XElement node)
        {
            return type switch
            {
                VariantType.MapVariant => new MapVariantMetadata(node),
                VariantType.UgcGameVariant => new UgcGameVariantMetadata(node),
                VariantType.EngineGameVariant => new EngineGameVariantMetadata(node),
                _ => throw new ArgumentException()
            };
        }

        public static VariantMetadata? TryLoadMetadata(string filepath)
        {
            string filename = Path.GetFileName(filepath);
            if (filename == MapVariant.MetadataFilenameStatic)
            {
                return new MapVariantMetadata(XElement.Load(filepath));
            }
            else if (filename == UgcGameVariant.MetadataFilenameStatic)
            {
                return new UgcGameVariantMetadata(XElement.Load(filepath));
            }
            else if (filename == EngineGameVariant.MetadataFilenameStatic)
            {
                return new EngineGameVariantMetadata(XElement.Load(filepath));
            }
            else
            {
                return null;
            }
        }
    }

    public class VariantMetadataBase : BondXmlSerializable
    {
        public Guid AssetId { get; set; }
        public Guid VersionId { get; set; }
        public string PublicName { get; set; }
        public string Description { get; set; }

        // Files
        public string Prefix { get; set; }
        public List<string> FileRelativePaths { get; }

        //// PrefixEndpoint
        public string AuthorityId { get; set; }
        public string Path { get; set; }
        public string QueryString { get; set; }
        public string RetryPolicyId { get; set; }
        public string TopicName { get; set; }
        public int AcknowledgementTypeId { get; set; }
        public bool AuthenticationLifetimeExtensionSupported { get; set; }
        public bool ClearanceAware { get; set; }

        public List<string> Contributors { get; }
        public int AssetHome { get; set; }

        // AssetStats
        public long PlaysRecent { get; set; }
        public long PlaysAllTime { get; set; }

        public int InspectionResult { get; set; }
        public int CloneBehavior { get; set; }

        // my data
        public bool IsBaseStruct { get; set; }

        public void SetAssetId(Guid newAssetId)
        {
            Guid oldAssetId = AssetId;
            AssetId = newAssetId;
            Prefix = Prefix.Replace(oldAssetId.ToString(), newAssetId.ToString());
            Path = Path.Replace(oldAssetId.ToString(), newAssetId.ToString());
        }

        public void SetVersionId(Guid newVersionId)
        {
            Guid oldVersionId = VersionId;
            VersionId = newVersionId;
            Prefix = Prefix.Replace(oldVersionId.ToString(), newVersionId.ToString());
            Path = Path.Replace(oldVersionId.ToString(), newVersionId.ToString());
        }

        public void GenerateAssetId()
        {
            SetAssetId(Guid.NewGuid());
        }

        public void GenerateVersionId()
        {
            SetVersionId(Guid.NewGuid());
        }

        public VariantMetadataBase()
        {
            PublicName = "";
            Description = "";
            Prefix = "";
            FileRelativePaths = new();
            AuthorityId = "";
            Path = "";
            QueryString = "";
            RetryPolicyId = "";
            TopicName = "";
            Contributors = new();
            IsBaseStruct = true;
        }

        public VariantMetadataBase(VariantMetadataBase other)
        {
            AssetId = other.AssetId;
            VersionId = other.VersionId;
            PublicName = other.PublicName;
            Description = other.Description;
            Prefix = other.Prefix;
            FileRelativePaths = new(other.FileRelativePaths);
            AuthorityId = other.AuthorityId;
            Path = other.Path;
            QueryString = other.QueryString;
            RetryPolicyId = other.RetryPolicyId;
            TopicName = other.TopicName;
            AcknowledgementTypeId = other.AcknowledgementTypeId;
            AuthenticationLifetimeExtensionSupported = other.AuthenticationLifetimeExtensionSupported;
            ClearanceAware = other.ClearanceAware;
            Contributors = new(other.Contributors);
            AssetHome = other.AssetHome;
            PlaysRecent = other.PlaysRecent;
            PlaysAllTime = other.PlaysAllTime;
            InspectionResult = other.InspectionResult;
            CloneBehavior = other.CloneBehavior;
            IsBaseStruct = other.IsBaseStruct;
        }

        public VariantMetadataBase(bool baseStruct)
            : this()
        {
            IsBaseStruct = baseStruct;
        }

        public override void Deserialize(BondXmlDeserializer d, int? id = null)
        {
            d.UseDefaultValues = true;
            d.ReadStruct(id, () =>
            {
                AssetId = d.ReadGuid(0);
                VersionId = d.ReadGuid(1);
                PublicName = d.ReadWString(2);
                Description = d.ReadWString(3);
                d.ReadStruct(4, () =>
                {
                    Prefix = d.ReadString(0);
                    d.ReadList(1, BondType.@string, () =>
                    {
                        FileRelativePaths.Add(d.ReadString());
                    });
                    d.ReadStruct(2, () =>
                    {
                        AuthorityId = d.ReadString(0);
                        Path = d.ReadString(1);
                        QueryString = d.ReadString(2);
                        RetryPolicyId = d.ReadString(3);
                        TopicName = d.ReadString(4);
                        AcknowledgementTypeId = d.ReadInt32(5);
                        AuthenticationLifetimeExtensionSupported = d.ReadBool(6);
                        ClearanceAware = d.ReadBool(7);
                    });
                });
                d.ReadSet(5, BondType.@string, () =>
                {
                    Contributors.Add(d.ReadString());
                });
                AssetHome = d.ReadInt32(6);
                d.ReadStruct(7, () =>
                {
                    PlaysRecent = d.ReadInt64(0);
                    PlaysAllTime = d.ReadInt64(1);
                });
                InspectionResult = d.ReadInt32(8);
                CloneBehavior = d.ReadInt32(9);
            }, IsBaseStruct ? BondType.struct_base : BondType.@struct);
        }

        public override void Serialize(BondXmlSerializer s, int? id = null)
        {
            s.IgnoreDefaultValues = true;
            s.WriteStruct(null, () =>
            {
                s.WriteGuid(AssetId, 0);
                s.WriteGuid(VersionId, 1);
                s.WriteWString(PublicName, 2);
                s.WriteWString(Description, 3);
                s.WriteStruct(4, () =>
                {
                    s.WriteString(Prefix, 0);
                    s.WriteList(BondType.@string, 1, () =>
                    {
                        foreach (string path in FileRelativePaths)
                        {
                            s.WriteString(path);
                        }
                    });
                    s.WriteStruct(2, () =>
                    {
                        s.WriteString(AuthorityId, 0);
                        s.WriteString(Path, 1);
                        s.WriteString(QueryString, 2);
                        s.WriteString(RetryPolicyId, 3);
                        s.WriteString(TopicName, 4);
                        s.WriteInt32(AcknowledgementTypeId, 5);
                        s.WriteBool(AuthenticationLifetimeExtensionSupported, 6);
                        s.WriteBool(ClearanceAware, 7);
                    });
                });
                s.WriteSet(BondType.@string, 5, () =>
                {
                    foreach (string contributor in Contributors)
                    {
                        s.WriteString(contributor);
                    }
                });
                s.WriteInt32(AssetHome, 6);
                s.WriteStruct(7, () =>
                {
                    s.WriteInt64(PlaysRecent, 0);
                    s.WriteInt64(PlaysAllTime, 1);
                });
                s.WriteInt32(InspectionResult, 8);
                s.WriteInt32(CloneBehavior, 9);
            }, IsBaseStruct ? BondType.struct_base : BondType.@struct);
        }
    }
}
