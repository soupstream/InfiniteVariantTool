using Bond.IO.Safe;
using Bond.Protocols;
using InfiniteVariantTool.Core.BondSchema;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace InfiniteVariantTool.Core.Serialization
{
    public class SchemaSerializer
    {
        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = true,
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            Converters =
            {
                new BondGuidJsonConverter()
            }
        };


        public static T DeserializeBond<T>(byte[] data)
        {
            InputBuffer input = new(data);
            CompactBinaryReader<InputBuffer> reader = new(input, 2);
            return Bond.Deserialize<T>.From(reader);
        }

        public static object DeserializeBond(byte[] data, Type type)
        {
            InputBuffer input = new(data);
            CompactBinaryReader<InputBuffer> reader = new(input, 2);
            Bond.Deserializer<CompactBinaryReader<InputBuffer>> deserializer = new(type);
            return deserializer.Deserialize(reader);
        }

        public static async Task<object> DeserializeBondAsync(string filePath, Type type)
        {
            var input = new InputBuffer(await File.ReadAllBytesAsync(filePath));
            var reader = new CompactBinaryReader<InputBuffer>(input, 2);
            var deserializer = new Bond.Deserializer<CompactBinaryReader<InputBuffer>>(type);
            return deserializer.Deserialize(reader);
        }

        public static byte[] SerializeBond(object src)
        {
            OutputBuffer output = new();
            CompactBinaryWriter<OutputBuffer> writer = new(output, 2);
            Bond.Serializer<CompactBinaryWriter<OutputBuffer>> serializer = new(src.GetType());
            serializer.Serialize(src, writer);
            return output.Data.ToArray();
        }

        public static T DeserializeJson<T>(byte[] data)
        {
            return JsonSerializer.Deserialize<T>(data, jsonOptions)!;
        }

        public static async Task<object> DeserializeJsonAsync(string filePath, Type type)
        {
            using var stream = File.OpenRead(filePath);
            return (await JsonSerializer.DeserializeAsync(stream, type, jsonOptions))!;
        }

        public static string SerializeJson(object src)
        {
            return JsonSerializer.Serialize(src, src.GetType(), jsonOptions);
        }

        public static T DeserializeXml<T>(XElement doc)
        {
            BondWriter bw = new(doc);
            byte[] packed = bw.Write();
            return DeserializeBond<T>(packed);
        }

        public static async Task<object> DeserializeXmlAsync(string filePath, Type type)
        {
            using var stream = File.OpenRead(filePath);
            XDocument doc = await XDocument.LoadAsync(stream, LoadOptions.None, new CancellationTokenSource().Token);
            BondWriter br = new(doc.Root!);
            var input = new InputBuffer(br.Write());
            var reader = new CompactBinaryReader<InputBuffer>(input, 2);
            var deserializer = new Bond.Deserializer<CompactBinaryReader<InputBuffer>>(type);
            return deserializer.Deserialize(reader);
        }
    }
}
