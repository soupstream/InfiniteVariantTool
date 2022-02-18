using InfiniteVariantTool.Core;
using InfiniteVariantTool.Core.Settings;
using InfiniteVariantTool.Core.Utils;
using InfiniteVariantTool.Core.Variants;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace InfiniteVariantTool.GUI
{
    public class VariantViewModel : ViewModel
    {
        public VariantManager? VariantManager { get; private set; }
        public UserVariantsViewModelContext UserVariantsContext { get; }
        public AllVariantsViewModelContext AllVariantsContext { get; }

        public VariantViewModel(IOService ioService, NavigationService navigationService)
        {
            IOService = ioService;
            NavigationService = navigationService;
            UserVariantsContext = new UserVariantsViewModelContext(this, VariantFilter)
            {
                IOService = ioService,
                NavigationService = navigationService
            };
            UserVariantsContext.PropertyChanged += OnContextPropertyChanged;
            AllVariantsContext = new AllVariantsViewModelContext(this, VariantFilter)
            {
                IOService = ioService,
                NavigationService = navigationService
            };
            AllVariantsContext.PropertyChanged += OnContextPropertyChanged;
        }

        public async Task LoadVariants()
        {
            UserVariantsContext.DiscardChangesCommand.Execute(null);
            AllVariantsContext.DiscardChangesCommand.Execute(null);
            if (!File.Exists(Path.Combine(UserSettings.Instance.GameDirectory, Constants.GameExeName)))
            {
                string errorMessage = "Halo Infinite not found at " + UserSettings.Instance.GameDirectory + ".\r\nSet the correct game location in Settings and try again.";
                UserVariantsContext.ErrorMessage = errorMessage;
                AllVariantsContext.ErrorMessage = errorMessage;
                UserVariantsContext.Variants.Clear();
                AllVariantsContext.Variants.Clear();
                UserVariantsContext.Loaded = false;
                AllVariantsContext.Loaded = false;
                return;
            }

            VariantManager = new VariantManager(UserSettings.Instance.GameDirectory);
            await AllVariantsContext.LoadVariants(VariantManager);
            if (AllVariantsContext.Loaded)
            {
                await UserVariantsContext.LoadVariants(VariantManager);
            }
            else
            {
                UserVariantsContext.ErrorMessage = AllVariantsContext.ErrorMessage;
            }
        }

        private RelayCommand? loadVariantsCommand;
        public RelayCommand LoadVariantsCommand
        {
            get
            {
                if (loadVariantsCommand == null)
                {
                    loadVariantsCommand = new AsyncRelayCommand(
                        async _ => await LoadVariants(),
                        _ => !Loading && !ApplyingVariantChanges);
                }
                return loadVariantsCommand;
            }
        }

        private bool VariantFilter(VariantModel variant)
        {
            bool result = variant.Type switch
            {
                VariantType.MapVariant => ShowMapVariants,
                VariantType.UgcGameVariant => ShowUgcGameVariants,
                VariantType.EngineGameVariant => ShowEngineGameVariants,
                _ => true
            };
            if (variant.Enabled != null)
            {
                result = result && ((variant.Enabled.Value && ShowEnabledVariants)
                    || (!variant.Enabled.Value && ShowDisabledVariants));
            }
            if (SearchText != "")
            {
                result = result
                    && (variant.Name.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
                    || variant.Description.Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
                    || variant.Type.ToString().Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
                    || variant.VersionId.ToString().Contains(SearchText, StringComparison.CurrentCultureIgnoreCase)
                    || variant.AssetId.ToString().Contains(SearchText, StringComparison.CurrentCultureIgnoreCase));
            }
            return result;
        }

        private void UpdateFilter()
        {
            _ = UserVariantsContext.UpdateFilter();
            _ = AllVariantsContext.UpdateFilter();
        }

        private bool applyingVariantChanges;
        public bool ApplyingVariantChanges
        {
            get => applyingVariantChanges;
            set
            {
                if (value != applyingVariantChanges)
                {
                    applyingVariantChanges = value;
                    OnPropertyChange();
                    UserVariantsContext.ApplyChangesCommand.RaiseCanExecuteChanged();
                    AllVariantsContext.ApplyChangesCommand.RaiseCanExecuteChanged();
                    LoadVariantsCommand.RaiseCanExecuteChanged();
                    UserVariantsContext.InstallVariantCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private void OnContextPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Loaded))
            {
                Loaded = AllVariantsContext.Loaded && UserVariantsContext.Loaded;
            }
            else if (e.PropertyName == nameof(Loading))
            {
                Loading = AllVariantsContext.Loading || UserVariantsContext.Loading;
            }
        }

        private string searchText = "";
        public string SearchText
        {
            get => searchText;
            set
            {
                if (value != searchText)
                {
                    searchText = value;
                    OnPropertyChange(nameof(SearchText));
                    UpdateFilter();
                }
            }
        }

        private bool showMapVariants = true;
        public bool ShowMapVariants
        {
            get => showMapVariants;
            set
            {
                if (value != showMapVariants)
                {
                    showMapVariants = value;
                    OnPropertyChange(nameof(ShowMapVariants));
                    UpdateFilter();
                }
            }
        }

        private bool showUgcGameVariants = true;
        public bool ShowUgcGameVariants
        {
            get => showUgcGameVariants;
            set
            {
                if (value != showUgcGameVariants)
                {
                    showUgcGameVariants = value;
                    OnPropertyChange(nameof(ShowUgcGameVariants));
                    UpdateFilter();
                }
            }
        }

        private bool showEngineGameVariants = true;
        public bool ShowEngineGameVariants
        {
            get => showEngineGameVariants;
            set
            {
                if (value != showEngineGameVariants)
                {
                    showEngineGameVariants = value;
                    OnPropertyChange(nameof(ShowEngineGameVariants));
                    UpdateFilter();
                }
            }
        }

        private bool showEnabledVariants = true;
        public bool ShowEnabledVariants
        {
            get => showEnabledVariants;
            set
            {
                if (value != showEnabledVariants)
                {
                    showEnabledVariants = value;
                    OnPropertyChange();
                    UpdateFilter();
                }
            }
        }

        private bool showDisabledVariants = true;
        public bool ShowDisabledVariants
        {
            get => showDisabledVariants;
            set
            {
                if (value != showDisabledVariants)
                {
                    showDisabledVariants = value;
                    OnPropertyChange();
                    UpdateFilter();
                }
            }
        }

        private bool loading;
        public bool Loading
        {
            get => loading;
            set
            {
                if (loading != value)
                {
                    loading = value;
                    OnPropertyChange(nameof(Loading));
                    LoadVariantsCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool loaded;
        public bool Loaded
        {
            get => loaded;
            set
            {
                if (loaded != value)
                {
                    loaded = value;
                    OnPropertyChange(nameof(Loaded));
                    UserVariantsContext.InstallVariantCommand.RaiseCanExecuteChanged();
                }
            }
        }
    }
}
