﻿using LANSearch.Data.User;
using Nancy;
using Nancy.Security;

namespace LANSearch.Modules.BaseClasses
{
    public class AppModule : NancyModule
    {
        public AppModule()
            : this(string.Empty)
        {
        }

        public AppModule(string modulePath)
            : base(modulePath)
        {
            Before += ctx =>
            {
                if (!Context.CurrentUser.HasClaim(UserRoles.ADMIN))
                {
                    if (Ctx.Config.AppBlockedIps != null && Ctx.Config.AppBlockedIps.Contains(Request.UserHostAddress))
                        return 403;

                    if (Ctx.Config.AppMaintenance)
                        return Response.AsRedirect("~/Maintenance");
                }
                if (Ctx.Config.AppAnnouncement)
                    ViewBag.App_Announcement = Ctx.Config.AppAnnouncementMessage;

                return null;
            };
        }

        public AppContext Ctx { get { return AppContext.GetContext(); } }
    }
}