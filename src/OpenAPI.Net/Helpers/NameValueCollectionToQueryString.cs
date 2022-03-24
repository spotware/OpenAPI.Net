using System;
using System.Collections.Specialized;
using System.Text;

namespace OpenAPI.Net.Helpers
{
    public static class NameValueCollectionToQueryString
    {
        public static string ToQueryString(this NameValueCollection collection)
        {
            if (collection == null) return string.Empty;

            StringBuilder stringBuilder = new();

            foreach (string key in collection.Keys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;

                string[] values = collection.GetValues(key);

                if (values == null) continue;

                foreach (string value in values)
                {
                    stringBuilder.Append(stringBuilder.Length == 0 ? "?" : "&");
                    stringBuilder.AppendFormat("{0}={1}", Uri.EscapeDataString(key), Uri.EscapeDataString(value));
                }
            }

            return stringBuilder.ToString();
        }
    }
}