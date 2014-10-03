using System;

namespace LANSearch.Data.Server
{
    public class Server
    {
        public int Id { get; set; }

        /// <summary>
        /// For future use, currently only the value 1 is valid (1=ftp)
        /// </summary>
        public int Type { get; set; }

        public string TypeName { get { return Type == 1 ? "FTP" : "-"; } }

        public int OwnerId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Address { get; set; }

        public int Port { get; set; }

        public bool HasLogin
        {
            get { return !string.IsNullOrWhiteSpace(Login); }
        }

        public string Login { get; set; }

        public string Password { get; set; }

        public bool Deleted { get; set; }

        /// <summary>
        /// Will be shown on search results and results are no longer linked, can be set by Owner/Admin.
        /// This will overwrite online status
        /// </summary>
        public bool Closed { get; set; }

        /// <summary>
        /// This will hide the server from all searches, can be set by Owner/Admin.
        /// </summary>
        public bool Hidden { get; set; }

        /// <summary>
        /// This will prevent scans of the server, can be set by Owner/Admin.
        /// </summary>
        public bool NoScans { get; set; }

        /// <summary>
        /// Will be shown on search results, automatically set/cleared by Cralwer Job
        /// </summary>
        public bool Online { get; set; }

        public DateTime Created { get; set; }

        public DateTime ScanDateFirst { get; set; }

        public DateTime ScanDateLast { get; set; }

        public DateTime ScanFailedLastDate { get; set; }

        public int ScanFailedAttempts { get; set; }

        public string ScanFailedMessage { get; set; }

        public string Url
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Login))
                {
                    return string.Format("ftp://{0}:{1}", Address, Port);
                }
                return string.Format("ftp://{2}:{3}@{0}:{1}", Address, Port, Login, Password);
            }
        }

        /// <summary>
        /// Admin Note for the server, set and read by admin only (no public use)
        /// </summary>
        public string AdminNote { get; set; }

        /* for future use
        /// <summary>
        /// Admin Public Notice, set by the admin for public information about the server.
        /// </summary>
        public string AdminPublicNotice { get; set; }

        /// <summary>
        /// Admin Public Notice Type is the used bootstrap css class (e.g. danger, info), set by the admin.
        /// </summary>
        public string AdminPublicNoticeType { get; set; }

        /// <summary>
        /// Server is closed by admin
        /// </summary>
        public bool AdminClosed { get; set; }

        /// <summary>
        /// Public Notice, set by the owner for public information about the server.
        /// </summary>
        public string PublicNotice { get; set; }

        /// <summary>
        /// Public Notice Type is the used bootstrap css class (e.g. danger, info), set by the owner.
        /// </summary>
        public string PublicNoticeType { get; set; }
        */
    }
}