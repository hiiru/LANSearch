using Nancy.Security;

namespace LANSearch.Modules.BaseClasses
{
    public class MemberModule : AppModule
    {
        public MemberModule()
            : base("/member")
        {
            this.RequiresAuthentication();
        }
    }
}