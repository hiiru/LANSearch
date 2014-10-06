using LANSearch.Data.Notification;
using LANSearch.Data.User;
using Microsoft.AspNet.SignalR;

namespace LANSearch.Hubs
{
    //[Authorize()]
    public class NotificationHub : Hub
    {
        private static IHubContext _signalRHub;

        public static void SearchNotification(NotificationEvent notificationEvent)
        {
            if (_signalRHub == null)
                _signalRHub = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
            if (_signalRHub == null) return;
            var userClient = _signalRHub.Clients.User(notificationEvent.UserName);
            if (userClient == null) return;
            userClient.notify(notificationEvent.NotificationId, notificationEvent.Name, notificationEvent.SearchUrl, notificationEvent.Items);
        }

        public static void SendAnnouncement(string title, string message, string type = "info")
        {
            if (_signalRHub == null)
                _signalRHub = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
            if (_signalRHub == null) return;
            _signalRHub.Clients.All.announcement(title, message, type);
        }

        public static void SendAdministratorMessage(string username, string message, string type)
        {
            if (_signalRHub == null)
                _signalRHub = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
            if (_signalRHub == null) return;
            var userClient = _signalRHub.Clients.User(username);
            if (userClient == null) return;
            userClient.administratorMessage(message, type);
        }

        [Authorize(Roles = UserRoles.ADMIN)]
        public void SendAnnouncementMessage(string title, string message, string type)
        {
            Clients.Others.announcement(title, message, type);
            Clients.Caller.cmdConfirm("Announcement is sent");
        }

        [Authorize(Roles = UserRoles.ADMIN)]
        public void SendAdminMessage(string username, string message, string type)
        {
            Clients.User(username).administratorMessage(message, type);
            Clients.Caller.cmdConfirm("Message is sent");
        }
    }
}