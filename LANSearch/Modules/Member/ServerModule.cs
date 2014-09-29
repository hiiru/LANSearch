using LANSearch.Modules.BaseClasses;

namespace LANSearch.Modules.Member
{
    public class ServerModule : MemberModule
    {
        public ServerModule()
        {
            //TODO
            Get["/server"] = x =>
            {
                return "TODO:Server";
            };
        }
    }
}