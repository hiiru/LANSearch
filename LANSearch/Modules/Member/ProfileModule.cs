using LANSearch.Modules.BaseClasses;

namespace LANSearch.Modules.Member
{
    public class ProfileModule : MemberModule
    {
        public ProfileModule()
        {
            //TODO
            Get["/profile"] = x =>
            {
                return "TODO:Profile";
            };
        }
    }
}