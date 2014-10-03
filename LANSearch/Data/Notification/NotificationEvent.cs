using System;
using System.Collections.Generic;

namespace LANSearch.Data.Notification
{
    public class NotificationEvent
    {
        public int NotificationId { get; set; }

        public string Name { get; set; }

        public string SearchUrl { get; set; }

        public List<NotificationEventItem> Items { get; set; }

        public DateTime NotificationTime { get; set; }
        
        public string UserName { get; set; }

        public string UserEmail { get; set; }
    }

    public class NotificationEventItem
    {
        public string FileName { get; set; }

        public string FileSize { get; set; }

        public string FileUrl { get; set; }

        public string ServerName { get; set; }
    }
}