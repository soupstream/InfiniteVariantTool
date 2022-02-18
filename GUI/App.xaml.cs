using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Threading;

namespace InfiniteVariantTool.GUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ErrorWindow errorWindow = new(e.Exception.ToString(), !IsExceptionRecoverable(e.Exception));
            errorWindow.Owner = Current.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive);
            errorWindow.Show();
            e.Handled = true;
        }

        private bool IsExceptionRecoverable(Exception e)
        {
            Exception? currentException = e;
            while (currentException != null)
            {
                if (currentException is XamlParseException)
                {
                    return false;
                }
                currentException = currentException.InnerException;
            }
            return true;
        }


        private const string UniqueEventName = "7d939ec9-437e-4438-8b96-2e1fb897e52a";
        private const string UniqueMutexName = "a56f78b5-6734-430d-b4c2-227e8e2d38b0";
        private EventWaitHandle? eventWaitHandle;
        private Mutex? mutex;
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            mutex = new Mutex(true, UniqueMutexName, out bool isOwned);
            eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, UniqueEventName);

            // So, R# would not give a warning that this variable is not used.
            GC.KeepAlive(mutex);

            if (isOwned)
            {
                // Spawn a thread which will be waiting for our event
                Thread thread = new(() =>
                {
                    while (eventWaitHandle.WaitOne())
                    {
                        Current.Dispatcher.BeginInvoke(() => ((MainWindow)Current.MainWindow).BringToForeground());
                    }
                });

                // It is important mark it as background otherwise it will prevent app from exiting.
                thread.IsBackground = true;

                thread.Start();
                return;
            }

            // Notify other instance so it could bring itself to foreground.
            eventWaitHandle.Set();

            // Terminate this instance.
            Shutdown();
        }
    }
}
