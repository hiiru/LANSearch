using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LANSearch.Data.Notification;
using LANSearch.Data.User;
using Microsoft.AspNet.SignalR;

namespace LANSearch.Hubs
{
    [CustomAuthorize()]
    public class NotificationHub
    {
        private static IHubContext _signalRHub;

        public static void PushNotification(NotificationEvent notificationEvent)
        {
            if (_signalRHub == null)
                _signalRHub = GlobalHost.ConnectionManager.GetHubContext<LogHub>();
            if (_signalRHub == null) return;
            var userClient=_signalRHub.Clients.User(notificationEvent.UserId.ToString());
            if (userClient == null) return;
            userClient.eventNotification(notificationEvent.Name, notificationEvent.SearchUrl, notificationEvent.Items);
        }
    }
}
