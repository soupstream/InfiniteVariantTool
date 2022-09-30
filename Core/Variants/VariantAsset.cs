using Bond.IO.Safe;
using Bond.Protocols;
using InfiniteVariantTool.Core.BondSchema;
using InfiniteVariantTool.Core.Serialization;
using InfiniteVariantTool.Core.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace InfiniteVariantTool.Core.Variants
{
    public class VariantAsset
    {
        public BondAsset Variant { get; }
        public Dictionary<string, byte[]> Files { get; }
        public List<VariantAsset> LinkedVariants { get; }
        public VariantType Type => VariantType.FromClassType(Variant.GetType());

        public string? FilePath { get; set; }  // not null if this is a user variant
        public bool? Enabled { get; set; }

        public VariantAsset(BondAsset variant)
        {
            if (!VariantType.VariantTypes.Any(type => type.ClassType == variant.GetType()))
            {
                throw new ArgumentException("expected variant type");
            }
            Variant = variant;
            Files = new();
            LinkedVariants = new();
        }

        public VariantAsset(BondAsset variant, VariantType type)
            : this((BondAsset)Activator.CreateInstance(type.ClassType, variant)!)
        {

        }

        private string VariantTypeName => Variant.GetType().Name;

        // gather variant types
        private static readonly Type[] variantTypes = typeof(BondAsset)
            .Assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<VariantAttribute>() != null)
            .ToArray();

        private static readonly FileExtension[] supportedExtensions = new FileExtension[] { FileExtension.Json, FileExtension.Xml, FileExtension.Bin };

        // set of possible file names
        public static readonly HashSet<string> VariantFileNames = variantTypes
            .SelectMany(t => supportedExtensions.Select(ext => t.Name + ext.Value))
            .ToHashSet();

        // load variant and attached files
        public static async Task<VariantAsset> Load(string variantFilePath, bool loadFiles)
        {
            string directory = Path.GetDirectoryName(variantFilePath)!;
            string variantFileName = Path.GetFileName(variantFilePath);
            foreach (Type variantType in variantTypes)
            {
                foreach (FileExtension extension in supportedExtensions)
                {
                    if (variantFileName.Equals(variantType.Name + extension.Value, StringComparison.InvariantCultureIgnoreCase))
                    {
                        BondAsset variant;
                        if (extension == FileExtension.Json)
                        {
                            variant = (BondAsset)(await SchemaSerializer.DeserializeJsonAsync(variantFilePath, variantType))!;
                        }
                        else if (extension == FileExtension.Xml)
                        {
                            variant = (BondAsset)(await SchemaSerializer.SerializeXmlAsync(variantFilePath, variantType));
                        }
                        else if (extension == FileExtension.Bin)
                        {
                            variant = (BondAsset)(await SchemaSerializer.DeserializeBondAsync(variantFilePath, variantType));
                        }
                        else
                        {
                            throw new Exception("invalid extension");
                        }

                        VariantAsset variantAsset = new(variant);
                        variantAsset.FilePath = Path.GetFullPath(variantFilePath);
                        if (loadFiles)
                        {
                            await variantAsset.LoadFiles(directory);
                        }
                        return variantAsset;
                    }
                }
            }
            throw new Exception("variant file not found");
        }

        public static IEnumerable<string> FindVariants(string dir)
        {
            foreach (string path in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(path);
                foreach (Type variantType in variantTypes)
                {
                    foreach (FileExtension extension in supportedExtensions)
                    {
                        if (fileName.Equals(variantType.Name + extension.Value, StringComparison.InvariantCultureIgnoreCase))
                        {
                            yield return path;
                        }
                    }
                }
            }
        }

        // save variant to disk
        public async Task Save(string directory)
        {
            if (LinkedVariants.Count != 0)
            {
                foreach (var variant in LinkedVariants)
                {
                    await variant.Save(Path.Combine(directory, variant.VariantTypeName));
                }
                directory = Path.Combine(directory, VariantTypeName);
            }

            string variantFilePath = Path.Combine(directory, VariantTypeName + FileExtension.Json.Value);
            Directory.CreateDirectory(directory);
            using var stream = File.OpenWrite(variantFilePath);
            await SchemaSerializer.SerializeJsonAsync(stream, Variant, Type.ClassType);
            foreach (string relativeFilePath in Variant.Files.FileRelativePaths)
            {
                if (Files.ContainsKey(relativeFilePath))
                {
                    string filePath = Path.Combine(directory, relativeFilePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                    await File.WriteAllBytesAsync(filePath, Files[relativeFilePath]);
                }
            }

            FilePath = variantFilePath;
        }

        // load attached files
        protected async Task LoadFiles(string directory)
        {
            foreach (string relativeFilePath in Variant.Files.FileRelativePaths)
            {
                string filePath = Path.Combine(directory, relativeFilePath);
                if (File.Exists(filePath))
                {
                    byte[] data = await File.ReadAllBytesAsync(filePath);
                    Files[relativeFilePath] = data;
                }
            }
        }

        public void GenerateGuids(bool generateAssetId = true, bool generateVersionId = true)
        {
            Variant.GenerateGuids(generateAssetId, generateVersionId);
            if (Variant is UgcGameVariant ugcVar && ugcVar.EngineGameVariantLink is BondAsset engineLink)
            {
                var linkedVariants = LinkedVariants.Where(variant => variant.Variant.AssetIdEqual(engineLink)).ToArray();
                engineLink.GenerateGuids(generateAssetId, generateVersionId);
                foreach (var linkedVariant in linkedVariants)
                {
                    linkedVariant.Variant.SetGuids((Guid)engineLink.AssetId, (Guid)engineLink.VersionId);
                }
            }
        }

        public void SetGuids(Guid? assetId, Guid? versionId)
        {
            Variant.SetGuids(assetId, versionId);
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class VariantAttribute : Attribute
    {
    }

    public enum VariantTypeEnum
    {
        UGC,
        Engine,
        Map
    }
    public class VariantType
    {
        public readonly Type ClassType;
        public readonly string EndpointId;
        public readonly VariantTypeEnum EnumValue;
        public string Name => ClassType.Name;
        private VariantType(Type classType, string endpointId, VariantTypeEnum enumValue)
        {
            ClassType = classType;
            EndpointId = endpointId;
            EnumValue = enumValue;
        }

        public static VariantType FromClassType(Type type)
        {
            return VariantTypes.Find(variantType => variantType.ClassType == type) ?? throw new KeyNotFoundException();
        }

        public static VariantType FromEnum(VariantTypeEnum enumValue)
        {
            return VariantTypes.Find(variantType => variantType.EnumValue == enumValue) ?? throw new KeyNotFoundException();
        }

        public static VariantType? FromEnum(VariantTypeEnum? enumValue)
        {
            return VariantTypes.Find(variantType => variantType.EnumValue == enumValue);
        }

        public List<BondAsset> GetLinks(GameManifest manifest)
        {
            if (this == MapVariant)
            {
                return manifest.MapLinks;
            }
            else if (this == EngineGameVariant)
            {
                return manifest.EngineGameVariantLinks;
            }
            else if (this == UgcGameVariant)
            {
                return manifest.UgcGameVariantLinks;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public List<BondAsset>? GetLinks(CustomsManifest manifest)
        {
            if (this == MapVariant)
            {
                return manifest.MapLinks;
            }
            else if (this == EngineGameVariant)
            {
                return null;
            }
            else if (this == UgcGameVariant)
            {
                return manifest.UgcGameVariantLinks;
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public static readonly VariantType MapVariant = new(typeof(MapVariant), "HIUGC_Discovery_GetMap", VariantTypeEnum.Map);
        public static readonly VariantType UgcGameVariant = new(typeof(UgcGameVariant), "HIUGC_Discovery_GetUgcGameVariant", VariantTypeEnum.UGC);
        public static readonly VariantType EngineGameVariant = new(typeof(EngineGameVariant), "HIUGC_Discovery_GetEngineGameVariant", VariantTypeEnum.Engine);
        public static readonly List<VariantType> VariantTypes = new() { MapVariant, UgcGameVariant, EngineGameVariant };
    }
}
