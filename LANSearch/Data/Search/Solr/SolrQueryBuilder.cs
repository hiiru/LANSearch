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

        public SolrQueryBuilder(Request request, List<IFilter> filters)
        {
            QueryParameters = new NamedList<string>();

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
                        var filter = filters.FirstOrDefault(x => x.QSKey == qsKey);
                        if (filter != null)
                        {
                            filter.UpdateFilterQuery(QueryParameters, request.Query[qsKey]);
                        }
                        break;
                }
            }

            if (Page < 0)
                Page = 0;
            if (PageSize < 20)
                PageSize = 20;

            QueryParameters.Add(CommonParams.Q, keyword);
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

        public int Page { get; protected set; }

        public int PageSize { get; protected set; }

        public string Keyword { get; protected set; }

        public string GetRawQuery()
        {
            throw new NotImplementedException();
        }

        public INamedList<string> QueryParameters { get; private set; }
    }
}