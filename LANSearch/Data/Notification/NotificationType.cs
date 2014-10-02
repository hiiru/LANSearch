using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LANSearch.Data.Notification
{
    [Flags]
    public enum NotificationType
    {
        Invalid=0,
        Mail=1,
        Html5=2,
    }
}
