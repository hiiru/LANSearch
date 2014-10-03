using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
