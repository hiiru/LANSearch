namespace LANSearch.Data.Jobs.Ftp
{
    public class FtpStatus
    {
        public bool IsOk { get { return ErrorType == FtpErrorType.None; } }

        public int ErrorFtpCode { get; set; }

        public string ErrorFtpMessage { get; set; }

        public FtpErrorType ErrorType { get; set; }

        public enum FtpErrorType
        {
            None = -1,
            Unknown = 0,

            /// <summary>
            /// Server is offline, can't connect.
            /// </summary>
            Offline,

            /// <summary>
            /// Authentication problem
            /// </summary>
            Login,

            /// <summary>
            /// Server rejected connection, possibly closed or full
            /// </summary>
            Rejected
        }
    }
}