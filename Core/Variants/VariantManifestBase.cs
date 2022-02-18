using System;
using System.Collections.Generic;
using System.Linq;

namespace InfiniteVariantTool.Core.Variants
{
    public class VariantEntry
    {
        public VariantType Type { get; set; }
        public VariantMetadataBase Metadata { get; set; }
        public bool? Enabled { get; set; }
        public VariantEntry(VariantType type, VariantMetadataBase metadata, bool? enabled = null)
        {
            Type = type;
            Metadata = metadata;
            Enabled = enabled;
        }
    }

    public abstract class VariantManifest : VariantMetadata
    {
        public abstract List<VariantMetadataBase> MapLinks { get; }
        public abstract List<VariantMetadataBase> UgcGameVariantLinks { get; }
        public abstract List<VariantMetadataBase> EngineGameVariantLinks { get; }

        public IEnumerable<VariantEntry> AllVariants =>
            MapLinks.Select(link => new VariantEntry(VariantType.MapVariant, link))
            .Concat(UgcGameVariantLinks.Select(link => new VariantEntry(VariantType.UgcGameVariant, link)))
            .Concat(EngineGameVariantLinks.Select(link => new VariantEntry(VariantType.EngineGameVariant, link)));

        public IEnumerable<VariantEntry> GetVariants(Guid? assetId, Guid? versionId, VariantType? variantType, string? name)
        {
            return AllVariants.Where(entry =>
                (variantType == null || entry.Type == variantType)
                && (assetId == null || assetId == entry.Metadata.AssetId)
                && (versionId == null || versionId == entry.Metadata.VersionId)
                && (name == null || name == entry.Metadata.PublicName));
        }

        public void AddVariant(VariantEntry entry)
        {
            AddVariant(entry.Type, entry.Metadata);
        }

        public void AddVariant(VariantType type, VariantMetadataBase metadata)
        {
            var variantList = GetVariantList(type);
            var match = GetVariants(metadata.AssetId, metadata.VersionId, type, null).FirstOrDefault();
            if (match != null)
            {
                int index = variantList.IndexOf(match.Metadata);
                variantList[index] = metadata;
            }
            else
            {
                variantList.Add(metadata);
            }
        }

        public bool RemoveVariant(Guid? assetId, Guid? versionId, VariantType? variantType, string? name)
        {
            bool removed = false;
            foreach (VariantEntry entry in GetVariants(assetId, versionId, variantType, name).ToArray())
            {
                removed |= GetVariantList(entry.Type).Remove(entry.Metadata);
            }
            return removed;
        }

        private List<VariantMetadataBase> GetVariantList(VariantType type)
        {
            return type switch
            {
                VariantType.MapVariant => MapLinks,
                VariantType.UgcGameVariant => UgcGameVariantLinks,
                VariantType.EngineGameVariant => EngineGameVariantLinks,
                _ => throw new ArgumentException()
            };
        }

        public bool CompareContentsByGuid(VariantManifest other)
        {
            if (other.AllVariants.Count() != AllVariants.Count())
            {
                return false;
            }

            var enumerator = AllVariants.GetEnumerator();
            var otherEnumerator = other.AllVariants.GetEnumerator();
            bool moveNextResult = enumerator.MoveNext();
            bool otherMoveNextResult = otherEnumerator.MoveNext();
            while (moveNextResult || otherMoveNextResult)
            {
                if (moveNextResult != otherMoveNextResult
                    || enumerator.Current.Metadata.AssetId != otherEnumerator.Current.Metadata.AssetId
                    || enumerator.Current.Metadata.VersionId != otherEnumerator.Current.Metadata.VersionId)
                {
                    return false;
                }
                moveNextResult = enumerator.MoveNext();
                otherMoveNextResult = otherEnumerator.MoveNext();
            }

            return true;
        }
    }
}
