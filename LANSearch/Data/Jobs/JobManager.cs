using Hangfire;
using LANSearch.Data.Jobs.Ftp;
using LANSearch.Data.Redis;

namespace LANSearch.Data.Jobs
{
    public class JobManager
    {
        protected RedisManager RedisManager;

        public JobManager(RedisManager redisManager, AppConfig config)
        {
            RedisManager = redisManager;
            FtpCrawler = new FtpCrawler();
            NotificationJob = new NotificationJob();

            //Setup hourly crawling
            if (config.JobHourlyCrawling)
                RecurringJob.AddOrUpdate(JOB_CRAWL_SERVERS, () => FtpCrawler.CrawlServers(), Cron.Hourly);
            else
                RecurringJob.RemoveIfExists(JOB_CRAWL_SERVERS);
        }

        #region Background Jobs

        public FtpCrawler FtpCrawler { get; protected set; }
        public NotificationJob NotificationJob { get; protected set; }

        #endregion Background Jobs

        #region Recurring Jobs

        protected const string JOB_CRAWL_SERVERS = "Ftp Crawler (All Servers)";

        public void RecuringAddCrawler(string interval = null)
        {
            if (interval == null)
                interval = Cron.Hourly();
            RecurringJob.AddOrUpdate(JOB_CRAWL_SERVERS, () => FtpCrawler.CrawlServers(), interval);
        }

        public void RecurringRemoveCrawler()
        {
            RecurringJob.RemoveIfExists(JOB_CRAWL_SERVERS);
        }

        public void RecurringRemove(string id)
        {
            RecurringJob.RemoveIfExists(id);
        }

        #endregion Recurring Jobs
    }
}