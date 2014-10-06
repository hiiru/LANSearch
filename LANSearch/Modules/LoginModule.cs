using LANSearch.Data.User;
using LANSearch.Models;
using LANSearch.Modules.BaseClasses;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Helpers;
using Nancy.Security;
using System;

namespace LANSearch.Modules
{
    public class LoginModule : AppModule
    {
        public LoginModule()
        {
            Get["/Login"] = x =>
            {
                if (Context.CurrentUser != null)
                {
                    return
                        View["login.cshtml", new LoginModel
                        {
                            IsLoggedIn = true,
                            LoginUsername = Context.CurrentUser.UserName
                        }];
                }
                return View["login.cshtml", new LoginModel
                {
                    IsLoggedIn = false,
                    ReturnUrl = Request.Query["returnUrl"],
                    WhyLoginMessage = GetMessage(Request.Query["returnUrl"]),
                }];
            };

            Post["/Login"] = x =>
            {
                try
                {
                    this.ValidateCsrfToken();
                }
                catch (CsrfValidationException)
                {
                    return Response.AsText("CSRF Token is invalid.").WithStatusCode(403);
                }
                if (Context.CurrentUser != null)
                    return Response.AsRedirect("~/Login");

                var user = Request.Form.user;
                var pass = Request.Form.pass;
                Guid guid;
                int status = Ctx.UserManager.Login(user, pass, Request, out guid);
                if (status == 0)
                {
                    return this.LoginAndRedirect(guid);
                }
                var model = new LoginModel
                {
                    IsLoggedIn = false,
                    LoginUsername = user,
                    ReturnUrl = Request.Query["returnUrl"]
                };
                switch (status)
                {
                    case 1:
                        model.LoginErrorUsername = "Username is empty.";
                        break;

                    case 2:
                        model.LoginErrorPassword = "Password is empty.";
                        break;

                    case 3:
                        model.LoginErrorUsername = "User does not exist.";
                        break;

                    case 4:
                        model.LoginErrorUsername = "User is disabled.";
                        break;

                    case 5:
                        model.LoginErrorUsername = "Invalid password.";
                        break;
                }
                return View["login.cshtml", model];
            };

            Get["/Login/Register"] = x => Response.AsRedirect("/Login");

            Post["/Login/Register"] = x =>
            {
                try
                {
                    this.ValidateCsrfToken();
                }
                catch (CsrfValidationException)
                {
                    return Response.AsText("CSRF Token is invalid.").WithStatusCode(403);
                }
                if (Context.CurrentUser != null)
                    return Response.AsRedirect("~/Login");

                Guid guid;
                UserRegisterState registerStatus = Ctx.UserManager.Register(Request.Form.regUser, Request.Form.regEmail, Request.Form.regPass1, Request.Form.regPass2, Request, out guid);
                if (registerStatus == UserRegisterState.Ok)
                {
                    return this.LoginAndRedirect(guid, fallbackRedirectUrl: "~/Member/Profile");
                }
                var model = new LoginModel();
                model.RegisterUser = Request.Form.regUser;
                model.RegisterEmail = Request.Form.regEmail;

                if (registerStatus.HasFlag(UserRegisterState.UserEmpty))
                {
                    model.RegisterErrorUser = "Username is empty.";
                }
                else if (registerStatus.HasFlag(UserRegisterState.UserTooShort))
                {
                    model.RegisterErrorUser = "Username is too short (requires 3-20 characters).";
                }
                else if (registerStatus.HasFlag(UserRegisterState.UserTooLong))
                {
                    model.RegisterErrorUser = "Username is too long (requires 3-20 characters).";
                }
                else if (registerStatus.HasFlag(UserRegisterState.UserAlreadyTaken))
                {
                    model.RegisterErrorUser = "Username is already taken.";
                }

                if (registerStatus.HasFlag(UserRegisterState.EmailEmpty))
                {
                    model.RegisterErrorEmail = "Email is empty.";
                }
                else if (registerStatus.HasFlag(UserRegisterState.EmailTooLong))
                {
                    model.RegisterErrorEmail = "Email is too long (only up to 50 characters are allowed).";
                }
                else if (registerStatus.HasFlag(UserRegisterState.EmailInvalid))
                {
                    model.RegisterErrorEmail = "Invalid Email Format, please enter it in name@domain.tld format";
                }
                else if (registerStatus.HasFlag(UserRegisterState.EmailAlreadyUsed))
                {
                    model.RegisterErrorEmail = "Email is already used.";
                }

                if (registerStatus.HasFlag(UserRegisterState.PassEmpty))
                {
                    model.RegisterErrorPassword = "Password is missing.";
                }
                else if (registerStatus.HasFlag(UserRegisterState.PassMissmatch))
                {
                    model.RegisterErrorPassword = "Passwords does not match.";
                }

                return View["login.cshtml", model];
            };

            Get["/Logout"] = x =>
            {
                if (Context.CurrentUser != null)
                {
                    return this.Logout("~/");
                }
                return Response.AsRedirect("~/");
            };
        }

        private string GetMessage(string returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl)) return null;
            returnUrl = HttpUtility.UrlDecode(returnUrl);
            int indexQuery = returnUrl.IndexOf('?');
            string returnPath = indexQuery != -1 ?
                returnUrl.Substring(0, indexQuery).ToLower() :
                returnUrl.ToLower();
            var pathSegements = returnPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (pathSegements[0] == "member")
            {
                if (pathSegements.Length == 1)
                    return null;
                switch (pathSegements[1])
                {
                    case "notification":
                        return "Please login to create or manage a Search Notification.";

                    case "server":
                        return "Please login to create or manage a Server.";

                    case "profile":
                        return "Please login to update your Profile settings.";
                }
            }
            else if (pathSegements[0] == "admin")
                return "Authenticate as an Admin to access admin features";
            return null;
        }
    }
}