using System;

namespace LANSearch.Data.Notification
{
    [Flags]
    public enum NotificationType
    {
        Invalid = 0,
        Mail = 1,
        Html5 = 2,
    }
}