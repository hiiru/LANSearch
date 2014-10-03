using LANSearch.Data.User;

namespace LANSearch.Models.Member
{
    public class ProfileModel
    {
        public User User { get; set; }

        public bool ChangePassErrorOldPass { get; set; }

        public bool ChangePassErrorNewPass { get; set; }

        public string ChangePassErrorNewPassText { get; set; }

        public bool ChangePassSuccess { get; set; }

        public bool ChangeMailErrorPass { get; set; }

        public bool ChangeMailErrorMail { get; set; }

        public bool ChangeMailSuccess { get; set; }

        public bool ConfirmError { get; set; }

        public bool ConfirmAccountSuccess { get; set; }

        public bool ConfirmMailInvalid { get; set; }

        public bool ConfirmMailAlreadyUsed { get; set; }

        public bool ConfirmResendSuccess { get; set; }
    }
}