namespace LANSearch.Models.Admin
{
    public class SetupModel
    {
        public string Error { get; set; }

        public Data.User.User User { get; set; }

        public string AesPass { get; set; }

        public string AesSalt { get; set; }

        public string HmacPass { get; set; }

        public string HmacSalt { get; set; }
    }
}