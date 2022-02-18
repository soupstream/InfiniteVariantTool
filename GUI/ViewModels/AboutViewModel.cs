using InfiniteVariantTool.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteVariantTool.GUI
{
    public class AboutViewModel : ViewModel
    {
        public string Version => Updater.CurrentVersion.ToString(3);
    }
}
