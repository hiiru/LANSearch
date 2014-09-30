using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LANSearch.Modules.BaseClasses;

namespace LANSearch.Modules.Admin
{
    public class LogViewerModule : AdminModule
    {
        public LogViewerModule()
        {
            Get["/LogViewer"] = x =>
            {
                return View["views/Admin/LogViewer.cshtml"];
            };
        }
    }
}
