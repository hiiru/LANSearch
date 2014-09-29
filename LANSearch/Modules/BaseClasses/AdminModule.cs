using LANSearch.Data.User;
using LANSearch.Modules.BaseClasses;
using Nancy.Security;

namespace LANSearch.Modules.Admin
{
    public class AdminModule : AppModule
    {
        public AdminModule()
            : base("/admin")
        {
            this.RequiresAuthentication();
            this.RequiresClaims(new[] { UserRoles.ADMIN });
        }
    }
}