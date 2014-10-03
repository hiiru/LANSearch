using LANSearch.Modules.BaseClasses;
using Nancy;
using System.Collections.Generic;
using System.Linq;

namespace LANSearch.Modules.Admin
{
    public class ConfigurationModule : AdminModule
    {
        public ConfigurationModule()
        {
            Get["/Configuration"] = x =>
            {
                var config = Ctx.Config.GetConfigDictionary().Where(setting => !AppConfig.ConfigBlacklist.Contains(setting.Key));
                return View["Views/Admin/Configuration.cshtml", config.OrderBy(kv => kv.Key)];
            };
            Post["/Configuration"] = x =>
            {
                var config = Ctx.Config.GetConfigDictionary().Where(setting => !AppConfig.ConfigBlacklist.Contains(setting.Key)).ToDictionary(y => y.Key, y => y.Value);
                foreach (var item in config.Keys.ToList())
                {
                    if (config[item] is bool)
                        config[item] = ((string)Request.Form[item]).ToBool();
                    else if (Request.Form[item] != null)
                        config[item] = Request.Form[item].Value;
                }
                Ctx.Config.SetConfigDictionary(config);
                Ctx.Config.SaveConfigToRedis();
                return Response.AsRedirect("~/Admin/Configuration");
            };
            Get["/Redis"] = x =>
            {
                Dictionary<string, string> items;
                using (var client = Ctx.RedisManager.Pool.GetClient())
                {
                    var keys = client.GetAllKeys();
                    items = client.GetValuesMap(keys);
                }
                return View["Views/Admin/Redis.cshtml", items.OrderBy(y => y.Key)];
            };
            Post["/Redis"] = x =>
            {
                if (!string.IsNullOrWhiteSpace(Request.Form.delete))
                {
                    using (var client = Ctx.RedisManager.Pool.GetClient())
                    {
                        client.RemoveEntry(Request.Form.delete);
                    }
                }

                return Response.AsRedirect("~/Admin/Redis");
            };
            Get["/Jobs"] = x =>
            {
                return View["Views/Admin/Jobs.cshtml"];
            };
            Post["/Jobs"] = x =>
            {
                if (Request.Form.recurringStop)
                    Ctx.JobManager.RecurringRemoveCrawler();
                else if (Request.Form.recurringStart)
                    Ctx.JobManager.RecuringAddCrawler();

                return View["Views/Admin/Jobs.cshtml"];
            };
        }
    }
}