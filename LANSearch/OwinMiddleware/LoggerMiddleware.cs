using System.Threading.Tasks;
using Common.Logging;
using Microsoft.Owin;
using Owin;

namespace LANSearch.OwinMiddleware
{
    public class LoggerMiddleware : Microsoft.Owin.OwinMiddleware
    {
        protected ILog Logger;

        public LoggerMiddleware(Microsoft.Owin.OwinMiddleware next, IAppBuilder app) : base(next)
        {
            Logger = LogManager.GetCurrentClassLogger();
        }

        public override async Task Invoke(IOwinContext context)
        {
            await Next.Invoke(context);
            string referer = context.Request.Headers.ContainsKey("referer")
                ? context.Request.Headers["referer"]
                : "-";
            string uagent = context.Request.Headers.ContainsKey("user-agent")
                ? context.Request.Headers["user-agent"]
                : "-";
            Logger.InfoFormat(@"{0} ""{1}"" ""{2}"" {3} {4} ""{5}"" ""{6}""",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString,
                context.Response.StatusCode,
                context.Request.RemoteIpAddress,
                referer,
                uagent);
        }
    }
}
