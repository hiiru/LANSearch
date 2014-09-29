using Hangfire;
using Hangfire.Redis;
using LANSearch.Data.User;
using Owin;

namespace LANSearch
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseStaticFiles("/Content");
            app.UseStaticFiles("/Scripts");
            app.UseStaticFiles("/Fonts");
            app.UseNancy(options =>
            {
                options.PerformPassThrough = context => context.Request.Url.Path.StartsWith("/Admin/Hangfire");
            });

            app.UseHangfire(config =>
            {
                config.UseRedisStorage(string.Format("{0}:{1}", InitConfig.RedisServer, InitConfig.RedisPort), InitConfig.RedisDbHangfire);
                config.UseServer();
                config.UseDashboardPath("/Admin/Hangfire");
                config.UseAuthorizationFilters(new HangfireAuthorizationFilter());
            });
        }
    }
}