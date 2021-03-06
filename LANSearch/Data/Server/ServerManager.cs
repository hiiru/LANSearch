﻿using LANSearch.Data.Jobs.Ftp;
using LANSearch.Data.Redis;
using LANSearch.Data.User;
using LANSearch.Models.Server;
using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LANSearch.Data.Server
{
    public class ServerManager
    {
        protected AppContext Ctx { get { return AppContext.GetContext(); } }

        protected RedisManager RedisManager;
        protected Dictionary<int, Server> Cache;
        protected HashSet<int> CacheHidden;
        protected HashSet<int> CacheOnlineAndOpen;

        public ServerManager(RedisManager redisManager)
        {
            RedisManager = redisManager;

            Cache = RedisManager.ServerGetAll().ToDictionary(x => x.Id, x => x);
            CacheHidden = new HashSet<int>(Cache.Values.Where(x => x.Hidden && !x.Deleted).Select(x => x.Id));
            CacheOnlineAndOpen = new HashSet<int>(Cache.Values.Where(x => x.Online && !x.Closed && !x.Deleted).Select(x => x.Id));

            redisManager.OnMessage += redisManager_OnMessage;
        }

        private void redisManager_OnMessage(string channel, string message)
        {
            if (channel != "server") return;
            int id;
            if (int.TryParse(message, out id))
            {
                var server = RedisManager.ServerGet(id);
                if (server == null)
                {
                    Cache.Remove(id);
                    CacheOnlineAndOpen.Remove(id);
                }
                else
                {
                    Cache[id] = server;
                    var isHidden = CacheHidden.Contains(id);
                    if (server.Hidden && !isHidden)
                        CacheHidden.Add(id);
                    else if (!server.Hidden && isHidden)
                        CacheHidden.Remove(id);

                    var isOnline = CacheOnlineAndOpen.Contains(id);
                    var onlineAndOpen = server.Online && !server.Closed;
                    if (onlineAndOpen && !isOnline)
                        CacheOnlineAndOpen.Add(id);
                    else if (!onlineAndOpen && isOnline)
                        CacheOnlineAndOpen.Remove(id);
                }
            }
        }

        public void Save(Server obj)
        {
            if (obj == null) return;
            if (obj.Id == 0)
            {
                var status = Ctx.JobManager.FtpCrawler.CheckServer(obj);
                if (!status.IsOk)
                {
                    obj.ScanFailedLastDate = DateTime.Now;
                    obj.ScanFailedAttempts = 1;
                    obj.Online = false;
                    obj.ScanFailedMessage = status.ErrorType == FtpStatus.FtpErrorType.Offline ?
                        string.Format("Connection couldn't be established, server is offline.") :
                        string.Format("{0} {1}", status.ErrorFtpCode, status.ErrorFtpMessage);
                }
                else
                {
                    obj.Online = true;
                }
            }
            RedisManager.ServerSave(obj);
        }

        public IList<Server> GetAll()
        {
            return Cache.Select(x => x.Value).ToList();
            //return RedisManager.ServerGetAll();
        }

        public Server Get(int id)
        {
            if (Cache.ContainsKey(id))
                return Cache[id];
            return null;
            //var server= RedisManager.ServerGet(id);
            //return server;
        }

        public List<Server> GetPaged(int page, int pagesize, out int count, int ownerId)
        {
            var servers = GetAll();
            count = servers.Count;
            var offset = page * pagesize;
            var filtered = servers.AsEnumerable();
            if (ownerId != -1)
                filtered = filtered.Where(x => x.OwnerId == ownerId && !x.Deleted);
            return filtered.OrderBy(x => x.Id).Skip(offset).Take(pagesize).ToList();
        }

        public IEnumerable<int> GetHiddenIds()
        {
            return CacheHidden;
            //return RedisManager.ServerGetHidden();
        }

        public IEnumerable<int> GetOnlineIds()
        {
            return CacheOnlineAndOpen;
        }

        public ServerDetailModel GetModelDetail(int serverId, int userId, dynamic form = null, bool isAdmin = false)
        {
            Server server;
            if (serverId > 0)
            {
                server = Get(serverId);
                if (server == null || (server.Deleted && !isAdmin))
                    return null;
            }
            else
            {
                if (!isAdmin)
                {
                    var serverCount = GetAll().Count(x => !x.Deleted && x.OwnerId == userId);
                    if (serverCount >= Ctx.Config.ServerLimitPerUser)
                    {
                        return new ServerDetailModel { IsAdmin = false, LimitReached = true };
                    }
                }

                server = new Server
                {
                    Id = 0,
                    Type = 1,
                    Created = DateTime.Now,
                };
            }

            var model = new ServerDetailModel
            {
                Server = server,
                IsAdmin = isAdmin
            };

            if (form != null)
            {
                if (isAdmin)
                    server.OwnerId = form.srvOwner;

                server.Name = form.srvName;
                server.Description = form.srvDescription;

                server.Address = form.srvAddress;
                int port;
                int.TryParse(form.srvPort, out port);
                server.Port = port;

                if (form.srvAuth)
                {
                    server.Login = form.srvLogin;
                    server.Password = form.srvPass;
                }
                else
                {
                    server.Login = null;
                    server.Password = null;
                }
                server.Closed = form.srvClosed;
                server.Hidden = form.srvHidden;
                server.NoScans = form.srvNoScan;
            }

            if (isAdmin)
            {
                if (server.OwnerId > 0)
                {
                    var user = Ctx.UserManager.Get(server.OwnerId);
                    model.OwnerName = user.UserName;
                    model.OwnerAdminUrl = user.GetAdminUrl();
                }
                else
                {
                    model.OwnerName = "<No Owner>";
                }
            }
            else
            {
                var conflictingServers = GetAll().Count(x => !x.Deleted && x.OwnerId != userId && x.Address == server.Address);
                if (conflictingServers > 0)
                {
                    model.ServerConflictDetected = true;
                }
            }

            return model;
        }

        public ServerListModel GetServerListModel(Request request, int userId)
        {
            var userlist = new ServerListModel(new UrlBuilder(request.Url));
            foreach (var qs in request.Query)
            {
                var qsKey = qs as string;
                if (qsKey == null) continue;
                switch (qsKey)
                {
                    case "p":
                        int page;
                        if (int.TryParse(request.Query["p"], out page))
                            userlist.Page = page;
                        break;

                    case "ps":
                        int pagesize;
                        if (int.TryParse(request.Query["ps"], out pagesize))
                        {
                            userlist.PageSize = pagesize;
                        }
                        break;
                }
            }

            if (userlist.Page < 0)
                userlist.Page = 0;
            if (userlist.PageSize < 20)
                userlist.PageSize = 20;

            int count;
            userlist.Servers = Ctx.ServerManager.GetPaged(userlist.Page, userlist.PageSize, out count, userId);
            userlist.Count = count;

            return userlist;
        }

        public void SetDeleted(Server server, bool deleted = true)
        {
            server.Deleted = deleted;
            server.Hidden = deleted;
            Save(server);
        }
    }
}