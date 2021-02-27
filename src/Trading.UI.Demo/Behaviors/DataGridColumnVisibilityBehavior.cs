using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Trading.UI.Demo.Behaviors
{
    public static class DataGridColumnVisibilityBehavior
    {
        public static readonly DependencyProperty ColumnIndexProperty =
                DependencyProperty.RegisterAttached("ColumnIndex", typeof(int), typeof(DataGridColumnVisibilityBehavior),
                new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnColumnIndexChanged));

        public static readonly DependencyProperty ColumnNameProperty =
            DependencyProperty.RegisterAttached("ColumnName", typeof(string), typeof(DataGridColumnVisibilityBehavior),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnColumnNameChanged));

        public static readonly DependencyProperty ColumnVisibilityProperty =
                DependencyProperty.RegisterAttached("ColumnVisibility", typeof(Visibility), typeof(DataGridColumnVisibilityBehavior),
                new FrameworkPropertyMetadata(Visibility.Visible, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnColumnVisibilityChanged));

        public static object GetColumnIndex(DependencyObject source)
        {
            return source.GetValue(ColumnIndexProperty);
        }

        public static object GetColumnName(DependencyObject source)
        {
            return source.GetValue(ColumnNameProperty);
        }

        public static object GetColumnVisibility(DependencyObject source)
        {
            return source.GetValue(ColumnVisibilityProperty);
        }

        public static void SetColumnIndex(DependencyObject source, object value)
        {
            source.SetValue(ColumnIndexProperty, value);
        }

        public static void SetColumnName(DependencyObject source, object value)
        {
            source.SetValue(ColumnNameProperty, value);
        }

        public static void SetColumnVisibility(DependencyObject source, object value)
        {
            source.SetValue(ColumnVisibilityProperty, value);
        }

        private static void ChangeVisibility(DependencyObject d, Visibility visibility, string columnName = null, int columnIndex = -1)
        {
            var dataGrid = d as DataGrid;

            DataGridColumn column;

            if (columnIndex >= 0 && dataGrid.Columns.Count > columnIndex)
            {
                column = dataGrid.Columns[columnIndex];
            }
            else
            {
                column = dataGrid.Columns.FirstOrDefault(col => col.Header is string && col.Header.ToString().Equals(columnName,
                    System.StringComparison.InvariantCultureIgnoreCase));
            }

            if (column != null)
            {
                column.Visibility = visibility;
            }
        }

        private static void OnColumnIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var visibility = (Visibility)GetColumnVisibility(d);

            ChangeVisibility(d, visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible, columnIndex: (int)e.OldValue);
            ChangeVisibility(d, visibility, columnIndex: (int)e.NewValue);
        }

        private static void OnColumnNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var visibility = (Visibility)GetColumnVisibility(d);

            ChangeVisibility(d, visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible, columnName: (string)e.OldValue);

            ChangeVisibility(d, visibility, columnName: (string)e.NewValue);
        }

        private static void OnColumnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var visibility = (Visibility)GetColumnVisibility(d);

            var columnName = (string)GetColumnName(d);
            var columnIndex = (int)GetColumnIndex(d);

            ChangeVisibility(d, visibility, columnName, columnIndex);
        }
    }
}