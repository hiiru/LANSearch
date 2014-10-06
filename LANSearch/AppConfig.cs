using Common.Logging;
using LANSearch.Data;
using LANSearch.Data.Redis;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace LANSearch
{
    public class AppConfig
    {
        #region Constructor and similar methods

        protected RedisManager RedisManager;
        private readonly List<PropertyInfo> _propertyInfos;
        protected ILog Logger;

        public AppConfig(RedisManager redisManager)
        {
            Logger = LogManager.GetCurrentClassLogger();
            _propertyInfos = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty).ToList();

            InitDefaults();
            RedisManager = redisManager;
            LoadRedisConfig();

            EnsureMinimalConfig();
        }

        ~AppConfig()
        {
            SaveConfigToRedis();
        }

        private void EnsureMinimalConfig()
        {
            bool changed = false;

            if (AppSecurityAesPass == null || AppSecurityAesSalt == null || AppSecurityHmacPass == null || AppSecurityHmacSalt == null)
            {
                //Generate Authentication passwords
                var rng = new RNGCryptoServiceProvider();
                var pwBytes = new byte[20];
                rng.GetNonZeroBytes(pwBytes);
                AppSecurityAesPass = Convert.ToBase64String(pwBytes);
                rng.GetNonZeroBytes(pwBytes);
                AppSecurityHmacPass = Convert.ToBase64String(pwBytes);
                AppSecurityAesSalt = new byte[8];
                rng.GetNonZeroBytes(AppSecurityAesSalt);
                AppSecurityHmacSalt = new byte[8];
                rng.GetNonZeroBytes(AppSecurityHmacSalt);
                changed = true;
            }

            if (changed)
                SaveConfigToRedis();
        }

        private void InitDefaults()
        {
            SearchServerUrl = "http://localhost:18983/solr";
            SearchDisabled = false;
            SearchAllowHideServer = true;
            CrawlerOfflineLimit = 5;
            JobHourlyCrawling = true;

            MailPort = 587;
            MailSsl = true;
            MailFromName = "LANSearch";
            MailCopyToSelf = true;
            UserRequireMailActivation = true;
            NotificationEnabled = true;
            NotificationFixedExpiration = true;
            NotificationFixedExpirationDate = DateTime.ParseExact("20.10.2014", "dd.MM.yyyy", CultureInfo.InvariantCulture);
            NotificationLifetimeDays = 7;
            NotificationPerUser = 5;

            ServerAllowedIps = new List<IpNet>
            {
                new IpNet("10.0.0.0/8"),
                new IpNet("172.16.0.0/12"),
                new IpNet("192.168.0.0/16"),
            };
            ServerLimitPerUser = 10;
            ServerRestrictIpToOwner = true;
            SearchBoostOnlineServers = true;
        }

        #endregion Constructor and similar methods

        #region Redis Storage

        private void LoadRedisConfig()
        {
            Dictionary<string, object> config;
            try
            {
                config = RedisManager.ConfigGet();
            }
            catch
            {
                return;
            }
            if (config == null)
            {
                //Save default configuration
                SaveConfigToRedis();
                return;
            }
            SetConfigDictionary(config);
        }

        public void SaveConfigToRedis()
        {
            RedisManager.ConfigStore(GetConfigDictionary());
        }

        #endregion Redis Storage

        #region De-/Serailazation

        public Dictionary<string, object> GetConfigDictionary()
        {
            var dict = new Dictionary<string, object>();
            foreach (var pi in _propertyInfos)
            {
                var value = pi.GetValue(this);
                try
                {
                    if (pi.PropertyType == typeof(List<string>))
                    {
                        var list = value as List<string>;
                        dict[pi.Name] = list == null ? "" : string.Join(",", list);
                    }
                    if (pi.PropertyType == typeof(List<IpNet>))
                    {
                        var list = value as List<IpNet>;
                        dict[pi.Name] = list == null ? "" : list.Select(x => x.ToString()).Join(",");
                    }
                    else if (pi.PropertyType == typeof(byte[]))
                    {
                        dict[pi.Name] = value == null ? null : Convert.ToBase64String((byte[])value);
                    }
                    else if (pi.PropertyType == typeof(DateTime))
                    {
                        //store and display the date in a readable way, also otherwise standard .NET classes can't easily deserialize it
                        dict[pi.Name] = ((DateTime)value).ToString("u");
                    }
                    else
                        dict[pi.Name] = value;
                }
                catch (Exception e)
                {
                    Logger.FatalFormat("Invalid Type Serialization for {0} (Type: {1}, Value: {2})", e, pi.Name, pi.PropertyType.Name, value);
                    throw e;
                }
            }
            return dict;
        }

        public void SetConfigDictionary(Dictionary<string, object> config)
        {
            if (config == null || config.Count == 0) return;
            foreach (var kvp in config)
            {
                var pi = _propertyInfos.FirstOrDefault(x => x.Name == kvp.Key);
                if (pi == null)
                    continue;
                try
                {
                    if (pi.PropertyType == typeof(List<string>))
                    {
                        var valueCsv = kvp.Value as string;

                        pi.SetValue(this,
                            valueCsv == null
                                ? new List<string>()
                                : valueCsv.Split(new[] { ',' }).Select(x => x.Trim()).ToList());
                    }
                    if (pi.PropertyType == typeof(List<IpNet>))
                    {
                        var valueCsv = kvp.Value as string;
                        if (string.IsNullOrWhiteSpace(valueCsv))
                            pi.SetValue(this, new List<IpNet>());
                        var splitedCidr = valueCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (splitedCidr.Length == 0)
                            pi.SetValue(this, new List<IpNet>());
                        pi.SetValue(this, splitedCidr.Select(cidr => new IpNet(cidr)).ToList());
                    }
                    else if (pi.PropertyType == typeof(byte[]))
                    {
                        pi.SetValue(this, Convert.FromBase64String((string)kvp.Value));
                    }
                    else
                        pi.SetValue(this,
                            kvp.Value == null
                                ? pi.PropertyType.GetDefaultValue()
                                : Convert.ChangeType(kvp.Value, pi.PropertyType));
                }
                catch (Exception e)
                {
                    Logger.FatalFormat("Invalid Type Deserialization for {0} (Type: {1}, Value: {2})", e, kvp.Key, pi.PropertyType.Name, kvp.Value);
                    throw e;
                }
            }
        }

        #endregion De-/Serailazation

        #region Setup Variables (Blacklisted from configuration page)

        public static List<string> ConfigBlacklist = new List<string>
        {
            "AppSetupDone",
            "AppSecurityAesPass",
            "AppSecurityAesSalt",
            "AppSecurityHmacPass",
            "AppSecurityHmacSalt",
        };

        public bool AppSetupDone { get; set; }

        public string AppSecurityAesPass { get; set; }

        public byte[] AppSecurityAesSalt { get; set; }

        public string AppSecurityHmacPass { get; set; }

        public byte[] AppSecurityHmacSalt { get; set; }

        #endregion Setup Variables (Blacklisted from configuration page)

        public bool AppMaintenance { get; set; }

        public string AppMaintenanceMessage { get; set; }

        public List<IpNet> AppBlockedIps { get; set; }

        public bool AppAnnouncement { get; set; }

        public string AppAnnouncementMessage { get; set; }

        public string NancyDiagnosticsPassword { get; set; }

        public bool UserRequireMailActivation { get; set; }

        #region Search

        public string SearchServerUrl { get; set; }

        public bool SearchDisabled { get; set; }

        public bool SearchAllowHideServer { get; set; }

        public bool SearchBoostOnlineServers { get; set; }

        #endregion Search

        #region Jobs

        public bool JobHourlyCrawling { get; set; }

        /// <summary>
        /// Crawler will set server offline after these tries.
        /// </summary>
        public int CrawlerOfflineLimit { get; set; }

        #endregion Jobs

        #region MailManager

        public string MailServer { get; set; }

        public int MailPort { get; set; }

        public bool MailSsl { get; set; }

        public string MailAccount { get; set; }

        public string MailPassword { get; set; }

        public string MailFromAddress { get; set; }

        public string MailFromName { get; set; }

        public bool MailCopyToSelf { get; set; }

        #endregion MailManager

        #region Notification

        public bool NotificationEnabled { get; set; }

        public bool NotificationFixedExpiration { get; set; }

        public DateTime NotificationFixedExpirationDate { get; set; }

        public int NotificationLifetimeDays { get; set; }

        public int NotificationPerUser { get; set; }

        #endregion Notification

        #region Server

        public List<IpNet> ServerAllowedIps { get; set; }

        public int ServerLimitPerUser { get; set; }

        public bool ServerRestrictIpToOwner { get; set; }

        #endregion Server
    }
}