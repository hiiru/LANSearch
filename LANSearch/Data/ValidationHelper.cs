using System.Text.RegularExpressions;

namespace LANSearch.Data
{
    public static class ValidationHelper
    {
        private static Regex EmailRegex = new Regex("[A-Z0-9._%+-]+@[A-Z0-9.-]+\\.[A-Z]{2,4}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool EmailValid(string email)
        {
            return EmailRegex.IsMatch(email);
        }

        private static Regex RegexIp = new Regex(@"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$", RegexOptions.Compiled);

        public static bool IsValidIP(string ip)
        {
            return RegexIp.IsMatch(ip);
        }
    }
}