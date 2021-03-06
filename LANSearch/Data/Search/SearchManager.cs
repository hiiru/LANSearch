﻿using LANSearch.Data.Search.Solr;
using LANSearch.Data.Search.Solr.Filters;
using LANSearch.Models.Search;
using Mizore.CommunicationHandler.RequestHandler;
using Mizore.CommunicationHandler.ResponseHandler;
using Mizore.DataMappingHandler.Reflection;
using Mizore.SolrServerHandler;
using Nancy;
using System;
using System.Collections.Generic;

namespace LANSearch.Data.Search
{
    public class SearchManager
    {
        public SearchManager(AppConfig config)
        {
            SolrServer = new HttpSolrServer(config.SearchServerUrl);
            SolrMapper = new ReflectionDataMapper<File>();
        }

        public List<IFilter> GetFilters()
        {
            return new List<IFilter> { new Solr.Filters.Server(), new Extension(), new Size() };
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

        public SearchModel SearchByQuery(string query, DateTime lastExecution)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;

            var handler = new SolrHandler(query, lastExecution);
            return handler.SolrSearch();
        }
    }
}