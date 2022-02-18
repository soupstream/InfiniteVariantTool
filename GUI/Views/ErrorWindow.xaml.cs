using AdonisUI.Controls;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace InfiniteVariantTool.GUI
{
    /// <summary>
    /// Interaction logic for ErrorWindow.xaml
    /// </summary>
    public partial class ErrorWindow : AdonisWindow
    {
        private string message;
        private bool forceClose;
        public ErrorWindow(string message, bool forceClose)
        {
            this.message = message;
            this.forceClose = forceClose;
            InitializeComponent();
            text_box.Text = message;
            if (forceClose)
            {
                dismiss_button.Visibility = Visibility.Collapsed;
                info_text.Text = "This error is unrecoverable and the application will exit.";
            }
        }

        private void DismissWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CopyMessage(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(message);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (forceClose)
            {
                Application.Current.Shutdown();
            }
            else
            {
                Application.Current.MainWindow.Focus();
            }
        }

        private void ExitApplication(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
