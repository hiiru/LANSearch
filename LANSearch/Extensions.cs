using Nancy;
using Nancy.Helpers;

namespace LANSearch
{
    public static class Extensions
    {
        public static string ToReturnUrl(this Url url)
        {
            return string.Concat("?returnUrl=", url.Path, "?", HttpUtility.UrlEncode(url.Query));
        }

        public static bool ToBool(this string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            switch (s.ToLower())
            {
                case "t":
                case "true":
                case "y":
                case "yes":
                case "on":
                case "1":
                    return true;

                default:
                    return false;
            }
        }
    }
}