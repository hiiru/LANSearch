namespace LANSearch.Models.Admin.Server
{
    public class ServerDetailModel
    {
        public Data.Server.Server Server { get; set; }

        public string Error { get; set; }

        public bool IsCreation { get; set; }
    }
}