using Bond.IO.Safe;
using Bond.Protocols;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteVariantTool.Core.BondSchema
{
    [Bond.Schema]
    public class CacheFile
    {
        #region Schema

        [Bond.Id(1), Bond.Type(typeof(Bond.Tag.nullable<CacheMap.Metadata>))]
        public CacheMap.Metadata? Metadata { get; set; }
        [Bond.Id(2)]
        public sbyte[] Data { get; set; }

        public CacheFile()
        {
            Data = Array.Empty<sbyte>();
        }

        #endregion

        #region MyExtensions

        public byte[] UData => (byte[])(Array)Data;

        #endregion
    }
}
