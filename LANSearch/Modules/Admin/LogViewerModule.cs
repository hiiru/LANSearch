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