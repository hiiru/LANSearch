namespace LANSearch.Models.Admin.User
{
    public class UserDetailModel
    {
        public Data.User.User User { get; set; }

        public string Error { get; set; }

        public bool IsCreation { get; set; }
    }
}