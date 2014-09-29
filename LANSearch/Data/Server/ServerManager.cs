using LANSearch.Data.Redis;
using System.Collections.Generic;
using System.Linq;

namespace LANSearch.Data.Server
{
    public class ServerManager
    {
        protected RedisManager RedisManager;

        public ServerManager(RedisManager redisManager)
        {
            RedisManager = redisManager;
        }

        public void Save(Server obj)
        {
            RedisManager.ServerSave(obj);
        }

        public IList<Server> GetAll()
        {
            return RedisManager.ServerGetAll();
        }

        public Server Get(int id)
        {
            return RedisManager.ServerGet(id);
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
            return RedisManager.ServerGetHidden();
        }
    }
}