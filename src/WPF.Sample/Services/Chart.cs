using CefSharp;
using CefSharp.Wpf;
using System.Collections.Generic;
using System.Linq;

namespace Trading.UI.Sample.Services
{
    public interface IChart
    {
        void LoadData(string name, IEnumerable<Ohlc> data);
    }

    public class Chart : ChromiumWebBrowser, IChart
    {
        public void LoadData(string name, IEnumerable<Ohlc> data)
        {
            var dataStr = data.Select(ohlc => $"{{t: {ohlc.Time.ToUnixTimeMilliseconds()},o: {ohlc.Open},h: {ohlc.High},l: {ohlc.Low},c: {ohlc.Close}}}");

            this.GetMainFrame().ExecuteJavaScriptAsync($"createChart('{name}',[{string.Join(',', dataStr)}]);");
        }
    }
}