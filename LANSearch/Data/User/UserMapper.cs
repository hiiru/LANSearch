using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Security;
using System;

namespace LANSearch.Data.User
{
    public class UserMapper : IUserMapper
    {
        protected AppContext Ctx { get { return AppContext.GetContext(); } }

        public IUserIdentity GetUserFromIdentifier(Guid identifier, NancyContext context)
        {
            var user = Ctx.UserManager.GetByGuid(identifier);
            //if (user!=null)
            //    Trace.WriteLine("identifier "+identifier.ToString("D")+" looked up to user "+user.UserName);
            //else
            //    Trace.WriteLine("identifier " + identifier.ToString("D") + " NOT FOUND");
            if (user == null || user.Disabled)
            {
                return null;
            }
            return user;
        }
    }
}