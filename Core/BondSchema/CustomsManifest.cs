using InfiniteVariantTool.Core.Variants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteVariantTool.Core.BondSchema
{
    [Bond.Schema]
    public class CustomsManifest : BondAsset
    {
        #region Schema

        [Bond.Id(0)]
        public CustomData_ CustomData { get; set; }
        [Bond.Id(1)]
        public List<BondAsset> MapLinks { get; set; }
        [Bond.Id(2)]
        public List<BondAsset> PlaylistLinks { get; set; }
        [Bond.Id(3)]
        public List<BondAsset> PrefabLinks { get; set; }
        [Bond.Id(4)]
        public List<BondAsset> UgcGameVariantLinks { get; set; }
        [Bond.Id(5)]
        public List<BondAsset> MapModePairLinks { get; set; }
        [Bond.Id(6), Bond.Type(typeof(List<Bond.Tag.wstring>))]
        public List<string> Tags { get; set; }

        public CustomsManifest()
        {
            CustomData = new();
            MapLinks = new();
            PlaylistLinks = new();
            PrefabLinks = new();
            UgcGameVariantLinks = new();
            MapModePairLinks = new();
            Tags = new();
        }

        [Bond.Schema]
        public class CustomData_
        {
        }

        #endregion

        #region MyExtensions

        public List<BondAsset>? LinksByType(VariantType type)
        {
            if (type == VariantType.UgcGameVariant)
            {
                return UgcGameVariantLinks;
            }
            else if (type == VariantType.MapVariant)
            {
                return MapLinks;
            }
            else if (type == VariantType.EngineGameVariant)
            {
                return null;
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }

        #endregion
    }
}
