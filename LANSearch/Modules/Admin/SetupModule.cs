using LANSearch.Data.User;
using LANSearch.Models.Admin;
using Nancy;
using Nancy.Authentication.Forms;
using System;
using System.Text;
using Nancy.Security;

namespace LANSearch.Modules.Admin
{
    public class SetupModule : NancyModule
    {
        public SetupModule()
        {
            //TODO: Email configuration

            var Ctx = AppContext.GetContext();
            Before += nancycontext =>
            {
                if (Ctx.Config.AppSetupDone)
                    return Response.AsRedirect("~/");

                if (!InitConfig.SetupIps.Contains(nancycontext.Request.UserHostAddress))
                    return Response.AsRedirect("~/Maintenance");

                return null;
            };
            Get["/Admin/Setup"] = x =>
            {
                var model = new SetupModel();
                model.AesPass = Ctx.Config.AppSecurityAesPass;
                model.AesSalt = Convert.ToBase64String(Ctx.Config.AppSecurityAesSalt);
                model.HmacPass = Ctx.Config.AppSecurityHmacPass;
                model.HmacSalt = Convert.ToBase64String(Ctx.Config.AppSecurityHmacSalt);
                return View["Views/Admin/Setup.cshtml", model];
            };
            Post["/Admin/Setup"] = x =>
            {
                try
                {
                    this.ValidateCsrfToken();
                }
                catch (CsrfValidationException)
                {
                    return Response.AsText("CSRF Token is invalid.").WithStatusCode(403);
                }
                Guid guid;
                var registerStatus = Ctx.UserManager.Register(Request.Form.user, Request.Form.email, Request.Form.pass, Request.Form.pass, Request, out guid, false, new[] { UserRoles.MEMBER, UserRoles.ADMIN });

                if (registerStatus == UserRegisterState.Ok)
                {
                    Ctx.Config.AppSetupDone = true;
                    Ctx.Config.SaveConfigToRedis();
                    return this.LoginAndRedirect(guid, null, "~/Admin/Configuration");
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
                var model = new SetupModel
                {
                    AesPass = Request.Form.aespass,
                    AesSalt = Request.Form.aessalt,
                    HmacPass = Request.Form.hmacpass,
                    HmacSalt = Request.Form.hmacsalt,
                    Error = sbError.ToString(),
                    User = new User
                    {
                        UserName = Request.Form.user,
                        Email = Request.Form.email
                    }
                };
                return View["Views/Admin/Setup.cshtml", model];
            };
        }
    }
}