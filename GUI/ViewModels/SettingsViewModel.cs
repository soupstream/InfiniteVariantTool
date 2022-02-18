using InfiniteVariantTool.Core;
using InfiniteVariantTool.Core.Settings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace InfiniteVariantTool.GUI
{
    public class SettingsViewModel : ViewModel
    {
        public SettingsViewModel()
        {
            gameDirectory = UserSettings.Instance.GameDirectory;
            variantDirectory = UserSettings.Instance.VariantDirectory;
            selectedLanguageIndex = LanguageToIndex(UserSettings.Instance.Language);
            checkForUpdates = UserSettings.Instance.CheckForUpdates;
        }

        public void Save()
        {
            UserSettings.Instance.GameDirectory = GameDirectory;
            UserSettings.Instance.VariantDirectory = VariantDirectory;
            UserSettings.Instance.Language = IndexToLanguage(SelectedLanguageIndex);
            UserSettings.Instance.CheckForUpdates = CheckForUpdates;
            UserSettings.Instance.Save();
        }

        public RelayCommand SaveCommand => new SyncRelayCommand(_ => Save());
        public RelayCommand SaveAndCloseCommand => new SyncRelayCommand(_ =>
        {
            Save();
            NavigationService.Close();
        });

        private string gameDirectory;
        public string GameDirectory
        {
            get => gameDirectory;
            set
            {
                if (value != gameDirectory)
                {
                    gameDirectory = value;
                    OnPropertyChange(nameof(GameDirectory));
                }
            }
        }

        public RelayCommand PickGameDirectoryCommand => new SyncRelayCommand(_ =>
        {
            if (IOService.TrySelectPath(out string? path, PathSelectType.Open, "Select game folder", "Directory|."))
            {
                GameDirectory = path;
            }
        });

        private string variantDirectory;
        public string VariantDirectory
        {
            get => variantDirectory;
            set
            {
                if (value != variantDirectory)
                {
                    variantDirectory = value;
                    OnPropertyChange(nameof(VariantDirectory));
                }
            }
        }

        public RelayCommand PickVariantDirectoryCommand => new SyncRelayCommand(_ =>
        {
            if (IOService.TrySelectPath(out string? path, PathSelectType.Open, "Select variant folder", "Directory|."))
            {
                VariantDirectory = path;
            }
        });

        private int selectedLanguageIndex;
        public int SelectedLanguageIndex
        {
            get => selectedLanguageIndex;
            set
            {
                if (value != selectedLanguageIndex)
                {
                    selectedLanguageIndex = value;
                    OnPropertyChange(nameof(SelectedLanguageIndex));
                }
            }
        }

        private List<LanguageModel>? languageOptions;
        public List<LanguageModel> LanguageOptions
        {
            get
            {
                if (languageOptions == null)
                {
                    languageOptions = new();
                    languageOptions.Add(new LanguageModel("auto", "auto"));
                    foreach (var lang in VariantManager.LanguageCodes)
                    {
                        if (lang.Name != "")
                        {
                            languageOptions.Add(new LanguageModel(lang.Name, lang.Code));
                        }
                    }
                }
                return languageOptions;
            }
        }

        private bool checkForUpdates;
        public bool CheckForUpdates
        {
            get => checkForUpdates;
            set
            {
                if (value != checkForUpdates)
                {
                    checkForUpdates = value;
                    OnPropertyChange(nameof(CheckForUpdates));
                }
            }
        }

        private RelayCommand? openGameDirectoryInExplorerCommand;
        public RelayCommand OpenInExplorerCommand
        {
            get
            {
                if (openGameDirectoryInExplorerCommand == null)
                {
                    openGameDirectoryInExplorerCommand = new SyncRelayCommand(
                        _ => IOService.ShowInExplorer(GameDirectory));
                }
                return openGameDirectoryInExplorerCommand;
            }
        }

        private RelayCommand? openVariantDirectoryInExplorerCommand;
        public RelayCommand OpenVariantDirectoryInExplorerCommand
        {
            get
            {
                if (openVariantDirectoryInExplorerCommand == null)
                {
                    openVariantDirectoryInExplorerCommand = new SyncRelayCommand(
                        _ => IOService.ShowInExplorer(VariantDirectory));
                }
                return openVariantDirectoryInExplorerCommand;
            }
        }

        private int LanguageToIndex(string code)
        {
            return Math.Max(0, LanguageOptions.FindIndex(lang => lang.Code == code));
        }

        private string IndexToLanguage(int index)
        {
            if (index < 0 || index >= LanguageOptions.Count)
            {
                index = 0;
            }
            return LanguageOptions[index].Code;
        }
    }
}
