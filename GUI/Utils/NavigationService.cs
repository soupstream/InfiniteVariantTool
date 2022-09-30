using InfiniteVariantTool.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InfiniteVariantTool.GUI
{
    public abstract class NavigationService
    {
        public abstract void Close();
        public abstract void OpenSettingsWindow();
        public abstract void OpenUnpackCacheFileWindow();
        public abstract void OpenPackCacheFileWindow();
        public abstract void OpenUnpackLuaBundleWindow();
        public abstract void OpenPackLuaBundleWindow();
        public abstract void OpenHashUrlWindow(VariantManager variantManager);
        public abstract void OpenResultWindow(string output, string? path);
        public abstract void OpenExtractWindow(VariantManager variantManager, VariantModel variant);
        public abstract void OpenAboutWindow();
        public abstract void OpenUpdateWindow();
        public abstract void OpenUpdateWindow(Version newVersion);
    }

    public class NavigationServiceImpl : NavigationService
    {
        private Window window;
        public WindowManager WindowManager { get; private set; }
        public NavigationServiceImpl(Window window, WindowManager? windowManager = null)
        {
            this.window = window;
            this.WindowManager = windowManager ?? new WindowManager(window);
        }

        public override void Close()
        {
            window.Close();
        }

        public override void OpenExtractWindow(VariantManager variantManager, VariantModel variant)
        {
            WindowManager.Show<ExtractWindow, ExtractViewModel>(variantManager, variant);
        }

        public override void OpenSettingsWindow()
        {
            WindowManager.ShowOrActivate<SettingsWindow>();
        }

        public override void OpenUnpackCacheFileWindow()
        {
            WindowManager.ShowOrActivate<FileActionWindow, UnpackCacheFileViewModel>();
        }

        public override void OpenPackCacheFileWindow()
        {
            WindowManager.ShowOrActivate<FileActionWindow, PackCacheFileViewModel>();
        }

        public override void OpenUnpackLuaBundleWindow()
        {
            WindowManager.ShowOrActivate<FileActionWindow, UnpackLuaBundleViewModel>();
        }

        public override void OpenPackLuaBundleWindow()
        {
            WindowManager.ShowOrActivate<FileActionWindow, PackLuaBundleViewModel>();
        }

        public override void OpenHashUrlWindow(VariantManager variantManager)
        {
            WindowManager.ShowOrActivate<HashUrlWindow>();
        }

        public override void OpenResultWindow(string output, string? path = null)
        {
            WindowManager.Show<ResultWindow, ResultViewModel>(output, path);
        }

        public override void OpenAboutWindow()
        {
            WindowManager.ShowOrActivate<AboutWindow>();
        }

        public override void OpenUpdateWindow()
        {
            WindowManager.ShowOrActivate<UpdateWindow, UpdaterViewModel>();
        }

        public override void OpenUpdateWindow(Version newVersion)
        {
            WindowManager.Show<UpdateWindow, UpdaterViewModel>(newVersion);
        }
    }
}
