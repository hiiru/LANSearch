using Hangfire;
using LANSearch.Data.Server;
using LANSearch.Models.Server;
using LANSearch.Modules.BaseClasses;
using Nancy;
using Nancy.Responses;
using Nancy.Security;

namespace LANSearch.Modules.Admin
{
    public class ServerModule : AdminModule
    {
        public ServerModule()
        {
            Get["/server"] = x =>
            {
                var model = Ctx.ServerManager.GetServerListModel(Request, -1);
                return View["Admin/Server/ServerList.cshtml", model];
            };

            Get["/server/detail/{serverId:int}"] = x =>
            {
                ServerDetailModel model = Ctx.ServerManager.GetModelDetail(x.serverId, 0, null, true);
                if (model == null)
                    return Response.AsRedirect("~/Admin/Server");

                return View["Admin/Server/ServerDetail.cshtml", model];
            };
            Post["/server/detail/{serverId:int}"] = x =>
            {
                try
                {
                    this.ValidateCsrfToken();
                }
                catch (CsrfValidationException)
                {
                    return Response.AsText("Can't add Server, CSRF Token is invalid.").WithStatusCode(403);
                }
                ServerDetailModel model = Ctx.ServerManager.GetModelDetail(x.serverId, 0, Request.Form, true);
                if (model == null)
                    return Response.AsRedirect("~/Admin/Server");
                if (model.ValidateServer())
                {
                    Ctx.ServerManager.Save(model.Server);
                    //Workaround because Response.AsRedirect doesn't accept dynamic arguments
                    var path = string.Format("{0}{1}{2}", Context.Request.Url.BasePath, "/Admin/Server/Detail/", x.serverId);
                    return new RedirectResponse(path, RedirectResponse.RedirectType.SeeOther);
                }

                return View["Admin/Server/ServerDetail.cshtml", model];
            };

            Post["/server/detail/{serverId:int}/action"] = x =>
            {
                try
                {
                    this.ValidateCsrfToken();
                }
                catch (CsrfValidationException)
                {
                    return Response.AsText("Can't add Server, CSRF Token is invalid.").WithStatusCode(403);
                }
                int serverId;
                if (!int.TryParse(x.serverId, out serverId))
                    return Response.AsRedirect("~/Admin/Server");
                var server = Ctx.ServerManager.Get(serverId);
                if (server == null)
                    return Response.AsRedirect("~/Admin/Server");

                if (Request.Form.remove)
                {
                    Ctx.SearchManager.UnindexServer(server.Id);
                }
                else if (Request.Form.rescan)
                {
                    Ctx.JobManager.EnqueueJob(() => Ctx.JobManager.FtpCrawler.CrawlServer(serverId, true));
                }
                else if (Request.Form.delete)
                {
                    Ctx.ServerManager.SetDeleted(server);
                }
                else if (Request.Form.restore)
                {
                    Ctx.ServerManager.SetDeleted(server, false);
                }

                //Workaround because Response.AsRedirect doesn't accept dynamic arguments
                var path = string.Format("{0}{1}{2}", Context.Request.Url.BasePath, "/Admin/Server/Detail/", x.serverId);
                return new RedirectResponse(path, RedirectResponse.RedirectType.SeeOther);
            };
            Get["/server/add"] = x =>
            {
                var model = new ServerDetailModel { IsCreation = true, Server = new Server(), IsAdmin = true };
                return View["Admin/Server/ServerDetail.cshtml", model];
            };
            Post["/server/add"] = x =>
            {
                try
                {
                    this.ValidateCsrfToken();
                }
                catch (CsrfValidationException)
                {
                    return Response.AsText("Can't add Server, CSRF Token is invalid.").WithStatusCode(403);
                }
                ServerDetailModel model = Ctx.ServerManager.GetModelDetail(0, 0, Request.Form, true);
                model.IsCreation = true;

                if (model.ValidateServer())
                {
                    Ctx.ServerManager.Save(model.Server);
                    return Response.AsRedirect("~/Admin/Server");
                }
                return View["Admin/Server/ServerDetail.cshtml", model];
            };
        }
    }
}