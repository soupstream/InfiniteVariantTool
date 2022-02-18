using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.IO;

using Ookii.Dialogs.Wpf;
using InfiniteVariantTool.Core;
using InfiniteVariantTool.Core.Settings;
using InfiniteVariantTool.Core.Serialization;
using AdonisUI.Controls;

namespace InfiniteVariantTool.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : AdonisWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel(new IOServiceImpl(this), new NavigationServiceImpl(this));
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            // workaround to fix style being incorrect until changing tabs
            TabControl.SelectedIndex = 0;


            // check for updates
            MainViewModel viewModel = (MainViewModel)DataContext;
            if (UserSettings.Instance.CheckForUpdates)
            {
                viewModel.AutoCheckForUpdateCommand.Execute(null);
            }

            // load variants here (rather than in viewmodel initialization) so that ErrorWindow can show exceptions
            if (!viewModel.VariantViewModel.Loaded)
            {
                viewModel.VariantViewModel.LoadVariantsCommand.Execute(null);
            }
        }

        public void BringToForeground()
        {
            if (WindowState == WindowState.Minimized || Visibility == Visibility.Hidden)
            {
                Show();
                WindowState = WindowState.Normal;
            }

            // According to some sources these steps guarantee that an app will be brought to foreground.
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
        }
    }
}
