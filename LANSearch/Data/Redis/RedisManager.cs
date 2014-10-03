using Common.Logging;
using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LANSearch.Data.Redis
{
    public class RedisManager
    {
        protected ILog Logger;

        public RedisManager()
        {
            Logger = LogManager.GetCurrentClassLogger();
            Logger.Debug("Initializing RedisManager");
            Pool = new PooledRedisClientManager(InitConfig.RedisDbApp, string.Format("{0}:{1}", InitConfig.RedisServer, InitConfig.RedisPort));

            //var server = new List<string> {string.Format("{0}:{1}", InitConfig.RedisServer, InitConfig.RedisPort)};
            //Pool = new PooledRedisClientManager(server, server, new RedisClientManagerConfig{AutoStart=true, DefaultDb = InitConfig.RedisDbApp, MaxWritePoolSize = 100, MaxReadPoolSize = 100});
            Logger.Debug("Pool Created.");

            ThreadPool.QueueUserWorkItem(x =>
            {
                using (var client = new RedisClient(string.Format("{0}:{1}", InitConfig.RedisServer, InitConfig.RedisPort)))
                {
                    var subscription = client.CreateSubscription();
                    subscription.OnMessage += OnSubscriptionMessage;
                    subscription.SubscribeToChannels("server", "user", "feedback");
                }
            });
            Logger.Debug("Subscribe Thread started, RedisManager is initialized.");
        }

        public delegate void RedisChangeMessage(string channel, string message);

        public event RedisChangeMessage OnMessage;

        private void OnSubscriptionMessage(string channel, string message)
        {
            if (OnMessage != null)
                OnMessage(channel, message);
            Logger.TraceFormat("OnSubscriptionMessage, channel:{0} message:{1}", channel, message);
        }

        public PooledRedisClientManager Pool { get; protected set; }

        protected int GetId(string key)
        {
            using (var client = Pool.GetClient())
            {
                return (int)client.Increment(key, 1);
            }
        }

        public IRedisSubscription GetSubscription()
        {
            using (var client = Pool.GetClient())
            {
                return client.CreateSubscription();
            }
        }

        #region Feedback

        protected const string RedisFeedbackKeyId = "urn:feedback:id";

        public void FeedbackSave(Feedback.Feedback obj)
        {
            if (obj == null) return;
            if (obj.Id == 0)
            {
                obj.Id = GetId(RedisFeedbackKeyId);
            }
            using (var client = Pool.GetClient())
            {
                var feedbackClient = client.As<Feedback.Feedback>();
                using (var transaction = feedbackClient.CreateTransaction())
                {
                    transaction.QueueCommand(x => x.DeleteById(obj.Id));
                    transaction.QueueCommand(x => x.Store(obj));
                    transaction.Commit();
                }
            }
        }

        public IList<Feedback.Feedback> FeedbackGetAll()
        {
            using (var client = Pool.GetClient())
            {
                var feedbackClient = client.As<Feedback.Feedback>();
                return feedbackClient.GetAll();
            }
        }

        public Feedback.Feedback FeedbackGet(int id)
        {
            using (var client = Pool.GetClient())
            {
                var feedbackClient = client.As<Feedback.Feedback>();

                return feedbackClient.GetById(id);
            }
        }

        #endregion Feedback

        #region User

        protected const string RedisUserKeyId = "urn:user:id";
        protected const string RedisUserKeyHash = "urn:user:nameHash";
        protected const string RedisUserKeyMail = "urn:user:mailaddresses";
        protected const string RedisUserKeySession = "urn:user:sess_{0}";

        protected const string RedisUserKeyLock = "urn:user:lock_{0}";

        public void UserSave(User.User user)
        {
            if (user == null) return;
            if (user.Id == 0)
            {
                user.Id = GetId(RedisUserKeyId);
            }
            using (var client = Pool.GetClient())
            {
                var userClient = client.As<User.User>();
                var oldUser = userClient.GetById(user.Id);
                using (var transaction = userClient.CreateTransaction())
                {
                    transaction.QueueCommand(x => x.DeleteById(user.Id));
                    transaction.QueueCommand(x => x.Store(user));
                    transaction.Commit();
                }
                if (oldUser != null && oldUser.UserName == user.UserName)
                    return;

                var clientNames = client.Hashes[RedisUserKeyHash];
                if (oldUser != null)
                    clientNames.Remove(oldUser.UserName);
                clientNames.Add(user.UserName, user.Id.ToString());

                bool mailChanged = oldUser == null || oldUser.Email != user.Email;
                if (mailChanged)
                {
                    var mailaddresses = client.Lists[RedisUserKeyMail];
                    if (oldUser != null)
                        mailaddresses.Remove(oldUser.Email);
                    mailaddresses.Add(user.Email);
                }
            }
        }

        public IList<User.User> UserGetAll()
        {
            using (var client = Pool.GetClient())
            {
                var userClient = client.As<User.User>();
                return userClient.GetAll();
            }
        }

        public User.User UserGet(int id)
        {
            using (var client = Pool.GetClient())
            {
                var userClient = client.As<User.User>();
                return userClient.GetById(id);
            }
        }

        public User.User UserGet(string name)
        {
            using (var client = Pool.GetClient())
            {
                var clientNames = client.Hashes[RedisUserKeyHash];
                string strId;
                if (!clientNames.TryGetValue(name, out strId))
                    return null;
                int id;
                if (!int.TryParse(strId, out id))
                    return null;
                var userClient = client.As<User.User>();
                return userClient.GetById(id);
            }
        }

        public bool UserIsNameUsed(string name)
        {
            using (var client = Pool.GetClient())
            {
                var clientNames = client.Hashes[RedisUserKeyHash];
                return clientNames.ContainsKey(name);
            }
        }

        public bool UserIsEmailUsed(string mail)
        {
            using (var client = Pool.GetClient())
            {
                var mailaddresses = client.Lists[RedisUserKeyMail];
                return mailaddresses.Contains(mail);
            }
        }

        public IDisposable UserGetLock(string name)
        {
            using (var client = Pool.GetClient())
            {
                return client.AcquireLock(string.Format(RedisUserKeyLock, name));
            }
        }

        public Guid UserSessionStart(User.User user)
        {
            if (user == null)
                throw new ArgumentException("user");
            var guid = Guid.NewGuid();
            var key = string.Format(RedisUserKeySession, guid.ToString("N"));
            using (var client = Pool.GetClient())
            {
                client.SetEntry(key, user.Id.ToString(), TimeSpan.FromHours(2));
            }
            return guid;
        }

        public User.User UserSessionResolve(Guid guid)
        {
            if (guid == Guid.Empty) return null;
            var key = string.Format(RedisUserKeySession, guid.ToString("N"));
            using (var client = Pool.GetClient())
            {
                var entry = client.Get<string>(key);
                if (!string.IsNullOrWhiteSpace(entry))
                {
                    int id;
                    if (!int.TryParse(entry, out id))
                        return null;

                    client.ExpireEntryIn(key, TimeSpan.FromHours(1));
                    var userClient = client.As<User.User>();
                    return userClient.GetById(id);
                }
            }
            return null;
        }

        public void UserRepairNameHash()
        {
            using (var client = Pool.GetClient())
            {
                var clientNames = client.Hashes[RedisUserKeyHash];
                if (clientNames.Count == 0)
                {
                    var userClient = client.As<User.User>();
                    var users = userClient.GetAll();
                    foreach (var user in users)
                    {
                        clientNames.Add(user.UserName, user.Id.ToString());
                    }
                }
            }
        }

        #endregion User

        #region Server

        protected const string RedisKeyId = "urn:server:id";
        protected const string RedisKeyHidden = "urn:server:Hidden";

        public void ServerSave(Server.Server obj)
        {
            if (obj == null) return;
            if (obj.Id == 0)
            {
                obj.Id = GetId(RedisKeyId);
            }
            using (var client = Pool.GetClient())
            {
                var serverClient = client.As<Server.Server>();
                using (var transaction = serverClient.CreateTransaction())
                {
                    transaction.QueueCommand(x => x.DeleteById(obj.Id));
                    transaction.QueueCommand(x => x.Store(obj));
                    transaction.Commit();
                }
                var intClient = client.As<int>();
                var hiddenList = intClient.Lists[RedisKeyHidden];
                var isHidden = hiddenList.Contains(obj.Id);
                if (obj.Hidden && !isHidden)
                    hiddenList.Add(obj.Id);
                else if (!obj.Hidden && isHidden)
                    hiddenList.Remove(obj.Id);

                client.PublishMessage("server", obj.Id.ToString());
            }
        }

        public IList<Server.Server> ServerGetAll()
        {
            using (var client = Pool.GetClient())
            {
                var serverClient = client.As<Server.Server>();
                return serverClient.GetAll();
            }
        }

        public Server.Server ServerGet(int id)
        {
            using (var client = Pool.GetClient())
            {
                var serverClient = client.As<Server.Server>();
                return serverClient.GetById(id);
            }
        }

        public List<int> ServerGetHidden()
        {
            using (var client = Pool.GetClient())
            {
                var intClient = client.As<int>();
                return intClient.Lists[RedisKeyHidden].ToList();
            }
        }

        #endregion Server

        #region Configuration Management

        protected const string RedisConfigKey = "urn:config:dict";

        public Dictionary<string, object> ConfigGet()
        {
            try
            {
                using (var client = Pool.GetClient())
                    return client.Get<Dictionary<string, object>>(RedisConfigKey);
            }
            catch
            {
                return null;
            }
        }

        public void ConfigStore(Dictionary<string, object> config)
        {
            try
            {
                using (var client = Pool.GetClient())
                    client.Set(RedisConfigKey, config);
            }
            catch
            {
                return;
            }
        }

        #endregion Configuration Management

        #region Ftp Crawler

        protected const string RedisCrawlerInUse = "urn:job_crawler:lock_{0}";

        public bool FtpCrawlerServerIsLocked(int id)
        {
            var key = string.Format(RedisCrawlerInUse, id);
            using (var client = Pool.GetClient())
            {
                if (client.ContainsKey(key))
                    return true;
                client.SetEntry(key, "true", TimeSpan.FromMinutes(10));
            }
            return false;
        }

        public void FtpCrawlerServerUnlock(int id)
        {
            var key = string.Format(RedisCrawlerInUse, id);
            using (var client = Pool.GetClient())
            {
                client.RemoveEntry(key);
            }
        }

        #endregion Ftp Crawler

        #region Notification

        protected const string RedisNotificationKeyId = "urn:feedback:id";

        public void NotificationSave(Notification.Notification notification)
        {
            if (notification == null) return;
            if (notification.Id == 0)
            {
                notification.Id = GetId(RedisNotificationKeyId);
            }
            using (var client = Pool.GetClient())
            {
                var notificationkClient = client.As<Notification.Notification>();
                using (var transaction = notificationkClient.CreateTransaction())
                {
                    transaction.QueueCommand(x => x.DeleteById(notification.Id));
                    transaction.QueueCommand(x => x.Store(notification));
                    transaction.Commit();
                }
            }
        }

        public IList<Notification.Notification> NotificationGetAll()
        {
            using (var client = Pool.GetClient())
            {
                var notificationkClient = client.As<Notification.Notification>();
                return notificationkClient.GetAll();
            }
        }

        public Notification.Notification NotificationGet(int id)
        {
            using (var client = Pool.GetClient())
            {
                var notificationkClient = client.As<Notification.Notification>();
                return notificationkClient.GetById(id);
            }
        }

        #endregion Notification

        public List<string> SearchKeys(string s)
        {
            using (var client = Pool.GetClient())
            {
                return client.SearchKeys(s);
            }
        }
    }
}