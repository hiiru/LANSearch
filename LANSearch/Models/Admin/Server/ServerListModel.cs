using LANSearch.Data;
using LANSearch.Models.BaseModels;
using System.Collections.Generic;

namespace LANSearch.Models.Admin.Server
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