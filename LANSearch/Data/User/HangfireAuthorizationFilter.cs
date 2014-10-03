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
            if (owinEnvironment.ContainsKey("server.User"))
            {
                var user = owinEnvironment["server.User"] as User;
                return user != null && !user.Disabled;
            }
            return false;
        }
    }
}