using LANSearch.Data;
using LANSearch.Models.BaseModels;
using System.Collections.Generic;

namespace LANSearch.Models.Admin.User
{
    public class UserListModel : BaseListModel
    {
        public UserListModel(UrlBuilder urlBuilder)
            : base(urlBuilder)
        {
        }

        public List<Data.User.User> Users { get; set; }
    }
}