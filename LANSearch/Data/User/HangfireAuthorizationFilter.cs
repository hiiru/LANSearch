using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire.Dashboard;
using Nancy.Authentication.Forms;

namespace LANSearch.Data.User
{
    public class HangfireAuthorizationFilter : IAuthorizationFilter
    {
        public bool Authorize(IDictionary<string, object> owinEnvironment)
        {
            try
            {
                var user = AppContext.GetContext().UserManager.GetUserByOwinEnvironment(owinEnvironment);
                return user != null && !user.Disabled && user.ClaimHas(UserRoles.ADMIN);
            }
            catch
            {
                return false;
            }
        }
    }
}