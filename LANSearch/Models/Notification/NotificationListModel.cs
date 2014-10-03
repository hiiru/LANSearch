using System.Collections.Generic;

namespace LANSearch.Models.Notification
{
    public class NotificationListModel
    {
        public List<Data.Notification.Notification> Notifications { get; set; }

        public bool ActiveLimitReached { get; set; }
    }
}