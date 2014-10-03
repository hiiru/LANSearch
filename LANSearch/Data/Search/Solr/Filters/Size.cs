using Mizore.CommunicationHandler.Data.Params;
using Mizore.ContentSerializer.Data;
using System;

namespace LANSearch.Data.Search.Solr.Filters
{
    public class Size : IFilter
    {
        public string QSKey { get { return "fsiz"; } }

        public bool HandlesSolrKey(string solrKey)
        {
            if (string.IsNullOrWhiteSpace(solrKey)) return false;
            return solrKey.StartsWith("{!ex=size}size:");
        }

        public string GetQSValue(string solrValue)
        {
            if (string.IsNullOrWhiteSpace(solrValue) || solrValue.Length < 16) return null;
            var range = solrValue.Substring(16, solrValue.Length - 17).Replace(" TO ", "-");

            return range;
        }

        public string GetFilterText(string value)
        {
            switch (value)
            {
                case "{!ex=size}size:[0 TO 1048576]":
                    return "Less than 1 MB";

                case "{!ex=size}size:[1048576  TO 104857600]":
                    return "1 MB - 100 MB";

                case "{!ex=size}size:[104857600 TO 1048576000]":
                    return "100MB - 1 GB";

                case "{!ex=size}size:[1048576000 TO *]":
                    return "More than 1 GB";

                default:
                    return null;
            }
        }

        public bool IsSelected(string value)
        {
            if (value == null || ActiveValue == null) return false;
            return ActiveValue == GetQSValue(value);
        }

        public void UpdateFacetQuery(INamedList<string> qp)
        {
            qp.Add(FacetParams.FACET_QUERY, "{!ex=size}size:[0+TO+1048576]");
            qp.Add(FacetParams.FACET_QUERY, "{!ex=size}size:[1048576++TO+104857600]");
            qp.Add(FacetParams.FACET_QUERY, "{!ex=size}size:[104857600+TO+1048576000]");
            qp.Add(FacetParams.FACET_QUERY, "{!ex=size}size:[1048576000+TO+*]");
        }

        protected string ActiveValue;

        public void UpdateFilterQuery(INamedList<string> qp, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            var splited = value.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (splited.Length != 2) return;
            int start, end;
            if (!int.TryParse(splited[0], out start)) return;
            if (!int.TryParse(splited[1], out end) && splited[1] != "*") return;
            ActiveValue = value;
            qp.Add(CommonParams.FQ, string.Format("{0}:[{1} TO {2}]", "{!tag=size}size", start, end == 0 ? "*" : end.ToString()));
        }
    }
}