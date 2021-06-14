using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Trading.UI.Demo.Behaviors
{
    public static class DataGridColumnSettingsBehavior
    {
        #region Dependency Properties

        public static readonly DependencyProperty SaveColumnsIndexProperty =
                DependencyProperty.RegisterAttached("SaveColumnsIndex", typeof(bool), typeof(DataGridColumnSettingsBehavior),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSaveColumnsIndexChanged));

        public static readonly DependencyProperty SaveColumnsWidthProperty =
            DependencyProperty.RegisterAttached("SaveColumnsWidth", typeof(bool), typeof(DataGridColumnSettingsBehavior),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSaveColumnsWidthChanged));

        public static readonly DependencyProperty SaveColumnsSortingProperty =
            DependencyProperty.RegisterAttached("SaveColumnsSorting", typeof(bool), typeof(DataGridColumnSettingsBehavior),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSaveColumnsSortingChanged));

        public static readonly DependencyProperty EnableColumnsVisibilityMenuProperty =
            DependencyProperty.RegisterAttached("EnableColumnsVisibilityMenu", typeof(bool), typeof(DataGridColumnSettingsBehavior),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnEnableColumnsVisibilityMenuChanged));

        #endregion Dependency Properties

        #region Dependency Properties Get/Set methods

        public static object GetSaveColumnsIndex(DependencyObject source)
        {
            return source.GetValue(SaveColumnsIndexProperty);
        }

        public static void SetSaveColumnsIndex(DependencyObject source, object value)
        {
            source.SetValue(SaveColumnsIndexProperty, value);
        }

        public static object GetSaveColumnsWidth(DependencyObject source)
        {
            return source.GetValue(SaveColumnsWidthProperty);
        }

        public static void SetSaveColumnsWidth(DependencyObject source, object value)
        {
            source.SetValue(SaveColumnsWidthProperty, value);
        }

        public static object GetSaveColumnsSorting(DependencyObject source)
        {
            return source.GetValue(SaveColumnsSortingProperty);
        }

        public static void SetSaveColumnsSorting(DependencyObject source, object value)
        {
            source.SetValue(SaveColumnsSortingProperty, value);
        }

        public static object GetEnableColumnsVisibilityMenu(DependencyObject source)
        {
            return source.GetValue(EnableColumnsVisibilityMenuProperty);
        }

        public static void SetEnableColumnsVisibilityMenu(DependencyObject source, object value)
        {
            source.SetValue(EnableColumnsVisibilityMenuProperty, value);
        }

        #endregion Dependency Properties Get/Set methods

        #region Dependency Properties on changed methods

        private static void OnSaveColumnsIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                return;
            }

            var dataGrid = d as DataGrid;

            ThrowExceptionIfNameNotSet(dataGrid);

            dataGrid.Loaded += (sender, args) => SaveColumnsIndexOnLoaded(sender as DataGrid);
        }

        private static void OnSaveColumnsWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                return;
            }

            var dataGrid = d as DataGrid;

            ThrowExceptionIfNameNotSet(dataGrid);

            dataGrid.Loaded += (sender, args) => SaveColumnsWidthOnLoaded(sender as DataGrid);
        }

        private static void OnSaveColumnsSortingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                return;
            }

            var dataGrid = d as DataGrid;

            ThrowExceptionIfNameNotSet(dataGrid);

            dataGrid.Loaded += (sender, args) => SaveColumnsSortingOnLoaded(sender as DataGrid);
        }

        private static void OnEnableColumnsVisibilityMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                return;
            }

            var dataGrid = d as DataGrid;

            ThrowExceptionIfNameNotSet(dataGrid);

            dataGrid.Loaded += (sender, args) => EnableColumnsVisibilityMenuOnLoaded(sender as DataGrid);
        }

        #endregion Dependency Properties on changed methods

        #region Other Methods

        private static void OverrideColumnsPropertyOnChanged(DataGrid dataGrid, DependencyProperty property, EventHandler handler)
        {
            PropertyDescriptor propertyDescriptor = DependencyPropertyDescriptor.FromProperty(property, typeof(DataGridColumn));

            foreach (var column in dataGrid.Columns)
            {
                propertyDescriptor.AddValueChanged(column, handler);
            }
        }

        private static void ApplySettingsToColumns(DataGrid dataGrid, Action<DataGrid, DataGridColumn, DataGridColumnSettings> function)
        {
            var settings = GetDataGridSettings(dataGrid.Name);

            if (settings.ColumnsSetting == null || !settings.ColumnsSetting.Any())
            {
                return;
            }

            var columns = dataGrid.Columns.Where(column => column.Header != null);

            foreach (var column in columns)
            {
                var columnSettings = settings.ColumnsSetting.FirstOrDefault(iColumnSettings =>
                iColumnSettings.Header.Equals(column.Header.ToString(), StringComparison.InvariantCultureIgnoreCase));

                if (columnSettings == null)
                {
                    continue;
                }

                function(dataGrid, column, columnSettings);
            }
        }

        private static void SetColumnWidth(DataGrid dataGrid, DataGridColumn column, DataGridColumnSettings settings)
        {
            column.Width = new DataGridLength(settings.Width.Value, settings.Width.UnitType, settings.Width.DesiredValue, settings.Width.DisplayValue);
        }

        private static void SetColumnDisplayIndex(DataGrid dataGrid, DataGridColumn column, DataGridColumnSettings settings)
        {
            if (settings.DisplayIndex >= 0 && settings.DisplayIndex < dataGrid.Columns.Count)
            {
                column.DisplayIndex = settings.DisplayIndex;
            }
        }

        private static void SetColumnSorting(DataGrid dataGrid, DataGridColumn column, DataGridColumnSettings settings)
        {
            column.SortDirection = settings.SortDirection;

            if (!column.SortDirection.HasValue)
            {
                return;
            }

            dataGrid.Items.SortDescriptions.Clear();

            dataGrid.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, column.SortDirection.Value));

            dataGrid.Items.Refresh();
        }

        private static void SetColumnVisibility(DataGrid dataGrid, DataGridColumn column, DataGridColumnSettings settings)
        {
            column.Visibility = settings.Visibility;
        }

        private static void SaveSettings(DataGrid dataGrid, DataGridColumnSettingsType settingsType)
        {
            var settings = GetDataGridSettings(dataGrid.Name);

            var columns = dataGrid.Columns.Where(column => column.Header != null);

            settings.ColumnsSetting ??= new List<DataGridColumnSettings>();

            foreach (var column in columns)
            {
                var columnSettings = settings.ColumnsSetting.FirstOrDefault(iColumnSettings => iColumnSettings.Header.Equals(
                    column.Header.ToString(), StringComparison.InvariantCultureIgnoreCase));

                if (columnSettings != null)
                {
                    switch (settingsType)
                    {
                        case DataGridColumnSettingsType.DisplayIndex:
                            columnSettings.DisplayIndex = column.DisplayIndex;
                            break;

                        case DataGridColumnSettingsType.Sorting:
                            columnSettings.SortDirection = column.SortDirection;
                            break;

                        case DataGridColumnSettingsType.Width:
                            columnSettings.Width = new DataGridLengthSettings
                            {
                                Value = column.Width.Value,
                                DesiredValue = column.Width.DesiredValue,
                                DisplayValue = column.Width.DisplayValue,
                                UnitType = column.Width.UnitType
                            };
                            break;

                        case DataGridColumnSettingsType.Visibility:
                            columnSettings.Visibility = column.Visibility;
                            break;
                    }
                }
                else
                {
                    columnSettings = new DataGridColumnSettings
                    {
                        Header = column.Header.ToString(),
                        DisplayIndex = column.DisplayIndex,
                        Width = new DataGridLengthSettings
                        {
                            Value = column.Width.Value,
                            DesiredValue = column.Width.DesiredValue,
                            DisplayValue = column.Width.DisplayValue,
                            UnitType = column.Width.UnitType
                        },
                        SortDirection = column.SortDirection,
                        Visibility = column.Visibility
                    };

                    settings.ColumnsSetting.Add(columnSettings);
                }
            }

            settings.Save();
        }

        private static void ThrowExceptionIfNameNotSet(DataGrid dataGrid)
        {
            if (string.IsNullOrEmpty(dataGrid.Name))
            {
                throw new NullReferenceException("Please set a unique name for your data grid if you want to use the 'DataGridColumnSettingsBehavior'");
            }
        }

        private static void SaveColumnsIndexOnLoaded(DataGrid dataGrid)
        {
            ApplySettingsToColumns(dataGrid, SetColumnDisplayIndex);

            dataGrid.ColumnReordered += (sender, args) => SaveSettings(dataGrid, DataGridColumnSettingsType.DisplayIndex);
        }

        private static void SaveColumnsWidthOnLoaded(DataGrid dataGrid)
        {
            ApplySettingsToColumns(dataGrid, SetColumnWidth);

            OverrideColumnsPropertyOnChanged(dataGrid, DataGridColumn.ActualWidthProperty, (obj, args) => SaveSettings(dataGrid, DataGridColumnSettingsType.Width));
        }

        private static void SaveColumnsSortingOnLoaded(DataGrid dataGrid)
        {
            ApplySettingsToColumns(dataGrid, SetColumnSorting);

            OverrideColumnsPropertyOnChanged(dataGrid, DataGridColumn.SortDirectionProperty, (obj, args) => SaveSettings(dataGrid, DataGridColumnSettingsType.Sorting));
        }

        private static void EnableColumnsVisibilityMenuOnLoaded(DataGrid dataGrid)
        {
            ApplySettingsToColumns(dataGrid, SetColumnVisibility);

            var contextMenu = new ContextMenu();

            var columnHeaders = GetChildren(dataGrid).Where(child => child is DataGridColumnHeader)
                .Select(child => child as DataGridColumnHeader);

            foreach (var columnHeader in columnHeaders)
            {
                columnHeader.ContextMenu = contextMenu;
            }

            foreach (var column in dataGrid.Columns)
            {
                if (!(column.Header is string) || string.IsNullOrEmpty(column.ToString()))
                {
                    continue;
                }

                var menuItem = new MenuItem
                {
                    Header = column.Header,
                    IsChecked = column.Visibility == Visibility.Visible,
                    StaysOpenOnClick = true
                };

                menuItem.Click += (sender, args) =>
                {
                    menuItem.IsChecked = !menuItem.IsChecked;

                    column.Visibility = menuItem.IsChecked ? Visibility.Visible : Visibility.Collapsed;

                    SaveSettings(dataGrid, DataGridColumnSettingsType.Visibility);
                };

                contextMenu.Items.Add(menuItem);
            }
        }

        private static List<DependencyObject> GetChildren(DependencyObject dp)
        {
            var result = new List<DependencyObject>();

            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(dp); i++)
            {
                var child = VisualTreeHelper.GetChild(dp, i);

                result.Add(child);

                var childChildren = GetChildren(child);

                result.AddRange(childChildren);
            }

            return result;
        }

        private static DataGridSettings GetDataGridSettings(string dataGridName)
        {
            var assemblyName = Assembly.GetEntryAssembly().GetName().Name;

            var settingsKey = $"{assemblyName}.{dataGridName}";

            return new DataGridSettings(settingsKey);
        }

        #endregion Other Methods
    }
}