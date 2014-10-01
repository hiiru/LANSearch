using LANSearch.Data.Redis;
using ServiceStack.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace LANSearch
{
    public class AppConfig
    {
        protected RedisManager RedisManager;

        private readonly List<PropertyInfo> _propertyInfos;

        public AppConfig(RedisManager redisManager)
        {
            _propertyInfos = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty).ToList();

            InitDefaults();
            RedisManager = redisManager;
            LoadRedisConfig();

            EnsureMinimalConfig();
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

        ~AppConfig()
        {
            SaveConfigToRedis();
        }

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

        public Dictionary<string, object> GetConfigDictionary()
        {
            var dict = new Dictionary<string, object>();
            foreach (var pi in _propertyInfos)
            {
                var value = pi.GetValue(this);
                if (pi.PropertyType == typeof(List<string>))
                {
                    var list = value as List<string>;
                    dict[pi.Name] = list == null ? "" : string.Join(",", list);
                }
                else if (pi.PropertyType == typeof(byte[]))
                {
                    dict[pi.Name] = value == null ? null : Convert.ToBase64String((byte[])value);
                }
                else
                    dict[pi.Name] = value;
            }
            return dict;
        }

        public void SetConfigDictionary(Dictionary<string, object> config)
        {
            if (config == null || config.Count == 0) return;
            foreach (var kvp in config)
            {
                var prop = _propertyInfos.FirstOrDefault(x => x.Name == kvp.Key);
                if (prop == null)
                    continue;
                if (prop.PropertyType == typeof(List<string>))
                {
                    var valueCsv = kvp.Value as string;

                    prop.SetValue(this, valueCsv == null ? new List<string>() : valueCsv.Split(new[] { ',' }).Select(x => x.Trim()).ToList());
                }
                else if (prop.PropertyType == typeof(byte[]))
                {
                    prop.SetValue(this, Convert.FromBase64String((string)kvp.Value));
                }
                else
                    prop.SetValue(this, kvp.Value == null ? prop.PropertyType.GetDefaultValue() : Convert.ChangeType(kvp.Value, prop.PropertyType));
            }
        }

        private void InitDefaults()
        {
            SearchServerUrl = "http://localhost:18983/solr";
            SearchDisabled = false;
            SearchAllowHideServer = true;
            CrawlerOfflineLimit = 5;
            JobHourlyCrawling = true;
        }

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

        public List<string> AppBlockedIps { get; set; }

        public bool AppAnnouncement { get; set; }

        public string AppAnnouncementMessage { get; set; }

        public string SearchServerUrl { get; set; }
        public bool SearchDisabled { get; set; }

        public bool SearchAllowHideServer { get; set; }

        /// <summary>
        /// Crawler will set server offline after these tries.
        /// </summary>
        public int CrawlerOfflineLimit { get; set; }

        public string NancyDiagnosticsPassword { get; set; }
        public bool JobHourlyCrawling { get; set; }
    }
}