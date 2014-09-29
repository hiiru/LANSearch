using System;
using System.Threading;
using System.Threading.Tasks;
using LANSearch.Data.Redis;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Redis;

namespace LANSearch.Data.Server
{
    public class ServerManager
    {
        protected RedisManager RedisManager;
        protected Dictionary<int, Server> Cache;
        protected List<int> CacheHidden; 

        public ServerManager(RedisManager redisManager)
        {
            RedisManager = redisManager;

            Cache = RedisManager.ServerGetAll().ToDictionary(x => x.Id, x => x);
            CacheHidden = Cache.Values.Where(x => x.Hidden).Select(x => x.Id).ToList();

            redisManager.OnMessage += redisManager_OnMessage;
        }
        
        void redisManager_OnMessage(string channel, string message)
        {
            if (channel != "server") return;
            int id;
            if (int.TryParse(message, out id))
            {
                var server = RedisManager.ServerGet(id);
                if (server == null)
                {
                    Cache.Remove(id);
                }
                else
                {
                    Cache[id] = server;
                    var isHidden = CacheHidden.Contains(id);

                    if (server.Hidden && !isHidden)
                        CacheHidden.Add(id);
                    else if (!server.Hidden && isHidden)
                        CacheHidden.RemoveAll(x => x == id);

                }
            }

        }


        public void Save(Server obj)
        {
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

        public List<Server> GetPaged(int page, int pagesize, out int count)
        {
            var servers = GetAll();
            count = servers.Count;
            var offset = page * pagesize;
            var filtered = servers.OrderBy(x => x.Id);
            return filtered.Skip(offset).Take(pagesize).ToList();
        }

        public List<int> GetHiddenIds()
        {
            return CacheHidden;
            //return RedisManager.ServerGetHidden();
        }
    }
}