using Mizore.ContentSerializer.Data;

namespace LANSearch.Data.Search.Solr.Filters
{
    public interface IFilter
    {
        string QSKey { get; }

        bool HandlesSolrKey(string solrKey);

        string GetQSValue(string solrValue);

        string GetFilterText(string value);

        void UpdateFacetQuery(INamedList<string> qp);

        void UpdateFilterQuery(INamedList<string> qp, string qsValue);

        string Value { get; }
    }
}