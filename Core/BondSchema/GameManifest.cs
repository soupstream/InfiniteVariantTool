using InfiniteVariantTool.Core.Cache;
using InfiniteVariantTool.Core.Variants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteVariantTool.Core.BondSchema
{
    [Bond.Schema]
    public class GameManifest : BondAsset
    {
        #region Schema

        [Bond.Id(0)]
        public CustomData_ CustomData { get; set; }
        [Bond.Id(1), Bond.Type(typeof(List<Bond.Tag.wstring>))]
        public List<string> Tags { get; set; }
        [Bond.Id(2)]
        public List<BondAsset> MapLinks { get; set; }
        [Bond.Id(3)]
        public List<BondAsset> UgcGameVariantLinks { get; set; }
        [Bond.Id(4)]
        public List<BondAsset> PlaylistLinks { get; set; }
        [Bond.Id(5)]
        public List<BondAsset> EngineGameVariantLinks { get; set; }

        public GameManifest()
        {
            CustomData = new();
            Tags = new();
            MapLinks = new();
            UgcGameVariantLinks = new();
            PlaylistLinks = new();
            EngineGameVariantLinks = new();
        }

        [Bond.Schema]
        public class CustomData_
        {
            [Bond.Id(0)]
            public string BranchName { get; set; }
            [Bond.Id(1)]
            public string BuildNumber { get; set; }
            [Bond.Id(2)]
            public int Kind { get; set; }
            [Bond.Id(3)]
            public string ContentVersion { get; set; }
            [Bond.Id(4)]
            public BondGuid BuildGuid { get; set; }
            [Bond.Id(5)]
            public int Visibility { get; set; }

            public CustomData_()
            {
                BranchName = "";
                BuildNumber = "";
                ContentVersion = "";
                BuildGuid = new();
            }
        }

        #endregion

        #region MyExtensions

        public List<BondAsset> LinksByType(VariantType type)
        {
            if (type == VariantType.UgcGameVariant)
            {
                return UgcGameVariantLinks;
            }
            else if (type == VariantType.EngineGameVariant)
            {
                return EngineGameVariantLinks;
            }
            else if (type == VariantType.MapVariant)
            {
                return MapLinks;
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }

        #endregion
    }
}
