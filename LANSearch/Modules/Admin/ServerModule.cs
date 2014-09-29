using LANSearch.Data;
using LANSearch.Data.Server;
using LANSearch.Models.Admin.Server;
using Nancy;
using Nancy.Responses;
using System;
using System.Text;

namespace LANSearch.Modules.Admin
{
    public class ServerModule : AdminModule
    {
        public ServerModule()
        {
            Get["/server"] = x =>
            {
                var model = GetServerListModel(Request);
                return View["Admin/Server/ServerList.cshtml", model];
            };

            Get["/server/detail/{serverId:int}"] = x =>
            {
                int serverId;
                if (!int.TryParse(x.serverId, out serverId))
                    return Response.AsRedirect("~/Admin/Server");
                var server = Ctx.ServerManager.Get(serverId);
                if (server == null)
                    return Response.AsRedirect("~/Admin/Server");

                var model = new ServerDetailModel { Server = server };
                return View["Admin/Server/ServerDetail.cshtml", model];
            };
            Post["/server/detail/{serverId:int}"] = x =>
            {
                int serverId;
                if (!int.TryParse(x.serverId, out serverId))
                    return Response.AsRedirect("~/Admin/Server");
                var server = Ctx.ServerManager.Get(serverId);
                if (server == null)
                    return Response.AsRedirect("~/Admin/Server");

                int port;
                int.TryParse(Request.Form.port, out port);
                server.Address = Request.Form.address;
                server.Port = port;
                server.Login = Request.Form.login;
                server.Password = Request.Form.pass;
                server.Owner = Request.Form.owner;
                server.Name = Request.Form.name;
                server.Description = Request.Form.description;
                server.Closed = Request.Form.closed;
                server.Hidden = Request.Form.hidden;
                server.NoScans = Request.Form.noscans;

                var error = ValidateServer(server);
                if (error == null)
                {
                    Ctx.ServerManager.Save(server);
                    //Workaround because Response.AsRedirect doesn't accept dynamic arguments
                    var path = string.Format("{0}{1}{2}", Context.Request.Url.BasePath, "/Admin/Server/Detail/", x.serverId);
                    return new RedirectResponse(path, RedirectResponse.RedirectType.SeeOther);
                }

                var model = new ServerDetailModel
                {
                    Server = server,
                    IsCreation = true,
                    Error = error
                };
                return View["Admin/Server/ServerDetail.cshtml", model];
            };

            Get["/server/create"] = x =>
            {
                var model = new ServerDetailModel { IsCreation = true, Server = new Server() };
                return View["Admin/Server/ServerDetail.cshtml", model];
            };
            Post["/server/create"] = x =>
            {
                int port;
                int.TryParse(Request.Form.port, out port);

                var server = new Server
                {
                    Id = 0,
                    Protocol = 1,
                    Created = DateTime.Now,

                    Address = Request.Form.address,
                    Port = port,
                    Login = Request.Form.login,
                    Password = Request.Form.pass,
                    Owner = Request.Form.owner,
                    Name = Request.Form.name,
                    Description = Request.Form.description,
                    Closed = Request.Form.closed,
                    Hidden = Request.Form.hidden,
                    NoScans = Request.Form.noscans
                };

                var error = ValidateServer(server);
                if (error == null)
                {
                    Ctx.ServerManager.Save(server);
                    return Response.AsRedirect("~/Admin/Server");
                }

                var model = new ServerDetailModel
                {
                    Server = server,
                    IsCreation = true,
                    Error = error
                };
                return View["Admin/Server/ServerDetail.cshtml", model];
            };
        }

        public ServerListModel GetServerListModel(Request request)
        {
            var userlist = new ServerListModel(new UrlBuilder(Request.Url));
            foreach (var qs in request.Query)
            {
                var qsKey = qs as string;
                if (qsKey == null) continue;
                switch (qsKey)
                {
                    case "p":
                        int page;
                        if (int.TryParse(request.Query["p"], out page))
                            userlist.Page = page;
                        break;

                    case "ps":
                        int pagesize;
                        if (int.TryParse(request.Query["ps"], out pagesize))
                        {
                            userlist.PageSize = pagesize;
                        }
                        break;
                }
            }

            if (userlist.Page < 0)
                userlist.Page = 0;
            if (userlist.PageSize < 20)
                userlist.PageSize = 20;

            int count = 0;
            userlist.Servers = Ctx.ServerManager.GetPaged(userlist.Page, userlist.PageSize, out count);
            userlist.Count = count;

            return userlist;
        }

        public string ValidateServer(Server server)
        {
            if (server == null)
                return "ERROR: No server supplied!";

            var sbError = new StringBuilder();
            if (!ValidationHelper.IsValidIP(server.Address))
            {
                sbError.AppendLine("Invalid Server Address.<br>");
            }
            if (server.Port < 1 || server.Port > 65535)
            {
                sbError.AppendLine("Invalid Server Port.<br>");
            }

            return sbError.Length == 0 ? null : sbError.ToString();
        }
    }
}