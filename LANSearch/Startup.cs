using Common.Logging;
using Hangfire;
using Hangfire.Redis;
using LANSearch.Data.User;
using Microsoft.AspNet.SignalR;
using Owin;

namespace LANSearch
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Debug("Entered Configuration");

            app.Use<LoggerMiddleware>(app);
            logger.Debug("LoggerMiddleware added.");
            
            app.MapSignalR("/sr", new HubConfiguration {EnableJavaScriptProxies = false});
            logger.Debug("MapSignalR Done.");

            app.UseNancy(options =>
            {
                options.PerformPassThrough = context => context.Request.Url.Path.StartsWith("/Admin/Hangfire");
            });

            logger.Debug("UseNancy Done.");
            
            app.UseHangfire(config =>
            {
                config.UseRedisStorage(string.Format("{0}:{1}", InitConfig.RedisServer, InitConfig.RedisPort), InitConfig.RedisDbHangfire);
                config.UseServer();
                config.UseDashboardPath("/Admin/Hangfire");
                config.UseAuthorizationFilters(new HangfireAuthorizationFilter());
            });
            logger.Debug("UseHangfire Done, completed startup configuration");
        }
    }
}