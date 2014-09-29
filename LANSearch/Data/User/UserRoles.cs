namespace LANSearch.Data.User
{
    public static class UserRoles
    {
        public const string UNVERIFIED = "unverified";
        public const string MEMBER = "member";
        public const string SERVER_OWNER = "serverowner";
        public const string ADMIN = "admin";

        public static bool IsValidRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role)) return false;

            return role == UNVERIFIED || role == MEMBER || role == SERVER_OWNER || role == ADMIN;
        }
    }
}