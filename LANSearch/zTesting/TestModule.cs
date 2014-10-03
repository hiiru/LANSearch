using System;
using System.Collections.Generic;
using Common.Logging;
using Hangfire;
using LANSearch.Data.Notification;
using LANSearch.Hubs;
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
                //BackgroundJob.Enqueue(() => Ctx.JobManager.NotificationJob.NotifyAll());

                var notEvent = new NotificationEvent
                {
                    Items = new List<NotificationEventItem>
                    {
                        new NotificationEventItem {FileName = "small.txt", FileSize = "2.12 KB", FileUrl = "#",ServerName = "127.0.0.1:21"},
                        new NotificationEventItem {FileName = "noise.mp3", FileSize = "5.27 MB", FileUrl = "#",ServerName = "127.0.0.1:21"},
                        new NotificationEventItem {FileName = "movie.avi", FileSize = "723.62 MB", FileUrl = "#",ServerName = "127.0.0.1:21"}
                    },
                    Name = "Name",
                    NotificationId = 1,
                    NotificationTime = DateTime.Now,
                    SearchUrl = "#",
                    UserName = "track",
                    UserEmail = "user@email.tld",
                };
                NotificationHub.SearchNotification(notEvent);
                //BackgroundJob.Enqueue(() => NotificationHub.SearchNotification(notEvent));

                return "OK";
            };

            Get["/z/announcement"] = x =>
            {
                NotificationHub.SendAnnouncement("Test Announcement","This is a test announcement");
                return "OK";
            };
            Get["/z/message/{user}"] = x =>
            {
                NotificationHub.SendAdministratorMessage(x.user,"Hi from the administrator","danger");
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