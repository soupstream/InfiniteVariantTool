using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteVariantTool.GUI
{
    public class NotifyPropertyChanged : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChange([CallerMemberName] string propertyName = "ayy lmao")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? BeforePropertyChanged;
        protected void OnBeforePropertyChange([CallerMemberName] string propertyName = "ayy lmao")
        {
            BeforePropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
