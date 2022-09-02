using InfiniteVariantTool.Core;
using InfiniteVariantTool.Core.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteVariantTool.GUI
{
    public class UnpackCacheFileViewModel : FileActionViewModel
    {
        public UnpackCacheFileViewModel()
            : base("Unpack", "cache file", "All files (*.*)|*.*|Bin files (*.bin)|*.bin", "XML files (*.xml)|*.xml|All files (*.*)|*.*")
        {

        }

        public override string FilenameFunc(string filename)
        {
            return BondReader.SuggestFilename(filename);
        }

        public override void FileAction(string inputFilename, string outputFilename)
        {
            BondReader br = new(inputFilename);
            br.Save(outputFilename);
        }
    }

    public class PackCacheFileViewModel : FileActionViewModel
    {
        public PackCacheFileViewModel()
            : base("Pack", "cache file", "XML files (*.xml)|*.xml|All files (*.*)|*.*", "All files (*.*)|*.*|Bin files (*.bin)|*.bin")
        {

        }

        public override string FilenameFunc(string filename)
        {
            return BondWriter.SuggestFilename(filename);
        }

        public override void FileAction(string inputFilename, string outputFilename)
        {
            BondWriter bw = new(inputFilename);
            bw.Save(outputFilename);
        }
    }

    public class UnpackLuaBundleViewModel : FileActionViewModel
    {
        public UnpackLuaBundleViewModel()
            : base("Unpack", "lua bundle", "Luabundle files (*.luabundle;*.debugscriptsource)|*.luabundle;*.debugscriptsource|All files (*.*)|*.*", "Directory|.")
        {

        }

        public override string FilenameFunc(string filename)
        {
            return LuaBundleUtils.SuggestUnpackFilename(filename);
        }

        public override void FileAction(string inputFilename, string outputFilename)
        {
            LuaBundle bundle = LuaBundle.Load(inputFilename);
            bundle.Save(outputFilename);
        }
    }

    public class PackLuaBundleViewModel : FileActionViewModel
    {
        public PackLuaBundleViewModel()
            : base("Pack", "lua bundle", "Directory|.", "Luabundle files (*.luabundle)|*.luabundle|All files (*.*)|*.*")
        {

        }

        public override string FilenameFunc(string filename)
        {
            return LuaBundleUtils.SuggestPackFilename(filename);
        }

        public override void FileAction(string inputFilename, string outputFilename)
        {
            LuaBundle bundle = LuaBundle.Unpack(inputFilename, Game.HaloInfinite);
            byte[] packed = bundle.Pack();
            File.WriteAllBytes(outputFilename, packed);
        }
    }

    public abstract class FileActionViewModel : ViewModel
    {
        public string Title { get; }
        public string ActionLabel { get; }
        private string name;
        private string inputFilter;
        private string outputFilter;

        public FileActionViewModel(string actionLabel, string name, string inputFilter, string outputFilter)
        {
            ActionLabel = actionLabel;
            this.name = name;
            this.inputFilter = inputFilter;
            this.outputFilter = outputFilter;
            Title = $"{actionLabel} {name}";

            inputPath = "";
            outputPath = "";
            defaultOutputPath = "";
        }

        public abstract string FilenameFunc(string filename);
        public abstract void FileAction(string inputFilename, string outputFilename);

        private string inputPath;
        public string InputPath
        {
            get => inputPath;
            set
            {
                if (value != inputPath)
                {
                    inputPath = value;
                    OnPropertyChange(nameof(InputPath));
                    ExecuteActionCommand.RaiseCanExecuteChanged();

                    if (inputPath == "")
                    {
                        DefaultOutputPath = "";
                    }
                    else
                    {
                        DefaultOutputPath = FilenameFunc(inputPath);
                    }
                }
            }
        }

        public RelayCommand PickInputPathCommand => new SyncRelayCommand(_ =>
        {
            if (IOService.TrySelectPath(out string? path, PathSelectType.Open, $"{ActionLabel} {name} - Input", inputFilter))
            {
                InputPath = path;
            }
        });

        private string outputPath;
        public string OutputPath
        {
            get => outputPath;
            set
            {
                if (value != outputPath)
                {
                    outputPath = value;
                    OnPropertyChange(nameof(OutputPath));
                }
            }
        }

        public RelayCommand PickOutputPathCommand => new SyncRelayCommand(_ =>
        {
            if (IOService.TrySelectPath(out string? path, PathSelectType.Save, $"{ActionLabel} {name} - Output", outputFilter))
            {
                OutputPath = path;
            }
        });

        private string defaultOutputPath;
        public string DefaultOutputPath
        {
            get => defaultOutputPath;
            set
            {
                if (value != defaultOutputPath)
                {
                    defaultOutputPath = value;
                    OnPropertyChange(nameof(DefaultOutputPath));
                }
            }
        }

        private async Task ExecuteAction()
        {
            string actualOutputPath = OutputPath;
            if (actualOutputPath == "")
            {
                actualOutputPath = DefaultOutputPath;
            }
            string result;
            bool success;
            try
            {
                await Task.Run(() => FileAction(InputPath, actualOutputPath));
                result = "Success\r\n\r\nSaved to " + actualOutputPath;
                success = true;
            }
            catch (Exception ex)
            {
                result = "Error\r\n\r\n" + ex;
                success = false;
            }
            NavigationService.OpenResultWindow(result, success ? actualOutputPath : null);
        }

        private RelayCommand? executeActionCommand;
        public RelayCommand ExecuteActionCommand
        {
            get
            {
                if (executeActionCommand == null)
                {
                    executeActionCommand = new AsyncRelayCommand(
                        async _ => await ExecuteAction(),
                        _ => InputPath != "");
                }
                return executeActionCommand;
            }
        }
    }
}
