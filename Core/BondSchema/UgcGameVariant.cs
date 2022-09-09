using InfiniteVariantTool.Core.Variants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteVariantTool.Core.BondSchema
{
    [Bond.Schema, Variant]
    public class UgcGameVariant : BondAsset
    {
        #region schema

        [Bond.Id(0)]
        public CustomData_ CustomData { get; set; }
        [Bond.Id(1), Bond.Type(typeof(List<Bond.Tag.wstring>))]
        public List<string> Tags { get; set; }
        [Bond.Id(2), Bond.Type(typeof(Bond.Tag.nullable<BondAsset>))]
        public BondAsset? EngineGameVariantLink { get; set; }

        public UgcGameVariant()
        {
            CustomData = new();
            Tags = new();
        }

        // partial copy constructor
        public UgcGameVariant(BondAsset other) : base(other)
        {
            CustomData = new();
            Tags = new();
        }

        [Bond.Schema]
        public class CustomData_
        {
            [Bond.Id(0)]
            public Dictionary<string, string> KeyValues { get; set; }

            public CustomData_()
            {
                KeyValues = new();
            }
        }

        #endregion
    }
}
