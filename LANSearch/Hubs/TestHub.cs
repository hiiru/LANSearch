using Microsoft.AspNet.SignalR;

namespace LANSearch.Hubs
{
    public class TestHub : Hub
    {
        private static IHubContext _signalRHub;

        public static void LogRequest(string longdate, string message)
        {
            if (_signalRHub == null)
            {
                _signalRHub = GlobalHost.ConnectionManager.GetHubContext<TestHub>();
            }
            if (_signalRHub != null)
            {
                _signalRHub.Clients.All.logReuqest(longdate, message);
            }
        }

        public static void Log(string longdate, string logLevel, string callsite, string message)
        {
            if (_signalRHub == null)
            {
                _signalRHub = GlobalHost.ConnectionManager.GetHubContext<TestHub>();
            }
            if (_signalRHub != null)
            {
                _signalRHub.Clients.All.logEvent(longdate, logLevel, callsite, message);
            }
        }
    }
}