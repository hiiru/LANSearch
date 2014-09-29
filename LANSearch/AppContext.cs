using System;
using Common.Logging;
using LANSearch.Data.Feedback;
using LANSearch.Data.Jobs;
using LANSearch.Data.Redis;
using LANSearch.Data.Search.Solr;
using LANSearch.Data.Server;
using LANSearch.Data.User;
using Mizore.DataMappingHandler.Reflection;
using Mizore.SolrServerHandler;

namespace LANSearch
{
    public class AppContext
    {
        protected static AppContext _Instance;

        public static AppContext GetContext()
        {
            return _Instance ?? (_Instance = new AppContext());
        }

        protected ILog Logger;
        protected AppContext()
        {
            Logger = LogManager.GetCurrentClassLogger();
            Logger.Debug("Initializing AppContext");
            RedisManager = new RedisManager();
            Config = new AppConfig(RedisManager);
            UserManager = new UserManager(RedisManager);
            FeedbackManager = new FeedbackManager(RedisManager);
            ServerManager = new ServerManager(RedisManager);
            JobManager = new JobManager(RedisManager);

            SolrServer = new HttpSolrServer(Config.SearchServerUrl);
            SolrMapper = new ReflectionDataMapper<File>();

            Logger.Info("AppContext is initialized.");
        }

        public RedisManager RedisManager { get; protected set; }

        public AppConfig Config { get; protected set; }

        public ISolrServerHandler SolrServer { get; protected set; }

        public ReflectionDataMapper<File> SolrMapper { get; protected set; }

        public FeedbackManager FeedbackManager { get; protected set; }

        public UserManager UserManager { get; protected set; }

        public ServerManager ServerManager { get; protected set; }

        public JobManager JobManager { get; protected set; }
    }
}