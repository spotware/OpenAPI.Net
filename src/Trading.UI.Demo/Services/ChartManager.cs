using CefSharp;
using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Trading.UI.Demo.Services
{
    public interface IChartManager : IDisposable
    {
        object Chart { get; }

        void CreateChart(string name, IEnumerable<Ohlc> data);
    }

    public class ChartManager : IChartManager
    {
        private readonly ChromiumWebBrowser _browser;

        public ChartManager()
        {
            _browser = new ChromiumWebBrowser();

            _browser.Loaded += Browser_Loaded;
        }

        public object Chart => _browser;

        public void Dispose()
        {
            _browser.Dispose();
        }

        public void CreateChart(string name, IEnumerable<Ohlc> data)
        {
            var dataStr = data.Select(ohlc => $"{{t: {ohlc.Time.ToUnixTimeMilliseconds()},o: {ohlc.Open},h: {ohlc.High},l: {ohlc.Low},c: {ohlc.Close}}}");

            _browser.GetMainFrame().ExecuteJavaScriptAsync($"createChart('{name}',[{string.Join(',', dataStr)}]);");
        }

        private void Browser_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _browser.Address = Path.Combine(Environment.CurrentDirectory, "Chart.js", "chart.html");
        }
    }
}