using InfiniteVariantTool.Core.Cache;
using InfiniteVariantTool.Core.Variants;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteVariantTool.GUI
{
    public class VariantModel : NotifyPropertyChanged
    {
        public VariantModel()
        {
            name = "";
            description = "";
        }

        public VariantModel(VariantEntry entry)
        {
            name = entry.Metadata.PublicName;
            description = entry.Metadata.Description;
            enabled = entry.Enabled;
            assetId = entry.Metadata.AssetId;
            versionId = entry.Metadata.VersionId;
            Type = entry.Type;
            Filename = null;
            IsUserVariant = false;
        }

        public VariantModel(UserVariantEntry entry)
        {
            name = entry.Metadata.Base.PublicName;
            description = entry.Metadata.Base.Description;
            enabled = entry.Enabled;
            assetId = entry.Metadata.Base.AssetId;
            versionId = entry.Metadata.Base.VersionId;
            Type = entry.Metadata.Type;
            Filename = entry.Path;
            IsUserVariant = true;
        }

        public VariantModel(VariantModel variant)
        {
            name = variant.name;
            description = variant.description;
            enabled = variant.enabled;
            assetId = variant.assetId;
            versionId = variant.versionId;
            Type = variant.Type;
            Filename = variant.Filename;
            IsUserVariant = variant.IsUserVariant;
        }

        protected string name;
        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    OnBeforePropertyChange();
                    name = value;
                    OnPropertyChange();
                }
            }
        }

        protected string description;
        public string Description
        {
            get => description;
            set
            {
                if (description != value)
                {
                    OnBeforePropertyChange();
                    description = value;
                    OnPropertyChange();
                }
            }
        }

        protected bool? enabled;
        public bool? Enabled
        {
            get => enabled;
            set
            {
                if (enabled != value)
                {
                    OnBeforePropertyChange();
                    enabled = value;
                    OnPropertyChange();
                }
            }
        }

        protected Guid assetId;
        public Guid AssetId
        {
            get => assetId;
            set
            {
                if (assetId != value)
                {
                    OnBeforePropertyChange();
                    assetId = value;
                    OnPropertyChange();
                }
            }
        }

        protected Guid versionId;
        public Guid VersionId
        {
            get => versionId;
            set
            {
                if (versionId != value)
                {
                    OnBeforePropertyChange();
                    versionId = value;
                    OnPropertyChange();
                }
            }
        }

        public VariantType Type { get; set; }
        public string? Filename { get; set; }
        public bool IsUserVariant { get; set; }
    }
}
