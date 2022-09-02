using System.Collections.Generic;
using System.Buffers.Binary;

namespace InfiniteVariantTool.Core.Serialization
{
    public class MyBinaryWriter
    {
        private readonly List<byte> data;
        private bool littleEndian;
        public MyBinaryWriter(bool littleEndian = true)
        {
            this.littleEndian = littleEndian;
            data = new List<byte>();
        }

        public void SetLittleEndian(bool isLittleEndian)
        {
            littleEndian = isLittleEndian;
        }

        public void WriteBool(bool value)
        {
            WriteUInt8((byte)(value ? 1 : 0));
        }

        public void WriteInt8(sbyte value)
        {
            WriteUInt8((byte)value);
        }

        public void WriteUInt8(byte value)
        {

            data.Add(value);
        }

        public void WriteInt16(short value)
        {
            byte[] bytes = new byte[sizeof(short)];
            if (littleEndian)
            {
                BinaryPrimitives.WriteInt16LittleEndian(bytes, value);
            }
            else
            {
                BinaryPrimitives.WriteInt16BigEndian(bytes, value);
            }
            data.AddRange(bytes);
        }

        public void WriteUInt16(ushort value)
        {
            WriteInt16((short)value);
        }

        public void WriteInt32(int value)
        {
            byte[] bytes = new byte[sizeof(int)];
            if (littleEndian)
            {
                BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
            }
            else
            {
                BinaryPrimitives.WriteInt32BigEndian(bytes, value);
            }
            data.AddRange(bytes);
        }

        public void WriteUInt32(uint value)
        {
            WriteInt32((int)value);
        }

        public void WriteInt64(long value)
        {
            byte[] bytes = new byte[sizeof(long)];
            if (littleEndian)
            {
                BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
            }
            else
            {
                BinaryPrimitives.WriteInt64BigEndian(bytes, value);
            }
            data.AddRange(bytes);
        }

        public void WriteUInt64(ulong value)
        {
            WriteInt64((long)value);
        }

        public void WriteFloat(float value)
        {
            byte[] bytes = new byte[sizeof(float)];
            if (littleEndian)
            {
                BinaryPrimitives.WriteSingleLittleEndian(bytes, value);
            }
            else
            {
                BinaryPrimitives.WriteSingleBigEndian(bytes, value);
            }
            data.AddRange(bytes);
        }

        public void WriteDouble(double value)
        {
            byte[] bytes = new byte[sizeof(double)];
            if (littleEndian)
            {
                BinaryPrimitives.WriteDoubleLittleEndian(bytes, value);
            }
            else
            {
                BinaryPrimitives.WriteDoubleBigEndian(bytes, value);
            }
            data.AddRange(bytes);
        }

        public int WriteLEB128U(ulong value)
        {
            int byteCount = 0;
            do
            {
                byte b = (byte)(value & 0x7f);
                value >>= 7;
                if (value != 0)
                {
                    b |= 0x80;
                }
                WriteUInt8(b);
                byteCount++;
            } while (value != 0);
            return byteCount;
        }

        public int WriteLEB128U(uint value)
        {
            return WriteLEB128U((ulong)value);
        }

        public int WriteLEB128U(ushort value)
        {
            return WriteLEB128U((ulong)value);
        }

        public int WriteLEB128U(byte value)
        {
            return WriteLEB128U((ulong)value);
        }

        public int WriteLEB128(long value)
        {
            ulong zigzag = (ulong)((value >> (sizeof(long) * 8 - 1)) ^ (value << 1));
            return WriteLEB128U(zigzag);
        }

        public int WriteLEB128(int value)
        {
            uint zigzag = (uint)((value >> (sizeof(int) * 8 - 1)) ^ (value << 1));
            return WriteLEB128U(zigzag);
        }

        public int WriteLEB128(short value)
        {
            ushort zigzag = (ushort)((value >> (sizeof(short) * 8 - 1)) ^ (value << 1));
            return WriteLEB128U(zigzag);
        }

        public int WriteLEB128(sbyte value)
        {
            byte zigzag = (byte)((value >> (sizeof(sbyte) * 8 - 1)) ^ (value << 1));
            return WriteLEB128U(zigzag);
        }

        public void WriteString(string value)
        {
            data.AddRange(System.Text.Encoding.UTF8.GetBytes(value));
        }

        public void WriteStringBuffer(string value, int size)
        {
            WriteString(value);
            if (size > value.Length)
            {
                data.AddRange(new byte[size - value.Length]);
            }
        }

        public void WriteWString(string value)
        {
            data.AddRange(System.Text.Encoding.Unicode.GetBytes(value));
        }

        public void WriteWStringBuffer(string value, int size)
        {
            WriteWString(value);
            if (size > value.Length)
            {
                data.AddRange(new byte[size - value.Length]);
            }
        }

        public void WriteBytes(IList<byte> value)
        {
            data.AddRange(value);
        }

        public void Pad(int padding, byte value = 0)
        {
            if (data.Count % padding != 0)
            {
                int size = padding - (data.Count % padding);
                for (int i = 0; i < size; i++)
                {
                    data.Add(value);
                }
            }
        }

        public List<byte> Data => data;
        public int Size => data.Count;
    }
}
