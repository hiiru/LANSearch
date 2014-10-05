using Nancy;
using SquishIt.Framework;
using System.IO;
using System.Text;

namespace LANSearch.Modules
{
    public class StaticModule : NancyModule
    {
        public StaticModule()
            : base("/static")
        {
            Get["css"] = x =>
            {
                return CreateResponse(Bundle.Css().RenderCached("css"), Configuration.Instance.CssMimeType);
            };
            Get["js"] = x =>
            {
                return CreateResponse(Bundle.JavaScript().RenderCached("js"), Configuration.Instance.CssMimeType);
            };
        }

        private Response CreateResponse(string content, string contentType)
        {
            return Response
                .FromStream(() => new MemoryStream(Encoding.UTF8.GetBytes(content)), contentType)
                .WithHeader("Cache-Control", "max-age=5");
        }
    }
}