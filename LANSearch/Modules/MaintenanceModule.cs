using Nancy;

namespace LANSearch.Modules
{
    public class MaintenanceModule : NancyModule
    {
        public MaintenanceModule()
        {
            Get["/Maintenance"] = x =>
            {
                var config = AppContext.GetContext().Config;
                if (!config.AppMaintenance)
                    return Response.AsRedirect("~/");
                return View["Views/Maintenance.cshtml", config.AppMaintenanceMessage];
            };
        }
    }
}