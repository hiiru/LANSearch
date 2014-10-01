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
            var results=Ctx.SearchManager.Search(Request);
            return View["search", results];
        }
    }
}