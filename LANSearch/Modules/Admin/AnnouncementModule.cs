using LANSearch.Modules.BaseClasses;

namespace LANSearch.Modules.Admin
{
    public class AnnouncementModule : AdminModule
    {
        public AnnouncementModule()
        {
            Get["/Announcement"] = x => View["Admin/Announcement.cshtml"];
        }
    }
}