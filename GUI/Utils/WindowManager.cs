using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InfiniteVariantTool.GUI
{
    public class WindowManager
    {
        private Window parent;
        private List<Window> windows = new();
        private Dictionary<Type, Window> singleWindows = new();
        private Dictionary<(Type, Type), Window> singleWindowsByViewModel = new();

        public WindowManager(Window parent)
        {
            this.parent = parent;
            parent.Closed += (_, _) => CloseAll();
        }

        private void CloseAll()
        {
            foreach (var window in singleWindows.Values
                .Concat(singleWindowsByViewModel.Values)
                .Concat(windows))
            {
                // not needed if setting owner
                // window.Close();
            }
            singleWindows.Clear();
            singleWindowsByViewModel.Clear();
            windows.Clear();
        }

        private void OnChildWindowClose(object? sender, EventArgs e)
        {
            var window = (Window)sender!;
            window.Owner.Focus();
        }

        public void Show<T>() where T : Window
        {
            T window = (T)(Activator.CreateInstance(typeof(T))!);
            window.Closed += (_, _) => windows.Remove(window);
            windows.Add(window);
            if (window.DataContext is ViewModel viewModel)
            {
                viewModel.IOService = new IOServiceImpl(window);
                viewModel.NavigationService = new NavigationServiceImpl(window);
            }
            window.Owner = parent;
            window.Closed += OnChildWindowClose;
            window.Show();
        }

        public void Show<TWindow, TViewModel>(params object?[]? args)
            where TWindow : Window
            where TViewModel : ViewModel
        {
            TWindow window = (TWindow)(Activator.CreateInstance(typeof(TWindow))!);
            TViewModel viewModel = (TViewModel)(Activator.CreateInstance(typeof(TViewModel), args)!);
            viewModel.IOService = new IOServiceImpl(window);
            viewModel.NavigationService = new NavigationServiceImpl(window);
            window.DataContext = viewModel;
            window.Closed += (_, _) => windows.Remove(window);
            windows.Add(window);
            window.Owner = parent;
            window.Closed += OnChildWindowClose;
            window.Show();
        }

        public void ShowOrActivate<T>() where T : Window
        {
            if (singleWindows.ContainsKey(typeof(T)))
            {
                singleWindows[typeof(T)].Activate();
            }
            else
            {
                T window = (T)(Activator.CreateInstance(typeof(T))!);
                window.Closed += (_, _) => singleWindows.Remove(typeof(T));
                singleWindows[typeof(T)] = window;
                if (window.DataContext is ViewModel viewModel)
                {
                    viewModel.IOService = new IOServiceImpl(window);
                    viewModel.NavigationService = new NavigationServiceImpl(window);
                }
                window.Owner = parent;
                window.Closed += OnChildWindowClose;
                window.Show();
            }
        }

        public void ShowOrActivate<TWindow, TViewModel>(params object?[]? args)
            where TWindow : Window
            where TViewModel : ViewModel
        {
            var key = (typeof(TWindow), typeof(TViewModel));
            if (singleWindowsByViewModel.ContainsKey(key))
            {
                singleWindowsByViewModel[key].Activate();
            }
            else
            {
                TWindow window = (TWindow)(Activator.CreateInstance(typeof(TWindow))!);
                TViewModel viewModel = (TViewModel)(Activator.CreateInstance(typeof(TViewModel), args)!);
                viewModel.IOService = new IOServiceImpl(window);
                viewModel.NavigationService = new NavigationServiceImpl(window);
                window.DataContext = viewModel;
                window.Closed += (_, _) => singleWindowsByViewModel.Remove(key);
                singleWindowsByViewModel[key] = window;
                window.Owner = parent;
                window.Closed += OnChildWindowClose;
                window.Show();
            }
        }

        private void CenterWindowOnParent(Window window)
        {
            window.Left = parent.Left + (parent.Width - window.ActualWidth) / 2;
            window.Top = parent.Top + (parent.Height - window.ActualHeight) / 2;
        }
    }
}
