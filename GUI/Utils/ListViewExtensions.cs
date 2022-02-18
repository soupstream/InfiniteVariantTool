using Microsoft.Xaml.Behaviors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace InfiniteVariantTool.GUI
{
    public class MultiSelectionBehavior : Behavior<ListView>
    {
        private bool _isUpdatingTarget;
        private bool _isUpdatingSource;

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register(nameof(SelectedItems), typeof(IList), typeof(MultiSelectionBehavior), new UIPropertyMetadata(null, SelectedItemsChanged));

        public IList SelectedItems
        {
            get { return (IList)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            if (SelectedItems != null)
            {
                AssociatedObject.SelectedItems.Clear();
                foreach (var item in SelectedItems)
                {
                    AssociatedObject.SelectedItems.Add(item);
                }
            }
        }

        private static void SelectedItemsChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var behavior = o as MultiSelectionBehavior;
            if (behavior?.AssociatedObject == null)
            {
                return;
            }

            if (e.OldValue is INotifyCollectionChanged oldValue)
            {
                oldValue.CollectionChanged -= behavior.SourceCollectionChanged;
                behavior.AssociatedObject.SelectionChanged -= behavior.ListBoxSelectionChanged;
            }
            if (e.NewValue is INotifyCollectionChanged newValue)
            {
                behavior.AssociatedObject.SelectedItems.Clear();
                foreach (var item in (IEnumerable)newValue)
                {
                    behavior.AssociatedObject.SelectedItems.Add(item);
                }

                behavior.AssociatedObject.SelectionChanged += behavior.ListBoxSelectionChanged;
                newValue.CollectionChanged += behavior.SourceCollectionChanged;
            }
        }

        void SourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_isUpdatingSource)
            {
                return;
            }

            try
            {
                _isUpdatingTarget = true;

                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        AssociatedObject.SelectedItems.Remove(item);
                    }
                }

                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        AssociatedObject.SelectedItems.Add(item);
                    }
                }

                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    AssociatedObject.SelectedItems.Clear();
                }
            }
            finally
            {
                _isUpdatingTarget = false;
            }
        }

        private void ListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingTarget)
            {
                return;
            }

            var selectedItems = SelectedItems;
            if (selectedItems == null)
            {
                return;
            }

            try
            {
                _isUpdatingSource = true;

                foreach (var item in e.RemovedItems)
                {
                    selectedItems.Remove(item);
                }

                foreach (var item in e.AddedItems)
                {
                    selectedItems.Add(item);
                }
            }
            finally
            {
                _isUpdatingSource = false;
            }
        }
    }


    public class ScrollToSelectedItemBehaviour : Behavior<ListView>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.SelectionChanged += AssociatedObjectOnSelectionChanged;
            AssociatedObject.IsVisibleChanged += AssociatedObjectOnIsVisibleChanged;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.SelectionChanged -= AssociatedObjectOnSelectionChanged;
            AssociatedObject.IsVisibleChanged -= AssociatedObjectOnIsVisibleChanged;
            base.OnDetaching();
        }

        private static void AssociatedObjectOnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ScrollIntoFirstSelectedItem(sender);
        }

        private static void AssociatedObjectOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ScrollIntoFirstSelectedItem(sender);
        }

        private static void ScrollIntoFirstSelectedItem(object sender)
        {
            if (sender is not ListView listBox)
            {
                return;
            }
            var selectedItems = listBox.SelectedItems;
            if (selectedItems.Count > 0)
            {
                listBox.ScrollIntoView(selectedItems[0]);
            }
        }
    }
}
