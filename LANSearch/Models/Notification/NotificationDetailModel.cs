using LANSearch.Data.Notification;
using System;
using System.Collections.Generic;

namespace LANSearch.Models.Notification
{
    public class NotificationDetailModel
    {
        public NotificationDetailModel()
        {
            Errors = new Dictionary<string, string>();
        }

        public bool ActiveLimitReached { get; set; }

        public Data.Notification.Notification Notification { get; set; }

        public string OwnerName { get; set; }

        public string OwnerAdminUrl { get; set; }

        public bool IsCreation { get; set; }

        public bool IsAdmin { get; set; }

        public Dictionary<string, string> Errors { get; set; }

        public string Keyword { get; set; }

        public bool ValidateNotification()
        {
            if (Notification == null)
                throw new InvalidOperationException("Notification is null");
            if (string.IsNullOrWhiteSpace(Notification.Name))
            {
                Errors["notName"] = "A notification name is required";
            }
            if (Notification.Type == NotificationType.Invalid)
            {
                Errors["notType"] = "Please select at least 1 Type.";
            }

            return Errors.Count == 0;
        }
    }
}