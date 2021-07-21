using System.Collections;
using System.Windows;
using System.Windows.Data;

namespace Trading.UI.Sample.Behaviors
{
    public static class DataGroupingBehavior
    {
        public static readonly DependencyProperty GroupByProperty =
                DependencyProperty.RegisterAttached("GroupBy", typeof(string), typeof(DataGroupingBehavior),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnGroupByChanged));

        public static object GetGroupBy(DependencyObject source)
        {
            return source.GetValue(GroupByProperty);
        }

        public static void SetGroupBy(DependencyObject source, object value)
        {
            source.SetValue(GroupByProperty, value);
        }

        private static void OnGroupByChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var itemsSource = d.GetType().GetProperty("ItemsSource").GetValue(d) as IEnumerable;

            var groupBy = e.NewValue as string;

            if (itemsSource == null || string.IsNullOrEmpty(groupBy))
            {
                return;
            }

            var sourceView = CollectionViewSource.GetDefaultView(itemsSource);

            if (sourceView == null)
            {
                return;
            }

            if (sourceView.GroupDescriptions.Count > 0)
            {
                sourceView.GroupDescriptions.Clear();
            }

            sourceView.GroupDescriptions.Add(new PropertyGroupDescription(groupBy));
        }
    }
}