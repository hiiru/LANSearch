﻿using System.Collections.Generic;
using Hangfire;
using LANSearch.Data;
using LANSearch.Data.Server;
using LANSearch.Data.User;
using LANSearch.Models.Server;
using LANSearch.Modules.BaseClasses;
using Nancy;
using Nancy.Responses;
using System;
using Nancy.Security;
using ServiceStack;
using ServiceStack.Common.Utils;

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
                ServerDetailModel model = Ctx.ServerManager.GetModelDetail(x.serverId, null, true);
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
                ServerDetailModel model = Ctx.ServerManager.GetModelDetail(x.serverId, Request.Form,true);
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
                    //TODO
                }
                else if (Request.Form.rescan)
                {
                    BackgroundJob.Enqueue(() => Ctx.JobManager.FtpCrawler.CrawlServer(serverId));
                }
                else if (Request.Form.restore)
                {
                    Ctx.ServerManager.SetDeleted(server,false);
                }

                //Workaround because Response.AsRedirect doesn't accept dynamic arguments
                var path = string.Format("{0}{1}{2}", Context.Request.Url.BasePath, "/Admin/Server/Detail/", x.serverId);
                return new RedirectResponse(path, RedirectResponse.RedirectType.SeeOther);
            };
            Get["/server/create"] = x =>
            {
                var model = new ServerDetailModel { IsCreation = true, Server = new Server(), IsAdmin = true };
                return View["Admin/Server/ServerDetail.cshtml", model];
            };
            Post["/server/create"] = x =>
            {
                try
                {
                    this.ValidateCsrfToken();
                }
                catch (CsrfValidationException)
                {
                    return Response.AsText("Can't add Server, CSRF Token is invalid.").WithStatusCode(403);
                }
                ServerDetailModel model = Ctx.ServerManager.GetModelDetail(0, Request.Form, true);
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