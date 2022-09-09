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
    public class CacheMap
    {
        #region Schema

        [Bond.Id(0)]
        public Dictionary<ulong, Entry> Entries { get; set; }
        [Bond.Id(1)]
        public sbyte Unk { get; set; }
        [Bond.Id(2)]
        public string Language { get; set; }

        public CacheMap()
        {
            Entries = new();
            Language = "";
        }

        [Bond.Schema]
        public class Entry
        {
            [Bond.Id(0)]
            public long CreateTime { get; set; }
            [Bond.Id(1)]
            public long AccessTime { get; set; }
            [Bond.Id(2)]
            public long WriteTime { get; set; }
            [Bond.Id(3)]
            public Metadata Metadata { get; set; }
            [Bond.Id(4)]
            public ulong Size { get; set; }

            public Entry()
            {
                Metadata = new();
            }
        }

        [Bond.Schema]
        public class Metadata
        {
            [Bond.Id(0)]
            public string Etag { get; set; }
            [Bond.Id(1)]
            public ulong Timestamp { get; set; }
            [Bond.Id(2)]
            public Dictionary<string, string> Headers { get; set; }
            [Bond.Id(3)]
            public BondGuid Guid { get; set; }
            [Bond.Id(4)]
            public string Url { get; set; }

            public Metadata()
            {
                Etag = "";
                Headers = new();
                Guid = new();
                Url = "";
            }
        }

        #endregion
    }
}
