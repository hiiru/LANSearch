using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LANSearch.Data.Search.Solr;
using LANSearch.Models.Search;
using Mizore.CommunicationHandler.RequestHandler;
using Mizore.CommunicationHandler.ResponseHandler;
using Mizore.DataMappingHandler.Reflection;
using Mizore.SolrServerHandler;
using Nancy;

namespace LANSearch.Data.Search
{
    public class SearchManager
    {
        public SearchManager(AppConfig config)
        {
            SolrServer = new HttpSolrServer(config.SearchServerUrl);
            SolrMapper = new ReflectionDataMapper<File>();
            
        }
        public ISolrServerHandler SolrServer { get; protected set; }

        public ReflectionDataMapper<File> SolrMapper { get; protected set; }

        public SearchModel Search(Request request)
        {
            var query = new SolrHandler(request);
            return query.SolrSearch();
        }

        public void UnindexServer(int id)
        {
            if (id <= 0) return;
            var solrRequest = new UpdateRequest(SolrServer.GetUriBuilder());
            solrRequest.DeleteByQuery(string.Format("server:{0}", id));
            SolrServer.Request<UpdateResponse>(solrRequest);
        }
    }
}
