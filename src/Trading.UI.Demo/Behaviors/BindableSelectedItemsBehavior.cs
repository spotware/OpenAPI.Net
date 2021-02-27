using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Trading.UI.Demo.Behaviors
{
    public static class BindableSelectedItemsBehavior
    {
        #region Dependency Properties

        public static readonly DependencyProperty SelectedItemsProperty =
                DependencyProperty.RegisterAttached("SelectedItems", typeof(IList),
                    typeof(BindableSelectedItemsBehavior),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.None,
                    OnSelectedItemsChanged));

        #endregion Dependency Properties

        #region Dependency Properties Get/Set methods

        public static object GetSelectedItems(DependencyObject source)
        {
            return source.GetValue(SelectedItemsProperty);
        }

        public static void SetSelectedItems(DependencyObject source, object value)
        {
            source.SetValue(SelectedItemsProperty, value);
        }

        #endregion Dependency Properties Get/Set methods

        #region Dependency Properties on changed methods

        private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is Selector selector))
            {
                throw new InvalidOperationException($"The attached object must be of type Selector");
            }

            if (!(e.NewValue is IList selectedItems))
            {
                throw new InvalidOperationException($"The selected items must be of type IList");
            }

            if (!(d.GetType().GetProperty("SelectedItems")?.GetValue(d) is IList baseSelectedItems))
            {
                throw new InvalidOperationException($"The attached object selected items is null or invalid type");
            }

            void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
            {
                SelectedItemsCollectionChangedHandler(baseSelectedItems, args);
            }

            if (e.OldValue is INotifyCollectionChanged oldCollectionChanged)
            {
                oldCollectionChanged.CollectionChanged -= OnCollectionChanged;
            }

            void OnSelectionChanged(object sender, SelectionChangedEventArgs args)
            {
                SelectionChangedHandler(selector, selectedItems, args);
            }

            selector.SelectionChanged -= OnSelectionChanged;

            baseSelectedItems.Clear();

            var selectedItemsCopy = new object[selectedItems.Count];

            selectedItems.CopyTo(selectedItemsCopy, 0);

            foreach (var item in selectedItemsCopy)
            {
                if (!baseSelectedItems.Contains(item))
                {
                    baseSelectedItems.Add(item);
                }
            }

            if (e.NewValue is INotifyCollectionChanged newCollectionChanged)
            {
                newCollectionChanged.CollectionChanged += OnCollectionChanged;
            }

            selector.SelectionChanged += OnSelectionChanged;
        }

        #endregion Dependency Properties on changed methods

        #region Event handlers

        private static void SelectedItemsCollectionChangedHandler(IList baseSelectedItems, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                baseSelectedItems.Clear();
            }
            else
            {
                if (e.NewItems != null)
                {
                    var newItems = e.NewItems.Cast<object>().Where(item => !baseSelectedItems.Contains(item)).ToArray();

                    foreach (var item in newItems)
                    {
                        baseSelectedItems.Add(item);
                    }
                }

                if (e.OldItems != null)
                {
                    var oldItems = e.OldItems.Cast<object>().Where(baseSelectedItems.Contains).ToArray();

                    foreach (var item in oldItems)
                    {
                        baseSelectedItems.Remove(item);
                    }
                }
            }
        }

        private static void SelectionChangedHandler(Selector selector, IList selectedItems,
            SelectionChangedEventArgs e)
        {
            if (selectedItems == null)
            {
                return;
            }

            foreach (var item in e.AddedItems)
            {
                if (selectedItems.Contains(item))
                {
                    continue;
                }

                if (selector.Items.Contains(item) || selector.ItemsSource != null && selector.ItemsSource.Cast<object>().Contains(item))
                {
                    selectedItems.Add(item);
                }
            }

            foreach (var item in e.RemovedItems)
            {
                if (selectedItems.Contains(item))
                {
                    selectedItems.Remove(item);
                }
            }
        }

        #endregion Event handlers
    }
}