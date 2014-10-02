using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using Hangfire;
using LANSearch.Data.Notification;
using LANSearch.Data.Search.Solr;

namespace LANSearch.Data.Jobs
{
    public class NotificationJob
    {
        protected AppContext Ctx { get { return AppContext.GetContext(); } }

        public void Notify(Notification.Notification notification)
        {
            if (!Ctx.Config.NotificationEnabled || notification == null || notification.Disabled || notification.Deleted)
                return;
            if (DateTime.Now-notification.LastExecution < TimeSpan.FromMinutes(1))
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
                logger.InfoFormat("Notify: Notification {0} is expired (expiration:{1})",notification.Id,notification.Expiration);
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

            if (notification.Type.HasFlag(NotificationType.Mail))
            {
                BackgroundJob.Enqueue(() => Ctx.MailManager.SendNotification(notification, user, results.Results.Take(5)));
            }
            if (notification.Type.HasFlag(NotificationType.Html5))
            {
                //TODO
            }

        }

        public void NotifyServer(int id)
        {
            var logger = LogManager.GetCurrentClassLogger();
            if (id <= 0)
            {
                logger.Info("NotifyServer was called with invalid ID: "+id);
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
