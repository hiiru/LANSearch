using LANSearch.Data;
using LANSearch.Data.User;
using LANSearch.Models.Member;
using LANSearch.Modules.BaseClasses;
using Nancy;
using Nancy.Responses;
using Nancy.Security;

namespace LANSearch.Modules.Member
{
    public class ProfileModule : AppModule
    {
        public ProfileModule()
            : base("/member")
        {
            // Note: Because this module isn't a MemberModule, we have to require Authentication here.
            // The module isn't MemberModule because of Unverified users.
            this.RequiresAuthentication();

            Get["/profile"] = x =>
            {
                var model = new ProfileModel { User = Context.CurrentUser as User };
                var success = Request.Query.success;
                if (!success.HasValue)
                {
                    var errPass = Request.Query.errPass;
                    int errPassCode = 0;
                    if (errPass.HasValue && int.TryParse(errPass.Value, out errPassCode))
                    {
                        if ((errPassCode & 1) == 1)
                        {
                            model.ChangePassErrorOldPass = true;
                        }
                        if ((errPassCode & 2) == 2)
                        {
                            model.ChangePassErrorNewPass = true;
                            model.ChangePassErrorNewPassText = "Password does not match.";
                        }
                        else if ((errPassCode & 4) == 4)
                        {
                            model.ChangePassErrorNewPass = true;
                            model.ChangePassErrorNewPassText = "Password is invalid.";
                        }
                    }

                    var errMail = Request.Query.errMail;
                    int errMailCode = 0;
                    if (errMail.HasValue && int.TryParse(errMail.Value, out errMailCode))
                    {
                        if ((errMailCode & 1) == 1)
                        {
                            model.ChangeMailErrorPass = true;
                        }
                        if ((errMailCode & 2) == 2)
                        {
                            model.ChangeMailErrorMail = true;
                        }
                    }
                    var errConfirm = Request.Query.errConfirm;
                    int errConfirmCode = 0;
                    if (errConfirm.HasValue && int.TryParse(errConfirm.Value, out errConfirmCode))
                    {
                        switch (errConfirmCode)
                        {
                            case 1:
                                model.ConfirmError = true;
                                break;

                            case 2:
                                model.ConfirmMailInvalid = true;
                                break;

                            case 3:
                                model.ConfirmMailAlreadyUsed = true;
                                break;
                        }
                    }
                }
                else
                {
                    switch (success.Value as string)
                    {
                        case "1":
                            model.ChangePassSuccess = true;
                            break;

                        case "2":
                            model.ChangeMailSuccess = true;
                            break;

                        case "3":
                            model.ConfirmAccountSuccess = true;
                            break;

                        case "4":
                            model.ConfirmResendSuccess = true;
                            break;
                    }
                }
                return View["views/Member/Profile.cshtml", model];
            };

            Post["/profile/ChangePass"] = x =>
            {
                try
                {
                    this.ValidateCsrfToken();
                }
                catch (CsrfValidationException)
                {
                    return Response.AsText("CSRF Token is invalid.").WithStatusCode(403);
                }
                bool oldPassValid = Ctx.UserManager.ValidatePassword(Context.CurrentUser as User, Request.Form.oldpass);
                bool newPassMatch = Request.Form.newpass == Request.Form.newpass2;
                bool newPassInvalid = string.IsNullOrWhiteSpace(Request.Form.newpass);
                if (oldPassValid && newPassMatch && newPassInvalid)
                {
                    Ctx.UserManager.ChangePassword(Context.CurrentUser as User, Request.Form.newpass);
                    return Response.AsRedirect("~/Member/Profile?success=1");
                }

                int error = 0;
                if (!oldPassValid)
                    error++;
                if (!newPassMatch)
                    error = error + 2;
                if (!newPassInvalid)
                    error = error + 4;
                //Workaround because Response.AsRedirect doesn't accept dynamic arguments
                var path = string.Format("{0}{1}{2}", Context.Request.Url.BasePath, "/Member/Profile?errpass=", error);
                return new RedirectResponse(path, RedirectResponse.RedirectType.SeeOther);
            };
            Post["/profile/ChangeEmail"] = x =>
            {
                try
                {
                    this.ValidateCsrfToken();
                }
                catch (CsrfValidationException)
                {
                    return Response.AsText("CSRF Token is invalid.").WithStatusCode(403);
                }
                bool passValid = Ctx.UserManager.ValidatePassword(Context.CurrentUser as User, Request.Form.emailpass);
                bool emailValid = ValidationHelper.EmailValid(Request.Form.email);
                if (passValid && emailValid)
                {
                    Ctx.UserManager.UpdateEmail(Context.CurrentUser as User, Request.Form.email);
                    return Response.AsRedirect("~/Member/Profile?success=2");
                }
                int error = 0;
                if (!passValid)
                    error++;
                if (!emailValid)
                    error = error + 2;

                //Workaround because Response.AsRedirect doesn't accept dynamic arguments
                var path = string.Format("{0}{1}{2}", Context.Request.Url.BasePath, "/Member/Profile?errmail=", error);
                return new RedirectResponse(path, RedirectResponse.RedirectType.SeeOther);
            };

            Post["/confirm"] = x =>
            {
                try
                {
                    this.ValidateCsrfToken();
                }
                catch (CsrfValidationException)
                {
                    return Response.AsText("CSRF Token is invalid.").WithStatusCode(403);
                }
                int error = 0;
                if (Request.Form.confirm)
                {
                    bool confirm = Ctx.UserManager.ActivateAccount(Context.CurrentUser as User, Request.Form.code);
                    if (confirm)
                    {
                        return Response.AsRedirect("~/Member/Profile?success=3");
                    }
                    error = 1;
                }
                else if (Request.Form.resend)
                {
                    var user = Context.CurrentUser as User;
                    error = Ctx.UserManager.ResendActivationMail(user, Request.Form.email, Request);
                    if (error == 0)
                    {
                        return Response.AsRedirect("~/Member/Profile?success=4");
                    }
                }
                //Workaround because Response.AsRedirect doesn't accept dynamic arguments
                var path = string.Format("{0}{1}{2}", Context.Request.Url.BasePath, "/Member/Profile?errConfirm=", error);
                return new RedirectResponse(path, RedirectResponse.RedirectType.SeeOther);
            };
            Get["/confirm/{userId:int}/{code}"] = x =>
            {
                int userId = x.userId;
                var user = Ctx.UserManager.Get(userId) as User;
                if (user != null)
                {
                    bool confirm = Ctx.UserManager.ActivateAccount(user, x.code);
                    if (confirm)
                    {
                        return Response.AsRedirect("~/Member/Profile?success=3");
                    }
                }
                if (Context.CurrentUser == null)
                {
                    return "Invalid Link.";
                }
                return Response.AsRedirect("~/Member/Profile?errConfirm=1");
            };
        }
    }
}