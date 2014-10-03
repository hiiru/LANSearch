using Common.Logging;
using Hangfire;
using Nancy;

namespace LANSearch.zTesting
{
    public class TestModule : NancyModule
    {
        protected AppContext Ctx { get { return AppContext.GetContext(); } }

        public TestModule()
        {
            Get["/z/notification"] = x =>
            {
                BackgroundJob.Enqueue(() => Ctx.JobManager.NotificationJob.NotifyAll());

                return "OK";
            };
            Get["/z/test"] = x =>
            {
                return View["zTesting/test.cshtml"];
            };
            Get["/z/log/{level}/{text}"] = x =>
            {
                var logger = LogManager.GetCurrentClassLogger();
                string level = x.level ?? "";
                switch (level.ToLower())
                {
                    case "trace":
                        logger.Trace(x.text);
                        break;

                    case "debug":
                        logger.Debug(x.text);
                        break;

                    case "warn":
                        logger.Warn(x.text);
                        break;

                    case "error":
                        logger.Error(x.text);
                        break;

                    case "fatal":
                        logger.Fatal(x.text);
                        break;

                    case "info":
                    default:
                        logger.Info(x.text);
                        break;
                }
                return "ok";
            };
        }
    }
}