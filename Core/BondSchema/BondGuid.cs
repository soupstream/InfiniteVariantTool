using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace InfiniteVariantTool.Core.BondSchema
{
    [Bond.Schema]
    public class BondGuid
    {
        #region Schema

        [Bond.Id(0)]
        public uint Data1 { get; set; }

        [Bond.Id(1)]
        public ushort Data2 { get; set; }

        [Bond.Id(2)]
        public ushort Data3 { get; set; }

        [Bond.Id(3)]
        public ulong Data4 { get; set; }

        public BondGuid()
        {

        }

        // copy constructor
        public BondGuid(BondGuid other)
        {
            Data1 = other.Data1;
            Data2 = other.Data2;
            Data3 = other.Data3;
            Data4 = other.Data4;
        }

        #endregion

        #region MyExtensions

        public BondGuid(Guid guid)
        {
            byte[] bytes = guid.ToByteArray();
            Data1 = BitConverter.ToUInt32(bytes, 0);
            Data2 = BitConverter.ToUInt16(bytes, 4);
            Data3 = BitConverter.ToUInt16(bytes, 6);
            Data4 = BitConverter.ToUInt64(bytes, 8);
        }

        public BondGuid(string guid) : this(Guid.Parse(guid))
        {

        }

        public override string ToString()
        {
            byte[] bytes = new byte[8];
            BinaryPrimitives.WriteUInt64LittleEndian(bytes, Data4);
            ulong data5 = BinaryPrimitives.ReadUInt64BigEndian(bytes);
            ushort data4 = (ushort)(data5 >> 48);
            data5 &= 0xffffffffffff;
            return $"{Data1:x08}-{Data2:x04}-{Data3:x04}-{data4:x04}-{data5:x012}";
        }

        public override bool Equals(object? obj)
        {
            return obj is BondGuid other
                && Data1 == other.Data1
                && Data2 == other.Data2
                && Data3 == other.Data3
                && Data4 == other.Data4;
        }

        public override int GetHashCode()
        {
            return (int)Data1 ^ Data2 ^ (Data3 << 16) ^ (int)(Data4 & 0xffffffff) ^ (int)(Data4 >> 32);
        }

        public static explicit operator Guid(BondGuid bondGuid) => Guid.Parse(bondGuid.ToString());
        public static explicit operator BondGuid(Guid guid) => new(guid);

        #endregion
    }

    public class BondGuidJsonConverter : JsonConverter<BondGuid>
    {
        public override BondGuid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return new BondGuid(reader.GetString()!);
        }

        public override void Write(Utf8JsonWriter writer, BondGuid bondGuid, JsonSerializerOptions options)
        {
            writer.WriteStringValue(bondGuid.ToString());
        }
    }
}
