using Common.Logging;
using LANSearch.Data.Notification;
using LANSearch.Data.User;
using LANSearch.Models;
using LANSearch.Modules.BaseClasses;
using Nancy;
using Nancy.Responses;
using Nancy.Security;

namespace LANSearch.Modules.Member
{
    public class NotificationModule : MemberModule
    {
        public NotificationModule()
        {
            Get["/Notification"] = x =>
            {
                var user = Context.CurrentUser as User;
                var model = Ctx.NotificationManager.GetListModel(Request, user);
                return View["Member/Notification/List.cshtml", model];
            };
            Get["/Notification/Create"] = x =>
            {
                var user = Context.CurrentUser as User;
                var model = Ctx.NotificationManager.GetNotificationFromQuery(Request, user);
                return View["Member/Notification/Create.cshtml", model];
            };
            Post["/Notification/Create"] = x =>
            {
                var user = Context.CurrentUser as User;
                var model = Ctx.NotificationManager.GetNotificationFromQuery(Request, user);
                if (model.ValidateNotification())
                {
                    Ctx.NotificationManager.Save(model.Notification);
                    return Response.AsRedirect("~/Member/Notification");
                }

                return View["Member/Notification/Create.cshtml", model];
            };
            Get["/Notification/Detail/{id:int}"] = x =>
            {
                var model = Ctx.NotificationManager.GetNotificationDetail(x.id);
                if (model == null)
                    return Response.AsRedirect("~/Member/Notification");
                var currentUser = Context.CurrentUser as User;
                if (currentUser == null || model.Notification.OwnerId != currentUser.Id)
                {
                    return ForbiddenResponse(model.Notification, Context);
                }
                return View["Member/Notification/Detail.cshtml", model];
            };
            Post["/Notification/Detail/{id:int}"] = x =>
            {
                try
                {
                    this.ValidateCsrfToken();
                }
                catch (CsrfValidationException)
                {
                    return Response.AsText("Can't save Server, CSRF Token is invalid.").WithStatusCode(403);
                }
                var model = Ctx.NotificationManager.GetNotificationDetail(x.id, Request);
                if (model == null)
                    return Response.AsRedirect("~/Member/Notification");
                var currentUser = Context.CurrentUser as User;
                if (currentUser == null || model.Notification.OwnerId != currentUser.Id)
                {
                    return ForbiddenResponse(model.Notification, Context);
                }

                if (model.ValidateNotification())
                {
                    Ctx.NotificationManager.Save(model.Notification);
                    return Response.AsRedirect("~/Member/Notification");
                }

                return View["Member/Notification/Create.cshtml", model];
            };
            Post["/Notification/Detail/{id:int}/Action"] = x =>
            {
                try
                {
                    this.ValidateCsrfToken();
                }
                catch (CsrfValidationException)
                {
                    return Response.AsText("Can't save Server, CSRF Token is invalid.").WithStatusCode(403);
                }
                var notification = Ctx.NotificationManager.Get(x.id);
                if (notification == null)
                    return Response.AsRedirect("~/Member/Notification");
                var currentUser = Context.CurrentUser as User;
                if (currentUser == null || notification.OwnerId != currentUser.Id)
                {
                    return ForbiddenResponse(notification, Context);
                }

                if (Request.Form.disable)
                {
                    Ctx.NotificationManager.SetDisabled(notification, true);
                }
                else if (Request.Form.enable)
                {
                    Ctx.NotificationManager.SetDisabled(notification, false);
                }
                else if (Request.Form.delete)
                {
                    Ctx.NotificationManager.SetDeleted(notification, true);
                    return Response.AsRedirect("~/Member/Notification");
                }
                var path = string.Format("{0}{1}{2}", Context.Request.Url.BasePath, "/Member/Notification/Detail/", x.id);
                return new RedirectResponse(path, RedirectResponse.RedirectType.SeeOther);
            };
        }

        protected dynamic ForbiddenResponse(Notification not, NancyContext context)
        {
            if (not == null)
                return 403;

            var logger = LogManager.GetCurrentClassLogger();
            var currentUser = context.CurrentUser as User;
            if (currentUser == null)
                logger.WarnFormat("[Unauthorized Access->Notification] Connection without user (ip:{0}) tried to access the notificationId {1} without permission!", Request.UserHostAddress, not.Id);
            else
                logger.WarnFormat("[Unauthorized Access->Notification] User {0} (id:{1}, ip:{2}) tried to access the notificationId {3} without permission!", currentUser.UserName, currentUser.Id, Request.UserHostAddress, not.Id);

            var view = View["views/Error.cshtml", new ErrorModel
            {
                StatusCode = 403,
                ErrorTitle = "Unauthorized Access to Notification " + not.Id + " Forbidden!",
                ErrorType = "danger",
                ErrorMessage =
                    "<strong>Unauthorized Access to a Notification detected and logged!</strong>" +
                    "<br>Please do NOT try to access the notifications of someone else." +
                    "<br>Continuing to access unauthorized content might lead to lost of account or IP-ban."
            }];
            view.NegotiationContext.StatusCode = HttpStatusCode.Forbidden;
            return view;
        }
    }
}