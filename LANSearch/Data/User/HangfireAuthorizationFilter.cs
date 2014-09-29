using Hangfire.Dashboard;
using Nancy.Authentication.Forms;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LANSearch.Data.User
{
    public class HangfireAuthorizationFilter : IAuthorizationFilter
    {
        public bool Authorize(IDictionary<string, object> owinEnvironment)
        {
            try
            {
                var requestHeaders = ((IDictionary<string, string[]>)owinEnvironment["owin.RequestHeaders"]);
                if (!requestHeaders.ContainsKey("Cookie"))
                    return false;

                var authCookieValue = requestHeaders["Cookie"]
                    .Select(cookie => cookie.Split(';')[0])
                    .Select(cookie =>
                    {
                        var x = cookie.Split('=');
                        if (x.Length == 2 && x[0] == FormsAuthentication.FormsAuthenticationCookieName)
                            return x[1];
                        return null;
                    }).FirstOrDefault(cookie => cookie != null);
                if (string.IsNullOrWhiteSpace(authCookieValue)) return false;

                var key = FormsAuthentication.DecryptAndValidateAuthenticationCookie(authCookieValue, AuthenticationConfiguration.GetContext().FormsAuthenticationConfiguration);
                Guid guid;
                if (!Guid.TryParse(key, out guid))
                    return false;
                var user = AppContext.GetContext().UserManager.GetByGuid(guid);
                return user != null && !user.Disabled && user.ClaimHas(UserRoles.ADMIN);
            }
            catch
            {
                return false;
            }
        }
    }
}