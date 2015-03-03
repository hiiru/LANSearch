using Hangfire;
using LANSearch.Data.Jobs.Ftp;
using LANSearch.Data.Redis;
using System;
using System.Linq.Expressions;

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

            InitRecurring(config);
        }

        #region Background Jobs

        public void EnqueueJob(Expression<Action> methodCall)
        {
            if (InitConfig.DisableHangfire)
                return;
            BackgroundJob.Enqueue(methodCall);
        }

        public FtpCrawler FtpCrawler { get; protected set; }

        public NotificationJob NotificationJob { get; protected set; }

        #endregion Background Jobs

        #region Recurring Jobs

        protected const string JOB_CRAWL_SERVERS = "Ftp Crawler (All Servers)";

        private void InitRecurring(AppConfig config)
        {
            if (InitConfig.DisableHangfire)
                return;
            //Setup hourly crawling
            if (config.JobHourlyCrawling)
                RecuringAddCrawler();
            else
                RecurringRemoveCrawler();
        }

        public void RecuringAddCrawler(string interval = null)
        {
            if (interval == null)
                //interval = Cron.Hourly();
                interval = "0 */2 * * *";
            RecurringJob.AddOrUpdate(JOB_CRAWL_SERVERS, () => FtpCrawler.CrawlServers(), interval);
        }

        public void RecurringRemoveCrawler()
        {
            RecurringJob.RemoveIfExists(JOB_CRAWL_SERVERS);
        }

        #endregion Recurring Jobs
    }
}