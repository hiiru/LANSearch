using LANSearch.Data.Search.Solr;
using LANSearch.Modules.BaseClasses;

namespace LANSearch.Modules
{
    public class SearchModule : AppModule
    {
        public SearchModule()
        {
            Get["/Search"] = x => HandleGet();
            Get["/"] = x => HandleGet();
        }

        private dynamic HandleGet()
        {
            var query = new SolrHandler(Request);
            return View["search", query.SolrSearch()];
        }
    }
}