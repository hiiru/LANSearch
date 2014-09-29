using Mizore.CommunicationHandler.Data.Params;
using Mizore.ContentSerializer.Data;
using System;

namespace LANSearch.Data.Search.Solr.Filters
{
    public class Extension : IFilter
    {
        public string QSKey { get { return "fext"; } }

        public bool HandlesSolrKey(string solrKey)
        {
            return solrKey == "fileExt";
        }

        public string GetQSValue(string solrValue)
        {
            return solrValue;
        }

        public string GetFilterText(string value)
        {
            return value;
        }

        public void UpdateFacetQuery(INamedList<string> qp)
        {
            if (qp == null) throw new ArgumentException("qp");

            //server facets
            qp.Add(FacetParams.FACET_FIELD, "{!ex=fileExt}fileExt");
            qp.Add("f.fileExt.facet.limit", "20");
            qp.Add("f.fileExt.facet.mincount", "2");
        }

        public void UpdateFilterQuery(INamedList<string> qp, string value)
        {
            if (value.Contains(" ")) return;
            Value = value;
            qp.Add(CommonParams.FQ, string.Format("{0}:{1}", "{!tag=fileExt}fileExt", value));
        }

        public string Value { get; private set; }
    }
}