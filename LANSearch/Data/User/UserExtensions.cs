namespace LANSearch.Data.User
{
    public static class UserExtensions
    {
        public static string GetAdminUrl(this User user)
        {
            if (user == null) return null;
            return string.Format("/Admin/User/Detail/{0}", user.Id);
        }
    }
}