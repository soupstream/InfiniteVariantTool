using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteVariantTool.Core.BondSchema
{
    [Bond.Schema]
    public class BondAsset
    {
        #region Schema

        [Bond.Id(0)]
        public BondGuid AssetId { get; set; }
        [Bond.Id(1)]
        public BondGuid VersionId { get; set; }
        [Bond.Id(2), Bond.Type(typeof(Bond.Tag.wstring))]
        public string PublicName { get; set; }
        [Bond.Id(3), Bond.Type(typeof(Bond.Tag.wstring))]
        public string Description { get; set; }
        [Bond.Id(4)]
        public Files_ Files { get; set; }
        [Bond.Id(5)]
        public List<string> Contributors { get; set; }
        [Bond.Id(6)]
        public int AssetHome { get; set; }
        [Bond.Id(7)]
        public AssetStats_ AssetStats { get; set; }
        [Bond.Id(8)]
        public int InspectionResult { get; set; }
        [Bond.Id(9)]
        public int CloneBehavior { get; set; }
        [Bond.Id(10)]
        public int Order { get; set; }

        public BondAsset()
        {
            AssetId = new();
            VersionId = new();
            PublicName = "";
            Description = "";
            Files = new();
            Contributors = new();
            AssetStats = new();
        }

        // copy constructor
        public BondAsset(BondAsset other)
        {
            AssetId = new(other.AssetId);
            VersionId = new(other.VersionId);
            PublicName = other.PublicName;
            Description = other.Description;
            Files = new(other.Files);
            Contributors = new(other.Contributors);
            AssetHome = other.AssetHome;
            AssetStats = new(other.AssetStats);
            InspectionResult = other.InspectionResult;
            CloneBehavior = other.CloneBehavior;
            Order = other.Order;
        }

        [Bond.Schema]
        public class Files_
        {
            [Bond.Id(0)]
            public string Prefix { get; set; }
            [Bond.Id(1)]
            public List<string> FileRelativePaths { get; set; }
            [Bond.Id(2)]
            public ApiManifest.Endpoint PrefixEndpoint { get; set; }

            public Files_()
            {
                Prefix = "";
                FileRelativePaths = new();
                PrefixEndpoint = new();
            }

            // copy constructor
            public Files_(Files_ other)
            {
                Prefix = other.Prefix;
                FileRelativePaths = new(other.FileRelativePaths);
                PrefixEndpoint = new(other.PrefixEndpoint);
            }
        }

        [Bond.Schema]
        public class AssetStats_
        {
            [Bond.Id(0)]
            public long PlaysRecent { get; set; }
            [Bond.Id(1)]
            public long PlaysAllTime { get; set; }
            [Bond.Id(2)]
            public long Favorites { get; set; }
            [Bond.Id(3)]
            public long Likes { get; set; }
            [Bond.Id(9)]
            public long Bookmarks { get; set; }
            [Bond.Id(20)]
            public long ParentAssetCount { get; set; }
            [Bond.Id(21), Bond.Type(typeof(Bond.Tag.nullable<double>))]
            public double? AverageRating { get; set; }
            [Bond.Id(22)]
            public long NumberOfRatings { get; set; }

            public AssetStats_()
            {

            }

            // copy constructor
            public AssetStats_(AssetStats_ other)
            {
                PlaysRecent = other.PlaysRecent;
                PlaysAllTime = other.PlaysAllTime;
                Favorites = other.Favorites;
                Likes = other.Likes;
                Bookmarks = other.Bookmarks;
                ParentAssetCount = other.ParentAssetCount;
                AverageRating = other.AverageRating;
                NumberOfRatings = other.NumberOfRatings;
            }
        }

        #endregion

        #region MyExtensions

        public bool GuidsEqual(Guid assetId, Guid versionId)
        {
            return (Guid)AssetId == assetId && (Guid)VersionId == versionId;
        }

        public bool GuidsEqual(BondAsset other)
        {
            return AssetId.Equals(other.AssetId) && VersionId.Equals(other.VersionId);
        }

        public bool AssetIdEqual(BondAsset other)
        {
            return AssetId.Equals(other.AssetId);
        }

        public void SetGuids(Guid? assetId, Guid? versionId)
        {
            if (assetId != null)
            {
                string oldGuid = AssetId.ToString();
                string newGuid = assetId.Value.ToString();
                Files.Prefix = Files.Prefix.Replace(oldGuid, newGuid);
                Files.PrefixEndpoint.Path = Files.PrefixEndpoint.Path.Replace(oldGuid, newGuid);
                AssetId = (BondGuid)assetId.Value;
            }

            if (versionId != null)
            {
                string oldGuid = VersionId.ToString();
                string newGuid = versionId.Value.ToString();
                Files.Prefix = Files.Prefix.Replace(oldGuid, newGuid);
                Files.PrefixEndpoint.Path = Files.PrefixEndpoint.Path.Replace(oldGuid, newGuid);
                VersionId = (BondGuid)versionId.Value;
            }
        }

        public void GenerateGuids(bool generateAssetId = true, bool generateVersionId = true)
        {
            SetGuids(generateAssetId ? Guid.NewGuid() : null, generateVersionId ? Guid.NewGuid() : null);
        }

        #endregion
    }
}
