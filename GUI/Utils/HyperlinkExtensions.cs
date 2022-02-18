using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace InfiniteVariantTool.GUI
{
    public class OpenHyperlinkBehavior : Behavior<Hyperlink>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.RequestNavigate += Hyperlink_RequestNavigate;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.RequestNavigate -= Hyperlink_RequestNavigate;
            base.OnDetaching();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            var ps = new ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(ps);
            e.Handled = true;
        }
    }
}
