namespace LANSearch.Models
{
    public class ErrorModel
    {
        public int StatusCode { get; set; }

        public string ErrorType { get; set; }

        public string ErrorTitle { get; set; }

        public string ErrorMessage { get; set; }
    }
}