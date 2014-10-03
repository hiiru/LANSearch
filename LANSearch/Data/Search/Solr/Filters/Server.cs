using Mizore.CommunicationHandler.Data.Params;
using Mizore.ContentSerializer.Data;
using System;

namespace LANSearch.Data.Search.Solr.Filters
{
    public class Server : IFilter
    {
        public string QSKey { get { return "fsrv"; } }

        public bool HandlesSolrKey(string solrKey)
        {
            return solrKey == "server";
        }

        public string GetQSValue(string solrValue)
        {
            return solrValue;
        }

        public bool IsSelected(string value)
        {
            if (ActiveValue == null) return false;
            return value == ActiveValue;
        }

        public string GetFilterText(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            int serverId;
            if (!int.TryParse(value, out serverId) || serverId < 1) return null;
            var server = AppContext.GetContext().ServerManager.Get(serverId);
            if (server == null) return null;

            if (string.IsNullOrWhiteSpace(server.Name))
                return string.Format("{0}:{1}", server.Address, server.Port);
            return server.Name;
        }

        public void UpdateFacetQuery(INamedList<string> qp)
        {
            if (qp == null) throw new ArgumentException("qp");

            //server facets
            qp.Add(FacetParams.FACET_FIELD, "{!ex=server}server");
            qp.Add("f.server.facet.limit", "20");
            qp.Add("f.server.facet.mincount", "1");
        }

        protected string ActiveValue;

        public void UpdateFilterQuery(INamedList<string> qp, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            int serverId;
            if (!int.TryParse(value, out serverId) || serverId < 1) return;
            ActiveValue = value;
            qp.Add(CommonParams.FQ, string.Format("{0}:{1}", "{!tag=server}server", serverId));
        }
    }
}