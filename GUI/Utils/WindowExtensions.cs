using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace InfiniteVariantTool.GUI
{
    // WindowStartupLocation="CenterOwner" doesn't work when using SizeToContent="WidthAndHeight"
    // Adding this behavior fixes the issue
    public class FixCenterWindowBehavior : Behavior<Window>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SizeChanged += AssociatedObject_SizeChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.SizeChanged -= AssociatedObject_SizeChanged;
            base.OnDetaching();
        }

        private void AssociatedObject_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AssociatedObject.Top = AssociatedObject.Owner.Top + (AssociatedObject.Owner.Height - e.NewSize.Height) / 2;
            AssociatedObject.Left = AssociatedObject.Owner.Left + (AssociatedObject.Owner.Width - e.NewSize.Width) / 2;
            AssociatedObject.SizeChanged -= AssociatedObject_SizeChanged;
        }
    }
}
