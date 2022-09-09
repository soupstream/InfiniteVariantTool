using InfiniteVariantTool.Core.Variants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteVariantTool.Core.BondSchema
{
    [Bond.Schema, Variant]
    public class MapVariant : BondAsset
    {
        [Bond.Id(0)]
        public CustomData_ CustomData { get; set; }
        [Bond.Id(1), Bond.Type(typeof(List<Bond.Tag.wstring>))]
        public List<string> Tags { get; set; }

        public MapVariant()
        {
            CustomData = new();
            Tags = new();
        }

        // partial copy constructor
        public MapVariant(BondAsset other) : base(other)
        {
            CustomData = new();
            Tags = new();
        }

        [Bond.Schema]
        public class CustomData_
        {
            [Bond.Id(0)]
            public int NumOfObjectsOnMap { get; set; }
            [Bond.Id(1)]
            public int TagLevelId { get; set; }
            [Bond.Id(2)]
            public bool IsBaked { get; set; }
            [Bond.Id(3)]
            public bool HasNodeGraph { get; set; }
        }
    }
}
