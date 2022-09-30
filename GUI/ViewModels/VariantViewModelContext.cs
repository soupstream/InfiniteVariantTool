using InfiniteVariantTool.Core;
using InfiniteVariantTool.Core.BondSchema;
using InfiniteVariantTool.Core.Settings;
using InfiniteVariantTool.Core.Utils;
using InfiniteVariantTool.Core.Variants;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InfiniteVariantTool.GUI
{
    public abstract class VariantViewModelContext : ViewModel
    {
        public VariantViewModel Parent { get; }
        private Predicate<VariantModel> filter;
        private CancellationTokenSource? filterCancelToken;

        public VariantViewModelContext(VariantViewModel parent, Predicate<VariantModel> filter)
        {
            Parent = parent;
            this.filter = filter;
            variants = new();
            variants.CollectionChanged += Variants_CollectionChanged;
            filteredVariants = new();
            selectedVariants = new();
            selectedVariants.CollectionChanged += SelectedVariants_CollectionChanged;
        }

        public async Task UpdateFilter()
        {
            filterCancelToken?.Cancel();
            filterCancelToken = new();
            await Task.Run(() => UpdateFilter(filterCancelToken.Token));
        }

        private void UpdateFilter(CancellationToken cancelToken)
        {
            if (UpdatingVariants)
            {
                return;
            }

            ObservableCollection<VariantModel> filtered = new();
            foreach (VariantModel variant in Variants)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    return;
                }
                if (filter(variant))
                {
                    filtered.Add(variant);
                }
            }
            if (!filtered.SequenceEqual(FilteredVariants))
            {
                FilteredVariants = filtered;
                OnPropertyChange(nameof(Variants));
            }
        }

        public async Task LoadVariants(VariantManager variantManager)
        {
            Loaded = false;
            Loading = true;
            ErrorMessage = null;
            FilteredVariants.Clear();
            try
            {
                Loaded = await LoadVariantsBase(variantManager);
            }
            finally
            {
                Loading = false;
            }
        }

        public abstract Task<bool> LoadVariantsBase(VariantManager variantManager);

        private ObservableCollection<VariantModel> variants;
        public ObservableCollection<VariantModel> Variants
        {
            get => variants;
            set
            {
                if (value != variants)
                {
                    var oldVariants = variants;
                    variants.CollectionChanged -= Variants_CollectionChanged;
                    variants = value;
                    variants.CollectionChanged += Variants_CollectionChanged;
                    Variants_CollectionChanged(null, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, variants, oldVariants));
                    OnPropertyChange(nameof(Variants));
                }
            }
        }

        private ObservableCollection<VariantModel> selectedVariants;
        public ObservableCollection<VariantModel> SelectedVariants
        {
            get => selectedVariants;
            set
            {
                if (value != selectedVariants)
                {
                    selectedVariants.CollectionChanged -= SelectedVariants_CollectionChanged;
                    selectedVariants = value;
                    selectedVariants.CollectionChanged += SelectedVariants_CollectionChanged;
                    OnPropertyChange();
                    SelectedVariants_CollectionChanged(null, null);
                }
            }
        }

        private bool areUserVariantsSelected;
        public bool AreUserVariantsSelected
        {
            get => areUserVariantsSelected;
            set
            {
                if (value != areUserVariantsSelected)
                {
                    areUserVariantsSelected = value;
                    OnPropertyChange();
                    ReinstallVariantsCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool areVariantsSelected;
        public bool AreVariantsSelected
        {
            get => areVariantsSelected;
            set
            {
                if (value != areVariantsSelected)
                {
                    areVariantsSelected = value;
                    OnPropertyChange();
                    ClearSelectionCommand.RaiseCanExecuteChanged();
                    DeleteVariantsCommand.RaiseCanExecuteChanged();
                    AreVariantsSelectedOrChangesQueued = AreVariantsSelected || AreChangesQueued;
                    AreUserVariantsSelected = SelectedVariants.Any(variant => variant.IsUserVariant);
                }
            }
        }

        private bool areVariantsSelectedOrChangesQueued;
        public bool AreVariantsSelectedOrChangesQueued
        {
            get => areVariantsSelectedOrChangesQueued;
            set
            {
                if (value != areVariantsSelectedOrChangesQueued)
                {
                    areVariantsSelectedOrChangesQueued = value;
                    OnPropertyChange();
                }
            }
        }

        private Dictionary<VariantModel, VariantModel> changeQueue = new(); // changed model => original model
        private HashSet<VariantModel> reinstallQueue = new();
        private HashSet<VariantModel> removeQueue = new();

        private bool updatingSelectedVariantsEnabled;
        private bool? selectedVariantsEnabled;
        public bool? SelectedVariantsEnabled
        {
            get => selectedVariantsEnabled;
            set
            {
                if (value != selectedVariantsEnabled)
                {
                    selectedVariantsEnabled = value;
                    if (selectedVariantsEnabled != null)
                    {
                        updatingSelectedVariantsEnabled = true;
                        foreach (var variant in SelectedVariants)
                        {
                            if (variant.Enabled != null) // prevent enabling EngineGameVariants
                            {
                                variant.Enabled = selectedVariantsEnabled;
                            }
                        }
                        updatingSelectedVariantsEnabled = false;
                    }
                    OnPropertyChange();
                }
            }
        }

        private bool canEnableSelectedVariants;
        public bool CanEnableSelectedVariants
        {
            get => canEnableSelectedVariants;
            set
            {
                if (value != canEnableSelectedVariants)
                {
                    canEnableSelectedVariants = value;
                    OnPropertyChange();
                }
            }
        }

        private void SelectedVariants_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs? e)
        {
            if (SelectedVariants.Count == 1)
            {
                SelectedVariant = SelectedVariants[0];
            }
            else
            {
                SelectedVariant = null;
            }

            UpdateSelectedVariantsEnabled();
            AreVariantsSelected = SelectedVariants.Any();
        }

        private void UpdateSelectedVariantsEnabled()
        {
            if (updatingSelectedVariantsEnabled)
            {
                return;
            }

            bool? enabled = null;
            bool canEnable = false;
            foreach (var variant in SelectedVariants)
            {
                if (variant.Enabled != null)
                {
                    if (!canEnable)
                    {
                        enabled = variant.Enabled;
                        canEnable = true;
                    }
                    else if (enabled != variant.Enabled)
                    {
                        enabled = null;
                        break;
                    }
                }
            }
            if (canEnable)
            {
                SelectedVariantsEnabled = enabled;
            }
            CanEnableSelectedVariants = canEnable;
        }

        private void QueueVariantChange(VariantModel variant)
        {
            if (!changeQueue.ContainsKey(variant))
            {
                changeQueue[variant] = new VariantModel(variant);
                AreChangesQueued = true;
            }
        }

        private void QueueVariantReinstall(VariantModel variant)
        {
            reinstallQueue.Add(variant);
            AreChangesQueued = true;
        }

        private void QueueVariantRemoval(VariantModel variant)
        {
            removeQueue.Add(variant);
            Variants.Remove(variant);
            AreChangesQueued = true;
        }

        private bool updatingVariants;
        public bool UpdatingVariants
        {
            get => updatingVariants;
            set
            {
                if (updatingVariants != value)
                {
                    updatingVariants = value;
                    OnPropertyChange();
                    if (!updatingVariants)
                    {
                        Variants_CollectionChanged(null, null);
                    }
                }
            }
        }
        private void QueueVariantRemoval()
        {
            UpdatingVariants = true;
            foreach (var variant in SelectedVariants)
            {
                QueueVariantRemoval(variant);
            }
            UpdatingVariants = false;
        }

        public async Task ApplyVariantChanges()
        {
            if (Parent.VariantManager == null)
            {
                throw new InvalidOperationException("VariantManager is null");
            }

            Parent.ApplyingVariantChanges = true;
            bool reload = false;

            foreach (var item in removeQueue)
            {
                changeQueue.Remove(item);
                await Task.Run(async () =>
                {
                    if (item.IsUserVariant)
                    {
                        Parent.VariantManager.UserCache.RemoveVariant(item.Filename!);
                    }
                    await Parent.VariantManager.RemoveVariant(item.AssetId, item.VersionId, VariantType.FromEnum(item.Type));
                });
            }
            removeQueue.Clear();

            var variantsToModify = changeQueue.Where(entry =>
            {
                VariantModel newVariant = entry.Key;
                VariantModel oldVariant = entry.Value;
                return newVariant.Name != oldVariant.Name
                    || newVariant.Description != oldVariant.Description
                    || newVariant.VersionId != oldVariant.VersionId
                    || newVariant.AssetId != oldVariant.AssetId;
            });

            var variantsToEnable = changeQueue.Where(entry =>
            {
                VariantModel newVariant = entry.Key;
                VariantModel oldVariant = entry.Value;
                return oldVariant.Enabled == false && newVariant.Enabled == true;
            }).ToHashSet();

            var variantsToDisable = changeQueue.Where(entry =>
            {
                VariantModel newVariant = entry.Key;
                VariantModel oldVariant = entry.Value;
                return oldVariant.Enabled == true && newVariant.Enabled == false;
            });

            foreach (var entry in variantsToModify)
            {
                VariantModel newVariant = entry.Key;
                VariantModel oldVariant = entry.Value;
                bool guidChanged = oldVariant.AssetId != newVariant.AssetId || oldVariant.VersionId != newVariant.VersionId;

                VariantAsset loadedVariant;
                if (oldVariant.IsUserVariant)
                {
                    loadedVariant = await VariantAsset.Load(oldVariant.Filename!, guidChanged);
                }
                else
                {
                    loadedVariant = await Parent.VariantManager.GetVariant(oldVariant.AssetId, oldVariant.VersionId, VariantType.FromEnum(oldVariant.Type), guidChanged, false);
                }
                if (guidChanged)
                {
                    loadedVariant.SetGuids(newVariant.AssetId, newVariant.VersionId);
                    await Parent.VariantManager.RemoveVariant(oldVariant.AssetId, oldVariant.VersionId, VariantType.FromEnum(oldVariant.Type));
                    if (oldVariant.Enabled == true)
                    {
                        variantsToEnable.Add(entry);
                    }
                }

                loadedVariant.Variant.PublicName = newVariant.Name;
                loadedVariant.Variant.Description = newVariant.Description;
                await Parent.VariantManager.StoreVariant(loadedVariant);
                reinstallQueue.Remove(newVariant);

                if (oldVariant.IsUserVariant)
                {
                    await loadedVariant.Save(Path.GetDirectoryName(newVariant.Filename!)!);
                }
            }

            foreach (var entry in variantsToEnable)
            {
                VariantModel newVariant = entry.Key;
                VariantModel oldVariant = entry.Value;

                // install user variant if it wasn't already installed
                if (newVariant.IsUserVariant && !variantsToModify.Contains(entry))
                {
                    VariantAsset variant = await VariantAsset.Load(oldVariant.Filename!, true);
                    await Parent.VariantManager.StoreVariant(variant);
                    reinstallQueue.Remove(newVariant);

                    // also install linked variant
                    if (variant.Variant is UgcGameVariant ugcVariant && ugcVariant.EngineGameVariantLink != null)
                    {
                        var engineMetadata = ugcVariant.EngineGameVariantLink;
                        foreach (var searchVariant in Variants)
                        {
                            if (searchVariant.AssetId == (Guid)engineMetadata.AssetId && searchVariant.VersionId == (Guid)engineMetadata.VersionId)
                            {
                                reinstallQueue.Add(searchVariant);
                            }
                        }
                    }
                }

                Parent.VariantManager.SetVariantEnabled(newVariant.AssetId, newVariant.VersionId, VariantType.FromEnum(newVariant.Type), true);
            }

            foreach (var entry in variantsToDisable)
            {
                VariantModel newVariant = entry.Key;
                Parent.VariantManager.SetVariantEnabled(newVariant.AssetId, newVariant.VersionId, VariantType.FromEnum(newVariant.Type), false);
            }

            changeQueue.Clear();

            foreach (var variant in reinstallQueue)
            {
                reload = true;
                await Parent.VariantManager.StoreVariant(await VariantAsset.Load(variant.Filename!, true));
            }
            reinstallQueue.Clear();

            AreChangesQueued = false;
            Parent.ApplyingVariantChanges = false;

            await Parent.VariantManager.Flush();

            if (reload)
            {
                await Parent.LoadVariants();
            }
        }

        public void DiscardVariantChanges()
        {
            UpdatingVariants = true;
            foreach (var variant in removeQueue)
            {
                Variants.Add(variant);
            }
            UpdatingVariants = false;
            removeQueue.Clear();

            foreach (var entry in changeQueue)
            {
                VariantModel newVariant = entry.Key;
                VariantModel oldVariant = entry.Value;
                newVariant.Name = oldVariant.Name;
                newVariant.Description = oldVariant.Description;
                newVariant.Enabled = oldVariant.Enabled;
                newVariant.AssetId = oldVariant.AssetId;
                newVariant.VersionId = oldVariant.VersionId;
            }
            reinstallQueue.Clear();

            AreChangesQueued = false;
            UpdateSelectedVariantsEnabled();
        }

        public async Task SelectEngineGameVariant()
        {
            if (SelectedVariant == null)
            {
                return;
            }

            VariantAsset variant;
            if (SelectedVariant.IsUserVariant)
            {
                variant = await VariantAsset.Load(SelectedVariant.Filename!, false);
            }
            else
            {
                variant = await Parent.VariantManager!.GetVariant(SelectedVariant.AssetId, SelectedVariant.VersionId, VariantType.FromEnum(SelectedVariant.Type), false, false);
            }
            ObservableCollection<VariantModel> newSelection = new();
            if (variant.Variant is UgcGameVariant ugcVariant && ugcVariant.EngineGameVariantLink != null)
            {
                var engineMetadata = ugcVariant.EngineGameVariantLink;
                foreach (var searchVariant in Variants)
                {
                    if (searchVariant.AssetId == (Guid)engineMetadata.AssetId && searchVariant.VersionId == (Guid)engineMetadata.VersionId)
                    {
                        newSelection.Add(searchVariant);
                    }
                }
            }
            
            SelectedVariants = newSelection;
        }

        private VariantModel? selectedVariant;
        public VariantModel? SelectedVariant
        {
            get => selectedVariant;
            set
            {
                if (value != selectedVariant)
                {
                    selectedVariant = value;
                    if (selectedVariant == null)
                    {
                        IsUserVariantSelected = false;
                        IsNonUserVariantSelected = false;
                        IsUgcGameVariantSelected = false;
                    }
                    else
                    {
                        IsUserVariantSelected = selectedVariant.IsUserVariant;
                        IsNonUserVariantSelected = !selectedVariant.IsUserVariant;
                        IsUgcGameVariantSelected = selectedVariant.Type == VariantType.UgcGameVariant.EnumValue;
                    }
                    OnPropertyChange();
                }
            }
        }

        private bool isUserVariantSelected;
        public bool IsUserVariantSelected
        {
            get => isUserVariantSelected;
            set
            {
                if (isUserVariantSelected != value)
                {
                    isUserVariantSelected = value;
                    OnPropertyChange();
                    OpenInExplorerCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool isUgcGameVariantSelected;
        public bool IsUgcGameVariantSelected
        {
            get => isUgcGameVariantSelected;
            set
            {
                if (isUgcGameVariantSelected != value)
                {
                    isUgcGameVariantSelected = value;
                    OnPropertyChange();
                    EngineGameVariantCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool isNonUserVariantSelected;
        public bool IsNonUserVariantSelected
        {
            get => isNonUserVariantSelected;
            set
            {
                if (isNonUserVariantSelected != value)
                {
                    isNonUserVariantSelected = value;
                    OnPropertyChange();
                    ExtractVariantCommand.RaiseCanExecuteChanged();
                }
            }
        }

        private bool areChangesQueued;
        public bool AreChangesQueued
        {
            get => areChangesQueued;
            set
            {
                if (areChangesQueued != value)
                {
                    areChangesQueued = value;
                    OnPropertyChange();
                    ApplyChangesCommand.RaiseCanExecuteChanged();
                    DiscardChangesCommand.RaiseCanExecuteChanged();
                    AreVariantsSelectedOrChangesQueued = AreVariantsSelected || AreChangesQueued;
                }
            }
        }

        private RelayCommand? clearSelectionCommand;
        public RelayCommand ClearSelectionCommand
        {
            get
            {
                if (clearSelectionCommand == null)
                {
                    clearSelectionCommand = new SyncRelayCommand(
                        _ => SelectedVariants.Clear(),
                        _ => AreVariantsSelected);
                }
                return clearSelectionCommand;
            }
        }

        private RelayCommand? deleteVariantsCommand;
        public RelayCommand DeleteVariantsCommand
        {
            get
            {
                if (deleteVariantsCommand == null)
                {
                    deleteVariantsCommand = new SyncRelayCommand(
                        _ => QueueVariantRemoval(),
                        _ => AreVariantsSelected);
                }
                return deleteVariantsCommand;
            }
        }

        private RelayCommand? applyChangesCommand;
        public RelayCommand ApplyChangesCommand
        {
            get
            {
                if (applyChangesCommand == null)
                {
                    applyChangesCommand = new AsyncRelayCommand(
                        async _ =>
                        {
                            DiscardChangesCommand.RaiseCanExecuteChanged();
                            await ApplyVariantChanges();
                        },
                        _ => AreChangesQueued && !Parent.ApplyingVariantChanges);
                }
                return applyChangesCommand;
            }
        }

        private RelayCommand? discardChangesCommand;
        public RelayCommand DiscardChangesCommand
        {
            get
            {
                if (discardChangesCommand == null)
                {
                    discardChangesCommand = new SyncRelayCommand(
                        _ => DiscardVariantChanges(),
                        _ => AreChangesQueued && !ApplyChangesCommand.IsExecuting);
                }
                return discardChangesCommand;
            }
        }

        private RelayCommand? openInExplorerCommand;
        public RelayCommand OpenInExplorerCommand
        {
            get
            {
                if (openInExplorerCommand == null)
                {
                    openInExplorerCommand = new SyncRelayCommand(
                        _ => IOService.ShowInExplorer(SelectedVariant!.Filename!),
                        _ => IsUserVariantSelected);
                }
                return openInExplorerCommand;
            }
        }

        private RelayCommand? reinstallVariantsCommand;
        public RelayCommand ReinstallVariantsCommand
        {
            get
            {
                if (reinstallVariantsCommand == null)
                {
                    reinstallVariantsCommand = new SyncRelayCommand(
                        _ =>
                        {
                            foreach (var variant in SelectedVariants)
                            {
                                QueueVariantReinstall(variant);
                            }
                        },
                        _ => AreUserVariantsSelected);
                }
                return reinstallVariantsCommand;
            }
        }

        private RelayCommand? extractVariantCommand;
        public RelayCommand ExtractVariantCommand
        {
            get
            {
                if (extractVariantCommand == null)
                {
                    extractVariantCommand = new SyncRelayCommand(
                        _ => NavigationService.OpenExtractWindow(Parent.VariantManager!, SelectedVariant!),
                        _ => IsNonUserVariantSelected);
                }
                return extractVariantCommand;
            }
        }

        private RelayCommand? engineGameVariantCommand;
        public RelayCommand EngineGameVariantCommand
        {
            get
            {
                if (engineGameVariantCommand == null)
                {
                    engineGameVariantCommand = new AsyncRelayCommand(
                        async _ => await SelectEngineGameVariant(),
                        _ => IsUgcGameVariantSelected);
                }
                return engineGameVariantCommand;
            }
        }

        private void Variants_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs? e)
        {
            if (e?.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    var variant = (VariantModel)item;
                    variant.BeforePropertyChanged -= Variant_BeforePropertyChanged;
                    variant.PropertyChanged -= Variant_PropertyChanged;
                }
            }
            _ = UpdateFilter();
            if (e?.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    var variant = (VariantModel)item;
                    variant.BeforePropertyChanged += Variant_BeforePropertyChanged;
                    variant.PropertyChanged += Variant_PropertyChanged;
                }
            }
        }

        private void Variant_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(VariantModel.Enabled))
            {
                UpdateSelectedVariantsEnabled();
            }
        }

        private void Variant_BeforePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            VariantModel variant = (VariantModel)sender!;
            QueueVariantChange(variant);
        }

        private ObservableCollection<VariantModel> filteredVariants;
        public ObservableCollection<VariantModel> FilteredVariants
        {
            get => filteredVariants;
            set
            {
                if (value != filteredVariants)
                {
                    filteredVariants = value;
                    OnPropertyChange(nameof(FilteredVariants));
                }
            }
        }

        private bool loaded;
        public bool Loaded
        {
            get => loaded;
            set
            {
                if (value != loaded)
                {
                    loaded = value;
                    OnPropertyChange(nameof(Loaded));
                }
            }
        }

        private bool loading;
        public bool Loading
        {
            get => loading;
            set
            {
                if (value != loading)
                {
                    loading = value;
                    OnPropertyChange(nameof(Loading));
                }
            }
        }

        private string? errorMessage;
        public string? ErrorMessage
        {
            get => errorMessage;
            set
            {
                if (value != errorMessage)
                {
                    errorMessage = value;
                    OnPropertyChange(nameof(ErrorMessage));
                }
            }
        }
    }

    public class UserVariantsViewModelContext : VariantViewModelContext
    {
        public UserVariantsViewModelContext(VariantViewModel parent, Predicate<VariantModel> filter)
            : base(parent, filter)
        {

        }

        public override async Task<bool> LoadVariantsBase(VariantManager variantManager)
        {
            if (!Directory.Exists(UserSettings.Instance.VariantDirectory))
            {
                if (Directory.Exists(Path.GetDirectoryName(UserSettings.Instance.VariantDirectory)))
                {
                    Directory.CreateDirectory(UserSettings.Instance.VariantDirectory);
                }
                else
                {
                    ErrorMessage = "No folder found at " + UserSettings.Instance.VariantDirectory + ".\r\nSet the correct variant location in Settings and try again.";
                    return false;
                }
            }

            await variantManager.UserCache.LoadEntries();
            variantManager.UpdateVariantsStatus();
            Variants = new(variantManager.UserCache.Entries
                .Select(entry => new VariantModel(entry, true)));
            if (Variants.Count == 0)
            {
                ErrorMessage = "No variants installed.\r\nYou can install variants with File > Install variant...";
            }
            return true;
        }

        private async Task PickAndInstallVariants()
        {
            if (IOService.TrySelectPath(out string? path, PathSelectType.Open, "Install variant", "Zip files (*.zip)|*.zip"))
            {
                DirectoryFileNameDeduper deduper = new(UserSettings.Instance.VariantDirectory);
                string outputDir = deduper.Dedupe(Path.Combine(UserSettings.Instance.VariantDirectory, Path.GetFileNameWithoutExtension(path)));
                await Task.Run(() => ZipFile.ExtractToDirectory(path, outputDir));
                await InstallVariants(outputDir);
            }
        }

        private async Task InstallVariants(string variantsDir)
        {
            if (Parent.VariantManager == null)
            {
                throw new InvalidOperationException();
            }

            Parent.ApplyingVariantChanges = true;
            foreach (string variantFile in VariantAsset.FindVariants(variantsDir))
            {
                VariantAsset variant = await VariantAsset.Load(variantFile, true);
                await Parent.VariantManager.StoreVariant(variant);
                Parent.VariantManager.SetVariantEnabled(variant, true);
            }
            await Parent.VariantManager.Flush();
            Parent.ApplyingVariantChanges = false;
            await Parent.LoadVariants();
        }

        private RelayCommand? installVariantCommand;
        public RelayCommand InstallVariantCommand
        {
            get
            {
                if (installVariantCommand == null)
                {
                    installVariantCommand = new AsyncRelayCommand(
                        async _ => await PickAndInstallVariants(),
                        _ => Loaded && !Parent.ApplyingVariantChanges);
                }
                return installVariantCommand;
            }
        }
    }

    public class AllVariantsViewModelContext : VariantViewModelContext
    {
        public AllVariantsViewModelContext(VariantViewModel parent, Predicate<VariantModel> filter)
            : base(parent, filter)
        {

        }

        public override async Task<bool> LoadVariantsBase(VariantManager variantManager)
        {
            Variants = new(variantManager.VariantAssets
                .Select(entry => new VariantModel(entry, false)));
            if (Variants.Count == 0)
            {
                ErrorMessage = "Variant cache is empty.";
            }
            return true;
        }
    }

}
