using LANSearch.Data.Redis;
using LANSearch.Data.Search.Solr;
using LANSearch.Data.User;
using LANSearch.Models.Notification;
using Nancy;
using ServiceStack.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LANSearch.Data.Notification
{
    public class NotificationManager
    {
        protected AppContext Ctx { get { return AppContext.GetContext(); } }

        protected RedisManager RedisManager;

        public NotificationManager(RedisManager redisManager)
        {
            RedisManager = redisManager;
        }

        public bool CreateNotification(User.User user, string name, Request request)
        {
            if (user == null || user.Disabled || !user.ClaimHas(UserRoles.MEMBER) || string.IsNullOrWhiteSpace(name) || request == null)
                return false;
            if (Ctx.Config.NotificationFixedExpiration && Ctx.Config.NotificationFixedExpirationDate < DateTime.Now)
                return false;

            var solrQuery = new SolrQueryBuilder(request, Ctx.SearchManager.GetFilters(), true);
            var notification = new Notification
            {
                OwnerId = user.Id,
                Name = name,
                Created = DateTime.Now,
                Expiration =
                    Ctx.Config.NotificationFixedExpiration
                        ? Ctx.Config.NotificationFixedExpirationDate
                        : DateTime.Now.AddDays(Ctx.Config.NotificationLifetimeDays),
                ServerId = solrQuery.ServerId,
                SolrQuery = solrQuery.GetRawQuery()
            };
            Save(notification);
            return true;
        }

        public void Save(Notification notification)
        {
            RedisManager.NotificationSave(notification);
        }

        public Notification Get(int id)
        {
            return RedisManager.NotificationGet(id);
        }

        public IList<Notification> GetAll()
        {
            return RedisManager.NotificationGetAll();
        }

        public List<Notification> GetForUser(int id)
        {
            var all = GetAll();
            return all.Where(x => x.OwnerId == id && !x.Deleted).ToList();
        }

        public List<Notification> GetForServer(int id)
        {
            var all = GetAll();
            return all.Where(x => x.ServerId == id && !x.Deleted).ToList();
        }

        protected int GetActiveNotificationCount(int userId)
        {
            if (userId <= 0) return 0;
            var notifications = GetForUser(userId);
            if (notifications == null) return 0;
            return notifications.Count(x => !x.Disabled && !x.Deleted);
        }

        public NotificationListModel GetListModel(Request request, User.User user)
        {
            if (user == null) return null;
            var notifications = GetForUser(user.Id);
            return new NotificationListModel { Notifications = notifications, ActiveLimitReached = notifications.Count(x => !x.Disabled && !x.Deleted) >= Ctx.Config.NotificationPerUser };
        }

        public NotificationDetailModel GetNotificationFromQuery(Request request, User.User user)
        {
            if (request == null || user == null || user.Disabled) return null;

            var count = GetActiveNotificationCount(user.Id);
            if (count >= Ctx.Config.NotificationPerUser)
            {
                return new NotificationDetailModel {ActiveLimitReached = true};
            }
            var url = request.Url.Clone();
            url.Path = "/Search";
            var solrQueryBuilder = new SolrQueryBuilder(request, Ctx.SearchManager.GetFilters(), true);

            var model = new NotificationDetailModel
            {
                OwnerName = user.UserName,
                OwnerAdminUrl = user.GetAdminUrl(),
                IsCreation = true,
                Keyword = solrQueryBuilder.Keyword,
                Notification = new Notification
                {
                    Created = DateTime.Now,
                    LastExecution = DateTime.Now,
                    Expiration =
                        Ctx.Config.NotificationFixedExpiration
                            ? Ctx.Config.NotificationFixedExpirationDate
                            : DateTime.Now.AddDays(Ctx.Config.NotificationLifetimeDays),
                    OwnerId = user.Id,
                    ServerId = solrQueryBuilder.ServerId,
                    SearchUrl = url.ToString(),
                    SolrQuery = solrQueryBuilder.GetRawQuery()
                }
            };
            if (request.Method == "POST")
            {
                model.Notification.Name = request.Form.notName;
                if (request.Form.notTypeMail)
                {
                    model.Notification.Type = model.Notification.Type.Add(NotificationType.Mail);
                }
                if (request.Form.notTypeHtml)
                {
                    model.Notification.Type = model.Notification.Type.Add(NotificationType.Html5);
                }
            }
            return model;
        }

        public NotificationDetailModel GetNotificationDetail(int notificationId, Request request = null, User.User user = null)
        {
            if (notificationId <= 0) return null;
            var model = new NotificationDetailModel();
            model.Notification = Get(notificationId);
            model.IsCreation = false;
            var count = GetActiveNotificationCount(model.Notification.OwnerId);
            model.ActiveLimitReached = count >= Ctx.Config.NotificationPerUser;
            if (user != null)
            {
                model.OwnerName = user.UserName;
                model.OwnerAdminUrl = user.GetAdminUrl();
            }
            if (request != null && request.Method == "POST")
            {
                model.Notification.Name = request.Form.notName;
                //reset all flags before setting selected flags
                model.Notification.Type = NotificationType.Invalid;
                if (request.Form.notTypeMail)
                {
                    model.Notification.Type = model.Notification.Type.Add(NotificationType.Mail);
                }
                if (request.Form.notTypeHtml)
                {
                    model.Notification.Type = model.Notification.Type.Add(NotificationType.Html5);
                }
            }
            return model;
        }

        public void SetDisabled(Notification notification, bool disable, bool isAdmin=false)
        {
            if (!isAdmin && !disable)
            {
                var count = GetActiveNotificationCount(notification.OwnerId);
                if (count >= Ctx.Config.NotificationPerUser)
                    return;
            }
            notification.Disabled = disable;
            Save(notification);
        }

        public void SetDeleted(Notification notification, bool status)
        {
            notification.Deleted = status;
            Save(notification);
        }
    }
}