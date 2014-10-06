using Common.Logging;
using Hangfire;
using LANSearch.Data.Notification;
using LANSearch.Hubs;
using System;
using System.Linq;

namespace LANSearch.Data.Jobs
{
    public class NotificationJob
    {
        protected AppContext Ctx { get { return AppContext.GetContext(); } }

        public void Notify(Notification.Notification notification)
        {
            if (!Ctx.Config.NotificationEnabled || notification == null || notification.Disabled || notification.Deleted)
                return;
            if (DateTime.Now - notification.LastExecution < TimeSpan.FromMinutes(1))
            {
                //Prevent searching too often
                return;
            }
            var logger = LogManager.GetCurrentClassLogger();
            if (string.IsNullOrWhiteSpace(notification.SolrQuery))
            {
                notification.Disabled = true;
                Ctx.NotificationManager.Save(notification);
                logger.InfoFormat("Notify: Notification {0} is disabled because query is empty.", notification.Id);
                return;
            }
            if (notification.Expiration < DateTime.Now)
            {
                notification.Disabled = true;
                Ctx.NotificationManager.Save(notification);
                logger.InfoFormat("Notify: Notification {0} is expired (expiration:{1})", notification.Id, notification.Expiration);
                return;
            }
            var user = Ctx.UserManager.Get(notification.OwnerId);
            if (user == null || user.Disabled)
            {
                notification.Disabled = true;
                Ctx.NotificationManager.Save(notification);
                logger.InfoFormat("Notify: Notification {0} is disabled because owner is disabled or invalid.", notification.Id);
                return;
            }

            var results = Ctx.SearchManager.SearchByQuery(notification.SolrQuery, notification.LastExecution);
            notification.LastExecution = DateTime.Now;
            Ctx.NotificationManager.Save(notification);
            if (!results.HasResults)
                return;
            var notEvent = new NotificationEvent
            {
                NotificationId = notification.Id,
                Name = notification.Name,
                SearchUrl = notification.SearchUrl,
                NotificationTime = DateTime.Now,
                UserName = user.UserName,
                UserEmail = user.Email,
                Items = results.Results.Select(result => new NotificationEventItem { FileName = result.Name, FileSize = result.Size, FileUrl = result.Url, ServerName = result.ServerName}).ToList()
            };

            if (notification.Type.HasFlag(NotificationType.Mail))
            {
                Ctx.JobManager.EnqueueJob(() => Ctx.MailManager.SendNotification(notEvent));
            }
            if (notification.Type.HasFlag(NotificationType.Html5))
            {
                Ctx.JobManager.EnqueueJob(() => NotificationHub.SearchNotification(notEvent));
            }
        }

        public void NotifyServer(int id)
        {
            var logger = LogManager.GetCurrentClassLogger();
            if (id <= 0)
            {
                logger.Info("NotifyServer was called with invalid ID: " + id);
                return;
            }
            var notifications = Ctx.NotificationManager.GetForServer(id);
            foreach (var notification in notifications.Where(x => !x.Disabled))
            {
                Notify(notification);
            }
        }

        public void NotifyAll()
        {
            var notifications = Ctx.NotificationManager.GetAll().Where(x => !x.Disabled && !x.Deleted);
            foreach (var notification in notifications)
            {
                Notify(notification);
            }
        }
    }
}