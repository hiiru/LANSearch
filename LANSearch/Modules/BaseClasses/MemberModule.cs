using LANSearch.Data.User;
using Nancy.Security;

namespace LANSearch.Modules.BaseClasses
{
    public class MemberModule : AppModule
    {
        public MemberModule()
            : base("/member")
        {
            this.RequiresAuthentication();
            this.RequiresClaims(new[]{UserRoles.MEMBER});
        }
    }
}