using InfiniteVariantTool.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InfiniteVariantTool.GUI
{
    public class UpdaterViewModel : ViewModel
    {
        public bool CheckedForUpdate { get; set; }

        public UpdaterViewModel(Version newVersion)
        {
            this.newVersion = newVersion;
            CheckedForUpdate = true;
        }

        public UpdaterViewModel()
        {
            CheckedForUpdate = false;
        }

        public async Task<bool> CheckForUpdates()
        {
            NewVersion = null;
            Updater updater = new();
            var latestVersion = await updater.GetLatestVersion();
            if (latestVersion != null && latestVersion > CurrentVersion)
            {
                NewVersion = latestVersion;
                return true;
            }
            else
            {
                UpToDate = true;
            }
            return false;
        }

        private bool upToDate;
        public bool UpToDate
        {
            get => upToDate;
            set
            {
                if (value != upToDate)
                {
                    upToDate = value;
                    OnPropertyChange();
                }
            }
        }

        public Version CurrentVersion => Updater.CurrentVersion;
        public string CurrentVersionStr => CurrentVersion.ToString(3);

        public string? NewVersionStr => NewVersion?.ToString(3);
        private Version? newVersion;
        public Version? NewVersion
        {
            get => newVersion;
            set
            {
                if (value != newVersion)
                {
                    newVersion = value;
                    OnPropertyChange();
                    OnPropertyChange(nameof(NewVersionStr));
                }
            }
        }

        public string UpdateUrl => Updater.GitHubReleaseUrl;

        private RelayCommand? openUpdatePageCommand;
        public RelayCommand OpenUpdatePageCommand
        {
            get
            {
                if (openUpdatePageCommand == null)
                {
                    openUpdatePageCommand = new SyncRelayCommand(_ =>
                    {
                        var ps = new ProcessStartInfo(Updater.GitHubReleaseUrl)
                        {
                            UseShellExecute = true,
                            Verb = "open"
                        };
                        Process.Start(ps);
                        NavigationService.Close();
                    });
                }
                return openUpdatePageCommand;
            }
        }

        private RelayCommand? checkForUpdatesCommand;
        public RelayCommand CheckForUpdatesCommand
        {
            get
            {
                if (checkForUpdatesCommand == null)
                {
                    checkForUpdatesCommand = new AsyncRelayCommand(async _ => await CheckForUpdates());
                }
                return checkForUpdatesCommand;
            }
        }
    }
}
