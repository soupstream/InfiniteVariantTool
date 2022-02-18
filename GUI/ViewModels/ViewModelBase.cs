using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteVariantTool.GUI
{
    public class ViewModel : NotifyPropertyChanged
    {
        private IOService? ioService;
        public IOService IOService
        {
            get => ioService ?? throw new InvalidOperationException("IOService not instantiated");
            set => ioService = value;
        }

        private NavigationService? navService;
        public NavigationService NavigationService
        {
            get => navService ?? throw new InvalidOperationException("NavigationService not instantiated");
            set => navService = value;
        }

        private RelayCommand? closeCommand;
        public RelayCommand CloseCommand
        {
            get
            {
                if (closeCommand == null)
                {
                    closeCommand = new SyncRelayCommand(_ => NavigationService.Close());
                }
                return closeCommand;
            }
        }
    }
}
