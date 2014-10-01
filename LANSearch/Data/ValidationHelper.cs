using System.Text.RegularExpressions;

namespace LANSearch.Data
{
    public static class ValidationHelper
    {
        private static readonly Regex EmailRegex = new Regex("[A-Z0-9._%+-]+@[A-Z0-9.-]+\\.[A-Z]{2,4}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool EmailValid(string email)
        {
            if (email == null) return false;
            return EmailRegex.IsMatch(email);
        }

        private static readonly Regex RegexIp = new Regex(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", RegexOptions.Compiled);

        public static bool IsValidIP(string ip)
        {
            if (ip == null) return false;
            return RegexIp.IsMatch(ip);
        }
    }
}