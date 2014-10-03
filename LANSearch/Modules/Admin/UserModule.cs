using LANSearch.Data;
using LANSearch.Data.User;
using LANSearch.Models.Admin.User;
using LANSearch.Modules.BaseClasses;
using Nancy;
using Nancy.Responses;
using Nancy.Security;
using System;
using System.Text;

namespace LANSearch.Modules.Admin
{
    public class UserModule : AdminModule
    {
        public UserModule()
        {
            Get["/user"] = x =>
            {
                var model = GetUserListModel(Request);
                return View["Admin/User/UserList.cshtml", model];
            };
            Get["/user/disable/{id:int}"] = x =>
            {
                if (Context.CurrentUser == null) return 403;
                Ctx.UserManager.SetDisabled(x.id, true);
                return Response.AsRedirect("~/Admin/User");
            };
            Get["/user/enable/{id:int}"] = x =>
            {
                if (Context.CurrentUser == null) return 403;
                Ctx.UserManager.SetDisabled(x.id, false);
                return Response.AsRedirect("~/Admin/User");
            };
            Get["/user/detail/{id:int}"] = x =>
            {
                int id = x.id;
                var user = Ctx.UserManager.Get(id);
                if (user == null)
                    return Response.AsRedirect("~/Admin/User");
                return View["Admin/User/UserDetail.cshtml", new UserDetailModel { User = user }];
            };
            Post["/user/detail/{id:int}"] = x =>
            {
                try
                {
                    this.ValidateCsrfToken();
                }
                catch (CsrfValidationException)
                {
                    return Response.AsText("CSRF Token is invalid.").WithStatusCode(403);
                }
                int id = x.id;
                var user = Ctx.UserManager.Get(id);
                if (user == null)
                    return Response.AsRedirect("~/Admin/User");

                int status = Ctx.UserManager.UpdateUser(user.UserName, Request.Form.user, Request.Form.email, Request.Form.pass, Request.Form.emailkey, Request.Form.disabled);
                switch (status)
                {
                    case 0:
                        return "Unexpected Error, can't save.";

                    case 1:
                        user.UserName = Request.Form.user;
                        user.Email = Request.Form.email;
                        user.EmailValidationKey = Request.Form.emailkey;
                        user.Disabled = Request.Form.disabled;
                        return View["Admin/User/UserDetail.cshtml", new UserDetailModel { User = user, Error = "Username is already in use." }];
                }

                //Workaround because Response.AsRedirect doesn't accept dynamic arguments
                var path = string.Format("{0}{1}{2}", Context.Request.Url.BasePath, "/Admin/User/Detail/", user.Id);
                return new RedirectResponse(path, RedirectResponse.RedirectType.SeeOther);
            };

            Post["/user/detail/{id:int}/claims"] = x =>
            {
                try
                {
                    this.ValidateCsrfToken();
                }
                catch (CsrfValidationException)
                {
                    return Response.AsText("CSRF Token is invalid.").WithStatusCode(403);
                }
                int id = x.id;
                User user = Ctx.UserManager.Get(id);
                if (user == null)
                    return Response.AsRedirect("~/Admin/User");

                var claim = Request.Form.claim;
                if (UserRoles.IsValidRole(claim))
                {
                    if (Request.Form.add)
                    {
                        Ctx.UserManager.UpdateUserClaims(user.UserName, add: new string[] { claim });
                    }
                    else if (Request.Form.remove)
                    {
                        Ctx.UserManager.UpdateUserClaims(user.UserName, remove: new string[] { claim });
                    }
                }

                //Workaround because Response.AsRedirect doesn't accept dynamic arguments
                var path = string.Format("{0}{1}{2}", Context.Request.Url.BasePath, "/Admin/User/Detail/", x.id);
                return new RedirectResponse(path, RedirectResponse.RedirectType.SeeOther);
            };

            Get["/user/create"] = x =>
            {
                return View["Admin/User/UserDetail.cshtml", new UserDetailModel { User = new User(), IsCreation = true }];
            };
            Post["/user/create"] = x =>
            {
                try
                {
                    this.ValidateCsrfToken();
                }
                catch (CsrfValidationException)
                {
                    return Response.AsText("CSRF Token is invalid.").WithStatusCode(403);
                }
                Guid unused;
                UserRegisterState registerStatus = Ctx.UserManager.Register(Request.Form.user, Request.Form.email, Request.Form.pass, Request.Form.pass, Request, out unused, true);
                if (registerStatus == UserRegisterState.Ok)
                {
                    //Workaround because Response.AsRedirect doesn't accept dynamic arguments
                    var path = string.Format("{0}{1}{2}", Context.Request.Url.BasePath, "/Admin/User/Detail/", Request.Form.user);
                    return new RedirectResponse(path, RedirectResponse.RedirectType.SeeOther);
                }
                var sbError = new StringBuilder();
                if (registerStatus.HasFlag(UserRegisterState.UserEmpty))
                {
                    sbError.AppendLine("Username is empty.<br>");
                }
                else if (registerStatus.HasFlag(UserRegisterState.UserTooShort))
                {
                    sbError.AppendLine("Username is too short (requires 3-20 characters).<br>");
                }
                else if (registerStatus.HasFlag(UserRegisterState.UserTooLong))
                {
                    sbError.AppendLine("Username is too long (requires 3-20 characters).<br>");
                }
                else if (registerStatus.HasFlag(UserRegisterState.UserAlreadyTaken))
                {
                    sbError.AppendLine("Username is already taken.<br>");
                }

                if (registerStatus.HasFlag(UserRegisterState.EmailEmpty))
                {
                    sbError.AppendLine("Email is empty.<br>");
                }
                else if (registerStatus.HasFlag(UserRegisterState.EmailTooLong))
                {
                    sbError.AppendLine("Email is too long (only up to 50 characters are allowed).<br>");
                }
                else if (registerStatus.HasFlag(UserRegisterState.EmailInvalid))
                {
                    sbError.AppendLine("Invalid Email Format, please enter it in name@domain.tld format.<br>");
                }
                else if (registerStatus.HasFlag(UserRegisterState.EmailAlreadyUsed))
                {
                    sbError.AppendLine("Email is already used.<br>");
                }

                if (registerStatus.HasFlag(UserRegisterState.PassEmpty))
                {
                    sbError.AppendLine("Password is missing.<br>");
                }
                else if (registerStatus.HasFlag(UserRegisterState.PassMissmatch))
                {
                    sbError.AppendLine("Passwords does not match.<br>");
                }
                var user = new User
                {
                    UserName = Request.Form.user,
                    Email = Request.Form.email
                };
                return View["Admin/User/UserDetail.cshtml", new UserDetailModel { User = user, Error = sbError.ToString(), IsCreation = true }];
            };
        }

        public UserListModel GetUserListModel(Request request)
        {
            var userlist = new UserListModel(new UrlBuilder(Request.Url));
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
            userlist.Users = Ctx.UserManager.GetPaged(userlist.Page, userlist.PageSize, out count);
            userlist.Count = count;

            return userlist;
        }
    }
}