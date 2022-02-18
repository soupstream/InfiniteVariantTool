using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
using System.Windows.Shapes;

namespace InfiniteVariantTool.GUI
{
    /// <summary>
    /// Interaction logic for VariantList.xaml
    /// </summary>
    public partial class VariantListView : UserControl
    {
        public VariantListView()
        {
            InitializeComponent();
        }


        // workaround to prevent list from scrolling on click when the variant editor appears under the cursor

        private bool mouseDownAndUnmoved;
        private double mouseY;
        private void VariantList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mouseDownAndUnmoved = true;
            mouseY = Mouse.GetPosition(VariantList).Y;
        }

        private void VariantList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mouseDownAndUnmoved = false;
        }

        private void VariantList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (mouseDownAndUnmoved)
            {
                if (Mouse.GetPosition(VariantList).Y != mouseY)
                {
                    mouseDownAndUnmoved = false;
                }

                if (Mouse.GetPosition(VariantEditor).Y > 0)
                {
                    VariantList.ReleaseMouseCapture();
                }
            }
        }
    }

    public sealed class BooleanToLoadingVariantsConverter : BooleanConverter<string>
    {
        public BooleanToLoadingVariantsConverter() : base("Loading variants...", "Load variants")
        {

        }
    }
}
