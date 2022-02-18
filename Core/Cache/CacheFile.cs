using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using InfiniteVariantTool.Core.Serialization;
using InfiniteVariantTool.Core.Utils;

namespace InfiniteVariantTool.Core.Cache
{
    public enum ContentType
    {
        Png,
        Jpg,
        Json,
        Bond,
        Bin,
        Strings,
        Luabundle,
        Undefined,
        AutoDetect
    }

    public class CacheFile : BondXmlSerializable
    {
        private XElement? blobElement;
        public CacheFileMetadata? Metadata { get; set; }
        public ICacheFileContent Content { get; private set; }
        public XElement Doc
        {
            get
            {
                if (Content is CacheFileContentBond bondContent)
                {
                    return bondContent.Data;
                }
                throw new InvalidOperationException();
            }
        }
        public byte[] Bytes
        {
            get
            {
                if (Content is CacheFileContentBytes bytesContent)
                {
                    return bytesContent.Data;
                }
                throw new InvalidOperationException();
            }
        }

        public CacheFile(string filename, ContentType contentType, bool? hasMetadata)
            : this(File.ReadAllBytes(filename), contentType, hasMetadata)
        {

        }

        public CacheFile(byte[] data, ContentType contentType, bool? hasMetadata)
        {
            Metadata = null;
            Content = Load(data, contentType, hasMetadata);
        }

        public CacheFile(XElement data)
        {
            Metadata = null;
            Content = new CacheFileContentBond("", data, new List<CacheFileContentBytes>());
        }

        public CacheFile(BondReadResult result)
        {
            Metadata = null;
            Content = new CacheFileContentBond(result);
        }

        private ICacheFileContent Load(byte[] data, ContentType contentType, bool? hasMetadata)
        {
            // load metadata
            ICacheFileContent? bondContent = null;
            if (hasMetadata is null or true)
            {
                CacheFileContentBytes rawContent = new CacheFileContentBytes(data);
                if (rawContent.InferType() == ContentType.Bin)
                {
                    // try to parse metadata
                    BondReader br = new(data);
                    BondReadResult result = br.Read();
                    try
                    {
                        Deserialize(result.Doc);
                        hasMetadata = true;
                    }
                    catch (BondXmlException)
                    {
                        if (hasMetadata is true)
                        {
                            throw;
                        }
                        hasMetadata = false;
                    }

                    // retrieve content
                    if (hasMetadata == true)
                    {
                        if (blobElement!.Name == BondType.list.ToString())
                        {
                            data = BondReadResult.ListToBlob(blobElement);
                        }
                        else
                        {
                            data = result.Blobs.First().Value;
                        }
                    }
                    else
                    {
                        // save parsed bond content for later if no metadata was found
                        bondContent = new CacheFileContentBond(result);
                    }
                }
                else if (hasMetadata == true)
                {
                    throw new Exception("Expected metadata, but didn't find it");
                }
            }

            // load content
            ICacheFileContent tmpContent;
            if (contentType == ContentType.Bond)
            {
                if (bondContent == null)
                {
                    BondReader br = new(data);
                    BondReadResult result = br.Read();
                    tmpContent = new CacheFileContentBond(result);
                }
                else
                {
                    tmpContent = bondContent;
                }
                tmpContent.InferType();
            }
            else if (contentType == ContentType.AutoDetect)
            {
                if (bondContent == null)
                {
                    tmpContent = new CacheFileContentBytes(data);
                    if (tmpContent.InferType() == ContentType.Bin)
                    {
                        // no specific type detected, so assume it's bond data
                        BondReader br = new(data);
                        BondReadResult result = br.Read();
                        tmpContent = new CacheFileContentBond(result);
                        tmpContent.InferType();
                    }
                }
                else
                {
                    tmpContent = bondContent;
                    tmpContent.InferType();
                }
            }
            else
            {
                tmpContent = new CacheFileContentBytes(data)
                {
                    Type = contentType
                };
            }

            return tmpContent;
        }

        public ICacheFileContent ContentWithMetadata()
        {
            if (Metadata == null)
            {
                return Content;
            }
            else
            {
                XElement doc = Serialize();
                if (Content is CacheFileContentBytes bytesContent)
                {
                    List<CacheFileContentBytes> contentBlobs = new();
                    contentBlobs.Add(new CacheFileContentBytes(bytesContent.Name, bytesContent.Type, bytesContent.Data, blobElement));
                    return new CacheFileContentBond(bytesContent.Name, doc, contentBlobs);
                }
                else if (Content is CacheFileContentBond bondContent)
                {
                    // XML is copied during serialization which invalidates node references, so we need to find the blob nodes again
                    List<XElement> blobElements = blobElement!.Descendants("blob").ToList();
                    List<CacheFileContentBytes> blobList = new(bondContent.Blobs);
                    for (int i = 0; i < blobElements.Count; i++)
                    {
                        blobList[i].BlobNode = blobElements[i];
                    }
                    return new CacheFileContentBond(bondContent.Name, doc, bondContent.Blobs);
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        public new void Save(string filename)
        {
            var tmpContent = ContentWithMetadata();
            tmpContent.SetName(filename, new FileNameDeduper());
            tmpContent.Save();
        }

        public new byte[] Pack()
        {
            var tmpContent = ContentWithMetadata();
            tmpContent.SetName("tmp", new FileNameDeduper());
            return tmpContent.Pack();
        }

        public new byte[] Pack(string filename)
        {
            byte[] data = Pack();
            File.WriteAllBytes(filename, data);
            return data;
        }

        public override void Deserialize(BondXmlDeserializer d, int? id = null)
        {
            CacheFileMetadata tmpMetadata = new();
            XElement? tmpBlobElement = null;
            d.ReadStruct(null, () =>
            {
                d.ReadList(1, BondType.@struct, () =>
                {
                    tmpMetadata.Deserialize(d);
                });
                tmpBlobElement = d.ReadBlob(2, BondType.int8);
            });
            Metadata = tmpMetadata;
            blobElement = tmpBlobElement;
        }

        public override void Serialize(BondXmlSerializer s, int? id = null)
        {
            if (Metadata != null)
            {
                s.WriteStruct(id, () =>
                {
                    s.WriteList(BondType.@struct, 1, () =>
                    {
                        Metadata.Serialize(s);
                    });
                    if (Content is CacheFileContentBytes contentBytes)
                    {
                        blobElement = s.WriteBlob(contentBytes.Name, BondType.int8, 2);
                    }
                    else if (Content is CacheFileContentBond contentBond)
                    {
                        blobElement = s.WriteBlob(new XElement(contentBond.Data), BondType.int8, 2);
                    }
                    else
                    {
                        throw new Exception();
                    }
                });
            }
            else if (Content is CacheFileContentBond contentBond)
            {
                s.WriteNode(contentBond.Data);
            }
        }

        public static string SuggestFilename(string filename, ContentType type)
        {
            switch (type)
            {
                case ContentType.Bond:
                    return BondReader.SuggestFilename(filename);
                default:
                    return filename + "." + type.ToString().ToLower();
            }
        }
    }

    public interface ICacheFileContent
    {
        string Name { get; set; }
        ContentType Type { get; }
        XElement? BlobNode { get; }
        ContentType InferType();
        void SetName(string name, FileNameDeduper deduper);
        void Save();
        byte[] Pack();
    }

    public class CacheFileContentBytes : ICacheFileContent
    {
        public string Name { get; set; }
        public ContentType Type { get; set; }
        public byte[] Data { get; }
        public XElement? BlobNode { get; set; }

        public CacheFileContentBytes(byte[] data)
        {
            Type = ContentType.Undefined;
            Name = "";
            Data = data;
        }

        public CacheFileContentBytes(string name, ContentType type, byte[] data, XElement? blobNode)
        {
            Name = name;
            Type = type;
            Data = data;
            BlobNode = blobNode;
        }

        public ContentType InferType()
        {
            if (Util.ArrayStartsWith(Data, Constants.pngSignature))
            {
                Type = ContentType.Png;
            }
            else if (Util.ArrayStartsWith(Data, Constants.jpgSignature))
            {
                Type = ContentType.Jpg;
            }
            else if (Util.IsJson(Data))
            {
                Type = ContentType.Json;
            }
            else if (LuaBundleUnpacker.IsLuaBundle(Data))
            {
                Type = ContentType.Luabundle;
            }
            else
            {
                Type = ContentType.Bin;
            }
            return Type;
        }

        public void SetName(string name, FileNameDeduper deduper)
        {
            if (Type == ContentType.Undefined)
            {
                Name = deduper.Dedupe(name);
            }
            else
            {
                name = Path.Combine(Path.GetDirectoryName(name)!, Path.GetFileNameWithoutExtension(name));
                Name = deduper.Dedupe(name, Type switch
                {
                    ContentType.Jpg => ".jpg",
                    ContentType.Png => ".png",
                    ContentType.Json => ".json",
                    ContentType.Strings => ".strings",
                    ContentType.Luabundle => ".luabundle",
                    ContentType.Bin => ".bin",
                    _ => throw new Exception()
                });
            }
            BlobNode?.SetText(Path.GetFileName(Name));
        }

        public void Save()
        {
            File.WriteAllBytes(Name, Data);
        }

        public byte[] Pack()
        {
            return Data;
        }
    }

    public class CacheFileContentBond : ICacheFileContent
    {
        public string Name { get; set; }
        public ContentType Type { get; }
        public XElement? BlobNode { get; }
        public XElement Data { get; }
        public List<CacheFileContentBytes> Blobs { get; }

        public CacheFileContentBond(BondReadResult data)
        {
            Type = ContentType.Bond;
            Name = "";
            Data = data.Doc;
            Blobs = data.Blobs.Select(entry => new CacheFileContentBytes(entry.Key.GetText(), ContentType.Undefined, entry.Value, entry.Key)).ToList();
        }

        public CacheFileContentBond(string name, XElement data, List<CacheFileContentBytes> blobs)
        {
            Type = ContentType.Bond;
            Name = name;
            Data = data;
            Blobs = blobs;
        }

        public ContentType InferType()
        {
            foreach (var blob in Blobs)
            {
                
                if (IsStrings(blob.Data))
                {
                    blob.Type = ContentType.Strings;
                }
                else
                {
                    blob.InferType();
                }
            }
            return ContentType.Bond;
        }

        // rough heuristic to check whether data is a blob of zero-terminated strings
        private bool IsStrings(byte[] data)
        {
            int end = Math.Min(data.Length, 64);
            int minLength = 4;
            for (int i = 0; i < end; i++)
            {
                if (i >= minLength && data[i] == 0)
                {
                    return true;
                }
                if (data[i] < 32 || data[i] > 127)
                {
                    return false;
                }
            }
            return false;
        }

        public void SetName(string name, FileNameDeduper deduper)
        {
            Name = deduper.Dedupe(name);
            foreach (var blob in Blobs)
            {
                blob.SetName(name, deduper);
            }
        }

        public void Save()
        {
            Data.SaveVersioned(Name);
            foreach (var blob in Blobs)
            {
                blob.Save();
            }
        }

        public byte[] Pack()
        {
            if (Name == "")
            {
                SetName("tmp", new FileNameDeduper());
            }
            BondWriter bw = new(Data, Blobs.ToDictionary(
                blob => Path.GetFileName(blob.Name),
                blob => blob.Data));
            return bw.Write();
        }
    }

    public class CacheFileMetadata : BondXmlSerializable
    {
        public string Etag { get; set; }
        public ulong FileId { get; set; }
        public OrderedDictionary<string, string> Headers { get; set; }
        public Guid? Guid { get; set; }
        public string Url { get; set; }

        public CacheFileMetadata()
        {
            Etag = "";
            Headers = new();
            Guid = null;
            Url = "";
        }

        public override void Serialize(BondXmlSerializer s, int? id = null)
        {
            s.IgnoreDefaultValues = true;
            s.WriteStruct(id, () =>
            {
                s.WriteString(Etag, 0);
                s.WriteUInt64(FileId, 1);
                s.WriteMap(BondType.@string, BondType.@string, 2, () =>
                {
                    foreach (var entry in Headers)
                    {
                        s.WriteString(entry.Key);
                        s.WriteString(entry.Value);
                    }
                });

                if (Guid != null)
                {
                    s.WriteGuid(Guid.Value, 3);
                }

                s.WriteString(Url, 4);
            });
        }

        public override void Deserialize(BondXmlDeserializer d, int? id = null)
        {
            d.UseDefaultValues = true;
            d.ReadStruct(id, () =>
            {
                Etag = d.ReadString(0);
                FileId = d.ReadUInt64(1);

                Headers = new();
                d.ReadMap(2, BondType.@string, BondType.@string, () =>
                {
                    string key = d.ReadString();
                    string value = d.ReadString();
                    Headers[key] = value;
                });

                Guid = d.IsNodeId(3) ? d.ReadGuid(3) : null;
                Url = d.ReadString(4);
            });
        }
    }
}
