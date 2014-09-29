namespace LANSearch.Models
{
    public class LoginModel
    {
        public string LoginUsername { get; set; }

        public string LoginErrorUsername { get; set; }

        public string LoginErrorPassword { get; set; }

        public string ReturnUrl { get; set; }

        public bool IsLoggedIn { get; set; }

        public string RegisterUser { get; set; }

        public string RegisterEmail { get; set; }

        public string RegisterErrorUser { get; set; }

        public string RegisterErrorEmail { get; set; }

        public string RegisterErrorPassword { get; set; }
    }
}