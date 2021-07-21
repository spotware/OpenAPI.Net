using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.IO;

namespace Trading.UI.Sample.Services
{
    public interface IChartingService : IDisposable
    {
        public IChart GetChart();
    }

    public sealed class ChartingService : IChartingService
    {
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }

            _disposables.Clear();
        }

        public IChart GetChart()
        {
            var browser = new Chart();

            browser.Loaded += Browser_Loaded;

            return browser;
        }

        private void Browser_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            (sender as ChromiumWebBrowser).Address = Path.Combine(Environment.CurrentDirectory, "Chart.js", "chart.html");
        }
    }
}