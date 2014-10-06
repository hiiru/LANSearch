using LANSearch.Data.User;
using Nancy;
using Nancy.Security;
using System.Linq;

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
                    if (Ctx.Config.AppBlockedIps != null && Ctx.Config.AppBlockedIps.Any(x => x.IsInRange(Request.UserHostAddress)))
                        return 403;

                    if (Ctx.Config.AppMaintenance || !Ctx.Config.AppSetupDone)
                        return Response.AsRedirect("~/Maintenance");
                }
                if (Ctx.Config.AppAnnouncement)
                    ViewBag.App_Announcement = Ctx.Config.AppAnnouncementMessage;

                ViewBag.App_NancyDiagnostics = !string.IsNullOrWhiteSpace(Ctx.Config.NancyDiagnosticsPassword);
                return null;
            };
        }

        public AppContext Ctx { get { return AppContext.GetContext(); } }
    }
}