using CustomControls.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Trading.UI.Demo.Controls
{
    /// <summary>
    /// Interaction logic for DropDownMultiSelect.xaml
    /// </summary>
    public partial class DropDownMultiSelect : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty ItemsSourceProperty
            = DependencyProperty.Register(
                "ItemsSource",
                typeof(IEnumerable),
                typeof(DropDownMultiSelect),
                new FrameworkPropertyMetadata(new List<object>(), ItemsSourcePropertyChangedCallback)
            );

        public static readonly DependencyProperty SelectedItemsProperty
            = DependencyProperty.Register(
                "SelectedItems",
                typeof(IList),
                typeof(DropDownMultiSelect),
                new FrameworkPropertyMetadata(new List<object>(), SelectedItemsPropertyChangedCallback)
            );

        public static readonly DependencyProperty DisplayMemberPathProperty
            = DependencyProperty.Register(
                "DisplayMemberPath",
                typeof(string),
                typeof(DropDownMultiSelect),
                new FrameworkPropertyMetadata(string.Empty)
            );

        public static readonly DependencyProperty ItemTemplateProperty
            = DependencyProperty.Register(
                "ItemTemplate",
                typeof(DataTemplate),
                typeof(DropDownMultiSelect),
                new FrameworkPropertyMetadata(null)
            );

        #endregion Dependency Properties

        public DropDownMultiSelect() => InitializeComponent();

        #region Properties

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public IList SelectedItems
        {
            get => (IList)GetValue(SelectedItemsProperty);
            set => SetValue(SelectedItemsProperty, value);
        }

        public string DisplayMemberPath
        {
            get => (string)GetValue(DisplayMemberPathProperty);
            set => SetValue(DisplayMemberPathProperty, value);
        }

        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        public bool IsItemTemplateProvided => ItemTemplate != null;

        #endregion Properties

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox checkBox) || !checkBox.IsChecked.HasValue || !(checkBox.Parent is Grid grid)
                || !(grid.DataContext is SelectableObject selectableObject))
            {
                return;
            }

            if (!checkBox.IsChecked.Value && SelectedItems.Contains(selectableObject.Object))
            {
                SelectedItems.Remove(selectableObject.Object);
            }
            else if (checkBox.IsChecked.Value && !SelectedItems.Contains(selectableObject.Object))
            {
                SelectedItems.Add(selectableObject.Object);
            }

            SetDisplayText();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (SelectableObject selectableObject in ItemsListView.Items)
            {
                selectableObject.IsSelected = false;
            }

            OutputTextBox.Text = string.Empty;
        }

        private void ShowButton_Click(object sender, RoutedEventArgs e)
        {
            ItemsPopup.IsOpen = !ItemsPopup.IsOpen;
        }

        public void SetDisplayText()
        {
            var text = string.Empty;

            foreach (var item in SelectedItems)
            {
                var itemDisplayText = GetItemDisplayText(this, item);

                text = string.IsNullOrEmpty(text) ? itemDisplayText : $"{text}, {itemDisplayText}";
            }

            OutputTextBox.Text = text ?? string.Empty;
        }

        private static void ItemsSourcePropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(e.NewValue is IEnumerable enumerable))
            {
                throw new InvalidOperationException($"The {nameof(DropDownMultiSelect)} items source must be an IEnumerable");
            }

            void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
            {
                FillDropDownItems(d as DropDownMultiSelect, sender as IEnumerable);
            }

            if (e.OldValue is INotifyCollectionChanged oldCollectionChanged)
            {
                oldCollectionChanged.CollectionChanged -= OnCollectionChanged;
            }

            if (e.NewValue is INotifyCollectionChanged collectionChanged)
            {
                collectionChanged.CollectionChanged += OnCollectionChanged;
            }

            FillDropDownItems(d as DropDownMultiSelect, enumerable);
        }

        private static void SelectedItemsPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(e.NewValue is IEnumerable enumerable))
            {
                throw new InvalidOperationException($"The {nameof(DropDownMultiSelect)} selected items must be not null and an" +
                                                    $" IEnumerable");
            }

            void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
            {
                SelectItems(d as DropDownMultiSelect, sender as IEnumerable);
            }

            if (e.OldValue is INotifyCollectionChanged oldCollectionChanged)
            {
                oldCollectionChanged.CollectionChanged -= OnCollectionChanged;
            }

            if (e.NewValue is INotifyCollectionChanged collectionChanged)
            {
                collectionChanged.CollectionChanged += OnCollectionChanged;
            }

            SelectItems(d as DropDownMultiSelect, enumerable);
        }

        private static void FillDropDownItems(DropDownMultiSelect dropDownMultiSelect, IEnumerable items)
        {
            dropDownMultiSelect.ItemsListView.Items.Clear();

            var itemsCopy = items.Cast<object>().ToArray();

            foreach (var item in itemsCopy)
            {
                var selectableObject = new SelectableObject(item, GetItemDisplayText(dropDownMultiSelect, item));

                dropDownMultiSelect.ItemsListView.Items.Add(selectableObject);
            }

            SelectItems(dropDownMultiSelect, dropDownMultiSelect.SelectedItems);
        }

        private static string GetItemDisplayText(DropDownMultiSelect dropDownMultiSelect, object item)
        {
            if (item is null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(dropDownMultiSelect.DisplayMemberPath))
            {
                var propertyInfo = item.GetType().GetProperty(dropDownMultiSelect.DisplayMemberPath);

                if (propertyInfo != null && propertyInfo.CanRead)
                {
                    var propertyValue = propertyInfo.GetValue(item);

                    if (propertyValue != null)
                    {
                        return propertyValue.ToString();
                    }
                }
            }
            else if (item.GetType().IsEnum && item.ToString() is { } fieldName)
            {
                var field = item.GetType().GetField(fieldName);

                if (field != null)
                {
                    var descriptionAttribute = field.GetCustomAttribute<DescriptionAttribute>(false);

                    if (descriptionAttribute != null && !string.IsNullOrEmpty(descriptionAttribute.Description))
                    {
                        return descriptionAttribute.Description;
                    }
                }
            }

            return item.ToString();
        }

        private static void SelectItems(DropDownMultiSelect dropDownMultiSelect, IEnumerable enumerable)
        {
            var enumerableCopy = enumerable.Cast<object>().ToArray();

            foreach (SelectableObject selectableObject in dropDownMultiSelect.ItemsListView.Items)
            {
                selectableObject.IsSelected = enumerableCopy.Contains(selectableObject.Object);
            }

            dropDownMultiSelect.SetDisplayText();
        }
    }
}