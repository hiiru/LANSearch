﻿using Common.Logging;
using Hangfire;
using Hangfire.Redis;
using LANSearch.Data.User;
using LANSearch.OwinMiddleware;
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

            app.Use<Nancy2OwinAuthMiddleware>();
            logger.Debug("Nancy2OwinAuthMiddleware added.");

            app.MapSignalR("/sr", new HubConfiguration { EnableJavaScriptProxies = false });
            logger.Debug("MapSignalR Done.");

            if (InitConfig.DisableHangfire)
            {
                logger.Debug("Skipping Hangfire, DisableHangfire is set.");
            }
            else
            {
                app.UseHangfire(config =>
                    {
                        config.UseRedisStorage(string.Format("{0}:{1}", InitConfig.RedisServer, InitConfig.RedisPort),
                            InitConfig.RedisDbHangfire);
                        config.UseServer();
                        config.UseDashboardPath("/Admin/Hangfire");
                        config.UseAuthorizationFilters(new HangfireAuthorizationFilter());
                    });
                logger.Debug("UseHangfire Done.");
            }

            app.UseNancy();
            logger.Debug("UseNancy Done, completed startup configuration");
        }
    }
}