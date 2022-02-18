using InfiniteVariantTool.Core.Cache;
using InfiniteVariantTool.Core.Serialization;
using System;
using System.Collections.Generic;
using System.IO;

namespace InfiniteVariantTool.Core.Variants
{
    public enum VariantType
    {
        MapVariant,
        UgcGameVariant,
        EngineGameVariant
    };

    public abstract class Variant
    {
        public abstract VariantMetadata Metadata { get; }

        public Dictionary<string, CacheFile> Files { get; } // key = file relative path
        public VariantType Type { get; }
        public abstract string Url { get; }
        public abstract string MetadataFilename { get; }
        public bool? Enabled { get; set; }
        public bool UnpackBondContent { get; set; }
        public bool UnpackLuaBundles { get; set; }

        protected Variant(VariantType type, Dictionary<string, CacheFile> files)
        {
            Type = type;
            Files = files;
        }

        public static Variant FromMetadata(VariantMetadata metadata, Dictionary<string, CacheFile> files)
        {
            return metadata switch
            {
                MapVariantMetadata mapMetadata => new MapVariant(mapMetadata, files),
                UgcGameVariantMetadata ugcMetadata => new UgcGameVariant(ugcMetadata, files),
                EngineGameVariantMetadata engineMetadata => new EngineGameVariant(engineMetadata, files),
                _ => throw new ArgumentException()
            };
        }

        public virtual void Save(string path)
        {
            Directory.CreateDirectory(path);
            if (Files.Count > 0)
            {
                string filesDirectory = Path.Combine(path, "files");
                foreach (var entry in Files)
                {
                    string filePath = Path.Combine(filesDirectory, entry.Key);
                    string directoryPath = Path.GetDirectoryName(filePath)!;
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                    entry.Value.Metadata = null;
                    entry.Value.Save(filePath);

                    if (UnpackBondContent && (filePath.EndsWith(".bin") || filePath.EndsWith(".mvar")))
                    {
                        CacheFile unpackedFile = new(entry.Value.Bytes, ContentType.Bond, false);
                        unpackedFile.Save(filePath + ".xml");
                        if (UnpackLuaBundles)
                        {
                            foreach (var blob in ((CacheFileContentBond)unpackedFile.Content).Blobs)
                            {
                                if (blob.Type == ContentType.Luabundle)
                                {
                                    LuaBundleUnpacker unpacker = new(blob.Data);
                                    unpacker.Save(filePath + "_lua");
                                }
                            }
                        }
                    }
                    if (UnpackLuaBundles && filePath.EndsWith(".debugscriptsource"))
                    {
                        LuaBundleUnpacker unpacker = new(entry.Value.Bytes);
                        unpacker.Save(filePath[..^18] + "_debug_lua");
                    }
                }
            }
            Metadata.Save(Path.Combine(path, MetadataFilename));
        }

        public static Variant Load(string path)
        {
            Variant? variant;
            if (Directory.Exists(path))
            {
                variant = TryLoadVariant(Path.Combine(path, MapVariant.MetadataFilenameStatic))
                    ?? TryLoadVariant(Path.Combine(path, UgcGameVariant.MetadataFilenameStatic))
                    ?? TryLoadVariant(Path.Combine(path, EngineGameVariant.MetadataFilenameStatic));
            }
            else if (File.Exists(path))
            {
                variant = TryLoadVariant(path);
            }
            else
            {
                throw new FileNotFoundException("No file or directory found");
            }
            
            return variant ?? throw new FileNotFoundException("Variant XML file not found");
        }

        public static Variant? TryLoadVariant(string variantMetadataFilepath)
        {
            if (!File.Exists(variantMetadataFilepath))
            {
                return null;
            }
            string filename = Path.GetFileName(variantMetadataFilepath);
            if (filename == MapVariant.MetadataFilenameStatic)
            {
                return new MapVariant(variantMetadataFilepath);
            }
            else if (filename == UgcGameVariant.MetadataFilenameStatic)
            {
                return new UgcGameVariant(variantMetadataFilepath);
            }
            else if (filename == EngineGameVariant.MetadataFilenameStatic)
            {
                return new EngineGameVariant(variantMetadataFilepath);
            }
            else
            {
                return null;
            }
        }

        public static IEnumerable<Variant> LoadVariants(string dirname)
        {
            foreach (string filepath in Directory.GetFiles(dirname, "*.xml", SearchOption.AllDirectories))
            {
                if (TryLoadVariant(filepath) is Variant variant)
                {
                    yield return variant;
                }
            }
        }

        protected void LoadFiles(string baseDir)
        {
            foreach (var relativeFilePath in Metadata.Base.FileRelativePaths)
            {
                string filename = Path.Combine(baseDir, "files", relativeFilePath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(filename))
                {
                    Files[relativeFilePath] = new CacheFile(filename, ContentType.Undefined, false);
                }
            }
        }

        public virtual void SetAssetId(Guid assetId)
        {
            Metadata.Base.SetAssetId(assetId);
        }

        public virtual void SetVersionId(Guid versionId)
        {
            Metadata.Base.SetVersionId(versionId);
        }

        public virtual void GenerateAssetId()
        {
            Metadata.Base.GenerateAssetId();
        }

        public virtual void GenerateVersionId()
        {
            Metadata.Base.GenerateVersionId();
        }
    }
}
