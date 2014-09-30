using System;
using System.Globalization;
using System.Linq;
using LANSearch.Data.Search.Solr;
using LANSearch.Data.User;
using Microsoft.AspNet.SignalR;

namespace LANSearch.Hubs
{
    //[CustomAuthorize(Roles = UserRoles.ADMIN)]
    public class LogHub : Hub
    {
        private static IHubContext _signalRHub;

        private class LogEntry
        {
            public string date { get; set; }
            public string logLevel { get; set; }
            public string callsite { get; set; }
            public string message { get; set; }
        }

        public void GetLastEvents()
        {
            var entries = System.IO.File.ReadLines("lansearch.log").Reverse().Take(500).Select(x =>
            {
                var splited = x.Split('|');
                var date = splited[0];
                date = date.Substring(0, date.IndexOf('.'));
                var loglevel = splited[1][0] + splited[1].Substring(1).ToLower();
                return new LogEntry
                {
                    date = date,
                    logLevel = loglevel,
                    callsite = splited[2],
                    message = splited[3]
                };
            }).ToArray();

            Clients.Caller.getLastEvents(entries);
        }
        public void GetLastRequests()
        {
            var entries = System.IO.File.ReadLines("requests.log").Reverse().Take(500).Select(x =>
            {
                var splited = x.Split('|');
                var date = splited[0];
                date = date.Substring(0, date.IndexOf('.'));
                return new LogEntry
                {
                    date = date,
                    message = splited[3]
                };
            }).ToArray();

            Clients.Caller.getLastRequests(entries);
        }

        private const string dateFormat = "MM/dd/yyyy HH:mm:ss";

        public static void LogRequest(string longdate, string message)
        {
            if (_signalRHub == null)
            {
                _signalRHub = GlobalHost.ConnectionManager.GetHubContext<LogHub>();
            }
            if (_signalRHub != null)
            {
                DateTime date;
                DateTime.TryParseExact(longdate, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
                _signalRHub.Clients.All.logReuqest(date.ToString("yyyy-MM-dd HH:mm:ss"), message);
            }
        }

        public static void Log(string longdate, string logLevel, string callsite, string message)
        {
            if (_signalRHub == null)
            {
                _signalRHub = GlobalHost.ConnectionManager.GetHubContext<LogHub>();
            }
            if (_signalRHub != null)
            {
                DateTime date;
                DateTime.TryParseExact(longdate,dateFormat,CultureInfo.InvariantCulture,DateTimeStyles.None, out date);
                
                _signalRHub.Clients.All.logEvent(date.ToString("yyyy-MM-dd HH:mm:ss"), logLevel, callsite, message);
            }
        }
    }
}
