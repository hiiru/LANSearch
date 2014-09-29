using System;
using System.Collections.Generic;
using System.Text;

namespace LANSearch.Data
{
    public class UrlBuilder
    {
        public UrlBuilder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("path");
            var splited = path.Split(new[] { '?', '#' }, 3, StringSplitOptions.RemoveEmptyEntries);
            BasePath = splited[0];
            QueryParameters = new SortedDictionary<string, string>();
            if (splited.Length > 1)
            {
                var splitedParams = splited[1].Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var qsKV in splitedParams)
                {
                    var kv = qsKV.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                    if (kv.Length != 2 || QueryParameters.ContainsKey(kv[0])) continue;
                    QueryParameters.Add(kv[0], kv[1]);
                }
            }
            //fragment (splitted[2]) will be ignored/removed
        }

        protected UrlBuilder()
        {
        }

        public SortedDictionary<string, string> QueryParameters { get; protected set; }

        public string BasePath { get; set; }

        public UrlBuilder Set(string key, string value)
        {
            QueryParameters[key] = value;
            return this;
        }

        public UrlBuilder Remove(string key)
        {
            if (QueryParameters.ContainsKey(key))
                QueryParameters.Remove(key);
            return this;
        }

        public override string ToString()
        {
            var sb = new StringBuilder(BasePath);
            if (QueryParameters.Count > 0)
            {
                sb.Append('?');
                bool first = true;
                foreach (var kvp in QueryParameters)
                {
                    if (!first)
                        sb.Append('&');
                    else
                        first = false;
                    sb.AppendFormat("{0}={1}", kvp.Key, kvp.Value);
                }
            }
            return sb.ToString();
        }

        public UrlBuilder Clone()
        {
            return new UrlBuilder
            {
                BasePath = BasePath,
                QueryParameters = new SortedDictionary<string, string>(QueryParameters)
            };
        }
    }
}