using InfiniteVariantTool.Core.Variants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteVariantTool.Core.BondSchema
{
    [Bond.Schema, Variant]
    public class EngineGameVariant : BondAsset
    {
        #region Schema

        [Bond.Id(0)]
        public CustomData_ CustomData { get; set; }
        [Bond.Id(1), Bond.Type(typeof(List<Bond.Tag.wstring>))]
        public List<string> Tags { get; set; }

        public EngineGameVariant()
        {
            CustomData = new();
            Tags = new();
        }

        // partial copy constructor
        public EngineGameVariant(BondAsset other) : base(other)
        {
            CustomData = new();
            Tags = new();
        }

        [Bond.Schema]
        public class CustomData_
        {
            [Bond.Id(0)]
            public SubsetData_ SubsetData { get; set; }
            [Bond.Id(1)]
            public LocalizedData_ LocalizedData { get; set; }

            public CustomData_()
            {
                SubsetData = new();
                LocalizedData = new();
            }
        }

        [Bond.Schema]
        public class SubsetData_
        {
            [Bond.Id(0)]
            public int StatBucketGameType { get; set; }
            [Bond.Id(1)]
            public string EngineName { get; set; }
            [Bond.Id(2)]
            public string VariantName { get; set; }

            public SubsetData_()
            {
                EngineName = "";
                VariantName = "";
            }
        }

        [Bond.Schema]
        public class LocalizedData_
        {
        }

        #endregion
    }
}
