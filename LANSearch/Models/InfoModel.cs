using LANSearch.Data;

namespace LANSearch.Models
{
    public class InfoModel
    {
        public bool IsSuccess { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Location { get; set; }

        public string Text { get; set; }

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorName = "Name is missing.";
            }
            else if (Name.Length < 3)
            {
                ErrorName = "Name is too short (at least 3 characters).";
            }
            else if (Name.Length > 50)
            {
                ErrorName = "Name is too long (only up to 50 characters are allowed).";
            }

            if (string.IsNullOrWhiteSpace(Text))
            {
                ErrorText = "Text is missing.";
            }
            else if (Text.Length < 3)
            {
                ErrorText = "Text is too short (at least 3 characters)";
            }
            else if (Text.Length > 1000)
            {
                ErrorText = "Text is too long, only 1000 characters are allowed.";
            }

            if (!string.IsNullOrWhiteSpace(Email))
            {
                if (Email.Length > 50)
                    ErrorEmail = "Email is too long (only up to 50 characters are allowed).";
                else if (!ValidationHelper.EmailValid(Email))
                    ErrorEmail = "Invalid Email Format, please enter it in name@domain.tld format";
            }

            if (Location != null && Location.Length > 50)
                ErrorLocation = "Location is too long (only up to 50 characters are allowed).";

            return ErrorName == null && ErrorText == null && ErrorEmail == null && ErrorLocation == null;
        }

        public string ErrorName { get; set; }

        public string ErrorEmail { get; set; }

        public string ErrorLocation { get; set; }

        public string ErrorText { get; set; }
    }
}