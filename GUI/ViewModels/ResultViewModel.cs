using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InfiniteVariantTool.GUI
{
    public class ResultViewModel : ViewModel
    {
        public string Output { get; set; }
        public string? Path { get; set; }
        public ResultViewModel(string output, string? path)
        {
            Output = output;
            Path = path;
        }

        public RelayCommand CopyCommand => new SyncRelayCommand(_ => Clipboard.SetText(Output));
        public RelayCommand OpenInExplorerCommand => new SyncRelayCommand(
            _ => IOService.ShowInExplorer(Path!),
            _ => Path != null);
    }
}
