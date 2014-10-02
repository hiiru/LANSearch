using LANSearch.Data.Search.Solr.Filters;
using Mizore.CommunicationHandler;
using Mizore.CommunicationHandler.Data.Params;
using Mizore.ContentSerializer.Data;
using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LANSearch.Data.Search.Solr
{
    public class SolrQueryBuilder : IQueryBuilder
    {
        private AppContext Ctx { get { return AppContext.GetContext(); } }

        public SolrQueryBuilder(Request request, List<IFilter> filters, bool notification = false)
        {
            QueryParameters = new NamedList<string>();
            IsEmptySearch = true;
            // FTS keyword
            string keyword = "*:*";
            foreach (var qs in request.Query)
            {
                var qsKey = qs as string;
                if (qsKey == null) continue;
                switch (qsKey)
                {
                    case "q":
                        if (!string.IsNullOrWhiteSpace(request.Query["q"]))
                        {
                            Keyword = request.Query["q"];
                            var sbKeywordQuery = new StringBuilder();
                            foreach (var word in Keyword.Split(' '))
                            {
                                if (sbKeywordQuery.Length > 0)
                                    sbKeywordQuery.Append(" AND ");
                                sbKeywordQuery.AppendFormat("fts:{0}", word);
                            }
                            keyword = sbKeywordQuery.ToString();
                            IsEmptySearch = false;
                        }
                        break;

                    case "p":
                        int page;
                        if (int.TryParse(request.Query["p"], out page))
                            Page = page;
                        break;

                    case "ps":
                        int pagesize;
                        if (int.TryParse(request.Query["ps"], out pagesize))
                        {
                            PageSize = pagesize > 100 ? 100 : pagesize;
                        }
                        break;

                    default:
                        if (notification && qsKey == "fsrv")
                        {
                            int srv;
                            int.TryParse(request.Query[qsKey], out srv);
                            ServerId = srv;
                        }
                        var filter = filters.FirstOrDefault(x => x.QSKey == qsKey);
                        if (filter != null)
                        {
                            filter.UpdateFilterQuery(QueryParameters, request.Query[qsKey]);
                        }
                        IsEmptySearch = false;
                        break;
                }
            }

            if (Page < 0)
                Page = 0;
            if (PageSize < 20)
                PageSize = 20;

            QueryParameters.Add(CommonParams.Q, keyword);

            if (notification)
                return;

            QueryParameters.Add(CommonParams.ROWS, PageSize.ToString());
            QueryParameters.Add(CommonParams.START, (PageSize * Page).ToString());

            if (filters != null)
            {
                QueryParameters.Add(FacetParams.FACET, "true");
                foreach (var filter in filters)
                {
                    filter.UpdateFacetQuery(QueryParameters);
                }
            }

            if (Ctx.Config.SearchAllowHideServer)
                HideHiddenServers();
        }

        public SolrQueryBuilder(string rawSolrQuery, DateTime lastExecution)
        {
            if (string.IsNullOrWhiteSpace(rawSolrQuery))
                throw new ArgumentNullException("rawSolrQuery");

            QueryParameters = new NamedList<string>();
            var kvPairs = rawSolrQuery.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part =>
                {
                    var split = part.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length != 2)
                        return null;
                    return new KeyValuePair<string, string>?(new KeyValuePair<string, string>(split[0], split[1]));
                }).Where(x => x.HasValue).Select(x => x.Value);
            foreach (var kvp in kvPairs)
            {
                QueryParameters.Add(kvp.Key, kvp.Value);
            }
            QueryParameters.Add(CommonParams.ROWS, "20");
            QueryParameters.Add(CommonParams.START, "0");

            lastExecution = DateTime.SpecifyKind(lastExecution, DateTimeKind.Local);
            QueryParameters.Add(CommonParams.FQ, string.Format("dateSeenFirst:[{0} TO *]", lastExecution.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")));
        }

        private void HideHiddenServers()
        {
            var hidden = Ctx.ServerManager.GetHiddenIds();
            if (hidden == null || hidden.Count == 0) return;
            var sbFilter = new StringBuilder();
            foreach (int id in hidden)
            {
                if (sbFilter.Length > 0)
                    sbFilter.Append(" AND ");
                sbFilter.AppendFormat("-server:{0}", id);
            }
            QueryParameters.Add(CommonParams.FQ, sbFilter.ToString());
        }

        public bool IsEmptySearch { get; protected set; }

        public int Page { get; protected set; }

        public int PageSize { get; protected set; }

        public int ServerId { get; protected set; }

        public string Keyword { get; protected set; }

        public string GetRawQuery()
        {
            //Do a sort here so that every search for the same thing, gets the same raw query
            var sortDict = new Dictionary<int, string>(QueryParameters.Count);
            for (int i = 0; i < QueryParameters.Count; i++)
            {
                sortDict.Add(i, QueryParameters.GetKey(i));
            }
            var sb = new StringBuilder();
            foreach (var sorted in sortDict.OrderBy(x => x.Value))
            {
                if (sb.Length > 0) sb.Append('&');
                sb.AppendFormat("{0}={1}", sorted.Value, QueryParameters.Get(sorted.Key));
            }
            return sb.ToString();
        }

        public INamedList<string> QueryParameters { get; private set; }
    }
}