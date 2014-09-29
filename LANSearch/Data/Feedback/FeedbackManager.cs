using LANSearch.Data.Redis;
using System.Collections.Generic;
using System.Linq;

namespace LANSearch.Data.Feedback
{
    public class FeedbackManager
    {
        protected RedisManager RedisManager;

        public FeedbackManager(RedisManager redisManager)
        {
            RedisManager = redisManager;
        }

        public void Save(Feedback obj)
        {
            RedisManager.FeedbackSave(obj);
        }

        public List<Feedback> GetPaged(int page, int pagesize, out int count, bool onlyNew = false, bool showDeleted = false)
        {
            var offset = page * pagesize;
            var objs = RedisManager.FeedbackGetAll();
            count = objs.Count;
            var filtered = objs.OrderByDescending(x => x.Created).AsEnumerable();
            if (onlyNew)
                filtered = filtered.Where(x => x.Read == false);
            if (!showDeleted)
                filtered = filtered.Where(x => x.Deleted == false);
            return filtered.Skip(offset).Take(pagesize).ToList();
        }

        public Feedback Get(int id)
        {
            return RedisManager.FeedbackGet(id);
        }

        public void SetDeleted(int id)
        {
            var obj = Get(id);
            obj.Deleted = true;
            Save(obj);
        }
    }
}