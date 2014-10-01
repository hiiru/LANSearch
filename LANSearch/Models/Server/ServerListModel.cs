using System.Collections.Generic;
using LANSearch.Data;
using LANSearch.Models.BaseModels;

namespace LANSearch.Models.Server
{
    public class ServerListModel : BaseListModel
    {
        public ServerListModel(UrlBuilder urlBuilder)
            : base(urlBuilder)
        {
        }

        public List<Data.Server.Server> Servers { get; set; }
    }
}