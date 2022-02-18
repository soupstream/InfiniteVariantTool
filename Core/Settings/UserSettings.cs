
using System.IO;

namespace InfiniteVariantTool.Core.Settings
{
    // Properties.Settings can't share settings beetween different executables so implement my own
    
    public class UserSettings : SettingsBase
    {
        public UserSettings(string name)
            : base(name)
        {
        }

        private static UserSettings? instance = null;
        public static UserSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new("InfiniteVariantTool");
                }
                return instance;
            }
        }

        [Setting("Directory of your Halo Infinite installation")]
        public string GameDirectory { get; set; } = @"C:\Program Files (x86)\Steam\steamapps\common\Halo Infinite";

        [Setting("Directory where your installed variants are stored")]
        public string VariantDirectory { get; set; } = Path.Combine(SettingsDirectoryVar, "variants");

        [Setting("Language that Halo Infinite is currently set to display")]
        public string Language { get; set; } = "auto";

        [Setting("Check for updates on program start and prompt to download if available")]
        public bool CheckForUpdates { get; set; } = true;
    }
}
