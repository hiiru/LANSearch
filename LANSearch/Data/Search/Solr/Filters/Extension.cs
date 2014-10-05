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
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value;
        }

        public string GetSelectedText()
        {
            if (!HasSelected) return null;
            return GetFilterText(ActiveValue);
        }

        public bool HasSelected { get { return ActiveValue != null; } }

        public bool IsSelected(string value)
        {
            if (ActiveValue == null) return false;
            return value == ActiveValue;
        }

        public void UpdateFacetQuery(INamedList<string> qp)
        {
            if (qp == null) throw new ArgumentException("qp");

            //server facets
            qp.Add(FacetParams.FACET_FIELD, "{!ex=fileExt}fileExt");
            qp.Add("f.fileExt.facet.limit", "20");
            qp.Add("f.fileExt.facet.mincount", "2");
        }

        protected string ActiveValue;

        public void UpdateFilterQuery(INamedList<string> qp, string value)
        {
            if (value.Contains(" ")) return;
            ActiveValue = value;
            qp.Add(CommonParams.FQ, string.Format("{0}:{1}", "{!tag=fileExt}fileExt", value));
        }
    }
}