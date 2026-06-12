using CefSharp;
using CefSharp.Wpf;
using System.Collections.Generic;
using System.Globalization;
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
            // Format the numbers with InvariantCulture so they always use '.' as the decimal
            // separator. With the current culture, cultures that use ',' (e.g. German) produced
            // invalid JavaScript like "o: 1,2345", which threw a SyntaxError in createChart and
            // left the chart blank.
            var dataStr = data.Select(ohlc => string.Format(CultureInfo.InvariantCulture,
                "{{t: {0},o: {1},h: {2},l: {3},c: {4}}}",
                ohlc.Time.ToUnixTimeMilliseconds(), ohlc.Open, ohlc.High, ohlc.Low, ohlc.Close));

            this.GetMainFrame().ExecuteJavaScriptAsync($"createChart('{name}',[{string.Join(',', dataStr)}]);");
        }
    }
}
