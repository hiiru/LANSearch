namespace LANSearch.Models.Search
{
    public class SearchFile
    {
        public string Id { get; set; }

        public string Login { get; set; }

        public string Path { get; set; }

        public string Extension { get; set; }

        public string Date { get; set; }

        public string Name { get; set; }

        public string Size { get; set; }

        public LANSearch.Data.Server.Server Server { get; set; }

        public string ServerName
        {
            get
            {
                if (Server == null) return "";
                if (!string.IsNullOrWhiteSpace(Server.Name))
                    return Server.Name;
                return string.Format("{0}:{1}", Server.Address, Server.Port);
            }
        }

        public string Url
        {
            get
            {
                if (Server == null) return "#";
                return string.Format("{0}{1}", Server.Url, Path);
            }
        }
    }
}