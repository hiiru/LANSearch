using LANSearch.Data.Notification;
using Microsoft.AspNet.SignalR;

namespace LANSearch.Hubs
{
    [Authorize()]
    public class NotificationHub
    {
        private static IHubContext _signalRHub;

        public static void PushNotification(NotificationEvent notificationEvent)
        {
            if (_signalRHub == null)
                _signalRHub = GlobalHost.ConnectionManager.GetHubContext<LogHub>();
            if (_signalRHub == null) return;
            var userClient = _signalRHub.Clients.User(notificationEvent.UserId.ToString());
            if (userClient == null) return;
            userClient.eventNotification(notificationEvent.Name, notificationEvent.SearchUrl, notificationEvent.Items);
        }
    }
}