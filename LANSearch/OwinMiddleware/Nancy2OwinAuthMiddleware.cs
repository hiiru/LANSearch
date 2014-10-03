using Microsoft.Owin;
using System.Threading.Tasks;

namespace LANSearch.OwinMiddleware
{
    public class Nancy2OwinAuthMiddleware : Microsoft.Owin.OwinMiddleware
    {
        public Nancy2OwinAuthMiddleware(Microsoft.Owin.OwinMiddleware next)
            : base(next)
        {
        }

        public async override Task Invoke(IOwinContext context)
        {
            var appCtx = AppContext.GetContext();
            var user = appCtx.UserManager.GetUserByOwinEnvironment(context.Environment);
            if (context.Environment.ContainsKey("server.User"))
            {
                context.Environment["server.User"] = user;
            }
            else
            {
                context.Environment.Add("server.User", user);
            }
            await Next.Invoke(context);
        }
    }
}