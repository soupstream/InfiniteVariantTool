using InfiniteVariantTool.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteVariantTool.GUI
{
    public class MainViewModel : ViewModel
    {
        public VariantViewModel VariantViewModel { get; }
        public MainViewModel(IOService ioService, NavigationService navigationService)
        {
            IOService = ioService;
            NavigationService = navigationService;
            VariantViewModel = new(ioService, navigationService);
        }

        private async Task AutoCheckForUpdate()
        {
            Version? latestVersion;
            try
            {
                Updater updater = new();
                latestVersion = await updater.GetLatestVersion();
            }
            catch
            {
                // fail silently
                return;
            }
            if (latestVersion != null && latestVersion > Updater.CurrentVersion)
            {
                NavigationService.OpenUpdateWindow(latestVersion);
            }
        }

        public RelayCommand OpenSettingsWindowCommand => new SyncRelayCommand(_ => NavigationService.OpenSettingsWindow());
        public RelayCommand OpenHashUrlWindowCommand => new SyncRelayCommand(_ => NavigationService.OpenHashUrlWindow(VariantViewModel.VariantManager!), _ => VariantViewModel.VariantManager != null);
        public RelayCommand OpenUnpackCacheFileWindowCommand => new SyncRelayCommand(_ => NavigationService.OpenUnpackCacheFileWindow());
        public RelayCommand OpenPackCacheFileWindowCommand => new SyncRelayCommand(_ => NavigationService.OpenPackCacheFileWindow());
        public RelayCommand OpenUnpackLuaBundleWindowCommand => new SyncRelayCommand(_ => NavigationService.OpenUnpackLuaBundleWindow());
        public RelayCommand OpenPackLuaBundleWindowCommand => new SyncRelayCommand(_ => NavigationService.OpenPackLuaBundleWindow());
        public RelayCommand OpenAboutWindowCommand => new SyncRelayCommand(_ => NavigationService.OpenAboutWindow());
        public RelayCommand ManualCheckForUpdateCommand => new SyncRelayCommand(_ => NavigationService.OpenUpdateWindow());
        public RelayCommand AutoCheckForUpdateCommand => new AsyncRelayCommand(async _ => await AutoCheckForUpdate());
    }
}
