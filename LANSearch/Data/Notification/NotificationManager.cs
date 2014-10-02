using LANSearch.Data.Redis;
using LANSearch.Data.Search.Solr;
using LANSearch.Data.User;
using Nancy;
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

            var solrQuery = new SolrQueryBuilder(request, Ctx.SearchManager.Filters, true);
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
                Query = solrQuery.GetRawQuery()
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
    }
}