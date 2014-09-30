using System;
using System.Collections.Generic;
using System.Linq;
using LANSearch.Data.User;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Nancy.Security;

namespace LANSearch.Hubs
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        protected User User;
        public override bool AuthorizeHubConnection(HubDescriptor hubDescriptor, IRequest request)
        {
            User = AppContext.GetContext().UserManager.GetUserByOwinEnvironment(request.Environment);
            return base.AuthorizeHubConnection(hubDescriptor, request);
        }
        
        protected override bool UserAuthorized(System.Security.Principal.IPrincipal user)
        {
            if (User == null || User.Disabled) 
                return false;

            if (Users.Length > 0)
            {
                var splitedUsers = SplitString(Users);
                return splitedUsers.Any(x => x == User.UserName);
            }

            if (Roles.Length > 0)
            {
                var splitedRoles = SplitString(Roles);
                return splitedRoles.Any(x => User.ClaimHas(x));
            }
            
            return true;

        }
        private static IEnumerable<string> SplitString(string original)
        {
            if (String.IsNullOrEmpty(original))
            {
                return new string[0];
            }

            var split = from piece in original.Split(',')
                        let trimmed = piece.Trim()
                        where !String.IsNullOrEmpty(trimmed)
                        select trimmed;
            return split.ToArray();
        }
    }
}
