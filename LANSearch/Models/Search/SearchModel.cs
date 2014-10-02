using LANSearch.Data;
using LANSearch.Models.BaseModels;
using System.Collections.Generic;

namespace LANSearch.Models.Search
{
    public class SearchModel : BaseListModel
    {
        public SearchModel(UrlBuilder urlBuilder)
            : base(urlBuilder)
        {
        }

        public SearchModel(string error)
            : base(null)
        {
            HasError = true;
            ErrorMessage = error;
        }

        public bool HasError { get; set; }

        public string ErrorMessage { get; set; }

        public string Keyword { get; set; }

        public bool HasResults { get { return Count > 0; } }

        public List<SearchFile> Results { get; set; }

        public List<SearchFilter> Filters { get; set; }

        public bool HasFilter { get { return Filters != null && Filters.Count > 0; } }
        public bool HasSearchParameters { get; set; }

        public string GetNotificationUrl()
        {
            var urlbuilder = _urlBuilder.Clone();
            urlbuilder.Remove("p").Remove("ps");
            urlbuilder.BasePath = "/Member/Notification/Create";
            return urlbuilder.ToString();
        }
    }
}