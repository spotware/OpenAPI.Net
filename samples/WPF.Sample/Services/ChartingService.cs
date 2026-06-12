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
        private readonly List<IDisposable> _disposables = new();

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
            // Resolve relative to the app directory (not the current working directory, which can
            // differ depending on how the app was launched) and hand CefSharp a proper file:// URI.
            var chartHtmlPath = Path.Combine(AppContext.BaseDirectory, "Chart.js", "chart.html");

            (sender as ChromiumWebBrowser).Address = new Uri(chartHtmlPath).AbsoluteUri;
        }
    }
}