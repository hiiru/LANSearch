using Common.Logging;
using LANSearch.Data.Server;
using LANSearch.Data.User;
using LANSearch.Models;
using LANSearch.Models.Server;
using LANSearch.Modules.BaseClasses;
using Nancy;
using Nancy.Responses;
using Nancy.Security;

namespace LANSearch.Modules.Member
{
    public class ServerModule : MemberModule
    {
        public ServerModule()
        {
            Get["/server"] = x =>
            {
                var user = Context.CurrentUser as User;
                var model = Ctx.ServerManager.GetServerListModel(Request, user.Id);
                return View["views/Member/Server/List.cshtml", model];
            };

            Get["/server/add"] = x =>
            {
                var user = Context.CurrentUser as User;
                ServerDetailModel model = Ctx.ServerManager.GetModelDetail(0, user.Id);
                model.IsCreation = true;
                return View["views/Member/Server/Add.cshtml", model];
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
                var user = Context.CurrentUser as User;
                ServerDetailModel model = Ctx.ServerManager.GetModelDetail(0, user.Id, Request.Form);
                model.IsCreation = true;

                if (model.ValidateServer())
                {
                    Ctx.ServerManager.Save(model.Server);
                    return Response.AsRedirect("~/Member/Server");
                }
                return View["views/Member/Server/Add.cshtml", model];
            };

            Get["/server/detail/{serverId:int}"] = x =>
            {
                var user = Context.CurrentUser as User;
                ServerDetailModel model = Ctx.ServerManager.GetModelDetail(x.serverId, user.Id);
                if (model == null)
                    return Response.AsRedirect("~/Member/Server");
                var currentUser = Context.CurrentUser as User;
                if (currentUser == null || model.Server.OwnerId != currentUser.Id)
                {
                    return ForbiddenResponse(model.Server, Context);
                }

                return View["views/Member/Server/Detail.cshtml", model];
            };
            Post["/server/detail/{serverId:int}"] = x =>
            {
                try
                {
                    this.ValidateCsrfToken();
                }
                catch (CsrfValidationException)
                {
                    return Response.AsText("Can't save Server, CSRF Token is invalid.").WithStatusCode(403);
                }

                var user = Context.CurrentUser as User;
                ServerDetailModel model = Ctx.ServerManager.GetModelDetail(x.serverId, user.Id, Request.Form);
                if (model == null)
                    return Response.AsRedirect("~/Member/Server");
                var currentUser = Context.CurrentUser as User;
                if (currentUser == null || model.Server.OwnerId != currentUser.Id)
                {
                    return ForbiddenResponse(model.Server, Context);
                }
                if (model.ValidateServer())
                {
                    Ctx.ServerManager.Save(model.Server);
                    return Response.AsRedirect("~/Member/Server");
                }
                return View["views/Member/Server/Detail.cshtml", model];
            };

            Post["/server/detail/{serverId:int}/action"] = x =>
            {
                try
                {
                    this.ValidateCsrfToken();
                }
                catch (CsrfValidationException)
                {
                    return Response.AsText("Can't save Server, CSRF Token is invalid.").WithStatusCode(403);
                }
                int serverId;
                if (!int.TryParse(x.serverId, out serverId))
                    return Response.AsRedirect("~/Member/Server");
                var server = Ctx.ServerManager.Get(serverId);
                if (server == null || server.Deleted)
                    return Response.AsRedirect("~/Member/Server");
                var currentUser = Context.CurrentUser as User;
                if (currentUser == null || server.OwnerId != currentUser.Id)
                {
                    return ForbiddenResponse(server, Context);
                }

                if (Request.Form.delete)
                {
                    Ctx.ServerManager.SetDeleted(server);
                    return Response.AsRedirect("~/Member/Server");
                }
                var path = string.Format("{0}{1}{2}", Context.Request.Url.BasePath, "/Member/Server/Detail/", x.serverId);
                return new RedirectResponse(path, RedirectResponse.RedirectType.SeeOther);
            };
        }

        protected dynamic ForbiddenResponse(Server srv, NancyContext context)
        {
            if (srv == null)
                return 403;

            var logger = LogManager.GetCurrentClassLogger();
            var currentUser = context.CurrentUser as User;
            if (currentUser == null)
                logger.WarnFormat("[Unauthorized Access->Server] Connection without user (ip:{0}) tried to access the serverId {1} without permission!", Request.UserHostAddress, srv.Id);
            else
                logger.WarnFormat("[Unauthorized Access->Server] User {0} (id:{1}, ip:{2}) tried to access the serverId {3} without permission!", currentUser.UserName, currentUser.Id, Request.UserHostAddress, srv.Id);

            var view = View["views/Error.cshtml", new ErrorModel
            {
                StatusCode = 403,
                ErrorTitle = "Unauthorized Access to Server " + srv.Id + " Forbidden!",
                ErrorType = "danger",
                ErrorMessage =
                    "<strong>Unauthorized Access to a Server detected and logged!</strong>" +
                    "<br>Please do NOT try to access the server of someone else." +
                    "<br>Continuing to access unauthorized content might lead to lost of account or ip-ban."
            }];
            view.NegotiationContext.StatusCode = HttpStatusCode.Forbidden;
            return view;
        }
    }
}