using LANSearch.Data.User;
using Nancy.Security;

namespace LANSearch.Modules.BaseClasses
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