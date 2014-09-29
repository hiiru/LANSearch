using LANSearch.Modules.BaseClasses;
using Nancy;

namespace LANSearch.Modules
{
    public class SandboxModule : AppModule
    {
        public SandboxModule()
        {
            Get["/Jobs/test"] = x =>
            {
                return Response.AsJson(Ctx.RedisManager.SearchKeys("urn:server:*"));
            };

            Get["/Jobs/removekey/{key}"] = x =>
            {
                if (string.IsNullOrWhiteSpace(x.key)) return "Invalid key.";
                //var key = "urn:job_crawler:lock_11";
                //Ctx.RedisPool.GetClient().RemoveEntry(x.key);
                return "key cleared";
            };
            Get["/Jobs/Crawl/{id}"] = x =>
            {
                int serverId;
                if (!int.TryParse(x.id, out serverId))
                {
                    return "Invalid ID";
                }

                //BackgroundJob.Enqueue(() => Ctx.FtpCrawler.CrawlServer(serverId));
                return "OK - Enqueued Crawling of server " + serverId;
            };
        }
    }
}