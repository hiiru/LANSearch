using LANSearch.Modules.BaseClasses;

namespace LANSearch.Modules.Member
{
    public class NotificationModule : MemberModule
    {
        public NotificationModule()
        {
            //TODO
            Get["/Notifications"] = x =>
            {
                return "TODO:Notifications";
            };
        }
    }
}