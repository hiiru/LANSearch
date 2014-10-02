using LANSearch.Data.Search.Solr.Filters;
using LANSearch.Models.Search;
using Mizore;
using Mizore.CommunicationHandler.Data;
using Mizore.CommunicationHandler.RequestHandler;
using Mizore.CommunicationHandler.ResponseHandler;
using Mizore.ContentSerializer.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mizore.DataMappingHandler;
using Mizore.SolrServerHandler;

namespace LANSearch.Data.Search.Solr
{
    public class SolrHandler
    {
        protected AppContext Ctx
        {
            get { return AppContext.GetContext(); }
        }

        public SolrHandler(Nancy.Request request)
        {
            switch (request.Url.Path)
            {
                case "/":
                case "/Search":
                    break;

                default:
                    //Invalid request
                    throw new InvalidOperationException("SolrHandler can only be used in /Search");
            }
            Filters = Ctx.SearchManager.GetFilters();
            QueryBuilder = new SolrQueryBuilder(request, Filters);
            UrlBuilder = new UrlBuilder(request.Url);
        }

        public SolrHandler(string rawQuery, DateTime lastExecution)
        {
            QueryBuilder = new SolrQueryBuilder(rawQuery, lastExecution);
            UrlBuilder = new UrlBuilder("/");
            NoFacets = true;
        }

        protected bool NoFacets { get; set; }
        protected List<IFilter> Filters { get; set; }

        protected ISolrServerHandler SolrServer { get { return Ctx.SearchManager.SolrServer; } }
        protected IDataMappingHandler SolrMapper { get { return Ctx.SearchManager.SolrMapper; } }
        
        protected UrlBuilder UrlBuilder { get; set; }

        protected SolrQueryBuilder QueryBuilder { get; set; }

        public SearchModel SolrSearch()
        {
            if (Ctx.Config.SearchDisabled)
            {
                return new SearchModel("Search is currently disabled by the Administrator.");
            }

            var solrRequest = new SelectRequest(SolrServer.GetUriBuilder(), QueryBuilder);
            SelectResponse solrResponse;

            if (!SolrServer.TryRequest(solrRequest, out solrResponse))
            {
                return new SearchModel("Sorry, you're search couldn't be executed at the moment, please try again later.");
            }
            var sm = new SearchModel(UrlBuilder.Clone())
            {
                Keyword = QueryBuilder.Keyword,
                PageSize = QueryBuilder.PageSize,
                Page = QueryBuilder.Page,
                HasSearchParameters = !QueryBuilder.IsEmptySearch
            };

            if (solrResponse.Documents.IsNullOrEmpty())
                return sm;
            sm.Count = solrResponse.Documents.NumFound;
            sm.Results = ParseDocuments(solrResponse.GetObjects<File>(SolrMapper));
            if (!NoFacets)
                sm.Filters = ParseFacets(solrResponse.Facets);
            return sm;
        }
        
        private List<SearchFilter> ParseFacets(FacetData facets)
        {
            if (facets == null) return null;
            var filters = new List<SearchFilter>();
            var server = new SearchFilter { Title = "Server" };
            var size = new SearchFilter { Title = "File Size" };
            var extension = new SearchFilter { Title = "File Extension" };
            if (facets.Fields.Count > 0)
            {
                for (int i = 0; i < facets.Fields.Count; i++)
                {
                    var key = facets.Fields.GetKey(i);
                    var solrFilter = Filters.FirstOrDefault(x => x.HandlesSolrKey(key));
                    if (solrFilter == null) continue;
                    SearchFilter current;
                    switch (key)
                    {
                        case "server":
                            current = server;
                            break;

                        case "fileExt":
                            current = extension;
                            break;

                        default:
                            continue;
                    }
                    var entries = facets.Fields.GetOrDefault<INamedList>(i);
                    if (entries == null)
                        continue;
                    current.Items = new List<SearchFilter.SearchFilterItem>();
                    var url = UrlBuilder.Clone();
                    for (int j = 0; j < entries.Count; j++)
                    {
                        var text = entries.GetKey(j);
                        var title = solrFilter.GetFilterText(text);
                        if (title == null) continue;
                        var isActive = solrFilter.IsSelected(text);
                        current.Items.Add(new SearchFilter.SearchFilterItem
                        {
                            Title = title,
                            Count = entries.GetOrDefaultStruct<Int64>(j),
                            IsActive = isActive,
                            Url = isActive ? url.Remove(solrFilter.QSKey).ToString() : url.Set(solrFilter.QSKey, solrFilter.GetQSValue(text)).ToString()
                        });
                    }
                }
            }
            if (facets.Queries.Count > 0)
            {
                size.Items = new List<SearchFilter.SearchFilterItem>();
                for (int i = 0; i < facets.Queries.Count; i++)
                {
                    var key = facets.Queries.GetKey(i);
                    var solrFilter = Filters.FirstOrDefault(x => x.HandlesSolrKey(key));
                    if (solrFilter == null) continue;
                    if (key != null)
                    {
                        var title = solrFilter.GetFilterText(key);
                        if (title == null) continue;
                        var url = UrlBuilder.Clone();
                        var isActive = solrFilter.IsSelected(key);
                        size.Items.Add(new SearchFilter.SearchFilterItem
                        {
                            Title = title,
                            Count = facets.Queries.GetOrDefaultStruct<Int64>(i),
                            IsActive = isActive,
                            Url = isActive ? url.Remove(solrFilter.QSKey).ToString() : url.Set(solrFilter.QSKey, solrFilter.GetQSValue(key)).ToString()
                        });
                    }
                }
            }
            if (server.HasItems)
                filters.Add(server);
            if (size.HasItems)
                filters.Add(size);
            if (extension.HasItems)
                filters.Add(extension);
            return filters.Count == 0 ? null : filters;
        }

        protected List<SearchFile> ParseDocuments(IList<File> docs)
        {
            var items = new List<SearchFile>();
            foreach (var file in docs)
            {
                var sfi = new SearchFile
                {
                    Extension = file.Extension,
                    Id = file.Id,
                    //Login = file.Login,
                    Path = file.Path,
                    Date = file.DateLastSeen.ToString(),
                    Name = Path.GetFileName(file.Path),
                    Size = GetReadableSize(file.Size),
                    Server = Ctx.ServerManager.Get(file.ServerId)
                };
                items.Add(sfi);
            }
            return items;
        }

        protected string GetReadableSize(long size)
        {
            float displaySize = size;
            int unit = 0;
            while (displaySize > 1536)
            {
                displaySize = displaySize / 1024;
                unit++;
                if (unit > 5)
                {
                    displaySize = size;
                    break;
                }
            }

            string unitString;

            switch (unit)
            {
                case 1:
                    unitString = "KB";
                    break;

                case 2:
                    unitString = "MB";
                    break;

                case 3:
                    unitString = "GB";
                    break;

                case 4:
                    unitString = "TB";
                    break;

                case 5:
                    unitString = "EB";
                    break;

                default:
                    unitString = "";
                    break;
            }
            if (string.IsNullOrWhiteSpace(unitString))
                return displaySize.ToString("0") + " " + unitString;
            else
                return displaySize.ToString("0.0") + " " + unitString;
        }
    }
}