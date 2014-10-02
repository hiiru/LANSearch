using System;

namespace LANSearch.Data.Notification
{
    public class Notification
    {
        public int Id { get; set; }

        public int OwnerId { get; set; }

        /// <summary>
        /// Set to server's ID if search only affects a single server, otherwise 0
        /// </summary>
        public int ServerId { get; set; }

        public string Name { get; set; }

        public string SolrQuery { get; set; }
        public string SearchUrl { get; set; }

        public bool Disabled { get; set; }

        public bool Deleted { get; set; }

        public NotificationType Type { get; set; }

        public DateTime Created { get; set; }

        public DateTime LastExecution { get; set; }

        /// <summary>
        /// This should be set to a date after the lan, to prevent spam later on
        /// </summary>
        public DateTime Expiration { get; set; }
    }
}