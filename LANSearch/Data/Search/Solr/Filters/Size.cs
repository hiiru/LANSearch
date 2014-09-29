using Mizore.CommunicationHandler.Data.Params;
using Mizore.ContentSerializer.Data;

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
            var range = solrValue.Substring(16, solrValue.Length - 17);

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

        public void UpdateFacetQuery(INamedList<string> qp)
        {
            qp.Add(FacetParams.FACET_QUERY, "{!ex=size}size:[0+TO+1048576]");
            qp.Add(FacetParams.FACET_QUERY, "{!ex=size}size:[1048576++TO+104857600]");
            qp.Add(FacetParams.FACET_QUERY, "{!ex=size}size:[104857600+TO+1048576000]");
            qp.Add(FacetParams.FACET_QUERY, "{!ex=size}size:[1048576000+TO+*]");
        }

        public void UpdateFilterQuery(INamedList<string> qp, string value)
        {
            return;
        }

        public string Value { get; private set; }
    }
}