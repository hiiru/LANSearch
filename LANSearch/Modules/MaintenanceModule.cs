using LANSearch.Models;
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
                var model = new MaintenanceModel();
                if (!config.AppSetupDone)
                {
                    model.Message = "The LANSearch Application Setup isn't completed yet by an Administrator. Please try again later.";
                    model.Setup = InitConfig.SetupIps.Contains(Request.UserHostAddress);
                }
                else if (config.AppMaintenance)
                {
                    model.Message = config.AppMaintenanceMessage;
                }
                else
                {
                    return Response.AsRedirect("~/");
                }
                return View["Views/Maintenance.cshtml", model];
            };
        }
    }
}