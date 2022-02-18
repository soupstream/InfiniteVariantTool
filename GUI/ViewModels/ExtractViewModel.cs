using InfiniteVariantTool.Core;
using InfiniteVariantTool.Core.Settings;
using InfiniteVariantTool.Core.Utils;
using InfiniteVariantTool.Core.Variants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteVariantTool.GUI
{
    public class ExtractViewModel : ViewModel
    {
        private VariantManager variantManager;
        private VariantModel variant;
        public ExtractViewModel(VariantManager variantManager, VariantModel variant)
        {
            this.variantManager = variantManager;
            this.variant = variant;
        }

        public string DefaultOutputDirectory
        {
            get
            {
                DirectoryFileNameDeduper deduper = new(UserSettings.Instance.VariantDirectory);
                string filename = Util.MakeValidFilename(variant.Name);
                return deduper.Dedupe(Path.Combine(UserSettings.Instance.VariantDirectory, filename));
            }
        }

        public bool IsUgcGameVariant => variant.Type == VariantType.UgcGameVariant;

        private string outputDirectory = "";
        public string OutputDirectory
        {
            get => outputDirectory;
            set
            {
                if (value != outputDirectory)
                {
                    outputDirectory = value;
                    OnPropertyChange();
                }
            }
        }

        private bool unpackXmlContent = true;
        public bool UnpackXmlContent
        {
            get => unpackXmlContent;
            set
            {
                if (value != unpackXmlContent)
                {
                    unpackXmlContent = value;
                    OnPropertyChange();
                }
            }
        }

        private bool unpackLuaBundles = true;
        public bool UnpackLuaBundles
        {
            get => unpackLuaBundles;
            set
            {
                if (value != unpackLuaBundles)
                {
                    unpackLuaBundles = value;
                    OnPropertyChange();
                }
            }
        }

        private bool extractEngineGameVariant = true;
        public bool ExtractEngineGameVariant
        {
            get => extractEngineGameVariant;
            set
            {
                if (value != extractEngineGameVariant)
                {
                    extractEngineGameVariant = value;
                    OnPropertyChange();
                }
            }
        }

        private bool generateNewAssetId = true;
        public bool GenerateNewAssetId
        {
            get => generateNewAssetId;
            set
            {
                if (value != generateNewAssetId)
                {
                    generateNewAssetId = value;
                    OnPropertyChange();
                }
            }
        }

        private bool generateNewVersionId = true;
        public bool GenerateNewVersionId
        {
            get => generateNewVersionId;
            set
            {
                if (value != generateNewVersionId)
                {
                    generateNewVersionId = value;
                    OnPropertyChange();
                }
            }
        }

        private bool downloadLuaSource;
        public bool DownloadLuaSource
        {
            get => downloadLuaSource;
            set
            {
                if (value != downloadLuaSource)
                {
                    downloadLuaSource = value;
                    OnPropertyChange();
                }
            }
        }

        public RelayCommand PickOutputDirectoryCommand => new SyncRelayCommand(_ =>
        {
            if (IOService.TrySelectPath(out string? path, PathSelectType.Open, "Select output folder", "Directory|."))
            {
                OutputDirectory = path;
            }
        });

        private async Task ExtractVariant()
        {
            string output;
            bool success;
            string outputDirectory = OutputDirectory == "" ? DefaultOutputDirectory : OutputDirectory;
            try
            {

                var loadedVariant = await variantManager.GetVariant(variant.AssetId, variant.VersionId, null, null, null, ExtractEngineGameVariant, true, true, downloadLuaSource);
                if (GenerateNewAssetId)
                {
                    loadedVariant.GenerateAssetId();
                }
                if (GenerateNewVersionId)
                {
                    loadedVariant.GenerateVersionId();
                }
                loadedVariant.UnpackBondContent = UnpackXmlContent;
                loadedVariant.UnpackLuaBundles = UnpackLuaBundles;
                await Task.Run(() => loadedVariant.Save(outputDirectory));
                output = "Success\r\n\r\nVariant extracted to " + outputDirectory;
                success = true;
            }
            catch (Exception ex)
            {
                output = "Error\r\n\r\n" + ex;
                success = false;
            }
            NavigationService.OpenResultWindow(output, success ? outputDirectory : null);
        }

        private RelayCommand? extractCommand; 
        public RelayCommand ExtractCommand
        {
            get
            {
                if (extractCommand == null)
                {
                    extractCommand = new AsyncRelayCommand(async _ => await ExtractVariant());
                }
                return extractCommand;
            }
        }
    }
}
