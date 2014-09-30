using LANSearch.Data;
using LANSearch.Data.User;
using LANSearch.Models.Member;
using LANSearch.Modules.BaseClasses;
using Nancy;
using Nancy.Responses;

namespace LANSearch.Modules.Member
{
    public class ProfileModule : MemberModule
    {
        public ProfileModule()
        {
            Get["/profile"] = x =>
            {
                var model = new ProfileModel { User = Context.CurrentUser as User };
                var success = Request.Query.success;
                if (success == null)
                {

                    var errPass = Request.Query.errPass;
                    if (errPass != null)
                    {
                        int errPassCode = (int) errPass;
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
                    if (errMail != null)
                    {
                        int errMailCode = (int) errMail;
                        if ((errMailCode & 1) == 1)
                        {
                            model.ChangeMailErrorPass = true;
                        }
                        if ((errMailCode & 2) == 2)
                        {
                            model.ChangeMailErrorMail = true;
                        }
                    }
                }
                else if(success==1)
                {
                    model.ChangePassSuccess = true;
                }
                else if(success==2)
                {
                    model.ChangeMailSuccess = true;
                }
                return View["views/Member/Profile.cshtml", model];
            };
            
            Post["/profile/ChangePass"] = x =>
            {
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
        }
    }
}