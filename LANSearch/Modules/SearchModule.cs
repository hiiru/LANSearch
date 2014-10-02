using LANSearch.Data;
using LANSearch.Data.Search.Solr;
using LANSearch.Modules.BaseClasses;
using Nancy.Responses;

namespace LANSearch.Modules
{
    public class SearchModule : AppModule
    {
        public SearchModule()
        {
            Get["/"] = x => HandleGet();
            Get["/Search"] = x => HandleGet();
            Post["/"] = x => HandlePost();
            Post["/Search"] = x => HandlePost();
        }

        private dynamic HandleGet()
        {
            var results = Ctx.SearchManager.Search(Request);
            return View["search", results];
        }
        private dynamic HandlePost()
        {
            var urlBuilder = new UrlBuilder(Request.Url);
            urlBuilder.Remove("q");
            if (Request.Form.q != null)
                urlBuilder.Set("q", Request.Form.q);
            return new RedirectResponse(urlBuilder.ToString(), RedirectResponse.RedirectType.SeeOther);
        }
    }
}